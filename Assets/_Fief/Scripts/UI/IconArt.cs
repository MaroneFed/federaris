using System;

namespace Fief
{
    /// <summary>
    /// LES ICONES, DESSINEES PAR LE CODE (30/09 -- Martin : "je veux aucun texte a l'ecran,
    /// je deteste le texte, je veux des icones et des trucs plus pro comme Fall Guys").
    ///
    /// Chaque icone est une FORME MATHEMATIQUE : pour chaque point, une fonction dit a
    /// quelle distance il est du bord (negatif dedans, positif dehors). C'est ce qu'on
    /// appelle un "champ de distance signe" (SDF). L'avantage : des bords parfaitement
    /// lisses a toutes les tailles, un contour epais gratuit (tout ce qui est a moins de
    /// "w" du bord), et aucune image a dessiner a la main.
    ///
    /// Ce fichier est du C# pur (pas d'Unity) : Tools/icones le compile a part pour
    /// fabriquer des apercus PNG. Icons.cs en fait des textures pour l'ecran.
    ///
    /// Le repere : x et y de -1 a 1, y vers le BAS (comme l'ecran).
    /// </summary>
    public static class IconArt
    {
        /// <summary>Toutes les icones connues.</summary>
        public static readonly string[] Names =
        {
            "couronne", "tour", "monument", "ailes", "pousser", "chrono", "joueur", "souris-g", "souris-d", "souris-m",
            "courant", "arbaleste", "cible", "pique", "sacre", "haut", "don", "oeil", "coche", "croix",
            "jouer", "reglages", "quitter", "en-ligne", "commandes", "bot", "manches", "duree", "pseudo", "volume",
            "vue", "texte", "ecran", "retour", "drapeau", "cloche", "touche", "bouclier", "internet",
            "ruee", "grappin", "crochet", "onde", "clignement", "bond", "mur", "nuee", "mine", "gel", "voile",
            "echange", "rappel", "souffle", "double-saut", "planeur", "coureur", "porteur", "poigne", "ancrage",
            "flair", "ombre", "prise-ferme", "recharge", "rebond", "aimant",
            "meteore", "tornade", "trou-noir", "boulet", "geant", "fusee", "ressort", "foudre",
            "riposte", "vampire", "tete-dure", "second-souffle", "plume",
            "prison", "bombe", "inversion", "mini", "glu", "banane", "ballon", "seisme", "gant", "fantome",
            "taupe", "deluge", "toupie", "encre", "bras-longs", "kangourou", "kamikaze", "ange", "rage", "ninja",
            "lasso", "missile", "apesanteur", "flammes", "pogo", "geyser", "boomerang", "piege", "catapulte", "oreiller",
            "hypnose", "raz", "coup-de-pied", "cri", "miroir", "increvable", "pickpocket", "chanceux", "armure", "sprinter",
            "dieu", "apocalypse", "arret-temps", "rayon", "teleport", "tempete", "nuke", "main-dieu", "essaim", "gravite",
            "invincible", "colosse", "eclair", "main-lourde", "phenix", "sablier",
            "bombardement", "singularite", "dragon", "comete", "cataclysme", "chaos", "anneau-feu", "geole", "tsunami",
            "foudre-chaine", "explosif", "lave", "echo",
            "armee", "volcan", "orbitale", "rocher", "lune", "ouragan", "frappe-ciel", "enclume", "lilliput", "demence",
            "orage", "orbes", "titan",
            "boulet-bleu", "mouton", "sainte-grenade", "poing-faucon", "gobe-tout", "roue-folle", "charge", "tnt",
            "buche", "tonneau", "disco", "force-imparable", "saut-mario", "home-run",
            "etourdi", "chute", "ko", "clip"
        };

        // ================================================================== le rendu

        /// <summary>
        /// La couverture (0-1) de l'icone "id" sur une grille size x size (ligne 0 = en BAS,
        /// comme les textures d'Unity). "grow" epaissit la forme (0 : la forme ; 0,1 : un
        /// contour de 0,1 autour -- ce qui fait le liseré sombre).
        /// </summary>
        public static float[] Render(string id, int size, float grow)
        {
            float[] a = new float[size * size];
            float px = 2f / size;
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = -1f + (i + 0.5f) * px;
                    // Ligne 0 d'une texture = le BAS de l'image ; nos formes ont y vers le bas.
                    float y = 1f - (j + 0.5f) * px;
                    float d = Sdf(id, x, y) - grow;
                    a[j * size + i] = Clamp01(0.5f - d / px);
                }
            return a;
        }

        // ================================================================== les formes

        // Ces icones-la ont ete dessinees y vers le haut : on les retourne.
        static readonly string[] Flipped =
        {
            "tour", "monument", "chrono", "don", "coche", "commandes",
            "bot", "texte", "ecran", "drapeau", "nuee", "mine", "voile",
            "poigne", "flair"
        };

        public static float Sdf(string id, float x, float y)
        {
            for (int i = 0; i < Flipped.Length; i++) if (Flipped[i] == id) return Shape(id, x, -y);
            return Shape(id, x, y);
        }

