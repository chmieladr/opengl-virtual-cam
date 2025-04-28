using opengl_virtual_cam.shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace opengl_virtual_cam.scene;

public class RubiksScene : BaseScene
{
    public override string Name => "Rubik's Cube";
    protected override ShaderManager ShaderManager { get; } = new(new SimplisticShaders());
    protected override float[] GenerateVertices() => GenerateRubiksCubeWireframe();
    protected override Vector3 Center => new(2.0f, 0.0f, 0.0f);
    public override void Render(ref Matrix4 view, ref Matrix4 projection)
    {
        GL.UseProgram(Shader);
        GL.UniformMatrix4(GL.GetUniformLocation(Shader, "uView"), false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(Shader, "uProjection"), false, ref projection);
        
        var model = Matrix4.Identity;
        GL.UniformMatrix4(GL.GetUniformLocation(Shader, "uModel"), false, ref model);
        
        GL.BindVertexArray(Vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, VertexCount);
    }

    private float[] GenerateRubiksCubeWireframe()
    {
        var vertices = new List<float>();
        var whiteColor = new Vector3(1.0f, 1.0f, 1.0f);
       
        const float cubeSize = 0.2f;
        const float spacing = 0.4f;

        // 3x3x3 grid of cube wireframes
        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                for (var z = 0; z < 3; z++)
                {
                    var center = new Vector3(
                        Center.X + (x - 1) * spacing,
                        Center.Y + (y - 1) * spacing,
                        Center.Z + (z - 1) * spacing
                        );
                    AddCubeWireframe(vertices, center, cubeSize, whiteColor);
                }
            }
        }

        return vertices.ToArray();
    }

    private static void AddCubeWireframe(List<float> vertices, Vector3 center, float size, Vector3 color)
    {
        // Half-size for vertex positions
        var h = size / 2.0f;

        // Vertex positions (8 corners of cube)
        Vector3[] corners =
        [
            center + new Vector3(-h, -h, -h), // 0: back bottom left
            center + new Vector3(-h, h, -h), // 1: back top left
            center + new Vector3(h, h, -h), // 2: back top right
            center + new Vector3(h, -h, -h), // 3: back bottom right
            center + new Vector3(-h, -h, h), // 4: front bottom left
            center + new Vector3(-h, h, h), // 5: front top left
            center + new Vector3(h, h, h), // 6: front top right
            center + new Vector3(h, -h, h) // 7: front bottom right
        ];

        // Edges as pairs of corners (12 edges)
        var edges = new[,]
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, // Back face
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, // Front face
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } // Connecting edges
        };

        // Adding vertices for each edge
        for (var edge = 0; edge < 12; edge++)
        {
            // First vertex of the edge
            var idx1 = edges[edge, 0];
            vertices.Add(corners[idx1].X);
            vertices.Add(corners[idx1].Y);
            vertices.Add(corners[idx1].Z);
            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);

            // Second vertex of the edge
            var idx2 = edges[edge, 1];
            vertices.Add(corners[idx2].X);
            vertices.Add(corners[idx2].Y);
            vertices.Add(corners[idx2].Z);
            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);
        }
    }
}