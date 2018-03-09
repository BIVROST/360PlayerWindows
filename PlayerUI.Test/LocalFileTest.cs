using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bivrost.Bivrost360Player.Streaming;

namespace Bivrost.Bivrost360Player.Test
{
	[TestClass]
	public class LocalFileTest:StreamingTest<LocalFileParser>
	{




		protected override string[] CorrectUris
		{
			get
			{
				return new string[] {
					"D:\\filmy360\\tego-filmu-nie-ma.mp4",
					"D:/filmy360/tego-filmu-nie-ma.mp4",
					"D:\\filmy360\\tutaj\\są spacje i polskie\\znaki.mp4",
					"\\\\Network\\shares\\work-too.mp4",
					"C:\\film.mp4",
					"C:just-drive.mp4",
				};
			}
		}


		protected override string[] IncorrectUris
		{
			get
			{
				return new string[] {
					"file:///c/film.mp4",
					"local-file.mp4",
					"../relative-file.mp4",
					"../dir/relative-file-in-dir.mp4",
					"http://url.example.com/sdf",
					"0:\\film-z-dysku-numerycznego.mp4"
				};
			}
		}


		[TestMethod]
		public void CanParseStream()
		{
			var result = parser.Parse("D:/Filmy360/EHF2016.mp4");
			Assert.IsTrue(result.videoStreams.Count > 0, "no video streams");
			Assert.IsFalse(result.videoStreams.Exists(vs => vs.url == null), "some url contains nulls");
			Assert.AreEqual(MediaDecoder.ProjectionMode.Sphere, result.projection);
			Assert.AreEqual(MediaDecoder.VideoMode.Mono, result.stereoscopy);
		}
	}
}
