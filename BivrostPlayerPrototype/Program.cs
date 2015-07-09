using System;
using System.Windows.Forms;
using OculusWrap;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.MediaFoundation;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using System.Linq;


namespace BivrostPlayerPrototype
{
	
	public class PlayerPrototype
	{
		
		public static ManualResetEvent eventReadyToPlay = new ManualResetEvent(false);
		public static bool AbortSignal = false;
		public static RenderForm form;
		public static MediaEngine mediaEngine;

		public static event Action<double> TimeUpdate = delegate { };
		public static event Action<double> VideoLoaded = delegate { };
		public static event Action<Texture2D> TextureCreated = delegate { };
		public static event Action<Matrix> LookChanged = delegate { };

		public static bool Loop = false;

		public static Texture2D externalRenderTargetTexture;

		private static float _sphereSize = 6f;
		private static bool _sphereSizeChanged = false;

		private static bool _stereoVideo = false;

		public static Device _presetDevice = null;

		public static bool IsPlaying { get { return isPlaying; } }

		private static bool isPlaying = false;

		private static bool readyToPlayLoadedVideo = false;

		[STAThread]
        public static void Play(string fileName, bool autoPlay = true)
		{
			var radius = 4.9f;
			if(File.Exists("radius.txt"))
			{
				string r = File.ReadAllText("radius.txt");
				float.TryParse(r, out radius);
			}

			AbortSignal = false;

			eventReadyToPlay = new ManualResetEvent(false);
			form = new RenderForm(); // new RenderForm("Bivrost Player");
			form.Width = 1920;
			form.Height = 1080;
			form.Visible = false;
			form.WindowState = FormWindowState.Maximized;
			form.FormBorderStyle = FormBorderStyle.None;
			form.VisibleChanged += (s, e) =>
			{
				form.Visible = false;
			};
			form.FormBorderStyle = FormBorderStyle.None;
			form.TransparencyKey = form.BackColor;
			form.ShowInTaskbar = false;

			form.StartPosition = FormStartPosition.Manual;
			
			form.Location = new System.Drawing.Point(0, 4000);
			

			Wrap	oculus	= new Wrap();
			Hmd		hmd;

			// Initialize the Oculus runtime.
			oculus.Initialize();

			// Use the head mounted display, if it's available, otherwise use the debug HMD.
			int numberOfHeadMountedDisplays = oculus.Hmd_Detect();

			if(numberOfHeadMountedDisplays > 0)
				hmd = oculus.Hmd_Create(0);
			else
				hmd = oculus.Hmd_CreateDebug(OculusWrap.OVR.HmdType.DK2);

			if (hmd == null)
			{
				MessageBox.Show("Oculus Rift not detected.","Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (hmd.ProductName == string.Empty) 
				MessageBox.Show("The HMD is not enabled.", "There's a tear in the Rift", MessageBoxButtons.OK, MessageBoxIcon.Error);

			OVR.Recti	destMirrorRect;
			OVR.Recti	sourceRenderTargetRect;
			hmd.AttachToWindow(form.Handle, out destMirrorRect, out sourceRenderTargetRect);

			// Create a backbuffer that's the same size as the HMD's resolution.
			OVR.Sizei backBufferSize;
			backBufferSize.Width = hmd.Resolution.Width;
			backBufferSize.Height = hmd.Resolution.Height;

			// Configure Stereo settings.
			OVR.Sizei recommenedTex0Size = hmd.GetFovTextureSize(OVR.EyeType.Left,  hmd.DefaultEyeFov[0], 1.0f);
			OVR.Sizei recommenedTex1Size = hmd.GetFovTextureSize(OVR.EyeType.Right, hmd.DefaultEyeFov[1], 1.0f);

			// Define a render target texture that's the size that the Oculus SDK recommends, for it's default field of view.
			OVR.Sizei renderTargetTextureSize;
			renderTargetTextureSize.Width = recommenedTex0Size.Width + recommenedTex1Size.Width;
			renderTargetTextureSize.Height = Math.Max(recommenedTex0Size.Height, recommenedTex1Size.Height);


			
			// Create DirectX Graphics Interface factory, used to create the swap chain.
			SharpDX.DXGI.Factory factory = new SharpDX.DXGI.Factory();
			string lines = "";
			foreach (Adapter a in factory.Adapters)
			{
				lines += a.Description.Description + "\n";
			}

			FeatureLevel[] levels = new FeatureLevel[] { FeatureLevel.Level_11_0 };
			
			SharpDX.Direct3D11.Device device = null;


			if (factory.Adapters.Any(a => a.Description.Description.ToLower().Contains("amd")))
			{
				Adapter adapter = factory.Adapters.First(a => a.Description.Description.ToLower().Contains("amd"));
				//device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport | DeviceCreationFlags.Debug, levels);
				device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, levels);
			}
			else if (factory.Adapters.Any(a => a.Description.Description.ToLower().Contains("nvidia")))
			{
				Adapter adapter = factory.Adapters.First(a => a.Description.Description.ToLower().Contains("nvidia"));
				//device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport | DeviceCreationFlags.Debug, levels);
				device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, levels);
			}
			else
			{
				// Create DirectX drawing device.
				//device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport | DeviceCreationFlags.Debug, levels);
				device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, levels);
			}


