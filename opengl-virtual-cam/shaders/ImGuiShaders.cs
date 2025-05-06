namespace opengl_virtual_cam.shaders;

public class ImGuiShaders : Shaders
{
    public ImGuiShaders()
    {
        VertexSource = $"{Version}\n" + """
                                        layout(location = 0) in vec2 aPosition;
                                        layout(location = 1) in vec2 aTexCoord;
                                        layout(location = 2) in vec4 aColor;

                                        uniform mat4 uProjection;
                                        out vec2 fTexCoord;
                                        out vec4 fColor;

                                        void main()
                                        {
                                            fTexCoord = aTexCoord;
                                            fColor = aColor;
                                            gl_Position = uProjection * vec4(aPosition, 0, 1);
                                        }
                                        """;

        FragmentSource = $"{Version}\n" + """
                                          in vec2 fTexCoord;
                                          in vec4 fColor;

                                          uniform sampler2D uTexture;
                                          out vec4 outColor;

                                          void main()
                                          {
                                              outColor = fColor * texture(uTexture, fTexCoord);
                                          }
                                          """;
    }
}