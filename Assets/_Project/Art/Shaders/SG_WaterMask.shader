// Máscara de agua (docs/09 §2): malla interior de la balsa que solo escribe profundidad, sin color.
// Se dibuja al final de la cola opaca (Geometry+500): los animales y la balsa ya están en el buffer y no se tapan,
// y el agua (cola Transparent, ZTest) descarta lo que queda detrás de la máscara, o sea, el agua dentro del bote.
Shader "GasSafari/SG_WaterMask"
{
    Properties { }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+500" "IgnoreProjector" = "True" }

        Pass
        {
            Name "WaterMask"
            Tags { "LightMode" = "UniversalForward" }
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
