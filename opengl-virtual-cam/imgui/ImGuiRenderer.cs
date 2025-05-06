using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;

// ReSharper disable InconsistentNaming

namespace opengl_virtual_cam.imgui
{
    public class ImGuiRenderer(ImGuiResourceManager resourceManager)
    {
        public void RenderImDrawData(ImDrawDataPtr drawData)
        {
            if (drawData.CmdListsCount == 0) return;

            // Save GL state
            var state = SaveGLState();

            // Setup ImGui rendering state
            SetupImGuiRenderState();

            // Configure viewport and projection
            var orthoProj = CreateOrthographicProjection(drawData);
            GL.UseProgram(resourceManager.ShaderProgram);
            GL.Uniform1(resourceManager.ShaderFontTextureLocation, 0);
            GL.UniformMatrix4(resourceManager.ShaderProjectionMatrixLocation, 1, false, ref orthoProj.M11);
            GL.BindVertexArray(resourceManager.VertexArray);

            // Process command lists
            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];
                UpdateBuffers(cmdList);
                RenderCommandList(cmdList, drawData);
            }

            // Restore original GL state
            RestoreGLState(state);
        }

        private void UpdateBuffers(ImDrawListPtr cmdList)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, resourceManager.VertexBuffer);
            var vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > resourceManager.VertexBufferSize)
            {
                resourceManager.ResizeVertexBuffer(Math.Max(resourceManager.VertexBufferSize * 2, vertexSize));
            }

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero,
                cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, resourceManager.IndexBuffer);
            var indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > resourceManager.IndexBufferSize)
            {
                resourceManager.ResizeIndexBuffer(Math.Max(resourceManager.IndexBufferSize * 2, indexSize));
            }

            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero,
                cmdList.IdxBuffer.Size * sizeof(ushort), cmdList.IdxBuffer.Data);
        }

        private static void RenderCommandList(ImDrawListPtr cmdList, ImDrawDataPtr drawData)
        {
            var idxOffset = 0;
            for (var cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                var cmd = cmdList.CmdBuffer[cmdIndex];

                // Apply scissor rectangle
                var clip = cmd.ClipRect;
                GL.Scissor(
                    (int)clip.X,
                    (int)(drawData.DisplaySize.Y - clip.W),
                    (int)(clip.Z - clip.X),
                    (int)(clip.W - clip.Y)
                );

                // Bind texture and draw
                GL.BindTexture(TextureTarget.Texture2D, (int)cmd.TextureId);
                GL.DrawElementsBaseVertex(
                    PrimitiveType.Triangles,
                    (int)cmd.ElemCount,
                    DrawElementsType.UnsignedShort,
                    idxOffset * sizeof(ushort),
                    0
                );

                idxOffset += (int)cmd.ElemCount;
            }
        }

        private static GLState SaveGLState()
        {
            var state = new GLState();
            GL.GetInteger(GetPName.ActiveTexture, out state.LastActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);

            GL.GetInteger(GetPName.CurrentProgram, out state.LastProgram);
            GL.GetInteger(GetPName.TextureBinding2D, out state.LastTexture);
            GL.GetInteger(GetPName.SamplerBinding, out state.LastSampler);

            GL.GetInteger(GetPName.ArrayBufferBinding, out state.LastArrayBuffer);
            GL.GetInteger(GetPName.VertexArrayBinding, out state.LastVertexArray);

            GL.GetInteger(GetPName.BlendSrc, out state.LastBlendSrcRgb);
            GL.GetInteger(GetPName.BlendDst, out state.LastBlendDstRgb);

            GL.GetInteger(GetPName.BlendSrcAlpha, out state.LastBlendSrcAlpha);
            GL.GetInteger(GetPName.BlendDstAlpha, out state.LastBlendDstAlpha);

            GL.GetInteger(GetPName.BlendEquationRgb, out state.LastBlendEquationRgb);
            GL.GetInteger(GetPName.BlendEquationAlpha, out state.LastBlendEquationAlpha);

            state.LastEnableBlend = GL.IsEnabled(EnableCap.Blend);
            state.LastEnableCullFace = GL.IsEnabled(EnableCap.CullFace);
            state.LastEnableDepthTest = GL.IsEnabled(EnableCap.DepthTest);
            state.LastEnableScissorTest = GL.IsEnabled(EnableCap.ScissorTest);

            return state;
        }

        private static void SetupImGuiRenderState()
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ScissorTest);
        }

        private static System.Numerics.Matrix4x4 CreateOrthographicProjection(ImDrawDataPtr drawData)
        {
            GL.Viewport(0, 0, (int)drawData.DisplaySize.X, (int)drawData.DisplaySize.Y);
            var L = drawData.DisplayPos.X;
            var R = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var T = drawData.DisplayPos.Y;
            var B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

            return new System.Numerics.Matrix4x4(
                2.0f / (R - L), 0.0f, 0.0f, 0.0f,
                0.0f, 2.0f / (T - B), 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f,
                (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f
            );
        }

        private static void RestoreGLState(GLState state)
        {
            GL.UseProgram(state.LastProgram);
            GL.BindTexture(TextureTarget.Texture2D, state.LastTexture);
            GL.BindSampler(0, state.LastSampler);
            GL.ActiveTexture((TextureUnit)state.LastActiveTexture);
            GL.BindVertexArray(state.LastVertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, state.LastArrayBuffer);

            GL.BlendEquationSeparate(
                (BlendEquationMode)state.LastBlendEquationRgb,
                (BlendEquationMode)state.LastBlendEquationAlpha
            );
            GL.BlendFuncSeparate(
                (BlendingFactorSrc)state.LastBlendSrcRgb,
                (BlendingFactorDest)state.LastBlendDstRgb,
                (BlendingFactorSrc)state.LastBlendSrcAlpha,
                (BlendingFactorDest)state.LastBlendDstAlpha
            );

            SetGLCapability(EnableCap.Blend, state.LastEnableBlend);
            SetGLCapability(EnableCap.CullFace, state.LastEnableCullFace);
            SetGLCapability(EnableCap.DepthTest, state.LastEnableDepthTest);
            SetGLCapability(EnableCap.ScissorTest, state.LastEnableScissorTest);
        }

        private static void SetGLCapability(EnableCap cap, bool enabled)
        {
            if (enabled)
                GL.Enable(cap);
            else
                GL.Disable(cap);
        }
    }
}