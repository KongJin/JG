#if UNITY_EDITOR
using System.Net;
using System.Threading.Tasks;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class DisabledUguiHandlers
    {
        static DisabledUguiHandlers()
        {
            "POST".Register("/ui/invoke", "Disabled UGUI route. Use /uitk/invoke.", HandleDisabledAsync);
            "POST".Register("/ui/get-state", "Disabled UGUI route. Use /uitk/get-state.", HandleDisabledAsync);
            "POST".Register("/ui/set-value", "Disabled UGUI route. Use /uitk/set-value.", HandleDisabledAsync);
            "POST".Register("/ui/list-handlers", "Disabled UGUI route. Use /uitk/state.", HandleDisabledAsync);
            "POST".Register("/ui/button/invoke", "Disabled UGUI route. Use /uitk/invoke.", HandleDisabledAsync);
            "POST".Register("/ui/create-button", "Disabled UGUI authoring route.", HandleDisabledAsync);
            "POST".Register("/ui/create-panel", "Disabled UGUI authoring route.", HandleDisabledAsync);
            "POST".Register("/ui/create-raw-image", "Disabled UGUI authoring route.", HandleDisabledAsync);
            "POST".Register("/ui/set-rect", "Disabled UGUI RectTransform route.", HandleDisabledAsync);
            "GET".Register("/ui/state", "Disabled UGUI Canvas snapshot route. Use /uitk/state.", HandleDisabledAsync);
            "POST".Register("/ui/wait-for-active", "Disabled UGUI route. Use /uitk/wait-for-element.", HandleDisabledAsync);
            "POST".Register("/ui/wait-for-inactive", "Disabled UGUI route. Use /uitk/get-state.", HandleDisabledAsync);
            "POST".Register("/ui/wait-for-text", "Disabled UGUI route. Use /uitk/wait-for-element.", HandleDisabledAsync);
            "POST".Register("/ui/wait-for-component", "Disabled UGUI route. Use /uitk/get-state or GameObject component endpoints.", HandleDisabledAsync);
            "POST".Register("/ui/compare-screenshots", "Disabled UGUI screenshot comparison route.", HandleDisabledAsync);
            "GET".Register("/explore/interactive", "Disabled UGUI Canvas explorer. Use /uitk/state.", HandleDisabledAsync);
            "GET".Register("/snapshot/ui", "Disabled UGUI Canvas snapshot. Use /uitk/state.", HandleDisabledAsync);
            "POST".Register("/snapshot/diff", "Disabled UGUI Canvas snapshot diff route. Use /uitk/state.", HandleDisabledAsync);
        }

        private static async Task HandleDisabledAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            await UnityMcpBridge.WriteJsonAsync(response, 410, new ErrorResponse
            {
                error = "UGUI route disabled",
                detail = request.HttpMethod.ToUpperInvariant() + " " + request.Url.AbsolutePath + " is disabled because this project uses UI Toolkit only.",
                hint = "Use /uitk/state, /uitk/get-state, /uitk/set-value, /uitk/invoke, or /uitk/wait-for-element."
            });
        }
    }
}
#endif
