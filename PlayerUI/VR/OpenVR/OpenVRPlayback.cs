using System;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Valve.VR;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using PlayerUI.Tools;
using System.Diagnostics;
using System.ComponentModel;
using Bivrost.Log;

namespace PlayerUI.OpenVR
{
	class OpenVRPlayback : Headset
	{
		private static Logger log = new Logger("OpenVR");



		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);


		bool dllsLoaded = false;
		SharpDX.Toolkit.Graphics.GeometricPrimitive primitive;

		protected void EnsureDllsLoaded()
		{
			if (dllsLoaded)
				return;

			// Preload native binaries
			var assembly = System.Uri.UnescapeDataString((new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath);
			var assemblyPath = Path.GetDirectoryName(assembly);
			string libPath = assemblyPath + Path.DirectorySeparatorChar + (Environment.Is64BitProcess ? "x64" : "x86") + Path.DirectorySeparatorChar + "openvr_api.dll";

			IntPtr success;
			success = LoadLibrary(libPath);
			if (success == IntPtr.Zero)
			{
				var lasterror = Marshal.GetLastWin32Error();
				var innerEx = new Win32Exception(lasterror);
				innerEx.Data.Add("LastWin32Error", lasterror);

				throw new Exception("OpenVR LoadLibrary error: " + libPath, innerEx);
			}
			dllsLoaded = true;
		}


		public override bool IsPresent()
		{
			//return true;
			EnsureDllsLoaded();
			return Valve.VR.OpenVR.IsHmdPresent();
		}


		protected override float Gamma { get { return 2.2f; } }


		#region ILookProvider properties
		public override string DescribeType { get { return "OpenVR"; } }
		public override event Action<Vector3, Quaternion, float> ProvideLook;

		#endregion


		protected override void Render()
		{
			EnsureDllsLoaded();

			Lock = true;

			EVRInitError initError = EVRInitError.None;
			Valve.VR.OpenVR.Init(ref initError);
			if (initError != EVRInitError.None)
				throw new Exception("OpenVR init error " + initError + ": " + Valve.VR.OpenVR.GetStringForHmdError(initError));
			CVRSystem hmd = Valve.VR.OpenVR.System;
			CVRCompositor compositor = Valve.VR.OpenVR.Compositor;
	

			uint targetWidth = 0, targetHeight = 0;
			hmd.GetRecommendedRenderTargetSize(ref targetWidth, ref targetHeight);
			float sceneWidth = (float)targetWidth;
			float sceneHeight = (float)targetHeight;

			float l_left = 0.0f, l_right = 0.0f, l_top = 0.0f, l_bottom = 0.0f;
			hmd.GetProjectionRaw(EVREye.Eye_Left, ref l_left, ref l_right, ref l_top, ref l_bottom);

			float r_left = 0.0f, r_right = 0.0f, r_top = 0.0f, r_bottom = 0.0f;
			hmd.GetProjectionRaw(EVREye.Eye_Right, ref r_left, ref r_right, ref r_top, ref r_bottom);

			Vector2 tanHalfFov = new Vector2(
				Math.Max(Math.Max (- l_left, l_right), Math.Max (- r_left, r_right)),
				Math.Max(Math.Max(- l_top, l_bottom), Math.Max (- r_top, r_bottom))
			);

			VRTextureBounds_t[] textureBounds = new VRTextureBounds_t[2];

			textureBounds[0].uMin = 0.5f + 0.5f * l_left / tanHalfFov.X;
			textureBounds[0].uMax = 0.5f + 0.5f * l_right / tanHalfFov.X;
			textureBounds[0].vMin = 0.5f - 0.5f * l_bottom / tanHalfFov.Y;
			textureBounds[0].vMax = 0.5f - 0.5f * l_top / tanHalfFov.Y;

			textureBounds[1].uMin = 0.5f + 0.5f * r_left / tanHalfFov.X;
			textureBounds[1].uMax = 0.5f + 0.5f * r_right / tanHalfFov.X;
			textureBounds[1].vMin = 0.5f - 0.5f * r_bottom / tanHalfFov.Y;
			textureBounds[1].vMax = 0.5f - 0.5f * r_top / tanHalfFov.Y;

			float aspect = tanHalfFov.X / tanHalfFov.Y;
			float lookFov = (float)(2.0f * Math.Atan(tanHalfFov.Y) * 180 / Math.PI);


			Texture2DDescription eyeTextureDescription = new Texture2DDescription()
			{
				Format = Format.R8G8B8A8_UNorm_SRgb,
				ArraySize = 1,
				MipLevels = 1,
				Width = (int)targetWidth,
				Height = (int)targetHeight,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.RenderTarget,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None
			};


			Texture2DDescription eyeDepthTextureDescription = new Texture2DDescription()
			{
				Format = Format.D32_Float_S8X24_UInt,
				ArraySize = 1,
				MipLevels = 1,
				Width = (int)targetWidth,
				Height = (int)targetHeight,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None
			};



			///// CUBE
			//var cubeEffect = new SharpDX.Toolkit.Graphics.BasicEffect(_gd)
			//{
			//	//PreferPerPixelLighting = false,
			//	//TextureEnabled = false,
			//	LightingEnabled = true,
			//	//DiffuseColor = new Vector4(0.5f,0.5f,0.5f,1f),
			//	Sampler = _gd.SamplerStates.AnisotropicClamp
			//};
			//cubeEffect.EnableDefaultLighting();
			//var cube = SharpDX.Toolkit.Graphics.GeometricPrimitive.Teapot.New(_gd, 1, 8, false);


			///// END


			using (_device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, new FeatureLevel[] { FeatureLevel.Level_10_0 }))
			using (DeviceContext context = _device.ImmediateContext)
			using (_gd = SharpDX.Toolkit.Graphics.GraphicsDevice.New(_device))
			//using (SharpDX.Toolkit.Graphics.GeometricPrimitive primitive = GraphicTools.CreateGeometry(_projection, _gd, false))
			using (customEffectL = GetCustomEffect(_gd))
			using (customEffectR = GetCustomEffect(_gd))
			using (Texture2D leftEye = new Texture2D(_device, eyeTextureDescription))
			using (RenderTargetView leftEyeView = new RenderTargetView(_device, leftEye))
			using (Texture2D leftEyeDepth = new Texture2D(_device, eyeDepthTextureDescription))
			using (DepthStencilView leftEyeDepthView = new DepthStencilView(_device, leftEyeDepth))
			using (Texture2D rightEye = new Texture2D(_device, eyeTextureDescription))
			using (RenderTargetView rightEyeView = new RenderTargetView(_device, rightEye))
			using (Texture2D rightEyeDepth = new Texture2D(_device, eyeDepthTextureDescription))
			using (DepthStencilView rightEyeDepthView = new DepthStencilView(_device, rightEyeDepth))
			using (vrui = new VRUI(_device, _gd))
			{
				MediaDecoder.Instance.OnFormatChanged += ResizeTexture;

				ResizeTexture(MediaDecoder.Instance.TextureL, _stereoVideo ? MediaDecoder.Instance.TextureR : MediaDecoder.Instance.TextureL);

				primitive = GraphicTools.CreateGeometry(_projection, _gd, false);

				Stopwatch stopwatch = new Stopwatch();
				Texture_t leftEyeTex = new Texture_t() { eColorSpace = EColorSpace.Gamma, eType = EGraphicsAPIConvention.API_DirectX, handle = leftEye.NativePointer };
				Texture_t rightEyeTex = new Texture_t() { eColorSpace = EColorSpace.Gamma, eType = EGraphicsAPIConvention.API_DirectX, handle = rightEye.NativePointer };

				TrackedDevicePose_t[] renderPoseArray = new TrackedDevicePose_t[16];
				TrackedDevicePose_t[] gamePoseArray = new TrackedDevicePose_t[16];
				TrackedDevicePose_t pose = new TrackedDevicePose_t();

				while (!abort)
				{
					updateSettingsActionQueue.RunAllActions();

					float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
					stopwatch.Restart();

					compositor.WaitGetPoses(renderPoseArray, gamePoseArray);
					if (renderPoseArray[Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd].bPoseIsValid)
					{
						pose = gamePoseArray[Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd];
					}

					foreach (EVREye eye in new EVREye[] { EVREye.Eye_Left, EVREye.Eye_Right })
					{
						DepthStencilView currentEyeDepthView = (eye == EVREye.Eye_Left) ? leftEyeDepthView : rightEyeDepthView;
						RenderTargetView currentEyeView = (eye == EVREye.Eye_Left) ? leftEyeView : rightEyeView;

						// Setup targets and viewport for rendering
						context.OutputMerger.SetTargets(currentEyeDepthView, currentEyeView);
						context.ClearDepthStencilView(currentEyeDepthView, DepthStencilClearFlags.Depth, 1.0f, 0);
						context.ClearRenderTargetView(currentEyeView, Color.Black);
						context.Rasterizer.SetViewport(new Viewport(0, 0, (int)targetWidth, (int)targetHeight, 0.0f, 1.0f));

						Quaternion lookRotation; // = pose.mDeviceToAbsoluteTracking.GetRotation();
						Vector3 lookPosition; // = pose.mDeviceToAbsoluteTracking.GetPosition();
						Vector3 scale;
						Matrix eyePose = hmd.GetEyeToHeadTransform(eye).RebuildTRSMatrix() * pose.mDeviceToAbsoluteTracking.RebuildTRSMatrix();
						eyePose.Decompose(out scale, out lookRotation, out lookPosition);

						Matrix rotationMatrix = Matrix.RotationQuaternion(lookRotation);
						Vector3 lookUp = Vector3.Transform(Vector3.Up, rotationMatrix).ToVector3();
						Vector3 lookAt = Vector3.Transform(Vector3.ForwardRH, rotationMatrix).ToVector3();

						//Console.WriteLine($"OpenVR {eye} up: {lookUp:00.00} at: {lookAt:00.00} position:{lookPosition:00.00}");

						Matrix viewMatrix = Matrix.LookAtRH(lookPosition, lookPosition + lookAt, lookUp);

						Matrix pm1 = Matrix.PerspectiveFovLH(lookFov * ((float)Math.PI / 180f), aspect, 0.001f, 100.0f);
						//Matrix pm2 = hmd.GetProjectionMatrix(eye, 0.001f, 100f, EGraphicsAPIConvention.API_DirectX).ToProjMatrix();

						Matrix projectionMatrix = pm1;


						Matrix worldMatrix = Matrix.Translation(lookPosition);
						if (Logic.Instance.settings.OpenVRReverse)
							worldMatrix = Matrix.RotationY(180) * worldMatrix;

						Matrix MVP = worldMatrix * viewMatrix * projectionMatrix;
						customEffectL.Parameters["WorldViewProj"].SetValue(MVP);
						customEffectR.Parameters["WorldViewProj"].SetValue(MVP);

						lock (localCritical)
						{
							if (_stereoVideo)
							{
								if (eye == EVREye.Eye_Left)
									primitive.Draw(customEffectL);
								if (eye == EVREye.Eye_Right)
									primitive.Draw(customEffectR);
							}
							else
								primitive.Draw(customEffectL);
						}

						if (eye == EVREye.Eye_Left || true)
						{
                            Vector3 fixedLookPosition = Vector3.Transform(lookPosition, MVP).ToVector3();
                            Vector3 fixedLookAt = Vector3.Transform(lookPosition + Vector3.ForwardLH, MVP).ToVector3() - fixedLookPosition;
                            Vector3 fixedLookUp = Vector3.Transform(lookPosition + Vector3.Up, MVP).ToVector3() - fixedLookPosition;
                            fixedLookAt.Normalize();
                            fixedLookUp.Normalize();
                            Quaternion fixedLookRotation = Quaternion.LookAtRH(Vector3.Zero, fixedLookAt, fixedLookUp);

                            //fixedLookRotation.X *= -1;
                            //fixedLookRotation.Y *= -1;
                            fixedLookRotation.Z *= -1;


                            //if(eye == EVREye.Eye_Left)
                            // fixedLookRotation.Invert();

                            //Mirror effect of rotation along z axis by flipping x and y elements of the quaternion.
                            //http://stackoverflow.com/a/32482386/785171
                            //float t = fixedLookRotation.X;
                            //fixedLookRotation.X = fixedLookRotation.Y;
                            //fixedLookRotation.Y = t;

                            //fixedLookRotation = new Quaternion(1, 0, 0, 0) * fixedLookRotation;
                            //fixedLookRotation = new Quaternion(0, 0, 1, 0) * fixedLookRotation;

                            LoggerManager.Publish("openvr.forward", fixedLookAt);
                            LoggerManager.Publish("openvr.up", fixedLookUp);


                            // TODO: normalize
                            ProvideLook(fixedLookPosition, fixedLookRotation, lookFov);
						}

                        // reset UI position every frame if it is not visible
                        if (vrui.isUIHidden)
							vrui.SetWorldPosition(viewMatrix.Forward, lookPosition, false);

						vrui.Draw(movieTitle, currentTime, duration);
						vrui.Render(deltaTime, viewMatrix, projectionMatrix, lookPosition, pause);

                        //// controllers:
                        //cubeEffect.View = viewMatrix;
                        //cubeEffect.Projection = projectionMatrix;

                        //for (uint controller = 1 /*skip hmd*/; controller < Valve.VR.OpenVR.k_unMaxTrackedDeviceCount; controller++)
                        //{
                        //	VRControllerState_t controllerState = default(VRControllerState_t);
                        //	//var controllerPose = renderPoseArray[controller];
                        //	//if (hmd.GetControllerState(controller, ref controllerState)) {
                        //	Vector3 pos = renderPoseArray[controller].mDeviceToAbsoluteTracking.GetPosition();
                        //	Quaternion rot = renderPoseArray[controller].mDeviceToAbsoluteTracking.GetRotation();
                        //	rot = rot * new Quaternion(0, 1, 0, 0);
                        //	float s = controllerState.ulButtonPressed > 0 ? 0.5f : 0.1f;
                        //	cubeEffect.World = Matrix.Scaling(s) * Matrix.RotationQuaternion(rot) * Matrix.Translation(pos);
                        //	cube.Draw(cubeEffect);

                        //	//}
                        //}

                    }




                    // RENDER TO HMD

                    EVRCompositorError errorLeft = compositor.Submit(
						EVREye.Eye_Left,
						ref leftEyeTex,
						ref textureBounds[0],
						EVRSubmitFlags.Submit_Default
					);

					EVRCompositorError errorRight = compositor.Submit(
						EVREye.Eye_Right,
						ref rightEyeTex,
						ref textureBounds[1],
						EVRSubmitFlags.Submit_Default
					);

					if (errorLeft != EVRCompositorError.None)
						log.Error("VR Compositor failure (left): " + errorLeft);
					if (errorRight != EVRCompositorError.None)
                        log.Error("VR Compositor failure (right): " + errorRight);
				};

				Valve.VR.OpenVR.Shutdown();
				MediaDecoder.Instance.OnFormatChanged -= ResizeTexture;

				primitive?.Dispose();
				context.ClearState();
				context.Flush();
			}
		}

		public override void UpdateSceneSettings(MediaDecoder.ProjectionMode projectionMode, MediaDecoder.VideoMode stereoscopy)
		{
			updateSettingsActionQueue.Enqueue(() =>
			{
				primitive?.Dispose();
				primitive = GraphicTools.CreateGeometry(projectionMode, _gd, false);
			});
		}
	}
}
