using UnityEngine;

namespace Fief
{
    /// <summary>
    /// UNE BOURSE PERDUE : une petite sacoche de cuir, quelques pieces qui brillent.
    ///
    /// L'or ne sert qu'a une chose : acheter les gardes. Il faut donc le CHERCHER --
    /// au pied des arbres morts, dans les ruines, pres des lieux-dits. Une bourse
    /// scintille un peu : on l'apercoit du coin de l'oeil, a condition de regarder
    /// par terre.
    /// </summary>
    public class Purse : MonoBehaviour, IInteractable
    {
        public int amount;
        bool taken;

        public static int Count;

        public static Purse Build(Transform parent, Vector3 at, int amount, float yaw)
        {
            GameObject root = new GameObject("BOURSE");
            root.transform.SetParent(parent, false);
            root.transform.position = at;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.3f, 0f);
            trigger.size = new Vector3(0.9f, 0.6f, 0.9f);

            Purse p = root.AddComponent<Purse>();
            p.amount = amount;

            Color leather = new Color(0.36f, 0.24f, 0.14f);
            Color gold = new Color(0.95f, 0.76f, 0.3f);
            Material coin = MaterialFactory.GetGlow(gold, 0.9f);
            Proto.BeginVisualOnly();
            // Une bourse de cuir renflee, le col serre par une cordelette, couchee
            // sur le flanc ; des pieces en deux petites piles et d'autres repandues.
            GameObject bag = Proto.Sphere(root.transform, new Vector3(0f, 0.13f, 0f), new Vector3(0.32f, 0.26f, 0.28f), leather, "Bourse");
            bag.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            GameObject neck = Proto.Cylinder(root.transform, new Vector3(-0.1f, 0.27f, 0f), new Vector3(0.1f, 0.05f, 0.1f), Palette.Shade(leather, 0.85f), "Col");
            neck.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            GameObject cord = Proto.Cylinder(root.transform, new Vector3(-0.09f, 0.25f, 0f), new Vector3(0.12f, 0.012f, 0.12f), new Color(0.62f, 0.5f, 0.3f), "Cordelette");
            cord.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            GameObject flap = Proto.Cone(root.transform, new Vector3(-0.15f, 0.31f, 0f), 0.07f, 0.08f, Palette.Shade(leather, 0.9f), "Fronce", 6);
            flap.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            for (int stack = 0; stack < 2; stack++)
            {
                int n = 3 + stack * 2;
                for (int i = 0; i < n; i++)
                {
                    GameObject c = Proto.Cylinder(root.transform, new Vector3(0.24f + stack * 0.1f, 0.008f + i * 0.016f, 0.1f - stack * 0.18f),
                                                  new Vector3(0.09f, 0.008f, 0.09f), gold, "Pièce");
                    c.transform.localRotation = Quaternion.Euler(0f, i * 23f, (i % 2) * 3f);
                    c.GetComponent<Renderer>().sharedMaterial = coin;
                }
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.1f + 0.4f;
                float d = 0.18f + (i % 3) * 0.09f;
                GameObject c = Proto.Cylinder(root.transform, new Vector3(0.12f + Mathf.Cos(a) * d, 0.01f, Mathf.Sin(a) * d),
                                              new Vector3(0.09f, 0.008f, 0.09f), gold, "Pièce");
                c.transform.localRotation = Quaternion.Euler(i % 2 == 0 ? 0f : 60f, i * 40f, 0f);
                c.GetComponent<Renderer>().sharedMaterial = coin;
            }
            Proto.EndVisualOnly();
            Ambiance.Sparkles(root.transform, new Vector3(0.15f, 0.2f, 0f), gold);
            Count++;
            return p;
        }

        /// <summary>
        /// Seme les bourses : une par lieu-dit, et une douzaine dans la foret, pres
        /// de ses arbres morts et de ses creux. Meme graine, memes bourses.
        /// </summary>
        public static void Scatter(Transform parent, GameConfig cfg)
        {
            Count = 0;
            GameObject group = new GameObject("BOURSES");
            group.transform.SetParent(parent, false);
            System.Random rng = new System.Random((cfg != null ? cfg.worldSeed : 1) * 53 + 9);
            float half = (cfg != null ? cfg.mapSize : 700f) * 0.5f - 60f;

            for (int i = 0; i < Landmarks.All.Count; i++)
            {
                Landmark m = Landmarks.All[i];
                if (m == null) continue;
                Vector3 at = m.transform.position + m.transform.rotation * new Vector3(5f, 0f, -9f);
                Build(group.transform, Ground.Place(at.x, at.z, 0f), 20 + rng.Next(15), rng.Next(360));
            }

            int placed = 0;
            for (int tries = 0; tries < 600 && placed < 24; tries++)
            {
                float x = ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = ((float)rng.NextDouble() * 2f - 1f) * half;
                if (Castle.Covers(x, z, 10f) || Landmarks.Near(x, z, 2f)) continue;
                Build(group.transform, Ground.Place(x, z, 0f), 12 + rng.Next(19), rng.Next(360));
                placed++;
            }
        }

        public Transform Anchor { get { return transform; } }
        public bool CanInteract { get { return !taken; } }
        public string Prompt { get { return "Ramasser une bourse (" + amount + " or)"; } }
        public float HoldDuration { get { return 0f; } }

        public void Interact()
        {
            if (taken || Game.Wallet == null) return;
            taken = true;
            Game.Wallet.Add(amount);
            Sfx.Coin();
            FloatingTexts.Spawn(transform.position + Vector3.up * 1.1f, "+" + amount + " or", new Color(0.95f, 0.78f, 0.35f));
            Destroy(gameObject);
        }
    }
}
