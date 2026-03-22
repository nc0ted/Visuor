using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace AudioVisualization
{
    [ExecuteAlways]
    [RequireComponent(typeof(AudioParse))]
    [RequireComponent(typeof(BandsVisualizer))]
    public class AudioVisualizerSetup : MonoBehaviour
    {
        [Header("Audio Parse Props")]
        [SerializeField] private int bandsCount = 16;
        [SerializeField] private int samplesCount = 512;

        [Header("Setup Props")]
        [SerializeField] private Band bandPrefab;
        [Range(0, 100)][SerializeField] private float indent = 1f;
        [SerializeField] private float baseUIScale = 1f;
        [SerializeField] private Transform originPosition;

        private AudioParse _audioParser;
        private BandsVisualizer _bandsVisualizer;
        private Transform _normalRoot;
        private Transform _reflectedRoot;

        private bool IsUI => bandPrefab != null && bandPrefab.GetComponent<RectTransform>() != null;

        private void OnValidate()
        {
            UpdateVisualizerReferences();
            if (_bandsVisualizer != null)
            {
                _bandsVisualizer.indent = indent;
                _bandsVisualizer.UpdateBandPositions();
            }
        }

        private void UpdateVisualizerReferences()
        {
            if (_audioParser == null) _audioParser = GetComponent<AudioParse>();
            if (_bandsVisualizer == null) _bandsVisualizer = GetComponent<BandsVisualizer>();
        }

        [Button("Setup Normal")]
        private void SetupNormal() => CreateBands(false);

        [Button("Setup Reflected")]
        private void SetupReflected() => CreateBands(true);

        [Button("Delete Normal")]
        private void DeleteNormal() => DeleteRoot(false);

        [Button("Delete Reflected")]
        private void DeleteReflected() => DeleteRoot(true);

        private void DeleteRoot(bool invert)
        {
            string rootName = invert ? "Reflected_Root" : "Normal_Root";
            Transform root = invert ? _reflectedRoot : _normalRoot;

            if (root == null && originPosition != null) root = originPosition.Find(rootName);
            if (root != null) DestroyImmediate(root.gameObject);

            if (invert) _reflectedRoot = null;
            else _normalRoot = null;

            SyncWithVisualizer();
        }

        private void CreateBands(bool invert)
        {
            if (bandPrefab == null || originPosition == null) return;

            if (bandPrefab.gameObject.scene.name != null && (bandPrefab.transform == originPosition || bandPrefab.transform.IsChildOf(originPosition) || originPosition.IsChildOf(bandPrefab.transform)))
            {
                Debug.LogError("AudioVisualizerSetup: Recursive setup detected! bandPrefab cannot be the same as or a child/parent of originPosition when it's a scene object.");
                return;
            }

            UpdateVisualizerReferences();

            if (_audioParser != null)
            {
                _audioParser.bandsCount = bandsCount;
                _audioParser.samplesCount = samplesCount;
            }

            string rootName = invert ? "Reflected_Root" : "Normal_Root";
            Transform existingRoot = invert ? _reflectedRoot : _normalRoot;
            if (existingRoot == null && originPosition != null) existingRoot = originPosition.Find(rootName);
            if (existingRoot != null) DestroyImmediate(existingRoot.gameObject);

            GameObject setupParent = new GameObject(rootName);
            setupParent.transform.SetParent(originPosition, false);
            if (invert) _reflectedRoot = setupParent.transform;
            else _normalRoot = setupParent.transform;

            GameObject bandsContainer = new GameObject("Bands_Container");
            bandsContainer.transform.SetParent(setupParent.transform, false);

            if (IsUI)
            {
                setupParent.AddComponent<RectTransform>();
                bandsContainer.AddComponent<RectTransform>();
                setupParent.transform.localScale = new Vector3(baseUIScale, baseUIScale, 1);
            }

            for (int i = 0; i < bandsCount; i++)
            {
                Band band = Instantiate(bandPrefab, bandsContainer.transform, false);
                band.name = $"Band_{i}";
                band.band = i;
                band.transform.localScale = Vector3.one;
            }

            if (invert) bandsContainer.transform.localScale = new Vector3(-1, 1, 1);

            SyncWithVisualizer();
        }

        private void SyncWithVisualizer()
        {
            if (_bandsVisualizer == null) return;

            List<Transform> allBands = new List<Transform>();
            foreach (Transform child in originPosition)
            {
                var foundBands = child.GetComponentsInChildren<Band>();
                foreach (var b in foundBands) allBands.Add(b.transform);
            }

            _bandsVisualizer.indent = indent;
            _bandsVisualizer.Initialize(allBands.ToArray());
            _bandsVisualizer.UpdateBandPositions();
        }
    }
}