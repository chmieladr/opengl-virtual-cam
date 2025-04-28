using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace opengl_virtual_cam.scene;

public abstract class BaseScene : IScene
{
    protected int Vao;
    protected int Shader;
    protected int VertexCount;
    protected abstract Vector3 Center { get; }

    protected abstract ShaderManager ShaderManager { get; }
    public abstract string Name { get; }

    public int Initialize()
    {
        Shader = ShaderManager.CreateProgram();
        Vao = GL.GenVertexArray();
        GL.BindVertexArray(Vao);

        var vertices = GenerateVertices();
        VertexCount = vertices.Length / 6;

        var vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
            vertices, BufferUsageHint.StaticDraw);

        // Configure attributes
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);

        return Shader;
    }

    // Scene-specific implementation
    protected abstract float[] GenerateVertices();
    public abstract void Render(ref Matrix4 view, ref Matrix4 projection);

    public virtual void Dispose()
    {
        GL.DeleteVertexArray(Vao);
        GL.DeleteProgram(Shader);
    }
}