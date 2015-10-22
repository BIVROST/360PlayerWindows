using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BivrostAnalytics
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ProtocolVariableAttribute : System.Attribute
	{
		public string QueryString { get; set; } = "";
		public bool Required { get; set; } = false;
		public int ByteLimit { get; set; } = 0;
	}


	public interface IParameterVariable
	{

	}

	public class StringVariable : IParameterVariable
	{

	}

}
