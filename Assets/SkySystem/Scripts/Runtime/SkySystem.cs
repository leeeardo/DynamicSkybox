using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VisualDesignCafe.ShaderX.Materials.Editor;

namespace SkySystem
{
    public enum ReflectionResolution
    {
        R16=16,
        R32=32,
        R64=64,
        R128=128,
        R256=256,
        R512=512,
        R1024=1024,
        R2048=2048
    }
    
    [ExecuteInEditMode]
    public class SkySystem:MonoBehaviour
    {
        private static SkySystem instance;
        public static SkySystem Instance
        { 
            get
            {
                if (instance == null)
                    instance = GameObject.FindObjectOfType<SkySystem>();
                return instance;
            }
        }

        [Title("动态天空盒系统", "--leardo", TitleAlignments.Centered)]
        [PropertyOrder(1), PropertyRange(0,24)] [ShowInInspector]
        public float Hour
        {
            get => data.hour;
            set { data.hour = value; }
        }

        [PropertyOrder(2)] [ShowInInspector] public bool auto = true;
        [PropertyOrder(1)] [ShowInInspector]
        public bool timeControlEverything
        {
            get => data.timeControlEverything;
            set { data.timeControlEverything = value; }
        }

        //Test
        [PropertyOrder(2)]
        public Light mainLight;
        [PropertyOrder(2)]
        public Vector3 LightDirection;

        //sky
        private SkyElement skySettings;
         [PropertyOrder(3)][FoldoutGroup("Sky",true)]
        public Gradient daySkyGradient;
        [PropertyOrder(3)][FoldoutGroup("Sky",true)]
        public Gradient nightSkyGradient;

        //sun
        private SunElement sunSettings;
        [PropertyOrder(4)] [ShowInInspector][FoldoutGroup("Sun",true)]
        public Gradient sunDiscGradient;
        [PropertyOrder(4)] [ShowInInspector][FoldoutGroup("Sun",true)]
        public Vector2 sunRotation
        {
            get => sunSettings.sunRotation;
            set { sunSettings.sunRotation = value; }
        }
        [PropertyOrder(4)] [ShowInInspector][FoldoutGroup("Sun",true)]
        public Vector4 sunHalo
        {
            get => sunSettings.sunHalo;
            set { sunSettings.sunHalo = value; }
        }  
        [PropertyOrder(5)] [ShowInInspector][FoldoutGroup("Sun",true)]
        public Gradient sunColorGradient;
        [PropertyOrder(5)] [ShowInInspector][FoldoutGroup("Sun",true)]
        public float sunIntensity
        {
            get => sunSettings.sunIntensity;
            set { sunSettings.sunIntensity = value; }
        }
         
        //moon
        private MoonElement moonSettings;
        [PropertyOrder(5)] [ShowInInspector][FoldoutGroup("moon",true)]
        public Texture2D moonTexture
        {
            get => moonSettings.moonTexture;
            set { moonSettings.moonTexture = value; }
        }
        [PropertyOrder(5)] [ShowInInspector][FoldoutGroup("moon",true)]
        public Vector2 moonRotation
        {
            get => moonSettings.moonRotation;
            set { moonSettings.moonRotation = value; }
        }
        [PropertyOrder(5)][FoldoutGroup("moon",true)]
        public Gradient moonColorGradient;
        [PropertyOrder(5)] [ShowInInspector][FoldoutGroup("moon",true)]
        public float moonIntensity
        {
            get => moonSettings.moonIntensity;
            set { moonSettings.moonIntensity = value; }
        }
        [PropertyOrder(5)] [ShowInInspector][FoldoutGroup("moon",true)]
        public float moonDistance
        {
            get => moonSettings.moonDistance;
            set { moonSettings.moonDistance = value; }
        }
        [PropertyOrder(6)] [ShowInInspector][FoldoutGroup("moon",true)]
        public Texture starTexture
        {
            get => moonSettings.starTexture;
            set { moonSettings.starTexture = value; }
        }
        [PropertyOrder(6)] [ShowInInspector,InspectorRange(0f,1f)][FoldoutGroup("moon",true)]
        public float starIntensity
        {
            get => moonSettings.starIntensity;
            set { moonSettings.starIntensity = value; }
        }
        
        
        //light
        private LightElement lightingSettings;
        [PropertyOrder(6)] [ShowInInspector][FoldoutGroup("Lighting",true)]
        public float lightIntensity
        {
            get => lightingSettings.lightIntensity;
            set { lightingSettings.lightIntensity = value; }
        }
        [PropertyOrder(6)] [ShowInInspector][FoldoutGroup("Lighting",true)]
        public Vector2 lightRotation
        {
            get => lightingSettings.lightRotation;
            set { lightingSettings.lightRotation = value; }
        }
        [PropertyOrder(7)]
        [SerializeField][FoldoutGroup("Lighting",true)]
        public Gradient sunLightGradient;
        [PropertyOrder(7)][FoldoutGroup("Lighting",true)]
        public Gradient moonLightGradient;

