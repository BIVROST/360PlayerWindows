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


	public interface IContentUpdatableFromMediaEngine
	{
		/// <summary>
		/// The renderer receives a texture that it will use but not manage
		/// Should not be disposed.
		/// </summary>
		/// <param name="textureL">Texture that should be used with the left eye or common view</param>
		/// <param name="textureR">Texture that should be used with the right eye, can be null</param>
		void ReceiveTextures(Texture2D textureL, Texture2D textureR);


		/// <summary>
		/// The renderer receives a source bitmap and coordinates for viewports in 
		/// </summary>
		/// <param name="bitmap">Source bitmap with the complete video frame</param>
		/// <param name="coordsL">Coordinates of the bitmap that should be used with the left eye or common view</param>
		/// <param name="coordsR">Coordinates of the bitmap that should be used with the right eye, can be null</param>
		void ReceiveBitmap(Bitmap bitmap, MediaDecoder.ClipCoords coordsL, MediaDecoder.ClipCoords coordsR);


		/// <summary>
		/// The renderer is instructed that the content previously sent is no more valid and should be discarded.
		/// </summary>
		void ClearContent();


		/// <summary>
		/// Sets the projection of the content provided.
		/// Should not affect anything when the content is cleared.
		/// </summary>
		/// <param name="projection">The projection to be set. Autodetect value should not reach here.</param>
		void SetProjection(ProjectionMode projection);
	}


	public class Scene : IScene, IContentUpdatableFromMediaEngine
	{
		Logger log = new Logger("scene");

		private ISceneHost Host;
		private Device _device;

		private object localCritical = new object();

		SharpDX.Toolkit.Graphics.GraphicsDevice graphicsDevice;
		SharpDX.Toolkit.Graphics.Effect customEffect;

        SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;
        ProjectionMode projectionMode;

        SharpDX.Toolkit.Graphics.GeometricPrimitive bgPrimitive;
        Effects.AutoRefreshEffect bgCustomEffectSource;


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

		private Func<IContentUpdatableFromMediaEngine, bool> requestContentCallback;
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
			log.Publish("headset", headset.DescribeType);
		}

		private void Headset_ProvideLook(Vector3 pos, Quaternion rot, float fov)
		{
			headsetLookRotation = rot;
			log.Publish("q.recv", rot);
		}

		internal void HeadsetDisabled(Headset headset)
		{
			headset.ProvideLook -= Headset_ProvideLook;
			Headset_ProvideLook(Vector3.Zero, Quaternion.Identity, 0);
			LoggerManager.Publish("headset", null);
		}
		#endregion


		public Scene(Func<IContentUpdatableFromMediaEngine, bool> requestContentCallback)
		{
			this.requestContentCallback = requestContentCallback;
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
            customEffect = Effects.GetEffect(graphicsDevice, Effects.GammaShader);

            bgCustomEffectSource = new Effects.AutoRefreshEffect(@"D:\Projekty\360PlayerWindows\360Player\Shaders\ImageBasedLightEquirectangular.hlsl");
            bgPrimitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Teapot.New(graphicsDevice, 1, 8);

            MediaDecoder.Instance.OnContentChanged += ContentChanged;

			projectionMatrix = Matrix.PerspectiveFovRH((float)(72f * Math.PI / 180f), (float)16f / 9f, 0.0001f, 50.0f);
			worldMatrix = Matrix.Identity;
			
			//primitive = GraphicTools.CreateGeometry(projectionMode, graphicsDevice);

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

		private void ContentChanged()
		{
			requestContent = true;
		}

		public void SetLook(System.Tuple<float, float, float, float> quat)
		{
			targetRotationQuaternion = new Quaternion(quat.Item1, quat.Item2, -quat.Item3, quat.Item4);
			lerpSpeed = 9f;
		}


		public void MoveDelta(float x, float y, float ratio, float lerpSpeed)
		{
			yaw += -MathUtil.DegreesToRadians(x) * ratio;
			pitch += -MathUtil.DegreesToRadians(y) * ratio;

			pitch = MathUtil.Clamp(pitch, (float)-Math.PI / 2f, (float)Math.PI / 2f);
			Quaternion q1 = Quaternion.RotationYawPitchRoll(yaw, 0, 0);
			Quaternion q2 = Quaternion.RotationYawPitchRoll(0, pitch, 0);
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
			MediaDecoder.Instance.OnContentChanged -= ContentChanged;

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
			var currentFrameTime = (float)sceneTime.TotalMilliseconds * 0.001f;
			if (lastFrameTime == 0) lastFrameTime = currentFrameTime;
			deltaTime = currentFrameTime - lastFrameTime;
			lastFrameTime = currentFrameTime;

			currentRotationQuaternion = Quaternion.Lerp(currentRotationQuaternion, targetRotationQuaternion, lerpSpeed * deltaTime);
			if (UseVrLook)
				currentRotationQuaternion = headsetLookRotation;

			currentOffset = Lerp(currentOffset, littlePlanet ? -3f : 0f, deltaTime * 3f);

			viewMatrix = Matrix.RotationQuaternion(currentRotationQuaternion);
			viewMatrix *= Matrix.Translation(0, 0, currentOffset);

			Vector3 lookUp = Vector3.Transform(Vector3.Up, viewMatrix).ToVector3();
			Vector3 lookAt = Vector3.Transform(Vector3.ForwardRH, viewMatrix).ToVector3();

			log.Publish("forward", lookAt.ToString("0.00"));
			log.Publish("up", lookUp.ToString("0.00"));
			log.Publish("vr_quat", headsetLookRotation);
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
					if(vYaw != 0 || vPitch != 0)
						MoveDelta(velocity * vYaw * deltaTime, velocity * vPitch * deltaTime, 1, 4);


					if (Logic.Instance.settings.SpaceNavigatorKeysAndZoomActive)
					{
						if (navigatorInput.leftPressed)
							ShellViewModel.Instance.PlayPause();

						if (navigatorInput.rightPressed)
							ShellViewModel.Instance.Rewind();

						float zoom = navigatorInput.vPush;
						if(zoom != 0)
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
				if(requestContentCallback(this))
					requestContent = false;
			}

            bgCustomRotation = Quaternion.Lerp(bgCustomRotation, Quaternion.RotationYawPitchRoll((float)Math.PI-yaw, 0, 0), deltaTime);

            //*Matrix.RotationQuaternion(Quaternion.Invert(bgCustomRotation)) *
            var pos = Vector3.ForwardLH * 1.5f + Vector3.Down * 0.5f;
            var bgWorldMatrix = Matrix.Scaling(Vector3.One) 
                * Matrix.RotationQuaternion(Quaternion.Invert(bgCustomRotation)) 
                * Matrix.Translation(pos) 
                * Matrix.RotationQuaternion(bgCustomRotation);
            SharpDX.Toolkit.Graphics.Effect bgCustomEffect = bgCustomEffectSource.Get(graphicsDevice);
            bgCustomEffect.Parameters["WorldViewProj"].SetValue(bgWorldMatrix * viewMatrix * projectionMatrix);


            lock (localCritical)
			{
				// requiestContentCallback should ensure that primitive is set at this point
				primitive?.Draw(customEffect);
                bgPrimitive?.Draw(bgCustomEffect);
			}
		}

        Quaternion bgCustomRotation = Quaternion.Identity;


        private Matrix projectionMatrix;
		private Matrix worldMatrix;
		private Matrix viewMatrix;

		private float Lerp(float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
		}


        #region IContentUpdatableFromMediaEngine


        SharpDX.Toolkit.Graphics.Texture2D mainTexture;
        void IContentUpdatableFromMediaEngine.ReceiveTextures(Texture2D textureL, Texture2D textureR)
		{
			if (MediaDecoder.Instance.TextureReleased) return;

            lock (localCritical)
            {
                localVideoTexture?.Dispose();
                localVideoTexture = null;

                mainTexture?.Dispose();
                mainTexture = null;

                using (var resource = textureL.QueryInterface<SharpDX.DXGI.Resource>())
                using (var sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle))
                {
                    mainTexture = SharpDX.Toolkit.Graphics.Texture2D.New(graphicsDevice, sharedTex);
                };

                customEffect.Parameters["UserTex"].SetResource(mainTexture);
                customEffect.Parameters["gammaFactor"].SetValue(1f);
                customEffect.CurrentTechnique = customEffect.Techniques["ColorTechnique"];
                customEffect.CurrentTechnique.Passes[0].Apply();
            }


            bgCustomEffectSource.InitAction = (e, gd) =>
            {
                lock (localCritical)
                {
                    if (mainTexture == null)
                    {
                        log.Error("Custom effect source did not receive main texture");
                        return;
                    }

                    e.Parameters["UserTex"].SetResource(mainTexture);
                    e.Parameters["gammaFactor"].SetValue(1f);
                    e.CurrentTechnique = e.Techniques["ColorTechnique"];
                    e.CurrentTechnique.Passes[0].Apply();
                }
            };

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


            bgCustomEffectSource.InitAction = (e, gd) =>
            {
                lock (localCritical)
                {
                    if (localVideoTexture == null) return;

                    using (var resource = localVideoTexture.QueryInterface<SharpDX.DXGI.Resource>())
                    using (var sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle))
                    {
                        e.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(gd, sharedTex));
                        e.Parameters["gammaFactor"].SetValue(1f);
                        e.CurrentTechnique = e.Techniques["ColorTechnique"];
                        e.CurrentTechnique.Passes[0].Apply();
                    }
                }
            };

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

		void IContentUpdatableFromMediaEngine.SetProjection(ProjectionMode projection)
		{
			lock(localCritical)
			{
				primitive?.Dispose();
				primitive = GraphicTools.CreateGeometry(projection, graphicsDevice);
				projectionMode = projection;
			};
		}

		#endregion
	}
}
