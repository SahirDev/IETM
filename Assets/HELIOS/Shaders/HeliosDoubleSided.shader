// HELIOS - the aircraft's own look, double sided (Built-in render pipeline). The model is a surface model (single
// panels, normals facing either way), so every surface is drawn on both sides and lit from the side that faces the
// camera: no black or missing panels in the normal view, under the Wireframe lines or through openings.
// Materials made by Tools > HELIOS > Set Up Scene from the model's own glTF materials (colour, texture, normal map,
// metallic, smoothness, emission and alpha cut-off copied).
Shader "HELIOS/Double Sided Lit (Built-in)"
{
    Properties
    {
        _Color ("Colour", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        _Cutoff ("Alpha Cut-off (0 = opaque)", Range(0,1)) = 0
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
        sampler2D _EmissionMap;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _Metallic;
        half _Glossiness;
        half _Cutoff;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            clip(c.a - _Cutoff);
            float3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            if (IN.viewDir.z < 0) n.z = -n.z;          // lit from the side that faces the camera
            o.Normal = n;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
