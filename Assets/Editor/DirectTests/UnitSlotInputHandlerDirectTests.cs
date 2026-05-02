using Features.Unit.Domain;
using Features.Unit.Presentation;
using NUnit.Framework;
using Shared.Kernel;
using Shared.Math;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnitSpec = Features.Unit.Domain.Unit;

namespace Tests.Editor
{
    public sealed class UnitSlotInputHandlerDirectTests
    {
        [Test]
        public void DragDropInsidePlacementArea_RequestsSummonAndHidesPreview()
        {
            var eventSystemGo = new GameObject("EventSystem");
            var cameraGo = new GameObject("WorldCamera", typeof(Camera));
            var slotGo = new GameObject("UnitSlot", typeof(RectTransform), typeof(Image), typeof(UnitSlotInputHandler));
            var viewGo = new GameObject("PlacementAreaView", typeof(PlacementAreaView));

            try
            {
                eventSystemGo.AddComponent<EventSystem>();

                var camera = cameraGo.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 10f, 0f);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var area = new PlacementArea(width: 8f, depth: 8f, forwardOffset: 0f);
                area.SetCorePosition(Vector3.zero);

                var placementView = viewGo.GetComponent<PlacementAreaView>();
                placementView.Initialize(area, validMaterial: null);

                var summonCount = 0;
                UnitSpec requestedUnit = null;
                Float3 requestedPosition = default;
                var unit = CreateUnit("unit-1");

                var handler = slotGo.GetComponent<UnitSlotInputHandler>();
                handler.Initialize(
                    unit,
                    (spec, position) =>
                    {
                        summonCount++;
                        requestedUnit = spec;
                        requestedPosition = position;
                    },
                    camera,
                    area,
                    errorView: null,
                    placementView);

                var eventData = new PointerEventData(EventSystem.current)
                {
                    position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                };

                handler.OnBeginDrag(eventData);
                handler.OnDrag(eventData);
                handler.OnEndDrag(eventData);

                Assert.AreEqual(1, summonCount);
                Assert.AreSame(unit, requestedUnit);
                Assert.IsTrue(area.Contains(new Vector3(requestedPosition.X, requestedPosition.Y, requestedPosition.Z)));
                Assert.IsFalse(placementView.HasUnitPreview);
            }
            finally
            {
                Object.DestroyImmediate(viewGo);
                Object.DestroyImmediate(slotGo);
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(eventSystemGo);
            }
        }

        private static UnitSpec CreateUnit(string id)
        {
            return new UnitSpec(
                new DomainEntityId(id),
                "frame",
                "Unit",
                "fire",
                "mobility",
                "",
                0,
                100f,
                2f,
                10f,
                1f,
                5f,
                4f,
                3f,
                3f,
                1,
                1,
                1,
                3);
        }
    }
}
