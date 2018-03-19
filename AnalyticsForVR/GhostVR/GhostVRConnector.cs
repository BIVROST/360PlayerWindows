using System;
using System.Collections.Generic;
using ChanibaL;
using RestSharp;
using Newtonsoft.Json;
using System.Threading;
using Bivrost.Bivrost360Player;
using Bivrost.Bivrost360Player.Tools;

namespace Bivrost.AnalyticsForVR
{
    
    public class GhostVRConnector
    {

        StateMachine sm;
		private Thread periodicUpdater;

		public enum ConnectionStatus { pending, connected, disconnected };

		ConnectionStatus _status = ConnectionStatus.disconnected;
        public ConnectionStatus status
		{
			get { return _status; }
			set
			{
				if (value == _status)
					return;
				_status = value;
				StatusChanged?.Invoke(status);
				logger.Info($"Connection status changed to {value}");
			}
		}

		public event Action<ConnectionStatus> StatusChanged;

        protected Guid? Token
		{
			get
			{
				string strtoken = Logic.Instance.settings.GhostVRLicenseToken;
				if (string.IsNullOrEmpty(strtoken))
					return null;

				Guid token;
				if (Guid.TryParse(strtoken, out token))
					return token;

				logger.Error("[GhostVR] cannot parse token from settings, removed");
				return null;
			}
			set
			{
				if (value.HasValue)
					Logic.Instance.settings.GhostVRLicenseToken = value.Value.ToString();
				else
					Logic.Instance.settings.GhostVRLicenseToken = null;
				Logic.Instance.settings.Save();
			}
		}

        public string Name { get; private set; } = null;

		public void Disconnect() { disconnectTrigger?.Invoke(); }
        public void Connect() { connectTrigger?.Invoke(); }
        public void Cancel() { cancelTrigger?.Invoke();  }

        protected Action disconnectTrigger;
        protected Action connectTrigger;
        protected Action cancelTrigger;

		public bool IsConnected { get { return status == ConnectionStatus.connected; } }

		internal Bivrost.Log.Logger logger = new Bivrost.Log.Logger("GhostVR");



		#region GhostVR API

		public class PlayerDetails
        {
			public enum LicenseType { development, pro, normal };
			public readonly string name = "BIVROST 360Player";
			public string version;
			public LicenseType licenseType;

			static public PlayerDetails Current {
                get {
					#pragma warning disable 0162 // code dependant on typedefs
					//**@license_type@:enum(development, pro, normal) - grupa licencji z której pochodzi player.
					LicenseType licenseType;
					if (Features.IsDebug || Features.IsCanary)
						licenseType = LicenseType.development;
					else if (Features.Commercial)
						licenseType = LicenseType.pro;
					else
						licenseType = LicenseType.normal;
					#pragma warning restore
					return new PlayerDetails()
                    {
                        version = PublishInfo.ApplicationIdentity?.Version?.ToString(),
						licenseType = licenseType
					};
                }
            }

            public string AsQsFormat
            {
                get
                {
                    // https://www.npmjs.com/package/qs
                    return string.Join("&",
                        Uri.EscapeDataString("player[name]") + "=" + Uri.EscapeDataString(name),
                        Uri.EscapeDataString("player[version]") + "=" + Uri.EscapeDataString(version ?? ""),
						Uri.EscapeDataString("player[license_type]") + "=" + Uri.EscapeDataString(licenseType.ToString())
					);


                }
            }
        }


		string GhostVREndpoint {
			get
			{
#if DEBUG || CANARY
				if (!string.IsNullOrWhiteSpace(Logic.Instance.settings.GhostVREndpointOverride))
					return Logic.Instance.settings.GhostVREndpointOverride;
#endif
				return "https://api.ghostvr.io/api/v1/";
			}
		}

		public bool potentialAuthError;

		void ApiRequest<T>(string endpoint, Action<RestRequest> arguments, Action<T> onSuccess, Action<string> onError)
		{
			var client = new RestClient(GhostVREndpoint);
			var request = new RestRequest(endpoint, Method.POST);
			if(Token.HasValue)
				request.AddHeader("Authorization", "Bearer " + Token.Value);
			arguments(request);
			logger.Info($"API request: {endpoint} {request} token={(Token.HasValue ? Token.Value.ToString() : "(none)")}");

			client.ExecuteAsync(request, (response, request_) =>
			{
				try
				{
					if (response.ErrorException != null)
					{
						logger.Error("Cannot connect to GhostVR API: " + response.ErrorMessage);
						onError("Cannot connect to GhostVR API: " + response.ErrorMessage);
						potentialAuthError = true;
						return;
					}

					ApiResponse apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response.Content);
					if (apiResponse.status == ApiResponse.ApiStatus.success)
					{
						ApiResponseSuccess<T> apiResponseSuccess = JsonConvert.DeserializeObject<ApiResponseSuccess<T>>(response.Content);
						logger.Info("ApiRequest success: " + endpoint);
						onSuccess(apiResponseSuccess.data);
					}
					else
					{
						ApiResponseError apiResponseError = JsonConvert.DeserializeObject<ApiResponseError>(response.Content);
						logger.Error($"ApiRequest error: {endpoint}, {apiResponseError.status} {apiResponseError.message} ({apiResponseError.code})");
						onError($"{apiResponseError.status} {apiResponseError.message} ({apiResponseError.code})");
						potentialAuthError = true;
					}
				}
				catch (Exception e)
				{
					logger.Error("ApiRequest parse error:" + endpoint + e);
					onError("an exception occurred: " + e);
					potentialAuthError = true;
				}
			});
		}

