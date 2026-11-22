// HELIOS - wireframe lines drawn OVER a part's own material: a fine grid of panel lines and a thin glow along the
// silhouette, in the part's criteria colour. The part keeps its own materials and textures (this is a second,
// see-through layer on top). Unlit; works in the built-in pipeline and in URP.
// HeliosApp > Wireframe sets the spacing, line width and brightness; each part gets its own colour.
Shader "HELIOS/Wireframe Overlay"
{
    Properties
    {
        _LineColor ("Line colour", Color) = (0.13, 0.83, 0.93, 1)
        _GridSpacing ("Grid spacing (world units)", Float) = 0.11
        _GridWidth ("Grid line width (pixels)", Range(0.05, 3)) = 0.4
        _GridStrength ("Grid opacity", Range(0, 1)) = 0.9
        _RimPower ("Outline sharpness", Range(0.5, 8)) = 3
        _RimStrength ("Outline glow", Range(0, 1)) = 0.35
        _Opacity ("Opacity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off                // surface model: lines on both sides of every panel
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _LineColor;
            float _GridSpacing, _GridWidth, _GridStrength, _RimPower, _RimStrength, _Opacity;
            float4 _HoloCenter;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 wn : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wn = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.wn);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                float rim = pow(1 - saturate(abs(dot(n, v))), _RimPower) * _RimStrength;
                float3 p = (i.wpos - _HoloCenter.xyz) / max(_GridSpacing, 1e-4);
                float3 w = max(fwidth(p), 1e-4);
                float3 d = abs(frac(p - 0.5) - 0.5) / w;                     // pixels to the nearest grid line, per axis
                float3 g = (1 - saturate(d - _GridWidth)) * saturate(1.0 - abs(n) * 1.15);
                float grid = max(g.x, max(g.y, g.z)) * _GridStrength;
                float a = saturate(max(grid, rim)) * _Opacity * _LineColor.a;
                return fixed4(_LineColor.rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
