using System;
using System.Transactions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SkySystem
{
    [Serializable]
    public class SunElement:BaseElement
    {
        private GameObject _sun;

        public Gradient sunDiscGradient = new Gradient();
        public Vector2 sunRotation;
        public Vector4 sunHalo = new Vector4(0.25f,0.25f,0.25f,0.25f);
        public float sunIntensity;
        public Gradient sunColorGradient = new Gradient();
        public SunElement(SkySystemData data)
        {
            _sun = GameObject.Find("Sun");
            if (_sun==null)
            {
                Debug.LogError("Sun Not Found");
            }
            LoadData(data);
        }

        //todo autoUpdateSunColor
        public override void AutoUpdate(float time)
        {
            if (_sun==null)
            {
                _sun = GameObject.Find("Sun");
            }
            
            sunRotation.y = 90 + time * 90f / 6f;
            _sun.transform.eulerAngles = new Vector3(sunRotation.y,sunRotation.x,0);
            Shader.SetGlobalVector("_SunDir",this._sun.transform.forward);
            Shader.SetGlobalVector("_SunHalo",sunHalo);
            Shader.SetGlobalColor("_SunGlowColor",sunColorGradient.Evaluate(time/24));
            Shader.SetGlobalFloat("_SunIntensity",sunIntensity);
            Shader.SetGlobalTexture("_SunDiscGradient",applyGradient(sunDiscGradient));
            SkySystem.Instance.LightDirection = -_sun.transform.forward;
        }
        public override void ManualUpdate()
        {
            if (_sun==null)
            {
                _sun = GameObject.Find("Sun");
            }
            _sun.transform.eulerAngles = new Vector3(sunRotation.y,sunRotation.x,0);
            Shader.SetGlobalVector("_SunDir",this._sun.transform.forward);
            Shader.SetGlobalVector("_SunHalo",sunHalo);
            Shader.SetGlobalColor("_SunGlowColor",sunColorGradient.Evaluate(0));
            SkySystem.Instance.LightDirection = -_sun.transform.forward;
        }
        public void LoadData(SkySystemData data)
        {
            sunDiscGradient = data.sunDiscGradient;
            sunRotation = data.sunRotation;
            sunHalo = data.sunHalo;
            sunIntensity = data.sunIntensity;
            sunColorGradient = data.sunColorGradient;
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