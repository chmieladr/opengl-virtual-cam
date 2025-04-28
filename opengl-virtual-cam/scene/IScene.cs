using OpenTK.Mathematics;

namespace opengl_virtual_cam.scene;

public interface IScene
{
    int Initialize();
    
    void Render(ref Matrix4 view, ref Matrix4 projection);
    
    void Dispose();

    string Name { get; }
}