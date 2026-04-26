using Features.Unit.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class PlacementAreaViewDirectTests
    {
        [Test]
        public void UnitPreview_ShowsExpectedSpawnAnchorAndAttackRangeVisuals()
        {
            var go = new GameObject("PlacementAreaViewTest");

            try
            {
                var view = go.AddComponent<PlacementAreaView>();
                var area = new PlacementArea(width: 8f, depth: 5f, forwardOffset: 0f);
                area.SetCorePosition(Vector3.zero);
                view.Initialize(area, validMaterial: null);

                view.ShowUnitPreview(new Vector3(1f, 0f, 2f), anchorRadius: 3f, attackRange: 5f);

                var visualRoot = go.transform.Find("PlacementPreviewVisuals");
                Assert.IsTrue(view.HasUnitPreview);
                Assert.AreEqual(new Vector3(1f, 0f, 2f), view.PreviewWorldPosition);
                Assert.AreEqual(3f, view.PreviewAnchorRadius);
                Assert.AreEqual(5f, view.PreviewAttackRange);
                Assert.IsNotNull(visualRoot);
                Assert.IsTrue(visualRoot.gameObject.activeSelf);
                Assert.IsNotNull(visualRoot.Find("AnchorRadius"));
                Assert.IsNotNull(visualRoot.Find("AttackRange"));
                Assert.IsNotNull(visualRoot.Find("ExpectedSpawnPoint"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void UnitPreview_HideClearsStateAndVisuals()
        {
            var go = new GameObject("PlacementAreaViewTest");

            try
            {
                var view = go.AddComponent<PlacementAreaView>();
                var area = new PlacementArea(width: 8f, depth: 5f, forwardOffset: 0f);
                area.SetCorePosition(Vector3.zero);
                view.Initialize(area, validMaterial: null);
                view.ShowUnitPreview(new Vector3(1f, 0f, 2f), anchorRadius: 3f, attackRange: 5f);

                view.HideUnitPreview();

                var visualRoot = go.transform.Find("PlacementPreviewVisuals");
                Assert.IsFalse(view.HasUnitPreview);
                Assert.AreEqual(Vector3.zero, view.PreviewWorldPosition);
                Assert.AreEqual(0f, view.PreviewAnchorRadius);
                Assert.AreEqual(0f, view.PreviewAttackRange);
                Assert.IsNotNull(visualRoot);
                Assert.IsFalse(visualRoot.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
