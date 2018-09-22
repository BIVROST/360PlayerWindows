using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using d3d11 = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;

namespace Bivrost.Bivrost360Player
{
    internal class SceneSharpDX4 : IScene
    {
        private ISceneHost host;
        private CompilationResult vertexShaderByteCode;
        private d3d11.VertexShader vertexShader;
        private CompilationResult pixelShaderByteCode;
        private d3d11.PixelShader pixelShader;
        private d3d11.InputLayout layout;
        private int verticesCount;
        private d3d11.Buffer vertices;
        private d3d11.Buffer indices;
        private d3d11.Buffer constantBuffer;

        private d3d11.Device device => host.Device;

        private d3d11.DeviceContext context => device.ImmediateContext;


        void IScene.Attach(ISceneHost host)
        {
            this.host = host;


            // Compile Vertex and Pixel shaders
            vertexShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            vertexShader = new d3d11.VertexShader(device, vertexShaderByteCode);

            pixelShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            pixelShader = new d3d11.PixelShader(device, pixelShaderByteCode);

            //var vsRefl = new ShaderReflection(vertexShaderByteCode);
            //vsRefl.Description.

            using (var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode))
            {
                layout = new d3d11.InputLayout(
                    device,
                    signature,
                    new[]
                    {
                    new d3d11.InputElement("POSITION", 0, dxgi.Format.R32G32B32A32_Float,  d3d11.InputElement.AppendAligned, 0),
                    new d3d11.InputElement("COLOR",    0, dxgi.Format.R32G32B32A32_Float,  d3d11.InputElement.AppendAligned, 0),
                    new d3d11.InputElement("NORMAL",   0, dxgi.Format.R32G32B32_Float,     d3d11.InputElement.AppendAligned, 0),
                    new d3d11.InputElement("TEXCOORD", 0, dxgi.Format.R32G32_Float,        d3d11.InputElement.AppendAligned, 0)
                    }
                    // D3D11_APPEND_ALIGNED_ELEMENT 
                );
            }

            List<float> verticesList = new List<float>();
            List<int> indicesList = new List<int>();


            Assimp.Scene scene;
            using (var ctx = new Assimp.AssimpContext())
            {
                scene = ctx.ImportFile("office_chair.obj", Assimp.PostProcessSteps.MakeLeftHanded);

                indicesList.Clear();
                verticesList.Clear();
                var mesh = scene.Meshes[0];
                foreach (var face in mesh.Faces)
                {
                    indicesList.AddRange(face.Indices);
                }
                for(int i =0; i < mesh.VertexCount; i++)
                {
                    var vert = mesh.Vertices[i];
                    float x = vert.X;
                    float y = vert.Y;
                    float z = vert.Z;
                    float w = 1.0f;

                    float u = 0;
                    float v = 0;
                    if (mesh.HasTextureCoords(0))
                    {
                        u = mesh.TextureCoordinateChannels[0][i].X;
                        v = mesh.TextureCoordinateChannels[0][i].Y;
                    }

                    float r = 0.5f;
                    float g = 0.8f;
                    float b = 0.3f;
                    float a = 0.5f;
                    if(mesh.VertexColorChannelCount > 0)
                    {
                        var col = mesh.VertexColorChannels[0][i];
                        r = col.R;
                        g = col.G;
                        b = col.B;
                        a = col.A;
                    }

                    verticesList.AddRange(new float[] {  x,y,z,w,      r, g, b, a,    1, 0, 0,    u, v });
                }
            }


            // Instantiate Vertex buffer from vertex data
            vertices = d3d11.Buffer.Create(device, d3d11.BindFlags.VertexBuffer, verticesList.ToArray());
            verticesCount = verticesList.Count;
            indices = d3d11.Buffer.Create(device, d3d11.BindFlags.IndexBuffer, indicesList.ToArray());
               

            constantBuffer = new d3d11.Buffer(
                device, 
                Utilities.SizeOf<Matrix>(),
                d3d11.ResourceUsage.Default,
                d3d11.BindFlags.ConstantBuffer,
                d3d11.CpuAccessFlags.None, 
                d3d11.ResourceOptionFlags.None, 
                0
            );


            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.SetVertexBuffers(0, new d3d11.VertexBufferBinding(vertices, 13 * sizeof(float), 0));
            context.InputAssembler.SetIndexBuffer(indices, dxgi.Format.R32_UInt, 0);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.VertexShader.Set(vertexShader);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);

            //context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
            context.PixelShader.Set(pixelShader);
            //context.OutputMerger.SetTargets(renderView);
        }

        void IScene.Detach()
        {
            vertexShaderByteCode.Dispose();
            vertexShader.Dispose();
            pixelShaderByteCode.Dispose();
            pixelShader.Dispose();
            layout.Dispose();
            vertices.Dispose();
            constantBuffer.Dispose();
        }

        void IScene.Render()
        {
            //context.ClearRenderTargetView(host., Color.Black);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Draw(verticesCount, 0);
            //swapChain.Present(0, PresentFlags.None);
        }

        void IScene.Update(TimeSpan timeSpan)
        {
            float time = (float)timeSpan.TotalSeconds;

            var view = Matrix.LookAtRH(Vector3.Zero, -Vector3.ForwardRH, Vector3.UnitY);
            var proj = Matrix.PerspectiveFovRH(72f * (float)Math.PI / 180f, 16f / 9f, 0.0001f, 50.0f);
            //proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);

            var viewProj = Matrix.Multiply(view, proj);

            //var worldViewProj = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f) * viewProj;
            var worldViewProj = Matrix.RotationZ(time * 2) * Matrix.RotationX(time) * Matrix.Translation(Vector3.BackwardRH * 5)  * viewProj;
            worldViewProj.Transpose();
            context.UpdateSubresource(ref worldViewProj, constantBuffer);
        }
    }
}