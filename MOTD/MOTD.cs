using Bivrost.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
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
			[JsonProperty("motd-server-version")]
			public string motdServerVersion;

			public enum Type { error, none, notification, popup };

			[JsonProperty("type")]
			public Type type;
		}


		public class ApiResponseNone : ApiResponse { }


		public class ApiResponseNotification : ApiResponse
		{
			[JsonProperty("text")]
			public string text;

			[JsonProperty("link")]
			public string link;

			[JsonProperty("uri")]
			public string uri;

			public bool HasLink => !(string.IsNullOrEmpty(link) || string.IsNullOrEmpty(uri));

		}


		public class ApiResponsePopup : ApiResponse
		{
			[JsonProperty("title")]
			public string title;

			[JsonProperty("url")]
			public string url;

			[JsonProperty("width")]
			public int width = 600;

			[JsonProperty("height")]
			public int height = 400;
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


		void ErrorOccured(string e)
		{
			logger.Error(e);
			;
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
			logger.Info($"Requesting for {app.Product} v.{app.Version} id={app.InstallId}");

			var client = new RestClient(serverUri);
			var request = new RestRequest(Method.POST);

			client.ExecuteAsync(request, (response, request_) =>
			{
				if (response.ErrorException != null)
				{
					ErrorOccured(response.ErrorException.ToString());
					return;
				}

				string json = response.Content;
				var responseObject = ParseResponse(json);

				Execute(responseObject);
			});
		}


		void Execute(ApiResponse r)
		{
			throw new NotSupportedException("No overload found for response of type "+r.GetType());
		}


		void Execute(ApiResponseNone r)
		{
			logger.Info("Got nothing interesting from the server");
		}


		void Execute(ApiResponseNotification r)
		{
			logger.Info($"Got a notification the server, hasLink={r.HasLink}");

			if (r.HasLink)
				app.DisplayNotification(r.text, r.link, r.uri);
			else
				app.DisplayNotification(r.text);
		}


		void Execute(ApiResponsePopup r)
		{
			logger.Info("Got a popup from the server");
			app.DisplayPopup(r.title, r.url, r.width, r.height);
		}

		#endregion


	}
}
