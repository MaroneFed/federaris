# -*- coding: utf-8 -*-
"""Reconstruit en Python le monde que GameBootstrap construit dans Unity.

Memes formules, meme generateur aleatoire (voir netrandom.py) : le relief et le
point d'apparition calcules ici sont ceux du jeu. C'est ce qui permet de
diagnostiquer un bug de terrain sans lancer Unity.

A tenir synchronise avec Ground.cs et GameBootstrap.FindClearing.
"""
import math
from netrandom import NetRandom

MAP_SIZE = 700.0
WORLD_SEED = 1337
RIM_HEIGHT = 14.0


class Terrain(object):
    def __init__(self, map_size=MAP_SIZE, seed=WORLD_SEED):
        self.size = map_size
        self.hills = []
        rng = NetRandom(seed ^ 0x51F5E)
        half = map_size * 0.5
        count = max(12, min(400, int(round(map_size * map_size / 9000.0))))
        for _ in range(count):
            x = (rng.next_double() * 2 - 1) * half
            z = (rng.next_double() * 2 - 1) * half
            radius = 34 + rng.next_double() * 96
            height = (rng.next_double() * 2 - 1) * 7.5
            self.hills.append((x, z, radius, height))
        for i in range(5):
            a = (i / 5.0) * math.pi * 2 + rng.next_double()
            d = half * (0.25 + rng.next_double() * 0.5)
            self.hills.append((math.cos(a) * d, math.sin(a) * d,
                               150 + rng.next_double() * 90, -11.0))

    def height(self, x, z):
        h = 0.0
        for (cx, cz, r, hh) in self.hills:
            d = math.hypot(x - cx, z - cz)
            if d < r:
                h += hh * 0.5 * (1 + math.cos(math.pi * d / r))
        edge = max(abs(x), abs(z)) / (self.size * 0.5)
        if edge > 0.8:
            t = min(1.0, max(0.0, (edge - 0.8) / 0.2))
            h += RIM_HEIGHT * t * t
        h += math.sin(x * 0.0065) * math.cos(z * 0.0055) * 4.5
        return h

    def slope(self, x, z, step=2.0):
        hx = self.height(x + step, z) - self.height(x - step, z)
        hz = self.height(x, z + step) - self.height(x, z - step)
        return min(1.0, math.hypot(hx, hz) / (2 * step))


def canopy(x, z):
    a = math.sin(x * 0.0121) * math.cos(z * 0.0095)
    b = math.sin((x + z) * 0.0052 + 1.7)
    c = math.cos(x * 0.0271 - z * 0.0233) * 0.45
    return min(1.0, max(0.0, 0.52 + (a * 0.42 + b * 0.34 + c) * 0.55))


def find_clearing(terrain):
    best, best_score = (0.0, 0.0), 99.0
    for i in range(220):
        a = i * 2.39996
        r = 9 * math.sqrt(i)
        x, z = math.cos(a) * r, math.sin(a) * r
        score = canopy(x, z) + terrain.slope(x, z) * 0.02
        if score < best_score:
            best_score, best = score, (x, z)
    return best
