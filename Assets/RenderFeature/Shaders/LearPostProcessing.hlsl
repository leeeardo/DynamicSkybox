#ifndef _LEAR_POSTPROCESSING
#define _LEAR_POSTPROCESSING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct appdata
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

float Rand(float2 seed)
{
    return frac(sin(dot(seed, float2(12.9898,78.233)))*43578.5453);   
}  

v2f DefaultVert(appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.positionOS.xyz);
    o.uv = v.texcoord;
    return o;
}

#endif
