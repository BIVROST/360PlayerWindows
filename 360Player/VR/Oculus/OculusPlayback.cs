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
using Bivrost.Bivrost360Player.Tools;
using Bivrost.AnalyticsForVR;

namespace Bivrost.Bivrost360Player.Oculus
{
	public class OculusPlayback : Headset
	{
        const byte OculusFOV = 75;


        override public bool IsPresent()
		{
			if (Lock)
				return true;

			using (Wrap oculus = new Wrap()) {
				bool success = oculus.Initialize(initializationParameters);

				if (!success)
					return false;

				else
				{
					var result = oculus.Detect(1000);
					bool detected = result.IsOculusHMDConnected == 1 && result.IsOculusServiceRunning == 1;
					return detected;
				}
			}
		}


		protected override float Gamma { get { return 2.2f; } }


		private OVRTypes.InitParams initializationParameters;
		public OculusPlayback()
		{
			log = new Log.Logger("Oculus");

			initializationParameters = new OVRTypes.InitParams()
			{
				LogCallback = (_, level, msg) => 
				{
					switch(level)
					{
						case OVRTypes.LogLevel.Info:
						case OVRTypes.LogLevel.Debug:
							log.Info(msg);
							break;

						case OVRTypes.LogLevel.Error:
							log.Error(msg);
							break;
					}
				}
			};
			initializationParameters.Flags |= OVRTypes.InitFlags.RequestVersion;
			initializationParameters.Flags |= OVRTypes.InitFlags.MixedRendering;
	}

	#region ILookProvider properties
		public override event Action<Vector3, Quaternion, float> ProvideLook;
		public override string DescribeType { get { return "Oculus"; } }
	#endregion

