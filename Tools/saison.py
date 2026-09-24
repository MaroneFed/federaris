# -*- coding: utf-8 -*-
"""Simulation d'une Saison solo complete (30 minutes), sans Unity.

But : repondre a la Porte 1 avec des chiffres, et regler les paliers de
Menus.Rank. Trois joueurs types parcourent la meme foret :

  - l'expert   : va aux six apparitions, vide le fer du chateau, remplit ses
                 caches entre deux apparitions et fait plusieurs voyages
                 pendant que le mage chante ;
  - le regulier : rate une apparition sur trois, pas de caches, sac aux 3/4 ;
  - le flaneur  : deux ou trois apparitions, jamais de fer, bois et pierre-lune.

Les valeurs viennent de GameConfig.cs, Gathering.cs, Castle.cs, Relic.cs,
Season.cs. Les deplacements sont en ligne droite multipliee par un facteur de
detour (la foret) et, pour trouver le mage a l'oreille, un facteur de recherche.

Usage : python3 Tools/saison.py
"""
import math
import random

# --- GameConfig / Season
SEASON = 1800.0
FIRST_MAGE, INTERVAL, STAY = 120.0, 270.0, 150.0
MAGE_NEAR, MAGE_FAR = 110.0, 260.0
BAG = 60.0
RELIC_WEIGHT = 5.0
SPEED_EMPTY, SPEED_FULL = 7.6, 5.6
HARVEST, PENALTY = 1.15, 2.4
DIG = 3.5
CACHE_CAP, CAMP_CAP = 40.0, 60.0

# --- ResourceInfo : poids, valeur a la forge
WEIGHT = {"bois": 1.0, "pierre": 3.0, "fer": 2.0}
VALUE = {"bois": 1, "pierre": 4, "fer": 10}
# unites par geste, et multiplicateur de duree du geste
YIELD = {"bois": 2, "pierre": 1, "fer": 1}
SLOW = {"bois": 1.0, "pierre": 1.4, "fer": 1.0}
IRON_TOTAL = 54                 # 3 reserves x 3 caisses x 6, ne revient pas

DETOUR = 1.35                   # la foret n'est pas une ligne droite
SEARCH = 1.3                    # trouver le mage a l'oreille
HOLLOW_WALK = 60.0              # marche moyenne jusqu'au creux suivant
HOLLOW_UNITS = 8                # pierres-lune par creux (2 gisements x 4)


def appearances():
    k, out = 0, []
    while FIRST_MAGE + k * INTERVAL + STAY <= SEASON + 0.01:
        out.append((FIRST_MAGE + k * INTERVAL, FIRST_MAGE + k * INTERVAL + STAY))
        k += 1
    return out


def power(parts):
    total = sum(parts[r] * VALUE[r] for r in parts)
    kinds = sum(1 for r in parts if parts[r] > 0)
    bonus = 1.6 if kinds >= 3 else 1.25 if kinds == 2 else 1.0
    return int(round(total * bonus))


