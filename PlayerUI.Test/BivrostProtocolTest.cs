using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using PlayerUI.Streaming;

namespace PlayerUI.Test
{
	[TestClass]
	public class BivrostProtocolTest:StreamingTest<LocalFileParser>
	{




		protected override string[] CorrectUris
		{
			get
			{
				return new string[] {
				};
			}
		}


		protected override string[] IncorrectUris
		{
			get
			{
				return new string[] {
				};
			}
		}


	}
}
