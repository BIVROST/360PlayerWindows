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
		private SharpDX.Toolkit.Graphics.Effect customEffectL;
		private SharpDX.Toolkit.Graphics.Effect customEffectR;

		override public bool IsPresent()
		{
			if (Lock)
				return true;

			using (Wrap oculus = new Wrap()) {
				bool success = oculus.Initialize();

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



		override protected void ResizeTexture(Texture2D tL, Texture2D tR)
		{
			if (MediaDecoder.Instance.TextureReleased) return;

			var tempL = textureL;
			var tempR = textureR;

			lock (localCritical)
			{
				//basicEffectL.Texture?.Dispose();
				(customEffectL.Parameters["UserTex"]?.GetResource<Texture2D>())?.Dispose();
				textureL = tL;

				if (_stereoVideo)
				{
					(customEffectR.Parameters["UserTex"]?.GetResource<Texture2D>())?.Dispose();
					//basicEffectR.Texture?.Dispose();
					textureR = tR;
				}

				var resourceL = textureL.QueryInterface<SharpDX.DXGI.Resource>();
				var sharedTexL = _device.OpenSharedResource<Texture2D>(resourceL.SharedHandle);


				//basicEffectL.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL);
				customEffectL.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexL));
				customEffectL.Parameters["gammaFactor"].SetValue(1/2.2f);
				customEffectL.CurrentTechnique = customEffectL.Techniques["ColorTechnique"];
				customEffectL.CurrentTechnique.Passes[0].Apply();

				resourceL?.Dispose();
				sharedTexL?.Dispose();

				if (_stereoVideo)
				{
					var resourceR = textureR.QueryInterface<SharpDX.DXGI.Resource>();
					var sharedTexR = _device.OpenSharedResource<Texture2D>(resourceR.SharedHandle);

					//basicEffectR.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR);
					customEffectR.Parameters["UserTex"].SetResource(SharpDX.Toolkit.Graphics.Texture2D.New(_gd, sharedTexR));
					customEffectR.Parameters["gammaFactor"].SetValue(1/2.2f);
					customEffectR.CurrentTechnique = customEffectR.Techniques["ColorTechnique"];
					customEffectR.CurrentTechnique.Passes[0].Apply();

					resourceR?.Dispose();
					sharedTexR?.Dispose();
				}
				//_device.ImmediateContext.Flush();
			}

		}


		override protected void Render()
		{
			Lock = true;

			using (Wrap oculus = new Wrap())
			{
				// Initialize the Oculus runtime.
				if (!oculus.Initialize())
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

#if DEBUG
				SharpDX.Configuration.EnableObjectTracking = true;
#endif
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
				using (SharpDX.Toolkit.Graphics.GeometricPrimitive primitive = GraphicTools.CreateGeometry(_projection, _gd, false))
				{
					//SharpDX.Toolkit.Graphics.GeometricPrimitive plane;
					//using (var planeSrc = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(_gd, 2, 1, 30, true))
					//{
					//	short[] indices = planeSrc.IndexBuffer.GetData<short>();
					//	var data = planeSrc.VertexBuffer.GetData();

					//	for (int it = 0; it < data.Length; it++)
					//	{
					//		var x = (data[it].TextureCoordinate.X * 2 - 0.5f);
					//		data[it].TextureCoordinate.X = x;
					//	}
					//	//plane = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(_gd, 2, 1, 10, true);
					//	plane = new SharpDX.Toolkit.Graphics.GeometricPrimitive(_gd, data, indices);
					//}

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

					#region Rendering primitives and resources

					MediaDecoder.Instance.OnFormatChanged += ResizeTexture;


					var gammaShader = GetGammaShader();

					customEffectL = new SharpDX.Toolkit.Graphics.Effect(_gd, gammaShader.EffectData);
					customEffectL.CurrentTechnique = customEffectL.Techniques["ColorTechnique"];
					customEffectL.CurrentTechnique.Passes[0].Apply();

					if (_stereoVideo)
					{
						customEffectR = new SharpDX.Toolkit.Graphics.Effect(_gd, gammaShader.EffectData);
						customEffectR.CurrentTechnique = customEffectR.Techniques["ColorTechnique"];
						customEffectR.CurrentTechnique.Passes[0].Apply();
					}



					ResizeTexture(MediaDecoder.Instance.TextureL, _stereoVideo ? MediaDecoder.Instance.TextureR : MediaDecoder.Instance.TextureL);

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
							AssertSuccess(eyeTexture.SwapTextureSet.GetCurrentIndex(out textureIndex),
								oculus, "Failed to retrieve texture swap chain current index.");

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

							Matrix worldMatrix = Matrix.Translation(viewPosition);

							Matrix viewMatrix = Matrix.LookAtLH(viewPosition, viewPosition + lookAt, lookUp);

							Matrix projectionMatrix = oculus.Matrix4f_Projection(eyeTexture.FieldOfView, 0.1f, 100.0f, OVRTypes.ProjectionModifier.LeftHanded).ToMatrix();
							projectionMatrix.Transpose();

							////// DEBUG PLANE
							//customEffectL.Parameters["WorldViewProj"].SetValue(Matrix.Translation(0, 0, -2) * worldMatrix * viewMatrix * projectionMatrix);
							//customEffectR.Parameters["WorldViewProj"].SetValue(Matrix.Translation(0, 0, -2) * worldMatrix * viewMatrix * projectionMatrix);
							////customEffectL.Parameters["WorldViewProj"].SetValue(Matrix.RotationX((float)-Math.PI / 2) * Matrix.Translation(0, -.5f, -.25f) * worldMatrix * viewMatrix * projectionMatrix);
							////customEffectR.Parameters["WorldViewProj"].SetValue(Matrix.RotationX((float)-Math.PI / 2) * Matrix.Translation(0, -.5f, -.25f) * worldMatrix * viewMatrix * projectionMatrix);
							//lock (localCritical)
							//{
							//	if (_stereoVideo)
							//	{
							//		if (eyeIndex == 0)
							//			plane.Draw(customEffectL);
							//		if (eyeIndex == 1)
							//			plane.Draw(customEffectR);
							//	}
							//	else
							//		plane.Draw(customEffectL);
							//}
							////// END





							customEffectL.Parameters["WorldViewProj"].SetValue(worldMatrix * viewMatrix * projectionMatrix);
							if (_stereoVideo)
								customEffectR.Parameters["WorldViewProj"].SetValue(worldMatrix * viewMatrix * projectionMatrix);

							lock (localCritical)
							{
								if (_stereoVideo)
								{
									if (eyeIndex == 0)
										primitive.Draw(customEffectL);
									if (eyeIndex == 1)
										primitive.Draw(customEffectR);
								}
								else
									primitive.Draw(customEffectL);
							}

							// reset UI position every frame if it is not visible
							if (vrui.isUIHidden)
								vrui.SetWorldPosition(viewMatrix.Forward, viewPosition, false);

							vrui.Draw(movieTitle, currentTime, duration);
							vrui.Render(deltaTime, viewMatrix, projectionMatrix, viewPosition, pause);

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

					MediaDecoder.Instance.OnFormatChanged -= ResizeTexture;

					waitForRendererStop.Set();

					// Release all resources
					eyeTextures[0].Dispose();
					eyeTextures[1].Dispose();
					immediateContext.ClearState();
					immediateContext.Flush();

					// Release all 2D resources
					customEffectL.Dispose();
					if (_stereoVideo)
						customEffectR.Dispose();

					// == nieaktualne? ==
					// Disposing the device, before the hmd, will cause the hmd to fail when disposing.
					// Disposing the device, after the hmd, will cause the dispose of the device to fail.
					// It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
					// device.Dispose();
				}
			}

			Lock = false;
		}	

		private static SharpDX.Toolkit.Graphics.EffectCompilerResult GetGammaShader()
		{
			string shaderSource = Properties.Resources.GammaShader;
			SharpDX.Toolkit.Graphics.EffectCompiler compiler = new SharpDX.Toolkit.Graphics.EffectCompiler();
			var shaderCode = compiler.Compile(shaderSource, "gamma shader", SharpDX.Toolkit.Graphics.EffectCompilerFlags.Debug | SharpDX.Toolkit.Graphics.EffectCompilerFlags.EnableBackwardsCompatibility | SharpDX.Toolkit.Graphics.EffectCompilerFlags.SkipOptimization);

			if (shaderCode.HasErrors)
				throw new HeadsetError("Shader compile error:\n" + string.Join("\n", shaderCode.Logger.Messages));

			return shaderCode;
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
