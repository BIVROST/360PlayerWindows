using Newtonsoft.Json;
using System;

namespace PlayerUI.Statistics
{
    public struct Session
    {
        public string version;
        public Guid guid;
        public string uri;
        public int sample_rate;
        public Guid installation_id;
        public DateTime time_start;
        public DateTime time_end;
        public string lookprovider;
        public string history;
        public string media_id;

        readonly static string CURRENT_VERSION = "0.20170321";

        public Session(string filename, DateTime start, DateTime end, LookHistory history, ILookProvider lookProvider, Streaming.ServiceResult serviceResult)
        {
            version = CURRENT_VERSION;
            guid = Guid.NewGuid();
            uri = filename;
            sample_rate = history.SampleRate;
            installation_id = Logic.Instance.settings.InstallId;
            time_start = start;
            time_end = end;
            lookprovider = lookProvider?.DescribeType;
            this.history = history.ToBase64();
            this.media_id = serviceResult.mediaId;
        }


        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
