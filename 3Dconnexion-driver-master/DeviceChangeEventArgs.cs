using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _3DconnexionDriver
{
    public class DeviceChangeEventArgs : EventArgs
    {
        public readonly EDeviceChangeType Type;
        public readonly int DeviceID;

        public DeviceChangeEventArgs(int deviceId, int type)
        {
            this.DeviceID = type;
            switch (type)
            {
                case 0: Type = EDeviceChangeType.CONNECTED; break;
                case 1: Type = EDeviceChangeType.DISCONNECTED; break;
                default: Type = EDeviceChangeType.UNKNOWN; break;
            }
        }
    }

    public enum EDeviceChangeType
    {
        CONNECTED,
        DISCONNECTED,
        UNKNOWN
    }
}
