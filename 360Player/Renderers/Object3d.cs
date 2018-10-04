using System;
using System.Runtime.InteropServices;
using SharpDX;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{
    internal class Object3d:IDisposable
    {
        protected MaterialAsset material;
        protected ModelAsset model;
        protected d3d11.Buffer constantBuffer;


        // must be multiple of 16 bytes
        struct Constant
        {
            public Matrix worldViewProj;
            public Matrix world;
            public float time;
            public float _pad1;
            public float _pad2;
            public float _pad3;
        }
        Constant constants;


        public Matrix World
        {
            set => constants.world = value;
            get => constants.world;
        }


        public Object3d(d3d11.Device device, ModelAsset model, MaterialAsset mat)
        {
            this.material = mat;
            this.model = model;
            if (Marshal.SizeOf(constants) % 16 != 0)
                throw new ArgumentException("Constant buffer's size must be multiple of 16 bytes, is " + Marshal.SizeOf(constants));
            constantBuffer = new d3d11.Buffer(
                device,
                Marshal.SizeOf(constants),
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


        internal void Render(Matrix viewProj, d3d11.DeviceContext context, float time)
        {
            constants.worldViewProj = World * viewProj;
            constants.worldViewProj.Transpose();
            constants.time = time;

            context.UpdateSubresource(ref constants, constantBuffer);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);
            context.PixelShader.SetConstantBuffer(0, constantBuffer);

            material.Apply(context);
            model.Render(context);
        }
    }
}