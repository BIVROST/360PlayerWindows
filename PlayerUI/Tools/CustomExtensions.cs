using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.Tools
{
	public static class CustomExtensions
	{
		public static float LerpInPlace(this float value1, float value2, float amount)
		{
            if (amount < 0) amount = 0;
            if (amount > 1) amount = 1;
			return value1 + (value2 - value1) * amount;
		}

	}
}
