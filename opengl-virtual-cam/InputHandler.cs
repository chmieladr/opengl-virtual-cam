using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

namespace opengl_virtual_cam;

public class InputHandler(Camera camera)
{
    private readonly float _movementSpeed = Config.MovementSpeed;
    private readonly float _rotationSpeed = Config.RotationSpeed;
    private readonly float _zoomStep = Config.ZoomStep;
    
    // Track previous key states
    private bool _previousEqualKeyPressed;
    private bool _previousMinusKeyPressed;
    private bool _previousKeypadAddPressed;
    private bool _previousKeypadSubtractPressed;

    public void ProcessInput(KeyboardState keyboardState, MouseState mouseState, float deltaTime)
    {
        if (ImGui.GetIO().WantCaptureKeyboard)
            return;

        ProcessMovementInput(keyboardState, deltaTime);
        ProcessRotationInput(keyboardState, deltaTime);
        ProcessZoomInput(keyboardState, mouseState);
    }

    private void ProcessMovementInput(KeyboardState keyboardState, float deltaTime)
    {
        if (keyboardState.IsKeyDown(Keys.W)) 
            camera.Move(_movementSpeed * deltaTime * camera.Front);
            
        if (keyboardState.IsKeyDown(Keys.S)) 
            camera.Move(-_movementSpeed * deltaTime * camera.Front);
            
        if (keyboardState.IsKeyDown(Keys.A))
            camera.Move(-Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * _movementSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.D))
            camera.Move(Vector3.Normalize(Vector3.Cross(camera.Front, camera.Up)) * _movementSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.Space)) 
            camera.Move(camera.Up * _movementSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.LeftShift)) 
            camera.Move(-camera.Up * _movementSpeed * deltaTime);
    }

    private void ProcessRotationInput(KeyboardState keyboardState, float deltaTime)
    {
        if (keyboardState.IsKeyDown(Keys.Left)) 
            camera.AddYaw(-_rotationSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.Right)) 
            camera.AddYaw(_rotationSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.Up)) 
            camera.AddPitch(_rotationSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.Down)) 
            camera.AddPitch(-_rotationSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.Q)) 
            camera.AddRoll(_rotationSpeed * deltaTime);
            
        if (keyboardState.IsKeyDown(Keys.E)) 
            camera.AddRoll(-_rotationSpeed * deltaTime);
    }

    private void ProcessZoomInput(KeyboardState keyboardState, MouseState mouseState)
    {
        // Zoom (scroll wheel)
        camera.Fov = MathHelper.Clamp(camera.Fov - mouseState.ScrollDelta.Y, 15f, 90f);

        // Zoom (keyboard)
        var equalKeyPressed = keyboardState.IsKeyDown(Keys.Equal);
        var minusKeyPressed = keyboardState.IsKeyDown(Keys.Minus);
        var keypadAddPressed = keyboardState.IsKeyDown(Keys.KeyPadAdd);
        var keypadSubtractPressed = keyboardState.IsKeyDown(Keys.KeyPadSubtract);

        if ((equalKeyPressed && !_previousEqualKeyPressed) ||
            (keypadAddPressed && !_previousKeypadAddPressed))
            camera.Fov = MathHelper.Clamp(camera.Fov - _zoomStep, 15f, 90f);

        if ((minusKeyPressed && !_previousMinusKeyPressed) ||
            (keypadSubtractPressed && !_previousKeypadSubtractPressed))
            camera.Fov = MathHelper.Clamp(camera.Fov + _zoomStep, 15f, 90f);

        // Update previous key states
        _previousEqualKeyPressed = equalKeyPressed;
        _previousMinusKeyPressed = minusKeyPressed;
        _previousKeypadAddPressed = keypadAddPressed;
        _previousKeypadSubtractPressed = keypadSubtractPressed;
    }
}