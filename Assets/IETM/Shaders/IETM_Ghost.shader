// Transparent "x-ray" ghost shader for context parts (URP).
// Faint body + fresnel edge, no depth write, so isolated parts stay visible through it.
Shader "IETM/Ghost"
{
    Properties
    {
        _Color ("Color", Color) = (0.45, 0.65, 1, 0.06)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.0
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _RimPower;
                half _RimStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half rim = pow(1.0h - saturate(abs(dot(normalize(i.normalWS), normalize(i.viewWS)))), _RimPower);
                return half4(_Color.rgb + rim * 0.3h, saturate(_Color.a + rim * _RimStrength));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
