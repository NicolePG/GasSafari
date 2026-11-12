// Cielo de Gas Safari: degradado cenit-horizonte-suelo, nubes toon de dos tonos que avanzan despacio (ruido fractal
// proyectado sobre un plano alto), brillo de sol opcional y estrellas para los mundos de noche.
// El color del horizonte coincide con el de la niebla del mundo para que las siluetas lejanas se fundan con el cielo.
Shader "GasSafari/SG_SkyClouds"
{
    Properties
    {
        _TopColor("Color del cenit", Color) = (0.3, 0.6, 0.9, 1)
        _HorizonColor("Color del horizonte (= niebla)", Color) = (0.75, 0.88, 0.95, 1)
        _GroundColor("Color bajo el horizonte", Color) = (0.55, 0.68, 0.75, 1)
        _HorizonPower("Curva del degradado", Range(0.2, 2)) = 0.55
        _CloudColor("Color de nube iluminada", Color) = (1, 1, 1, 1)
        _CloudShade("Color de nube en sombra", Color) = (0.72, 0.8, 0.9, 1)
        _CloudCover("Cobertura (más alto = menos nubes)", Range(0, 1)) = 0.5
        _CloudSoft("Suavidad del borde", Range(0.01, 0.4)) = 0.08
        _CloudScale("Escala de las nubes", Float) = 1.4
        _CloudSpeed("Velocidad de las nubes", Float) = 0.01
        _CloudOpacity("Opacidad de las nubes", Range(0, 1)) = 0.9
        _SunGlow("Brillo del sol (rgb, a = tamaño)", Color) = (1, 0.9, 0.6, 0.0)
        _StarAmount("Estrellas (0 = ninguna)", Range(0, 1)) = 0
        _StarColor("Color de estrellas", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Sky"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                float _HorizonPower;
                half4 _CloudColor;
                half4 _CloudShade;
                float _CloudCover;
                float _CloudSoft;
                float _CloudScale;
                float _CloudSpeed;
                half _CloudOpacity;
                half4 _SunGlow;
                half _StarAmount;
                half4 _StarColor;
            CBUFFER_END

            // Global: 1 dentro de una zona oscura (DarkZone), 0 en el resto.
            float _GS_SkyDark;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Hash3(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amp = 0.5;
                [unroll] for (int i = 0; i < 5; i++)
                {
                    sum += ValueNoise(p) * amp;
                    p = p * 2.03 + 17.0;
                    amp *= 0.5;
                }
                return sum;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dir = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.dir);
                float h = dir.y;

                half3 col = h >= 0.0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), _HorizonPower))
                    : lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-h * 6.0));

                // Brillo del sol (la luz principal): halo suave.
                float3 sunDir = normalize(_MainLightPosition.xyz);
                float sunDot = saturate(dot(dir, sunDir));
                col += _SunGlow.rgb * pow(sunDot, lerp(256.0, 12.0, saturate(_SunGlow.a))) * step(0.0001, _SunGlow.a);

                // Estrellas: celdas del cielo con una estrella que titila.
                if (_StarAmount > 0.001 && h > 0.0)
                {
                    float3 cell = floor(dir * 140.0);
                    float rnd = Hash3(cell);
                    float star = step(1.0 - 0.012 * _StarAmount, rnd) * (0.6 + 0.4 * sin(_Time.y * 2.0 + rnd * 60.0));
                    col += _StarColor.rgb * star * saturate(h * 3.0);
                }

                // Nubes: plano alto proyectado; se desvanecen hacia el horizonte.
                if (h > 0.0)
                {
                    float2 uv = dir.xz / (h + 0.18) * _CloudScale + float2(_Time.y * _CloudSpeed, 0.0);
                    float n = Fbm(uv);
                    float cover = smoothstep(_CloudCover, _CloudCover + _CloudSoft, n);
                    // Dos tonos: el lado que mira al sol queda iluminado.
                    float2 toSun = normalize(sunDir.xz + 1e-4) * 0.06;
                    float lit = smoothstep(-0.02, 0.05, n - Fbm(uv + toSun) + 0.015);
                    half3 cloud = lerp(_CloudShade.rgb, _CloudColor.rgb, lit);
                    cover *= smoothstep(0.0, 0.22, h) * _CloudOpacity;
                    col = lerp(col, cloud, cover);
                }
                col *= 1.0 - saturate(_GS_SkyDark);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
