using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkySystem
{
   
    public class CreateMenuItemInHierachy
    {
        
        [MenuItem("GameObject/Customs/SkySystem")]
        static void CreateSkySystemObject()
        {
            if (GameObject.FindObjectOfType(typeof(SkySystem))!=null)
            {
                Debug.Log("存在同名SkySystem");
                return;
            }
            Debug.Log("创建Sky System");
            GameObject obj = new GameObject();
            obj.name = "SkySystem";
            SkySystem system= obj.AddComponent<SkySystem>();
            //GameObject.DontDestroyOnLoad(obj);
        }
    }
}

