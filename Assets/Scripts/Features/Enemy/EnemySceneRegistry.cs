using System.Collections.Generic;
using UnityEngine;

namespace Features.Enemy
{
    public sealed class EnemySceneRegistry : MonoBehaviour
    {
        private readonly Queue<EnemySetup> _pendingArrivals = new();

        public event System.Action<EnemySetup> EnemyArrived;

        public void NotifyArrived(EnemySetup setup)
        {
            if (setup == null)
            {
                Debug.LogError("[EnemySceneRegistry] Arrived EnemySetup is missing.", this);
                return;
            }

            _pendingArrivals.Enqueue(setup);
            EnemyArrived?.Invoke(setup);
        }

        public void DrainPendingArrivals(System.Action<EnemySetup> handler)
        {
            if (handler == null)
                return;

            while (_pendingArrivals.Count > 0)
                handler(_pendingArrivals.Dequeue());
        }
    }
}
