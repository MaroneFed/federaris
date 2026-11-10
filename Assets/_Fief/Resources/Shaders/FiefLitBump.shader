// LA MATIERE A RELIEF (v49) : la meme que Fief/Lit, avec une CARTE DE RELIEF (les joints
// de la pierre, les cailloux du sol). Elle sert la ou le maillage a ses "tangentes" :
// la maconnerie du chateau (Masonry) et les matieres du sol (Surfaces).
Shader "Fief/LitBump"
{
    Properties
    {
        _Color ("Couleur", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _BumpMap ("Relief", 2D) = "bump" {}
        _BumpScale ("Force du relief", Float) = 1
        _Glossiness ("Lisse", Range(0, 1)) = 0.05
        _Metallic ("Metal", Range(0, 1)) = 0
        _EmissionColor ("Lueur", Color) = (0, 0, 0, 1)
        _Grain ("Grain", Range(0, 1)) = 0.6
        _Rim ("Lisere de ciel", Range(0, 2)) = 0.3
        _Moss ("Mousse au pied", Range(0, 1)) = 0
        _MossColor ("Couleur de la mousse", Color) = (0.36, 0.5, 0.22, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma shader_feature _EMISSION
        #pragma shader_feature _NORMALMAP

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _FiefNoise;
        fixed4 _Color;
        half _BumpScale;
        half _Glossiness;
        half _Metallic;
        half4 _EmissionColor;
        half _Grain;
        half _Rim;
        half _Moss;
        fixed4 _MossColor;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            float3 geo = WorldNormalVector(IN, float3(0, 0, 1));
            float3 w = abs(geo);
            w = w / (w.x + w.y + w.z + 0.0001);
            float3 p = IN.worldPos;
            float big = tex2D(_FiefNoise, p.zy * 0.027).r * w.x + tex2D(_FiefNoise, p.xz * 0.027).r * w.y + tex2D(_FiefNoise, p.xy * 0.027).r * w.z;
            float mid = tex2D(_FiefNoise, p.zy * 0.11).g * w.x + tex2D(_FiefNoise, p.xz * 0.11).g * w.y + tex2D(_FiefNoise, p.xy * 0.11).g * w.z;
            c.rgb *= 1.0 + ((big - 0.5) * 0.3 + (mid - 0.5) * 0.14) * _Grain;

            // La mousse et la salissure au pied des murs : les deux premiers metres, en taches.
            float foot = saturate(1.0 - p.y / 2.6) * step(-0.5, p.y);
            float patch = smoothstep(0.42, 0.62, mid * 0.6 + big * 0.5);
            float moss = foot * patch * _Moss;
            c.rgb = lerp(c.rgb, _MossColor.rgb * (0.75 + mid * 0.5), moss * 0.75);
            // Un peu plus sombre tout en bas (la ou le mur rencontre le sol).
            c.rgb *= lerp(1.0, 0.82, saturate(1.0 - p.y / 0.9) * step(-0.5, p.y) * _Moss);

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness * (1.0 - moss);
            o.Alpha = 1.0;

            fixed3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            n.xy *= _BumpScale;
            o.Normal = normalize(n);

            // (viewDir est dans le repere de la surface quand on ecrit o.Normal : (0,0,1) = la face.)
            half rim = 1.0 - saturate(normalize(IN.viewDir).z);
            rim = rim * rim * rim;
            o.Emission = _EmissionColor.rgb + c.rgb * unity_AmbientSky.rgb * (rim * _Rim);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
