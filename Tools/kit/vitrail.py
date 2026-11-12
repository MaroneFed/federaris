# Texture du vitrail gothique (lancette) : losanges au plomb, trois medaillons, bordure rubis et saphir.
# Usage : blender -b --factory-startup --python vitrail.py -- <dossier Textures>
import bpy, sys, os, math
import numpy as np

TEX = sys.argv[sys.argv.index('--') + 1]
os.makedirs(TEX, exist_ok=True)
W, H = 512, 1024
A, HJ = 0.6, 2.4                         # demi-largeur, hauteur des jambages (comme dans kit_fief.py)
HT = HJ + 2 * A * math.sin(math.radians(60))

ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
x = ((xs + 0.5) / W - 0.5) * 2 * A
z = (1 - (ys + 0.5) / H) * HT

RUBY = np.array([0.80, 0.05, 0.07], np.float32)
SAPH = np.array([0.07, 0.20, 0.85], np.float32)
GOLD = np.array([0.98, 0.66, 0.12], np.float32)
EMER = np.array([0.05, 0.58, 0.28], np.float32)
PALE = np.array([0.62, 0.78, 0.92], np.float32)
VIOL = np.array([0.45, 0.12, 0.62], np.float32)


def hash2(a, b, k=0.0):
    return np.modf(np.abs(np.sin(a * 127.1 + b * 311.7 + k * 74.7) * 43758.5453))[0]


# distance au bord de la lancette (positive dedans)
d = np.where(z < HJ, A - np.abs(x), 2 * A - np.hypot(np.abs(x) + A, z - HJ))
d = np.minimum(d, z)

col = np.zeros((H, W, 3), np.float32)
lead = np.full((H, W), 9.0, np.float32)          # distance au plomb le plus proche (m)

# champ de losanges
p = 0.17
u, v = (x + z * 0.62) / p, (x - z * 0.62) / p
iu, iv = np.floor(u), np.floor(v)
fu, fv = u - iu, v - iv
lead = np.minimum(lead, np.minimum(np.minimum(fu, 1 - fu), np.minimum(fv, 1 - fv)) * p * 0.75)
h = hash2(iu, iv)
field = PALE[None, None, :] * (0.55 + 0.45 * hash2(iu, iv, 1.0))[..., None]
field = np.where((h > 0.86)[..., None], SAPH * 0.9, field)
field = np.where((h < 0.07)[..., None], GOLD * 0.9, field)
col[:] = field

# trois medaillons
for k, (zc, c1, c2) in enumerate([(0.62, RUBY, SAPH), (1.50, SAPH, GOLD), (2.38, RUBY, EMER)]):
    r = np.hypot(x, z - zc)
    ang = np.arctan2(z - zc, x)
    inside = r < 0.40
    sect = np.floor((ang + math.pi) / (2 * math.pi) * 8)
    fs = (ang + math.pi) / (2 * math.pi) * 8 - sect
    m = np.where((sect % 2 == 0)[..., None], c1, c2) * (0.7 + 0.3 * hash2(sect, np.full_like(sect, k)))[..., None]
    m = np.where((r < 0.13)[..., None], GOLD, m)
    m = np.where(((r > 0.31) & (r < 0.40))[..., None], np.where((sect % 2 == 0)[..., None], VIOL, PALE * 0.9), m)
    col = np.where(inside[..., None], m, col)
    ml = np.minimum(np.minimum(np.abs(r - 0.40), np.abs(r - 0.31)), np.abs(r - 0.13))
    spoke = np.where((r > 0.13) & (r < 0.31), np.minimum(fs, 1 - fs) * r * (2 * math.pi / 8), 9.0)
    ml = np.minimum(ml, spoke)
    lead = np.where(r < 0.44, np.minimum(np.where(inside, 9.0, lead), ml), lead)

# bordure : paves rubis et saphir
B = 0.085
band = d < B
blk = np.floor(np.where(z < HJ, z, HJ + np.arctan2(z - HJ, np.abs(x) + A) * 2 * A) / 0.14)
fb = np.where(z < HJ, z, HJ + np.arctan2(z - HJ, np.abs(x) + A) * 2 * A) / 0.14 - blk
bc = np.where((blk % 2 == 0)[..., None], RUBY, SAPH) * (0.75 + 0.25 * hash2(blk, np.sign(x)))[..., None]
col = np.where(band[..., None], bc, col)
lead = np.where(band, np.minimum(np.abs(d - B), np.minimum(fb, 1 - fb) * 0.14), np.minimum(lead, np.abs(d - B)))
lead = np.minimum(lead, np.abs(d))

# plombs, salissure, verre inegal
came = np.clip(lead / 0.007, 0, 1) ** 2
grime = np.clip(lead / 0.035, 0, 1) * 0.35 + 0.65
streak = 0.75 + 0.25 * np.sin(x * 37 + np.sin(z * 9) * 2) * np.sin(z * 53 + x * 11)
light = (0.55 + 0.45 * z / HT) * streak
emis = col * (came * grime * light)[..., None] * np.array([0.82, 0.90, 1.0], np.float32)
emis = np.clip(emis, 0, 1)
albedo = np.clip(col * 0.22 * came[..., None] + 0.015, 0, 1)


def save(arr, name):
    hh, ww = arr.shape[:2]
    rgba = np.concatenate([arr, np.ones((hh, ww, 1), np.float32)], -1)
    lin = rgba.copy()
    lin[..., :3] = np.where(lin[..., :3] <= 0.04045, lin[..., :3] / 12.92, ((lin[..., :3] + 0.055) / 1.055) ** 2.4)
    img = bpy.data.images.new(name, ww, hh, alpha=False)
    img.pixels.foreach_set(np.ascontiguousarray(lin[::-1]).ravel())
    img.filepath_raw = os.path.join(TEX, name + '.png')
    img.file_format = 'PNG'
    img.save()
    print('SAVED', img.filepath_raw)


save(emis, 'Vitrail_Emission')
save(albedo, 'Vitrail_Albedo')
