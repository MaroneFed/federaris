# -*- coding: utf-8 -*-
"""Tient les fichiers .meta d'Assets/ a jour.

Unity attache a chaque fichier et a chaque dossier d'Assets/ un fichier .meta qui
porte son identite (GUID). Le projet les versionne : sans eux, toute reference a un
asset se casse (voir CLAUDE.md). Quand un script est ecrit hors d'Unity -- ce que
fait Claude --, personne ne cree son .meta ; et quand un fichier est supprime, son
.meta reste orphelin.

    python3 Tools/meta.py          # cree les manquants, supprime les orphelins

Les GUID sont DETERMINISTES (derives du chemin) : relancer le script ne change rien,
et deux machines produisent le meme .meta.
"""
import hashlib, os, sys

ROOT = "Assets"

SCRIPT = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

OTHER = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


# Une police (.ttf, .otf) : "Dynamic", Unity dessine les lettres a la demande,
# dans toutes les tailles, accents compris.
FONT = """fileFormatVersion: 2
guid: {guid}
TrueTypeFontImporter:
  externalObjects: {{}}
  serializedVersion: 4
  fontSize: 16
  forceTextureCase: -2
  characterSpacing: 0
  characterPadding: 1
  includeFontData: 1
  fontNames:
  - {family}
  fallbackFontReferences: []
  customCharacters: 
  fontRenderingMode: 0
  ascentCalculationMode: 1
  useLegacyBoundsCalculation: 0
  shouldRoundAdvanceValue: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

TEXT = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def guid_for(path):
    return hashlib.md5(("fief:" + path.replace(os.sep, "/")).encode("utf-8")).hexdigest()


def main():
    created, removed = [], []

    # 1. les orphelins : un .meta dont le fichier ou le dossier n'existe plus
    for dp, dn, fn in os.walk(ROOT):
        for f in fn:
            if not f.endswith(".meta"):
                continue
            target = os.path.join(dp, f[:-5])
            if not os.path.exists(target):
                os.remove(os.path.join(dp, f))
                removed.append(target)

    # 2. les dossiers devenus vides (Unity les recreerait avec un .meta neuf)
    for dp, dn, fn in sorted(os.walk(ROOT), key=lambda t: -t[0].count(os.sep)):
        if dp != ROOT and not os.listdir(dp):
            os.rmdir(dp)
            meta = dp + ".meta"
            if os.path.exists(meta):
                os.remove(meta)
            removed.append(dp + "/")

    # 3. les manquants
    for dp, dn, fn in os.walk(ROOT):
        for name in dn + fn:
            if name.endswith(".meta"):
                continue
            path = os.path.join(dp, name)
            meta = path + ".meta"
            if os.path.exists(meta):
                continue
            if os.path.isdir(path):
                template = FOLDER
            elif name.endswith(".cs"):
                template = SCRIPT
            elif name.lower().endswith((".ttf", ".otf")):
                template = FONT
            elif name.lower().endswith((".txt", ".json", ".md")):
                template = TEXT
            else:
                template = OTHER
            with open(meta, "w", newline="\n") as out:
                family = os.path.splitext(name)[0]
                out.write(template.format(guid=guid_for(path), family=family))
            created.append(path)

    for p in created:
        print("  cree      " + p + ".meta")
    for p in removed:
        print("  supprime  " + p)
    if not created and not removed:
        print("  .meta a jour")


if __name__ == "__main__":
    main()
