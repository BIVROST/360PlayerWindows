using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayerUI.Streaming;

namespace PlayerUI.Test
{
    [TestClass]
    public class YoutubeTest : StreamingTest<YoutubeParser>
    {
        protected override string[] CorrectUris
        {
            get
            {
                return new string[]
                {
                    "https://www.youtube.com/watch?v=12otR342ijc",
                    "http://www.youtube.com/watch?v=12otR342ijc",
                    "www.youtube.com/watch?v=12otR342ijc",
                    "www.youtube.com/watch?v=12otR342ijc",
                    "https://youtube.com/watch?v=12otR342ijc",
                    "http://youtube.com/watch?v=12otR342ijc",
                    "youtube.com/watch?v=12otR342ijc",
                    "youtube.com/watch?v=12otR342ijc",
                    "https://www.youtube.com/watch?v=ywoe0obYaLU",
                    "youtu.be/8lsB-P8nGSM",
                    "http://youtu.be/8lsB-P8nGSM",
                    "https://youtu.be/8lsB-P8nGSM",
                    "https://www.youtube.com/watch?qqq=www&v=12otR342ijc",
                    "https://www.youtube.com/watch?v=hJPwn7o0K7c&index=1&list=PLU8wpH_LfhmvMokgsfQtiHNsP96bU7cnr"
                };
            }
        }

        protected override string[] IncorrectUris
        {
            get
            {
                return new string[] {
                    "www.youtu.be/8lsB-P8nGSM",
                    "http://www.youtu.be/8lsB-P8nGSM",
                    "https://www.youtu.be/8lsB-P8nGSM"
                };
            }
        }


        [TestMethod]
        public void ShouldParseURI()
        {
            // var result = parser.Parse("https://www.youtube.com/watch?v=RWYKrePZwkM");
            // var result = parser.Parse("https://www.youtube.com/watch?v=h8mwhm0PoKc");
            var result = parser.Parse("https://www.youtube.com/watch?v=hJPwn7o0K7c&index=1&list=PLU8wpH_LfhmvMokgsfQtiHNsP96bU7cnr");
            ;
        }


        public void CanParseMovieForUSOnly()
        {
            // ERROR: YouTube said: This video is available in United States only
            var result = parser.Parse("https://www.youtube.com/watch?v=hJPwn7o0K7c&index=1&list=PLU8wpH_LfhmvMokgsfQtiHNsP96bU7cnr");
        }

    }
}
