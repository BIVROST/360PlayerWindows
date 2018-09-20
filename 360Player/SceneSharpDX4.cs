using System;
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
        private d3d11.Buffer vertices;
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

            //// Layout from VertexShader input signature
            int size = 0;
            Func<dxgi.Format, int> sizeUtil = fmt => {
                int s = size;
                switch(fmt)
                {
                    case dxgi.Format.R32G32B32A32_Float: size += sizeof(float) * 4; break;
                    case dxgi.Format.R32G32B32_Float: size += sizeof(float) * 3; break;
                    case dxgi.Format.R32G32_Float: size += sizeof(float) * 2; break;
                    default: throw new ArgumentOutOfRangeException(nameof(fmt), "Unsupported DXGI Format (add to switch)");
                }
                return s;
            };

            using (var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode))
            {
                layout = new d3d11.InputLayout(
                    device,
                    signature,
                    new[]
                    {
                    new d3d11.InputElement("POSITION", 0, dxgi.Format.R32G32B32A32_Float,  sizeUtil(dxgi.Format.R32G32B32A32_Float), 0),
                    new d3d11.InputElement("COLOR",    0, dxgi.Format.R32G32B32A32_Float,  sizeUtil(dxgi.Format.R32G32B32A32_Float), 0),
                    new d3d11.InputElement("NORMAL",   0, dxgi.Format.R32G32B32_Float,     sizeUtil(dxgi.Format.R32G32B32_Float), 0),
                    new d3d11.InputElement("TEXCOORD", 0, dxgi.Format.R32G32_Float,        sizeUtil(dxgi.Format.R32G32_Float), 0)
                    }
                );
            }

            // Instantiate Vertex buffer from vertex data
            vertices = d3d11.Buffer.Create(device, d3d11.BindFlags.VertexBuffer, new[]
            {
                // position                    color                       normal       uv
                0.0f,   0.5f, 0.5f, 1.0f,      1.0f, 1.0f, 1.0f, 1.0f,     1,0,0,       0,0,
                0.5f,  -0.5f, 0.5f, 1.0f,      0.0f, 1.0f, 0.0f, 1.0f,     1,0,0,       0,0,
                -0.5f, -0.5f, 0.5f, 1.0f,      0.0f, 0.0f, 1.0f, 1.0f,     1,0,0,       0,0,
                                                                           
                -0.0f, -0.5f, 0.5f, 1.0f,      1.0f, 1.0f, 1.0f, 1.0f,     1,0,0,       0,0,
                -0.5f,  0.5f, 0.5f, 1.0f,      0.0f, 1.0f, 0.0f, 1.0f,     1,0,0,       0,0,
                0.5f,   0.5f, 0.5f, 1.0f,      0.0f, 0.0f, 1.0f, 1.0f,     1,0,0,       0,0
            });


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
            context.InputAssembler.SetVertexBuffers(0, new d3d11.VertexBufferBinding(vertices, size, 0));
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
            context.Draw(6, 0);
            //swapChain.Present(0, PresentFlags.None);
        }

        void IScene.Update(TimeSpan timeSpan)
        {
            float time = (float)timeSpan.TotalSeconds;

            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovRH(72f * (float)Math.PI / 180f, 16f / 9f, 0.0001f, 50.0f);
            //proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);

            var viewProj = Matrix.Multiply(view, proj);

            var worldViewProj = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f) * viewProj;
            worldViewProj.Transpose();
            context.UpdateSubresource(ref worldViewProj, constantBuffer);
        }
    }
}