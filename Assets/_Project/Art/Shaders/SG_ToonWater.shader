// Agua toon de Gas Safari (docs/09 §2): color por profundidad (requiere Depth Texture en el URP Asset),
// espuma en intersecciones, olas por vértice (seno, 2 octavas), red de cáusticas (líneas de rizos) que se desplaza con la
// corriente, brillo especular con destellos y chispas, y un leve reflejo del cielo en ángulos rasantes.
// Escrito en HLSL URP en lugar de Shader Graph. _FlowDir (xz) lo fija el nivel con la dirección de la corriente.
Shader "GasSafari/SG_ToonWater"
{
    Properties
    {
        _ShallowColor("Color poco profundo", Color) = (0.36, 0.85, 0.85, 0.9)
        _DeepColor("Color profundo", Color) = (0.07, 0.35, 0.55, 0.97)
        _DepthMax("Profundidad del color profundo (m)", Float) = 3
        _FoamColor("Color espuma", Color) = (1, 1, 1, 1)
        _FoamWidth("Ancho de espuma (m)", Float) = 0.5
        _FoamScale("Escala del patrón de espuma", Float) = 2.5
        _WaveHeight("Altura de olas (m)", Float) = 0.08
        _WaveLength("Longitud de ola (m)", Float) = 6
        _WaveSpeed("Velocidad de olas", Float) = 1
        _FlowDir("Dirección de corriente (xz)", Vector) = (0, 1, 0, 0)
        _FlowSpeed("Velocidad de corriente de UV", Float) = 0.4
        _RippleColor("Color de rizos", Color) = (0.7, 1, 1, 1)
        _RippleScale("Escala de rizos (1/m)", Float) = 0.45
        _RippleSpeed("Velocidad de rizos", Float) = 0.6
        _RippleStrength("Fuerza de rizos", Range(0, 1)) = 0.35
        [HDR] _GlintColor("Color de destellos (HDR)", Color) = (2, 2, 2, 1)
        _SpecPower("Potencia del brillo", Float) = 90
        _SpecStrength("Fuerza del brillo", Float) = 1.4
        _NormalStrength("Inclinación de las olas", Float) = 0.22
        _SparkleScale("Escala de chispas (1/m)", Float) = 2.2
        _SparkleDensity("Densidad de chispas", Range(0, 0.2)) = 0.03
        _SkyColor("Color del reflejo del cielo", Color) = (0.7, 0.85, 1, 1)
        _FresnelStrength("Fuerza del reflejo", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _DepthMax;
                half4 _FoamColor;
                float _FoamWidth;
                float _FoamScale;
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
                float4 _FlowDir;
                float _FlowSpeed;
                half4 _RippleColor;
                float _RippleScale;
                float _RippleSpeed;
                half _RippleStrength;
                half4 _GlintColor;
                float _SpecPower;
                float _SpecStrength;
                float _NormalStrength;
                float _SparkleScale;
                half _SparkleDensity;
                half4 _SkyColor;
                half _FresnelStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            // Dos octavas de seno: la segunda con doble frecuencia, mitad de amplitud y otra dirección.
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

            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            // Cáusticas: diferencia entre la 2.ª y la 1.ª distancia de un diagrama de Voronoi animado; las líneas son los bordes.
            float Caustic(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float d1 = 8.0;
                float d2 = 8.0;
                [unroll] for (int y = -1; y <= 1; y++)
                {
                    [unroll] for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float2 o = Hash2(i + g);
                        o = 0.5 + 0.5 * sin(_Time.y * _RippleSpeed + 6.2831853 * o);
                        float2 r = g + o - f;
                        float d = dot(r, r);
                        if (d < d1) { d2 = d1; d1 = d; }
                        else if (d < d2) { d2 = d; }
                    }
                }
                return sqrt(d2) - sqrt(d1);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS.y += WaveOffset(positionWS);
                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                // Agua casi blanca = poco profundo + espuma: pasa cuando bajo la superficie hay algo a menos de _FoamWidth
                // (p. ej. el piso de la balsa cuando el agua lo cubre) o cuando la cámara no tiene su propia textura de
                // profundidad (capturas con cámara temporal). Dentro de la balsa lo resuelve RaftWaterMask.
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = IN.screenPos.w;
                float depthDiff = max(sceneDepth - surfaceDepth, 0.0);

                half4 water = lerp(_ShallowColor, _DeepColor, saturate(depthDiff / max(_DepthMax, 0.01)));

                float2 p = IN.positionWS.xz;
                float2 flow = normalize(_FlowDir.xy + 1e-5);
                float2 pf = p - flow * _Time.y * _FlowSpeed;

                // Rizos: red de líneas que se mueve con la corriente; más visible donde el agua es clara.
                half ripple = 1.0 - smoothstep(0.0, 0.16, Caustic(pf * _RippleScale));
                // Con la distancia los rizos y las chispas se apagan: de lejos la red de líneas parpadea (aliasing).
                half distanceFade = 1.0 - smoothstep(50.0, 220.0, distance(_WorldSpaceCameraPos, IN.positionWS));
                half rippleMix = ripple * _RippleStrength * lerp(1.0, 0.55, saturate(depthDiff / max(_DepthMax, 0.01))) * distanceFade;

                // Normal analítica de las dos octavas de olas para el brillo especular.
                float k = 6.2831853 / max(_WaveLength, 0.01);
                float t = _Time.y * _WaveSpeed;
                float2 d1 = float2(0.8, 0.6);
                float2 d2 = float2(-0.5, 0.87);
                float2 slope = d1 * cos(dot(p, d1) * k + t) + d2 * cos(dot(p, d2) * k * 2.0 + t * 1.3);
                float3 N = normalize(float3(-slope.x * _NormalStrength, 1.0, -slope.y * _NormalStrength));
                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 L = normalize(_MainLightPosition.xyz);
                float spec = pow(saturate(dot(N, normalize(L + V))), _SpecPower) * _SpecStrength;
                half specToon = smoothstep(0.45, 0.6, spec);

                // Chispas: puntos que parpadean sobre la superficie.
                float2 sp = pf * _SparkleScale;
                float2 cell = floor(sp);
                float h = Hash(cell);
                float twinkle = step(1.0 - _SparkleDensity, h) * (0.5 + 0.5 * sin(_Time.y * 4.0 + h * 40.0));
                twinkle *= (1.0 - smoothstep(0.05, 0.16, length(frac(sp) - 0.5))) * distanceFade;

                half fresnel = pow(1.0 - saturate(dot(N, V)), 4.0) * _FresnelStrength;

                half3 color = water.rgb;
                color = lerp(color, _SkyColor.rgb, fresnel);
                color += _RippleColor.rgb * rippleMix;
                color += _GlintColor.rgb * (specToon * 0.7 + twinkle);

                // La espuma se desplaza con la corriente.
                float2 uv = IN.positionWS.xz * _FoamScale * 0.2 - flow * _Time.y * _FlowSpeed;
                float pattern = ValueNoise(uv * 3.0) * 0.6 + ValueNoise(uv * 7.0) * 0.4;
                float edge = 1.0 - saturate(depthDiff / max(_FoamWidth, 0.01));
                half foam = step(0.5, edge * (0.6 + pattern * 0.8));

                color = lerp(color, _FoamColor.rgb, foam * _FoamColor.a);
                color = MixFog(color, IN.fogFactor);
                return half4(color, saturate(water.a + foam + rippleMix * 0.1));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
