using UnityEngine;

namespace Features.Garage.Runtime
{
    internal static class GaragePreviewAssembler
    {
        internal static void Attach(GameObject child, Transform parent, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (child == null || parent == null)
                return;

            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localEulerAngles = localEulerAngles;
        }
    }
}
