// HELIOS - the blueprint hologram outlines: the model's real edges (where the surface bends), glowing,
// coloured by the same highlight zones as the surface. Additive and unlit; built-in pipeline and URP.
Shader "HELIOS/Hologram Lines"
{
    Properties
    {
        _LineColor ("Line colour", Color) = (0.13, 0.83, 0.93, 1)
        _EdgeStrength ("Edge brightness", Range(0, 3)) = 1.1
        _Intensity ("Brightness", Range(0, 4)) = 1.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+1" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            Offset -1, -1                 // drawn just in front of the body, so a solid body hides only the edges behind it
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _LineColor;
            float _EdgeStrength, _Intensity;
            float _HoloZoneCount;
            float4x4 _HoloZoneW2L[12];
            float4 _HoloZoneColor[12];

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 c = _LineColor.rgb;
                float boost = 0;
                [loop] for (int k = 0; k < 12; k++)
                {
                    if (k >= (int)_HoloZoneCount) break;
                    float3 l = mul(_HoloZoneW2L[k], float4(i.wpos, 1)).xyz;
                    if (all(abs(l) <= 0.5)) { c = _HoloZoneColor[k].rgb; boost = _HoloZoneColor[k].a; }
                }
                return fixed4(c * _EdgeStrength * _Intensity * (1 + 0.35 * boost), 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