class Player(object):
    def __init__(self, name, rng, skip_every=0, bag_fill=1.0, uses_iron=True,
                 uses_caches=False, stone_share=1.0, max_visits=6):
        self.name = name
        self.rng = rng
        self.skip_every = skip_every
        self.bag_fill = bag_fill
        self.uses_iron = uses_iron
        self.uses_caches = uses_caches
        self.stone_share = stone_share
        self.max_visits = max_visits
        self.t = 0.0
        self.pos = (250.0, 0.0)
        self.bag = {"bois": 0, "pierre": 0, "fer": 0}
        self.relic = None
        self.stores = []            # [position, contenu en kg de pierre-lune]
        self.visits = 0
        self.log = []

    # ---------------------------------------------------------------- outils
    def carried(self):
        w = sum(self.bag[r] * WEIGHT[r] for r in self.bag)
        return w + (RELIC_WEIGHT if self.relic is not None else 0.0)

    def load(self):
        return min(1.0, self.carried() / BAG)

    def speed(self):
        return SPEED_EMPTY + (SPEED_FULL - SPEED_EMPTY) * self.load()

    def walk(self, to, factor=DETOUR):
        d = math.hypot(to[0] - self.pos[0], to[1] - self.pos[1]) * factor
        self.t += d / self.speed()
        self.pos = to

    def harvest(self, res, units):
        got = 0
        while got < units and self.carried() + WEIGHT[res] <= BAG * self.bag_fill + 1e-6:
            self.t += HARVEST * SLOW[res] * (1 + (PENALTY - 1) * self.load())
            n = min(YIELD[res], units - got)
            while n > 0 and self.carried() + WEIGHT[res] <= BAG + 1e-6:
                self.bag[res] += 1
                got += 1
                n -= 1
        return got

    # ---------------------------------------------------------------- remplir le sac
    def wander(self):
        """Jusqu'au creux suivant : 60 m dans une direction au hasard, dans la carte."""
        a = self.rng.uniform(0, 2 * math.pi)
        x = max(-300.0, min(300.0, self.pos[0] + math.cos(a) * HOLLOW_WALK))
        z = max(-300.0, min(300.0, self.pos[1] + math.sin(a) * HOLLOW_WALK))
        if abs(x) < 70 and abs(z) < 70:
            x = math.copysign(90.0, x)
        self.walk((x, z), 1.0)

    def fill_bag(self, iron_left, until=None):
        """Remplit le sac avec le meilleur possible. Renvoie le fer restant."""
        if self.bag["bois"] == 0 and (self.relic is None or self.relic["bois"] == 0):
            self.t += 8.0
            self.harvest("bois", 1)
        if self.uses_iron and iron_left > 0:
            self.walk((self.rng.uniform(-30, 30), self.rng.uniform(-30, 30)))
            got = self.harvest("fer", iron_left)
            self.t += got / 6.0 * 3.0          # d'une caisse a l'autre
            iron_left -= got
        # pierre-lune, puis bois mort pour finir le sac
        while self.carried() + WEIGHT["pierre"] <= BAG * self.bag_fill and self.stone_share > 0:
            if until is not None and self.t > until:
                break
            if self.rng.random() > self.stone_share:
                break
            self.wander()
            self.harvest("pierre", HOLLOW_UNITS)
        while self.carried() + 1 <= BAG * self.bag_fill:
            if until is not None and self.t > until:
                break
            self.t += 6.0
            if self.harvest("bois", 8) == 0:
                break
        return iron_left

    def stock_stores(self, until, iron_left):
        """
        Entre deux apparitions : on ne reste pas les bras croises. On remplit le
        sac, on le vide dans une cache creusee la ou l'on est (ou dans la plus
        proche qui a de la place), et on recommence tant qu'il reste du temps
        pour remplir le sac une derniere fois.
        """
        if not self.uses_caches:
            return iron_left
        while self.t < until:
            iron_left = self.fill_bag(iron_left, until)
            room = [s for s in self.stores if s[1] + 3 <= s[2]]
            near = [s for s in room if math.hypot(s[0][0] - self.pos[0], s[0][1] - self.pos[1]) < 60]
            if near:
                store = near[0]
                self.walk(store[0])
            elif len(self.stores) < 4:
                cap = CAMP_CAP if not self.stores else CACHE_CAP
                if self.stores:
                    self.t += DIG * (1 + (PENALTY - 1) * self.load())
                store = [self.pos, 0.0, cap]
                self.stores.append(store)
            elif room:
                store = min(room, key=lambda s: math.hypot(s[0][0] - self.pos[0], s[0][1] - self.pos[1]))
                self.walk(store[0])
            else:
                break
            self.t += 5
            for r in ("fer", "pierre"):
                while self.bag[r] > 0 and store[1] + WEIGHT[r] <= store[2]:
                    self.bag[r] -= 1
                    store[1] += WEIGHT[r]
                    self.stored[r] += 1
        return iron_left

    def forge(self):
        if self.relic is None:
            self.relic = {"bois": 0, "pierre": 0, "fer": 0}
        melted = sum(self.bag.values())
        for r in self.bag:
            self.relic[r] += self.bag[r]
            self.bag[r] = 0
        self.t += 4.0
        return melted

    def take_from(self, store):
        """Recharger le sac depuis une cache : le fer d'abord (il vaut plus)."""
        for r in ("fer", "pierre"):
            while self.stored[r] > 0 and store[1] >= WEIGHT[r] and self.carried() + WEIGHT[r] <= BAG:
                self.stored[r] -= 1
                store[1] -= WEIGHT[r]
                self.bag[r] += 1
        self.t += 6

    # ---------------------------------------------------------------- une Saison
    def play(self):
        iron = IRON_TOTAL
        self.stored = {"fer": 0, "pierre": 0}
        for k, (start, end) in enumerate(appearances()):
            if self.visits >= self.max_visits:
                break
            if self.skip_every and k % self.skip_every == self.skip_every - 1:
                continue
            # Le sac vient d'etre vide a la forge : on remplit d'abord les reserves
            # (en gardant de quoi remplir le sac ensuite), puis le sac.
            if self.t < start:
                iron = self.stock_stores(start - 110, iron)
            if self.t < start:
                iron = self.fill_bag(iron)
            self.t = max(self.t, start)
            # le mage apparait a 110-260 m de la ou l'on se tient
            a = self.rng.uniform(0, 2 * math.pi)
            d = self.rng.uniform(MAGE_NEAR, MAGE_FAR)
            mage = (max(-300.0, min(300.0, self.pos[0] + math.cos(a) * d)),
                    max(-300.0, min(300.0, self.pos[1] + math.sin(a) * d)))
            self.walk(mage, DETOUR * SEARCH)
            if self.t > end:
                self.log.append("  apparition %d : arrive trop tard" % (k + 1))
                continue
            melted = self.forge()
            trips = 1
            # voyages supplementaires depuis la reserve la plus proche
            while self.uses_caches and self.t < end:
                full = [s for s in self.stores if s[1] >= 2]
                if not full:
                    break
                s = min(full, key=lambda s: math.hypot(s[0][0] - mage[0], s[0][1] - mage[1]))
                trip = math.hypot(s[0][0] - mage[0], s[0][1] - mage[1]) * DETOUR
                if self.t + trip / SPEED_EMPTY + trip / SPEED_FULL + 10 > end:
                    break
                self.walk(s[0])
                self.take_from(s)
                self.walk(mage)
                melted += self.forge()
                trips += 1
            self.visits += 1
            self.log.append("  apparition %d : %2d unites fondues en %d voyage(s), puissance %d   (%s)"
                            % (k + 1, melted, trips, power(self.relic), clock(self.t)))
        # la stele
        if self.relic is not None:
            self.walk((0.0, 0.0))
            placed = self.t <= SEASON
            self.log.append("  stele atteinte a %s%s" % (clock(self.t), "" if placed else "  -> TROP TARD"))
            return power(self.relic) if placed else 0
        return 0


