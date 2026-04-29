using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Shared.Ui
{
    public static class UitkIconRegistry
    {
        private const string IconClassPrefix = "uitk-icon--";

        private static readonly IReadOnlyDictionary<string, string> IconClasses =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["settings"] = "uitk-icon--settings",
                ["smart_toy"] = "uitk-icon--smart-toy",
                ["security"] = "uitk-icon--security",
                ["precision_manufacturing"] = "uitk-icon--precision-manufacturing",
                ["add"] = "uitk-icon--add",
                ["save"] = "uitk-icon--save",
                ["garage"] = "uitk-icon--garage",
                ["terminal"] = "uitk-icon--terminal",
                ["group"] = "uitk-icon--group",
                ["radar"] = "uitk-icon--radar",
                ["timer"] = "uitk-icon--timer",
                ["shield"] = "uitk-icon--shield",
                ["bolt"] = "uitk-icon--bolt",
                ["menu"] = "uitk-icon--menu",
                ["back"] = "uitk-icon--back",
                ["records"] = "uitk-icon--records",
                ["target"] = "uitk-icon--target",
                ["swords"] = "uitk-icon--swords",
                ["speed"] = "uitk-icon--speed",
                ["view_in_ar"] = "uitk-icon--cube",
                ["inventory_2"] = "uitk-icon--inventory-2",
                ["warning"] = "uitk-icon--warning",
                ["check_circle"] = "uitk-icon--check-circle",
                ["cloud"] = "uitk-icon--cloud",
                ["sync"] = "uitk-icon--sync",
                ["link"] = "uitk-icon--link",
                ["memory"] = "uitk-icon--memory",
                ["stars"] = "uitk-icon--star",
            };

        private static readonly HashSet<string> IconClassNames =
            new HashSet<string>(IconClasses.Values, StringComparer.Ordinal);

        public static IEnumerable<string> IconIds => IconClasses.Keys;

        public static string GetClassName(string iconId)
        {
            if (string.IsNullOrWhiteSpace(iconId))
                throw new ArgumentException("UITK icon id is required.", nameof(iconId));

            if (!IconClasses.TryGetValue(iconId, out var className))
                throw new InvalidOperationException($"UITK icon id is not registered: {iconId}");

            return className;
        }

        public static void Apply(VisualElement element, string iconId)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.AddToClassList("uitk-icon");
            RemoveIconClass(element);
            element.AddToClassList(GetClassName(iconId));
        }

        private static void RemoveIconClass(VisualElement element)
        {
            var classNames = new List<string>(element.GetClasses());
            foreach (var className in classNames)
            {
                if (className.StartsWith(IconClassPrefix, StringComparison.Ordinal)
                    && IconClassNames.Contains(className))
                {
                    element.RemoveFromClassList(className);
                }
            }
        }
    }
}
