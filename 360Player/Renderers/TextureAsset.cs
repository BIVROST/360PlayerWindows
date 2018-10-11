using System;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{
    internal class TextureAsset:Asset
    {
        readonly private string path;
        private d3d11.Device device;
        private d3d11.Texture2D ownedTexture;


        public d3d11.ShaderResourceView TextureView { get; protected set; }


        public TextureAsset(string path, string name = null):base("Texture", name ?? path)
        {
            this.path = path;
            ownedTexture = null;
        }


        public override void Load(d3d11.Device device)
        {
            this.device = device;

            if (path == null)
                return;

            TextureView = d3d11.ShaderResourceView.FromFile(device, path, d3d11.ImageLoadInformation.Default);
        }

        internal void Apply(d3d11.DeviceContext context, int idx)
        {
            if(TextureView != null)
                context.PixelShader.SetShaderResource(idx, TextureView);
        }

        public void ReplaceTextureNonOwning(d3d11.Texture2D texture, string newName = null)
        {
            Unload();
            ownedTexture = null;
            TextureView = new d3d11.ShaderResourceView(device, texture);
        }


        public void ReplaceTextureOwning(d3d11.Texture2D texture, string newName = null)
        {
            Unload();
            ownedTexture = texture;
            TextureView = new d3d11.ShaderResourceView(device, texture);
        }


        protected override void Unload()
        {
            ownedTexture?.Dispose();
            TextureView?.Dispose();
            ownedTexture = null;
        }


    }
}
