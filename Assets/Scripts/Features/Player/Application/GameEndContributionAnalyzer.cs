using Features.Combat.Application.Events;
using Features.Enemy.Application.Events;
using Features.Player.Application.Events;
using Shared.Kernel;
using System;
using System.Collections.Generic;

namespace Features.Player.Application
{
    public sealed class GameEndContributionAnalyzer
    {
        private readonly DomainEntityId _coreId;
        private readonly float _coreMaxHealth;
        private readonly Dictionary<DomainEntityId, DomainEntityId> _unitOwners = new();
        private readonly Dictionary<DomainEntityId, string> _unitLoadoutKeys = new();
        private readonly HashSet<DomainEntityId> _enemyIds = new();
        private readonly Dictionary<DomainEntityId, ContributionBucket> _ownerBuckets = new();
        private readonly ContributionBucket _teamBucket = new();
        private float _coreRemainingHealth;

        public GameEndContributionAnalyzer(DomainEntityId coreId = default, float coreMaxHealth = 0f)
        {
            _coreId = coreId;
            _coreMaxHealth = Math.Max(0f, coreMaxHealth);
            _coreRemainingHealth = _coreMaxHealth;
        }

        public float CoreRemainingHealth => _coreRemainingHealth;
        public float CoreMaxHealth => _coreMaxHealth;

        public void RecordUnitDeployed(
            DomainEntityId playerId,
            DomainEntityId battleEntityId,
            string loadoutKey)
        {
            if (!IsEmpty(battleEntityId) && !IsEmpty(playerId))
                _unitOwners[battleEntityId] = playerId;

            if (!IsEmpty(battleEntityId) && !string.IsNullOrWhiteSpace(loadoutKey))
                _unitLoadoutKeys[battleEntityId] = loadoutKey.Trim();

            _teamBucket.Summons++;

            var ownerBucket = GetOrCreateOwnerBucket(playerId);
            if (ownerBucket == null)
                return;

            ownerBucket.OwnerId = playerId;
            ownerBucket.Summons++;
            if (!IsEmpty(battleEntityId))
            {
                ownerBucket.RepresentativeUnitId = battleEntityId;
                ownerBucket.RepresentativeLoadoutKey = ResolveLoadoutKey(battleEntityId);
            }
        }

        public void RecordEnemySpawned(EnemySpawnedEvent e)
        {
            if (!IsEmpty(e.EnemyId))
                _enemyIds.Add(e.EnemyId);
        }

        public void RecordDamageApplied(DamageAppliedEvent e)
        {
            if (!IsEmpty(_coreId) && e.TargetId == _coreId)
            {
                _coreRemainingHealth = Math.Max(0f, e.RemainingHealth);
                return;
            }

            if (!IsEmpty(e.TargetId) && _unitOwners.TryGetValue(e.TargetId, out var damagedOwnerId))
            {
                var damagedOwner = GetOrCreateOwnerBucket(damagedOwnerId);
                if (damagedOwner != null)
                {
                    damagedOwner.DamageTaken += Math.Max(0f, e.Damage);
                    damagedOwner.RepresentativeUnitId = e.TargetId;
                    damagedOwner.RepresentativeLoadoutKey = ResolveLoadoutKey(e.TargetId);
                }

                _teamBucket.DamageTaken += Math.Max(0f, e.Damage);
            }

            if (IsEmpty(e.TargetId) || !_enemyIds.Contains(e.TargetId))
                return;

            var damage = Math.Max(0f, e.Damage);
            _teamBucket.DamageDealt += damage;
            if (e.IsDead)
                _teamBucket.Kills++;

            if (IsEmpty(e.AttackerId) || !_unitOwners.TryGetValue(e.AttackerId, out var attackerOwnerId))
                return;

            var attackerOwner = GetOrCreateOwnerBucket(attackerOwnerId);
            if (attackerOwner == null)
                return;

            attackerOwner.DamageDealt += damage;
            attackerOwner.RepresentativeUnitId = e.AttackerId;
            attackerOwner.RepresentativeLoadoutKey = ResolveLoadoutKey(e.AttackerId);
            if (e.IsDead)
                attackerOwner.Kills++;
        }

        public ResultContributionCard[] BuildContributionCards(int fallbackSummonCount, int fallbackKillCount)
        {
            var cards = new List<ResultContributionCard>(3);
            AddCoreCard(cards);

            var candidates = new List<ResultContributionCard>(3);
            AddPressureCard(candidates, Math.Max(0, fallbackKillCount));
            AddHoldPositionCard(candidates);
            AddDeployCard(candidates, Math.Max(0, fallbackSummonCount));
            candidates.Sort((left, right) => right.PrimaryValue.CompareTo(left.PrimaryValue));

            for (var i = 0; i < candidates.Count && cards.Count < 3; i++)
                cards.Add(candidates[i]);

            if (cards.Count == 0)
            {
                cards.Add(new ResultContributionCard(
                    ResultContributionKind.DeployUnits,
                    "작전 참여",
                    "마지막까지 작전에 남아 결과를 만들었습니다.",
                    1f));
            }

            return cards.ToArray();
        }

