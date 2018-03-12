using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bivrost.Bivrost360Player.Streaming;

namespace Bivrost.Bivrost360Player.Test
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
