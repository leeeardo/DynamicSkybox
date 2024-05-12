using System;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkySystem
{
    [Serializable]
    public class LightElement : BaseElement
    {
        // [Range(0f,24f)]
        // public float time;
        //主光源：日光月光
        public float   lightIntensity;
        public Gradient sunLightGradient, moonLightGradient;
        public Vector2 lightRotation;

        public ReflectionResolution resolution;
        public LightElement(SkySystemData data)
        {
            lightIntensity = data.lightIntensity;
            sunLightGradient = data.sunLightGradient;
            moonLightGradient = data.moonLightGradient;
            lightRotation = data.lightRotation;
            resolution = data.resolution;
        }


        
        public override void ManualUpdate()
        {

            SkySystem.Instance.mainLight.color = GetNowLightColor(sunLightGradient, 1);
            SkySystem.Instance.mainLight.color = GetNowLightColor(moonLightGradient, 1);
            SkySystem.Instance.mainLight.gameObject.transform.eulerAngles =
                new Vector3(lightRotation.y, lightRotation.x, 0);
        }

        public override void AutoUpdate(float time)
        {
            isDayTime = time > 6f && time <= 18f;
            
            if (isDayTime)
            {
                SkySystem.Instance.mainLight.color = GetNowLightColor(sunLightGradient, time/24);
                //SkySystem.Instance.mainLight.gameObject.transform.LookAt( lightDir*10000);
                SkySystem.Instance.mainLight.gameObject.transform.LookAt(SkySystem.Instance.LightDirection * 10000);
            }
            else
            {
                SkySystem.Instance.mainLight.color = GetNowLightColor(moonLightGradient, time/24);
                SkySystem.Instance.mainLight.gameObject.transform.LookAt(-SkySystem.Instance.LightDirection * 10000);
                //Debug.Log( rate);
            }
        }
        private Color GetNowLightColor(Gradient gradient, float rate)
        {
            Color c = Color.black;
            if (gradient != null)
            {
                c = gradient.Evaluate(rate);
            }

            return c;
        }
    }
}