using System;
using Unity.Mathematics;
using UnityEngine;

namespace SkySystem
{
    [Serializable]
    public class MoonElement:BaseElement
    {
        private GameObject _moon;

        public Texture2D moonTexture;
        public Vector2 moonRotation;

        public Gradient moonColorGradient;
        public Texture starTexture;
        public float starIntensity;
        public float moonIntensity;
        public float moonDistance;
        public MoonElement(SkySystemData data)
        {
            moonTexture = data.moonTexture;
            moonRotation = data.moonRotation;
            moonColorGradient = data.moonColorGradient;
            starTexture = data.starTexture;
            starIntensity = data.starIntensity;
            moonIntensity = data.moonIntensity;
            moonDistance = data.moonDistance;
            _moon = GameObject.Find("Moon");
            if (_moon==null)
            {
                Debug.LogError("Moon Not Found");
            }
        }
        public override void AutoUpdate(float time)
        {
            if (_moon==null)
            {
                _moon = GameObject.Find("Moon");
            }
            time = time % 24;
            isDayTime = time > 6f && time <= 18f;

            _moon.transform.LookAt(-SkySystem.Instance.LightDirection*10000);
                //Debug.Log( rate);
            Shader.SetGlobalVector("_MoonDir",_moon.transform.forward);
            Shader.SetGlobalTexture("_MoonTexture",moonTexture);
            Shader.SetGlobalTexture("_StarTexture",starTexture);
            //float rate = 
            Shader.SetGlobalVector("_MoonGlowColor",moonColorGradient.Evaluate(time/24));
            Shader.SetGlobalFloat("_StarIntensity",starIntensity*math.saturate( math.abs(time-12)-5.5f));

            Shader.SetGlobalFloat("_MoonIntensity",moonIntensity*math.saturate( math.abs(time-12)-5));
            Shader.SetGlobalFloat("_MoonDistance", moonDistance);

        }
        public override void ManualUpdate()
        {
            if (_moon==null)
            {
                _moon = GameObject.Find("Moon");
            }
            _moon.transform.eulerAngles = new Vector3(moonRotation.y,moonRotation.x,0);
            Shader.SetGlobalVector("_MoonDir",_moon.transform.forward);
            Shader.SetGlobalTexture("_MoonTexture",moonTexture);
            //Shader.SetGlobalVector("_MoonGlowColor",moonColorGradient);
        }
         
        private Color GetNowLightColor(Gradient gradient,float rate)
        {
            Color c = Color.black;
            if (gradient!=null)
            {
                c=gradient.Evaluate(rate);
            }
            
            return c;
        }
    }
}