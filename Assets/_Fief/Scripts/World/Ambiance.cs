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
    /// simulationSpace = World : une particule nee reste là où elle est nee, meme si
    /// l'emetteur (accroche au joueur) s'en va. Sans ca, toute la poussiere suivrait
    /// le joueur comme une bulle.
    /// </summary>
    public static class Ambiance
    {
        static Texture2D softDot;
        static Material additive;
        static Material blended;
        static uint sparkleCount;

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
            ParticleSystem ps = NewSystem("Poussières", player, new Vector3(0f, 1.6f, 0f), additive);

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

        /// <summary>Un essaim de lucioles, pour un creux ou un lieu-dit.</summary>
        public static void Fireflies(Transform parent, Vector3 at, int index)
        {
            if (!EnsureMaterials()) return;
            BuildFireflies(parent, at, index);
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

        /// <summary>
        /// Des etincelles qui montent d'un feu (braseros du chateau). Une poignee de
        /// points orange qui s'elevent, derivent et s'eteignent : c'est ce qui fait
        /// qu'un feu "vit", bien plus qu'une flamme qui bouge.
        /// </summary>
        public static void Embers(Transform parent, Vector3 localPosition)
        {
            if (!EnsureMaterials()) return;
            ParticleSystem ps = NewSystem("Étincelles", parent, localPosition, additive);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 4f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.18f, 1f),
                                                                new Color(1f, 0.82f, 0.4f, 1f));
            main.maxParticles = 40;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 9f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(-90f, 0f, 0f);     // le cone pointe vers le haut

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 1.2f;

            FadeInOut(ps, 1f);
            ps.randomSeed = (uint)localPosition.GetHashCode();
            ps.Play();
        }

        /// <summary>
        /// Des paillettes qui tombent lentement d'un talisman, de sa couleur. Elles
        /// se voient de plus loin que l'objet : on s'approche pour savoir ce que c'est.
        /// </summary>
        public static void Sparkles(Transform parent, Vector3 localPosition, Color tint)
        {
            if (!EnsureMaterials()) return;
            ParticleSystem ps = NewSystem("Paillettes", parent, localPosition, additive);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.Lerp(tint, Color.white, 0.6f));
            main.gravityModifier = 0.015f;
            main.maxParticles = 50;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 11f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.55f;

            FadeInOut(ps, 1f);
            // Chaque nuage de paillettes a sa graine : sinon ils scintillent tous en
            // meme temps. (GetInstanceID, qu'on aurait pu utiliser, est perime dans
            // Unity 6 : un simple compteur fait l'affaire.)
            sparkleCount++;
            ps.randomSeed = (uint)localPosition.GetHashCode() + sparkleCount * 7919u;
            ps.Play();
        }

        /// <summary>
        /// La pluie de l'orage, accrochee au joueur. Elle ne tombe pas encore : Sky
        /// regle son debit (0 hors orage). Chaque goutte meurt au premier contact --
        /// toit, feuillage, sol -- grace au module de collision : il ne pleut donc
        /// pas dans la salle du trone.
        /// </summary>
        public static ParticleSystem RainSystem(Transform player)
        {
            if (!EnsureMaterials()) return null;
            ParticleSystem ps = NewSystem("Pluie", player, new Vector3(0f, 11f, 0f), additive);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.03f);
            main.startColor = new Color(0.55f, 0.6f, 0.68f, 0.5f);
            main.maxParticles = 2500;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 1f, 30f);

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-2.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-16f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.8f);

            ParticleSystem.CollisionModule collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.lifetimeLoss = 1f;
            collision.bounce = 0f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.045f;
            renderer.lengthScale = 1f;
            return ps;
        }

        /// <summary>
        /// Une gerbe unique : quatre-vingts eclats projetes dans toutes les
        /// directions, qui ralentissent et s'eteignent. La forge du mage.
        /// Le systeme se detruit tout seul quand il a fini (stopAction).
        /// </summary>
        public static void Burst(Transform parent, Vector3 localPosition, Color tint)
        {
            if (!EnsureMaterials()) return;
            ParticleSystem ps = NewSystem("Gerbe", parent, localPosition, additive);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.white);
            main.gravityModifier = 0.25f;
            main.maxParticles = 120;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)80), new ParticleSystem.Burst(0.25f, (short)30) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = 0.6f;
            drag.dampen = 0.08f;

            FadeInOut(ps, 1f);
            burstCount++;
            ps.randomSeed = burstCount * 104729u;
            ps.Play();
        }

        static uint burstCount;

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
