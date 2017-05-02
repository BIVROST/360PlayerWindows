using System;
using System.Collections.Generic;
using ChanibaL;
using RestSharp;
using Bivrost.Log;
using Newtonsoft.Json;

namespace PlayerUI.Statistics
{
    
    public class GhostVRConnector
    {

        StateMachine sm;

        public enum Status { pending, connected, disconnected };

        protected Status status { get; set; } = Status.disconnected;
        protected Guid? Token { get; set; } = null;
        protected string Name { get; set; } = null;

#if DEBUG || CANARY
		public string DevelopmentToken { get { return Logic.Instance.settings.GhostVRLicenseToken; } }
#endif


		public void Disconnect() { disconnectTrigger?.Invoke(); }
        public void Connect() { connectTrigger?.Invoke(); }
        public void Cancel() { cancelTrigger?.Invoke();  }

        protected Action disconnectTrigger;
        protected Action connectTrigger;
        protected Action cancelTrigger;


		private void Log(string v)
		{
			Logger.Info("[GhostVR] " + v);
		}


		private void Log(string tag, ErrorResponse errorResponse)
		{
			Log(tag + " error: " + errorResponse);
		}


		#region GhostVR API

		public class PlayerDetails
        {
			public enum LicenseType { development, pro, normal };
			public readonly string name = "BIVROST 360Player";
			public string version;
			public LicenseType licenseType;

