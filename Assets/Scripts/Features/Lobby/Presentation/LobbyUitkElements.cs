using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal static class LobbyUitkElements
    {
        public static void SetText(VisualElement root, string labelName, string text)
        {
            var label = root?.Q<Label>(labelName);
            if (label != null)
                label.text = text ?? string.Empty;
        }

        public static void SetPage(VisualElement page, bool visible)
        {
            if (page != null)
                page.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetSelected(VisualElement element, bool selected)
        {
            if (element == null)
                return;

            if (selected)
                element.AddToClassList("shared-nav-item--selected");
            else
                element.RemoveFromClassList("shared-nav-item--selected");
        }

        public static Label Label(string text, string className)
        {
            var label = new Label(text);
            AddClasses(label, className);
            label.style.color = new Color(0.86f, 0.91f, 0.96f, 1f);
            return label;
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
