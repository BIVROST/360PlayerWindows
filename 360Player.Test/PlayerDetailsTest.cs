using System;
using Bivrost.AnalyticsForVR;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bivrost.Bivrost360Player.Test
{
    [TestClass]
    public class PlayerDetailsTest
    {
        [TestMethod]
        public void TestPlayerDetailsCurrentWorks()
        {
            var pd = Bivrost.AnalyticsForVR.GhostVRConnector.PlayerDetails.Current;
            Assert.AreEqual(pd.name, "BIVROST 360Player");

        }


        [TestMethod]
        public void TestPlayerDetailsQsWorks()
        {
            var pd = new GhostVRConnector.PlayerDetails()
            {
				licenseType = GhostVRConnector.PlayerDetails.LicenseType.development,
				version = "0.0.0.0"
            };
            Assert.AreEqual(pd.AsQsFormat, "player%5Bname%5D=BIVROST%20360Player&player%5Bversion%5D=0.0.0.0&player%5Blicense_type%5D=development");
        }
        

    }
}
