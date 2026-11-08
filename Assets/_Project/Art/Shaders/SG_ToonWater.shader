// Agua toon de Gas Safari (docs/09 §2): color por profundidad (requiere Depth Texture en el URP Asset),
// espuma en intersecciones, olas por vértice (seno, 2 octavas) y desplazamiento de UV según la corriente.
// Escrito en HLSL URP en lugar de Shader Graph. _FlowDir (xz) lo fija el nivel con la dirección de la corriente.
Shader "GasSafari/SG_ToonWater"
{
    Properties
    {
        _ShallowColor("Color poco profundo", Color) = (0.36, 0.85, 0.85, 0.75)
        _DeepColor("Color profundo", Color) = (0.07, 0.35, 0.55, 0.95)
        _DepthMax("Profundidad del color profundo (m)", Float) = 3
        _FoamColor("Color espuma", Color) = (1, 1, 1, 1)
        _FoamWidth("Ancho de espuma (m)", Float) = 0.4
        _FoamScale("Escala del patrón de espuma", Float) = 2.5
        _WaveHeight("Altura de olas (m)", Float) = 0.08
        _WaveLength("Longitud de ola (m)", Float) = 6
        _WaveSpeed("Velocidad de olas", Float) = 1
        _FlowDir("Dirección de corriente (xz)", Vector) = (0, 1, 0, 0)
        _FlowSpeed("Velocidad de corriente de UV", Float) = 0.4
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

                // La espuma se desplaza con la corriente.
                float2 flow = normalize(_FlowDir.xy + 1e-5);
                float2 uv = IN.positionWS.xz * _FoamScale * 0.2 - flow * _Time.y * _FlowSpeed;
                float pattern = ValueNoise(uv * 3.0) * 0.6 + ValueNoise(uv * 7.0) * 0.4;
                float edge = 1.0 - saturate(depthDiff / max(_FoamWidth, 0.01));
                half foam = step(0.5, edge * (0.6 + pattern * 0.8));

                half3 color = lerp(water.rgb, _FoamColor.rgb, foam * _FoamColor.a);
                color = MixFog(color, IN.fogFactor);
                return half4(color, saturate(water.a + foam));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