def clock(t):
    return "%d:%02d" % (int(t) // 60, int(t) % 60)


def rank(p):
    if p <= 0:
        return "rien"
    if p < 120:
        return "une babiole"
    if p < 400:
        return "un fetiche"
    if p < 800:
        return "une relique"
    if p < 1450:
        return "un tresor de mage"
    return "une legende"


def main():
    print("Apparitions du mage : " + ", ".join("%s-%s" % (clock(a), clock(b)) for a, b in appearances()))
    print("")
    profiles = [
        ("expert", dict(uses_caches=True)),
        ("expert sans caches", dict()),
        ("regulier", dict(skip_every=3, bag_fill=0.75)),
        ("flaneur", dict(max_visits=3, bag_fill=0.6, uses_iron=False, stone_share=0.5)),
    ]
    for name, kw in profiles:
        scores = []
        for seed in range(200):
            scores.append(Player(name, random.Random(seed), **kw).play())
        scores.sort()
        p = Player(name, random.Random(7), **kw)
        s = p.play()
        print("%-20s  mediane %4d  (%s)   10%%-90%% : %d - %d" %
              (name, scores[len(scores) // 2], rank(scores[len(scores) // 2]),
               scores[len(scores) // 10], scores[len(scores) * 9 // 10]))
        for line in p.log:
            print(line)
        print("")


if __name__ == "__main__":
    main()
