namespace Shared.Sound
{
    public readonly struct SoundRequestEvent
    {
        public SoundRequest Request { get; }

        public SoundRequestEvent(SoundRequest request)
        {
            Request = request;
        }
    }
}
