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
            "vue", "texte", "ecran", "retour", "drapeau", "cloche", "touche", "bouclier",
            "ruee", "grappin", "crochet", "onde", "clignement", "bond", "mur", "nuee", "mine", "gel", "voile",
            "echange", "rappel", "souffle", "double-saut", "planeur", "coureur", "porteur", "poigne", "ancrage",
            "flair", "ombre", "prise-ferme", "recharge", "rebond", "aimant",
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
                default: return Circle(x, y, 0.6f);
            }
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
