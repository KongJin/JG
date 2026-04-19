using Features.Player.Application.Ports;
using Features.Player.Infrastructure;
using Features.Status;
using Shared.EventBus;

namespace Features.Player
{
    internal sealed class GameScenePlayerConnector
    {
        public bool Connect(
            PlayerSetup setup,
            EventBus eventBus,
            StatusSetup statusSetup,
            PlayerSceneRegistry playerSceneRegistry,
            IPlayerLookupPort playerLookup)
        {
            if (!setup.IsInitialized)
            {
                var specProvider = new DefaultPlayerSpecProvider();
                if (setup.NetworkAdapter.IsMine)
                {
                    setup.InitializeLocal(
                        eventBus,
                        specProvider,
                        statusSetup.SpeedModifier,
                        playerSceneRegistry,
                        playerLookup);
                }
                else
                {
                    setup.InitializeRemote(eventBus, specProvider, playerLookup);
                }
            }

            if (!playerSceneRegistry.TryRegister(setup))
                return false;

            if (!setup.NetworkAdapter.IsMine)
            {
                statusSetup.RegisterRemoteCallbackPort(setup.StatusNetworkAdapter);
                setup.NetworkAdapter.HydrateFromProperties();
            }

            return true;
        }
    }
}
