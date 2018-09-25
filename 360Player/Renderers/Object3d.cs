using System;
using SharpDX;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{
    internal class Object3d:IDisposable
    {
        protected MaterialAsset material;
        protected ModelAsset model;
        protected d3d11.Buffer constantBuffer;

        public Matrix World { set; get; }


        public Object3d(d3d11.Device device, ModelAsset model, MaterialAsset mat)
        {
            this.material = mat;
            this.model = model;
            constantBuffer = new d3d11.Buffer(
                device,
                Utilities.SizeOf<Matrix>(),
                d3d11.ResourceUsage.Default,
                d3d11.BindFlags.ConstantBuffer,
                d3d11.CpuAccessFlags.None,
                d3d11.ResourceOptionFlags.None,
                0
            );
        }


        public void Dispose()
        {
            constantBuffer.Dispose();
        }


        internal void Render(Matrix viewProj, d3d11.DeviceContext context)
        {
            var worldViewProj = World * viewProj;
            worldViewProj.Transpose();

            context.UpdateSubresource(ref worldViewProj, constantBuffer);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);

            material.Apply(context);
            model.Render(context);
        }
    }
}