#if UNITY_EDITOR
using UnityEditor;

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
        void OnPreprocessModel()
        {
            string p = assetPath.Replace('\', '/');
            if (!p.Contains("Resources/Modeles/Monstres/") && !p.Contains("Resources/Modeles/Gens/")) return;
            ModelImporter mi = assetImporter as ModelImporter;
            if (mi == null) return;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true;
            mi.generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
            mi.importCameras = false;
            mi.importLights = false;
        }
    }
}
#endif
