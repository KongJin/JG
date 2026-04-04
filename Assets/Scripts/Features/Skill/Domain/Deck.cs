using System.Collections.Generic;
using Shared.Kernel;

namespace Features.Skill.Domain
{
    /// <summary>
    /// 뽑기/버리기 덱. 시전한 스킬은 버린 더미로, 뽑을 더미가 비면 셔플.
    /// </summary>
    public sealed class Deck
    {
        private readonly List<DomainEntityId> _drawPile;
        private readonly List<DomainEntityId> _discardPile = new();
        private readonly System.Random _rng;

        public Deck(IReadOnlyList<DomainEntityId> allSkillIds, System.Random rng = null)
        {
            _rng = rng ?? new System.Random();
            _drawPile = new List<DomainEntityId>(allSkillIds);
            Shuffle(_drawPile);
        }

        public int DrawPileCount => _drawPile.Count;
        public int DiscardPileCount => _discardPile.Count;
        public IReadOnlyList<DomainEntityId> DrawPileIds => _drawPile.AsReadOnly();
        public IReadOnlyList<DomainEntityId> DiscardPileIds => _discardPile.AsReadOnly();

        /// <summary>
        /// 다음 <see cref="Draw"/>로 뽑힐 스킬 id. 뽑기 더미가 비어 있으면 null(셔플 직전에는 미리 알 수 없음).
        /// </summary>
        public string PeekNextDrawSkillId()
        {
            if (_drawPile.Count == 0)
                return null;
            return _drawPile[^1].Value;
        }

        public DomainEntityId Draw()
        {
            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0)
                    return default;

                _drawPile.AddRange(_discardPile);
                _discardPile.Clear();
                Shuffle(_drawPile);
            }

            var last = _drawPile.Count - 1;
            var id = _drawPile[last];
            _drawPile.RemoveAt(last);
            return id;
        }

        public void Discard(DomainEntityId skillId)
        {
            _discardPile.Add(skillId);
        }

        public void AddToDiscardPile(DomainEntityId skillId)
        {
            _discardPile.Add(skillId);
        }

        private void Shuffle(List<DomainEntityId> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
