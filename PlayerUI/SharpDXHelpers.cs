using OculusWrap;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player
{
	public static class SharpDXHelpers
	{
		/// <summary>
		/// Convert a Vector4 to a Vector3
		/// </summary>
		/// <param name="vector4">Vector4 to convert to a Vector3.</param>
		/// <returns>Vector3 based on the X, Y and Z coordinates of the Vector4.</returns>
		public static Vector3 ToVector3(this Vector4 vector4)
		{
			return new Vector3(vector4.X, vector4.Y, vector4.Z);
		}

		/// <summary>
		/// Convert an ovrVector3f to SharpDX Vector3.
		/// </summary>
		/// <param name="ovrVector3f">ovrVector3f to convert to a SharpDX Vector3.</param>
		/// <returns>SharpDX Vector3, based on the ovrVector3f.</returns>
		public static Vector3 ToVector3(this OVRTypes.Vector3f ovrVector3f)
		{
			return new Vector3(ovrVector3f.X, ovrVector3f.Y, ovrVector3f.Z);
		}

		/// <summary>
		/// Convert an Vec3 to SharpDX Vector3.
		/// </summary>
		/// <param name="ovrVector3f">ovrVector3f to convert to a SharpDX Vector3.</param>
		/// <returns>SharpDX Vector3, based on the ovrVector3f.</returns>
		public static Vector3 ToVector3(this OSVR.ClientKit.Vec3 ovrVector3f)
		{
			return new Vector3((float)ovrVector3f.x, (float)ovrVector3f.y, (float)ovrVector3f.z);
		}

		/// <summary>
		/// Convert an ovrMatrix4f to a SharpDX Matrix.
		/// </summary>
		/// <param name="ovrMatrix4f">ovrMatrix4f to convert to a SharpDX Matrix.</param>
		/// <returns>SharpDX Matrix, based on the ovrMatrix4f.</returns>
		public static Matrix ToMatrix(this OVRTypes.Matrix4f ovrMatrix4f)
		{
			return new Matrix(ovrMatrix4f.M11, ovrMatrix4f.M12, ovrMatrix4f.M13, ovrMatrix4f.M14, ovrMatrix4f.M21, ovrMatrix4f.M22, ovrMatrix4f.M23, ovrMatrix4f.M24, ovrMatrix4f.M31, ovrMatrix4f.M32, ovrMatrix4f.M33, ovrMatrix4f.M34, ovrMatrix4f.M41, ovrMatrix4f.M42, ovrMatrix4f.M43, ovrMatrix4f.M44);
		}


		/// <summary>
		/// Convert an OSVR Matrix44f to a SharpDX Matrix.
		/// </summary>
		/// <param name="ovrMatrix4f">ovrMatrix4f to convert to a SharpDX Matrix.</param>
		/// <returns>SharpDX Matrix, based on the ovrMatrix4f.</returns>
		public static Matrix ToMatrix(this OSVR.ClientKit.Matrix44f projectionf)
		{
			return new Matrix()
			{
				M11 = projectionf.M0,
				M12 = projectionf.M1,
				M13 = projectionf.M2,
				M14 = projectionf.M3,
				M21 = projectionf.M4,
				M22 = projectionf.M5,
				M23 = projectionf.M6,
				M24 = projectionf.M7,
				M31 = projectionf.M8,
				M32 = projectionf.M9,
				M33 = projectionf.M10,
				M34 = projectionf.M11,
				M41 = projectionf.M12,
				M42 = projectionf.M13,
				M43 = projectionf.M14,
				M44 = projectionf.M15
			};
		}





		public static Quaternion QuaternionFromMatrix(this Matrix m)
		{
			// Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
			Quaternion q = new Quaternion();
			q.W = (float)Math.Sqrt(Math.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
			q.X = (float)Math.Sqrt(Math.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
			q.Y = (float)Math.Sqrt(Math.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
			q.Z = (float)Math.Sqrt(Math.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
			q.X *= Math.Sign(q.X * (m[2, 1] - m[1, 2]));
			q.Y *= Math.Sign(q.Y * (m[0, 2] - m[2, 0]));
			q.Z *= Math.Sign(q.Z * (m[1, 0] - m[0, 1]));
			return q;
		}


		/// <summary>
		/// TODO: untested
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public static Matrix LeftToRightHanded(this Matrix m)
		{
			//			{ rx, ry, rz, 0 }
			//			{ ux, uy, uz, 0 }
			//			{ lx, ly, lz, 0 }
			//			{ px, py, pz, 1 }
			//			To change it from left to right or right to left, flip it like this:
			//			{ rx, rz, ry, 0 }
			//			{ lx, lz, ly, 0 }
			//			{ ux, uz, uy, 0 }
			//			{ px, pz, py, 1 }

			return new Matrix(
				m.M11, m.M13, m.M12, m.M14,
				m.M32, m.M33, m.M32, m.M34,
				m.M21, m.M23, m.M22, m.M24,
				m.M41, m.M43, m.M42, m.M44
			);
		}

		/// <summary>
		/// Converts an ovrQuatf to a SharpDX Quaternion.
		/// </summary>
		public static Quaternion ToQuaternion(this OVRTypes.Quaternionf ovrQuatf)
		{
			return new Quaternion(ovrQuatf.X, ovrQuatf.Y, ovrQuatf.Z, ovrQuatf.W);
		}


		public static SharpDX.Quaternion ToQuaternion(this OSVR.ClientKit.Quaternion osvrQuatf)
		{
			return new SharpDX.Quaternion((float)osvrQuatf.x, (float)osvrQuatf.y, (float)osvrQuatf.z, (float)osvrQuatf.w);
		}


		/// <summary>
		/// Creates a TextureSwapChainDesc, based on a specified SharpDX texture description.
		/// </summary>
		/// <param name="texture2DDescription">SharpDX texture description.</param>
		/// <returns>TextureSwapChainDesc, based on the SharpDX texture description.</returns>
		public static OVRTypes.TextureSwapChainDesc CreateTextureSwapChainDescription(Texture2DDescription texture2DDescription)
		{
			OVRTypes.TextureSwapChainDesc textureSwapChainDescription = new OVRTypes.TextureSwapChainDesc();
			textureSwapChainDescription.Type = OVRTypes.TextureType.Texture2D;
			textureSwapChainDescription.Format = GetTextureFormat(texture2DDescription.Format);
			textureSwapChainDescription.ArraySize = (int)texture2DDescription.ArraySize;
			textureSwapChainDescription.Width = (int)texture2DDescription.Width;
			textureSwapChainDescription.Height = (int)texture2DDescription.Height;
			textureSwapChainDescription.MipLevels = (int)texture2DDescription.MipLevels;
			textureSwapChainDescription.SampleCount = (int)texture2DDescription.SampleDescription.Count;
			textureSwapChainDescription.StaticImage = 0;
			textureSwapChainDescription.MiscFlags = GetTextureMiscFlags(texture2DDescription, false);
			textureSwapChainDescription.BindFlags = GetTextureBindFlags(texture2DDescription.BindFlags);

			return textureSwapChainDescription;
		}

		/// <summary>
		/// Translates a DirectX texture format into an Oculus SDK texture format.
		/// </summary>
		/// <param name="textureFormat">DirectX texture format to translate into an Oculus SDK texture format.</param>
		/// <returns>
		/// Oculus SDK texture format matching the specified textureFormat or OVRTypes.TextureFormat.Unknown if a match count not be found.
		/// </returns>
		public static OVRTypes.TextureFormat GetTextureFormat(SharpDX.DXGI.Format textureFormat)
		{
			switch (textureFormat)
			{
				case SharpDX.DXGI.Format.B5G6R5_UNorm: return OVRTypes.TextureFormat.B5G6R5_UNORM;
				case SharpDX.DXGI.Format.B5G5R5A1_UNorm: return OVRTypes.TextureFormat.B5G5R5A1_UNORM;
				case SharpDX.DXGI.Format.R8G8B8A8_UNorm: return OVRTypes.TextureFormat.R8G8B8A8_UNORM;
				case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb: return OVRTypes.TextureFormat.R8G8B8A8_UNORM_SRGB;
				case SharpDX.DXGI.Format.B8G8R8A8_UNorm: return OVRTypes.TextureFormat.B8G8R8A8_UNORM;
				case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb: return OVRTypes.TextureFormat.B8G8R8A8_UNORM_SRGB;
				case SharpDX.DXGI.Format.B8G8R8X8_UNorm: return OVRTypes.TextureFormat.B8G8R8X8_UNORM;
				case SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb: return OVRTypes.TextureFormat.B8G8R8X8_UNORM_SRGB;
				case SharpDX.DXGI.Format.R16G16B16A16_Float: return OVRTypes.TextureFormat.R16G16B16A16_FLOAT;
				case SharpDX.DXGI.Format.D16_UNorm: return OVRTypes.TextureFormat.D16_UNORM;
				case SharpDX.DXGI.Format.D24_UNorm_S8_UInt: return OVRTypes.TextureFormat.D24_UNORM_S8_UINT;
				case SharpDX.DXGI.Format.D32_Float: return OVRTypes.TextureFormat.D32_FLOAT;
				case SharpDX.DXGI.Format.D32_Float_S8X24_UInt: return OVRTypes.TextureFormat.D32_FLOAT_S8X24_UINT;

				case SharpDX.DXGI.Format.R8G8B8A8_Typeless: return OVRTypes.TextureFormat.R8G8B8A8_UNORM;
				case SharpDX.DXGI.Format.R16G16B16A16_Typeless: return OVRTypes.TextureFormat.R16G16B16A16_FLOAT;

				default: return OVRTypes.TextureFormat.UNKNOWN;
			}
		}

		/// <summary>
		/// Creates a set of TextureMiscFlags, based on a specified SharpDX texture description and a mip map generation flag.
		/// </summary>
		/// <param name="texture2DDescription">SharpDX texture description.</param>
		/// <param name="allowGenerateMips">
		/// When set, allows generation of the mip chain on the GPU via the GenerateMips
		/// call. This flag requires that RenderTarget binding also be specified.
		/// </param>
		/// <returns>Created TextureMiscFlags, based on the specified SharpDX texture description and mip map generation flag.</returns>
		public static OVRTypes.TextureMiscFlags GetTextureMiscFlags(Texture2DDescription texture2DDescription, bool allowGenerateMips)
		{
			OVRTypes.TextureMiscFlags results = OVRTypes.TextureMiscFlags.None;

			if (texture2DDescription.Format == SharpDX.DXGI.Format.R8G8B8A8_Typeless || texture2DDescription.Format == SharpDX.DXGI.Format.R16G16B16A16_Typeless)
				results |= OVRTypes.TextureMiscFlags.DX_Typeless;

			if (texture2DDescription.BindFlags.HasFlag(BindFlags.RenderTarget) && allowGenerateMips)
				results |= OVRTypes.TextureMiscFlags.AllowGenerateMips;

			return results;
		}

		/// <summary>
		/// Retrieves a list of flags matching the specified DirectX texture binding flags.
		/// </summary>
		/// <param name="bindFlags">DirectX texture binding flags to translate into Oculus SDK texture binding flags.</param>
		/// <returns>Oculus SDK texture binding flags matching the specified bindFlags.</returns>
		public static OVRTypes.TextureBindFlags GetTextureBindFlags(SharpDX.Direct3D11.BindFlags bindFlags)
		{
			OVRTypes.TextureBindFlags result = OVRTypes.TextureBindFlags.None;

			if (bindFlags.HasFlag(SharpDX.Direct3D11.BindFlags.DepthStencil))
				result |= OVRTypes.TextureBindFlags.DX_DepthStencil;

			if (bindFlags.HasFlag(SharpDX.Direct3D11.BindFlags.RenderTarget))
				result |= OVRTypes.TextureBindFlags.DX_RenderTarget;

			if (bindFlags.HasFlag(SharpDX.Direct3D11.BindFlags.UnorderedAccess))
				result |= OVRTypes.TextureBindFlags.DX_DepthStencil;

			return result;
		}
	}
}
