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
using System.Diagnostics;

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

		private static Texture2D textureL;
		private static Texture2D textureR;

		public static Texture2D videoTextureL;
		public static Texture2D videoTextureR;

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
			bool success = oculus.Initialize();

			if (!success)
			{
				MessageBox.Show("Failed to initialize the Oculus runtime library.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

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


			// Specify which head tracking capabilities to enable.
			hmd.SetEnabledCaps(OVR.HmdCaps.LowPersistence | OVR.HmdCaps.DynamicPrediction);

			// Start the sensor which informs of the Rift's pose and motion	
			hmd.ConfigureTracking(OVR.TrackingCaps.ovrTrackingCap_Orientation | OVR.TrackingCaps.ovrTrackingCap_MagYawCorrection | OVR.TrackingCaps.ovrTrackingCap_Position, OVR.TrackingCaps.None);


			// Create a set of layers to submit.
			EyeTexture[] eyeTextures = new EyeTexture[2];
			OVR.ovrResult ovrResult;





			// Create DirectX Graphics Interface factory, used to create the swap chain.
			SharpDX.DXGI.Factory factory = new SharpDX.DXGI.Factory();
			string lines = "";
			foreach (Adapter a in factory.Adapters)
			{
				lines += a.Description.Description + "\n";
			}

			FeatureLevel[] levels = new FeatureLevel[] { FeatureLevel.Level_11_0 };
			
			SharpDX.Direct3D11.Device device = null;


			// TODO dopracowac wybor adaptera dla systemow z wieloma GPU

			//if (factory.Adapters.Any(a => a.Description.Description.ToLower().Contains("amd")))
			//{
			//	Adapter adapter = factory.Adapters.First(a => a.Description.Description.ToLower().Contains("amd"));
			//	//device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport | DeviceCreationFlags.Debug, levels);
			//	device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, levels);
			//}
			//else if (factory.Adapters.Any(a => a.Description.Description.ToLower().Contains("nvidia")))
			//{
			//	Adapter adapter = factory.Adapters.First(a => a.Description.Description.ToLower().Contains("nvidia"));
			//	//device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport | DeviceCreationFlags.Debug, levels);
			//	device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, levels);
			//}
			//else
			//{
			//	// Create DirectX drawing device.
			//	//device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport | DeviceCreationFlags.Debug, levels);
			//	device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport, levels);
			//}

			// Create DirectX drawing device.
			device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug, levels);


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
			//TODO uncomment
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
			
			DeviceContext immediateContext = device.ImmediateContext;
			
			
			//Texture2DDescription swapChainTextureDescription = new Texture2DDescription()
			//{
			//	Width = 1920,
			//	Height = 1080,
			//	MipLevels = 1,
			//	ArraySize = 1,
			//	Format = Format.B8G8R8A8_UNorm,
			//	Usage = ResourceUsage.Default,
			//	SampleDescription = new SampleDescription(1, 0),
			//	BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
			//	CpuAccessFlags = CpuAccessFlags.None,
			//	OptionFlags = ResourceOptionFlags.Shared
			//};
			
			//Texture2D swapChainTexture = new Texture2D(device, swapChainTextureDescription);


			// Define the properties of the swap chain.
			SwapChainDescription swapChainDescription						= new SwapChainDescription();
			swapChainDescription.BufferCount								= 1;
			swapChainDescription.IsWindowed									= true;
			swapChainDescription.OutputHandle								= form.Handle;
			swapChainDescription.SampleDescription							= new SampleDescription(1, 0);
			swapChainDescription.Usage										= Usage.RenderTargetOutput | Usage.ShaderInput;
			swapChainDescription.SwapEffect									= SwapEffect.Sequential;
			swapChainDescription.Flags										= SwapChainFlags.AllowModeSwitch;
			// TODO change to form.width and form.height
			swapChainDescription.ModeDescription.Width						= 1920;
			swapChainDescription.ModeDescription.Height						= 1080;
			swapChainDescription.ModeDescription.Format						= Format.R8G8B8A8_UNorm;
			swapChainDescription.ModeDescription.RefreshRate.Numerator		= 0;
			swapChainDescription.ModeDescription.RefreshRate.Denominator	= 1;

			// Create the swap chain.
			SharpDX.DXGI.SwapChain	swapChain	= new SwapChain(factory, device, swapChainDescription);

			// Retrieve the back buffer of the swap chain.
			Texture2D			backBufferTexture				= swapChain.GetBackBuffer<Texture2D>(0);				// = BackBuffer
			RenderTargetView	backBufferRenderTargetView		= new RenderTargetView(device, backBufferTexture);		// = BackBufferRT

		

			//Texture2D backBufferTexture = new Texture2D(device, swapChainTextureDescription);
			//RenderTargetView backBufferRenderTargetView = new RenderTargetView(device, backBufferTexture);      // = BackBufferRT

			// Create a depth buffer, using the same width and height as the back buffer.
			Texture2DDescription depthBufferDescription = new Texture2DDescription();
			depthBufferDescription.Format				= Format.D32_Float;
			depthBufferDescription.ArraySize			= 1;
			depthBufferDescription.MipLevels			= 1;
			// TODO change to form.width and form.height
			depthBufferDescription.Width				= 1920;
			depthBufferDescription.Height				= 1080;
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
			Viewport viewport = new Viewport(0, 0, hmd.Resolution.Width, hmd.Resolution.Height, 0.0f, 1.0f);
			
			immediateContext.OutputMerger.SetDepthStencilState(depthStencilState);
			immediateContext.OutputMerger.SetRenderTargets(depthStencilView, backBufferRenderTargetView);
			immediateContext.Rasterizer.SetViewport(viewport);
			


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
            //DeviceContext immediateContext						= device.ImmediateContext;
            immediateContext.InputAssembler.InputLayout			= new InputLayout(device, shaderSignature, inputElements);
            immediateContext.InputAssembler.PrimitiveTopology	= PrimitiveTopology.TriangleList;
            immediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, sizeof(float)*4*2, 0));
            immediateContext.VertexShader.SetConstantBuffer(0, contantBuffer);
            immediateContext.VertexShader.Set(vertexShader);
            immediateContext.PixelShader.Set(pixelShader);

			// Retrieve the DXGI device, in order to set the maximum frame latency.
			using (SharpDX.DXGI.Device1 dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device1>())
			{
				dxgiDevice.MaximumFrameLatency = 1;
			}

			#endregion



			#region Media Playback configuration

			var resourceL =  videoTextureL.QueryInterface<SharpDX.DXGI.Resource>();
			textureL = device.OpenSharedResource<Texture2D>(resourceL.SharedHandle);

			var resourceR = videoTextureR.QueryInterface<SharpDX.DXGI.Resource>();
			textureR = device.OpenSharedResource<Texture2D>(resourceR.SharedHandle);

			/*

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
			if (fileName.Contains("http://") || fileName.Contains("https://"))
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

			Texture2DDescription frameTextureDescription = new Texture2DDescription()
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
			};


			var textureL = new SharpDX.Direct3D11.Texture2D(device, frameTextureDescription);
			var textureR = new SharpDX.Direct3D11.Texture2D(device, frameTextureDescription);

			

			TextureCreated(textureL);

			var surfaceL = textureL.QueryInterface<SharpDX.DXGI.Surface>();
			var surfaceR = textureR.QueryInterface<SharpDX.DXGI.Surface>();

			bool surfaceFirst = true;

			// Play the music
			mediaEngineEx.Loop = Loop;

			readyToPlayLoadedVideo = true;

			if(autoPlay) { 
				mediaEngineEx.Play();
				mediaEngineEx.Volume = 0;
				isPlaying = true;
			}

			long ts;
			*/
			#endregion


			#region configure hmd layers and rendering

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
				ovrResult = hmd.CreateSwapTextureSetD3D11(device.NativePointer, ref swapTextureDescriptionD3D11, out eyeTexture.SwapTextureSet);
				WriteErrorDetails(oculus, ovrResult, "Failed to create swap texture set.");
				

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
				layerEyeFov.Header.Flags = OVR.LayerFlags.None;
			}



			#endregion




			// basic effect for drawing sphere config

			SharpDX.Toolkit.Graphics.GraphicsDevice gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(device);

			var basicEffectL = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

			basicEffectL.PreferPerPixelLighting = false;
			basicEffectL.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, textureL);
			
			basicEffectL.TextureEnabled = true;
			basicEffectL.LightingEnabled = false;

			var basicEffectR = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

			basicEffectR.PreferPerPixelLighting = false;
			basicEffectR.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, textureR);

			basicEffectR.TextureEnabled = true;
			basicEffectR.LightingEnabled = false;



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

			ManualResetEvent waitForOculus = new ManualResetEvent(false);

			//Task.Factory.StartNew(() =>
			//{
			//	while (!endPlayer && !AbortSignal)
			//	{
					

			//		if (!mediaEngine.IsPaused && mediaEngine.OnVideoStreamTick(out ts))
			//		{
			//			//waitForOculus.WaitOne();

			//			if (_stereoVideo)
			//			{
			//				mediaEngine.TransferVideoFrame(textureL, topRect, new SharpDX.Rectangle(0, 0, w, h), null);
			//				Thread.Sleep(1);
			//				mediaEngine.TransferVideoFrame(textureR, bottomRect, new SharpDX.Rectangle(0, 0, w, h), null);
			//			}
			//			else
			//			{
			//				mediaEngine.TransferVideoFrame(textureL, null, new SharpDX.Rectangle(0, 0, w, h), null);
			//			}
						
			//		}
			//		Thread.Sleep(1);
			//	}
			//});

			RenderLoop.Run(form, () =>
			//Task.Factory.StartNew(() =>
            {
				if (AbortSignal) form.Close();
				//while (!AbortSignal)
				//{

					OVR.Vector3f[] hmdToEyeViewOffsets = { eyeTextures[0].HmdToEyeViewOffset, eyeTextures[1].HmdToEyeViewOffset };
					OVR.FrameTiming frameTiming = hmd.GetFrameTiming(0);
					OVR.TrackingState trackingState = hmd.GetTrackingState(frameTiming.DisplayMidpointSeconds);
					OVR.Posef[] eyePoses = new OVR.Posef[2];



					// Calculate the position and orientation of each eye.
					oculus.CalcEyePoses(trackingState.HeadPose.ThePose, hmdToEyeViewOffsets, ref eyePoses);

					frames++;
					timeSinceStart = (float)(DateTime.Now - startTime).TotalSeconds;
					deltaTime = timeSinceStart - last;
					last = timeSinceStart;


					for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
					{
						OVR.EyeType eye = (OVR.EyeType)eyeIndex;
						EyeTexture eyeTexture = eyeTextures[eyeIndex];

						layerEyeFov.RenderPose[eyeIndex] = eyePoses[eyeIndex];

						// Retrieve the index of the active texture and select the next texture as being active next.
						int textureIndex = eyeTexture.SwapTextureSet.CurrentIndex++;

						immediateContext.OutputMerger.SetRenderTargets(eyeTexture.DepthStencilView, eyeTexture.RenderTargetViews[textureIndex]);
						immediateContext.ClearRenderTargetView(eyeTexture.RenderTargetViews[textureIndex], Color.Black);
						immediateContext.ClearDepthStencilView(eyeTexture.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

						Viewport viewportCorrrected = eyeTexture.Viewport;
						//viewportCorrrected.
						immediateContext.Rasterizer.SetViewport(viewportCorrrected);

						Quaternion rotationQuaternion = SharpDXHelpers.ToQuaternion(eyePoses[eyeIndex].Orientation);
						Matrix viewMatrix = Matrix.RotationQuaternion(rotationQuaternion);
						viewMatrix.Transpose();

						Matrix projectionMatrix = Matrix.PerspectiveFovRH((float)(90f * Math.PI / 180f), (float)hmd.Resolution.Width / 2f / hmd.Resolution.Height, 0.001f, 100.0f);

						LookChanged(viewMatrix);

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
				//}
			});

			isPlaying = false;

			mediaEngine.Shutdown();
			//endPlayer = true;
			//mediaEngine.Dispose();

			//mediaEngineEx.Dispose();

			MediaManager.Shutdown();

			//surfaceL.Dispose();
			//surfaceR.Dispose();

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
			device.Dispose();
			immediateContext.Dispose();
			swapChain.Dispose();
			factory.Dispose();
			hmd.Dispose();
			oculus.Dispose();


        }

		/// <summary>
		/// Write out any error details received from the Oculus SDK, into the debug output window.
		/// 
		/// Please note that writing text to the debug output window is a slow operation and will affect performance,
		/// if too many messages are written in a short timespan.
		/// </summary>
		/// <param name="oculus">OculusWrap object for which the error occurred.</param>
		/// <param name="result">Error code to write in the debug text.</param>
		/// <param name="message">Error message to include in the debug text.</param>
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
