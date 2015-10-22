using SharpDX;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyPlayer
{
    class TopoBuilder
    {
        private Topology topology;
        private MediaSource mediaSource;
        private VideoDisplayControl videoDisplay;
        private IntPtr videoHwnd;

        public void RenderUrl(string fileUrl, IntPtr videoHwnd)
        {
            this.videoHwnd = videoHwnd;
            mediaSource = CreateMediaSource(fileUrl);
            CreateTopology();
        }

        public void RenderUrl2(string fileUrl1, string fileUrl2, IntPtr videoHwnd)
        {
            this.videoHwnd = videoHwnd;
            var tempSource = CreateMediaSource(fileUrl1);
            var tempSource2 = CreateMediaSource(fileUrl2);
            Collection mediaCollection;
            MediaFactory.CreateCollection(out mediaCollection);
            mediaCollection.AddElement(tempSource);
            mediaCollection.AddElement(tempSource2);
            MediaFactory.CreateAggregateSource(mediaCollection, out mediaSource);

            CreateTopology();
        }

        public Topology GetTopology()
        {
            return this.topology;
        }

        public void ShutdownSource()
        {
            if (mediaSource != null)
            {
                mediaSource.Shutdown();
                mediaSource.Dispose();
                mediaSource = null;
            }
        }

        private MediaSource CreateMediaSource(string sURL)
        {
            SourceResolver sourceResolver = new SourceResolver();
            ComObject comObject;
            comObject = sourceResolver.CreateObjectFromURL(sURL, SourceResolverFlags.MediaSource | SourceResolverFlags.ContentDoesNotHaveToMatchExtensionOrMimeType);
            return comObject.QueryInterface<MediaSource>();
        }

        private void CreateTopology()
        {
            topology?.Dispose();
            topology = null;

            MediaFactory.CreateTopology(out topology);

            if (mediaSource != null)
            {
                PresentationDescriptor presentationDescriptor;
                mediaSource.CreatePresentationDescriptor(out presentationDescriptor);

                for (int it = 0; it < presentationDescriptor.StreamDescriptorCount; it++)
                {
                    try
                    {
                        AddBranchToPartialTopology(presentationDescriptor, it);
                    }
                    catch (Exception exc)
                    {
                        presentationDescriptor.DeselectStream(it);
                    }
                }
            }
        }

        private void AddBranchToPartialTopology(PresentationDescriptor presentationDescriptor, int iStream)
        {
            SharpDX.Bool isSelected;
            StreamDescriptor streamDescriptor;
            presentationDescriptor.GetStreamDescriptorByIndex(iStream, out isSelected, out streamDescriptor);
            if (isSelected)
            {
                TopologyNode sourceNode = CreateSourceStreamNode(presentationDescriptor, streamDescriptor);
                //var resource = videoTexture.QueryInterface<SharpDX.DXGI.Resource>();
                //var sharedTex = _device.OpenSharedResource<Texture2D>(resource.SharedHandle)
                TopologyNode outputNode = CreateOutputNode(streamDescriptor, videoHwnd);

                topology.AddNode(sourceNode);
                topology.AddNode(outputNode);
                sourceNode.ConnectOutput(0, outputNode, 0);
            }
        }

        private TopologyNode CreateSourceStreamNode(PresentationDescriptor presentationDescriptor, StreamDescriptor streamDescriptor)
        {
            TopologyNode inputNode;
            MediaFactory.CreateTopologyNode(TopologyType.SourceStreamNode, out inputNode);
            inputNode.Set(TopologyNodeAttributeKeys.Source.Guid, mediaSource);
            inputNode.Set(TopologyNodeAttributeKeys.PresentationDescriptor.Guid, presentationDescriptor);
            inputNode.Set(TopologyNodeAttributeKeys.StreamDescriptor.Guid, streamDescriptor);

            return inputNode;
        }

        private TopologyNode CreateOutputNode(StreamDescriptor streamDescriptor, IntPtr hwnd)
        {
            TopologyNode outputNode;
            Activate rendererActivate;

            if (streamDescriptor.MediaTypeHandler.MajorType == MediaTypeGuids.Audio)
            {
                MediaFactory.CreateAudioRendererActivate(out rendererActivate);
            }
            else if (streamDescriptor.MediaTypeHandler.MajorType == MediaTypeGuids.Video)
            {                
                MediaFactory.CreateVideoRendererActivate(hwnd, out rendererActivate);
            }
            else throw new Exception("Bad stream");

            MediaFactory.CreateTopologyNode(TopologyType.OutputNode, out outputNode);            
            outputNode.Object = rendererActivate;

            return outputNode;
        }
    }
}