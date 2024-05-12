using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomsPostProcessing/Fog")]
public class Fog : CustomVolumeComponent
{
    //parameter
    public BoolParameter activeHeightFog = new BoolParameter(false);
    
    public FloatParameter fogStartHeight = new FloatParameter(0);
    public FloatParameter fogEndHeight = new FloatParameter(1000);
    public ClampedFloatParameter heightFogIntensity = new ClampedFloatParameter(0.5f, 0f, 10f);
    public GradientParameter heightGradient = new GradientParameter(new Gradient());
    
    public BoolParameter activeDistanceFog = new BoolParameter(false);
    public FloatParameter fogNear = new FloatParameter(0);
    public FloatParameter fogFar = new FloatParameter(150);
    public ClampedFloatParameter distanceFogIntensity = new ClampedFloatParameter(0.5f, 0f, 10f);
    public GradientParameter distanceGradient = new GradientParameter(new Gradient());

    public ClampedFloatParameter heightDistanceBlend = new ClampedFloatParameter(0.5f, 0f, 1f);

    private Texture2D heightTex;
    private Texture2D distanceTex;
    
    public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

    public override int orderInPass => 2;

    //
    public const string ShaderName = "Custom/Postprocess/Fog";
    private Material _material;
    private Vector3[] _outCorners;
    private Camera _camera;
    private Vector4[] _vectorArray;
    public override void Setup()
    {
        if (_material == null)
        {
            _material = CoreUtils.CreateEngineMaterial(ShaderName);
        }

        _vectorArray = new Vector4[4];
        // heightTex = new Texture2D(256, 1, TextureFormat.ARGB32, false, true);
        // heightTex.filterMode = FilterMode.Bilinear;
        // heightTex.wrapMode = TextureWrapMode.Clamp;
        // heightTex.anisoLevel = 1;
        //SetTexture(out heightTex);
        //SetTexture(out distanceTex);
    }
    

    Texture2D applyGradient(Gradient ramp)
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
    
    public override void DoRenderCmd(
        CommandBuffer cmd, ref RenderingData renderingData,RenderTargetIdentifier source, RenderTargetIdentifier destination)
    {
        if (_material == null)
        {
            return;
        }

        _outCorners = new Vector3[4];
        _camera = renderingData.cameraData.camera;
        _camera.CalculateFrustumCorners(
            new Rect(0f,0f,1f,1f),_camera.farClipPlane,_camera.stereoActiveEye,_outCorners);
        _vectorArray[0] = _outCorners[0];
        _vectorArray[1] = _outCorners[3];
        _vectorArray[2] = _outCorners[1];
        _vectorArray[3] = _outCorners[2];
        _material.SetVectorArray("_FrustumCorners",_vectorArray);
        _material.SetFloat("_HeightDistanceBlend",heightDistanceBlend.value);
        if (activeDistanceFog.value)
        {
            _material.SetFloat("_FogFar",fogFar.value);
            _material.SetFloat("_FogNear",fogNear.value);
            _material.SetFloat("_DistanceFogIntensity",distanceFogIntensity.value);
            
            distanceTex = applyGradient(distanceGradient.value);
            
            _material.SetTexture("_DistanceGradient" , distanceTex);
        }
        else
        {
            _material.SetFloat("_DistanceFogIntensity",0);
        }

        if (activeHeightFog.value)
        {
            _material.SetFloat("_FogEndHeight",fogEndHeight.value);
            _material.SetFloat("_FogStartHeight",fogStartHeight.value);
            _material.SetFloat("_HeightFogIntensity",heightFogIntensity.value);
            

            heightTex = applyGradient(heightGradient.value);
            
            _material.SetTexture("_HeightGradient" , heightTex);
        }
        else
        {
            _material.SetFloat("_HeightFogIntensity",0);
        }

        cmd.Blit(source,destination,_material,0);

    }

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_material);
    }

    public override bool IsActive() => activeDistanceFog.value|| activeHeightFog.value ;

    public override bool IsTileCompatible() => false;
}

[Serializable]
public sealed class GradientParameter : VolumeParameter<Gradient>
{
    public GradientParameter(Gradient value, bool overrideState = false) : base(value, overrideState)
    {
        
    }
}
