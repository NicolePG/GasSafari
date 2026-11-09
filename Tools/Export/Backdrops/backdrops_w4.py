"""Fondos W4 · Cuevas y Volcán. Paleta de docs/04: morado #3C1642, cian #4CC9F0, rojo magma #D00000;
cristales y lava emisivos, acento amarillo-naranja.
blender --background --python backdrops_w4.py [-- Mid_CaveWall_A Far_Volcano ...]
Salida: Tools/Export/Backdrops/W4/W4_<Capa>_<Nombre>.fbx. Suelo / línea de agua en z = 0.
Las piezas colgantes (Mid_Stalactites, Far_Stalactites, Ground_CeilingTile) tienen el pivote en el techo (z = 0 arriba,
la malla baja hacia z negativo)."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backdrop_lib as bd
from backdrop_lib import B, M, E, Vector

W = "W4"


def pal():
    return dict(
        r1=M("#2B1A33"), r2=M("#3C1642"), r3=M("#5A2A66"), r4=M("#1E0B26"), r5=M("#7A3E8C"), ash=M("#3A3340"), ash2=M("#585062"),
        cy=E("#4CC9F0", 3.0), cy2=E("#9BE8FF", 3.4), mg=E("#C77DFF", 2.8), lava=E("#FF4A12", 3.4), lava2=E("#FFB02E", 3.4),
        crust=M("#3A1414"), smoke=M("#6B6272"), smoke2=M("#4A4352"), steam=M("#CDB6D8"), vein=E("#FF7A1F", 2.8))


def cluster(b, base, C, rnd, n, size, mats, up=(0, 0, 1)):
    base = Vector(base)
    for i in range(n):
        d = (Vector(up) + Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.6, 0.6), rnd.uniform(-0.1, 0.4)))).normalized()
        b.crystal(base + Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), 0)) * size * 0.35, d, size * rnd.uniform(0.5, 1.2),
                  size * rnd.uniform(0.1, 0.18), rnd.choice(mats), n=5)


def rock_column(b, x, y, z0, r, h, body, rnd, seed, top=None):
    bd.spire(b, x, y, z0, r, h, body, top, n=7, lean=rnd.uniform(-0.08, 0.08), seed=seed)


# ---------------------------------------------------------------------------
# Capa media
# ---------------------------------------------------------------------------
def wall_group(b, rnd, crystals, veins, seed):
    C = pal()
    x = -62
    for i in range(7):
        w = rnd.uniform(16, 24)
        h = rnd.uniform(26, 50)
        y = rnd.uniform(-6, 6)
        b.box(x + w / 2, y, -1, w, rnd.uniform(14, 22), h + 1, rnd.choice([C["r1"], C["r2"], C["r4"]]), taper=rnd.uniform(0.6, 0.85), top_m=C["r3"])
        for k in range(2):
            rock_column(b, x + rnd.uniform(0, w), y - 9 - rnd.uniform(0, 3), 0, rnd.uniform(3, 6), rnd.uniform(10, 22), C["r3"], rnd, i * 4 + k)
        cluster(b, (x + w * rnd.uniform(0.2, 0.8), y - 10, 1), C, rnd, rnd.randint(3, 5), rnd.uniform(4, 8), crystals)
        if veins:
            for k in range(3):
                vx = x + rnd.uniform(2, w - 2)
                z0 = rnd.uniform(4, h * 0.6)
                b.wall_quad((vx, y - 9.2 - (h - z0) * 0.0, z0 + 4), (0, -1, 0), 0.4, rnd.uniform(3, 6), C["vein"])
        x += w * 0.9
    for i in range(14):
        bd.rock_mass(b, rnd.uniform(-60, 60), rnd.uniform(-22, -12), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["r3"], seed=i + seed, rings=3, n=6)


def cave_wall_a(b, rnd):
    C = pal()
    wall_group(b, rnd, [C["cy"], C["cy2"], C["cy"]], False, 0)


def cave_wall_b(b, rnd):
    C = pal()
    wall_group(b, rnd, [C["mg"], C["mg"], C["cy"]], True, 20)


def stalactites(b, rnd):
    C = pal()
    for i in range(18):
        x, y = rnd.uniform(-60, 60), rnd.uniform(-24, 24)
        L = rnd.uniform(10, 38)
        r = rnd.uniform(2, 5.5) * (0.6 + L / 38)
        n = 7
        rnd2 = [rnd.uniform(-0.15, 0.15) for _ in range(n)]
        a = bd.ring(x, y, 0.5, r, r, n, rnd.random(), jit=rnd2)
        c = bd.ring(x + rnd.uniform(-1, 1), y, -L * 0.7, r * 0.45, r * 0.45, n, 0.2, jit=rnd2)
        tip = Vector((x + rnd.uniform(-2, 2), y, -L))
        # sentido invertido para que miren hacia fuera: giro horario visto desde arriba
        b.loft([list(reversed(a)), list(reversed(c)), tip], C["r3"] if i % 3 else C["r5"], top=False, bottom=False)
        if i % 5 == 0:
            b.crystal((x, y, -L * 0.2), (0, 0, -1), L * 0.35, 0.8, C["cy"], n=5)
        if b.tris() > 4500:
            break
    b.box(0, 0, 0.0, 130, 56, 1.5, C["r4"], taper=1.0)


def stalagmites(b, rnd):
    C = pal()
    for i in range(24):
        x, y = rnd.uniform(-60, 60), rnd.uniform(-26, 26)
        rock_column(b, x, y, 0, rnd.uniform(1.8, 4.5), rnd.uniform(8, 30), C["r3"] if i % 3 else C["r5"], rnd, i)
    for i in range(7):
        cluster(b, (rnd.uniform(-55, 55), rnd.uniform(-24, 24), 0), C, rnd, rnd.randint(3, 6), rnd.uniform(4, 9), [C["cy"], C["mg"], C["cy2"]])
    for i in range(10):
        bd.rock_mass(b, rnd.uniform(-60, 60), rnd.uniform(-26, 26), rnd.uniform(2, 5), rnd.uniform(1, 3), C["r1"], seed=i + 5, rings=3, n=6)


def crystal_cavern(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 38, 9, C["r1"], C["r3"], seed=2, jit=0.3, n=9, rings=3, ry=28)
    for i in range(9):
        a = i / 9 * 6.28 + rnd.uniform(-0.3, 0.3)
        r = rnd.uniform(4, 22)
        base = Vector((math.cos(a) * r, math.sin(a) * r * 0.7, 4))
        d = Vector((math.cos(a) * 0.35, math.sin(a) * 0.3, 1)).normalized()
        b.crystal(base, d, rnd.uniform(24, 52), rnd.uniform(3, 6), [C["cy"], C["cy2"], C["mg"]][i % 3], n=6, tip=0.3)
    cluster(b, (0, 0, 8), C, rnd, 10, 18, [C["cy"], C["cy2"], C["mg"]])
    for i in range(12):
        a = rnd.uniform(0, 6.28)
        r = rnd.uniform(30, 60)
        cluster(b, (math.cos(a) * r, math.sin(a) * r * 0.6, 0), C, rnd, 3, rnd.uniform(4, 9), [C["cy"], C["cy2"]])


def lava_bank(b, rnd):
    C = pal()
    # río de lava (z = 0.05) con costras
    b.sheet(-64, 64, -9, 9, 32, 3, lambda x, y: 0.08 + 0.5 * math.sin(x * 0.2 + y) * 0.0,
            lambda x, y, z, s: C["lava2"] if bd.fbm(x * 0.12, y * 0.2, 4, 2) > 0.1 else C["lava"])
    for sd in (-1, 1):
        b.sheet(-64, 64, sd * 9 if sd > 0 else -34, 34 if sd > 0 else -9, 32, 4,
                lambda x, y, sd=sd: (3 + 7 * (0.5 + 0.5 * bd.fbm(x * 0.05, y * 0.05, 2 + sd, 3))) * bd.smooth((abs(y) - 9) / 7) * bd.smooth((34 - abs(y)) / 22),
                lambda x, y, z, s: C["r3"] if z > 6 else C["r1"])
    for i in range(10):
        x = rnd.uniform(-58, 58)
        bd.rock_mass(b, x, rnd.uniform(-4, 4), rnd.uniform(1.8, 4), rnd.uniform(1, 2.2), C["crust"], seed=i, rings=2, n=6, z0=0.1)
    for i in range(7):
        rock_column(b, rnd.uniform(-60, 60), rnd.choice([-1, 1]) * rnd.uniform(14, 28), 1, rnd.uniform(2, 4), rnd.uniform(10, 24), C["r3"], rnd, i)
    for i in range(4):
        cluster(b, (rnd.uniform(-55, 55), rnd.choice([-1, 1]) * rnd.uniform(14, 26), 2), C, rnd, 3, 6, [C["cy"], C["mg"]])


def lava_falls(b, rnd):
    C = pal()
    b.box(0, 8, -1, 110, 30, 42, C["r2"], taper=0.75, top_m=C["r3"])
    for fx in (-30, 8, 38):
        b.quad(Vector((fx - 4, -6.7, 1)), Vector((fx + 4, -6.7, 1)), Vector((fx + 3, -5.6, 38)), Vector((fx - 3, -5.6, 38)), C["lava"])
        b.quad(Vector((fx - 1.5, -6.9, 1)), Vector((fx + 1.5, -6.9, 1)), Vector((fx + 1.2, -5.8, 30)), Vector((fx - 1.2, -5.8, 30)), C["lava2"])
        bd.rock_mass(b, fx, -12, 9, 0.8, C["lava"], seed=int(fx), n=8, rings=2, jit=0.1)
        bd.rock_mass(b, fx, -12, 5, 0.9, C["lava2"], seed=int(fx) + 1, n=8, rings=2, jit=0.1, z0=0.1)
        bd.rock_mass(b, fx, -10, 6, 4, C["steam"], seed=int(fx) + 2, n=6, rings=3, jit=0.2, z0=1.0)
    for i in range(12):
        bd.rock_mass(b, rnd.uniform(-55, 55), rnd.uniform(-22, -8), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["r1"], seed=i, rings=3, n=6)
    for i in range(6):
        rock_column(b, rnd.uniform(-55, 55), rnd.uniform(-24, -14), 0, rnd.uniform(2, 4), rnd.uniform(8, 18), C["r3"], rnd, i)


def rock_pillars(b, rnd):
    C = pal()
    for i, x in enumerate((-48, -16, 18, 50)):
        for s in (-1, 1):
            rock_column(b, x + s * 7, 0, -1, rnd.uniform(5, 7), rnd.uniform(30, 44), C["r3"] if i % 2 else C["r2"], rnd, i * 2 + (s > 0))
        b.box(x, 0, 24 + i * 2, 20, 7, 6, C["r2"], taper=0.8, top_m=C["r3"])
        for k in range(3):
            b.wall_quad((x + s * 0, -3.9, 8 + k * 6), (0, -1, 0), 0.35, rnd.uniform(2, 4), C["vein"])
        cluster(b, (x, -5, 1), C, rnd, 4, 5, [C["cy"], C["mg"]])
    for i in range(12):
        bd.rock_mass(b, rnd.uniform(-60, 60), rnd.uniform(-20, 18), rnd.uniform(2, 5), rnd.uniform(1.5, 4), C["r1"], seed=i, rings=3, n=6)


# ---------------------------------------------------------------------------
# Capa lejana
# ---------------------------------------------------------------------------
def far_cave_wall(b, rnd):
    C = pal()
    b.sheet(-240, 240, -30, 30, 64, 6, lambda x, y: (70 + 55 * bd.fbm(x * 0.02, 0, 4, 3) + 25 * abs(math.sin(x * 0.11))) *
            bd.smooth((30 - abs(y)) / 25) * bd.smooth((240 - abs(x)) / 40) - 2.0,
            lambda x, y, z, s: C["r3"] if z > 90 else (C["r2"] if z > 45 else C["r4"]))
    for i in range(10):
        x = rnd.uniform(-210, 210)
        cluster(b, (x, -9, 6 + rnd.uniform(0, 30)), C, rnd, 4, rnd.uniform(14, 26), [C["cy"], C["cy2"], C["mg"]], up=(0, -0.6, 0.8))


def far_stalactites(b, rnd):
    C = pal()
    for i in range(16):
        x = -230 + i * 30 + rnd.uniform(-8, 8)
        L = rnd.uniform(40, 130)
        r = L * rnd.uniform(0.12, 0.2)
        n = 6
        j = [rnd.uniform(-0.15, 0.15) for _ in range(n)]
        a = bd.ring(x, rnd.uniform(-10, 10), 0.5, r, r, n, rnd.random(), jit=j)
        c = bd.ring(x, 0, -L * 0.7, r * 0.4, r * 0.4, n, 0.2, jit=j)
        b.loft([list(reversed(a)), list(reversed(c)), Vector((x + rnd.uniform(-4, 4), 0, -L))], C["r3"] if i % 3 else C["r5"], top=False, bottom=False)
        if i % 4 == 1:
            b.crystal((x, -r * 0.5, -L * 0.3), (0, -0.3, -1), L * 0.3, r * 0.15, C["cy"], n=5)
    b.box(0, 0, 0, 500, 40, 2, C["r4"])


def far_volcano(b, rnd):
    C = pal()
    H, R = 230.0, 260.0
    rs = lambda t: R * (1 - t) ** 1.35 + R * 0.07 * (1 - t)
    rings = []
    N = 12
    for k in range(7):
        t = k / 6.0 * 0.97
        j = [rnd.uniform(-0.07, 0.07) for _ in range(N)]
        rings.append(bd.ring(0, 0, t * H - 4, rs(t), rs(t), N, 0.0, jit=j))
    rim_r = rs(0.97)
    inner = bd.ring(0, 0, 0.93 * H, rim_r * 0.6, rim_r * 0.6, N, 0.0)
    b.loft(rings, C["ash"], top=False, bottom=False, ms=[C["ash"], C["ash"], C["r1"], C["r1"], C["ash2"], C["ash2"]])
    # interior del cráter (cara hacia dentro) y lava
    b.loft([list(reversed(rings[-1])), list(reversed(inner))], C["ash2"], top=False, bottom=False)
    b.poly(list(inner), C["lava"])
    # ríos de lava por la ladera
    for kth in range(5):
        a = kth / 5 * 6.28 + 0.5 + rnd.uniform(-0.2, 0.2)
        prev = None
        for s in range(9):
            t = 0.96 - s * 0.105
            if t < 0.02:
                break
            r = rs(t) + 1.2
            aa = a + math.sin(s * 1.3 + kth) * 0.08
            wd = (9 - s * 0.7) / r
            pl = Vector((math.cos(aa - wd) * r, math.sin(aa - wd) * r, t * H - 4 + 0.6))
            pr = Vector((math.cos(aa + wd) * r, math.sin(aa + wd) * r, t * H - 4 + 0.6))
            if prev is not None:
                out = Vector((math.cos(aa), math.sin(aa), 0.5))
                b.facing((prev[0], prev[1], pr, pl), C["lava2"] if s % 3 == 0 else C["lava"], out)
            prev = (pl, pr)
    # penacho de humo
    for i in range(11):
        z = H + 8 + i * 15
        sc = 18 + i * 5
        b.blob((rnd.uniform(-8, 8) + i * 7, rnd.uniform(-8, 8), z), (sc, sc * 0.9, sc * 0.55), C["smoke"] if i % 2 else C["smoke2"], n=7, rings=3, jit=0.22, seed=i)
    # volcanes menores
    for x, h, r in ((-170, 70, 90), (190, 90, 110)):
        b.cyl(x, 30, -2, r, r * 0.12, h, C["ash"], n=9)
        b.cyl(x, 30, h - 3, r * 0.12, r * 0.07, 3, C["lava"], n=9)


def far_lava_field(b, rnd):
    C = pal()
    fn = lambda x, y: (18 + 22 * (0.5 + 0.5 * bd.fbm(x * 0.012, y * 0.02, 6, 3))) * bd.smooth((50 - abs(y)) / 40) * \
        (0.3 + 0.7 * bd.smooth((240 - abs(x)) / 70)) - 1.0
    b.sheet(-240, 240, -50, 50, 60, 10, fn,
            lambda x, y, z, s: C["lava2"] if (z < 9 and abs(math.sin(x * 0.03 + y * 0.05 + 2 * bd.fbm(x * 0.02, y * 0.02, 1, 2))) < 0.2) else
            (C["lava"] if z < 5 else (C["r1"] if z < 22 else C["r3"])))


def far_crystal_spires(b, rnd):
    C = pal()
    for i in range(10):
        x = -220 + i * 48 + rnd.uniform(-10, 10)
        rock_column(b, x, rnd.uniform(-10, 10), -2, rnd.uniform(14, 26), rnd.uniform(40, 80), C["r3"], rnd, i)
        for k in range(rnd.randint(1, 3)):
            a = rnd.uniform(0, 6.28)
            d = Vector((math.cos(a) * 0.4, math.sin(a) * 0.3, 1)).normalized()
            b.crystal(Vector((x + math.cos(a) * 14, math.sin(a) * 8, 0)), d, rnd.uniform(50, 130), rnd.uniform(6, 12),
                      [C["cy"], C["mg"], C["cy2"]][(i + k) % 3], n=6)


def far_arches(b, rnd):
    C = pal()
    for i, x in enumerate((-170, 0, 170)):
        for s in (-1, 1):
            rock_column(b, x + s * 38, 0, -2, rnd.uniform(18, 24), rnd.uniform(90, 120), C["r2"], rnd, i * 2 + (s > 0))
        b.box(x, 0, 85 + i * 6, 110, 36, 28, C["r2"], taper=0.7, top_m=C["r3"])
        cluster(b, (x, -18, 0), C, rnd, 4, 22, [C["cy"], C["mg"]])
    bd.rock_mass(b, 0, 0, 240, 28, C["r1"], seed=7, n=14, rings=3, ry=30)


# ---------------------------------------------------------------------------
# Suelo
# ---------------------------------------------------------------------------
def gm():
    return [M("#2B1A33"), M("#3C1642"), M("#5A2A66")]


def ground_a(b, rnd):
    bd.ground_tile(b, gm(), 1.0, amp=1.1, patch=0.5)


def ground_b(b, rnd):
    bd.ground_tile(b, gm(), 6.0, amp=1.8, freq=0.016, patch=0.55)


def ground_skirt(b, rnd):
    bd.ground_skirt(b, [M("#241529"), M("#2B1A33")])


def horizon(b, rnd):
    bd.horizon_ring(b, [M("#1E0B26"), M("#3C1642")], hmin=60, hmax=190, seed=6.6, pointy=0.5)


def ceiling_tile(b, rnd):
    h = 100.0
    m = gm()
    edge = 18.0

    def fn(x, y):
        k = min(bd.smooth((h - abs(x)) / edge), bd.smooth((h - abs(y)) / edge))
        return -(1.0 + 7.0 * (0.5 + 0.5 * bd.fbm(x * 0.03, y * 0.03, 3, 3))) * k

    b.sheet(-h, h, -h, h, 20, 20, fn, lambda x, y, z, s: m[0] if z > -3 else (m[1] if z > -6 else m[2]))
    b.flip_all()


PIECES = [
    ("Mid", "CaveWall_A", cave_wall_a), ("Mid", "CaveWall_B", cave_wall_b), ("Mid", "Stalactites", stalactites),
    ("Mid", "Stalagmites", stalagmites), ("Mid", "CrystalCavern", crystal_cavern), ("Mid", "LavaBank", lava_bank),
    ("Mid", "LavaFalls", lava_falls), ("Mid", "RockPillars", rock_pillars),
    ("Far", "CaveWall", far_cave_wall), ("Far", "Stalactites", far_stalactites), ("Far", "Volcano", far_volcano),
    ("Far", "LavaField", far_lava_field), ("Far", "CrystalSpires", far_crystal_spires), ("Far", "Arches", far_arches),
    ("Ground", "TileA", ground_a), ("Ground", "TileB", ground_b), ("Ground", "Skirt", ground_skirt),
    ("Ground", "HorizonRing", horizon), ("Ground", "CeilingTile", ceiling_tile),
]

if __name__ == "__main__":
    bd.build_all(W, PIECES, only=bd.script_args())
