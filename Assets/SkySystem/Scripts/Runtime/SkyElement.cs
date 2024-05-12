using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace SkySystem
{
    [Serializable]
    public class SkyElement:BaseElement
    {
        [GradientUsage(true)]
        public Gradient daySkyGradient;

        public Gradient nightSkyGradient;
        public Gradient fogColorGradient;
        private Texture2D _skyRampMap;

        public SkyElement(SkySystemData data)
        {
            daySkyGradient = data.daySkyGradient;
            nightSkyGradient = data.nightSkyGradient;
            fogColorGradient = data.fogColorGradient;
            // skyGradient = new Gradient();
            // GradientColorKey key1 = new GradientColorKey(Color.black, 0);
            // GradientColorKey key2 = new GradientColorKey(Color.white, 1);
            // skyGradient.colorKeys = new[] { key1, key2 };

            // _skyRampMap = new Texture2D(256,1,TextureFormat.ARGB32,false,true);
            // _skyRampMap.filterMode = FilterMode.Bilinear;
            // _skyRampMap.wrapMode = TextureWrapMode.Clamp;
            // _skyRampMap.anisoLevel = 1;
        }


        public void ManualUpdate(float time)
        {
            
            Shader.SetGlobalTexture("_SkyRampMap",applyGradient(daySkyGradient));
            Shader.SetGlobalTexture("_SkyWorldYRampMap",applyGradient(nightSkyGradient));
            // RenderSettings.fogColor = Color.Lerp(daySkyGradient.Evaluate(0.5f),
            //     nightSkyGradient.Evaluate(0.5f),
            //      math.saturate(SkySystem.Instance.LightDirection.y * 5));
            RenderSettings.fogColor = fogColorGradient.Evaluate(time/24);
            
        }
        
        
        
        /// <summary>
        /// 把Gradient类信息记录在Texture2D上(内存，未写入文件)
        /// </summary>
        /// <param name="ramp"></param>
        /// <returns></returns>
        private Texture2D applyGradient(Gradient ramp)
        {
            Texture2D tempTex = new Texture2D(256,1,TextureFormat.ARGB32,false,true);
            tempTex.filterMode = FilterMode.Bilinear;
            tempTex.wrapMode = TextureWrapMode.Clamp;
            tempTex.anisoLevel = 1;
            Color[] colors = new Color[256];
            float div = 256.0f;
            for (int i = 0; i < 256; ++i)
            {
                float t = (float)i / div;
                colors[i] = ramp.Evaluate(t);
            }
            tempTex.SetPixels(colors);
            tempTex.Apply();
            return tempTex;
        }
    }
    
    
}