using System;
using System.Collections.Generic;
using System.IO;
using Bivrost.Log;
using SharpDX.Toolkit.Graphics;
#if DEBUG

#endif

namespace Bivrost.Bivrost360Player.Tools
{


    /// <summary>
    /// Provides compiled shaders in SharpDXToolkits's Effect form
    /// </summary>
    internal static class Effects
    {


        static private Dictionary<int, EffectData> compiledShaders = new Dictionary<int, EffectData>();


        static private Logger log = new Logger("Effects");


        public static EffectData Compile(string shaderCode, string shaderName)
        {
            int key = shaderCode.GetHashCode();

            EffectData effectData;
            if (compiledShaders.TryGetValue(key, out effectData))
                return effectData;

            log.Info($"Compiling shader {shaderName} (0x{key:X})");

            EffectCompiler compiler = new EffectCompiler();
            var effectCompilerResult = compiler.Compile(shaderCode, shaderName, EffectCompilerFlags.Debug | EffectCompilerFlags.EnableBackwardsCompatibility | SharpDX.Toolkit.Graphics.EffectCompilerFlags.SkipOptimization);

            if (effectCompilerResult.HasErrors)
            {
                var msg = string.Join("\n", effectCompilerResult.Logger.Messages);
                log.Fatal($"Error while compiling shader {shaderName} (0x{key:X}): {msg}");
                throw new HeadsetError($"Shader compile error: {msg}");
            }

            compiledShaders[key] = effectCompilerResult.EffectData;
            return effectCompilerResult.EffectData;
        }



        public static Effect GetEffect(GraphicsDevice gd, EffectData effectData)
        {
            var effect = new Effect(gd, effectData);
            effect.CurrentTechnique = effect.Techniques["ColorTechnique"];
            effect.CurrentTechnique.Passes[0].Apply();
            return effect;
        }



        public static EffectData GammaShader => Compile(Properties.Resources.GammaShader, "GammaShader");
        public static EffectData ImageBasedLightEquirectangular => Compile(Properties.Resources.ImageBasedLightEquirectangular, "ImageBasedLightEquirectangular");



#if DEBUG
        internal class AutoRefreshEffect:IDisposable
        {
            private bool dirty = false;
            private Effect effect = null;
            private string filePath;
            private FileSystemWatcher watcher;

            Action<Effect, GraphicsDevice> _initAction = null;
            public Action<Effect, GraphicsDevice> InitAction {
                set
                {
                    _initAction = value;
                    dirty = true;
                }
            }

            public AutoRefreshEffect(string filePath)
            {
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

            void IDisposable.Dispose()
            {
                effect?.Dispose();
                effect = null;

                watcher.Dispose();
                watcher = null;
            }


            public Effect Get(GraphicsDevice gd)
            {
                
                // First shader build
                if (effect == null)
                {
                    var hlslSource = File.ReadAllText(filePath);
                    var effectData = Compile(hlslSource, filePath);
                    effect = GetEffect(gd, effectData);
                    _initAction(effect, gd);
                    dirty = false;
                }

                // Shader changed
                else if (dirty)
                {
                    try
                    {
                        var hlslSource = File.ReadAllText(filePath);
                        var effectData = Compile(hlslSource, filePath);
                        var nextEffect = GetEffect(gd, effectData);
                        effect?.Dispose();
                        effect = nextEffect;
                        _initAction(effect, gd);
                    }
                    catch(Exception e)
                    {
                        log.Error(e, $"While compiling updated shader {filePath} (last working version kept)");
                    }
                    dirty = false;
                }


                return effect;
            }


        }
#endif
    }
}
