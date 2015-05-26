namespace PlayerUI
{
    using System;
    using SharpDX;
    //using SharpDX.D3DCompiler;
    using SharpDX.Direct3D11;
    using SharpDX.DXGI;
    using Buffer = SharpDX.Direct3D11.Buffer;
    using Device = SharpDX.Direct3D11.Device;

    public class Scene : IScene
    {
        private ISceneHost Host;
        private InputLayout VertexLayout;
        private DataStream VertexStream;
        private Buffer Vertices;
        //private Effect SimpleEffect;
        private Color4 OverlayColor = new Color4(1.0f);

		private Device _device;

		private Texture2D videoTexture;
		private Surface videoSurface;
		private bool isSurfaceCreated = false;

		SharpDX.Toolkit.Graphics.GraphicsDevice graphicsDevice;
		SharpDX.Toolkit.Graphics.BasicEffect basicEffect;
		SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;

		KeyedMutex mutex;

        void IScene.Attach(ISceneHost host)
        {
            this.Host = host;

            _device = host.Device;

            if (_device == null)
                throw new Exception("Scene host device is null");


			graphicsDevice = SharpDX.Toolkit.Graphics.GraphicsDevice.New(_device);
			basicEffect = new SharpDX.Toolkit.Graphics.BasicEffect(graphicsDevice);
			
			basicEffect.Projection = Matrix.PerspectiveFovRH((float)(72f * Math.PI / 180f), (float)16f/9f, 0.001f, 100.0f);
			//basicEffect.Projection = Matrix.Identity;
			basicEffect.World = Matrix.Identity;

			var tempTex = new Texture2D(videoTexture.NativePointer);
			basicEffect.PreferPerPixelLighting = false;

			var resource = videoTexture.QueryInterface<SharpDX.DXGI.Resource>();
			var sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle);

			mutex = new KeyedMutex(sharedTex.NativePointer);


			basicEffect.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(graphicsDevice, sharedTex);
			//basicEffect.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(graphicsDevice, videoTexture);
			
			basicEffect.TextureEnabled = true;
			basicEffect.LightingEnabled = false;

			primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Sphere.New(graphicsDevice, 6, 64, true);
			//primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(graphicsDevice,2f,1f);


            //ShaderBytecode shaderBytes = ShaderBytecode.CompileFromFile("Simple.fx", "fx_4_0", ShaderFlags.None, EffectFlags.None, null, null);
            //this.SimpleEffect = new Effect(device, shaderBytes);

            //EffectTechnique technique = this.SimpleEffect.GetTechniqueByIndex(0); ;
            //EffectPass pass = technique.GetPassByIndex(0);

			//this.VertexLayout = new InputLayout(device, pass.Description.Signature, new[] {
			//	new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
			//	new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0) 
			//});

			//this.VertexStream = new DataStream(3 * 32, true, true);
			//this.VertexStream.WriteRange(new[] {
			//	new Vector4(0.0f, 0.5f, 0.5f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
			//	new Vector4(0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
			//	new Vector4(-0.5f, -0.5f, 0.5f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
			//});
			//this.VertexStream.Position = 0;

			//this.Vertices = new Buffer(device, this.VertexStream, new BufferDescription()
			//	{
			//		BindFlags = BindFlags.VertexBuffer,
			//		CpuAccessFlags = CpuAccessFlags.None,
			//		OptionFlags = ResourceOptionFlags.None,
			//		SizeInBytes = 3 * 32,
			//		Usage = ResourceUsage.Default
			//	}
			//);

			_device.ImmediateContext.Flush();

			BivrostPlayerPrototype.PlayerPrototype.LookChanged += (look) =>
			{
				basicEffect.View = look;
			};


        }

		private bool textureWaiting = false;
		public void SetVideoTexture(Texture2D sharedTexture)
		{
			this.videoTexture = sharedTexture;
		}


        void IScene.Detach()
        {
            Disposer.RemoveAndDispose(ref this.Vertices);
            Disposer.RemoveAndDispose(ref this.VertexLayout);
            //Disposer.RemoveAndDispose(ref this.SimpleEffect);
            Disposer.RemoveAndDispose(ref this.VertexStream);
        }

        void IScene.Update(TimeSpan sceneTime)
        {
            float t = (float) sceneTime.Milliseconds * 0.001f;
            this.OverlayColor.Alpha = t;
        }

        void IScene.Render()
        {
            Device device = this.Host.Device;
            if (device == null)
                return;

			primitive.Draw(basicEffect);
			
			//device.ImmediateContext.InputAssembler.InputLayout = this.VertexLayout;
			//device.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
			//device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(this.Vertices, 32, 0));

            //EffectTechnique technique = this.SimpleEffect.GetTechniqueByIndex(0);
            //EffectPass pass = technique.GetPassByIndex(0);

            //EffectVectorVariable overlayColor = this.SimpleEffect.GetVariableBySemantic("OverlayColor").AsVector();

			//overlayColor.Set(this.OverlayColor);

			//for (int i = 0; i < technique.Description.PassCount; ++i)
			//{
			//	pass.Apply();
			//	device.ImmediateContext.Draw(3, 0);
			//}
        }
    }
}
