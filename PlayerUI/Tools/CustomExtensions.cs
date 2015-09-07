using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI.Tools
{
	public static class CustomExtensions
	{
		public static float LerpInPlace(this float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
		}

	}
}
