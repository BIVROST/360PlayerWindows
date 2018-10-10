using System;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{


    internal partial class SceneSharpDX4
    {
        internal class TextureAsset:Asset
        {
            private string path;
            private d3d11.Device device;
            private d3d11.Texture2D ownedTexture;

            public override string AssetType => "Texture";


            public d3d11.ShaderResourceView TextureView { get; protected set; }


            public TextureAsset(d3d11.Device device, string path):base(path)
            {
                this.device = device;
                TextureView = d3d11.ShaderResourceView.FromFile(device, path, d3d11.ImageLoadInformation.Default);
                ownedTexture = null;
            }


            public TextureAsset(d3d11.Device device) : base(null)
            {
                this.device = device;
                TextureView = null;
                ownedTexture = null;
            }


            public void ReplaceTextureNonOwning(d3d11.Texture2D texture, string newName = null)
            {
                Dispose();
                ownedTexture = null;
                TextureView = new d3d11.ShaderResourceView(device, texture);
            }


            public void ReplaceTextureOwning(d3d11.Texture2D texture, string newName = null)
            {
                Dispose();
                ownedTexture = texture;
                TextureView = new d3d11.ShaderResourceView(device, texture);
            }


            public override void Dispose()
            {
                ownedTexture?.Dispose();
                TextureView?.Dispose();
                ownedTexture = null;
            }

        }
        

    }
}