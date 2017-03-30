using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlayerUI.Test
{
    [TestClass]
    public class PlayerDetailsTest
    {
        [TestMethod]
        public void TestPlayerDetailsCurrentWorks()
        {
            var pd = PlayerUI.Statistics.GhostVRConnector.PlayerDetails.Current;
            Assert.AreEqual(pd.name, "BIVROST 360Player");

        }


        [TestMethod]
        public void TestPlayerDetailsQsWorks()
        {
            var pd = new PlayerUI.Statistics.GhostVRConnector.PlayerDetails()
            {
                //licenseType = Statistics.GhostVRConnector.PlayerDetails.LicenseType.debug,
                version = "0.0.0.0"
            };
            Assert.AreEqual(pd.AsQsFormat, "player%5Bname%5D=BIVROST%20360Player&player%5Bversion%5D=0.0.0.0");
        }
        

    }
}
