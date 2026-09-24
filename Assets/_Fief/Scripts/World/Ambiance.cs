using UnityEngine;
using UnityEngine.Rendering;

namespace Fief
{
    /// <summary>
    /// CE QUI FLOTTE DANS L'AIR. Trois systemes de particules, fabriques par le code :
    ///
    ///   - LES POUSSIERES : des grains minuscules qui derivent autour de toi. Ils
    ///     donnent a l'air une EPAISSEUR : on sent qu'on marche dans quelque chose,
    ///     pas dans du vide. C'est l'effet le moins cher et le plus efficace du jeu.
    ///   - LA BRUME RASANTE : de grandes nappes pales, a hauteur de genou, qui
    ///     glissent lentement. La brume de Unity est uniforme ; celle-ci a des trous.
    ///   - LES LUCIOLES : au-dessus de chaque creux a pierres-lune. Elles se voient a
    ///     travers la brume, un peu plus loin que les arbres : quand on apercoit des
    ///     petites lueurs vertes entre les troncs, on sait qu'un creux est la.
    ///
    /// Concept Unity : un ParticleSystem est un COMPOSANT qui fait naitre, bouger et
    /// mourir des centaines de petits carres toujours tournes vers la camera (des
    /// "billboards"). On le regle par modules : main (duree de vie, taille, couleur),
    /// emission (combien par seconde), shape (ou ils naissent), noise (le vent)...
    /// Chaque module se lit dans une variable, puis se modifie : c'est une "vue" sur
    /// le composant, pas une copie.
    ///
    /// simulationSpace = World : une particule nee reste la ou elle est nee, meme si
    /// l'emetteur (accroche au joueur) s'en va. Sans ca, toute la poussiere suivrait
    /// le joueur comme une bulle.
    /// </summary>
    public static class Ambiance
    {
        static Texture2D softDot;
        static Material additive;
        static Material blended;

        public static void Build(Transform worldRoot, Transform player, GameConfig cfg)
        {
            if (!EnsureMaterials()) return;

            if (player != null)
            {
                BuildMotes(player);
                BuildGroundMist(player, cfg);
            }

            GameObject flies = new GameObject("LUCIOLES");
            flies.transform.SetParent(worldRoot, false);
            for (int i = 0; i < Gathering.HollowSpotCount; i++)
            {
                Vector2 spot = Gathering.HollowSpot(i);
                BuildFireflies(flies.transform, Ground.Place(spot.x, spot.y, 1.2f), i);
            }
        }

        // ================================================================== les trois nuages

        static void BuildMotes(Transform player)
        {
            ParticleSystem ps = NewSystem("Poussieres", player, new Vector3(0f, 1.6f, 0f), additive);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new Color(0.55f, 0.56f, 0.46f, 0.55f);
            main.maxParticles = 450;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 42f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(22f, 5f, 22f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.08f;

            FadeInOut(ps, 1f);
            ps.Play();
        }

        static void BuildGroundMist(Transform player, GameConfig cfg)
        {
            ParticleSystem ps = NewSystem("Brume rasante", player, new Vector3(0f, 0.35f, 0f), blended);

            Color haze = cfg != null ? cfg.hazeColor : Palette.Haze;
            Color pale = Color.Lerp(haze, Color.white, 0.18f);
            pale.a = 0.10f;

            ParticleSystem.MainModule main = ps.main;
            main.duration = 20f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 20f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = pale;
            main.maxParticles = 90;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 4.5f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 0.4f, 34f);

            // A PLAT, comme des nappes : un grand carre tourne vers la camera
            // passerait devant les yeux et voilerait tout l'ecran d'un coup.
            ParticleSystemRenderer flat = ps.GetComponent<ParticleSystemRenderer>();
            flat.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

            ParticleSystem.RotationOverLifetimeModule spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            FadeInOut(ps, 1f);
            ps.Play();
        }

        static void BuildFireflies(Transform parent, Vector3 at, int index)
        {
            ParticleSystem ps = NewSystem("Lucioles", parent, Vector3.zero, additive);
            ps.transform.position = at;

            ParticleSystem.MainModule main = ps.main;
            main.duration = 8f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 1f, 0.42f, 1f),
                                                                new Color(0.95f, 0.92f, 0.45f, 1f));
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 2.2f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 5f;

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.7f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.3f;

            // Une luciole ne brille pas en continu : elle s'allume, s'eteint, se
            // rallume. Trois pulsations sur sa vie.
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient blink = new Gradient();
            blink.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.1f, 0.3f),
                    new GradientAlphaKey(1f, 0.48f), new GradientAlphaKey(0.15f, 0.66f), new GradientAlphaKey(0.9f, 0.82f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(blink);

            // Chaque essaim a sa propre graine : sinon les 40 clignotent ensemble.
            ps.randomSeed = (uint)(1000 + index * 7919);
            ps.Play();
        }

        // ================================================================== outils

        /// <summary>
        /// Un ParticleSystem neuf, ARRETE : Unity refuse qu'on change la duree d'un
        /// systeme qui tourne, et AddComponent le demarre tout seul.
        /// </summary>
        static ParticleSystem NewSystem(string name, Transform parent, Vector3 localPosition, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        /// <summary>Apparition et disparition en fondu : jamais de grain qui "pop".</summary>
        static void FadeInOut(ParticleSystem ps, float peak)
        {
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.25f),
                        new GradientAlphaKey(peak, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);
        }

        /// <summary>
        /// Un point doux dessine pixel par pixel, et deux materiaux qui l'utilisent :
        /// l'un ADDITIF (la lumiere s'ajoute : poussieres, lucioles), l'autre
        /// MELANGE (on voit a travers : la brume).
        ///
        /// On cherche plusieurs shaders, du plus joli au plus sur : si aucun n'existe
        /// (build trop depouille), il n'y a simplement pas de particules -- le jeu,
        /// lui, tourne.
        /// </summary>
        static bool EnsureMaterials()
        {
            if (additive != null && blended != null) return true;

            if (softDot == null)
            {
                const int Size = 64;
                softDot = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                softDot.wrapMode = TextureWrapMode.Clamp;
                softDot.hideFlags = HideFlags.HideAndDontSave;
                Color[] pixels = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x + 0.5f) / Size * 2f - 1f;
                        float dy = (y + 0.5f) / Size * 2f - 1f;
                        float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                        float a = Mathf.Pow(1f - d, 2.2f);
                        pixels[y * Size + x] = new Color(1f, 1f, 1f, a);
                    }
                }
                softDot.SetPixels(pixels);
                softDot.Apply();
            }

            Shader add = Shader.Find("Legacy Shaders/Particles/Additive");
            Shader blend = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            Shader fallback = Shader.Find("Sprites/Default");
            if (add == null) add = fallback;
            if (blend == null) blend = fallback;
            if (add == null || blend == null) return false;

            additive = new Material(add) { name = "Particules additives", mainTexture = softDot };
            blended = new Material(blend) { name = "Particules brume", mainTexture = softDot };
            return true;
        }
    }
}
