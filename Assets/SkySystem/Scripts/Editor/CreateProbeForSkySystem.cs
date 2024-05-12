using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.LookDev;

namespace SkySystem
{
    public class CreateProbeForSkySystem : EditorWindow
    {
        [MenuItem("Customs/CreateProbeForSkySystem")]
        static void AddWindow()
        {
            EditorWindow window =  GetWindow<CreateProbeForSkySystem>("SkySystemSettings");
            window.position = new Rect(50f, 50f, 300, 500);
            window.Show();
        }

        private Camera _camera;
        private ReflectionProbe _reflectionProbe;
        private static string path = "Assets/SkySystem";
        private static Quaternion[] orientations = new Quaternion[]
        {
            Quaternion.LookRotation(Vector3.right, Vector3.down),
            Quaternion.LookRotation(Vector3.left, Vector3.down),
            Quaternion.LookRotation(Vector3.up, Vector3.forward),
            Quaternion.LookRotation(Vector3.down, Vector3.back),
            Quaternion.LookRotation(Vector3.forward, Vector3.down),
            Quaternion.LookRotation(Vector3.back, Vector3.down)
        };

        private SkySystem _skySystem;
        private Cubemap _cubemap;
        
        private GUIStyle _style = new GUIStyle();
        private void OnGUI()
        {
            _style.alignment = TextAnchor.MiddleCenter;
            _style.fontSize = 20;
            _style.margin = new RectOffset(0, 0, 10, 10);
            _style.normal.textColor = Color.white;
            GUILayout.Label("动态天空盒系统---辅助",_style);
            _style.fontSize = 15;
            if (GUILayout.Button( "Create SkySystem",GUILayout.Height(30)))
            {
                if (GameObject.FindObjectOfType(typeof(SkySystem)) != null)
                {
                    Debug.Log("存在同名SkySystem");
                    return;
                }

                Debug.Log("创建Sky System");
                GameObject obj = new GameObject();
                obj.name = "SkySystem";
                //创建天空盒系统
                _skySystem = obj.AddComponent<SkySystem>();
                _reflectionProbe = GameObject.Find("SkySystem_ReflectionProbe").GetComponent<ReflectionProbe>();

                GameObject camObj = new GameObject("RenderCamera");
                camObj.transform.SetParent(GameObject.Find("SkySystem").transform);
                camObj.transform.position = Vector3.zero;
                camObj.transform.rotation = Quaternion.identity;
                _camera = camObj.AddComponent<Camera>();
            }

            SkySystem.Instance.Hour = GUILayout.HorizontalSlider(SkySystem.Instance.Hour, 0, 24);
            
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            //获取想要保存天空盒的路径
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("路径",GUILayout.Width(100));
                //获得一个长300的框
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(400));
                //将上面的框作为文本输入框
                path = EditorGUI.TextField(rect, path);
            
                //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内
                if ((Event.current.type == EventType.DragUpdated
                     || Event.current.type == EventType.DragExited)
                    && rect.Contains(Event.current.mousePosition))
                {
                    //改变鼠标的外表
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                    {
                        path = DragAndDrop.paths[0];
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("获取当前天空盒保存在上述路径",GUILayout.Height(25)))
            {
                //创建目录
                string texturePath = path;
                if (!Directory.Exists(texturePath))
                {
                    Directory.CreateDirectory(texturePath);
                }
                
                //创建GameObject和Camera
                //初始化相机并失活
                InitCamSetting();

                TextureFormat format = _reflectionProbe.hdr ? TextureFormat.RGBAFloat : TextureFormat.RGBA64;
                Cubemap cubemap = new Cubemap(_reflectionProbe.resolution,format,false);

                _camera.enabled = true;
                
                _camera.RenderToCubemap(cubemap);
                Texture2D tex = GetTexture2DByCubeMap(cubemap, format);
                SaveTexture2DFile(tex, texturePath+"/Skybox_"+SkySystem.Instance.Hour+".png");
                
                AssetDatabase.Refresh();
                SetTextureAsCubemap(texturePath);
                //收尾工作
                DestroyImmediate(cubemap);
                cubemap = null;
                AssetDatabase.Refresh();
                
            }
            
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
        private void InitCamSetting()
        {
            if (_camera==null)
            {
                _camera = GameObject.Find("RenderCamera").GetComponent<Camera>();
            }
            _camera.cameraType = CameraType.Reflection;
            _camera.hideFlags = HideFlags.HideAndDontSave;
            _camera.gameObject.SetActive(true);
            _camera.fieldOfView = 90;
            _camera.farClipPlane = _reflectionProbe.farClipPlane;
            _camera.nearClipPlane = _reflectionProbe.nearClipPlane;
            _camera.clearFlags = (CameraClearFlags)_reflectionProbe.clearFlags;
            _camera.backgroundColor = _reflectionProbe.backgroundColor;
            _camera.allowHDR = _reflectionProbe.hdr;
            _camera.enabled = false;
            _camera.cullingMask = 0;
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
        public static Texture2D GetTexture2DByCubeMap(Cubemap cubemap , TextureFormat format)
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

