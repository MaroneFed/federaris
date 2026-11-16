# Decoupe un fichier de voix en morceaux, un par mot ou par phrase, en coupant dans les silences.
# Usage : blender -b --factory-startup --python decouper_voix.py -- <fichier.mp3> <dossier de sortie> <prefixe>
# (Blender sait lire le mp3 ; les morceaux sortent en .wav, qu'Unity lit tres bien.)
import sys, os, wave
import aud
import numpy as np

src, out, prefix = sys.argv[sys.argv.index('--') + 1:][:3]
os.makedirs(out, exist_ok=True)
sound = aud.Sound(src)
rate = int(sound.specs[0])
data = np.array(sound.data(), dtype=np.float32)
mono = data.mean(axis=1) if data.ndim > 1 else data
print('DUREE %.2f s, %d Hz' % (len(mono) / rate, rate))

win = int(rate * 0.02)                                  # des tranches de 20 ms
n = len(mono) // win
rms = np.sqrt((mono[:n * win].reshape(n, win) ** 2).mean(axis=1))
loud = rms > max(0.012, rms.max() * 0.06)
segments, start, quiet = [], None, 0
for i, on in enumerate(loud):
    if on:
        if start is None:
            start = i
        quiet = 0
    elif start is not None:
        quiet += 1
        if quiet >= 18:                                # 0,36 s de silence : le mot est fini
            segments.append((start, i - quiet + 1))
            start, quiet = None, 0
if start is not None:
    segments.append((start, n))
segments = [(a, b) for a, b in segments if (b - a) * 0.02 >= 0.18]
for k, (a, b) in enumerate(segments):
    lo, hi = max(0, a * win - int(rate * 0.06)), min(len(mono), b * win + int(rate * 0.15))
    piece = mono[lo:hi].copy()
    fade = int(rate * 0.02)
    piece[:fade] *= np.linspace(0, 1, fade)
    piece[-fade:] *= np.linspace(1, 0, fade)
    piece /= max(0.05, np.abs(piece).max()) / 0.9        # meme niveau pour tous
    path = os.path.join(out, '%s-%d.wav' % (prefix, k + 1))
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes((piece * 32767).astype(np.int16).tobytes())
    print('MORCEAU %d : %.2f s -> %.2f s (%.2f s)' % (k + 1, lo / rate, hi / rate, (hi - lo) / rate))
print('TOTAL', len(segments))
