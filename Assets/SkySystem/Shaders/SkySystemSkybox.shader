Shader "Custom/Skybox" {
	Properties {
		_Color("Color",Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Pass{
			
			Tags {
				"Queue"="Background" 
					"RenderType"="Background" 
					"PreviewType"="Skybox"
					"IgnoreProjector"="True"
				 }
			Cull Off
			ZWrite Off
			HLSLPROGRAM
			
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#pragma target 3.0

			#pragma vertex MySkyboxVert
			#pragma fragment MySkyboxFrag

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_SkyRampMap);
			SAMPLER(sampler_SkyRampMap);
			TEXTURE2D(_SkyWorldYRampMap);
			SAMPLER(sampler_SkyWorldYRampMap);
			TEXTURE2D(_SunDiscGradient);
			SAMPLER(sampler_SunDiscGradient);
			TEXTURE2D(_MoonTexture);
			SAMPLER(sampler_MoonTexture);
			TEXTURECUBE(_StarTexture);
			SAMPLER(sampler_StarTexture);
		
			float4 _Color,_MoonGlowColor,_SunGlowColor;
			float4 _SunDir,_SunHalo,_MoonDir;		
			float _StarIntensity,_SunIntensity,_MoonIntensity,_MoonDistance;
			struct Attributes
			{
				float4 positionOS	: POSITION;
				float2 uv:TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS 	: SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 sunAndMoonPos:TEXCOORD2;
				float3 positionOS:TEXCOORD3;
			};

			Varyings MySkyboxVert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input); 
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 
				output.positionCS = mul(UNITY_MATRIX_MVP, input.positionOS);
				output.uv = input.uv;
				output.positionWS = TransformObjectToWorld(input.positionOS);

				float3 rSun = normalize(cross(_SunDir.xyz, float3(0, -1, 0)));
				float3 uSun = cross(_SunDir.xyz, rSun);
				float3 rMoon = normalize(cross(_MoonDir.xyz, float3(0, -1, 0)));
				float3 uMoon = cross(_MoonDir.xyz, rMoon);
				output.sunAndMoonPos.xy = float2(dot(rSun, input.positionOS.xyz), dot(uSun, input.positionOS.xyz))  + 0.5;
				output.sunAndMoonPos.zw = float2(dot(rMoon, input.positionOS.xyz), dot(uMoon, input.positionOS.xyz))*_MoonDistance + 0.5;
				output.positionOS = input.positionOS;
				return output;
			}

			void SetMoon(Varyings input,inout float3 color)
			{
				float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
				float hideBackMoon = saturate(dot(_MoonDir.xyz, viewDir));
				float4 moonTex = _MoonIntensity*SAMPLE_TEXTURE2D(_MoonTexture,sampler_MoonTexture,input.sunAndMoonPos.zw)*hideBackMoon;
				
				//moon glow
				color += _MoonGlowColor*pow(hideBackMoon,1024)+moonTex;

				color = lerp(moonTex.rgb,color,1-moonTex.a).rgb;
			}
			float3 SetSun(Varyings input,float mask,float horizontalLine)
			{
				float3 sunColor = _SunGlowColor.rgb;
				float3 sunLightDir = -_SunDir.xyz;
				float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
				float sundot = saturate(dot(sunLightDir,viewDir));
				float sun = _SunHalo.x* Pow4(sundot);
				sun += _SunHalo.y * pow(sundot, 32.0);
				sun += _SunHalo.z  * pow(sundot, 128.0);
				sun += _SunHalo.w * pow(sundot, 2048.0);
				float3 sunDisc = SAMPLE_TEXTURE2D(_SunDiscGradient,sampler_SunDiscGradient,float2(sundot,sundot))*mask;
				
				return sunDisc+(sun+horizontalLine)*_SunIntensity*sunColor*Pow4(sundot);
			}
			float3 RotateAroundY(float3 postion,float speed)
			{
				speed *= _Time.x;
				float x = cos(speed)*postion.x+sin(speed)*postion.z;
				float y = postion.y;
				float z = -sin(speed)*postion.x+cos(speed)*postion.z;
				return float3(x,y,z);
			}

			
			half4 MySkyboxFrag(Varyings input) : SV_Target
			{
				float worldUp = (normalize(input.positionWS).y);
				float worldForward = (normalize(input.positionWS).z);
				//float worldRight = (normalize(input.positionWS).x)*0.5+0.5;
				float3 dayGradient = SAMPLE_TEXTURE2D(_SkyRampMap,sampler_SkyRampMap,worldUp*0.5+0.5);
				float3 nightGradient = SAMPLE_TEXTURE2D(_SkyWorldYRampMap,sampler_SkyWorldYRampMap,worldUp*0.5+0.5);

				//horizontalLine
				//worldUp = worldUp*2+1;
				float horizontalLine = abs(input.uv.y);
				horizontalLine = smoothstep(0.248,0.255,horizontalLine);
				float horizon = abs(input.uv.y*5)+0.3;
				horizon = 1-smoothstep(0.0,0.6,horizon);
				float horizonGlow = saturate((1 - horizon) * GetMainLight().direction.y);
				//worldUpMask 比地平线稍低的渐变
				float worldUpMask = worldUp+0.1;
				worldUpMask = smoothstep(0,0.1,worldUpMask);
				
				

				float3 color=0;
				
				float3 skyGradient = lerp(dayGradient,nightGradient,saturate((1-saturate(_SunDir.y*5))));
				//sun
				color+=skyGradient;
				color += SetSun(input,worldUpMask,horizon);
				//Moon
				//color =0;
				SetMoon(input, color);

				//star
				float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
				float3 star = _StarIntensity*SAMPLE_TEXTURECUBE(_StarTexture,sampler_StarTexture,RotateAroundY(input.positionOS.xyz,0.1));;
				color += star;
				
				float sunNightStep = smoothstep(-0.3,0.25,GetMainLight().direction.y);
				//return saturate(1-_SunDir.y);
				//return float4(SetSun(input,worldUpMask,horizon),1);
				return float4((color),1);
			}
			ENDHLSL
		}
	}
}