using static Bivrost.Log.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bivrost.Log;
using Newtonsoft.Json;

namespace PlayerUI.Statistics
{
    public class LookListener: IDisposable
    {

        double _lastMediaTime = 0;
        Stopwatch _mediaTimeDelta = new Stopwatch();
        double MediaTime {
            get
            {
                return _lastMediaTime + _mediaTimeDelta.Elapsed.TotalSeconds;
            }
            set
            {
                _lastMediaTime = value;
                _mediaTimeDelta.Restart();
            }
        }

        ILookProvider lookProvider = null;
        LookHistory history = null;
        private string filename;
        private DateTime startTime;

        public LookListener()
        {
            MediaDecoder.OnInstantiated += MediaDecoder_OnInstantiated;
            ShellViewModel.OnInstantiated += ShellViewModel_OnInstantiated;

            listeners.Clear();
            listeners.Add(new TraceLogMsgOnlyListener());
        }

        private void ShellViewModel_OnInstantiated(ShellViewModel shellViewModel)
        {
            Info("history: ShellViewModel instantiated");
            ShellViewModel.OnInstantiated -= ShellViewModel_OnInstantiated;
            shellViewModel.HeadsetEnable += HandleHeadsetEnable;
            shellViewModel.HeadsetDisable += HandleHeadsetDisable;
        }

        private void MediaDecoder_OnInstantiated(MediaDecoder instance)
        {
            Info("history: media decoder instantiated");
            MediaDecoder.OnInstantiated -= MediaDecoder_OnInstantiated;
            instance.OnTimeUpdate += HandleTimeUpdate;
            instance.OnPlay += HandlePlay;
            instance.OnStop += HandleStop;

        }

        private void HandleStop()
        {
            // session end
            Info("ended history session " + filename);
            if (history == null)
                return;
            Info("http://tools.bivrost360.com/heatmap-viewer/?" + history.ToBase64());
            SaveSession(Guid.NewGuid(), filename, startTime, DateTime.Now, history, lookProvider);
            history = null;
        }

        private void SaveSession(Guid id, string filename, DateTime start, DateTime end, LookHistory history, ILookProvider lookProvider)
        {
            System.IO.File.WriteAllText(
                $"{Logic.LocalDataDirectory}/session-{start.ToString("yyyy-MM-ddTHHmmss")}.log", 
                JsonConvert.SerializeObject(new
                {
                    guid = id,
                    uri = filename,
                    sample_rate = history.SampleRate,
                    installation_id = Logic.Instance.settings.InstallId,
                    time_start = start.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    time_end = end.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    lookprovider = lookProvider.DescribeType,
                    history = history.ToBase64()
                })
            );
        }

        private void HandlePlay()
        {
            history = new LookHistory(10, MediaDecoder.Instance.Duration);
            Info("new history session: " + MediaDecoder.Instance.FileName);
            filename = MediaDecoder.Instance.FileName;
            startTime = DateTime.Now;
        }

        private void HandleHeadsetDisable(ILookProvider headset)
        {
            headset.ProvideLook -= HandleProvideLook;
            lookProvider = null;
        }

        private void HandleHeadsetEnable(ILookProvider headset)
        {
            headset.ProvideLook += HandleProvideLook;
            lookProvider = headset;
        }

        //Stopwatch hpls = new Stopwatch();
        private void HandleProvideLook(SharpDX.Vector3 position, SharpDX.Quaternion rotation, float fov)
        {
            history?.TrackData((float)MediaTime, rotation, (byte)fov);
            //Info($"{hpls.ElapsedMilliseconds:0000}   {_lastMediaTime:000.0000} + {_mediaTimeDelta.Elapsed.TotalSeconds:000.0000} = {_lastMediaTime + _mediaTimeDelta.Elapsed.TotalSeconds:000.0000}");
            //hpls.Restart();
        }

        public void Dispose()
        {
            MediaDecoder.OnInstantiated -= MediaDecoder_OnInstantiated;
            if (MediaDecoder.Instance != null)
            {
                MediaDecoder.Instance.OnTimeUpdate -= HandleTimeUpdate;
                MediaDecoder.Instance.OnPlay -= HandlePlay;
                MediaDecoder.Instance.OnStop -= HandleStop;
            }
            ShellViewModel.OnInstantiated -= ShellViewModel_OnInstantiated;
            if (ShellViewModel.Instance != null)
            {
                ShellViewModel.Instance.HeadsetEnable -= HandleHeadsetEnable;
                ShellViewModel.Instance.HeadsetDisable -= HandleHeadsetDisable;
            }
            if(lookProvider != null)
                lookProvider.ProvideLook -= HandleProvideLook;
        }

        private void HandleTimeUpdate(double currentTime)
        {
            MediaTime = currentTime;
        }
    }

}
