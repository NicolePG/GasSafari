"""Fondos W3 · Selva de Madagascar (playa y selva). Paleta de docs/04: verde #2D6A4F, naranja #F77F00, amarillo.
blender --background --python backdrops_w3.py [-- Mid_Baobabs Far_Birds ...]
Salida: Tools/Export/Backdrops/W3/W3_<Capa>_<Nombre>.fbx. Suelo / línea de agua en z = 0."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backdrop_lib as bd
from backdrop_lib import B, M, E, Vector

W = "W3"


def pal():
    return dict(
        g1=M("#2D6A4F"), g2=M("#40916C"), g3=M("#52B788"), g4=M("#1B4332"), g5=M("#74C69D"), trunk=M("#6B4426"),
        bao=M("#8A6A4A"), bao2=M("#6E5238"), sand=M("#E9C98B"), rock=M("#7A7F73"), rock2=M("#5C6258"), moss=M("#4D8B55"),
        orange=M("#F77F00"), yellow=M("#FFD23F"), thatch=M("#C9A24A"), wall=M("#B07A45"), fall=E("#BFEFFF", 1.1),
        mist=M("#E8FBFF"), flame=E("#FFA11F", 3.0), bird=M("#FFFFFF"), bird2=M("#FF8FB0"), fruit=M("#F77F00"),
        pink=M("#F25C9B"), vine=M("#2E8B57"), dirt=M("#8A5A34"), water=M("#3FB5B0"))


def greens(C):
    return [C["g1"], C["g2"], C["g3"], C["g4"]]


# ---------------------------------------------------------------------------
# Árboles
# ---------------------------------------------------------------------------
def umbrella(b, C, x, y, h, rnd, seed, z0=0.0):
    b.prism_between((x, y, z0), (x + rnd.uniform(-1, 1), y + rnd.uniform(-1, 1), z0 + h * 0.8), h * 0.045, h * 0.03, C["trunk"], n=5, caps=False)
    b.blob((x, y, z0 + h * 0.9), (h * 0.5, h * 0.5, h * 0.16), rnd.choice(greens(C)), n=8, rings=3, jit=0.2, seed=seed)
    b.blob((x + h * 0.15, y - h * 0.1, z0 + h * 1.0), (h * 0.3, h * 0.3, h * 0.12), rnd.choice(greens(C)), n=6, rings=3, jit=0.2, seed=seed + 1)


def round_tree(b, C, x, y, h, rnd, seed, z0=0.0):
    bd.tree_round(b, x, y, h, C["trunk"], greens(C), rnd, seed=seed, z0=z0, crown=rnd.uniform(0.3, 0.42))


def vines(b, C, x, y, z, n, rnd, L=(4, 9)):
    for i in range(n):
        a = rnd.uniform(0, 6.28)
        r = rnd.uniform(2, 6)
        p = Vector((x + math.cos(a) * r, y + math.sin(a) * r, z))
        b.prism_between(p, p + Vector((0, 0, -rnd.uniform(*L))), 0.12, 0.07, C["vine"], n=3, caps=False)


def baobab(b, C, x, y, h, rnd, seed=0, z0=0.0):
    r = h * 0.17
    rings = []
    prof = [(0.0, 1.0), (0.15, 0.82), (0.5, 0.78), (0.75, 0.62), (0.9, 0.45), (1.0, 0.4)]
    jit = [rnd.uniform(-0.08, 0.08) for _ in range(9)]
    for t, k in prof:
        rings.append(bd.ring(x, y, z0 + t * h * 0.78, r * k * (1.25 if t == 0 else 1.0), r * k, 9, 0.0, jit=jit))
    b.loft(rings, C["bao"], top=True, bottom=False, ms=[C["bao"], C["bao"], C["bao2"], C["bao"], C["bao2"]])
    top = z0 + h * 0.78
    for i in range(6):
        a = i / 6 * 6.28 + rnd.uniform(-0.3, 0.3)
        d = Vector((math.cos(a), math.sin(a), 0))
        tip = Vector((x, y, top)) + d * h * 0.32 + Vector((0, 0, h * rnd.uniform(0.15, 0.26)))
        b.prism_between((x, y, top - h * 0.03), tip, r * 0.2, r * 0.1, C["bao2"], n=4, caps=False)
        b.blob(tip + Vector((0, 0, h * 0.03)), (h * 0.17, h * 0.17, h * 0.07), rnd.choice(greens(C)), n=6, rings=3, jit=0.2, seed=seed + i)


# ---------------------------------------------------------------------------
# Capa media
# ---------------------------------------------------------------------------
def canopy_a(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 60, 3.5, C["g1"], C["g2"], seed=1, n=10, rings=3, ry=34)
    k = 0
    while b.tris() < 4200 and k < 26:
        x, y = rnd.uniform(-55, 55), rnd.uniform(-28, 28)
        round_tree(b, C, x, y, rnd.uniform(14, 26), rnd, k, z0=1.0)
        k += 1
    for i in range(6):
        bd.palm(b, rnd.uniform(-50, 50), rnd.uniform(-25, 25), rnd.uniform(9, 14), C["trunk"], rnd.choice([C["g2"], C["g3"]]), rnd, z0=1.0)


def canopy_b(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 62, 4.5, C["g4"], C["g1"], seed=2, n=10, rings=3, ry=32)
    for i in range(13):
        x, y = rnd.uniform(-55, 55), rnd.uniform(-26, 26)
        h = rnd.uniform(22, 38)
        umbrella(b, C, x, y, h, rnd, i, z0=1.5)
        vines(b, C, x, y, h * 0.85, 4, rnd)
    for i in range(10):
        round_tree(b, C, rnd.uniform(-55, 55), rnd.uniform(-26, 26), rnd.uniform(10, 16), rnd, 30 + i, z0=1.5)


def baobabs(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 64, 2.5, C["sand"], C["g2"], seed=3, n=10, rings=3, ry=28)
    for i, (x, y, h) in enumerate(((-34, 0, 34), (4, 10, 44), (38, -6, 30), (-8, -18, 22))):
        baobab(b, C, x, y, h, rnd, seed=i * 10, z0=1.0)
    for i in range(8):
        round_tree(b, C, rnd.uniform(-55, 55), rnd.uniform(-25, 25), rnd.uniform(6, 10), rnd, 50 + i, z0=1.0)
    for i in range(6):
        bd.rock_mass(b, rnd.uniform(-50, 50), rnd.uniform(-24, 24), rnd.uniform(2, 4), rnd.uniform(1.5, 3), C["rock"], seed=i, rings=3, n=6)


def palm_grove(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 62, 3, C["sand"], C["sand"], seed=1, n=12, rings=2, jit=0.1, ry=26)
    bd.rock_mass(b, 0, -14, 64, 0.5, C["mist"], seed=2, n=12, rings=2, jit=0.1, ry=16)
    for i in range(24):
        bd.palm(b, rnd.uniform(-52, 52), rnd.uniform(-18, 22), rnd.uniform(9, 17), C["trunk"], rnd.choice([C["g2"], C["g3"], C["g1"]]), rnd,
                z0=1.5, lean=0.25, fronds=7)
        if b.tris() > 4300:
            break
    for i in range(5):
        a = rnd.uniform(-45, 45)
        b.prism_between((a, -8, 1.3), (a + rnd.uniform(5, 9), -10, 1.8), 0.5, 0.35, C["trunk"], n=4)
    for i in range(7):
        bd.rock_mass(b, rnd.uniform(-50, 50), rnd.uniform(-12, 12), rnd.uniform(2, 3.5), rnd.uniform(1.2, 2.6), C["rock"], seed=i, rings=3, n=6, z0=1.0)


def cliff_falls(b, rnd):
    C = pal()
    x = -60
    hs = [38, 54, 46, 62, 44]
    for i, h in enumerate(hs):
        w = rnd.uniform(24, 30)
        b.box(x + w / 2, 0, -1, w, rnd.uniform(26, 34), h + 1, C["rock"] if i % 2 else C["rock2"], taper=rnd.uniform(0.7, 0.92), top_m=C["moss"])
        for k in range(3):
            round_tree(b, C, x + rnd.uniform(2, w - 2), rnd.uniform(-5, 5), rnd.uniform(7, 12), rnd, i * 7 + k, z0=h * 0.9)
        x += w * 0.9
    # cascadas
    for fx, fh in ((-34, 54), (4, 50)):
        b.quad(Vector((fx - 3.5, -14.6, 2)), Vector((fx + 3.5, -14.6, 2)), Vector((fx + 3, -13.6, fh)), Vector((fx - 3, -13.6, fh)), C["fall"])
        bd.rock_mass(b, fx, -16, 8, 3, C["mist"], seed=fx, n=7, rings=3, jit=0.2)
    for i in range(6):
        bd.rock_mass(b, rnd.uniform(-60, 60), rnd.uniform(-22, -16), rnd.uniform(2, 4), rnd.uniform(1, 3), C["rock2"], seed=i + 10, rings=3, n=6)


def moss_rocks(b, rnd):
    C = pal()
    for i in range(12):
        r = rnd.uniform(5, 13)
        x, y = rnd.uniform(-52, 52), rnd.uniform(-26, 26)
        bd.rock_mass(b, x, y, r, r * rnd.uniform(0.6, 1.0), C["rock"], C["moss"], seed=i, jit=0.3, n=7, rings=4)
        for k in range(3):
            a = rnd.uniform(0, 6.28)
            bd.palm(b, x + math.cos(a) * r * 0.5, y + math.sin(a) * r * 0.5, rnd.uniform(3, 5), C["trunk"], C["g3"], rnd, z0=r * 0.55, fronds=7, lean=0.1)
        vines(b, C, x, y, r * 0.5, 2, rnd, L=(2, 5))
        if b.tris() > 4300:
            break


def village(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 60, 1.8, C["dirt"], C["g2"], seed=3, n=10, rings=3, ry=30)
    for i in range(7):
        x, y = -44 + (i % 4) * 28 + rnd.uniform(-4, 4), -14 + (i // 4) * 28 + rnd.uniform(-4, 4)
        s = rnd.uniform(0.9, 1.3)
        b.cyl(x, y, 1.6, 4.2 * s, 4.2 * s, 3.6 * s, C["wall"], n=8)
        b.cyl(x, y, 5.2 * s + 1.6 - 0.2, 6.2 * s, 0, 5.2 * s, C["thatch"], n=8)
        b.box(x, y - 4.15 * s, 1.6, 1.4, 0.2, 2.2, C["trunk"])
        b.prism_between((x + 6 * s, y, 1.6), (x + 6 * s, y, 5.5), 0.2, 0.15, C["trunk"], n=4, caps=False)
        b.blob((x + 6 * s, y, 5.9), 0.55, C["flame"], n=5, rings=2, jit=0)
    for i in range(14):
        round_tree(b, C, rnd.uniform(-55, 55), rnd.uniform(-28, 28), rnd.uniform(9, 16), rnd, 70 + i, z0=1.8)
    for i in range(12):
        x = -50 + i * 9
        b.prism_between((x, 22, 1.8), (x, 22, 4), 0.25, 0.2, C["trunk"], n=4, caps=False)


def fig_trees(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 64, 3, C["g1"], C["g2"], seed=4, n=10, rings=3, ry=30)
    for i, (x, y, h) in enumerate(((-32, 0, 30), (14, 8, 38), (46, -10, 26))):
        b.cyl(x, y, 1.5, h * 0.12, h * 0.075, h * 0.5, C["trunk"], n=7)
        for k in range(7):
            a = k / 7 * 6.28 + rnd.uniform(-0.2, 0.2)
            d = Vector((math.cos(a), math.sin(a), 0))
            b.prism_between((x, y, 1.5 + h * 0.4), Vector((x, y, 1.5 + h * 0.4)) + d * h * 0.28 + Vector((0, 0, -h * 0.4)), h * 0.05, h * 0.018, C["trunk"], n=4, caps=False)
        for k in range(3):
            b.blob((x + rnd.uniform(-1, 1) * h * 0.2, y + rnd.uniform(-1, 1) * h * 0.2, 1.5 + h * (0.78 + 0.08 * k)), (h * 0.5, h * 0.42, h * 0.2), rnd.choice(greens(C)), n=8, rings=3, jit=0.2, seed=i * 5 + k)
        vines(b, C, x, y, 1.5 + h * 0.75, 7, rnd, L=(h * 0.2, h * 0.45))
    for i in range(12):
        round_tree(b, C, rnd.uniform(-60, 60), rnd.uniform(-28, 28), rnd.uniform(8, 14), rnd, 90 + i, z0=1.5)
    for i in range(8):
        a, c = rnd.uniform(-55, 55), rnd.uniform(-25, 25)
        b.blob((a, c, 1.5), (2, 1.2, 1.0), C["pink"] if i % 2 else C["orange"], n=5, rings=2, jit=0.1)


# ---------------------------------------------------------------------------
# Capa lejana
# ---------------------------------------------------------------------------
def hills(b, C, seed, amp, base, mats, trees):
    b.sheet(-240, 240, -45, 45, 60, 8, lambda x, y: (base + amp * (0.5 + 0.5 * bd.fbm(x * 0.012, seed, seed, 3))) *
            bd.smooth((45 - abs(y)) / 40) * (0.5 + 0.5 * bd.smooth((240 - abs(x)) / 80)) - 1.0,
            lambda x, y, z, s: mats[0] if z < base + amp * 0.45 else mats[1])
    return


def far_hills_a(b, rnd):
    C = pal()
    hills(b, C, 2.0, 38, 18, [C["g1"], C["g2"]], 0)
    for i in range(30):
        x = rnd.uniform(-220, 220)
        z = (18 + 38 * (0.5 + 0.5 * bd.fbm(x * 0.012, 2.0, 2.0, 3))) * (0.5 + 0.5 * bd.smooth((240 - abs(x)) / 80))
        b.blob((x, -4, z * 0.9), (6, 5, 5), rnd.choice(greens(C)), n=5, rings=2, jit=0.2, seed=i)
        if b.tris() > 1850:
            break


def far_hills_b(b, rnd):
    C = pal()
    hills(b, C, 7.0, 70, 24, [C["g2"], C["g3"]], 0)


def far_baobabs(b, rnd):
    C = pal()
    for i, x in enumerate((-190, -90, 10, 100, 200)):
        baobab(b, C, x + rnd.uniform(-15, 15), rnd.uniform(-10, 10), rnd.uniform(45, 70), rnd, seed=i * 5)
    for i in range(10):
        round_tree(b, C, rnd.uniform(-220, 220), rnd.uniform(-20, 20), rnd.uniform(16, 26), rnd, i)


def far_waterfalls(b, rnd):
    C = pal()
    x = -230
    for i in range(7):
        w = rnd.uniform(55, 80)
        h = rnd.uniform(60, 110)
        b.box(x + w / 2, 0, -2, w, rnd.uniform(30, 44), h + 2, C["rock"] if i % 2 else C["rock2"], taper=rnd.uniform(0.6, 0.85), top_m=C["g1"])
        if i in (1, 3, 5):
            fx = x + w / 2
            b.quad(Vector((fx - 5, -21.5, 2)), Vector((fx + 5, -21.5, 2)), Vector((fx + 4, -19.5, h * 0.96)), Vector((fx - 4, -19.5, h * 0.96)), C["fall"])
            bd.rock_mass(b, fx, -24, 12, 5, C["mist"], seed=i, n=7, rings=3, jit=0.2)
        x += w * 0.92


def birds(b, rnd):
    C = pal()
    for i in range(46):
        cx = rnd.uniform(-220, 220) if i > 12 else -60 + rnd.uniform(-25, 25) + (i * 3)
        cy = rnd.uniform(-20, 20)
        z = 70 + 45 * bd.fbm(cx * 0.02, 5, 5, 2) + rnd.uniform(0, 50)
        s = rnd.uniform(1.4, 2.8)
        col = C["bird"] if i % 7 else C["bird2"]
        b.push((cx, cy, z), rnd.uniform(-0.3, 0.3), s)
        flap = rnd.uniform(0.6, 1.8)
        for sd in (-1, 1):
            b.quad(Vector((0.6, 0, 0)), Vector((-0.8, 0, 0)), Vector((-1.6, sd * 3.2, flap)), Vector((1.0, sd * 2.8, flap * 0.8)), col)
            b.quad(Vector((0.6, 0, 0)), Vector((1.0, sd * 2.8, flap * 0.8)), Vector((-1.6, sd * 3.2, flap)), Vector((-0.8, 0, 0)), col)
        b.quad(Vector((1.6, 0, 0.1)), Vector((-0.8, -0.35, 0)), Vector((-1.6, 0, 0.1)), Vector((-0.8, 0.35, 0)), C["orange"])
        b.pop()


def far_peaks(b, rnd):
    C = pal()
    x = -220
    for i in range(9):
        r = rnd.uniform(30, 55)
        h = rnd.uniform(80, 150)
        bd.spire(b, x, rnd.uniform(-10, 10), -2, r, h, C["g1"], C["rock"], n=7, lean=rnd.uniform(-0.1, 0.1), seed=i)
        bd.spire(b, x + r * 0.9, 6, -2, r * 0.7, h * 0.65, C["g2"], C["g2"], n=6, seed=i + 20)
        x += r * 1.0 + 20
    bd.rock_mass(b, 0, 0, 240, 22, C["g4"], seed=3, n=14, rings=3, ry=30)


# ---------------------------------------------------------------------------
# Suelo
# ---------------------------------------------------------------------------
def gm():
    return [M("#2D6A4F"), M("#3C8A5F"), M("#C9A24A")]


def ground_a(b, rnd):
    bd.ground_tile(b, gm(), 1.0, amp=1.0, patch=0.55)


def ground_b(b, rnd):
    bd.ground_tile(b, gm(), 8.0, amp=1.6, freq=0.016, patch=0.6)


def ground_skirt(b, rnd):
    bd.ground_skirt(b, [M("#2A6249"), M("#2F7050")])


def horizon(b, rnd):
    bd.horizon_ring(b, [M("#2A6B4D"), M("#3E8F66")], hmin=25, hmax=110, seed=2.4)


PIECES = [
    ("Mid", "CanopyA", canopy_a), ("Mid", "CanopyB", canopy_b), ("Mid", "Baobabs", baobabs),
    ("Mid", "PalmGrove", palm_grove), ("Mid", "CliffFalls", cliff_falls), ("Mid", "MossRocks", moss_rocks),
    ("Mid", "Village", village), ("Mid", "FigTrees", fig_trees),
    ("Far", "HillsA", far_hills_a), ("Far", "HillsB", far_hills_b), ("Far", "Baobabs", far_baobabs),
    ("Far", "Waterfalls", far_waterfalls), ("Far", "Birds", birds), ("Far", "Peaks", far_peaks),
    ("Ground", "TileA", ground_a), ("Ground", "TileB", ground_b), ("Ground", "Skirt", ground_skirt),
    ("Ground", "HorizonRing", horizon),
]

if __name__ == "__main__":
    bd.build_all(W, PIECES, only=bd.script_args())
