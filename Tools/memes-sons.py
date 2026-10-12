# -*- coding: utf-8 -*-
"""TELECHARGE LES VRAIS SONS DES MEMES (mode MEME de FIEF), sur TON PC.

    python Tools/memes-sons.py            (ou double-clic sur Tools/memes-sons.bat)
    python Tools/memes-sons.py --tout     (re-telecharge meme ceux qui sont deja la)

La liste des memes est lue dans Assets/_Fief/Scripts/Core/MemeSounds.cs (une seule
reference). Pour chacun, le script cherche le son sur myinstants.com (la banque de
boutons de memes) et range le premier resultat dans MemesSons/<Meme>/.

Le son ne te plait pas ? Supprime le fichier et pose le tien (.mp3, .ogg, .wav) dans le
meme dossier : le jeu prend ce qu'il y trouve, au hasard s'il y en a plusieurs.

ATTENTION : ces enregistrements appartiennent a leurs auteurs. C'est pour jouer entre
amis : ils restent dans MemesSons/ (jamais dans Assets, jamais dans Git, jamais dans le
jeu vendu sur Steam).
"""
import os, re, sys, time, urllib.parse, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LISTE = os.path.join(ROOT, "Assets", "_Fief", "Scripts", "Core", "MemeSounds.cs")
DOSSIER = os.path.join(ROOT, "MemesSons")
SITE = "https://www.myinstants.com"
ENTETES = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36"}
SONS = (".mp3", ".ogg", ".wav")


def lire(url):
    req = urllib.request.Request(url, headers=ENTETES)
    with urllib.request.urlopen(req, timeout=20) as r:
        return r.read()


def memes():
    texte = open(LISTE, encoding="utf-8").read()
    return re.findall(r'new Entry\("(\w+)", "([^"]+)", "\w+"\)', texte)


def main():
    tout = "--tout" in sys.argv
    liste = memes()
    print("%d memes dans la liste. Dossier : %s\n" % (len(liste), DOSSIER))
    ok, rate = 0, []
    for cle, cherche in liste:
        dossier = os.path.join(DOSSIER, cle)
        os.makedirs(dossier, exist_ok=True)
        deja = [f for f in os.listdir(dossier) if f.lower().endswith(SONS)]
        if deja and not tout:
            print("  deja la  %-16s %s" % (cle, deja[0]))
            ok += 1
            continue
        try:
            page = lire(SITE + "/en/search/?name=" + urllib.parse.quote(cherche)).decode("utf-8", "replace")
            trouves = re.findall(r"play\('(/media/sounds/[^']+?\.mp3)'", page)
            if not trouves:
                print("  RIEN     %-16s (cherche : %s)" % (cle, cherche))
                rate.append(cle)
                continue
            chemin = trouves[0]
            nom = os.path.basename(urllib.parse.unquote(chemin))
            data = lire(SITE + urllib.parse.quote(chemin))
            with open(os.path.join(dossier, nom), "wb") as f:
                f.write(data)
            print("  ok       %-16s %s (%d Ko)" % (cle, nom, len(data) // 1024))
            ok += 1
            time.sleep(0.4)
        except Exception as e:
            print("  ERREUR   %-16s %s" % (cle, e))
            rate.append(cle)
    print("\n%d / %d memes ont leur vrai son." % (ok, len(liste)))
    if rate:
        print("Sans son (pose un .mp3 a la main dans MemesSons/<Meme>/) : " + ", ".join(rate))
    print("Relance le jeu (ou le Play d'Unity) : il les charge au demarrage.")


if __name__ == "__main__":
    main()
    if os.name == "nt" and len(sys.argv) == 1:
        input("\nEntree pour fermer...")
