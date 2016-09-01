using System;
using SharpDX;

namespace PlayerUI.Statistics
{
    internal interface ILookProvider
    {
        event Action<Vector3, Quaternion, float> ProvideLook;
        string DescribeType { get; }
    }
}