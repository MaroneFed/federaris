// LE SOL ET LA ROCHE DE L'ILE (v49). Avant : cinq verts unis en damier, quatre bruns unis.
// Maintenant, pixel par pixel :
//   DESSUS  de l'herbe en grandes plaques de deux verts, des zones plus seches (jaunies),
//           des plaques de terre battue, le grain du sol et son relief ;
//   FLANCS  des STRATES de roche (les couches horizontales d'une falaise), de la mousse
//           qui deborde sous le bord, et la roche qui s'assombrit vers la pointe.
// La couleur des sommets dit a quelle profondeur on est sous le dessus (0 : en haut,
// 1 : a la pointe) -- l'ile, les ilots et les plateformes n'ont pas la meme hauteur.
Shader "Fief/Terrain"
{
    Properties
    {
        _MainTex ("Grain du sol", 2D) = "white" {}
        _BumpMap ("Relief du sol", 2D) = "bump" {}
        _RockTex ("Grain de la roche", 2D) = "white" {}
        _RockBump ("Relief de la roche", 2D) = "bump" {}
        _BumpScale ("Force du relief", Float) = 1
        _Grass ("Herbe", Color) = (0.36, 0.60, 0.24, 1)
        _GrassB ("Herbe (autre vert)", Color) = (0.46, 0.68, 0.28, 1)
        _GrassDry ("Herbe seche", Color) = (0.66, 0.68, 0.32, 1)
        _Dirt ("Terre", Color) = (0.52, 0.42, 0.30, 1)
        _Rock ("Roche", Color) = (0.62, 0.50, 0.42, 1)
        _RockDark ("Roche sombre", Color) = (0.42, 0.33, 0.30, 1)
        _MossColor ("Mousse", Color) = (0.30, 0.46, 0.20, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _RockTex;
        sampler2D _RockBump;
        sampler2D _FiefNoise;
        half _BumpScale;
        fixed4 _Grass;
        fixed4 _GrassB;
        fixed4 _GrassDry;
        fixed4 _Dirt;
        fixed4 _Rock;
        fixed4 _RockDark;
        fixed4 _MossColor;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float4 color : COLOR;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 geo = WorldNormalVector(IN, float3(0, 0, 1));
            float top = smoothstep(0.55, 0.8, geo.y);
            float3 p = IN.worldPos;
            float depth = IN.color.r;

            float m1 = tex2D(_FiefNoise, p.xz * 0.0085).r;
            float m2 = tex2D(_FiefNoise, p.xz * 0.047).g;
            float m3 = tex2D(_FiefNoise, p.xz * 0.37).b;

            // --- le dessus
            fixed3 ground = tex2D(_MainTex, IN.uv_MainTex).rgb;
            fixed3 grass = lerp(_Grass.rgb, _GrassB.rgb, smoothstep(0.3, 0.7, m1));
            grass = lerp(grass, _GrassDry.rgb, smoothstep(0.55, 0.8, m2) * 0.5);
            float dirt = smoothstep(0.66, 0.8, m2 * 0.55 + (1.0 - m1) * 0.5);
            grass = lerp(grass, _Dirt.rgb, dirt * 0.75);
            grass *= (0.88 + m3 * 0.24) * ground;

            // --- les flancs : des strates qui ondulent, la mousse en haut, l'ombre en bas
            fixed3 stone = tex2D(_RockTex, IN.uv_MainTex * 0.55).rgb;
            float layer = tex2D(_FiefNoise, float2((p.x + p.z) * 0.006, p.y * 0.055 + m1 * 0.25)).g;
            fixed3 rock = lerp(_Rock.rgb, _RockDark.rgb, smoothstep(0.38, 0.62, layer));
            rock *= (0.82 + m3 * 0.36) * stone;
            float moss = saturate(1.0 - depth * 9.0) * smoothstep(0.35, 0.6, m2);
            rock = lerp(rock, _MossColor.rgb * (0.8 + m3 * 0.4), moss * 0.7);
            rock *= lerp(1.0, 0.5, saturate(depth * 1.15));

            o.Albedo = lerp(rock, grass, top);
            o.Metallic = 0.0;
            o.Smoothness = lerp(0.14, 0.05, top);
            o.Alpha = 1.0;

            fixed3 n1 = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            fixed3 n2 = UnpackNormal(tex2D(_RockBump, IN.uv_MainTex * 0.55));
            n1.xy *= _BumpScale;
            n2.xy *= _BumpScale * 1.4;
            o.Normal = normalize(lerp(n2, n1, top));
        }
        ENDCG
    }
    FallBack "Diffuse"
}
