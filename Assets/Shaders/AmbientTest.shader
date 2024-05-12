Shader "Custom/ambientTest" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_BaseColor ("Color", Color) = (0, 0.66, 0.73, 1)
		[Toggle(_ALPHATEST_ON)] _AlphaTestToggle ("Alpha Clipping", Float) = 0
		_Cutoff ("Alpha Cutoff", Float) = 0.5
	}
	SubShader {
		Tags {
			"RenderPipeline"="UniversalPipeline"
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
		float4 _BaseMap_ST;
		float4 _BaseColor;
		float _Cutoff;
		CBUFFER_END
		ENDHLSL

		Pass {
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local_fragment _EMISSION
			#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature_local_fragment _OCCLUSIONMAP

			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			#pragma shader_feature_local_fragment _SPECULAR_SETUP
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF

			// URP Keywords
			
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING 
			#pragma multi_compile _ SHADOWS_SHADOWMASK 

			// Unity Keywords
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile_fog

			// TODO GPU Instancing
			

			// Includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			// Structs
			struct Attributes {
				float4 positionOS	: POSITION;
				float4 normalOS		: NORMAL;
				float2 uv		    : TEXCOORD0;
				float2 lightmapUV	: TEXCOORD1;
				float4 color		: COLOR;
			};

			struct Varyings {
				float4 positionCS 	: SV_POSITION;
				float2 uv		    : TEXCOORD0;
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
				float3 normalWS		: TEXCOORD2;
				float3 positionWS	: TEXCOORD3;
				float4 color		: COLOR;
			};

			// Textures, Samplers & Global Properties
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			// Vertex Shader
			Varyings LitPassVertex(Attributes Input) {
				Varyings Output;

				VertexPositionInputs positionInputs = GetVertexPositionInputs(Input.positionOS.xyz);
				Output.positionCS = positionInputs.positionCS;
				Output.positionWS = positionInputs.positionWS;

				VertexNormalInputs normalInputs = GetVertexNormalInputs(Input.normalOS.xyz);
				Output.normalWS = normalInputs.normalWS;

				OUTPUT_LIGHTMAP_UV(Input.lightmapUV, unity_LightmapST, Output.lightmapUV);
				OUTPUT_SH(Output.normalWS.xyz, Output.vertexSH);

				Output.uv = TRANSFORM_TEX(Input.uv, _BaseMap);
				Output.color = Input.color;
				return Output;
			}

			// Fragment Shader
			half4 LitPassFragment(Varyings Input) : SV_Target {
				half4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, Input.uv);

				#ifdef _ALPHATEST_ON
					// Alpha Clipping
					clip(baseMap.a - _Cutoff);
				#endif

				// Get Baked GI
				half3 bakedGI = SAMPLE_GI(Input.lightmapUV, Input.vertexSH, Input.normalWS);
				
				// Main Light & Shadows
				float4 shadowCoord = TransformWorldToShadowCoord(Input.positionWS.xyz);
				Light mainLight = GetMainLight(shadowCoord);
				half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);

				// Mix Realtime & Baked (if LIGHTMAP_SHADOW_MIXING / _MIXED_LIGHTING_SUBTRACTIVE is enabled)
				MixRealtimeAndBakedGI(mainLight, Input.normalWS, bakedGI);
				float3 reflectDir = reflect(-GetWorldSpaceViewDir(Input.positionWS),Input.normalWS);
				float3 test= GlossyEnvironmentReflection(reflectDir,Input.positionWS,0,1);
				// Diffuse
				half3 shading = bakedGI + LightingLambert(attenuatedLightColor, mainLight.direction, Input.normalWS);
				half4 color = baseMap * _BaseColor *Input.color;
				return half4(shading, color.a);
			}
			ENDHLSL
		}
		
		// ShadowCaster, for casting shadows
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
			
			ENDHLSL
		}

		// DepthOnly, used for Camera Depth Texture (if cannot copy depth buffer instead, and the DepthNormals below isn't used)
		Pass {
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ColorMask 0
			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			// GPU Instancing
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
			
			ENDHLSL
		}

		// DepthNormals, used for SSAO & other custom renderer features that request it
		Pass {
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormals" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex DepthNormalsVertex
			#pragma fragment DepthNormalsFragment

			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			// GPU Instancing
			#pragma multi_compile_instancing
			//#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
			
			ENDHLSL
		}
		
	}
}