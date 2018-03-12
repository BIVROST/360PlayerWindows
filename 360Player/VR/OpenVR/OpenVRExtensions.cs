using Valve.VR;
using SharpDX;
using System;



namespace Bivrost.Bivrost360Player.OpenVR
{
	/// <summary>
	/// Utility class derived from OpenVR's unity integration
	/// </summary>
	static class OpenVRExtensions
	{

		public static Matrix ToMatrix(this HmdMatrix34_t pose)
		{
			var m = Matrix.Identity;

			m[0, 0] = pose.m0;
			m[0, 1] = pose.m1;
			m[0, 2] = -pose.m2;
			m[0, 3] = pose.m3;

			m[1, 0] = pose.m4;
			m[1, 1] = pose.m5;
			m[1, 2] = -pose.m6;
			m[1, 3] = pose.m7;

			m[2, 0] = -pose.m8;
			m[2, 1] = -pose.m9;
			m[2, 2] = pose.m10;
			m[2, 3] = -pose.m11;

			m.Transpose();
			return m;
		}

		public static Matrix ToMatrix(this HmdMatrix44_t pose)
		{
			var m = Matrix.Identity;

			m[0, 0] = pose.m0;
			m[0, 1] = pose.m1;
			m[0, 2] = -pose.m2;
			m[0, 3] = pose.m3;

			m[1, 0] = pose.m4;
			m[1, 1] = pose.m5;
			m[1, 2] = -pose.m6;
			m[1, 3] = pose.m7;

			m[2, 0] = -pose.m8;
			m[2, 1] = -pose.m9;
			m[2, 2] = pose.m10;
			m[2, 3] = -pose.m11;

			m[3, 0] = pose.m12;
			m[3, 1] = pose.m13;
			m[3, 2] = -pose.m14;
			m[3, 3] = pose.m15;

			m.Transpose();
			return m;
		}


		public static Matrix ToProjMatrix(this HmdMatrix44_t pose)
		{
			var m = Matrix.Identity;

			m[0, 0] = pose.m0;
			m[0, 1] = pose.m1;
			m[0, 2] = -pose.m2;
			m[0, 3] = pose.m3;

			m[1, 0] = pose.m4;
			m[1, 1] = pose.m5;
			m[1, 2] = -pose.m6;
			m[1, 3] = pose.m7;

			m[2, 0] = pose.m8;
			m[2, 1] = pose.m9;
			m[2, 2] = -pose.m10;
			m[2, 3] = pose.m11;

			m[3, 0] = -pose.m12;
			m[3, 1] = -pose.m13;
			m[3, 2] = -pose.m14;
			m[3, 3] = -pose.m15;

			m.Transpose();

			return m;
		}
		

		public static Quaternion GetRotation(this HmdMatrix34_t pose)
		{
			float m00 = pose.m0;
			float m11 = pose.m5;
			float m22 = pose.m10;

			float m21 = -pose.m9;
			float m02 = -pose.m2;
			float m12 = -pose.m6;
			float m20 = -pose.m8;
			float m10 = pose.m4;
			float m01 = pose.m1;

			Quaternion q = new Quaternion();
			q.W = (float)Math.Sqrt(Math.Max(0, 1 + m00 + m11 + m22)) / 2;
			q.X = (float)Math.Sqrt(Math.Max(0, 1 + m00 - m11 - m22)) / 2;
			q.Y = (float)Math.Sqrt(Math.Max(0, 1 - m00 + m11 - m22)) / 2;
			q.Z = (float)Math.Sqrt(Math.Max(0, 1 - m00 - m11 + m22)) / 2;
			q.X = _copysign(q.X, m21 - m12);
			q.Y = _copysign(q.Y, m02 - m20);
			q.Z = _copysign(q.Z, m10 - m01);
			return q;
		}


		public static Matrix RebuildTRSMatrix(this HmdMatrix34_t pose)
		{
			return Matrix.Scaling(pose.GetScale()) * Matrix.RotationQuaternion(pose.GetRotation()) * Matrix.Translation(pose.GetPosition());;
		}



		private static float _copysign(float sizeval, float signval)
		{
			return Math.Sign(signval) == 1 ? Math.Abs(sizeval) : -Math.Abs(sizeval);
		}



		public static Vector3 GetPosition(this HmdMatrix34_t pose)
		{
			var x = pose.m3;
			var y = pose.m7;
			var z = -pose.m11;

			return new Vector3(x, y, z);
		}

		public static Vector3 GetScale(this HmdMatrix34_t pose) {
			float m00 = pose.m0;
			float m01 = pose.m1;
			float m02 = -pose.m2;
			float m10 = pose.m4;
			float m11 = pose.m5;
			float m12 = -pose.m6;
			float m20 = pose.m8;
			float m21 = pose.m9;
			float m22 = -pose.m10;

			float x = (float)Math.Sqrt(m00 * m00 + m01 * m01 + m02 * m02);
			float y = (float)Math.Sqrt(m10 * m10 + m11 * m11 + m12 * m12);
			float z = (float)Math.Sqrt(m20 * m20 + m21 * m21 + m22 * m22);

			return new Vector3(x, y, z);
		}


		public static string PrintMatrix(Matrix m)
		{
			return string.Format("SHARPDX MATRIX:\n{0:F4}\t{1:F4}\t{2:F4}\t{3:F4}\n{4:F4}\t{5:F4}\t{6:F4}\t{7:F4}\n{8:F4}\t{9:F4}\t{10:F4}\t{11:F4}\n{12:F4}\t{13:F4}\t{14:F4}\t{15:F4}\n", new object[]
				{
				m.M11,
				m.M12,
				m.M13,
				m.M14,
				m.M21,
				m.M22,
				m.M23,
				m.M24,
				m.M31,
				m.M32,
				m.M33,
				m.M34,
				m.M41,
				m.M42,
				m.M43,
				m.M44
			});
		}


	}
}
