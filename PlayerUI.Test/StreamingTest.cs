using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace PlayerUI.Test
{
	[TestClass]
	public class VrideoTest
	{

		string[] correctUris = new string[] {
			"http://www.vrideo.com/watch/KLOtqZE",
			"https://www.vrideo.com/watch/KLOtqZE",
			"www.vrideo.com/watch/KLOtqZE",
			"vrideo.com/watch/KLOtqZE",
			"vrideo.com/watch/KLOtqZE?some&arg#hash",
			"VRIDEO.com/watch/KLOtqZE",
			"http://vrideo.com/watch/KLOtqZE"
		}; 

		[TestMethod]
		public void ShouldParseURIs() {
			var parser = new PlayerUI.Streaming.VrideoParser();
			foreach (var uri in correctUris)
				Assert.IsTrue(parser.CanParse(uri), "should parse " + uri);
		}

		[TestMethod]
		public void ShouldntParseURIs() {
			var parser = new PlayerUI.Streaming.VrideoParser();
			foreach (var uri in new string[] {
				"ftp://www.vrideo.com/watch/KLOtqZE",
				"www.vrideo.com/łocz/KLOtqZE",
				"ftp://www.vrideo.com/łocz/KLOtqZE",
				"youtube.com/watch/KLOtqZE",
				"vrideo.com/asdf",
				"vrideo.com/watchcośtam",
				"",
				null,
				"\n"
			})
				Assert.IsTrue(!parser.CanParse(uri), "shouldn't parse " + uri);
		}


		[TestMethod]
		public void CanExtractIdFromUri() {
			var parser = new PlayerUI.Streaming.VrideoParser();
			foreach (var uri in correctUris) {
				var id = parser.UriToId("vrideo.com/watch/KLOtqZE");
				Assert.AreEqual("KLOtqZE", id, uri + " should have the id of KLOtqZE, had: " + id);
			}
		}

		//[TestMethod]
		//public void CanParseStereo()
		//{
		//	// TODO
		//}

		//[TestMethod]
		//public void CanParseProjection()
		//{
		//	// TODO
		//}

		[TestMethod]
		public async Task CanAsync() {
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
			catch (Streaming.StreamNetworkFailue) {; }

			try
			{
				await Streaming.ServiceParser.HTTPGetStringAsync("http://127.0.0.1:6666/");
				Assert.Fail("connection failure detection failed");
			}
			catch (Streaming.StreamNetworkFailue) {; }

			try
			{
				await Streaming.ServiceParser.HTTPGetStringAsync("http://thisdomainshouldntexistasdasd.net/");
				Assert.Fail("dns failure detection failed");
			}
			catch (Streaming.StreamNetworkFailue) {; }
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
			catch (Streaming.StreamNetworkFailue) {; }

			try
			{
				Streaming.ServiceParser.HTTPGetString("http://127.0.0.1:6666/");
				Assert.Fail("connection failure detection failed");
			}
			catch (Streaming.StreamNetworkFailue) {; }

			try
			{
				Streaming.ServiceParser.HTTPGetString("http://thisdomainshouldntexistasdasd.net/");
				Assert.Fail("dns failure detection failed");
			}
			catch (Streaming.StreamNetworkFailue) {; }
		}

		[TestMethod]
		public void CanParseStream()
		{
			var parser = new Streaming.VrideoParser();
			var result = parser.Parse("vrideo.com/watch/KLOtqZE");
			Assert.IsTrue(result.VideoStreams.Count > 0, "no video streams");
			Assert.IsFalse(result.VideoStreams.Exists(vs => vs.url == null), "some url contains nulls");
			Assert.IsTrue(result.VideoStreams.Exists(vs => vs.url == "http://cdn2.vrideo.com/prod_videos/v1/KLOtqZE_4k_full.mp4"), "did not find mp4 4k");
			Assert.AreEqual(MediaDecoder.ProjectionMode.Sphere, result.projection);
			Assert.AreEqual(MediaDecoder.VideoMode.Mono, result.stereoscopy);
		}

		[TestMethod]
		public void ParsesTaB()
		{
			var parser = new Streaming.VrideoParser();
			var result = parser.Parse("http://www.vrideo.com/watch/bh4WvTc4");
			Assert.AreEqual(MediaDecoder.VideoMode.TopBottom, result.stereoscopy);
		}

		[TestMethod]
		public void ParsesSbS()
		{
			var parser = new Streaming.VrideoParser();
			var result = parser.Parse("http://www.vrideo.com/watch/FomUnwO");
			Assert.AreEqual(MediaDecoder.VideoMode.SideBySide, result.stereoscopy);
		}

		[TestMethod]
		public void FailsOnFisheye()
		{
			var parser = new Streaming.VrideoParser();
			try
			{
				var result = parser.Parse("http://www.vrideo.com/watch/5W44H7Y");
				Assert.Fail("did not fail on fisheye");
			}
			catch (Streaming.StreamNotSupported)
			{
				// pass
			}
		}

		[TestMethod]
		public void FailsOnDome()
		{
			var parser = new Streaming.VrideoParser();
			try
			{
				var result = parser.Parse("http://www.vrideo.com/watch/tH270XQ");
				Assert.Fail("did not fail on dome");
			}
			catch (Streaming.StreamNotSupported)
			{
				// pass
			}
		}

	}
}
