# -*- coding: utf-8 -*-
"""System.Random de .NET/Mono, reproduit a l'identique.

Le jeu tire tout son monde d'un System.Random(graine) : relief, foret, point
d'apparition. En reproduisant exactement le meme generateur, on peut recalculer
en Python le MEME monde que celui qu'Unity construit -- et donc verifier un bug
de terrain ou de placement sans lancer le jeu.

Algorithme soustractif de Knuth, tel qu'implemente dans .NET Framework, dans
Mono (qu'utilise Unity) et dans le mode compatible de .NET Core pour les
generateurs graines.
"""

MBIG = 2147483647
MSEED = 161803398


class NetRandom(object):
    def __init__(self, seed):
        seed = int(seed)
        # meme conversion qu'en C# : la graine est un int 32 bits signe
        seed = ((seed + 2**31) % 2**32) - 2**31
        subtraction = MBIG if seed == -2**31 else abs(seed)
        mj = MSEED - subtraction
        a = [0] * 56
        a[55] = mj
        mk = 1
        for i in range(1, 55):
            ii = (21 * i) % 55
            a[ii] = mk
            mk = mj - mk
            if mk < 0:
                mk += MBIG
            mj = a[ii]
        for _ in range(1, 5):
            for i in range(1, 56):
                a[i] -= a[1 + (i + 30) % 55]
                if a[i] < 0:
                    a[i] += MBIG
        self.a = a
        self.inext = 0
        self.inextp = 21

    def _internal(self):
        i = self.inext + 1
        if i >= 56:
            i = 1
        j = self.inextp + 1
        if j >= 56:
            j = 1
        v = self.a[i] - self.a[j]
        if v == MBIG:
            v -= 1
        if v < 0:
            v += MBIG
        self.a[i] = v
        self.inext = i
        self.inextp = j
        return v

    def next_double(self):
        return self._internal() * (1.0 / MBIG)

    def next(self, max_value=None):
        if max_value is None:
            return self._internal()
        return int(self.next_double() * max_value)
