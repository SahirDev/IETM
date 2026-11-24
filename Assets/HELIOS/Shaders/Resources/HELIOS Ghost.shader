// HELIOS - the Transparent look (View Tools > Transparent): each part keeps its own colour and texture, drawn as
// soft see-through glass - faces facing you are faint, edges turning away are stronger - so the cockpit, engine and
// stores inside show through. Matte (low specular), no wireframe. Both sides of every face are drawn and lit.
// Built-in Render Pipeline. In Resources so Shader.Find finds it in builds (WebGL).
Shader "HELIOS/Ghost"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Colour", Color) = (1,1,1,1)
        _Cutoff ("Alpha cut-out (0 = off)", Range(0,1)) = 0
        _Tint ("Tint", Color) = (0.82,0.85,0.9,1)
        _KeepColour ("Keep part colour (0 = grey)", Range(0,1)) = 0.55
        _Alpha ("Face opacity", Range(0,1)) = 0.2
        _EdgeAlpha ("Edge opacity", Range(0,1)) = 0.75
        _FresnelPower ("Edge sharpness", Range(0.5,8)) = 2.2
        _Brightness ("Brightness", Range(0,3)) = 1.25
        _Specular ("Shine", Range(0,1)) = 0.35
        _Reflection ("Glass reflection", Range(0,1)) = 0.45
        [HideInInspector] _WireColor ("Wireframe colour (per part, alpha 0 = off)", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _Tint;
            half _Cutoff, _KeepColour, _Alpha, _EdgeAlpha, _FresnelPower, _Brightness, _Specular, _Reflection;
            fixed4 _WireColor;
            float4 _HoloCenter;
            float _HeliosGridSpacing, _HeliosGridWidth, _HeliosGridStrength, _HeliosRimPower, _HeliosRimStrength, _HeliosWireOpacity;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 wp : TEXCOORD2; };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;
                if (_Cutoff > 0) clip(tex.a - _Cutoff);
                float3 n = normalize(i.n) * (facing > 0 ? 1 : -1);          // the side facing the camera
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                half ndv = saturate(abs(dot(n, v)));
                // glass in the part's own colour: soft wrapped light, a smooth sharp highlight, the sky reflected at
                // grazing angles (Fresnel), faces you look through stay clear, edges turning away get denser
                half grey = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                half3 albedo = lerp(grey.xxx, tex.rgb, _KeepColour) * _Tint.rgb;
                albedo = max(albedo, 0.12);                                   // near-black parts stay readable
                half wrap = saturate(dot(n, l)) * 0.5 + 0.5;
                half3 amb = ShadeSH9(float4(n, 1));
                half3 h = normalize(l + v);
                half spec = pow(saturate(dot(n, h)), 96) * _Specular * 4;     // small, smooth glass highlight
                half F = 0.04 + 0.96 * pow(1 - ndv, 5);                       // Fresnel (glass)
                half3 env = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflect(-v, n), 1), unity_SpecCube0_HDR);
                half3 col = albedo * (_LightColor0.rgb * wrap + amb) * _Brightness;
                col = lerp(col, env + albedo * 0.25, F * _Reflection) + spec * _LightColor0.rgb;
                half fr = pow(1 - ndv, _FresnelPower);
                half a = saturate(lerp(_Alpha, _EdgeAlpha, fr) + spec * 0.6 + F * _Reflection * 0.35);
                if (_WireColor.a > 0.001)                                     // Wireframe on top of the glass
                {
                    float3 p = (i.wp - _HoloCenter.xyz) / max(_HeliosGridSpacing, 1e-4);
                    float3 w = max(fwidth(p), 1e-4);
                    float3 d = abs(frac(p - 0.5) - 0.5) / w;
                    float3 g = (1 - saturate(d - _HeliosGridWidth)) * saturate(1.0 - abs(n) * 1.15);
                    half rim = pow(1 - ndv, max(_HeliosRimPower, 0.5)) * _HeliosRimStrength;
                    half wa = saturate(max(max(g.x, max(g.y, g.z)) * _HeliosGridStrength, rim)) * _HeliosWireOpacity * _WireColor.a;
                    col = lerp(col, _WireColor.rgb * 1.2, wa);
                    a = max(a, wa);
                }
                return fixed4(col, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
