using System;
using System.Collections.Generic;
using System.Drawing;
using SharpDX;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{


    internal partial class SceneSharpDX4 : IScene, IContentUpdatableFromMediaEngine
    {
        // TODO: Asset Manager holding all assets and cleaning up after them

        ISceneHost host;
        MaterialAsset matSphere;
        MaterialAsset matChair;
        ModelAsset chairModel;
        Object3d chair1;
        Object3d chair2;
        ModelAsset sphereModel;
        Object3d sphere;
        TextureAsset chairTextureDiffuse;
        TextureAsset chairTextureNormal;
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

            chairTextureDiffuse = new TextureAsset(device, "Renderers/office_chair_d.png");
            chairTextureNormal = new TextureAsset(device, "Renderers/office_chair_n.png");

            equirectangularTexture = new TextureAsset(device);

            matSphere = new MaterialAsset(device, "Renderers/Sphere.fx");
            matChair = new MaterialAsset(device, "Renderers/Chair.fx");

            chairModel = new ModelAsset(device, "Renderers/office_chair.obj");
            sphereModel = new ModelAsset(device, "Renderers/sphere.obj");

            chair1 = new Object3d(device, sphereModel, matChair);
            chair2 = new Object3d(device, chairModel, matChair);
            sphere = new Object3d(device, sphereModel, matSphere);

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

        }

        void IScene.Detach()
        {
            MediaDecoder.Instance.OnContentChanged -= Instance_OnContentChanged;

            chairModel.Dispose();
            chair1.Dispose();
            chair2.Dispose();
            sphereModel.Dispose();
            sphere.Dispose();
            matSphere.Dispose();
            matChair.Dispose();

            sampler.Dispose();
        }

        void IScene.Render()
        {
            if (requestContent)
            {
                if (requestContentCallback(this))
                    requestContent = false;
            }

            context.PixelShader.SetShaderResource(0, equirectangularTexture.TextureView);
            context.PixelShader.SetSampler(0, sampler);
            sphere.Render(viewProj, context, totalSeconds);

            context.PixelShader.SetShaderResource(0, chairTextureDiffuse.TextureView);
            context.PixelShader.SetShaderResource(1, chairTextureNormal.TextureView);
            context.PixelShader.SetSampler(0, sampler);
            context.PixelShader.SetSampler(1, sampler);

            context.PixelShader.SetShaderResource(2, equirectangularTexture.TextureView);
            context.PixelShader.SetSampler(2, sampler);


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