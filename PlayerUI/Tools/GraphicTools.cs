using OculusWrap;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Tools
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

		public static GeometricPrimitive GenerateFacebookCube(GraphicsDevice graphicsDevice)
		{
			GeometricPrimitive primitive = SharpDX.Toolkit.Graphics.GeometricPrimitive.Cube.New(graphicsDevice, 6, true);
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

			primitive.Dispose();

			return new SharpDX.Toolkit.Graphics.GeometricPrimitive(graphicsDevice, data, indices);
		}

		public static GeometricPrimitive CreateGeometry(MediaDecoder.ProjectionMode projection, GraphicsDevice graphicsDevice, bool toLeftHanded=true)
		{
			switch (projection)
			{
				case MediaDecoder.ProjectionMode.Sphere: return GeometricPrimitive.Sphere.New(graphicsDevice, 6, 64, toLeftHanded);
				case MediaDecoder.ProjectionMode.CubeFacebook: return GenerateFacebookCube(graphicsDevice);
				default: throw new ArgumentException("Unknown projection");
			}
		}

		public static Vector2 QuaternionToYawPitch(OVRTypes.Quaternionf ovrQuatf)
		{
			return QuaternionToYawPitch(new Quaternion(-ovrQuatf.X, -ovrQuatf.Y, -ovrQuatf.Z, ovrQuatf.W));
		}

		public static Vector2 QuaternionToYawPitch(Quaternion q)
		{			
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
