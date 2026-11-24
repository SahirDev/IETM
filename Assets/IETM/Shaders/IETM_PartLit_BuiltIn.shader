// Landing gear part material for the Built-in render pipeline (the HELIOS project runs without a URP asset).
// Same look as URP/Lit with Render Face = Both: Standard (metallic) lighting, double-sided, back faces lit correctly.
Shader "IETM/Part Lit (Built-in)"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        CGPROGRAM
        // WebGL: one pass per part (extra lights as per-vertex / SH), no lightmap / fog / deferred variants
        #pragma surface surf Standard addshadow noforwardadd exclude_path:deferred exclude_path:prepass nolightmap nodynlightmap nodirlightmap nometa nofog
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Metallic;
        half _Glossiness;
        struct Input { float2 uv_MainTex; fixed facing : VFACE; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
            o.Normal = IN.facing > 0 ? float3(0, 0, 1) : float3(0, 0, -1);   // light the inside faces too
        }
        ENDCG
    }
    FallBack "Diffuse"
}
