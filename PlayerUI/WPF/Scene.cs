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
    using Tools;
    using System.Linq;
    using System.Collections.Generic;
    using SharpDX.XInput;

    public class Scene : IScene
    {
        //private class RefBool
        //{
        //    public RefBool() { }
        //    public RefBool(bool value) { this.Value = value; }
        //    public bool Value = false;
        //}

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
		private float targetFov = 72f;
		private float currentFov = 72f;
		private bool littlePlanet = false;
		private float currentOffset = 0f;
        
		private const float MIN_FOV = 40f;		
		private const float DEFAULT_FOV = 90f;
		private const float DEFAULT_LITTLE_FOV = 120f;
		private const float MAX_FOV = 150f;
        

        private Texture2D sharedTex;
		private MediaDecoder.ProjectionMode projectionMode;
		private SharpDX.DXGI.Resource resource;

        public Controller xpad;
        Dictionary<GamepadButtonFlags, bool> buttonStates = new Dictionary<GamepadButtonFlags, bool>();

        public Scene(Texture2D sharedTexture, MediaDecoder.ProjectionMode projection)
		{
			videoTexture = sharedTexture;
			projectionMode = projection;
		}

		public Vector2 MapCube(int index, Vector2 vector)
		{
			Vector2 vector2;
			if (index != 5)
				vector2 = new Vector2(vector.X == 1 ? 0 : 1, vector.Y);
			else
				vector2 = new Vector2(vector.X, vector.Y == 1?0:1);

			Dictionary<int, Vector4> map = new Dictionary<int, Vector4>();

			map.Add(3, new Vector4(3f, 0f / 3f, 2f, 0f));
			map.Add(2, new Vector4(3f, 1f / 3f, 2f, 0f));
			map.Add(4, new Vector4(3f, 2f / 3f, 2f, 0f));
			map.Add(5, new Vector4(3f, 0f / 3f, 2f, 1f / 2f));
			map.Add(1, new Vector4(3f, 1f / 3f, 2f, 1f / 2f));
			map.Add(0, new Vector4(3f, 2f / 3f, 2f, 1f / 2f));

			return new Vector2(
				vector2.X / map[index].X + map[index].Y,
				vector2.Y / map[index].Z + map[index].W
				);
		}

		Statistics.Heatmap heatmap;
		float heatmapDelta = 1;

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
			basicEffect.Sampler = graphicsDevice.SamplerStates.AnisotropicClamp;

			primitive = GraphicTools.CreateGeometry(projectionMode, graphicsDevice);


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

			//ShellViewModel.Instance.ShowDebug();
			heatmap = new Statistics.Heatmap();
            
            var devices = SharpDX.RawInput.Device.GetDevices();
            devices.ForEach(dev =>
            {
                if(dev.DeviceType == SharpDX.RawInput.DeviceType.Mouse)
                    SharpDX.RawInput.Device.RegisterDevice(SharpDX.Multimedia.UsagePage.Generic, SharpDX.Multimedia.UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None, dev.Handle);
                Console.WriteLine($"{dev.DeviceName} :: {dev.DeviceType}");
            });

            SharpDX.RawInput.Device.MouseInput += (s, e) =>
            {
                Console.WriteLine("Mouse " + e.X + " " + e.Y);
            };          
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

		public void ChangeFov(float fov)
		{
			targetFov += fov;
			targetFov = Math.Min(MAX_FOV, Math.Max(targetFov, MIN_FOV));
		}

		public void ResetFov()
		{
			targetFov = DEFAULT_FOV;
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
			Console.WriteLine(heatmap.ToBase64());

			Disposer.RemoveAndDispose(ref sharedTex);
			Disposer.RemoveAndDispose(ref resource);
			Disposer.RemoveAndDispose(ref graphicsDevice);
			Disposer.RemoveAndDispose(ref basicEffect);
			Disposer.RemoveAndDispose(ref primitive);
		}

		public void StereographicProjection()
		{
			littlePlanet = true;
		}

		public void RectlinearProjection()
		{
			littlePlanet = false;
		}

        void IScene.Update(TimeSpan sceneTime)
        {
			var currentFrameTime = (float)sceneTime.TotalMilliseconds * 0.001f;
			if (lastFrameTime == 0) lastFrameTime = currentFrameTime;
			deltaTime = currentFrameTime - lastFrameTime;
			lastFrameTime = currentFrameTime;

			currentRotationQuaternion = Quaternion.Lerp(currentRotationQuaternion, targetRotationQuaternion, lerpSpeed * deltaTime);

			basicEffect.View = Matrix.RotationQuaternion(currentRotationQuaternion);
			//if(littlePlanet)
			currentOffset = Lerp(currentOffset, littlePlanet ? -3f : 0f, deltaTime * 3f);
            basicEffect.View *= Matrix.Translation(0, 0, currentOffset);


			//basicEffect.View = Matrix.Lerp(basicEffect.View, Matrix.RotationQuaternion(targetRotationQuaternion), 3f * deltaTime);
		}

        public void ButtonOnce(State padState, GamepadButtonFlags button, Action buttonAction)
        {
            if(!buttonStates.ContainsKey(button))
                buttonStates.Add(button, false);
            if (padState.Gamepad.Buttons == button)
            {
                if(!buttonStates[button])
                {
                    buttonStates[button] = true;
                    buttonAction();
                }
            }
            else buttonStates[button] = false;
        }

		void IScene.Render()
        {
			

            Device device = this.Host.Device;
            if (device == null)
                return;

			var speed = 50f;
			//currentFov = Lerp(currentFov, targetFov, 5f * deltaTime);
			currentFov = currentFov.LerpInPlace(targetFov, 5f * deltaTime);
			basicEffect.Projection = Matrix.PerspectiveFovRH((float)(currentFov * Math.PI / 180f), (float)16f / 9f, 0.0001f, 50.0f);


			// rotation quaternion to heatmap directions
			heatmapDelta += deltaTime;
			if(heatmapDelta > 0.33333333f)
			{
				heatmap.TrackData(currentRotationQuaternion);
				heatmapDelta = 0;
			}

            //ShellViewModel.Instance.ClearDebugText();
            //Vector2 v = GraphicTools.QuaternionToYawPitch(currentRotationQuaternion);
            //var yawdeg = MathUtil.RadiansToDegrees(v.X);
            //var pitchdeg = MathUtil.RadiansToDegrees(v.Y);
            //ShellViewModel.Instance.AppendDebugText($"YAW:{yawdeg} \t\t PITCH:{pitchdeg}");
            //ShellViewModel.Instance.UpdateDebugText();
            //==========================================

            if (xpad.IsConnected)
            {
                var state = xpad.GetState();
                float padx = state.Gamepad.LeftThumbX / 256;
                float pady = state.Gamepad.LeftThumbY / 256;
                Vector2 padVector = new Vector2(padx, pady);
                if(padVector.LengthSquared() > 5)
                {
                    MoveDelta(-1f * padVector.X, 1f * padVector.Y, 0.02f * speed * deltaTime, 4f);
                }

                ButtonOnce(state, GamepadButtonFlags.A, () => ShellViewModel.Instance.PlayPause());
                ButtonOnce(state, GamepadButtonFlags.Y, () => ShellViewModel.Instance.Rewind());
                ButtonOnce(state, GamepadButtonFlags.DPadLeft, () => ShellViewModel.Instance.SeekRelative(-5));
                ButtonOnce(state, GamepadButtonFlags.DPadRight, () => ShellViewModel.Instance.SeekRelative(5));
                ButtonOnce(state, GamepadButtonFlags.DPadUp, () => Caliburn.Micro.Execute.OnUIThreadAsync(() => ShellViewModel.Instance.VolumeRocker.Volume += 0.1));
                ButtonOnce(state, GamepadButtonFlags.DPadDown, () => Caliburn.Micro.Execute.OnUIThreadAsync(() => ShellViewModel.Instance.VolumeRocker.Volume -= 0.1));

            }

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
				if (Keyboard.IsKeyDown(Key.Z))
				{
					ResetFov();
				}

                


                if (projectionMode == MediaDecoder.ProjectionMode.Sphere)
				{
					if (Keyboard.IsKeyDown(Key.L))
					{
						littlePlanet = true;
						targetFov = DEFAULT_LITTLE_FOV;
					}
					if (Keyboard.IsKeyDown(Key.N))
					{
						littlePlanet = false;
						targetFov = DEFAULT_FOV;
					}
				}
			}

			primitive.Draw(basicEffect);
        }

		private float Lerp(float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
        }
    }
}
