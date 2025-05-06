using ImGuiNET;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace opengl_virtual_cam.imgui
{
    public class ImGuiInputHandler
    {
        // Keys tracker for preventing duplicate character inputs
        private HashSet<Keys> _previouslyPressedKeys = [];
        private HashSet<Keys> _currentlyPressedKeys = [];

        public void UpdateImGuiInput(GameWindow wnd)
        {
            var io = ImGui.GetIO();
            _currentlyPressedKeys.Clear();

            // Setup mouse inputs
            io.MousePos = new System.Numerics.Vector2(wnd.MouseState.X, wnd.MouseState.Y);
            io.MouseDown[0] = wnd.MouseState.IsButtonDown(MouseButton.Left);
            io.MouseDown[1] = wnd.MouseState.IsButtonDown(MouseButton.Right);
            io.MouseDown[2] = wnd.MouseState.IsButtonDown(MouseButton.Middle);
            io.MouseWheel = wnd.MouseState.ScrollDelta.Y;

            // Setup keyboard inputs
            var keyboardState = wnd.KeyboardState;
            ProcessKeys(keyboardState, io);
            (_previouslyPressedKeys, _currentlyPressedKeys) = (_currentlyPressedKeys, _previouslyPressedKeys);
        }

        private void ProcessKeys(KeyboardState keyboardState, ImGuiIOPtr io)
        {
            // Define all key groups in a single collection
            var keyGroups = new[]
            {
                [Keys.W, Keys.A, Keys.S, Keys.D, Keys.Q, Keys.E],
                [Keys.Left, Keys.Right, Keys.Up, Keys.Down],
                [Keys.Space, Keys.LeftShift, Keys.Backspace],
                Enumerable.Range((int)Keys.D0, 10).Select(i => (Keys)i).ToArray(),
                Enumerable.Range((int)Keys.KeyPad0, 10).Select(i => (Keys)i).ToArray(),
                [Keys.Period, Keys.Equal, Keys.Minus, Keys.KeyPadAdd, Keys.KeyPadSubtract, Keys.KeyPadDecimal]
            };

            // Process all key groups at once
            foreach (var group in keyGroups)
            {
                ProcessKeyGroup(keyboardState, io, group);
            }
        }

        private void ProcessKeyGroup(KeyboardState keyboardState, ImGuiIOPtr io, params Keys[] keys)
        {
            var shift = SafeIsKeyDown(keyboardState, Keys.LeftShift) || SafeIsKeyDown(keyboardState, Keys.RightShift);

            foreach (var key in keys)
            {
                var isDown = SafeIsKeyDown(keyboardState, key);
                io.AddKeyEvent(MapKey(key), isDown);

                if (!isDown) continue;
                _currentlyPressedKeys.Add(key);

                // Only add character input on the initial press
                if (_previouslyPressedKeys.Contains(key)) continue;
                var c = GetCharFromKey(key, shift);
                if (c != '\0')
                {
                    io.AddInputCharacter(c);
                }
            }
        }

        private static bool SafeIsKeyDown(KeyboardState keyboardState, Keys key)
        {
            try
            {
                return keyboardState.IsKeyDown(key);
            }
            catch
            {
                return false;
            }
        }

        private static ImGuiKey MapKey(Keys key)
        {
            return key switch
            {
                Keys.Left => ImGuiKey.LeftArrow,
                Keys.Right => ImGuiKey.RightArrow,
                Keys.Up => ImGuiKey.UpArrow,
                Keys.Down => ImGuiKey.DownArrow,
                Keys.Backspace => ImGuiKey.Backspace,
                Keys.Space => ImGuiKey.Space,
                Keys.Enter => ImGuiKey.Enter,
                _ => ImGuiKey.None
            };
        }

        private static char GetCharFromKey(Keys key, bool shift)
        {
            return key switch
            {
                >= Keys.D0 and <= Keys.D9 when !shift => (char)('0' + (key - Keys.D0)),
                >= Keys.KeyPad0 and <= Keys.KeyPad9 when !shift => (char)('0' + (key - Keys.KeyPad0)),
                Keys.Period or Keys.KeyPadDecimal => '.',
                Keys.Minus or Keys.KeyPadSubtract => '-',
                Keys.Equal when shift => '+',
                Keys.KeyPadAdd => '+',
                _ => '\0'
            };
        }
    }
}