		enum TokenStatus { pending, ok, rejected };


		// this are JSON serialized classes with a structure passed on from the server
		#pragma warning disable 0649
		class ApiResponse
		{
			public enum ApiStatus { success, error, fail }
			public ApiStatus status;
		}


		class ApiResponseError:ApiResponse
		{
			public string message;
			public string code;
			public string data;
		}


		class ApiResponseSuccess<T>:ApiResponse
		{
			public T data;
		}
		#pragma warning restore


		void AuthorizePlayerInBrowser()
		{
			string uri = GhostVREndpoint + "authorize_player"
				+ "?access_token=" + Token.ToString()
				+ "&installation_id=" + Logic.Instance.settings.InstallId
				+ "&" + PlayerDetails.Current.AsQsFormat;
			logger.Info($"Opening URI in browser: {uri}...");
			System.Diagnostics.Process.Start(uri);
		}


		// this is a JSON serialized class with a structure passed on from the server
		#pragma warning disable 0649
		class VerifyTokenResponse : ApiResponse
		{
			public string team_name;
			public TokenStatus verification_status;
		}
		#pragma warning restore


		void VerifyPlayerToken(Action<VerifyTokenResponse> onSuccess, Action onPending, Action onRejection, Action<string> onError)
		{
			if(!Token.HasValue)
			{
				onError("no token to verify");
				return;
			}

			ApiRequest<VerifyTokenResponse>(
				"verify_player_token",
				r => r.AddParameter("access_token", Token.Value, ParameterType.GetOrPost),
				response => {
					switch (response.verification_status)
					{
						case TokenStatus.ok:
							logger.Info("VerifyToken OK");
							onSuccess(response);
							break;
						case TokenStatus.pending:
							logger.Info("VerifyToken pending");
							onPending();
							break;
						case TokenStatus.rejected:
							logger.Info("VerifyToken rejected");
							onRejection();
							break;
					}
				},
				error => {
					onError(error);
				}
			);
		}


		void DiscardPlayerToken()
		{
			var client = new RestClient(GhostVREndpoint);
			var request = new RestRequest("discard_player_token", Method.POST);
			request.AddParameter("access_token", Token, ParameterType.GetOrPost);
			client.ExecuteAsync(request, (response, req) => { logger.Info("DiscardToken: " + response.StatusCode); });
		}



		// this is a JSON serialized class with a structure passed on from the server
		#pragma warning disable 0649
		class VideoSessionResponse : ApiResponse
		{
			public string follow_up;
		}
		#pragma warning restore

		public void VideoSession(Session session, Action<string> onSuccess, Action<string> onFailure)
		{
			ApiRequest<VideoSessionResponse>(
				"video_session",
				r => r.AddParameter("application/json; charset=UTF-8", session.ToJson(), ParameterType.RequestBody),
				success => 
				{
					logger.Info("VideoSession sent");
					var uri = new UriBuilder(success.follow_up);
					//if (uri.Query == "?" || uri.Query == "")
					//	uri.Query = $"access_token={token}";
					//else
					//	uri.Query += $"&access_token={token}";
					onSuccess(uri.ToString());
				},
				error => onFailure(error)
			);
		}

		#endregion


		#region StateMachine


		public GhostVRConnector()
        {
			sm = new StateMachine(StateInit, (msg, warn) => { if (warn) logger.Error(msg); else logger.Info(msg); });
			sm.Update(0);

			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			periodicUpdater = new Thread(() =>
			{
				while (true)
				{
					if (!sm.CurrentlyExecuting)
						lock (sm.CurrentlyExecutingSyncRoot)
						{
							if (sm.CurrentlyExecuting)
								continue;
							sm.Update((float)sw.Elapsed.TotalSeconds);
							sw.Restart();
						}
					Thread.Sleep(1000);
				}
			}) { Name = "GhostVR SM periodic updater", IsBackground = true };
			periodicUpdater.Start();
        }


