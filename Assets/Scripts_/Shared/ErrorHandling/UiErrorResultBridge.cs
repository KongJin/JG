using Shared.EventBus;
using Shared.Kernel;

namespace Shared.ErrorHandling
{
    public static class UiErrorResultBridge
    {
        public static bool PublishBannerIfFailure(
            IEventPublisher publisher,
            Result result,
            string sourceFeature,
            float durationSeconds = 3f
        )
        {
            return PublishIfFailure(
                publisher,
                result.IsFailure,
                result.Error,
                UiErrorMessage.Banner(result.Error, sourceFeature, durationSeconds)
            );
        }

        public static bool PublishBannerIfFailure<T>(
            IEventPublisher publisher,
            Result<T> result,
            string sourceFeature,
            float durationSeconds = 3f
        )
        {
            return PublishIfFailure(
                publisher,
                result.IsFailure,
                result.Error,
                UiErrorMessage.Banner(result.Error, sourceFeature, durationSeconds)
            );
        }

        public static bool PublishModalIfFailure(
            IEventPublisher publisher,
            Result result,
            string sourceFeature,
            bool canDismiss = true
        )
        {
            return PublishIfFailure(
                publisher,
                result.IsFailure,
                result.Error,
                UiErrorMessage.Modal(result.Error, sourceFeature, canDismiss)
            );
        }

        public static bool PublishModalIfFailure<T>(
            IEventPublisher publisher,
            Result<T> result,
            string sourceFeature,
            bool canDismiss = true
        )
        {
            return PublishIfFailure(
                publisher,
                result.IsFailure,
                result.Error,
                UiErrorMessage.Modal(result.Error, sourceFeature, canDismiss)
            );
        }

        private static bool PublishIfFailure(
            IEventPublisher publisher,
            bool isFailure,
            string error,
            UiErrorMessage message
        )
        {
            if (!isFailure)
                return false;

            if (publisher == null || string.IsNullOrWhiteSpace(error))
                return false;

            publisher.Publish(new UiErrorRequestedEvent(message));
            return true;
        }
    }
}
