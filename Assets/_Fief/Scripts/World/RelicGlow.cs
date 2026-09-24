using UnityEngine;

namespace Fief
{
    /// <summary>
    /// PORTER UNE RELIQUE, CA SE VOIT. Quiconque la porte (la sienne en main, ou
    /// une volee) baigne dans une lueur bleue qui palpite -- toi comme les rivaux.
    /// Deux effets, voulus : on SAIT qu'on la porte (et qu'on ne peut pas frapper),
    /// et les autres le voient dans la brume. Porter, c'est etre une cible.
    /// </summary>
    public class RelicGlow : MonoBehaviour
    {
        [System.NonSerialized] public Seeker seeker;
        Light glow;

        public static void Attach(Transform body, Seeker seeker)
        {
            if (body == null || seeker == null) return;
            RelicGlow g = body.gameObject.AddComponent<RelicGlow>();
            g.seeker = seeker;
            GameObject go = new GameObject("Lueur de relique");
            go.transform.SetParent(body, false);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0.2f);
            g.glow = go.AddComponent<Light>();
            g.glow.type = LightType.Point;
            g.glow.color = Stele.RuneBlue;
            g.glow.range = 6f;
            g.glow.intensity = 0f;
            g.glow.shadows = LightShadows.None;
        }

        void Update()
        {
            if (glow == null || seeker == null) return;
            Hoard h = seeker.Hoard;
            bool carrying = seeker.Alive && (h.RelicInHand || h.Trophy != null);
            float pulse = 1.1f + Mathf.Sin(Time.time * 3f) * 0.35f;
            glow.intensity = Mathf.MoveTowards(glow.intensity, carrying ? pulse : 0f, Time.deltaTime * 3f);
            glow.enabled = glow.intensity > 0.01f;
        }
    }
}
