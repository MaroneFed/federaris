# Kit "fief" : chateau medieval low-poly + textures PBR cuites, pour Unity.
# Usage : blender -b --factory-startup --python kit_fief.py -- <dossier_sortie> <dossier_apercus> <taille_atlas> [echantillons]
import bpy, bmesh, sys, os, math, random, time
import numpy as np
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index('--') + 1:]
OUT, PREV, S = argv[0], argv[1], int(argv[2])
SAMPLES = int(argv[3]) if len(argv) > 3 else 16
S_FER = max(1024, S // 2)
TEX = os.path.join(OUT, 'Textures')
os.makedirs(TEX, exist_ok=True)
os.makedirs(PREV, exist_ok=True)
T0 = time.time()


def log(*a):
    print('[%6.1fs]' % (time.time() - T0), *a, flush=True)


T = 0.5        # epaisseur des murs
G = 0.035      # profondeur des joints
B = 0.03       # largeur du chanfrein des pierres
ROWS = [0.55, 0.45, 0.5, 0.5, 0.6, 0.4, 0.5, 0.5]

FACES = []     # (points, couleur, kind)  kind : 0 normal, 1 cache, 2 braises, 3 face avant (a dupliquer en miroir)
EXTRA = []
CAP = {}


def newell(pts):
    n = Vector((0, 0, 0))
    for i in range(len(pts)):
        a, b = pts[i], pts[(i + 1) % len(pts)]
        n += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    return n


def add(pts, col=1.0, hint=None, lst=None, kind=0):
    out = []
    for p in pts:
        p = Vector(p)
        if not out or (p - out[-1]).length > 1e-5:
            out.append(p)
    if len(out) > 1 and (out[0] - out[-1]).length < 1e-5:
        out.pop()
    if len(out) < 3:
        return
    if hint is not None and newell(out).dot(Vector(hint)) < 0:
        out.reverse()
    (FACES if lst is None else lst).append((out, col, kind))


def inset(poly, ds):
    n = len(poly)
    lines = []
    for i in range(n):
        ax, ay = poly[i]
        bx, by = poly[(i + 1) % n]
        dx, dy = bx - ax, by - ay
        L = math.hypot(dx, dy)
        nx, ny = -dy / L, dx / L
        lines.append(((ax + nx * ds[i], ay + ny * ds[i]), (dx, dy)))
    out = []
    for i in range(n):
        (p, d), (q, e) = lines[i - 1], lines[i]
        den = d[0] * e[1] - d[1] * e[0]
        if abs(den) < 1e-9:
            out.append(q)
        else:
            t = ((q[0] - p[0]) * e[1] - (q[1] - p[1]) * e[0]) / den
            out.append((p[0] + d[0] * t, p[1] + d[1] * t))
    return out


def stone(poly, flags, tags, h, col, to3d, b=B):
    n = len(poly)
    ins = inset(poly, [b if f else 0.0 for f in flags])
    base = [Vector(to3d(u, v, 0)) for u, v in poly]
    front = [Vector(to3d(u, v, h)) for u, v in ins]
    cu = sum(p[0] for p in poly) / n
    cv = sum(p[1] for p in poly) / n
    cen = Vector(to3d(cu, cv, 0))
    nrm = Vector(to3d(cu, cv, 1)) - cen
    add(front, col, nrm)
    for i in range(n):
        j = (i + 1) % n
        if flags[i]:
            add([base[i], base[j], front[j], front[i]], col, nrm)
            prof = [base[i], base[j]]
        else:
            prof = [base[i], front[i], front[j], base[j]]
        if tags[i]:
            CAP[tags[i]](prof, (base[i] + base[j]) / 2 - cen, col)


def mirror_spec(poly, flags, tags):
    n = len(poly)
    return ([(-u, v) for u, v in reversed(poly)],
            [flags[(n - 2 - k) % n] for k in range(n)],
            [tags[(n - 2 - k) % n] for k in range(n)])


# ---------------------------------------------------------------- murs
def W3(u, v, h):
    return (u, -(T / 2 - G) - h, v)


def my(p):
    return Vector((p.x, -p.y, p.z))


def cap_mirror(prof, hint, col):
    add(prof + [my(p) for p in reversed(prof)], col, hint, EXTRA)


def row_bounds():
    z, out = 0.0, []
    for hgt in ROWS:
        out.append((z, z + hgt))
        z += hgt
    return out


def joints_for(r):
    rng = random.Random(100 + r)
    base = [-1.0, 0.0, 1.0] if r % 2 == 0 else [-1.5, -0.5, 0.5, 1.5]
    return [x + rng.uniform(-0.12, 0.12) for x in base]


def edge_vals(r):
    rng = random.Random(500 + r)
    return G + rng.uniform(-0.008, 0.012), rng.uniform(0.78, 1.08)


def rnd_vals(rng):
    return G + rng.uniform(-0.012, 0.014), rng.uniform(0.70, 1.12)


def row_part(r, z0, z1, lb, lflags, ltags, rng):
    nrows = len(ROWS)
    xa = max(p[0] for p in lb)
    js = [x for x in joints_for(r) if xa + 0.25 < x < 2 - 0.25]
    xs = [None] + js + [2.0]
    out = []
    for k in range(len(xs) - 1):
        x1 = xs[k + 1]
        last = (k == len(xs) - 2)
        rflag = (r % 2 == 0) if last else True
        rtag = 'cap' if last else None
        bt = 'cap' if r == 0 else None
        tt = 'cap' if r == nrows - 1 else None
        if k == 0:
            poly = [lb[0], (x1, z0), (x1, z1)] + list(reversed(lb[1:]))
            flags = [True, rflag, True] + list(lflags)
            tags = [bt, rtag, tt] + list(ltags)
            at_edge = (lb[0][0] <= -2 + 1e-6)
        else:
            x0 = xs[k]
            poly = [(x0, z0), (x1, z0), (x1, z1), (x0, z1)]
            flags = [True, rflag, True, True]
            tags = [bt, rtag, tt, None]
            at_edge = False
        edge = (r % 2 == 1) and (last or at_edge)
        h, col = edge_vals(r) if edge else rnd_vals(rng)
        out.append((poly, flags, tags, h, col, edge))
    return out


def finish_wall():
    FACES[:] = [(p, c, 3) for p, c, k in FACES] + EXTRA


def build_mur():
    CAP['cap'] = cap_mirror
    rng = random.Random(1)
    for r, (z0, z1) in enumerate(row_bounds()):
        for poly, flags, tags, h, col, _ in row_part(r, z0, z1, [(-2.0, z0), (-2.0, z1)], [r % 2 == 0], ['cap'], rng):
            stone(poly, flags, tags, h, col, W3)
    finish_wall()


def build_arche():
    CAP['cap'] = cap_mirror
    rng = random.Random(2)
    RHO, R1, R2, P, ZS = 1.2, 1.0, 1.42, 0.045, 2.0

    def xc(z):
        d = z - ZS
        return math.sqrt(RHO * RHO - d * d) if d < RHO else 0.0

    def both(spec, b=B):
        poly, flags, tags, h, col, edge = spec
        stone(poly, flags, tags, h, col, W3, b)
        mp, mf, mt = mirror_spec(poly, flags, tags)
        h2, col2 = (h, col) if edge else rnd_vals(rng)
        if b != B:
            h2 = h + rng.uniform(-0.006, 0.006)
        stone(mp, mf, mt, h2, col2, W3, b)

    rows = row_bounds()
    for r, (z0, z1) in enumerate(rows):
        if z1 <= ZS + 1e-6:
            lb, lf, lt = [(RHO, z0), (RHO, z1)], [False], [None]
        elif z0 - ZS >= RHO:
            for poly, flags, tags, h, col, _ in row_part(r, z0, z1, [(-2.0, z0), (-2.0, z1)], [r % 2 == 0], ['cap'], rng):
                stone(poly, flags, tags, h, col, W3)
            continue
        elif z1 - ZS > RHO:
            lb, lf, lt = [(xc(z0), z0), (0.0, ZS + RHO), (0.0, z1)], [True, False], [None, None]
        else:
            lb, lf, lt = [(xc(z0), z0), (xc(z1), z1)], [False], [None]
        for spec in row_part(r, z0, z1, lb, lf, lt, rng):
            both(spec)
    for r, (z0, z1) in enumerate(rows):   # piedroits
        if z1 > ZS + 1e-6:
            break
        w = 0.42 if r % 2 == 0 else 0.6
        poly = [(R1, z0), (R1 + w, z0), (R1 + w, z1), (R1, z1)]
        both((poly, [True, True, True, False], ['cap' if r == 0 else None, None, None, 'cap'],
              G + P + rng.uniform(-0.006, 0.006), rng.uniform(0.8, 1.05), False), 0.05)
    N = 9                                  # claveaux
    for k in range(N):
        a0, a1 = math.pi * k / N, math.pi * (k + 1) / N
        pt = lambda rad, a: (rad * math.cos(a), ZS + rad * math.sin(a))
        r2 = R2 + (0.08 if k == N // 2 else 0.0)
        poly = [pt(R1, a0), pt(r2, a0), pt(r2, a1), pt(R1, a1)]
        h = G + P + rng.uniform(-0.006, 0.006) + (0.02 if k == N // 2 else 0.0)
        stone(poly, [True, True, True, False], [None, None, None, 'cap'], h, rng.uniform(0.8, 1.05), W3, 0.05)
    finish_wall()


def build_dalle():
    TH = 0.2
    rng = random.Random(3)

    def F3(u, v, h):
        return (u, v, -G + h)

    def skirt(prof, hint, col):
        a, b = prof[0], prof[-1]
        add([a, b, Vector((b.x, b.y, -TH)), Vector((a.x, a.y, -TH))], 0.9, hint, kind=1)

    CAP['skirt'] = skirt
    depths = [0.9, 0.7, 0.8, 0.9, 0.7]
    y = -2.0
    for r, d in enumerate(depths):
        n = 3 if r % 2 == 0 else 4
        base = [-2 + 4 * (i + 1) / n for i in range(n - 1)]
        if r % 2 == 0:
            base = [x + 0.25 for x in base]
        xs = [-2.0] + [x + rng.uniform(-0.18, 0.18) for x in base] + [2.0]
        for k in range(len(xs) - 1):
            x0, x1, y0, y1 = xs[k], xs[k + 1], y, y + d
            tags = ['skirt' if abs(y0 + 2) < 1e-6 else None, 'skirt' if abs(x1 - 2) < 1e-6 else None,
                    'skirt' if abs(y1 - 2) < 1e-6 else None, 'skirt' if abs(x0 + 2) < 1e-6 else None]
            stone([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], [True] * 4, tags,
                  G + rng.uniform(-0.008, 0.0), rng.uniform(0.70, 1.1), F3)
        y += d
    add([(-2, -2, -TH), (2, -2, -TH), (2, 2, -TH), (-2, 2, -TH)], 0.9, (0, 0, -1), kind=1)


def build_voute():
    R, RE, Z0, N = 1.75, 2.25, 4.0, 9
    rng = random.Random(4)

    def V3(s, y, h):
        a = s / R
        rad = R + G - h
        return (rad * math.cos(a), y, Z0 + rad * math.sin(a))

    def ext(p):
        d = Vector((p.x, 0, p.z - Z0))
        d *= RE / d.length
        return Vector((d.x, p.y, Z0 + d.z))

    def vcap(prof, hint, col):
        a, b = prof[0], prof[-1]
        add([a, b, ext(b), ext(a)], 0.9, hint)

    CAP['v'] = vcap
    for k in range(N):
        s0, s1 = R * math.pi * k / N, R * math.pi * (k + 1) / N
        base = [-1.0, 0.0, 1.0] if k % 2 == 0 else [-0.667, 0.667]
        ys = [-2.0] + [y + rng.uniform(-0.1, 0.1) for y in base] + [2.0]
        for i in range(len(ys) - 1):
            y0, y1 = ys[i], ys[i + 1]
            tags = ['v' if i == 0 else None, 'v' if k == N - 1 else None,
                    'v' if i == len(ys) - 2 else None, 'v' if k == 0 else None]
            stone([(s0, y0), (s1, y0), (s1, y1), (s0, y1)], [True] * 4, tags,
                  G + rng.uniform(-0.01, 0.012), rng.uniform(0.70, 1.1), V3)
        a0, a1 = math.pi * k / N, math.pi * (k + 1) / N
        e = lambda a, y: Vector((RE * math.cos(a), y, Z0 + RE * math.sin(a)))
        am = (a0 + a1) / 2
        add([e(a0, -2), e(a0, 2), e(a1, 2), e(a1, -2)], 0.85, (math.cos(am), 0, math.sin(am)), kind=1)


# ---------------------------------------------------------------- volumes fermes
def loft(rings, cols):
    n = len(rings[0])
    for i in range(len(rings) - 1):
        a, b = rings[i], rings[i + 1]
        for k in range(n):
            add([a[k], a[(k + 1) % n], b[(k + 1) % n], b[k]], cols[i])
    add(list(reversed(rings[0])), cols[0])
    add(rings[-1], cols[-1])


def lathe(prof, n, cx=0.0, cy=0.0, closed=False, col=1.0, kind=0, hint=None, jit=None):
    def P(i, k):
        r, z = prof[i % len(prof)]
        k = k % n
        if jit:
            z += jit[(i % len(prof), k)]
        return Vector((cx + r * math.cos(2 * math.pi * k / n), cy + r * math.sin(2 * math.pi * k / n), z))
    m = len(prof) if closed else len(prof) - 1
    for i in range(m):
        for k in range(n):
            add([P(i, k), P(i, k + 1), P(i + 1, k + 1), P(i + 1, k)], col, hint, kind=kind)


def sweep(path, phi, hw, ht, ox=0.0, oy=0.0, col=1.0):
    c, s = math.cos(phi), math.sin(phi)
    P = lambda r, z, o: Vector((ox + r * c - o * s, oy + r * s + o * c, z))
    secs = []
    for i, (r, z) in enumerate(path):
        tan = Vector((0, 0))
        if i > 0:
            tan += (Vector(path[i]) - Vector(path[i - 1])).normalized()
        if i < len(path) - 1:
            tan += (Vector(path[i + 1]) - Vector(path[i])).normalized()
        k = 1.0
        if 0 < i < len(path) - 1:
            k = 1.0 / max(0.4, tan.length / 2)
        tan.normalize()
        nr, nz = -tan.y * ht * k, tan.x * ht * k
        secs.append([P(r + nr, z + nz, -hw), P(r + nr, z + nz, hw), P(r - nr, z - nz, hw), P(r - nr, z - nz, -hw)])
    loft(secs, [col] * len(secs))


def build_pilier():
    rng = random.Random(5)

    def ring(a, c, z):
        d = a - c
        return [Vector(p + (z,)) for p in [(a, -d), (a, d), (d, a), (-d, a), (-a, d), (-a, -d), (-d, -a), (d, -a)]]

    A, C = 0.30, 0.09
    spec = [(0.5, 0, 0.0), (0.5, 0, 0.30), (0.43, 0, 0.37), (0.39, 0, 0.37), (0.39, 0, 0.50), (A, C, 0.60)]
    cols = [0.85, 0.85, 0.8, 0.95, 0.95]
    c = rng.uniform(0.8, 1.05)
    for zj in [1.15, 1.72, 2.20, 2.78]:
        spec += [(A, C, zj - 0.025), (A - 0.022, C, zj), (A, C, zj + 0.025)]
        c2 = rng.uniform(0.75, 1.08)
        cols += [c, c, c2]
        c = c2
    spec += [(A, C, 3.28), (A + 0.04, C, 3.31), (A + 0.04, C, 3.38), (A, C, 3.41), (A, C, 3.46),
             (0.45, 0, 3.76), (0.5, 0, 3.76), (0.5, 0, 4.0)]
    cols += [c, 0.9, 0.9, 0.9, c, 0.92, 0.85, 0.85]
    loft([ring(*s) for s in spec], cols)


def build_torche():
    Y = -0.235
    prof = [(-.07, 1.42), (0, 1.35), (.07, 1.42), (.07, 1.80), (0, 1.87), (-.07, 1.80)]
    loft([[Vector((x, y, z)) for x, z in prof] for y in (Y, Y - 0.03)], [1.0])
    ph = -math.pi / 2
    sweep([(.25, 1.70), (.44, 1.70), (.52, 1.78), (.52, 1.83)], ph, 0.016, 0.016)
    sweep([(.25, 1.48), (.46, 1.69)], ph, 0.012, 0.012)
    lathe([(0, 1.80), (.035, 1.80), (.045, 1.83), (.075, 2.02), (.092, 2.02), (.092, 2.06), (.066, 2.06),
           (.036, 1.88), (0, 1.88)], 10, 0, -.52)
    for k in range(5):
        a = 2 * math.pi * (k + 0.5) / 5
        cx, cy = .079 * math.cos(a), -.52 + .079 * math.sin(a)
        lathe([(0, 2.05), (.013, 2.05), (0, 2.12)], 4, cx, cy)
    # braises dans la coupe
    lathe([(0, 2.045), (.035, 2.04), (.064, 2.025)], 10, 0, -.52, kind=2, hint=(0, 0, 1))
    return (0, -0.52, 2.06)


WIN_A, WIN_Z0, WIN_H = 0.6, 1.6, 2.4


def lancet(a, z0, n=6):
    """Le contour d'une fenetre en lancette (arc brise), du bas a gauche au bas a droite."""
    zs = z0 + WIN_H
    pts = [(-a, z0), (-a, zs)]
    for i in range(1, n + 1):
        t = math.radians(180 - 60 * i / n)
        pts.append((a + 2 * a * math.cos(t), zs + 2 * a * math.sin(t)))
    for i in range(n - 1, -1, -1):
        t = math.radians(60 * i / n)
        pts.append((-a + 2 * a * math.cos(t), zs + 2 * a * math.sin(t)))
    pts.append((a, z0))
    return pts


def build_fenetre():
    sweep(lancet(WIN_A + 0.09, WIN_Z0), 0.0, 0.09, 0.09, 0.0, -0.32, col=0.92)
    rect = [(-0.8, -0.46), (0.8, -0.46), (0.8, -0.23), (-0.8, -0.23)]
    loft([[Vector((x, y, z)) for x, y in rect] for z in (WIN_Z0 - 0.16, WIN_Z0)], [0.85])


def build_autel():
    """Un autel de pierre de 3 m : socle, corps en retrait, corniche, table."""
    def ring(ax, ay, z):
        return [Vector((ax, -ay, z)), Vector((ax, ay, z)), Vector((-ax, ay, z)), Vector((-ax, -ay, z))]
    spec = [(1.50, 0.60, 0.0), (1.50, 0.60, 0.14), (1.40, 0.50, 0.20), (1.40, 0.50, 0.82), (1.46, 0.56, 0.90),
            (1.56, 0.66, 0.94), (1.56, 0.66, 1.08), (1.52, 0.62, 1.10)]
    loft([ring(*r) for r in spec], [0.80, 0.85, 0.95, 0.9, 0.85, 1.05, 1.0])


def build_cierge():
    lathe([(0, 0), (.20, 0), (.20, .03), (.07, .07), (.03, .10), (.022, .14), (.022, .42), (.04, .45), (.022, .48),
           (.022, .88), (.04, .91), (.022, .94), (.022, 1.22), (.13, 1.26), (.14, 1.30), (.11, 1.30), (.03, 1.275),
           (0, 1.275)], 10)
    lathe([(0, 1.27), (.034, 1.27), (.034, 1.58), (.02, 1.60), (0, 1.595)], 8, kind=4)
    return (0, 0, 1.63)


def build_brasero():
    for k in range(3):
        sweep([(.40, 0.0), (.34, .05), (.07, .72), (.07, .82), (.215, .985)], math.pi / 2 + k * 2 * math.pi / 3, 0.02, 0.02)
    lathe([(.045, .72), (.10, .72), (.10, .80), (.045, .80)], 12, closed=True)
    lathe([(.215, .26), (.262, .26), (.262, .30), (.215, .30)], 12, closed=True)
    lathe([(0, .92), (.12, .93), (.24, 1.00), (.32, 1.11), (.345, 1.20), (.37, 1.20), (.37, 1.235), (.315, 1.235),
           (.29, 1.13), (.22, 1.04), (.11, .975), (0, .965)], 12)
    rng = random.Random(7)
    prof = [(0, 1.215), (.11, 1.205), (.21, 1.185), (.30, 1.15)]
    jit = {(i, k): (rng.uniform(-0.012, 0.018) if 0 < i < 3 else 0.0) for i in range(4) for k in range(12)}
    lathe(prof, 12, kind=2, hint=(0, 0, 1), jit=jit)
    return (0, 0, 1.24)


PIECES = [
    ('mur_droit', build_mur, 'Pierre', 66, True, ((3.6, -7.6, 2.7), (0, 0, 1.95))),
    ('mur_arche', build_arche, 'Pierre', 66, True, ((3.6, -7.6, 2.7), (0, 0, 1.95))),
    ('dalle_sol', build_dalle, 'Pierre', 66, False, ((3.6, -5.6, 4.2), (0, 0, -0.2))),
    ('voute_couloir', build_voute, 'Pierre', 66, False, ((1.6, -8.5, 2.6), (0, 0, 4.9))),
    ('pilier', build_pilier, 'Pierre', 40, True, ((3.0, -6.2, 2.7), (0, 0, 2.0))),
    ('torche_murale', build_torche, 'Fer', 50, False, ((0.95, -1.75, 2.05), (0, -0.4, 1.72))),
    ('brasero', build_brasero, 'Fer', 50, False, ((1.7, -2.5, 1.5), (0, 0, 0.62))),
    ('fenetre', build_fenetre, 'Pierre', 40, False, ((2.2, -5.5, 3.2), (0, 0, 3.3))),
    ('cierge', build_cierge, 'Fer', 50, False, ((1.5, -2.4, 1.6), (0, 0, 0.85))),
    ('autel', build_autel, 'Pierre', 40, True, ((2.6, -4.2, 1.9), (0, 0, 0.55))),
]

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.tool_settings.use_uv_select_sync = True


def only(objs, active=None):
    for o in scene.objects:
        o.select_set(o in objs)
    bpy.context.view_layer.objects.active = active or objs[0]


def make_object(name, fn, iron):
    FACES.clear(); EXTRA.clear(); CAP.clear()
    socket = fn()
    bm = bmesh.new()
    col_l = bm.loops.layers.float_color.new('Col')
    bm.loops.layers.uv.new('UVMap')
    kind_l = bm.faces.layers.int.new('kind')
    vmap = {}

    def vert(p):
        key = (round(p.x, 4), round(p.y, 4), round(p.z, 4))
        if key not in vmap:
            vmap[key] = bm.verts.new(p)
        return vmap[key]

    for pts, col, kind in FACES:
        vs = [vert(p) for p in pts]
        if len(set(vs)) != len(vs) or len(vs) < 3:
            continue
        try:
            f = bm.faces.new(vs)
        except ValueError:
            continue
        f[kind_l] = kind
        for lp in f.loops:
            lp[col_l] = (col, 1.0 if kind == 2 else 0.0, 1.0 if kind == 4 else 0.0, 1.0)
    if iron or name in ('pilier', 'fenetre', 'autel'):
        bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces if f[kind_l] != 2])
    bm.normal_update()
    for f in bm.faces:
        f.smooth = iron
    if iron:
        for e in bm.edges:
            if len(e.link_faces) == 2:
                e.smooth = e.calc_face_angle() < math.radians(40)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    scene.collection.objects.link(obj)
    return obj, socket


def unwrap(obj, angle):
    only([obj])
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=0.0, area_weight=0.0)
    bpy.ops.object.mode_set(mode='OBJECT')
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    uv = bm.loops.layers.uv.active
    kl = bm.faces.layers.int['kind']
    a3 = auv = 0.0
    for f in bm.faces:
        a3 += f.calc_area()
        pts = [lp[uv].uv for lp in f.loops]
        auv += abs(sum(pts[i].x * pts[(i + 1) % len(pts)].y - pts[(i + 1) % len(pts)].x * pts[i].y for i in range(len(pts)))) / 2
    k = math.sqrt(a3 / auv)
    for f in bm.faces:
        for lp in f.loops:
            lp[uv].uv *= k
        if f[kl] == 1:
            c = Vector((0, 0))
            for lp in f.loops:
                c += lp[uv].uv
            c /= len(f.loops)
            for lp in f.loops:
                lp[uv].uv = c + (lp[uv].uv - c) * 0.2
    bm.to_mesh(obj.data)
    bm.free()


