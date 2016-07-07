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
using PlayerUI.Oculus;

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
					_playbackLock = false;
				}
                catch (Exception) { _playbackLock = false; }
            });
        }


        public static void Pause()
        {
            vrui?.EnqueueUIRedraw();
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

		public static void Reset()
		{
			abort = false;
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
			int mainRetry = 3;

			do
			{
				using (ClientContext context = new ClientContext("com.bivrost360.desktopplayer"))
				{
					for (int retry = 0; retry < 12; retry++)
						using (var displayConfig = context.GetDisplayConfig())
						{
							if (abort)
							{
								context.Dispose();
								return false;
							}
							// GetDisplayConfig can sometimes fail, returning null
							if (displayConfig != null)
							{
								int contextRetry = 0;
								do
								{
									context.update();
									if (abort)
									{
										context.Dispose();
										return false;
									}
									Thread.Sleep(1);
									contextRetry++;
								} while (!displayConfig.CheckDisplayStartup() || contextRetry < 300);

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
						}
				}
			} while (mainRetry-- > 0);
            return false;
        }

		private static SharpDX.Toolkit.Graphics.GraphicsDevice _gd;
		private static Device _device;
		private static VRUI vrui;

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

			int mainRetry = 5;
			ClientContext context;
			do
			{
				context = new ClientContext("com.bivrost360.desktopplayer");
				Thread.Sleep(50);
			}
			while (context == null && mainRetry-- > 0);

			DisplayConfig displayConfig = null;

			for (int retry = 0; retry < 12; retry++)
			{
				if (abort)
				{
					context.Dispose();
					_playbackLock = false;
					return;
				}

				displayConfig = context.GetDisplayConfig();

				//if (displayConfig == null)
				//	displayConfig = context.GetDisplayConfig();
				//if (displayConfig == null)
				//{
				//	context.Dispose();
				//	_playbackLock = false;
				//	return;
				//}

				if (displayConfig != null)
				{
					int contextRetry = 0;
					do
					{
						context.update();
						contextRetry++;
						if (abort)
						{
							context.Dispose();
							_playbackLock = false;
							return;
						}
						Thread.Sleep(1);
					} while (!displayConfig.CheckDisplayStartup() || contextRetry < 300);
					if (displayConfig.CheckDisplayStartup()) break;
				}
			}
			if(displayConfig == null)
			{
				context.Dispose();
				_playbackLock = false;
				return;
			}

            var numDisplayInputs = displayConfig.GetNumDisplayInputs();
            if (numDisplayInputs != 1)
			{
				context.Dispose();
				_playbackLock = false;
				return;
			}

            var displayDimensions = displayConfig.GetDisplayDimensions(0);
            var numViewers = displayConfig.GetNumViewers();

            if (numViewers != 1)
			{
				context.Dispose();
				_playbackLock = false;
				return;
			}


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
					context.Dispose();
					_playbackLock = false;
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

			ResizeTexture(MediaDecoder.Instance.TextureL, _stereoVideo ? MediaDecoder.Instance.TextureR : MediaDecoder.Instance.TextureL);

			//var primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Sphere.New(gd, radius, 32, true);
			var primitive = GraphicTools.CreateGeometry(_projection, gd);


			// UI Rendering
			vrui = new Oculus.VRUI(device, gd);
			vrui.Draw(movieTitle, currentTime, duration);


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
                        uint numSurfaces = displayConfig.GetNumSurfacesForViewerEye(viewer, eye);
                        Pose3 viewerEyePose = displayConfig.GetViewerEyePose(viewer, eye);
                        Matrix44f viewerEyeMatrixf = displayConfig.GetViewerEyeViewMatrixf(viewer, eye, MatrixConventionsFlags.Default);
                        uint surface = 0;
                        OSVR.ClientKit.Viewport viewport = displayConfig.GetRelativeViewportForViewerEyeSurface(viewer, eye, surface);
                        Matrix44f projectionf = displayConfig.GetProjectionMatrixForViewerEyeSurfacef(viewer, eye, surface, 0.001f, 1000.0f, MatrixConventionsFlags.Default);
                        ProjectionClippingPlanes projectionClippingPlanes = displayConfig.GetViewerEyeSurfaceProjectionClippingPlanes(viewer, eye, surface);

                        ViewportF vp = new ViewportF(viewport.Left, viewport.Bottom, viewport.Width, viewport.Height);
                        immediateContext.Rasterizer.SetViewport(vp);

						Vector3 viewPosition = viewerEyePose.translation.ToVector3();

						Matrix rotationMatrix = Matrix.RotationQuaternion(viewerEyePose.rotation.ToQuaternion());
						Vector3 lookUp = Vector3.Transform(new Vector3(0, 1, 0), rotationMatrix).ToVector3();
						Vector3 lookAt = Vector3.Transform(new Vector3(0, 0, -1), rotationMatrix).ToVector3();
						Matrix viewMatrix = Matrix.LookAtRH(viewPosition, viewPosition + lookAt, lookUp);

						Matrix projectionMatrix = projectionf.ToMatrix();

						basicEffectL.World = Matrix.Translation(viewPosition);
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
								if (eye == 0)
									primitive.Draw(basicEffectL);
								if (eye == 1)
									primitive.Draw(basicEffectR);
							}
							else
								primitive.Draw(basicEffectL);
						}

						// reset UI position every frame if it is not visible
						if (vrui.isUIHidden)
							vrui.SetWorldPosition(viewMatrix.Forward, viewPosition, true);

						vrui.Draw(movieTitle, currentTime, duration);
						vrui.Render(deltaTime, viewMatrix, projectionMatrix, viewPosition, pause);
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


			vrui.Dispose();
			vrui = null;

			// Disposing the device, before the hmd, will cause the hmd to fail when disposing.
			// Disposing the device, after the hmd, will cause the dispose of the device to fail.
			// It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
			// device.Dispose();
			_gd.Dispose();
			_device.Dispose();

			//hmd.Dispose();
			//oculus.Dispose();

			displayConfig.Dispose();
			context.Dispose();

			_playbackLock = false;
		}

        public static event Action OnGotFocus = delegate {};

    }
}
