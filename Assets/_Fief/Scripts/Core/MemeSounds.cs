using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Fief
{
    /// <summary>
    /// LES VRAIS SONS DES MEMES (v46.2 -- Martin : "il n'y a pas le son des vrais memes qui gueulent ;
    /// quand quelqu'un fait un 6 7, ca doit faire le vrai meme qui gueule, hyper fort ; tous les memes
    /// doivent etre des vrais memes d'internet").
    ///
    /// Les vrais enregistrements (le "six seven", le "fahhh", le vine boom, le metal pipe, le
    /// rickroll...) appartiennent a leurs auteurs : ils ne vont PAS dans le jeu (Steam pourrait
    /// retirer la page), ni dans le depot Git. Ils vivent dans le dossier MemesSons/, A COTE du jeu :
    ///   - dans l'editeur : a la racine du projet (federaris/MemesSons/), hors d'Assets ;
    ///   - dans le jeu exporte : a cote du .exe (le Build l'y recopie, Editor/SteamAppId.cs).
    /// Un sous-dossier par meme (MemesSons/SixSept, MemesSons/Fahhh...), un ou plusieurs .mp3, .ogg ou
    /// .wav dedans. Le script Tools/memes-sons.py les telecharge tous d'un coup sur TON PC
    /// (double-clic sur Tools/memes-sons.bat). Le jeu les charge au lancement ; un dossier vide :
    /// le son fabrique par le code (Sfx.Meme).
    ///
    /// La liste ci-dessous est LA reference : le script Python la lit dans ce fichier.
    /// </summary>
    public static class MemeSounds
    {
        public sealed class Entry
        {
            public readonly string Key;        // le sous-dossier de MemesSons
            public readonly string Search;     // ce que le script cherche sur la banque de sons
            public readonly string Fallback;   // le son du code s'il n'y a pas de fichier
            public Entry(string key, string search, string fallback) { Key = key; Search = search; Fallback = fallback; }
        }

        /// <summary>LES MEMES QUI GUEULENT : 44 sons.</summary>
        public static readonly Entry[] All =
        {
            new Entry("SixSept", "67 six seven", "SixSept"),
            new Entry("Fahhh", "fahhh", "Fahhh"),
            new Entry("VineBoom", "vine boom", "Boum"),
            new Entry("Bruh", "bruh", "Bruh"),
            new Entry("MetalPipe", "metal pipe falling", "Bonk"),
            new Entry("TacoBell", "taco bell bong", "Boum"),
            new Entry("Airhorn", "mlg air horn", "Klaxon"),
            new Entry("WindowsError", "windows xp error", "Faux"),
            new Entry("RobertWeide", "directed by robert b weide", "Trombone"),
            new Entry("SadViolin", "sad violin", "Trombone"),
            new Entry("Rickroll", "rick roll", "Fanfare"),
            new Entry("JohnCena", "john cena", "Fanfare"),
            new Entry("AmongUs", "among us role reveal", "Suspense"),
            new Entry("Sheesh", "sheesh", "Sheesh"),
            new Entry("Wow", "owen wilson wow", "Wow"),
            new Entry("Bonk", "bonk", "Bonk"),
            new Entry("Oof", "roblox oof", "Oof"),
            new Entry("Nooo", "noooo", "Nooo"),
            new Entry("Oiia", "oiia oiia cat", "Couic"),
            new Entry("ChipiChapa", "chipi chipi chapa chapa", "Fanfare"),
            new Entry("CoffinDance", "coffin dance", "Fanfare"),
            new Entry("TungTung", "tung tung tung sahur", "Bonk"),
            new Entry("Bombardiro", "bombardiro crocodilo", "Klaxon"),
            new Entry("Tralalero", "tralalero tralala", "Sifflet"),
            new Entry("Patapim", "brr brr patapim", "Bruh"),
            new Entry("EmotionalDamage", "emotional damage", "Boum"),
            new Entry("Huh", "huh cat", "Faux"),
            new Entry("Skibidi", "skibidi toilet", "SixSept"),
            new Entry("Sparta", "this is sparta", "Boum"),
            new Entry("Leeroy", "leeroy jenkins", "Fahhh"),
            new Entry("AwMan", "creeper aw man", "Suspense"),
            new Entry("Yeet", "yeet", "Wow"),
            new Entry("MissionFailed", "mission failed we'll get em next time", "Trombone"),
            new Entry("Gigachad", "gigachad", "Sheesh"),
            new Entry("Sigma", "sigma boy", "Sheesh"),
            new Entry("Rizz", "rizz sound effect", "Wow"),
            new Entry("FanumTax", "fanum tax", "Caisse"),
            new Entry("Ohio", "ohio meme", "Suspense"),
            new Entry("ShootingStars", "shooting stars meme", "Wow"),
            new Entry("Stonks", "stonks", "Wow"),
            new Entry("ThisIsFine", "this is fine", "Suspense"),
            new Entry("DunDun", "dun dun dunnn", "Suspense"),
            new Entry("Lag", "lag", "Scratch"),
            new Entry("Boing", "boing", "Couic"),
        };

        /// <summary>Le dossier des vrais sons : a cote du jeu (a la racine du projet dans l'editeur).</summary>
        public static string Folder { get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), "MemesSons"); } }

        static readonly Dictionary<string, List<AudioClip>> loaded = new Dictionary<string, List<AudioClip>>(System.StringComparer.OrdinalIgnoreCase);
        static readonly List<KeyValuePair<string, UnityWebRequest>> pending = new List<KeyValuePair<string, UnityWebRequest>>();
        static readonly System.Random rng = new System.Random();
        static bool scanned;

        /// <summary>Combien de vrais sons sont charges (la Console le dit).</summary>
        public static int Loaded { get { int n = 0; foreach (var kv in loaded) n += kv.Value.Count; return n; } }

        /// <summary>
        /// Lire le dossier (une fois) : chaque fichier part en chargement (UnityWebRequest lit aussi
        /// les fichiers du disque, "file://"). Concept Unity : le chargement ne bloque pas le jeu,
        /// on regarde a chaque image s'il est fini (Poll).
        /// </summary>
        public static void Scan()
        {
            if (scanned) return;
            scanned = true;
            try
            {
                string root = Folder;
                // Les sous-dossiers vides sont crees : Martin voit tout de suite ou poser quoi.
                if (!Directory.Exists(root)) Directory.CreateDirectory(root);
                for (int i = 0; i < All.Length; i++)
                {
                    string d = Path.Combine(root, All[i].Key);
                    if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                }
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string key = Path.GetFileName(dir);
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        AudioType type = TypeOf(file);
                        if (type == AudioType.UNKNOWN) continue;
                        UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(file).AbsoluteUri, type);
                        req.SendWebRequest();
                        pending.Add(new KeyValuePair<string, UnityWebRequest>(key, req));
                    }
                }
                Debug.Log("[FIEF] Mode mème : " + pending.Count + " vrais sons trouvés dans " + root);
            }
            catch (System.Exception e) { Debug.LogWarning("[FIEF] Mode mème : dossier MemesSons illisible (" + e.Message + ")"); }
        }

        static AudioType TypeOf(string file)
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".mp3") return AudioType.MPEG;
            if (ext == ".ogg") return AudioType.OGGVORBIS;
            if (ext == ".wav") return AudioType.WAV;
            return AudioType.UNKNOWN;
        }

        /// <summary>Ranger les sons dont le chargement est fini.</summary>
        public static void Poll()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                UnityWebRequest req = pending[i].Value;
                if (!req.isDone) continue;
                string key = pending[i].Key;
                pending.RemoveAt(i);
                try
                {
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                        if (clip != null)
                        {
                            List<AudioClip> list;
                            if (!loaded.TryGetValue(key, out list)) { list = new List<AudioClip>(); loaded[key] = list; }
                            list.Add(clip);
                        }
                    }
                    else Debug.LogWarning("[FIEF] Mode mème : son illisible (" + key + ") : " + req.error);
                }
                catch (System.Exception e) { Debug.LogWarning("[FIEF] Mode mème : " + key + " : " + e.Message); }
                finally { req.Dispose(); }
            }
        }

        /// <summary>Un vrai son du meme "key", au hasard ; null s'il n'y en a pas (encore).</summary>
        public static AudioClip Real(string key)
        {
            Scan();
            Poll();
            List<AudioClip> list;
            if (!loaded.TryGetValue(key, out list) || list.Count == 0) return null;
            return list[rng.Next(list.Count)];
        }

        /// <summary>Le son du code qui remplace le meme "key" quand son dossier est vide.</summary>
        public static string FallbackOf(string key)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Key == key) return All[i].Fallback;
            return key;
        }
    }
}
