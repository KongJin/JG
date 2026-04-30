using UnityEngine;
using Shared.Runtime;
using Shared.Runtime.Pooling;

namespace Features.Garage.Runtime
{
    internal static class GaragePreviewAssembler
    {
        private const string PreviewKeyLightName = "GaragePreviewKeyLight";

        internal static void Attach(GameObject child, Transform parent, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (child == null || parent == null)
                return;

            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localEulerAngles = localEulerAngles;
        }

        internal static void AttachToPreviewCamera(GameObject root, Camera previewCamera, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (root == null || previewCamera == null)
                return;

            Attach(root, previewCamera.transform, localPosition, localEulerAngles);
        }

        internal static Light EnsurePreviewLighting(Camera previewCamera, Light keyLight)
        {
            if (previewCamera == null)
                return keyLight;

            if (keyLight == null)
            {
                var lightObject = new GameObject(PreviewKeyLightName);
                keyLight = ComponentAccess.Ensure<Light>(lightObject);
            }

            Attach(keyLight.gameObject, previewCamera.transform, Vector3.zero, new Vector3(38f, -32f, 0f));
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.92f, 0.97f, 1f, 1f);
            keyLight.intensity = 2.4f;
            return keyLight;
        }

        internal static void SetYaw(GameObject root, float yawDegrees)
        {
            if (root == null)
                return;

            root.transform.localEulerAngles = new Vector3(0f, yawDegrees, 0f);
        }
    }
}
