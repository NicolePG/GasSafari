"""Fondos W1 · Fuga del Zoológico (ciudad de noche). Paleta de docs/04: azul #1B2A4A, neón #FF4FA3 / #3AE1FF, farolas ámbar.
blender --background --python backdrops_w1.py [-- Mid_TowersA Far_Zoo ...]
Salida: Tools/Export/Backdrops/W1/W1_<Capa>_<Nombre>.fbx (Mid <= 5000 tris, Far <= 2000, Ground <= 2000)."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import backdrop_lib as bd
from backdrop_lib import B, M, E, Vector

W = "W1"


def pal():
    return dict(
        n1=M("#1B2A4A"), n2=M("#24365E"), n3=M("#2E4272"), n4=M("#162040"), roof=M("#131B33"), conc=M("#5A6585"),
        brick=M("#4A3552"), pink=E("#FF4FA3", 3.0), cyan=E("#3AE1FF", 3.0), amber=E("#FFB238", 2.6),
        warm=E("#FFE08A", 2.4), red=E("#FF3B4E", 3.0), green=M("#2E5E4C"), leaf=M("#2F6B55"), leaf2=M("#3C8068"),
        trunk=M("#3A2A3A"), steel=M("#3A4668"), steel2=M("#566086"), cont=[M("#C8483C"), M("#3E7CB8"), M("#E0A233"),
                                                                           M("#4AA070"), M("#8A5FB0")],
        hull=M("#2A2F4A"), hullr=M("#8E2F3E"), white=M("#B9C2DC"), glass=E("#79D8FF", 1.4))


def lit(C):
    return [C["amber"], C["amber"], C["warm"], C["cyan"], C["pink"]]


def walls(C):
    return [C["n1"], C["n2"], C["n3"], C["n4"]]


def antenna(C):
    def f(b, x, y, z, w, d):
        h = 10 + (abs(x * 7 + y * 3) % 9)
        b.prism_between((x, y, z), (x, y, z + h), 0.35, 0.12, C["steel2"], n=4, caps=False)
        b.blob((x, y, z + h + 0.4), 0.7, C["red"], n=4, rings=2, jit=0)
    return f


# ---------------------------------------------------------------------------
# Capa media
# ---------------------------------------------------------------------------
def towers_a(b, rnd):
    C = pal()
    slots = [(-34, -18), (-12, -22), (12, -20), (34, -16), (-30, 10), (-8, 14), (16, 12), (38, 8)]
    for i, (x, y) in enumerate(slots):
        if b.tris() > 4200:
            break
        h = rnd.uniform(34, 95)
        tower(b, C, x + rnd.uniform(-3, 3), y + rnd.uniform(-3, 3), rnd.uniform(11, 17), rnd.uniform(11, 17), h,
              rnd.uniform(-0.25, 0.25), rnd, steps=rnd.choice([1, 2, 2, 3]), prob=0.46)


def tower(b, C, x, y, w, d, h, yaw, rnd, steps=1, prob=0.42, rim=None):
    wall = rnd.choice(walls(C))
    top = bd.tower(b, x, y, w, d, h, yaw, wall, C["roof"], lit(C), rnd, steps=steps, prob=prob, crown=antenna(C))
    if rim is not None:
        # luces de contorno en el borde superior
        for s in (1, -1):
            b.box(x, y + s * d * 0.5, top - 0.6, w + 0.4, 0.3, 0.6, rim, yaw=yaw)
    return top


def towers_b(b, rnd):
    C = pal()
    tower(b, C, 0, 0, 20, 20, 150, 0.0, rnd, steps=3, prob=0.5, rim=C["cyan"])
    pts = [(-34, 6, 70), (32, 8, 62), (-26, -26, 48), (24, -24, 54), (0, -36, 40), (-50, -8, 38), (50, -10, 44)]
    for i, (x, y, h) in enumerate(pts):
        if b.tris() > 4300:
            break
        tower(b, C, x, y, rnd.uniform(11, 15), rnd.uniform(11, 15), h, rnd.uniform(-0.3, 0.3), rnd, steps=2,
              prob=0.5, rim=[C["pink"], C["cyan"]][i % 2])


def neon_block(b, rnd):
    C = pal()
    for i in range(7):
        x = -45 + i * 15 + rnd.uniform(-2, 2)
        y = rnd.uniform(-8, 8)
        h = rnd.uniform(14, 34)
        w = rnd.uniform(11, 14)
        wall = rnd.choice(walls(C) + [C["brick"]])
        b.box(x, y, 0, w, 14, h, wall, top_m=C["roof"])
        bd.windows(b, x, y, 0, w, 14, h, 0.0, lit(C), rnd, bay=3.4, floor=3.4, prob=0.5)
        # franja de neón en la fachada y letrero sobre la azotea
        col = C["pink"] if i % 2 == 0 else C["cyan"]
        b.box(x, y - 7.2, h * 0.55, w * 0.9, 0.4, 0.5, col)
        sw, sh = w * 0.8, 5 + rnd.random() * 3
        b.box(x, y - 3, h + 0.2, 0.5, 0.5, 2.6, C["steel"])
        b.box(x, y - 3.6, h + 2.4, sw, 0.6, sh, C["steel"])
        b.box(x, y - 3.95, h + 2.4 + sh * 0.12, sw * 0.86, 0.12, sh * 0.76, col)
        # toldo
        b.box(x, y - 8.2, 3.6, w * 0.8, 3, 0.4, C["pink"] if i % 2 else C["amber"], taper=0.95)
    # segunda fila baja con anuncios
    for i in range(5):
        x = -34 + i * 17
        b.box(x, 22, 0, 12, 10, 9 + (i % 3) * 3, rnd.choice(walls(C)), top_m=C["roof"])
        bd.windows(b, x, 22, 0, 12, 10, 9 + (i % 3) * 3, 0.0, lit(C), rnd, bay=3.2, floor=3.0, prob=0.55, skip_low=0)


def gantry(b, C, x, y, yaw, s=1.0):
    b.push((x, y, 0), yaw, s)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.prism_between((sx * 9, sy * 6, 0), (sx * 9, sy * 6, 30), 0.8, 0.6, C["steel2"])
    b.prism_between((-9, -6, 30), (-9, 6, 30), 0.6, 0.6, C["steel2"])
    b.prism_between((9, -6, 30), (9, 6, 30), 0.6, 0.6, C["steel2"])
    b.box(0, 0, 30, 26, 14, 2.4, C["steel2"])
    b.box(-9, 0, 15, 1.0, 14, 1.0, C["steel"])
    b.box(9, 0, 15, 1.0, 14, 1.0, C["steel"])
    # pluma inclinada
    b.prism_between((0, 0, 32), (-12, -40, 44), 1.0, 0.7, C["steel"], n=4)
    b.prism_between((0, 0, 32), (-12, 40, 40), 1.0, 0.7, C["steel"], n=4)
    b.prism_between((0, 0, 32), (0, 0, 52), 0.7, 0.4, C["steel"], n=4)
    b.prism_between((0, 0, 52), (-12, -40, 44), 0.2, 0.2, C["steel2"], n=4, caps=False)
    b.box(0, 0, 32.4, 6, 4, 4, C["red"], top_m=C["roof"])
    b.blob((0, 0, 53), 0.8, C["red"], n=4, rings=2, jit=0)
    b.pop()


def port_cranes(b, rnd):
    C = pal()
    for i, x in enumerate((-50, 0, 50)):
        gantry(b, C, x, -10, rnd.uniform(-0.1, 0.1), 0.9 + 0.1 * (i % 2))
    for r in range(3):
        for c in range(9):
            x = -62 + c * 15
            y = 22 + r * 14
            for lvl in range(rnd.choice([1, 2, 3, 4])):
                b.box(x + rnd.uniform(-0.3, 0.3), y, lvl * 2.7, 12, 2.5, 2.6, rnd.choice(C["cont"]), top_m=C["roof"])
    for x in (-30, 30):
        b.box(x, -34, 0, 70, 12, 1.2, C["conc"], top_m=C["steel"])
    for x in range(-60, 61, 12):
        b.prism_between((x, -26, 1.2), (x, -26, 7), 0.25, 0.2, C["steel"], n=4, caps=False)
        b.blob((x, -26, 7.4), 0.55, C["amber"], n=5, rings=2, jit=0)


def bridge_lit(b, rnd):
    C = pal()
    L, H = 150.0, 40.0
    b.box(0, 0, 11.0, L * 1.6, 12, 2.0, C["steel2"], top_m=C["n3"])
    # pilas
    for x in (-L * 0.45, L * 0.45):
        for s in (-1, 1):
            b.box(x, s * 5.5, 0, 3.4, 3.4, H, C["steel"], taper=0.8, top_m=C["roof"])
        b.box(x, 0, H * 0.82, 4, 14.5, 2.8, C["steel"])
        b.box(x, 0, H * 0.45, 3.2, 14.5, 2.0, C["steel"])
        b.box(x, 5.5, H, 1.8, 1.8, 1.2, C["red"])
        b.box(x, -5.5, H, 1.8, 1.8, 1.2, C["red"])
    # cables principales (catenaria) y péndolas
    xs = [i * (L * 0.9) / 20 - L * 0.45 for i in range(21)]
    for s in (-1, 1):
        prev = None
        for x in xs:
            t = (x + L * 0.45) / (L * 0.9)
            z = 14 + (H - 3 - 14) * (2 * (t - 0.5)) ** 2
            p = Vector((x, s * 5.5, z))
            if prev is not None:
                b.prism_between(prev, p, 0.35, 0.35, C["steel2"], n=4, caps=False)
            if int(round(t * 20)) % 2 == 0:
                b.prism_between((x, s * 5.5, 12.0), p, 0.12, 0.12, C["amber"], n=4, caps=False)
            prev = p
        # tramos exteriores
        b.prism_between((-L * 0.45, s * 5.5, H - 3), (-L * 0.8, s * 5.5, 12.5), 0.35, 0.35, C["steel2"], n=4, caps=False)
        b.prism_between((L * 0.45, s * 5.5, H - 3), (L * 0.8, s * 5.5, 12.5), 0.35, 0.35, C["steel2"], n=4, caps=False)
    # farolas del tablero
    for i in range(-16, 17):
        x = i * 4.6
        for s in (-1, 1):
            b.blob((x, s * 6.4, 14.0), 0.5, C["warm"], n=5, rings=2, jit=0)
            b.prism_between((x, s * 6.4, 12.9), (x, s * 6.4, 13.8), 0.14, 0.1, C["steel"], n=4, caps=False)
    # pilotes
    for x in (-L * 0.75, L * 0.75):
        b.box(x, 0, 0, 5, 12, 11, C["conc"], top_m=C["steel"])


def warehouses(b, rnd):
    C = pal()
    for i in range(9):
        x = -48 + (i % 3) * 38 + rnd.uniform(-3, 3)
        y = -40 + (i // 3) * 40
        w, d = rnd.uniform(22, 30), rnd.uniform(18, 26)
        h = rnd.uniform(9, 13)
        wall = rnd.choice([C["n1"], C["n2"], C["brick"], C["n4"]])
        b.box(x, y, 0, w, d, h, wall, top_m=C["roof"])
        # techo de dientes de sierra
        teeth = 5
        for t in range(teeth):
            tx = x - w / 2 + (t + 0.5) * w / teeth
            a = Vector((tx - w / teeth / 2, y - d / 2, h))
            c1 = Vector((tx + w / teeth / 2, y - d / 2, h))
            top1 = Vector((tx - w / teeth / 2, y - d / 2, h + 3))
            top2 = Vector((tx - w / teeth / 2, y + d / 2, h + 3))
            a2 = Vector((tx - w / teeth / 2, y + d / 2, h))
            c2 = Vector((tx + w / teeth / 2, y + d / 2, h))
            b.quad(a, c1, c2, a2, C["roof"])
            b.quad(a, a2, top2, top1, C["roof"])
            b.quad(top1, top2, c2, c1, C["roof"])
            b.tri(a, top1, c1, C["roof"])
            b.tri(a2, c2, top2, C["roof"])
            b.wall_quad((tx - w / teeth / 2 + 0.1, y, h + 1.5), (-1, 0, 0), d * 0.4, 0.8, C["amber"]) if t % 2 == 0 else None
        bd.windows(b, x, y, 0, w, d, h, 0.0, [C["amber"], C["warm"]], rnd, bay=5.0, floor=4.5, prob=0.5, skip_low=0)
        if i % 2 == 0:
            b.cyl(x + w / 2 + 4, y, 0, 3.2, 3.2, 16, C["steel2"], n=8, top=True)
            b.cyl(x - w / 2 - 5, y + 4, 0, 1.1, 0.9, 28, C["conc"], n=6)
            b.blob((x - w / 2 - 5, y + 4, 28.6), 0.6, C["red"], n=4, rings=2, jit=0)


def apartments(b, rnd):
    C = pal()
    for i in range(5):
        x = -50 + i * 25
        y = rnd.uniform(-6, 6)
        w, d, h = rnd.uniform(16, 22), 12, rnd.uniform(26, 48)
        wall = rnd.choice([C["n2"], C["brick"], C["n3"]])
        b.box(x, y, 0, w, d, h, wall, top_m=C["roof"])
        bd.windows(b, x, y, 0, w, d, h, 0.0, [C["amber"], C["warm"], C["warm"], C["cyan"]], rnd, bay=2.8, floor=3.0,
                   prob=0.5, sides=(0, 2), skip_low=0)
        # balcones
        for fl in range(2, int(h // 3), 3):
            b.box(x, y - d / 2 - 0.8, fl * 3.0, w * 0.9, 1.6, 0.35, C["conc"])
        b.cyl(x + w * 0.25, y, h, 1.8, 1.8, 3.0, C["steel"], n=6)
        b.cyl(x - w * 0.25, y, h, 1.8, 1.8, 3.0, C["steel"], n=6)
        b.box(x, y - d / 2 - 0.1, h - 3, w * 0.7, 0.3, 0.5, C["pink"] if i % 2 else C["cyan"])


def ferris(b, C, x, y, r, yaw=0.0, n=20):
    b.push((x, y, 0), yaw)
    cz = r + 4
    for s in (-1, 1):
        for sx in (-1, 1):
            b.prism_between((sx * r * 0.5, s * 1.2, 0), (0, s * 0.8, cz), 0.35, 0.3, C["steel2"], n=4)
    b.prism_between((0, -2, cz), (0, 2, cz), 0.5, 0.5, C["steel2"])
    for s in (-1, 1):
        prev0 = None
        for i in range(n + 1):
            a = i / n * 2 * math.pi
            p = Vector((math.cos(a) * r, s * 0.8, cz + math.sin(a) * r))
            if prev0 is not None:
                b.prism_between(prev0, p, 0.18, 0.18, C["cyan"] if s > 0 else C["pink"], n=4, caps=False)
            prev0 = p
        for i in range(0, n, 2):
            a = i / n * 2 * math.pi
            b.prism_between((0, s * 0.8, cz), (math.cos(a) * r, s * 0.8, cz + math.sin(a) * r), 0.07, 0.07,
                            C["steel2"], n=4, caps=False)
    for i in range(0, n, 2):
        a = i / n * 2 * math.pi
        b.blob((math.cos(a) * r, 0, cz + math.sin(a) * r - 0.8), (0.9, 0.8, 0.7), [C["amber"], C["pink"], C["cyan"]][i // 2 % 3],
               n=5, rings=2, jit=0)
    b.pop()


def zoo_gate(b, C, x, y, s=1.0, yaw=0.0):
    b.push((x, y, 0), yaw, s)
    for sx in (-1, 1):
        b.box(sx * 7, 0, 0, 3.4, 3.4, 11, C["conc"], taper=0.85, top_m=C["roof"])
        b.cyl(sx * 7, 0, 11, 1.8, 0.0, 3, C["pink"], n=6)
    b.box(0, 0, 9, 18, 2.6, 3.6, C["n3"], top_m=C["roof"])
    b.box(0, -1.4, 9.5, 14, 0.2, 2.6, C["amber"])
    for k in range(-3, 4):
        b.blob((k * 2.0, -1.5, 13.1 - (3 - abs(k)) * 0.0), 0.35, C["warm"], n=4, rings=2, jit=0)
    b.pop()


def zoo_park(b, rnd):
    C = pal()
    ferris(b, C, -22, -8, 20, 0.3)
    zoo_gate(b, C, 30, -26, 1.4)
    for i in range(34):
        x, y = rnd.uniform(-60, 60), rnd.uniform(-34, 34)
        if abs(x + 22) < 14 and abs(y + 8) < 6:
            continue
        h = rnd.uniform(4, 10)
        b.cyl(x, y, 0, 0.5, 0.35, h * 0.55, C["trunk"], n=5)
        b.blob((x, y, h * 0.8), (h * 0.38, h * 0.38, h * 0.34), rnd.choice([C["leaf"], C["leaf2"]]), n=7, rings=3,
               jit=0.22, seed=i)
        if b.tris() > 4300:
            break
    for i in range(16):
        x = -56 + i * 7.5
        b.prism_between((x, 12, 0), (x, 12, 6), 0.2, 0.15, C["steel"], n=4, caps=False)
        b.blob((x, 12, 6.4), 0.6, C["amber"], n=5, rings=2, jit=0)
    # cúpula del recinto
    b.blob((26, 12, 0), (13, 13, 9), C["glass"], n=10, rings=3, jit=0, flat_bottom=0.0)
    b.cyl(26, 12, 0, 1.0, 1.0, 14, C["steel2"], n=5)


# ---------------------------------------------------------------------------
# Capa lejana (siluetas de poco detalle, <= 2000 triángulos)
# ---------------------------------------------------------------------------
def skyline(b, rnd, seed_shift, tall, spread=420, count=34, spires=False):
    C = pal()
    mats = [C["n4"], C["n1"], C["n2"]]
    for row, (yy, hk, wk) in enumerate(((16, 1.0, 1.0), (-14, 0.55, 0.8))):
        x = -spread / 2
        k = 0
        while x < spread / 2 and k < count:
            w = rnd.uniform(14, 28) * wk
            h = rnd.uniform(25, 75) * (tall * (0.5 + 0.6 * math.sin((x / spread + 0.5) * math.pi * 3 + seed_shift + row) ** 2)) * hk
            h = max(h, 18)
            d = rnd.uniform(14, 24)
            wall = rnd.choice(mats)
            b.box(x + w / 2, yy + rnd.uniform(-4, 4), 0, w, d, h, wall, taper=rnd.choice([1, 1, 0.88, 0.7]), top_m=C["roof"])
            if rnd.random() < 0.7 and b.tris() < 1750:
                for _ in range(int(h / 12)):
                    b.wall_quad((x + w / 2 + rnd.uniform(-w * 0.3, w * 0.3), yy - d / 2 - 0.5, rnd.uniform(5, h - 3)),
                                (0, -1, 0), 1.7, 1.4, rnd.choice(lit(C)))
            if spires and rnd.random() < 0.35:
                b.cyl(x + w / 2, yy, h * 0.9, 1.2, 0, h * 0.4, C["steel"], n=4)
            x += w + rnd.uniform(0, 6)
            k += 1


def skyline_a(b, rnd):
    skyline(b, rnd, 0.0, 1.0)


def skyline_b(b, rnd):
    skyline(b, rnd, 2.1, 1.5, spires=True, count=30)


def far_harbor(b, rnd):
    C = pal()
    for i, x in enumerate((-150, -70, 20, 110)):
        s = 1.6
        gantry(b, C, x, -10, 0, s)
    x = -200
    while x < 200:
        for lvl in range(rnd.choice([1, 2, 2, 3])):
            b.box(x, 22, lvl * 2.7, 14, 2.6, 2.6, rnd.choice(C["cont"]), top_m=C["roof"])
        x += 15
    # buques
    for sx, ln in ((-110, 90), (130, 120)):
        b.box(sx, -34, 0, ln, 16, 12, C["hull"], taper=0.94)
        b.box(sx, -34, 0, ln, 16.2, 3, C["hullr"])
        b.box(sx + ln * 0.34, -34, 12, 24, 14, 14, C["white"], top_m=C["roof"])
        b.box(sx + ln * 0.34, -34, 26, 12, 10, 8, C["white"], top_m=C["roof"])
        for k in range(6):
            b.box(sx - ln * 0.4 + k * ln * 0.11, -34, 12, 10, 12, 4, rnd.choice(C["cont"]))
    for x in range(-180, 181, 24):
        b.blob((x, -50, 7), 0.9, C["amber"], n=4, rings=2, jit=0)


def far_zoo(b, rnd):
    C = pal()
    ferris(b, C, 60, 0, 42, 0.0, n=22)
    zoo_gate(b, C, -70, -4, 3.2)
    # recintos con cúpulas y carpas
    for i, (x, r) in enumerate(((-10, 24), (20, 16), (-135, 20), (125, 18))):
        b.blob((x, 8, 0), (r, r * 0.9, r * 0.6), C["n3"], n=9, rings=3, jit=0, flat_bottom=0.0)
        b.blob((x, 8, r * 0.58), 2.4, C["amber"], n=5, rings=2, jit=0)
    for x in (-45, 3):
        b.cyl(x, 6, 0, 8.0, 0, 26, C["pink"], n=7)
    for i in range(34):
        x = -190 + i * 11 + rnd.uniform(-3, 3)
        h = rnd.uniform(9, 16)
        b.blob((x, rnd.uniform(-14, 14), h * 0.55), (h * 0.5, h * 0.5, h * 0.55), C["leaf"], n=6, rings=2, jit=0.2, seed=i)


def far_bridge(b, rnd):
    C = pal()
    L, H = 360.0, 110.0
    b.box(0, 0, 26, L * 1.8, 16, 4, C["steel2"], top_m=C["n3"])
    for x in (-L * 0.45, L * 0.45):
        for s in (-1, 1):
            b.box(x, s * 7, 0, 7, 6, H, C["steel"], taper=0.7, top_m=C["roof"])
        b.box(x, 0, H * 0.8, 7, 20, 5, C["steel"])
        b.box(x, 0, H * 0.45, 7, 20, 4, C["steel"])
        b.blob((x, 7, H + 2), 2.2, C["red"], n=4, rings=2, jit=0)
        b.blob((x, -7, H + 2), 2.2, C["red"], n=4, rings=2, jit=0)
    for s in (-1, 1):
        prev = None
        for i in range(17):
            t = i / 16
            x = -L * 0.45 + t * L * 0.9
            z = 34 + (H - 6 - 34) * (2 * (t - 0.5)) ** 2
            p = Vector((x, s * 7, z))
            if prev is not None:
                b.prism_between(prev, p, 0.8, 0.8, C["steel2"], n=4, caps=False)
            if i % 2 == 0:
                b.prism_between((x, s * 7, 30), p, 0.3, 0.3, C["amber"], n=4, caps=False)
            prev = p
        b.prism_between((-L * 0.45, s * 7, H - 6), (-L * 0.88, s * 7, 30), 0.8, 0.8, C["steel2"], n=4, caps=False)
        b.prism_between((L * 0.45, s * 7, H - 6), (L * 0.88, s * 7, 30), 0.8, 0.8, C["steel2"], n=4, caps=False)
    for x in (-L * 0.85, L * 0.85):
        b.box(x, 0, 0, 14, 16, 26, C["conc"], top_m=C["steel"])
    for i in range(-30, 31):
        b.blob((i * 5.7, 8.5, 31), 0.9, C["warm"], n=4, rings=2, jit=0)


def far_hills(b, rnd):
    C = pal()
    b.sheet(-260, 260, -40, 40, 52, 8,
            lambda x, y: (36 + 30 * bd.fbm(x * 0.012, 0, 2.0, 3)) * bd.smooth((40 - abs(y)) / 36) * (0.55 + 0.45 * bd.smooth(
                (260 - abs(x)) / 90)) - 1.0,
            lambda x, y, z, s: C["n1"] if z < 38 else C["n2"])
    for i in range(34):
        x = rnd.uniform(-230, 230)
        z = (36 + 30 * bd.fbm(x * 0.012, 0, 2.0, 3)) * (0.55 + 0.45 * bd.smooth((260 - abs(x)) / 90))
        b.blob((x, -22, z * 0.55 + rnd.uniform(0, 4)), 1.4, rnd.choice(lit(C)), n=4, rings=2, jit=0)


# ---------------------------------------------------------------------------
# Suelo de relleno
# ---------------------------------------------------------------------------
def gmats():
    return [M("#1D2A4C"), M("#243562"), M("#2A5A4A")]


def ground_a(b, rnd):
    bd.ground_tile(b, gmats(), 1.0, amp=0.5)


def ground_b(b, rnd):
    bd.ground_tile(b, gmats(), 7.0, amp=0.9, freq=0.014, patch=0.2)


def ground_skirt(b, rnd):
    bd.ground_skirt(b, [M("#1B2745"), M("#202E55")])


def horizon(b, rnd):
    bd.horizon_ring(b, [M("#141D38"), M("#1B2850")], hmin=30, hmax=130, seed=1.7)


PIECES = [
    ("Mid", "TowersA", towers_a), ("Mid", "TowersB", towers_b), ("Mid", "NeonBlock", neon_block),
    ("Mid", "PortCranes", port_cranes), ("Mid", "BridgeLit", bridge_lit), ("Mid", "Warehouses", warehouses),
    ("Mid", "Apartments", apartments), ("Mid", "ZooPark", zoo_park),
    ("Far", "SkylineA", skyline_a), ("Far", "SkylineB", skyline_b), ("Far", "Harbor", far_harbor),
    ("Far", "Zoo", far_zoo), ("Far", "Bridge", far_bridge), ("Far", "Hills", far_hills),
    ("Ground", "TileA", ground_a), ("Ground", "TileB", ground_b), ("Ground", "Skirt", ground_skirt),
    ("Ground", "HorizonRing", horizon),
]

if __name__ == "__main__":
    bd.build_all(W, PIECES, only=bd.script_args())
