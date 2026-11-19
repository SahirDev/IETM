// Opaque highlight shader for selected / hovered parts (URP + Built-in).
// Simple lit colour + strong fresnel rim so the selection reads from any angle.
Shader "IETM/Highlight"
{
    Properties
    {
        _Color ("Color", Color) = (0.13, 0.83, 0.93, 1)
        _RimColor ("Rim Color", Color) = (0.8, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RimColor;
                half _RimPower;
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

            half4 frag (Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                n = front ? n : -n;
                float3 v = normalize(i.viewWS);
                Light l = GetMainLight();
                half ndl = saturate(dot(n, l.direction)) * 0.6h + 0.4h;
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower);
                half3 c = _Color.rgb * ndl + _RimColor.rgb * rim * 0.9h;
                return half4(c, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert (A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half frag (V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    // Built-in render pipeline (used when no URP asset is active, e.g. the HELIOS project)
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardBuiltIn"
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            fixed4 _RimColor;
            half _RimPower;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.viewWS = WorldSpaceViewDir(v.vertex);
                return o;
            }
            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                n = facing > 0 ? n : -n;
                float3 v = normalize(i.viewWS);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                half ndl = saturate(dot(n, l)) * 0.6 + 0.4;
                half rim = pow(1.0 - saturate(dot(n, v)), _RimPower);
                return fixed4(_Color.rgb * ndl + _RimColor.rgb * rim * 0.9, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
