#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Fief
{
    /// <summary>
    /// (21/11) LES MODELES ANIMES de La Nuit : les monstres (Resources/Modeles/Monstres) et les vivants (Resources/Modeles/Gens),
    /// pris dans les packs gratuits (CC0) de Quaternius, convertis en FBX par Blender.
    ///
    /// Concept Unity : un AssetPostprocessor est appele par Unity a chaque fois qu'il importe un fichier. Ici, on lui dit
    /// d'importer les animations de ces modeles en "Legacy" : le composant Animation les joue par leur nom
    /// (anim.Play("Walk")), sans Animator Controller a dessiner a la main. C'est le plus simple pour un debutant.
    /// </summary>
    public class ModelesAnimes : AssetPostprocessor
    {
        /// <summary>Changer ce numero force Unity a re-importer ces modeles avec les nouveaux reglages.</summary>
        public override uint GetVersion() { return 2; }

        /// <summary>(21/11) Les longues pistes de la nuit (musique, aube, fond) se lisent en continu depuis le disque.</summary>
        void OnPreprocessAudio()
        {
            string p = assetPath.Replace("\\", "/");
            if (!p.Contains("Resources/Sons/Nuit/Musique/") && !p.Contains("Resources/Sons/Nuit/Aube/") && !p.Contains("Resources/Sons/Nuit/Fond/")) return;
            AudioImporter ai = assetImporter as AudioImporter;
            if (ai == null) return;
            AudioImporterSampleSettings set = ai.defaultSampleSettings;
            set.loadType = AudioClipLoadType.Streaming;
            ai.defaultSampleSettings = set;
        }

        void OnPreprocessModel()
        {
            string p = assetPath.Replace("\\", "/");
            if (!p.Contains("Resources/Modeles/Monstres/") && !p.Contains("Resources/Modeles/Gens/")) return;
            ModelImporter mi = assetImporter as ModelImporter;
            if (mi == null) return;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true;
            mi.generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
            // (21/11) Lisible par le code : NightBeasts mesure la vraie forme du modele (BakeMesh) pour le mettre a la bonne taille.
            mi.isReadable = true;
            mi.importCameras = false;
            mi.importLights = false;
        }
    }
}
#endif
