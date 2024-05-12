using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SkySystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
[CreateAssetMenu(fileName = "SkySystemDefaultData",menuName = "ScriptableObjects/CreateSkySystemData")]
public class SkySystemData : ScriptableObject
{
    
    public float hour;
    public bool timeControlEverything;
    //sky
    public Gradient daySkyGradient;
    public Gradient nightSkyGradient;

    public Gradient fogColorGradient;
    //sun
    public Gradient sunDiscGradient;
    public Vector2 sunRotation;
    public Vector4 sunHalo;
    public float sunIntensity;
    public Gradient sunColorGradient;
    //moon
    public Texture2D moonTexture;
    public Vector2 moonRotation;
    public float moonIntensity;
    public Gradient moonColorGradient;
    public Texture starTexture;
    public float starIntensity;
    public float moonDistance;
    //lighting 
    public float lightIntensity;
    public Gradient sunLightGradient;
    public Gradient moonLightGradient;
    public Vector2 lightRotation;
    //Cloud
    public Color tint;
    public Color cloudTopColor;
    public Color cloudBottomColor;
    public float GIIndex;

    //probe
    public AmbientMode ambientMode;
    public ReflectionProbeMode mode;
    public Texture cubemap;
    public bool boxProjection;
    public ReflectionResolution resolution;
    public bool HDR;
    public ReflectionProbeClearFlags clearFlags= ReflectionProbeClearFlags.Skybox;
    public int cullingMask;
    public void SaveSystemData()
    {
        string path = Application.streamingAssetsPath + "/TestData.json";
        SaveSystemData(path);
    }
    public void SaveSystemData(string path)
    {
        string str = JsonUtility.ToJson(this);
        Debug.Log(str);
        Debug.Log(path);
        File.WriteAllText(path,str);
        AssetDatabase.Refresh();
        Debug.Log("SaveDataTo"+path);
    }
    
    public void LoadSystemData()
    {
        string path = Application.streamingAssetsPath + "/TestData.json";
        LoadSystemData(path);
    }

    public void LoadSystemData(string path)
    {
        if (File.Exists(path))
        {
            string str = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(str,this);
            Debug.Log("LoadDataFrom"+path);
        }
        else
        {
            Debug.Log(path+"Data Not Exits");
        }
    }
}