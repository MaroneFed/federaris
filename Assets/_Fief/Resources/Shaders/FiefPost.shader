// L'IMAGE FINALE (v49). Ce shader ne dessine aucun objet : il retravaille l'image entiere
// une fois le monde dessine (un "post-traitement", comme l'etalonnage d'un film).
// Huit passes, appelees par Core/PostFx.cs :
//   0  ce qui brille (au-dessus d'un seuil), en reduisant l'image de moitie
//   1  reduire encore (flou qui s'elargit)        2  remonter en additionnant : le HALO (bloom)
//   3  le masque des RAYONS DU SOLEIL (le ciel visible) 4  l'etirer depuis le soleil
//   5  l'OMBRE DE CONTACT (occlusion ambiante : les coins et les pieds des murs s'assombrissent)
//   6  son flou                                    7  la composition : tout assembler, puis
//      l'epaule des hautes lumieres, le contraste, la saturation, les teintes (ombres
//      froides, lumieres chaudes), le vignettage, le grain, l'aberration des bords.
Shader "Hidden/Fief/Post"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    sampler2D _ShaftTex;
    sampler2D _AoTex;
    sampler2D_float _CameraDepthTexture;

    float4 _Bloom;        // x seuil, y genou, z force
    float4 _SunPos;       // xy position du soleil a l'ecran, z force des rayons, w pas du flou
    float4 _ShaftColor;
    float4 _Grade;        // x exposition, y contraste, z saturation, w vignettage
    float4 _ShadowTint;
    float4 _HighTint;
    float4 _Fx;           // x grain, y aberration, z nettete, w temps
    float4 _Ao;           // x rayon (m), y force, z biais
    float4 _BlurDir;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float2 uv2 : TEXCOORD1;
    };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        o.uv2 = v.texcoord.xy;
        // Sur certaines cartes, l'image de la camera arrive tete en bas par rapport aux
        // autres textures : on retourne les coordonnees de ces autres-la.
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0.0) o.uv2.y = 1.0 - o.uv2.y;
        #endif
        return o;
    }

    half3 Box(float2 uv, float2 d)
    {
        half3 c = tex2D(_MainTex, uv + float2(-d.x, -d.y)).rgb;
        c += tex2D(_MainTex, uv + float2(d.x, -d.y)).rgb;
        c += tex2D(_MainTex, uv + float2(-d.x, d.y)).rgb;
        c += tex2D(_MainTex, uv + float2(d.x, d.y)).rgb;
        return c * 0.25;
    }

    // --- 0 : ce qui brille
    half4 fragBright(v2f i) : SV_Target
    {
        half3 c = Box(i.uv, _MainTex_TexelSize.xy);
        c = min(c, 12.0);
        half br = max(c.r, max(c.g, c.b));
        half knee = max(_Bloom.y, 0.0001);
        half soft = clamp(br - _Bloom.x + knee, 0.0, 2.0 * knee);
        soft = soft * soft / (4.0 * knee);
        half keep = max(soft, br - _Bloom.x) / max(br, 0.0001);
        return half4(c * keep, 1.0);
    }

    // --- 1 : reduire
    half4 fragDown(v2f i) : SV_Target
    {
        return half4(Box(i.uv, _MainTex_TexelSize.xy), 1.0);
    }

    // --- 2 : remonter, en ajoutant l'etage du dessus
    half4 fragUp(v2f i) : SV_Target
    {
        half3 c = Box(i.uv, _MainTex_TexelSize.xy * 0.5);
        c += tex2D(_BloomTex, i.uv).rgb;
        return half4(c, 1.0);
    }

    // --- 3 : le masque des rayons (le ciel, plus clair pres du soleil)
    half4 fragShaftMask(v2f i) : SV_Target
    {
        float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv2));
        half sky = step(0.9995, depth);
        half3 c = tex2D(_MainTex, i.uv).rgb;
        half lum = dot(min(c, 4.0), half3(0.299, 0.587, 0.114));
        float2 toSun = i.uv - _SunPos.xy;
        toSun.x *= _ScreenParams.x / _ScreenParams.y;
        half nearSun = saturate(1.0 - length(toSun) * 1.1);
        half m = sky * saturate(lum - 0.35) * nearSun * nearSun;
        return half4(m, m, m, 1.0);
    }

    // --- 4 : etirer le masque depuis le soleil
    half4 fragShaftBlur(v2f i) : SV_Target
    {
        float2 stepUv = (_SunPos.xy - i.uv) * _SunPos.w;
        float2 uv = i.uv;
        half acc = 0.0;
        for (int k = 0; k < 10; k++)
        {
            acc += tex2D(_MainTex, uv).r;
            uv += stepUv;
        }
        acc *= 0.1;
        return half4(acc, acc, acc, 1.0);
    }

    // --- 5 : l'ombre de contact
    float3 ViewPos(float2 uv)
    {
        float d = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0.0, 0.0)));
        float2 p = float2(unity_CameraProjection._11, unity_CameraProjection._22);
        return float3((uv * 2.0 - 1.0) / p, 1.0) * d;
    }

    float Hash(float2 p)
    {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    }

    half4 fragAo(v2f i) : SV_Target
    {
        float2 uv = i.uv2;
        float3 p = ViewPos(uv);
        float2 px = _MainTex_TexelSize.xy;
        // La normale de la surface, devinee d'apres les voisins (on prend, de chaque cote,
        // le voisin le plus proche en profondeur : sinon les silhouettes font des traits).
        float3 pr = ViewPos(uv + float2(px.x, 0.0));
        float3 pl = ViewPos(uv - float2(px.x, 0.0));
        float3 pu = ViewPos(uv + float2(0.0, px.y));
        float3 pd = ViewPos(uv - float2(0.0, px.y));
        float3 dxv = abs(pr.z - p.z) < abs(p.z - pl.z) ? pr - p : p - pl;
        float3 dyv = abs(pu.z - p.z) < abs(p.z - pd.z) ? pu - p : p - pd;
        float3 n = normalize(cross(dyv, dxv));
        float radius = _Ao.x;
        // Le rayon, vu a l'ecran, retrecit avec la distance.
        float2 reach = radius * float2(unity_CameraProjection._11, unity_CameraProjection._22) * 0.5 / p.z;
        float turn = Hash(uv * _ScreenParams.xy) * 6.2831853;
        float occ = 0.0;
        for (int k = 0; k < 8; k++)
        {
            float a = turn + k * 2.3999632;
            float r = sqrt((k + 0.5) / 8.0);
            float2 o = float2(cos(a), sin(a)) * r * reach;
            float3 s = ViewPos(uv + o);
            float3 v = s - p;
            float dist2 = dot(v, v);
            float facing = max(dot(v, n) - _Ao.z * p.z, 0.0);
            occ += facing / (dist2 + 0.0001) * step(dist2, radius * radius);
        }
        occ = saturate(1.0 - occ * radius * 0.25 * 0.5);
        // Loin, l'effet s'efface (il scintillerait).
        occ = lerp(occ, 1.0, saturate((p.z - 90.0) / 90.0));
        return half4(occ, occ, occ, 1.0);
    }

    // --- 6 : flouter l'ombre de contact (dans une direction a la fois)
    half4 fragAoBlur(v2f i) : SV_Target
    {
        float2 d = _BlurDir.xy * _MainTex_TexelSize.xy;
        half c = tex2D(_MainTex, i.uv).r * 0.36;
        c += (tex2D(_MainTex, i.uv + d).r + tex2D(_MainTex, i.uv - d).r) * 0.22;
        c += (tex2D(_MainTex, i.uv + d * 2.0).r + tex2D(_MainTex, i.uv - d * 2.0).r) * 0.10;
        return half4(c, c, c, 1.0);
    }

    // L'epaule : jusqu'a 0,62 on ne touche a rien ; au-dela, on se rapproche de 1 sans jamais
    // le depasser (les blancs ne "crament" plus d'un coup).
    half3 Shoulder(half3 x)
    {
        half k = 0.62;
        half3 over = max(x - k, 0.0) / (1.0 - k);
        half3 soft = k + (1.0 - k) * (over / (1.0 + over));
        return lerp(x, soft, step(k, x));
    }

    // --- 7 : la composition
    half4 fragFinal(v2f i) : SV_Target
    {
        float2 uv = i.uv;
        float2 centre = uv - 0.5;

        // L'aberration des bords (rouge et bleu a peine decales), et la nettete.
        float2 off = centre * dot(centre, centre) * _Fx.y;
        half3 col;
        col.r = tex2D(_MainTex, uv + off).r;
        col.g = tex2D(_MainTex, uv).g;
        col.b = tex2D(_MainTex, uv - off).b;
        half3 around = Box(uv, _MainTex_TexelSize.xy);
        col += (col - around) * _Fx.z;
        col = max(col, 0.0);

        // L'ombre de contact, le halo, les rayons.
        col *= lerp(1.0, tex2D(_AoTex, i.uv2).r, _Ao.y);
        col += tex2D(_BloomTex, i.uv2).rgb * _Bloom.z;
        col += tex2D(_ShaftTex, i.uv2).r * _ShaftColor.rgb * _SunPos.z;

        col *= _Grade.x;
        col = Shoulder(col);

        // L'etalonnage : saturation, contraste, ombres froides et lumieres chaudes.
        half lum = dot(col, half3(0.299, 0.587, 0.114));
        col = lerp(lum.xxx, col, _Grade.z);
        col = (col - 0.5) * _Grade.y + 0.5;
        col *= lerp(_ShadowTint.rgb, _HighTint.rgb, smoothstep(0.0, 1.0, lum));

        // Le vignettage : les coins un peu plus sombres, le regard va au centre.
        half edge = saturate(length(centre) * 1.3);
        col *= 1.0 - _Grade.w * edge * edge * edge;

        // Le grain (casse les degrades en bandes du ciel).
        half grain = Hash(uv * _ScreenParams.xy + _Fx.w) - 0.5;
        col += grain * _Fx.x;

        return half4(saturate(col), 1.0);
    }
    ENDCG

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBright
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDown
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragUp
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragShaftMask
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragShaftBlur
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAo
            #pragma target 3.0
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragAoBlur
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragFinal
            #pragma target 3.0
            ENDCG }
    }
    FallBack Off
}
