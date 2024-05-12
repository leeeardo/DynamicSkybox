Shader "Custom/Postprocess/Fog"
{
     Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off Zwrite Off ZTest Always
        HLSLINCLUDE

        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        struct appdata
        {
            float4 positionOS : POSITION;
            float2 texcoord : TEXCOORD0;
        };
        struct v2f
        {
            float2 uv : TEXCOORD0;
            float3 ray : TEXCOORD1;
            float4 vertex : SV_POSITION;
        };
                
        TEXTURE2D (_MainTex);
        float4 _MainTex_ST;
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);
        TEXTURE2D(_NoiseTexture);
        SAMPLER(sampler_NoiseTexture);
        float4 _NoiseTexture_ST;

        float3 _FrustumCorners[4];

        float _DistanceFogIntensity,_HeightFogIntensity;
        float _FogStartHeight,_FogEndHeight;
        float _FogNear,_FogFar;
        float _HeightDistanceBlend;

        TEXTURE2D(_HeightGradient);
        SAMPLER(sampler_HeightGradient);

        TEXTURE2D(_DistanceGradient);
        SAMPLER(sampler_DistanceGradient);

        v2f FogVert(appdata input)
        {
            v2f o;
            o.vertex = TransformObjectToHClip(input.positionOS.xyz);
            o.uv = input.texcoord;
            o.ray = _FrustumCorners[input.texcoord.x+2*input.texcoord.y];
            return o;
        }

        half4 FogFrag(v2f input) : SV_Target
        {
            float depthTex = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,input.uv).r;
            float depth = LinearEyeDepth(depthTex,_ZBufferParams);
            float depth01 = Linear01Depth(depthTex,_ZBufferParams);
            float3 worldPos = GetCameraPositionWS()+depth*input.ray;
            float noise = SAMPLE_TEXTURE2D(_NoiseTexture,sampler_NoiseTexture,worldPos.xz);

            float depthAbsolute = _ProjectionParams.y+(_ProjectionParams.z-_ProjectionParams.y)*depth01;
            
            float distanceFogDensity = saturate((depth-_FogNear)/(_FogFar-_FogNear));
            distanceFogDensity = saturate(pow(distanceFogDensity,_DistanceFogIntensity));

            float heightFogDensity = saturate((worldPos.y-_FogStartHeight*1000)/(_FogEndHeight*1000-_FogStartHeight));
            heightFogDensity = saturate(pow(heightFogDensity,_HeightFogIntensity));

            float4 heightFog = SAMPLE_TEXTURE2D(_HeightGradient,sampler_HeightGradient,float2(heightFogDensity,0.5));
            float4 distanceFog = SAMPLE_TEXTURE2D(_DistanceGradient,sampler_DistanceGradient,float2(distanceFogDensity,0.5));
            
            half4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
            half3 heightCol = lerp(col.rgb,heightFog.rgb,heightFogDensity*heightFog.a);
            half3 distanceCol = lerp(col.rgb,distanceFog.rgb,distanceFogDensity*distanceFog.a);
            //col.rgb = lerp(heightCol,distanceCol,_HeightDistanceBlend);

            
            float test = saturate(heightFogDensity*(1-heightFogDensity));
            //col.rgb = lerp(col.rgb,float3(0.5,0.5,0.5),fogDensity);
            //col.rgb =test*distanceCol;
            return test;
        }
        
        
        ENDHLSL

        Pass    //0
        {
            Name "Fog"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex FogVert
            #pragma fragment  FogFrag
            ENDHLSL
        }
        
         
    } 
}
