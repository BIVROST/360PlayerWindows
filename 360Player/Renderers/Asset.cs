using System;
using d3d11 = SharpDX.Direct3D11;

namespace Bivrost.Bivrost360Player
{
    internal abstract class Asset : IDisposable
    {
        public readonly string AssetID;

        public Asset(string assetType, string assetName)
        {
            if (assetType == null) throw new ArgumentNullException(nameof(assetType));
            if (assetName == null) throw new ArgumentNullException(nameof(assetName));
            AssetID = $"{assetType}:{assetName}";
        }

        public abstract void Load(d3d11.Device device);

        protected abstract void Unload();

        public override int GetHashCode()
        {
            return AssetID.GetHashCode();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Unload();
                }
                disposedValue = true;
            }
        }


        ~Asset()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}