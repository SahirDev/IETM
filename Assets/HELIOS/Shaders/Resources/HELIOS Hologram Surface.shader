// HELIOS - the blueprint hologram surface: a see-through glowing body with an even grid of panel lines,
// a bright glow along the silhouette, and coloured highlight zones (engine, avionics, hydraulics, your own).
// Solid (opaque, hides what is behind it) or see-through (additive glow); HeliosApp sets which. Unlit;
// works in the built-in pipeline and in URP.
// Colours and sizes are set by HeliosApp (Inspector > Hologram); zones by HeliosModel and HeliosHoloZone.
Shader "HELIOS/Hologram Surface"
{
    Properties
    {
        _LineColor ("Line colour", Color) = (0.13, 0.83, 0.93, 1)
        _FillColor ("Body colour (alpha = strength)", Color) = (0.03, 0.18, 0.36, 0.35)
        _RimPower ("Outline sharpness", Range(0.5, 8)) = 2.6
        _RimStrength ("Outline glow", Range(0, 3)) = 1.25
        _GridSpacing ("Grid spacing (world units)", Float) = 0.32
        _GridWidth ("Grid line width (pixels)", Range(0.1, 3)) = 0.75
        _GridStrength ("Grid brightness", Range(0, 2)) = 0.6
        _ZoneGlow ("Zone glow", Range(0, 4)) = 1.6
        _Intensity ("Brightness", Range(0, 4)) = 1.2
        _BackFace ("Inside surfaces", Range(0, 1)) = 0.4
        _Solid ("Solid body (1) or see-through (0)", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Blend source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend destination", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Depth write", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _LineColor, _FillColor;
            float _RimPower, _RimStrength, _GridSpacing, _GridWidth, _GridStrength, _ZoneGlow, _Intensity, _BackFace, _Solid;
            float4 _HoloCenter;
            float _HoloZoneCount;
            float4x4 _HoloZoneW2L[12];
            float4 _HoloZoneColor[12];

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

            float3 ZoneColor(float3 wpos, float3 baseCol, out float inZone)
            {
                inZone = 0;
                float3 c = baseCol;
                [loop] for (int i = 0; i < 12; i++)
                {
                    if (i >= (int)_HoloZoneCount) break;
                    float3 l = mul(_HoloZoneW2L[i], float4(wpos, 1)).xyz;
                    if (all(abs(l) <= 0.5)) { c = _HoloZoneColor[i].rgb; inZone = _HoloZoneColor[i].a; }
                }
                return c;
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                float3 n = normalize(i.wn);
                float3 v = normalize(_WorldSpaceCameraPos - i.wpos);
                float ndv = saturate(abs(dot(n, v)));
                float rim = pow(1 - ndv, _RimPower) * _RimStrength;

                float3 p = (i.wpos - _HoloCenter.xyz) / max(_GridSpacing, 1e-4);
                float3 w = max(fwidth(p), 1e-4);
                float3 d = abs(frac(p - 0.5) - 0.5) / w;                    // pixels to the nearest grid line, per axis
                float3 an = abs(n);
                float3 g = (1 - saturate(d - _GridWidth)) * saturate(1.0 - an * 1.15);   // no lines along the axis the surface faces
                float grid = max(g.x, max(g.y, g.z)) * _GridStrength;

                float inZone;
                float3 lc = ZoneColor(i.wpos, _LineColor.rgb, inZone);
                float3 body = lerp(_FillColor.rgb * _FillColor.a, _FillColor.rgb, _Solid);    // solid: the full body colour, nothing shows through
                float3 col = body + lc * (rim + grid) + lc * inZone * _ZoneGlow * (0.25 + 0.75 * rim + 0.4 * grid);
                col *= _Intensity;                                       // both sides alike (surface model)
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
