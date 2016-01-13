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

		[TestMethod]
		public void CanParseStereo()
		{
			// TODO
		}

		[TestMethod]
		public void CanParseProjection()
		{
			// TODO
		}


		[TestMethod]
		public async Task CanParseStream()
		{
			var parser = new PlayerUI.Streaming.VrideoParser();
			var result = await parser.TryParse("vrideo.com/watch/KLOtqZE");
			Assert.IsTrue(result.VideoStreams.Count > 0, "no video streams");
			Assert.AreEqual(MediaDecoder.ProjectionMode.Sphere, result.projection);
			Assert.AreEqual(MediaDecoder.VideoMode.Mono, result.stereoscopy);
		}

		[TestMethod]
		public async Task ParsesTaB()
		{
			var parser = new Streaming.VrideoParser();
			var result = await parser.TryParse("http://www.vrideo.com/watch/bh4WvTc4");
			Assert.AreEqual(MediaDecoder.VideoMode.TopBottom, result.stereoscopy);
		}

		[TestMethod]
		public async Task ParsesSbS()
		{
			var parser = new Streaming.VrideoParser();
			var result = await parser.TryParse("http://www.vrideo.com/watch/FomUnwO");
			Assert.AreEqual(MediaDecoder.VideoMode.SideBySide, result.stereoscopy);
		}



	}
}
