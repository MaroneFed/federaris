// L'HERBE (v49). Des milliers de touffes dessinees d'un coup par la carte graphique
// (l'"instanciation" : un seul maillage, une liste de positions). Le VENT les couche par
// vagues (un bruit qui defile), et elles s'ecartent sous les pieds du joueur.
// La hauteur dans le brin (0 au pied, 1 a la pointe) est dans la coordonnee V de la texture.
Shader "Fief/Grass"
{
    Properties
    {
        _Color ("Vert", Color) = (0.34, 0.58, 0.22, 1)
        _ColorB ("Autre vert", Color) = (0.46, 0.68, 0.28, 1)
        _DryColor ("Sec", Color) = (0.68, 0.70, 0.34, 1)
        _TipColor ("Pointe", Color) = (0.72, 0.86, 0.42, 1)
        _TipSharp ("Fleur (0 herbe, 1 fleur)", Range(0, 1)) = 0
        _Wind ("Vent", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert nolightmap nodynlightmap nodirlightmap
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _FiefNoise;
        float4 _FiefPlayerPos;
        fixed4 _Color;
        fixed4 _ColorB;
        fixed4 _DryColor;
        fixed4 _TipColor;
        half _TipSharp;
        half _Wind;

        struct Input
        {
            float3 worldPos;
            float tip;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float h = saturate(v.texcoord.y);
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;

            // Le vent : une grande vague lente, et un frisson rapide.
            float2 nuv = wp.xz * 0.021 + _Time.y * float2(0.045, 0.028);
            float gust = tex2Dlod(_FiefNoise, float4(nuv, 0, 0)).r - 0.42;
            float shiver = sin(_Time.y * 2.6 + wp.x * 0.9 + wp.z * 1.3) * 0.12;
            float3 push = float3(1.0, 0.0, 0.45) * (gust * 1.5 + shiver) * _Wind;

            // Le joueur couche l'herbe autour de lui.
            float3 away = wp - _FiefPlayerPos.xyz;
            away.y = 0.0;
            float d = length(away);
            float nearby = saturate(1.0 - d / 1.7);
            push += (away / max(d, 0.001)) * nearby * 0.9;
            push.y -= nearby * 0.45;

            push *= h * h;
            v.vertex.xyz += mul(unity_WorldToObject, float4(push, 0.0)).xyz;
            // Une normale vers le ciel : l'herbe prend la lumiere comme un tapis, sans faces noires.
            v.normal = normalize(mul(unity_WorldToObject, float4(0.0, 1.0, 0.0, 0.0)).xyz);
            o.tip = h;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float m1 = tex2D(_FiefNoise, IN.worldPos.xz * 0.0085).r;
            float m2 = tex2D(_FiefNoise, IN.worldPos.xz * 0.047).g;
            fixed3 base = lerp(_Color.rgb, _ColorB.rgb, smoothstep(0.3, 0.7, m1));
            base = lerp(base, _DryColor.rgb, smoothstep(0.55, 0.8, m2) * 0.5);
            fixed3 blade = lerp(base * 0.5, lerp(base, _TipColor.rgb, 0.55), IN.tip);
            fixed3 flower = lerp(base * 0.6, _TipColor.rgb, smoothstep(0.55, 0.7, IN.tip));
            o.Albedo = lerp(blade, flower, _TipSharp);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack Off
}
