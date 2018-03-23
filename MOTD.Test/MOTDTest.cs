using System;
using Bivrost.MOTD;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MOTD.Test
{
	[TestClass]
	public class MOTDTest
	{
		private class TestBridge : IMOTDBridge
		{
			public string InstallId => "test-installid";

			public string Version => "1.2.3";

			public string Product => "test-product";

			public void DisplayNotification(string text)
			{
				throw new NotImplementedException();
			}

			public void DisplayNotification(string text, string link, string url)
			{
				throw new NotImplementedException();
			}

			public void DisplayPopup(string title, string url, int width = 600, int height = 400)
			{
				throw new NotImplementedException();
			}
		}
		MOTDClient client = new MOTDClient("http://example.com", new TestBridge());


		[TestMethod]
		public void TestNoneDeserialization()
		{
			var r = client.ParseResponse(
				@"{
					""motd-server-version"": ""1.0"",
					""type"": ""none""
				}"
			);
			Assert.IsInstanceOfType(r, typeof(MOTDClient.ApiResponseNone));
			Assert.AreEqual(r.motdServerVersion, "1.0");
		}


		[TestMethod]
		public void TestNotificationWithLinkDeserialization()
		{
			var r = client.ParseResponse(
				@"{
					""motd-server-version"": ""1.0"",
					""type"": ""notification"",
					""text"": ""the text of the notification"",
					""link"": ""optional: title of the link at the end of the notification"",
					""uri"": ""http://example.com/the/link/for/url#optional""
				}"
			);
			Assert.IsInstanceOfType(r, typeof(MOTDClient.ApiResponseNotification));
			Assert.AreEqual(r.motdServerVersion, "1.0");
			var notification = (MOTDClient.ApiResponseNotification)r;
			Assert.AreEqual("the text of the notification", notification.text);
			Assert.AreEqual("optional: title of the link at the end of the notification", notification.link);
			Assert.AreEqual("http://example.com/the/link/for/url#optional", notification.uri);
			Assert.IsTrue(notification.HasLink);
		}


		[TestMethod]
		public void TestNotificationWithoutLinkDeserialization()
		{
			foreach (var json in new[] {
				@"{
					""motd-server-version"": ""1.0"",
					""type"": ""notification"",
					""text"": ""the text of the notification"",
				}",
				@"{
					""motd-server-version"": ""1.0"",
					""type"": ""notification"",
					""text"": ""the text of the notification"",
					""uri"": ""http://example.com/the/link/for/url#optional""
				}",
				@"{
					""motd-server-version"": ""1.0"",
					""type"": ""notification"",
					""text"": ""the text of the notification"",
					""link"": ""optional: title of the link at the end of the notification"",
				}"
			})
			{

				var r = client.ParseResponse(json);
				Assert.IsInstanceOfType(r, typeof(MOTDClient.ApiResponseNotification));
				Assert.AreEqual(r.motdServerVersion, "1.0");
				var notification = (MOTDClient.ApiResponseNotification)r;
				Assert.AreEqual("the text of the notification", notification.text);
				Assert.IsFalse(notification.HasLink);
			}
		}


		[TestMethod]
		public void TestPopupDeserialization()
		{
			var r = client.ParseResponse(
				@"{ 
					""motd-server-version"": ""1.0"",
					""type"": ""popup"",
					""title"": ""the title of the popup"",
					""url"": ""http://tools.bivrost360.com/motd-server/v1/some-message.html"",
					""width"": 1337,
					""height"": 666
				}"
			);
			Assert.IsInstanceOfType(r, typeof(MOTDClient.ApiResponsePopup));
			var popup = (MOTDClient.ApiResponsePopup)r;
			Assert.AreEqual(popup.motdServerVersion, "1.0");
			Assert.AreEqual(popup.title, "the title of the popup");
			Assert.AreEqual(popup.url, "http://tools.bivrost360.com/motd-server/v1/some-message.html");
			Assert.AreEqual(popup.width, 1337);
			Assert.AreEqual(popup.height, 666);
		}


		[TestMethod]
		public void TestPopupDeserializationWithDefaults()
		{
			var r = client.ParseResponse(
				@"{ 
					""motd-server-version"": ""1.0"",
					""type"": ""popup"",
					""title"": ""the title of the popup"",
					""url"": ""http://tools.bivrost360.com/motd-server/v1/some-message.html""
				}"
			);
			Assert.IsInstanceOfType(r, typeof(MOTDClient.ApiResponsePopup));
			var popup = (MOTDClient.ApiResponsePopup)r;
			Assert.AreEqual(popup.width, 600);
			Assert.AreEqual(popup.height, 400);
		}

	}
}
