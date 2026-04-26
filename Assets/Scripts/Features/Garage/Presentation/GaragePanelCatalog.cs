using System.Collections.Generic;
using UnityEngine;

namespace Features.Garage.Presentation
{
    public sealed class GaragePanelCatalog
    {
        public sealed class FrameOption
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public float BaseHp { get; set; }
            public float BaseAttackSpeed { get; set; }
            public GameObject PreviewPrefab { get; set; }
        }

        public sealed class FirepowerOption
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public float AttackDamage { get; set; }
            public float AttackSpeed { get; set; }
            public float Range { get; set; }
            public GameObject PreviewPrefab { get; set; }
        }

        public sealed class MobilityOption
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public float HpBonus { get; set; }
            public float MoveRange { get; set; }
            public float AnchorRange { get; set; }
            public GameObject PreviewPrefab { get; set; }
        }

        private readonly Dictionary<string, FrameOption> _framesById;
        private readonly Dictionary<string, FirepowerOption> _firepowerById;
        private readonly Dictionary<string, MobilityOption> _mobilityById;

        public GaragePanelCatalog(
            IReadOnlyList<FrameOption> frames,
            IReadOnlyList<FirepowerOption> firepower,
            IReadOnlyList<MobilityOption> mobility)
        {
            Frames = frames ?? new List<FrameOption>();
            Firepower = firepower ?? new List<FirepowerOption>();
            Mobility = mobility ?? new List<MobilityOption>();

            _framesById = new Dictionary<string, FrameOption>();
            _firepowerById = new Dictionary<string, FirepowerOption>();
            _mobilityById = new Dictionary<string, MobilityOption>();

            for (int i = 0; i < Frames.Count; i++)
                _framesById[Frames[i].Id] = Frames[i];
            for (int i = 0; i < Firepower.Count; i++)
                _firepowerById[Firepower[i].Id] = Firepower[i];
            for (int i = 0; i < Mobility.Count; i++)
                _mobilityById[Mobility[i].Id] = Mobility[i];
        }

        public IReadOnlyList<FrameOption> Frames { get; }
        public IReadOnlyList<FirepowerOption> Firepower { get; }
        public IReadOnlyList<MobilityOption> Mobility { get; }

        public FrameOption FindFrame(string id) =>
            !string.IsNullOrWhiteSpace(id) && _framesById.TryGetValue(id, out var value) ? value : null;

        public FirepowerOption FindFirepower(string id) =>
            !string.IsNullOrWhiteSpace(id) && _firepowerById.TryGetValue(id, out var value) ? value : null;

        public MobilityOption FindMobility(string id) =>
            !string.IsNullOrWhiteSpace(id) && _mobilityById.TryGetValue(id, out var value) ? value : null;
    }
}
