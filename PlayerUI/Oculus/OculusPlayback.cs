using OculusWrap;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Device = SharpDX.Direct3D11.Device;
using PlayerUI.Tools;

namespace PlayerUI.Oculus
{
	public class OculusPlayback : Headset
	{


		override public void Start()
		{
			abort = false;
			pause = false;
			waitForRendererStop.Reset();
			if (Lock)
				return;
			Task.Factory.StartNew(() =>
			{
				try
				{
					Render();
				}
				catch (Exception exc) {
					Console.WriteLine("[EXC] " + exc.Message);
				}
			});
		}


		override public bool IsPresent()
		{
			if (Lock) return true;
			Wrap oculus = new Wrap();
			try {
				bool success = oculus.Initialize();
			
				if (!success)
				{
					oculus.Dispose();
					return false;
				} else
				{
					var result = oculus.Detect(1000);                
					oculus.Dispose();
					bool detected = result.IsOculusHMDConnected == 1 && result.IsOculusServiceRunning == 1;
					return detected;
				}
			}
			catch (Exception exc)
			{
				oculus.Dispose();
				return false;
			}
		}


		SharpDX.Toolkit.Graphics.GraphicsDevice _gd;
		Device _device;

		void ResizeTexture(Texture2D tL, Texture2D tR)
		{
			if (MediaDecoder.Instance.TextureReleased) return;

			var tempL = textureL;
			var tempR = textureR;

			lock (localCritical)
			{
				basicEffectL.Texture?.Dispose();
				textureL = tL;

				if (_stereoVideo)
				{
					basicEffectR.Texture?.Dispose();
					textureR = tR;
				}



				var resourceL = textureL.QueryInterface<SharpDX.DXGI.Resource>();
				var sharedTexL = _device.OpenSharedResource<Texture2D>(resourceL.SharedHandle);
				basicEffectL.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL);
				resourceL?.Dispose();
				sharedTexL?.Dispose();

				if (_stereoVideo)
				{
					var resourceR = textureR.QueryInterface<SharpDX.DXGI.Resource>();
					var sharedTexR = _device.OpenSharedResource<Texture2D>(resourceR.SharedHandle);
					basicEffectR.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR);
					resourceR?.Dispose();
					sharedTexR?.Dispose();
				}
				//_device.ImmediateContext.Flush();
			}

		}

		private void Render()
		{
			Lock = true;

			Wrap oculus = new Wrap();
			Hmd hmd;

			// Initialize the Oculus runtime.
			bool success = oculus.Initialize();
			if (!success)
			{
				System.Windows.Forms.MessageBox.Show("Failed to initialize the Oculus runtime library.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

            OVRTypes.GraphicsLuid graphicsLuid;
            hmd = oculus.Hmd_Create(out graphicsLuid);

			if (hmd == null)
			{
				System.Windows.Forms.MessageBox.Show("Oculus Rift not detected.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (hmd.ProductName == string.Empty)
				System.Windows.Forms.MessageBox.Show("The HMD is not enabled.", "There's a tear in the Rift", MessageBoxButtons.OK, MessageBoxIcon.Error);

			// Create a set of layers to submit.
			EyeTexture[] eyeTextures = new EyeTexture[2];
            OVRTypes.Result result;

			// Create DirectX drawing device.
			SharpDX.Direct3D11.Device device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 });

			// Create DirectX Graphics Interface factory, used to create the swap chain.
			Factory factory = new Factory();

			DeviceContext immediateContext = device.ImmediateContext;

			// Create a depth buffer, using the same width and height as the back buffer.
			Texture2DDescription depthBufferDescription = new Texture2DDescription();
			depthBufferDescription.Format = Format.D32_Float;
			depthBufferDescription.ArraySize = 1;
			depthBufferDescription.MipLevels = 1;
			depthBufferDescription.Width = 1920;
			depthBufferDescription.Height = 1080;
			depthBufferDescription.SampleDescription = new SampleDescription(1, 0);
			depthBufferDescription.Usage = ResourceUsage.Default;
			depthBufferDescription.BindFlags = BindFlags.DepthStencil;
			depthBufferDescription.CpuAccessFlags = CpuAccessFlags.None;
			depthBufferDescription.OptionFlags = ResourceOptionFlags.None;

			// Define how the depth buffer will be used to filter out objects, based on their distance from the viewer.
			DepthStencilStateDescription depthStencilStateDescription = new DepthStencilStateDescription();
			depthStencilStateDescription.IsDepthEnabled = true;
			depthStencilStateDescription.DepthComparison = Comparison.Less;
			depthStencilStateDescription.DepthWriteMask = DepthWriteMask.Zero;

			// Create the depth buffer.
			Texture2D depthBuffer = new Texture2D(device, depthBufferDescription);
			DepthStencilView depthStencilView = new DepthStencilView(device, depthBuffer);
			DepthStencilState depthStencilState = new DepthStencilState(device, depthStencilStateDescription);
			Viewport viewport = new Viewport(0, 0, hmd.Resolution.Width, hmd.Resolution.Height, 0.0f, 1.0f);


			// Retrieve the DXGI device, in order to set the maximum frame latency.
			using (SharpDX.DXGI.Device1 dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device1>())
			{
				dxgiDevice.MaximumFrameLatency = 1;
			}

			Layers layers = new Layers();
			LayerEyeFov layerEyeFov = layers.AddLayerEyeFov();

			for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
			{
                OVRTypes.EyeType eye = (OVRTypes.EyeType)eyeIndex;
				EyeTexture eyeTexture = new EyeTexture();
				eyeTextures[eyeIndex] = eyeTexture;

				// Retrieve size and position of the texture for the current eye.
				eyeTexture.FieldOfView = hmd.DefaultEyeFov[eyeIndex];
				eyeTexture.TextureSize = hmd.GetFovTextureSize(eye, hmd.DefaultEyeFov[eyeIndex], 1.0f);
				eyeTexture.RenderDescription = hmd.GetRenderDesc(eye, hmd.DefaultEyeFov[eyeIndex]);
				eyeTexture.HmdToEyeViewOffset = eyeTexture.RenderDescription.HmdToEyeOffset; 
				eyeTexture.ViewportSize.Position = new OVRTypes.Vector2i(0, 0);
				eyeTexture.ViewportSize.Size = eyeTexture.TextureSize;
				eyeTexture.Viewport = new Viewport(0, 0, eyeTexture.TextureSize.Width, eyeTexture.TextureSize.Height, 0.0f, 1.0f);

				// Define a texture at the size recommended for the eye texture.
				eyeTexture.Texture2DDescription = new Texture2DDescription();
				eyeTexture.Texture2DDescription.Width = eyeTexture.TextureSize.Width;
				eyeTexture.Texture2DDescription.Height = eyeTexture.TextureSize.Height;
				eyeTexture.Texture2DDescription.ArraySize = 1;
				eyeTexture.Texture2DDescription.MipLevels = 1;
				eyeTexture.Texture2DDescription.Format = Format.R8G8B8A8_UNorm;
				eyeTexture.Texture2DDescription.SampleDescription = new SampleDescription(1, 0);
				eyeTexture.Texture2DDescription.Usage = ResourceUsage.Default;
				eyeTexture.Texture2DDescription.CpuAccessFlags = CpuAccessFlags.None;
				eyeTexture.Texture2DDescription.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;

				// Convert the SharpDX texture description to the native Direct3D texture description.
				OVRTypes.TextureSwapChainDesc textureSwapChainDesc = SharpDXHelpers.CreateTextureSwapChainDescription(eyeTexture.Texture2DDescription);

				result = hmd.CreateTextureSwapChainDX(device.NativePointer, textureSwapChainDesc, out eyeTexture.SwapTextureSet);
				WriteErrorDetails(oculus, result, "Failed to create swap chain.");

				// Retrieve the number of buffers of the created swap chain.
				int textureSwapChainBufferCount;
				result = eyeTexture.SwapTextureSet.GetLength(out textureSwapChainBufferCount);
				WriteErrorDetails(oculus, result, "Failed to retrieve the number of buffers of the created swap chain.");


				// Create room for each DirectX texture in the SwapTextureSet.
				eyeTexture.Textures = new Texture2D[textureSwapChainBufferCount];
				eyeTexture.RenderTargetViews = new RenderTargetView[textureSwapChainBufferCount];

				// Create a texture 2D and a render target view, for each unmanaged texture contained in the SwapTextureSet.
				for (int textureIndex = 0; textureIndex < textureSwapChainBufferCount; textureIndex++)
				{
					// Interface ID of the Direct3D Texture2D interface.
					Guid textureInterfaceId = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c"); 

					// Retrieve the Direct3D texture contained in the Oculus TextureSwapChainBuffer.
					IntPtr swapChainTextureComPtr = IntPtr.Zero;
					result = eyeTexture.SwapTextureSet.GetBufferDX(textureIndex, textureInterfaceId, out swapChainTextureComPtr);
					WriteErrorDetails(oculus, result, "Failed to retrieve a texture from the created swap chain.");


					// Create a managed Texture2D, based on the unmanaged texture pointer.
					eyeTexture.Textures[textureIndex] = new Texture2D(swapChainTextureComPtr);

					// Create a render target view for the current Texture2D.
					eyeTexture.RenderTargetViews[textureIndex] = new RenderTargetView(device, eyeTexture.Textures[textureIndex]);
				}

				// Define the depth buffer, at the size recommended for the eye texture.
				eyeTexture.DepthBufferDescription = new Texture2DDescription();
				eyeTexture.DepthBufferDescription.Format = Format.D32_Float;
				eyeTexture.DepthBufferDescription.Width = eyeTexture.TextureSize.Width;
				eyeTexture.DepthBufferDescription.Height = eyeTexture.TextureSize.Height;
				eyeTexture.DepthBufferDescription.ArraySize = 1;
				eyeTexture.DepthBufferDescription.MipLevels = 1;
				eyeTexture.DepthBufferDescription.SampleDescription = new SampleDescription(1, 0);
				eyeTexture.DepthBufferDescription.Usage = ResourceUsage.Default;
				eyeTexture.DepthBufferDescription.BindFlags = BindFlags.DepthStencil;
				eyeTexture.DepthBufferDescription.CpuAccessFlags = CpuAccessFlags.None;
				eyeTexture.DepthBufferDescription.OptionFlags = ResourceOptionFlags.None;

				// Create the depth buffer.
				eyeTexture.DepthBuffer = new Texture2D(device, eyeTexture.DepthBufferDescription);
				eyeTexture.DepthStencilView = new DepthStencilView(device, eyeTexture.DepthBuffer);

				// Specify the texture to show on the HMD.
				layerEyeFov.ColorTexture[eyeIndex] = eyeTexture.SwapTextureSet.TextureSwapChainPtr;
				layerEyeFov.Viewport[eyeIndex].Position = new OVRTypes.Vector2i(0, 0);
				layerEyeFov.Viewport[eyeIndex].Size = eyeTexture.TextureSize;
				layerEyeFov.Fov[eyeIndex] = eyeTexture.FieldOfView;
				layerEyeFov.Header.Flags = OVRTypes.LayerFlags.HighQuality;
			}

			#region Rendering primitives and resources

			SharpDX.Toolkit.Graphics.GraphicsDevice gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(device);

			_device = device;
			_gd = gd;

			MediaDecoder.Instance.OnFormatChanged += ResizeTexture;

			//var resourceL = textureL.QueryInterface<SharpDX.DXGI.Resource>();
			//var sharedTexL = device.OpenSharedResource<Texture2D>(resourceL.SharedHandle);

			basicEffectL = new SharpDX.Toolkit.Graphics.BasicEffect(gd);
			basicEffectL.PreferPerPixelLighting = false;
			//basicEffectL.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, sharedTexL);

			basicEffectL.TextureEnabled = true;
			basicEffectL.LightingEnabled = false;
			basicEffectL.Sampler = gd.SamplerStates.AnisotropicClamp;
			//basicEffectL.DiffuseColor = new Vector4(1f, 0f, 0f, 0f);
			

			if (_stereoVideo)
			{
				//var resourceR = textureR.QueryInterface<SharpDX.DXGI.Resource>();
				//var sharedTexR = device.OpenSharedResource<Texture2D>(resourceR.SharedHandle);

				basicEffectR = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

				basicEffectR.PreferPerPixelLighting = false;
				//basicEffectR.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, sharedTexR);

				basicEffectR.TextureEnabled = true;
				basicEffectR.LightingEnabled = false;
				basicEffectR.Sampler = gd.SamplerStates.AnisotropicClamp;
			}

			ResizeTexture(MediaDecoder.Instance.TextureL, _stereoVideo ? MediaDecoder.Instance.TextureR : MediaDecoder.Instance.TextureL);

			//var primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Sphere.New(gd, radius, 32, true);
			var primitive = GraphicTools.CreateGeometry(_projection, gd, false);


			// UI Rendering
			vrui = new VRUI(device, gd);
			vrui.Draw(movieTitle, currentTime, duration);
			#endregion



			#region Render loop
			DateTime startTime = DateTime.Now;
			DateTime lastTime = DateTime.Now;
			float deltaTime = 0;


			while (!abort)
			{
                OVRTypes.Vector3f[] hmdToEyeViewOffsets = { eyeTextures[0].HmdToEyeViewOffset, eyeTextures[1].HmdToEyeViewOffset };
                //OVR.FrameTiming frameTiming = hmd.GetFrameTiming(0);
                //OVR.TrackingState trackingState = hmd.GetTrackingState(frameTiming.DisplayMidpointSeconds);
                double displayMidpoint = hmd.GetPredictedDisplayTime(0);
                OVRTypes.TrackingState trackingState = hmd.GetTrackingState(displayMidpoint, true);
                OVRTypes.Posef[] eyePoses = new OVRTypes.Posef[2];

				// Calculate the position and orientation of each eye.
				oculus.CalcEyePoses(trackingState.HeadPose.ThePose, hmdToEyeViewOffsets, ref eyePoses);

				// rotation quaternion to heatmap directions
				ShellViewModel.Instance.ClearDebugText();
				Vector2 v = GraphicTools.QuaternionToYawPitch(trackingState.HeadPose.ThePose.Orientation);
				var yawdeg = MathUtil.RadiansToDegrees(v.X);
				var pitchdeg = MathUtil.RadiansToDegrees(v.Y);
				ShellViewModel.Instance.AppendDebugText($"YAW:{yawdeg} \t\t PITCH:{pitchdeg}");
				ShellViewModel.Instance.UpdateDebugText();
				////==========================================

				float timeSinceStart = (float)(DateTime.Now - startTime).TotalSeconds;
				deltaTime = (float)(DateTime.Now - lastTime).TotalSeconds;
				lastTime = DateTime.Now;

				Vector3 centerEye = (eyePoses[0].Position.ToVector3() + eyePoses[1].Position.ToVector3()) * 0.5f;

				for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
				{
                    OVRTypes.EyeType eye = (OVRTypes.EyeType)eyeIndex;
					EyeTexture eyeTexture = eyeTextures[eyeIndex];

					layerEyeFov.RenderPose[eyeIndex] = eyePoses[eyeIndex];

					// Update the render description at each frame, as the HmdToEyeOffset can change at runtime.
					eyeTexture.RenderDescription = hmd.GetRenderDesc(eye, hmd.DefaultEyeFov[eyeIndex]);

					// Retrieve the index of the active texture
					int textureIndex;
					result = eyeTexture.SwapTextureSet.GetCurrentIndex(out textureIndex);
					WriteErrorDetails(oculus, result, "Failed to retrieve texture swap chain current index.");

					immediateContext.OutputMerger.SetRenderTargets(eyeTexture.DepthStencilView, eyeTexture.RenderTargetViews[textureIndex]);
					immediateContext.ClearRenderTargetView(eyeTexture.RenderTargetViews[textureIndex], Color.Black);
					immediateContext.ClearDepthStencilView(eyeTexture.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
					immediateContext.Rasterizer.SetViewport(eyeTexture.Viewport);



					// Retrieve the eye rotation quaternion and use it to calculate the LookAt direction and the LookUp direction.
					Quaternion rotationQuaternion = SharpDXHelpers.ToQuaternion(eyePoses[eyeIndex].Orientation);
					rotationQuaternion = new Quaternion(1, 0, 0, 0) * rotationQuaternion;
					Matrix rotationMatrix = Matrix.RotationQuaternion(rotationQuaternion);
					Vector3 lookUp = Vector3.Transform(new Vector3(0, -1, 0), rotationMatrix).ToVector3();
					Vector3 lookAt = Vector3.Transform(new Vector3(0, 0, 1), rotationMatrix).ToVector3();

					//Vector3 eyeDiff = eyePoses[eyeIndex].Position.ToVector3() - eyePoses[1 - eyeIndex].Position.ToVector3();
					Vector3 viewPosition = new Vector3(
						-eyePoses[eyeIndex].Position.X,
						eyePoses[eyeIndex].Position.Y,
						eyePoses[eyeIndex].Position.Z
					);

					Matrix viewMatrix = Matrix.LookAtLH(viewPosition, viewPosition + lookAt, lookUp);

					//Vector3 vmvp = viewMatrix.TranslationVector;

					Matrix projectionMatrix = oculus.Matrix4f_Projection(eyeTexture.FieldOfView, 0.1f, 100.0f, OVRTypes.ProjectionModifier.LeftHanded).ToMatrix();
					projectionMatrix.Transpose();

					//float fov = eyeTexture.FieldOfView.LeftTan + eyeTexture.FieldOfView.RightTan;
					//double a = Math.Atan(eyeTexture.FieldOfView.LeftTan) * 180f / Math.PI + Math.Atan(eyeTexture.FieldOfView.RightTan) * 180f / Math.PI; ;
					////float fov2 = (float)(a * Math.PI / 180f);
					//float fov2 = (float)(102.57f * Math.PI / 180f);

					//Matrix worldViewProjection = world * viewMatrix * projectionMatrix;
					//worldViewProjection.Transpose();

					basicEffectL.World = Matrix.Translation(viewPosition); //Matrix.Identity;
					basicEffectL.View = viewMatrix;
					basicEffectL.Projection = projectionMatrix;

					if (_stereoVideo)
					{
						basicEffectR.World = Matrix.Translation(viewPosition);
						basicEffectR.View = viewMatrix;
						basicEffectR.Projection = projectionMatrix;
					}
					lock (localCritical)
					{
						if (_stereoVideo)
						{
							if (eyeIndex == 0)
								primitive.Draw(basicEffectL);
							if (eyeIndex == 1)
								primitive.Draw(basicEffectR);
						}
						else
							primitive.Draw(basicEffectL);
					}

					// reset UI position every frame if it is not visible
					if (vrui.isUIHidden)
						vrui.SetWorldPosition(viewMatrix.Forward, viewPosition, false);

					vrui.Draw(movieTitle, currentTime, duration);
					vrui.Render(deltaTime, viewMatrix, projectionMatrix, viewPosition, pause);

					// Commits any pending changes to the TextureSwapChain, and advances its current index
					result = eyeTexture.SwapTextureSet.Commit();
					WriteErrorDetails(oculus, result, "Failed to commit the swap chain texture.");

					//Console.WriteLine("xbox: " + ((hmd.ovr_GetConnectedControllerTypes() & OVRTypes.ControllerType.XBox) != 0));
					//Console.WriteLine("remote: " + ((hmd.ovr_GetConnectedControllerTypes() & OVRTypes.ControllerType.Remote) != 0));
					//Console.WriteLine("active: " + hmd.GetInputState(OVRTypes.ControllerType.Active));
					//Console.WriteLine("buttons: " + hmd.GetInputState(OVRTypes.ControllerType.Remote).Buttons);
				}

				hmd.SubmitFrame(0, layers);
			}

			#endregion
			//debugWindow.Stop();

			MediaDecoder.Instance.OnFormatChanged -= ResizeTexture;

			waitForRendererStop.Set();

			// Release all resources
			layers.Dispose();
			eyeTextures[0].Dispose();
			eyeTextures[1].Dispose();
			immediateContext.ClearState();
			immediateContext.Flush();
			immediateContext.Dispose();
			depthStencilState.Dispose();
			depthStencilView.Dispose();
			depthBuffer.Dispose();
			factory.Dispose();

			// Release all 2D resources
			basicEffectL.Dispose();
			if (_stereoVideo)
				basicEffectR.Dispose();

			//target2d.Dispose();
			//uiSurface.Dispose();
			//uiTexture.Dispose();			
			//factory2d.Dispose();
			vrui.Dispose();
			vrui = null;

			// Disposing the device, before the hmd, will cause the hmd to fail when disposing.
			// Disposing the device, after the hmd, will cause the dispose of the device to fail.
			// It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
			// device.Dispose();

			hmd.Dispose();
			oculus.Dispose();

			Lock = false;
		}


		void WriteErrorDetails(Wrap oculus, OVRTypes.Result result, string message)
		{
			if (result >= OVRTypes.Result.Success)
				return;

			// Retrieve the error message from the last occurring error.
			OVRTypes.ErrorInfo errorInformation = oculus.GetLastError();

			string formattedMessage = string.Format("{0}. Message: {1} (Error code={2})", message, errorInformation.ErrorString, errorInformation.Result);
			Trace.WriteLine(formattedMessage);

			throw new Exception(formattedMessage);
		}

	}
}
