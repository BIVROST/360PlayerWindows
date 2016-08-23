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
		private bool textureReleased = true;
		private bool pollForTexture = false;

		private object localCritical = new object();


		SharpDX.Toolkit.Graphics.GraphicsDevice graphicsDevice;
		SharpDX.Toolkit.Graphics.Effect customEffect;

		SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;
		//SharpDX.Toolkit.Graphics.GeometricPrimitive primitive2;

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

		private OSVR.ClientKit.ClientContext context;
		private OSVR.ClientKit.DisplayConfig displayConfig;
		private bool useOSVR = false;
		private bool overrideManual = false;

        private Quaternion osvrQuaternion;
        public Quaternion GetCurrentLook()
        {
            if(useOSVR && !overrideManual)
            {
                if (osvrQuaternion == null) return Quaternion.Identity;
                return osvrQuaternion;
            } else
            {
                if (currentRotationQuaternion == null) return Quaternion.Identity;
                return currentRotationQuaternion;
            }
        }

        public Scene(Texture2D sharedTexture, MediaDecoder.ProjectionMode projection)
		{
			videoTexture = sharedTexture;
			projectionMode = projection;
		}

		//public Vector2 MapCube(int index, Vector2 vector)
		//{
		//	Vector2 vector2;
		//	if (index != 5)
		//		vector2 = new Vector2(vector.X == 1 ? 0 : 1, vector.Y);
		//	else
		//		vector2 = new Vector2(vector.X, vector.Y == 1?0:1);

		//	Dictionary<int, Vector4> map = new Dictionary<int, Vector4>();

		//	map.Add(3, new Vector4(3f, 0f / 3f, 2f, 0f));
		//	map.Add(2, new Vector4(3f, 1f / 3f, 2f, 0f));
		//	map.Add(4, new Vector4(3f, 2f / 3f, 2f, 0f));
		//	map.Add(5, new Vector4(3f, 0f / 3f, 2f, 1f / 2f));
		//	map.Add(1, new Vector4(3f, 1f / 3f, 2f, 1f / 2f));
		//	map.Add(0, new Vector4(3f, 2f / 3f, 2f, 1f / 2f));

		//	return new Vector2(
		//		vector2.X / map[index].X + map[index].Y,
		//		vector2.Y / map[index].Z + map[index].W
		//		);
		//}

		Statistics.Heatmap heatmap;
		float heatmapDelta = 1;

		void ResizeTexture(Texture2D tL, Texture2D tR)
		{
			if(MediaDecoder.Instance.TextureReleased) return;
			var tempResource = resource;
			var tempSharedTex = sharedTex;
			var tempVideotexture = videoTexture;

			lock(localCritical)
			{
				//resource?.Dispose();
				//sharedTex?.Dispose();
				//videoTexture?.Dispose();

				videoTexture = tL;

				resource = videoTexture.QueryInterface<SharpDX.DXGI.Resource>();
				sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle);

				customEffect.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(graphicsDevice, sharedTex));
				customEffect.Parameters["gammaFactor"].SetValue(1f);
				customEffect.CurrentTechnique = customEffect.Techniques["ColorTechnique"];
				customEffect.CurrentTechnique.Passes[0].Apply();

				//SamplerStateDescription samplerDescription = new SamplerStateDescription()
				//{
				//	AddressU = TextureAddressMode.Wrap,
				//	AddressV = TextureAddressMode.Wrap,
				//	AddressW = TextureAddressMode.Wrap,
				//	BorderColor = new Color4(0, 0, 0, 0),
				//	ComparisonFunction = Comparison.Never,
				//	Filter = Filter.Anisotropic,
				//	MaximumAnisotropy = 16,
				//	MaximumLod = float.MaxValue,
				//	MinimumLod = 0,
				//	MipLodBias = 0
				//};
				//SharpDX.Toolkit.Graphics.SamplerState textureSampler = SharpDX.Toolkit.Graphics.SamplerState.New(graphicsDevice, samplerDescription);



				ShaderResourceView shaderResourceView = new ShaderResourceView(_device, sharedTex);

				//_device.ImmediateContext.PixelShader.SetShaderResource(0, shaderResourceView);

				//customEffect.Parameters["UserTexSampler"].SetResource(textureSampler);
				//customEffect.CurrentTechnique = customEffect.Techniques["ColorTechnique"];
				//customEffect.CurrentTechnique.Passes[0].Apply();

				resource.Dispose();
				sharedTex.Dispose();
				//textureReleased = false;

				//_device.ImmediateContext.Flush();
			}
			tempResource?.Dispose();
			tempSharedTex?.Dispose();
			tempVideotexture?.Dispose();
		}

		//void ReleaseTexture()
		//{
		//	textureReleased = true;
		//}

		void IScene.Attach(ISceneHost host)
        {
            this.Host = host;
            _device = host.Device;

            if (_device == null)
                throw new Exception("Scene host device is null");

			graphicsDevice = SharpDX.Toolkit.Graphics.GraphicsDevice.New(_device);
			customEffect = Headset.GetCustomEffect(graphicsDevice);


			//==============
			//SharpDX.Toolkit.Graphics.EffectCompiler compiler = new SharpDX.Toolkit.Graphics.EffectCompiler();
			//var shaderCode = compiler.CompileFromFile("Shaders/GammaShader.fx", SharpDX.Toolkit.Graphics.EffectCompilerFlags.Debug | SharpDX.Toolkit.Graphics.EffectCompilerFlags.EnableBackwardsCompatibility | SharpDX.Toolkit.Graphics.EffectCompilerFlags.SkipOptimization);
			
			//if (shaderCode.HasErrors)
			//{
			//	shaderCode.Logger.Messages.ForEach(m => System.Diagnostics.Debug.WriteLine("[shader error] " + m));
			//}
			//customEffect = new SharpDX.Toolkit.Graphics.Effect(graphicsDevice, shaderCode.EffectData);
			//customEffect.CurrentTechnique = customEffect.Techniques["ColorTechnique"];
			//customEffect.CurrentTechnique.Passes[0].Apply();

			//SharpDX.D3DCompiler.ShaderReflection sr;
			//sr = new SharpDX.D3DCompiler.ShaderReflection(shaderCode.EffectData.Shaders[0].Bytecode);
			//int ResourceCount = sr.Description.BoundResources;
			//SharpDX.D3DCompiler.InputBindingDescription desc = sr.GetResourceBindingDescription(0);
			//;


			//==============





			MediaDecoder.Instance.OnFormatChanged += ResizeTexture;
			//MediaDecoder.Instance.OnReleaseTexture += ReleaseTexture;

			projectionMatrix = Matrix.PerspectiveFovRH((float)(72f * Math.PI / 180f), (float)16f/9f, 0.0001f, 50.0f);
			worldMatrix = Matrix.Identity;

			//basicEffect.PreferPerPixelLighting = false;

			ResizeTexture(MediaDecoder.Instance.TextureL, MediaDecoder.Instance.TextureL);

			//basicEffect.TextureEnabled = true;
			//basicEffect.LightingEnabled = false;
			//basicEffect.Sampler = graphicsDevice.SamplerStates.AnisotropicClamp;

			primitive = GraphicTools.CreateGeometry(projectionMode, graphicsDevice);
			//primitive2 = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(graphicsDevice, 1f,1f);

			_device.ImmediateContext.Flush();
			ResetRotation();
			
			heatmap = new Statistics.Heatmap();
            
            var devices = SharpDX.RawInput.Device.GetDevices();
            devices.ForEach(dev =>
            {
                if(dev.DeviceType == SharpDX.RawInput.DeviceType.Mouse)
                    SharpDX.RawInput.Device.RegisterDevice(SharpDX.Multimedia.UsagePage.Generic, SharpDX.Multimedia.UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None, dev.Handle);
                Console.WriteLine($"{dev.DeviceName} :: {dev.DeviceType}");
            });

			useOSVR = Logic.Instance.settings.UserOSVRTracking;
			if(useOSVR)
			{
				OSVR.ClientKit.ClientContext.PreloadNativeLibraries();
				context = new OSVR.ClientKit.ClientContext("com.bivrost360.desktopplayer");
				displayConfig = context.GetDisplayConfig();
				for (int retry = 0; retry < 5; retry++)
					if (displayConfig == null)
						displayConfig = context.GetDisplayConfig();
				if (displayConfig == null) return;
				do
				{
					context.update();
				} while (!displayConfig.CheckDisplayStartup());
			}
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

		public void SetLook(System.Tuple<float,float,float,float> quat)
		{
			targetRotationQuaternion = new Quaternion(quat.Item1, quat.Item2, -quat.Item3, quat.Item4);
			lerpSpeed = 9f;
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
            ShellViewModel.SendEvent("zoomChanged", targetFov);
        }

		public void ResetFov()
		{
			targetFov = DEFAULT_FOV;
            ShellViewModel.SendEvent("zoomChanged", targetFov);
        }

		public void ResetRotation()
		{
			yaw = 0;
			pitch = 0;
			Quaternion q1 = Quaternion.RotationYawPitchRoll(yaw, 0, 0);
			Quaternion q2 = Quaternion.RotationYawPitchRoll(0, pitch, 0);
			viewMatrix = Matrix.RotationQuaternion(q2 * q1);
			currentRotationQuaternion = q2 * q1;
		}


        void IScene.Detach()
        {
			//Console.WriteLine(heatmap.ToBase64());

			MediaDecoder.Instance.OnFormatChanged -= ResizeTexture;
			//MediaDecoder.Instance.OnReleaseTexture -= ReleaseTexture;

			Disposer.RemoveAndDispose(ref sharedTex);
			Disposer.RemoveAndDispose(ref resource);
			Disposer.RemoveAndDispose(ref graphicsDevice);
			Disposer.RemoveAndDispose(ref customEffect);
			Disposer.RemoveAndDispose(ref primitive);
		}

		public void StereographicProjection()
		{
			littlePlanet = true;
            ShellViewModel.SendEvent("projectionChanged", "stereographic");
		}

		public void RectlinearProjection()
		{
			littlePlanet = false;
            ShellViewModel.SendEvent("projectionChanged", "gnomic");
        }

        void IScene.Update(TimeSpan sceneTime)
        {
			//if (pollForTexture)
			//	if (!MediaDecoder.Instance.TextureReleased)
			//	{
			//		ResizeTexture(MediaDecoder.Instance.TextureL, MediaDecoder.Instance.TextureR);
			//		pollForTexture = false;
			//	}

			var currentFrameTime = (float)sceneTime.TotalMilliseconds * 0.001f;
			if (lastFrameTime == 0) lastFrameTime = currentFrameTime;
			deltaTime = currentFrameTime - lastFrameTime;
			lastFrameTime = currentFrameTime;

			currentRotationQuaternion = Quaternion.Lerp(currentRotationQuaternion, targetRotationQuaternion, lerpSpeed * deltaTime);

			if (useOSVR && !overrideManual)
			{
                try
                {
                    context.update();
                    var viewerPose = displayConfig.GetViewerPose(0);
                    Quaternion oq = new Quaternion(-(float)viewerPose.rotation.x, -(float)viewerPose.rotation.y, -(float)viewerPose.rotation.z, (float)viewerPose.rotation.w);
                    osvrQuaternion = oq;
                    viewMatrix = Matrix.RotationQuaternion(oq);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("[EXC] " + exc.Message);
                    Console.WriteLine("[EXC] " + exc.StackTrace);
                    ;
                    overrideManual = true;
                }
            }
			else {
				viewMatrix = Matrix.RotationQuaternion(currentRotationQuaternion);
			}

			currentOffset = Lerp(currentOffset, littlePlanet ? -3f : 0f, deltaTime * 3f);
            viewMatrix *= Matrix.Translation(0, 0, currentOffset);


			//basicEffect.View = Matrix.Lerp(basicEffect.View, Matrix.RotationQuaternion(targetRotationQuaternion), 3f * deltaTime);

			//customEffect.Parameters["WorldViewProj"].SetValue(basicEffect.World * basicEffect.View * basicEffect.Projection);

		}

        //public void ButtonOnce(State padState, GamepadButtonFlags button, Action buttonAction)
        //{
        //    if(!buttonStates.ContainsKey(button))
        //        buttonStates.Add(button, false);
        //    if (padState.Gamepad.Buttons == button)
        //    {
        //        if(!buttonStates[button])
        //        {
        //            buttonStates[button] = true;
        //            buttonAction();
        //        }
        //    }
        //    else buttonStates[button] = false;
        //}

		void IScene.Render()
        {
			

            Device device = this.Host.Device;
            if (device == null)
                return;

			var speed = 50f;
			//currentFov = Lerp(currentFov, targetFov, 5f * deltaTime);
			currentFov = currentFov.LerpInPlace(targetFov, 5f * deltaTime);
			projectionMatrix = Matrix.PerspectiveFovRH((float)(currentFov * Math.PI / 180f), (float)16f / 9f, 0.0001f, 50.0f);


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

			if (xpad != null && xpad.IsConnected)
			{
				var state = xpad.GetState();
				float padx = state.Gamepad.LeftThumbX / 256;
				float pady = state.Gamepad.LeftThumbY / 256;
				Vector2 padVector = new Vector2(padx, pady);
				if (padVector.LengthSquared() > 50)
				{
					MoveDelta(-1f * padVector.X, 1f * padVector.Y, 0.02f * speed * deltaTime, 4f);
				}

				//ButtonOnce(state, GamepadButtonFlags.A, () => ShellViewModel.Instance.PlayPause());
				//ButtonOnce(state, GamepadButtonFlags.Y, () => ShellViewModel.Instance.Rewind());
				//ButtonOnce(state, GamepadButtonFlags.DPadLeft, () => ShellViewModel.Instance.SeekRelative(-5));
				//ButtonOnce(state, GamepadButtonFlags.DPadRight, () => ShellViewModel.Instance.SeekRelative(5));
				//ButtonOnce(state, GamepadButtonFlags.DPadUp, () => Caliburn.Micro.Execute.OnUIThreadAsync(() => ShellViewModel.Instance.VolumeRocker.Volume += 0.1));
				//ButtonOnce(state, GamepadButtonFlags.DPadDown, () => Caliburn.Micro.Execute.OnUIThreadAsync(() => ShellViewModel.Instance.VolumeRocker.Volume -= 0.1));
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

				if (useOSVR) {
					if(Keyboard.IsKeyDown(Key.T))
					{
						if(tUp)
						{
							overrideManual = !overrideManual;
							tUp = false;
						}
					}
					if (Keyboard.IsKeyUp(Key.T)) tUp = true;
				}

                if (projectionMode == MediaDecoder.ProjectionMode.Sphere)
				{
					if (Keyboard.IsKeyDown(Key.L))
					{
                        //littlePlanet = true;
                        StereographicProjection();
                        targetFov = DEFAULT_LITTLE_FOV;
                        
					}
					if (Keyboard.IsKeyDown(Key.N))
					{
                        //littlePlanet = false;
                        RectlinearProjection();
						targetFov = DEFAULT_FOV;
					}
				}
			}
			//if (!textureReleased)
			//{

			customEffect.Parameters["WorldViewProj"].SetValue(worldMatrix * viewMatrix * projectionMatrix);

			
			lock (localCritical)
			{
				primitive.Draw(customEffect);
			}
        }

		private bool tUp = false;
		private Matrix projectionMatrix;
		private Matrix worldMatrix;
		private Matrix viewMatrix;

		private float Lerp(float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
        }
    }
}