			SharpDX.DXGI.Device1 dxdevice = device.QueryInterface<SharpDX.DXGI.Device1>();
			
			dxdevice.Disposing += (e,s) =>
			{
				Console.WriteLine("DISPOSING PLAYER");
			};

			lines += "\nSelected adapter: " + dxdevice.Adapter.Description.Description;
			File.WriteAllText("adapter.txt", lines);


			DeviceMultithread mt = device.QueryInterface<DeviceMultithread>();
			mt.SetMultithreadProtected(true);

			

            // Ignore all windows events.
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // Define the properties of the swap chain.
			SwapChainDescription swapChainDescription						= new SwapChainDescription();
			swapChainDescription.BufferCount								= 1;
			swapChainDescription.IsWindowed									= true;
			swapChainDescription.OutputHandle								= form.Handle;
			swapChainDescription.SampleDescription							= new SampleDescription(1, 0);
			swapChainDescription.Usage										= Usage.RenderTargetOutput | Usage.ShaderInput;
			swapChainDescription.SwapEffect									= SwapEffect.Sequential;
			swapChainDescription.Flags										= SwapChainFlags.AllowModeSwitch;
			swapChainDescription.ModeDescription.Width						= backBufferSize.Width;
			swapChainDescription.ModeDescription.Height						= backBufferSize.Height;
			swapChainDescription.ModeDescription.Format						= Format.R8G8B8A8_UNorm;
			swapChainDescription.ModeDescription.RefreshRate.Numerator		= 0;
			swapChainDescription.ModeDescription.RefreshRate.Denominator	= 1;

			// Create the swap chain.
			SharpDX.DXGI.SwapChain	swapChain	= new SwapChain(factory, device, swapChainDescription);

			// Retrieve the back buffer of the swap chain.
			Texture2D			backBufferTexture				= swapChain.GetBackBuffer<Texture2D>(0);				// = BackBuffer
			RenderTargetView	backBufferRenderTargetView		= new RenderTargetView(device, backBufferTexture);		// = BackBufferRT

			

			// Create a depth buffer, using the same width and height as the back buffer.
			Texture2DDescription depthBufferDescription = new Texture2DDescription();
			depthBufferDescription.Format				= Format.D32_Float;
			depthBufferDescription.ArraySize			= 1;
			depthBufferDescription.MipLevels			= 1;
			depthBufferDescription.Width				= backBufferSize.Width;
			depthBufferDescription.Height				= backBufferSize.Height;
			depthBufferDescription.SampleDescription	= new SampleDescription(1, 0);
			depthBufferDescription.Usage				= ResourceUsage.Default;
			depthBufferDescription.BindFlags			= BindFlags.DepthStencil;
			depthBufferDescription.CpuAccessFlags		= CpuAccessFlags.None;
			depthBufferDescription.OptionFlags			= ResourceOptionFlags.None;

			// Define how the depth buffer will be used to filter out objects, based on their distance from the viewer.
			DepthStencilStateDescription depthStencilStateDescription	= new DepthStencilStateDescription();
			depthStencilStateDescription.IsDepthEnabled					= true;
			depthStencilStateDescription.DepthComparison				= Comparison.Less;
			depthStencilStateDescription.DepthWriteMask					= DepthWriteMask.Zero;

