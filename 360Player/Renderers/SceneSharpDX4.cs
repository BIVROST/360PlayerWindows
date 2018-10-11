using System;
using System.Collections.Generic;
using System.Drawing;
using SharpDX;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{

    internal partial class SceneSharpDX4 : IScene, IContentUpdatableFromMediaEngine
    {
        ISceneHost host;
        Object3d chair1;
        Object3d chair2;
        Object3d sphere;
        AssetManager assetManager;
        Matrix viewProj;
        TextureAsset equirectangularTexture;
        d3d11.SamplerState sampler;

        public SceneSharpDX4(Func<IContentUpdatableFromMediaEngine, bool> contentRequested)
        {
            requestContentCallback = contentRequested;
        }

        private d3d11.Device device => host.Device;

        private d3d11.DeviceContext context => device.ImmediateContext;


        void IScene.Attach(ISceneHost host)
        {
            this.host = host;
            MediaDecoder.Instance.OnContentChanged += Instance_OnContentChanged;

            assetManager = new AssetManager(device);

            equirectangularTexture = assetManager.EmptyTexture("equirectangular media texture");

            sampler = new d3d11.SamplerState(device, new d3d11.SamplerStateDescription()
            {
                Filter = d3d11.Filter.MinMagMipLinear,
                AddressU = d3d11.TextureAddressMode.Clamp,
                AddressV = d3d11.TextureAddressMode.Clamp,
                AddressW = d3d11.TextureAddressMode.Clamp,
                BorderColor = SharpDX.Color.Black,
                ComparisonFunction = d3d11.Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0,
                MinimumLod = -float.MaxValue,
                MaximumLod = float.MaxValue
            });
            assetManager.RegisterIDisposable(sampler);

            chair1 = new Object3d(
                device,
                assetManager.Mesh("Renderers/sphere.obj"),
                new Material(
                    assetManager.Shader("Renderers/chair.fx"),
                    Tuple.Create(assetManager.Texture("Renderers/office_chair_d.png"), sampler),
                    Tuple.Create(assetManager.Texture("Renderers/office_chair_n.png"), sampler)
                )
            );

            chair2 = new Object3d(
                device,
                assetManager.Mesh("Renderers/office_chair.obj"),
                new Material(
                    assetManager.Shader("Renderers/chair.fx"),
                    Tuple.Create(assetManager.Texture("Renderers/office_chair_d.png"), sampler),
                    Tuple.Create(assetManager.Texture("Renderers/office_chair_n.png"), sampler)
                )
            );

            sphere = new Object3d(
                device,
                assetManager.Mesh("Renderers/sphere.obj"),
                new Material(
                    assetManager.Shader("Renderers/sphere.fx"),
                    Tuple.Create(equirectangularTexture, sampler)
                )
            );

        }

        void IScene.Detach()
        {
            MediaDecoder.Instance.OnContentChanged -= Instance_OnContentChanged;

            chair1.Dispose();
            chair2.Dispose();
            sphere.Dispose();
            assetManager.Dispose();
        }

        void IScene.Render()
        {
            if (requestContent)
            {
                if (requestContentCallback(this))
                    requestContent = false;
            }

            sphere.Render(viewProj, context, totalSeconds);
            chair1.Render(viewProj, context, totalSeconds);
            chair2.Render(viewProj, context, totalSeconds);
        }


        void IScene.Update(TimeSpan timeSpan)
        {
            totalSeconds = (float)timeSpan.TotalSeconds;

            var view = Matrix.LookAtRH(Vector3.Zero, Vector3.ForwardRH, Vector3.UnitY);
            var proj = Matrix.PerspectiveFovRH(72f * (float)Math.PI / 180f, 16f / 9f, 0.01f, 100.0f);
            viewProj = Matrix.Multiply(view, proj);
            chair1.World = Matrix.RotationZ(totalSeconds * 2) * Matrix.RotationX(totalSeconds) * Matrix.Translation(Vector3.ForwardRH * 5) * Matrix.Translation(Vector3.Left * 4);
            chair2.World = Matrix.RotationY(totalSeconds / 2) * Matrix.Translation(Vector3.ForwardRH * 3) * Matrix.Translation(Vector3.Right * 0) * Matrix.Translation(Vector3.Down *2f);
            sphere.World = Matrix.RotationY(totalSeconds / 5) /** Matrix.RotationX(time / 3) */ * Matrix.Scaling(50);
        }

        #region content updates

        bool requestContent = true;

        protected Func<IContentUpdatableFromMediaEngine, bool> requestContentCallback;
        //private d3d11.ShaderResourceView textureView;

        protected void Instance_OnContentChanged()
        {
            requestContent = true;
        }

        void IContentUpdatableFromMediaEngine.ClearContent()
        {
            throw new NotImplementedException();
        }

        void IContentUpdatableFromMediaEngine.ReceiveBitmap(Bitmap bitmap, MediaDecoder.ClipCoords coordsL, MediaDecoder.ClipCoords coordsR)
        {
            throw new NotImplementedException();
        }

        //d3d11.Texture2D mainTexture;
        float totalSeconds;

        void IContentUpdatableFromMediaEngine.ReceiveTextures(d3d11.Texture2D textureL, d3d11.Texture2D textureR)
        {
            using (var resource = textureL.QueryInterface<SharpDX.DXGI.Resource>())
            {
                equirectangularTexture.ReplaceTextureOwning(device.OpenSharedResource<d3d11.Texture2D>(resource.SharedHandle));
            };
        }

        void IContentUpdatableFromMediaEngine.SetProjection(ProjectionMode projection)
        {
            ;
        }
        #endregion
    }
}