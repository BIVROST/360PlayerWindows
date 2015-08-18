using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace PlayerUI.Oculus
{
	public class OculusUIDebug
	{
		SharpDX.Windows.RenderForm form;
        bool running = false;
		Texture2D sharedTexture;

		public void SetSharedTexture(Texture2D externalTexture)
		{
			sharedTexture = externalTexture;
		}

		public void Start()
		{
			running = true;
			Task.Factory.StartNew(() =>
			{
				form = new SharpDX.Windows.RenderForm("Oculus UI Debug");
				form.Width = 1024 + 16;
				form.Height = 512 + 39;
				form.AllowUserResizing = false;

				// Create DirectX drawing device.
				SharpDX.Direct3D11.Device device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug);

				// Create DirectX Graphics Interface factory, used to create the swap chain.
				Factory factory = new Factory();

				DeviceContext immediateContext = device.ImmediateContext;

				// Define the properties of the swap chain.
				SwapChainDescription swapChainDescription = new SwapChainDescription();
				swapChainDescription.BufferCount = 1;
				swapChainDescription.IsWindowed = true;
				swapChainDescription.OutputHandle = form.Handle;
				swapChainDescription.SampleDescription = new SampleDescription(1, 0);
				swapChainDescription.Usage = Usage.RenderTargetOutput | Usage.ShaderInput;
				swapChainDescription.SwapEffect = SwapEffect.Sequential;
				swapChainDescription.Flags = SwapChainFlags.AllowModeSwitch;
				swapChainDescription.ModeDescription.Width = 1024;
				swapChainDescription.ModeDescription.Height = 512;
				swapChainDescription.ModeDescription.Format = Format.R8G8B8A8_UNorm;
				swapChainDescription.ModeDescription.RefreshRate.Numerator = 0;
				swapChainDescription.ModeDescription.RefreshRate.Denominator = 1;

				// Create the swap chain.
				SharpDX.DXGI.SwapChain swapChain = new SwapChain(factory, device, swapChainDescription);

				// Retrieve the back buffer of the swap chain.
				Texture2D backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
				RenderTargetView backBufferRenderTargetView = new RenderTargetView(device, backBuffer);

				// Create a depth buffer, using the same width and height as the back buffer.
				Texture2DDescription depthBufferDescription = new Texture2DDescription();
				depthBufferDescription.Format = Format.D32_Float;
				depthBufferDescription.ArraySize = 1;
				depthBufferDescription.MipLevels = 1;
				depthBufferDescription.Width = 1024;
				depthBufferDescription.Height = 512;
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
				Viewport viewport = new Viewport(0, 0, 1024, 512, 0.0f, 1.0f);

				immediateContext.OutputMerger.SetDepthStencilState(depthStencilState);
				immediateContext.OutputMerger.SetRenderTargets(depthStencilView, backBufferRenderTargetView);
				immediateContext.Rasterizer.SetViewport(viewport);


				SharpDX.Toolkit.Graphics.GraphicsDevice gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(device);


				var blendStateDescription = new BlendStateDescription();

				blendStateDescription.AlphaToCoverageEnable = false;

				blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
				blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
				blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
				blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
				blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
				blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
				blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
				blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

				var blendState = SharpDX.Toolkit.Graphics.BlendState.New(gd, blendStateDescription);
				gd.SetBlendState(blendState);


				var resource = sharedTexture.QueryInterface<SharpDX.DXGI.Resource>();
				var texture = device.OpenSharedResource<Texture2D>(resource.SharedHandle);

				var basicEffect = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

				basicEffect.PreferPerPixelLighting = false;
				basicEffect.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, texture);

				basicEffect.TextureEnabled = true;
				basicEffect.LightingEnabled = false;

				// background texture
				var backgroundTexture = SharpDX.Toolkit.Graphics.Texture2D.Load(gd, "Graphics/debug.png");
				var backEffect = new SharpDX.Toolkit.Graphics.BasicEffect(gd);

				backEffect.PreferPerPixelLighting = false;
				backEffect.Texture = backgroundTexture;

				backEffect.TextureEnabled = true;
				backEffect.LightingEnabled = false;

				


				var primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(gd, 2f, 2f, 1);

				// Retrieve the DXGI device, in order to set the maximum frame latency.
				using (SharpDX.DXGI.Device1 dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device1>())
				{
					dxgiDevice.MaximumFrameLatency = 1;
				}

				RenderLoop.Run(form, () =>
				{
					immediateContext.ClearRenderTargetView(backBufferRenderTargetView, new Color4(1f, 0.5f, 0.3f, 1f));
					immediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

					backEffect.World = basicEffect.World = Matrix.Identity;
					backEffect.View = basicEffect.View = Matrix.Identity;
					backEffect.Projection = basicEffect.Projection = Matrix.Identity;

					primitive.Draw(backEffect);
					primitive.Draw(basicEffect);

					swapChain.Present(0, PresentFlags.None);

					if (!running) form.Close();
				});


			});
		}

		public void Stop()
		{
			running = false;
			//form.Close();
		}
	}
}
