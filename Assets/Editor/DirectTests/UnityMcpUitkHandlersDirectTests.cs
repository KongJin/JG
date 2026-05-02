using NUnit.Framework;
using ProjectSD.EditorTools.UnityMcp;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor
{
    public sealed class UnityMcpUitkHandlersDirectTests
    {
        [Test]
        public void FindElementForTest_ResolvesNamedAndPathElements()
        {
            var root = new VisualElement { name = "Root" };
            var panel = new VisualElement { name = "Panel" };
            var button = new Button { name = "SaveButton", text = "Save" };
            root.Add(panel);
            panel.Add(button);

            Assert.AreSame(button, UitkHandlers.FindElementForTest(root, "SaveButton", null));
            Assert.AreSame(button, UitkHandlers.FindElementForTest(root, null, "Panel/SaveButton"));
            Assert.IsNull(UitkHandlers.FindElementForTest(root, null, "Panel/Missing"));
        }

        [Test]
        public void BuildElementStateForTest_ReadsTextValueAndClasses()
        {
            var field = new TextField { name = "RoomNameInput", value = "Alpha" };
            field.AddToClassList("room-input");

            var state = UitkHandlers.BuildElementStateForTest(field);

            Assert.AreEqual("RoomNameInput", state.name);
            Assert.AreEqual("TextField", state.type);
            Assert.AreEqual("Alpha", state.value);
            CollectionAssert.Contains(state.classList, "room-input");
        }

        [Test]
        public void SetElementValueForTest_UpdatesUitkFields()
        {
            var integerField = new IntegerField { name = "CapacityInput", value = 2 };
            var toggle = new Toggle { name = "ReadyToggle", value = false };

            Assert.AreEqual("IntegerField.value", UitkHandlers.SetElementValueForTest(integerField, "4"));
            Assert.AreEqual(4, integerField.value);

            Assert.AreEqual("Toggle.value", UitkHandlers.SetElementValueForTest(toggle, "true"));
            Assert.IsTrue(toggle.value);
        }

        [Test]
        public void InvokeElementForTest_ClickSendsUitkClickEvent()
        {
            var clicked = false;
            var button = new Button { name = "ReadyButton", text = "Ready" };
            button.clicked += () => clicked = true;

            Assert.AreEqual("Button.clicked", UitkHandlers.InvokeElementForTest(button, "click"));
            Assert.IsTrue(clicked);
        }

        [Test]
        public void GameObjectInvokeMethodForTest_CallsMonoBehaviourMethodWithoutUiRoute()
        {
            var host = new GameObject("McpInvokeHost");
            try
            {
                var probe = host.AddComponent<UnityMcpGameObjectInvokeProbe>();

                var response = GameObjectInvokeHandlers.InvokeGameObjectMethodForTest(new GameObjectInvokeRequest
                {
                    path = "/McpInvokeHost",
                    method = nameof(UnityMcpGameObjectInvokeProbe.Record),
                    args = new[] { "ready", "3" }
                });

                Assert.IsTrue(response.success);
                Assert.AreEqual(nameof(UnityMcpGameObjectInvokeProbe.Record), response.method);
                Assert.AreEqual("ready:3", probe.Value);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }

    internal sealed class UnityMcpGameObjectInvokeProbe : MonoBehaviour
    {
        public string Value { get; private set; }

        public void Record(string label, int count)
        {
            Value = $"{label}:{count}";
        }
    }
}
