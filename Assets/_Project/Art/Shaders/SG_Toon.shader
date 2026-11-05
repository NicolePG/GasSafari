// Toon de Gas Safari (docs/09 §2): rampa de luz de 3 bandas, color base + textura, rim light suave y
// color de sombra por mundo (_ShadowColor). Escrito en HLSL URP en lugar de Shader Graph.
Shader "GasSafari/SG_Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Textura base", 2D) = "white" {}
        [MainColor] _BaseColor("Color base", Color) = (1, 1, 1, 1)
        _ShadowColor("Color de sombra (por mundo)", Color) = (0.35, 0.3, 0.5, 1)
        _Band1("Umbral banda sombra", Range(0, 1)) = 0.35
        _Band2("Umbral banda luz", Range(0, 1)) = 0.7
        _BandSoftness("Suavizado de bandas", Range(0.001, 0.2)) = 0.02
        _RimColor("Color rim", Color) = (1, 1, 1, 1)
        _RimPower("Potencia rim", Range(0.5, 8)) = 3
        _RimStrength("Fuerza rim", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half _Band1;
                half _Band2;
                half _BandSoftness;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 normal = normalize(IN.normalWS);
                half3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light light = GetMainLight(shadowCoord);

                // 0..1: más alto = más iluminado; la sombra proyectada baja el valor.
                half lit = (dot(normal, light.direction) * 0.5h + 0.5h) * light.shadowAttenuation;

                // Tres tonos: sombra, medio y luz.
                half3 dark = albedo * _ShadowColor.rgb;
                half3 mid = albedo * lerp(_ShadowColor.rgb, half3(1, 1, 1), 0.5h);
                half s1 = smoothstep(_Band1 - _BandSoftness, _Band1 + _BandSoftness, lit);
                half s2 = smoothstep(_Band2 - _BandSoftness, _Band2 + _BandSoftness, lit);
                half3 shaded = lerp(lerp(dark, mid, s1), albedo, s2);

                half3 color = shaded * light.color + albedo * SampleSH(normal) * 0.25h;

                half rim = pow(1.0h - saturate(dot(normal, viewDir)), _RimPower) * _RimStrength * s1;
                color += _RimColor.rgb * rim;

                color = MixFog(color, IN.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Universal Render Pipeline/Lit"
}
