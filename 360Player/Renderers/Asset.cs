using System;

namespace Bivrost.Bivrost360Player
{
    internal abstract class Asset : IDisposable
    {
        public string AssetName { get; private set; }
        public abstract string AssetType { get; }

        public Asset(string name) { AssetName = name; }

        public abstract void Dispose();
    }
}