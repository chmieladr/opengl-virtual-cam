namespace opengl_virtual_cam.shaders;

public class SimplisticShaders : Shaders
{
    public SimplisticShaders()
    {
        VertexSource = $"{Version}\n" + """
                                            layout(location = 0) in vec3 aPosition;
                                            layout(location = 1) in vec3 aColor;
                                            out vec3 vColor;
                                            uniform mat4 uModel, uView, uProjection;
                                            void main() {
                                                vColor = aColor;
                                                gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
                                            }
                                        """;
        FragmentSource = $"{Version}\n" + """
                                              in vec3 vColor;
                                              out vec4 fragColor;
                                              void main() {
                                                  fragColor = vec4(vColor, 1.0);
                                              }
                                          """;
    }
}