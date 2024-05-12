using System;
using Unity.VisualScripting;
using UnityEngine;

namespace SkySystem
{
    [Serializable]
    public class CloudElement:BaseElement
    {
        //public Texture2D mainTexture;
        public Color tint;
        public Color cloudTopColor;
        public Color cloudBottomColor;
        public float GIIndex;

        public CloudElement(SkySystemData data)
        {
            tint = data.tint;
            cloudTopColor = data.cloudTopColor;
            cloudBottomColor = data.cloudBottomColor;
            GIIndex = data.GIIndex;
        }


        public void ManualUpdate(float time)
        {
            Shader.SetGlobalColor("_BaseColor",tint);
            Shader.SetGlobalColor("_CloudTopColor",cloudTopColor);
            Shader.SetGlobalColor("_CloudBottomColor",cloudBottomColor);
            Shader.SetGlobalFloat("_GIIndex",GIIndex);
        }
        
        
        
        
        
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