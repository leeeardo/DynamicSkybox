Shader "Custom/Postprocess/Glitch/RGBSplit"
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
        #include "LearPostProcessing.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float2 _Params;
        CBUFFER_END
    
        TEXTURE2D (_MainTex);
        SAMPLER(sampler_MainTex);

        #define _Indensity _Params.x
        #define _TimeX _Params.y

        half4 GlitchRGBSplitHorizontalFrag(v2f input) : SV_Target
        {
            float splitAmount = _Indensity * Rand(float2(_TimeX,2));

            half4 ColorR = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv.x +splitAmount,input.uv.y));
            half4 ColorG = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , input.uv);
            half4 ColorB = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv.x -splitAmount,input.uv.y));

            return half4(ColorR.r,ColorG.g,ColorB.b,1);
        }

        half4 GlitchRGBSplitVerticalFrag(v2f input) : SV_Target
        {
            float splitAmount = _Indensity * Rand(float2(_TimeX,2));

            half4 ColorR = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv.x ,input.uv.y+splitAmount));
            half4 ColorG = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , input.uv);
            half4 ColorB = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv.x ,input.uv.y-splitAmount));
            
            return half4(ColorR.r,ColorG.g,ColorB.b,1);
        }

        half4 GlitchRGBSplit_HV_Frag(v2f input) : SV_Target
        {
            float splitAmount = _Indensity * Rand(float2(_TimeX,2));

            half4 ColorR = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv.x +splitAmount,input.uv.y+splitAmount));
            half4 ColorG = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , input.uv);
            half4 ColorB = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv.x -splitAmount,input.uv.y+splitAmount));

            return half4(ColorR.r,ColorG.g,ColorB.b,1);
        }
        
        ENDHLSL

        Pass    //0
        {
            Name "GlitchRGBSplitHorizontal"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex DefaultVert
            #pragma fragment  GlitchRGBSplitHorizontalFrag
            ENDHLSL
        }
        
         Pass   //1
        {
            Name "GlitchRGBSplitVertical"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex DefaultVert
            #pragma fragment GlitchRGBSplitVerticalFrag
            ENDHLSL
        }
        
        Pass   //2
        {
            Name "GlitchRGBSplitHV"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex DefaultVert
            #pragma fragment GlitchRGBSplit_HV_Frag
            ENDHLSL
        }
    } 
}
