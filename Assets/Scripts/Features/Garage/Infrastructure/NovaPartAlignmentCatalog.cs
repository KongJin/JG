using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Garage.Infrastructure
{
    [CreateAssetMenu(fileName = "NovaPartAlignmentCatalog", menuName = "Garage/Nova Part Alignment Catalog")]
    public sealed class NovaPartAlignmentCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string partId;
            [SerializeField] private NovaPartSlot slot;
            [SerializeField] private Vector3 boundsSize;
            [SerializeField] private Vector3 boundsCenter;
            [SerializeField] private float normalizedScale;
            [SerializeField] private Vector3 pivotOffset;
            [SerializeField] private Vector3 socketOffset;
            [SerializeField] private Vector3 socketEuler;
            [SerializeField] private string qualityFlag;
            [SerializeField] private string reviewReason;

            public string PartId => partId;
            public NovaPartSlot Slot => slot;
            public Vector3 BoundsSize => boundsSize;
            public Vector3 BoundsCenter => boundsCenter;
            public float NormalizedScale => normalizedScale;
            public Vector3 PivotOffset => pivotOffset;
            public Vector3 SocketOffset => socketOffset;
            public Vector3 SocketEuler => socketEuler;
            public string QualityFlag => qualityFlag;
            public string ReviewReason => reviewReason;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
    }
}
