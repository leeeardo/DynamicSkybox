Shader "Unlit/RO_Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.03)) = .005
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Front
			ZWrite On
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float _Outline;
	        float4 _OutlineColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);

		        float3 norm   = normalize(mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal));
		        float2 offset = mul((float2x2)UNITY_MATRIX_P,norm.xy);

		        #ifdef UNITY_Z_0_FAR_FROM_CLIPSPACE //to handle recent standard asset package on older version of unity (before 5.5)
			        o.vertex.xy += offset * UNITY_Z_0_FAR_FROM_CLIPSPACE(o.vertex.z) * _Outline;
		        #else
			        o.pos.xy += offset * o.pos.z * _Outline;
		        #endif
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
