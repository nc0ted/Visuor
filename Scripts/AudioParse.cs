using NaughtyAttributes;
using UnityEngine;

namespace AudioVisualization
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioParse : MonoBehaviour
    {
        public enum ScalingStrategy { Linear, Logarithmic, Musical }

        [Header("Main Settings")]
        [SerializeField] private ScalingStrategy strategy = ScalingStrategy.Musical;
        [SerializeField] private FFTWindow fftWindow = FFTWindow.Blackman;
        [SerializeField] private AudioSource audioSource;

        [Header("Global Multipliers")]
        [SerializeField] private float baseGain = 50f;
        [Range(0f, 1f)][SerializeField] private float highFreqComp = 0.5f;

        [Header("Buffer Physics")]
        [SerializeField] private float initialFallSpeed = 1f;
        [SerializeField] private float gravityForce = 1f;

        [ReadOnly] public float[] FreqBand;
        [ReadOnly] public float[] BandBuffer;

        [HideInInspector] public int bandsCount = 16;
        [HideInInspector] public int samplesCount = 512;

        private float[] _samples;
        private float[] _bufferDecrease;

        private void Start()
        {
            if (bandsCount <= 0) bandsCount = 16;
            if (samplesCount <= 0) samplesCount = 512;

            _samples = new float[samplesCount];
            FreqBand = new float[bandsCount];
            BandBuffer = new float[bandsCount];
            _bufferDecrease = new float[bandsCount];
        }

        private void Reset()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (audioSource == null) return;
            audioSource.GetSpectrumData(_samples, 0, fftWindow);

            switch (strategy)
            {
                case ScalingStrategy.Linear:
                    CalculateLinear();
                    break;
                case ScalingStrategy.Logarithmic:
                    CalculateLogarithmic();
                    break;
                case ScalingStrategy.Musical:
                    CalculateMusical();
                    break;
            }

            ApplyBandBuffer();
        }

        private void CalculateLinear()
        {
            int samplesPerBand = samplesCount / bandsCount;
            for (int i = 0; i < bandsCount; i++)
            {
                float average = 0;
                for (int j = 0; j < samplesPerBand; j++)
                {
                    average += _samples[i * samplesPerBand + j];
                }
                FreqBand[i] = (average / samplesPerBand) * baseGain * 2f;
            }
        }

        private void CalculateLogarithmic()
        {
            float logStep = Mathf.Log10(samplesCount / 2f) / bandsCount;
            for (int i = 0; i < bandsCount; i++)
            {
                int lowBound = (int)Mathf.Pow(10, i * logStep);
                int highBound = (int)Mathf.Pow(10, (i + 1) * logStep);
                if (highBound <= lowBound) highBound = lowBound + 1;

                float sum = 0;
                for (int j = lowBound; j < Mathf.Min(highBound, _samples.Length); j++)
                {
                    sum += _samples[j];
                }
                FreqBand[i] = (sum / (highBound - lowBound)) * baseGain;
            }
        }

        private void CalculateMusical()
        {
            int currentSample = 0;
            for (int i = 0; i < bandsCount; i++)
            {
                float average = 0;
                int sampleCount = (int)Mathf.Pow(2, i / (bandsCount / 8f)) + 2;

                for (int j = 0; j < sampleCount; j++)
                {
                    if (currentSample < _samples.Length)
                    {
                        float weight = 1f + (currentSample * highFreqComp * 0.1f);
                        average += _samples[currentSample] * weight;
                        currentSample++;
                    }
                }

                average /= sampleCount;
                float falloff = Mathf.Lerp(1.2f, 0.5f, (float)i / bandsCount);
                FreqBand[i] = average * baseGain * falloff;
            }
        }

        private void ApplyBandBuffer()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < bandsCount; ++i)
            {
                if (FreqBand[i] > BandBuffer[i])
                {
                    BandBuffer[i] = FreqBand[i];
                    _bufferDecrease[i] = initialFallSpeed * dt;
                }
                else
                {
                    BandBuffer[i] -= _bufferDecrease[i];
                    _bufferDecrease[i] += gravityForce * dt;
                }
                if (BandBuffer[i] < 0) BandBuffer[i] = 0;
            }
        }
    }
}