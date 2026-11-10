using System.Collections.Generic;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// L'IMAGE FINALE (v49). Pose sur la camera, ce composant retravaille chaque image une fois
    /// le monde dessine, comme on etalonne un film (le shader : Resources/Shaders/FiefPost) :
    ///
    ///   LE HALO (bloom)      ce qui brille tres fort (le soleil, l'or, les lueurs, les sorts)
    ///                        deborde en lumiere douce autour de lui ;
    ///   LES RAYONS           le soleil tire des rais de lumiere entre les tours quand on le regarde ;
    ///   L'OMBRE DE CONTACT   (Ultra) les coins, les pieds des murs, le dessous des objets
    ///                        s'assombrissent : les choses "tiennent" au sol ;
    ///   L'ETALONNAGE         les blancs ne crament plus, un peu de contraste et de couleur, des
    ///                        ombres legerement froides et des lumieres chaudes, les coins de
    ///                        l'image un peu plus sombres, un grain tres fin.
    ///
    /// Concept Unity : OnRenderImage est appele par la camera avec l'image qu'elle vient de
    /// dessiner ("src") ; on doit ecrire le resultat dans "dst". L'interface (le HUD) est
    /// dessinee APRES : elle reste nette, sans halo.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PostFx : MonoBehaviour
    {
        /// <summary>La vitesse en vol (0 a 1) : les bords de l'image s'irisent quand on file.</summary>
        public static float Rush;
        /// <summary>Un coup recu, une explosion toute proche : l'image "claque" un instant (retombe seul).</summary>
        public static float Jolt;

        Camera cam;
        Material mat;
        static Texture2D white;
        readonly List<RenderTexture> temps = new List<RenderTexture>();

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            Shader s = Gfx.Find("Hidden/Fief/Post");
            if (s == null) { enabled = false; return; }
            mat = new Material(s);
            mat.hideFlags = HideFlags.HideAndDontSave;
            // Les rayons et l'ombre de contact lisent la PROFONDEUR de chaque pixel.
            cam.depthTextureMode |= DepthTextureMode.Depth;
            if (white == null)
            {
                white = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                white.Apply();
                white.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void OnDisable()
        {
            if (mat != null) Destroy(mat);
            mat = null;
        }

        void Update()
        {
            Jolt = Mathf.MoveTowards(Jolt, 0f, Time.unscaledDeltaTime * 2.5f);
        }

        RenderTexture Temp(int w, int h, RenderTextureFormat format)
        {
            RenderTexture t = RenderTexture.GetTemporary(Mathf.Max(1, w), Mathf.Max(1, h), 0, format);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            temps.Add(t);
            return t;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null || !Gfx.Fancy) { Graphics.Blit(src, dst); return; }
            int w = src.width, h = src.height;
            RenderTextureFormat hdr = src.format;

            // ---------------------------------------------------------------- le halo
            mat.SetVector("_Bloom", new Vector4(1.0f, 0.22f, 0.13f + Jolt * 0.12f, 0f));
            RenderTexture[] down = new RenderTexture[5];
            int count = 0;
            int bw = w / 2, bh = h / 2;
            down[0] = Temp(bw, bh, hdr);
            Graphics.Blit(src, down[0], mat, 0);
            count = 1;
            for (int i = 1; i < down.Length; i++)
            {
                bw /= 2; bh /= 2;
                if (bw < 8 || bh < 8) break;
                down[i] = Temp(bw, bh, hdr);
                Graphics.Blit(down[i - 1], down[i], mat, 1);
                count++;
            }
            RenderTexture bloom = down[count - 1];
            for (int i = count - 2; i >= 0; i--)
            {
                RenderTexture up = Temp(down[i].width, down[i].height, hdr);
                mat.SetTexture("_BloomTex", down[i]);
                Graphics.Blit(bloom, up, mat, 2);
                bloom = up;
            }

            // ---------------------------------------------------------------- les rayons du soleil
            float shaft = 0f;
            Vector2 sunUv = new Vector2(0.5f, 0.5f);
            Light sun = Atmosphere.Sun;
            if (sun != null)
            {
                Vector3 vp = cam.WorldToViewportPoint(cam.transform.position - sun.transform.forward * 1000f);
                if (vp.z > 0f)
                {
                    sunUv = new Vector2(vp.x, vp.y);
                    float away = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f));
                    shaft = 1f - Mathf.SmoothStep(0.5f, 1.15f, away);
                }
            }
            Texture shafts = white;
            if (shaft > 0.01f)
            {
                RenderTexture a = Temp(w / 4, h / 4, RenderTextureFormat.Default);
                RenderTexture b = Temp(w / 4, h / 4, RenderTextureFormat.Default);
                mat.SetVector("_SunPos", new Vector4(sunUv.x, sunUv.y, 0f, 0.06f));
                Graphics.Blit(src, a, mat, 3);
                Graphics.Blit(a, b, mat, 4);
                mat.SetVector("_SunPos", new Vector4(sunUv.x, sunUv.y, 0f, 0.025f));
                Graphics.Blit(b, a, mat, 4);
                shafts = a;
            }
            else shaft = 0f;
            Color sunTint = sun != null ? sun.color : Color.white;
            mat.SetVector("_SunPos", new Vector4(sunUv.x, sunUv.y, shaft * 0.6f, 0f));
            mat.SetColor("_ShaftColor", new Color(sunTint.r, sunTint.g * 0.92f, sunTint.b * 0.8f, 1f));

            // ---------------------------------------------------------------- l'ombre de contact
            Texture ao = white;
            float aoForce = 0f;
            if (Gfx.Ultra)
            {
                aoForce = 0.62f;
                mat.SetVector("_Ao", new Vector4(1.1f, aoForce, 0.0025f, 0f));
                RenderTexture a = Temp(w / 2, h / 2, RenderTextureFormat.Default);
                RenderTexture b = Temp(w / 2, h / 2, RenderTextureFormat.Default);
                Graphics.Blit(src, a, mat, 5);
                mat.SetVector("_BlurDir", new Vector4(1f, 0f, 0f, 0f));
                Graphics.Blit(a, b, mat, 6);
                mat.SetVector("_BlurDir", new Vector4(0f, 1f, 0f, 0f));
                Graphics.Blit(b, a, mat, 6);
                ao = a;
            }
            mat.SetVector("_Ao", new Vector4(1.1f, aoForce, 0.0025f, 0f));

            // ---------------------------------------------------------------- la composition
            mat.SetTexture("_BloomTex", bloom);
            mat.SetTexture("_ShaftTex", shafts);
            mat.SetTexture("_AoTex", ao);
            mat.SetVector("_Grade", new Vector4(1.0f, 1.07f, 1.13f, 0.3f));
            mat.SetColor("_ShadowTint", new Color(0.95f, 0.975f, 1.05f, 1f));
            mat.SetColor("_HighTint", new Color(1.04f, 1.01f, 0.965f, 1f));
            float rush = Mathf.Clamp01(Rush);
            mat.SetVector("_Fx", new Vector4(0.016f, 0.0035f + rush * 0.012f + Jolt * 0.02f, 0.22f, Time.unscaledTime * 7.3f % 10f));
            Graphics.Blit(src, dst, mat, 7);

            for (int i = 0; i < temps.Count; i++) RenderTexture.ReleaseTemporary(temps[i]);
            temps.Clear();
        }
    }
}
