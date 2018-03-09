using System;
using SharpDX;

namespace Bivrost.AnalyticsForVR
{
    public interface ILookProvider
    {
		/// <summary>
		/// SharpDX.Vector3 position
		/// SharpDX.Quaternion rotation
		/// float fov
		/// </summary>
		event Action<Vector3, Quaternion, float> ProvideLook;
        string DescribeType { get; }
	}
}