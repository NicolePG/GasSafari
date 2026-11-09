"""Fondos W5 · El Gran Regreso (cañón al atardecer). Paleta de docs/04: dorado #FFB703, rojo #9C3D1C, naranja #FB8500.
blender --background --python backdrops_w5.py [-- Mid_Mesas Far_SlideTower ...]
Salida: Tools/Export/Backdrops/W5/W5_<Capa>_<Nombre>.fbx. Suelo / línea de agua en z = 0."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backdrop_lib as bd
from backdrop_lib import B, M, E, Vector

W = "W5"


def pal():
    S = [M("#7A2E18"), M("#9C3D1C"), M("#B5522A"), M("#D06A33"), M("#E88A3E"), M("#F2B36B")]
    return dict(
        S=S, dark=M("#5E2214"), cream=M("#E8C48A"), sand=M("#D9A05B"), gold=M("#FFB703"), orange=M("#FB8500"),
        shrub=M("#6B7A2E"), shrub2=M("#8A8F3A"), trunk=M("#5A3A22"), cactus=M("#5E8A3A"), sun=E("#FFC83A", 2.6),
        slide=M("#2EC4B6"), slide2=M("#FB8500"), steel=M("#E8DCC8"), steel2=M("#7A6A5A"), red=E("#FF4B3E", 2.6),
        blue=M("#3A6EA5"), water=M("#2E8FA0"))


def strata_mat(C, z, lo, hi):
    i = int((z - lo) / max(hi - lo, 1) * 5.99 + 0.4 * math.sin(z * 0.7))
    return C["S"][max(0, min(5, i))]


def mesa(b, C, x, y, w, d, h, rnd, layers=6, taper=0.96):
    z = -1.0
    lh = (h + 1) / layers
    ww, dd = w, d
    for i in range(layers):
        sh = lh * rnd.uniform(0.7, 1.3)
        b.box(x + rnd.uniform(-1, 1), y + rnd.uniform(-1, 1), z, ww, dd, sh, C["S"][min(5, i if i < 5 else 5)] if i % 2 == 0 else C["S"][max(0, i - 1)],
              taper=taper, top_m=None)
        z += sh
        ww *= rnd.uniform(0.93, 1.02)
        dd *= rnd.uniform(0.93, 1.02)
    b.box(x, y, z - 0.1, ww * 0.96, dd * 0.96, 1.2, C["cream"], taper=0.96, top_m=C["sand"])
    return z


def wall_sheet(b, C, x0, x1, ydepth, hmax, nx, ny, seed, steps=4, tall=1.0):
    """Pared de cañón con terrazas; el color va por franjas de altura (estratos)."""
    def fn(x, y):
        t = (y + ydepth) / (2 * ydepth)
        k = int(t * steps * 1.0 + 0.5 * math.sin(x * 0.04 + seed))
        base = min(1.0, max(0.0, (t * steps - int(t * steps)) * 3.5))
        terr = (math.floor(t * steps) + smooth3(base)) / steps
        r = 0.6 + 0.4 * (0.5 + 0.5 * bd.fbm(x * 0.03, seed, seed, 3))
        edge = bd.smooth((x1 - abs(x)) / 40)
        return (hmax * terr * r * tall) * edge - 1.0

    b.sheet(x0, x1, -ydepth, ydepth, nx, ny, fn, lambda x, y, z, s: strata_mat(C, z, 0, hmax * tall))


def smooth3(t):
    return bd.smooth(t)


# ---------------------------------------------------------------------------
# Capa media
# ---------------------------------------------------------------------------
def mesas(b, rnd):
    C = pal()
    for i, (x, y) in enumerate(((-48, 6), (-6, -10), (36, 8), (62, -14))):
        mesa(b, C, x, y, rnd.uniform(26, 40), rnd.uniform(22, 32), rnd.uniform(36, 62), rnd, layers=rnd.randint(5, 7))
    for i in range(4):
        mesa(b, C, rnd.uniform(-66, 66), rnd.uniform(-28, -18), rnd.uniform(8, 14), rnd.uniform(8, 12), rnd.uniform(8, 18), rnd, layers=3)
    for i in range(30):
        bd.rock_mass(b, rnd.uniform(-70, 70), rnd.uniform(-30, 30), rnd.uniform(2, 6), rnd.uniform(1.5, 4), C["S"][rnd.randint(1, 3)], seed=i, rings=3, n=6)
    for i in range(14):
        b.blob((rnd.uniform(-66, 66), rnd.uniform(-30, 30), 0.6), (1.8, 1.5, 1.0), C["shrub"], n=5, rings=2, jit=0.25, seed=i)


def canyon_wall_a(b, rnd):
    C = pal()
    wall_sheet(b, C, -66, 66, 30, 62, 44, 16, 1.0, steps=4)
    for i in range(10):
        bd.rock_mass(b, rnd.uniform(-60, 60), rnd.uniform(-34, -26), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["S"][rnd.randint(1, 3)], seed=i, rings=3, n=6)


def canyon_wall_b(b, rnd):
    C = pal()
    wall_sheet(b, C, -66, 66, 28, 80, 44, 16, 6.0, steps=5, tall=1.0)
    for i in range(6):
        x = rnd.uniform(-55, 55)
        mesa(b, C, x, -22, rnd.uniform(8, 14), rnd.uniform(8, 12), rnd.uniform(10, 22), rnd, layers=3)
    for i in range(8):
        bd.rock_mass(b, rnd.uniform(-60, 60), rnd.uniform(-34, -26), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["S"][rnd.randint(1, 3)], seed=i + 5, rings=3, n=6)


def arch_path(b, C, cx, cy, R, thick, width, z0, nseg, mats, a0=0.0, a1=math.pi):
    rings = []
    for k in range(nseg + 1):
        th = a0 + (a1 - a0) * k / nseg
        c = Vector((cx + R * math.cos(th), cy, z0 + R * math.sin(th) * 1.0))
        u = Vector((math.cos(th), 0, math.sin(th)))
        t = thick * (1.0 + 0.25 * math.sin(k * 1.7))
        rings.append((c, [c + u * t / 2 + Vector((0, 1, 0)) * width / 2, c + u * t / 2 - Vector((0, 1, 0)) * width / 2,
                         c - u * t / 2 - Vector((0, 1, 0)) * width / 2, c - u * t / 2 + Vector((0, 1, 0)) * width / 2]))
    for k in range(nseg):
        c0, r0 = rings[k]
        c1, r1 = rings[k + 1]
        for i in range(4):
            a, bb, cc, dd = r0[i], r0[(i + 1) % 4], r1[(i + 1) % 4], r1[i]
            ctr = (a + bb + cc + dd) / 4
            out = ctr - (c0 + c1) / 2
            b.facing((a, bb, cc, dd), mats[k % len(mats)], out)
    return rings


def natural_arch(b, rnd):
    C = pal()
    for s, col in ((-1, 0), (1, 1)):
        mesa(b, C, s * 40, 0, 30, 26, 34, rnd, layers=5)
    arch_path(b, C, 0, 0, 40, 12, 22, 12, 14, [C["S"][2], C["S"][3], C["S"][4]], 0.0, math.pi)
    for i in range(10):
        bd.rock_mass(b, rnd.uniform(-70, 70), rnd.uniform(-22, 22), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["S"][rnd.randint(1, 3)], seed=i, rings=3, n=6)


def hoodoos(b, rnd):
    C = pal()
    for i in range(16):
        x, y = rnd.uniform(-60, 60), rnd.uniform(-26, 26)
        h = rnd.uniform(14, 40)
        r = rnd.uniform(2.2, 4.4)
        z = 0.0
        for k in range(4):
            lh = h / 4
            col = C["S"][(i + k) % 6]
            b.cyl(x, y, z, r * (1.0 - 0.15 * k), r * (1.0 - 0.15 * k) * rnd.uniform(0.8, 1.05), lh, col, n=7)
            z += lh
        b.blob((x, y, z + 0.4), (r * 1.7, r * 1.7, r * 0.7), C["S"][2], n=7, rings=3, jit=0.15, seed=i)
        if b.tris() > 4400:
            break


def terraces(b, rnd):
    C = pal()
    z = 0.0
    w, d = 120, 56
    for i in range(7):
        h = rnd.uniform(4, 7)
        b.box(0, -i * 4.0, z - 1, w, d - i * 8, h + 1, C["S"][i % 6], taper=0.97, top_m=C["sand"] if i % 2 else C["cream"])
        z += h
        w *= 0.88
    for i in range(40):
        x = rnd.uniform(-48, 48)
        b.blob((x, rnd.uniform(-26, -8), rnd.uniform(1, 30)), (1.6, 1.4, 1.0), C["shrub"], n=5, rings=2, jit=0.2, seed=i)
    for i in range(18):
        bd.rock_mass(b, rnd.uniform(-55, 55), rnd.uniform(-28, -14), rnd.uniform(2, 5), rnd.uniform(1.5, 3.5), C["S"][rnd.randint(1, 4)], seed=i, rings=3, n=6, z0=rnd.uniform(0, 24))
    for i in range(10):
        x = rnd.uniform(-45, 45)
        b.cyl(x, -30, 0, 4, 3.4, rnd.uniform(6, 14), C["S"][i % 6], n=6)


def flora(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 64, 2.5, C["sand"], C["sand"], seed=1, n=12, rings=2, jit=0.12, ry=28)
    for i in range(8):
        x, y = rnd.uniform(-55, 55), rnd.uniform(-22, 22)
        h = rnd.uniform(9, 14)
        b.prism_between((x, y, 1.5), (x + 1, y, 1.5 + h * 0.8), h * 0.05, h * 0.035, C["trunk"], n=4, caps=False)
        b.blob((x + 1, y, 1.5 + h * 0.9), (h * 0.55, h * 0.55, h * 0.14), C["shrub2"], n=8, rings=3, jit=0.2, seed=i)
    for i in range(10):
        x, y = rnd.uniform(-55, 55), rnd.uniform(-22, 22)
        h = rnd.uniform(5, 10)
        b.cyl(x, y, 1.5, h * 0.09, h * 0.08, h, C["cactus"], n=6)
        b.blob((x, y, 1.5 + h), h * 0.09, C["cactus"], n=6, rings=2, jit=0)
        for sd in (-1, 1):
            b.prism_between((x, y, 1.5 + h * 0.4), (x + sd * h * 0.35, y, 1.5 + h * 0.4), h * 0.05, h * 0.05, C["cactus"], n=4, caps=False)
            b.cyl(x + sd * h * 0.35, y, 1.5 + h * 0.4, h * 0.05, h * 0.045, h * 0.35, C["cactus"], n=5)
    for i in range(16):
        b.blob((rnd.uniform(-58, 58), rnd.uniform(-24, 24), 1.8), (1.8, 1.6, 0.9), C["shrub"], n=5, rings=2, jit=0.25, seed=i)
    for i in range(9):
        bd.rock_mass(b, rnd.uniform(-58, 58), rnd.uniform(-24, 24), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["S"][rnd.randint(1, 3)], seed=i, rings=3, n=6, z0=1.5)


def rock_bridge(b, rnd):
    C = pal()
    mesa(b, C, -44, 0, 28, 24, 40, rnd, layers=5)
    mesa(b, C, 44, 0, 28, 24, 36, rnd, layers=5)
    # puente natural entre mesas
    for k in range(9):
        t = k / 8
        x = -30 + 60 * t
        z = 33 + 6 * math.sin(t * math.pi)
        b.box(x, 0, z, 8.5, 9 + 1.4 * math.sin(k * 2.1), 3.0 + 1.4 * math.sin(math.pi * t), C["S"][3 + k % 2], taper=0.9, top_m=C["sand"])
    for i in range(12):
        bd.rock_mass(b, rnd.uniform(-62, 62), rnd.uniform(-26, 26), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["S"][rnd.randint(1, 3)], seed=i, rings=3, n=6)


# ---------------------------------------------------------------------------
# Capa lejana
# ---------------------------------------------------------------------------
def far_walls_a(b, rnd):
    C = pal()
    wall_sheet(b, C, -240, 240, 40, 120, 60, 12, 2.0, steps=4)


def far_walls_b(b, rnd):
    C = pal()
    wall_sheet(b, C, -240, 240, 40, 170, 60, 12, 9.0, steps=6, tall=1.0)


def far_mesas(b, rnd):
    C = pal()
    x = -215
    for i in range(6):
        w = rnd.uniform(50, 90)
        mesa(b, C, x + w / 2, rnd.uniform(-8, 8), w, rnd.uniform(36, 60), rnd.uniform(60, 130), rnd, layers=5, taper=0.94)
        x += w * 0.85 + 10


def far_arches(b, rnd):
    C = pal()
    for i, x in enumerate((-150, 30, 190)):
        mesa(b, C, x - 55, 0, 40, 34, 70, rnd, layers=4)
        mesa(b, C, x + 55, 0, 40, 34, 64, rnd, layers=4)
        arch_path(b, C, x, 0, 55, 18, 30, 60, 10, [C["S"][2], C["S"][3]], 0.0, math.pi)


def slide_tower(b, rnd):
    C = pal()
    H = 150.0
    # base rocosa y torre
    bd.rock_mass(b, 0, 0, 90, 24, C["S"][1], C["S"][3], seed=3, n=9, rings=3, ry=70)
    b.cyl(0, 0, 10, 9, 6, H - 20, C["steel"], n=8)
    # hélice del tobogán
    turns = 3.2
    N = int(turns * 14)
    prev = None
    for i in range(N + 1):
        t = i / N
        a = t * turns * 2 * math.pi
        z = H - 8 - t * (H - 46)
        r = 24 - 6 * t
        p = Vector((math.cos(a) * r, math.sin(a) * r, z))
        if prev is not None:
            b.prism_between(prev, p, 3.4, 3.4, C["slide"] if (i // 3) % 2 == 0 else C["slide2"], n=6, caps=False)
        prev = p
    # tramo final hacia el agua
    b.prism_between(prev, Vector((60, 20, 4)), 3.4, 3.0, C["slide"], n=6, caps=False)
    for k in range(5):
        a = k / 5 * 6.28
        b.prism_between((math.cos(a) * 24, math.sin(a) * 24, 10), (math.cos(a) * 20, math.sin(a) * 20, H - 40), 0.9, 0.8, C["steel2"], n=4, caps=False)
    b.cyl(0, 0, H - 14, 26, 26, 3, C["steel"], n=12)
    b.cyl(0, 0, H - 11, 15, 12, 14, C["orange"], n=10)
    b.blob((0, 0, H + 6), (14, 14, 12), C["gold"], n=10, rings=3, jit=0, flat_bottom=0.0)
    b.blob((0, 0, H + 20), 2.2, C["red"], n=4, rings=2, jit=0)


def sunset_ridge(b, rnd):
    C = pal()
    # disco solar detrás de la cresta
    ang = [i / 24 * 2 * math.pi for i in range(24)]
    b.poly([Vector((math.cos(a) * 46, 70, 74 + math.sin(a) * 46)) for a in reversed(ang)], C["sun"])
    # crestas en capas (de atrás hacia adelante)
    for layer, (yy, hh, col) in enumerate(((50, 120, 0), (20, 95, 1), (-10, 70, 2))):
        b.sheet(-240, 240, yy - 14, yy + 14, 60, 3, lambda x, y, hh=hh, layer=layer: (hh * (0.35 + 0.65 * (0.5 + 0.5 * bd.fbm(x * 0.012, layer * 4, layer * 4 + 1, 3)))
                * (0.45 + 0.55 * abs(math.sin(x * 0.02 + layer))) * bd.smooth((14 - abs(y - yy)) / 10)) - 1.0 if False else
                (hh * (0.25 + 0.75 * (0.5 + 0.5 * bd.fbm(x * 0.013, layer * 4, layer * 4 + 1, 3))) * bd.smooth((14 - abs(y - yy)) / 10)
                 * bd.smooth((240 - abs(x)) / 50)) - 1.0,
                lambda x, y, z, s, col=col: C["S"][col] if z < 40 else C["S"][col + 1])


# ---------------------------------------------------------------------------
# Suelo
# ---------------------------------------------------------------------------
def gm():
    return [M("#A5452A"), M("#B85A33"), M("#D9A05B")]


def ground_a(b, rnd):
    bd.ground_tile(b, gm(), 1.0, amp=1.0, patch=0.5)


def ground_b(b, rnd):
    bd.ground_tile(b, gm(), 5.0, amp=1.7, freq=0.017, patch=0.55)


def ground_skirt(b, rnd):
    bd.ground_skirt(b, [M("#8E3A22"), M("#A5452A")])


def horizon(b, rnd):
    bd.horizon_ring(b, [M("#7A2E18"), M("#C0562B")], hmin=45, hmax=170, seed=8.1, pointy=0.35)


PIECES = [
    ("Mid", "Mesas", mesas), ("Mid", "CanyonWall_A", canyon_wall_a), ("Mid", "CanyonWall_B", canyon_wall_b),
    ("Mid", "NaturalArch", natural_arch), ("Mid", "Hoodoos", hoodoos), ("Mid", "Terraces", terraces),
    ("Mid", "Flora", flora), ("Mid", "RockBridge", rock_bridge),
    ("Far", "WallsA", far_walls_a), ("Far", "WallsB", far_walls_b), ("Far", "Mesas", far_mesas),
    ("Far", "Arches", far_arches), ("Far", "SlideTower", slide_tower), ("Far", "SunsetRidge", sunset_ridge),
    ("Ground", "TileA", ground_a), ("Ground", "TileB", ground_b), ("Ground", "Skirt", ground_skirt),
    ("Ground", "HorizonRing", horizon),
]

if __name__ == "__main__":
    bd.build_all(W, PIECES, only=bd.script_args())
