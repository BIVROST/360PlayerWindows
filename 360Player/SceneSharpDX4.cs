using System;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;

namespace Bivrost.Bivrost360Player
{
    internal class SceneSharpDX4 : IScene
    {
        private ISceneHost host;
        private CompilationResult vertexShaderByteCode;
        private VertexShader vertexShader;
        private CompilationResult pixelShaderByteCode;
        private PixelShader pixelShader;
        private InputLayout layout;
        private SharpDX.Direct3D11.Buffer vertices;

        private Device device => host.Device;

        private DeviceContext context => device.ImmediateContext;


        void IScene.Attach(ISceneHost host)
        {
            this.host = host;


            // Compile Vertex and Pixel shaders
            vertexShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            vertexShader = new VertexShader(device, vertexShaderByteCode);

            pixelShaderByteCode = ShaderBytecode.CompileFromFile("MiniTri.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            // Layout from VertexShader input signature
            layout = new InputLayout(
                device,
                ShaderSignature.GetInputSignature(vertexShaderByteCode),
                new[]
                    {
                        new InputElement("POSITION", 0, dxgi.Format.R32G32B32A32_Float, 0, 0),
                        new InputElement("COLOR", 0, dxgi.Format.R32G32B32A32_Float, 16, 0)
                    });

            // Instantiate Vertex buiffer from vertex data
            vertices = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, new[]
                                  {
                                      new Vector4(0.0f, 0.5f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                                      new Vector4(0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                                      new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
                                  });

            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, 32, 0));
            context.VertexShader.Set(vertexShader);
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
        }

        void IScene.Render()
        {
            //context.ClearRenderTargetView(host., Color.Black);
            context.Draw(3, 0);
            //swapChain.Present(0, PresentFlags.None);
        }

        void IScene.Update(TimeSpan timeSpan)
        {
        }
    }
}