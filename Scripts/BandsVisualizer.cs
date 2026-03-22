using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace AudioVisualization
{
    [ExecuteAlways]
    [RequireComponent(typeof(AudioParse))]
    public class BandsVisualizer : MonoBehaviour
    {
        [BurstCompile]
        private struct BandVisualizerJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<float> YScales;
            [ReadOnly] public bool CutY;
            [ReadOnly] public bool InvertCutY;

            public void Execute(int index, TransformAccess transform)
            {
                Vector3 s = transform.localScale;
                float currentY = math.abs(YScales[index]);

                transform.localScale = new Vector3(s.x, currentY, s.z);

                if (CutY)
                {
                    float yPosition = InvertCutY ? -currentY * 0.5f : currentY * 0.5f;
                    Vector3 lp = transform.localPosition;
                    transform.localPosition = new Vector3(lp.x, yPosition, lp.z);
                }
                else
                {
                    Vector3 lp = transform.localPosition;
                    transform.localPosition = new Vector3(lp.x, 0, lp.z);
                }
            }
        }

        [Header("Settings")]
        [SerializeField] private float scaleMultiplier = 1f;
        [SerializeField] private bool useBuffer = true;

        [Header("Positioning (CutY)")]
        [SerializeField] private bool cutY;
        [SerializeField] private bool invertCutY;

        [Header("References")]
        [SerializeField] private AudioParse _audioParser;
        [HideInInspector] public float indent;
        [HideInInspector] public Transform[] bandsTransforms;

        private TransformAccessArray _accessArray;
        private NativeArray<float> _yScales;
        private bool _isReady;

        private void Start()
        {
            if (!Application.isPlaying) return;

            if (_audioParser == null) _audioParser = GetComponent<AudioParse>();

            if (bandsTransforms != null && bandsTransforms.Length > 0)
            {
                Initialize(bandsTransforms);
            }
        }

        public void Initialize(Transform[] transforms)
        {
            DisposeNative();
            bandsTransforms = transforms;

            if (bandsTransforms == null || bandsTransforms.Length == 0) return;

            _accessArray = new TransformAccessArray(bandsTransforms);
            _yScales = new NativeArray<float>(bandsTransforms.Length, Allocator.Persistent);

            for (int i = 0; i < _yScales.Length; i++) _yScales[i] = 1f;

            _isReady = true;
            Debug.Log($"Visualizer Ready: {bandsTransforms.Length} bands.");
        }

        private void Reset()
        {
            if (_audioParser == null) _audioParser = GetComponent<AudioParse>();
        }

        private void Update()
        {
            if (!Application.isPlaying || !_isReady || _audioParser == null) return;

            float[] dataSource = useBuffer ? _audioParser.BandBuffer : _audioParser.FreqBand;

            if (dataSource == null || dataSource.Length == 0) return;

            for (int i = 0; i < bandsTransforms.Length; i++)
            {
                if (bandsTransforms[i] == null) continue;

                int freqIdx = 0;
                if (bandsTransforms[i].TryGetComponent<Band>(out var b)) freqIdx = b.band;
                freqIdx = Mathf.Clamp(freqIdx, 0, dataSource.Length - 1);

                float targetY = dataSource[freqIdx] * scaleMultiplier;
                _yScales[i] = targetY;
            }

            var job = new BandVisualizerJob
            {
                YScales = _yScales,
                CutY = cutY,
                InvertCutY = invertCutY
            };

            job.Schedule(_accessArray).Complete();
        }

        public void UpdateBandPositions()
        {
            if (bandsTransforms == null || bandsTransforms.Length == 0) return;

            if (bandsTransforms[0] == null) return;

            float width = 1f;
            bool isUI = bandsTransforms[0] is RectTransform;
            if (isUI) width = ((RectTransform)bandsTransforms[0]).rect.width;
            else width = bandsTransforms[0].localScale.x;

            float step = width + indent;

            for (int i = 0; i < bandsTransforms.Length; i++)
            {
                if (bandsTransforms[i] == null) continue;
                int localIdx = 0;
                if (bandsTransforms[i].TryGetComponent<Band>(out var b)) localIdx = b.band;

                Vector3 pos = new Vector3(localIdx * step, 0, 0);
                if (isUI) ((RectTransform)bandsTransforms[i]).anchoredPosition = pos;
                else bandsTransforms[i].localPosition = pos;
            }
        }

        private void OnDisable() => DisposeNative();
        private void DisposeNative()
        {
            if (_accessArray.isCreated) _accessArray.Dispose();
            if (_yScales.IsCreated) _yScales.Dispose();
            _isReady = false;
        }
    }
}