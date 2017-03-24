using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _3DconnexionDriver
{
    public class MotionEventArgs : EventArgs
    {
        public readonly int TX, TY, TZ;
        public readonly int RX, RY, RZ;

        public MotionEventArgs(int tx, int ty, int tz, int rx, int ry, int rz)
        {
            this.TX = tx;
            this.TY = ty;
            this.TZ = tz;
            this.RX = rx;
            this.RY = ry;
            this.RZ = rz;
        }

        public static MotionEventArgs FromEventArray(int[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length < 6)
                throw new ArgumentException("data array to small");

            return new MotionEventArgs(data[0], data[1], data[2], data[3], data[4], data[5]);
        }
    }
}
