// L'ONDE DE CHOC (01/10 -- Martin : "les effets sont horribles"). Une sphere dont le
// CENTRE est transparent et le BORD brille : on voit une bulle d'energie nette qui
// gonfle, au lieu d'une boule laiteuse. C'est l'effet "Fresnel" : plus la surface est
// vue de biais (le bord), plus elle luit.
//
// Pas de lumiere, pas d'ombre : elle s'ajoute a l'image (additif). Rangee dans
// Resources : elle part avec le jeu quand on l'exporte.
Shader "Fief/Onde"
{
    Properties
    {
        _TintColor ("Couleur", Color) = (1, 1, 1, 1)
        _Power ("Finesse du bord", Float) = 2.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TintColor;
            float _Power;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 view : TEXCOORD1;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float facing = abs(dot(normalize(i.normal), normalize(i.view)));
                float rim = pow(1.0 - saturate(facing), _Power);
                fixed4 c = _TintColor;
                c.a = saturate(c.a * (rim * 2.2 + 0.04));
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