		override protected void Render()
		{
			Lock = true;

			using (Wrap oculus = new Wrap())
			{
				// Initialize the Oculus runtime.
				if (!oculus.Initialize(initializationParameters))
					throw new HeadsetError("Failed to initialize the Oculus runtime library.");

				OVRTypes.GraphicsLuid graphicsLuid;

				// Create a set of layers to submit.
				EyeTexture[] eyeTextures = new EyeTexture[2];

				// Create a depth buffer, using the same width and height as the back buffer.
				Texture2DDescription depthBufferDescription = new Texture2DDescription()
				{
					Format = Format.D32_Float,
					ArraySize = 1,
					MipLevels = 1,
					Width = 1920,    // TODO: FIXME?
					Height = 1080,
					SampleDescription = new SampleDescription(1, 0),
					Usage = ResourceUsage.Default,
					BindFlags = BindFlags.DepthStencil,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None
				};

				// Define how the depth buffer will be used to filter out objects, based on their distance from the viewer.
				DepthStencilStateDescription depthStencilStateDescription = new DepthStencilStateDescription()
				{
					IsDepthEnabled = true,
					DepthComparison = Comparison.Less,
					DepthWriteMask = DepthWriteMask.Zero
				};

				//#if DEBUG
				//				SharpDX.Configuration.EnableObjectTracking = true;
				//#endif

				
				using (Hmd hmd = oculus.Hmd_Create(out graphicsLuid))
				// Create DirectX drawing device.
				using (_device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 }))
				// Create DirectX Graphics Interface factory, used to create the swap chain.
				using (Factory factory = new Factory())
				using (DeviceContext immediateContext = _device.ImmediateContext)
				// Create the depth buffer.
				using (Texture2D depthBuffer = new Texture2D(_device, depthBufferDescription))
				using (DepthStencilView depthStencilView = new DepthStencilView(_device, depthBuffer))
				using (DepthStencilState depthStencilState = new DepthStencilState(_device, depthStencilStateDescription))
				using (Layers layers = new Layers())
				using (_gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(_device))
				using (vrui = new VRUI(_device, _gd))
				using (customEffectL = GetCustomEffect(_gd))
				using (customEffectR = GetCustomEffect(_gd))
				//using (SharpDX.Toolkit.Graphics.GeometricPrimitive primitive = GraphicTools.CreateGeometry(_projection, _gd, false))
				{
					if (hmd == null)
						throw new HeadsetError("Oculus Rift not detected.");
					if (hmd.ProductName == string.Empty)
						throw new HeadsetError("The HMD is not enabled.");

					Viewport viewport = new Viewport(0, 0, hmd.Resolution.Width, hmd.Resolution.Height, 0.0f, 1.0f);
					LayerEyeFov layerEyeFov = layers.AddLayerEyeFov();

					// Retrieve the DXGI device, in order to set the maximum frame latency.
					using (SharpDX.DXGI.Device1 dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device1>())
					{
						dxgiDevice.MaximumFrameLatency = 1;
					}

					for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
					{
						OVRTypes.EyeType eye = (OVRTypes.EyeType)eyeIndex;
						var textureSize = hmd.GetFovTextureSize(eye, hmd.DefaultEyeFov[eyeIndex], 1.0f);
						var renderDescription = hmd.GetRenderDesc(eye, hmd.DefaultEyeFov[eyeIndex]);
						EyeTexture eyeTexture = eyeTextures[eyeIndex] = new EyeTexture()
						{
							// Retrieve size and position of the texture for the current eye.
							FieldOfView = hmd.DefaultEyeFov[eyeIndex],
							TextureSize = textureSize,
							RenderDescription = renderDescription,
							// Define a texture at the size recommended for the eye texture.
							Viewport = new Viewport(0, 0, textureSize.Width, textureSize.Height, 0.0f, 1.0f),
							HmdToEyeViewOffset = renderDescription.HmdToEyeOffset,
							Texture2DDescription = new Texture2DDescription()
							{
								Width = textureSize.Width,
								Height = textureSize.Height,
								ArraySize = 1,
								MipLevels = 1,
								Format = Format.R8G8B8A8_UNorm_SRgb,
								SampleDescription = new SampleDescription(1, 0),
								Usage = ResourceUsage.Default,
								CpuAccessFlags = CpuAccessFlags.None,
								BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget
							}
						};
						eyeTexture.ViewportSize.Position = new OVRTypes.Vector2i(0, 0);
						eyeTexture.ViewportSize.Size = textureSize;


						// Convert the SharpDX texture description to the native Direct3D texture description.
						OVRTypes.TextureSwapChainDesc textureSwapChainDesc = SharpDXHelpers.CreateTextureSwapChainDescription(eyeTexture.Texture2DDescription);

						AssertSuccess(hmd.CreateTextureSwapChainDX(_device.NativePointer, textureSwapChainDesc, out eyeTexture.SwapTextureSet),
							oculus, "Failed to create swap chain.");

						// Retrieve the number of buffers of the created swap chain.
						int textureSwapChainBufferCount;
						AssertSuccess(eyeTexture.SwapTextureSet.GetLength(out textureSwapChainBufferCount),
							oculus, "Failed to retrieve the number of buffers of the created swap chain.");


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
							AssertSuccess(eyeTexture.SwapTextureSet.GetBufferDX(textureIndex, textureInterfaceId, out swapChainTextureComPtr),
								oculus, "Failed to retrieve a texture from the created swap chain.");

							// Create a managed Texture2D, based on the unmanaged texture pointer.
							eyeTexture.Textures[textureIndex] = new Texture2D(swapChainTextureComPtr);

							// Create a render target view for the current Texture2D.
							eyeTexture.RenderTargetViews[textureIndex] = new RenderTargetView(_device, eyeTexture.Textures[textureIndex]);
						}

						// Define the depth buffer, at the size recommended for the eye texture.
						eyeTexture.DepthBufferDescription = new Texture2DDescription()
						{
							Format = Format.D32_Float,
							Width = eyeTexture.TextureSize.Width,
							Height = eyeTexture.TextureSize.Height,
							ArraySize = 1,
							MipLevels = 1,
							SampleDescription = new SampleDescription(1, 0),
							Usage = ResourceUsage.Default,
							BindFlags = BindFlags.DepthStencil,
							CpuAccessFlags = CpuAccessFlags.None,
							OptionFlags = ResourceOptionFlags.None
						};

						// Create the depth buffer.
						eyeTexture.DepthBuffer = new Texture2D(_device, eyeTexture.DepthBufferDescription);
						eyeTexture.DepthStencilView = new DepthStencilView(_device, eyeTexture.DepthBuffer);

						// Specify the texture to show on the HMD.
						layerEyeFov.ColorTexture[eyeIndex] = eyeTexture.SwapTextureSet.TextureSwapChainPtr;
						layerEyeFov.Viewport[eyeIndex].Position = new OVRTypes.Vector2i(0, 0);
						layerEyeFov.Viewport[eyeIndex].Size = eyeTexture.TextureSize;
						layerEyeFov.Fov[eyeIndex] = eyeTexture.FieldOfView;
						layerEyeFov.Header.Flags = OVRTypes.LayerFlags.HighQuality;
					}

					#region Render loop
					DateTime startTime = DateTime.Now;
					DateTime lastTime = DateTime.Now;
					float deltaTime = 0;


