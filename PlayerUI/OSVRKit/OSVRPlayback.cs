using OSVR.ClientKit;
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
using DX2D = SharpDX.Direct2D1;
using System.Runtime.InteropServices;
using PlayerUI.Tools;
using SharpDX.Windows;
using SharpDX.Direct3D;

namespace PlayerUI.OSVRKit
{
    public partial class OSVRPlayback
    {
        public static Texture2D textureL;
        public static Texture2D textureR;
        public static float radius = 4.9f;
        public static bool _stereoVideo = false;
        public static MediaDecoder.ProjectionMode _projection = MediaDecoder.ProjectionMode.Sphere;

        public static ManualResetEvent waitForRendererStop = new ManualResetEvent(false);
        public static bool abort = false;
        public static bool pause = false;

        private static float uiOpacity = 0;
        private static string movieTitle = "";
        private static float duration = 0;
        private static float currentTime = 0;

        private static SharpDX.Toolkit.Graphics.BasicEffect basicEffectL;
        private static SharpDX.Toolkit.Graphics.BasicEffect basicEffectR;

        private static bool _playbackLock = false;
        public static bool Lock { get { return _playbackLock; } }
		private static object localCritical = new object();

        public static void Start()
        {
            abort = false;
            pause = false;
            waitForRendererStop.Reset();
            if (_playbackLock)
                return;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Render();
                }
                catch (Exception) { }
            });
        }


        public static void Pause()
        {
            EnqueueUIRedraw();
            pause = true;
        }
        public static void UnPause() { pause = false; }

        public static void UpdateTime(float time)
        {
            currentTime = time;
        }

        public static void Configure(string title, float movieDuration)
        {
            movieTitle = title;
            duration = movieDuration;
        }

        public static void Stop()
        {
            abort = true;
        }

		private static bool _preloaded = false;
		private static int _selectedOutput = 0;

        public static bool IsOculusPresent()
        {
			if (!_preloaded)
			{
				ClientContext.PreloadNativeLibraries();
				_preloaded = true;
			}

            if (_playbackLock) return true;

            using (ClientContext context = new ClientContext("com.osvr.exampleclients.managed.DisplayParameter"))
            {
                for (int retry = 0; retry < 10; retry++)
                    using (var displayConfig = context.GetDisplayConfig())
                    {
                        // GetDisplayConfig can sometimes fail, returning null
                        if (displayConfig != null)
                        {
							int contextRetry = 0;
                            do
                            {
                                context.update();
								Thread.Sleep(10);
								contextRetry++;
                            } while (!displayConfig.CheckDisplayStartup() || contextRetry < 20);

                            var numDisplayInputs = displayConfig.GetNumDisplayInputs();

                            for (byte displayInputIndex = 0; displayInputIndex < numDisplayInputs; displayInputIndex++)
                            {
                                var displayDimensions = displayConfig.GetDisplayDimensions(displayInputIndex);
                                Console.WriteLine("Display input {0} is width {1} and height {2}",
                                    displayInputIndex, displayDimensions.Width, displayDimensions.Height);
                            }

                            var numViewers = displayConfig.GetNumViewers();

                            if (numViewers > 0) return true;
                        }
                        else
                            return false;
                    }
            }
            return false;
        }

		private static SharpDX.Toolkit.Graphics.GraphicsDevice _gd;
		private static Device _device;

		static void ResizeTexture(Texture2D tL, Texture2D tR)
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

		private static void Render()
        {
            _playbackLock = true;

            //Wrap oculus = new Wrap();
            //Hmd hmd;

            ClientContext context = new ClientContext("com.bivrost360.desktopplayer");
            var displayConfig = context.GetDisplayConfig();

            for (int retry = 0; retry < 10; retry++)
                if (displayConfig == null)
                    displayConfig = context.GetDisplayConfig();
            if (displayConfig == null) return;

			int contextRetry = 0;
            do
            {
                context.update();
				contextRetry++;
				Thread.Sleep(10);
            } while (!displayConfig.CheckDisplayStartup() || contextRetry < 20);


            var numDisplayInputs = displayConfig.GetNumDisplayInputs();
            if (numDisplayInputs != 1) return;

            var displayDimensions = displayConfig.GetDisplayDimensions(0);
            var numViewers = displayConfig.GetNumViewers();

            if (numViewers != 1) return;


            var form = new RenderForm("BIVROST - OSVR");
            form.Width = displayDimensions.Width;
            form.Height = displayDimensions.Height;
            form.ShowInTaskbar = false;


            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                    new ModeDescription(displayDimensions.Width, displayDimensions.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            SwapChain swapChain;


            //// Initialize the Oculus runtime.
            //bool success = oculus.Initialize();
            //if (!success)
            //{
            //    MessageBox.Show("Failed to initialize the Oculus runtime library.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}

            // Use the head mounted display, if it's available, otherwise use the debug HMD.
            //int numberOfHeadMountedDisplays = oculus.Hmd_Detect();
            //if (numberOfHeadMountedDisplays > 0)
            //	hmd = oculus.Hmd_Create(0);
            //else
            //	hmd = oculus.Hmd_CreateDebug(OculusWrap.OVR.HmdType.DK2);
            //OVR.GraphicsLuid graphicsLuid;
            //hmd = oculus.Hmd_Create(out graphicsLuid);

            //if (hmd == null)
            //{
            //    MessageBox.Show("Oculus Rift not detected.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}

            //if (hmd.ProductName == string.Empty)
            //    MessageBox.Show("The HMD is not enabled.", "There's a tear in the Rift", MessageBoxButtons.OK, MessageBoxIcon.Error);

            //// Specify which head tracking capabilities to enable.
            //hmd.SetEnabledCaps(OVR.HmdCaps.DebugDevice);

            //// Start the sensor which informs of the Rift's pose and motion
            //hmd.ConfigureTracking(OVR.TrackingCaps.Orientation | OVR.TrackingCaps.MagYawCorrection, OVR.TrackingCaps.None);

            // Create a set of layers to submit.
            //EyeTexture[] eyeTextures = new EyeTexture[2];
            //OVR.ovrResult result;

            // Create DirectX drawing device.
            //SharpDX.Direct3D11.Device device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 });
            Device device;
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, desc, out device, out swapChain);			

            // Create DirectX Graphics Interface factory, used to create the swap chain.
            Factory factory;// = new Factory();

            factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
			form.FormBorderStyle = FormBorderStyle.None;
			form.TopMost = true;

            DeviceContext immediateContext = device.ImmediateContext;

            {
                SharpDX.DXGI.Device2 dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device2>();

				//var bounds = dxgiDevice.Adapter.Outputs[1].Description.DesktopBounds;
				//form.DesktopBounds = new System.Drawing.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

				//dxgiDevice.Adapter.Outputs.ToList().ForEach(o =>
				//{
				//    if (o.Description.DeviceName.EndsWith("2"))
				//    {
				//        swapChain.SetFullscreenState(true, o);
				//    }
				//});

				if (dxgiDevice.Adapter.Outputs.Length > 1)
				{
					switch (Logic.Instance.settings.OSVRScreen)
					{
						case ScreenSelection.One:
							//swapChain.SetFullscreenState(true, dxgiDevice.Adapter.Outputs[0]);
							_selectedOutput = 0;
							break;
						case ScreenSelection.Two:
							if (dxgiDevice.Adapter.Outputs.Length > 1)
							{
								//swapChain.SetFullscreenState(true, dxgiDevice.Adapter.Outputs[1]);
								_selectedOutput = 1;
							}
							else {
								//swapChain.SetFullscreenState(true, dxgiDevice.Adapter.Outputs[0]);
								_selectedOutput = 0;
							}
							break;
						case ScreenSelection.Three:
							if (dxgiDevice.Adapter.Outputs.Length > 2)
							{
								//swapChain.SetFullscreenState(true, dxgiDevice.Adapter.Outputs[2]);
								_selectedOutput = 2;
							}
							else {
								if (dxgiDevice.Adapter.Outputs.Length > 1)
								{
									//swapChain.SetFullscreenState(true, dxgiDevice.Adapter.Outputs[1]);
									_selectedOutput = 1;
								}
								else {
									//swapChain.SetFullscreenState(true, dxgiDevice.Adapter.Outputs[0]);
									_selectedOutput = 0;
								}
							}
							break;
					}

					var bounds = dxgiDevice.Adapter.Outputs[_selectedOutput].Description.DesktopBounds;
					form.DesktopBounds = new System.Drawing.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
				}
				else
				{
					return;
				}


				//swapChain.SetFullscreenState(true, target);
			}



            // Create a depth buffer, using the same width and height as the back buffer.
            Texture2DDescription depthBufferDescription = new Texture2DDescription();
            depthBufferDescription.Format = Format.D32_Float;
            depthBufferDescription.ArraySize = 1;
            depthBufferDescription.MipLevels = 1;
            depthBufferDescription.Width = displayDimensions.Width;
            depthBufferDescription.Height = displayDimensions.Height;
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
            //DepthStencilView depthStencilView = new DepthStencilView(device, depthBuffer);
            //DepthStencilState depthStencilState = new DepthStencilState(device, depthStencilStateDescription);
            //SharpDX.Viewport viewport = new SharpDX.Viewport(0, 0, displayDimensions.Width, displayDimensions.Height, 0.0f, 1.0f);


            // Retrieve the DXGI device, in order to set the maximum frame latency.
            using (SharpDX.DXGI.Device1 dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device1>())
            {
                dxgiDevice.MaximumFrameLatency = 1;
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

			ResizeTexture(MediaDecoder.Instance.TextureL, MediaDecoder.Instance.TextureL);

			//var primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Sphere.New(gd, radius, 32, true);
			var primitive = GraphicTools.CreateGeometry(_projection, gd);


            // UI Rendering
            InitUI(device, gd);
            DrawUI();


            //Oculus.OculusUIDebug debugWindow = new Oculus.OculusUIDebug();
            //debugWindow.SetSharedTexture(uiTexture);
            //debugWindow.Start();

            #endregion


            DateTime startTime = DateTime.Now;
            Vector3 position = new Vector3(0, 0, -1);

            #region Render loop

            DateTime lastTime = DateTime.Now;
            float deltaTime = 0;

            var depthView = new DepthStencilView(device, depthBuffer);
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            var renderView = new RenderTargetView(device, backBuffer);
            immediateContext.OutputMerger.SetTargets(depthView, renderView);


            form.GotFocus += (s, e) =>
            {
                OnGotFocus();
            };
            bool first = true;

            RenderLoop.Run(form, () =>
            {
				if (abort)
				{
					form.Close();
					return;
				}				

                if(first)
                {
                    OnGotFocus();
                    first = false;
                }

                context.update();

                float timeSinceStart = (float)(DateTime.Now - startTime).TotalSeconds;
                deltaTime = (float)(DateTime.Now - lastTime).TotalSeconds;
                lastTime = DateTime.Now;

                immediateContext.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
                immediateContext.ClearRenderTargetView(renderView, Color.Black);

                uint viewer = 0;

                for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                {
                    var numEyes = displayConfig.GetNumEyesForViewer(viewer);
                    var viewerPose = displayConfig.GetViewerPose(viewer);



                    for (byte eye = 0; eye < numEyes; eye++)
                    {
                        var numSurfaces = displayConfig.GetNumSurfacesForViewerEye(viewer, eye);
                        var viewerEyePose = displayConfig.GetViewerEyePose(viewer, eye);
                        var viewerEyeMatrixd = displayConfig.GetViewerEyeViewMatrixd(viewer, eye, MatrixConventionsFlags.Default);
                        var viewerEyeMatrixf = displayConfig.GetViewerEyeViewMatrixf(viewer, eye, MatrixConventionsFlags.Default);
                        uint surface = 0;
                        var viewport = displayConfig.GetRelativeViewportForViewerEyeSurface(viewer, eye, surface);
                        var projectiond = displayConfig.GetProjectionMatrixForViewerEyeSurfaced(viewer, eye, surface, 0.001, 1000.0, MatrixConventionsFlags.Default);
                        var projectionf = displayConfig.GetProjectionMatrixForViewerEyeSurfacef(viewer, eye, surface, 0.001f, 1000.0f, MatrixConventionsFlags.Default);
                        var projectionClippingPlanes = displayConfig.GetViewerEyeSurfaceProjectionClippingPlanes(viewer, eye, surface);

                        ViewportF vp = new ViewportF(viewport.Left, viewport.Bottom, viewport.Width, viewport.Height);
                        immediateContext.Rasterizer.SetViewport(vp);

                        SharpDX.Quaternion rotationQuaternion = SharpDXHelpers.ToQuaternion(viewerEyePose.rotation);
                        Matrix viewMatrix = Matrix.RotationQuaternion(rotationQuaternion);
                        viewMatrix.Transpose();


                        float fov2 = (float)(90f * Math.PI / 180f);

                        Matrix projectionMatrix = Matrix.PerspectiveFovRH(fov2, viewport.Width / (float)viewport.Height, 0.001f, 100.0f);
                        //Matrix projectionMatrix = new Matrix()
                        //{
                        //    M11 = projectionf.M0,
                        //    M12 = projectionf.M1,
                        //    M13 = projectionf.M2,
                        //    M14 = projectionf.M3,
                        //    M21 = projectionf.M4,
                        //    M22 = projectionf.M5,
                        //    M23 = projectionf.M6,
                        //    M24 = projectionf.M7,
                        //    M31 = projectionf.M8,
                        //    M32 = projectionf.M9,
                        //    M33 = projectionf.M10,
                        //    M34 = projectionf.M11,
                        //    M41 = projectionf.M12,
                        //    M42 = projectionf.M13,
                        //    M43 = projectionf.M14,
                        //    M44 = projectionf.M15,
                        //};

                        basicEffectL.World = Matrix.Identity;
                        basicEffectL.View = viewMatrix;
                        basicEffectL.Projection = projectionMatrix;

                        uiEffect.World = Matrix.Identity * Matrix.Scaling(1f) * Matrix.Translation(0, 0, -1.5f);
                        uiEffect.View = viewMatrix;
                        uiEffect.Projection = projectionMatrix;

                        if (_stereoVideo)
                        {
                            basicEffectR.World = Matrix.Identity;
                            basicEffectR.View = viewMatrix;
                            basicEffectR.Projection = projectionMatrix;
                        }

						lock (localCritical)
						{
							if (_stereoVideo)
							{
								if (eye == 0)
									primitive.Draw(basicEffectL);
								if (eye == 1)
									primitive.Draw(basicEffectR);
							}
							else
								primitive.Draw(basicEffectL);
						}

						DrawUI();
                        RenderUI(deltaTime);

                    }


                }

                swapChain.Present(0, PresentFlags.None);
            });

			#endregion
			//debugWindow.Stop();

			MediaDecoder.Instance.OnFormatChanged -= ResizeTexture;

			waitForRendererStop.Set();

			//swapChain.SetFullscreenState(false, null);



			immediateContext.ClearState();
			immediateContext.Flush();
			immediateContext.Dispose();

			swapChain.Dispose();

			backBuffer.Dispose();
			renderView.Dispose();
			depthView.Dispose();
			depthBuffer.Dispose();
			factory.Dispose();

			//swapChain.Dispose();

			// Release all 2D resources
			basicEffectL.Dispose();
			if (_stereoVideo)
				basicEffectR.Dispose();


			DisposeUI();

			// Disposing the device, before the hmd, will cause the hmd to fail when disposing.
			// Disposing the device, after the hmd, will cause the dispose of the device to fail.
			// It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
			// device.Dispose();
			_gd.Dispose();
			_device.Dispose();

			//hmd.Dispose();
			//oculus.Dispose();

			context.Dispose();
			_playbackLock = false;
		}

        public static event Action OnGotFocus = delegate {};

        //public static void WriteErrorDetails(Wrap oculus, OVR.ovrResult result, string message)
        //{
        //    if (result >= OVR.ovrResult.Success)
        //        return;

        //    // Retrieve the error message from the last occurring error.
        //    OVR.ovrErrorInfo errorInformation = oculus.GetLastError();

        //    string formattedMessage = string.Format("{0}. Message: {1} (Error code={2})", message, errorInformation.ErrorString, errorInformation.Result);
        //    Trace.WriteLine(formattedMessage);

        //    throw new Exception(formattedMessage);
        //}
    }
}
