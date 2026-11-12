// Cinta de corriente (docs/09 §2): malla que sigue el cauce con UV0 = (metros laterales con signo, metros a lo largo) y
// UV1.x = medio ancho del agua. Dibuja las líneas de corriente que avanzan por el cauce y la espuma pegada a las orillas.
// Se monta sobre el agua con las mismas olas de SG_ToonWater, así sube y baja con ella.
Shader "GasSafari/SG_WaterFlow"
{
    Properties
    {
        _LineColor("Color de líneas de corriente", Color) = (0.9, 1, 1, 0.55)
        _FoamColor("Color espuma de orilla", Color) = (1, 1, 1, 0.95)
        _LineLanes("Carriles por metro de ancho", Float) = 0.9
        _LineLength("Largo de las líneas (1/m)", Float) = 0.22
        _Speed("Velocidad de las líneas (m/s)", Float) = 2.2
        _EdgeWidth("Ancho de espuma de orilla (m)", Float) = 0.45
        _EdgeFoam("Espuma en orillas (0/1)", Range(0, 1)) = 1
        _EdgeFade("Desvanecido hacia la orilla (m)", Float) = 0.6
        _WaveHeight("Altura de olas (m)", Float) = 0.08
        _WaveLength("Longitud de ola (m)", Float) = 6
        _WaveSpeed("Velocidad de olas", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+1" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LineColor;
                half4 _FoamColor;
                float _LineLanes;
                float _LineLength;
                float _Speed;
                float _EdgeWidth;
                half _EdgeFoam;
                float _EdgeFade;
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float halfWidth : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            float WaveOffset(float3 positionWS)
            {
                float k = 6.2831853 / max(_WaveLength, 0.01);
                float t = _Time.y * _WaveSpeed;
                float a = sin(dot(positionWS.xz, float2(0.8, 0.6)) * k + t);
                float b = sin(dot(positionWS.xz, float2(-0.5, 0.87)) * k * 2.0 + t * 1.3) * 0.5;
                return (a + b) * _WaveHeight;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS.y += WaveOffset(positionWS) + 0.015;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv;
                OUT.halfWidth = IN.uv1.x;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float x = IN.uv.x;
                float v = IN.uv.y;
                float t = _Time.y * _Speed;
                float edge = IN.halfWidth - abs(x);

                // Dos capas de líneas alargadas en el sentido de la corriente, a distinta velocidad.
                float n1 = ValueNoise(float2(x * _LineLanes, (v - t) * _LineLength));
                float n2 = ValueNoise(float2(x * _LineLanes * 1.7 + 13.0, (v - t * 1.5) * _LineLength * 1.3));
                half lines = max(smoothstep(0.66, 0.74, n1), smoothstep(0.7, 0.78, n2) * 0.75);
                half fade = saturate((edge + 0.2) / max(_EdgeFade, 0.01));
                half lineAlpha = lines * _LineColor.a * fade;

                // Espuma pegada a la orilla, con borde irregular que corre despacio con la corriente.
                float breakup = ValueNoise(float2(v * 0.9 - t * 0.35, x * 0.6));
                float width = _EdgeWidth * (0.55 + breakup * 0.9);
                half foam = (1.0 - smoothstep(width, width + 0.12, edge)) * step(-0.4, edge) * _EdgeFoam * _FoamColor.a;

                half3 color = lerp(_LineColor.rgb, _FoamColor.rgb, foam);
                half alpha = saturate(max(lineAlpha, foam));
                color = MixFog(color, IN.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
