using opengl_virtual_cam.shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace opengl_virtual_cam.scene;

public class CuboidsScene : BaseScene
{
    public override string Name => "Cuboids";
    protected override ShaderManager ShaderManager { get; } = new(new SimplisticShaders());

    protected override float[] GenerateVertices() => GenerateCuboids();
    protected override Vector3 Center => new(4.0f, 0.0f, 0.0f);
    
    public override void Render(ref Matrix4 view, ref Matrix4 projection)
    {
        GL.UseProgram(Shader);
        GL.UniformMatrix4(GL.GetUniformLocation(Shader, "uView"), false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(Shader, "uProjection"), false, ref projection);

        Vector3[] positions =
        [
            new(Center.X - 1.0f, Center.Y + 0.0f, Center.Z - 1.0f),
            new(Center.X + 1.0f, Center.Y + 0.0f, Center.Z - 1.0f),
            new(Center.X - 1.0f, Center.Y + 0.0f, Center.Z + 1.0f),
            new(Center.X + 1.0f, Center.Y + 0.0f, Center.Z + 1.0f)
        ];

        // Each cuboid with its own model matrix
        for (var i = 0; i < 4; i++)
        {
            var model = Matrix4.CreateTranslation(positions[i]);
            GL.UniformMatrix4(GL.GetUniformLocation(Shader, "uModel"), false, ref model);

            GL.BindVertexArray(Vao);
            GL.DrawArrays(PrimitiveType.Lines, i * 24, 24);
        }
    }

    private static float[] GenerateCuboids()
    {
        var vertices = new List<float>();
        float[] heights = [1.0f, 2.0f, 1.5f, 0.75f];

        // Colors in RGB format
        Vector3[] colors =
        [
            new(1.0f, 0.2f, 0.2f), // Red
            new(0.2f, 1.0f, 0.2f), // Green
            new(0.2f, 0.2f, 1.0f), // Blue
            new(1.0f, 0.7f, 0.2f) // Yellow
        ];

        for (var i = 0; i < 4; i++)
        {
            var h = heights[i];
            var color = colors[i];

            const float width = 0.8f;
            const float depth = 0.8f;

            // Half-dimensions for vertices
            const float hw = width / 2.0f;
            var hh = h / 2.0f;
            const float hd = depth / 2.0f;

            // Corners of the cuboid (8 vertices)
            Vector3[] corners =
            [
                new(-hw, -hh, -hd), // 0: back bottom left
                new(-hw, hh, -hd), // 1: back top left
                new(hw, hh, -hd), // 2: back top right
                new(hw, -hh, -hd), // 3: back bottom right
                new(-hw, -hh, hd), // 4: front bottom left
                new(-hw, hh, hd), // 5: front top left
                new(hw, hh, hd), // 6: front top right
                new(hw, -hh, hd) // 7: front bottom right
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

        return vertices.ToArray();
    }
}