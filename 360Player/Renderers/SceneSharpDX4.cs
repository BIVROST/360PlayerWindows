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

        private ISceneHost host;
        private MaterialAsset mat;
        ModelAsset chairModel;
        Object3d chair1;
        Object3d chair2;
        ModelAsset sphereModel;
        Object3d sphere;
        TextureAsset chairTextureDiffuse;
        TextureAsset chairTextureNormal;
        private Matrix viewProj;

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

            mat = new MaterialAsset(device, "Renderers/MiniTri.fx");

            chairModel = new ModelAsset(device, "Renderers/office_chair.obj");
            sphereModel = new ModelAsset(device, "Renderers/sphere.obj");

            chair1 = new Object3d(device, chairModel, mat);
            chair2 = new Object3d(device, chairModel, mat);
            sphere = new Object3d(device, sphereModel, mat);

            chairTextureDiffuse = new TextureAsset(device, "Renderers/office_chair_d.png");
            chairTextureNormal = new TextureAsset(device, "Renderers/office_chair_n.png");
        }

        void IScene.Detach()
        {
            MediaDecoder.Instance.OnContentChanged -= Instance_OnContentChanged;

            chairModel.Dispose();
            chair1.Dispose();
            chair2.Dispose();
            sphereModel.Dispose();
            sphere.Dispose();
            mat.Dispose();
        }

        void IScene.Render()
        {
            if (requestContent)
            {
                if (requestContentCallback(this))
                    requestContent = false;
            }

            context.PixelShader.SetShaderResource(0, textureView);
            context.PixelShader.SetSampler(0, sampler);
            sphere.Render(viewProj, context);

            context.PixelShader.SetShaderResource(0, chairTextureDiffuse.TextureView);
            context.PixelShader.SetShaderResource(1, chairTextureNormal.TextureView);
            context.PixelShader.SetSampler(0, sampler);
            context.PixelShader.SetSampler(1, sampler);
            chair1.Render(viewProj, context);
            chair2.Render(viewProj, context);

        }

        void IScene.Update(TimeSpan timeSpan)
        {
            float time = (float)timeSpan.TotalSeconds;

            var view = Matrix.LookAtRH(Vector3.Zero, Vector3.ForwardRH, Vector3.UnitY);
            var proj = Matrix.PerspectiveFovRH(72f * (float)Math.PI / 180f, 16f / 9f, 0.01f, 100.0f);
            viewProj = Matrix.Multiply(view, proj);
            chair1.World = Matrix.RotationZ(time * 2) * Matrix.RotationX(time) * Matrix.Translation(Vector3.ForwardRH * 5) * Matrix.Translation(Vector3.Left * 2);
            chair2.World = Matrix.RotationY(time /2 ) * Matrix.Translation(Vector3.ForwardRH * 5) * Matrix.Translation(Vector3.Right * 2);
            sphere.World = Matrix.RotationY(time / 5) /** Matrix.RotationX(time / 3) */ * Matrix.Scaling(50);
        }

        #region content updates

        bool requestContent = true;

        protected Func<IContentUpdatableFromMediaEngine, bool> requestContentCallback;
        private d3d11.SamplerState sampler;
        private d3d11.ShaderResourceView textureView;

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

        d3d11.Texture2D mainTexture;
        void IContentUpdatableFromMediaEngine.ReceiveTextures(d3d11.Texture2D textureL, d3d11.Texture2D textureR)
        {
            mainTexture?.Dispose();
            using (var resource = textureL.QueryInterface<SharpDX.DXGI.Resource>())
            {
                mainTexture = device.OpenSharedResource<d3d11.Texture2D>(resource.SharedHandle);
            };

            textureView?.Dispose();
            textureView = new d3d11.ShaderResourceView(device, mainTexture);

            sampler?.Dispose();
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

        void IContentUpdatableFromMediaEngine.SetProjection(ProjectionMode projection)
        {
            ;
        }
        #endregion
    }
}