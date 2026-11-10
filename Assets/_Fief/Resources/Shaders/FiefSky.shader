// LE CIEL (v49). Le ciel d'Unity ("Skybox/Procedural") etait un degrade. Celui-ci est
// peint : le zenith bleu, l'horizon peche, le halo du soleil et son disque (assez
// brillant pour que l'image "bave" de lumiere autour : le bloom), une bande doree du
// cote du soleil, et de VRAIS NUAGES qui derivent, eclaires par le soleil (bord
// lumineux, ventre plus sombre).
// Les noms des reglages (_SkyTint, _GroundColor, _Exposure, _AtmosphereThickness,
// _SunSize) sont ceux du ciel d'Unity : le ciel du Mode Dieu (GodSky) les pilote tels quels.
Shader "Fief/Sky"
{
    Properties
    {
        _SkyTint ("Zenith", Color) = (0.22, 0.42, 0.82, 1)
        _HorizonColor ("Horizon", Color) = (1.0, 0.80, 0.64, 1)
        _GroundColor ("Sous l'horizon", Color) = (0.93, 0.86, 0.86, 1)
        _SunColor ("Soleil", Color) = (1.0, 0.86, 0.62, 1)
        _CloudLit ("Nuage eclaire", Color) = (1.0, 0.95, 0.88, 1)
        _CloudDark ("Nuage a l'ombre", Color) = (0.62, 0.58, 0.72, 1)
        _Exposure ("Exposition", Float) = 1
        _AtmosphereThickness ("Epaisseur de l'air", Float) = 1
        _SunSize ("Taille du soleil", Float) = 0.045
        _Cover ("Couverture nuageuse", Range(0, 1)) = 0.5
        _CloudSpeed ("Vitesse des nuages", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _FiefNoise;
            fixed4 _SkyTint;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _SunColor;
            fixed4 _CloudLit;
            fixed4 _CloudDark;
            half _Exposure;
            half _AtmosphereThickness;
            half _SunSize;
            half _Cover;
            half _CloudSpeed;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 sun = normalize(_WorldSpaceLightPos0.xyz);
                float h = d.y;
                float toSun = saturate(dot(d, sun));

                // --- le degrade : horizon -> zenith ; sous l'horizon, la clarte des nuages
                float thick = saturate((_AtmosphereThickness - 1.0) / 2.2);
                half3 horizon = lerp(_HorizonColor.rgb, _HorizonColor.rgb * half3(1.25, 0.62, 0.42), thick);
                half3 zenith = lerp(_SkyTint.rgb, _SkyTint.rgb * half3(1.1, 0.55, 0.5), thick);
                half3 col = lerp(horizon, zenith, pow(saturate(h), 0.5));
                col = lerp(col, _GroundColor.rgb, saturate(-h * 5.0));

                // --- la bande doree a l'horizon, du cote du soleil
                float band = pow(1.0 - abs(h), 7.0);
                col += _SunColor.rgb * band * (0.10 + 0.45 * toSun * toSun);

                // --- le soleil : un grand halo, un petit, le disque
                float size = max(_SunSize, 0.01);
                float disc = smoothstep(1.0 - size * 0.02, 1.0 - size * 0.012, toSun);
                half3 glow = _SunColor.rgb * (pow(toSun, 6.0) * 0.22 + pow(toSun, 60.0) * 0.5 + pow(toSun, 600.0) * 1.2);
                col += glow;

                // --- les nuages : une nappe plate tres haut, vue en perspective
                float fade = smoothstep(0.03, 0.22, h);
                {
                    float2 uv = d.xz / (max(h, 0.0) + 0.18);
                    float2 drift = _Time.y * _CloudSpeed * float2(0.0035, 0.0018);
                    float n = tex2D(_FiefNoise, uv * 0.11 + drift).r * 0.55
                            + tex2D(_FiefNoise, uv * 0.27 + drift * 1.7).g * 0.30
                            + tex2D(_FiefNoise, uv * 0.71 + drift * 2.6).a * 0.15;
                    float edge = 0.86 - _Cover * 0.62;
                    float cover = smoothstep(edge, edge + 0.22, n);
                    // Le meme bruit, decale vers le soleil : la ou il est plus epais, le nuage est a l'ombre.
                    float2 toward = normalize(sun.xz + 0.0001) * 0.035;
                    float n2 = tex2D(_FiefNoise, uv * 0.11 + drift + toward).r * 0.55
                             + tex2D(_FiefNoise, uv * 0.27 + drift * 1.7 + toward).g * 0.30;
                    float shade = saturate((n2 - (n - 0.15)) * 3.0 + 0.5);
                    half3 cloud = lerp(_CloudLit.rgb, _CloudDark.rgb, shade * 0.75);
                    // Le lisere d'argent quand le soleil est derriere.
                    cloud += _SunColor.rgb * pow(toSun, 5.0) * (1.0 - cover) * 1.4;
                    cloud = lerp(cloud, cloud * half3(1.15, 0.6, 0.45), thick * 0.7);
                    col = lerp(col, cloud, cover * fade * 0.92);
                }

                // Le disque passe devant les nuages fins (il les traverse de sa lumiere).
                col += _SunColor.rgb * disc * 5.0;

                return half4(col * _Exposure, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
