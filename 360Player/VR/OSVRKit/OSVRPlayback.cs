using OSVR.ClientKit;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Threading;
using System.Windows.Forms;
using Device = SharpDX.Direct3D11.Device;
using Bivrost.Bivrost360Player.Tools;
using SharpDX.Windows;
using SharpDX.Direct3D;
using Bivrost.Log;
using System.Linq;

namespace Bivrost.Bivrost360Player.OSVRKit
{
	public class OSVRPlayback : Headset
	{
		bool _preloaded = false;

		override public bool IsPresent()
        {
			if (!_preloaded)
			{
				ClientContext.PreloadNativeLibraries();
				_preloaded = true;
			}

            if (Lock) return true;
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


		public OSVRPlayback()
		{
			log = new Logger("OSVR");
		}


		protected override float Gamma { get { return 1f; } }

		#region ILookProvider properties
		public override string DescribeType { get { return "OSVR"; } }
		#endregion

		override protected void Render()
		{
			Lock = true;

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
					Lock = false;
					return;
				}

				displayConfig = context.GetDisplayConfig();

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
							Lock = false;
							return;
						}
						Thread.Sleep(1);
					} while (!displayConfig.CheckDisplayStartup() || contextRetry < 300);
					if (displayConfig.CheckDisplayStartup()) break;
				}
			}
			if (displayConfig == null)
			{
				context.Dispose();
				Lock = false;
				return;
			}

			var numDisplayInputs = displayConfig.GetNumDisplayInputs();
			if (numDisplayInputs != 1)
			{
				context.Dispose();
				Lock = false;
				return;
			}

			var displayDimensions = displayConfig.GetDisplayDimensions(0);
			var numViewers = displayConfig.GetNumViewers();

			if (numViewers != 1)
			{
				context.Dispose();
				Lock = false;
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
			Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, desc, out _device, out swapChain);

			// Create DirectX Graphics Interface factory, used to create the swap chain.
			Factory factory = swapChain.GetParent<Factory>();
			factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
			form.FormBorderStyle = FormBorderStyle.None;
			form.TopMost = true;

			DeviceContext immediateContext = _device.ImmediateContext;

			using (SharpDX.DXGI.Device2 dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device2>())
			{

				//var bounds = dxgiDevice.Adapter.Outputs[1].Description.DesktopBounds;
				//form.DesktopBounds = new System.Drawing.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

				//dxgiDevice.Adapter.Outputs.ToList().ForEach(o =>
				//{
				//    if (o.Description.DeviceName.EndsWith("2"))
				//    {
				//        swapChain.SetFullscreenState(true, o);
				//    }
				//});

				Rectangle bounds;

				if (Features.IsDebug)
				{
					log.Info("OSVR: available screens: " + string.Join("\n", dxgiDevice.Adapter.Outputs.ToList().ConvertAll(o => o.Description.DeviceName + " (" + o.Description.DesktopBounds + ")")));
				}

				if (Logic.Instance.settings.OSVRScreen == ScreenSelection.Autodetect)
				{
					// start with last screen
					Output output = dxgiDevice.Adapter.Outputs[dxgiDevice.Adapter.Outputs.Length - 1];

					// but something resembling a HDK 1.4 (1920x1080) will be better
					foreach (var o in dxgiDevice.Adapter.Outputs)
					{
						var b = o.Description.DesktopBounds;
						if (b.Width == 1920 && b.Height == 1080)
						{
							log.Info("OSVR: found a 1920x1080 candidate for a HDK 1.4");
							output = o;
						}
					}

					// and something resembling a HDK 2.0 (2160x1200) will be even more better
					foreach (var o in dxgiDevice.Adapter.Outputs)
					{
						var b = o.Description.DesktopBounds;
						if (b.Width == 2160 && b.Height == 1200)
						{
							log.Info("OSVR: found a 2160x1200 candidate for a HDK 2.0");
							output = o;
						}
					}

					bounds = output.Description.DesktopBounds;
					log.Info($"OSVR: guessed output ({bounds})");
				}
				else
				{
					int osvrScreen = (int)Logic.Instance.settings.OSVRScreen;
					if (osvrScreen >= dxgiDevice.Adapter.Outputs.Length)
						osvrScreen = dxgiDevice.Adapter.Outputs.Length - 1;
					bounds = dxgiDevice.Adapter.Outputs[osvrScreen].Description.DesktopBounds;
					log.Info($"OSVR: selected output #{osvrScreen} ({bounds})");
				}

				form.DesktopBounds = new System.Drawing.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

				if (dxgiDevice.Adapter.Outputs.Length <= 1)
					Logic.Notify("Only one screen is active. Press Control+S to stop the movie if needed.");
			}

			// Create a depth buffer, using the same width and height as the back buffer.
			Texture2DDescription depthBufferDescription = new Texture2DDescription()
			{
				Format = Format.D32_Float,
				ArraySize = 1,
				MipLevels = 1,
				Width = displayDimensions.Width,
				Height = displayDimensions.Height,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None
			};



			// Retrieve the DXGI device, in order to set the maximum frame latency.
			using (SharpDX.DXGI.Device1 dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device1>())
                dxgiDevice.MaximumFrameLatency = 1;

			using (_gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(_device))
			using (customEffectL = GetCustomEffect(_gd))
			using (customEffectR = GetCustomEffect(_gd))
			//using (var primitive = GraphicTools.CreateGeometry(_projection, _gd))
			using (vrui = new VRUI(_device, _gd))
			using (Texture2D depthBuffer = new Texture2D(_device, depthBufferDescription))
			using (DepthStencilView depthView = new DepthStencilView(_device, depthBuffer))
			using (Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0))
			using (RenderTargetView renderView = new RenderTargetView(_device, backBuffer))
			{
				//primitive = GraphicTools.CreateGeometry(Projection, _gd);

				DateTime startTime = DateTime.Now;
				Vector3 position = new Vector3(0, 0, -1);

				#region Render loop

				DateTime lastTime = DateTime.Now;
				float deltaTime = 0;


				immediateContext.OutputMerger.SetTargets(depthView, renderView);


				form.GotFocus += (s, e) =>	OnGotFocus();
				bool first = true;

				RenderLoop.Run(form, () =>
				{
					if (abort)
					{
						form.Close();
						return;
					}

					if (first)
					{
						OnGotFocus();
						first = false;
					}

					UpdateContentIfRequested();

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

							Vector3 lookPosition = viewerEyePose.translation.ToVector3();

							SharpDX.Quaternion lookRotation = viewerEyePose.rotation.ToQuaternion();
							Matrix rotationMatrix = Matrix.RotationQuaternion(lookRotation);
							Vector3 lookUp = Vector3.Transform(new Vector3(0, 1, 0), rotationMatrix).ToVector3();
							Vector3 lookAt = Vector3.Transform(new Vector3(0, 0, -1), rotationMatrix).ToVector3();
							Matrix viewMatrix = Matrix.LookAtRH(lookPosition, lookPosition + lookAt, lookUp);

							Matrix projectionMatrix = projectionf.ToMatrix();

							Matrix worldMatrix = Matrix.Translation(lookPosition);

							Matrix MVP = worldMatrix * viewMatrix * projectionMatrix;
							customEffectL.Parameters["WorldViewProj"].SetValue(MVP);
							customEffectR.Parameters["WorldViewProj"].SetValue(MVP);

							lock (localCritical)
							{
								if (eye == 0)
									primitive?.Draw(customEffectL);
								if (eye == 1)
									primitive?.Draw(customEffectR);
							}

							// reset UI position every frame if it is not visible
							if (vrui.isUIHidden)
								vrui.SetWorldPosition(viewMatrix.Forward, lookPosition, true);

                            if (eye == 0)
                            {
                                lookRotation.Invert();
                                ProvideLook?.Invoke(lookPosition, lookRotation, OSVRFOV);
                            }

                            vrui.Draw(Media, currentTime, Duration);
							vrui.Render(deltaTime, viewMatrix, projectionMatrix, lookPosition, ShouldShowVRUI);
						}


					}

					swapChain.Present(0, PresentFlags.None);
				});

				#endregion
				//debugWindow.Stop();

				waitForRendererStop.Set();

				//swapChain.SetFullscreenState(false, null);



				immediateContext.ClearState();
				immediateContext.Flush();
				immediateContext.Dispose();

				swapChain.Dispose();

				factory.Dispose();

				//swapChain.Dispose();

				// Disposing the device, before the hmd, will cause the hmd to fail when disposing.
				// Disposing the device, after the hmd, will cause the dispose of the device to fail.
				// It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
				// device.Dispose();
				base._device.Dispose();

				//hmd.Dispose();
				//oculus.Dispose();

				displayConfig.Dispose();
				context.Dispose();
			}

			Lock = false;
		}


		public event Action OnGotFocus = delegate {};


        public override event Action<Vector3, SharpDX.Quaternion, float> ProvideLook;

        const float OSVRFOV = 90;

    }
}
