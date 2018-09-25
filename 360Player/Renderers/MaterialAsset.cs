using System;
using SharpDX.D3DCompiler;
using d3d11 = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;

namespace Bivrost.Bivrost360Player
{
    internal class MaterialAsset : IDisposable
    {
        private d3d11.VertexShader vertexShader;
        private d3d11.PixelShader pixelShader;
        private d3d11.InputLayout layout;

        public MaterialAsset(d3d11.Device device, string shaderFile):this(device, System.IO.File.ReadAllBytes(shaderFile), shaderFile)
        { }


        public MaterialAsset(d3d11.Device device, byte[] shaderBytes, string shaderName = "unknown")  // TODO: additional shader flags?
        {
            // Compile Vertex and Pixel shaders
            using (var vertexShaderByteCode = ShaderBytecode.Compile(shaderBytes, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None, shaderName))
            using (var pixelShaderByteCode = ShaderBytecode.Compile(shaderBytes, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None, shaderName))
            using (var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode))
            {
                vertexShader = new d3d11.VertexShader(device, vertexShaderByteCode);
                pixelShader = new d3d11.PixelShader(device, pixelShaderByteCode);

                //var vsRefl = new ShaderReflection(vertexShaderByteCode);

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
                );
            }
        }


        public void Apply(d3d11.DeviceContext context)
        {
            context.InputAssembler.InputLayout = layout;
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
        }


        public void Dispose()
        {
            vertexShader.Dispose();
            pixelShader.Dispose();
            layout.Dispose();
        }
    }
}