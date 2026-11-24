// HELIOS - cross-section look (Built-in render pipeline). Used only while the Cross Section tool is on: the model's
// materials are swapped for this shader (colour, texture, normal map, metallic and smoothness are copied).
// - Everything on the positive side of _HeliosSectionPlane is cut away.
// - Both sides of every surface are drawn and lit from the side that faces the camera, so the model can be a surface
//   model with panels facing either way (no face turns another colour because its normal or winding is flipped).
// - Where the plane cuts through a surface, a band of _HeliosSectionEdge world units is drawn in _CapColor:
//   the outline of the cross-section.
// _HeliosSectionPlane (world space): xyz = normal, w = -dot(normal, point).
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
        // WebGL: one pass per part (extra lights as per-vertex / SH), no lightmap / fog / deferred variants
        #pragma surface surf Standard addshadow noforwardadd exclude_path:deferred exclude_path:prepass nolightmap nodynlightmap nodirlightmap nometa nofog
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        fixed4 _CapColor;
        half _Metallic;
        half _Glossiness;
        float4 _HeliosSectionPlane;
        float _HeliosSectionEdge;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float d = -(dot(IN.worldPos, _HeliosSectionPlane.xyz) + _HeliosSectionPlane.w);   // distance into the kept side
            clip(d);

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            // light the side that faces the camera (tangent space: viewDir and the normal are in the same space)
            if (IN.viewDir.z < 0) n.z = -n.z;
            o.Normal = n;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;

            // the cut outline
            float edge = max(_HeliosSectionEdge, 1e-4);
            float k = 1 - saturate(d / edge);
            o.Albedo = lerp(o.Albedo, _CapColor.rgb * 0.3, k);
            o.Emission = _CapColor.rgb * k;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
