using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE COLONNE DE LUMIERE qui perce la brume.
    ///
    /// Tout le decor est efface par la brume a vingt metres : c'est le shader
    /// (Standard) qui le fait. Cette colonne utilise un autre shader, "Sprites/
    /// Default", qui IGNORE la brume : elle reste lumineuse quelle que soit la
    /// distance. Les troncs devant elle la cachent quand meme (la profondeur est
    /// respectee) : on la voit entre les arbres, comme une vraie lueur au loin.
    ///
    /// C'est un simple rectangle, toujours tourne vers la camera (autour de l'axe
    /// vertical seulement), avec une texture douce : opaque au centre, transparente
    /// sur les bords et vers le haut.
    ///
    /// LIMITE : la camera ne dessine rien au-dela de ~50 m (Atmosphere, plan
    /// lointain). Pour un point plus loin, on pose la colonne a 30 m DANS SA
    /// DIRECTION : c'est un trompe-l'oeil, mais l'oeil ne sait pas la difference.
    /// (A 30 m, une colonne de 34 m reste entierement sous le plan lointain.)
    /// </summary>
    public class LightBeam : MonoBehaviour
    {
        static Texture2D beamTexture;
        static Material beamMaterial;

        public Vector3 source;          // le vrai point d'ou monte la lumiere
        public Color color = new Color(0.6f, 0.78f, 1f);
        public float targetAlpha = 0.8f;
        public float fadeSpeed = 0.5f;
        public bool projectFar = true;

        Material material;
        float alpha;
        Transform quad;

        public static LightBeam Build(Transform parent, Vector3 source, Color color, float width, float height)
        {
            if (!EnsureMaterial()) return null;
            GameObject root = new GameObject("Colonne de lumiere");
            root.transform.SetParent(parent, false);
            LightBeam beam = root.AddComponent<LightBeam>();
            beam.source = source;
            beam.color = color;

            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Proto.StripCollider(q);
            q.name = "Lueur";
            q.transform.SetParent(root.transform, false);
            q.transform.localScale = new Vector3(width, height, 1f);
            q.transform.localPosition = new Vector3(0f, height * 0.5f - 1f, 0f);
            beam.material = new Material(beamMaterial);
            Renderer r = q.GetComponent<Renderer>();
            r.sharedMaterial = beam.material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            beam.quad = q.transform;
            beam.Apply();
            return beam;
        }

        void LateUpdate()
        {
            alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            Apply();

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 eye = cam.transform.position;

            // Trop loin pour etre dessinee : on la rapproche dans la bonne direction.
            Vector3 at = source;
            if (projectFar)
            {
                Vector3 flat = new Vector3(source.x - eye.x, 0f, source.z - eye.z);
                float d = flat.magnitude;
                // Juste en deca du plan lointain de la camera (qui suit la brume).
                float limit = cam.farClipPlane * 0.5f;
                if (d > limit) at = new Vector3(eye.x, source.y, eye.z) + flat / d * limit;
            }
            transform.position = at;

            // Toujours de face, autour de l'axe vertical seulement.
            Vector3 toCam = eye - transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }

        void Apply()
        {
            if (material == null) return;
            float pulse = 1f + Mathf.Sin(Time.time * 2.3f) * 0.08f;
            material.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha * pulse));
            if (quad != null) quad.gameObject.SetActive(alpha > 0.005f);
        }

        public bool Faded { get { return alpha <= 0.005f && targetAlpha <= 0f; } }

        static bool EnsureMaterial()
        {
            if (beamMaterial != null) return true;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) return false;

            const int W = 32, H = 64;
            beamTexture = new Texture2D(W, H, TextureFormat.RGBA32, false);
            beamTexture.wrapMode = TextureWrapMode.Clamp;
            beamTexture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;
                float vertical = Mathf.Clamp01(v * 8f) * Mathf.Pow(1f - v, 1.6f);   // pied doux, sommet evanoui
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f;
                    float horizontal = Mathf.Exp(-u * u * 5f);                        // coeur brillant, bords flous
                    pixels[y * W + x] = new Color(1f, 1f, 1f, horizontal * vertical);
                }
            }
            beamTexture.SetPixels(pixels);
            beamTexture.Apply();

            beamMaterial = new Material(shader) { name = "Colonne", mainTexture = beamTexture };
            return true;
        }
    }
}