        private CloudElement cloudSettings;

        [PropertyOrder(7)] [ShowInInspector] [FoldoutGroup("Cloud", true)]
        public Color cloudTint
        {
            get => cloudSettings.tint;
            set { cloudSettings.tint = value; }
        }
        [PropertyOrder(7)] [ShowInInspector] [FoldoutGroup("Cloud", true)]
        public Color cloudTopColor
        {
            get => cloudSettings.cloudTopColor;
            set { cloudSettings.cloudTopColor = value; }
        }
        [PropertyOrder(7)] [ShowInInspector] [FoldoutGroup("Cloud", true)]
        public Color cloudBottomColor
        {
            get => cloudSettings.cloudBottomColor;
            set { cloudSettings.cloudBottomColor = value; }
        }

        [PropertyOrder(7)] [ShowInInspector] [FoldoutGroup("Cloud", true),PropertyRange(0f,1f)]
        public float GIIndex
        {
            get => cloudSettings.GIIndex;
            set { cloudSettings.GIIndex = value; }
        }
        private ProbeElement probeSettings;
        [PropertyOrder(8)][FoldoutGroup("EnvironmentSettings",true)]
        public Gradient fogColorGradient;
        [PropertyOrder(8)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public AmbientMode ambientMode
        {
            get => probeSettings.ambientMode;
            set { probeSettings.ambientMode = value; }
        }
        [PropertyOrder(8)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public ReflectionProbeMode reflectionProbeMode
        {
            get => probeSettings.mode;
            set { probeSettings.mode = value; }
        }

        [PropertyOrder(8)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public ReflectionResolution reflectionResolition
        {
            get => (ReflectionResolution)probeSettings.resolution;
            set { probeSettings.resolution = (int)value; }
        }
        [PropertyOrder(8)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public bool boxProjection
        {
            get => probeSettings.boxProjection;
            set { probeSettings.boxProjection = value; }
        }
        [PropertyOrder(8)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public bool HDR
        {
            get => probeSettings.HDR;
            set { probeSettings.HDR = value; }
        }
        [PropertyOrder(8)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public ReflectionProbeClearFlags clearFlags
        {
            get => probeSettings.clearFlags;
            set { probeSettings.clearFlags = value; }
        }

        [PropertyOrder(9)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public Texture skyboxCubemap
        {
            get => probeSettings.cubemap;
            set { probeSettings.cubemap=value; }
        }
        [PropertyOrder(9)] [ShowInInspector][FoldoutGroup("EnvironmentSettings",true)]
        public LayerMask cullingMask
        {
            get => probeSettings.cullingMask;
            set { probeSettings.cullingMask = value; }
        }
        
        //private
        private Material skyboxMat;
        private Material defaultSkyboxMat;
        private SkySystemData data;
        [Title("辅助功能")]
        [PropertyOrder(252)]
        [Sirenix.OdinInspector.FilePath][BoxGroup("others")]
        public string dataPath=Application.streamingAssetsPath + "/TestData.json";
        [PropertyOrder(253)]
        [Button("保存数据",Icon = SdfIconType.SaveFill)][BoxGroup("others")]
        private void SaveData()
        {
            //获取各element的数据
            data.hour = Hour;
            data.timeControlEverything = timeControlEverything;
            //sky
            data.daySkyGradient = daySkyGradient;
            data.nightSkyGradient = nightSkyGradient;
            //sun
            data.sunDiscGradient = sunDiscGradient;
            data.sunRotation = sunRotation;
            data.sunHalo = sunHalo;
            data.sunIntensity = sunIntensity;
            data.sunColorGradient = sunColorGradient;
            //moon
            data.moonTexture = moonTexture;
            data.moonRotation = moonRotation;
            data.moonIntensity = moonIntensity;
            data.moonDistance = moonDistance;
            data.moonColorGradient = moonColorGradient;
            data.starTexture = starTexture;
            data.starIntensity = starIntensity;
            //lighting
            data.lightIntensity = lightIntensity;
            data.sunLightGradient = sunLightGradient;
            data.moonLightGradient = moonLightGradient;
            data.lightRotation = lightRotation;
            //cloud
            data.tint = cloudTint;
            data.cloudTopColor = cloudTopColor;
            data.cloudBottomColor = cloudBottomColor;
            data.GIIndex = GIIndex;
            data.fogColorGradient = fogColorGradient;
            //probe
            data.ambientMode = ambientMode;
            data.mode = reflectionProbeMode;
            data.cubemap = skyboxCubemap;
            data.boxProjection = boxProjection;
            data.resolution = reflectionResolition;
            data.HDR = HDR;
            data.clearFlags = clearFlags;
            data.cullingMask = cullingMask;
            
            //保存到json
            data.SaveSystemData(dataPath);
        }
        [PropertyOrder(253)]
        [Button("读取数据",Icon = SdfIconType.Download)][BoxGroup("others")]
        private void LoadData()
        {
            //从json获取数据
            data.LoadSystemData(dataPath);
            //赋予各element
            Hour = data.hour;
            timeControlEverything = data.timeControlEverything;
            //sky
            daySkyGradient = data.daySkyGradient;
            nightSkyGradient = data.nightSkyGradient;
            //sun
            sunDiscGradient = data.sunDiscGradient;
            sunRotation = data.sunRotation;
            sunHalo = data.sunHalo;
            sunIntensity = data.sunIntensity;
            sunColorGradient = data.sunColorGradient;
            //moon
            moonTexture = data.moonTexture;
            moonRotation = data.moonRotation;
            moonIntensity = data.moonIntensity;
            moonDistance = data.moonDistance;
            moonColorGradient = data.moonColorGradient;
            starTexture = data.starTexture;
            starIntensity = data.starIntensity;
            //lighting
            lightIntensity = data.lightIntensity;
            sunLightGradient = data.sunLightGradient;
            moonLightGradient = data.moonLightGradient;
            lightRotation = data.lightRotation;
            //cloud
            cloudTint = data.tint;
            cloudTopColor = data.cloudTopColor;
            cloudBottomColor = data.cloudBottomColor;
            GIIndex = data.GIIndex;
            fogColorGradient = data.fogColorGradient;
            //probe
            ambientMode = data.ambientMode;
            reflectionProbeMode = data.mode;
            skyboxCubemap = data.cubemap;
            boxProjection = data.boxProjection;
            reflectionResolition = data.resolution;
            HDR = data.HDR;
            clearFlags = data.clearFlags;
            cullingMask = (LayerMask)data.cullingMask;
        }
        
        
        [PropertyOrder(254)]
        [FolderPath][BoxGroup]
        public string path="Assets/SkySystem/Resources/Textures/Skyboxs";
        [PropertyOrder(255)][BoxGroup]
        [Button("渲染当前天空盒保存在路径里")]
        private void RenderSkybox()
        {
            //创建目录
            // string texturePath = path;
            // if (!Directory.Exists(texturePath))
            // {
            //     Directory.CreateDirectory(texturePath);
            // }
            // TextureFormat format = probe.hdr ? TextureFormat.RGBAFloat : TextureFormat.RGBA64;
            // Cubemap cubemap = new Cubemap(probe.resolution,format,false);
            //
            // renderCam.enabled = true;
            //     
            // renderCam.RenderToCubemap(cubemap);
            // Texture2D tex = GetTexture2DByCubeMap(cubemap, format);
            // SaveTexture2DFile(tex, texturePath+"/Skybox_"+SkySystem.Instance.Hour+".png");
            //     
            // AssetDatabase.Refresh();
            // SetTextureAsCubemap(texturePath);
            // //收尾工作
            // DestroyImmediate(cubemap);
            // cubemap = null;
            // AssetDatabase.Refresh();
            probeSettings.RenderSkybox(path);
        }
        private void Start()
        {
            if (data!=null)
            {
                data.SaveSystemData();
            }
            if( Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }
        private void Awake()
        {
            //实例化GameObject&绑定

            //获取defaultData 并实例化各个Object
            
        }
        

        private void OnEnable()
        {
            if (skyboxMat==null)
            {
                skyboxMat = new Material(Shader.Find("Custom/Skybox"));
            }
            if (skyboxMat!=null)
            {
                defaultSkyboxMat = RenderSettings.skybox;
                RenderSettings.skybox = skyboxMat;
                //RenderSettings.sun = mainLight;
            }
            //data.LoadSystemData();
            if (data==null)
            {
                data = ScriptableObject.CreateInstance<SkySystemData>();
                data.LoadSystemData();
            }
            timeControlEverything = data.timeControlEverything;
            
            skySettings = new SkyElement(data);
            daySkyGradient = skySettings.daySkyGradient;
            nightSkyGradient = skySettings.nightSkyGradient;
            fogColorGradient = skySettings.fogColorGradient;
            if (GameObject.Find("Sun")==null)
            {
                GameObject sunObj = new GameObject("Sun");
                sunObj.transform.parent = transform;
            }
            if (sunSettings==null)
            {
                sunSettings = new SunElement(data);
            }

            sunDiscGradient = sunSettings.sunDiscGradient;
            sunColorGradient = sunSettings.sunColorGradient;
            sunIntensity = sunSettings.sunIntensity;
            
            if (GameObject.Find("Moon")==null)
            {
                GameObject moonObj = new GameObject("Moon");
                moonObj.transform.parent = transform;
            }
            if (moonSettings==null)
            {
                moonSettings = new MoonElement(data);
            }
            moonTexture = moonSettings.moonTexture;
            starTexture = moonSettings.starTexture;
            moonRotation = moonSettings.moonRotation;
            moonColorGradient = moonSettings.moonColorGradient;
            starIntensity = moonSettings.starIntensity;
            moonDistance = moonSettings.moonDistance;
            moonIntensity = moonSettings.moonIntensity;
            
            if (GameObject.Find("CloudController")==null)
            {
                GameObject cloudControllerObj = new GameObject("CloudController");
                cloudControllerObj.transform.parent = transform;
            }
            if (cloudSettings==null)
            {
                cloudSettings = new CloudElement(data);
            }
            cloudTint = cloudSettings.tint;
            cloudTopColor = cloudSettings.cloudTopColor;
            cloudBottomColor = cloudSettings.cloudBottomColor;
            GIIndex = cloudSettings.GIIndex;

            if (lightingSettings==null)
            {
                lightingSettings = new LightElement(data);
            }
            if (GameObject.Find("Sun&Moon Light")==null)
            {
                GameObject lightObj = new GameObject("Sun&Moon Light");
                lightObj.transform.parent = transform;
                mainLight = lightObj.AddComponent<Light>();
                RenderSettings.sun = mainLight;
                mainLight.shadows = LightShadows.Soft;
            }

            if (mainLight==null)
            {
                mainLight = GameObject.Find("Sun&Moon Light").GetComponent<Light>();
            }
            lightIntensity = lightingSettings.lightIntensity;
            sunLightGradient = lightingSettings.sunLightGradient;
            moonLightGradient = lightingSettings.moonLightGradient;
            lightRotation = lightingSettings.lightRotation;
            mainLight.type = LightType.Directional;

            //反射探针
            if (GameObject.Find("SkySystem_ReflectionProbe")==null)
            {
                GameObject probeObj = new GameObject("SkySystem_ReflectionProbe");
                probeObj.transform.SetParent(transform);
                probeObj.AddComponent<ReflectionProbe>();
                
            }

            if (probeSettings==null)
            {
                probeSettings = new ProbeElement(data);
            }
        }

        private void Update()
        {
            if (auto)
            {
                Hour += Time.deltaTime/2;
                Hour %= 24;
            }
            
            skySettings.ManualUpdate(Hour);
            cloudSettings.ManualUpdate(Hour);
            if (timeControlEverything)
            {
                sunSettings.AutoUpdate(Hour);
                lightingSettings.AutoUpdate(Hour);
                moonSettings.AutoUpdate(Hour);
            }
             
            else
            {
                sunSettings.ManualUpdate();
                lightingSettings.ManualUpdate();
                moonSettings.ManualUpdate();
            }
        }

        private void OnDisable()
        {
            if (skyboxMat!=null)
            {
                RenderSettings.skybox = defaultSkyboxMat;
            }
            data.SaveSystemData();
        }
        private void SaveTexture2DFile(Texture2D texture, string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            
            byte[] vs = texture.EncodeToPNG();
            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(vs , 0 , vs.Length);
            fileStream.Dispose();
            fileStream.Close();
        }
        
        private void Convert2EXR(RenderTexture renderTexture, string path)
        {
            
            int width = renderTexture.width;
            int height = renderTexture.height;
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0,0,width,height),0,0);
            texture2D.Apply();
            byte[] vs = texture2D.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);

            FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(vs , 0 , vs.Length);
            fileStream.Dispose();
            fileStream.Close();
            Debug.Log("保存成功");
            DestroyImmediate(texture2D);
        }
        
        
        public static Texture2D FlipPixels(Texture2D texture, bool flipX, bool flipY)
        {
            if (!flipX && !flipY)
            {
                return texture;
            }
            if (flipX)
            {
                for (int i = 0; i < texture.width / 2; i++)
                {
                    for (int j = 0; j < texture.height; j++)
                    {
                        Color tempC = texture.GetPixel(i, j);
                        texture.SetPixel(i, j, texture.GetPixel(texture.width - 1 - i, j));
                        texture.SetPixel(texture.width - 1 - i, j, tempC);
                    }
                }
            }
            if (flipY)
            {
                for (int i = 0; i < texture.width; i++)
                {
                    for (int j = 0; j < texture.height / 2; j++)
                    {
                        Color tempC = texture.GetPixel(i, j);
                        texture.SetPixel(i, j, texture.GetPixel(i, texture.height - 1 - j));
                        texture.SetPixel(i, texture.height - 1 - j, tempC);
                    }
                }
            }
            texture.Apply();
            return texture;
        }

        
    }
}