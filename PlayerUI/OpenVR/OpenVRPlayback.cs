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

namespace PlayerUI.OpenVR
{
	class OpenVRPlayback : Headset
	{
		public override bool IsPresent()
		{
			return true;	// TODO
		}


		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);


		protected override void Render()
		{
			// Preload native binaries
			var assembly = System.Uri.UnescapeDataString((new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath);
			var assemblyPath = Path.GetDirectoryName(assembly);

			IntPtr success;
			if (IntPtr.Size == 8)	// 64 bit
				success = LoadLibrary(assemblyPath + Path.DirectorySeparatorChar + "x86_64" + Path.DirectorySeparatorChar + "openvr_api.dll");
			else   // 32 bit
				success = LoadLibrary(assemblyPath + Path.DirectorySeparatorChar + "x86" + Path.DirectorySeparatorChar + "openvr_api.dll");
			if (success == IntPtr.Zero)
				throw new Exception("LoadLibrary error: " + Marshal.GetLastWin32Error());


			

			Device device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, new FeatureLevel[] { FeatureLevel.Level_10_0 });
			var context = device.ImmediateContext;

			// Compile Vertex and Pixel shaders
			VertexShader vertexShader;
			ShaderSignature signature;

			string shaderCode = @"struct VS_IN
{
    float4 pos : POSITION;
    float4 col : COLOR;
};

struct PS_IN
{
    float4 pos : SV_POSITION;
    float4 col : COLOR;
};

float4x4 worldViewProj;

PS_IN VS(VS_IN input)
{
    PS_IN output = (PS_IN) 0;
	
    output.pos = mul(input.pos, worldViewProj);
    output.col = input.col;
	
    return output;
}

float4 PS(PS_IN input) : SV_Target
{
    return input.col;
}";

			using (var vertexShaderByteCode = ShaderBytecode.Compile(shaderCode, "VS", "vs_4_0"))
			{
				vertexShader = new VertexShader(device, vertexShaderByteCode);
				signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
			}

			PixelShader pixelShader;
			using (var pixelShaderByteCode = ShaderBytecode.Compile(shaderCode, "PS", "ps_4_0"))
				pixelShader = new PixelShader(device, pixelShaderByteCode);

			// Layout from VertexShader input signature
			var layout = new InputLayout(device, signature, new[]
					{
						new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
						new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
					});

			// Create Constant Buffer
			var contantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);



			// Prepare All the stages
			context.InputAssembler.InputLayout = layout;
			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

