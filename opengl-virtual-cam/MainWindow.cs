using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ImGuiNET;
using opengl_virtual_cam.imgui;
using opengl_virtual_cam.scene;

namespace opengl_virtual_cam;

public class MainWindow() : GameWindow(GameWindowSettings.Default,
    new NativeWindowSettings
    {
        Title = Config.Title,
        ClientSize = Config.ClientSize,
        MinimumClientSize = new Vector2i(640, 480)
    })
{
    private readonly Camera _cam = new();
    private InputHandler? _inputHandler;
    
    // Scene management
    private readonly List<IScene?> _scenes = [];
    private int _currentSceneIndex;
    private IScene? _currentScene;
    private int _shader;

    // ImGui fields
    private ImGuiController _controller = null!;
    private bool _showSidePanel = true;

    // FPS calculation
    private readonly Queue<float> _frameTimeHistory = new();
    private const int FrameTimeHistoryLength = 60;
    private float _averageFrameTime;
    private float _fps;

    protected override void OnLoad()
    {
        // OpenGL settings
        GL.Enable(EnableCap.DepthTest);
        GL.LineWidth(2.0f);

        // Initialize ImGui controller
        _controller = new ImGuiController(Size.X, Size.Y);
        
        // Initialize input handler
        _inputHandler = new InputHandler(_cam);

        // Add all scenes
        _scenes.Add(new CuboidsScene());
        _scenes.Add(new RubiksScene());

        // Load the first scene
        _currentScene = _scenes[_currentSceneIndex];
        _shader = _currentScene!.Initialize();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
        ImGuiController.WindowResized(Size.X, Size.Y);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        // Process all inputs through the InputHandler
        _inputHandler!.ProcessInput(KeyboardState, MouseState, (float)e.Time);
        
        // Update ImGui controller
        _controller.Update(this, (float)e.Time);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        // Clearing OpenGL buffers
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.UseProgram(_shader);

        var proj = _cam.Projection(Size.X / (float)Size.Y);
        var view = _cam.View;

        _currentScene!.Render(ref view, ref proj);

        // Render ImGui
        RenderImGuiContent();
        RenderHideButton();
        RecalculateFps((float)e.Time);
        RenderFpsCounter();
        _controller.Render();
        SwapBuffers();
    }

    private void RecalculateFps(float currentFrameTime)
    {
        _frameTimeHistory.Enqueue(currentFrameTime);
        if (_frameTimeHistory.Count > FrameTimeHistoryLength)
            _frameTimeHistory.Dequeue();

        _averageFrameTime = 0;
        foreach (var time in _frameTimeHistory)
            _averageFrameTime += time;

        _averageFrameTime /= _frameTimeHistory.Count;
        _fps = 1.0f / (_averageFrameTime > 0 ? _averageFrameTime : 0.001f);
    }

    private void RenderFpsCounter()
    {
        // We skip the render if the side panel is hidden
        if (!_showSidePanel) return;

        const ImGuiWindowFlags fpsWindowFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize |
                                                ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoMove |
                                                ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings;

        var fpsWindowPos = new System.Numerics.Vector2(Size.X - 80, Size.Y - 40);
        ImGui.SetNextWindowPos(fpsWindowPos, ImGuiCond.Always);

        if (ImGui.Begin("FPSCounter", fpsWindowFlags))
            ImGui.Text($"FPS: {_fps:F0}");

        ImGui.End();
    }

    private void RenderHideButton()
    {
        const ImGuiWindowFlags hideButtonFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                                                 ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings;
        const float buttonWidth = 30;
        const float buttonHeight = 25;

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X - buttonWidth - 10, 5));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(buttonWidth, buttonHeight));
        ImGui.Begin("HideButton", hideButtonFlags);

        if (ImGui.Button(_showSidePanel ? "X" : "+"))
            _showSidePanel = !_showSidePanel;

        ImGui.End();
    }

    private void RenderImGuiContent()
    {
        if (!_showSidePanel)
            return;

        const float panelWidth = 240;
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X - panelWidth, 0));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(panelWidth, Size.Y));

        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                                             ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
                                             ImGuiWindowFlags.NoTitleBar;
        ImGui.Begin("SidePanel", windowFlags);

        ImGui.Text("Scene Selection:");

        for (var i = 0; i < _scenes.Count; i++)
        {
            var isSelected = i == _currentSceneIndex;
            if (!ImGui.RadioButton(_scenes[i]!.Name, isSelected)) continue;
            if (i == _currentSceneIndex) continue;

            _currentScene?.Dispose();
            _currentSceneIndex = i;
            _currentScene = _scenes[_currentSceneIndex];
            _shader = _currentScene!.Initialize();
        }

        ImGui.Separator();

        // Position inputs
        ImGui.Text("Position:");
        var posX = _cam.Position.X;
        var posY = _cam.Position.Y;
        var posZ = _cam.Position.Z;

        if (ImGui.InputFloat("X", ref posX, 0.1f, 1.0f, "%.2f"))
            _cam.Position = new Vector3(posX, _cam.Position.Y, _cam.Position.Z);

        if (ImGui.InputFloat("Y", ref posY, 0.1f, 1.0f, "%.2f"))
            _cam.Position = new Vector3(_cam.Position.X, posY, _cam.Position.Z);

        if (ImGui.InputFloat("Z", ref posZ, 0.1f, 1.0f, "%.2f"))
            _cam.Position = new Vector3(_cam.Position.X, _cam.Position.Y, posZ);

        ImGui.Separator();

        // Rotation inputs
        ImGui.Text("Rotation:");
        var yaw = _cam.Yaw;
        var pitch = _cam.Pitch;
        var roll = _cam.Roll;

        if (ImGui.SliderFloat("Yaw", ref yaw, -180f, 180f, "%.1f°"))
        {
            _cam.Yaw = yaw;
            _cam.UpdateVectors();
        }

        if (ImGui.SliderFloat("Pitch", ref pitch, -89f, 89f, "%.1f°"))
        {
            _cam.Pitch = pitch;
            _cam.UpdateVectors();
        }

        if (ImGui.SliderFloat("Roll", ref roll, -180f, 180f, "%.1f°"))
        {
            _cam.Roll = roll;
            _cam.UpdateVectors();
        }

        ImGui.Separator();

        // FOV input
        ImGui.Text("Field of View:");
        var fov = _cam.Fov;
        if (ImGui.SliderFloat("FOV", ref fov, 15f, 90f, "%.1f°"))
            _cam.Fov = fov;

        ImGui.Separator();

        // Reset button
        if (ImGui.Button("Reset Camera", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 30)))
            ResetCamera();

        ImGui.End();
    }

    private void ResetCamera()
    {
        _cam.Position = Config.CameraPosition;
        _cam.Yaw = Config.Yaw;
        _cam.Pitch = Config.Pitch;
        _cam.Roll = Config.Roll;
        _cam.Fov = Config.FieldOfView;
        _cam.UpdateVectors();
    }

    protected override void OnUnload()
    {
        _currentScene?.Dispose();
        _controller.Dispose();
        base.OnUnload();
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
        base.OnFocusedChanged(e);
        var io = ImGui.GetIO();
        io.AddFocusEvent(e.IsFocused);
    }
}