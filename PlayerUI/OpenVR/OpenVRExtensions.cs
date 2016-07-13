using Valve.VR;
using SharpDX;
using Mathf = System.Math;


namespace PlayerUI.OpenVR
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

			m[2, 0] = -pose.m8;
			m[2, 1] = -pose.m9;
			m[2, 2] = pose.m10;
			m[2, 3] = -pose.m11;

			m[3, 0] = pose.m12;
			m[3, 1] = pose.m13;
			m[3, 2] = pose.m14;
			m[3, 3] = pose.m15;

			m.Transpose();
			m *= -1;

			return m;
		}

		//public static Quaternion GetRotation(this HmdMatrix44_t pose)
		//{
		//	//float m00 = pose.m0;
		//	//float m11 = pose.m5;
		//	//float m22 = pose.m10;

		//	//float m21 = -pose.m9;
		//	//float m02 = -pose.m2;
		//	//float m12 = -pose.m6;
		//	//float m20 = -pose.m8;
		//	//float m10 = pose.m4;
		//	//float m01 = pose.m1;

		//	////float m21 = -pose.m6;
		//	////float m02 = -pose.m8;
		//	//// float m12 = -pose.m9;
		//	//// float m20 = -pose.m2
		//	//// float m10 = pose.m1
		//	//// float m01 = pose.m4;

		//	//Quaternion q = new Quaternion();
		//	//q.W = (float)Mathf.Sqrt(Mathf.Max(0, 1 + m00 + m11 + m22)) / 2;
		//	//q.X = (float)Mathf.Sqrt(Mathf.Max(0, 1 + m00 - m11 - m22)) / 2;
		//	//q.Y = (float)Mathf.Sqrt(Mathf.Max(0, 1 - m00 + m11 - m22)) / 2;
		//	//q.Z = (float)Mathf.Sqrt(Mathf.Max(0, 1 - m00 - m11 + m22)) / 2;
		//	//q.X = _copysign(q.X, m21 - m12);
		//	//q.Y = _copysign(q.Y, m02 - m20);
		//	//q.Z = _copysign(q.Z, m10 - m01);
		//	//return q;
		//}


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
			q.W = (float)Mathf.Sqrt(Mathf.Max(0, 1 + m00 + m11 + m22)) / 2;
			q.X = (float)Mathf.Sqrt(Mathf.Max(0, 1 + m00 - m11 - m22)) / 2;
			q.Y = (float)Mathf.Sqrt(Mathf.Max(0, 1 - m00 + m11 - m22)) / 2;
			q.Z = (float)Mathf.Sqrt(Mathf.Max(0, 1 - m00 - m11 + m22)) / 2;
			q.X = _copysign(q.X, m21 - m12);
			q.Y = _copysign(q.Y, m02 - m20);
			q.Z = _copysign(q.Z, m10 - m01);
			return q;
		}



		private static float _copysign(float sizeval, float signval)
		{
			return Mathf.Sign(signval) == 1 ? Mathf.Abs(sizeval) : -Mathf.Abs(sizeval);
		}



		public static Vector3 GetPosition(this HmdMatrix34_t pose)
		{
			var x = pose.m3;
			var y = pose.m7;
			var z = -pose.m11;

			return new Vector3(x, y, z);
		}

		//public static Vector3 GetScale(this Matrix4x4 m)
		//{
		//	var x = Mathf.Sqrt(m.M00 * m.M00 + m.M01 * m.M01 + m.M02 * m.M02);
		//	var y = Mathf.Sqrt(m.M10 * m.M10 + m.M11 * m.M11 + m.M12 * m.M12);
		//	var z = Mathf.Sqrt(m.M20 * m.M20 + m.M21 * m.M21 + m.M22 * m.M22);

		//	return new Vector3(x, y, z);
		//}


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


		//private static float _copysign(float sizeval, float signval)
		//{
		//	return Mathf.Sign(signval) == 1 ? Mathf.Abs(sizeval) : -Mathf.Abs(sizeval);
		//}


		//public static Quaternion GetRotation(this Matrix matrix)
		//{
		//	Quaternion q = new Quaternion();
		//	q.W = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.M00 + matrix.M11 + matrix.M22)) / 2;
		//	q.X = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.M00 - matrix.M11 - matrix.M22)) / 2;
		//	q.Y = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.M00 + matrix.M11 - matrix.M22)) / 2;
		//	q.Z = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.M00 - matrix.M11 + matrix.M22)) / 2;
		//	q.X = _copysign(q.x, matrix.M21 - matrix.M12);
		//	q.Y = _copysign(q.y, matrix.M02 - matrix.M20);
		//	q.Z = _copysign(q.z, matrix.M10 - matrix.M01);
		//	return q;
		//}


		//public static Vector3 GetPosition(this Matrix matrix)
		//{
		//	var x = matrix.M03;
		//	var y = matrix.M13;
		//	var z = matrix.M23;

		//	return new Vector3(x, y, z);
		//}

		//public static Vector3 GetScale(this Matrix4x4 m)
		//{
		//	var x = Mathf.Sqrt(m.M00 * m.M00 + m.M01 * m.M01 + m.M02 * m.M02);
		//	var y = Mathf.Sqrt(m.M10 * m.M10 + m.M11 * m.M11 + m.M12 * m.M12);
		//	var z = Mathf.Sqrt(m.M20 * m.M20 + m.M21 * m.M21 + m.M22 * m.M22);

		//	return new Vector3(x, y, z);
		//}


		//[System.Serializable]
		//public struct RigidTransform
		//{
		//	public Vector3 pos;
		//	public Quaternion rot;

		//	public static RigidTransform identity
		//	{
		//		get { return new RigidTransform(Vector3.zero, Quaternion.identity); }
		//	}

		//	public RigidTransform(Vector3 pos, Quaternion rot)
		//	{
		//		this.pos = pos;
		//		this.rot = rot;
		//	}

		//	//public RigidTransform(Transform t)
		//	//{
		//	//	this.pos = t.position;
		//	//	this.rot = t.rotation;
		//	//}

		//	//public RigidTransform(Transform from, Transform to)
		//	//{
		//	//	var inv = Quaternion.Inverse(from.rotation);
		//	//	rot = inv * to.rotation;
		//	//	pos = inv * (to.position - from.position);
		//	//}

		//	public RigidTransform(HmdMatrix34_t pose)
		//	{
		//		var m = Matrix4x4.Identity;

		//		m[0, 0] = pose.m0;
		//		m[0, 1] = pose.m1;
		//		m[0, 2] = -pose.m2;
		//		m[0, 3] = pose.m3;

		//		m[1, 0] = pose.m4;
		//		m[1, 1] = pose.m5;
		//		m[1, 2] = -pose.m6;
		//		m[1, 3] = pose.m7;

		//		m[2, 0] = -pose.m8;
		//		m[2, 1] = -pose.m9;
		//		m[2, 2] = pose.M10;
		//		m[2, 3] = -pose.M11;

		//		this.pos = m.GetPosition();
		//		this.rot = m.GetRotation();
		//	}

		//	public RigidTransform(HmdMatrix44_t pose)
		//	{
		//		var m = Matrix4x4.Identity;

		//		m[0, 0] = pose.m0;
		//		m[0, 1] = pose.m1;
		//		m[0, 2] = -pose.m2;
		//		m[0, 3] = pose.m3;

		//		m[1, 0] = pose.m4;
		//		m[1, 1] = pose.m5;
		//		m[1, 2] = -pose.m6;
		//		m[1, 3] = pose.m7;

		//		m[2, 0] = -pose.m8;
		//		m[2, 1] = -pose.m9;
		//		m[2, 2] = pose.M10;
		//		m[2, 3] = -pose.M11;

		//		m[3, 0] = pose.M12;
		//		m[3, 1] = pose.M13;
		//		m[3, 2] = -pose.M14;
		//		m[3, 3] = pose.M15;

		//		this.pos = m.GetPosition();
		//		this.rot = m.GetRotation();
		//	}

		//	public HmdMatrix44_t ToHmdMatrix44()
		//	{
		//		var m = Matrix4x4.TRS(pos, rot, Vector3.one);
		//		var pose = new HmdMatrix44_t();

		//		pose.m0 = m[0, 0];
		//		pose.m1 = m[0, 1];
		//		pose.m2 = -m[0, 2];
		//		pose.m3 = m[0, 3];

		//		pose.m4 = m[1, 0];
		//		pose.m5 = m[1, 1];
		//		pose.m6 = -m[1, 2];
		//		pose.m7 = m[1, 3];

		//		pose.m8 = -m[2, 0];
		//		pose.m9 = -m[2, 1];
		//		pose.M10 = m[2, 2];
		//		pose.M11 = -m[2, 3];

		//		pose.M12 = m[3, 0];
		//		pose.M13 = m[3, 1];
		//		pose.M14 = -m[3, 2];
		//		pose.M15 = m[3, 3];

		//		return pose;
		//	}

		//	public HmdMatrix34_t ToHmdMatrix34()
		//	{
		//		var m = Matrix4x4.TRS(pos, rot, Vector3.one);
		//		var pose = new HmdMatrix34_t();

		//		pose.m0 = m[0, 0];
		//		pose.m1 = m[0, 1];
		//		pose.m2 = -m[0, 2];
		//		pose.m3 = m[0, 3];

		//		pose.m4 = m[1, 0];
		//		pose.m5 = m[1, 1];
		//		pose.m6 = -m[1, 2];
		//		pose.m7 = m[1, 3];

		//		pose.m8 = -m[2, 0];
		//		pose.m9 = -m[2, 1];
		//		pose.M10 = m[2, 2];
		//		pose.M11 = -m[2, 3];

		//		return pose;
		//	}

		//	public override bool Equals(object o)
		//	{
		//		if (o is RigidTransform)
		//		{
		//			RigidTransform t = (RigidTransform)o;
		//			return pos == t.pos && rot == t.rot;
		//		}
		//		return false;
		//	}

		//	public override int GetHashCode()
		//	{
		//		return pos.GetHashCode() ^ rot.GetHashCode();
		//	}

		//	public static bool operator ==(RigidTransform a, RigidTransform b)
		//	{
		//		return a.pos == b.pos && a.rot == b.rot;
		//	}

		//	public static bool operator !=(RigidTransform a, RigidTransform b)
		//	{
		//		return a.pos != b.pos || a.rot != b.rot;
		//	}

		//	//public static RigidTransform operator *(RigidTransform a, RigidTransform b)
		//	//{
		//	//	return new RigidTransform
		//	//	{
		//	//		rot = a.rot * b.rot,
		//	//		pos = a.pos + a.rot * b.pos
		//	//	};
		//	//}

		//	//public void Inverse()
		//	//{
		//	//	rot = Quaternion.Inverse(rot);
		//	//	pos = -(rot * pos);
		//	//}

		//	//public RigidTransform GetInverse()
		//	//{
		//	//	var t = new RigidTransform(pos, rot);
		//	//	t.Inverse();
		//	//	return t;
		//	//}

		//	//public void Multiply(RigidTransform a, RigidTransform b)
		//	//{
		//	//	rot = a.rot * b.rot;
		//	//	pos = a.pos + a.rot * b.pos;
		//	//}

		//	//public Vector3 InverseTransformPoint(Vector3 point)
		//	//{
		//	//	return Quaternion.Inverse(rot) * (point - pos);
		//	//}

		//	//public Vector3 TransformPoint(Vector3 point)
		//	//{
		//	//	return pos + (rot * point);
		//	//}

		//	//public static Vector3 operator *(RigidTransform t, Vector3 v)
		//	//{
		//	//	return t.TransformPoint(v);
		//	//}

		//}

	}
}
