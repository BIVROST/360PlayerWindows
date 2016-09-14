using PlayerUI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace PlayerUI.Statistics
{
	public class LookHistory
	{
		private const string BASE64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

		class HeadPosition
		{

            public static byte Angle01to063(double angle01)
            {
                int v = (int)Math.Round(64 * angle01);
                while (v < 0)
                    v += 64;
                while (v >= 64)
                    v -= 64;
                return (byte)v;
            }

            public HeadPosition(double yaw01, double pitch01, float t = 0, byte fov = 0)
            {
                yaw = Angle01to063(yaw01);
                pitch = Angle01to063(pitch01);
                this.t = t;
                this.fov = fov;
            }

            public HeadPosition(char yawBase64, char pitchBase64, float t = 0, byte fov = 0)
            {
                yaw = (byte)BASE64.IndexOf(yawBase64);
                pitch = (byte)BASE64.IndexOf(pitchBase64);
                this.t = t;
                this.fov = fov;
            }

            public readonly float t;
			internal readonly byte yaw;
            internal readonly byte pitch;
            public readonly byte fov;

            public double Yaw { get { return (yaw * 2F * Math.PI) - Math.PI; } }
            public double Pitch { get { return pitch * Math.PI - 0.5f * Math.PI; } }
        }

        private List<HeadPosition> data;
        private int precision;

        public int SampleRate { get { return precision; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="precision">How many times per second should heatmaps be sampled</param>
        /// <param name="mediaLength">How long is this medium</param>
        public LookHistory(int precision, double mediaLength)
		{
            this.precision = precision;
            int count = (int)Math.Ceiling(mediaLength / precision);
            data = new List<HeadPosition>(count);
            data.AddRange(Enumerable.Repeat<HeadPosition>(null, count));
        }

        public void TrackData(float t, SharpDX.Quaternion quaternion, byte fov)
		{
            //double rad2deg = 180 / Math.PI;
			var v = GraphicTools.QuaternionToYawPitch(quaternion);
            // angle from quaternion should be always [-4PI, 4PI], no need for while loop
            double yaw = ( Math.PI * 10 - v.X ) % (2 * Math.PI) - Math.PI; // => [-PI, +PI]
            double pitch = -v.Y;    // => [-PI/2, PI/2]
            double yaw01 = yaw / (2 * Math.PI) + 0.5;
            double pitch01 = pitch / Math.PI + 0.5;
            TrackData(t, yaw01, pitch01, fov);
		}

		void TrackData(float t, double yaw01, double pitch01, byte fov)
		{
            int idx = (int)Math.Floor(t * precision);
            HeadPosition headPosition = new HeadPosition(yaw01, pitch01, t, fov);
            lock (data)
            {
                // add new element at end (and return)
                if (data.Count == idx)
                {
                    data.Add(headPosition);
                    return;
                }
                
                // no space for element - need to resize
                else if (data.Count <= idx)
                { 
                    if (idx > data.Capacity)
                        data.Capacity = idx + 1;
                    data.AddRange(Enumerable.Repeat<HeadPosition>(null, data.Capacity - data.Count));
                }

                // finally add the element
                if (data[idx] == null)
                {
                    // TODO: integrate
                }
                data[idx] = (headPosition);
            }
        }

		public string ToBase64()
		{
            int fov = 0;
            StringBuilder sb = new StringBuilder();
            lock (data) data.ForEach(pair =>
            {
                if (pair == null)
                    sb.Append("--");
                else
                {
                    if (pair.fov != fov)
                    {
                        fov = pair.fov;
                        sb.Append($"!F{fov}!");
                    }
                    sb.Append(BASE64[pair.yaw]);
                    sb.Append(BASE64[pair.pitch]);
                }
            });
			return sb.ToString();
		}

        //public static LookHistory FromBase64(string serialized)
        //{
        //    throw new NotImplementedException();
        //}

	}
}
