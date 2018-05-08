namespace Bivrost.Bivrost360Player
{
	using System;
	using SharpDX;
	using SharpDX.Direct3D11;
	using SharpDX.DXGI;
	using Device = SharpDX.Direct3D11.Device;
	using System.Windows.Input;
	using Tools;
	using System.Linq;
	using SharpDX.XInput;
	using Bivrost.Log;
	using Bivrost;
	using System.IO;
	using System.Drawing;
	using System.Drawing.Imaging;

	public interface IUpdatableSceneSettings
	{
		void UpdateSceneSettings(ProjectionMode projectionMode, VideoMode stereoscopy);
	}


	public interface IContentUpdatableFromMediaEngine
	{
		//bool IsReady { get; }
		void ReceiveTextures(Texture2D textureL, Texture2D textureR);
		void ReceiveBitmap(Bitmap bitmap, MediaDecoder.ClipCoords coordsL, MediaDecoder.ClipCoords coordsR);
		void ClearContent();

		// TODO: stereoscopy?
	}


	public class Scene : IScene, IUpdatableSceneSettings, IContentUpdatableFromMediaEngine
	{
		Logger log = new Logger("scene");

		private ISceneHost Host;
		private Device _device;

		private object localCritical = new object();

		SharpDX.Toolkit.Graphics.GraphicsDevice graphicsDevice;
		SharpDX.Toolkit.Graphics.Effect customEffect;

		SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;

		private float yaw = 0;
		private float pitch = 0;

		private float deltaTime = 0;
		private float lastFrameTime = 0;
		public bool HasFocus { get; set; } = true;
		private Quaternion targetRotationQuaternion;
		private Quaternion currentRotationQuaternion;
		private float lerpSpeed = 3f;
		private float targetFov = DEFAULT_FOV;
		private float currentFov = DEFAULT_FOV;
		private bool littlePlanet = false;
		private float currentOffset = 0f;

		private const float MIN_FOV = 40f;
		private const float DEFAULT_FOV = 72f;
		private const float DEFAULT_LITTLE_FOV = 120f;
		private const float MAX_FOV = 150f;

		private ProjectionMode projectionMode;
		private Action<IContentUpdatableFromMediaEngine> requestContentCallback;
		private bool requestContent = true;

		#region ILookProvider integration
		private Quaternion headsetLookRotation = Quaternion.Identity;

		private bool UseVrLook
		{
			get { return headsetLookRotation != Quaternion.Identity && SettingsVrLookEnabled; }
		}

		private bool SettingsVrLookEnabled
		{
			get { return Logic.Instance.settings.UserHeadsetTracking; }
			set { Logic.Instance.settings.UserHeadsetTracking = value; }
		}

		//bool IContentUpdatableFromMediaEngine.IsReady => throw new NotImplementedException();

		internal void HeadsetEnabled(Headset headset)
		{
			headset.ProvideLook += Headset_ProvideLook;
			LoggerManager.Publish("headset", headset.DescribeType);
		}

		private void Headset_ProvideLook(Vector3 pos, Quaternion rot, float fov)
		{
			headsetLookRotation = rot;
			LoggerManager.Publish("q.recv", rot);
		}

		internal void HeadsetDisabled(Headset headset)
		{
			headset.ProvideLook -= Headset_ProvideLook;
			Headset_ProvideLook(Vector3.Zero, Quaternion.Identity, 0);
			LoggerManager.Publish("headset", null);
		}
		#endregion


		public Scene(ProjectionMode projection, Action<IContentUpdatableFromMediaEngine> requestContent)
		{
			//videoTexture = sharedTexture;
			projectionMode = projection;
			this.requestContentCallback = requestContent;
			//ResizeTexture(sharedTexture, null);
		}


		InputDevices.KeyboardInputDevice keyboardInput;
		InputDevices.GamepadInputDevice gamepadInput;
		InputDevices.NavigatorInputDevice navigatorInput;


		void IScene.Attach(ISceneHost host)
		{
			this.Host = host;
			keyboardInput = new InputDevices.KeyboardInputDevice();
			gamepadInput = new InputDevices.GamepadInputDevice();
			navigatorInput = new InputDevices.NavigatorInputDevice();

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





			//			MediaDecoder.Instance.OnFormatChanged += ResizeTexture;
			//MediaDecoder.Instance.OnReleaseTexture += ReleaseTexture;

			//MediaDecoder.Instance.OnLoadItYourself += (s, l, r) => actionQueue.Enqueue(() => {
			//	Instance_OnLoadItYourself(s, l, r);
			//});

			MediaDecoder.Instance.OnContentChanged += () => requestContent = true;


			projectionMatrix = Matrix.PerspectiveFovRH((float)(72f * Math.PI / 180f), (float)16f / 9f, 0.0001f, 50.0f);
			worldMatrix = Matrix.Identity;

			//basicEffect.PreferPerPixelLighting = false;

			//ResizeTexture(MediaDecoder.Instance.TextureL, MediaDecoder.Instance.TextureL);

			//basicEffect.TextureEnabled = true;
			//basicEffect.LightingEnabled = false;
			//basicEffect.Sampler = graphicsDevice.SamplerStates.AnisotropicClamp;

			primitive = GraphicTools.CreateGeometry(projectionMode, graphicsDevice);
			//primitive2 = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(graphicsDevice, 1f,1f);

			_device.ImmediateContext.Flush();
			ResetRotation();


			var devices = SharpDX.RawInput.Device.GetDevices();
			devices.ForEach(dev =>
			{
				if (dev.DeviceType == SharpDX.RawInput.DeviceType.Mouse)
					SharpDX.RawInput.Device.RegisterDevice(SharpDX.Multimedia.UsagePage.Generic, SharpDX.Multimedia.UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None, dev.Handle);
				Console.WriteLine($"Scene::Attach DX device: {dev.DeviceName} :: {dev.DeviceType}");
			});
		}


		//public void SetLook(System.Tuple<float, float,float> euler)
		//{
		//	remoteRotationOverride = true;
		//	Quaternion q1 = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(euler.Item2), 0, 0);
		//	Quaternion q2 = Quaternion.RotationYawPitchRoll(0, MathUtil.DegreesToRadians(euler.Item1), 0);
		//	Quaternion q3 = Quaternion.RotationYawPitchRoll(0, 0, MathUtil.DegreesToRadians(euler.Item3));
		//	remoteRotation = Matrix.RotationQuaternion(q3 * (q2 * q1));
		//	//remoteRotation = Matrix.RotationYawPitchRoll(MathUtil.DegreesToRadians(euler.Item2), MathUtil.DegreesToRadians(euler.Item1), MathUtil.DegreesToRadians(euler.Item3));
		//}

		public void SetLook(System.Tuple<float, float, float, float> quat)
		{
			targetRotationQuaternion = new Quaternion(quat.Item1, quat.Item2, -quat.Item3, quat.Item4);
			lerpSpeed = 9f;
		}

		public void SetVideoTexture(Texture2D sharedTexture)
		{
			throw new NotImplementedException();
			//this.videoTexture = sharedTexture;
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
			//MediaDecoder.Instance.OnFormatChanged -= ResizeTexture;
			//MediaDecoder.Instance.OnReleaseTexture -= ReleaseTexture;

			//Disposer.RemoveAndDispose(ref sharedTex);
			//Disposer.RemoveAndDispose(ref resource);
			Disposer.RemoveAndDispose(ref graphicsDevice);
			Disposer.RemoveAndDispose(ref customEffect);
			Disposer.RemoveAndDispose(ref primitive);
		}

		public void StereographicProjection()
		{
			if (littlePlanet) return;

			littlePlanet = true;
			targetFov = DEFAULT_LITTLE_FOV;

			ShellViewModel.SendEvent("projectionChanged", "stereographic");
		}

		public void RectlinearProjection()
		{
			if (!littlePlanet) return;

			littlePlanet = false;
			targetFov = DEFAULT_FOV;

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




			//Console.WriteLine("")

			if (UseVrLook)
				currentRotationQuaternion = headsetLookRotation;

			currentOffset = Lerp(currentOffset, littlePlanet ? -3f : 0f, deltaTime * 3f);

			viewMatrix = Matrix.RotationQuaternion(currentRotationQuaternion);
			viewMatrix *= Matrix.Translation(0, 0, currentOffset);


			Vector3 lookUp = Vector3.Transform(Vector3.Up, viewMatrix).ToVector3();
			Vector3 lookAt = Vector3.Transform(Vector3.ForwardRH, viewMatrix).ToVector3();

			//Console.WriteLine($"SCENE: up: {lookUp:00.00}|{Vector3.Transform(Vector3.Up, currentRotationQuaternion):00.00} at: {lookAt:00.00}|{Vector3.Transform(Vector3.ForwardRH, currentRotationQuaternion):00.00}");

			LoggerManager.Publish("scene.forward", lookAt.ToString("0.00"));
			LoggerManager.Publish("scene.up", lookUp.ToString("0.00"));
			LoggerManager.Publish("scene.vr_quat", headsetLookRotation);


			//Quaternion q = new Quaternion(headsetLookRotation.X, headsetLookRotation.Y, headsetLookRotation.Z, headsetLookRotation.W);
			//Quaternion q2 = q;
			//q.X = 666;
			//;


			//Logger.Publish("q1", q.ToString());
			//GraphicTools.QuaternionToYawPitch(q);
			//Logger.Publish("q2", q.ToString());




			//basicEffect.View = Matrix.Lerp(basicEffect.View, Matrix.RotationQuaternion(targetRotationQuaternion), 3f * deltaTime);

			//customEffect.Parameters["WorldViewProj"].SetValue(basicEffect.World * basicEffect.View * basicEffect.Projection);

		}


		void UpdateInput()
		{
			// For more shortcuts see also: ShellView.xaml DPFCanvas1's cal:Message.Attach

			var allInputDevices = new InputDevices.InputDevice[]
			{
				keyboardInput,
				gamepadInput,
				navigatorInput
			};

			const float velocity = 90f; // deg per second
			const float fovVelocity = 75; // 1 second full push will change fov by 75 degrees 

			foreach (var id in allInputDevices)
			{
				id.Update(deltaTime);
			}


			if (HasFocus)
			{
				if (keyboardInput.Active)
				{
					MoveDelta(velocity * keyboardInput.vYaw * deltaTime, velocity * keyboardInput.vPitch * deltaTime, 1, 4);

					if (keyboardInput.KeyPressed(Key.L))
					{
						StereographicProjection();
					}

					if (keyboardInput.KeyPressed(Key.N))
					{
						RectlinearProjection();
					}

					if (keyboardInput.KeyPressed(Key.OemOpenBrackets))
						ShellViewModel.Instance.SeekRelative(-5);

					if (keyboardInput.KeyPressed(Key.OemCloseBrackets))
						ShellViewModel.Instance.SeekRelative(5);

					if (keyboardInput.KeyDown(Key.OemMinus) || keyboardInput.KeyDown(Key.Subtract))
						ChangeFov(fovVelocity * deltaTime);

					if (keyboardInput.KeyDown(Key.OemPlus) || keyboardInput.KeyDown(Key.Add))
						ChangeFov(-fovVelocity * deltaTime);
				}



				if (gamepadInput.Active)
				{
					MoveDelta(velocity * gamepadInput.vYaw * deltaTime, velocity * gamepadInput.vPitch * deltaTime, 1, 4);

					if (gamepadInput.ButtonPressed(GamepadButtonFlags.A))
						ShellViewModel.Instance.PlayPause();

					if (gamepadInput.ButtonPressed(GamepadButtonFlags.Y))
						ShellViewModel.Instance.Rewind();

					if (gamepadInput.ButtonPressed(GamepadButtonFlags.DPadLeft))
						ShellViewModel.Instance.SeekRelative(-5);

					if (gamepadInput.ButtonPressed(GamepadButtonFlags.DPadRight))
						ShellViewModel.Instance.SeekRelative(5);

					if (gamepadInput.ButtonPressed(GamepadButtonFlags.DPadUp))
						Caliburn.Micro.Execute.OnUIThreadAsync(() => ShellViewModel.Instance.VolumeRocker.Volume += 0.1);

					if (gamepadInput.ButtonPressed(GamepadButtonFlags.DPadDown))
						Caliburn.Micro.Execute.OnUIThreadAsync(() => ShellViewModel.Instance.VolumeRocker.Volume -= 0.1);
				}


				if (navigatorInput.Active)
				{
					float vYaw = navigatorInput.vRoll + navigatorInput.vYaw;
					if (vYaw < -1) vYaw = -1;
					if (vYaw > 1) vYaw = 1;
					float vPitch = navigatorInput.vPitch;
					MoveDelta(velocity * vYaw * deltaTime, velocity * vPitch * deltaTime, 1, 4);


					if (Logic.Instance.settings.SpaceNavigatorKeysAndZoomActive)
					{
						if (navigatorInput.leftPressed)
							ShellViewModel.Instance.PlayPause();

						if (navigatorInput.rightPressed)
							ShellViewModel.Instance.Rewind();

						float zoom = navigatorInput.vPush;
						ChangeFov(fovVelocity * zoom * deltaTime);
					}
				}
			}

			foreach (var id in allInputDevices)
			{
				id.LateUpdate(deltaTime);
			}
		}



		void IScene.Render()
		{
			actionQueue.RunAllActions();

			Device device = this.Host.Device;
			if (device == null)
				return;

			//currentFov = Lerp(currentFov, targetFov, 5f * deltaTime);
			currentFov = currentFov.LerpInPlace(targetFov, 5f * deltaTime);
			projectionMatrix = Matrix.PerspectiveFovRH((float)(currentFov * Math.PI / 180f), (float)16f / 9f, 0.0001f, 50.0f);

			UpdateInput();

			// Little planet makes sense only in sphere and dome projections
			var littlePlanetProjections = new[] { ProjectionMode.Sphere, ProjectionMode.Dome };
			if (littlePlanet && !littlePlanetProjections.Contains(projectionMode))
			{
				RectlinearProjection();
			}


			//if (!textureReleased)
			//{

			customEffect.Parameters["WorldViewProj"].SetValue(worldMatrix * viewMatrix * projectionMatrix);

			if (requestContent)
			{
				requestContent = false;
				requestContentCallback(this);
			}


			lock (localCritical)
			{
				primitive.Draw(customEffect);
			}
		}

		private Matrix projectionMatrix;
		private Matrix worldMatrix;
		private Matrix viewMatrix;

		private float Lerp(float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
		}


		#region scene settings updater

		ActionQueue actionQueue = new ActionQueue();

		public void UpdateSceneSettings(ProjectionMode projectionMode, VideoMode stereoscopy)
		{
			actionQueue.Enqueue(() =>
			{
				primitive?.Dispose();
				primitive = GraphicTools.CreateGeometry(projectionMode, graphicsDevice);
				this.projectionMode = projectionMode;
				// TODO: stereoscopy
			});
		}
		#endregion


		#region IContentUpdatableFromMediaEngine
		void IContentUpdatableFromMediaEngine.ReceiveTextures(Texture2D textureL, Texture2D textureR)
		{
			if (MediaDecoder.Instance.TextureReleased) return;

			lock (localCritical)
			{
				localVideoTexture?.Dispose();
				localVideoTexture = null;

				using (var resource = textureL.QueryInterface<SharpDX.DXGI.Resource>())
				using (var sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle))
				{
					customEffect.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(graphicsDevice, sharedTex));
					customEffect.Parameters["gammaFactor"].SetValue(1f);
					customEffect.CurrentTechnique = customEffect.Techniques["ColorTechnique"];
					customEffect.CurrentTechnique.Passes[0].Apply();
				}
			}
		}


		/// <summary>
		/// Texture used with DataRects
		/// </summary>
		private Texture2D localVideoTexture = null;


		void IContentUpdatableFromMediaEngine.ReceiveBitmap(Bitmap bitmap, MediaDecoder.ClipCoords texL, MediaDecoder.ClipCoords texR)
		{
			log.Info($"Received image of size {bitmap.Width}x{bitmap.Height} from stream");

			var r = texL.SrcRectSystemDrawing(bitmap.Width, bitmap.Height);
			var data = bitmap.LockBits(
				r,
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppRgb
			);
			var dataRect = new DataRectangle(data.Scan0, data.Stride);

			lock (localCritical)
			{
				localVideoTexture?.Dispose();

				localVideoTexture = new Texture2D(
					_device,
					new Texture2DDescription()
					{
						Width = r.Width,
						Height = r.Height,
						MipLevels = 1,
						ArraySize = 1,
						Format = Format.B8G8R8X8_UNorm,
						Usage = ResourceUsage.Default,
						SampleDescription = new SampleDescription(1, 0),
						BindFlags = /*BindFlags.RenderTarget |*/ BindFlags.ShaderResource,
						CpuAccessFlags = CpuAccessFlags.None,
						OptionFlags = ResourceOptionFlags.Shared
					},
					dataRect
				);

				using (var resource = localVideoTexture.QueryInterface<SharpDX.DXGI.Resource>())
				using (var sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle))
				{
					customEffect.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(graphicsDevice, sharedTex));
					customEffect.Parameters["gammaFactor"].SetValue(1f);
					customEffect.CurrentTechnique = customEffect.Techniques["ColorTechnique"];
					customEffect.CurrentTechnique.Passes[0].Apply();
				}
			}

			bitmap.UnlockBits(data);

			log.Info($"Changed texture");
		}


		void IContentUpdatableFromMediaEngine.ClearContent()
		{
			lock (localCritical)
			{
				localVideoTexture?.Dispose();
				//customEffect.Parameters["UserTex"].SetResource(null);
			}
		}

		#endregion
	}
}
