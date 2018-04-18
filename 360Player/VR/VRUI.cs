using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using Device = SharpDX.Direct3D11.Device;
using DX2D = SharpDX.Direct2D1;
using Bivrost.Bivrost360Player.Tools;
using Bivrost.Bivrost360Player.Streaming;

namespace Bivrost.Bivrost360Player
{
	public class VRUI : IDisposable
	{
		Texture2D uiTexture;
		public SharpDX.Toolkit.Graphics.BasicEffect uiEffect;

		DX2D.RenderTarget target2d;
		SharpDX.Toolkit.Graphics.GeometricPrimitive uiPrimitive;

		SharpDX.DirectWrite.TextFormat textFormat;
		SharpDX.DirectWrite.TextFormat textFormatSmall;
		DX2D.SolidColorBrush textBrush;
		DX2D.SolidColorBrush blueBrush;

		bool uiInitialized = false;
		bool redraw = false;

		const float uiDistanceStart = 1.5f;
		const float uiDistanceFade = 0.5f;
		const float uiDistanceDisappear = 0.25f;

		public VRUI(Device device, SharpDX.Toolkit.Graphics.GraphicsDevice gd)
		{
			Texture2DDescription uiTextureDescription = new Texture2DDescription()
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

			using (DX2D.Factory factory2d = new DX2D.Factory(SharpDX.Direct2D1.FactoryType.SingleThreaded, DX2D.DebugLevel.Information))
			{
				DX2D.RenderTargetProperties renderTargetProperties = new DX2D.RenderTargetProperties()
				{
					DpiX = 96,
					DpiY = 96,
					PixelFormat = new DX2D.PixelFormat(Format.B8G8R8A8_UNorm, DX2D.AlphaMode.Premultiplied),
					Type = DX2D.RenderTargetType.Hardware,
					MinLevel = DX2D.FeatureLevel.Level_10,
					Usage = DX2D.RenderTargetUsage.None
				};
				using (var uiSurface = uiTexture.QueryInterface<Surface>())
					target2d = new DX2D.RenderTarget(factory2d, uiSurface, renderTargetProperties)
					{
						AntialiasMode = DX2D.AntialiasMode.PerPrimitive
					};
			}


			// 2D materials
			uiEffect = new SharpDX.Toolkit.Graphics.BasicEffect(gd)
			{
				PreferPerPixelLighting = false,
				Texture = SharpDX.Toolkit.Graphics.Texture2D.New(gd, uiTexture),
				TextureEnabled = true,
				LightingEnabled = false
			};


			BlendStateDescription blendStateDescription = new BlendStateDescription()
			{
				AlphaToCoverageEnable = false
			};
			blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
			blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
			blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
			blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
			blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
			blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
			blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
			blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

			using (var blendState = SharpDX.Toolkit.Graphics.BlendState.New(gd, blendStateDescription))
				gd.SetBlendState(blendState);

			uiPrimitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Plane.New(gd, 2, 1);

			using (SharpDX.DirectWrite.Factory factoryDW = new SharpDX.DirectWrite.Factory())
			{
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
			}

			textBrush = new SharpDX.Direct2D1.SolidColorBrush(target2d, new Color(1f, 1f, 1f, 1f));
			blueBrush = new SharpDX.Direct2D1.SolidColorBrush(target2d, new Color(0, 167, 245, 255));

			uiInitialized = true;
		}


		public void EnqueueUIRedraw()
		{
			redraw = true;
		}


		public void Draw(ServiceResult serviceResult, float currentTime, float duration)
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

			if (serviceResult.contentType == ServiceResult.ContentType.video)
			{
				target2d.DrawText("now playing:", textFormatSmall, new RectangleF(0, 0, 1024, 100), textBrush);
				target2d.DrawText(serviceResult.TitleWithFallback, textFormat, new RectangleF(0, 50, 1024, 100), textBrush);

				var barLength = 1024 - 265;
				var currentLength = barLength * (currentTime / duration);

				target2d.DrawLine(new Vector2(128, 384), new Vector2(1024 - 128, 384), textBrush, 6);
				target2d.DrawLine(new Vector2(128, 384), new Vector2(128 + currentLength, 384), blueBrush, 6);
				var ellipse = new DX2D.Ellipse(new Vector2(128 + currentLength + 3.5f, 384), 7, 7);
				target2d.FillEllipse(ellipse, blueBrush);

				target2d.DrawEllipse(new DX2D.Ellipse(new Vector2(512, 256), 48, 48), textBrush, 2);
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
			}

			target2d.EndDraw();
		}


		public bool isUIHidden = true;
		private float showAlpha = 0f;


		public void Render(float deltaTime, Matrix viewMatrix, Matrix projectionMatrix, Vector3 viewPosition, bool pause)
		{
			uiEffect.View = viewMatrix;
			uiEffect.Projection = projectionMatrix;

			showAlpha = showAlpha.LerpInPlace(pause ? 1 : 0, 8f * deltaTime);
			if (showAlpha < 0.01f)
				showAlpha = 0f;
			else if (showAlpha > 0.99f)
				showAlpha = 1f;

			isUIHidden = showAlpha <= 0;

			// distance ui plane - eye
			// { 0    for d <= uiDistanceDisappear
			// { 0..1 for uiDistanceDisappear  d < uiDistanceFade
			// { 1    for d >= uiDistanceFade
			float dot;
			Vector3.Dot(ref uiPlane.Normal, ref viewPosition, out dot);
			float d = Math.Abs(dot - uiPlane.D);
			float overrideShowAlpha = (d - uiDistanceDisappear) / uiDistanceFade;
			if (overrideShowAlpha < 0) overrideShowAlpha = 0;
			else if (overrideShowAlpha > 1) overrideShowAlpha = 1;

			uiEffect.Alpha = showAlpha * overrideShowAlpha;

			if (uiEffect.Alpha > 0)
				uiPrimitive.Draw(uiEffect);
		}	


		Plane uiPlane = new Plane(1);

		internal void SetWorldPosition(Vector3 forward, Vector3 viewPosition, bool reverseDistance)
		{
			float yaw = (float)(Math.PI - Math.Atan2(forward.X, forward.Z));
			uiEffect.World = Matrix.Identity * Matrix.Scaling(1f) * Matrix.Translation(0, 0, reverseDistance ? -uiDistanceStart : uiDistanceStart) * Matrix.RotationAxis(Vector3.Up, yaw) * Matrix.Translation(viewPosition);
			uiPlane = new Plane(-uiEffect.World.TranslationVector, uiEffect.World.Forward);
		}


		public void Dispose()
		{
			uiInitialized = false;

			uiTexture?.Dispose();
			uiEffect?.Dispose();

			target2d?.Dispose();
			uiPrimitive?.Dispose();

			textFormat?.Dispose();
			textFormatSmall?.Dispose();
			textBrush?.Dispose();
			blueBrush?.Dispose();
		}

	}
}
