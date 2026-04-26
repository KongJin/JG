using Shared.Runtime;
using UnityEngine;

namespace Features.Enemy
{
    public sealed class EnemySceneRegistry : MonoBehaviour
    {
        private readonly PendingArrivalBuffer<EnemySetup> _pendingArrivals = new();

        public event System.Action<EnemySetup> EnemyArrived;

        public void NotifyArrived(EnemySetup setup)
        {
            if (setup == null)
            {
                Debug.LogError("[EnemySceneRegistry] Arrived EnemySetup is missing.", this);
                return;
            }

            _pendingArrivals.Notify(setup, EnemyArrived);
        }

        public void DrainPendingArrivals(System.Action<EnemySetup> handler)
        {
            if (handler == null)
                return;

            _pendingArrivals.Drain(handler);
        }
    }
}
