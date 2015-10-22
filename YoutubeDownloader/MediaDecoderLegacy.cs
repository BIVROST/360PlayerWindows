using SharpDX;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeDownloader
{
	public class MediaDecoderLegacy
	{
		public MediaDecoderLegacy()
		{
		}


		public void Init()
		{
			MediaManager.Startup();

			MediaSession mediaSession;
			MediaFactory.CreateMediaSession(null, out mediaSession);

			SourceResolver sourceResolver = new SourceResolver();
			

			ComObject comObject;
			ObjectType objectType;
			sourceResolver.CreateObjectFromURL(@"D:\TestVideos\Bitspiration3f.mp4", SourceResolverFlags.None);
				//("Jack.mp4", (int)SourceResolverFlags.None, null, out objectType, out comObject);

			Topology topology;
			MediaFactory.CreateTopology(out topology);
			


		}



	}
}