        void StateInit() {
			if (Token.HasValue && Features.GhostVR) // has a stored token - will check
				sm.SwitchState(StateVerifyOldToken);

			if (!Token.HasValue && Features.GhostVR)
				sm.SwitchState(StateDisconnected);
        }


        void StateDisconnected()
        {
            if(sm.EnterState)
            {
                Token = null;
                Name = null;
                status = ConnectionStatus.disconnected;
                connectTrigger = sm.ValidOnlyInThisState(() => sm.SwitchStateExternalImmidiate(StateConnecting));
            }
        }


        void StateConnecting() {
            Token = Guid.NewGuid();
            AuthorizePlayerInBrowser();
            sm.SwitchState(StatePending);
        }


        void StatePending()
        {
            if (sm.EnterState)
            {
                status = ConnectionStatus.pending;
				VerifyPlayerToken(
					sm.ValidOnlyInThisState<VerifyTokenResponse>(vtr => sm.SwitchStateExternalImmidiate(StateVerified(vtr))),
					sm.StateSwitcherValidOnlyInThisState(StatePendingWait),
					sm.StateSwitcherValidOnlyInThisState(StateConnectingFailed),
					sm.ValidOnlyInThisState<string>(vtr => sm.SwitchStateExternalImmidiate(StatePendingWait))
				);
				cancelTrigger = sm.StateSwitcherValidOnlyInThisState(StateCancelPending);
			}
        }


		void StateVerifyOldToken()
		{
			if (sm.EnterState)
			{
				status = ConnectionStatus.pending;
				VerifyPlayerToken(
					sm.ValidOnlyInThisState<VerifyTokenResponse>(vtr => sm.SwitchStateExternalImmidiate(StateVerified(vtr))),
					sm.StateSwitcherValidOnlyInThisState(StateConnectingFailed),
					sm.StateSwitcherValidOnlyInThisState(StateConnectingFailed),
					sm.ValidOnlyInThisState<string>(vtr => sm.SwitchStateExternalImmidiate(StateConnectingFailed))
				);
				cancelTrigger = sm.StateSwitcherValidOnlyInThisState(StateCancelPending);
			}
		}


		void StatePendingWait()
		{
			if(sm.EnterState)
			{
				cancelTrigger = sm.StateSwitcherValidOnlyInThisState(StateCancelPending);
			}

			if (sm.TimeInState > 5)
				sm.SwitchState(StatePending);
		}


		void StateConnectingFailed()
		{
			if(sm.EnterState)
			{
				Logic.Notify("Connecting to GhostVR failed.");
				sm.SwitchState(StateDisconnected);
			}
		}


		void StateCancelPending()
		{
			if(sm.EnterState)
			{
				Logic.Notify("Connecting to GhostVR aborted.");
				DiscardPlayerToken();
				sm.SwitchState(StateDisconnected);
			}
		}


		StateMachine.State StateVerified(VerifyTokenResponse verifyTokenResponse)
		{
			return () =>
			{
				if (sm.EnterState)
				{
					Name = verifyTokenResponse.team_name;
					status = ConnectionStatus.connected;
					Logic.Notify($"Connected to GhostVR team {Name}");
					sm.SwitchState(StateConnected);
				}
			};
		}


		void StateConnected()
		{
			if(sm.EnterState)
			{
				potentialAuthError = false;
				disconnectTrigger = sm.StateSwitcherValidOnlyInThisState(StateDisconnect);
			}

			if (sm.TimeInState > 600 || potentialAuthError)
				sm.SwitchState(StateVerifyAgain);
		}


		void StateVerifyAgain()
		{
			if(sm.EnterState)
			{
				VerifyPlayerToken(
					sm.ValidOnlyInThisState<VerifyTokenResponse>(verifyApiResponse => sm.SwitchStateExternalImmidiate(StateConnected)),
					sm.StateSwitcherValidOnlyInThisState(StateDisconnect),
					sm.StateSwitcherValidOnlyInThisState(StateDisconnect),
					sm.ValidOnlyInThisState<string>(err => sm.SwitchStateExternalImmidiate(StateVerificationAgainFailed))
				);
				disconnectTrigger = sm.StateSwitcherValidOnlyInThisState(StateDisconnect);
			}
		}


		void StateVerificationAgainFailed()
		{
			if(sm.EnterState)
			{
				Logic.Notify("GhostVR verification failed.");
				cancelTrigger = sm.StateSwitcherValidOnlyInThisState(StateDisconnect);
			}
			if(sm.TimeInState > 10)
				sm.SwitchState(StateConnected);
		}


		void StateDisconnect()
		{
			if(sm.EnterState)
			{
				Logic.Notify("You have been disconnected from GhostVR.");
				DiscardPlayerToken();
				sm.SwitchState(StateDisconnected);
			}
		}

		#endregion
	}
}
