namespace opengl_virtual_cam;

using OpenTK.Mathematics;

public class Camera
{
    public Vector3 Position = Config.CameraPosition;
    public Vector3 Front = -Vector3.UnitZ;
    public Vector3 Up = Vector3.UnitY;

    public float Yaw = Config.Yaw;
    public float Pitch = Config.Pitch;
    public float Roll = Config.Roll;
    public float Fov = Config.FieldOfView;
    
    public Camera()
    {
        UpdateVectors();
    }

    public Matrix4 View => Matrix4.LookAt(Position, Position + Front, Up);

    public Matrix4 Projection(float aspect) =>
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), aspect, 0.1f, 100f);

    public void Move(Vector3 delta) => Position += delta;

    public void AddYaw(float degrees)
    {
        Yaw += degrees;
        Yaw = NormalizeAngle(Yaw);
        UpdateVectors();
    }
    
    public void AddRoll(float degrees)
    {
        Roll += degrees;
        Roll = NormalizeAngle(Roll);
        UpdateVectors();
    }

    public void AddPitch(float degrees)
    {
        Pitch = MathHelper.Clamp(Pitch + degrees, -89f, 89f);
        UpdateVectors();
    }
    
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        switch (angle)
        {
            case > 180f:
                angle -= 360f;
                break;
            case < -180f:
                angle += 360f;
                break;
        }
        return angle;
    }
    
    public void UpdateVectors()
    {
        // Updating the front vector based on yaw and pitch
        var yawRad = MathHelper.DegreesToRadians(Yaw);
        var pitchRad = MathHelper.DegreesToRadians(Pitch);
        Front = Vector3.Normalize(new Vector3(
            MathF.Cos(yawRad) * MathF.Cos(pitchRad),
            MathF.Sin(pitchRad),
            MathF.Sin(yawRad) * MathF.Cos(pitchRad)
        ));
    
        // Applying roll rotation directly to the world up vector
        var worldUp = Vector3.UnitY;
        var rollRad = MathHelper.DegreesToRadians(Roll);
        var rollRotation = Quaternion.FromAxisAngle(Front, rollRad);
        Up = Vector3.Transform(worldUp, rollRotation);
    }
}