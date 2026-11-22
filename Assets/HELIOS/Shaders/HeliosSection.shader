// HELIOS - cross-section look (Built-in render pipeline). Used only while the Cross Section tool is on: the model's
// materials are swapped for this shader (their colour, texture, metallic and smoothness are copied), everything on the
// positive side of _HeliosSectionPlane is cut away, and the inside faces that the cut exposes are drawn in _CapColor,
// so the section reads as a solid slice. _HeliosSectionPlane (world space): xyz = normal, w = -dot(normal, point).
Shader "HELIOS/Section (Built-in)"
{
    Properties
    {
        _Color ("Colour", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _CapColor ("Section Colour", Color) = (0.13, 0.83, 0.93, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        fixed4 _CapColor;
        half _Metallic;
        half _Glossiness;
        float4 _HeliosSectionPlane;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float face : VFACE;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // cut: keep the side the normal points away from
            clip(-(dot(IN.worldPos, _HeliosSectionPlane.xyz) + _HeliosSectionPlane.w));
            if (IN.face < 0)
            {
                // inside of the shell, seen through the cut: the section face
                o.Albedo = _CapColor.rgb * 0.35;
                o.Emission = _CapColor.rgb * 0.65;
                o.Metallic = 0;
                o.Smoothness = 0.2;
                o.Alpha = 1;
            }
            else
            {
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = 1;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
