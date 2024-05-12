using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomsPostProcessing/OutLine")]
public class OutLine : CustomVolumeComponent
{
    //parameter
    public ColorParameter colorChange = new ColorParameter(Color.white, true);
    public override CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    //
    public const string ShaderName = "Custom/Postprocess/OutLine";
    private Material renderMaterial;

    public override void Setup()
    {
        if (renderMaterial == null)
        {
            renderMaterial = CoreUtils.CreateEngineMaterial(ShaderName);
        }
    }

    public override void DoRenderCmd(
        CommandBuffer cmd, ref RenderingData renderingData,RenderTargetIdentifier source, RenderTargetIdentifier destination)
    {
        if (renderMaterial == null)
        {
            return;
        }

        renderMaterial.SetColor("_ColorTint" , colorChange.value);

        cmd.Blit(source,destination,renderMaterial);
    }

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(renderMaterial);
    }

    public override bool IsActive() => colorChange.value != Color.white;

    public override bool IsTileCompatible() => false;
}
