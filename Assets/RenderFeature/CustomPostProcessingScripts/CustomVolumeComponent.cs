using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public abstract class CustomVolumeComponent : VolumeComponent,IPostProcessComponent,IDisposable
{
    public enum CustomPostProcessInjectionPoint
    {
        AfterOpaqueAndSky,BeforePostProcess,AfterPostProcess
    }

    public virtual  int orderInPass => 0;
    public virtual CustomPostProcessInjectionPoint InjectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public abstract void Setup();
    public abstract void DoRenderCmd( CommandBuffer cmd, ref RenderingData renderingData, RenderTargetIdentifier source, RenderTargetIdentifier destination );

    public abstract bool IsActive();

    public virtual bool IsTileCompatible() => false;
    
    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual void Dispose(bool disposing) { }
    #endregion
}
