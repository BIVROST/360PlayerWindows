﻿using OculusWrap;
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
		public static Texture2D textureL;
		public static Texture2D textureR;
		public static float radius = 4.9f;
		public static bool _stereoVideo = false;
		public static MediaDecoder.ProjectionMode _projection = MediaDecoder.ProjectionMode.Sphere;

		public static ManualResetEvent waitForRendererStop = new ManualResetEvent(false);
		public static bool abort = false;
		public static bool pause = false;

		private static string movieTitle = "";
		private static float duration = 0;
		private static float currentTime = 0;

		private static SharpDX.Toolkit.Graphics.BasicEffect basicEffectL;
		private static SharpDX.Toolkit.Graphics.BasicEffect basicEffectR;

		private static bool _playbackLock = false;
        public static bool Lock { get { return _playbackLock; } }
		private static object localCritical = new object();


		//public static readonly SharpDX.Toolkit.Graphics.EffectData EffectBytecode = SharpDX.Toolkit.Graphics.EffectData.Load(new byte[] {
		//84, 75, 70, 88, 251, 7, 0, 0, 1, 1, 0, 0, 83, 72, 68, 82, 184, 7, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 145, 0, 0, 188, 6, 68, 88, 66, 67, 78, 222, 27, 125, 77, 231, 207, 149, 84, 242, 51, 75, 92, 54, 62, 202, 1, 0, 0, 0, 60, 3, 0, 0, 4, 0, 0, 0,
		//48, 0, 0, 0, 28, 1, 0, 0, 84, 2, 0, 0, 200, 2, 0, 0, 65, 111, 110, 57, 228, 0, 0, 0, 228, 0, 0, 0, 0, 2, 254, 255, 176, 0, 0, 0, 52, 0, 0, 0, 1, 0, 36, 0, 0, 0, 48, 0, 0, 0, 48, 0, 0, 0, 36, 0, 1, 0, 48, 0, 0, 0, 0, 0,
		//4, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 254, 255, 31, 0, 0, 2, 5, 0, 0, 128, 0, 0, 15, 144, 31, 0, 0, 2, 5, 0, 1, 128, 1, 0, 15, 144, 31, 0, 0, 2, 5, 0, 2, 128, 2, 0, 15, 144, 5, 0, 0, 3, 0, 0, 15, 128, 2, 0, 85, 144,
		//2, 0, 228, 160, 4, 0, 0, 4, 0, 0, 15, 128, 2, 0, 0, 144, 1, 0, 228, 160, 0, 0, 228, 128, 4, 0, 0, 4, 0, 0, 15, 128, 2, 0, 170, 144, 3, 0, 228, 160, 0, 0, 228, 128, 4, 0, 0, 4, 0, 0, 15, 128, 2, 0, 255, 144, 4, 0, 228, 160, 0, 0, 228, 128,
		//4, 0, 0, 4, 0, 0, 3, 192, 0, 0, 255, 128, 0, 0, 228, 160, 0, 0, 228, 128, 1, 0, 0, 2, 0, 0, 12, 192, 0, 0, 228, 128, 1, 0, 0, 2, 0, 0, 15, 224, 0, 0, 228, 144, 1, 0, 0, 2, 1, 0, 3, 224, 1, 0, 228, 144, 255, 255, 0, 0, 83, 72, 68, 82,
		//48, 1, 0, 0, 64, 0, 1, 0, 76, 0, 0, 0, 89, 0, 0, 4, 70, 142, 32, 0, 0, 0, 0, 0, 4, 0, 0, 0, 95, 0, 0, 3, 242, 16, 16, 0, 0, 0, 0, 0, 95, 0, 0, 3, 50, 16, 16, 0, 1, 0, 0, 0, 95, 0, 0, 3, 242, 16, 16, 0, 2, 0, 0, 0,
		//101, 0, 0, 3, 242, 32, 16, 0, 0, 0, 0, 0, 101, 0, 0, 3, 50, 32, 16, 0, 1, 0, 0, 0, 103, 0, 0, 4, 242, 32, 16, 0, 2, 0, 0, 0, 1, 0, 0, 0, 104, 0, 0, 2, 1, 0, 0, 0, 54, 0, 0, 5, 242, 32, 16, 0, 0, 0, 0, 0, 70, 30, 16, 0,
		//0, 0, 0, 0, 54, 0, 0, 5, 50, 32, 16, 0, 1, 0, 0, 0, 70, 16, 16, 0, 1, 0, 0, 0, 56, 0, 0, 8, 242, 0, 16, 0, 0, 0, 0, 0, 86, 21, 16, 0, 2, 0, 0, 0, 70, 142, 32, 0, 0, 0, 0, 0, 1, 0, 0, 0, 50, 0, 0, 10, 242, 0, 16, 0,
		//0, 0, 0, 0, 6, 16, 16, 0, 2, 0, 0, 0, 70, 142, 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 70, 14, 16, 0, 0, 0, 0, 0, 50, 0, 0, 10, 242, 0, 16, 0, 0, 0, 0, 0, 166, 26, 16, 0, 2, 0, 0, 0, 70, 142, 32, 0, 0, 0, 0, 0, 2, 0, 0, 0,
		//70, 14, 16, 0, 0, 0, 0, 0, 50, 0, 0, 10, 242, 32, 16, 0, 2, 0, 0, 0, 246, 31, 16, 0, 2, 0, 0, 0, 70, 142, 32, 0, 0, 0, 0, 0, 3, 0, 0, 0, 70, 14, 16, 0, 0, 0, 0, 0, 62, 0, 0, 1, 73, 83, 71, 78, 108, 0, 0, 0, 3, 0, 0, 0,
		//8, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 15, 15, 0, 0, 86, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 1, 0, 0, 0, 3, 3, 0, 0, 95, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		//3, 0, 0, 0, 2, 0, 0, 0, 15, 15, 0, 0, 67, 79, 76, 79, 82, 0, 84, 69, 88, 67, 79, 79, 82, 68, 0, 83, 86, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 171, 79, 83, 71, 78, 108, 0, 0, 0, 3, 0, 0, 0, 8, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 0,
		//0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 15, 0, 0, 0, 86, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 1, 0, 0, 0, 3, 12, 0, 0, 95, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 2, 0, 0, 0, 15, 0, 0, 0,
		//67, 79, 76, 79, 82, 0, 84, 69, 88, 67, 79, 79, 82, 68, 0, 83, 86, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 171, 185, 36, 58, 62, 3, 5, 67, 79, 76, 79, 82, 0, 0, 0, 3, 15, 0, 0, 8, 84, 69, 88, 67, 79, 79, 82, 68, 0, 1, 0, 3, 3, 0, 0, 11, 83,
		//86, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 2, 0, 3, 15, 0, 0, 1, 152, 1, 68, 88, 66, 67, 21, 219, 222, 169, 247, 114, 122, 118, 211, 95, 148, 219, 130, 126, 181, 238, 1, 0, 0, 0, 152, 0, 0, 0, 1, 0, 0, 0, 36, 0, 0, 0, 73, 83, 71, 78, 108, 0, 0, 0,
		//3, 0, 0, 0, 8, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 15, 15, 0, 0, 86, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 1, 0, 0, 0, 3, 3, 0, 0, 95, 0, 0, 0, 0, 0, 0, 0,
		//0, 0, 0, 0, 3, 0, 0, 0, 2, 0, 0, 0, 15, 15, 0, 0, 67, 79, 76, 79, 82, 0, 84, 69, 88, 67, 79, 79, 82, 68, 0, 83, 86, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 171, 200, 35, 59, 96, 3, 5, 67, 79, 76, 79, 82, 0, 0, 0, 3, 15, 0, 0, 8, 84,
		//69, 88, 67, 79, 79, 82, 68, 0, 1, 0, 3, 3, 0, 0, 11, 83, 86, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 2, 1, 3, 15, 0, 0, 0, 0, 0, 0, 0, 1, 16, 80, 114, 111, 106, 101, 99, 116, 105, 111, 110, 77, 97, 116, 114, 105, 120, 64, 1, 15, 77, 97, 116, 114, 105,
		//120, 84, 114, 97, 110, 115, 102, 111, 114, 109, 2, 3, 0, 0, 64, 4, 4, 0, 1, 16, 80, 114, 111, 106, 101, 99, 116, 105, 111, 110, 77, 97, 116, 114, 105, 120, 4, 26, 0, 1, 0, 4, 0, 0, 0, 0, 0, 145, 0, 0, 220, 4, 68, 88, 66, 67, 133, 3, 0, 192, 207, 222, 249, 179,
		//29, 143, 136, 244, 72, 165, 214, 125, 1, 0, 0, 0, 92, 2, 0, 0, 4, 0, 0, 0, 48, 0, 0, 0, 236, 0, 0, 0, 172, 1, 0, 0, 40, 2, 0, 0, 65, 111, 110, 57, 180, 0, 0, 0, 180, 0, 0, 0, 0, 2, 255, 255, 140, 0, 0, 0, 40, 0, 0, 0, 0, 0, 40, 0,
		//0, 0, 40, 0, 0, 0, 40, 0, 1, 0, 36, 0, 0, 0, 40, 0, 0, 0, 0, 0, 0, 2, 255, 255, 81, 0, 0, 5, 0, 0, 15, 160, 0, 0, 128, 191, 0, 0, 128, 191, 0, 0, 128, 191, 0, 0, 128, 63, 81, 0, 0, 5, 1, 0, 15, 160, 0, 0, 128, 63, 0, 0, 128, 63,
		//0, 0, 128, 63, 0, 0, 0, 0, 31, 0, 0, 2, 0, 0, 0, 128, 1, 0, 3, 176, 31, 0, 0, 2, 0, 0, 0, 144, 0, 8, 15, 160, 66, 0, 0, 3, 0, 0, 15, 128, 1, 0, 228, 176, 0, 8, 228, 160, 1, 0, 0, 2, 1, 0, 15, 128, 0, 0, 228, 160, 4, 0, 0, 4,
		//0, 0, 15, 128, 0, 0, 228, 128, 1, 0, 228, 128, 1, 0, 228, 160, 1, 0, 0, 2, 0, 8, 15, 128, 0, 0, 228, 128, 255, 255, 0, 0, 83, 72, 68, 82, 184, 0, 0, 0, 64, 0, 0, 0, 46, 0, 0, 0, 89, 0, 0, 4, 70, 142, 32, 0, 0, 0, 0, 0, 1, 0, 0, 0,
		//90, 0, 0, 3, 0, 96, 16, 0, 0, 0, 0, 0, 88, 24, 0, 4, 0, 112, 16, 0, 0, 0, 0, 0, 85, 85, 0, 0, 98, 16, 0, 3, 50, 16, 16, 0, 2, 0, 0, 0, 101, 0, 0, 3, 242, 32, 16, 0, 0, 0, 0, 0, 104, 0, 0, 2, 1, 0, 0, 0, 69, 0, 0, 9,
		//242, 0, 16, 0, 0, 0, 0, 0, 70, 16, 16, 0, 2, 0, 0, 0, 70, 126, 16, 0, 0, 0, 0, 0, 0, 96, 16, 0, 0, 0, 0, 0, 50, 0, 0, 15, 242, 32, 16, 0, 0, 0, 0, 0, 70, 14, 16, 0, 0, 0, 0, 0, 2, 64, 0, 0, 0, 0, 128, 191, 0, 0, 128, 191,
		//0, 0, 128, 191, 0, 0, 128, 63, 2, 64, 0, 0, 0, 0, 128, 63, 0, 0, 128, 63, 0, 0, 128, 63, 0, 0, 0, 0, 62, 0, 0, 1, 73, 83, 71, 78, 116, 0, 0, 0, 3, 0, 0, 0, 8, 0, 0, 0, 80, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0,
		//0, 0, 0, 0, 15, 0, 0, 0, 92, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 1, 0, 0, 0, 15, 0, 0, 0, 107, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 2, 0, 0, 0, 3, 3, 0, 0, 83, 86, 95, 80, 79, 83, 73, 84,
		//73, 79, 78, 0, 83, 67, 69, 78, 69, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 84, 69, 88, 67, 79, 79, 82, 68, 0, 79, 83, 71, 78, 44, 0, 0, 0, 1, 0, 0, 0, 8, 0, 0, 0, 32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0,
		//15, 0, 0, 0, 83, 86, 95, 84, 65, 82, 71, 69, 84, 0, 171, 171, 64, 166, 240, 66, 3, 11, 83, 86, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 0, 1, 3, 15, 0, 0, 14, 83, 67, 69, 78, 69, 95, 80, 79, 83, 73, 84, 73, 79, 78, 0, 1, 0, 3, 15, 0, 0, 8, 84,
		//69, 88, 67, 79, 79, 82, 68, 0, 2, 0, 3, 3, 0, 0, 0, 0, 0, 0, 0, 1, 9, 83, 86, 95, 84, 65, 82, 71, 69, 84, 0, 0, 64, 3, 15, 0, 0, 0, 0, 0, 0, 0, 1, 16, 80, 114, 111, 106, 101, 99, 116, 105, 111, 110, 77, 97, 116, 114, 105, 120, 64, 1, 15, 77,
		//97, 116, 114, 105, 120, 84, 114, 97, 110, 115, 102, 111, 114, 109, 2, 3, 0, 0, 64, 4, 4, 0, 3, 14, 84, 101, 120, 116, 117, 114, 101, 83, 97, 109, 112, 108, 101, 114, 4, 10, 0, 1, 7, 84, 101, 120, 116, 117, 114, 101, 4, 7, 0, 1, 16, 80, 114, 111, 106, 101, 99, 116, 105, 111,
		//110, 77, 97, 116, 114, 105, 120, 4, 26, 0, 1, 69, 70, 70, 88, 47, 0, 0, 0, 6, 73, 110, 118, 101, 114, 116, 0, 1, 1, 6, 73, 110, 118, 101, 114, 116, 1, 0, 0, 0, 0, 0, 0, 1, 6, 1, 0, 0, 255, 255, 255, 255, 0, 0, 0, 0, 1, 1, 0, 255, 255, 255, 255, 0,
		//0, 0,
		//});


		const float uiDistanceStart = 1.5f;
		const float uiDistanceFade = 0.5f;
		const float uiDistanceDisappear = 0.25f;



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
				catch (Exception exc) {
					Console.WriteLine("[EXC] " + exc.Message);
				}
			});
		}

		public static void Pause() {
			EnqueueUIRedraw();
			pause = true;
		}
		public static void UnPause() { pause = false; }

		public static void UpdateTime(float time) {
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

		public static bool IsOculusPresent()
		{
			if (_playbackLock) return true;
			Wrap oculus = new Wrap();
			try {
				bool success = oculus.Initialize();
			
				if (!success)
				{
					oculus.Dispose();
					return false;
				} else
				{
					var result = oculus.Detect(1000);                
					oculus.Dispose();
					bool detected = result.IsOculusHMDConnected == 1 && result.IsOculusServiceRunning == 1;
					return detected;
				}
			}
			catch (Exception exc)
			{
				oculus.Dispose();
				return false;
			}
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

			Wrap oculus = new Wrap();
			Hmd hmd;

			// Initialize the Oculus runtime.
			bool success = oculus.Initialize();
			if (!success)
			{
				System.Windows.Forms.MessageBox.Show("Failed to initialize the Oculus runtime library.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

            OVRTypes.GraphicsLuid graphicsLuid;
            hmd = oculus.Hmd_Create(out graphicsLuid);

			if (hmd == null)
			{
				System.Windows.Forms.MessageBox.Show("Oculus Rift not detected.", "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (hmd.ProductName == string.Empty)
				System.Windows.Forms.MessageBox.Show("The HMD is not enabled.", "There's a tear in the Rift", MessageBoxButtons.OK, MessageBoxIcon.Error);

			// Create a set of layers to submit.
			EyeTexture[] eyeTextures = new EyeTexture[2];
            OVRTypes.Result result;

			// Create DirectX drawing device.
			SharpDX.Direct3D11.Device device = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, new SharpDX.Direct3D.FeatureLevel[] { SharpDX.Direct3D.FeatureLevel.Level_10_0 });

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
                OVRTypes.EyeType eye = (OVRTypes.EyeType)eyeIndex;
				EyeTexture eyeTexture = new EyeTexture();
				eyeTextures[eyeIndex] = eyeTexture;

				// Retrieve size and position of the texture for the current eye.
				eyeTexture.FieldOfView = hmd.DefaultEyeFov[eyeIndex];
				eyeTexture.TextureSize = hmd.GetFovTextureSize(eye, hmd.DefaultEyeFov[eyeIndex], 1.0f);
				eyeTexture.RenderDescription = hmd.GetRenderDesc(eye, hmd.DefaultEyeFov[eyeIndex]);
				eyeTexture.HmdToEyeViewOffset = eyeTexture.RenderDescription.HmdToEyeOffset; 
				eyeTexture.ViewportSize.Position = new OVRTypes.Vector2i(0, 0);
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
				OVRTypes.TextureSwapChainDesc textureSwapChainDesc = SharpDXHelpers.CreateTextureSwapChainDescription(eyeTexture.Texture2DDescription);

				result = hmd.CreateTextureSwapChainDX(device.NativePointer, textureSwapChainDesc, out eyeTexture.SwapTextureSet);
				WriteErrorDetails(oculus, result, "Failed to create swap chain.");

				// Retrieve the number of buffers of the created swap chain.
				int textureSwapChainBufferCount;
				result = eyeTexture.SwapTextureSet.GetLength(out textureSwapChainBufferCount);
				WriteErrorDetails(oculus, result, "Failed to retrieve the number of buffers of the created swap chain.");


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
					result = eyeTexture.SwapTextureSet.GetBufferDX(textureIndex, textureInterfaceId, out swapChainTextureComPtr);
					WriteErrorDetails(oculus, result, "Failed to retrieve a texture from the created swap chain.");


					// Create a managed Texture2D, based on the unmanaged texture pointer.
					eyeTexture.Textures[textureIndex] = new Texture2D(swapChainTextureComPtr);

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
				layerEyeFov.ColorTexture[eyeIndex] = eyeTexture.SwapTextureSet.TextureSwapChainPtr;
				layerEyeFov.Viewport[eyeIndex].Position = new OVRTypes.Vector2i(0, 0);
				layerEyeFov.Viewport[eyeIndex].Size = eyeTexture.TextureSize;
				layerEyeFov.Fov[eyeIndex] = eyeTexture.FieldOfView;
				layerEyeFov.Header.Flags = OVRTypes.LayerFlags.HighQuality;
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
			//basicEffectL.DiffuseColor = new Vector4(1f, 0f, 0f, 0f);
			

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
			var primitive = GraphicTools.CreateGeometry(_projection, gd, false);


			// UI Rendering
			InitUI(device, gd);
			DrawUI();
			#endregion



			#region Render loop
			DateTime startTime = DateTime.Now;
			DateTime lastTime = DateTime.Now;
			float deltaTime = 0;
			Plane uiPlane = new Plane(1);


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
					result = eyeTexture.SwapTextureSet.GetCurrentIndex(out textureIndex);
					WriteErrorDetails(oculus, result, "Failed to retrieve texture swap chain current index.");

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

					Matrix world = Matrix.Identity;
					Matrix viewMatrix = Matrix.LookAtLH(viewPosition, viewPosition + lookAt, lookUp);

					Matrix projectionMatrix = oculus.Matrix4f_Projection(eyeTexture.FieldOfView, 0.1f, 100.0f, OVRTypes.ProjectionModifier.LeftHanded).ToMatrix();
					projectionMatrix.Transpose();

					Matrix worldViewProjection = world * viewMatrix * projectionMatrix;
					worldViewProjection.Transpose();

					basicEffectL.World = Matrix.Translation(viewPosition); //Matrix.Identity;
					basicEffectL.View = viewMatrix;
					basicEffectL.Projection = projectionMatrix;

					//uiEffect.World = Matrix.Identity * Matrix.Scaling(1f) * Matrix.RotationAxis(Vector3.Up, (float)Math.PI) * Matrix.Translation(0, 0, -1.5f);

					// reset UI position every frame if it is not visible
					if (isUIHidden)
					{
						float yaw = (float)(Math.PI - Math.Atan2(viewMatrix.Forward.X, viewMatrix.Forward.Z));
						uiEffect.World = Matrix.Identity * Matrix.Scaling(1f) * Matrix.Translation(0, 0, uiDistanceStart) * Matrix.RotationAxis(Vector3.Up, yaw) * Matrix.Translation(viewPosition);
						uiPlane = new Plane(-uiEffect.World.TranslationVector, uiEffect.World.Forward);
					}
					uiEffect.View = viewMatrix;
					uiEffect.Projection = projectionMatrix;

					// distance ui plane - eye
					{
						// { 0    for d <= uiDistanceDisappear
						// { 0..1 for uiDistanceDisappear  d < uiDistanceFade
						// { 1    for d >= uiDistanceFade
						float dot;
						Vector3.Dot(ref uiPlane.Normal, ref viewPosition, out dot);
						float d = dot - uiPlane.D;
						overrideShowAlpha = (d - uiDistanceDisappear) / uiDistanceFade;
						if (overrideShowAlpha < 0) overrideShowAlpha = 0;
						else if (overrideShowAlpha > 1) overrideShowAlpha = 1;
					}


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
							if (eyeIndex == 0)
								primitive.Draw(basicEffectL);
							if (eyeIndex == 1)
								primitive.Draw(basicEffectR);
						}
						else
							primitive.Draw(basicEffectL);
					}


					DrawUI();
					RenderUI(deltaTime);
					
					// Commits any pending changes to the TextureSwapChain, and advances its current index
					result = eyeTexture.SwapTextureSet.Commit();
					WriteErrorDetails(oculus, result, "Failed to commit the swap chain texture.");

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

			// Release all 2D resources
			basicEffectL.Dispose();
			if (_stereoVideo)
				basicEffectR.Dispose();

			//target2d.Dispose();
			//uiSurface.Dispose();
			//uiTexture.Dispose();			
			//factory2d.Dispose();
			DisposeUI();

			// Disposing the device, before the hmd, will cause the hmd to fail when disposing.
			// Disposing the device, after the hmd, will cause the dispose of the device to fail.
			// It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
			// device.Dispose();

			hmd.Dispose();
			oculus.Dispose();

			_playbackLock = false;
		}


		public static void WriteErrorDetails(Wrap oculus, OVRTypes.Result result, string message)
		{
			if (result >= OVRTypes.Result.Success)
				return;

			// Retrieve the error message from the last occurring error.
			OVRTypes.ErrorInfo errorInformation = oculus.GetLastError();

			string formattedMessage = string.Format("{0}. Message: {1} (Error code={2})", message, errorInformation.ErrorString, errorInformation.Result);
			Trace.WriteLine(formattedMessage);

			throw new Exception(formattedMessage);
		}

	}
}