        static float Shape(string id, float x, float y)
        {
            switch (id)
            {
                case "couronne": return Crown(x, y);
                case "tour": return Tower(x, y);
                case "monument":
                {
                    float body = Box(x, y + 0.05f, 0.62f, 0.62f, 0.06f);
                    float arch = Min(Circle(x, y + 0.02f, 0.36f), Box(x, y + 0.45f, 0.36f, 0.45f, 0f));
                    float roof = Poly(x, y, -0.82f, 0.62f, 0.82f, 0.62f, 0f, 0.95f);
                    float step = Box(x, y + 0.78f, 0.8f, 0.1f, 0.04f);
                    return Min(Min(Sub(body, arch), roof), step);
                }
                case "ailes": return Wings(x, y);
                case "planeur": return Min(Wings(x, y + 0.08f), Star(x, y - 0.62f, 0.3f, 0.13f, 5));
                case "pousser":
                {
                    // Une main ouverte qui pousse, et le choc devant elle.
                    float palm = Box(x + 0.02f, y - 0.14f, 0.36f, 0.34f, 0.18f);
                    float f = Min(Min(Seg(x, y, -0.24f, -0.05f, -0.24f, -0.66f, 0.1f), Seg(x, y, -0.04f, -0.05f, -0.04f, -0.8f, 0.1f)),
                                  Min(Seg(x, y, 0.16f, -0.05f, 0.16f, -0.74f, 0.1f), Seg(x, y, 0.34f, 0f, 0.34f, -0.56f, 0.09f)));
                    float thumb = Seg(x, y, -0.3f, 0.2f, -0.64f, -0.12f, 0.1f);
                    float wrist = Box(x + 0.02f, y - 0.62f, 0.26f, 0.16f, 0.06f);
                    float impact = Min(Seg(x, y, 0.62f, -0.5f, 0.86f, -0.72f, 0.06f), Min(Seg(x, y, 0.66f, -0.1f, 0.95f, -0.1f, 0.06f), Seg(x, y, 0.62f, 0.3f, 0.86f, 0.52f, 0.06f)));
                    return Min(Min(Min(palm, f), Min(thumb, wrist)), impact);
                }
                case "poigne":
                {
                    float fist = Box(x, y, 0.5f, 0.42f, 0.2f);
                    float k1 = Circle(x + 0.3f, y + 0.36f, 0.17f);
                    float k2 = Circle(x - 0.02f, y + 0.4f, 0.17f);
                    float k3 = Circle(x - 0.33f, y + 0.36f, 0.17f);
                    float thumb = Seg(x, y, -0.5f, -0.05f, -0.15f, -0.18f, 0.13f);
                    float wrist = Box(x, y + 0.66f, 0.34f, 0.22f, 0.06f);
                    float gap = Min(Seg(x, y, -0.17f, 0.35f, -0.17f, 0.1f, 0.025f), Seg(x, y, 0.15f, 0.35f, 0.15f, 0.1f, 0.025f));
                    return Sub(Min(Min(fist, Min(k1, Min(k2, k3))), Min(thumb, wrist)), gap);
                }
                case "chrono":
                {
                    float ring = Ring(x, y + 0.06f, 0.72f, 0.12f);
                    float top = Box(x, y - 0.86f, 0.16f, 0.08f, 0.03f);
                    float h1 = Seg(x, y, 0f, 0.06f, 0f, -0.42f, 0.08f);
                    float h2 = Seg(x, y, 0f, 0.06f, 0.3f, 0.2f, 0.08f);
                    return Min(Min(ring, top), Min(h1, h2));
                }
                case "joueur":
                {
                    float head = Circle(x, y + 0.36f, 0.3f);
                    float body = Sub(Circle(x, y - 1.05f, 0.82f), Box(x, y - 1.3f, 1f, 0.3f, 0f));
                    return Min(head, body);
                }
                case "bot":
                {
                    float head = Box(x, y - 0.05f, 0.66f, 0.52f, 0.18f);
                    float eyes = Min(Circle(x - 0.26f, y, 0.13f), Circle(x + 0.26f, y, 0.13f));
                    float mouth = Box(x, y - 0.3f, 0.26f, 0.05f, 0.02f);
                    float ant = Min(Seg(x, y, 0f, 0.45f, 0f, 0.75f, 0.06f), Circle(x, y - 0.8f, 0.12f));
                    float ears = Min(Box(x - 0.74f, y - 0.05f, 0.08f, 0.2f, 0.04f), Box(x + 0.74f, y - 0.05f, 0.08f, 0.2f, 0.04f));
                    return Min(Sub(head, Min(eyes, mouth)), Min(ant, ears));
                }
                case "souris-g": return Mouse(x, y, 0);
                case "souris-d": return Mouse(x, y, 1);
                case "souris-m": return Mouse(x, y, 2);
                case "courant":
                {
                    float a1 = ArrowUp(x + 0.42f, y + 0.1f, 0.5f);
                    float a2 = ArrowUp(x, y - 0.1f, 0.62f);
                    float a3 = ArrowUp(x - 0.42f, y + 0.1f, 0.5f);
                    return Min(a1, Min(a2, a3));
                }
                case "arbaleste":
                {
                    // De face, pointee vers le haut : l'arc, la corde tiree, le fut, le carreau.
                    float bow = Max(Ring(x, y - 0.62f, 0.9f, 0.1f), y + 0.02f);
                    float cord = Min(Seg(x, y, -0.64f, -0.04f, 0f, 0.28f, 0.035f), Seg(x, y, 0.64f, -0.04f, 0f, 0.28f, 0.035f));
                    float stock = Seg(x, y, 0f, -0.3f, 0f, 0.82f, 0.11f);
                    float butt = Box(x, y - 0.82f, 0.16f, 0.1f, 0.04f);
                    float bolt = Min(Seg(x, y, 0f, -0.62f, 0f, 0.1f, 0.05f), Poly(x, y, -0.16f, -0.6f, 0.16f, -0.6f, 0f, -0.94f));
                    return Min(Min(bow, cord), Min(Min(stock, butt), bolt));
                }
                case "cible":
                {
                    float r1 = Ring(x, y, 0.72f, 0.09f);
                    float r2 = Circle(x, y, 0.16f);
                    float cross = Min(Min(Seg(x, y, 0f, 0.5f, 0f, 0.95f, 0.07f), Seg(x, y, 0f, -0.5f, 0f, -0.95f, 0.07f)),
                                      Min(Seg(x, y, 0.5f, 0f, 0.95f, 0f, 0.07f), Seg(x, y, -0.5f, 0f, -0.95f, 0f, 0.07f)));
                    return Min(Min(r1, r2), cross);
                }
                case "pique":
                {
                    // Le pique d'aigle : les ailes repliees en V, et la chute droite vers la proie.
                    float wings = Min(Seg(x, y, -0.8f, -0.6f, 0f, 0.02f, 0.11f), Seg(x, y, 0.8f, -0.6f, 0f, 0.02f, 0.11f));
                    float shaft = Seg(x, y, 0f, -0.82f, 0f, 0.4f, 0.1f);
                    float head = Poly(x, y, -0.32f, 0.32f, 0.32f, 0.32f, 0f, 0.92f);
                    return Min(wings, Min(shaft, head));
                }
                case "sacre":
                {
                    float c = Crown(x * 1.4f, (y + 0.2f) * 1.4f) / 1.4f;
                    float ring = Ring(x, (y - 0.62f) * 2.6f, 0.82f, 0.12f) / 2.6f;
                    return Min(c, ring);
                }
                case "haut": return ArrowUp(x, y, 1f);
                case "don":
                {
                    float gem = Poly(x, y, -0.7f, 0.25f, 0.7f, 0.25f, 0f, -0.85f);
                    float top = Poly4(x, y, -0.7f, 0.25f, -0.4f, 0.65f, 0.4f, 0.65f, 0.7f, 0.25f);
                    float facets = Min(Seg(x, y, -0.7f, 0.25f, 0.7f, 0.25f, 0.035f), Min(Seg(x, y, -0.2f, 0.62f, -0.3f, 0.25f, 0.03f), Seg(x, y, 0.2f, 0.62f, 0.3f, 0.25f, 0.03f)));
                    return Sub(Min(gem, top), facets);
                }
                case "oeil":
                case "flair":
                {
                    float lens = Max(Circle(x, y - 0.62f, 0.95f), Circle(x, y + 0.62f, 0.95f));
                    float iris = Circle(x, y, 0.3f);
                    float pupil = Circle(x, y, 0.12f);
                    float eye = Min(Sub(lens, Circle(x, y, 0.42f)), Sub(iris, pupil));
                    if (id == "oeil") return eye;
                    float rays = Min(Seg(x, y, 0f, 0.62f, 0f, 0.92f, 0.06f), Min(Seg(x, y, -0.5f, 0.52f, -0.66f, 0.76f, 0.06f), Seg(x, y, 0.5f, 0.52f, 0.66f, 0.76f, 0.06f)));
                    return Min(eye, rays);
                }
                case "coche": return Min(Seg(x, y, -0.62f, 0.02f, -0.2f, -0.45f, 0.14f), Seg(x, y, -0.2f, -0.45f, 0.65f, 0.55f, 0.14f));
                case "croix": return Min(Seg(x, y, -0.55f, -0.55f, 0.55f, 0.55f, 0.14f), Seg(x, y, -0.55f, 0.55f, 0.55f, -0.55f, 0.14f));
                case "jouer": return Poly(x, y, -0.45f, 0.72f, -0.45f, -0.72f, 0.72f, 0f) - 0.06f;
                case "reglages": return Gear(x, y);
                case "quitter":
                {
                    float ring = Max(Ring(x, y + 0.05f, 0.62f, 0.12f), -Box(x, y + 0.62f, 0.3f, 0.35f, 0f));
                    return Min(ring, Seg(x, y, 0f, -0.85f, 0f, -0.1f, 0.12f));
                }
                case "en-ligne":
                {
                    float globe = Ring(x, y, 0.78f, 0.09f);
                    float m1 = Max(Ring(x * 2.2f, y, 0.78f, 0.2f) / 2.2f, Circle(x, y, 0.78f));
                    float eq = Max(Seg(x, y, -0.8f, 0f, 0.8f, 0f, 0.06f), Circle(x, y, 0.78f));
                    float l1 = Max(Seg(x, y, -0.8f, -0.38f, 0.8f, -0.38f, 0.05f), Circle(x, y, 0.78f));
                    float l2 = Max(Seg(x, y, -0.8f, 0.38f, 0.8f, 0.38f, 0.05f), Circle(x, y, 0.78f));
                    float mid = Seg(x, y, 0f, -0.78f, 0f, 0.78f, 0.05f);
                    return Min(Min(globe, m1), Min(Min(eq, mid), Min(l1, l2)));
                }
                case "commandes":
                {
                    float pad = Min(Box(x, y + 0.02f, 0.62f, 0.36f, 0.3f), Min(Circle(x - 0.52f, y + 0.25f, 0.34f), Circle(x + 0.52f, y + 0.25f, 0.34f)));
                    float cross = Min(Box(x + 0.42f, y, 0.2f, 0.06f, 0.02f), Box(x + 0.42f, y, 0.06f, 0.2f, 0.02f));
                    float btn = Min(Circle(x - 0.36f, y - 0.08f, 0.08f), Circle(x - 0.52f, y + 0.08f, 0.08f));
                    return Sub(pad, Min(cross, btn));
                }
                case "manches":
                {
                    float d1 = Circle(x + 0.55f, y, 0.2f);
                    float d2 = Circle(x, y, 0.2f);
                    float d3 = Ring(x - 0.55f, y, 0.18f, 0.06f);
                    return Min(d1, Min(d2, d3));
                }
                case "duree":
                {
                    float glass = Min(Poly(x, y, -0.5f, -0.72f, 0.5f, -0.72f, 0f, -0.02f), Poly(x, y, -0.5f, 0.72f, 0.5f, 0.72f, 0f, 0.02f));
                    float frame = Min(Box(x, y - 0.82f, 0.62f, 0.08f, 0.04f), Box(x, y + 0.82f, 0.62f, 0.08f, 0.04f));
                    float sand = Sub(glass, Box(x, y - 0.5f, 0.6f, 0.3f, 0f));
                    return Min(Sub(glass, Max(Poly(x, y, -0.36f, -0.6f, 0.36f, -0.6f, 0f, -0.14f) + 0.02f, -Box(x, y + 0.45f, 0.6f, 0.3f, 0f))), frame);
                }
                case "pseudo":
                {
                    float tag = Box(x, y + 0.05f, 0.82f, 0.46f, 0.14f);
                    float hole = Circle(x + 0.6f, y + 0.05f, 0.1f);
                    float lines = Min(Box(x + 0.18f, y - 0.1f, 0.38f, 0.06f, 0.03f), Box(x + 0.1f, y + 0.2f, 0.3f, 0.06f, 0.03f));
                    return Sub(tag, Min(hole, lines));
                }
                case "volume":
                {
                    float spk = Min(Box(x + 0.5f, y, 0.16f, 0.22f, 0.03f), Poly(x, y, -0.36f, 0.2f, -0.36f, -0.2f, 0.12f, 0.58f) - 0f);
                    float cone = Poly4(x, y, -0.36f, 0.22f, 0.1f, 0.62f, 0.1f, -0.62f, -0.36f, -0.22f);
                    float w1 = Max(Ring(x - 0.1f, y, 0.42f, 0.07f), -(x - 0.25f));
                    float w2 = Max(Ring(x - 0.1f, y, 0.7f, 0.07f), -(x - 0.35f));
                    return Min(Min(Box(x + 0.5f, y, 0.18f, 0.22f, 0.03f), cone), Min(w1, w2));
                }
                case "vue":
                {
                    float cone = Poly(x, y, -0.8f, 0f, 0.7f, 0.62f, 0.7f, -0.62f);
                    float inner = Poly(x, y, -0.55f, 0f, 0.62f, 0.45f, 0.62f, -0.45f);
                    return Min(Sub(cone, inner), Circle(x + 0.72f, y, 0.2f));
                }
                case "texte":
                {
                    float a = Min(Seg(x, y, -0.78f, -0.6f, -0.45f, 0.6f, 0.1f), Seg(x, y, -0.45f, 0.6f, -0.12f, -0.6f, 0.1f));
                    float bar = Seg(x, y, -0.62f, -0.15f, -0.28f, -0.15f, 0.08f);
                    float b = Min(Ring(x - 0.45f, y + 0.25f, 0.28f, 0.1f), Seg(x, y, 0.73f, 0.0f, 0.73f, -0.6f, 0.1f));
                    return Min(Min(a, bar), b);
                }
                case "ecran":
                {
                    float scr = Sub(Box(x, y - 0.12f, 0.82f, 0.52f, 0.08f), Box(x, y - 0.12f, 0.66f, 0.38f, 0.02f));
                    float foot = Min(Box(x, y + 0.56f, 0.1f, 0.14f, 0f), Box(x, y + 0.72f, 0.36f, 0.06f, 0.03f));
                    return Min(scr, foot);
                }
                case "retour": return Min(Seg(x, y, -0.2f, 0f, 0.75f, 0f, 0.13f), Poly(x, y, -0.85f, 0f, -0.2f, -0.55f, -0.2f, 0.55f));
                case "drapeau":
                {
                    float pole = Seg(x, y, -0.55f, -0.85f, -0.55f, 0.8f, 0.07f);
                    float flag = Poly4(x, y, -0.5f, 0.78f, 0.7f, 0.62f, 0.55f, 0.28f, -0.5f, 0.08f);
                    return Min(pole, flag);
                }
                case "cloche":
                {
                    float bell = Min(Circle(x, y + 0.2f, 0.42f), Poly4(x, y, -0.42f, -0.2f, 0.42f, -0.2f, 0.64f, 0.45f, -0.64f, 0.45f));
                    float lip = Box(x, y - 0.48f, 0.74f, 0.08f, 0.05f);
                    float clap = Circle(x, y - 0.68f, 0.13f);
                    float loop = Ring(x, y + 0.72f, 0.11f, 0.05f);
                    return Min(Min(bell, lip), Min(clap, loop));
                }
                case "touche": return Sub(Box(x, y, 0.78f, 0.78f, 0.22f), Box(x, y + 0.08f, 0.6f, 0.6f, 0.16f)) ;
                case "bouclier":
                    return Min(Box(x, y + 0.25f, 0.62f, 0.42f, 0.12f), Poly(x, y, -0.62f, 0.1f, 0.62f, 0.1f, 0f, 0.9f));
                case "ruee":
                    return Min(Min(Chevron(x - 0.1f, y, 0.55f), Chevron(x - 0.55f, y, 0.55f)),
                               Min(Seg(x, y, -0.95f, 0.45f, -0.75f, 0.45f, 0.06f), Seg(x, y, -0.95f, -0.45f, -0.75f, -0.45f, 0.06f)));
                case "grappin":
                {
                    float shaft = Seg(x, y, 0f, 0.72f, 0f, -0.3f, 0.09f);
                    float hooks = Min(Max(Ring(x - 0.34f, y + 0.3f, 0.34f, 0.09f), y + 0.3f),
                                      Max(Ring(x + 0.34f, y + 0.3f, 0.34f, 0.09f), y + 0.3f));
                    float tips = Min(Poly(x, y, -0.78f, -0.3f, -0.56f, -0.3f, -0.72f, -0.62f), Poly(x, y, 0.78f, -0.3f, 0.56f, -0.3f, 0.72f, -0.62f));
                    float ring = Ring(x, y - 0.8f, 0.14f, 0.06f);
                    return Min(Min(shaft, hooks), Min(tips, ring));
                }
                case "crochet":
                {
                    // Un croc au bout d'une chaine : il attrape et ramene.
                    float shank = Seg(x, y, 0.3f, -0.5f, 0.3f, 0.3f, 0.11f);
                    float hook = Max(Ring(x + 0.02f, y - 0.3f, 0.28f, 0.11f), -(y - 0.3f));
                    float tip = Poly(x, y, -0.4f, 0.28f, -0.12f, 0.28f, -0.26f, -0.02f);
                    float link1 = Ring((x - 0.3f) * 1.4f, y + 0.66f, 0.13f, 0.06f);
                    float link2 = Ring(x - 0.3f, (y + 0.9f) * 1.4f, 0.1f, 0.05f);
                    return Min(Min(shank, hook), Min(tip, Min(link1, link2)));
                }
                case "onde": return Min(Circle(x, y, 0.2f), Min(Ring(x, y, 0.48f, 0.08f), Ring(x, y, 0.8f, 0.07f)));
                case "clignement": return Min(Min(Circle(x + 0.75f, y, 0.09f), Circle(x + 0.45f, y, 0.12f)), Star(x - 0.15f, y, 0.62f, 0.22f, 4));
                case "bond": return Min(ArrowUp(x, y + 0.12f, 0.8f), Box(x, y - 0.82f, 0.7f, 0.07f, 0.04f));
                case "mur":
                {
                    float wall = Box(x, y, 0.8f, 0.62f, 0.06f);
                    float m1 = Box(x, y - 0.21f, 0.82f, 0.035f, 0f);
                    float m2 = Box(x, y + 0.21f, 0.82f, 0.035f, 0f);
                    float v1 = Min(Box(x + 0.3f, y - 0.42f, 0.035f, 0.21f, 0f), Box(x - 0.3f, y - 0.42f, 0.035f, 0.21f, 0f));
                    float v2 = Box(x, y, 0.035f, 0.21f, 0f);
                    float v3 = Min(Box(x + 0.3f, y + 0.42f, 0.035f, 0.21f, 0f), Box(x - 0.3f, y + 0.42f, 0.035f, 0.21f, 0f));
                    return Sub(wall, Min(Min(m1, m2), Min(v1, Min(v2, v3))));
                }
                case "nuee": return Cloud(x, y);
                case "etourdi":
                {
                    // (02/10) ETOURDI : trois etoiles qui tournent sur une orbite aplatie (avant :
                    // l'icone du Clignement -- une meme image pour deux choses).
                    float yy = (y - 0.1f) * 2.3f;
                    float orbit = (Math.Abs((float)Math.Sqrt(x * x + yy * yy) - 0.72f) - 0.1f) / 1.6f;
                    float stars = Min(Star(x + 0.66f, y - 0.1f, 0.3f, 0.13f, 5), Min(Star(x - 0.66f, y - 0.1f, 0.3f, 0.13f, 5), Star(x, y + 0.26f, 0.36f, 0.15f, 5)));
                    return Min(orbit, stars);
                }
                case "ko":
                    // (02/10, le clipper) LE KO : une etoile d'explosion a huit branches, un
                    // eclat au coeur -- celui qu'on a pousse dans les nuages.
                    return Sub(Star(x, y, 0.98f, 0.5f, 8), Circle(x, y, 0.2f));
                case "clip":
                {
                    // UN MOMENT A CLIPPER : une claquette de cinema (le cadre, la barre qui claque).
                    float body = Sub(Box(x, y - 0.18f, 0.8f, 0.5f, 0.12f), Box(x, y - 0.18f, 0.62f, 0.32f, 0.06f));
                    float bar = Box(x, y + 0.56f, 0.8f, 0.12f, 0.06f);
                    return Min(Min(body, bar), Circle(x, y - 0.18f, 0.16f));
                }
                case "chute":
                    // (02/10) LA CHUTE : une fleche qui plonge vers une ligne (avant : l'icone
                    // de la Nuee pour "tombe dans les nuages").
                    return Min(ArrowUp(x, -(y + 0.12f), 0.78f), Seg(x, y, -0.72f, 0.84f, 0.72f, 0.84f, 0.08f));
                case "mine":
                {
                    float body = Circle(x, y + 0.08f, 0.5f);
                    float spikes = Star(x, y + 0.08f, 0.8f, 0.4f, 8);
                    float fuse = Seg(x, y, 0.3f, 0.5f, 0.55f, 0.8f, 0.06f);
                    float spark = Circle(x - 0.14f, y - 0.06f, 0.1f);
                    return Sub(Min(Min(body, spikes), fuse), spark);
                }
                case "gel":
                {
                    float d = 10f;
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * (float)Math.PI / 3f;
                        float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                        d = Min(d, Seg(x, y, -c * 0.82f, -s * 0.82f, c * 0.82f, s * 0.82f, 0.07f));
                        for (int e = -1; e <= 1; e += 2)
                        {
                            float bx = c * 0.5f * e, by = s * 0.5f * e;
                            float na = a + (float)Math.PI * 0.25f, nb = a - (float)Math.PI * 0.25f;
                            d = Min(d, Seg(x, y, bx, by, bx + (float)Math.Cos(na) * 0.24f * e, by + (float)Math.Sin(na) * 0.24f * e, 0.055f));
                            d = Min(d, Seg(x, y, bx, by, bx + (float)Math.Cos(nb) * 0.24f * e, by + (float)Math.Sin(nb) * 0.24f * e, 0.055f));
                        }
                    }
                    return d;
                }
                case "voile":
                {
                    float ghost = Min(Circle(x, y - 0.2f, 0.55f), Box(x, y + 0.25f, 0.55f, 0.45f, 0f));
                    float hem = Min(Circle(x - 0.37f, y + 0.72f, 0.18f), Min(Circle(x, y + 0.72f, 0.18f), Circle(x + 0.37f, y + 0.72f, 0.18f)));
                    float cut = Min(Circle(x - 0.18f, y + 0.72f, 0.12f), Circle(x + 0.18f, y + 0.72f, 0.12f));
                    float eyes = Min(Circle(x - 0.2f, y - 0.2f, 0.11f), Circle(x + 0.2f, y - 0.2f, 0.11f));
                    return Sub(Min(Sub(ghost, Box(x, y + 0.8f, 0.6f, 0.1f, 0f)), hem), Min(cut, eyes));
                }
                case "echange":
                {
                    float top = Min(Seg(x, y, -0.6f, -0.32f, 0.45f, -0.32f, 0.1f), Poly(x, y, 0.35f, -0.62f, 0.35f, -0.02f, 0.82f, -0.32f));
                    float bot = Min(Seg(x, y, 0.6f, 0.32f, -0.45f, 0.32f, 0.1f), Poly(x, y, -0.35f, 0.62f, -0.35f, 0.02f, -0.82f, 0.32f));
                    return Min(top, bot);
                }
                case "rappel":
                case "recharge":
                {
                    float arc = Max(Ring(x, y, 0.58f, 0.12f), -Max(-x - 0.05f, -y));
                    float head = Poly(x, y, -0.05f, -0.9f, -0.05f, -0.26f, -0.5f, -0.58f);
                    if (id == "rappel") return Min(Min(arc, head), Min(Seg(x, y, 0f, 0f, 0f, -0.32f, 0.07f), Seg(x, y, 0f, 0f, 0.24f, 0.14f, 0.07f)));
                    float bolt = Poly4(x, y, 0.06f, -0.4f, -0.2f, 0.06f, 0.02f, 0.06f, -0.08f, 0.42f) - 0.0f;
                    float bolt2 = Poly4(x, y, 0.2f, -0.04f, -0.02f, -0.04f, 0.08f, -0.42f, 0.2f, -0.04f);
                    return Min(Min(arc, head), Min(Poly(x, y, 0.1f, -0.44f, -0.22f, 0.06f, 0.04f, 0.06f), Poly(x, y, -0.02f, -0.04f, 0.22f, -0.04f, -0.08f, 0.44f)));
                }
                case "souffle":
                {
                    float w1 = Min(Seg(x, y, -0.85f, -0.35f, 0.3f, -0.35f, 0.08f), Max(Ring(x - 0.3f, y + 0.6f, 0.25f, 0.08f), -(x - 0.3f)));
                    float w2 = Min(Seg(x, y, -0.85f, 0f, 0.55f, 0f, 0.08f), Max(Ring(x - 0.55f, y + 0.25f, 0.25f, 0.08f), -(x - 0.55f)));
                    float w3 = Min(Seg(x, y, -0.85f, 0.38f, 0.1f, 0.38f, 0.08f), Max(Ring(x - 0.1f, y - 0.15f, 0.23f, 0.08f), -(x - 0.1f)));
                    return Min(w1, Min(w2, w3));
                }
                case "double-saut": return Min(Chevron2(x, y + 0.3f), Chevron2(x, y - 0.25f));
                case "coureur": return Min(Poly(x, y, 0.28f, -0.92f, -0.5f, 0.14f, 0.05f, 0.14f), Poly(x, y, -0.05f, -0.1f, 0.5f, -0.1f, -0.28f, 0.92f));
                case "porteur":
                {
                    // La Couronne, et un poing dessous : on la porte ET on pousse.
                    float c = Crown(x * 1.7f, (y + 0.42f) * 1.7f) / 1.7f;
                    float fist = Box(x, y - 0.42f, 0.42f, 0.3f, 0.16f);
                    float gaps = Min(Seg(x, y, -0.14f, 0.22f, -0.14f, 0.42f, 0.025f), Seg(x, y, 0.14f, 0.22f, 0.14f, 0.42f, 0.025f));
                    return Min(c, Sub(fist, gaps));
                }
                case "ancrage":
                {
                    float ring = Ring(x, y + 0.76f, 0.13f, 0.06f);
                    float stock = Seg(x, y, -0.3f, -0.45f, 0.3f, -0.45f, 0.07f);
                    float shank = Seg(x, y, 0f, -0.63f, 0f, 0.74f, 0.08f);
                    float arms = Max(Ring(x, y - 0.1f, 0.62f, 0.08f), 0.28f - y);
                    float tips = Min(Poly(x, y, -0.78f, 0.36f, -0.46f, 0.3f, -0.66f, 0.04f), Poly(x, y, 0.78f, 0.36f, 0.46f, 0.3f, 0.66f, 0.04f));
                    return Min(Min(ring, stock), Min(shank, Min(arms, tips)));
                }
                case "ombre":
                {
                    float moon = Sub(Circle(x, y, 0.72f), Circle(x + 0.38f, y - 0.22f, 0.6f));
                    return Min(moon, Star(x - 0.55f + 1f, y + 0.5f, 0.2f, 0.08f, 4) );
                }
                case "prise-ferme":
                {
                    float body = Box(x, y - 0.3f, 0.6f, 0.42f, 0.1f);
                    float shackle = Max(Ring(x, y + 0.15f, 0.36f, 0.1f), y + 0.15f);
                    float legs = Min(Seg(x, y, -0.36f, -0.15f, -0.36f, 0f, 0.1f), Seg(x, y, 0.36f, -0.15f, 0.36f, 0f, 0.1f));
                    float hole = Min(Circle(x, y - 0.22f, 0.1f), Box(x, y - 0.38f, 0.04f, 0.13f, 0.02f));
                    return Min(Sub(body, hole), Min(shackle, legs));
                }
                case "rebond":
                {
                    float arrow = Min(Seg(x, y, 0f, -0.85f, 0f, 0.05f, 0.1f), Poly(x, y, -0.3f, 0.02f, 0.3f, 0.02f, 0f, 0.38f));
                    float ground = Box(x, y - 0.62f, 0.82f, 0.07f, 0.04f);
                    float burst = Min(Seg(x, y, -0.36f, 0.42f, -0.72f, 0.18f, 0.06f), Seg(x, y, 0.36f, 0.42f, 0.72f, 0.18f, 0.06f));
                    return Min(Min(arrow, ground), burst);
                }
                case "aimant":
                {
                    float u = Max(Ring(x, y - 0.05f, 0.5f, 0.2f), -(y - 0.05f));
                    float legs = Min(Box(x - 0.5f, y + 0.3f, 0.2f, 0.36f, 0f), Box(x + 0.5f, y + 0.3f, 0.2f, 0.36f, 0f));
                    return Sub(Min(u, legs), Box(x, y + 0.42f, 1f, 0.03f, 0f));
                }
                // ============================================ les capacites de malade (05/10)
                case "meteore":
                {
                    // Une boule de feu qui plonge en bas a droite, trois trainees derriere elle.
                    float ball = Circle(x - 0.28f, y - 0.28f, 0.36f);
                    float t1 = Seg(x, y, -0.92f, -0.42f, -0.2f, 0.3f, 0.08f);
                    float t2 = Seg(x, y, -0.62f, -0.88f, 0.1f, -0.15f, 0.08f);
                    float t3 = Seg(x, y, -0.95f, -0.95f, -0.3f, -0.3f, 0.11f);
                    return Min(ball, Min(t1, Min(t2, t3)));
                }
                case "tornade":
                {
                    float d = Seg(x, y, -0.82f, -0.66f, 0.82f, -0.66f, 0.1f);
                    d = Min(d, Seg(x, y, -0.58f, -0.3f, 0.66f, -0.3f, 0.09f));
                    d = Min(d, Seg(x, y, -0.32f, 0.06f, 0.48f, 0.06f, 0.085f));
                    d = Min(d, Seg(x, y, -0.12f, 0.4f, 0.32f, 0.4f, 0.08f));
                    return Min(d, Seg(x, y, 0.02f, 0.72f, 0.18f, 0.72f, 0.075f));
                }
                case "trou-noir":
                {
                    float core = Circle(x, y, 0.24f);
                    float a1 = Max(Ring(x, y, 0.52f, 0.08f), -x);
                    float a2 = Max(Ring(x, y, 0.8f, 0.07f), x);
                    float a3 = Max(Ring(x, y + 0.04f, 0.52f, 0.06f), x + 0.6f);
                    return Min(core, Min(a1, Min(a2, a3)));
                }
                case "boulet":
                {
                    float ball = Circle(x - 0.28f, y, 0.46f);
                    float lines = Min(Seg(x, y, 0.32f, -0.32f, 0.92f, -0.32f, 0.07f), Min(Seg(x, y, 0.36f, 0f, 0.95f, 0f, 0.07f), Seg(x, y, 0.32f, 0.32f, 0.92f, 0.32f, 0.07f)));
                    return Min(Sub(ball, Circle(x - 0.42f, y - 0.16f, 0.1f)), lines);
                }
                case "geant":
                {
                    float head = Circle(x, y + 0.62f, 0.24f);
                    float body = Box(x, y - 0.25f, 0.48f, 0.5f, 0.3f);
                    float up = Min(ArrowUp(x - 0.8f, y + 0.1f, 0.3f), ArrowUp(x + 0.8f, y + 0.1f, 0.3f));
                    return Min(Min(head, body), up);
                }
                case "fusee":
                {
                    float body = Box(x, y - 0.05f, 0.22f, 0.5f, 0.2f);
                    float nose = Poly(x, y, -0.22f, -0.45f, 0.22f, -0.45f, 0f, -0.92f);
                    float fins = Min(Poly(x, y, -0.22f, 0.2f, -0.52f, 0.6f, -0.22f, 0.45f), Poly(x, y, 0.22f, 0.2f, 0.52f, 0.6f, 0.22f, 0.45f));
                    float flame = Poly(x, y, -0.13f, 0.56f, 0.13f, 0.56f, 0f, 0.95f);
                    return Sub(Min(Min(body, nose), Min(fins, flame)), Circle(x, y - 0.12f, 0.09f));
                }
                case "ressort":
                {
                    float top = Box(x, y - 0.62f, 0.8f, 0.1f, 0.06f);
                    float foot = Box(x, y + 0.78f, 0.6f, 0.08f, 0.04f);
                    float z = Seg(x, y, -0.5f, -0.48f, 0.5f, -0.22f, 0.07f);
                    z = Min(z, Seg(x, y, 0.5f, -0.22f, -0.5f, 0.06f, 0.07f));
                    z = Min(z, Seg(x, y, -0.5f, 0.06f, 0.5f, 0.34f, 0.07f));
                    z = Min(z, Seg(x, y, 0.5f, 0.34f, -0.5f, 0.62f, 0.07f));
                    return Min(Min(top, foot), z);
                }
                case "foudre":
                    return Min(Poly(x, y, 0.22f, -0.95f, -0.42f, 0.12f, 0.08f, 0.12f), Poly(x, y, -0.08f, -0.08f, 0.42f, -0.08f, -0.24f, 0.95f));
                case "riposte":
                {
                    // Une fleche qui frappe un mur et REVIENT.
                    float wall = Box(x - 0.66f, y, 0.08f, 0.78f, 0.03f);
                    float go = Min(Seg(x, y, -0.85f, -0.34f, 0.24f, -0.34f, 0.08f), Poly(x, y, 0.18f, -0.58f, 0.18f, -0.1f, 0.5f, -0.34f));
                    float back = Min(Seg(x, y, 0.42f, 0.34f, -0.45f, 0.34f, 0.08f), Poly(x, y, -0.4f, 0.1f, -0.4f, 0.58f, -0.82f, 0.34f));
                    return Min(wall, Min(go, back));
                }
                case "vampire":
                {
                    // Deux crocs.
                    float jaw = Box(x, y + 0.55f, 0.75f, 0.14f, 0.08f);
                    float f1 = Poly(x, y, -0.5f, -0.45f, -0.14f, -0.45f, -0.32f, 0.55f);
                    float f2 = Poly(x, y, 0.14f, -0.45f, 0.5f, -0.45f, 0.32f, 0.55f);
                    return Min(jaw, Min(f1, f2));
                }
                case "tete-dure":
                {
                    // Un heaume : la coque arrondie, la fente des yeux, le nasal.
                    float shell = Box(x, y - 0.08f, 0.62f, 0.78f, 0.55f);
                    float slit = Box(x, y + 0.08f, 0.48f, 0.07f, 0.03f);
                    float nasal = Box(x, y - 0.22f, 0.06f, 0.22f, 0.02f);
                    return Sub(shell, Min(Sub(slit, nasal), Box(x, y - 0.72f, 0.08f, 0.1f, 0.02f)));
                }
                case "second-souffle":
                    return Min(Wings(x, y + 0.2f), Star(x, y - 0.62f, 0.3f, 0.12f, 4));
                case "plume":
                {
                    float u = (x + y) * 0.7071f, v = (y - x) * 0.7071f;
                    float leaf = ((float)Math.Sqrt(u * u + v * v * 5.5f) - 0.8f) * 0.5f;
                    return Sub(Min(leaf, Seg(x, y, 0.5f, 0.5f, 0.88f, 0.88f, 0.05f)), Seg(x, y, -0.5f, -0.5f, 0.5f, 0.5f, 0.025f));
                }
                // ============================================ les capacites de fou (06/10)
                case "prison":
                {
                    // Une cage : un cadre arrondi et trois barreaux.
                    float frame = Sub(Box(x, y, 0.7f, 0.82f, 0.14f), Box(x, y, 0.54f, 0.66f, 0.06f));
                    float bars = Min(Box(x - 0.2f, y, 0.065f, 0.7f, 0f), Box(x + 0.2f, y, 0.065f, 0.7f, 0f));
                    return Min(frame, bars);
                }
                case "bombe":
                {
                    float body = Sub(Circle(x + 0.1f, y - 0.15f, 0.6f), Circle(x - 0.1f, y + 0.02f, 0.12f));
                    float fuse = Seg(x, y, 0.45f, -0.5f, 0.62f, -0.72f, 0.08f);
                    float spark = Star(x - 0.72f, y + 0.82f, 0.24f, 0.09f, 5);
                    return Min(Min(body, fuse), spark);
                }
                case "inversion":
                    // Une fleche monte, l'autre descend.
                    return Min(ArrowUp(x + 0.42f, y, 0.55f), ArrowUp(x - 0.42f, -y, 0.55f));
                case "mini":
                {
                    // Un tout petit bonhomme, et deux fleches qui le pressent.
                    float man = Min(Circle(x, y - 0.1f, 0.17f), Box(x, y - 0.55f, 0.2f, 0.25f, 0.14f));
                    float a1 = Min(Seg(x, y, -0.9f, -0.85f, -0.42f, -0.37f, 0.08f), Poly(x, y, -0.3f, -0.25f, -0.62f, -0.3f, -0.35f, -0.57f));
                    float a2 = Min(Seg(x, y, 0.9f, -0.85f, 0.42f, -0.37f, 0.08f), Poly(x, y, 0.3f, -0.25f, 0.62f, -0.3f, 0.35f, -0.57f));
                    return Min(man, Min(a1, a2));
                }
                case "glu":
                {
                    float drop = Min(Circle(x, y - 0.1f, 0.46f), Poly(x, y, -0.34f, -0.05f, 0.34f, -0.05f, 0f, -0.85f));
                    float pool = Box(x, y - 0.76f, 0.88f, 0.11f, 0.1f);
                    return Sub(Min(drop, pool), Circle(x + 0.16f, y + 0.02f, 0.1f));
                }
                case "banane":
                {
                    float c = Sub(Circle(x - 0.05f, y + 0.05f, 0.82f), Circle(x - 0.38f, y - 0.32f, 0.82f));
                    return Min(Max(c, Box(x, y, 0.95f, 0.95f, 0f)), Box(x + 0.7f, y - 0.62f, 0.09f, 0.14f, 0.04f));
                }
                case "ballon":
                {
                    float ball = Sub(Circle(x, y + 0.2f, 0.58f), Circle(x - 0.22f, y + 0.44f, 0.13f));
                    float knot = Poly(x, y, -0.12f, 0.46f, 0.12f, 0.46f, 0f, 0.32f);
                    float line = Min(Seg(x, y, 0f, 0.44f, 0.12f, 0.68f, 0.045f), Seg(x, y, 0.12f, 0.68f, -0.05f, 0.95f, 0.045f));
                    return Min(Min(ball, knot), line);
                }
                case "seisme":
                {
                    float ground = Box(x, y - 0.72f, 0.92f, 0.08f, 0.04f);
                    float wave = Seg(x, y, -0.92f, 0.1f, -0.55f, 0.1f, 0.08f);
                    wave = Min(wave, Seg(x, y, -0.55f, 0.1f, -0.32f, -0.55f, 0.08f));
                    wave = Min(wave, Seg(x, y, -0.32f, -0.55f, -0.02f, 0.5f, 0.08f));
                    wave = Min(wave, Seg(x, y, -0.02f, 0.5f, 0.28f, -0.35f, 0.08f));
                    wave = Min(wave, Seg(x, y, 0.28f, -0.35f, 0.52f, 0.1f, 0.08f));
                    wave = Min(wave, Seg(x, y, 0.52f, 0.1f, 0.92f, 0.1f, 0.08f));
                    return Min(ground, wave);
                }
                case "gant":
                {
                    float fist = Min(Circle(x + 0.05f, y + 0.2f, 0.55f), Circle(x - 0.47f, y - 0.08f, 0.24f));
                    float cuff = Box(x + 0.05f, y - 0.62f, 0.36f, 0.2f, 0.07f);
                    return Sub(Min(fist, cuff), Box(x + 0.05f, y - 0.4f, 0.46f, 0.035f, 0f));
                }
                case "fantome":
                {
                    float body = Min(Circle(x, y + 0.22f, 0.56f), Box(x, y - 0.25f, 0.56f, 0.47f, 0f));
                    body = Sub(body, Circle(x - 0.28f, y - 0.76f, 0.17f));
                    body = Sub(body, Circle(x + 0.28f, y - 0.76f, 0.17f));
                    return Sub(Sub(body, Circle(x - 0.2f, y + 0.25f, 0.1f)), Circle(x + 0.2f, y + 0.25f, 0.1f));
                }
                case "taupe":
                {
                    float mound = Max(Circle(x, y - 0.95f, 0.75f), y - 0.82f);
                    return Min(mound, ArrowUp(x, y + 0.3f, 0.45f));
                }
                case "deluge":
                {
                    // Trois meteores, leur trainee vers le haut a droite.
                    float d = Min(Circle(x + 0.5f, y - 0.5f, 0.2f), Seg(x, y, -0.5f, 0.5f, -0.15f, 0.15f, 0.07f));
                    d = Min(d, Min(Circle(x - 0.35f, y - 0.2f, 0.2f), Seg(x, y, 0.35f, 0.2f, 0.7f, -0.15f, 0.07f)));
                    return Min(d, Min(Circle(x + 0.15f, y + 0.35f, 0.18f), Seg(x, y, -0.15f, -0.35f, 0.18f, -0.68f, 0.06f)));
                }
                case "toupie":
                {
                    float cone = Poly(x, y, -0.62f, -0.12f, 0.62f, -0.12f, 0f, 0.85f);
                    float rim = Box(x, y + 0.2f, 0.64f, 0.11f, 0.1f);
                    float handle = Box(x, y + 0.55f, 0.08f, 0.22f, 0.04f);
                    float swirl = Max(Ring(x, y - 0.15f, 0.82f, 0.05f), -(y + 0.3f));
                    return Min(Min(cone, rim), Min(handle, swirl));
                }
                case "encre":
                {
                    float d = Circle(x, y, 0.46f);
                    d = Min(d, Circle(x - 0.62f, y + 0.3f, 0.15f));
                    d = Min(d, Circle(x + 0.55f, y - 0.42f, 0.18f));
                    d = Min(d, Circle(x + 0.42f, y + 0.62f, 0.12f));
                    d = Min(d, Circle(x - 0.55f, y - 0.5f, 0.1f));
                    d = Min(d, Circle(x + 0.05f, y - 0.75f, 0.13f));
                    return Min(d, Seg(x, y, -0.2f, 0.3f, -0.25f, 0.8f, 0.08f));
                }
                case "bras-longs":
                {
                    float arm = Seg(x, y, -0.92f, 0.5f, 0.32f, -0.08f, 0.12f);
                    float hand = Circle(x - 0.55f, y + 0.22f, 0.28f);
                    float sleeve = Seg(x, y, -0.92f, 0.5f, -0.6f, 0.35f, 0.2f);
                    return Min(Min(arm, hand), sleeve);
                }
                case "kangourou":
                {
                    // Deux grands bonds vers le haut, au-dessus du sol.
                    float ground = Box(x, y - 0.82f, 0.9f, 0.07f, 0.04f);
                    return Min(ground, Min(Chevron2(x, y - 0.3f), Chevron2(x, y + 0.28f)));
                }
                case "kamikaze":
                    return Sub(Star(x, y, 0.92f, 0.42f, 8), Circle(x, y, 0.18f));
                case "ange":
                {
                    float halo = Ring(x, (y + 0.74f) * 2.6f, 0.36f, 0.11f) / 2.6f;
                    float head = Circle(x, y + 0.15f, 0.22f);
                    float body = Box(x, y - 0.5f, 0.22f, 0.28f, 0.16f);
                    float wl = Sub(Circle(x + 0.52f, y - 0.1f, 0.34f), Circle(x + 0.2f, y + 0.12f, 0.26f));
                    float wr = Sub(Circle(x - 0.52f, y - 0.1f, 0.34f), Circle(x - 0.2f, y + 0.12f, 0.26f));
                    return Min(Min(halo, head), Min(body, Min(wl, wr)));
                }
                case "rage":
                {
                    // Une flamme.
                    float f = Min(Circle(x, y - 0.3f, 0.52f), Poly(x, y, -0.5f, 0.2f, 0.5f, 0.2f, 0.1f, -0.92f));
                    f = Min(f, Poly(x, y, -0.52f, 0.25f, -0.15f, 0.05f, -0.58f, -0.5f));
                    return Sub(f, Min(Circle(x, y - 0.42f, 0.2f), Poly(x, y, -0.18f, 0.38f, 0.18f, 0.38f, 0.04f, -0.02f)));
                }
                case "ninja":
                {
                    float head = Circle(x, y + 0.05f, 0.7f);
                    float band = Box(x, y - 0.08f, 0.62f, 0.17f, 0.12f);
                    float eyes = Min(Circle(x - 0.25f, y + 0.08f, 0.09f), Circle(x + 0.25f, y + 0.08f, 0.09f));
                    float tails = Min(Seg(x, y, 0.62f, -0.12f, 0.95f, -0.45f, 0.07f), Seg(x, y, 0.62f, -0.05f, 0.98f, 0.05f, 0.07f));
                    return Min(Min(Sub(head, band), eyes), tails);
                }
                // ============================================ la deuxieme fournee (07/10)
                case "lasso":
                {
                    float loop = Ring(x - 0.25f, y + 0.3f, 0.42f, 0.08f);
                    float rope = Min(Seg(x, y, 0.02f, 0.05f, -0.35f, 0.4f, 0.07f), Seg(x, y, -0.35f, 0.4f, -0.75f, 0.85f, 0.07f));
                    return Min(loop, rope);
                }
                case "missile":
                {
                    float body = Seg(x, y, -0.45f, 0.45f, 0.4f, -0.4f, 0.19f);
                    float nose = Poly(x, y, 0.26f, -0.54f, 0.54f, -0.26f, 0.82f, -0.82f);
                    float fins = Min(Poly(x, y, -0.3f, 0.3f, -0.82f, 0.32f, -0.5f, 0.55f), Poly(x, y, -0.3f, 0.3f, -0.32f, 0.82f, -0.55f, 0.5f));
                    return Min(Min(body, nose), Min(fins, Circle(x + 0.72f, y - 0.72f, 0.13f)));
                }
                case "apesanteur":
                {
                    float man = Min(Circle(x, y - 0.02f, 0.17f), Box(x, y - 0.42f, 0.18f, 0.22f, 0.12f));
                    float arrows = Min(ArrowUp(x + 0.62f, y - 0.1f, 0.32f), ArrowUp(x - 0.62f, y - 0.1f, 0.32f));
                    float moon = Sub(Circle(x, y + 0.62f, 0.3f), Circle(x - 0.16f, y + 0.72f, 0.27f));
                    return Min(Min(man, arrows), moon);
                }
                case "flammes":
                    return Min(Flame(x + 0.55f, y - 0.25f, 0.42f), Min(Flame(x, y + 0.05f, 0.58f), Flame(x - 0.55f, y - 0.25f, 0.42f)));
                case "pogo":
                {
                    float stick = Box(x, y + 0.05f, 0.07f, 0.72f, 0.03f);
                    float bar = Box(x, y + 0.72f, 0.42f, 0.07f, 0.05f);
                    float pegs = Box(x, y - 0.3f, 0.32f, 0.06f, 0.04f);
                    float spring = Seg(x, y, -0.2f, 0.45f, 0.2f, 0.55f, 0.05f);
                    spring = Min(spring, Seg(x, y, 0.2f, 0.55f, -0.2f, 0.65f, 0.05f));
                    spring = Min(spring, Seg(x, y, -0.2f, 0.65f, 0.2f, 0.75f, 0.05f));
                    float foot = Box(x, y - 0.86f, 0.14f, 0.07f, 0.05f);
                    return Min(Min(stick, bar), Min(pegs, Min(spring, foot)));
                }
                case "geyser":
                {
                    float jet = Box(x, y - 0.15f, 0.17f, 0.6f, 0.1f);
                    float top = Circle(x, y + 0.52f, 0.36f);
                    float drops = Min(Circle(x - 0.6f, y + 0.35f, 0.12f), Circle(x + 0.6f, y + 0.35f, 0.12f));
                    float ground = Box(x, y - 0.82f, 0.82f, 0.07f, 0.04f);
                    return Min(Min(jet, top), Min(drops, ground));
                }
                case "boomerang":
                    return Min(Seg(x, y, -0.72f, 0.35f, 0f, -0.42f, 0.17f), Seg(x, y, 0f, -0.42f, 0.72f, 0.35f, 0.17f));
                case "piege":
                {
                    float plate = Box(x, y - 0.6f, 0.86f, 0.09f, 0.04f);
                    float jaws = Max(Ring(x, y - 0.6f, 0.62f, 0.08f), y - 0.6f);
                    float teeth = Min(Poly(x, y, -0.5f, 0.12f, -0.3f, 0.12f, -0.4f, 0.4f), Poly(x, y, -0.1f, -0.02f, 0.1f, -0.02f, 0f, 0.3f));
                    teeth = Min(teeth, Poly(x, y, 0.3f, 0.12f, 0.5f, 0.12f, 0.4f, 0.4f));
                    return Min(Min(plate, jaws), teeth);
                }
                case "catapulte":
                {
                    float base0 = Box(x, y - 0.68f, 0.75f, 0.09f, 0.04f);
                    float wheels = Min(Circle(x + 0.5f, y - 0.78f, 0.16f), Circle(x - 0.5f, y - 0.78f, 0.16f));
                    float arm = Seg(x, y, -0.55f, 0.6f, 0.5f, -0.42f, 0.08f);
                    float stone = Circle(x - 0.66f, y + 0.6f, 0.2f);
                    return Min(Min(base0, wheels), Min(arm, stone));
                }
                case "oreiller":
                {
                    float p0 = Box(x, y, 0.72f, 0.42f, 0.3f);
                    float ears = Min(Min(Circle(x - 0.72f, y - 0.42f, 0.13f), Circle(x + 0.72f, y - 0.42f, 0.13f)), Min(Circle(x - 0.72f, y + 0.42f, 0.13f), Circle(x + 0.72f, y + 0.42f, 0.13f)));
                    return Sub(Min(p0, ears), Seg(x, y, -0.3f, 0f, 0.3f, 0f, 0.035f));
                }
                case "hypnose":
                {
                    float rings = Min(Ring(x, y, 0.2f, 0.07f), Min(Ring(x, y, 0.46f, 0.07f), Ring(x, y, 0.72f, 0.07f)));
                    return Sub(rings, Box(x + 0.45f, y, 0.45f, 0.06f, 0f));
                }
                case "raz":
                {
                    // Deux vagues en zigzag doux.
                    float w = float.MaxValue;
                    for (int k = 0; k < 2; k++)
                    {
                        float o = k == 0 ? -0.32f : 0.28f;
                        for (int i = 0; i < 5; i++)
                        {
                            float ax = -0.88f + i * 0.35f, bx = ax + 0.35f;
                            float ay = o + (i % 2 == 0 ? 0.15f : -0.15f), by = o + (i % 2 == 0 ? -0.15f : 0.15f);
                            w = Min(w, Seg(x, y, ax, ay, bx, by, 0.1f));
                        }
                    }
                    return w;
                }
                case "coup-de-pied":
                {
                    float leg = Box(x + 0.2f, y + 0.25f, 0.2f, 0.5f, 0.1f);
                    float foot = Box(x - 0.05f, y - 0.38f, 0.48f, 0.2f, 0.16f);
                    float bang = Star(x + 0.62f, y - 0.42f, 0.32f, 0.14f, 6);
                    return Min(Min(leg, foot), bang);
                }
                case "cri":
                {
                    float horn = Poly4(x, y, -0.6f, -0.13f, 0.2f, -0.5f, 0.2f, 0.5f, -0.6f, 0.13f);
                    float grip = Box(x + 0.7f, y, 0.12f, 0.18f, 0.05f);
                    float waves = Max(Min(Ring(x + 0.2f, y, 0.7f, 0.06f), Ring(x + 0.2f, y, 0.94f, 0.06f)), 0.42f - x);
                    return Min(Min(horn, grip), waves);
                }
                case "miroir":
                {
                    float frame = Ring(x, y * 0.82f + 0.08f, 0.6f, 0.09f);
                    float stand = Box(x, y - 0.86f, 0.32f, 0.07f, 0.04f);
                    float shine = Seg(x, y, -0.25f, 0.12f, 0.1f, -0.28f, 0.05f);
                    return Min(Min(frame, stand), shine);
                }
                case "increvable":
                {
                    float glass = Min(Poly(x, y, -0.48f, -0.72f, 0.48f, -0.72f, 0f, 0f), Poly(x, y, -0.48f, 0.72f, 0.48f, 0.72f, 0f, 0f));
                    float bars = Min(Box(x, y + 0.8f, 0.6f, 0.07f, 0.04f), Box(x, y - 0.8f, 0.6f, 0.07f, 0.04f));
                    return Min(glass, bars);
                }
                case "pickpocket":
                {
                    float bag = Circle(x, y - 0.22f, 0.55f);
                    float neck = Box(x, y + 0.38f, 0.2f, 0.1f, 0.04f);
                    float flaps = Min(Poly(x, y, -0.2f, -0.45f, -0.5f, -0.75f, 0f, -0.55f), Poly(x, y, 0.2f, -0.45f, 0.5f, -0.75f, 0f, -0.55f));
                    return Sub(Min(Min(bag, neck), flaps), Star(x, y - 0.22f, 0.25f, 0.1f, 5));
                }
                case "chanceux":
                {
                    float c = Min(Min(Circle(x - 0.27f, y + 0.25f, 0.29f), Circle(x + 0.27f, y + 0.25f, 0.29f)), Min(Circle(x - 0.27f, y - 0.25f, 0.29f), Circle(x + 0.27f, y - 0.25f, 0.29f)));
                    return Min(c, Seg(x, y, 0.1f, 0.3f, 0.42f, 0.88f, 0.07f));
                }
                case "armure":
                {
                    float plate = Box(x, y, 0.55f, 0.66f, 0.26f);
                    plate = Sub(plate, Circle(x, y + 0.74f, 0.3f));
                    plate = Sub(plate, Circle(x - 0.72f, y + 0.42f, 0.26f));
                    plate = Sub(plate, Circle(x + 0.72f, y + 0.42f, 0.26f));
                    return Sub(plate, Box(x, y - 0.1f, 0.03f, 0.4f, 0f));
                }
                case "sprinter":
                {
                    float shoe = Min(Box(x + 0.15f, y - 0.25f, 0.6f, 0.22f, 0.18f), Box(x - 0.15f, y + 0.12f, 0.28f, 0.22f, 0.1f));
                    float sole = Box(x + 0.15f, y - 0.52f, 0.62f, 0.06f, 0.03f);
                    float lines = Min(Seg(x, y, -0.95f, -0.25f, -0.62f, -0.25f, 0.06f), Seg(x, y, -0.92f, 0.05f, -0.55f, 0.05f, 0.06f));
                    return Min(Min(shoe, sole), lines);
                }
                // ============================================ les divines (08/10, Mode Dieu)
                case "dieu":
                {
                    float rays = Sub(Star(x, y, 0.98f, 0.62f, 12), Circle(x, y, 0.64f));
                    return Min(rays, Crown(x / 0.6f, y / 0.6f) * 0.6f);
                }
                case "apocalypse":
                {
                    float ball = Circle(x + 0.3f, y - 0.3f, 0.42f);
                    float trail = Poly(x, y, -0.62f, 0.08f, -0.08f, 0.62f, 0.9f, -0.9f);
                    float bits = Min(Circle(x - 0.58f, y + 0.6f, 0.13f), Circle(x + 0.75f, y + 0.05f, 0.1f));
                    return Min(Min(ball, trail), bits);
                }
                case "arret-temps":
                    return Min(Ring(x, y, 0.72f, 0.09f), Min(Box(x + 0.18f, y, 0.08f, 0.32f, 0.03f), Box(x - 0.18f, y, 0.08f, 0.32f, 0.03f)));
                case "rayon":
                    return Min(Poly4(x, y, -0.3f, -0.08f, 0.95f, -0.38f, 0.95f, 0.38f, -0.3f, 0.08f), Star(x + 0.55f, y, 0.4f, 0.17f, 8));
                case "teleport":
                {
                    float a1 = Ring(x + 0.55f, (y - 0.2f) * 0.55f, 0.24f, 0.05f);
                    float a2 = Ring(x - 0.55f, (y - 0.2f) * 0.55f, 0.24f, 0.05f);
                    float arc = Max(Ring(x, y + 0.05f, 0.55f, 0.06f), y + 0.05f);
                    float head = Poly(x, y, 0.42f, -0.08f, 0.68f, -0.08f, 0.55f, 0.15f);
                    return Min(Min(a1, a2), Min(arc, head));
                }
                case "tempete":
                {
                    float cloud = Cloud(x, y + 0.3f);
                    float bolt = Min(Poly(x, y, 0.12f, 0.02f, -0.28f, 0.52f, 0.05f, 0.52f), Poly(x, y, -0.05f, 0.42f, 0.25f, 0.42f, -0.15f, 0.96f));
                    return Min(cloud, bolt);
                }
                case "nuke":
                {
                    float r = (float)Math.Sqrt(x * x + y * y);
                    float a = (float)Math.Atan2(y, x);
                    float blades = float.MaxValue;
                    for (int k = 0; k < 3; k++)
                    {
                        float a0 = -1.5708f + k * 2.0944f;
                        float da = Math.Abs((float)Math.IEEERemainder(a - a0, 6.2832f)) - 0.52f;
                        blades = Min(blades, Max(Max(r - 0.88f, 0.32f - r), da * r));
                    }
                    return Min(blades, Circle(x, y, 0.18f));
                }
                case "main-dieu":
                {
                    float palm = Box(x, y - 0.25f, 0.4f, 0.38f, 0.15f);
                    float fingers = Min(Min(Box(x + 0.3f, y + 0.32f, 0.08f, 0.3f, 0.08f), Box(x + 0.1f, y + 0.4f, 0.08f, 0.36f, 0.08f)),
                                        Min(Box(x - 0.1f, y + 0.38f, 0.08f, 0.34f, 0.08f), Box(x - 0.3f, y + 0.28f, 0.08f, 0.28f, 0.08f)));
                    float thumb = Seg(x, y, 0.38f, 0.15f, 0.72f, -0.15f, 0.09f);
                    return Min(Min(palm, fingers), thumb);
                }
                case "essaim":
                {
                    float d = float.MaxValue;
                    for (int k = -1; k <= 1; k++)
                    {
                        float ox = k * 0.36f, oy = k * 0.36f;
                        d = Min(d, Seg(x, y, -0.4f + ox, 0.4f + oy, 0.2f + ox, -0.2f + oy, 0.09f));
                        d = Min(d, Poly(x, y, 0.12f + ox, -0.32f + oy, 0.32f + ox, -0.12f + oy, 0.44f + ox, -0.44f + oy));
                    }
                    return d;
                }
                case "gravite":
                {
                    // Un bonhomme la tete en bas, et des fleches qui montent.
                    float man = Min(Circle(x, y - 0.45f, 0.18f), Box(x, y + 0.05f, 0.18f, 0.26f, 0.12f));
                    return Min(man, Min(ArrowUp(x + 0.62f, y, 0.42f), ArrowUp(x - 0.62f, y, 0.42f)));
                }
                case "invincible":
                    return Sub(Star(x, y, 0.92f, 0.42f, 5), Circle(x, y + 0.04f, 0.14f));
                case "colosse":
                {
                    float head = Circle(x, y + 0.58f, 0.24f);
                    float body = Box(x, y - 0.22f, 0.62f, 0.52f, 0.25f);
                    return Min(head, Sub(body, Box(x, y - 0.6f, 0.06f, 0.2f, 0f)));
                }
                case "eclair":
                {
                    float bolt = Min(Poly(x, y, 0.45f, -0.95f, -0.08f, 0.12f, 0.32f, 0.12f), Poly(x, y, 0.15f, -0.08f, 0.55f, -0.08f, 0.03f, 0.95f));
                    float lines = Min(Seg(x, y, -0.95f, -0.3f, -0.45f, -0.3f, 0.07f), Seg(x, y, -0.92f, 0.22f, -0.38f, 0.22f, 0.07f));
                    return Min(bolt, lines);
                }
                case "main-lourde":
                {
                    float fist = Box(x + 0.1f, y - 0.1f, 0.52f, 0.46f, 0.22f);
                    fist = Sub(fist, Min(Box(x + 0.1f, y + 0.12f, 0.02f, 0.2f, 0f), Box(x - 0.15f, y + 0.12f, 0.02f, 0.2f, 0f)));
                    fist = Sub(fist, Box(x - 0.35f, y + 0.12f, 0.02f, 0.2f, 0f));
                    return Min(fist, Star(x - 0.68f, y + 0.68f, 0.3f, 0.12f, 6));
                }
                case "phenix":
                {
                    float body = Flame(x, y - 0.12f, 0.55f);
                    float wl = Sub(Circle(x + 0.52f, y + 0.12f, 0.42f), Circle(x + 0.28f, y - 0.2f, 0.4f));
                    float wr = Sub(Circle(x - 0.52f, y + 0.12f, 0.42f), Circle(x - 0.28f, y - 0.2f, 0.4f));
                    return Min(body, Min(wl, wr));
                }
                case "sablier":
                {
                    float glass = Min(Poly(x, y, -0.36f, -0.55f, 0.36f, -0.55f, 0f, 0f), Poly(x, y, -0.36f, 0.55f, 0.36f, 0.55f, 0f, 0f));
                    float bars = Min(Box(x, y + 0.62f, 0.45f, 0.06f, 0.03f), Box(x, y - 0.62f, 0.45f, 0.06f, 0.03f));
                    float arc = Max(Ring(x, y, 0.88f, 0.06f), -x + 0.2f);
                    float head = Poly(x, y, 0.62f, 0.5f, 0.9f, 0.62f, 0.62f, 0.85f);
                    return Min(Min(glass, bars), Min(arc, head));
                }
                case "internet":
                {
                    // Le globe du reseau, plus petit, et deux ondes qui partent au loin : le monde entier.
                    const float k = 0.68f;
                    float globe = Shape("en-ligne", (x + 0.22f) / k, (y - 0.22f) / k) * k;
                    float w1 = Max(Max(Ring(x + 0.22f, y - 0.22f, 0.78f, 0.065f), -(x - 0.12f)), y - 0.0f);
                    float w2 = Max(Max(Ring(x + 0.22f, y - 0.22f, 1.04f, 0.065f), -(x - 0.24f)), y + 0.1f);
                    return Min(globe, Min(w1, w2));
                }
                // ============================================ les divines, deuxieme vague (v30)
                case "bombardement":
                {
                    // Trois bombes qui tombent, chacune sa trainee.
                    float d = float.MaxValue;
                    for (int k = -1; k <= 1; k++)
                    {
                        float bx = k * 0.6f, by = 0.42f - (k == 0 ? 0.3f : 0f);
                        d = Min(d, Circle(x - bx, y - by, 0.24f));
                        d = Min(d, Seg(x, y, bx, by - 0.3f, bx, by - 0.78f, 0.06f));
                    }
                    return d;
                }
                case "singularite":
                {
                    // Un point noir, un anneau, et tout ce qui est aspire vers lui.
                    float d = Min(Circle(x, y, 0.2f), Ring(x, y, 0.46f, 0.055f));
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * 0.7854f + 0.39f;
                        float c = (float)Math.Cos(a), sn = (float)Math.Sin(a);
                        d = Min(d, Seg(x, y, c * 0.66f, sn * 0.66f, c * 0.94f, sn * 0.94f, 0.07f));
                    }
                    return d;
                }
                case "dragon":
                {
                    // Une tete cornue qui crache un cone de feu.
                    float head = Min(Circle(x + 0.55f, y + 0.02f, 0.3f), Box(x + 0.25f, y - 0.06f, 0.2f, 0.14f, 0.1f));
                    float horn = Poly(x, y, -0.78f, -0.18f, -0.52f, -0.26f, -0.92f, -0.62f);
                    head = Min(head, horn);
                    head = Sub(head, Circle(x + 0.55f, y + 0.1f, 0.07f));
                    float fire = Poly(x, y, -0.02f, 0.02f, 0.95f, -0.42f, 0.95f, 0.46f);
                    fire = Sub(fire, Min(Circle(x - 1.02f, y + 0.18f, 0.16f), Circle(x - 1.02f, y - 0.24f, 0.16f)));
                    return Min(head, Sub(fire, Box(x - 0.08f, y, 0.06f, 0.6f, 0f)));
                }
                case "comete":
                {
                    float ball = Circle(x - 0.42f, y - 0.42f, 0.34f);
                    float tail = Poly(x, y, 0.14f, 0.66f, 0.66f, 0.14f, -0.92f, -0.92f);
                    float spark = Star(x + 0.62f, y - 0.5f, 0.24f, 0.09f, 4);
                    return Min(Min(ball, tail), spark);
                }
                case "cataclysme":
                {
                    // Le sol qui se fend et des rochers qui s'envolent.
                    float ground = Box(x, y - 0.66f, 0.92f, 0.2f, 0.06f);
                    ground = Sub(ground, Poly4(x, y, -0.12f, 0.42f, 0.12f, 0.42f, 0.04f, 0.92f, -0.04f, 0.92f));
                    float rocks = Min(Box(x + 0.5f, y + 0.12f, 0.17f, 0.15f, 0.04f), Min(Box(x, y + 0.62f, 0.2f, 0.18f, 0.05f), Box(x - 0.55f, y + 0.22f, 0.15f, 0.13f, 0.04f)));
                    float lines = Min(Seg(x, y, -0.32f, 0.3f, -0.42f, 0.0f, 0.05f), Seg(x, y, 0.3f, 0.3f, 0.4f, 0.05f, 0.05f));
                    return Min(Min(ground, rocks), lines);
                }
                case "chaos":
                {
                    // Deux fleches qui se croisent : on melange tout le monde.
                    float a = Seg(x, y, -0.85f, 0.5f, 0.48f, -0.48f, 0.09f);
                    float b = Seg(x, y, -0.85f, -0.5f, 0.48f, 0.48f, 0.09f);
                    float ha = Poly(x, y, 0.3f, -0.72f, 0.92f, -0.72f, 0.66f, -0.2f);
                    float hb = Poly(x, y, 0.3f, 0.72f, 0.92f, 0.72f, 0.66f, 0.2f);
                    return Min(Min(a, b), Min(ha, hb));
                }
                case "anneau-feu":
                {
                    float ring = Ring(x, (y - 0.5f) * 2.2f, 0.78f, 0.16f) / 2.2f;
                    float fl = Min(Flame(x + 0.55f, y - 0.05f, 0.3f), Min(Flame(x, y + 0.12f, 0.42f), Flame(x - 0.55f, y - 0.05f, 0.3f)));
                    return Min(ring, fl);
                }
                case "geole":
                {
                    // Une cage dans un cercle : tout le monde autour est enferme.
                    float frame = Sub(Box(x, y, 0.42f, 0.5f, 0.1f), Box(x, y, 0.3f, 0.38f, 0.04f));
                    float bars = Box(x, y, 0.06f, 0.42f, 0f);
                    float ring = Ring(x, y, 0.84f, 0.07f);
                    return Min(Min(frame, bars), ring);
                }
                case "tsunami":
                {
                    // Une grande vague qui s'enroule, sur une mer.
                    float wave = Sub(Circle(x, y + 0.05f, 0.72f), Circle(x - 0.3f, y + 0.18f, 0.5f));
                    wave = Max(wave, y - 0.55f);
                    float sea = Box(x, y - 0.68f, 0.95f, 0.13f, 0.08f);
                    return Min(wave, sea);
                }
                case "foudre-chaine":
                {
                    // Un eclair au centre, relie a trois cibles.
                    float bolt = Min(Poly(x, y, 0.1f, -0.52f, -0.24f, 0.06f, 0.04f, 0.06f), Poly(x, y, -0.04f, -0.04f, 0.24f, -0.04f, -0.12f, 0.52f));
                    float links = Min(Seg(x, y, -0.2f, -0.2f, -0.72f, -0.62f, 0.045f), Min(Seg(x, y, 0.2f, -0.2f, 0.72f, -0.62f, 0.045f), Seg(x, y, 0.15f, 0.3f, 0.7f, 0.66f, 0.045f)));
                    float dots = Min(Circle(x + 0.74f, y + 0.66f, 0.17f), Min(Circle(x - 0.74f, y + 0.66f, 0.17f), Circle(x - 0.72f, y - 0.68f, 0.17f)));
                    return Min(Min(bolt, links), dots);
                }
                case "explosif":
                {
                    // Une deflagration : la poussee qui explose autour de la victime.
                    float burst = Sub(Star(x, y, 0.96f, 0.5f, 9), Star(x, y, 0.56f, 0.28f, 9));
                    return Min(burst, Circle(x, y, 0.16f));
                }
                case "lave":
                {
                    // Une goutte de lave qui coule.
                    float blob = Circle(x, y + 0.18f, 0.5f);
                    float drips = Min(Box(x + 0.3f, y - 0.42f, 0.1f, 0.24f, 0.1f), Min(Box(x - 0.05f, y - 0.52f, 0.1f, 0.34f, 0.1f), Box(x - 0.36f, y - 0.36f, 0.09f, 0.18f, 0.09f)));
                    float drop = Circle(x - 0.05f, y - 0.96f, 0.1f);
                    return Sub(Min(Min(blob, drips), drop), Circle(x + 0.18f, y + 0.36f, 0.1f));
                }
                case "echo":
                {
                    // Une etoile et son ombre : le sort repart une seconde fois.
                    float a = Star(x + 0.3f, y + 0.22f, 0.56f, 0.25f, 5);
                    float b = Sub(Star(x - 0.3f, y - 0.22f, 0.56f, 0.25f, 5), Star(x - 0.3f, y - 0.22f, 0.42f, 0.13f, 5));
                    return Min(a, Sub(b, Star(x + 0.3f, y + 0.22f, 0.7f, 0.36f, 5)));
                }
                // ============================================ les divines, troisieme vague (v31)
                case "armee":
                {
                    // Trois haricots aux grands yeux, celui du milieu devant, une meche allumee.
                    float d = float.MaxValue;
                    for (int k = -1; k <= 1; k++)
                    {
                        float bx = k * 0.56f, by = k == 0 ? 0.08f : 0.28f, sc = k == 0 ? 1f : 0.78f;
                        float bean = Box(x - bx, y - by, 0.22f * sc, 0.4f * sc, 0.22f * sc);
                        bean = Sub(bean, Min(Circle(x - bx + 0.08f * sc, y - by + 0.14f * sc, 0.065f * sc), Circle(x - bx - 0.08f * sc, y - by + 0.14f * sc, 0.065f * sc)));
                        d = Min(d, bean);
                    }
                    d = Min(d, Seg(x, y, 0f, -0.34f, 0.08f, -0.56f, 0.04f));
                    return Min(d, Star(x - 0.12f, y + 0.68f, 0.17f, 0.07f, 5));
                }
                case "volcan":
                {
                    float hill = Poly4(x, y, -0.92f, 0.82f, 0.92f, 0.82f, 0.26f, -0.12f, -0.26f, -0.12f);
                    hill = Sub(hill, Poly(x, y, -0.14f, -0.14f, 0.14f, -0.14f, 0f, 0.12f));
                    float lava = Min(Circle(x + 0.52f, y + 0.5f, 0.12f), Min(Circle(x, y + 0.78f, 0.15f), Circle(x - 0.5f, y + 0.46f, 0.11f)));
                    float plume = Min(Seg(x, y, -0.06f, -0.2f, -0.38f, -0.42f, 0.05f), Seg(x, y, 0.06f, -0.2f, 0.36f, -0.4f, 0.05f));
                    return Min(Min(hill, lava), plume);
                }
                case "orbitale":
                {
                    float sat = Min(Box(x, y + 0.82f, 0.13f, 0.1f, 0.03f), Min(Box(x - 0.36f, y + 0.82f, 0.18f, 0.06f, 0.01f), Box(x + 0.36f, y + 0.82f, 0.18f, 0.06f, 0.01f)));
                    float beam = Poly4(x, y, -0.07f, -0.7f, 0.07f, -0.7f, 0.2f, 0.55f, -0.2f, 0.55f);
                    float target = Ring(x, (y - 0.66f) * 2.6f, 0.62f, 0.16f) / 2.6f;
                    return Min(Min(sat, beam), target);
                }
                case "rocher":
                {
                    float rock = Circle(x - 0.15f, y, 0.62f);
                    rock = Sub(rock, Min(Circle(x + 0.05f, y + 0.25f, 0.12f), Min(Circle(x - 0.32f, y - 0.05f, 0.09f), Circle(x + 0.25f, y - 0.28f, 0.1f))));
                    float lines = Min(Seg(x, y, 0.62f, -0.3f, 0.95f, -0.3f, 0.06f), Min(Seg(x, y, 0.66f, 0.05f, 0.98f, 0.05f, 0.06f), Seg(x, y, 0.6f, 0.38f, 0.9f, 0.38f, 0.06f)));
                    float ground = Box(x, y - 0.74f, 0.95f, 0.06f, 0.03f);
                    return Min(Min(rock, lines), ground);
                }
                case "lune":
                {
                    float moon = Circle(x - 0.12f, y - 0.12f, 0.6f);
                    moon = Sub(moon, Min(Circle(x - 0.32f, y - 0.02f, 0.16f), Min(Circle(x + 0.12f, y - 0.38f, 0.11f), Circle(x - 0.02f, y + 0.22f, 0.08f))));
                    float lines = Min(Seg(x, y, 0.45f, -0.55f, 0.75f, -0.8f, 0.05f), Min(Seg(x, y, 0.6f, -0.32f, 0.88f, -0.56f, 0.05f), Seg(x, y, 0.22f, -0.7f, 0.46f, -0.9f, 0.05f)));
                    return Min(moon, lines);
                }
                case "ouragan":
                {
                    float d = float.MaxValue;
                    for (int i = 0; i < 5; i++)
                    {
                        float yy = 0.72f - i * 0.36f;
                        float half = 0.12f + i * 0.17f;
                        float off = (float)Math.Sin(i * 1.3f) * 0.1f;
                        d = Min(d, Seg(x, y, off - half, yy, off + half, yy, 0.08f));
                    }
                    return d;
                }
                case "frappe-ciel":
                {
                    float arrow = ArrowUp(x, -(y + 0.2f), 0.62f);
                    float ground = Box(x, y - 0.72f, 0.9f, 0.07f, 0.03f);
                    float burst = Min(Seg(x, y, -0.32f, 0.5f, -0.62f, 0.3f, 0.05f), Seg(x, y, 0.32f, 0.5f, 0.62f, 0.3f, 0.05f));
                    return Min(Min(arrow, ground), burst);
                }
                case "enclume":
                {
                    float top = Box(x - 0.1f, y + 0.25f, 0.55f, 0.14f, 0.03f);
                    float horn = Poly(x, y, 0.45f, -0.39f, 0.45f, -0.11f, 0.95f, -0.25f);
                    float waist = Box(x - 0.1f, y - 0.08f, 0.2f, 0.2f, 0.02f);
                    float foot = Box(x - 0.1f, y - 0.4f, 0.44f, 0.12f, 0.03f);
                    return Min(Min(top, horn), Min(waist, foot));
                }
                case "lilliput":
                {
                    // Un grand haricot en creux, un tout petit plein : on retrecit.
                    float big = Box(x + 0.48f, y, 0.32f, 0.66f, 0.32f);
                    big = Sub(big, Box(x + 0.48f, y, 0.2f, 0.54f, 0.2f));
                    float small = Box(x - 0.66f, y - 0.44f, 0.13f, 0.24f, 0.13f);
                    float arrow = Min(Seg(x, y, -0.08f, 0.02f, 0.26f, 0.28f, 0.065f), Poly(x, y, 0.14f, 0.38f, 0.38f, 0.12f, 0.44f, 0.42f));
                    return Min(Min(big, small), arrow);
                }
                // ============================================ les classiques (v36)
                case "boulet-bleu":
                {
                    // La carapace : un dome a pointes, un bord, deux petites ailes.
                    float dome = Max(Circle(x, y - 0.18f, 0.56f), y - 0.12f);
                    float rim = Box(x, y - 0.2f, 0.66f, 0.1f, 0.08f);
                    float spikes = Min(Poly(x, y, -0.34f, -0.18f, -0.14f, -0.2f, -0.26f, -0.62f), Min(Poly(x, y, -0.1f, -0.32f, 0.1f, -0.32f, 0f, -0.82f), Poly(x, y, 0.14f, -0.2f, 0.34f, -0.18f, 0.26f, -0.62f)));
                    float wing = Min(Box(x + 0.78f, y + 0.02f, 0.2f, 0.07f, 0.07f), Box(x - 0.78f, y + 0.02f, 0.2f, 0.07f, 0.07f));
                    float shadow = Box(x, y - 0.62f, 0.5f, 0.06f, 0.06f);
                    return Min(Min(Min(dome, rim), spikes), Min(wing, shadow));
                }
                case "mouton":
                {
                    float wool = Cloud(x * 1.1f - 0.12f, y + 0.08f);
                    float head = Sub(Box(x + 0.72f, y + 0.02f, 0.22f, 0.26f, 0.2f), Circle(x + 0.78f, y + 0.08f, 0.05f));
                    float legs = Min(Seg(x, y, -0.35f, 0.38f, -0.35f, 0.72f, 0.07f), Seg(x, y, 0.35f, 0.38f, 0.35f, 0.72f, 0.07f));
                    float fuse = Min(Seg(x, y, 0.2f, -0.45f, 0.42f, -0.72f, 0.04f), Star(x - 0.48f, y + 0.8f, 0.13f, 0.06f, 5));
                    return Min(Min(wool, head), Min(legs, fuse));
                }
                case "sainte-grenade":
                {
                    float body = Circle(x, y - 0.18f, 0.5f);
                    float cross = Min(Box(x, y + 0.56f, 0.07f, 0.28f, 0.02f), Box(x, y + 0.62f, 0.2f, 0.07f, 0.02f));
                    float neck = Box(x, y + 0.34f, 0.14f, 0.08f, 0.02f);
                    float rays = Min(Seg(x, y, 0.62f, -0.6f, 0.86f, -0.82f, 0.04f), Seg(x, y, -0.62f, -0.6f, -0.86f, -0.82f, 0.04f));
                    return Min(Min(Sub(body, Box(x, y - 0.18f, 0.5f, 0.03f, 0f)), Min(cross, neck)), rays);
                }
                case "poing-faucon":
                {
                    float fist = Box(x - 0.08f, y, 0.42f, 0.34f, 0.18f);
                    float thumb = Box(x - 0.08f, y - 0.36f, 0.24f, 0.08f, 0.08f);
                    float gaps = Min(Box(x + 0.34f, y - 0.11f, 0.08f, 0.015f, 0f), Box(x + 0.34f, y + 0.11f, 0.08f, 0.015f, 0f));
                    float fire = Min(Flame(-(y) * 1f, (x + 0.72f) * 1f, 0.42f), Flame(-(y + 0.38f), x + 0.62f, 0.26f));
                    return Min(Sub(Min(fist, thumb), gaps), fire);
                }
                case "gobe-tout":
                {
                    // Une grosse boule qui ouvre grand la bouche vers la gauche, et une etoile aspiree.
                    float ball = Circle(x - 0.22f, y, 0.62f);
                    float mouth = Poly(x, y, 0.22f, 0.02f, -0.6f, -0.56f, -0.6f, 0.6f);
                    float eye = Circle(x - 0.38f, y + 0.3f, 0.09f);
                    float star = Star(x + 0.66f, y, 0.2f, 0.09f, 5);
                    float lines = Min(Seg(x, y, -0.98f, -0.34f, -0.72f, -0.16f, 0.045f), Seg(x, y, -0.98f, 0.34f, -0.72f, 0.16f, 0.045f));
                    return Min(Sub(Sub(ball, mouth), eye), Min(star, lines));
                }
                case "roue-folle":
                {
                    float tyre = Ring(x + 0.1f, y, 0.52f, 0.13f);
                    float hub = Circle(x + 0.1f, y, 0.16f);
                    float spokes = Min(Seg(x, y, 0.1f, -0.42f, 0.1f, 0.42f, 0.05f), Seg(x, y, -0.32f, 0f, 0.52f, 0f, 0.05f));
                    float lines = Min(Seg(x, y, -0.95f, -0.3f, -0.58f, -0.3f, 0.05f), Min(Seg(x, y, -0.98f, 0f, -0.62f, 0f, 0.05f), Seg(x, y, -0.95f, 0.3f, -0.58f, 0.3f, 0.05f)));
                    float flame = Flame(x - 0.1f, y + 0.66f, 0.28f);
                    return Min(Min(Min(tyre, hub), spokes), Min(lines, flame));
                }
                case "charge":
                {
                    float shield = Poly4(x, y, -0.05f, -0.7f, 0.65f, -0.5f, 0.6f, 0.25f, -0.05f, 0.25f);
                    shield = Min(shield, Poly(x, y, -0.05f, 0.2f, 0.6f, 0.2f, 0.28f, 0.78f));
                    shield = Sub(shield, Min(Box(x - 0.28f + 0.55f, y - 0.05f, 0.05f, 0.42f, 0f), Box(x + 0.28f, y - 0.12f, 0.24f, 0.05f, 0f)));
                    float lines = Min(Seg(x, y, -0.95f, -0.35f, -0.3f, -0.35f, 0.06f), Min(Seg(x, y, -0.95f, 0f, -0.25f, 0f, 0.06f), Seg(x, y, -0.95f, 0.35f, -0.3f, 0.35f, 0.06f)));
                    return Min(shield, lines);
                }
                case "tnt":
                {
                    float crate = Box(x, y + 0.12f, 0.62f, 0.56f, 0.06f);
                    float band = Box(x, y + 0.12f, 0.62f, 0.16f, 0f);
                    float body = Sub(crate, Sub(band, Box(x, y + 0.12f, 0.54f, 0.1f, 0f)));
                    float fuse = Seg(x, y, 0f, -0.44f, 0.22f, -0.72f, 0.05f);
                    float spark = Star(x - 0.3f, y + 0.8f, 0.2f, 0.08f, 6);
                    return Min(Min(body, fuse), spark);
                }
                case "buche":
                {
                    float log = Box(x + 0.15f, y, 0.72f, 0.36f, 0.34f);
                    float end = Ring(x - 0.56f + 0.0f, y, 0.22f, 0.05f);
                    log = Sub(log, Circle(x - 0.56f, y, 0.3f));
                    float rings = Min(Circle(x - 0.56f, y, 0.06f), end);
                    float lines = Min(Seg(x, y, 0.95f, -0.25f, 0.95f, 0.25f, 0.05f), Seg(x, y, -0.2f, 0.52f, 0.6f, 0.52f, 0.05f));
                    return Min(Min(log, rings), lines);
                }
                case "tonneau":
                {
                    float barrel = Box(x - 0.18f, y, 0.42f, 0.66f, 0.3f);
                    float hoops = Min(Box(x - 0.18f, y - 0.36f, 0.46f, 0.04f, 0f), Box(x - 0.18f, y + 0.36f, 0.46f, 0.04f, 0f));
                    barrel = Sub(barrel, hoops);
                    float bean = Box(x + 0.62f, y + 0.2f, 0.17f, 0.3f, 0.17f);
                    bean = Sub(bean, Min(Circle(x + 0.56f, y + 0.08f, 0.05f), Circle(x + 0.68f, y + 0.08f, 0.05f)));
                    return Min(barrel, bean);
                }
                case "disco":
                {
                    float ball = Circle(x, y + 0.1f, 0.56f);
                    float grid = Min(Box(x, y + 0.1f, 0.6f, 0.025f, 0f), Min(Box(x, y - 0.12f, 0.6f, 0.025f, 0f), Box(x, y + 0.32f, 0.6f, 0.025f, 0f)));
                    grid = Min(grid, Min(Box(x - 0.2f, y + 0.1f, 0.025f, 0.6f, 0f), Box(x + 0.2f, y + 0.1f, 0.025f, 0.6f, 0f)));
                    float string_ = Seg(x, y, 0f, -0.46f, 0f, -0.9f, 0.04f);
                    float sparkle = Min(Star(x - 0.72f, y + 0.62f, 0.16f, 0.05f, 4), Star(x + 0.74f, y - 0.5f, 0.16f, 0.05f, 4));
                    return Min(Min(Sub(ball, grid), string_), sparkle);
                }
                case "force-imparable":
                {
                    float arrow = ArrowUp(x, -(y - 0.05f), 0.6f);
                    float ground = Box(x, y - 0.74f, 0.92f, 0.07f, 0.03f);
                    float burst = Min(Seg(x, y, -0.38f, 0.58f, -0.82f, 0.32f, 0.06f), Seg(x, y, 0.38f, 0.58f, 0.82f, 0.32f, 0.06f));
                    float rocks = Min(Circle(x - 0.55f, y - 0.1f, 0.1f), Circle(x + 0.6f, y - 0.05f, 0.08f));
                    return Min(Min(arrow, ground), Min(burst, rocks));
                }
                case "saut-mario":
                {
                    // Un haricot tout ecrase, et celui qui lui retombe dessus.
                    float squashed = Box(x, y - 0.66f, 0.6f, 0.16f, 0.16f);
                    float jumper = Box(x, y + 0.2f, 0.22f, 0.34f, 0.22f);
                    jumper = Sub(jumper, Min(Circle(x - 0.08f, y + 0.08f, 0.05f), Circle(x + 0.08f, y + 0.08f, 0.05f)));
                    float stars = Min(Star(x - 0.74f, y - 0.4f, 0.14f, 0.06f, 5), Star(x + 0.74f, y - 0.4f, 0.14f, 0.06f, 5));
                    float lines = Min(Seg(x, y, -0.42f, -0.85f, -0.42f, -0.55f, 0.04f), Seg(x, y, 0.42f, -0.85f, 0.42f, -0.55f, 0.04f));
                    return Min(Min(squashed, jumper), Min(stars, lines));
                }
                case "home-run":
                {
                    float ball = Circle(x + 0.22f, y + 0.1f, 0.42f);
                    float seams = Min(Max(Ring(x + 0.62f, y + 0.1f, 0.3f, 0.04f), x + 0.08f - 0.22f), Max(Ring(x - 0.18f, y + 0.1f, 0.3f, 0.04f), -(x - 0.36f)));
                    ball = Sub(ball, seams);
                    float lines = Min(Seg(x, y, -0.95f, 0.45f, -0.35f, 0.25f, 0.05f), Min(Seg(x, y, -0.95f, 0.1f, -0.32f, 0.08f, 0.05f), Seg(x, y, -0.9f, -0.25f, -0.38f, -0.12f, 0.05f)));
                    float spark = Star(x - 0.62f, y + 0.72f, 0.18f, 0.07f, 5);
                    return Min(Min(ball, lines), spark);
                }
                case "demence":
                {
                    // Une spirale : la tete qui tourne.
                    float d = float.MaxValue;
                    float[] up = { 0.15f, 0.45f, 0.75f };
                    float[] low = { 0.3f, 0.6f };
                    for (int i = 0; i < up.Length; i++) d = Min(d, Max(Ring(x, y, up[i], 0.06f), y));
                    for (int i = 0; i < low.Length; i++) d = Min(d, Max(Ring(x + 0.15f, y, low[i], 0.06f), -y));
                    return d;
                }
                case "orage":
                {
                    float cloud = Cloud(x, y + 0.35f);
                    float bolt = Min(Poly(x, y, 0.12f, 0.02f, -0.22f, 0.46f, 0.04f, 0.46f), Poly(x, y, -0.04f, 0.38f, 0.2f, 0.38f, -0.1f, 0.92f));
                    float rain = Min(Seg(x, y, -0.5f, 0.15f, -0.6f, 0.45f, 0.05f), Seg(x, y, 0.5f, 0.15f, 0.4f, 0.45f, 0.05f));
                    return Min(cloud, Min(bolt, rain));
                }
                case "orbes":
                {
                    float d = Min(Circle(x, y, 0.2f), Ring(x, y, 0.62f, 0.035f));
                    for (int k = 0; k < 3; k++)
                    {
                        float a = -1.5708f + k * 2.0944f;
                        d = Min(d, Circle(x - (float)Math.Cos(a) * 0.62f, y - (float)Math.Sin(a) * 0.62f, 0.2f));
                    }
                    return d;
                }
                case "titan":
                {
                    float foot = Box(x + 0.05f, y + 0.05f, 0.3f, 0.42f, 0.2f);
                    float toes = Box(x + 0.22f, y - 0.28f, 0.45f, 0.14f, 0.12f);
                    float ground = Box(x, y - 0.64f, 0.95f, 0.06f, 0.03f);
                    float cracks = Min(Seg(x, y, -0.55f, 0.68f, -0.75f, 0.92f, 0.04f), Seg(x, y, 0.6f, 0.68f, 0.82f, 0.9f, 0.04f));
                    float shock = Min(Seg(x, y, -0.5f, 0.45f, -0.85f, 0.35f, 0.05f), Seg(x, y, 0.75f, 0.45f, 0.95f, 0.3f, 0.05f));
                    return Min(Min(Min(foot, toes), ground), Min(cracks, shock));
                }
                default: return Circle(x, y, 0.6f);
            }
        }

        /// <summary>Une flamme (la pointe en haut), de taille "s".</summary>
        static float Flame(float x, float y, float s)
        {
            x /= s; y /= s;
            float f = Min(Circle(x, y - 0.3f, 0.52f), Poly(x, y, -0.5f, 0.2f, 0.5f, 0.2f, 0.1f, -0.92f));
            return f * s;
        }

        // ================================================================== les motifs

        static readonly float[] CrownBody = { -0.72f, 0.32f, -0.72f, -0.42f, -0.36f, -0.02f, 0f, -0.6f, 0.36f, -0.02f, 0.72f, -0.42f, 0.72f, 0.32f };

        static float Crown(float x, float y)
        {
            // Trois pointes perlees au-dessus d'un bandeau serti de trois joyaux.
            float body = PolyN(x, y, CrownBody);
            float band = Box(x, y - 0.44f, 0.74f, 0.17f, 0.06f);
            float balls = Min(Circle(x + 0.72f, y + 0.52f, 0.13f), Min(Circle(x, y + 0.7f, 0.15f), Circle(x - 0.72f, y + 0.52f, 0.13f)));
            float gems = Min(Circle(x + 0.38f, y - 0.44f, 0.075f), Min(Circle(x, y - 0.44f, 0.09f), Circle(x - 0.38f, y - 0.44f, 0.075f)));
            float line = Box(x, y - 0.25f, 0.74f, 0.025f, 0f);
            return Min(Sub(Min(body, band), Min(gems, line)), balls);
        }

        static float Tower(float x, float y)
        {
            float shaft = Box(x, y + 0.2f, 0.34f, 0.62f, 0.03f);
            float top = Box(x, y - 0.5f, 0.5f, 0.16f, 0.03f);
            float merlons = Min(Box(x - 0.36f, y - 0.72f, 0.12f, 0.1f, 0.02f), Min(Box(x, y - 0.72f, 0.12f, 0.1f, 0.02f), Box(x + 0.36f, y - 0.72f, 0.12f, 0.1f, 0.02f)));
            float spiral = Min(Seg(x, y, -0.36f, 0.62f, 0.36f, 0.36f, 0.045f), Min(Seg(x, y, -0.36f, 0.22f, 0.36f, -0.04f, 0.045f), Seg(x, y, -0.36f, -0.18f, 0.36f, -0.42f, 0.045f)));
            float door = Min(Circle(x, y + 0.62f, 0.12f), Box(x, y + 0.72f, 0.12f, 0.1f, 0f));
            return Sub(Min(Min(shaft, top), merlons), Min(spiral, door));
        }

        static float Wings(float x, float y)
        {
            // Deux ailes deployees : quatre plumes en eventail de chaque cote.
            float ax = Math.Abs(x);
            float d = Seg(ax, y, 0.1f, 0.12f, 0.84f, -0.55f, 0.13f);
            d = Min(d, Seg(ax, y, 0.1f, 0.16f, 0.82f, -0.2f, 0.12f));
            d = Min(d, Seg(ax, y, 0.1f, 0.2f, 0.78f, 0.12f, 0.11f));
            d = Min(d, Seg(ax, y, 0.1f, 0.24f, 0.58f, 0.38f, 0.1f));
            d = Min(d, Circle(ax - 0.08f, y - 0.18f, 0.16f));
            return d;
        }

        static float Mouse(float x, float y, int button)
        {
            float body = Box(x, y, 0.52f, 0.78f, 0.5f);
            float inner = Box(x, y, 0.4f, 0.66f, 0.4f);
            float shell = Sub(body, inner);
            float split = Min(Box(x, y + 0.36f, 0.03f, 0.36f, 0f), Box(x, y - 0.02f, 0.52f, 0.03f, 0f));
            float lit;
            if (button == 0) lit = Max(inner, Max(x + 0.02f, y + 0.02f));
            else if (button == 1) lit = Max(inner, Max(-x + 0.02f, y + 0.02f));
            else lit = Box(x, y + 0.34f, 0.08f, 0.18f, 0.08f);
            return Min(Sub(shell, split), lit + 0.02f) ;
        }

        static float ArrowUp(float x, float y, float s)
        {
            x /= s; y /= s;
            float shaft = Seg(x, y, 0f, 0.85f, 0f, -0.1f, 0.14f);
            float head = Poly(x, y, -0.5f, 0.02f, 0.5f, 0.02f, 0f, -0.75f);
            return Min(shaft, head) * s;
        }

        static float Chevron(float x, float y, float s)
        {
            return Min(Seg(x, y, 0f, -s * 0.7f, s * 0.6f, 0f, 0.13f), Seg(x, y, 0f, s * 0.7f, s * 0.6f, 0f, 0.13f));
        }

        static float Chevron2(float x, float y)
        {
            return Min(Seg(x, y, -0.6f, 0.25f, 0f, -0.3f, 0.13f), Seg(x, y, 0.6f, 0.25f, 0f, -0.3f, 0.13f));
        }

        static float Cloud(float x, float y)
        {
            float d = Min(Circle(x + 0.32f, y + 0.02f, 0.4f), Circle(x - 0.08f, y - 0.2f, 0.45f));
            d = Min(d, Circle(x - 0.5f, y + 0.12f, 0.3f));
            d = Min(d, Box(x, y + 0.25f, 0.72f, 0.18f, 0.16f));
            return d;
        }

        static float Gear(float x, float y)
        {
            float r = (float)Math.Sqrt(x * x + y * y);
            float a = (float)Math.Atan2(y, x);
            float teeth = (float)Math.Cos(a * 8f);
            float outer = r - (0.62f + (teeth > 0.3f ? 0.2f : 0f));
            outer = Max(outer, r - 0.82f);
            float body = Min(outer, r - 0.6f);
            return Max(body, 0.26f - r);
        }

        static float Star(float x, float y, float r1, float r2, int n)
        {
            for (int i = 0; i < n * 2; i++)
            {
                float a = (float)Math.PI * 0.5f + i * (float)Math.PI / n;
                float r = i % 2 == 0 ? r1 : r2;
                star[i * 2] = (float)Math.Cos(a) * r;
                star[i * 2 + 1] = -(float)Math.Sin(a) * r;
            }
            return PolyN(x, y, star, n * 2);
        }

        // ================================================================== les briques

        static float Circle(float x, float y, float r) { return (float)Math.Sqrt(x * x + y * y) - r; }
        static float Ring(float x, float y, float r, float t) { return Math.Abs(Circle(x, y, r)) - t; }

        /// <summary>Une boite (demi-tailles hx, hy) aux coins arrondis de rayon "round".</summary>
        static float Box(float x, float y, float hx, float hy, float round)
        {
            float qx = Math.Abs(x) - hx + round, qy = Math.Abs(y) - hy + round;
            float ox = Math.Max(qx, 0f), oy = Math.Max(qy, 0f);
            return (float)Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0f) - round;
        }

        /// <summary>Un trait epais (une gelule) de a a b.</summary>
        static float Seg(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay;
            float h = Clamp01((px * dx + py * dy) / Math.Max(1e-6f, dx * dx + dy * dy));
            float ex = px - dx * h, ey = py - dy * h;
            return (float)Math.Sqrt(ex * ex + ey * ey) - r;
        }

        // Des tableaux de travail reutilises : une icone evalue ces formes 16 000 fois, on
        // n'alloue rien a chaque pixel (le ramasse-miettes d'Unity ferait des a-coups).
        static readonly float[] tri = new float[6];
        static readonly float[] quad = new float[8];
        static readonly float[] star = new float[64];

        static float Poly(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
        {
            tri[0] = ax; tri[1] = ay; tri[2] = bx; tri[3] = by; tri[4] = cx; tri[5] = cy;
            return PolyN(x, y, tri, 3);
        }

        static float Poly4(float x, float y, float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy)
        {
            quad[0] = ax; quad[1] = ay; quad[2] = bx; quad[3] = by; quad[4] = cx; quad[5] = cy; quad[6] = dx; quad[7] = dy;
            return PolyN(x, y, quad, 4);
        }

        /// <summary>Un polygone quelconque (sommets x0,y0,x1,y1...).</summary>
        static float PolyN(float x, float y, float[] v)
        {
            return PolyN(x, y, v, v.Length / 2);
        }

        static float PolyN(float x, float y, float[] v, int n)
        {
            float d = (x - v[0]) * (x - v[0]) + (y - v[1]) * (y - v[1]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                float ex = v[j * 2] - v[i * 2], ey = v[j * 2 + 1] - v[i * 2 + 1];
                float wx = x - v[i * 2], wy = y - v[i * 2 + 1];
                float h = Clamp01((wx * ex + wy * ey) / Math.Max(1e-6f, ex * ex + ey * ey));
                float bx = wx - ex * h, by = wy - ey * h;
                d = Math.Min(d, bx * bx + by * by);
                bool c1 = y >= v[i * 2 + 1], c2 = y < v[j * 2 + 1], c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * (float)Math.Sqrt(d);
        }

        static float Min(float a, float b) { return a < b ? a : b; }
        static float Max(float a, float b) { return a > b ? a : b; }
        static float Sub(float a, float b) { return Max(a, -b); }
        static float Clamp01(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
    }
}
