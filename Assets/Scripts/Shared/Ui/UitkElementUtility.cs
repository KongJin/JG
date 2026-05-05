using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shared.Ui
{
    public static class UitkElementUtility
    {
        public static T Required<T>(
            VisualElement root,
            string name,
            string ownerLabel = "UITK") where T : VisualElement
        {
            // csharp-guardrails: allow-null-defense
            if (root == null)
                throw new InvalidOperationException($"{ownerLabel} root is missing while querying element: {name}");

            var element = root.Q<T>(name);
            // csharp-guardrails: allow-null-defense
            if (element == null)
                throw new InvalidOperationException($"{ownerLabel} element not found: {name}");

            return element;
        }

        public static void SetText(VisualElement root, string labelName, string text)
        {
            var label = root?.Q<Label>(labelName);
            // csharp-guardrails: allow-null-defense
            if (label != null)
                label.text = text ?? string.Empty;
        }

        public static void SetDisplay(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (element == null || string.IsNullOrWhiteSpace(className))
                return;

            if (enabled)
                element.AddToClassList(className);
            else
                element.RemoveFromClassList(className);
        }

        public static Label CreateLabel(string text, string classNames = null)
        {
            var label = new Label(text ?? string.Empty);
            AddClasses(label, classNames);
            return label;
        }

        public static Image CreateAbsoluteImage(string name = "RuntimeUnitPreviewImage")
        {
            var image = new Image
            {
                name = name,
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };

            image.style.position = Position.Absolute;
            image.style.left = 0;
            image.style.right = 0;
            image.style.top = 0;
            image.style.bottom = 0;
            return image;
        }

        public static void AddClasses(VisualElement element, string classNames)
        {
            if (element == null || string.IsNullOrWhiteSpace(classNames))
                return;

            var parts = classNames.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
                element.AddToClassList(parts[i]);
        }
    }
}
