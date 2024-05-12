Shader "Custom/URPUnLitTemplate" {
	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_BaseColor ("Color", Color) = (0, 0.66, 0.73, 1)
		[Toggle(_ALPHATEST_ON)] _AlphaTestToggle ("Alpha Clipping", Float) = 0
		_Cutoff ("Alpha Cutoff", Float) = 0.5
		_Left ("_Left", Range(-2,5)) = 0
		_Right ("_Right", Range(-20,10)) = 1
		_ParamA ("_ParamA", Range(0,360)) = 1
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
				float2 uv1	: TEXCOORD1;
				float2 uv2	: TEXCOORD2;
				float4 color		: COLOR;
			};

			struct Varyings {
				float4 positionCS 	: SV_POSITION;
				float2 uv		    : TEXCOORD0;
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
				float3 normalWS		: TEXCOORD2;
				float3 positionWS	: TEXCOORD3;
				float4 color		: COLOR;
				float4 interpolate : TEXCOORD4;
			};

			// Textures, Samplers & Global Properties
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			float3 _Pos;
			float _Radius,_Left,_Right,_ParamA;

			
			float3 GetPivotPos(float2 uv1,float2 uv2)
			{
				float3 pos = float3(uv1.xy,uv2.x);
				pos = pos*2-1;
				pos*=float3(-1,1,-1);
				return pos;
			}
			float DistanceField(float3 pos,float radius)
			{
				return saturate((distance(pos,_Pos)+_Right)/radius);
			}

			float hash3to1(float3 p3)
			{
			    p3  = frac(p3 * .1031);
			    p3 += dot(p3, p3.zyx + 31.32);
			    return frac((p3.x + p3.y) * p3.z);
			}
			float3 hash33(float3 p3)
			{
			    p3 = frac(p3 * float3(.1031, .1030, .0973));
			    p3 += dot(p3, p3.yxz+33.33);
			    return frac((p3.xxy + p3.yxx)*p3.zyx);
			}
			void Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
			{
			    Rotation = radians(Rotation);
			    float s = sin(Rotation);
			    float c = cos(Rotation);
			    float one_minus_c = 1.0 - c;

			    Axis = normalize(Axis);
			    float3x3 rot_mat = 
			    {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
			        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
			        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
			    };
			    Out = mul(rot_mat,  In);
			}
			// Vertex Shader
			Varyings LitPassVertex(Attributes Input) {
				Varyings Output;

				VertexPositionInputs positionInputs = GetVertexPositionInputs(Input.positionOS.xyz);
				//Output.positionCS = positionInputs.positionCS;
				Output.positionWS = positionInputs.positionWS;

				float3 worldPos = Output.positionWS;
				
				float3 pivotPosWS = TransformObjectToWorld(GetPivotPos(Input.uv1,Input.uv2));

				//worldPos = (worldPos+pivotPosWS)/2;

				float3 expandDir = (pivotPosWS-worldPos);
				
				float distance = DistanceField(worldPos,_Radius);

				float3 newPos = lerp(worldPos,worldPos+expandDir*_Left,1-distance);

				//rotate new Pos based on pivot
				float3 rotateDir = normalize(pivotPosWS+hash33(pivotPosWS));
				float3 Out;
				Unity_RotateAboutAxis_Degrees_float(newPos,rotateDir,hash33(pivotPosWS)*_ParamA*(1-distance), Out);
				newPos = Out;
				
				Output.interpolate = float4(distance,pivotPosWS);
				Output.positionCS = TransformWorldToHClip(newPos);
				

				VertexNormalInputs normalInputs = GetVertexNormalInputs(Input.normalOS.xyz);
				Output.normalWS = normalInputs.normalWS;

				OUTPUT_LIGHTMAP_UV(Input.uv1, unity_LightmapST, Output.lightmapUV);
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

				// Diffuse
				half3 shading = bakedGI+LightingLambert(attenuatedLightColor, mainLight.direction, Input.normalWS);
				half4 color = baseMap * _BaseColor *Input.color;
				half4 newColor =color.rgba;
				color.rgb = lerp(color.rgb,newColor.rgb,1-step((1-Input.interpolate.x),0.01+hash3to1(Input.interpolate.yzw)*0.1));
				return half4(color.rgb * shading, color.a);
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