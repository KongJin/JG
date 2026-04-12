#if UNITY_EDITOR
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.UnityMcp
{
    internal static class InputHandlers
    {
        static InputHandlers()
        {
            "POST".Register("/input/click", "Mouse click in game view", async (req, res) => await HandleInputClickAsync(req, res));
            "POST".Register("/input/move", "Mouse move in game view", async (req, res) => await HandleInputMoveAsync(req, res));
            "POST".Register("/input/drag", "Mouse drag in game view", async (req, res) => await HandleInputDragAsync(req, res));
            "POST".Register("/input/key", "Keyboard key press", async (req, res) => await HandleInputKeyAsync(req, res));
            "POST".Register("/input/text", "Text input", async (req, res) => await HandleInputTextAsync(req, res));
            "POST".Register("/input/scroll", "Mouse scroll", async (req, res) => await HandleInputScrollAsync(req, res));
            "POST".Register("/input/key-combo", "Key combo preset (copy/paste/etc)", async (req, res) => await HandleInputKeyComboAsync(req, res));
        }

        public static async Task HandleInputClickAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            InputClickRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<InputClickRequest>(body);
                }
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteInputClick(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid click request", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Click unavailable", detail = ex.Message });
            }
        }

        public static async Task HandleInputMoveAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            InputMoveRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<InputMoveRequest>(body);
                }
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteInputMove(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid move request", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Move unavailable", detail = ex.Message });
            }
        }

        public static async Task HandleInputDragAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            InputDragRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<InputDragRequest>(body);
                }
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteInputDrag(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid drag request", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Drag unavailable", detail = ex.Message });
            }
        }

        public static async Task HandleInputKeyAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            InputKeyRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<InputKeyRequest>(body);
                }
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteInputKey(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid key request", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Key input unavailable", detail = ex.Message });
            }
        }

        public static async Task HandleInputTextAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            InputTextRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<InputTextRequest>(body);
                }
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteInputText(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid text request", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Text input unavailable", detail = ex.Message });
            }
        }

        public static async Task HandleInputScrollAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            InputScrollRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<InputScrollRequest>(body);
                }
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteInputScroll(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid scroll request", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Scroll unavailable", detail = ex.Message });
            }
        }

        public static async Task HandleInputKeyComboAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            InputKeyComboRequest req = null;
            if (request.HasEntityBody)
            {
                var body = await UnityMcpBridge.ReadRequestBodyAsync(request);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    req = JsonUtility.FromJson<InputKeyComboRequest>(body);
                }
            }

            try
            {
                var result = await UnityMcpBridge.RunOnMainThreadAsync(() => ExecuteInputKeyCombo(req));
                await UnityMcpBridge.WriteJsonAsync(response, 200, result);
            }
            catch (ArgumentException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 400, new ErrorResponse { error = "Invalid key combo request", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                await UnityMcpBridge.WriteJsonAsync(response, 409, new ErrorResponse { error = "Key combo unavailable", detail = ex.Message });
            }
        }

        // =====================================================================
        // Execution Methods
        // =====================================================================

        private static InputClickResponse ExecuteInputClick(InputClickRequest req)
        {
            if (req == null)
            {
                throw new ArgumentException("Request body is required.");
            }

            var button = Mathf.Clamp(req.button, 0, 2);
            var clickCount = Mathf.Clamp(req.clickCount <= 0 ? 1 : req.clickCount, 1, 3);
            var inputContext = GetGameViewInputContext();
            var mousePosition = ResolveMousePosition(inputContext, req.x, req.y, req.normalized);

            inputContext.gameView.Focus();
            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseMove, mousePosition, button, 0));
            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseDown, mousePosition, button, clickCount));
            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseUp, mousePosition, button, clickCount));
            inputContext.gameView.Repaint();

            return new InputClickResponse
            {
                success = true,
                message = "Click dispatched to Game view.",
                x = mousePosition.x,
                y = mousePosition.y,
                button = button,
                clickCount = clickCount,
                gameViewWidth = inputContext.width,
                gameViewHeight = inputContext.height,
                normalized = req.normalized
            };
        }

        private static InputMoveResponse ExecuteInputMove(InputMoveRequest req)
        {
            if (req == null)
            {
                throw new ArgumentException("Request body is required.");
            }

            var inputContext = GetGameViewInputContext();
            var mousePosition = ResolveMousePosition(inputContext, req.x, req.y, req.normalized);

            inputContext.gameView.Focus();
            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseMove, mousePosition, 0, 0));
            inputContext.gameView.Repaint();

            return new InputMoveResponse
            {
                success = true,
                message = "Mouse move dispatched to Game view.",
                x = mousePosition.x,
                y = mousePosition.y,
                gameViewWidth = inputContext.width,
                gameViewHeight = inputContext.height,
                normalized = req.normalized
            };
        }

        private static InputDragResponse ExecuteInputDrag(InputDragRequest req)
        {
            if (req == null)
            {
                throw new ArgumentException("Request body is required.");
            }

            var button = Mathf.Clamp(req.button, 0, 2);
            var steps = Mathf.Clamp(req.steps <= 0 ? 12 : req.steps, 1, 120);
            var inputContext = GetGameViewInputContext();
            var start = ResolveMousePosition(inputContext, req.startX, req.startY, req.normalized);
            var end = ResolveMousePosition(inputContext, req.endX, req.endY, req.normalized);

            inputContext.gameView.Focus();
            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseMove, start, button, 0));
            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseDown, start, button, 1));

            var previous = start;
            for (var i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                var current = Vector2.Lerp(start, end, t);
                inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseDrag, current, button, 0, current - previous));
                previous = current;
            }

            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseUp, end, button, 1));
            inputContext.gameView.Repaint();

            return new InputDragResponse
            {
                success = true,
                message = "Drag dispatched to Game view.",
                startX = start.x,
                startY = start.y,
                endX = end.x,
                endY = end.y,
                button = button,
                steps = steps,
                gameViewWidth = inputContext.width,
                gameViewHeight = inputContext.height,
                normalized = req.normalized
            };
        }

        private static InputKeyResponse ExecuteInputKey(InputKeyRequest req)
        {
            if (req == null)
            {
                throw new ArgumentException("Request body is required.");
            }

            var inputContext = GetGameViewInputContext();
            var phase = ParseKeyPhase(req.phase);
            var keyCode = ParseKeyCode(req.keyCode);
            var character = ParseOptionalCharacter(req.character);
            var modifiers = BuildEventModifiers(req.shift, req.control, req.alt, req.command);

            if (keyCode == KeyCode.None && character == '\0')
            {
                throw new ArgumentException("keyCode or character is required.");
            }

            inputContext.gameView.Focus();
            if (phase == "down" || phase == "press")
            {
                inputContext.gameView.SendEvent(CreateKeyboardEvent(EventType.KeyDown, keyCode, character, modifiers));
            }

            if (phase == "up" || phase == "press")
            {
                inputContext.gameView.SendEvent(CreateKeyboardEvent(EventType.KeyUp, keyCode, character, modifiers));
            }

            inputContext.gameView.Repaint();

            return new InputKeyResponse
            {
                success = true,
                message = "Key input dispatched to Game view.",
                phase = phase,
                keyCode = keyCode.ToString(),
                character = character == '\0' ? string.Empty : character.ToString(),
                modifiers = modifiers.ToString()
            };
        }

        private static InputTextResponse ExecuteInputText(InputTextRequest req)
        {
            if (req == null || req.text == null)
            {
                throw new ArgumentException("text is required.");
            }

            var inputContext = GetGameViewInputContext();
            inputContext.gameView.Focus();

            var charactersSubmitted = 0;
            foreach (var character in req.text)
            {
                DispatchTextCharacter(inputContext.gameView, character);
                charactersSubmitted++;
            }

            if (req.appendReturn)
            {
                inputContext.gameView.SendEvent(CreateKeyboardEvent(EventType.KeyDown, KeyCode.Return, '\n', EventModifiers.None));
                inputContext.gameView.SendEvent(CreateKeyboardEvent(EventType.KeyUp, KeyCode.Return, '\n', EventModifiers.None));
            }

            inputContext.gameView.Repaint();

            return new InputTextResponse
            {
                success = true,
                message = "Text input dispatched to Game view.",
                charactersSubmitted = charactersSubmitted,
                appendReturn = req.appendReturn
            };
        }

        private static InputScrollResponse ExecuteInputScroll(InputScrollRequest req)
        {
            if (req == null)
            {
                throw new ArgumentException("Request body is required.");
            }

            var inputContext = GetGameViewInputContext();
            var mousePosition = ResolveMousePosition(inputContext, req.x, req.y, req.normalized);
            var deltaX = req.deltaX;
            var deltaY = Mathf.Approximately(req.deltaY, 0f) ? req.delta : req.deltaY;
            if (float.IsNaN(deltaX) || float.IsInfinity(deltaX) || float.IsNaN(deltaY) || float.IsInfinity(deltaY))
            {
                throw new ArgumentException("Scroll delta values must be finite numbers.");
            }

            var scrollDelta = new Vector2(deltaX, deltaY);
            inputContext.gameView.Focus();
            inputContext.gameView.SendEvent(CreateMouseEvent(EventType.MouseMove, mousePosition, 0, 0));
            inputContext.gameView.SendEvent(CreateScrollEvent(mousePosition, scrollDelta));
            inputContext.gameView.Repaint();

            return new InputScrollResponse
            {
                success = true,
                message = "Scroll dispatched to Game view.",
                x = mousePosition.x,
                y = mousePosition.y,
                deltaX = scrollDelta.x,
                deltaY = scrollDelta.y,
                gameViewWidth = inputContext.width,
                gameViewHeight = inputContext.height,
                normalized = req.normalized
            };
        }

        private static InputKeyComboResponse ExecuteInputKeyCombo(InputKeyComboRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.preset))
            {
                throw new ArgumentException("preset is required.");
            }

            var inputContext = GetGameViewInputContext();
            var preset = ResolveKeyComboPreset(req.preset);
            var repeat = Mathf.Clamp(req.repeat <= 0 ? 1 : req.repeat, 1, 10);

            inputContext.gameView.Focus();
            for (var i = 0; i < repeat; i++)
            {
                SendKeyPress(inputContext.gameView, preset.keyCode, preset.character, preset.modifiers);
            }

            inputContext.gameView.Repaint();

            return new InputKeyComboResponse
            {
                success = true,
                message = "Key combo preset dispatched to Game view.",
                preset = preset.name,
                keyCode = preset.keyCode.ToString(),
                character = preset.character == '\0' ? string.Empty : preset.character.ToString(),
                modifiers = preset.modifiers.ToString(),
                repeat = repeat
            };
        }

        // =====================================================================
        // Helper Methods
        // =====================================================================

        private static void DispatchTextCharacter(EditorWindow gameView, char character)
        {
            var keyCode = KeyCode.None;
            var eventCharacter = character;

            switch (character)
            {
                case '\r':
                case '\n':
                    keyCode = KeyCode.Return;
                    eventCharacter = '\n';
                    break;
                case '\b':
                    keyCode = KeyCode.Backspace;
                    break;
                case '\t':
                    keyCode = KeyCode.Tab;
                    break;
            }

            SendKeyPress(gameView, keyCode, eventCharacter, EventModifiers.None);
        }

        private static void SendKeyPress(EditorWindow gameView, KeyCode keyCode, char character, EventModifiers modifiers)
        {
            gameView.SendEvent(CreateKeyboardEvent(EventType.KeyDown, keyCode, character, modifiers));
            gameView.SendEvent(CreateKeyboardEvent(EventType.KeyUp, keyCode, character, modifiers));
        }

        private static GameViewInputContext GetGameViewInputContext()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Game view input requires play mode.");
            }

            var gameView = GetGameViewWindow();
            if (gameView == null)
            {
                throw new InvalidOperationException("Game view window could not be opened.");
            }

            var gameViewSize = Handles.GetMainGameViewSize();
            return new GameViewInputContext
            {
                gameView = gameView,
                width = Mathf.Max(1f, gameViewSize.x),
                height = Mathf.Max(1f, gameViewSize.y)
            };
        }

        private static Vector2 ResolveMousePosition(GameViewInputContext inputContext, float x, float y, bool normalized)
        {
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
            {
                throw new ArgumentException("x and y must be finite numbers.");
            }

            if (normalized)
            {
                if (x < 0f || x > 1f || y < 0f || y > 1f)
                {
                    throw new ArgumentException("Normalized x and y must be between 0 and 1.");
                }

                return new Vector2(x * inputContext.width, y * inputContext.height);
            }

            if (x < 0f || x > inputContext.width || y < 0f || y > inputContext.height)
            {
                throw new ArgumentException(
                    "Pixel coordinates must stay inside the Game view bounds: "
                    + inputContext.width.ToString("0") + "x" + inputContext.height.ToString("0") + ".");
            }

            return new Vector2(x, y);
        }

        private static string ParseKeyPhase(string phase)
        {
            if (string.IsNullOrWhiteSpace(phase))
            {
                return "press";
            }

            var normalized = phase.Trim().ToLowerInvariant();
            if (normalized == "press" || normalized == "down" || normalized == "up")
            {
                return normalized;
            }

            throw new ArgumentException("phase must be one of: press, down, up.");
        }

        private static KeyCode ParseKeyCode(string keyCodeRaw)
        {
            if (string.IsNullOrWhiteSpace(keyCodeRaw))
            {
                return KeyCode.None;
            }

            if (Enum.TryParse(keyCodeRaw.Trim(), true, out KeyCode keyCode))
            {
                return keyCode;
            }

            throw new ArgumentException("Unknown Unity KeyCode: " + keyCodeRaw);
        }

        private static char ParseOptionalCharacter(string characterRaw)
        {
            if (string.IsNullOrEmpty(characterRaw))
            {
                return '\0';
            }

            if (characterRaw.Length == 1)
            {
                return characterRaw[0];
            }

            switch (characterRaw.Trim().ToLowerInvariant())
            {
                case "\\n":
                case "newline":
                case "return":
                    return '\n';
                case "\\t":
                case "tab":
                    return '\t';
                case "\\b":
                case "backspace":
                    return '\b';
                default:
                    throw new ArgumentException("character must be a single character or one of: \\n, \\t, \\b.");
            }
        }

        private static EventModifiers BuildEventModifiers(bool shift, bool control, bool alt, bool command)
        {
            var modifiers = EventModifiers.None;
            if (shift) modifiers |= EventModifiers.Shift;
            if (control) modifiers |= EventModifiers.Control;
            if (alt) modifiers |= EventModifiers.Alt;
            if (command) modifiers |= EventModifiers.Command;
            return modifiers;
        }

        private static KeyComboPreset ResolveKeyComboPreset(string presetRaw)
        {
            var normalized = presetRaw.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "copy": return new KeyComboPreset("copy", KeyCode.C, '\0', EventModifiers.Control);
                case "paste": return new KeyComboPreset("paste", KeyCode.V, '\0', EventModifiers.Control);
                case "cut": return new KeyComboPreset("cut", KeyCode.X, '\0', EventModifiers.Control);
                case "selectall":
                case "select_all": return new KeyComboPreset("selectAll", KeyCode.A, '\0', EventModifiers.Control);
                case "undo": return new KeyComboPreset("undo", KeyCode.Z, '\0', EventModifiers.Control);
                case "redo": return new KeyComboPreset("redo", KeyCode.Y, '\0', EventModifiers.Control);
                case "submit":
                case "enter": return new KeyComboPreset("submit", KeyCode.Return, '\n', EventModifiers.None);
                case "cancel":
                case "escape": return new KeyComboPreset("cancel", KeyCode.Escape, '\0', EventModifiers.None);
                case "tabforward":
                case "tab_forward": return new KeyComboPreset("tabForward", KeyCode.Tab, '\t', EventModifiers.None);
                case "tabbackward":
                case "tab_backward": return new KeyComboPreset("tabBackward", KeyCode.Tab, '\t', EventModifiers.Shift);
                case "delete": return new KeyComboPreset("delete", KeyCode.Delete, '\0', EventModifiers.None);
                default:
                    throw new ArgumentException("Unknown preset. Supported: copy, paste, cut, selectAll, undo, redo, submit, cancel, tabForward, tabBackward, delete.");
            }
        }

        private static Event CreateKeyboardEvent(EventType eventType, KeyCode keyCode, char character, EventModifiers modifiers)
        {
            return new Event { type = eventType, keyCode = keyCode, character = character, modifiers = modifiers };
        }

        private static Event CreateScrollEvent(Vector2 mousePosition, Vector2 delta)
        {
            return new Event { type = EventType.ScrollWheel, mousePosition = mousePosition, delta = delta, modifiers = EventModifiers.None };
        }

        private static Event CreateMouseEvent(EventType eventType, Vector2 mousePosition, int button, int clickCount)
        {
            return CreateMouseEvent(eventType, mousePosition, button, clickCount, Vector2.zero);
        }

        private static Event CreateMouseEvent(EventType eventType, Vector2 mousePosition, int button, int clickCount, Vector2 delta)
        {
            return new Event { type = eventType, mousePosition = mousePosition, button = button, clickCount = clickCount, delta = delta, modifiers = EventModifiers.None };
        }

        private static EditorWindow GetGameViewWindow()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null) return null;
            var gameView = EditorWindow.GetWindow(gameViewType);
            gameView?.Show();
            return gameView;
        }

        private sealed class GameViewInputContext
        {
            public EditorWindow gameView;
            public float width;
            public float height;
        }

        private sealed class KeyComboPreset
        {
            public readonly string name;
            public readonly KeyCode keyCode;
            public readonly char character;
            public readonly EventModifiers modifiers;

            public KeyComboPreset(string name, KeyCode keyCode, char character, EventModifiers modifiers)
            {
                this.name = name;
                this.keyCode = keyCode;
                this.character = character;
                this.modifiers = modifiers;
            }
        }
    }
}
#endif
