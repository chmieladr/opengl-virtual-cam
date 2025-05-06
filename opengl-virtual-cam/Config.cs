// ReSharper disable RedundantDefaultMemberInitializer

using OpenTK.Mathematics;

namespace opengl_virtual_cam;

public static class Config
{
    public static Vector3i CameraPosition { get; } = new(0, 0, 0);
    
    public static string Title => "Virtual Camera | OpenGL";
    public static Vector2i ClientSize { get; } = new(1280, 720);
    
    public static float FieldOfView => 45f;
    public static float MovementSpeed => 2.5f;
    public static float RotationSpeed => 60.0f;

    public static float Yaw => 0f;
    public static float Pitch => 0f;
    public static float Roll => 0f;
    public static float ZoomStep => 1.5f;
}