					while (!abort)
					{
						UpdateContentIfRequested();

						OVRTypes.Vector3f[] hmdToEyeViewOffsets = { eyeTextures[0].HmdToEyeViewOffset, eyeTextures[1].HmdToEyeViewOffset };
						//OVR.FrameTiming frameTiming = hmd.GetFrameTiming(0);
						//OVR.TrackingState trackingState = hmd.GetTrackingState(frameTiming.DisplayMidpointSeconds);
						double displayMidpoint = hmd.GetPredictedDisplayTime(0);
						OVRTypes.TrackingState trackingState = hmd.GetTrackingState(displayMidpoint, true);
						OVRTypes.Posef[] eyePoses = new OVRTypes.Posef[2];

						// Calculate the position and orientation of each eye.
						oculus.CalcEyePoses(trackingState.HeadPose.ThePose, hmdToEyeViewOffsets, ref eyePoses);
						
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
							AssertSuccess(eyeTexture.SwapTextureSet.GetCurrentIndex(out textureIndex),
								oculus, "Failed to retrieve texture swap chain current index.");

							immediateContext.OutputMerger.SetRenderTargets(eyeTexture.DepthStencilView, eyeTexture.RenderTargetViews[textureIndex]);
							immediateContext.ClearRenderTargetView(eyeTexture.RenderTargetViews[textureIndex], Color.Black);
							immediateContext.ClearDepthStencilView(eyeTexture.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
							immediateContext.Rasterizer.SetViewport(eyeTexture.Viewport);



							// Retrieve the eye rotation quaternion and use it to calculate the LookAt direction and the LookUp direction.
							Quaternion lookRotation = SharpDXHelpers.ToQuaternion(eyePoses[eyeIndex].Orientation);
							lookRotation = new Quaternion(1, 0, 0, 0) * lookRotation;
							Matrix rotationMatrix = Matrix.RotationQuaternion(lookRotation);
							Vector3 lookUp = Vector3.Transform(new Vector3(0, -1, 0), rotationMatrix).ToVector3();
							Vector3 lookAt = Vector3.Transform(new Vector3(0, 0, 1), rotationMatrix).ToVector3();

							//Vector3 eyeDiff = eyePoses[eyeIndex].Position.ToVector3() - eyePoses[1 - eyeIndex].Position.ToVector3();
							Vector3 lookPosition = new Vector3(
								-eyePoses[eyeIndex].Position.X,
								eyePoses[eyeIndex].Position.Y,
								eyePoses[eyeIndex].Position.Z
							);

							Matrix worldMatrix = Matrix.Translation(lookPosition);

							Matrix viewMatrix = Matrix.LookAtLH(lookPosition, lookPosition + lookAt, lookUp);

							Matrix projectionMatrix = oculus.Matrix4f_Projection(eyeTexture.FieldOfView, 0.1f, 100.0f, OVRTypes.ProjectionModifier.LeftHanded).ToMatrix();
							projectionMatrix.Transpose();

							Matrix MVP = worldMatrix * viewMatrix * projectionMatrix;
							customEffectL.Parameters["WorldViewProj"].SetValue(MVP);
							customEffectR.Parameters["WorldViewProj"].SetValue(MVP);

							lock (localCritical)
							{
								if (eyeIndex == 0)
									primitive?.Draw(customEffectL);
								if (eyeIndex == 1)
									primitive?.Draw(customEffectR);
							}

							if (ProvideLook != null && eyeIndex == 0)
							{
                                lookRotation.Invert();
                                lookRotation = lookRotation * new Quaternion(1, 0, 0, 0);   // rotate 180 in x

                                Vector3 forward = Vector3.Transform(Vector3.ForwardRH, lookRotation);
                                Vector3 up = Vector3.Transform(Vector3.Up, lookRotation);

								log.Publish("oculus.forward", forward.ToString("0.00"));
								log.Publish("oculus.up", up.ToString("0.00"));
								log.Publish("oculus.lookAt", lookAt.ToString("0.00"));
								log.Publish("oculus.lookUp", lookUp.ToString("0.00"));
								log.Publish("oculus.vr_quat", lookRotation);
								log.Publish("q.sent", lookRotation);

								ProvideLook(lookPosition, lookRotation, OculusFOV);
							}

							// reset UI position every frame if it is not visible
							if (vrui.isUIHidden)
								vrui.SetWorldPosition(viewMatrix.Forward, lookPosition, false);


							vrui.Draw(Media, currentTime, Duration);
							vrui.Render(deltaTime, viewMatrix, projectionMatrix, lookPosition, ShouldShowVRUI);

							// Commits any pending changes to the TextureSwapChain, and advances its current index
							AssertSuccess(eyeTexture.SwapTextureSet.Commit(), oculus, "Failed to commit the swap chain texture.");

							//Console.WriteLine("xbox: " + ((hmd.ovr_GetConnectedControllerTypes() & OVRTypes.ControllerType.XBox) != 0));
							//Console.WriteLine("remote: " + ((hmd.ovr_GetConnectedControllerTypes() & OVRTypes.ControllerType.Remote) != 0));
							//Console.WriteLine("active: " + hmd.GetInputState(OVRTypes.ControllerType.Active));
							//Console.WriteLine("buttons: " + hmd.GetInputState(OVRTypes.ControllerType.Remote).Buttons);
						}

						hmd.SubmitFrame(0, layers);
					}

					#endregion
					//debugWindow.Stop();

					waitForRendererStop.Set();

					// Release all resources
					primitive.Dispose();
					eyeTextures[0].Dispose();
					eyeTextures[1].Dispose();
					immediateContext.ClearState();
					immediateContext.Flush();
				}
			}

			Lock = false;
		}	



		void AssertSuccess(OVRTypes.Result result, Wrap oculus, string message)
		{
			if (result >= OVRTypes.Result.Success)
				return;

			// Retrieve the error message from the last occurring error.
			OVRTypes.ErrorInfo errorInformation = oculus.GetLastError();

			string formattedMessage = string.Format("{0}. Message: {1} (Error code={2})", message, errorInformation.ErrorString, errorInformation.Result);
			Trace.WriteLine(formattedMessage);

			throw new HeadsetError(formattedMessage);
		}

	}
}
