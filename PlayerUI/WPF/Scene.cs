namespace PlayerUI
{
	using System;
	using SharpDX;
	//using SharpDX.D3DCompiler;
	using SharpDX.Direct3D11;
	using SharpDX.DXGI;
	using Buffer = SharpDX.Direct3D11.Buffer;
	using Device = SharpDX.Direct3D11.Device;
	using System.Windows.Input;

	public class Scene : IScene
    {
        private ISceneHost Host;
		private Device _device;

		private Texture2D videoTexture;

		SharpDX.Toolkit.Graphics.GraphicsDevice graphicsDevice;
		SharpDX.Toolkit.Graphics.BasicEffect basicEffect;
		SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;

		private float yaw = 0;
		private float pitch = 0;
		private bool remoteRotationOverride = false;
		private Matrix remoteRotation;

		private float deltaTime = 0;
		private float lastFrameTime = 0;
		public bool HasFocus = true;
		private Quaternion targetRotationQuaternion;
		private Quaternion currentRotationQuaternion;
		private float lerpSpeed = 3f;

		private Texture2D sharedTex;
		private SharpDX.DXGI.Resource resource;

		public Scene(Texture2D sharedTexture)
		{
			videoTexture = sharedTexture;
		}

        void IScene.Attach(ISceneHost host)
        {
            this.Host = host;

            _device = host.Device;

            if (_device == null)
                throw new Exception("Scene host device is null");


			graphicsDevice = SharpDX.Toolkit.Graphics.GraphicsDevice.New(_device);
			basicEffect = new SharpDX.Toolkit.Graphics.BasicEffect(graphicsDevice);
			
			basicEffect.Projection = Matrix.PerspectiveFovRH((float)(72f * Math.PI / 180f), (float)16f/9f, 0.0001f, 50.0f);
			basicEffect.World = Matrix.Identity;

			basicEffect.PreferPerPixelLighting = false;

			resource = videoTexture.QueryInterface<SharpDX.DXGI.Resource>();
			sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle);


			basicEffect.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(graphicsDevice, sharedTex);


			basicEffect.TextureEnabled = true;
			basicEffect.LightingEnabled = false;

			primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Sphere.New(graphicsDevice, 6, 64, true);

			_device.ImmediateContext.Flush();

			//BivrostPlayerPrototype.PlayerPrototype.LookChanged += (look) =>
			//{
			//	if (remoteRotationOverride)
			//	{
			//		Matrix lerpMatrix = Matrix.Lerp(basicEffect.View, remoteRotation, 1 / 15f);
			//		basicEffect.View = lerpMatrix;
			//             } else { 
			//		pitch = MathUtil.Clamp(pitch, (float)-Math.PI/2f, (float)Math.PI / 2f);
			//		Quaternion q1 = Quaternion.RotationYawPitchRoll(yaw, 0, 0);
			//		Quaternion q2 = Quaternion.RotationYawPitchRoll(0, pitch, 0);
			//		basicEffect.View = look * Matrix.RotationQuaternion(q2 * q1);
			//	}
			//};
			ResetRotation();
		}

		public void SetLook(System.Tuple<float, float,float> euler)
		{
			remoteRotationOverride = true;
			Quaternion q1 = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(euler.Item2), 0, 0);
			Quaternion q2 = Quaternion.RotationYawPitchRoll(0, MathUtil.DegreesToRadians(euler.Item1), 0);
			Quaternion q3 = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.DegreesToRadians(euler.Item3));
			remoteRotation = Matrix.RotationQuaternion(q3 * (q2 * q1));
			//remoteRotation = Matrix.RotationYawPitchRoll(MathUtil.DegreesToRadians(euler.Item2), MathUtil.DegreesToRadians(euler.Item1), MathUtil.DegreesToRadians(euler.Item3));
		}

		public void SetVideoTexture(Texture2D sharedTexture)
		{
			this.videoTexture = sharedTexture;
		}

		public void MoveDelta(float x, float y, float ratio, float lerpSpeed)
		{
			yaw += -MathUtil.DegreesToRadians(x) * ratio;
            pitch += -MathUtil.DegreesToRadians(y) * ratio;

			pitch = MathUtil.Clamp(pitch, (float)-Math.PI / 2f, (float)Math.PI / 2f);
			Quaternion q1 = Quaternion.RotationYawPitchRoll(yaw, 0, 0);
			Quaternion q2 = Quaternion.RotationYawPitchRoll(0, pitch, 0);
			//basicEffect.View = Matrix.RotationQuaternion(q2 * q1);
			this.lerpSpeed = lerpSpeed;
			targetRotationQuaternion = q2 * q1;
		}

		public void ResetRotation()
		{
			yaw = 0;
			pitch = 0;
			Quaternion q1 = Quaternion.RotationYawPitchRoll(yaw, 0, 0);
			Quaternion q2 = Quaternion.RotationYawPitchRoll(0, pitch, 0);
			basicEffect.View = Matrix.RotationQuaternion(q2 * q1);
			currentRotationQuaternion = q2 * q1;
		}


        void IScene.Detach()
        {
			Disposer.RemoveAndDispose(ref sharedTex);
			Disposer.RemoveAndDispose(ref resource);
			Disposer.RemoveAndDispose(ref graphicsDevice);
			Disposer.RemoveAndDispose(ref basicEffect);
			Disposer.RemoveAndDispose(ref primitive);
		}



        void IScene.Update(TimeSpan sceneTime)
        {
			var currentFrameTime = (float)sceneTime.TotalMilliseconds * 0.001f;
			if (lastFrameTime == 0) lastFrameTime = currentFrameTime;
			deltaTime = currentFrameTime - lastFrameTime;
			lastFrameTime = currentFrameTime;
			currentRotationQuaternion = Quaternion.Lerp(currentRotationQuaternion, targetRotationQuaternion, lerpSpeed * deltaTime);

			basicEffect.View = Matrix.RotationQuaternion(currentRotationQuaternion);
			//basicEffect.View = Matrix.Lerp(basicEffect.View, Matrix.RotationQuaternion(targetRotationQuaternion), 3f * deltaTime);
		}

		void IScene.Render()
        {
            Device device = this.Host.Device;
            if (device == null)
                return;

			var speed = 50f;

			if (HasFocus)
			{
				if (Keyboard.IsKeyDown(Key.Left))
					MoveDelta(1f, 0f, speed * deltaTime, 4f);
				if (Keyboard.IsKeyDown(Key.Right))
					MoveDelta(-1.0f, 0f, speed * deltaTime, 4f);
				if (Keyboard.IsKeyDown(Key.Up))
					MoveDelta(0f, 1f, speed * deltaTime, 4f);
				if (Keyboard.IsKeyDown(Key.Down))
					MoveDelta(0f, -1f, speed * deltaTime, 4f);
			}

			primitive.Draw(basicEffect);
        }
    }
}
