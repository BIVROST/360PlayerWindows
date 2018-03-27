using Bivrost.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace Bivrost.MOTD
{

	public interface IMOTDBridge
	{
		string InstallId { get; }
		string Version { get; }
		string Product { get; }

		void DisplayNotification(string text);
		void DisplayNotification(string text, string link, string url);

		/// <summary>
		/// You can use the generic one:
		/// var popup = new MOTDPopup(title, url, width, height);
		/// popup.Show();
		/// </summary>
		/// <param name="title">Title of popup</param>
		/// <param name="url">Url to load</param>
		/// <param name="width">Width in pixels, with decoration</param>
		/// <param name="height">Height in pixels, with decoration</param>
		void DisplayPopup(string title, string url, int width = 600, int height = 400);
	}



	public class MOTDClient
	{

		// TODO: register callbacks
		// TODO: html popup
		// TODO: register notification from system

		internal static Logger logger = new Logger("MOTD");
		private readonly string serverUri;
		private readonly IMOTDBridge app;

		public MOTDClient(string serverUri, IMOTDBridge app)
		{
			this.serverUri = serverUri;
			this.app = app;
		}

		#region HTTP API


		public abstract class ApiResponse
		{
			[JsonProperty("motd-server-version", Required = Required.Always)]
			public string motdServerVersion;

			public enum Type { error, none, notification, popup };

			[JsonProperty("type", Required = Required.Always)]
			public Type type;

			internal abstract void Execute(IMOTDBridge app);
		}


		public class ApiResponseNone : ApiResponse
		{
			internal override void Execute(IMOTDBridge app)
			{
				logger.Info("The server has nothing interesting to say.");
			}
		}

		public class ApiResponseError : ApiResponse
		{
			[JsonProperty("message", Required = Required.Always)]
			public string text;

			internal override void Execute(IMOTDBridge app)
			{
				logger.Error("The server has returned an error: " + text);
			}
		}


		public class ApiResponseNotification : ApiResponse
		{
			[JsonProperty("text", Required = Required.Always)]
			public string text;

			[JsonProperty("link")]
			public string link;

			[JsonProperty("uri")]
			public string uri;

			public bool HasLink => !(string.IsNullOrEmpty(link) || string.IsNullOrEmpty(uri));

			internal override void Execute(IMOTDBridge app)
			{
				logger.Info($"Got a notification the server, hasLink={HasLink}");

				if (HasLink)
					app.DisplayNotification(text, link, uri);
				else
					app.DisplayNotification(text);
			}
		}


		public class ApiResponsePopup : ApiResponse
		{
			[JsonProperty("title", Required = Required.Always)]
			public string title;

			[JsonProperty("url", Required = Required.Always)]
			public string url;

			[JsonProperty("width", Required = Required.Default)]
			public int width = 600;

			[JsonProperty("height", Required = Required.Default)]
			public int height = 400;

			internal override void Execute(IMOTDBridge app)
			{
				logger.Info("Got a popup from the server");
				app.DisplayPopup(title, url, width, height);
			}
		}


		public class ApiResponseConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return (objectType == typeof(ApiResponse));
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				JObject jo = JObject.Load(reader);
				switch (jo["type"].Value<string>())
				{
					case nameof(ApiResponse.Type.none):
						return jo.ToObject<ApiResponseNone>(serializer);
					case nameof(ApiResponse.Type.notification):
						return jo.ToObject<ApiResponseNotification>(serializer);
					case nameof(ApiResponse.Type.popup):
						return jo.ToObject<ApiResponsePopup>(serializer);
					case nameof(ApiResponse.Type.error):
						return jo.ToObject<ApiResponseError>(serializer);
					default:
						throw new Exception("Could not deserialize");
				}
			}

			public override bool CanWrite
			{
				get { return false; }
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}
		}


		public ApiResponse ParseResponse(string json)
		{
			ApiResponse message = JsonConvert.DeserializeObject<ApiResponse>(
				json,
				new JsonSerializerSettings() { Converters = { new ApiResponseConverter() } }
			);
			return message;
		}


		public void RequestMOTD()
		{
			logger.Info($"Requesting MOTD for {app.Product} version={app.Version??"(development)"} id={app.InstallId}");

			var client = new RestClient(serverUri + "motd");
			var request = new RestRequest(Method.POST);
			request.AddParameter("install-id", app.InstallId, ParameterType.GetOrPost);
			request.AddParameter("product", app.Product, ParameterType.GetOrPost);
			request.AddParameter("version", app.Version, ParameterType.GetOrPost);

			client.ExecuteAsync(request, (response, request_) =>
			{
				if (response.ErrorException != null)
				{
					logger.Error(response.ErrorException.Message);
					return;
				}

				string json = response.Content;
				var responseObject = ParseResponse(json);

				responseObject.Execute(app);
			});
		}


		internal void RequestUpgradeNotice(string prevVersion)
		{
			string currentVersion = app.Version;

			logger.Info($"Requesting update notice for {app.Product} version={prevVersion ?? "(development)"} -> {currentVersion ?? "(development)"} id={app.InstallId}");

			var client = new RestClient(serverUri + "upgrade");
			var request = new RestRequest(Method.POST);
			request.AddParameter("install-id", app.InstallId, ParameterType.GetOrPost);
			request.AddParameter("product", app.Product, ParameterType.GetOrPost);
			request.AddParameter("version-previous", prevVersion, ParameterType.GetOrPost);
			request.AddParameter("version-current", currentVersion, ParameterType.GetOrPost);

			client.ExecuteAsync(request, (response, request_) =>
			{
				if (response.ErrorException != null)
				{
					logger.Error(response.ErrorException.Message);
					return;
				}

				string json = response.Content;
				var responseObject = ParseResponse(json);

				responseObject.Execute(app);
			});
		}


		#endregion


	}
}
