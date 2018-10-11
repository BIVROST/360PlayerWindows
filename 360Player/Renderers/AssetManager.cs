using System;
using System.Collections.Generic;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{
    internal class AssetManager:IDisposable
    {
        private d3d11.Device device;
        protected HashSet<IDisposable> idisposables = new HashSet<IDisposable>();
        protected Dictionary<string, Asset> assets = new Dictionary<string, Asset>();
            

        public AssetManager(d3d11.Device device)
        {
            this.device = device;
        }


        public void Register(Asset asset) { throw new NotImplementedException(); }
        public void RegisterIDisposable(IDisposable disposable) => idisposables.Add(disposable);


        protected T RetrieveFromCacheOrLoad<T>(T asset) where T:Asset
        {
            Asset returned;
            if (assets.TryGetValue(asset.AssetID, out returned))
            {
                asset.Dispose();
                return (T)returned;
            }

            asset.Load(device);
            assets[asset.AssetID] = asset;

            return asset;
        }


        public void Remove(Asset asset)
        {
            if (!assets.Remove(asset.AssetID)) throw new ArgumentException("Asset was not registered", nameof(asset));
            asset.Dispose();
        }

        public void Remove(IDisposable disposable)
        {
            if (!idisposables.Remove(disposable)) throw new ArgumentException("IDisposable was not registered", nameof(disposable));
            disposable.Dispose();
        }


        public TextureAsset Texture(string filename)
        {
            return RetrieveFromCacheOrLoad(new TextureAsset(filename));
        }


        public TextureAsset EmptyTexture(string name)
        {
            return RetrieveFromCacheOrLoad(new TextureAsset(null, name));
        }


        public MeshAsset Mesh(string filename)
        {
            return RetrieveFromCacheOrLoad(new MeshAsset(filename));
        }


        public ShaderAsset Shader(string shaderFile)
        {
            return RetrieveFromCacheOrLoad(new ShaderAsset(shaderFile));
        }


        public void Dispose()
        {
            foreach (var d in idisposables) d.Dispose();

            foreach (var kvp in assets) kvp.Value.Dispose();

        }

    }

}