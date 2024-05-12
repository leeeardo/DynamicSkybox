using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomsPostProcessing/MyBlur")]
public class MyBlur : CustomVolumeComponent
{
    public BlurTypeParameter blurType = new BlurTypeParameter(BlurType.GaussianBlur);
    public GaussianFilerModeParameter filterMode = new GaussianFilerModeParameter(FilterMode.Bilinear);
    public ClampedIntParameter downSample = new ClampedIntParameter(2, 1, 8);
    
    public ClampedIntParameter iterations = new ClampedIntParameter(0, 0, 20);
    public ClampedFloatParameter blurSpread = new ClampedFloatParameter(0.6f, 0.2f, 3.0f);
    public ClampedFloatParameter blurRadius = new ClampedFloatParameter(1.5f, 0, 10);
    public ClampedFloatParameter area = new ClampedFloatParameter(1.55f, 0, 6);
    public ClampedFloatParameter spread = new ClampedFloatParameter(5.56f, 1, 16);
    public ClampedFloatParameter offset = new ClampedFloatParameter(0, -1, 1);
    
    public Vector2Parameter radialCenter = new Vector2Parameter(Vector2.zero);

    public ClampedFloatParameter angle = new ClampedFloatParameter(0,0,360);
    //Others
    public string ShaderPath = "Custom/Postprocess/BlurTest";
    private RenderTargetHandle tempRT0,tempRT1;
    private Material _material;
    public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        if (_material==null)
        {
            _material = CoreUtils.CreateEngineMaterial(ShaderPath);
        }
        tempRT0.Init("TempRT0");
        tempRT1.Init("TempRT1");
    }

    public override void DoRenderCmd(CommandBuffer cmd, ref RenderingData renderingData, RenderTargetIdentifier source,
        RenderTargetIdentifier destination)
    {
        if (_material==null)
        {
            return;
        }

        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        int rtW = descriptor.width/downSample.value;
        int rtH = descriptor.height/downSample.value;
        cmd.GetTemporaryRT(tempRT0.id,rtW,rtH,0,filterMode.value);
        cmd.GetTemporaryRT(tempRT1.id,rtW,rtH,0,filterMode.value);
        
        cmd.Blit(source,tempRT0.Identifier());
        switch (blurType.value)
        {
            case BlurType.GaussianBlur:
                GaussianBlur(cmd);
                break; 
            case BlurType.DualBlur:
                DualBlur(cmd, descriptor);
                break;
            case BlurType.BoxBlur:
                BoxBlur(cmd);
                break;
            case BlurType.KawaseBlur:
                KawaseBlur(cmd);
                break;
            case BlurType.BokehBlur:
                BokehBlur(cmd ,descriptor);
                break;
            case BlurType.TileShiftBlur:
                TileShiftBlur(cmd , descriptor);
                break;
            case BlurType.IrisBlur:
                IrisBlur(cmd , descriptor);
                break;
            case BlurType.GrainyBlur:
                GrainyBlur(cmd);
                break;
            case BlurType.RadialBlur:
                RadialBlur(cmd);
                break;
            case BlurType.DirectionalBlur:
                DirectionalBlur(cmd);
                break;
        }
        //GaussianBlur(cmd);

        //DualBlur(cmd, rtW, rtH);
        
        cmd.Blit(tempRT0.Identifier(),destination);

        cmd.ReleaseTemporaryRT(tempRT0.id);
        cmd.ReleaseTemporaryRT(tempRT1.id);
    }

    private void DirectionalBlur(CommandBuffer cmd)
    {
        float sinVal = (Mathf.Sin(Mathf.Deg2Rad*angle.value) * blurRadius.value * 0.05f) / iterations.value;
        float cosVal = (Mathf.Cos(Mathf.Deg2Rad*angle.value) * blurRadius.value * 0.05f) / iterations.value;
        _material.SetVector("_Params",new Vector4(
            iterations.value,blurRadius.value,sinVal,cosVal));
        
        cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,9);
        cmd.Blit(tempRT1.Identifier(), tempRT0.Identifier());
    }
    
    private void RadialBlur(CommandBuffer cmd)
    {
        _material.SetVector("_Params",new Vector4(
            iterations.value,blurRadius.value,radialCenter.value.x,radialCenter.value.y));
        
        cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,8);
        cmd.Blit(tempRT1.Identifier(), tempRT0.Identifier());
    }
    private void GrainyBlur(CommandBuffer cmd)
    {
        _material.SetVector("_Params",new Vector4(iterations.value,blurRadius.value,0,0));
        cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,7);
        cmd.Blit(tempRT1.Identifier(), tempRT0.Identifier());
    }
    
    private void IrisBlur(CommandBuffer cmd ,RenderTextureDescriptor descriptor)
    {
        float c = Mathf.Cos(2.39996323f);
        float s = Mathf.Sin(2.39996323f);
        Vector4 mGoldenRot = new Vector4(c, s, -s, c);
        
        _material.SetFloat("_Area" , area.value);
        _material.SetFloat("_Spread" , spread.value);
        _material.SetVector("_GoldenRot" , mGoldenRot);
        _material.SetVector("_Params" , new Vector4(
            iterations.value,blurRadius.value,1f/descriptor.width,1f/descriptor.height));
        cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,6);
        cmd.Blit(tempRT1.Identifier(),tempRT0.Identifier());
    }
    
    private void TileShiftBlur(CommandBuffer cmd , RenderTextureDescriptor descriptor)
    {
        float c = Mathf.Cos(2.39996323f);
        float s = Mathf.Sin(2.39996323f);
        Vector4 mGoldenRot = new Vector4(c, s, -s, c);
        
        _material.SetFloat("_Area" , area.value);
        _material.SetFloat("_Spread" , spread.value);
        _material.SetVector("_GoldenRot" , mGoldenRot);
        _material.SetFloat("_OFFset" , offset.value);
        _material.SetVector("_Params" , new Vector4(
            iterations.value,blurRadius.value,1f/descriptor.width,1f/descriptor.height));
        cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,5);
        cmd.Blit(tempRT1.Identifier(),tempRT0.Identifier());
    }
    private void BokehBlur(CommandBuffer cmd , RenderTextureDescriptor descriptor)
    {
        float c = Mathf.Cos(2.39996323f);
        float s = Mathf.Sin(2.39996323f);
        Vector4 mGoldenRot = new Vector4(c, s, -s, c);
        
        _material.SetVector("_GoldenRot" , mGoldenRot);
        _material.SetVector("_Params" , new Vector4(
            iterations.value,blurRadius.value,1f/descriptor.width,1f/descriptor.height));
        cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,4);
        cmd.Blit(tempRT1.Identifier(),tempRT0.Identifier());
    }

    
    private void DualBlur(CommandBuffer cmd , RenderTextureDescriptor descriptor)
    {
        descriptor.depthBufferBits = 0;

        _material.SetFloat("_offset" , blurRadius.value);
        int rtW = descriptor.width;
        int rtH = descriptor.height;

        int[] downSampleRT = new int[iterations.value];
        int[] upSampleRT = new int[iterations.value];
        for (int i = 0; i < iterations.value; i++)
        {
            downSampleRT[i] = Shader.PropertyToID("DownSample" + i);
            upSampleRT[i] = Shader.PropertyToID("UpSample" + i);
        }

        RenderTargetIdentifier tempRT = tempRT0.Identifier();
        //downSample
        for (int i = 0; i < iterations.value; i++)
        {
            cmd.GetTemporaryRT(downSampleRT[i],rtW,rtH,0,filterMode.value);
            cmd.GetTemporaryRT(upSampleRT[i],rtW,rtH,0,filterMode.value);
            rtW = Mathf.Max(rtW / 2, 1);
            rtH = Mathf.Max(rtH / 2, 1);
            cmd.Blit(tempRT,downSampleRT[i],_material,3);
            tempRT = downSampleRT[i];
        }
        
        //upSample
        for (int i = iterations.value-2; i >=0 ; i--)
        {
            cmd.Blit(tempRT,upSampleRT[i],_material,3);
            tempRT = upSampleRT[i];
        }
        
        cmd.Blit(tempRT,tempRT0.Identifier());

        for (int i = 0; i < iterations.value; i++)
        {
            cmd.ReleaseTemporaryRT(downSampleRT[i]);
            cmd.ReleaseTemporaryRT(upSampleRT[i]);
        }
    }

    private void BoxBlur(CommandBuffer cmd)
    {
        for (int i = 0; i < iterations.value; i++)
        {
            //setfloat
            _material.SetFloat("_BlurSize" , 1.0f+blurSpread.value*i);
            
            cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,2);
            CoreUtils.Swap(ref tempRT1,ref tempRT0);
        }
        cmd.Blit(tempRT1.Identifier(),tempRT0.Identifier());
    }
   
    private void GaussianBlur(CommandBuffer cmd)
    {
        for (int i = 0; i < iterations.value; i++)
        {
            //setfloat
            _material.SetFloat("_BlurSize" , 1.0f+blurSpread.value*i);
            
            cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,0);
            cmd.Blit(tempRT1.Identifier(),tempRT0.Identifier(),_material,1);
        }
    }

    private void KawaseBlur(CommandBuffer cmd)
    {
        
        for (int i = 0; i < iterations.value; i++)
        {
            _material.SetFloat("_offset" , blurRadius.value);
            cmd.Blit(tempRT0.Identifier(),tempRT1.Identifier(),_material,3);
            
            CoreUtils.Swap(ref tempRT1,ref tempRT0);
        }
    }

    
    public override bool IsActive()
    {
        return iterations.value!=0;
    }

    public override bool IsTileCompatible() => false;

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(_material);
    }
}

[Serializable]
public sealed class GaussianFilerModeParameter : VolumeParameter<FilterMode>
{
    public GaussianFilerModeParameter(FilterMode value, bool overrideState = false) : base(value, overrideState)
    {
        
    }
}

public enum BlurType
{
    GaussianBlur,DualBlur,BoxBlur,KawaseBlur,BokehBlur,TileShiftBlur,IrisBlur,GrainyBlur,RadialBlur,DirectionalBlur
}
[Serializable]
public sealed class BlurTypeParameter : VolumeParameter<BlurType>
{
    public BlurTypeParameter(BlurType value, bool overrideState = false) : base(value, overrideState)
    {
        
    }
}