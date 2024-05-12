using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomsPostProcessing/Scan")]
public class Scan : CustomVolumeComponent
{
    //time controller
    public ClampedFloatParameter effectTimeSpan = new ClampedFloatParameter(5, 0, 10);
    public ClampedFloatParameter fadeOutTime = new ClampedFloatParameter(4, 0, 10);
    public ClampedFloatParameter scanSpeed = new ClampedFloatParameter(35,0,300);
    
    //scan parameters
    public ClampedFloatParameter scanWidth = new ClampedFloatParameter(300,0,1000);
    public ClampedFloatParameter scanLineSmoothness = new ClampedFloatParameter(1,0,1);
    public ColorParameter scanLineColor = new ColorParameter(Color.blue,true,true,true);
    
    //decoration
    public ClampedFloatParameter dotDensity = new ClampedFloatParameter(1, 0.1f, 10);
    public ClampedFloatParameter dotSize = new ClampedFloatParameter(0.25f, 0, 1);
    public ClampedFloatParameter isoHeightDensity = new ClampedFloatParameter(0.1f, 0.001f, 1);
    public ClampedFloatParameter isoHeightThickness = new ClampedFloatParameter(0.1f, 0.001f, 1);
    //outline
    public BoolParameter outLine = new BoolParameter(true);
    public ClampedFloatParameter outlineThickness = new ClampedFloatParameter(0.8f, 0.001f, 1f);
    
    //inside Color
    public ClampedFloatParameter decolorationStrength = new ClampedFloatParameter(1, 0, 1);
    public ClampedFloatParameter insideBrightness = new ClampedFloatParameter(1, 0, 1);
    
    //glitch strength
    public ClampedFloatParameter glitchStrength = new ClampedFloatParameter(1, -5f, 5);
    
    public BoolParameter fresnel = new BoolParameter(true);
    public BoolParameter glitch = new BoolParameter(true);

    public TextureParameter dotTexture = new TextureParameter(null);
    
    public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;
    //
    public const string ShaderName = "Custom/Postprocess/Scan";
    private Material _material;
    private Camera _camera;
    
    public override void Setup()
    {
        if (_material == null)
        {
            _material = CoreUtils.CreateEngineMaterial(ShaderName);
        }

    }
    
    
    public override void DoRenderCmd(
        CommandBuffer cmd, ref RenderingData renderingData,RenderTargetIdentifier source, RenderTargetIdentifier destination)
    {
        if (_material == null)
        {
            return;
        }
        _camera = renderingData.cameraData.camera;
        // _camera.CalculateFrustumCorners(
        //     new Rect(0f,0f,1f,1f),_camera.farClipPlane,_camera.stereoActiveEye,_outCorners);
        // _vectorArray[0] = _outCorners[0];
        // _vectorArray[1] = _outCorners[3];
        // _vectorArray[2] = _outCorners[1];
        // _vectorArray[3] = _outCorners[2];
        var aspect = _camera.aspect;
        var far = _camera.farClipPlane;
        var right = _camera.transform.right;
        var up = _camera.transform.up;
        var forward = _camera.transform.forward;
        var halfFovTan = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
 
        //计算相机在远裁剪面处的xyz三方向向量
        var rightVec = right * far * halfFovTan * aspect;
        var upVec = up * far * halfFovTan;
        var forwardVec = forward * far;
 
        //构建四个角的方向向量
        var topLeft = (forwardVec - rightVec + upVec);
        var topRight = (forwardVec + rightVec + upVec);
        var bottomLeft = (forwardVec - rightVec - upVec);
        var bottomRight = (forwardVec + rightVec - upVec);
 
        var viewPortRay = Matrix4x4.identity;
        viewPortRay.SetRow(0, topLeft);
        viewPortRay.SetRow(1, topRight);
        viewPortRay.SetRow(2, bottomLeft);
        viewPortRay.SetRow(3, bottomRight);
        _material.SetMatrix("_FrustumCorners",viewPortRay);

        _material.SetFloat("_ScanWidth" , scanWidth.value);
        _material.SetFloat("_ScanSpeed",scanSpeed.value);
        _material.SetFloat("_ScanSmoothness",scanLineSmoothness.value);

        _material.SetTexture("_GridTexture",dotTexture.value);
        _material.SetFloat("_DotDensity",dotDensity.value);
        _material.SetFloat("_DotSize",dotSize.value);
        _material.SetColor("_ScanLineColor",scanLineColor.value);
        _material.SetFloat("_IsoHeightDensity",isoHeightDensity.value);
        _material.SetFloat("_IsoHeightThickness",isoHeightThickness.value);
        
        _material.SetFloat("_EffectTimeSpan",effectTimeSpan.value);
        _material.SetFloat("_FadeOutTime" , fadeOutTime.value);
        
        EnableKeyWorld("_OUTLINE",outLine.value);
        EnableKeyWorld("_FRESNEL",fresnel.value);
        EnableKeyWorld("_GLITCH",glitch.value);
        
        _material.SetFloat("_OutlineThickness",outlineThickness.value);
        _material.SetFloat("_DecolorationStrength",decolorationStrength.value);
        _material.SetFloat("_InsideBrightness",insideBrightness.value);
        _material.SetFloat("_GlitchStrength",glitchStrength.value);
        
        cmd.Blit(source,destination,_material,0);
    }

    void EnableKeyWorld(string keyword, bool enable)
    {
        if (enable)
        {
            _material.EnableKeyword(keyword);
        }
        else
        {
            _material.DisableKeyword(keyword);
        }
    }
    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_material);
    }

    public override bool IsActive() => true;

    public override bool IsTileCompatible() => false;
}


