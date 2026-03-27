namespace Shared.ErrorHandling
{
    public enum ErrorDisplayMode
    {
        Banner = 0,
        Modal = 1,
    }

    public readonly struct UiErrorMessage
    {
        public UiErrorMessage(
            string message,
            ErrorDisplayMode displayMode,
            string sourceFeature,
            float durationSeconds,
            bool canDismiss
        )
        {
            Message = string.IsNullOrWhiteSpace(message) ? "Unknown error." : message.Trim();
            DisplayMode = displayMode;
            SourceFeature = string.IsNullOrWhiteSpace(sourceFeature)
                ? "Unknown"
                : sourceFeature.Trim();
            DurationSeconds = durationSeconds;
            CanDismiss = canDismiss;
        }

        public string Message { get; }
        public ErrorDisplayMode DisplayMode { get; }
        public string SourceFeature { get; }
        public float DurationSeconds { get; }
        public bool CanDismiss { get; }

        public static UiErrorMessage Banner(
            string message,
            string sourceFeature,
            float durationSeconds = 3f
        )
        {
            return new UiErrorMessage(
                message,
                ErrorDisplayMode.Banner,
                sourceFeature,
                durationSeconds,
                true
            );
        }

        public static UiErrorMessage Modal(
            string message,
            string sourceFeature,
            bool canDismiss = true
        )
        {
            return new UiErrorMessage(message, ErrorDisplayMode.Modal, sourceFeature, 0f, canDismiss);
        }
    }

    public readonly struct UiErrorRequestedEvent
    {
        public UiErrorRequestedEvent(UiErrorMessage error)
        {
            Error = error;
        }

        public UiErrorMessage Error { get; }
    }
}
