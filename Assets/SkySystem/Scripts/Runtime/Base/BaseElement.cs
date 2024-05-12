using UnityEngine;

namespace SkySystem
{
    public class BaseElement
    {
        [HideInInspector]
        public bool isDayTime;

        public virtual void ManualUpdate()
        {
            
        }
        public virtual void AutoUpdate(float time)
        {
            
        }
        public virtual void Enable()
        {

        }

        public virtual void Disable()
        {

        }
    }
    
}
