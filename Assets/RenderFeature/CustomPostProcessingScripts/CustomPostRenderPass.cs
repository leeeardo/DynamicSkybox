using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class CustomPostRenderPass : ScriptableRenderPass
{
    private List<CustomVolumeComponent> components;
    private List<int> activeComponents;

    public string ProfileName ;

    //private List<ProfilingSampler> profilingSamplers;
    //public const string TempBuffer = "_CustomPostBuffer";
        
    private RenderTargetHandle  source;
    private RenderTargetHandle  destination;
    private RenderTargetHandle  tempRT0;
    private RenderTargetHandle  tempRT1;

    public CustomPostRenderPass(List<CustomVolumeComponent> customVolumeComponent,string profileName)
    {
        this.ProfileName = profileName;
        this.components = customVolumeComponent;
        activeComponents = new List<int>(customVolumeComponent.Count);
        
        tempRT0.Init("TemporaryRenderTexture0");
        tempRT1.Init("TemporaryRenderTexture1");
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get(ProfileName);
        //context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.msaaSamples = 1;
        descriptor.depthBufferBits = 0;

        RenderTargetIdentifier buff0, buff1;
        bool rt1Used = false;
        cmd.GetTemporaryRT(tempRT0.id,descriptor);
        buff0 = tempRT0.id;
        

        if (activeComponents.Count ==1)
        {
            int index = activeComponents[0];
            
            cmd.BeginSample(components[index].GetType().ToString());
            components[index].DoRenderCmd(cmd,ref renderingData,source.Identifier(),buff0);
            cmd.EndSample(components[index].GetType().ToString());
        }
        else
        {
            cmd.GetTemporaryRT(tempRT1.id,descriptor);
            buff1 = tempRT1.id;
            rt1Used = true;
            Blit(cmd,source.Identifier(),buff0);

            for (int i = 0; i < activeComponents.Count; i++)
            {
                int index = activeComponents[i];
                var component = components[index];
                
                cmd.BeginSample(component.GetType().ToString());
                component.DoRenderCmd(cmd,ref renderingData,buff0,buff1);
                cmd.EndSample(component.GetType().ToString());
                
                CoreUtils.Swap(ref buff0,ref buff1);
            }
        }
        Blit(cmd,buff0,destination.Identifier());
        
        cmd.ReleaseTemporaryRT(tempRT0.id);
        if (rt1Used)
        {
            cmd.ReleaseTemporaryRT(tempRT1.id);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public bool SetupComponents()
    {
        activeComponents.Clear();
        for (int i = 0; i < components.Count; i++)
        {
            components[i].Setup();
            if (components[i].IsActive())
            {
                activeComponents.Add(i);
            }
        }
        return activeComponents.Count != 0;
    }

    public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
    {
        this.source = source;
        this.destination = destination;
    }
}


