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
using DX2D = SharpDX.Direct2D1;
using System.Runtime.InteropServices;
using PlayerUI.Tools;

namespace PlayerUI.Oculus
{
	public partial class OculusPlayback
	{
		static Texture2DDescription uiTextureDescription;
		static Texture2D uiTexture;
		static SharpDX.DXGI.Surface uiSurface;
		static SharpDX.Toolkit.Graphics.BasicEffect uiEffect;

		static BlendStateDescription blendStateDescription;
		static SharpDX.Toolkit.Graphics.BlendState blendState;
		static DX2D.RenderTarget target2d;
		static SharpDX.Toolkit.Graphics.GeometricPrimitive uiPrimitive;

		static SharpDX.DirectWrite.TextFormat textFormat;
		static SharpDX.DirectWrite.TextFormat textFormatSmall;
		static DX2D.SolidColorBrush textBrush;
		static DX2D.SolidColorBrush blueBrush;

		static bool uiInitialized = false;
		static bool redraw = false;

		private static void InitUI(Device device, SharpDX.Toolkit.Graphics.GraphicsDevice gd)
		{
			uiTextureDescription = new Texture2DDescription()
			{
				Width = 1024,
				Height = 512,
				MipLevels = 1,
				ArraySize = 1,
				Format = Format.B8G8R8A8_UNorm,
				Usage = ResourceUsage.Default,
				SampleDescription = new SampleDescription(1, 0),
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.Shared
			};


			uiTexture = new SharpDX.Direct3D11.Texture2D(device, uiTextureDescription);
			uiSurface = uiTexture.QueryInterface<Surface>();

			DX2D.Factory factory2d = new DX2D.Factory(SharpDX.Direct2D1.FactoryType.SingleThreaded, DX2D.DebugLevel.Information);
			DX2D.RenderTargetProperties renderTargetProperties = new DX2D.RenderTargetProperties()
			{
				DpiX = 96,
				DpiY = 96,
				PixelFormat = new DX2D.PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
				Type = SharpDX.Direct2D1.RenderTargetType.Hardware,
				MinLevel = DX2D.FeatureLevel.Level_10,
				Usage = SharpDX.Direct2D1.RenderTargetUsage.None
			};
			target2d = new DX2D.RenderTarget(factory2d, uiSurface, renderTargetProperties);
			SharpDX.DirectWrite.Factory factoryDW = new SharpDX.DirectWrite.Factory();


			// 2D materials

			uiEffect = new SharpDX.Toolkit.Graphics.BasicEffect(gd);
			uiEffect.PreferPerPixelLighting = false;
			uiEffect.Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, uiTexture);
			uiEffect.TextureEnabled = true;
			uiEffect.LightingEnabled = false;


			blendStateDescription = new BlendStateDescription();

			blendStateDescription.AlphaToCoverageEnable = false;

			blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
			blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
			blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
			blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
			blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
			blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
			blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
			blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

			blendState = SharpDX.Toolkit.Graphics.BlendState.New(gd, blendStateDescription);
			gd.SetBlendState(blendState);

			uiPrimitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(gd, 2, 1);

			textFormat = new SharpDX.DirectWrite.TextFormat(factoryDW, "Segoe UI Light", 34f)
			{
				TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
				ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
			};

			textFormatSmall = new SharpDX.DirectWrite.TextFormat(factoryDW, "Segoe UI Light", 20f)
			{
				TextAlignment = SharpDX.DirectWrite.TextAlignment.Center,
				ParagraphAlignment = SharpDX.DirectWrite.ParagraphAlignment.Center
			};

			textBrush = new SharpDX.Direct2D1.SolidColorBrush(target2d, new Color(1f, 1f, 1f, 1f));
			blueBrush = new SharpDX.Direct2D1.SolidColorBrush(target2d, new Color(0, 167, 245, 255));

			target2d.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;

			uiInitialized = true;
		}

		private static void EnqueueUIRedraw()
		{
			redraw = true;
		}

		private static void DrawUI()
		{
			if (!uiInitialized) return;
			if (!redraw) return;
			redraw = false;

			target2d.BeginDraw();
			target2d.Clear(new Color4(0, 0, 0, 0.7f));
			target2d.DrawLine(new Vector2(0, 1), new Vector2(1024, 1), blueBrush, 2);
			target2d.DrawLine(new Vector2(0, 511), new Vector2(1024, 511), blueBrush, 2);

			textFormat.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			textFormatSmall.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			target2d.DrawText("now playing:", textFormatSmall, new RectangleF(0, 0, 1024, 100), textBrush);
			target2d.DrawText(movieTitle, textFormat, new RectangleF(0, 50, 1024, 100), textBrush);

			var barLength = 1024 - 265;
			var currentLength = barLength * (currentTime / duration);

			target2d.DrawLine(new Vector2(128, 384), new Vector2(1024 - 128, 384), textBrush, 6);
			target2d.DrawLine(new Vector2(128, 384), new Vector2(128 + currentLength, 384), blueBrush, 6);
			var ellipse = new DX2D.Ellipse(new Vector2(128 + currentLength + 3.5f, 384), 7, 7);
			target2d.FillEllipse(ellipse, blueBrush);

			target2d.DrawEllipse(new SharpDX.Direct2D1.Ellipse(new Vector2(512, 256), 48, 48), textBrush, 2);
			var dist = 8;
			var len = 10;
			target2d.DrawLine(new Vector2(512 - dist, 256 - len), new Vector2(512 - dist, 256 + len), textBrush, 2);
			target2d.DrawLine(new Vector2(512 + dist, 256 - len), new Vector2(512 + dist, 256 + len), textBrush, 2);

			textFormatSmall.TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
			target2d.DrawText((new TimeSpan(0, 0, (int)Math.Floor(duration))).ToString(), textFormatSmall, new Rectangle(1024 - 128 - 150, 384, 150, 50), textBrush);


			int positionx = (int)(128 + currentLength - 74 / 2);
			positionx = MathUtil.Clamp(positionx, 128, 1024 - 128 - 74);
			textFormatSmall.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
			target2d.DrawText((new TimeSpan(0, 0, (int)Math.Floor(currentTime))).ToString(), textFormatSmall, new Rectangle(positionx, 340, 74, 32), textBrush);

			target2d.EndDraw();
		}

		private static bool isUIHidden = true;
		private static float showAlpha = 0f;
		private static float overrideShowAlpha = 1f;
		private static void RenderUI(float deltaTime)
		{
			showAlpha = showAlpha.LerpInPlace(pause ? 1 : 0, 8f * deltaTime);
			if (showAlpha < 0.01f)
				showAlpha = 0f;
			else if (showAlpha > 0.99f)
				showAlpha = 1f;

			isUIHidden = showAlpha <= 0;

			uiEffect.Alpha = showAlpha * overrideShowAlpha;

			if (uiEffect.Alpha > 0)
				uiPrimitive.Draw(uiEffect);
		}

		private static void DisposeUI()
		{
			uiInitialized = false;

			uiTexture?.Dispose();
			uiSurface?.Dispose();
			uiEffect?.Dispose();

			blendState?.Dispose();
			target2d?.Dispose();
			uiPrimitive?.Dispose();

			textFormat?.Dispose();
			textFormatSmall?.Dispose();
			textBrush?.Dispose();
			blueBrush?.Dispose();
		}

	}
}
