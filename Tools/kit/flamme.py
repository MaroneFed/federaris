# Planche d'animation de flamme (8 x 8 images) + point de braise, generes par bruit procedural.
# Usage : blender -b --factory-startup --python flamme.py -- <dossier Textures>
import bpy, sys, os
import numpy as np

TEX = sys.argv[sys.argv.index('--') + 1]
os.makedirs(TEX, exist_ok=True)
N, GRID, FR = 256, 8, 64
rng = np.random.default_rng(11)
LAT = rng.random((64, 64, 64), dtype=np.float32)


def vnoise(x, y, z):
    xi, yi, zi = np.floor(x).astype(np.int32), np.floor(y).astype(np.int32), np.floor(z).astype(np.int32)
    xf, yf, zf = x - xi, y - yi, z - zi
    sx, sy, sz = xf * xf * (3 - 2 * xf), yf * yf * (3 - 2 * yf), zf * zf * (3 - 2 * zf)
    g = lambda a, b, c: LAT[(xi + a) & 63, (yi + b) & 63, (zi + c) & 63]
    x00 = g(0, 0, 0) * (1 - sx) + g(1, 0, 0) * sx
    x10 = g(0, 1, 0) * (1 - sx) + g(1, 1, 0) * sx
    x01 = g(0, 0, 1) * (1 - sx) + g(1, 0, 1) * sx
    x11 = g(0, 1, 1) * (1 - sx) + g(1, 1, 1) * sx
    return (x00 * (1 - sy) + x10 * sy) * (1 - sz) + (x01 * (1 - sy) + x11 * sy) * sz


def fbm(x, y, z, octaves=4):
    v, a, f, tot = 0.0, 0.5, 1.0, 0.0
    for o in range(octaves):
        v = v + a * vnoise(x * f + o * 17.3, y * f + o * 9.1, z * f + o * 3.7)
        tot += a
        a *= 0.5
        f *= 2.0
    return v / tot


def sstep(lo, hi, v):
    t = np.clip((v - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


RAMP_X = np.array([0.0, 0.12, 0.32, 0.55, 0.8, 1.0], np.float32)
RAMP = np.array([[0, 0, 0], [0.45, 0.02, 0.0], [0.95, 0.20, 0.01], [1.0, 0.52, 0.06], [1.0, 0.82, 0.30], [1.0, 0.97, 0.80]], np.float32)

ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
x = (xs + 0.5) / N * 2 - 1            # -1 .. 1
y = 1 - (ys + 0.5) / N                # 1 en haut, 0 en bas
border = sstep(0.0, 0.10, 1 - np.abs(x)) * sstep(0.0, 0.08, 1 - y)
sheet = np.zeros((N * GRID, N * GRID, 4), np.float32)
for f in range(FR):
    T = f / FR * 2.6
    n1 = fbm(x * 1.6 + 5, y * 2.2 - T * 2.6, np.full_like(x, T * 0.9))
    n2 = fbm(x * 4.2 + 9, y * 4.5 - T * 5.5, np.full_like(x, T * 1.8 + 20))
    n3 = fbm(x * 9.0 + 3, y * 8.0 - T * 9.0, np.full_like(x, T * 2.5 + 40), 3)
    xd = x + (n1 - 0.5) * 0.8 * y ** 1.2 + (n2 - 0.5) * 0.40 * y
    w = 0.60 * (1 - y) ** 0.70 * np.clip(y / 0.10, 0, 1) ** 0.5 + 0.01
    d = np.abs(xd) / w
    body = np.clip(1 - d * d, 0, 1)
    top = np.clip(1 - y * (0.95 + (n1 - 0.5) * 1.1), 0, 1)
    inten = body * top ** 0.7
    inten = np.clip(inten * 1.5 - n2 * 0.72 * (0.25 + y) - n3 * 0.30 * (0.2 + y), 0, 1)
    inten = inten * border
    rgb = np.stack([np.interp(inten, RAMP_X, RAMP[:, c]) for c in range(3)], -1)
    blue = sstep(0.0, 0.5, body) * sstep(0.16, 0.02, y) * 0.5
    rgb = rgb + blue[..., None] * np.array([0.08, 0.22, 0.75], np.float32)
    alpha = np.clip(np.maximum(inten * 1.9, blue * border), 0, 1)
    r, c = divmod(f, GRID)
    sheet[r * N:(r + 1) * N, c * N:(c + 1) * N, :3] = np.clip(rgb, 0, 1)
    sheet[r * N:(r + 1) * N, c * N:(c + 1) * N, 3] = alpha


def save(arr, name):
    h, w = arr.shape[:2]
    img = bpy.data.images.new(name, w, h, alpha=True)
    img.alpha_mode = 'CHANNEL_PACKED'
    img.pixels.foreach_set(np.ascontiguousarray(arr[::-1]).ravel())
    img.filepath_raw = os.path.join(TEX, name + '.png')
    img.file_format = 'PNG'
    img.save()
    print('SAVED', img.filepath_raw)


# Blender convertit les pixels lineaires en sRGB a l'enregistrement : on fournit donc du lineaire.
lin = sheet.copy()
lin[..., :3] = np.where(lin[..., :3] <= 0.04045, lin[..., :3] / 12.92, ((lin[..., :3] + 0.055) / 1.055) ** 2.4)
save(lin, 'Flamme_Flipbook')
M = 64
yy, xx = np.mgrid[0:M, 0:M].astype(np.float32)
rr = np.hypot((xx + 0.5) / M * 2 - 1, (yy + 0.5) / M * 2 - 1)
a = np.clip(1 - rr, 0, 1) ** 2.2
dot = np.stack([np.ones_like(a), np.ones_like(a), np.ones_like(a), a], -1)
save(dot, 'Braise_Point')
