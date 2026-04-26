using Shared.Logging;
using UnityEngine;

namespace Features.Garage.Presentation
{
    internal static class GaragePageChromeBindingResolver
    {
        public static GaragePageChromeController CreateController(
            MonoBehaviour owner,
            ref GaragePageChromeBindings bindings)
        {
            var resolved = Resolve(owner, ref bindings);
            return resolved != null ? resolved.CreateController() : null;
        }

        public static GaragePageChromeBindings Resolve(
            MonoBehaviour owner,
            ref GaragePageChromeBindings bindings)
        {
            if (bindings != null)
                return bindings;

            bindings = owner.GetComponent<GaragePageChromeBindings>();
            if (bindings == null)
            {
                Log.Warn(
                    "GaragePage",
                    "Chrome bindings are missing. Mobile chrome controls will be skipped.",
                    owner);
            }

            return bindings;
        }
    }
}
