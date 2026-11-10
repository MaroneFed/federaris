// LA MATIERE DE TOUT LE JEU (v49 -- Martin : "ameliore tout, les textures, de A a Z").
// Jusqu'ici, chaque objet etait une couleur unie (le shader "Standard" d'Unity). Celui-ci
// garde le meme eclairage (physique : lisse, metal, ombres) et ajoute, sur TOUT :
//   - un GRAIN lu dans l'espace du monde : de grandes plaques qui nuancent la teinte
//     (plus de murs "peints d'un seul coup de rouleau") et un grain fin de pres ;
//   - un LISERE de ciel sur les bords vus de biais (l'effet "Fresnel") : les formes se
//     detachent du fond, comme eclairees par le ciel derriere elles.
// Pas de carte de relief ici (elle demande des "tangentes" que tous nos maillages n'ont
// pas) : c'est Fief/LitBump qui s'en charge, pour le sol et la pierre.
Shader "Fief/Lit"
{
    Properties
    {
        _Color ("Couleur", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _Glossiness ("Lisse", Range(0, 1)) = 0.05
        _Metallic ("Metal", Range(0, 1)) = 0
        _EmissionColor ("Lueur", Color) = (0, 0, 0, 1)
        _Grain ("Grain", Range(0, 1)) = 0.6
        _Rim ("Lisere de ciel", Range(0, 2)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma shader_feature _EMISSION
        #pragma shader_feature _NORMALMAP

        sampler2D _MainTex;
        sampler2D _FiefNoise;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        half4 _EmissionColor;
        half _Grain;
        half _Rim;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Le grain, plaque sur les trois axes du monde (pas besoin d'UV).
            float3 w = abs(IN.worldNormal);
            w = w / (w.x + w.y + w.z + 0.0001);
            float3 p = IN.worldPos;
            float big = tex2D(_FiefNoise, p.zy * 0.031).r * w.x + tex2D(_FiefNoise, p.xz * 0.031).r * w.y + tex2D(_FiefNoise, p.xy * 0.031).r * w.z;
            float fine = tex2D(_FiefNoise, p.zy * 0.42).b * w.x + tex2D(_FiefNoise, p.xz * 0.42).b * w.y + tex2D(_FiefNoise, p.xy * 0.42).b * w.z;
            float shade = 1.0 + ((big - 0.5) * 0.26 + (fine - 0.5) * 0.12) * _Grain;
            c.rgb *= shade;

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;

            // Le lisere : le ciel accroche les bords.
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normalize(IN.worldNormal)));
            rim = rim * rim * rim;
            o.Emission = _EmissionColor.rgb + c.rgb * unity_AmbientSky.rgb * (rim * _Rim);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
