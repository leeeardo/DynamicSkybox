Shader "Custom/Postprocess/Scan"
{
     Properties
    {
        _BlurSize("_BlurSize", Float) = 1.0
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off Zwrite Off ZTest Always
        
        HLSLINCLUDE


            #pragma shader_feature_local _OUTLINE
            #pragma shader_feature_local _FRESNEL
            #pragma shader_feature_local _GLITCH
        
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv[9] : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 ray : TEXCOORD0;
            };
        
            

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                
            CBUFFER_END
        
            TEXTURE2D (_MainTex);
            SAMPLER(sampler_MainTex);
            
            float4x4 _FrustumCorners;
            float3 _HitPos;
            float _ScanTimer,_ScanWidth,_ScanSpeed;
            float _ScanSmoothness;
            float4 _ScanLineColor;

            float _EffectTimeSpan,_FadeOutTime;
            float _DotDensity,_DotSize;
            float _IsoHeightDensity,_IsoHeightThickness;
            float _OutlineThickness;
            float _DecolorationStrength,_InsideBrightness,_GlitchStrength;
        
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);
            TEXTURE2D(_GridTexture);
            SAMPLER(sampler_GridTexture);
        
            v2f BaseVert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.positionOS.xyz);
                half2 uv = v.texcoord;

                o.uv[0] = uv + _MainTex_TexelSize.xy * half2(-1, -1); //计算像素左下的uv坐标
                o.uv[1] = uv + _MainTex_TexelSize.xy * half2(0, -1);  //计算像素下的uv坐标
                o.uv[2] = uv + _MainTex_TexelSize.xy * half2(1, -1);  //计算像素右下的uv坐标
                o.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1, 0);  //计算像素左的uv坐标
                o.uv[4] = uv + _MainTex_TexelSize.xy * half2(0, 0); //计算像素的uv坐标
                o.uv[5] = uv + _MainTex_TexelSize.xy * half2(1, 0); //计算像素右的uv坐标
                o.uv[6] = uv + _MainTex_TexelSize.xy * half2(-1, 1);  //计算像素左上的uv坐标
                o.uv[7] = uv + _MainTex_TexelSize.xy * half2(0, 1); //计算像素上的uv坐标
                o.uv[8] = uv + _MainTex_TexelSize.xy * half2(1, 1);
                
                int index = 0;
                if (v.texcoord.x < 0.5 && v.texcoord.y > 0.5)
                    index = 0;
                else if (v.texcoord.x > 0.5 && v.texcoord.y > 0.5)
                    index = 1;
                else if (v.texcoord.x < 0.5 && v.texcoord.y < 0.5)
                    index = 2;
                else
                    index = 3;
                o.ray = _FrustumCorners[index];
                return o;
            }
        
            half4 Discoloration(float4 color,float strength)
            {
                float luminance = color.r * 0.2125 + color.g * 0.7154 + color.b * 0.0721;
                float4 deColor = half4(luminance,luminance,luminance,1);
                return lerp(color,deColor,strength);
            }

            half Sobel(v2f i)
            {
                //此处的Sobel算子是经过反转之后的，所以可以直接用于梯度计算
                const half Gx[9] =
                {
                    -1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1
                };
                const half Gy[9] =
                {
                    -1, -2, -1,
                    0, 0, 0,
                    1, 2, 1
                };

                half texColor;
                half edgeX = 0; 
                half edgeY = 0; 
                for (int it = 0; it < 9; it++)
                {
                    texColor = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture, i.uv[it]),_ZBufferParams); //获取灰度值
                    edgeX += texColor * Gx[it];
                    edgeY += texColor * Gy[it];
                }
                
                //梯度值越大，edge越小，则需要在在边缘颜色属性中取占比更多的值
                half edge = 1 - abs(edgeX) - abs(edgeY);
                return edge; 
            }

            float Rand(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898,78.233)))*43578.5453);   
            }  

            half4 GlitchRGBSplit_HV_Frag(v2f input,float strength) 
            {
            float splitAmount = strength*0.01*Rand(float2(_ScanTimer,2));

            half4 ColorR = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv[4].x +splitAmount,input.uv[4].y));
            half4 ColorG = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , input.uv[4]);
            half4 ColorB = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex , float2(input.uv[4].x -splitAmount,input.uv[4].y));

            return half4(ColorR.r,ColorG.g,ColorB.b,1);
            }   
        
            half4 ScanFrag(v2f input) :SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv[4]);
                half3 normal = SAMPLE_TEXTURE2D(_CameraNormalsTexture,sampler_CameraNormalsTexture,input.uv[4]);
                half3 normalSplit = normalize(max(0,abs(normal)-0.2));
                
                float depthTex = SAMPLE_TEXTURE2D(_CameraDepthTexture,sampler_CameraDepthTexture,input.uv[4]).r;
                //float depth = LinearEyeDepth(depthTex,_ZBufferParams);
                float depth01 = Linear01Depth(depthTex,_ZBufferParams);
                float3 worldPos = GetCameraPositionWS()+depth01*input.ray.xyz;

                float scanDepth= _ScanTimer*_ScanSpeed;
                float scanDistance= distance(worldPos,_HitPos);
                scanDepth = min(scanDepth,_ScanWidth);
                float distanceRamp = 1-saturate(scanDepth/scanDistance);
                _ScanSmoothness = _ScanSmoothness*0.5+0.5;
                float halfDistanceRamp = smoothstep(1-_ScanSmoothness,_ScanSmoothness,distanceRamp);
                distanceRamp = smoothstep(0,0.3,halfDistanceRamp*(1-halfDistanceRamp));
                
                //DotAndLineMask
                float gridXZ = step(frac(worldPos.y*_IsoHeightDensity),_IsoHeightThickness)*(1-normalSplit.y);
                float gridY = SAMPLE_TEXTURE2D(_GridTexture,sampler_GridTexture,_DotDensity*worldPos.xz)*normalSplit.y;

                float grid = step(1-gridY,_DotSize)+1-step(gridXZ,0.5);
                
                float mask = saturate((grid)*(1-halfDistanceRamp));

                //fresnel
                #ifdef _FRESNEL
                    float fresnel = (1-saturate(dot(normal,normalize(GetCameraPositionWS()-worldPos))));
                    fresnel = Pow4(fresnel)*distanceRamp;
                    mask=saturate(fresnel+mask);
                #else
                    mask = saturate(distanceRamp+mask);
                #endif
                //edge
                #ifdef _OUTLINE
                    half edge = Sobel(input);
                    edge = step(edge,_OutlineThickness);
                    mask = saturate(mask+(edge)*(1-halfDistanceRamp));
                #endif
                
                
                if(_ScanTimer>0.00001)
                {
                    
                    float4 ScanedCol = lerp(col,Discoloration(col,_DecolorationStrength)*_InsideBrightness,1-halfDistanceRamp);
                    ScanedCol = lerp(ScanedCol,_ScanLineColor,mask);
                    //col.rgb = lerp(col.rgb,float3(0,0.2,0.9),distanceRamp);
                    if(_ScanTimer>_FadeOutTime)
                    {
                        float noise =max(sin(10*abs(sin(2*_ScanTimer)+0.2)+0.1),0.75);
                        #ifdef _GLITCH
                            col = GlitchRGBSplit_HV_Frag(input,_GlitchStrength);
                        #endif
                        
                        ScanedCol.rgb = lerp(ScanedCol,col.rgb,noise*(_ScanTimer-_FadeOutTime)/(_EffectTimeSpan-_FadeOutTime));
                    }
                    return ScanedCol;

                }
                return col;
                
            }
            
        
        ENDHLSL

        Pass    //0
        {
            Name "Scan"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BaseVert
            #pragma fragment  ScanFrag
            ENDHLSL
        }
        
         
    } 
}
