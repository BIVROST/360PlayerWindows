#if DEBUG
#define LIVE_UPDATE_ENABLED
using System.IO;
using Bivrost.Log;
#endif

using SharpDX.D3DCompiler;
using d3d11 = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;

namespace Bivrost.Bivrost360Player
{
    internal class MaterialAsset : Asset
    {

        public override string AssetType => "Material";
        private d3d11.VertexShader vertexShader;
        private d3d11.PixelShader pixelShader;
        private d3d11.InputLayout layout;


        public MaterialAsset(d3d11.Device device, string shaderFile):base(shaderFile)
        {
            Load(device, System.IO.File.ReadAllBytes(shaderFile), shaderFile);
            EnableLiveReload("../../" + shaderFile);
        }


        public MaterialAsset(d3d11.Device device, byte[] shaderBytes, string shaderName):base(shaderName)
        {
            Load(device, shaderBytes, shaderName);
        }


        public void Load(d3d11.Device device, byte[] shaderBytes, string shaderName)
        {
            // Compile Vertex and Pixel shaders
            using (var vertexShaderByteCode = ShaderBytecode.Compile(shaderBytes, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None, shaderName))
            using (var pixelShaderByteCode = ShaderBytecode.Compile(shaderBytes, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None, shaderName))
            using (var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode))
            {
                var nextVertexShader = new d3d11.VertexShader(device, vertexShaderByteCode);
                var nextPixelShader = new d3d11.PixelShader(device, pixelShaderByteCode);

                //var vsRefl = new ShaderReflection(vertexShaderByteCode);

                var nextLayout = new d3d11.InputLayout(
                    device,
                    signature,
                    new[]
                    {
                        new d3d11.InputElement("POSITION", 0, dxgi.Format.R32G32B32A32_Float,  d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("COLOR",    0, dxgi.Format.R32G32B32A32_Float,  d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("NORMAL",   0, dxgi.Format.R32G32B32_Float,     d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("TEXCOORD", 0, dxgi.Format.R32G32_Float,        d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("TANGENT",  0, dxgi.Format.R32G32B32_Float,     d3d11.InputElement.AppendAligned, 0),
                        new d3d11.InputElement("BINORMAL", 0, dxgi.Format.R32G32B32_Float,     d3d11.InputElement.AppendAligned, 0)
                    }
                );

                vertexShader?.Dispose();
                pixelShader?.Dispose();
                layout?.Dispose();

                vertexShader = nextVertexShader;
                pixelShader = nextPixelShader;
                layout = nextLayout;
            }
        }



#if LIVE_UPDATE_ENABLED
        private string filePath;
        private FileSystemWatcher watcher;
        private bool dirty = false;
        static private Logger log = new Logger("Effects");

        void EnableLiveReload(string filePath)
        {
            if(!File.Exists(filePath))
            {
                throw new FileNotFoundException("Could not load shader file for live reload", filePath);
            }

            this.filePath = filePath;

            watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath))
            {
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            watcher.Created += WatcherTriggered;
            watcher.Deleted += WatcherTriggered;
            watcher.Changed += WatcherTriggered;
            watcher.Renamed += WatcherTriggered;

            watcher.EnableRaisingEvents = true;
        }

        private void WatcherTriggered(object sender, FileSystemEventArgs e)
        {
            dirty = true;
            log.Info($"Shader source at {filePath} has updated ({e.ChangeType})");
        }
#endif

        public void Apply(d3d11.DeviceContext context)
        {
#if LIVE_UPDATE_ENABLED
            if(dirty)
            {
                try
                {
                    dirty = false;
                    log.Info("Live reload of {filePath}");
                    Load(context.Device, File.ReadAllBytes(filePath), Path.GetFileName(filePath));
                }
                catch(System.Exception e)
                {
                    log.Error(e, "Live reload of {filePath} failed:");
                }
            }
#endif
            context.InputAssembler.InputLayout = layout;
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
        }


        public override void Dispose()
        {
            vertexShader.Dispose();
            pixelShader.Dispose();
            layout.Dispose();
#if LIVE_UPDATE_ENABLED
            watcher?.Dispose();
#endif
        }


    }
}