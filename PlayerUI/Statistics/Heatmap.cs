using PlayerUI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Statistics
{
	public class Heatmap
	{
		private static String codes = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

		struct HeadPosition
		{
			public byte yaw;
			public byte pitch;
		}

		private List<HeadPosition> data;

		public Heatmap()
		{
			data = new List<HeadPosition>();
		}

		public void TrackData(OculusWrap.OVR.Quaternionf quaternion)
		{
			TrackData(new SharpDX.Quaternion((float)quaternion.X, (float)quaternion.Y, (float)quaternion.Z, (float)quaternion.W));
		}

		public void TrackData(System.Windows.Media.Media3D.Quaternion quaternion)
		{
			TrackData(new SharpDX.Quaternion((float)quaternion.X, (float)quaternion.Y, (float)quaternion.Z, (float)quaternion.W));
		}

		public void TrackData(SharpDX.Quaternion quaternion)
		{
			var v = GraphicTools.QuaternionToYawPitch(quaternion);
			TrackData((v.X + Math.PI) / (2F*Math.PI), (v.Y + 0.5f*Math.PI) / (Math.PI));
		}

		public void TrackData(double yaw, double pitch)
		{
			HeadPosition hp = new HeadPosition() { yaw = (byte)((64 * yaw) % 64), pitch = (byte)((64 * pitch) % 64) };
			data.Add(hp);
		}

		public string ToBase64()
		{
			StringBuilder sb = new StringBuilder();
			data.ForEach(pair =>
			{
				sb.Append(codes[pair.yaw].ToString() + codes[pair.pitch].ToString());
			});
			return sb.ToString();
		}

	}
}
