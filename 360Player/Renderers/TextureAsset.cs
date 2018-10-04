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

            public override string AssetType => "Texture";
            public d3d11.Texture2D Texture { get; protected set; }
            public d3d11.ShaderResourceView TextureView { get; protected set; }


            public TextureAsset(d3d11.Device device, string path):base(path)
            {
                using (var image = Image.FromFile(path))
                using (var bitmap = new Bitmap(image))
                {
                    Bitmap bitmapArgb32;
                    if (image.PixelFormat != PixelFormat.Format32bppArgb)
                    {
                        bitmapArgb32 = bitmap.Clone(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format32bppArgb);
                    }
                    else 
                    {
                        bitmapArgb32 = bitmap;
                    }

                    var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Texture = new d3d11.Texture2D(device, new d3d11.Texture2DDescription()
                    {
                        Width = bitmap.Width,
                        Height = bitmap.Height,
                        ArraySize = 1,
                        BindFlags = d3d11.BindFlags.ShaderResource,
                        Usage = d3d11.ResourceUsage.Immutable,
                        CpuAccessFlags = d3d11.CpuAccessFlags.None,
                        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        MipLevels = 1,
                        OptionFlags = d3d11.ResourceOptionFlags.None,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    }, new DataRectangle(data.Scan0, data.Stride));
                    bitmap.UnlockBits(data);

                    if(bitmap != bitmapArgb32)
                    {
                        bitmapArgb32.Dispose();
                    }
                }

                TextureView = new d3d11.ShaderResourceView(device, Texture);
            }


            public override void Dispose()
            {
                Texture.Dispose();
                TextureView.Dispose();
            }

        }
    }
}