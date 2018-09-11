using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bivrost.Log;
using SharpDX.Toolkit.Graphics;

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

    }
}
