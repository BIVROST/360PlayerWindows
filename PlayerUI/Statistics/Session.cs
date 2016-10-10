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

        readonly static string CURRENT_VERSION = "0.20160905";

        public Session(Guid id, string filename, DateTime start, DateTime end, LookHistory history, ILookProvider lookProvider)
        {
            version = CURRENT_VERSION;
            guid = id;
            uri = filename;
            sample_rate = history.SampleRate;
            installation_id = Logic.Instance.settings.InstallId;
            time_start = start;
            time_end = end;
            lookprovider = lookProvider?.DescribeType;
            this.history = history.ToBase64();
        }


        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
