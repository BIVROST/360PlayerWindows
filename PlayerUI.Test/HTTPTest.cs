using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.Test
{
	[TestClass]
	public class HTTPTest
	{

		[TestMethod]
		public async Task CanAsync()
		{
			await Task.Delay(2000);
		}

		[TestMethod]
		public async Task CanHttpAsync()
		{
			var result = await Streaming.ServiceParser.HTTPGetStringAsync("http://example.com/");
		}

		[TestMethod]
		public async Task CanDetectHttpFailAsync()
		{
			try
			{
				await Streaming.ServiceParser.HTTPGetStringAsync("http://example.com/404");
				Assert.Fail("404 detection failed");
			}
			catch (Streaming.StreamNetworkFailure) {; }

			try
			{
				await Streaming.ServiceParser.HTTPGetStringAsync("http://127.0.0.1:6666/");
				Assert.Fail("connection failure detection failed");
			}
			catch (Streaming.StreamNetworkFailure) {; }

			try
			{
				await Streaming.ServiceParser.HTTPGetStringAsync("http://thisdomainshouldntexistasdasd.net/");
				Assert.Fail("dns failure detection failed");
			}
			catch (Streaming.StreamNetworkFailure) {; }
		}

		[TestMethod]
		public void CanHttp()
		{
			Streaming.ServiceParser.HTTPGetString("http://example.com/");
		}

		[TestMethod]
		public void CanDetectHttpFail()
		{
			try
			{
				Streaming.ServiceParser.HTTPGetString("http://example.com/404");
				Assert.Fail("404 detection failed");
			}
			catch (Streaming.StreamNetworkFailure) {; }

			try
			{
				Streaming.ServiceParser.HTTPGetString("http://127.0.0.1:6666/");
				Assert.Fail("connection failure detection failed");
			}
			catch (Streaming.StreamNetworkFailure) {; }

			try
			{
				Streaming.ServiceParser.HTTPGetString("http://thisdomainshouldntexistasdasd.net/");
				Assert.Fail("dns failure detection failed");
			}
			catch (Streaming.StreamNetworkFailure) {; }
		}

	}
}