            // Create the depth buffer.
            Texture2D			depthBufferTexture	= new Texture2D(device, depthBufferDescription);
            DepthStencilView	depthStencilView	= new DepthStencilView(device, depthBufferTexture);
			DepthStencilState	depthStencilState	= new DepthStencilState(device, depthStencilStateDescription);

			// Define a texture that will contain the rendered graphics.
			Texture2DDescription texture2DDescription	= new Texture2DDescription();
			texture2DDescription.Width					= renderTargetTextureSize.Width;
			texture2DDescription.Height					= renderTargetTextureSize.Height;
			texture2DDescription.ArraySize				= 1;
			texture2DDescription.MipLevels				= 1;
			texture2DDescription.Format = Format.R8G8B8A8_UNorm;
			texture2DDescription.SampleDescription		= new SampleDescription(1, 0);
			texture2DDescription.BindFlags				= BindFlags.ShaderResource | BindFlags.RenderTarget;
			texture2DDescription.Usage					= ResourceUsage.Default;
			texture2DDescription.CpuAccessFlags			= CpuAccessFlags.None;
			//texture2DDescription.OptionFlags = ResourceOptionFlags.Shared;

			// Create the texture that will contain the rendered graphics.
			Texture2D			renderTargetTexture				= new Texture2D(device, texture2DDescription);			// = pRendertargetTexture
			RenderTargetView	renderTargetRenderTargetView	= new RenderTargetView(device, renderTargetTexture);	// = pRendertargetTexture->TexRtv
			ShaderResourceView	renderTargetShaderResourceView	= new ShaderResourceView(device, renderTargetTexture);	// = pRendertargetTexture->TexSv


			

		    // Update the actual size of the render target texture. 
			// This may differ from the requested size, for certain kinds of graphics adapters.
			renderTargetTextureSize.Width = renderTargetTexture.Description.Width;
			renderTargetTextureSize.Height = renderTargetTexture.Description.Height;

			// Define a depth buffer for the render target texture, matching the dimensions of the texture.
			Texture2DDescription renderTargetDepthBufferDescription = new Texture2DDescription();
			renderTargetDepthBufferDescription.Format				= Format.D32_Float;
			renderTargetDepthBufferDescription.ArraySize			= 1;
			renderTargetDepthBufferDescription.MipLevels			= 1;
			renderTargetDepthBufferDescription.Width				= renderTargetTexture.Description.Width;
			renderTargetDepthBufferDescription.Height				= renderTargetTexture.Description.Height;
			renderTargetDepthBufferDescription.SampleDescription	= new SampleDescription(1, 0);
			renderTargetDepthBufferDescription.Usage				= ResourceUsage.Default;
			renderTargetDepthBufferDescription.BindFlags			= BindFlags.DepthStencil;
			renderTargetDepthBufferDescription.CpuAccessFlags		= CpuAccessFlags.None;
			renderTargetDepthBufferDescription.OptionFlags			= ResourceOptionFlags.None;

			// Define how the depth buffer will be used to filter out objects, based on their distance from the viewer.
			DepthStencilStateDescription renderTargetDepthStencilStateDescription	= new DepthStencilStateDescription();
			renderTargetDepthStencilStateDescription.IsDepthEnabled					= true;
			renderTargetDepthStencilStateDescription.DepthComparison				= Comparison.Less;
			renderTargetDepthStencilStateDescription.DepthWriteMask					= DepthWriteMask.Zero;

			// Create depth buffer for the render target texture, matching the dimensions of the texture.
			Texture2D			renderTargetDepthBufferTexture	= new Texture2D(device, renderTargetDepthBufferDescription);
			DepthStencilView	renderTargetDepthStencilView	= new DepthStencilView(device, renderTargetDepthBufferTexture);
			DepthStencilState	renderTargetDepthStencilState	= new DepthStencilState(device, renderTargetDepthStencilStateDescription);

