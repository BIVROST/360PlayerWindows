using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Bivrost.Bivrost360Player
{
	public class EnabledFlowDocument : FlowDocument
	{
		protected override bool IsEnabledCore
		{
			get
			{
				return true;
			}
		}
	}
}
