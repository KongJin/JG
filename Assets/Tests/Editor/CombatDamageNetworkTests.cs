using System.Collections.Generic;
using Features.Combat.Application;
using Features.Combat.Application.Events;
using Features.Combat.Application.Ports;
using Features.Combat.Domain;
using Features.Player.Application;
using Features.Player.Application.Events;
using Features.Player.Domain;
using Features.Projectile.Application.Events;
using NUnit.Framework;
using Shared.EventBus;
using Shared.Kernel;

public sealed class CombatDamageNetworkTests
{
    [Test]
    public void CombatNetworkEventHandler_IgnoresForeignProjectileHit_WhenAuthorityIsConfigured()
    {
        var targetPort = new FakeCombatTargetPort();
        var publisher = new RecordingPublisher();
        var network = new RecordingCombatNetworkPort();
        var authorityId = new DomainEntityId("local-player");
        var foreignOwnerId = new DomainEntityId("remote-player");
        var targetId = new DomainEntityId("target");

        targetPort.AddTarget(targetId, health: 100f, defense: 5f);

        var useCase = new ApplyDamageUseCase(targetPort, publisher, network);
        var handler = new CombatNetworkEventHandler(useCase, authorityId);
        var result = handler.HandleProjectileHit(
            new ProjectileHitEvent(
                DomainEntityId.New(),
                foreignOwnerId,
                targetId,
                20f,
                DamageType.Physical
            )
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(targetPort.GetRemainingHealth(targetId), Is.EqualTo(100f));
        Assert.That(network.DamagesSent.Count, Is.EqualTo(0));
        Assert.That(publisher.PublishedEvents.Count, Is.EqualTo(0));
    }

    [Test]
    public void ApplyDamageUseCase_ExecuteReplicated_UsesReplicatedDamageWithoutDefenseRecalculation()
    {
        var targetPort = new FakeCombatTargetPort();
        var publisher = new RecordingPublisher();
        var network = new RecordingCombatNetworkPort();
        var targetId = new DomainEntityId("target");
        var attackerId = new DomainEntityId("attacker");

        targetPort.AddTarget(targetId, health: 100f, defense: 5f);

        var useCase = new ApplyDamageUseCase(targetPort, publisher, network);
        var result = useCase.ExecuteReplicated(targetId, 17f, DamageType.Physical, attackerId);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(targetPort.GetRemainingHealth(targetId), Is.EqualTo(83f));
        Assert.That(network.DamagesSent.Count, Is.EqualTo(0));
        Assert.That(publisher.TryGetLast<DamageAppliedEvent>(out var damageEvent), Is.True);
        Assert.That(damageEvent.Damage, Is.EqualTo(17f));
        Assert.That(damageEvent.RemainingHealth, Is.EqualTo(83f));
        Assert.That(damageEvent.AttackerId, Is.EqualTo(attackerId));
    }

    [Test]
    public void ApplyDamageUseCase_Execute_DoesNotSendDeathRpc()
    {
        var targetPort = new FakeCombatTargetPort();
        var publisher = new RecordingPublisher();
        var network = new RecordingCombatNetworkPort();
        var targetId = new DomainEntityId("target");
        var attackerId = new DomainEntityId("attacker");

        targetPort.AddTarget(targetId, health: 10f, defense: 0f);

        var useCase = new ApplyDamageUseCase(targetPort, publisher, network);
        useCase.Execute(targetId, 50f, DamageType.Physical, attackerId);

        Assert.That(targetPort.GetRemainingHealth(targetId), Is.EqualTo(0f));
        Assert.That(network.DeathsSent.Count, Is.EqualTo(0),
            "Death should be conveyed via damage replication, not a separate RPC");
        Assert.That(network.DamagesSent.Count, Is.EqualTo(1));
    }

    [Test]
    public void CombatReplicationEventHandler_ForwardsToExecuteReplicated()
    {
        var targetPort = new FakeCombatTargetPort();
        var publisher = new RecordingPublisher();
        var network = new RecordingCombatNetworkPort();
        var targetId = new DomainEntityId("target");
        var attackerId = new DomainEntityId("attacker");

        targetPort.AddTarget(targetId, health: 100f, defense: 5f);

        var useCase = new ApplyDamageUseCase(targetPort, publisher, network);
        var handler = new CombatReplicationEventHandler(useCase);
        var result = handler.HandleDamageReplicated(
            new DamageReplicatedEvent(targetId, 25f, DamageType.Physical, attackerId)
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(targetPort.GetRemainingHealth(targetId), Is.EqualTo(75f));
        Assert.That(network.DamagesSent.Count, Is.EqualTo(0),
            "Replicated damage must not re-send over network");
    }

    [Test]
    public void PlayerDamageEventHandler_PublishesPlayerDiedEvent_OnlyOnce()
    {
        var playerId = new DomainEntityId("player");
        var attackerId = new DomainEntityId("attacker");
        var spec = new PlayerSpec(0f, 1f, 0f, 0f, 50f, 0f, 0f);
        var player = new Player(playerId, spec);
        var eventBus = new EventBus();

        var diedEvents = new List<PlayerDiedEvent>();
        eventBus.Subscribe(this, new System.Action<PlayerDiedEvent>(e => diedEvents.Add(e)));

        new PlayerDamageEventHandler(player, eventBus, eventBus);

        // First lethal damage
        player.TakeDamage(50f);
        eventBus.Publish(new DamageAppliedEvent(playerId, 50f, DamageType.Physical, 0f, true, attackerId));

        // Second damage on already-dead target
        eventBus.Publish(new DamageAppliedEvent(playerId, 10f, DamageType.Physical, 0f, true, attackerId));

        Assert.That(diedEvents.Count, Is.EqualTo(1), "PlayerDiedEvent must fire exactly once");
    }

    [Test]
    public void PlayerDamageEventHandler_ResetsDeathFlag_OnRespawn()
    {
        var playerId = new DomainEntityId("player");
        var attackerId = new DomainEntityId("attacker");
        var spec = new PlayerSpec(0f, 1f, 0f, 0f, 50f, 0f, 0f);
        var player = new Player(playerId, spec);
        var eventBus = new EventBus();

        var diedEvents = new List<PlayerDiedEvent>();
        eventBus.Subscribe(this, new System.Action<PlayerDiedEvent>(e => diedEvents.Add(e)));

        new PlayerDamageEventHandler(player, eventBus, eventBus);

        // Kill
        player.TakeDamage(50f);
        eventBus.Publish(new DamageAppliedEvent(playerId, 50f, DamageType.Physical, 0f, true, attackerId));
        Assert.That(diedEvents.Count, Is.EqualTo(1));

        // Respawn
        player.Respawn();
        eventBus.Publish(new PlayerRespawnedEvent(playerId, player.CurrentHp, player.MaxHp));

        // Kill again
        player.TakeDamage(50f);
        eventBus.Publish(new DamageAppliedEvent(playerId, 50f, DamageType.Physical, 0f, true, attackerId));
        Assert.That(diedEvents.Count, Is.EqualTo(2), "Death flag should reset after respawn");
    }

    private sealed class FakeCombatTargetPort : ICombatTargetPort
    {
        private readonly Dictionary<DomainEntityId, FakeTargetState> _targets = new Dictionary<DomainEntityId, FakeTargetState>();

        public void AddTarget(DomainEntityId id, float health, float defense)
        {
            _targets[id] = new FakeTargetState(health, defense);
        }

        public bool Exists(DomainEntityId targetId)
        {
            return _targets.ContainsKey(targetId);
        }

        public float GetDefense(DomainEntityId targetId)
        {
            return _targets[targetId].Defense;
        }

        public CombatTargetDamageResult ApplyDamage(DomainEntityId targetId, float damage)
        {
            var target = _targets[targetId];
            target.Health -= damage;
            if (target.Health < 0f)
                target.Health = 0f;

            return new CombatTargetDamageResult(target.Health, target.Health <= 0f);
        }

        public float GetRemainingHealth(DomainEntityId targetId)
        {
            return _targets[targetId].Health;
        }

        private sealed class FakeTargetState
        {
            public FakeTargetState(float health, float defense)
            {
                Health = health;
                Defense = defense;
            }

            public float Health { get; set; }
            public float Defense { get; }
        }
    }

    private sealed class RecordingCombatNetworkPort : ICombatNetworkCommandPort
    {
        public List<(DomainEntityId targetId, float damage, DamageType damageType, DomainEntityId attackerId)> DamagesSent { get; } =
            new List<(DomainEntityId, float, DamageType, DomainEntityId)>();

        public List<(DomainEntityId targetId, DomainEntityId killerId)> DeathsSent { get; } =
            new List<(DomainEntityId, DomainEntityId)>();

        public void SendDamage(
            DomainEntityId targetId,
            float damage,
            DamageType damageType,
            DomainEntityId attackerId
        )
        {
            DamagesSent.Add((targetId, damage, damageType, attackerId));
        }

        public void SendDeath(DomainEntityId targetId, DomainEntityId killerId)
        {
            DeathsSent.Add((targetId, killerId));
        }

        public void SendRespawn(DomainEntityId targetId) { }
    }

    private sealed class RecordingPublisher : IEventPublisher
    {
        public List<object> PublishedEvents { get; } = new List<object>();

        public void Publish<T>(T e)
        {
            PublishedEvents.Add(e);
        }

        public bool TryGetLast<T>(out T result)
        {
            for (var i = PublishedEvents.Count - 1; i >= 0; i--)
            {
                if (PublishedEvents[i] is T value)
                {
                    result = value;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}
