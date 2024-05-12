Shader "Custom/Postprocess/BlurTest"
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
        
            struct v2f_uv5
            {
                float2 uv[5] : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            struct v2f_uv9
            {
                float2 uv[9] : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float _BlurSize;
                float _offset;
                half4 _GoldenRot;
                half4 _Params;
                half _Area;
                half _Spread;
                float _OFFset;
            CBUFFER_END
        
            TEXTURE2D (_MainTex);
            SAMPLER(sampler_MainTex);

            v2f_uv5 VerticalVert (appdata v)
            {
                v2f_uv5 o;
                o.vertex = TransformObjectToHClip(v.positionOS.xyz);
                float2 uv = v.texcoord;
                o.uv[0] = uv;
                o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
                o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
                o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
                o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
                return o;
            }

            v2f_uv5 HorizontalVert (appdata v)
            {
                v2f_uv5 o;
                o.vertex = TransformObjectToHClip(v.positionOS.xyz);
                float2 uv = v.texcoord;
                o.uv[0] = uv;
                o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0,0.0) * _BlurSize;
                o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0,0.0) * _BlurSize;
                o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0,0.0) * _BlurSize;
                o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0,0.0) * _BlurSize;
                return o;
            }

            half4 GaussianBlurFrag (v2f_uv5 i) : SV_Target
            {
                float weight[3] = {0.4026f,0.2442f,0.0545f};

                float3 sum = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0]).rgb*weight[0];

                for (int it = 1; it<3;it++)
                {
                    sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[it]).rgb * weight[it];
                    sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[2*it]).rgb * weight[it];
                }
                return float4(sum,1.0);
            }

            v2f_uv9 BoxBlurVert (appdata v)
            {
                v2f_uv9 o;
                o.vertex = TransformObjectToHClip(v.positionOS.xyz);
                float2 uv = v.texcoord;
                o.uv[0] = uv + _MainTex_TexelSize.xy*float2( -1.0, 1.0) * _BlurSize;
                o.uv[1] = uv + _MainTex_TexelSize.xy*float2(  0.0, 1.0) * _BlurSize;
                o.uv[2] = uv + _MainTex_TexelSize.xy*float2(  1.0, 1.0) * _BlurSize;
                o.uv[3] = uv + _MainTex_TexelSize.xy*float2( -1.0, 0.0) * _BlurSize;
                o.uv[4] = uv + _MainTex_TexelSize.xy*float2(  0.0, 0.0) * _BlurSize;
                o.uv[5] = uv + _MainTex_TexelSize.xy*float2(  1.0, 0.0) * _BlurSize;
                o.uv[6] = uv + _MainTex_TexelSize.xy*float2( -1.0,-1.0) * _BlurSize;
                o.uv[7] = uv + _MainTex_TexelSize.xy*float2(  0.0,-1.0) * _BlurSize;
                o.uv[8] = uv + _MainTex_TexelSize.xy*float2(  1.0,-1.0) * _BlurSize;
                return o;
            }

            half4 BoxBlurFrag (v2f_uv9 i) : SV_Target
            {

                float3 sum = 0;

                for (int it = 0; it<9;it++)
                {
                    sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[it]).rgb *1/9;
                }
                return float4(sum,1.0);
            }

            v2f_uv5 KawaseBlurVert (appdata v)
            {
                v2f_uv5 o;
                float i = _offset;
                o.vertex = TransformObjectToHClip(v.positionOS.xyz);
                float2 uv = v.texcoord;
                o.uv[0] = uv;
                o.uv[1] = uv + _MainTex_TexelSize.xy*float2(  i,  i) * _BlurSize;
                o.uv[2] = uv + _MainTex_TexelSize.xy*float2(  i, -i) * _BlurSize;
                o.uv[3] = uv + _MainTex_TexelSize.xy*float2( -i,  i) * _BlurSize;
                o.uv[4] = uv + _MainTex_TexelSize.xy*float2( -i, -i) * _BlurSize;

                return o;
            }

            half4 KawaseBlurFrag (v2f_uv5 i) : SV_Target
            {

                float3 sum = 0;

                for (int it = 0; it<5;it++)
                {
                    sum += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[it]).rgb;
                }
                return float4(sum/5.0f,1.0);
            }

            v2f BaseVert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.texcoord;
                return o;
            }

            #define _Iteration _Params.x
            #define _Radius _Params.y
            #define _PixelSize _Params.zw

            half4 BokehBlur(v2f i)
            {
                half2x2 rot = half2x2(_GoldenRot);
                half4 accumulator = 0.0;
                half4 divisor = 0.0;

                half r = 1;
                half2 angle = half2(0.0,_Radius);

                for (int j = 0; j<(int)_Iteration;j++)
                {
                    r += 1.0/r;
                    angle = mul(rot,angle);
                    half4 bokeh = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,float2(i.uv+_PixelSize*(r-1.0)*angle));
                    accumulator += bokeh * bokeh;
                    divisor += bokeh;
                }
                return accumulator /divisor;
            }
        
            half4 BokehBlurFrag(v2f i) : SV_Target
            {
                return BokehBlur(i);
            }

            float TileShiftMask(float2 uv)
            {
                float centerY = uv.y*2.0-1.0+_OFFset;
                return pow(abs(centerY*_Area),_Spread);
            }

            half4 TileShiftBlur(v2f i)
            {
                half2x2 rot = half2x2(_GoldenRot);
                half4 accumulator = 0.0;
                half4 divisor = 0.0;

                half r = 1;
                half2 angle = half2(0.0,_Radius * saturate(TileShiftMask(i.uv)));

                for (int j = 0; j<(int)_Iteration;j++)
                {
                    r += 1.0/r;
                    angle = mul(rot,angle);
                    half4 bokeh = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,float2(i.uv+_PixelSize*(r-1.0)*angle));
                    accumulator += bokeh * bokeh;
                    divisor += bokeh;
                }
                return accumulator /divisor;
            }

            half4 TileShiftBlurFrag(v2f i) : SV_Target
            {
                return TileShiftBlur(i);
            }

            float IrisMask(float2 uv)
            {
                float2 center = uv * 2.0 - 1.0 ; // [0,1] -> [-1,1] 
                return pow(dot(center, center) * _Area,_Spread);
            }
            half4 IrisBlur(v2f i)
            {
                half2x2 rot = half2x2(_GoldenRot);
                half4 finalColor = 0.0;
                half4 divisor = 0.0;

                half r = 1;
                half2 angle = half2(0.0,_Radius * saturate(IrisMask(i.uv)));

                for (int j = 0; j<(int)_Iteration;j++)
                {
                    r += 1.0/r;
                    angle = mul(rot,angle);
                    half4 bokeh = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,float2(i.uv+_PixelSize*(r-1.0)*angle));
                    finalColor += bokeh * bokeh;
                    divisor += bokeh;
                }
                return finalColor /divisor;
            }
        
            half4 IrisBlurFrag(v2f i) : SV_Target
            {
                return IrisBlur(i);
            }

            float Rand(float2 uv){
                return frac(sin(dot(uv, float2(12.9898,78.233)))*43578.5453);   
            }

            half4 GrainyBlur(v2f i)
            {
                half2 randomOffset = float2(0.0,0.0);
                half4 finalColor = half4(0.0,0.0,0.0,0.0);
                float random = Rand(i.uv);

                for(int j = 0;j<(int)_Iteration;j++)
                {
                    _MainTex_TexelSize*=_Radius;
                    float r1 = clamp(Rand(i.uv*float(j))*2-1,-_MainTex_TexelSize.x,_MainTex_TexelSize.x);
                    float r2 = clamp(Rand(i.uv*float(j+_Iteration))*2-1,-_MainTex_TexelSize.y,_MainTex_TexelSize.y);
                    random = frac(43758.5453 * random + 0.61432);
                    randomOffset.x = (random -0.5)*2.0;
                    //random = frac(43758.5453 * random + 0.61432);
                    randomOffset.x = (random -0.5)*2.0;

                    finalColor += SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(r1,r2));
                }
                return finalColor/_Iteration;
            }

            half4 GrainyBlurFrag(v2f i) : SV_Target
            {
                return GrainyBlur(i);
            }

            half4 RadialBlur(v2f i)
            {
                float2 blurVector = (_PixelSize - i.uv) * _Radius*0.05;

                half4 acumulateColor = half4(0, 0, 0, 0);

                [unroll(30)]
                for (int j = 0; j < _Iteration; j ++)
                {
                    acumulateColor += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                    i.uv += blurVector;
                }

                return acumulateColor/_Iteration;
            }

            half4 RadialBlurFrag(v2f i) : SV_Target
            {
                return RadialBlur(i);
            }

            half4 DirectionalBlur(v2f i)
            {
                half4 color = half4(0.0, 0.0, 0.0, 0.0);

                for (int k = -_Iteration; k < _Iteration; k++)
                {
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - _PixelSize * k);
                }
                half4 finalColor = color / (_Iteration * 2.0);

                return finalColor;
            }

            half4 DirectionalBlurFrag(v2f i) : SV_Target
            {
                return DirectionalBlur(i);
            }
        
        ENDHLSL

        Pass    //0
        {
            Name "GaussianBlurVertical"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex VerticalVert
            #pragma fragment  GaussianBlurFrag
            ENDHLSL
        }
        
         Pass   //1
        {
            Name "GaussianBlurHorizontal"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex HorizontalVert
            #pragma fragment GaussianBlurFrag
            ENDHLSL
        }
        
        Pass    //2
        {
            Name "BoxBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BoxBlurVert
            #pragma fragment BoxBlurFrag
            ENDHLSL
        }
        
        Pass    //3
        {
            Name "KawaseBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex KawaseBlurVert
            #pragma fragment KawaseBlurFrag
            ENDHLSL
        }
        
        Pass    //4
        {
            Name "BokehBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BaseVert
            #pragma fragment BokehBlurFrag
            ENDHLSL
        }
        
        Pass    //5
        {
            Name "TileShiftBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BaseVert
            #pragma fragment TileShiftBlurFrag
            ENDHLSL
        }
        
        Pass    //6
        {
            Name "IrisBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BaseVert
            #pragma fragment IrisBlurFrag
            ENDHLSL
        }
        
        Pass    //7
        {
            Name "GrainyBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BaseVert
            #pragma fragment GrainyBlurFrag
            ENDHLSL
        }
        Pass    //8
        {
            Name "RadialBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BaseVert
            #pragma fragment RadialBlurFrag
            ENDHLSL
        }
        
        Pass    //9
        {
            Name "DirectionalBlur"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex BaseVert
            #pragma fragment DirectionalBlurFrag
            ENDHLSL
        }
    } 
}
