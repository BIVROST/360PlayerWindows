using OculusWrap;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows;
using System.Windows.Forms;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace PlayerUI.Oculus
{
	public class OculusPlayback
	{
		public static Texture2D textureL;
		public static Texture2D textureR;
		public static float radius = 4.9f;
		public static bool _stereoVideo = false;

		public static ManualResetEvent waitForRendererStop = new ManualResetEvent(false);
		public static bool abort = false;


		public static void Start()
		{
			abort = false;
			waitForRendererStop.Reset();
			Task.Factory.StartNew(() => Render());
		}

		public static void Stop()
		{
			abort = true;
			waitForRendererStop.WaitOne(1000);
		}

		public static bool IsOculusPresent()
		{
			Wrap oculus = new Wrap();

			bool success = oculus.Initialize();
			if(!success)
			{
				oculus.Dispose();
				return false;
			} else
			{
				int numberOfHMD = oculus.Hmd_Detect();
				oculus.Dispose();
				return numberOfHMD > 0 ? true : false;
			}
		}

		private static void Render()
		{
			Wrap oculus = new Wrap();
			Hmd hmd;

			// Initialize the Oculus runtime.
			bool success = oculus.Initialize();
			if (!success)
			{
				MessageBox.Show("Failed to initialize the Oculus runtime library.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Use the head mounted display, if it's available, otherwise use the debug HMD.
			int numberOfHeadMountedDisplays = oculus.Hmd_Detect();
			if (numberOfHeadMountedDisplays > 0)
				hmd = oculus.Hmd_Create(0);
			else
				hmd = oculus.Hmd_CreateDebug(OculusWrap.OVR.HmdType.DK2);

			if (hmd == null)
			{
				MessageBox.Show("Oculus Rift not detected.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (hmd.ProductName == string.Empty)
				MessageBox.Show("The HMD is not enabled.", "There's a tear in the Rift", MessageBoxButtons.OK, MessageBoxIcon.Error);

			// Specify which head tracking capabilities to enable.
			hmd.SetEnabledCaps(OVR.HmdCaps.LowPersistence | OVR.HmdCaps.DynamicPrediction);

			// Start the sensor which informs of the Rift's pose and motion
			hmd.ConfigureTracking(OVR.TrackingCaps.ovrTrackingCap_Orientation | OVR.TrackingCaps.ovrTrackingCap_MagYawCorrection | OVR.TrackingCaps.ovrTrackingCap_Position, OVR.TrackingCaps.None);

			// Create a set of layers to submit.
			EyeTexture[] eyeTextures = new EyeTexture[2];
			OVR.ovrResult result;

			// Create DirectX drawing device.
			SharpDX.Direct3D11.Device device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None);

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
				OVR.EyeType eye = (OVR.EyeType)eyeIndex;
				EyeTexture eyeTexture = new EyeTexture();
				eyeTextures[eyeIndex] = eyeTexture;

				// Retrieve size and position of the texture for the current eye.
				eyeTexture.FieldOfView = hmd.DefaultEyeFov[eyeIndex];
				eyeTexture.TextureSize = hmd.GetFovTextureSize(eye, hmd.DefaultEyeFov[eyeIndex], 1.0f);
				eyeTexture.RenderDescription = hmd.GetRenderDesc(eye, hmd.DefaultEyeFov[eyeIndex]);
				eyeTexture.HmdToEyeViewOffset = eyeTexture.RenderDescription.HmdToEyeViewOffset;
				eyeTexture.ViewportSize.Position = new OVR.Vector2i(0, 0);
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
				OVR.D3D11.D3D11_TEXTURE2D_DESC swapTextureDescriptionD3D11 = SharpDXHelpers.CreateTexture2DDescription(eyeTexture.Texture2DDescription);

				// Create a SwapTextureSet, which will contain the textures to render to, for the current eye.
				result = hmd.CreateSwapTextureSetD3D11(device.NativePointer, ref swapTextureDescriptionD3D11, out eyeTexture.SwapTextureSet);
				WriteErrorDetails(oculus, result, "Failed to create swap texture set.");

				// Create room for each DirectX texture in the SwapTextureSet.
				eyeTexture.Textures = new Texture2D[eyeTexture.SwapTextureSet.TextureCount];
				eyeTexture.RenderTargetViews = new RenderTargetView[eyeTexture.SwapTextureSet.TextureCount];

				// Create a texture 2D and a render target view, for each unmanaged texture contained in the SwapTextureSet.
				for (int textureIndex = 0; textureIndex < eyeTexture.SwapTextureSet.TextureCount; textureIndex++)
				{
					// Retrieve the current textureData object.
					OVR.D3D11.D3D11TextureData textureData = eyeTexture.SwapTextureSet.Textures[textureIndex];

					// Create a managed Texture2D, based on the unmanaged texture pointer.
					eyeTexture.Textures[textureIndex] = new Texture2D(textureData.Texture);

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
				layerEyeFov.ColorTexture[eyeIndex] = eyeTexture.SwapTextureSet.SwapTextureSetPtr;
				layerEyeFov.Viewport[eyeIndex].Position = new OVR.Vector2i(0, 0);
				layerEyeFov.Viewport[eyeIndex].Size = eyeTexture.TextureSize;
				layerEyeFov.Fov[eyeIndex] = eyeTexture.FieldOfView;
				layerEyeFov.Header.Flags = OVR.LayerFlags.HighQuality;
			}


			SharpDX.Toolkit.Graphics.GraphicsDevice gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(device);

			var resourceL = textureL.QueryInterface<SharpDX.DXGI.Resource>();
			var sharedTexL = device.OpenSharedResource<Texture2D>(resourceL.SharedHandle);
			var resourceR = textureR.QueryInterface<SharpDX.DXGI.Resource>();
			var sharedTexR = device.OpenSharedResource<Texture2D>(resourceR.SharedHandle);

			var basicEffectL = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

			basicEffectL.PreferPerPixelLighting = false;
			basicEffectL.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, sharedTexL);

			basicEffectL.TextureEnabled = true;
			basicEffectL.LightingEnabled = false;

			var basicEffectR = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

			basicEffectR.PreferPerPixelLighting = false;
			basicEffectR.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, sharedTexR);

			basicEffectR.TextureEnabled = true;
			basicEffectR.LightingEnabled = false;

			var primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Sphere.New(gd, radius, 32, true);


			DateTime startTime = DateTime.Now;
			Vector3 position = new Vector3(0, 0, -1);

			#region Render loop

			while (!abort)
			{
				OVR.Vector3f[] hmdToEyeViewOffsets = { eyeTextures[0].HmdToEyeViewOffset, eyeTextures[1].HmdToEyeViewOffset };
				OVR.FrameTiming frameTiming = hmd.GetFrameTiming(0);
				OVR.TrackingState trackingState = hmd.GetTrackingState(frameTiming.DisplayMidpointSeconds);
				OVR.Posef[] eyePoses = new OVR.Posef[2];

				// Calculate the position and orientation of each eye.
				oculus.CalcEyePoses(trackingState.HeadPose.ThePose, hmdToEyeViewOffsets, ref eyePoses);

				float timeSinceStart = (float)(DateTime.Now - startTime).TotalSeconds;

				for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
				{
					OVR.EyeType eye = (OVR.EyeType)eyeIndex;
					EyeTexture eyeTexture = eyeTextures[eyeIndex];

					layerEyeFov.RenderPose[eyeIndex] = eyePoses[eyeIndex];

					// Retrieve the index of the active texture and select the next texture as being active next.
					int textureIndex = eyeTexture.SwapTextureSet.CurrentIndex++;

					immediateContext.OutputMerger.SetRenderTargets(eyeTexture.DepthStencilView, eyeTexture.RenderTargetViews[textureIndex]);
					immediateContext.ClearRenderTargetView(eyeTexture.RenderTargetViews[textureIndex], Color.CornflowerBlue);
					immediateContext.ClearDepthStencilView(eyeTexture.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
					immediateContext.Rasterizer.SetViewport(eyeTexture.Viewport);


					Quaternion rotationQuaternion = SharpDXHelpers.ToQuaternion(eyePoses[eyeIndex].Orientation);
					Matrix viewMatrix = Matrix.RotationQuaternion(rotationQuaternion);
					viewMatrix.Transpose();

					Matrix projectionMatrix = Matrix.PerspectiveFovRH((float)(90f * Math.PI / 180f), (float)hmd.Resolution.Width / 2f / hmd.Resolution.Height, 0.001f, 100.0f);

					//LookChanged(viewMatrix);

					basicEffectL.World = Matrix.Identity;
					basicEffectL.View = viewMatrix;
					basicEffectL.Projection = projectionMatrix;

					if (_stereoVideo)
					{
						basicEffectR.World = Matrix.Identity;
						basicEffectR.View = viewMatrix;
						basicEffectR.Projection = projectionMatrix;
					}

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

				hmd.SubmitFrame(0, layers);
			}

			#endregion

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

			// Disposing the device, before the hmd, will cause the hmd to fail when disposing.
			// Disposing the device, after the hmd, will cause the dispose of the device to fail.
			// It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
			// device.Dispose();

			hmd.Dispose();
			oculus.Dispose();
		}





		public static void WriteErrorDetails(Wrap oculus, OVR.ovrResult result, string message)
		{
			if (result >= OVR.ovrResult.Success)
				return;

			// Retrieve the error message from the last occurring error.
			OVR.ovrErrorInfo errorInformation = oculus.GetLastError();

			string formattedMessage = string.Format("{0}. Message: {1} (Error code={2})", message, errorInformation.ErrorString, errorInformation.Result);
			Trace.WriteLine(formattedMessage);

			throw new Exception(formattedMessage);
		}

	}
}