			using (var vertices = Buffer.Create(device, BindFlags.VertexBuffer, CubeField()))
				context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector4>() * 2, 0));
			context.VertexShader.SetConstantBuffer(0, contantBuffer);
			context.VertexShader.Set(vertexShader);
			context.PixelShader.Set(pixelShader);

			//VIVE render target textures

			Texture_t leftEyeTex;
			Texture_t rightEyeTex;

			TrackedDevicePose_t[] deviceArray1 = new TrackedDevicePose_t[16];
			TrackedDevicePose_t[] deviceArray2 = new TrackedDevicePose_t[16];




			// HTC VIVE INIT
			EVRInitError initError = EVRInitError.None;
			Valve.VR.OpenVR.Init(ref initError);

			CVRSystem hmd = Valve.VR.OpenVR.System;
			CVRCompositor compositor = Valve.VR.OpenVR.Compositor;

			uint recommendedWidth = 0, recommendedHeight = 0;
			hmd.GetRecommendedRenderTargetSize(ref recommendedWidth, ref recommendedHeight);

			int targetWidth = (int)recommendedWidth;
			int targetHeight = (int)recommendedHeight;


			//Console.WriteLine("Connected to " + OpenVR.name + ":" + hmd_SerialNumber);

			compositor = Valve.VR.OpenVR.Compositor;
			//overlay = OpenVR.Overlay;

			// Setup render values
			//uint w = 0, h = 0;
			//hmd.GetRecommendedRenderTargetSize(ref w, ref h);
			//sceneWidth = (float)w;
			//sceneHeight = (float)h;

			float l_left = 0.0f, l_right = 0.0f, l_top = 0.0f, l_bottom = 0.0f;
			hmd.GetProjectionRaw(EVREye.Eye_Left, ref l_left, ref l_right, ref l_top, ref l_bottom);

			float r_left = 0.0f, r_right = 0.0f, r_top = 0.0f, r_bottom = 0.0f;
			hmd.GetProjectionRaw(EVREye.Eye_Right, ref r_left, ref r_right, ref r_top, ref r_bottom);

			Vector2 tanHalfFov = new Vector2(
				Math.Max(Math.Max(-l_left, l_right), Math.Max(-r_left, r_right)),
				Math.Max(Math.Max(-l_top, l_bottom), Math.Max(-r_top, r_bottom))
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

			Matrix m = Matrix.Identity;



			bool abort = false;

			var form = new RenderForm("SharpDX - MiniCube Direct3D11 Sample");
			form.FormClosing += (s, e) => abort = true;


			Texture2DDescription eyeTextureDescription = new Texture2DDescription()
			{
				Format = Format.R8G8B8A8_UNorm,
				ArraySize = 1,
				MipLevels = 1,
				Width = targetWidth,
				Height = targetHeight,
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
				Width = targetWidth,
				Height = targetHeight,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None
			};


			// Main loop
			using (Texture2D leftEye = new Texture2D(device, eyeTextureDescription))
			using (RenderTargetView leftEyeView = new RenderTargetView(device, leftEye))
			using (Texture2D leftEyeDepth = new Texture2D(device, eyeDepthTextureDescription))
			using (DepthStencilView leftEyeDepthView = new DepthStencilView(device, leftEyeDepth))
			using (Texture2D rightEye = new Texture2D(device, eyeTextureDescription))
			using (RenderTargetView rightEyeView = new RenderTargetView(device, rightEye))
			using (Texture2D rightEyeDepth = new Texture2D(device, eyeDepthTextureDescription))
			using (DepthStencilView rightEyeDepthView = new DepthStencilView(device, rightEyeDepth))
				while (!abort)
				{

					compositor.WaitGetPoses(deviceArray1, deviceArray2);

					TrackedDevicePose_t pose = new TrackedDevicePose_t();
					TrackedDevicePose_t gamePose = new TrackedDevicePose_t();
					compositor.GetLastPoseForTrackedDeviceIndex(Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd, ref pose, ref gamePose);



					if (deviceArray1[Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd].bPoseIsValid)
					{
						pose = deviceArray1[Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd];

					}




					const float halfIPD = 0.65f / 2;        // TODO: config

					// LEFT EYE
					{
						// Setup targets and viewport for rendering
						context.Rasterizer.SetViewport(new Viewport(0, 0, targetWidth, targetHeight, 0.0f, 1.0f));
						context.OutputMerger.SetTargets(leftEyeDepthView, leftEyeView);

						// Setup new projection matrix with correct aspect ratio
						Matrix worldMatrix = Matrix.Identity * Matrix.Scaling(1f) * Matrix.Translation(0, 0, -1.5f);

						Quaternion rotationQuaternion = pose.mDeviceToAbsoluteTracking.ToMatrix().QuaternionFromMatrix();

						Matrix viewMatrix = Matrix.RotationQuaternion(rotationQuaternion);
						//viewMatrix = Matrix.Translation(-IPD, 0, 0) * viewMatrix;
						viewMatrix.Transpose();

						float fov2 = (float)(110f * Math.PI / 180f);
						Matrix projectionMatrix = Matrix.PerspectiveFovLH(fov2, 1, 0.001f, 100.0f);

						Matrix worldViewProj = worldMatrix * viewMatrix * Matrix.Translation(halfIPD, 0, 0) * projectionMatrix;
						worldViewProj.Transpose();
						context.UpdateSubresource(ref worldViewProj, contantBuffer);

						context.ClearDepthStencilView(leftEyeDepthView, DepthStencilClearFlags.Depth, 1.0f, 0);
						context.ClearRenderTargetView(leftEyeView, Color.CornflowerBlue);

						context.Draw(36 * 1000, 0);

						//context.CopySubresourceRegion(leftEye, 0, new ResourceRegion(0, 0, 0, targetWidth, targetHeight, 100), backBuffer, 0, 0, 0, 0);
					}


					// RIGHT EYE
					{
						// Setup targets and viewport for rendering
						context.Rasterizer.SetViewport(new Viewport(0, 0, targetWidth, targetHeight, 0.0f, 1.0f));
						context.OutputMerger.SetTargets(rightEyeDepthView, rightEyeView);

						// Setup new projection matrix with correct aspect ratio
						//proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, targetWidth / (float)targetHeight, 0.1f, 100.0f);


						Matrix worldMatrix = Matrix.Identity * Matrix.Scaling(1f) * Matrix.Translation(0, 0, -1.5f);

						Quaternion rotationQuaternion = pose.mDeviceToAbsoluteTracking.ToMatrix().QuaternionFromMatrix();

						Matrix viewMatrix = Matrix.RotationQuaternion(rotationQuaternion);
						//viewMatrix = Matrix.Translation(IPD, 0, 0) * viewMatrix;
						viewMatrix.Transpose();

						float fov2 = (float)(110f * Math.PI / 180f);
						Matrix projectionMatrix = Matrix.PerspectiveFovLH(fov2, (float)targetWidth / (float)targetHeight, 0.001f, 100.0f);

						Matrix worldViewProj = worldMatrix * viewMatrix * Matrix.Translation(-halfIPD, 0, 0) * projectionMatrix;
						worldViewProj.Transpose();
						context.UpdateSubresource(ref worldViewProj, contantBuffer);

						context.ClearDepthStencilView(rightEyeDepthView, DepthStencilClearFlags.Depth, 1.0f, 0);
						context.ClearRenderTargetView(rightEyeView, Color.CornflowerBlue);

						context.Draw(36 * 1000, 0);

						//context.CopySubresourceRegion(rightEye, 0, new ResourceRegion(0, 0, 0, targetWidth, targetHeight, 100), backBuffer, 0, 0, 0, 0);
					}



					// RENDER TO HMD

					leftEyeTex = new Texture_t() { eColorSpace = EColorSpace.Gamma, eType = EGraphicsAPIConvention.API_DirectX, handle = leftEye.NativePointer };
					rightEyeTex = new Texture_t() { eColorSpace = EColorSpace.Gamma, eType = EGraphicsAPIConvention.API_DirectX, handle = rightEye.NativePointer };



					//compositor.WaitGetPoses(deviceArray1, deviceArray2);

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
						;
					//if (errorRight != EVRCompositorError.None)
					//	;
				};

			Valve.VR.OpenVR.Shutdown();


			// Release all resources
			signature.Dispose();
			vertexShader.Dispose();
			pixelShader.Dispose();
			layout.Dispose();
			contantBuffer.Dispose();
			context.ClearState();
			context.Flush();
			device.Dispose();
			context.Dispose();
		}




		public static Vector4[] CreateCube(Vector3 offset, Vector3 scale)
		{
			Vector4[] cube = new[]
			{
				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Front
                new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),

				new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // BACK
                new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),

				new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Top
                new Vector4(-1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),

				new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom
                new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),

				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Left
                new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),

				new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), // Right
                new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
	};

			for (int it = 0; it < cube.Length; it += 2)
			{
				cube[it].X = cube[it].X * scale.X + offset.X;
				cube[it].Y = cube[it].Y * scale.Y + offset.Y;
				cube[it].Z = cube[it].Z * scale.Z + offset.Z;
			}

			return cube;
		}

		public static Vector4[] CubeField()
		{
			List<Vector4> vertexBuffer = new List<Vector4>();

			float distance = 3f;
			float scale = 0.2f;

			int c = 5;

			for (int x = -c; x < c; x++)
				for (int y = -c; y < c; y++)
					for (int z = -c; z < c; z++)
					{
						vertexBuffer.AddRange(CreateCube(new Vector3(x * distance, y * distance, z * distance), Vector3.One * scale));
					}

			return vertexBuffer.ToArray();
		}
	}
}