			// Create a depth stencil for clearing the renderTargetDepthBufferTexture.
			DepthStencilStateDescription clearDepthStencilStateDescription	= new DepthStencilStateDescription();
			clearDepthStencilStateDescription.IsDepthEnabled				= true;
			clearDepthStencilStateDescription.DepthComparison				= Comparison.Always;
			clearDepthStencilStateDescription.DepthWriteMask				= DepthWriteMask.All;

			DepthStencilState	clearDepthStencilState	= new DepthStencilState(device, clearDepthStencilStateDescription);

			#region Vertex and pixel shader
			// Create vertex shader.
            ShaderBytecode	vertexShaderByteCode	= ShaderBytecode.CompileFromFile("Shaders.fx", "VertexShaderPositionColor", "vs_4_0");
            VertexShader	vertexShader			= new VertexShader(device, vertexShaderByteCode);

			// Create pixel shader.
            ShaderBytecode	pixelShaderByteCode		= ShaderBytecode.CompileFromFile("Shaders.fx", "PixelShaderPositionColor", "ps_4_0");
            PixelShader		pixelShader				= new PixelShader(device, pixelShaderByteCode);
            
			ShaderSignature shaderSignature			= ShaderSignature.GetInputSignature(vertexShaderByteCode);

			// Specify that each vertex consists of a single vertex position and color.
			InputElement[] inputElements = new InputElement[]
            {
                new InputElement("POSITION",	0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("COLOR",		0, Format.R32G32B32A32_Float, 16, 0)
            };

            // Define an input layout to be passed to the vertex shader.
            InputLayout inputLayout = new InputLayout(device, shaderSignature, inputElements);

            // Create a vertex buffer, containing our 3D model.
            Buffer vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, m_vertices);

