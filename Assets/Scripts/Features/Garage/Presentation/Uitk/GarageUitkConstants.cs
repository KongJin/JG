namespace Features.Garage.Presentation
{
    /// <summary>
    /// Garage UI Toolkit 관련 상수 중앙화.
    /// 하드코딩된 문자열/매직 넘버 재발 방지.
    /// </summary>
    internal static class GarageUitkConstants
    {
        /// <summary>
        /// 슬롯 관련 상수
        /// </summary>
        public static class Slots
        {
            public const int MaxCount = 8;
            public const string CardNamePrefix = "SlotCard";
            public const string CodeLabelPrefix = "SlotCode";
            public const string IconPrefix = "SlotIcon";
            public const string NameLabelPrefix = "SlotName";
            public const string ImageSuffix = "PreviewImage";

            public static string BuildCardName(int index) => $"{CardNamePrefix}{index:00}";
            public static string BuildCodeLabelName(int index) => $"{CodeLabelPrefix}{index:00}Label";
            public static string BuildIconName(int index) => $"{IconPrefix}{index:00}";
            public static string BuildIconGlyphName(int index) => $"{IconPrefix}{index:00}Glyph";
            public static string BuildNameLabelName(int index) => $"{NameLabelPrefix}{index:00}Label";
        }

        /// <summary>
        /// 부품 목록 관련 상수
        /// </summary>
        public static class Parts
        {
            public const int InitialRowCount = 8;
            public const string RowNamePrefix = "PartRow";
            public const string NameLabelSuffix = "NameLabel";
            public const string MetaLabelSuffix = "MetaLabel";
            public const string BadgeLabelSuffix = "BadgeLabel";
            public const string StatBarPrefix = "SelectedPartStat";

            public static string BuildRowName(int index) => $"{RowNamePrefix}{index:00}";
            public static string BuildRowNameLabelName(int index) => $"{RowNamePrefix}{index:00}{NameLabelSuffix}";
            public static string BuildRowMetaLabelName(int index) => $"{RowNamePrefix}{index:00}{MetaLabelSuffix}";
            public static string BuildRowBadgeLabelName(int index) => $"{RowNamePrefix}{index:00}{BadgeLabelSuffix}";
            public static string BuildStatRowName(int index) => $"{StatBarPrefix}Row{index:02}";
            public static string BuildStatLabelName(int index) => $"{StatBarPrefix}{index:02}Label";
            public static string BuildStatFillName(int index) => $"{StatBarPrefix}{index:02}Fill";
            public static string BuildStatValueName(int index) => $"{StatBarPrefix}{index:02}Value";
        }

        /// <summary>
        /// 프리뷰 관련 상수
        /// </summary>
        public static class Preview
        {
            public const int TextureSize = 512;
            public const int TextureMinSize = 128;
            public const int TextureAntiAliasing = 2;
            public const string HostSuffix = "Host";
            public const string TitleLabelSuffix = "TitleLabel";
            public const string PreviewImageSuffix = "PreviewImage";
            public const string PreviewLabelSuffix = "PreviewLabel";
        }

        /// <summary>
        /// 레이어 관련 상수
        /// </summary>
        public static class Layers
        {
            public const int AssemblyPreview = 29;
            public const int PartPreview = 30;
            public const int MaxLayer = 30;
            public const int MinLayer = 0;
        }

        /// <summary>
        /// 렌더링 관련 상수
        /// </summary>
        public static class Rendering
        {
            public const float SlotPreviewRendererSpacing = 1000f;
            public const int SlotPreviewRendererCount = 8;
            public const float RotationThreshold = 0.5f; // 렌더링 트리거용 회전 각도 차이

            // Assembly 프리뷰
            public const float AssemblyDefaultScale = 1.0f;
            public const float AssemblyScaleMultiplier = 1.32f;
            public const float AssemblyMinScale = 0.55f;
            public const float AssemblyMaxScale = 5.8f;

            // Part 프리뷰
            public const float PartDefaultScale = 1.12f;
            public const float PartMinScale = 0.45f;
            public const float PartMaxScale = 3.4f;
        }

        /// <summary>
        /// 피드백 아이콘 ID
        /// </summary>
        public static class Icons
        {
            public const string Add = "add";
            public const string Security = "security";
            public const string PrecisionManufacturing = "precision_manufacturing";
            public const string SmartToy = "smart_toy";
        }

        /// <summary>
        /// CSS 클래스 이름
        /// </summary>
        public static class Classes
        {
            public static class Slot
            {
                public const string Card = "slot-card";
                public const string CardActive = "slot-card--active";
                public const string CardEmpty = "slot-card--empty";
                public const string Code = "slot-code";
                public const string CodeActive = "slot-code--active";
                public const string CodeEmpty = "slot-code--empty";
                public const string Icon = "slot-icon";
                public const string IconActive = "slot-icon--active";
                public const string IconEmpty = "slot-icon--empty";
                public const string IconGlyph = "slot-icon-glyph";
                public const string IconGlyphActive = "slot-icon-glyph--active";
                public const string IconGlyphEmpty = "slot-icon-glyph--empty";
                public const string Name = "slot-name";
                public const string NameActive = "slot-name--active";
                public const string NameEmpty = "slot-name--empty";
            }

            public static class Part
            {
                public const string Row = "part-row";
                public const string RowSelected = "part-row--selected";
                public const string Badge = "part-row-badge";
                public const string BadgeSelected = "part-row-badge--selected";
                public const string BadgeReview = "part-row-badge--review";
            }

            public static class Radar
            {
                public const string Graph = "stat-radar-graph";
            }
        }
    }
}
