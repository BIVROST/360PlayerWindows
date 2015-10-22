using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyPlayer
{
    public enum PlayerState : int
    {
        PlayerState_Closed = 0,     // No session.
        PlayerState_Ready,          // Session was created, ready to open a file.
        PlayerState_OpenPending,    // Session is opening a file.
        PlayerState_Started,        // Session is playing a file.
        PlayerState_Paused,         // Session is paused.
        PlayerState_Stopped,        // Session is stopped (ready to play).
        PlayerState_Closing         // Application has closed the session, but is waiting for MESessionClosed.
    }

    public class MediaDecoderLegacy : IAsyncCallback
	{
        public static Guid MR_VIDEO_RENDER_SERVICE = new Guid(0x1092a86c, 0xab1a, 0x459a, 0xa3, 0x36, 0x83, 0x1f, 0xbc, 0x4d, 0x11, 0xff);


        IntPtr hwndVideo;
		object critical;
        TopoBuilder topoBuilder;
		MediaSession mediaSession;
        PlayerState state;
        VideoDisplayControl videoDisplay;
        AutoResetEvent closeCompleteEvent = new AutoResetEvent(false);

        public AsyncCallbackFlags Flags
        {
            get
            {
                return AsyncCallbackFlags.None;
            }
        }

        public WorkQueueId WorkQueueId
        {
            get
            {
                return WorkQueueType.Standard;
            }
        }

        private IDisposable _shadow;
        public IDisposable Shadow
        {
            get
            {
                return _shadow;
            }

            set
            {
                _shadow = value;
            }
        }

        public MediaDecoderLegacy(IntPtr hWnd)
		{
			this.critical = new object();
			this.hwndVideo = hWnd;
            this.state = PlayerState.PlayerState_Closed;
            MediaManager.Startup();
            topoBuilder = new TopoBuilder();
        }

		public void Dispose()
		{
			CloseSession();
			MediaManager.Shutdown();
		}

		public void CloseSession()
		{
            lock(critical)
            {
                videoDisplay?.Dispose();
                videoDisplay = null;
                if(mediaSession != null)
                {
                    state = PlayerState.PlayerState_Closing;
                    mediaSession.Close();
                    if(!closeCompleteEvent.WaitOne(5000))
                    {
                        return;
                    }
                }

                if(mediaSession != null)
                {
                    mediaSession.Shutdown();
                }
                mediaSession = null;
                state = PlayerState.PlayerState_Closed;
            }
		}

		public void OpenUrl(string url)
		{
            try {
                lock (critical)
                {
                    if (mediaSession == null)
                        CreateSession();
                    topoBuilder.RenderUrl(url, hwndVideo);
                    Topology topology = topoBuilder.GetTopology();
                    mediaSession.SetTopology(0, topology);

                    if (state == PlayerState.PlayerState_Ready)
                        state = PlayerState.PlayerState_OpenPending;
                }
            
            } catch (Exception exc)
            {
                Console.WriteLine("EXC");
                state = PlayerState.PlayerState_Closed;
            }
		}

        public void OpenUrl2(string url1, string url2)
        {
            try
            {
                lock (critical)
                {
                    if (mediaSession == null)
                        CreateSession();
                    topoBuilder.RenderUrl2(url1, url2, hwndVideo);
                    Topology topology = topoBuilder.GetTopology();
                    mediaSession.SetTopology(0, topology);

                    if (state == PlayerState.PlayerState_Ready)
                        state = PlayerState.PlayerState_OpenPending;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("EXC");
                state = PlayerState.PlayerState_Closed;
            }
        }



		//public void Init()
		//{
		//	MediaManager.Startup();

		//	MediaSession mediaSession;
		//	MediaFactory.CreateMediaSession(null, out mediaSession);
			

		//	SourceResolver sourceResolver = new SourceResolver();
			

		//	ComObject comObject;
		//	ObjectType objectType;
		//	comObject = sourceResolver.CreateObjectFromURL(@"D:\TestVideos\Prismatic_by_QubaHQ_2K_360Stereo_TB.mp4", SourceResolverFlags.None);
		//		//("Jack.mp4", (int)SourceResolverFlags.None, null, out objectType, out comObject);

		//	Topology topology;
		//	//sourceResolver.
		//	var mediaSource = comObject.QueryInterface<MediaSource>();
		//	PresentationDescriptor presentationDescriptor;
		//	mediaSource.CreatePresentationDescriptor(out presentationDescriptor);

		//	MediaFactory.CreateTopology(out topology);


			
			

		//	for (int it = 0; it < presentationDescriptor.StreamDescriptorCount; it++)
		//	{
		//		SharpDX.Bool selectedRef;
		//		StreamDescriptor descriptor;
		//		presentationDescriptor.GetStreamDescriptorByIndex(it, out selectedRef, out descriptor);
		//		if (descriptor.MediaTypeHandler.MajorType == MediaTypeGuids.Audio)
		//		{					
		//			TopologyNode inputNode;
		//			MediaFactory.CreateTopologyNode(TopologyType.SourceStreamNode, out inputNode);
		//			inputNode.Set(TopologyNodeAttributeKeys.Source.Guid, mediaSource);
		//			inputNode.Set(TopologyNodeAttributeKeys.PresentationDescriptor.Guid, presentationDescriptor);
		//			inputNode.Set(TopologyNodeAttributeKeys.StreamDescriptor.Guid, descriptor);

		//			TopologyNode outputNode;
		//			MediaFactory.CreateTopologyNode(TopologyType.OutputNode, out outputNode);
		//			Activate audioActivate;
		//			MediaFactory.CreateAudioRendererActivate(out audioActivate);
		//			outputNode.Object = audioActivate;

		//			topology.AddNode(inputNode);
		//			topology.AddNode(outputNode);
		//			inputNode.ConnectOutput(0, outputNode, 0);
		//		}

		//		if (descriptor.MediaTypeHandler.MajorType == MediaTypeGuids.Video)
		//		{
		//			TopologyNode inputNode;
		//			MediaFactory.CreateTopologyNode(TopologyType.SourceStreamNode, out inputNode);
		//			inputNode.Set(TopologyNodeAttributeKeys.Source.Guid, mediaSource);
		//			inputNode.Set(TopologyNodeAttributeKeys.PresentationDescriptor.Guid, presentationDescriptor);
		//			inputNode.Set(TopologyNodeAttributeKeys.StreamDescriptor.Guid, descriptor);

		//			TopologyNode outputNode;
		//			MediaFactory.CreateTopologyNode(TopologyType.OutputNode, out outputNode);
		//			Activate audioActivate;
		//			MediaFactory.CreateVideoRendererActivate(hwndVideo, out audioActivate);
		//			outputNode.Object = audioActivate;

		//			topology.AddNode(inputNode);
		//			topology.AddNode(outputNode);
		//			inputNode.ConnectOutput(0, outputNode, 0);
		//		}
		//	}
			
			

		//	mediaSession.SetTopology(SessionSetTopologyFlags.Immediate, topology);
		//}

		public IAsyncCallback ProcessMediaEvent(MediaEvent mediaEvent)
		{
            TopologyStatus topoStatus = TopologyStatus.Invalid;
			MediaEventTypes eventType = mediaEvent.TypeInfo;
			if(mediaEvent.Status.Failure)
			{
				throw new Exception("Bad event status");
			}
			switch(eventType)
			{
				case MediaEventTypes.SessionTopologyStatus:                    
					topoStatus = (TopologyStatus)mediaEvent.Status.Code;
					//if(topoStatus == TopologyStatus.Ready)
					{
						state = PlayerState.PlayerState_Stopped;
						OnTopologyReady();
					}
                    break;

                case MediaEventTypes.EndOfPresentation:
                    state = PlayerState.PlayerState_Stopped;
                    break;

                case MediaEventTypes.SessionClosed:
                    closeCompleteEvent.Set();
                    break;
			}

            return null;
		}

        public void OnTopologyReady()
        {
            videoDisplay?.Dispose();
            using (ServiceProvider serviceProvider = mediaSession.QueryInterface<ServiceProvider>())
            {
                videoDisplay = serviceProvider.GetService<VideoDisplayControl>(MR_VIDEO_RENDER_SERVICE);
            }
            Play();
        }


        public void Play()
        {
            lock(critical)
            {
                if(state != PlayerState.PlayerState_Paused && state != PlayerState.PlayerState_Stopped)
                {
                    throw new Exception("Wrong state");
                }
                if (mediaSession == null) throw new NullReferenceException();
                StartPlayback();
                state = PlayerState.PlayerState_Started;
            }
        }

        public void StartPlayback()
        {
            mediaSession.Start(null, new Variant() { ElementType = VariantElementType.Empty });
        }

        public void CreateSession()
        {
            lock(critical)
            {
                CloseSession();
                if (state != PlayerState.PlayerState_Closed)
                    throw new Exception("Invalid state");

                MediaFactory.CreateMediaSession(null, out mediaSession);
                if (mediaSession == null) throw new NullReferenceException();
                state = PlayerState.PlayerState_Ready;

                mediaSession.BeginGetEvent(this, null);
            }
        }

        public void Invoke(AsyncResult asyncResultRef)
        {
            MediaEvent mediaEvent;

            lock (critical)
            {
                if (asyncResultRef == null) throw new NullReferenceException();
                mediaEvent = mediaSession.EndGetEvent(asyncResultRef);
                if(state != PlayerState.PlayerState_Closing)
                {
                    ProcessMediaEvent(mediaEvent);
                }
                mediaSession.BeginGetEvent(this, null);
            }
        }

        public void Pause()
        {
            lock(critical)
            {
                if (state != PlayerState.PlayerState_Started)
                    throw new Exception("Wrong state");
                if (mediaSession == null)
                    throw new NullReferenceException();
                mediaSession.Pause();
                state = PlayerState.PlayerState_Paused;
            }
        }

        public void Repaint()
        {
            if (videoDisplay != null)
            {
                videoDisplay.RepaintVideo();
            }
        }

    }
}
