using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkySystem
{
    [Serializable]
    public class ProbeElement : BaseElement
    {
        //private
        private ReflectionProbe _probe;
        private Camera renderCam;

        public AmbientMode ambientMode
        {
            get => RenderSettings.ambientMode;
            set{RenderSettings.ambientMode = value;}
        }
        //public
        public ReflectionProbeMode mode
        {
            get => _probe.mode;
            set { _probe.mode = value; }
        }
        public Texture cubemap
        {
            get => _probe.customBakedTexture;
            set { _probe.customBakedTexture = value; }
        }
        public bool boxProjection
        {
            get => _probe.boxProjection;
            set { _probe.boxProjection = value; }
        }
        public Vector3 boxSize
        {
            get => _probe.size;
            set { _probe.size = value; }
        }
        public int resolution
        {
            get => _probe.resolution;
            set { _probe.resolution = value; }
        }
        public bool HDR
        {
            get => _probe.hdr;
            set { _probe.hdr = value; }
        }
        public ReflectionProbeClearFlags clearFlags
        {
            get => _probe.clearFlags;
            set { _probe.clearFlags = value; }
        }
        public int cullingMask
        {
            get => _probe.cullingMask;
            set { _probe.cullingMask = value; }
        }

        /// <summary>
        /// 未完成，todo 异步渲染当前探针
        /// </summary>
        public void Bake()
        {
            _probe.RenderProbe();
        }
        
        public ProbeElement(SkySystemData data)
        {
            GameObject obj = GameObject.Find("SkySystem_ReflectionProbe");
            if (obj!=null)
            {
                _probe = obj.GetComponent<ReflectionProbe>();
            }
            else
            {
                Debug.Log("No probeObj");
            }
            LoadData(data);
            _probe.mode = mode;
            _probe.size = Vector3.one * 10000;
            _probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
        }

        public void LoadData(SkySystemData data)
        {
            ambientMode = data.ambientMode;
            mode = data.mode;
            cubemap = data.cubemap;
            boxProjection = data.boxProjection;
            resolution = (int)data.resolution;
            HDR = data.HDR;
            clearFlags = data.clearFlags;
            cullingMask = data.cullingMask;
        }

        public void RenderSkybox(string path)
        {
            string texturePath = path;
            if (!Directory.Exists(texturePath))
            {
                Directory.CreateDirectory(texturePath);
            }
            TextureFormat format = _probe.hdr ? TextureFormat.RGBAFloat : TextureFormat.RGBA64;
            Cubemap tempCubemap = new Cubemap(_probe.resolution,format,false);
            
            GameObject camObj = new GameObject("RenderCam");
            renderCam = camObj.AddComponent<Camera>();
            camObj.transform.position = Vector3.zero;
            InitCamSetting();
            //renderCam.enabled = true;
            renderCam.RenderToCubemap(tempCubemap);
            Texture2D tex = GetTexture2DByCubeMap(tempCubemap, format);
            SaveTexture2DFile(tex, texturePath+"/Skybox_"+SkySystem.Instance.Hour+".png");
            AssetDatabase.Refresh();
            SetTextureAsCubemap(texturePath);
            //收尾工作
            AssetDatabase.Refresh();
            //renderCam.enabled = false;
            renderCam = null;
            GameObject.DestroyImmediate(camObj);
        }
        
        private void InitCamSetting()
        {
            
            renderCam.cameraType = CameraType.Reflection;
            renderCam.hideFlags = HideFlags.HideAndDontSave;
            renderCam.gameObject.SetActive(true);
            renderCam.fieldOfView = 90;
            renderCam.farClipPlane = _probe.farClipPlane;
            renderCam.nearClipPlane = _probe.nearClipPlane;
            renderCam.clearFlags = (CameraClearFlags)_probe.clearFlags;
            renderCam.backgroundColor = _probe.backgroundColor;
            renderCam.allowHDR = _probe.hdr;
            renderCam.cullingMask = _probe.cullingMask;
            renderCam.enabled = false;
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
        }
        private Texture2D GetTexture2DByCubeMap(Cubemap cubemap , TextureFormat format)
        {
            int everyW = cubemap.width;
            int everyH = cubemap.height;

            Texture2D texture2D = new Texture2D(everyW * 4, everyH * 3,format,false);
            texture2D.SetPixels(everyW, 0, everyW, everyH, cubemap.GetPixels(CubemapFace.PositiveY));
            texture2D.SetPixels(0, everyH, everyW, everyH, cubemap.GetPixels(CubemapFace.NegativeX));
            texture2D.SetPixels(everyW, everyH, everyW, everyH, cubemap.GetPixels(CubemapFace.PositiveZ));
            texture2D.SetPixels(2 * everyW, everyH, everyW, everyH, cubemap.GetPixels(CubemapFace.PositiveX));
            texture2D.SetPixels(3 * everyW, everyH, everyW, everyH, cubemap.GetPixels(CubemapFace.NegativeZ));
            texture2D.SetPixels(everyW, 2 * everyH, everyW, everyH, cubemap.GetPixels(CubemapFace.NegativeY));
            texture2D.Apply();
            texture2D = FlipPixels(texture2D, false, true);
            return texture2D;
        }
        
        private Texture2D FlipPixels(Texture2D texture, bool flipX, bool flipY)
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
        private void SetTextureAsCubemap(string path)
        {
            string[] paths = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);

            for (int i = 0; i < paths.Length; i++)
            {
                string assetPath = paths[i].Substring(path.IndexOf("Assets/"));
                Debug.Log(assetPath);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                importer.textureShape = TextureImporterShape.TextureCube;
                AssetDatabase.ImportAsset(assetPath);
            }
        }
    }
}