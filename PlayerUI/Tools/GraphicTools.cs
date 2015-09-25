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
			Vector2 vector2;
			if (index != 5)
				vector2 = new Vector2(vector.X == 1 ? 0 : 1, vector.Y);
			else
				vector2 = new Vector2(vector.X, vector.Y == 1 ? 0 : 1);

			Dictionary<int, Vector4> map = new Dictionary<int, Vector4>();

			map.Add(3, new Vector4(3f, 0f / 3f, 2f, 0f));
			map.Add(2, new Vector4(3f, 1f / 3f, 2f, 0f));
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

		public static GeometricPrimitive CreateGeometry(MediaDecoder.ProjectionMode projection, GraphicsDevice graphicsDevice)
		{
			switch (projection)
			{
				case MediaDecoder.ProjectionMode.Sphere: return GeometricPrimitive.Sphere.New(graphicsDevice, 6, 32, true);
				case MediaDecoder.ProjectionMode.CubeFacebook: return GenerateFacebookCube(graphicsDevice);
				default: throw new ArgumentException("Unknown projection");
			}
		}


	}
}
