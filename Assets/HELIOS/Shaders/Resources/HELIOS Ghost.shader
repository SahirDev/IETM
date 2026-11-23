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
        _Specular ("Shine", Range(0,1)) = 0.08
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
            half _Cutoff, _KeepColour, _Alpha, _EdgeAlpha, _FresnelPower, _Brightness, _Specular;

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
                half grey = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                half3 albedo = lerp(grey.xxx, tex.rgb, _KeepColour) * _Tint.rgb;
                albedo = max(albedo, 0.18);                                  // near-black parts stay readable as glass
                half wrap = saturate(dot(n, l)) * 0.55 + 0.45;               // soft, wrapped light
                half3 amb = ShadeSH9(float4(n, 1));
                half3 h = normalize(l + v);
                half spec = pow(saturate(dot(n, h)), 20) * _Specular;
                half3 col = albedo * (_LightColor0.rgb * wrap + amb) * _Brightness + spec * _LightColor0.rgb;
                half fr = pow(1 - saturate(abs(dot(n, v))), _FresnelPower);
                return fixed4(col, lerp(_Alpha, _EdgeAlpha, fr));
            }
            ENDCG
        }
    }
    Fallback Off
}
