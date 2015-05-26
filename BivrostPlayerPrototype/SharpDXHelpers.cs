using OculusWrap;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BivrostPlayerPrototype
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
		public static Vector3 ToVector3(this OVR.Vector3f ovrVector3f)
		{
			return new Vector3(ovrVector3f.X, ovrVector3f.Y, ovrVector3f.Z);
		}

		/// <summary>
		/// Convert an ovrMatrix4f to a SharpDX Matrix.
		/// </summary>
		/// <param name="ovrMatrix4f">ovrMatrix4f to convert to a SharpDX Matrix.</param>
		/// <returns>SharpDX Matrix, based on the ovrMatrix4f.</returns>
		public static Matrix ToMatrix(this OculusWrap.OVR.Matrix4f ovrMatrix4f)
		{
			return new Matrix(ovrMatrix4f.M11, ovrMatrix4f.M12, ovrMatrix4f.M13, ovrMatrix4f.M14, ovrMatrix4f.M21, ovrMatrix4f.M22, ovrMatrix4f.M23, ovrMatrix4f.M24, ovrMatrix4f.M31, ovrMatrix4f.M32, ovrMatrix4f.M33, ovrMatrix4f.M34, ovrMatrix4f.M41, ovrMatrix4f.M42, ovrMatrix4f.M43, ovrMatrix4f.M44);
		}

		/// <summary>
		/// Converts an ovrQuatf to a SharpDX Quaternion.
		/// </summary>
		public static Quaternion ToQuaternion(OVR.Quaternionf ovrQuatf)
		{
			return new Quaternion(ovrQuatf.X, ovrQuatf.Y, ovrQuatf.Z, ovrQuatf.W);
		}
	}	
}