        private void AddCoreCard(List<ResultContributionCard> cards)
        {
            if (_coreMaxHealth <= 0f)
                return;

            var percent = Clamp01(_coreRemainingHealth / _coreMaxHealth);
            if (percent <= 0f)
                return;

            cards.Add(new ResultContributionCard(
                ResultContributionKind.KeepCoreAlive,
                "거점 보존",
                $"거점 내구도 {percent * 100f:F0}%로 마지막 공세를 넘겼습니다.",
                percent));
        }

        private void AddPressureCard(List<ResultContributionCard> candidates, int fallbackKillCount)
        {
            var bucket = FindBestOwnerBucket(value => value.Kills > 0 ? value.Kills : value.DamageDealt);
            var teamKills = _teamBucket.Kills > 0 ? _teamBucket.Kills : fallbackKillCount;
            var value = bucket != null
                ? Math.Max((float)bucket.Kills, bucket.DamageDealt)
                : Math.Max((float)teamKills, _teamBucket.DamageDealt);
            if (value <= 0f)
                return;

            var kills = bucket != null ? bucket.Kills : teamKills;
            var damage = bucket != null ? bucket.DamageDealt : _teamBucket.DamageDealt;
            var body = kills > 0
                ? $"침공 기체 {kills}기를 정리했습니다."
                : $"누적 피해 {damage:F0}을 넣어 압박을 줄였습니다.";

            candidates.Add(new ResultContributionCard(
                ResultContributionKind.ClearPressure,
                "압박 정리",
                body,
                value,
                bucket != null ? bucket.OwnerId : default,
                bucket != null ? bucket.RepresentativeUnitId : default,
                bucket == null,
                bucket != null ? bucket.RepresentativeLoadoutKey : null));
        }

        private void AddHoldPositionCard(List<ResultContributionCard> candidates)
        {
            var bucket = FindBestOwnerBucket(value => value.DamageTaken);
            var value = bucket != null ? bucket.DamageTaken : _teamBucket.DamageTaken;
            if (value <= 0f)
                return;

            candidates.Add(new ResultContributionCard(
                ResultContributionKind.HoldPosition,
                "자리 지킴",
                $"아군 기체가 피해 {value:F0}을 받아 거점으로 향한 압박을 붙잡았습니다.",
                value,
                bucket != null ? bucket.OwnerId : default,
                bucket != null ? bucket.RepresentativeUnitId : default,
                bucket == null,
                bucket != null ? bucket.RepresentativeLoadoutKey : null));
        }

        private void AddDeployCard(List<ResultContributionCard> candidates, int fallbackSummonCount)
        {
            var bucket = FindBestOwnerBucket(value => value.Summons);
            var teamSummons = _teamBucket.Summons > 0 ? _teamBucket.Summons : fallbackSummonCount;
            var value = bucket != null ? bucket.Summons : teamSummons;
            if (value <= 0f)
                return;

            candidates.Add(new ResultContributionCard(
                ResultContributionKind.DeployUnits,
                "기체 전개",
                $"전장에 기체 {value:F0}기를 투입했습니다.",
                value,
                bucket != null ? bucket.OwnerId : default,
                bucket != null ? bucket.RepresentativeUnitId : default,
                bucket == null,
                bucket != null ? bucket.RepresentativeLoadoutKey : null));
        }

        private ContributionBucket FindBestOwnerBucket(Func<ContributionBucket, float> selector)
        {
            ContributionBucket best = null;
            var bestValue = 0f;
            foreach (var bucket in _ownerBuckets.Values)
            {
                var value = selector(bucket);
                if (value <= bestValue)
                    continue;

                best = bucket;
                bestValue = value;
            }

            return best;
        }

        private ContributionBucket GetOrCreateOwnerBucket(DomainEntityId ownerId)
        {
            if (IsEmpty(ownerId))
                return null;

            if (_ownerBuckets.TryGetValue(ownerId, out var bucket))
                return bucket;

            bucket = new ContributionBucket { OwnerId = ownerId };
            _ownerBuckets.Add(ownerId, bucket);
            return bucket;
        }

        private static bool IsEmpty(DomainEntityId id)
        {
            return string.IsNullOrWhiteSpace(id.Value);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }

        private string ResolveLoadoutKey(DomainEntityId battleEntityId)
        {
            return _unitLoadoutKeys.TryGetValue(battleEntityId, out var loadoutKey)
                ? loadoutKey
                : string.Empty;
        }

        private sealed class ContributionBucket
        {
            public DomainEntityId OwnerId;
            public DomainEntityId RepresentativeUnitId;
            public string RepresentativeLoadoutKey;
            public int Summons;
            public int Kills;
            public float DamageDealt;
            public float DamageTaken;
        }
    }
}
