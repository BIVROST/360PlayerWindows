﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bivrost.Bivrost360Player.Streaming;
using System;

namespace Bivrost.Bivrost360Player.Test
{

	[TestClass]
	public abstract class StreamingTest<T> where T: ServiceParser, new()
	{
		protected T parser = new T();


		abstract protected string[] CorrectUris { get; }
		abstract protected string[] IncorrectUris { get; }


		[TestMethod]
		public void ShouldParseURIs()
		{
			foreach (var uri in CorrectUris)
				Assert.IsTrue(parser.CanParse(uri), "should parse " + uri);
		}


		[TestMethod]
		public void ShouldntParseURIs()
		{
			foreach (var uri in IncorrectUris)
				Assert.IsTrue(!parser.CanParse(uri), "shouldn't parse " + uri);
		}


        [TestMethod]
        public void CanParseURIs()
        {
            foreach (var uri in CorrectUris)
            {
                Bivrost.Log.LoggerManager.Info("parsing " + uri + "...");
                parser.Parse(uri);
            }
        }
	}



}
