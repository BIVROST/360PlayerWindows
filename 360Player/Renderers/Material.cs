using System;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{
    internal class Material
    {

        public ShaderAsset shader;
        public Tuple<TextureAsset, d3d11.SamplerState>[] texturesWithSamplers;


        public Material(ShaderAsset shader, params Tuple<TextureAsset, d3d11.SamplerState>[] texturesWithSamplers)
        {
            this.shader = shader;
            this.texturesWithSamplers = texturesWithSamplers;
        }


        public void Apply(d3d11.DeviceContext context)
        {
            shader.Apply(context);
            for (int idx = 0; idx < texturesWithSamplers.Length; idx++)
            {
                context.PixelShader.SetShaderResource(idx, texturesWithSamplers[idx].Item1.TextureView);
                context.PixelShader.SetSampler(idx, texturesWithSamplers[idx].Item2);
            }
        }


    }

}