using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Garage.Infrastructure
{
    public enum NovaPartSlot
    {
        Frame,
        Firepower,
        Mobility,
    }

    [CreateAssetMenu(fileName = "NovaPartVisualCatalog", menuName = "Garage/Nova Part Visual Catalog")]
    public sealed class NovaPartVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string partId;
            [SerializeField] private NovaPartSlot slot;
            [SerializeField] private string displayName;
            [SerializeField] private string sourceRelativePath;
            [SerializeField] private string modelPath;
            [SerializeField] private int tier;
            [SerializeField] private bool needsNameReview;
            [SerializeField] private GameObject previewPrefab;
            [SerializeField] private ScriptableObject partAsset;

            public string PartId => partId;
            public NovaPartSlot Slot => slot;
            public string DisplayName => displayName;
            public string SourceRelativePath => sourceRelativePath;
            public string ModelPath => modelPath;
            public int Tier => tier;
            public bool NeedsNameReview => needsNameReview;
            public GameObject PreviewPrefab => previewPrefab;
            public ScriptableObject PartAsset => partAsset;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
    }
}