			static public PlayerDetails Current {
                get {
					//**@license_type@:enum(development, pro, normal) - grupa licencji z której pochodzi player.
					LicenseType licenseType;
					if (Features.IsDebug || Features.IsCanary)
						licenseType = LicenseType.development;
					else if (Features.Commercial)
						licenseType = LicenseType.pro;
					else
						licenseType = LicenseType.normal;
					return new PlayerDetails()
                    {
                        version = Tools.PublishInfo.ApplicationIdentity?.Version?.ToString(),
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


		string GhostVREndpoint {  get { return "https://dev.ghostvr.io/api/v1/"; } }

		enum TokenStatus { ok, pending, rejected };

		enum ApiStatus { success, error }

		class ApiResponse
		{
			public int code;
			public ApiStatus status;
		}

		class ErrorResponse:ApiResponse
		{
			public string message;
		}


		class VerifyTokenResponse: ApiResponse
		{
			public string name;
			public TokenStatus verification_status;
		}


		void VerifyToken(Action<VerifyTokenResponse> onSuccess, Action onErrorOrPending, Action onRejection)
        {
			var client = new RestClient(GhostVREndpoint);
			var request = new RestRequest("verify_player_token ", Method.POST);
			request.AddParameter("access_token", Token, ParameterType.GetOrPost);
			client.ExecuteAsync(request, (response, req) => 
			{
				if ((int)response.StatusCode >= 400)
				{
					var errorResponse = SimpleJson.DeserializeObject<ErrorResponse>(response.Content);
					Log("VerifyToken", errorResponse);
					onErrorOrPending();
				}
				else
				{
					var successResponse = SimpleJson.DeserializeObject<VerifyTokenResponse>(response.Content);
					switch(successResponse.verification_status)
					{
						case TokenStatus.ok:
							Log("VerifyToken OK");
							onSuccess(successResponse);
							break;
						case TokenStatus.pending:
							Log("VerifyToken pending");
							onErrorOrPending();
							break;
						case TokenStatus.rejected:
							Log("VerifyToken rejected");
							onRejection();
							break;
					}
				}
			});
        }


		void DiscardToken()
		{
			var client = new RestClient(GhostVREndpoint);
			var request = new RestRequest("discard_player_token", Method.POST);
			request.AddParameter("access_token", Token, ParameterType.GetOrPost);
			client.ExecuteAsync(request, (response, req) => { Log("DiscardToken: " + response.StatusCode); });
		}


		void AuthorizePlayerInBrowser()
        {
			string uri = GhostVREndpoint + "authorize_player"
				+ "?access_token=" + Token.ToString()
				+ "&installation_id=" + Logic.Instance.settings.InstallId
				+ "&" + PlayerDetails.Current.AsQsFormat;
			System.Diagnostics.Process.Start(uri);
		}


		class VideoSessionResponse:ApiResponse
		{
			public string response;
			public string followUp;
		}

		public void VideoSession(Session session, string token, Action<string> onSuccess, Action<string> onFailure)
		{
			if (string.IsNullOrEmpty(token))
			{
				onFailure("No GhostVR token available");
				return;
			}

			//var client = new RestClient(GhostVREndpoint);
			//var request = new RestRequest("video_session", Method.POST);
			var client = new RestClient("https://api.ghostvr.io/v1/");      // FIXME
			var request = new RestRequest("session", Method.POST);

			request.AddHeader("Authorization", $"Bearer {token}");
			request.AddParameter("application/json; charset=UTF-8", session.ToJson(), ParameterType.RequestBody);
			client.ExecuteAsync(request, (response, req) =>
			{
				try
				{
					if ((int)response.StatusCode >= 400 || (int)response.StatusCode < 200)
					{
						var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
						Log("VideoSession", errorResponse);
						onFailure(errorResponse?.message);
					}
					else
					{
						var successResponse = JsonConvert.DeserializeObject<VideoSessionResponse>(response.Content);
						Log("VideoSession sent");
						var uri = new UriBuilder(successResponse.followUp);
						if (uri.Query == "?" || uri.Query == "")
							uri.Query = $"access_token={token}";
						else
							uri.Query += $"&access_token={token}";

						onSuccess(uri.ToString());
					}
				}
				catch(Exception e)
				{
					Logger.Error(e);
					onFailure("Server returned malformed data");
				}
			});
		}

		#endregion


		#region StateMachine


		public GhostVRConnector()
        {
            sm = new StateMachine(StateIdle);
        }


        void StateIdle() {
            if (status == Status.connected)
                sm.SwitchState(StateConnected);
            if (status == Status.disconnected)
                sm.SwitchState(StateDisconnected);
            if (status == Status.pending)
                sm.SwitchState(StatePending);
        }

        void StateDisconnected()
        {
            if(sm.EnterState)
            {
                Token = null;
                Name = null;
                status = Status.disconnected;
                connectTrigger = sm.ValidOnlyInThisState(() => sm.SwitchStateExternalImmidiate(StateConnecting));
            }

            if (sm.ExitState)
                connectTrigger = null;
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
                status = Status.pending;
				VerifyToken(
					sm.ValidOnlyInThisState<VerifyTokenResponse>(vtr => sm.SwitchState(StateVerified(vtr))),
					sm.StateSwitcherValidOnlyInThisState(StatePendingWait),
					sm.StateSwitcherValidOnlyInThisState(StateConnectingFailed)
                );
				cancelTrigger = sm.StateSwitcherValidOnlyInThisState(StateCancelPending);
			}

			if(sm.ExitState)
			{
				cancelTrigger = null;
			}
        }


		void StatePendingWait()
		{
			if(sm.EnterState)
			{
				cancelTrigger = sm.StateSwitcherValidOnlyInThisState(StateCancelPending);
			}

			if (sm.TimeInState > 20)
				sm.SwitchState(StatePending);

			if(sm.ExitState)
			{
				cancelTrigger = null;
			}
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
				DiscardToken();
				sm.SwitchState(StateDisconnected);
			}
		}


		StateMachine.State StateVerified(VerifyTokenResponse verifyTokenResponse)
		{
			return () =>
			{
				if (sm.EnterState)
				{
					Name = verifyTokenResponse.name;
					status = Status.connected;
					sm.SwitchState(StateConnected);
				}
			};
		}


		void StateConnected()
		{
			if(sm.EnterState)
			{
				disconnectTrigger = sm.StateSwitcherValidOnlyInThisState(StateDisconnect);
			}

			if (sm.TimeInState > 600)
				sm.SwitchState(StateVerify);

			if(sm.ExitState)
			{
				disconnectTrigger = null;
			}
		}


		void StateVerify()
		{
			if(sm.EnterState)
			{
				VerifyToken(
					sm.ValidOnlyInThisState<VerifyTokenResponse>(verifyApiResponse => {
						sm.SwitchState(StateVerified(verifyApiResponse));
					}),
					sm.StateSwitcherValidOnlyInThisState(StateVerificationFailed),
					sm.StateSwitcherValidOnlyInThisState(StateDisconnect)
				);
				disconnectTrigger = sm.StateSwitcherValidOnlyInThisState(StateDisconnect);
			}

			if (sm.ExitState)
				disconnectTrigger = null;
		}


		void StateVerificationFailed()
		{
			if(sm.EnterState)
			{
				Logic.Notify("GhostVR verification failed.");
				sm.SwitchState(StateConnected);
			}
		}


		void StateDisconnect()
		{
			if(sm.EnterState)
			{
				Logic.Notify("You have been disconnected from GhostVR.");
				DiscardToken();
				sm.SwitchState(StateDisconnected);
			}
		}

		#endregion
	}
}