def pack(objs):
    only(objs)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    try:
        bpy.ops.uv.pack_islands(rotate=True, margin=0.0035, shape_method='CONCAVE')
    except TypeError:
        bpy.ops.uv.pack_islands(rotate=True, margin=0.0035)
    bpy.ops.object.mode_set(mode='OBJECT')
    lo, hi = [9, 9], [-9, -9]
    for o in objs:
        for d in o.data.uv_layers.active.data:
            for i in (0, 1):
                lo[i] = min(lo[i], d.uv[i]); hi[i] = max(hi[i], d.uv[i])
    log('UV bornes', [round(x, 3) for x in lo + hi])


# ---------------------------------------------------------------- noeuds
def C(r, g, b):
    return (r, g, b, 1.0)


class NG:
    def __init__(s, nt):
        s.nt = nt

    def node(s, t, **kw):
        n = s.nt.nodes.new(t)
        for k, v in kw.items():
            setattr(n, k, v)
        return n

    def put(s, sock, v):
        if v is None:
            return
        if isinstance(v, bpy.types.NodeSocket):
            s.nt.links.new(v, sock)
        else:
            if sock.type == 'RGBA' and isinstance(v, (int, float)):
                v = (v, v, v, 1.0)
            sock.default_value = v

    def math(s, op, a, b=None, clamp=False):
        n = s.node('ShaderNodeMath', operation=op, use_clamp=clamp)
        s.put(n.inputs[0], a)
        s.put(n.inputs[1], b)
        return n.outputs[0]

    def sstep(s, lo, hi, v):
        n = s.node('ShaderNodeMapRange', interpolation_type='SMOOTHSTEP')
        s.put(n.inputs[0], v)
        n.inputs[1].default_value, n.inputs[2].default_value = lo, hi
        return n.outputs[0]

    def mix(s, fac, a, b, blend='MIX'):
        n = s.node('ShaderNodeMix', data_type='RGBA', blend_type=blend)
        s.put(n.inputs[0], fac)
        s.put(n.inputs[6], a)
        s.put(n.inputs[7], b)
        return n.outputs[2]

    def noise(s, vec, scale, detail=3.0, rough=0.5, w=None):
        n = s.node('ShaderNodeTexNoise', noise_dimensions='4D' if w is not None else '3D')
        s.put(n.inputs['Vector'], vec)
        if w is not None:
            s.put(n.inputs['W'], w)
        n.inputs['Scale'].default_value = scale
        n.inputs['Detail'].default_value = detail
        n.inputs['Roughness'].default_value = rough
        return n.outputs[0]

    def vor(s, vec, scale, feature='F1'):
        n = s.node('ShaderNodeTexVoronoi', feature=feature)
        s.put(n.inputs['Vector'], vec)
        n.inputs['Scale'].default_value = scale
        return n.outputs['Distance']

    def vmul(s, vec, xyz):
        n = s.node('ShaderNodeVectorMath', operation='MULTIPLY')
        s.put(n.inputs[0], vec)
        n.inputs[1].default_value = xyz
        return n.outputs[0]

    def bevel(s, radius, samples=6):
        n = s.node('ShaderNodeBevel', samples=samples)
        n.inputs['Radius'].default_value = radius
        return n.outputs[0]

    def edge(s, TN, radius, lo, hi):
        d = s.node('ShaderNodeVectorMath', operation='DOT_PRODUCT')
        s.put(d.inputs[0], s.bevel(radius))
        s.put(d.inputs[1], TN)
        return s.sstep(lo, hi, s.math('SUBTRACT', 1.0, d.outputs['Value']))

    def ao(s, dist, samples=8):
        n = s.node('ShaderNodeAmbientOcclusion', samples=samples, only_local=True)
        n.inputs['Distance'].default_value = dist
        return n.outputs['AO']

    def bump(s, height, dist, normal):
        n = s.node('ShaderNodeBump')
        n.inputs['Strength'].default_value = 1.0
        n.inputs['Distance'].default_value = dist
        s.put(n.inputs['Height'], height)
        s.put(n.inputs['Normal'], normal)
        return n.outputs[0]

    def rgb(s, r, g, b):
        n = s.node('ShaderNodeCombineColor')
        s.put(n.inputs[0], r); s.put(n.inputs[1], g); s.put(n.inputs[2], b)
        return n.outputs[0]


