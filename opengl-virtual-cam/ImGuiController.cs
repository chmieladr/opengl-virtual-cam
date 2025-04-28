using System.Runtime.CompilerServices;
using ImGuiNET;
using opengl_virtual_cam.shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

// ReSharper disable InconsistentNaming

namespace opengl_virtual_cam
{
    public class ImGuiController : IDisposable
    {
        private bool _frameBegun;
        private int _vertexArray;
        private int _vertexBuffer;
        private int _vertexBufferSize;
        private int _indexBuffer;
        private int _indexBufferSize;

        private int _fontTexture;
        private int _shader;
        private int _shaderFontTextureLocation;
        private int _shaderProjectionMatrixLocation;

        // Keys tracker
        private HashSet<Keys> _previouslyPressedKeys = [];
        private HashSet<Keys> _currentlyPressedKeys = [];

        private readonly System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

        public ImGuiController(int width, int height)
        {
            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            var io = ImGui.GetIO();
            io.Fonts.AddFontDefault();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            io.DisplaySize = new System.Numerics.Vector2(width, height);

            CreateDeviceResources();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();
            _frameBegun = true;
        }

        public static void WindowResized(int width, int height)
        {
            var io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(width, height);
        }

        public void Update(GameWindow wnd, float deltaSeconds)
        {
            if (_frameBegun)
            {
                ImGui.Render();
                _frameBegun = false;
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(wnd);

            var io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(wnd.Size.X, wnd.Size.Y);

            _frameBegun = true;
            ImGui.NewFrame();
        }

        public void Render()
        {
            if (!_frameBegun) return;
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteBuffer(_indexBuffer);
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteTexture(_fontTexture);
            GL.DeleteProgram(_shader);
            ImGui.DestroyContext();

            GC.SuppressFinalize(this);
        }

        private void CreateDeviceResources()
        {
            _vertexBufferSize = 10000;
            _indexBufferSize = 2000;

            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);

            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            RecreateFontDeviceTexture();

            var shaderManager = new ShaderManager(new ImGuiShaders());
            _shader = shaderManager.CreateProgram();

            _shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "uProjection");
            _shaderFontTextureLocation = GL.GetUniformLocation(_shader, "uTexture");

