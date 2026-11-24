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
        [HideInInspector] _WireColor ("Wireframe colour (per part, alpha 0 = off)", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Off

        CGPROGRAM
        // WebGL: one pass per part (extra lights as per-vertex / SH), no lightmap / fog / deferred variants
        // Wireframe drawn in the same pass (per-part colour from a MaterialPropertyBlock, settings from globals set by
        // HeliosModel.UpdateWireframe): no extra objects, no extra draw calls, nothing built when Wireframe is turned on.
        #pragma surface surf Standard vertex:vert addshadow noforwardadd exclude_path:deferred exclude_path:prepass nolightmap nodynlightmap nodirlightmap nometa nofog
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _EmissionMap;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _Metallic;
        half _Glossiness;
        half _Cutoff;
        fixed4 _WireColor;
        float4 _HoloCenter;
        float _HeliosGridSpacing, _HeliosGridWidth, _HeliosGridStrength, _HeliosRimPower, _HeliosRimStrength, _HeliosWireOpacity;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
            float3 wn;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.wn = UnityObjectToWorldNormal(v.normal);
        }

        // grid of panel lines (world space) + silhouette glow, as the HELIOS Wireframe Overlay shader draws it
        half WireAmount (Input IN)
        {
            float3 n = normalize(IN.wn);
            float3 v = normalize(_WorldSpaceCameraPos - IN.worldPos);
            float rim = pow(1 - saturate(abs(dot(n, v))), max(_HeliosRimPower, 0.5)) * _HeliosRimStrength;
            float3 p = (IN.worldPos - _HoloCenter.xyz) / max(_HeliosGridSpacing, 1e-4);
            float3 w = max(fwidth(p), 1e-4);
            float3 d = abs(frac(p - 0.5) - 0.5) / w;
            float3 g = (1 - saturate(d - _HeliosGridWidth)) * saturate(1.0 - abs(n) * 1.15);
            float grid = max(g.x, max(g.y, g.z)) * _HeliosGridStrength;
            return saturate(max(grid, rim)) * _HeliosWireOpacity * _WireColor.a;
        }

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
            if (_WireColor.a > 0.001)
            {
                half a = WireAmount(IN);
                o.Albedo = lerp(o.Albedo, o.Albedo * 0.35, a);
                o.Emission += _WireColor.rgb * a * 1.25;
            }
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