def bake_material(name, iron, grime=False, floor=False):
    mat = bpy.data.materials.new('cuisson_' + name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    g = NG(nt)
    out = g.node('ShaderNodeOutputMaterial')
    em = g.node('ShaderNodeEmission')
    df = g.node('ShaderNodeBsdfDiffuse')
    img = g.node('ShaderNodeTexImage')
    geo = g.node('ShaderNodeNewGeometry')
    P, TN = geo.outputs['Position'], geo.outputs['True Normal']
    att = g.node('ShaderNodeAttribute', attribute_name='Col')
    sc = g.node('ShaderNodeSeparateColor')
    nt.links.new(att.outputs['Color'], sc.inputs[0])
    tint, flag = sc.outputs[0], sc.outputs[1]
    res = {'mat': mat, 'nt': nt, 'out': out, 'em': em, 'df': df, 'img': img, 'emis': None}
    if not iron:
        W = g.math('MULTIPLY', tint, 53.0)
        n_big = g.noise(P, 0.9, 3, 0.5, W)
        n_tone = g.noise(P, 2.6, 4, 0.55, W)
        n_med = g.noise(P, 11.0, 6, 0.62, W)
        n_fine = g.noise(P, 75.0, 3, 0.6, W)
        n_pit = g.noise(P, 6.0, 2, 0.5, W)
        n_stain = g.noise(P, 1.3, 3, 0.5)
        n_lichen = g.noise(P, 8.0, 5, 0.65)
        n_crk = g.noise(P, 0.7, 2, 0.5)
        warp = g.node('ShaderNodeTexNoise')
        g.put(warp.inputs['Vector'], P)
        warp.inputs['Scale'].default_value = 3.0
        wv = g.node('ShaderNodeVectorMath', operation='MULTIPLY_ADD')
        g.put(wv.inputs[0], warp.outputs[1])
        wv.inputs[1].default_value = (0.22, 0.22, 0.22)
        g.put(wv.inputs[2], P)
        vd = g.vor(wv.outputs[0], 1.7, 'DISTANCE_TO_EDGE')
        crack = g.math('MULTIPLY', g.math('SUBTRACT', 1.0, g.sstep(0.0, 0.009, vd)), g.sstep(0.57, 0.64, n_crk))
        pits = g.sstep(0.60, 0.74, n_pit)
        h = g.math('MULTIPLY', n_big, 0.5)
        h = g.math('ADD', h, g.math('MULTIPLY', n_med, 0.75))
        h = g.math('ADD', h, g.math('MULTIPLY', n_fine, 0.14))
        h = g.math('SUBTRACT', h, g.math('MULTIPLY', crack, 1.3))
        h = g.math('SUBTRACT', h, g.math('MULTIPLY', pits, 0.55))
        normal = g.bump(h, 0.022, g.bevel(0.016))
        edge = g.math('MULTIPLY', g.edge(TN, 0.045, 0.01, 0.2), g.math('ADD', 0.35, g.sstep(0.35, 0.65, n_med)), clamp=True)
        ao_s = g.ao(0.10)
        cav = g.math('SUBTRACT', 1.0, g.sstep(0.45, 0.95, ao_s))
        ao_b = g.ao(1.0)
        c = g.mix(g.sstep(0.32, 0.68, n_tone), C(0.085, 0.082, 0.078), C(0.235, 0.230, 0.220))
        c = g.mix(1.0, c, tint, 'MULTIPLY')
        c = g.mix(g.math('MULTIPLY', g.sstep(0.52, 0.72, n_stain), 0.5), c, C(0.20, 0.15, 0.085))
        c = g.mix(1.0, c, g.math('ADD', 0.72, g.math('MULTIPLY', n_fine, 0.56)), 'MULTIPLY')
        c = g.mix(1.0, c, g.math('ADD', 0.80, g.math('MULTIPLY', n_med, 0.40)), 'MULTIPLY')
        c = g.mix(g.math('MULTIPLY', g.sstep(0.64, 0.72, n_lichen), 0.6), c, C(0.30, 0.33, 0.21))
        c = g.mix(g.math('MULTIPLY', pits, 0.45), c, C(0.04, 0.04, 0.04))
        c = g.mix(g.math('MULTIPLY', edge, 0.7), c, C(0.44, 0.43, 0.41))
        c = g.mix(g.math('MULTIPLY', crack, 0.8), c, C(0.02, 0.02, 0.02))
        c = g.mix(g.math('MULTIPLY', cav, 0.9), c, C(0.02, 0.018, 0.016))
        rough = g.math('SUBTRACT', 0.90, g.math('MULTIPLY', edge, 0.14))
        if grime or floor:
            sz = g.node('ShaderNodeSeparateXYZ')
            g.put(sz.inputs[0], P)
            if grime:
                zz = g.math('ADD', sz.outputs[2], g.math('MULTIPLY', g.math('SUBTRACT', n_big, 0.5), 1.2))
                gr = g.math('SUBTRACT', 1.0, g.sstep(0.0, 1.3, zz))
                c = g.mix(g.math('MULTIPLY', gr, 0.7), c, C(0.035, 0.05, 0.025))
                drip = g.sstep(0.58, 0.74, g.noise(g.vmul(P, (4.5, 4.5, 0.3)), 1.0, 3, 0.5))
                c = g.mix(g.math('MULTIPLY', drip, 0.38), c, C(0.02, 0.02, 0.02))
            else:
                gr = g.sstep(0.45, 0.70, n_stain)
                c = g.mix(g.math('MULTIPLY', gr, 0.45), c, C(0.03, 0.035, 0.03))
            rough = g.math('SUBTRACT', rough, g.math('MULTIPLY', gr, 0.38))
        occ = g.math('MULTIPLY', ao_b, g.math('SUBTRACT', 1.0, g.math('MULTIPLY', cav, 0.6)))
        orm = g.rgb(0.0, occ, rough)
        res.update(color=c, orm=orm, normal=normal)
    else:
        dimp = g.vor(P, 55.0, 'F1')
        n_med = g.noise(P, 28.0, 4, 0.6)
        n_r = g.noise(P, 9.0, 5, 0.6)
        n_f = g.noise(P, 120.0, 2, 0.5)
        cellc = g.vor(P, 24.0, 'F1')
        h_i = g.math('ADD', g.math('MULTIPLY', dimp, 0.6), g.math('MULTIPLY', n_med, 0.6))
        h_c = g.math('ADD', g.math('MULTIPLY', cellc, 5.0), g.math('MULTIPLY', n_f, 0.6))
        h = g.mix(flag, h_i, h_c)
        normal = g.bump(h, 0.004, g.bevel(0.005))
        edge = g.math('MULTIPLY', g.edge(TN, 0.012, 0.01, 0.22), g.math('ADD', 0.3, g.sstep(0.35, 0.65, n_med)), clamp=True)
        cav = g.math('SUBTRACT', 1.0, g.sstep(0.5, 0.95, g.ao(0.04)))
        rust = g.math('ADD', g.math('MULTIPLY', g.sstep(0.50, 0.66, n_r), 0.85), g.math('MULTIPLY', cav, 0.8), clamp=True)
        rcol = g.mix(n_f, C(0.11, 0.04, 0.018), C(0.27, 0.11, 0.04))
        c = g.mix(1.0, C(0.085, 0.085, 0.09), g.math('ADD', 0.7, g.math('MULTIPLY', n_med, 0.6)), 'MULTIPLY')
        c = g.mix(rust, c, rcol)
        c = g.mix(g.math('MULTIPLY', edge, 0.75), c, C(0.42, 0.41, 0.40))
        metal = g.math('SUBTRACT', 1.0, g.math('MULTIPLY', rust, 0.9), clamp=True)
        rough = g.math('SUBTRACT', g.math('ADD', 0.42, g.math('MULTIPLY', rust, 0.5)), g.math('MULTIPLY', edge, 0.14), clamp=True)
        # braises
        ccol = g.mix(g.sstep(0.35, 0.7, n_f), C(0.010, 0.009, 0.008), C(0.10, 0.095, 0.09))
        vde = g.vor(P, 24.0, 'DISTANCE_TO_EDGE')
        glow = g.math('MULTIPLY', g.math('SUBTRACT', 1.0, g.sstep(0.0, 0.16, vde)), g.sstep(0.30, 0.55, g.noise(P, 7.0, 2, 0.5)))
        ecol = g.mix(g.sstep(0.3, 0.9, glow), C(0.55, 0.035, 0.0), C(1.0, 0.42, 0.06))
        emis = g.mix(1.0, ecol, g.math('MULTIPLY', g.math('ADD', glow, 0.06), flag), 'MULTIPLY')
        c = g.mix(flag, c, ccol)
        metal = g.math('MULTIPLY', metal, g.math('SUBTRACT', 1.0, flag))
        rough = g.mix(flag, rough, 0.95)
        # cire du cierge
        wax = sc.outputs[2]
        wcol = g.mix(g.sstep(0.3, 0.7, g.noise(g.vmul(P, (30.0, 30.0, 4.0)), 1.0, 3, 0.5)), C(0.80, 0.70, 0.50), C(0.62, 0.52, 0.34))
        wcol = g.mix(g.math('MULTIPLY', cav, 0.6), wcol, C(0.25, 0.18, 0.10))
        c = g.mix(wax, c, wcol)
        metal = g.math('MULTIPLY', metal, g.math('SUBTRACT', 1.0, wax))
        rough = g.mix(wax, rough, 0.55)
        orm = g.rgb(metal, g.ao(0.5), rough)
        res.update(color=c, orm=orm, normal=normal, emis=emis)
    nt.links.new(res['normal'], df.inputs['Normal'])
    return res


def setup_cycles():
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = SAMPLES
    scene.cycles.use_denoising = False
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        for t in ('OPTIX', 'CUDA', 'HIP', 'ONEAPI'):
            try:
                prefs.compute_device_type = t
            except TypeError:
                continue
            prefs.refresh_devices()
            devs = [d for d in prefs.devices if d.type == t]
            if devs:
                for d in prefs.devices:
                    d.use = (d.type == t)
                scene.cycles.device = 'GPU'
                log('Cycles GPU', t, [d.name for d in devs])
                return
        log('Cycles CPU')
    except Exception as e:
        log('Cycles CPU (', e, ')')


def bake(obj, bm_, which, image, margin):
    nt = bm_['nt']
    bm_['img'].image = image
    for n in nt.nodes:
        n.select = False
    bm_['img'].select = True
    nt.nodes.active = bm_['img']
    for l in list(bm_['out'].inputs[0].links):
        nt.links.remove(l)
    if which == 'normal':
        nt.links.new(bm_['df'].outputs[0], bm_['out'].inputs[0])
        bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', margin=margin, use_clear=False)
    else:
        for l in list(bm_['em'].inputs[0].links):
            nt.links.remove(l)
        nt.links.new(bm_[which], bm_['em'].inputs[0])
        nt.links.new(bm_['em'].outputs[0], bm_['out'].inputs[0])
        bpy.ops.object.bake(type='EMIT', margin=margin, use_clear=False)


def new_image(name, size, data, color):
    img = bpy.data.images.new(name, size, size, alpha=False, is_data=data)
    img.generated_color = color
    px = np.empty(size * size * 4, np.float32)
    px.reshape(-1, 4)[:] = color
    img.pixels.foreach_set(px)
    return img


def save(img, name):
    img.filepath_raw = os.path.join(TEX, name)
    img.file_format = 'PNG'
    img.save()


def make_mask(orm, size, name):
    a = np.empty(size * size * 4, np.float32)
    orm.pixels.foreach_get(a)
    a = a.reshape(-1, 4)
    m = np.empty_like(a)
    m[:, 0] = a[:, 0]; m[:, 1] = a[:, 1]; m[:, 2] = 0.0; m[:, 3] = 1.0 - a[:, 2]
    img = bpy.data.images.new(name, size, size, alpha=True, is_data=True)
    img.alpha_mode = 'CHANNEL_PACKED'
    img.pixels.foreach_set(m.ravel())
    save(img, name + '.png')
    return img


def final_material(name, alb, nrm, orm, emis):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = alb
    nt.links.new(t.outputs[0], bsdf.inputs['Base Color'])
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = nrm
    nm = nt.nodes.new('ShaderNodeNormalMap')
    nt.links.new(t.outputs[0], nm.inputs['Color'])
    nt.links.new(nm.outputs[0], bsdf.inputs['Normal'])
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = orm
    sp = nt.nodes.new('ShaderNodeSeparateColor')
    nt.links.new(t.outputs[0], sp.inputs[0])
    nt.links.new(sp.outputs[0], bsdf.inputs['Metallic'])
    inv = nt.nodes.new('ShaderNodeMath')
    inv.operation = 'SUBTRACT'
    inv.inputs[0].default_value = 1.0
    nt.links.new(t.outputs['Alpha'], inv.inputs[1])
    nt.links.new(inv.outputs[0], bsdf.inputs['Roughness'])
    if emis:
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = emis
        nt.links.new(t.outputs[0], bsdf.inputs['Emission Color'])
        bsdf.inputs['Emission Strength'].default_value = 12.0
    return mat


def add_mirror(me):
    bm = bmesh.new()
    bm.from_mesh(me)
    kl = bm.faces.layers.int['kind']
    uv = bm.loops.layers.uv.active
    key = lambda x, y, z: (round(x, 4), round(y, 4), round(z, 4))
    vm = {key(*v.co): v for v in bm.verts}
    for f in [f for f in bm.faces if f[kl] == 3]:
        vs = []
        for v in reversed(f.verts):
            k = key(v.co.x, -v.co.y, v.co.z)
            if k not in vm:
                vm[k] = bm.verts.new((v.co.x, -v.co.y, v.co.z))
            vs.append(vm[k])
        try:
            nf = bm.faces.new(vs)
        except ValueError:
            continue
        nf.smooth = False
        n = len(vs)
        for i, lp in enumerate(nf.loops):
            lp[uv].uv = f.loops[n - 1 - i][uv].uv
    bm.to_mesh(me)
    bm.free()


# ================================================================ fabrication
objs = {}
for name, fn, matname, angle, grime, cam in PIECES:
    obj, socket = make_object(name, fn, matname == 'Fer')
    unwrap(obj, angle)
    objs[name] = dict(obj=obj, socket=socket, mat=matname, grime=grime, cam=cam)
stone_objs = [d['obj'] for d in objs.values() if d['mat'] == 'Pierre']
iron_objs = [d['obj'] for d in objs.values() if d['mat'] == 'Fer']
pack(stone_objs)
pack(iron_objs)
log('geometrie + UV ok')

setup_cycles()
IMG = {
    'Pierre': dict(alb=new_image('Pierre_Albedo', S, False, (0.1, 0.1, 0.1, 1)),
                   nrm=new_image('Pierre_Normal', S, True, (0.5, 0.5, 1, 1)),
                   orm=new_image('Pierre_ORM', S, True, (0, 1, 0.9, 1)), emis=None, size=S),
    'Fer': dict(alb=new_image('Fer_Albedo', S_FER, False, (0.08, 0.08, 0.08, 1)),
                nrm=new_image('Fer_Normal', S_FER, True, (0.5, 0.5, 1, 1)),
                orm=new_image('Fer_ORM', S_FER, True, (1, 1, 0.5, 1)),
                emis=new_image('Fer_Emission', S_FER, False, (0, 0, 0, 1)), size=S_FER),
}
REUSE = len(argv) > 4 and argv[4] == 'reuse'
for name, d in ([] if REUSE else list(objs.items())):
    obj = d['obj']
    iron = d['mat'] == 'Fer'
    bmat = bake_material(name, iron, grime=d['grime'], floor=(name == 'dalle_sol'))
    obj.data.materials.append(bmat['mat'])
    for o in scene.objects:
        o.hide_render = (o is not obj)
    only([obj])
    im = IMG[d['mat']]
    margin = max(3, im['size'] // 400)
    for which, key in (('color', 'alb'), ('normal', 'nrm'), ('orm', 'orm'), ('emis', 'emis')):
        if im[key] is None or bmat.get(which) is None:
            continue
        bake(obj, bmat, which, im[key], margin)
    log('cuit', name)

def load(fname, data):
    img = bpy.data.images.load(os.path.join(TEX, fname))
    if data:
        img.colorspace_settings.name = 'Non-Color'
    img.alpha_mode = 'CHANNEL_PACKED'
    return img


for mname, im in IMG.items():
    if REUSE:
        im['alb'] = load(mname + '_Albedo.png', False)
        im['nrm'] = load(mname + '_Normal.png', True)
        im['mask'] = load(mname + '_Mask.png', True)
        if im['emis']:
            im['emis'] = load(mname + '_Emission.png', False)
        continue
    save(im['alb'], mname + '_Albedo.png')
    save(im['nrm'], mname + '_Normal.png')
    im['mask'] = make_mask(im['orm'], im['size'], mname + '_Mask')
    if im['emis']:
        save(im['emis'], mname + '_Emission.png')
log('textures pretes')

FINAL = {m: final_material(m, im['alb'], im['nrm'], im['mask'], im['emis']) for m, im in IMG.items()}
for name, d in objs.items():
    obj = d['obj']
    me = obj.data
    if d['mat'] == 'Pierre' and name in ('mur_droit', 'mur_arche'):
        add_mirror(me)
    for a in ('Col', 'kind'):
        if a in me.attributes:
            me.attributes.remove(me.attributes[a])
    me.materials.clear()
    me.materials.append(FINAL[d['mat']])
    exp = [obj]
    if d['socket']:
        emp = bpy.data.objects.new('socket_flamme', None)
        emp.empty_display_size = 0.05
        emp.location = d['socket']
        emp.parent = obj
        scene.collection.objects.link(emp)
        exp.append(emp)
        d['emp'] = emp
    me.calc_loop_triangles()
    only(exp, obj)
    path = os.path.join(OUT, name + '.fbx')
    if not REUSE:
      bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             object_types={'MESH', 'EMPTY'}, mesh_smooth_type='OFF', bake_anim=False,
                             add_leaf_bones=False, path_mode='RELATIVE', use_tspace=False)
    log('RESULT', name, 'tris=%d' % len(me.loop_triangles), 'verts=%d' % len(me.vertices))

# le verre du vitrail : une simple plaque en lancette, sa propre matiere (texture faite par vitrail.py)
if not REUSE:
    gpts = list(reversed(lancet(WIN_A, WIN_Z0)))
    ghei = WIN_H + 2 * WIN_A * math.sin(math.radians(60))
    gme = bpy.data.meshes.new('vitrail')
    gme.from_pydata([(x, -0.275, z) for x, z in gpts], [], [list(range(len(gpts)))])
    guv = gme.uv_layers.new(name='UVMap')
    for i, (x, z) in enumerate(gpts):
        guv.data[i].uv = ((x + WIN_A) / (2 * WIN_A), (z - WIN_Z0) / ghei)
    gmat = bpy.data.materials.new('Vitrail')
    gme.materials.append(gmat)
    gobj = bpy.data.objects.new('vitrail', gme)
    scene.collection.objects.link(gobj)
    only([gobj])
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'vitrail.fbx'), use_selection=True,
                             apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, object_types={'MESH'}, mesh_smooth_type='OFF',
                             bake_anim=False, add_leaf_bones=False)
    gobj.hide_render = True
    log('RESULT vitrail')

