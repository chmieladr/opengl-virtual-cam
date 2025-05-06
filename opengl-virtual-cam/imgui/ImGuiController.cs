using ImGuiNET;
using OpenTK.Windowing.Desktop;

namespace opengl_virtual_cam.imgui
{
    public class ImGuiController : IDisposable
    {
        private bool _frameBegun;
        private readonly System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;
        
        private readonly ImGuiInputHandler _inputHandler;
        private readonly ImGuiRenderer _renderer;
        private readonly ImGuiResourceManager _resourceManager;

        public ImGuiController(int width, int height)
        {
            // Initialize ImGui context
            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            // Configure ImGui
            var io = ImGui.GetIO();
            io.Fonts.AddFontDefault();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.DisplaySize = new System.Numerics.Vector2(width, height);

            // Create components
            _inputHandler = new ImGuiInputHandler();
            _resourceManager = new ImGuiResourceManager();
            _renderer = new ImGuiRenderer(_resourceManager);
            
            // Initialize resources
            _resourceManager.CreateDeviceResources();

            // Initial frame setup
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
            _inputHandler.UpdateImGuiInput(wnd);

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
            _renderer.RenderImDrawData(ImGui.GetDrawData());
        }

        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            var io = ImGui.GetIO();
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds;
        }

        public void Dispose()
        {
            _resourceManager.Dispose();
            ImGui.DestroyContext();
            
            GC.SuppressFinalize(this);
        }
    }
}