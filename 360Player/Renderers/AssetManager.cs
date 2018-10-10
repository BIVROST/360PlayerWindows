using System;
using System.Collections.Generic;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{
    internal class AssetManager:IDisposable
    {
        private d3d11.Device device;
        public HashSet<IDisposable> idisposables = new HashSet<IDisposable>();

        public AssetManager(d3d11.Device device)
        {
            this.device = device;
        }


        public void Register(Asset asset) { }
        public void RegisterIDisposable(IDisposable disposable) => idisposables.Add(disposable);


        public TextureAsset Texture(string filename)
        {
            throw new NotImplementedException();
        }


        public MeshAsset Model(string filename)
        {
            throw new NotImplementedException();
        }


        public ShaderAsset Material(string shaderName)
        {
            throw new NotImplementedException();
        }


        void IDisposable.Dispose()
        {
            foreach (var d in idisposables) d.Dispose();
            throw new NotImplementedException();
        }

    }

}