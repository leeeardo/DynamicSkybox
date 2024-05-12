using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomsPostProcessing/Glitch")]
public class Glitch : CustomVolumeComponent
{
    //Params
    public GlitchRGBSplitDirectionParameter direction = new GlitchRGBSplitDirectionParameter(Direction.Horizontal);
    public ClampedFloatParameter indensity = new ClampedFloatParameter(0f, -1f, 1f);
    public ClampedFloatParameter speed = new ClampedFloatParameter(10f, 0f, 100f);
    
    private string _shaderPath = "Custom/Postprocess/Glitch/RGBSplit";
    private Material _material;
    public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

    private float TimeX = 1.0f;

    public override void Setup()
    {
        if (_material==null)
        {
            _material = CoreUtils.CreateEngineMaterial(_shaderPath);
        }
    }

    public override void DoRenderCmd(CommandBuffer cmd, ref RenderingData renderingData, RenderTargetIdentifier source,
        RenderTargetIdentifier destination)
    {
        if (_material ==null)
        {
            return;
        }
        
        TimeX += Time.deltaTime;
        if (TimeX > 100)
        {
            TimeX = 0;
        }
        _material.SetVector("_Params" , new Vector4(indensity.value*0.1f,Mathf.Floor(TimeX*speed.value)));
        
        cmd.Blit(source,destination,_material,(int)direction.value);
    }

    public override bool IsActive() => indensity.value != 0;

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_material);
    }
}

public enum Direction
{
    Horizontal,Vertical,HorizontalVertical
}
[Serializable]
public sealed class GlitchRGBSplitDirectionParameter : VolumeParameter<Direction>
{
    public GlitchRGBSplitDirectionParameter(Direction value, bool overrideState = false) : base(value, overrideState)
    {
        
    }
}