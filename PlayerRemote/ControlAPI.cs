using RestSharp;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Globalization;

namespace PlayerRemote
{
	public class ControlAPI
	{

		private RestClient client;
		private Action<RestRequest, IRestResponse> apiDebugger;


		/// <summary>
		/// Creates an object for accessing the Control API of the BIVROST 360Player
		/// </summary>
		/// <param name="endpointUrl">The endpoint base for the urls, ex. http://example.com:8080/v1/ with trailing slash.</param>
		public ControlAPI(string endpointUrl, Action<RestRequest, IRestResponse, RestClient> apiDebugger)
		{
			client = new RestClient(endpointUrl);
			this.apiDebugger = (rq, rp) =>
			{
				if (apiDebugger == null) return;
				apiDebugger(rq, rp, client);
			};
		}

		#region common api
		/// <summary>
		/// Returns the version of the API 
		/// </summary>
		/// <returns>"api v1"</returns>
		public async Task<string> Version()
		{
			RestRequest request = new RestRequest("", Method.GET);
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return response.Content;
		}


		/// <summary>
		/// Sends a debug message (printed in logs)
		/// </summary>
		/// <param name="message"></param>
		/// <returns>Status</returns>
		public async Task<string> Info(string message)
		{
			RestRequest request = new RestRequest("info", Method.POST);
			request.AddParameter("message", message);
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return response.Content;
		}
		#endregion



		#region control API
		/// <summary>
		/// GET /v1/movies
		/// Lists paths to all movies available on the main app in the media direcotry.
		/// </summary>
		/// <returns>Paths to movies on players drive</returns>
		public async Task<string[]> Movies()
		{
			RestRequest request = new RestRequest("movies", Method.GET);
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return JsonConvert.DeserializeObject<string[]>(response.Content);
		}


		/// <summary>
		/// GET /v1/load
		/// Loads the movie by path on the main app.
		/// </summary>
		/// <param name="movie">path to the movie in the same form as in Movies()</param>
		/// <param name="autoplay">the movie is played automaticaly if autoplay is set to true</param>
		/// <returns>Returns boolean true if succeeded</returns>
		public async Task<bool> Load(string movie, bool autoplay)
		{
			RestRequest request = new RestRequest("load", Method.GET);
			request.AddQueryParameter("movie", movie);
			request.AddQueryParameter("autoplay", autoplay.ToString());
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return JsonConvert.DeserializeObject<bool>(response.Content);
		}



		/// <summary>
		/// Seeks the movie to the supplied.
		/// </summary>
		/// <param name="t">time in seconds</param>
		/// <returns> true if seek succeeded</returns>
		public async Task<bool> Seek(float t)
		{
			RestRequest request = new RestRequest("seek", Method.GET);
			request.AddQueryParameter("t", t.ToString(CultureInfo.InvariantCulture));
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return JsonConvert.DeserializeObject<bool>(response.Content);
		}


		/// <summary>
		/// Stops playback, rewinds the movie and recenters the viewport
		/// </summary>
		/// <returns>true if movie is loaded</returns>
		public async Task<bool> StopAndReset()
		{
			RestRequest request = new RestRequest("stop-and-reset", Method.GET);
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return JsonConvert.DeserializeObject<bool>(response.Content);
		}


		/// <summary>
		/// Pauses the movie
		/// </summary>
		/// <returns>true if movie is loaded</returns>
		public async Task<bool> Pause()
		{
			RestRequest request = new RestRequest("pause", Method.GET);
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return JsonConvert.DeserializeObject<bool>(response.Content);
		}


		/// <summary>
		/// Unpauses the movie
		/// </summary>
		/// <returns>true if movie is loaded</returns>
		public async Task<bool> Unpause()
		{
			RestRequest request = new RestRequest("unpause", Method.GET);
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return JsonConvert.DeserializeObject<bool>(response.Content);
		}


		/// <summary>
		/// Returns information on playback status
		/// </summary>
		/// <returns>PlayingInfo object</returns>
		public async Task<PlayingInfo> Playing()
		{
			RestRequest request = new RestRequest("playing", Method.GET);
			IRestResponse response = await client.ExecuteTaskAsync(request);
			apiDebugger(request, response);
			return JsonConvert.DeserializeObject<PlayingInfo>(response.Content);
		}
		#endregion

	}




	public struct PlayingInfo
	{

		/// <summary>
		/// The path to the current movie.
		/// </summary>
		public string movie;

		/// <summary>
		/// The current camera orientation quaternion
		/// </summary>
		public float quat_x;

		/// <summary>
		/// The current camera orientation quaternion
		/// </summary>
		public float quat_y;

		/// <summary>
		/// The current camera orientation quaternion
		/// </summary>
		public float quat_z;

		/// <summary>
		/// The current camera orientation quaternion
		/// </summary>
		public float quat_w;


		/// <summary>
		/// Current playback time
		/// </summary>
		public float t;


		/// <summary>
		/// Movie length
		/// </summary>
		public float tmax;


		/// <summary>
		/// Is the movie is currently playing
		/// </summary>
		public bool is_playing;

	}
}
