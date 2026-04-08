using NUnit.Framework;
using Features.Unit.Domain;

namespace Tests.Unit.Domain
{
    /// <summary>
    /// UnitComposition 검증 로직 테스트.
    /// 조합 유효성 체크 규칙: 이동범위/사거리 관계에 따라 금지조합 판정.
    /// </summary>
    public class UnitCompositionTests
    {
        [Test]
        public void Validate_근접형_이동범위4m_사거리3m_유효()
        {
            bool result = UnitComposition.Validate(moveRange: 4f, range: 3f, out _);
            Assert.IsTrue(result);
        }

        [Test]
        public void Validate_원거리형_이동범위3m_사거리6m_유효()
        {
            bool result = UnitComposition.Validate(moveRange: 3f, range: 6f, out _);
            Assert.IsTrue(result);
        }

        [Test]
        public void Validate_하이브리드형_이동범위3m_사거리4m_유효()
        {
            bool result = UnitComposition.Validate(moveRange: 3f, range: 4f, out _);
            Assert.IsTrue(result);
        }

        [Test]
        public void Validate_금지조합_이동범위2m_사거리3m_실패()
        {
            bool result = UnitComposition.Validate(moveRange: 2f, range: 3f, out string error);
            Assert.IsFalse(result);
            Assert.IsNotNull(error);
            StringAssert.Contains("이동범위", error);
        }

        [Test]
        public void Validate_금지조합_연사3m_고정포대2m_실패()
        {
            // 연사(사거리 3m) + 고정포대(이동범위 2m) = 접근 불가
            bool result = UnitComposition.Validate(moveRange: 2f, range: 3f, out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void Validate_경계값_이동범위4m_사거리6m_유효()
        {
            // 둘 다 조건 만족
            bool result = UnitComposition.Validate(moveRange: 4f, range: 6f, out _);
            Assert.IsTrue(result);
        }

        [Test]
        public void Validate_경계값_이동범위2.9m_사거리3.9m_실패()
        {
            // 하이브리드 조건 미달 (3m 미만, 4m 미만)
            bool result = UnitComposition.Validate(moveRange: 2.9f, range: 3.9f, out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void Compose_스탯계산_올바른값()
        {
            var input = new UnitComposition.CompositionInput(
                baseHp: 300f,
                baseAttackSpeed: 1.0f,
                firepowerDamage: 30f,
                firepowerAttackSpeed: 1.0f,
                firepowerRange: 4f,
                mobilityHpBonus: 500f,
                mobilityMoveRange: 3f,
                mobilityAnchorRange: 2.5f,
                passiveTraitCostBonus: 5
            );

            var stats = UnitComposition.Compose("frame1", "fire1", "mob1", input);

            Assert.AreEqual(800f, stats.Hp);        // 300 + 500
            Assert.AreEqual(30f, stats.AttackDamage);
            Assert.AreEqual(1.0f, stats.AttackSpeed); // 1.0 * 1.0
            Assert.AreEqual(4f, stats.Range);
            Assert.AreEqual(3f, stats.MoveRange);
            Assert.AreEqual(2.5f, stats.AnchorRange);
            Assert.AreEqual(5, stats.PassiveTraitCostBonus);
        }

        [Test]
        public void Compose_역할분류_탱커()
        {
            var input = new UnitComposition.CompositionInput(
                baseHp: 1000f,
                baseAttackSpeed: 1.0f,
                firepowerDamage: 20f,
                firepowerAttackSpeed: 0.8f,
                firepowerRange: 3f,
                mobilityHpBonus: 500f,
                mobilityMoveRange: 3f,
                mobilityAnchorRange: 2f,
                passiveTraitCostBonus: 5
            );

            var stats = UnitComposition.Compose("frame1", "fire1", "mob1", input);
            Assert.AreEqual(UnitRole.Tanker, stats.Role);
        }
    }
}
