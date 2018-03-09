using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bivrost.Bivrost360Player.Streaming;

namespace Bivrost.Bivrost360Player.Test
{
	[TestClass]
	public class VrideoTest
	{

		VrideoParser parser = new VrideoParser();

		string[] correctUris = new string[] {
			"http://www.vrideo.com/watch/KLOtqZE",
			"https://www.vrideo.com/watch/KLOtqZE",
			"www.vrideo.com/watch/KLOtqZE",
			"vrideo.com/watch/KLOtqZE",
			"vrideo.com/watch/KLOtqZE?some&arg#hash",
			"VRIDEO.com/watch/KLOtqZE",
			"http://vrideo.com/watch/KLOtqZE"
		};

		string[] incorrectUris = new string[] {
			"ftp://www.vrideo.com/watch/KLOtqZE",
			"www.vrideo.com/łocz/KLOtqZE",
			"ftp://www.vrideo.com/łocz/KLOtqZE",
			"youtube.com/watch/KLOtqZE",
			"vrideo.com/asdf",
			"vrideo.com/watchcośtam",
			"",
			null,
			"\n"
		};

		[TestMethod]
		public void ShouldParseURIs()
		{
			foreach (var uri in correctUris)
				Assert.IsTrue(parser.CanParse(uri), "should parse " + uri);
		}

		[TestMethod]
		public void ShouldntParseURIs()
		{
			foreach (var uri in incorrectUris)
				Assert.IsTrue(!parser.CanParse(uri), "shouldn't parse " + uri);
		}


		[TestMethod]
		public void CanExtractIdFromUri()
		{
			foreach (var uri in correctUris)
			{
				var id = parser.UriToId(uri);
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
		public void CanParseStream()
		{
			var result = parser.Parse("vrideo.com/watch/KLOtqZE");
			Assert.IsTrue(result.videoStreams.Count > 0, "no video streams");
			Assert.IsFalse(result.videoStreams.Exists(vs => vs.url == null), "some url contains nulls");
			Assert.IsTrue(result.videoStreams.Exists(vs => vs.url == "http://cdn2.vrideo.com/prod_videos/v1/KLOtqZE_4k_full.mp4"), "did not find mp4 4k");
			Assert.AreEqual(MediaDecoder.ProjectionMode.Sphere, result.projection);
			Assert.AreEqual(MediaDecoder.VideoMode.Mono, result.stereoscopy);
		}

		[TestMethod]
		public void ParsesTaB()
		{
			var result = parser.Parse("http://www.vrideo.com/watch/bh4WvTc4");
			Assert.AreEqual(MediaDecoder.VideoMode.TopBottom, result.stereoscopy);
		}

		[TestMethod]
		public void ParsesSbS()
		{
			var result = parser.Parse("http://www.vrideo.com/watch/FomUnwO");
			Assert.AreEqual(MediaDecoder.VideoMode.SideBySide, result.stereoscopy);
		}

		[TestMethod]
		public void FailsOnFisheye()
		{
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
		public void SupportsDome()
		{
			var result = parser.Parse("http://www.vrideo.com/watch/tH270XQ");
			Assert.AreEqual(result.projection, MediaDecoder.ProjectionMode.Dome);
		}

	}
}
