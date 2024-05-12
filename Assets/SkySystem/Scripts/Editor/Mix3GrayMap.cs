using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class Mix3GrayMap : EditorWindow
{
    [MenuItem("Customs/Mix4GrayScaleTex")]
    public static void AddWindow()
    {
        EditorWindow window = GetWindow<Mix3GrayMap>("做云专用");
        window.position = new Rect(200, 150, 350, 200);
        window.Show();
    }

    private Texture2D mapForR,mapForG,mapForB,mapForA;
    
    private string path="Assets/SkySystem/test/TestMap";
    private string fileName;
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("R通道图",GUILayout.Width(50));
        mapForR = EditorGUILayout.ObjectField(mapForR, typeof(Texture2D))as Texture2D;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("G通道图",GUILayout.Width(50));
        mapForG = EditorGUILayout.ObjectField(mapForG, typeof(Texture2D))as Texture2D;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("B通道图",GUILayout.Width(50));
        mapForB = EditorGUILayout.ObjectField(mapForB, typeof(Texture2D))as Texture2D;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("A通道图",GUILayout.Width(50));
        mapForA = EditorGUILayout.ObjectField(mapForA, typeof(Texture2D))as Texture2D;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("路径",GUILayout.Width(50));
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(290));
            path = EditorGUI.TextField(rect,path);
            if (rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (Event.current.type== EventType.DragExited)
                {
                    if (DragAndDrop.paths!=null&& DragAndDrop.paths.Length>0)
                    {
                        path = DragAndDrop.paths[0];
                    }
                }
            }
        }
        GUILayout.EndHorizontal();
        fileName = EditorGUILayout.TextField(fileName);
        GUILayout.Space(20);
        if (GUILayout.Button("合成"))
        {
            if (mapForR == null || mapForG == null || mapForB == null || mapForA==null)
            {
                Debug.Log("map不全");
                return;
            }

            Texture2D map = MixTexture(mapForR, mapForG, mapForB,mapForA);
            if (path!=null)
            {
                SaveTexture2DFile(map, path+"/"+fileName+".png");
                map.name = "cloud2";
                AssetDatabase.Refresh();
            }
        }
    }

    private Texture2D MixTexture(Texture2D mapA, Texture2D mapB, Texture2D mapC,Texture2D mapD)
    {
        Color[] mapAColor= mapA.GetPixels();
        Color[] mapBColor= mapB.GetPixels();
        Color[] mapCColor= mapC.GetPixels();
        Color[] mapDColor = mapD.GetPixels();

        if (mapAColor.Length != mapBColor.Length || mapAColor.Length != mapCColor.Length)
        {
            Debug.Log("三张图分辨率不一致");
        }
        
        for (int i = 0; i < mapAColor.Length; i++)
        {
            mapAColor[i].g = mapBColor[i].g;
            mapAColor[i].b = mapCColor[i].b;
            mapAColor[i].a = mapDColor[i].r;
        }
        Texture2D map = new Texture2D(mapA.width,mapA.height,TextureFormat.RGBA64,true);
        map.SetPixels(mapAColor);
        map.Apply();
        return map;
    }
    private void SaveTexture2DFile(Texture2D texture, string path)
    {
        string fullPath = path;
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
            
        byte[] vs = texture.EncodeToPNG();
        FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        fileStream.Write(vs , 0 , vs.Length);
        fileStream.Dispose();
        fileStream.Close();
        Debug.Log("succeed save"+fullPath);
    }
    
}