# ================================================================ rendus
scene.render.engine = 'BLENDER_EEVEE'
try:
    scene.eevee.taa_render_samples = 64
    scene.eevee.use_raytracing = True
except Exception as e:
    log('eevee', e)
try:
    scene.view_settings.look = 'AgX - Medium High Contrast'
except Exception as e:
    log('look', e)
world = bpy.data.worlds.new('nuit')
world.use_nodes = True
bg = next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND')
bg.inputs[0].default_value = (0.010, 0.016, 0.034, 1)
scene.world = world
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
scene.collection.objects.link(cam)
scene.camera = cam
moon = bpy.data.objects.new('lune', bpy.data.lights.new('lune', 'SUN'))
moon.data.color = (0.55, 0.70, 1.0)
moon.rotation_euler = (math.radians(58), 0, math.radians(38))
scene.collection.objects.link(moon)
fire = bpy.data.objects.new('feu', bpy.data.lights.new('feu', 'POINT'))
fire.data.color = (1.0, 0.50, 0.20)
fire.data.shadow_soft_size = 0.1
scene.collection.objects.link(fire)
gm = bpy.data.meshes.new('sol')
gm.from_pydata([(-60, -60, 0), (60, -60, 0), (60, 60, 0), (-60, 60, 0)], [], [(0, 1, 2, 3)])
ground = bpy.data.objects.new('sol', gm)
gmat = bpy.data.materials.new('sol')
gmat.use_nodes = True
gb = next(n for n in gmat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
gb.inputs['Base Color'].default_value = (0.03, 0.03, 0.035, 1)
gb.inputs['Roughness'].default_value = 1.0
gm.materials.append(gmat)
scene.collection.objects.link(ground)


FLAMES = []


def flame_quad(s, frame):
    key = 'flamme_mat'
    mat = bpy.data.materials.get(key)
    if mat is None:
        mat = bpy.data.materials.new(key)
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        out = nt.nodes.new('ShaderNodeOutputMaterial')
        tex = nt.nodes.new('ShaderNodeTexImage')
        tex.image = bpy.data.images.load(os.path.join(TEX, 'Flamme_Flipbook.png'))
        em = nt.nodes.new('ShaderNodeEmission')
        em.inputs['Strength'].default_value = 3.2
        tr = nt.nodes.new('ShaderNodeBsdfTransparent')
        mx = nt.nodes.new('ShaderNodeMixShader')
        nt.links.new(tex.outputs['Color'], em.inputs['Color'])
        nt.links.new(tex.outputs['Alpha'], mx.inputs[0])
        nt.links.new(tr.outputs[0], mx.inputs[1])
        nt.links.new(em.outputs[0], mx.inputs[2])
        nt.links.new(mx.outputs[0], out.inputs[0])
        for attr, val in (('surface_render_method', 'BLENDED'), ('blend_method', 'BLEND')):
            try:
                setattr(mat, attr, val)
            except Exception:
                pass
    c, r = frame % 8, frame // 8
    u0, u1, v0, v1 = c / 8, (c + 1) / 8, 1 - (r + 1) / 8, 1 - r / 8
    me = bpy.data.meshes.new('flamme_q')
    me.from_pydata([(-s / 2, 0, 0), (s / 2, 0, 0), (s / 2, 0, s), (-s / 2, 0, s)], [], [(0, 1, 2, 3)])
    uvl = me.uv_layers.new(name='UVMap')
    for i, uv in enumerate([(u0, v0), (u1, v0), (u1, v1), (u0, v1)]):
        uvl.data[i].uv = uv
    me.materials.append(mat)
    return bpy.data.objects.new('flamme_q', me)


def aim(loc, tgt, lens):
    for fl in FLAMES:
        fl.rotation_euler = (0, 0, math.atan2(loc[0] - fl.location.x, -(loc[1] - fl.location.y)))
    cam.location = loc
    cam.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    cam.data.lens = lens


def render(path, w, h):
    scene.render.resolution_x, scene.render.resolution_y = w, h
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    log('RENDERED', path)


for i, (name, d) in enumerate(objs.items()):
    for o in scene.objects:
        o.hide_render = o.type in ('MESH', 'EMPTY') and o not in (d['obj'], ground)
    loc, tgt = Vector(d['cam'][0]), Vector(d['cam'][1])
    dist = (loc - tgt).length
    aim(loc, tgt, 35)
    ground.location.z = min(min(v.co.z for v in d['obj'].data.vertices), 0) - 0.002
    moon.data.energy = 2.5
    fire.data.energy = 28 * dist * dist
    fire.location = tgt + Vector((-0.50 * dist, -0.34 * dist, 0.10 * dist))
    if name == 'voute_couloir':
        fire.location = (0.6, -1.6, 4.05)
        fire.data.energy = 500
    render(os.path.join(PREV, '%d_%s.png' % (i + 1, name)), 1280, 960)

# scene de couloir assemblee
for o in list(scene.objects):
    if o.type in ('MESH', 'EMPTY') and o is not ground:
        o.hide_render = True
fire.hide_render = True
ground.location.z = -0.21
fmat = bpy.data.materials.new('flamme')
fmat.use_nodes = True
fb = next(n for n in fmat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
fb.inputs['Base Color'].default_value = (0, 0, 0, 1)
fb.inputs['Emission Color'].default_value = (1.0, 0.36, 0.06, 1)
fb.inputs['Emission Strength'].default_value = 22.0
fbm = bmesh.new()
bmesh.ops.create_icosphere(fbm, subdivisions=3, radius=1.0)
for _f in fbm.faces:
    _f.smooth = True
fme = bpy.data.meshes.new('flamme')
fbm.to_mesh(fme)
fbm.free()
fme.materials.append(fmat)


def place(name, x, y, rot=0.0, light=0.0):
    d = objs[name]
    o = bpy.data.objects.new('i_' + name, d['obj'].data)
    o.location = (x, y, 0)
    o.rotation_euler = (0, 0, math.radians(rot))
    scene.collection.objects.link(o)
    if light:
        p = Matrix.Rotation(math.radians(rot), 4, 'Z') @ Vector(d['socket']) + Vector((x, y, 0))
        L = bpy.data.objects.new('l', bpy.data.lights.new('l', 'POINT'))
        L.data.color = (1.0, 0.42, 0.13)
        L.data.energy = light
        L.data.shadow_soft_size = 0.06
        L.location = p + Vector((0, 0, 0.14))
        scene.collection.objects.link(L)
        s = 0.42 if name == 'torche_murale' else 1.0
        fl = flame_quad(s, (len(FLAMES) * 11 + 3) % 64)
        fl.location = p + Vector((0, 0, -0.05 * s))
        fl.visible_shadow = False
        scene.collection.objects.link(fl)
        FLAMES.append(fl)
        L.location = p + Vector((0, 0, 0.3 * s))


for y in (-8, -4, 0, 4, 8, 12, 16):
    for x in (-4, 0, 4):
        if x == 0 or y < 0 or y > 10:
            place('dalle_sol', x, y)
for y in (0, 4, 8):
    place('mur_droit', -2, y, 90)
    place('mur_droit', 2, y, -90)
    place('voute_couloir', 0, y)
place('mur_arche', 0, 10)
for x in (-4, 4):
    place('mur_droit', x, -2)
    place('mur_droit', x, 10)
place('pilier', -2.0, -2.75)
place('pilier', 2.0, -2.75)
place('torche_murale', -2, 0, 90, 160)
place('torche_murale', 2, 4, -90, 160)
place('torche_murale', -2, 8, 90, 160)
place('torche_murale', -4, -2, 0, 140)
place('torche_murale', 4, -2, 0, 140)
place('brasero', 3.4, -5.0, 0, 450)
place('brasero', -3.4, -5.0, 0, 450)
back = bpy.data.objects.new('lune2', bpy.data.lights.new('lune2', 'POINT'))
back.data.color = (0.45, 0.62, 1.0)
back.data.energy = 2500
back.location = (1.5, 15, 5)
scene.collection.objects.link(back)
moon.data.energy = 0.9
moon.rotation_euler = (math.radians(55), 0, math.radians(25))
aim((-1.3, -10.8, 1.55), (0.2, 3, 2.8), 24)
render(os.path.join(PREV, 'scene_couloir.png'), 1920, 1080)
aim((0.5, -1.4, 1.6), (-1.9, 0.6, 1.75), 28)
render(os.path.join(PREV, 'scene_detail.png'), 1920, 1080)
log('FINI')
