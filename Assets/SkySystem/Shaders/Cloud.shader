Shader "Custom/Cloud" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
//		[HDR]_BaseColor ("Color", Color) = (0, 0.66, 0.73, 1)
//		_CloudTopColor("CloudTopColor",Color)=(1,1,1,1)
//		_CloudBottomColor("CloudBottomColor",Color)=(0.2,0.2,0.2,1)
		_Dissolve("Dissolve",Range(0,1)) = 1
		//_GIIndex("GI index",Range(0,1))=0
	}
	SubShader {
		Tags {
			"RenderPipeline"="UniversalPipeline"
			"RenderType"="Opaque"
			"Queue"="Transparent"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
		float4 _MainTex_ST;
		float4 _BaseColor;
		float4 _CloudTopColor,_CloudBottomColor;
		float _Dissolve,_GIIndex;
		CBUFFER_END
		ENDHLSL

		Pass {
			Name "UnLit"
//			Tags { "LightMode"="UniversalForward" }
			Cull Front
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			//ZTest Always
			HLSLPROGRAM
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			

			// Includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			// Structs
			struct Attributes {
				float4 positionOS	: POSITION;
				float4 normalOS		: NORMAL;
				float2 uv		    : TEXCOORD0;
				float2 lightmapUV	: TEXCOORD1;
			};

			struct Varyings {
				float4 positionCS 	: SV_POSITION;
				float2 uv		    : TEXCOORD0;
				float3 normalWS		: TEXCOORD1;
				float3 positionWS	: TEXCOORD2;
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
			};

			// Textures, Samplers & Global Properties
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			float4 _SunDir;

			float3 RotateAroundY(float3 postion,float speed)
			{
				speed *= _Time.x;
				float x = cos(speed)*postion.x+sin(speed)*postion.z;
				float y = postion.y;
				float z = -sin(speed)*postion.x+cos(speed)*postion.z;
				return float3(x,y,z);
			}
			
			// Vertex Shader
			Varyings LitPassVertex(Attributes Input) {
				Varyings Output;

				VertexPositionInputs positionInputs = GetVertexPositionInputs(Input.positionOS.xyz);
				//Output.positionCS = positionInputs.positionCS;
				Output.positionWS = positionInputs.positionWS;
				float3 newWorldPos = (positionInputs.positionWS)+GetCameraPositionWS();
				//newWorldPos= RotateAroundY(newWorldPos,1);
				Output.positionCS = TransformWorldToHClip(newWorldPos);

				VertexNormalInputs normalInputs = GetVertexNormalInputs(Input.normalOS.xyz);
				Output.normalWS = normalInputs.normalWS;
				OUTPUT_SH(Output.normalWS.xyz, Output.vertexSH);
				Output.uv = TRANSFORM_TEX(Input.uv, _MainTex);
				return Output;
			}

			float2 RotateUV(float2 uv,float degress)
			{
				degress = DegToRad(degress);
				float u = cos(degress)*uv.x+sin(degress)*uv.y;
				float v = -sin(degress)*uv.x+cos(degress)*uv.y;
				return float2(u,v);
			}
			
			// Fragment Shader
			half4 LitPassFragment(Varyings Input) : SV_Target {
				half4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,Input.uv);

				Light light = GetMainLight();
				light.direction = _SunDir.xyz;
				//r for diffuse
				//float simpleLight=saturate(light.direction.y)*baseMap.r;
				float simpleLight = saturate(dot(light.direction.y,Input.normalWS))*baseMap.r;
				//g for backLight
				float3 pixelDir = normalize(Input.positionWS);
				float backLight = baseMap.g*saturate(dot(pixelDir,light.direction));
				float newDir = light.direction;
				float newDot = dot(newDir,float3(0,1,0));
				//backLight*=newDot;
				//b for sdf dissolve
				float alpha = saturate((baseMap.b)-_Dissolve)*baseMap.a;

				backLight = 5*pow(backLight,8);
				// Diffuse
				float3 diffuse = lerp(_CloudBottomColor,_CloudTopColor,simpleLight+backLight);
				half3 color = diffuse * _BaseColor.rgb*light.color;

				// Get Baked GI
				half3 bakedGI = SAMPLE_GI(Input.lightmapUV, Input.vertexSH, Input.normalWS);
				color+= bakedGI*_GIIndex;

				//test
				half3 reflDir = reflect(-GetWorldSpaceViewDir(Input.positionWS),Input.normalWS);
				float3 test= GlossyEnvironmentReflection(reflDir,Input.positionWS,1,1);
				color+=test;
				return half4(color, alpha);
			}
			ENDHLSL
		}
		
		// ShadowCaster, for casting shadows

	}
}