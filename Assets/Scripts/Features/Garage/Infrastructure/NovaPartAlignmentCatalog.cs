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
            [SerializeField] private float normalizedScale;
            [SerializeField] private Vector3 pivotOffset;
            [SerializeField] private bool hasVisualBounds;
            [SerializeField] private Vector3 visualBoundsCenter;
            [SerializeField] private Vector3 visualBoundsMin;
            [SerializeField] private Vector3 visualBoundsMax;
            [SerializeField] private Vector3 socketOffset;
            [SerializeField] private Vector3 socketEuler;
            [SerializeField] private bool hasGxTreeSocket;
            [SerializeField] private Vector3 gxTreeSocketOffset;
            [SerializeField] private string gxTreeSocketName;
            [SerializeField] private bool hasXfiMetadata;
            [SerializeField] private string xfiPath;
            [SerializeField] private string xfiHeader;
            [SerializeField] private string xfiHeaderKind;
            [SerializeField] private string xfiAttachSlot;
            [SerializeField] private string xfiAttachVariant;
            [SerializeField] private int xfiTransformCount;
            [SerializeField] private string xfiTransformTranslations;
            [SerializeField] private int xfiDirectionRangeCount;
            [SerializeField] private string xfiDirectionRanges;
            [SerializeField] private bool hasXfiAttachSocket;
            [SerializeField] private Vector3 xfiAttachSocketOffset;
            [SerializeField] private bool hasFrameTopSocket;
            [SerializeField] private Vector3 frameTopSocketOffset;
            [SerializeField] private string xfiSocketQuality;
            [SerializeField] private string xfiSocketName;
            [SerializeField] private string qualityFlag;
            [SerializeField] private string reviewReason;
            [SerializeField] private string assemblySourceSlotCode;
            [SerializeField] private string assemblySlotMode;
            [SerializeField] private string assemblyAnchorMode;
            [SerializeField] private Vector3 assemblyLocalOffset;
            [SerializeField] private Vector3 assemblyLocalEuler;
            [SerializeField] private Vector3 assemblyLocalScale = Vector3.one;
            [SerializeField] private string assemblyConfidence;
            [SerializeField] private string assemblyEvidencePath;
            [SerializeField] private string assemblyReviewResult;

            public string PartId => partId;
            public NovaPartSlot Slot => slot;
            public float NormalizedScale => normalizedScale;
            public Vector3 PivotOffset => pivotOffset;
            public bool HasVisualBounds => hasVisualBounds;
            public Vector3 VisualBoundsCenter => visualBoundsCenter;
            public Vector3 VisualBoundsMin => visualBoundsMin;
            public Vector3 VisualBoundsMax => visualBoundsMax;
            public Vector3 SocketOffset => socketOffset;
            public Vector3 SocketEuler => socketEuler;
            public bool HasGxTreeSocket => hasGxTreeSocket;
            public Vector3 GxTreeSocketOffset => gxTreeSocketOffset;
            public string GxTreeSocketName => gxTreeSocketName;
            public bool HasXfiMetadata => hasXfiMetadata;
            public string XfiPath => xfiPath;
            public string XfiHeader => xfiHeader;
            public string XfiHeaderKind => xfiHeaderKind;
            public string XfiAttachSlot => xfiAttachSlot;
            public string XfiAttachVariant => xfiAttachVariant;
            public int XfiTransformCount => xfiTransformCount;
            public string XfiTransformTranslations => xfiTransformTranslations;
            public int XfiDirectionRangeCount => xfiDirectionRangeCount;
            public string XfiDirectionRanges => xfiDirectionRanges;
            public bool HasXfiAttachSocket => hasXfiAttachSocket;
            public Vector3 XfiAttachSocketOffset => xfiAttachSocketOffset;
            public bool HasFrameTopSocket => hasFrameTopSocket;
            public Vector3 FrameTopSocketOffset => frameTopSocketOffset;
            public string XfiSocketQuality => xfiSocketQuality;
            public string XfiSocketName => xfiSocketName;
            public string QualityFlag => qualityFlag;
            public string ReviewReason => reviewReason;
            public string AssemblySourceSlotCode => assemblySourceSlotCode;
            public string AssemblySlotMode => assemblySlotMode;
            public string AssemblyAnchorMode => assemblyAnchorMode;
            public Vector3 AssemblyLocalOffset => assemblyLocalOffset;
            public Vector3 AssemblyLocalEuler => assemblyLocalEuler;
            public Vector3 AssemblyLocalScale => assemblyLocalScale;
            public string AssemblyConfidence => assemblyConfidence;
            public string AssemblyEvidencePath => assemblyEvidencePath;
            public string AssemblyReviewResult => assemblyReviewResult;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
    }
}
