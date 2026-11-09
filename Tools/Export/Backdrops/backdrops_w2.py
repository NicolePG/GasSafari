"""Fondos W2 · Mar Abierto (mar y tormenta). Paleta de docs/04: turquesa #2EC4B6, gris #5C6B73, espuma.
blender --background --python backdrops_w2.py [-- Mid_Islet_A Far_StormClouds ...]
Salida: Tools/Export/Backdrops/W2/W2_<Capa>_<Nombre>.fbx. Línea de agua en z = 0."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backdrop_lib as bd
from backdrop_lib import B, M, E, Vector

W = "W2"


def pal():
    return dict(
        rock=M("#5C6B73"), rock2=M("#46545C"), rock3=M("#7B8B92"), moss=M("#3E7F5C"), sand=M("#E3D3A0"),
        foam=M("#F2FBFA"), teal=M("#2EC4B6"), deep=M("#1B8E96"), trunk=M("#7A5232"), leaf=M("#2F9A5C"),
        leaf2=M("#43B36A"), wood=M("#8A6038"), wood2=M("#5A3D24"), red=M("#D9423E"), white=M("#EDEFEA"),
        yellow=M("#F2B93B"), navy=M("#27384A"), hull=[M("#D9423E"), M("#2E6FA8"), M("#F2B93B"), M("#3AA37A")],
        coral=[M("#FF6B6B"), M("#FF9F6B"), M("#B56BD9"), M("#FFD166")], steel=M("#4A5A66"), steel2=M("#6B7C88"),
        light=E("#FFF1A8", 3.0), lred=E("#FF4B3E", 3.0), cloud=M("#4B5762"), cloud2=M("#66727D"),
        cloud3=M("#2F3841"), bolt=E("#FFF4B0", 3.0), sail=M("#F4F0E4"), sail2=M("#E9B94A"))


def islet(b, C, x, y, r, h, rnd, seed, palms=5, sand=True):
    bd.rock_mass(b, x, y, r, h, C["rock"], C["moss"], seed=seed, jit=0.3, rings=4)
    for i in range(3):
        a = rnd.uniform(0, 6.28)
        bd.rock_mass(b, x + math.cos(a) * r * 0.8, y + math.sin(a) * r * 0.8, r * 0.35, h * 0.4, C["rock2"], seed=seed + i,
                     rings=3)
    if sand:
        bd.rock_mass(b, x, y - r * 0.2, r * 1.5, 1.3, C["sand"], seed=seed, n=10, rings=2, jit=0.12, z0=0.0)
        bd.rock_mass(b, x, y - r * 0.2, r * 1.7, 0.4, C["foam"], seed=seed + 1, n=10, rings=2, jit=0.12, z0=0.0)
    for i in range(palms):
        a = rnd.uniform(0, 6.28)
        rr = rnd.uniform(0.2, 0.7) * r
        bd.palm(b, x + math.cos(a) * rr, y + math.sin(a) * rr, rnd.uniform(7, 12), C["trunk"], rnd.choice([C["leaf"], C["leaf2"]]), rnd,
                z0=h * 0.55 * (1 - (rr / r) ** 2))


def islet_a(b, rnd):
    C = pal()
    islet(b, C, 0, 0, 32, 16, rnd, 1, palms=7)
    islet(b, C, 58, 22, 15, 7, rnd, 5, palms=3)
    islet(b, C, -54, -18, 12, 6, rnd, 9, palms=2)


def islet_b(b, rnd):
    C = pal()
    for i, (x, y, r) in enumerate(((0, 0, 44), (-52, 26, 18), (50, -22, 22))):
        bd.rock_mass(b, x, y, r, 2.4, C["sand"], C["sand"], seed=i, n=12, rings=2, jit=0.12, ry=r * 0.5)
        bd.rock_mass(b, x, y, r * 1.2, 0.5, C["foam"], seed=i + 4, n=12, rings=2, jit=0.1, ry=r * 0.62)
        for j in range(5 if i == 0 else 2):
            a = rnd.uniform(0, 6.28)
            bd.palm(b, x + math.cos(a) * r * 0.5, y + math.sin(a) * r * 0.25, rnd.uniform(8, 13), C["trunk"],
                    rnd.choice([C["leaf"], C["leaf2"]]), rnd, z0=2.0, lean=0.22)
    for i in range(6):
        bd.rock_mass(b, rnd.uniform(-45, 45), rnd.uniform(-20, 20), rnd.uniform(3, 5), rnd.uniform(2, 4), C["rock"],
                     seed=i + 20, rings=3, n=6)
    b.box(8, 4, 2.0, 7, 6, 3.6, C["wood"], top_m=C["wood2"])
    b.cyl(8, 4, 5.6, 5.8, 0, 3.0, C["wood2"], n=4, yaw=0.78)


def sea_stacks(b, rnd):
    C = pal()
    specs = [(-45, 0, 8, 34), (-18, 12, 10, 46), (14, -6, 9, 28), (46, 8, 12, 40), (70, -14, 7, 22), (-66, 24, 6, 24), (30, 26, 7, 30), (96, 10, 8, 36), (0, -26, 6, 18)]
    for i, (x, y, r, h) in enumerate(specs):
        bd.spire(b, x, y, -2, r, h + 2, C["rock"], C["rock3"], n=7, lean=rnd.uniform(-0.12, 0.12), seed=i)
        bd.rock_mass(b, x, y, r * 1.6, 3, C["rock2"], seed=i, n=8, rings=2)
        bd.rock_mass(b, x, y, r * 2.1, 0.6, C["foam"], seed=i + 8, n=10, rings=2, jit=0.1)
    # arco natural
    ax = -90
    for s in (-1, 1):
        bd.spire(b, ax + s * 11, 0, -2, 7, 30, C["rock"], C["rock3"], n=6, seed=30 + s)
    b.box(ax, 0, 22, 34, 9, 7, C["rock"], taper=0.8, top_m=C["rock3"])
    for i in range(7):
        bd.rock_mass(b, rnd.uniform(-100, 90), rnd.uniform(-20, 22), rnd.uniform(3, 6), rnd.uniform(2, 5), C["rock2"],
                     seed=i + 40, rings=3, n=6)


def fishing_boats(b, rnd):
    C = pal()
    for i in range(9):
        x, y = -66 + i * 17 + rnd.uniform(-6, 6), rnd.uniform(-28, 28)
        yaw = rnd.uniform(0, 6.28)
        col = C["hull"][i % 4]
        bd.hull(b, x, y, 0, 15, 5, 3.4, yaw, col, C["wood"], C["white"])
        b.push((x, y, 0), yaw)
        b.box(-3, 0, 2.4, 4.4, 3.6, 3.0, C["white"], top_m=C["navy"])
        b.box(-3, 1.85, 3.4, 2.2, 0.1, 1.0, C["light"])
        b.prism_between((1.5, 0, 2.4), (1.5, 0, 10), 0.18, 0.12, C["steel2"], n=4, caps=False)
        b.prism_between((1.5, 0, 9), (-7, 0, 3), 0.05, 0.05, C["steel2"], n=4, caps=False)
        b.prism_between((1.5, 0, 9), (6.5, 0, 2.6), 0.05, 0.05, C["steel2"], n=4, caps=False)
        b.blob((1.5, 0, 10.4), 0.4, C["light"], n=4, rings=2, jit=0)
        b.pop()
    for i in range(26):
        bd.rock_mass(b, rnd.uniform(-70, 70), rnd.uniform(-34, 34), 0.9, 0.9, C["red"] if i % 2 else C["yellow"], seed=i,
                     n=5, rings=2, jit=0.0)


def lighthouse_isle(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 30, 14, C["rock"], C["moss"], seed=3, jit=0.3, n=8, rings=4)
    bd.rock_mass(b, 20, 10, 12, 8, C["rock2"], seed=6, n=6, rings=3)
    bd.rock_mass(b, 0, 0, 40, 0.5, C["foam"], seed=7, n=12, rings=2, jit=0.1)
    z = 10.0
    segs = 6
    for i in range(segs):
        r0, r1 = 5.0 - i * 0.45, 5.0 - (i + 1) * 0.45
        b.cyl(0, 0, z + i * 5, r0, r1, 5, C["red"] if i % 2 == 0 else C["white"], n=10)
    zt = z + segs * 5
    b.cyl(0, 0, zt, 3.9, 3.9, 0.7, C["steel"], n=10)
    b.cyl(0, 0, zt + 0.7, 2.2, 2.2, 3.4, C["light"], n=8)
    b.cyl(0, 0, zt + 4.1, 3.3, 0, 3.2, C["red"], n=10)
    for i in range(5):
        a = rnd.uniform(0, 6.28)
        bd.rock_mass(b, math.cos(a) * 60, math.sin(a) * 40, rnd.uniform(5, 10), rnd.uniform(3, 7), C["rock"], C["moss"], seed=i + 40, rings=3, n=7)
        bd.rock_mass(b, math.cos(a) * 60, math.sin(a) * 40, rnd.uniform(9, 14), 0.4, C["foam"], seed=i + 50, n=9, rings=2, jit=0.1)
    for i in range(4):
        bd.palm(b, 60 + rnd.uniform(-8, 8), 10 + rnd.uniform(-8, 8), rnd.uniform(7, 10), C["trunk"], C["leaf"], rnd, z0=6)
    b.box(14, -6, 8.0, 8, 6, 4.5, C["white"], top_m=C["red"], taper=0.9)
    b.cyl(14, -6, 12.5, 6.2, 0, 3.4, C["red"], n=4, yaw=0.78)
    # rayo de luz
    b.prism_between((0, 0, zt + 2), (45, 12, zt + 4), 0.25, 1.1, C["light"], n=4)
    b.prism_between((0, 0, zt + 2), (-45, -12, zt + 4), 0.25, 1.1, C["light"], n=4)


def reef(b, rnd):
    C = pal()
    for i in range(10):
        bd.rock_mass(b, rnd.uniform(-70, 70), rnd.uniform(-32, 32), rnd.uniform(4, 9), rnd.uniform(1.2, 3.5), C["rock2"],
                     C["rock3"], seed=i, n=6, rings=3)
    for i in range(46):
        x, y = rnd.uniform(-75, 75), rnd.uniform(-35, 35)
        col = rnd.choice(C["coral"])
        h = rnd.uniform(1.5, 4)
        for k in range(rnd.randint(3, 4)):
            a = k / 3.5 * 6.28 + rnd.random()
            tip = Vector((x + math.cos(a) * h * 0.6, y + math.sin(a) * h * 0.6, h))
            b.prism_between((x, y, -0.8), tip, 0.35, 0.15, col, n=4, caps=False)
            b.blob(tip, 0.3, col, n=4, rings=2, jit=0)
        if b.tris() > 4200:
            break
    for i in range(7):
        bd.rock_mass(b, rnd.uniform(-70, 70), rnd.uniform(-32, 32), rnd.uniform(8, 14), 0.4, C["foam"], seed=i + 30, n=9, rings=2, jit=0.15)


def shipwreck(b, rnd):
    C = pal()
    b.push((0, 0, -2), 0.0)
    # casco medio hundido y escorado
    top = [Vector(p) for p in ((-26, -7, 10), (8, -7, 10), (26, 0, 15), (8, 7, 10), (-26, 7, 10))]
    bot = [Vector(p) for p in ((-24, -4, -4), (8, -4, -4), (24, 0, -2), (8, 4, -4), (-24, 4, -4))]
    tilt = Vector((0, 0, 0))
    b.loft([bot, top], C["wood2"], top=False, bottom=True)
    # costillas rotas
    for i in range(9):
        x = -22 + i * 3.6
        for s in (-1, 1):
            b.prism_between((x, s * 6.9, 10), (x + 0.4, s * 5.8, 17 - (i % 3) * 2), 0.28, 0.16, C["wood"], n=4)
    b.pop()
    b.box(-4, 0, 4, 12, 8, 6, C["wood"], top_m=C["wood2"], taper=0.9)
    b.prism_between((-2, 0, 6), (6, 2, 38), 0.7, 0.35, C["wood"], n=5)
    b.prism_between((4, 0, 28), (6.3, 11, 31), 0.3, 0.2, C["wood2"], n=4)
    bd.sail(b, Vector((5.9, 0.3, 36)), Vector((5.9, 0.3, 22)), Vector((13, 8, 24)), C["sail"])
    b.prism_between((-14, 0, 6), (-18, -1, 22), 0.4, 0.2, C["wood2"], n=5)
    for i in range(7):
        bd.rock_mass(b, rnd.uniform(-34, 34), rnd.uniform(-14, 14), rnd.uniform(3, 6), rnd.uniform(1, 3), C["rock2"], seed=i,
                     rings=3, n=6)
    bd.rock_mass(b, 0, 0, 36, 0.5, C["foam"], seed=9, n=12, rings=2, jit=0.1, ry=12)
    for i in range(14):
        p = Vector((rnd.uniform(-38, 38), rnd.uniform(-16, 16), 0.0))
        b.prism_between(p, p + Vector((rnd.uniform(-6, 6), rnd.uniform(-3, 3), rnd.uniform(0.3, 2))), 0.35, 0.3, C["wood2"], n=4)


def sailboats(b, rnd):
    C = pal()
    for i in range(10):
        x, y = -78 + i * 17 + rnd.uniform(-5, 5), rnd.uniform(-25, 25)
        yaw = rnd.uniform(-0.5, 0.5) + (math.pi if i % 2 else 0)
        s = rnd.uniform(0.9, 1.4)
        b.push((x, y, 0), yaw, s)
        bd.hull(b, 0, 0, 0, 12, 4, 2.2, 0, C["hull"][i % 4], C["wood"], C["white"])
        b.prism_between((0, 0, 1.4), (0, 0, 17), 0.16, 0.1, C["steel2"], n=4, caps=False)
        bd.sail(b, Vector((0.2, 0, 3)), Vector((0.2, 0, 16.5)), Vector((-5.6, 0, 3)), C["sail"])
        bd.sail(b, Vector((0.9, 0, 3)), Vector((0.9, 0, 15)), Vector((5.5, 0, 3.2)), C["sail2"] if i % 3 == 0 else C["sail"])
        b.pop()
    # goleta grande
    b.push((0, 38, 0), 0.4, 1.3)
    bd.hull(b, 0, 0, 0, 30, 7, 4.5, 0, C["navy"], C["wood"], C["red"])
    for k, mx in enumerate((-8, 6)):
        b.prism_between((mx, 0, 3), (mx, 0, 30), 0.3, 0.18, C["wood2"], n=4, caps=False)
        bd.sail(b, Vector((mx + 0.3, 0, 5)), Vector((mx + 0.3, 0, 28)), Vector((mx - 8, 0, 5)), C["sail"])
        bd.sail(b, Vector((mx + 0.5, 0, 6)), Vector((mx + 0.5, 0, 22)), Vector((mx + 8, 0, 6)), C["sail"])
    b.pop()


# ---------------------------------------------------------------------------
# Capa lejana
# ---------------------------------------------------------------------------
def far_islands(b, rnd):
    C = pal()
    x = -200
    for i in range(6):
        r = rnd.uniform(40, 70)
        h = rnd.uniform(28, 66)
        bd.rock_mass(b, x, rnd.uniform(-10, 10), r, h, C["rock"], C["moss"], seed=i, n=9, rings=4, jit=0.25, ry=r * 0.5)
        bd.rock_mass(b, x + r * 0.6, 8, r * 0.5, h * 0.7, C["rock2"], seed=i + 9, n=7, rings=3, ry=r * 0.3)
        bd.rock_mass(b, x, 0, r * 1.5, 0.8, C["foam"], seed=i + 5, n=10, rings=2, jit=0.08, ry=r * 0.5)
        x += r * 1.35 + rnd.uniform(10, 30)


def storm_clouds(b, rnd):
    C = pal()
    for row, (z, ry, tone) in enumerate(((90, 30, C["cloud"]), (130, 40, C["cloud2"]), (60, 24, C["cloud3"]))):
        x = -230
        while x < 230:
            rx = rnd.uniform(40, 80)
            bd.cloud(b, x, rnd.uniform(-20, 20) + row * 8, z + rnd.uniform(-8, 8), rx, ry, rnd.uniform(14, 26), tone,
                     C["cloud3"], seed=int(x) + row, n=8, rings=3)
            x += rx * 1.1
    # rayo
    pts = [Vector((60, -12, 78)), Vector((66, -12, 62)), Vector((59, -12, 50)), Vector((67, -12, 30)), Vector((61, -12, 12)),
           Vector((63, -12, 0))]
    for a, c in zip(pts[:-1], pts[1:]):
        b.prism_between(a, c, 0.9, 0.7, C["bolt"], n=4, caps=False)


def far_lighthouse(b, rnd):
    C = pal()
    bd.rock_mass(b, 0, 0, 46, 36, C["rock"], C["moss"], seed=4, jit=0.3, n=9, rings=4, ry=26)
    bd.rock_mass(b, -60, 4, 28, 20, C["rock2"], seed=8, n=7, rings=3, ry=16)
    bd.rock_mass(b, 0, 0, 62, 0.8, C["foam"], seed=7, n=12, rings=2, jit=0.1, ry=34)
    z = 32.0
    for i in range(6):
        b.cyl(0, 0, z + i * 9, 8.0 - i * 0.8, 8.0 - (i + 1) * 0.8, 9, C["red"] if i % 2 == 0 else C["white"], n=8)
    zt = z + 54
    b.cyl(0, 0, zt, 6.4, 6.4, 1.2, C["steel"], n=8)
    b.cyl(0, 0, zt + 1.2, 3.6, 3.6, 5.5, C["light"], n=8)
    b.blob((0, 0, zt + 4), 7.5, C["light"], n=8, rings=3, jit=0)
    b.cyl(0, 0, zt + 6.7, 5.2, 0, 5.0, C["red"], n=8)


def far_ships(b, rnd):
    C = pal()
    for sx, ln, col in ((-170, 130, C["navy"]), (30, 160, C["steel"]), (200, 100, C["red"])):
        bd.hull(b, sx, 0, 0, ln, ln * 0.14, 16, 0.0, col, C["steel2"], C["white"])
        b.box(sx - ln * 0.32, 0, 10, ln * 0.14, ln * 0.12, 18, C["white"], top_m=C["navy"])
        b.box(sx - ln * 0.32, 0, 28, ln * 0.1, ln * 0.1, 8, C["white"], top_m=C["navy"])
        b.cyl(sx - ln * 0.36, 0, 28, 4, 3, 10, C["red"], n=6)
        for k in range(5):
            b.box(sx - ln * 0.1 + k * ln * 0.12, 0, 11, ln * 0.1, ln * 0.11, 6, rnd.choice(C["hull"]))
    for i in range(3):
        b.push((-90 + i * 130, 40, 0), 0.2 * i, 2.4)
        bd.hull(b, 0, 0, 0, 10, 3.4, 2, 0, C["hull"][i], C["wood"], C["white"])
        b.prism_between((0, 0, 1.4), (0, 0, 15), 0.15, 0.1, C["steel2"], n=4, caps=False)
        bd.sail(b, Vector((0.2, 0, 3)), Vector((0.2, 0, 14.5)), Vector((-5, 0, 3)), C["sail"])
        bd.sail(b, Vector((0.9, 0, 3)), Vector((0.9, 0, 13)), Vector((5, 0, 3.2)), C["sail"])
        b.pop()


def wave_wall(b, rnd):
    C = pal()
    sw = lambda x, y: (10 + 7 * math.sin(x * 0.03 + 1.0) + 5 * math.sin(x * 0.07 + 2.0) + 3 * bd.fbm(x * 0.02, 0, 3, 2)) * \
        (0.5 + 0.5 * math.sin(y * 0.09 + x * 0.004)) * bd.smooth((60 - abs(y)) / 40) * bd.smooth((230 - abs(x)) / 60)
    b.sheet(-230, 230, -60, 60, 64, 8, lambda x, y: sw(x, y) - 1.0,
            lambda x, y, z, s: C["foam"] if z > 12.0 else (C["teal"] if z > 5 else C["deep"]))


def sea_cliffs(b, rnd):
    C = pal()
    x = -230
    for i in range(8):
        w = rnd.uniform(40, 70)
        h = rnd.uniform(40, 90)
        b.box(x + w / 2, rnd.uniform(-8, 8), -2, w, rnd.uniform(26, 40), h + 2, C["rock"] if i % 2 else C["rock2"],
              taper=rnd.uniform(0.55, 0.85), top_m=C["moss"])
        spk = rnd.randint(1, 2)
        for k in range(spk):
            bd.spire(b, x + rnd.uniform(0, w), rnd.uniform(-30, -18), -2, rnd.uniform(5, 9), rnd.uniform(18, 40), C["rock2"],
                     C["rock3"], n=5, seed=i * 3 + k)
        x += w * 0.85
    bd.rock_mass(b, 0, -26, 250, 0.8, C["foam"], seed=2, n=14, rings=2, jit=0.05, ry=8)


# ---------------------------------------------------------------------------
# Suelo / mar de relleno
# ---------------------------------------------------------------------------
def gm():
    return [M("#1E8F99"), M("#27A9AB"), M("#BFE7E4")]


def ground_a(b, rnd):
    bd.ground_tile(b, gm(), 1.0, amp=1.5, freq=0.025, patch=0.62)


def ground_b(b, rnd):
    bd.ground_tile(b, gm(), 9.0, amp=1.9, freq=0.02, patch=0.7)


def ground_skirt(b, rnd):
    bd.ground_skirt(b, [M("#2392A0"), M("#2B9FA6")], rise=0.2)


def horizon(b, rnd):
    bd.horizon_ring(b, [M("#3F8E9C"), M("#7FB6BE")], hmin=3, hmax=16, seed=5.0, base=-1.0)


PIECES = [
    ("Mid", "Islet_A", islet_a), ("Mid", "Islet_B", islet_b), ("Mid", "SeaStacks", sea_stacks),
    ("Mid", "FishingBoats", fishing_boats), ("Mid", "LighthouseIsle", lighthouse_isle), ("Mid", "Reef", reef),
    ("Mid", "Shipwreck", shipwreck), ("Mid", "Sailboats", sailboats),
    ("Far", "Islands", far_islands), ("Far", "StormClouds", storm_clouds), ("Far", "Lighthouse", far_lighthouse),
    ("Far", "Ships", far_ships), ("Far", "WaveWall", wave_wall), ("Far", "SeaCliffs", sea_cliffs),
    ("Ground", "TileA", ground_a), ("Ground", "TileB", ground_b), ("Ground", "Skirt", ground_skirt),
    ("Ground", "HorizonRing", horizon),
]

if __name__ == "__main__":
    bd.build_all(W, PIECES, only=bd.script_args())
