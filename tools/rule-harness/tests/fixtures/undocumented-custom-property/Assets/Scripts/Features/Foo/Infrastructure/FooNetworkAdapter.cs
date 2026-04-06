using ExitGames.Client.Photon;

namespace Features.Foo.Infrastructure
{
    public sealed class FooNetworkAdapter
    {
        public void Sync()
        {
            var props = new Hashtable
            {
                { "newKey", 1 }
            };
            SomeApi.SetCustomProperties(props);
        }
    }
}
