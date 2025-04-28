using opengl_virtual_cam.shaders;
using OpenTK.Graphics.OpenGL4;

namespace opengl_virtual_cam;

public class ShaderManager(Shaders shaders)
{
    public int CreateProgram()
    {
        // Vertex Shader: Transforms 3D coordinates from model space to screen space using matrices
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, shaders.VertexSource);
        GL.CompileShader(vertexShader);
        CheckCompileErrors(vertexShader, "vertex");

        // Fragment Shader: Outputs the color of the pixel
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, shaders.FragmentSource);
        GL.CompileShader(fragmentShader);
        CheckCompileErrors(fragmentShader, "fragment");

        // Linking shaders to create a program
        var program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        CheckLinkErrors(program);

        // Cleanup
        GL.DetachShader(program, vertexShader);
        GL.DetachShader(program, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }

    private static void CheckCompileErrors(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
        if (code != 0) return;
        var infoLog = GL.GetShaderInfoLog(shader);
        Console.WriteLine($"ERROR::SHADER::{type.ToUpper()}::COMPILATION_FAILED\n{infoLog}");
    }

    private static void CheckLinkErrors(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
        if (code != 0) return;
        var infoLog = GL.GetProgramInfoLog(program);
        Console.WriteLine($"ERROR::PROGRAM::LINKING_FAILED\n{infoLog}");
    }
}