            // Create a constant buffer, to contain our WorldViewProjection matrix, that will be passed to the vertex shader.
            Buffer contantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Setup the immediate context to use the shaders and model we defined.
            DeviceContext immediateContext						= device.ImmediateContext;
            immediateContext.InputAssembler.InputLayout			= new InputLayout(device, shaderSignature, inputElements);
            immediateContext.InputAssembler.PrimitiveTopology	= PrimitiveTopology.TriangleList;
            immediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, sizeof(float)*4*2, 0));
            immediateContext.VertexShader.SetConstantBuffer(0, contantBuffer);
            immediateContext.VertexShader.Set(vertexShader);
            immediateContext.PixelShader.Set(pixelShader);
			#endregion



			#region Media Playback configuration

			// MEDIA PLAYBACK

			bool isMusicStopped;
			MediaEngineEx mediaEngineEx;
			bool endPlayer = false;

			MediaManager.Startup();

			var mediaEngineFactory = new MediaEngineClassFactory();
			var dxgiManager = new DXGIDeviceManager();
			dxgiManager.ResetDevice(device);
			MediaEngineAttributes attr = new MediaEngineAttributes();
			attr.VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm;
			attr.DxgiManager = dxgiManager;
			mediaEngine = new MediaEngine(mediaEngineFactory, attr, MediaEngineCreateFlags.None);
			mediaEngine.PlaybackEvent += (playEvent, param1, param2) =>
			{
				switch (playEvent)
				{
					case MediaEngineEvent.CanPlay:
						eventReadyToPlay.Set();
						VideoLoaded(mediaEngine.Duration);
						break;
					case MediaEngineEvent.TimeUpdate:
						TimeUpdate(mediaEngine.CurrentTime);
						break;
					case MediaEngineEvent.Error:
						isMusicStopped = true;
						long result = (param2 & 0xFFFFFFFF);
						break;
					case MediaEngineEvent.Abort:
						isMusicStopped = true;
						break;
					case MediaEngineEvent.Ended:
						isMusicStopped = true;
						isPlaying = false;
						break;
				}
			};

			mediaEngineEx = mediaEngine.QueryInterface<MediaEngineEx>();
			if (fileName.Contains("http://"))
			{
				var webStream = new System.Net.WebClient().OpenRead(fileName);
				var stream = new ByteStream(webStream);
				var url = new Uri(fileName, UriKind.Absolute);
				
				mediaEngineEx.SetSourceFromByteStream(stream, url.AbsoluteUri);
				mediaEngineEx.Load();
				
			}
			else
			{
				var fileStream = File.OpenRead(fileName);
				var stream = new ByteStream(fileStream);
				var url = new Uri(fileStream.Name, UriKind.RelativeOrAbsolute);
				mediaEngineEx.SetSourceFromByteStream(stream, url.AbsoluteUri);
			}
						

			if (!eventReadyToPlay.WaitOne(10000))
			{
				Console.WriteLine("Unexpected error: Unable to play this file");
			}

			

			//Get our video size
			int w, h;
			mediaEngine.GetNativeVideoSize(out w, out h);
			var hasVideo = mediaEngine.HasVideo();
			var hasAudio = mediaEngine.HasAudio();
			

			float videoAspect = w / h;
			_stereoVideo = videoAspect < 1.5;
			h = _stereoVideo ? h / 2 : h;


			var textureL = new SharpDX.Direct3D11.Texture2D(device, new Texture2DDescription()
			{
				Width = w,
				Height = h,
				MipLevels = 1,
				ArraySize = 1,
				Format = Format.B8G8R8A8_UNorm,
				Usage = ResourceUsage.Default,
				SampleDescription = new SampleDescription(1, 0),
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.Shared
			});

			var textureR = new SharpDX.Direct3D11.Texture2D(device, new Texture2DDescription()
			{
				Width = w,
				Height = h,
				MipLevels = 1,
				ArraySize = 1,
				Format = Format.B8G8R8A8_UNorm,
				Usage = ResourceUsage.Default,
				SampleDescription = new SampleDescription(1, 0),
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.Shared
			});

			TextureCreated(textureL);

			var surfaceL = textureL.QueryInterface<SharpDX.DXGI.Surface>();
			var surfaceR = textureR.QueryInterface<SharpDX.DXGI.Surface>();

			// Play the music
			mediaEngineEx.Loop = Loop;

			readyToPlayLoadedVideo = true;

			if(autoPlay) { 
				mediaEngineEx.Play();
				//mediaEngineEx.Volume = 0;
				isPlaying = true;
			}

			long ts;

			#endregion


			OVR.FovPort[] eyeFov = new OVR.FovPort[]
			{ 
				hmd.DefaultEyeFov[0], 
				hmd.DefaultEyeFov[1] 
			};

			OVR.Recti[] eyeRenderViewport	= new OVR.Recti[2];
			eyeRenderViewport[0].Position	= new OVR.Vector2i(0, 0);
			eyeRenderViewport[0].Size		= new OVR.Sizei(renderTargetTextureSize.Width/2, renderTargetTextureSize.Height);
			eyeRenderViewport[1].Position	= new OVR.Vector2i((renderTargetTextureSize.Width + 1) / 2, 0);
			eyeRenderViewport[1].Size		= eyeRenderViewport[0].Size;

			// Query D3D texture data.
			OVR.D3D11.D3D11TextureData[] eyeTexture	= new OVR.D3D11.D3D11TextureData[2];
			eyeTexture[0].Header.API				= OVR.RenderAPIType.D3D11;
			eyeTexture[0].Header.TextureSize		= renderTargetTextureSize;
			eyeTexture[0].Header.RenderViewport		= eyeRenderViewport[0];
			eyeTexture[0].Texture					= renderTargetTexture.NativePointer;
			eyeTexture[0].ShaderResourceView		= renderTargetShaderResourceView.NativePointer;

			// Right eye uses the same texture, but different rendering viewport.
			eyeTexture[1]						= eyeTexture[0];
			eyeTexture[1].Header.RenderViewport = eyeRenderViewport[1];

			// Configure d3d11.
			OVR.D3D11.D3D11ConfigData d3d11cfg	= new OVR.D3D11.D3D11ConfigData();
			d3d11cfg.Header.API						= OVR.RenderAPIType.D3D11;
			d3d11cfg.Header.BackBufferSize		= new OVR.Sizei(hmd.Resolution.Width, hmd.Resolution.Height);
			d3d11cfg.Header.Multisample				= 1;
			d3d11cfg.Device							= device.NativePointer;
			d3d11cfg.DeviceContext					= immediateContext.NativePointer;
			d3d11cfg.BackBufferRenderTargetView		= backBufferRenderTargetView.NativePointer;
			d3d11cfg.SwapChain						= swapChain.NativePointer;

			OVR.EyeRenderDesc[]	eyeRenderDesc = hmd.ConfigureRendering(d3d11cfg, 
				//OVR.DistortionCaps.None
				OVR.DistortionCaps.ovrDistortionCap_Chromatic
				| OVR.DistortionCaps.ovrDistortionCap_Vignette
				| OVR.DistortionCaps.ovrDistortionCap_TimeWarp
				| OVR.DistortionCaps.ovrDistortionCap_Overdrive
				, eyeFov);
			if(eyeRenderDesc == null)
				return;

			// Specify which head tracking capabilities to enable.
			hmd.SetEnabledCaps(OVR.HmdCaps.LowPersistence | OVR.HmdCaps.DynamicPrediction);

			// Start the sensor which informs of the Rift's pose and motion
			hmd.ConfigureTracking(OVR.TrackingCaps.ovrTrackingCap_Orientation | OVR.TrackingCaps.ovrTrackingCap_MagYawCorrection | OVR.TrackingCaps.ovrTrackingCap_Position, OVR.TrackingCaps.None);


			// Get HMD output
			var riftAdapter = (Adapter)dxdevice.Adapter;
			var hmdOutput = riftAdapter.Outputs.FirstOrDefault(o => hmd.DisplayDeviceName.StartsWith(o.Description.DeviceName, StringComparison.OrdinalIgnoreCase));
			if (hmdOutput != null)
			{
				// Set game to fullscreen on rift
				var riftDescription = swapChain.Description.ModeDescription;
				swapChain.ResizeTarget(ref riftDescription);
				swapChain.SetFullscreenState(true, hmdOutput);
				
			}



			// basic effect for drawing sphere config

			SharpDX.Toolkit.Graphics.GraphicsDevice gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(device);

			var basicEffect = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

			basicEffect.PreferPerPixelLighting = false;
			basicEffect.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, textureL);
			
			basicEffect.TextureEnabled = true;
			basicEffect.LightingEnabled = false;

			var basicEffect2 = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

			basicEffect2.PreferPerPixelLighting = false;
			basicEffect2.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, textureR);

			basicEffect2.TextureEnabled = true;
			basicEffect2.LightingEnabled = false;
			
			var primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Sphere.New(gd, radius, 32, true);


			DateTime startTime = DateTime.Now;
			

			float deltaTime = 0;
			float last = 0;
			long frames = 0;
			float timeSinceStart = 0;

			VideoNormalizedRect topRect = new VideoNormalizedRect()
			{
				Left = 0,
				Top = 0,
				Right = 1,
				Bottom = 0.5f
			};

			VideoNormalizedRect bottomRect = new VideoNormalizedRect()
			{
				Left = 0,
				Top = 0.5f,
				Right = 1,
				Bottom = 1f
			};

			Task.Factory.StartNew(() =>
			{
				while (!endPlayer && !AbortSignal) { 
					if (mediaEngine.OnVideoStreamTick(out ts))
					{
						if (_stereoVideo) { 
							mediaEngine.TransferVideoFrame(surfaceL, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
							mediaEngine.TransferVideoFrame(surfaceR, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);
						} else
						{
							mediaEngine.TransferVideoFrame(surfaceL, null, new SharpDX.Rectangle(0, 0, w, h), null);
						}
					}
					Thread.Sleep(10);
				}
			});
			

            RenderLoop.Run(form, () =>
            {
				if (AbortSignal) form.Close();


				frames++;
				timeSinceStart = (float) (DateTime.Now-startTime).TotalSeconds;
				deltaTime = timeSinceStart - last;
				last = timeSinceStart;

				OculusWrap.OVR.HSWDisplayState hasWarningState;
				hmd.GetHSWDisplayState(out hasWarningState);

				// Remove the health and safety warning.
				if(hasWarningState.Displayed == 1)
					hmd.DismissHSWDisplay();

				OVR.FrameTiming frameTiming = hmd.BeginFrame(0); 

                // Clear views
				immediateContext.OutputMerger.SetDepthStencilState(clearDepthStencilState);
                immediateContext.ClearDepthStencilView(renderTargetDepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
                immediateContext.ClearRenderTargetView(renderTargetRenderTargetView, Color.CornflowerBlue);

				float			bodyYaw				= 3.141592f;
				OVR.Vector3f	headPos				= new OVR.Vector3f(0.0f, hmd.GetFloat(OVR.OVR_KEY_EYE_HEIGHT, 1.6f), -5.0f);
				Viewport		viewport			= new Viewport(0, 0, renderTargetTexture.Description.Width, renderTargetTexture.Description.Height, 0.0f, 1.0f);
				OVR.Posef[]		eyeRenderPose		= new OVR.Posef[2];

				immediateContext.OutputMerger.SetDepthStencilState(renderTargetDepthStencilState);
				immediateContext.OutputMerger.SetRenderTargets(renderTargetDepthStencilView, renderTargetRenderTargetView);
				immediateContext.Rasterizer.SetViewport(viewport);

				

				for (int eyeIndex=0; eyeIndex<OVR.Eye_Count; eyeIndex++)
				{
					OVR.EyeType eye = hmd.EyeRenderOrder[eyeIndex];

					eyeRenderPose[(int) eye] = hmd.GetHmdPosePerEye(eye);


					Quaternion	rotationQuaternion	= SharpDXHelpers.ToQuaternion(eyeRenderPose[(int) eye].Orientation);
					Matrix viewMatrix = Matrix.RotationQuaternion(rotationQuaternion);
					viewMatrix.Transpose();
					
					Matrix		projectionMatrix	= Matrix.PerspectiveFovRH((float)(90f * Math.PI / 180f), (float)hmd.Resolution.Width / 2f / hmd.Resolution.Height, 0.001f, 100.0f);
					viewport	= new Viewport(eyeRenderViewport[(int) eye].Position.x, eyeRenderViewport[(int) eye].Position.y, eyeRenderViewport[(int) eye].Size.Width, eyeRenderViewport[(int) eye].Size.Height, 0.0f, 1.0f);
					immediateContext.Rasterizer.SetViewport(viewport);

					LookChanged(viewMatrix);

					basicEffect.World = Matrix.Identity;
					basicEffect.View = viewMatrix;
					basicEffect.Projection = projectionMatrix;

					if (_stereoVideo) { 
						basicEffect2.World = Matrix.Identity;
						basicEffect2.View = viewMatrix;
						basicEffect2.Projection = projectionMatrix;
					}

					if (_stereoVideo)
					{
						if (eyeIndex == 0)
							primitive.Draw(basicEffect2);
						if (eyeIndex == 1)
							primitive.Draw(basicEffect);
					} else
						primitive.Draw(basicEffect);
				}
				
				hmd.EndFrame(eyeRenderPose, eyeTexture);				
			
            });

			isPlaying = false;

			mediaEngine.Shutdown();
			endPlayer = true;
			mediaEngine.Dispose();

			mediaEngineEx.Dispose();

			MediaManager.Shutdown();

			surfaceL.Dispose();
			surfaceR.Dispose();

			textureL.Dispose();
			textureR.Dispose();

            // Release all resources
            shaderSignature.Dispose();
            vertexShaderByteCode.Dispose();
            vertexShader.Dispose();
            pixelShaderByteCode.Dispose();
            pixelShader.Dispose();
            vertexBuffer.Dispose();
            inputLayout.Dispose();
            contantBuffer.Dispose();
            depthBufferTexture.Dispose();
            depthStencilView.Dispose();
            backBufferRenderTargetView.Dispose();
            backBufferTexture.Dispose();
            immediateContext.ClearState();
            immediateContext.Flush();
			//device.Dispose();
			immediateContext.Dispose();
			swapChain.Dispose();
            factory.Dispose();
			hmd.Dispose();
			oculus.Dispose();

        }

		public static void Close()
		{
			isPlaying = false;
			AbortSignal = true;
		}

		public static void SetTime(double time)
		{
			if(mediaEngine != null)
				mediaEngine.CurrentTime = time;
		}

		static Vector4[] m_vertices = new Vector4[]
		{
			// Near
			new Vector4( 1,  1, -1, 1), new Vector4(1, 0, 0, 1),	
			new Vector4( 1, -1, -1, 1), new Vector4(1, 0, 0, 1),	
			new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 0, 1),	
			new Vector4(-1,  1, -1, 1), new Vector4(1, 0, 0, 1),	
			new Vector4( 1,  1, -1, 1), new Vector4(1, 0, 0, 1),	
			new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 0, 1),	
			
			// Far
			new Vector4(-1, -1,  1, 1), new Vector4(0, 1, 0, 1),	
			new Vector4( 1, -1,  1, 1), new Vector4(0, 1, 0, 1),	
			new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 0, 1),	
			new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 0, 1),	
			new Vector4(-1,  1,  1, 1), new Vector4(0, 1, 0, 1),	
			new Vector4(-1, -1,  1, 1), new Vector4(0, 1, 0, 1),	

			// Left
			new Vector4(-1,  1,  1, 1), new Vector4(0, 0, 1, 1),	
			new Vector4(-1,  1, -1, 1), new Vector4(0, 0, 1, 1),	
			new Vector4(-1, -1, -1, 1), new Vector4(0, 0, 1, 1),	
			new Vector4(-1, -1, -1, 1), new Vector4(0, 0, 1, 1),	
			new Vector4(-1, -1,  1, 1), new Vector4(0, 0, 1, 1),	
			new Vector4(-1,  1,  1, 1), new Vector4(0, 0, 1, 1),	

			// Right
			new Vector4( 1, -1, -1, 1), new Vector4(1, 1, 0, 1),	
			new Vector4( 1,  1, -1, 1), new Vector4(1, 1, 0, 1),	
			new Vector4( 1,  1,  1, 1), new Vector4(1, 1, 0, 1),	
			new Vector4( 1,  1,  1, 1), new Vector4(1, 1, 0, 1),	
			new Vector4( 1, -1,  1, 1), new Vector4(1, 1, 0, 1),	
			new Vector4( 1, -1, -1, 1), new Vector4(1, 1, 0, 1),	

			// Bottom
			new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 1, 1),	
			new Vector4( 1, -1, -1, 1), new Vector4(1, 0, 1, 1),	
			new Vector4( 1, -1,  1, 1), new Vector4(1, 0, 1, 1),	
			new Vector4( 1, -1,  1, 1), new Vector4(1, 0, 1, 1),	
			new Vector4(-1, -1,  1, 1), new Vector4(1, 0, 1, 1),	
			new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 1, 1),	

			// Top
			new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 1, 1),	
			new Vector4( 1,  1, -1, 1), new Vector4(0, 1, 1, 1),	
			new Vector4(-1,  1, -1, 1), new Vector4(0, 1, 1, 1),	
			new Vector4(-1,  1, -1, 1), new Vector4(0, 1, 1, 1),	
			new Vector4(-1,  1,  1, 1), new Vector4(0, 1, 1, 1),
			new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 1, 1)	
		};


		public static void PlayLoadedFile()
		{
			if (readyToPlayLoadedVideo)
			{
				mediaEngine.Play();
				isPlaying = true;
			}
		}

		public static void PlayPause()
		{
			if(mediaEngine!= null)
				if (isPlaying)
				{
					if (mediaEngine.IsPaused) mediaEngine.Play();
					else mediaEngine.Pause();
				} else
				{
					if (readyToPlayLoadedVideo)
						PlayLoadedFile();
				}
		}

		public static void Pause()
		{
			if (mediaEngine != null)
				if (isPlaying && !mediaEngine.IsPaused)
				{
					mediaEngine.Pause();
				}
		}

		public static void UnPause()
		{
			if (mediaEngine != null)
				if (isPlaying && mediaEngine.IsPaused)
				{
					mediaEngine.Play();
				}
		}

		public static void Rewind()
		{
			if (mediaEngine != null)
			{
				if (isPlaying)
				{
					mediaEngine.Pause();
					SetTime(0);
				}
				else
				{
					mediaEngine.Play();
					mediaEngine.Pause();
				}
			}
		}

		public static void SetSphereSize(double value)
		{
			_sphereSize = (float)value;
			_sphereSizeChanged = true;
		}
	}
}
