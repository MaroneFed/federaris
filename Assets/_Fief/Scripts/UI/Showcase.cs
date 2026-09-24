using UnityEngine;

namespace Fief
{
    /// <summary>
    /// LA VITRINE : un talisman en 3D, qui tourne, DANS l'interface.
    ///
    /// Comment on met un objet 3D dans un panneau 2D ? On le filme. Loin sous la
    /// carte (a -800 m, ou rien d'autre n'existe), une petite scene : l'objet, une
    /// lumiere, et une deuxieme camera. Cette camera ne filme pas l'ecran : elle
    /// filme dans une TEXTURE (RenderTexture), une image qui se met a jour a chaque
    /// frame. L'interface n'a plus qu'a afficher cette image.
    ///
    /// Concept Unity : les LAYERS (calques). Chaque GameObject appartient a un
    /// calque (0 a 31). Une camera peut ne filmer que certains calques
    /// (cullingMask), une lumiere n'eclairer que certains calques. La vitrine vit
    /// seule sur le calque 31 : sa camera ne voit qu'elle, et la camera du jeu ne
    /// la voit jamais.
    /// </summary>
    public class Showcase : MonoBehaviour
    {
        const int Layer = 31;
        static Showcase instance;

        Camera lens;
        Transform pivot;
        RenderTexture image;
        int shown = -1;
        float wantedUntil;

        public static Texture Image { get { return instance != null ? instance.image : null; } }

        public static void Build()
        {
            if (instance != null) return;
            GameObject root = new GameObject("VITRINE");
            root.transform.position = new Vector3(0f, -800f, 0f);
            instance = root.AddComponent<Showcase>();

            instance.image = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            instance.image.name = "Vitrine";

            GameObject camGo = new GameObject("Objectif");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.1f, -1.35f);
            instance.lens = camGo.AddComponent<Camera>();
            instance.lens.clearFlags = CameraClearFlags.SolidColor;
            instance.lens.backgroundColor = new Color(0f, 0f, 0f, 0f);
            instance.lens.cullingMask = 1 << Layer;
            instance.lens.fieldOfView = 32f;
            instance.lens.nearClipPlane = 0.1f;
            instance.lens.farClipPlane = 5f;
            instance.lens.targetTexture = instance.image;
            instance.lens.enabled = false;

            // Une lumiere chaude de face, une froide par-derriere : l'objet se detache.
            instance.AddLight(root.transform, new Vector3(-0.8f, 0.9f, -1.2f), new Color(1f, 0.9f, 0.75f), 2.2f);
            instance.AddLight(root.transform, new Vector3(0.9f, 0.4f, 1.0f), new Color(0.55f, 0.7f, 1f), 1.6f);

            GameObject p = new GameObject("Socle tournant");
            p.transform.SetParent(root.transform, false);
            instance.pivot = p.transform;
        }

        void AddLight(Transform parent, Vector3 at, Color color, float intensity)
        {
            GameObject go = new GameObject("Lumière");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = 4f;
            l.cullingMask = 1 << Layer;
            l.shadows = LightShadows.None;
        }

        /// <summary>
        /// Montrer ce talisman pendant quelques secondes (ou tant qu'on le redemande,
        /// chaque frame, depuis un panneau). Hors de ces moments, la camera est
        /// eteinte : elle ne coute rien.
        /// </summary>
        public static void Show(Talisman t, float seconds)
        {
            if (instance == null) Build();
            instance.wantedUntil = Mathf.Max(instance.wantedUntil, Time.unscaledTime + seconds);
            if (instance.shown == (int)t) return;
            instance.shown = (int)t;
            for (int i = instance.pivot.childCount - 1; i >= 0; i--) Destroy(instance.pivot.GetChild(i).gameObject);
            GameObject holder = new GameObject("Objet");
            holder.transform.SetParent(instance.pivot, false);
            holder.transform.localScale = Vector3.one * 1.3f;
            Proto.BeginVisualOnly();
            TalismanModels.Build(holder.transform, t);
            Proto.EndVisualOnly();
            SetLayer(holder.transform, Layer);
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        void LateUpdate()
        {
            bool wanted = Time.unscaledTime < wantedUntil;
            if (lens.enabled != wanted) lens.enabled = wanted;
            if (!wanted) return;
            float t = Time.unscaledTime;
            pivot.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 12f, t * 50f, 0f);
            pivot.localPosition = new Vector3(0f, Mathf.Sin(t * 1.3f) * 0.03f, 0f);
        }
    }
}
