using OculusWrap;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.Tools
{
	public class GraphicTools
	{
		public static Vector2 MapCubeFacebook(int index, Vector2 vector)
		{
			Vector2 vector2 = vector;
			if (index == 5)
				vector2 = new Vector2(1 - vector.X, 1 - vector.Y);			

			Dictionary<int, Vector4> map = new Dictionary<int, Vector4>();

			map.Add(2, new Vector4(3f, 0f / 3f, 2f, 0f));
			map.Add(3, new Vector4(3f, 1f / 3f, 2f, 0f));
			map.Add(4, new Vector4(3f, 2f / 3f, 2f, 0f));
			map.Add(5, new Vector4(3f, 0f / 3f, 2f, 1f / 2f));
			map.Add(1, new Vector4(3f, 1f / 3f, 2f, 1f / 2f));
			map.Add(0, new Vector4(3f, 2f / 3f, 2f, 1f / 2f));

			return new Vector2(
				vector2.X / map[index].X + map[index].Y,
				vector2.Y / map[index].Z + map[index].W
				);
		}

		public static GeometricPrimitive GenerateFacebookCube(GraphicsDevice graphicsDevice, bool toLeftHanded)
		{
			using (GeometricPrimitive primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Cube.New(graphicsDevice, 6, toLeftHanded))
			{
				short[] indices = primitive.IndexBuffer.GetData<short>();
				var data = primitive.VertexBuffer.GetData();

				float offset = 0.99f;

				for (int it = 0; it < data.Length; it++)
				{
					//0,0
					//0,1
					//1,1
					//1,0
					int index = (int)Math.Floor(it / 4f);
					data[it].TextureCoordinate = MapCubeFacebook(index, data[it].TextureCoordinate);
					switch (index)
					{
						case 0:
							data[it].Position.Z *= offset;
							break;
						case 1:
							data[it].Position.Z *= offset;
							break;
						case 2:
							data[it].Position.X *= offset;
							break;
						case 3:
							data[it].Position.X *= offset;
							break;
						case 4:
							data[it].Position.Y *= offset;
							break;
						case 5:
							data[it].Position.Y *= offset;
							break;
					}
				}

				return new GeometricPrimitive(graphicsDevice, data, indices);
			}
		}


		private static GeometricPrimitive GenerateDome(GraphicsDevice graphicsDevice, bool toLeftHanded)
		{
			//const int vslices = 2;
			//const int hslices = vslices * 2;
			//const float radius = 6f;
			//Vector3 forward = toLeftHanded ? Vector3.ForwardLH : Vector3.ForwardRH;

			//VertexPositionNormalTexture[] data = new VertexPositionNormalTexture[(hslices + 1) * (vslices + 1)];
			//for (int horiz = 0; horiz <= hslices; horiz++)      // +1 (<=) h slice for the same points with X = 0 and X = 1
			//	for (int vert = 0; vert <= vslices; vert++)		// +1 (<=) slice for bottom cap points
			//	{
			//		int idx = vert * hslices + horiz;
			//		float yaw = (float)((Math.PI * 2 * horiz) / hslices);
			//		float pitch = (float)((Math.PI * vert) / vslices - Math.PI / 2);
			//		data[idx] = new VertexPositionNormalTexture();
			//		Vector3 n = Vector3.Transform(forward, Quaternion.RotationYawPitchRoll(yaw, pitch, 0f));

			//		data[idx].Normal = n;
			//		data[idx].Position = n * radius;
			//		data[idx].TextureCoordinate.X = horiz / (float)hslices;
			//		data[idx].TextureCoordinate.Y = 1 - vert / (float)vslices;
			//	}

			//short[] indices = new short[6 * hslices * vslices];
			//for (int v = 0; v < vslices; v++)
			//	for (int h = 0; h < hslices; h++)
			//	{
			//		int idx = 6 * (v * hslices + h) - 3;

			//		// A---B   triangles:
			//		// | / |   ABC, BDC
			//		// C---D   ACB, BCD

			//		// no need for modulo - indices are [hslices+1, vslices+1]
			//		short idxA = (short)(v * hslices + h);
			//		short idxB = (short)(v * hslices + (h + 1));
			//		short idxC = (short)((v + 1) * hslices + h);
			//		short idxD = (short)((v + 1) * hslices + (h + 1));

			//		// not first v slice (top cap)
			//		if (v > 0)
			//		{
			//			indices[idx] = idxA;
			//			indices[idx + 1] = idxC;
			//			indices[idx + 2] = idxB;
			//		}

			//		// not last v slice (bottom cap)
			//		if (v < vslices - 1)
			//		{
			//			indices[idx + 3] = idxB;
			//			indices[idx + 4] = idxC;
			//			indices[idx + 5] = idxD;
			//		}
			//	}

			//return new GeometricPrimitive(graphicsDevice, data, indices);

			using (GeometricPrimitive primitive = GeometricPrimitive.Sphere.New(graphicsDevice, 50, 64, toLeftHanded))
			{

				short[] indices = primitive.IndexBuffer.GetData<short>();
				var data = primitive.VertexBuffer.GetData();

				for (int it = 0; it < data.Length; it++)
				{
					var u = data[it].TextureCoordinate.X * 2 - 0.5f;

					// reverse
					//if (x > 1)
					//	x = x - 1;

					// mirror
					//if (x > 1)
					//	x = 2 - x;

					data[it].TextureCoordinate.X = u;

					//var z = data[it].Position.Z;
					//data[it].Position.Z = -data[it].Position.X;
					//data[it].Position.X = z;

					//z = data[it].Normal.Z;
					//data[it].Normal.Z = -data[it].Normal.X;
					//data[it].Normal.X = z;
				}

				// remove edges (requires mirror or reverse)
				//indices = indices.ToList().FindAll(i => data[i].TextureCoordinate.X > 0.05f && data[i].TextureCoordinate.X < 0.95f).ToArray();

				// remove back (do not use with mirror or reverse)
				//indices = indices.ToList().FindAll(i => data[i].TextureCoordinate.X <= 1).ToArray();

				return new GeometricPrimitive(graphicsDevice, data, indices);
			}
		}


		public static GeometricPrimitive CreateGeometry(ProjectionMode projection, GraphicsDevice graphicsDevice, bool toLeftHanded=true)
		{
			switch (projection)
			{
				case ProjectionMode.Sphere: return GeometricPrimitive.Sphere.New(graphicsDevice, 6, 64, toLeftHanded);
				case ProjectionMode.CubeFacebook: return GenerateFacebookCube(graphicsDevice, toLeftHanded);
				case ProjectionMode.Dome: return GenerateDome(graphicsDevice, toLeftHanded);
				default: throw new ArgumentException("Unknown projection: " + projection);
			}
		}


		public static Vector2 QuaternionToYawPitch(OVRTypes.Quaternionf ovrQuatf)
		{
			return QuaternionToYawPitch(new Quaternion(-ovrQuatf.X, -ovrQuatf.Y, -ovrQuatf.Z, ovrQuatf.W));
		}

		public static Vector2 QuaternionToYawPitch(Quaternion qVeryWTF)
		{
            // visual studio compiler bug? 
            // without this the parameter quaternion is modified by line #208 and this change persists in the calling scope... sometimes
            Quaternion q = new Quaternion(qVeryWTF.X, qVeryWTF.Y, qVeryWTF.Z, qVeryWTF.W);

			q.Normalize();

			var x = 0f;
			var y = 0f;
			var z = 1f;

			var pole = q.X * q.W + q.Z * q.Y;
			q = q * Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(90) * x, MathUtil.DegreesToRadians(90) * y, MathUtil.DegreesToRadians(90) * z);

			q.Normalize();

			double yaw = 0, pitch = 0;

			ShellViewModel.Instance.AppendDebugText($"X: {q.X} \t Y: {q.Y} \t Z: {q.Z} \t W: {q.W}");

			if (pole > 0.49999f)
			{
				yaw = 2f * (float)Math.Atan2(q.Y, q.X) + Math.PI * 0.5f;
				ShellViewModel.Instance.AppendDebugText("\t" + MathUtil.RadiansToDegrees((float)yaw));
				pitch = -Math.PI * 0.5f;
			}
			else if (pole < -0.49999f)
			{
				yaw = 2f * (float)Math.Atan2(q.Y, q.Z) - Math.PI * 0.5f;
				ShellViewModel.Instance.AppendDebugText("\t" + MathUtil.RadiansToDegrees((float)yaw));
				pitch = Math.PI * 0.5f;
			}
			else
			{
				yaw = Math.Atan2(2 * (q.Y * q.Z + q.W * q.X), q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z);
				pitch = Math.Asin(-2 * (q.X * q.Z - q.W * q.Y));
				//roll = Math.Atan2(2 * (q.X * q.Y + q.W * q.Z), q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);
			}

			yaw = yaw > Math.PI ? yaw - 2 * Math.PI : (yaw < -Math.PI ? yaw + 2 * Math.PI : yaw);

			return new Vector2((float)yaw, (float)pitch);
		}

	}
}
