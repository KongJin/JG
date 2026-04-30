using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal static class GarageSetBUitkElements
    {
        public static T Required<T>(VisualElement root, string name) where T : VisualElement
        {
            var element = root.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException($"Garage SetB UITK element not found: {name}");

            return element;
        }

        public static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (enabled)
                element.AddToClassList(className);
            else
                element.RemoveFromClassList(className);
        }

        public static Image CreatePreviewImage(string name = "RuntimeUnitPreviewImage")
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
    }
}
