using System.Runtime.CompilerServices;
using ImGuiNET;
using opengl_virtual_cam.shaders;
using OpenTK.Graphics.OpenGL4;

namespace opengl_virtual_cam.imgui
{
    public class ImGuiResourceManager : IDisposable
    {
        // OpenGL resource handles
        public int VertexArray { get; private set; }
        public int VertexBuffer { get; private set; }
        public int VertexBufferSize { get; private set; }
        public int IndexBuffer { get; private set; }
        public int IndexBufferSize { get; private set; }
        public int ShaderProgram { get; private set; }
        public int ShaderFontTextureLocation { get; private set; }
        public int ShaderProjectionMatrixLocation { get; private set; }
        private int FontTexture { get; set; }

        public void CreateDeviceResources()
        {
            VertexBufferSize = 10000;
            IndexBufferSize = 2000;

            // Create vertex array and buffers
            VertexArray = GL.GenVertexArray();
            GL.BindVertexArray(VertexArray);

            VertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, VertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            IndexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, IndexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            // Create font texture
            RecreateFontDeviceTexture();

            // Create shaders
            var shaderManager = new ShaderManager(new ImGuiShaders());
            ShaderProgram = shaderManager.CreateProgram();

            // Get shader uniform locations
            ShaderProjectionMatrixLocation = GL.GetUniformLocation(ShaderProgram, "uProjection");
            ShaderFontTextureLocation = GL.GetUniformLocation(ShaderProgram, "uTexture");

            // Set up vertex attributes for ImGui vertex format
            GL.BindVertexArray(VertexArray);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 8);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, Unsafe.SizeOf<ImDrawVert>(), 16);
        }

        private void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out _);

            if (FontTexture != 0)
            {
                GL.DeleteTexture(FontTexture);
            }

            FontTexture = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, FontTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, pixels);

            io.Fonts.SetTexID(FontTexture);
            io.Fonts.ClearTexData();
        }

        public void ResizeVertexBuffer(int newSize)
        {
            VertexBufferSize = newSize;
            GL.BufferData(BufferTarget.ArrayBuffer, VertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }

        public void ResizeIndexBuffer(int newSize)
        {
            IndexBufferSize = newSize;
            GL.BufferData(BufferTarget.ElementArrayBuffer, IndexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(VertexBuffer);
            GL.DeleteBuffer(IndexBuffer);
            GL.DeleteVertexArray(VertexArray);
            GL.DeleteTexture(FontTexture);
            GL.DeleteProgram(ShaderProgram);

            GC.SuppressFinalize(this);
        }
    }
}