            GL.BindVertexArray(_vertexArray);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 8);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, Unsafe.SizeOf<ImDrawVert>(), 16);
        }

        private void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out _);

            if (_fontTexture != 0)
            {
                GL.DeleteTexture(_fontTexture);
            }

            _fontTexture = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, pixels);

            io.Fonts.SetTexID(_fontTexture);
            io.Fonts.ClearTexData();
        }

        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            var io = ImGui.GetIO();
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds;
        }

        private void UpdateImGuiInput(GameWindow wnd)
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
            // Process camera movement keys
            ProcessKeyGroup(keyboardState, io, Keys.W, Keys.A, Keys.S, Keys.D, Keys.Q, Keys.E);
            ProcessKeyGroup(keyboardState, io, Keys.Left, Keys.Right, Keys.Up, Keys.Down);
            ProcessKeyGroup(keyboardState, io, Keys.Space, Keys.LeftShift);
            ProcessKeyGroup(keyboardState, io, Keys.Backspace);

            // Process number keys
            for (var k = Keys.D0; k <= Keys.D9; k++)
                ProcessKeyGroup(keyboardState, io, k);

            // Process numpad keys
            for (var k = Keys.KeyPad0; k <= Keys.KeyPad9; k++)
                ProcessKeyGroup(keyboardState, io, k);

            // Process other special keys
            ProcessKeyGroup(keyboardState, io, Keys.Period, Keys.Equal, Keys.Minus, Keys.KeyPadAdd,
                Keys.KeyPadSubtract, Keys.KeyPadDecimal);
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

        private void RenderImDrawData(ImDrawDataPtr drawData)
        {
            if (drawData.CmdListsCount == 0) return;

            // Save GL state
            var state = SaveGLState();

            // Setup ImGui rendering state
            SetupImGuiRenderState();

            // Configure viewport and projection
            var orthoProj = CreateOrthographicProjection(drawData);
            GL.UseProgram(_shader);
            GL.Uniform1(_shaderFontTextureLocation, 0);
            GL.UniformMatrix4(_shaderProjectionMatrixLocation, 1, false, ref orthoProj.M11);
            GL.BindVertexArray(_vertexArray);

            // Process command lists
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];
                UpdateBuffers(cmdList);
                RenderCommandList(cmdList, drawData);
            }

            // Restore original GL state
            RestoreGLState(state);
        }

        private static GLState SaveGLState()
        {
            var state = new GLState();
            GL.GetInteger(GetPName.ActiveTexture, out state.LastActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);

            GL.GetInteger(GetPName.CurrentProgram, out state.LastProgram);
            GL.GetInteger(GetPName.TextureBinding2D, out state.LastTexture);
            GL.GetInteger(GetPName.SamplerBinding, out state.LastSampler);

            GL.GetInteger(GetPName.ArrayBufferBinding, out state.LastArrayBuffer);
            GL.GetInteger(GetPName.VertexArrayBinding, out state.LastVertexArray);

            GL.GetInteger(GetPName.BlendSrc, out state.LastBlendSrcRgb);
            GL.GetInteger(GetPName.BlendDst, out state.LastBlendDstRgb);

            GL.GetInteger(GetPName.BlendSrcAlpha, out state.LastBlendSrcAlpha);
            GL.GetInteger(GetPName.BlendDstAlpha, out state.LastBlendDstAlpha);

            GL.GetInteger(GetPName.BlendEquationRgb, out state.LastBlendEquationRgb);
            GL.GetInteger(GetPName.BlendEquationAlpha, out state.LastBlendEquationAlpha);

            state.LastEnableBlend = GL.IsEnabled(EnableCap.Blend);
            state.LastEnableCullFace = GL.IsEnabled(EnableCap.CullFace);
            state.LastEnableDepthTest = GL.IsEnabled(EnableCap.DepthTest);
            state.LastEnableScissorTest = GL.IsEnabled(EnableCap.ScissorTest);

            return state;
        }

        private void SetupImGuiRenderState()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ScissorTest);
        }

        private static System.Numerics.Matrix4x4 CreateOrthographicProjection(ImDrawDataPtr drawData)
        {
            GL.Viewport(0, 0, (int)drawData.DisplaySize.X, (int)drawData.DisplaySize.Y);
            var L = drawData.DisplayPos.X;
            var R = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var T = drawData.DisplayPos.Y;
            var B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            return new System.Numerics.Matrix4x4(
                2.0f / (R - L), 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f / (T - B), 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f,
                (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f
            );
        }

        private void UpdateBuffers(ImDrawListPtr cmdList)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            var vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                _vertexBufferSize = Math.Max(_vertexBufferSize * 2, vertexSize);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero,
                    BufferUsageHint.DynamicDraw);
            }

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            var indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize)
            {
                _indexBufferSize = Math.Max(_indexBufferSize * 2, indexSize);
                GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero,
                    BufferUsageHint.DynamicDraw);
            }

            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero,
                cmdList.IdxBuffer.Size * sizeof(ushort), cmdList.IdxBuffer.Data);
        }

        private static void RenderCommandList(ImDrawListPtr cmdList, ImDrawDataPtr drawData)
        {
            var idxOffset = 0;
            for (var cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                var cmd = cmdList.CmdBuffer[cmdIndex];

                // Apply scissor rectangle
                var clip = cmd.ClipRect;
                GL.Scissor(
                    (int)clip.X,
                    (int)(drawData.DisplaySize.Y - clip.W),
                    (int)(clip.Z - clip.X),
                    (int)(clip.W - clip.Y)
                );

                // Bind texture and draw
                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);
                GL.DrawElementsBaseVertex(
                    PrimitiveType.Triangles,
                    (int)cmd.ElemCount,
                    DrawElementsType.UnsignedShort,
                    idxOffset * sizeof(ushort),
                    0
                );

                idxOffset += (int)cmd.ElemCount;
            }
        }

        private static void RestoreGLState(GLState state)
        {
            GL.UseProgram(state.LastProgram);
            GL.BindTexture(TextureTarget.Texture2D, state.LastTexture);
            GL.BindSampler(0, state.LastSampler);
            GL.ActiveTexture((TextureUnit)state.LastActiveTexture);
            GL.BindVertexArray(state.LastVertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, state.LastArrayBuffer);

            GL.BlendEquationSeparate(
                (BlendEquationMode)state.LastBlendEquationRgb,
                (BlendEquationMode)state.LastBlendEquationAlpha
            );
            GL.BlendFuncSeparate(
                (BlendingFactorSrc)state.LastBlendSrcRgb,
                (BlendingFactorDest)state.LastBlendDstRgb,
                (BlendingFactorSrc)state.LastBlendSrcAlpha,
                (BlendingFactorDest)state.LastBlendDstAlpha
            );

            SetGLCapability(EnableCap.Blend, state.LastEnableBlend);
            SetGLCapability(EnableCap.CullFace, state.LastEnableCullFace);
            SetGLCapability(EnableCap.DepthTest, state.LastEnableDepthTest);
            SetGLCapability(EnableCap.ScissorTest, state.LastEnableScissorTest);
        }

        private static void SetGLCapability(EnableCap cap, bool enabled)
        {
            if (enabled)
                GL.Enable(cap);
            else
                GL.Disable(cap);
        }
    }
}