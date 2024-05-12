using System;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System.Reflection;
using UnityEngine.Rendering.Universal;
using System.Linq;
using UnityEngine.Rendering;
using Assembly = System.Reflection.Assembly;

public class CustomPostRendererFeature : ScriptableRendererFeature
{
    private CustomPostRenderPass afterOpaqueAndSky;
    private CustomPostRenderPass beforePostProcess;
    private CustomPostRenderPass afterPostProcess;
    
    private List<CustomVolumeComponent> components ;

    private RenderTargetHandle afterPostProcessTexture;
    
    public override void Create()
    {
        var stack = VolumeManager.instance.stack;
        components = VolumeManager.instance.baseComponentTypeArray.Where(
                t => t.IsSubclassOf(typeof(CustomVolumeComponent)) && stack.GetComponent(t) != null)
                .Select(t => stack.GetComponent(t) as CustomVolumeComponent).ToList();

        var afterOpaqueAndSkyComponents = components
            .Where(c => c.InjectionPoint == CustomVolumeComponent.CustomPostProcessInjectionPoint.AfterOpaqueAndSky)
            .OrderBy(c => c.orderInPass)
            .ToList();
        afterOpaqueAndSky = new CustomPostRenderPass(afterOpaqueAndSkyComponents, "CustomPostProcess-AfterOpaqueAndSky");
        afterOpaqueAndSky.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        
        var beforePostProcessComponents = components
            .Where(c => c.InjectionPoint == CustomVolumeComponent.CustomPostProcessInjectionPoint.BeforePostProcess)
            .OrderBy(c => c.orderInPass)
            .ToList();
        beforePostProcess = new CustomPostRenderPass(beforePostProcessComponents, "CustomPostProcess-BeforePostProcess");
        beforePostProcess.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        var afterPostProcessComponents = components
            .Where(c => c.InjectionPoint == CustomVolumeComponent.CustomPostProcessInjectionPoint.AfterPostProcess)
            .OrderBy(c => c.orderInPass)
            .ToList();
        afterPostProcess = new CustomPostRenderPass(afterPostProcessComponents,"CustomPostProcess-AfterPostProcess");
        afterPostProcess.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.postProcessEnabled)
        {
            var source = new RenderTargetHandle(renderer.cameraColorTarget);
            if (afterOpaqueAndSky.SetupComponents())
            {
                afterPostProcessTexture.Init("_CameraColorAttachmentA");
                afterOpaqueAndSky.Setup(source,afterPostProcessTexture);
                renderer.EnqueuePass(afterOpaqueAndSky);
            }

            if (beforePostProcess.SetupComponents())
            {
                beforePostProcess.Setup(source,source);
                renderer.EnqueuePass(beforePostProcess);
            }

            if (afterPostProcess.SetupComponents())
            {
                afterPostProcessTexture.Init("_CameraColorAttachmentB");
                source = renderingData.cameraData.resolveFinalTarget ? afterPostProcessTexture : source;
                afterPostProcess.Setup(source,source);
                renderer.EnqueuePass(afterPostProcess);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && components != null)
        {
            foreach (var item in components)
            {
                item.Dispose();
            }
        }
    }
}



