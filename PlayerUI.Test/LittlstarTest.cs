using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bivrost.Bivrost360Player.Streaming;

namespace Bivrost.Bivrost360Player.Test
{
	[TestClass]
	public class LittlstarTest:StreamingTest<LittlstarParser>
	{




		protected override string[] CorrectUris
		{
			get
			{
				return new string[] {
					"https://littlstar.com/videos/0fde0e55",
					"https://littlstar.com/videos/114a6c9b",
					"https://littlstar.com/videos/0fde0e55/",
					"https://littlstar.com/videos/0fde0e55?asd",
					"https://LittlStar.com/videos/0fde0e55",
					"https://littlstar.com/videos/0fde0e55#asd",
					"https://embed.littlstar.com/videos/114a6c9b",
					"http://embed.littlstar.com/videos/114a6c9b",
				};
			}
		}


		protected override string[] IncorrectUris
		{
			get
			{
				return new string[] {
					"ftp://littlstar.com/videos/0fde0e55",
					"https://littlstar.com/photos/0fde0e55",
					"https://littlstar.com/photos/0fde0e55/videos/asd",
					"https://littlstar.com/",
					"view_video.php?viewkey=439767200",
					"vrideo.com/watchcośtam",
					"",
					null,
					"\n"
				};
			}
		}


		[TestMethod]
		public void CanParseStream()
		{
			var result = parser.Parse("https://littlstar.com/videos/0fde0e55");
			Assert.IsTrue(result.videoStreams.Count > 0, "no video streams");
			Assert.IsFalse(result.videoStreams.Exists(vs => vs.url == null), "some url contains nulls");
			Assert.AreEqual(MediaDecoder.ProjectionMode.Sphere, result.projection);
			Assert.AreEqual(MediaDecoder.VideoMode.Mono, result.stereoscopy);
		}
	}
}
