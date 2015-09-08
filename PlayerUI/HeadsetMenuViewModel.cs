using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerUI
{
	public class HeadsetMenuViewModel : FadeMenuBase
	{
		public event Action OnRift = delegate { };

		public void SelectRift()
		{
			this.Hide(0.25f);
			OnRift();
		}
	}
}
