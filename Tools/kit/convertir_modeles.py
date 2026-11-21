# Convertit des modeles glTF (.glb, ex. Quaternius CC0 telecharges sur poly.pizza) en FBX pour Unity, animations comprises.
#   blender -b --factory-startup --python Tools/kit/convertir_modeles.py -- <dossier des .glb> <dossier de sortie>
# (21/11) Blender ajoute a l'import une boule "Icosphere" qui sert a dessiner les os : on la supprime avant d'exporter.
import bpy, sys, os
args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
os.makedirs(out, exist_ok=True)
for f in sorted(os.listdir(src)):
    if not f.endswith(".glb"):
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(src, f))
    for o in list(bpy.data.objects):
        if o.name.startswith("Icosphere"):
            bpy.data.objects.remove(o, do_unlink=True)
    name = f[:-4]
    print("RESULT", name, "anims", [a.name for a in bpy.data.actions])
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), axis_forward="-Z", axis_up="Y",
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             add_leaf_bones=False, path_mode="COPY", embed_textures=True)
