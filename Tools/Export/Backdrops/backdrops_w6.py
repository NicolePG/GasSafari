"""Fondos W6 · Reino de Julien (fiesta nocturna). Paleta de docs/04: magenta #F72585, lima #B5E48C.
blender --background --python backdrops_w6.py [-- Mid_Stage_A Far_Fireworks ...]
Salida: Tools/Export/Backdrops/W6/W6_<Capa>_<Nombre>.fbx. Suelo / línea de agua en z = 0.
Mid_Fireworks, Far_Fireworks y Far_Balloons llevan los elementos ya elevados (z 40-220): se colocan con y = 0."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backdrop_lib as bd
from backdrop_lib import B, M, E, Vector

W = "W6"


def pal():
    return dict(
        mg=E("#F72585", 3.0), lime=E("#B5E48C", 2.8), cy=E("#4CC9F0", 3.0), yl=E("#FFD60A", 3.0), or_=E("#FF7B00", 3.0),
        vio=E("#9B5DE5", 3.0), wh=E("#FFF6FF", 3.2), dark=M("#14072A"), dark2=M("#2A0F45"), dark3=M("#3A1260"),
        steel=M("#3B3552"), steel2=M("#6A6488"), mgm=M("#C21A6B"), limem=M("#7FB05A"), cym=M("#2F8FB5"), yelm=M("#D9A800"),
        violm=M("#6A3FA8"), white=M("#E9E2F5"), gold=E("#FFC83A", 2.2), goldm=M("#E0A21C"), cake=M("#FFD7E8"),
        ball=[M("#F72585"), M("#4CC9F0"), M("#FFD60A"), M("#B5E48C"), M("#9B5DE5"), M("#FF7B00")],
        glow=[E("#F72585", 3.0), E("#4CC9F0", 3.0), E("#FFD60A", 3.0), E("#B5E48C", 3.0), E("#9B5DE5", 3.0), E("#FF7B00", 3.0)])


def truss_leg(b, C, x, y, z0, z1, w=2.0):
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.prism_between((x + sx * w, y + sy * w, z0), (x + sx * w, y + sy * w, z1), 0.28, 0.28, C["steel2"], n=4, caps=False)
    n = max(2, int((z1 - z0) / 6))
    for i in range(n):
        za, zb = z0 + (z1 - z0) * i / n, z0 + (z1 - z0) * (i + 1) / n
        b.prism_between((x - w, y - w, za), (x + w, y - w, zb), 0.12, 0.12, C["steel2"], n=3, caps=False)
        b.prism_between((x + w, y + w, za), (x - w, y + w, zb), 0.12, 0.12, C["steel2"], n=3, caps=False)


def beam(b, p0, p1, m, r0=0.4, r1=2.6):
    b.prism_between(p0, p1, r0, r1, m, n=4, caps=False)


def burst(b, c, R, mats, rnd, n=16, th=0.35):
    c = Vector(c)
    for i in range(n):
        y = 1 - 2 * (i + 0.5) / n
        rr = math.sqrt(max(0, 1 - y * y))
        a = i * 2.39996
        d = Vector((math.cos(a) * rr, math.sin(a) * rr, y))
        m = mats[i % len(mats)]
        b.prism_between(c + d * R * 0.4, c + d * R, th * R / 14, th * R / 40, m, n=3, caps=False)
        b.blob(c + d * R * 1.02, R * 0.045, m, n=4, rings=2, jit=0)
    b.blob(c, R * 0.07, mats[0], n=5, rings=2, jit=0)


# ---------------------------------------------------------------------------
# Capa media
# ---------------------------------------------------------------------------
def stage(b, C, x, y, s, yaw, col, colm):
    b.push((x, y, 0), yaw, s)
    b.box(0, 0, 0, 36, 22, 3, C["steel"], top_m=C["dark3"])
    for sx in (-1, 1):
        truss_leg(b, C, sx * 16, 8, 3, 26)
        b.box(sx * 22, -2, 3, 6, 6, 10, C["dark"], top_m=C["steel"])
        b.wall_quad((sx * 22, -5.1, 8), (0, -1, 0), 2.2, 3.5, C["cy"])
    b.box(0, 8, 24, 36, 4, 4, C["steel2"])
    b.box(0, 9.8, 6, 28, 0.6, 16, C["dark"])
    b.wall_quad((0, 9.4, 14), (0, -1, 0), 12.5, 6.8, col)
    b.wall_quad((0, 9.4, 14), (0, -1, 0), 8, 3.6, C["wh"])
    for k in range(7):
        xx = -15 + k * 5
        b.blob((xx, 5, 23), 0.8, C["glow"][k % 6], n=4, rings=2, jit=0)
        if k % 2 == 0:
            beam(b, (xx, 5, 23), (xx + (k - 3) * 9, -30, 52), C["glow"][(k + 1) % 6], 0.2, 1.1)
    b.cyl(0, 6, 26, 20, 0, 4, colm, n=4, yaw=0.78, ry_scale=0.7)
    b.pop()


def stage_a(b, rnd):
    C = pal()
    stage(b, C, 0, 0, 1.4, 0.0, C["mg"], C["mgm"])
    for i in range(16):
        x = rnd.uniform(-60, 60)
        bd.rock_mass(b, x, rnd.uniform(-45, -22), 1.4, 1.4, C["ball"][i % 6], seed=i, n=5, rings=2, jit=0)
    for i in range(60):
        b.blob((rnd.uniform(-34, 34), rnd.uniform(-34, -14), 1.0), (0.7, 0.7, 1.0), C["ball"][i % 6], n=4, rings=2, jit=0)
    for i in range(6):
        x = -50 + i * 20
        b.box(x, -44, 0, 3, 3, 12, C["steel"], top_m=C["dark3"])
        b.wall_quad((x, -45.6, 8), (0, -1, 0), 1.0, 1.0, C["glow"][i % 6])
    for i in range(8):
        x = -42 + i * 12
        b.prism_between((x, -30, 0), (x, -30, 7), 0.2, 0.15, C["steel2"], n=4, caps=False)
        b.blob((x, -30, 7.4), 0.7, C["glow"][i % 6], n=5, rings=2, jit=0)


def stage_b(b, rnd):
    C = pal()
    b.cyl(0, 0, 0, 28, 28, 3, C["steel"], n=12, top=True)
    b.blob((0, 0, 3), (26, 26, 24), C["violm"], n=12, rings=4, jit=0, flat_bottom=0.0,
           ms=[C["violm"], C["mgm"], C["violm"]])
    for k in range(12):
        a = k / 12 * 6.28
        b.prism_between((math.cos(a) * 27, math.sin(a) * 27, 3), (0, 0, 27), 0.3, 0.3, C["glow"][k % 6], n=3, caps=False)
    for k in range(0, 6, 2):
        a = k / 6 * 6.28
        beam(b, (math.cos(a) * 20, math.sin(a) * 20, 14), (math.cos(a) * 95, math.sin(a) * 95, 60 + 20 * (k % 2)), C["glow"][k], 0.2, 1.1)
    b.box(0, -27, 0, 14, 4, 12, C["dark"], top_m=C["steel"])
    b.wall_quad((0, -29.1, 6), (0, -1, 0), 5, 4, C["lime"])
    for i in range(40):
        a = rnd.uniform(0, 6.28)
        bd.rock_mass(b, math.cos(a) * rnd.uniform(34, 70), math.sin(a) * rnd.uniform(34, 70), 0.8, 1.0, C["ball"][i % 6], seed=i, n=4, rings=2, jit=0)
    for i in range(10):
        a = i / 10 * 6.28 + 0.3
        r = rnd.uniform(40, 60)
        bd.rock_mass(b, math.cos(a) * r, math.sin(a) * r * 0.6, 2.2, 2.2, C["ball"][i % 6], seed=i, n=6, rings=3, jit=0)


def light_towers(b, rnd):
    C = pal()
    for i, x in enumerate((-58, -30, 0, 30, 58)):
        h = rnd.uniform(40, 70)
        y = rnd.uniform(-12, 12)
        truss_leg(b, C, x, y, 0, h, 2.2)
        b.box(x, y, h, 7, 7, 2.5, C["steel"], top_m=C["dark3"])
        for k in range(1):
            a = k / 2 * 6.28 + i
            hp = Vector((x + math.cos(a) * 2.6, y + math.sin(a) * 2.6, h + 3))
            b.blob(hp, 1.0, C["glow"][(i + k) % 6], n=5, rings=2, jit=0)
            beam(b, hp, hp + Vector((math.cos(a) * 30, math.sin(a) * 30 - 10, 55)), C["glow"][(i + k) % 6], 0.2, 1.2)
    for i in range(10):
        b.box(rnd.uniform(-65, 65), rnd.uniform(-28, -16), 0, 3, 3, 2, C["steel"], top_m=C["glow"][i % 6])


def balloons(b, rnd):
    C = pal()
    for i in range(18):
        x, y = rnd.uniform(-60, 60), rnd.uniform(-26, 26)
        r = rnd.uniform(4.5, 9)
        z = r * 0.9 + rnd.uniform(0, 14)
        m = C["ball"][i % 6]
        b.blob((x, y, z + r), (r, r, r * 1.2), m, n=10, rings=5, jit=0, seed=i)
        b.cyl(x, y, z - 0.4, 0.7, 0.0, -1, m, n=3) if False else None
        b.cyl(x, y, z, 1.0, 0.3, -1.0, m, n=4, top=False) if False else None
        b.tri(Vector((x - 0.8, y, z + 0.5)), Vector((x + 0.8, y, z + 0.5)), Vector((x, y, z - 0.9)), m)
        b.tri(Vector((x + 0.8, y, z + 0.5)), Vector((x - 0.8, y, z + 0.5)), Vector((x, y, z - 0.9)), m)
        b.prism_between((x, y, z - 0.9), (x + rnd.uniform(-2, 2), y + rnd.uniform(-2, 2), 0.0), 0.12, 0.1, C["white"], n=3, caps=False)
        if b.tris() > 4500:
            break


def fireworks(b, rnd):
    C = pal()
    for i in range(4):
        burst(b, (-70 + i * 44 + rnd.uniform(-6, 6), rnd.uniform(-16, 16), rnd.uniform(48, 95)), rnd.uniform(18, 30),
              [C["glow"][(i + k) % 6] for k in range(3)], rnd, n=20)
    for i in range(7):
        x = -60 + i * 20
        b.prism_between((x, -20, 0), (x, -20, 5), 0.5, 0.4, C["steel"], n=4)
        b.prism_between((x, -20, 5), (x + rnd.uniform(-8, 8), rnd.uniform(-8, 8), rnd.uniform(35, 60)), 0.1, 0.08, C["yl"], n=3, caps=False)


def tent(b, C, x, y, r, h, cols, n=10):
    b.cyl(x, y, 0, r, r, h * 0.45, C["white"], n=n)
    top = Vector((x, y, h))
    ringp = bd.ring(x, y, h * 0.45, r * 1.12, r * 1.12, n, 0.0)
    for i in range(n):
        b.tri(ringp[i], ringp[(i + 1) % n], top, cols[i % len(cols)])
    b.prism_between(top, top + Vector((0, 0, r * 0.5)), 0.2, 0.1, C["steel2"], n=3, caps=False)
    b.tri(top + Vector((0, 0, r * 0.5)), top + Vector((r * 0.4, 0, r * 0.35)), top + Vector((0, 0, r * 0.2)), C["mg"])
    b.tri(top + Vector((0, 0, r * 0.5)), top + Vector((0, 0, r * 0.2)), top + Vector((r * 0.4, 0, r * 0.35)), C["mg"])


def party_tents(b, rnd):
    C = pal()
    for i in range(6):
        x = -52 + (i % 3) * 52 + rnd.uniform(-6, 6)
        y = -14 + (i // 3) * 30 + rnd.uniform(-5, 5)
        tent(b, C, x, y, rnd.uniform(8, 13), rnd.uniform(16, 26), [C["mgm"], C["white"], C["cym"], C["white"]] if i % 2 else [C["limem"], C["white"], C["violm"], C["white"]])
        for k in range(8):
            a = k / 8 * 6.28
            b.blob((x + math.cos(a) * 9.5, y + math.sin(a) * 9.5, 7.5), 0.55, C["glow"][k % 6], n=4, rings=2, jit=0)
    # guirnaldas de luces
    for row in range(3):
        for k in range(26):
            t = k / 25
            x = -66 + t * 132
            z = 9 - 4 * math.sin(t * math.pi) + row * 4
            b.blob((x, -30 + row * 30, z), 0.45, C["glow"][(k + row) % 6], n=4, rings=2, jit=0)
        for x in (-66, 66):
            b.prism_between((x, -30 + row * 30, 0), (x, -30 + row * 30, 10 + row * 4), 0.25, 0.2, C["steel2"], n=4, caps=False)


def gifts(b, rnd):
    C = pal()
    for i in range(10):
        x, y = rnd.uniform(-58, 58), rnd.uniform(-24, 24)
        s = rnd.uniform(6, 15)
        bm = C["ball"][i % 6]
        rb = C["glow"][(i + 3) % 6]
        b.box(x, y, 0, s, s, s * 0.8, bm, yaw=rnd.uniform(0, 1.5), top_m=bm)
        b.box(x, y, 0, s * 1.02, s * 0.2, s * 0.82, rb, yaw=0)
        b.box(x, y, 0, s * 0.2, s * 1.02, s * 0.82, rb, yaw=0)
        for sg in (-1, 1):
            b.blob((x + sg * s * 0.16, y, s * 0.8 + s * 0.12), (s * 0.2, s * 0.08, s * 0.14), rb, n=6, rings=3, jit=0)
        if b.tris() > 4500:
            break
    # pastel gigante
    for k, (r, h) in enumerate(((16, 8), (11, 7), (6.5, 6))):
        b.cyl(0, 36, sum(hh for _, hh in ((16, 8), (11, 7), (6.5, 6))[:k]), r, r, h, C["cake"] if k % 2 == 0 else C["mgm"], n=12)
    b.prism_between((0, 36, 21), (0, 36, 27), 0.3, 0.2, C["white"], n=4, caps=False)
    b.blob((0, 36, 27.8), 0.8, C["yl"], n=5, rings=3, jit=0)


def neon_arches(b, rnd):
    C = pal()
    for i, x in enumerate((-48, -16, 16, 48)):
        R = 15 + 3 * (i % 2)
        for sd in (0.0, 1.5):
            prev = None
            for k in range(15):
                a = k / 14 * math.pi
                p = Vector((x + R * math.cos(a), sd, R * math.sin(a) + 1))
                if prev is not None:
                    b.prism_between(prev, p, 0.7, 0.7, C["glow"][(i + int(sd * 2)) % 6], n=4, caps=False)
                prev = p
        for k in range(6):
            a = k / 5 * math.pi
            b.blob((x + R * math.cos(a), 3.0, R * math.sin(a) + 1), 0.9, C["glow"][(k + i) % 6], n=4, rings=2, jit=0)
    for i in range(9):
        x = -60 + i * 15
        b.prism_between((x, -22, 0), (x, -22, 9), 0.3, 0.2, C["steel2"], n=4, caps=False)
        b.blob((x, -22, 9.6), 1.1, C["glow"][i % 6], n=5, rings=2, jit=0)
        b.blob((x + 7.5, -22, 12), 0.9, C["glow"][(i + 2) % 6], n=5, rings=2, jit=0)


# ---------------------------------------------------------------------------
# Capa lejana
# ---------------------------------------------------------------------------
def far_stages(b, rnd):
    C = pal()
    for i, x in enumerate((-170, -60, 70, 180)):
        s = rnd.uniform(2.4, 3.4)
        stage(b, C, x, 0, s, rnd.uniform(-0.15, 0.15), C["glow"][i % 6], C["mgm"])
        if b.tris() > 1400:
            break
    for i in range(6):
        b.box(-200 + i * 80, -40, 0, 40, 4, 30 + 10 * (i % 3), C["dark3"], top_m=C["glow"][i % 6])


def far_towers(b, rnd):
    C = pal()
    for i in range(9):
        x = -230 + i * 58 + rnd.uniform(-8, 8)
        h = rnd.uniform(80, 130)
        for sx in (-1, 1):
            for sy in (-1, 1):
                b.prism_between((x + sx * 4, sy * 4, 0), (x + sx * 3, sy * 3, h), 0.9, 0.7, C["steel2"], n=4, caps=False)
        b.box(x, 0, h, 12, 12, 4, C["steel"], top_m=C["dark3"])
        b.blob((x, 0, h + 6), 3.0, C["glow"][i % 6], n=6, rings=2, jit=0)
        beam(b, (x, 0, h + 6), (x + rnd.uniform(-70, 70), rnd.uniform(-30, 10), h + 200), C["glow"][(i + 2) % 6], 0.7, 3.2)


def far_fireworks(b, rnd):
    C = pal()
    for i in range(4):
        burst(b, (-180 + i * 120 + rnd.uniform(-20, 20), rnd.uniform(-10, 10), rnd.uniform(130, 210)), rnd.uniform(55, 85),
              [C["glow"][(i * 2 + k) % 6] for k in range(3)], rnd, n=24, th=0.5)


def far_balloons(b, rnd):
    C = pal()
    for i in range(34):
        x = rnd.uniform(-230, 230)
        z = rnd.uniform(40, 190)
        r = rnd.uniform(8, 18)
        m = C["ball"][i % 6]
        b.blob((x, rnd.uniform(-20, 20), z), (r, r, r * 1.2), m, n=8, rings=4, jit=0)


def palace(b, rnd):
    C = pal()
    b.box(0, 0, 0, 170, 60, 38, C["dark3"], taper=0.9, top_m=C["violm"])
    for k in range(5):
        x = -70 + k * 35
        h = 70 + (30 if k == 2 else 0) + rnd.uniform(0, 20)
        b.cyl(x, -10, 0, 12, 10, h, C["violm"] if k % 2 else C["mgm"], n=8)
        b.cyl(x, -10, h, 15, 0, 28, C["glow"][k % 6], n=8)
        for z in range(10, int(h), 14):
            b.wall_quad((x, -20.2, z), (0, -1, 0), 2.4, 3.2, C["gold"])
    # corona gigante
    cz = 150.0
    b.cyl(0, 0, cz - 10, 20, 24, 12, C["goldm"], n=10)
    for k in range(5):
        a = k / 5 * 2 * math.pi
        b.cyl(math.cos(a) * 21, math.sin(a) * 21, cz, 5, 0, 22, C["gold"], n=5)
        b.blob((math.cos(a) * 21, math.sin(a) * 21, cz + 24), 3.4, C["glow"][k % 6], n=5, rings=2, jit=0)
    b.cyl(0, 0, 38, 12, 12, cz - 48, C["steel"], n=8)
    for x in (-110, 110):
        b.cyl(x, 0, 0, 8, 6, 60, C["mgm"], n=8)
        b.cyl(x, 0, 60, 10, 0, 22, C["gold"], n=8)


def party_hills(b, rnd):
    C = pal()
    fn = lambda x, y: (22 + 26 * (0.5 + 0.5 * bd.fbm(x * 0.012, 1, 3, 3))) * bd.smooth((45 - abs(y)) / 38) * (0.5 + 0.5 * bd.smooth((240 - abs(x)) / 70)) - 1.0
    b.sheet(-240, 240, -45, 45, 56, 8, fn, lambda x, y, z, s: C["dark3"] if z < 24 else C["violm"])
    for i in range(60):
        x = rnd.uniform(-230, 230)
        z = fn(x, 0) * rnd.uniform(0.7, 1.0) + 1
        b.blob((x, rnd.uniform(-22, -6), z), 1.6, C["glow"][i % 6], n=4, rings=2, jit=0)
        if b.tris() > 1900:
            break


# ---------------------------------------------------------------------------
# Suelo
# ---------------------------------------------------------------------------
def gm():
    return [M("#24103F"), M("#331558"), M("#3C7A4A")]


def ground_a(b, rnd):
    bd.ground_tile(b, gm(), 1.0, amp=0.5, patch=0.55)


def ground_b(b, rnd):
    bd.ground_tile(b, gm(), 4.0, amp=0.9, freq=0.015, patch=0.6)


def ground_skirt(b, rnd):
    bd.ground_skirt(b, [M("#1B0A33"), M("#220C3E")])


def horizon(b, rnd):
    bd.horizon_ring(b, [M("#1B0A33"), M("#3A1260")], hmin=30, hmax=120, seed=9.3)


PIECES = [
    ("Mid", "Stage_A", stage_a), ("Mid", "Stage_B", stage_b), ("Mid", "LightTowers", light_towers),
    ("Mid", "Balloons", balloons), ("Mid", "Fireworks", fireworks), ("Mid", "Tents", party_tents),
    ("Mid", "Gifts", gifts), ("Mid", "NeonArches", neon_arches),
    ("Far", "Stages", far_stages), ("Far", "LightTowers", far_towers), ("Far", "Fireworks", far_fireworks),
    ("Far", "Balloons", far_balloons), ("Far", "Palace", palace), ("Far", "PartyHills", party_hills),
    ("Ground", "TileA", ground_a), ("Ground", "TileB", ground_b), ("Ground", "Skirt", ground_skirt),
    ("Ground", "HorizonRing", horizon),
]

if __name__ == "__main__":
    bd.build_all(W, PIECES, only=bd.script_args())
