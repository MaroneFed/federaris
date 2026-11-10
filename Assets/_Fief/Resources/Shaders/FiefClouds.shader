// LA MER DE NUAGES (v49). Avant : de grandes taches plates et floues. Maintenant, une
// vraie SURFACE : un maillage dont les sommets montent et descendent comme une houle de
// coton (un bruit qui derive), eclaire par le soleil -- les cretes blanches et dorees,
// les creux mauves --, qui se fond dans la brume au loin.
Shader "Fief/Clouds"
{
    Properties
    {
        _Lit ("Cretes", Color) = (1.0, 0.97, 0.93, 1)
        _Dark ("Creux", Color) = (0.72, 0.62, 0.78, 1)
        _SunTint ("Reflet du soleil", Color) = (1.0, 0.82, 0.58, 1)
        _Height ("Hauteur de la houle", Float) = 11
        _Scale ("Taille des nuages", Float) = 0.0035
        _Speed ("Derive", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _FiefNoise;
            fixed4 _FiefCloudTint;
            fixed4 _Lit;
            fixed4 _Dark;
            fixed4 _SunTint;
            float _Height;
            float _Scale;
            float _Speed;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            float Billow(float2 uv)
            {
                float a = tex2Dlod(_FiefNoise, float4(uv, 0, 0)).r;
                float b = tex2Dlod(_FiefNoise, float4(uv * 2.7 + 0.31, 0, 0)).g;
                return a * 0.72 + b * 0.28;
            }

            v2f vert(appdata_base v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 uv = wp.xz * _Scale + _Time.y * _Speed * float2(0.0021, 0.0012);
                float hgt = Billow(uv);
                wp.y += (hgt - 0.5) * 2.0 * _Height;
                o.world = wp;
                o.uv = uv;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // La pente de la houle, lue dans le bruit : c'est elle qui prend la lumiere.
                float e = 0.012;
                float h0 = tex2D(_FiefNoise, i.uv).r * 0.72 + tex2D(_FiefNoise, i.uv * 2.7 + 0.31).g * 0.28;
                float hx = tex2D(_FiefNoise, i.uv + float2(e, 0)).r * 0.72 + tex2D(_FiefNoise, (i.uv + float2(e, 0)) * 2.7 + 0.31).g * 0.28;
                float hz = tex2D(_FiefNoise, i.uv + float2(0, e)).r * 0.72 + tex2D(_FiefNoise, (i.uv + float2(0, e)) * 2.7 + 0.31).g * 0.28;
                float fine = tex2D(_FiefNoise, i.uv * 9.0).b;
                float3 n = normalize(float3((h0 - hx) * 9.0, 1.0, (h0 - hz) * 9.0));
                float3 sun = normalize(_WorldSpaceLightPos0.xyz);
                float light = saturate(dot(n, sun) * 0.6 + 0.4);
                float crest = smoothstep(0.35, 0.75, h0 + (fine - 0.5) * 0.12);
                half3 col = lerp(_Dark.rgb, _Lit.rgb, saturate(crest * 0.7 + light * 0.5));
                // Le soleil rasant dore les cretes qui lui font face.
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float glint = pow(saturate(dot(normalize(sun + view), n)), 12.0);
                col += _SunTint.rgb * (glint * 0.35 + crest * light * 0.12);
                col *= _FiefCloudTint.rgb;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return half4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
