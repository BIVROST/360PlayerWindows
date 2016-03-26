using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using PlayerUI.Streaming;

namespace PlayerUI.Test
{
	[TestClass]
	public class PornhubTest
	{

		PornhubParser parser = new PornhubParser();

		string[] correctUris = new string[] {
			"http://www.pornhub.com/view_video.php?viewkey=439767200",
			"https://www.pornhub.com/view_video.php?viewkey=439767200",
			"http://pornhub.com/view_video.php?viewkey=439767200",
			"http://pornhub.com/view_video.php?viewkey=439767200",
			"http://www.PORNHUB.com/view_video.php?viewkey=439767200",
			"http://www.pornhub.com/view_video.php?smth=asdf&viewkey=439767200"
		};

		string[] incorrectUris = new string[] {
			"ftp://www.pornhub.com/view_video.php?viewkey=439767200",
			"www.pornhub.com/asdf.php?viewkey=439767200",
			"example.com",
			"ASDF.com/view_video.php?viewkey=439767200",
			"www.pornhub.com/www.pornhub.com/view_video.php?viewkey=439767200",
			"view_video.php?viewkey=439767200",
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
		public void CanParseStream()
		{
			var result = parser.Parse("http://www.pornhub.com/view_video.php?viewkey=439767200");
			Assert.IsTrue(result.VideoStreams.Count > 0, "no video streams");
			Assert.IsFalse(result.VideoStreams.Exists(vs => vs.url == null), "some url contains nulls");
			Assert.IsTrue(result.VideoStreams.Exists(vs => vs.url.StartsWith("http://cdn1.hqvideo.pornhub.phncdn.com/videos/201603/18/71403731/")), "did not find url");
			Assert.AreEqual(MediaDecoder.ProjectionMode.Dome, result.projection);
			Assert.AreEqual(MediaDecoder.VideoMode.SideBySide, result.stereoscopy);
		}

		[TestMethod]
		public void ParsesTaB()
		{
			var result = parser.Parse("http://www.pornhub.com/view_video.php?viewkey=705949422");
			Assert.AreEqual(MediaDecoder.VideoMode.TopBottomReversed, result.stereoscopy);
		}

		[TestMethod]
		public void ParsesEquirectangular360()
		{
			var result = parser.Parse("http://www.pornhub.com/view_video.php?viewkey=705949422");
			Assert.AreEqual(MediaDecoder.ProjectionMode.Sphere, result.projection);
		}

		[TestMethod]
		public void FailsOnNotVR()
		{
			try
			{
				var result = parser.Parse("http://www.pornhub.com/view_video.php?viewkey=ph56cede641bf05");
				Assert.Fail("did not fail on non VR");
			}
			catch (Streaming.StreamNotSupported)
			{
				// pass
			}
		}
		
	}
}
