"""Escenario W3 · Selva de Madagascar (playa y selva). docs/09 §3 fila "Escenario W3", paleta de docs/04:
verde #2D6A4F, naranja #F77F00, amarillo; acento complementario magenta y violeta.
blender --background --python env_w3.py [-- Baobab_A DJStage ...]"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bmesh
import gs_common as gs
import env_kit as k
from gs_common import Vector, Matrix
from env_kit import M, E

W = "W3"


def pal():
    return dict(green=M("#2D6A4F"), green2=M("#40916C"), leaf=M("#52B788"), leaf2=M("#74C69D"), orange=M("#F77F00"),
                yellow=M("#FCBF49"), magenta=M("#D6336C"), purple=M("#7B2CBF"), bark=M("#8B5E3C"), bark2=M("#5C3D2E"),
                sand=M("#F4D58D"), sand2=M("#E0B86A"), rock=M("#7D7A70"), white=M("#F4F1EA"), black=M("#22232B"),
                red=M("#E2394B"), bamboo=M("#C9D86B"), rope=M("#D9B77A"), gold=M("#F5C431", 0.35),
                metal=M("#DDE3EA", 0.15), fire=E("#FF9F1C", 4.0), neon_m=E("#FF4FA3", 3.0), neon_y=E("#FCBF49", 3.0),
                neon_c=E("#3AE1FF", 3.0), shell=M("#FBE3D2"), water=M("#7FDBFF", 0.1))


def leaf_clump(name, c, r, mats, seed, n=5):
    return k.bush(name, c, r, mats, seed=seed, n=n)


# ---------------------------------------------------------------------------
# Hitos: baobabs y escenario de DJ
# ---------------------------------------------------------------------------
def baobab(S, name, v):
    """Baobab enorme (≈ 20 m): tronco botella con estrías, copa de ramas cortas y matas de hojas.
    B lleva una casita de lémures con escalera de cuerda."""
    C = pal()
    rnd = random.Random(40 + v)
    H = 15.0 if v == 0 else 17.0
    prof = [(0.0, 0.0), (4.2, 0.0), (4.6, 2.5), (4.4, 6.0), (3.6, 10.0), (2.6, H - 1.0), (2.4, H), (0.0, H + 0.3)] \
        if v == 0 else [(0.0, 0.0), (3.6, 0.0), (3.4, 3.0), (3.0, 8.0), (2.4, 13.0), (2.0, H), (0.0, H + 0.3)]
    trunk = gs.lathe("Trunk", prof, [C["bark"], C["bark2"]], seg=18)
    for vv in trunk.data.vertices:  # tronco algo ovalado y torcido
        vv.co.x *= 1.08
        vv.co.x += 0.25 * math.sin(vv.co.z * 0.25)
    gs.bands_by_plane(trunk, (1, 0.3, 0), (0, 0, 0), 1.3, 0.25, 1, cut=True)
    P = [trunk]
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.4
        P.append(gs.ellipsoid(f"RootFlare{i}", (3.9 * math.cos(a), 3.9 * math.sin(a), 0.5), (1.6, 1.0, 1.0),
                              C["bark"], seg=8, rings=5, rot=("Z", math.degrees(a))))
    nb = 7 if v == 0 else 6
    for i in range(nb):
        a = i / nb * 2 * math.pi + rnd.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        base = Vector((0, 0, H - 0.5)) + d * 1.4
        mid = base + d * rnd.uniform(2.0, 3.0) + Vector((0, 0, rnd.uniform(1.5, 2.5)))
        tip = mid + d * rnd.uniform(1.0, 2.0) + Vector((0, 0, rnd.uniform(0.8, 1.8)))
        P.append(gs.tube(f"Branch{i}", [base, mid, tip], [0.8, 0.5, 0.3], C["bark"], seg=6, sub=1, cap=2))
        cl = tip - Vector((0, 0, 0.4))
        P.append(gs.ellipsoid(f"Clump{i}", cl, (2.3, 2.1, 1.3), C["leaf"] if i % 2 else C["green2"], seg=9, rings=5))
        P.append(gs.ellipsoid(f"ClumpB{i}", cl + d * 1.2 + Vector((0, 0, 0.7)), 1.3, C["leaf2"], seg=7, rings=4))
    P.append(gs.ellipsoid("Hollow", (0, -3.9 if v == 0 else -3.2, 4.0), (0.9, 0.5, 1.3), C["black"], seg=10, rings=6))
    if v == 1:
        hut = Vector((2.6, -1.5, 9.0))
        P.append(gs.rounded_box("Deck", hut, (4.2, 3.6, 0.3), 0.1, C["bamboo"]))
        P.append(gs.rounded_box("Hut", hut + Vector((0.3, 0, 1.2)), (2.6, 2.2, 2.0), 0.3, C["sand2"], bulge=0.08,
                                segments=1))
        P.append(gs.cone("Roof", hut + Vector((0.3, 0, 2.1)), hut + Vector((0.3, 0, 4.2)), 2.3, 0.05, C["yellow"],
                         seg=8))
        P.append(gs.rounded_box("Door", hut + Vector((0.3, -1.12, 1.0)), (0.8, 0.1, 1.3), 0.2, C["black"]))
        for s in (1, -1):
            P.append(gs.tube(f"Ladder{s}", [hut + Vector((-1.2 + 0.4 * s, -1.6, 0)), hut + Vector((-1.3 + 0.4 * s, -1.8, -4.5)),
                                            hut + Vector((-1.2 + 0.4 * s, -1.7, -9.0))], 0.05, C["rope"], seg=4, sub=2,
                             cap=1))
        for j in range(5):
            z = -1.2 - 1.5 * j
            P.append(gs.capsule(f"Rung{j}", hut + Vector((-1.75, -1.7, z)), hut + Vector((-0.85, -1.7, z)), 0.05,
                                C["bark2"], seg=4, cap=1, sub=1))
        moving = [("Flag", hut + Vector((0.3, 0, 4.1)), [k.flag("Flag", hut + Vector((0.3, 0, 4.1)), 1.2, 0.7,
                                                                [C["magenta"], C["yellow"]], stripes=2)])]
    else:
        moving = None
    k.finish(S, name, P, moving=moving, view=(0.75, -0.8, 0.25), margin=1.05)


def dj_stage(S, name):
    """Hito W3-5: escenario de DJ de los lémures sobre pilotes. Partes móviles: platos, conos de los altavoces
    (para latir con la música) y bola de espejos."""
    C = pal()
    P = [gs.rounded_box("Deck", (0, 0, 1.5), (11.0, 6.5, 0.6), 0.2, C["bamboo"], bulge=0.03, segments=1)]
    for x in (-5.0, 0.0, 5.0):
        for y in (-2.8, 2.8):
            P.append(gs.capsule(f"Stilt{x}{y}", (x, y, -1.5), (x, y, 1.3), 0.25, C["bark"], seg=5, cap=1, sub=1))
    P.append(gs.rounded_box("Steps", (0, -4.0, 0.6), (3.0, 1.6, 0.6), 0.1, C["bark2"], rot=("X", -20), segments=1))
    # arco de bambú con corona y luces
    arch = [Vector((x, 2.4, 1.8 + 6.0 * math.cos(x / 5.6 * math.pi / 2) ** 0.6)) for x in [-5.4 + 10.8 * i / 12
                                                                                          for i in range(13)]]
    P.append(gs.tube("Arch", arch, 0.28, C["bamboo"], seg=6, sub=1, cap=2))
    crown_c = Vector((0, 2.4, 8.4))
    P.append(gs.rounded_box("CrownBand", crown_c, (2.4, 0.4, 0.6), 0.15, C["gold"]))
    for i in range(5):
        x = -1.0 + 0.5 * i
        P.append(gs.cone(f"CrownSpike{i}", crown_c + Vector((x, 0, 0.3)), crown_c + Vector((x, 0, 1.2 - 0.3 * (i % 2))),
                         0.22, 0.03, C["gold"], seg=4, round_tip=False))
        if i % 2 == 0:
            P.append(gs.ellipsoid(f"Gem{i}", crown_c + Vector((x, -0.22, 0)), 0.12, [C["red"], C["leaf"], C["purple"]][i % 3],
                              seg=5, rings=3))
    for i, p in enumerate(arch[1:-1:2]):
        P.append(gs.ellipsoid(f"Bulb{i}", p + Vector((0, -0.32, -0.15)), 0.14,
                              [C["neon_m"], C["neon_y"], C["neon_c"]][i % 3], seg=5, rings=3))
    for i in range(3):
        a = (i + 0.5) / 3 * math.pi
        P.append(k.leaf(f"Thatch{i}", [crown_c + Vector((0, 0.3, 1.0)), crown_c + Vector((2.0 * math.cos(a), 0.8,
                        0.6 + 0.8 * math.sin(a))), crown_c + Vector((3.6 * math.cos(a), 1.0, -0.4 + 0.6 * math.sin(a)))],
                        0.45, C["leaf"] if i % 2 else C["green2"]))
    # cabina del DJ con platos
    P.append(gs.rounded_box("Booth", (0, 0.4, 2.6), (4.0, 1.6, 1.6), 0.25, C["purple"], bulge=0.05, segments=1))
    P.append(gs.rounded_box("BoothTop", (0, 0.4, 3.45), (4.2, 1.8, 0.15), 0.06, C["black"]))
    P.append(gs.rounded_box("BoothStripe", (0, -0.42, 2.6), (3.6, 0.08, 0.3), 0.05, C["neon_m"]))
    P.append(gs.rounded_box("Mixer", (0, 0.4, 3.62), (0.8, 0.9, 0.2), 0.05, C["metal"]))
    moving = []
    for s, nm in ((1, "Turntable_L"), (-1, "Turntable_R")):
        c = Vector((1.25 * s, 0.4, 3.6))
        moving.append((nm, c, [gs.rounded_cylinder(f"{nm}Disc", c, 0.6, 0.08, 0.02, C["black"], seg=12, steps=1),
                               gs.ellipsoid(f"{nm}Label", c + Vector((0, 0, 0.05)), (0.2, 0.2, 0.03),
                                            C["orange"] if s == 1 else C["yellow"], seg=6, rings=3),
                               gs.ellipsoid(f"{nm}Mark", c + Vector((0.4, 0, 0.05)), (0.1, 0.04, 0.03), C["white"],
                                            seg=4, rings=3)]))
    # pilas de altavoces con conos móviles
    idx = 0
    for s in (1, -1):
        for lvl, (sz, z) in enumerate(((1.9, 2.75), (1.4, 4.4))):
            c = Vector((4.4 * s, 0.6, z))
            P.append(gs.rounded_box(f"Cab{s}{lvl}", c, (sz, sz * 0.9, sz), 0.2, C["black"], bulge=0.04, segments=1))
            cc = c + Vector((0, -sz * 0.45 - 0.02, 0))
            cone = [gs.lathe(f"Cone{idx}", [(0.0, 0.0), (sz * 0.38, 0.0), (sz * 0.34, 0.06), (sz * 0.12, 0.2),
                                            (0.0, 0.18)], C["metal"], loc=cc, axis=(0, -1, 0), seg=8),
                    gs.ellipsoid(f"Dust{idx}", cc + Vector((0, -0.2, 0)), sz * 0.11, C["orange"], seg=6, rings=4),
                    gs.torus(f"Surround{idx}", cc, sz * 0.38, 0.05, C["gold"], axis=(0, 1, 0), seg=8, mseg=3)]
            moving.append((f"Speaker_{idx}", cc, cone))
            idx += 1
    ball_c = Vector((0, 1.2, 6.6))
    P.append(gs.capsule("BallWire", ball_c, ball_c + Vector((0, 1.1, 1.6)), 0.03, C["black"], seg=4, cap=1, sub=1))
    ball = gs.ellipsoid("DiscoBall", ball_c, 0.7, [C["metal"], C["neon_c"]], seg=8, rings=6)
    gs.bands_by_plane(ball, (1, 0, 0.2), ball_c, 0.35, 0.35, 1, cut=True)
    moving.append(("DiscoBall", ball_c, [ball]))
    for s in (1, -1):
        P.append(gs.rounded_cylinder(f"Spot{s}", (2.6 * s, -2.8, 2.1), 0.25, 0.4, 0.06, C["neon_y"],
                                     axis=(-0.4 * s, -0.3, 1), seg=8, steps=1))
    k.finish(S, name, P, moving=moving, view=(0.6, -0.9, 0.3), margin=1.05)


# ---------------------------------------------------------------------------
# Selva y pantano
# ---------------------------------------------------------------------------
def palm_p(S, name, v):
    C = pal()
    P = k.palm(v, C["bark"], C["bark2"], [C["leaf"], C["green2"], C["leaf2"]], C["bark2"], seed=v + 3)
    P += leaf_clump("Base", (0.3, 0.2, 0), 0.5, [C["green2"]], seed=v, n=2) if v == 2 else []
    k.finish(S, name, P)


def liana(S, name):
    """Rama horizontal con lianas colgando (el cauce pasa por debajo; 10 m de luz)."""
    C = pal()
    rnd = random.Random(8)
    P = [gs.tube("Branch", [(-6.5, 0.2, 6.5), (-3, 0, 7.4), (2, 0.1, 7.2), (6.5, -0.1, 6.2)], [0.6, 0.5, 0.45, 0.4],
                 C["bark"], seg=10, sub=3, cap=2)]
    for s in (1, -1):
        P.append(gs.capsule(f"Post{s}", (6.3 * s, 0, -1.0), (6.5 * s, 0, 6.8), 0.55, C["bark2"], seg=9, cap=2, sub=1))
    for i in range(7):
        x = -4.5 + 1.5 * i
        top = Vector((x, 0, 7.0 + 0.3 * math.sin(i)))
        L = rnd.uniform(3.0, 4.6)
        pts = [top, top + Vector((0.3, 0.2, -L * 0.4)), top + Vector((-0.2, 0.1, -L * 0.75)),
               top + Vector((0.2, 0, -L))]
        P.append(gs.tube(f"Vine{i}", pts, [0.14, 0.11, 0.09, 0.07], C["green2"], seg=5, sub=3, cap=1))
        for j in range(2):
            q = pts[j + 1]
            P.append(k.leaf(f"VL{i}{j}", [q, q + Vector((0.5, -0.3, 0.15)), q + Vector((0.9, -0.45, -0.15))], 0.22,
                            C["leaf"] if (i + j) % 2 else C["leaf2"], droop_up=(0, 0, 1)))
        if i % 3 == 1:
            P += k.flower(f"VF{i}", pts[-1], C["magenta"], C["yellow"], size=0.2, h=0.0, tilt=60)
    k.finish(S, name, P, view=(0.4, -1, 0.25))


def log(S, name, v):
    """A: tronco flotante con hongos. B: tronco hueco con musgo."""
    C = pal()
    if v == 0:
        P = [gs.capsule("Log", (-2.4, 0, 0.1), (2.4, 0.2, 0.15), 0.5, [C["bark"]], seg=10, cap=3, sub=2),
             gs.rounded_cylinder("End", (-2.75, 0, 0.1), 0.42, 0.06, 0.02, C["sand2"], axis=(1, 0, 0), seg=10, steps=1),
             gs.tube("Stub", [(0.8, 0.0, 0.5), (1.1, -0.2, 1.2), (1.4, -0.3, 1.5)], [0.2, 0.13, 0.08], C["bark"], seg=6,
                     sub=2, cap=2)]
        for i, (x, s) in enumerate(((-1.2, 0.25), (-0.8, 0.17), (1.8, 0.2))):
            P.append(gs.capsule(f"MStem{i}", (x, -0.35, 0.4), (x, -0.45, 0.4 + s * 1.5), s * 0.3, C["white"], seg=6,
                                cap=1, sub=1))
            cap = gs.ellipsoid(f"MCap{i}", (x, -0.45, 0.42 + s * 1.5), (s, s, s * 0.55), C["red"], seg=10, rings=5)
            P.append(cap)
            for j in range(3):
                P.append(gs.ellipsoid(f"MDot{i}{j}", (x + s * 0.5 * math.cos(j * 2), -0.45 + s * 0.5 * math.sin(j * 2),
                                                      0.42 + s * 1.5 + s * 0.42), s * 0.15, C["white"], seg=4, rings=3))
    else:
        hollow = gs.lathe("Hollow", [(1.0, -2.2), (1.05, 2.2), (0.78, 2.2), (0.75, -2.2), (1.0, -2.2)], [C["bark"]],
                          loc=(0, 0, 0.8), axis=(1, 0, 0), seg=14)
        bm = bmesh.new()
        bm.from_mesh(hollow.data)
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if len(f.verts) >= 14], context="FACES_ONLY")
        bm.to_mesh(hollow.data)
        bm.free()
        P = [hollow] + [gs.torus(f"Rim{x}", (x, 0, 0.8), 0.9, 0.14, C["sand2"], axis=(1, 0, 0), seg=14, mseg=5)
                        for x in (-2.2, 2.2)]
        P += leaf_clump("Moss", (0.5, 0.1, 1.5), 0.55, [C["green2"], C["leaf"]], seed=4, n=3)
        P += k.flower("F", (-1.2, -0.6, 1.4), C["purple"], C["yellow"], size=0.2, h=0.2)
    k.finish(S, name, P)


def lilypad(S, name, v):
    C = pal()

    def pad(nm, c, r, rot):
        n = 16
        poly = [(0.0, 0.0)] + [(r * math.cos(math.radians(20 + 320 * i / n)), r * math.sin(math.radians(20 + 320 * i / n)))
                               for i in range(n + 1)]
        o = k.extrude(nm, poly, -0.04, 0.04, [C["leaf"]])
        o.data.transform(Matrix.Translation(Vector(c)) @ Matrix.Rotation(rot, 4, "Z")
                         @ Matrix.Rotation(math.radians(90), 4, "X"))
        return o
    P = []
    pads = [((0, 0, 0.04), 1.4, 0.3)] if v == 0 else [((0, 0, 0.04), 1.0, 0.0), ((1.7, 0.6, 0.04), 0.8, 2.0),
                                                       ((-1.3, 1.1, 0.04), 0.7, 4.0), ((0.4, -1.6, 0.04), 0.6, 1.0)]
    for i, (c, r, rot) in enumerate(pads):
        P.append(pad(f"Pad{i}", c, r, rot))
    fc = (0.3, 0.2, 0.08)
    for ring, (n, rr, tilt, col) in enumerate(((8, 0.32, 35, C["magenta"]), (6, 0.2, 60, M("#FF8FB3")))):
        for i in range(n):
            a = i / n * 2 * math.pi + ring * 0.3
            P.append(gs.ellipsoid(f"Petal{ring}{i}", (fc[0] + rr * math.cos(a), fc[1] + rr * math.sin(a),
                                                      fc[2] + 0.12 + 0.08 * ring), (0.28 - 0.06 * ring, 0.11, 0.05),
                                  col, seg=6, rings=3, rot=[("Y", -tilt), ("Z", math.degrees(a))]))
    P.append(gs.ellipsoid("Center", (fc[0], fc[1], fc[2] + 0.22), 0.1, C["yellow"], seg=6, rings=4))
    if v == 1:
        P.append(gs.ellipsoid("FrogBody", (1.7, 0.6, 0.25), (0.25, 0.3, 0.2), C["green2"], seg=8, rings=5))
        for s in (1, -1):
            P.append(gs.ellipsoid(f"FrogEye{s}", (1.7 + 0.12 * s, 0.42, 0.43), 0.08, C["white"], seg=6, rings=4))
            P.append(gs.ellipsoid(f"FrogPupil{s}", (1.7 + 0.12 * s, 0.36, 0.44), 0.035, C["black"], seg=4, rings=3))
    k.finish(S, name, P)


def roots(S, name):
    """Raíces arqueadas que salen de la orilla hacia el agua (manglar)."""
    C = pal()
    rnd = random.Random(12)
    P = [k.rock("Bank", (0, 2.5, -0.4), (4.0, 2.0, 1.2), [C["bark2"], C["green2"]], seed=5, top_index=1)]
    for i in range(7):
        x = -3.0 + i
        top = Vector((x * 0.6, 2.0, 1.4))
        foot = Vector((x + rnd.uniform(-0.3, 0.3), -2.0 - rnd.uniform(0, 1.0), -0.6))
        mid = (top + foot) / 2 + Vector((0, 0, 1.6 + rnd.uniform(0, 0.8)))
        P.append(gs.tube(f"Root{i}", [top, mid, foot], [0.32, 0.22, 0.14], C["bark"], seg=7, sub=3, cap=2))
    P += leaf_clump("Moss", (0, 1.8, 1.5), 0.9, [C["leaf"], C["green2"]], seed=9, n=3)
    k.finish(S, name, P)


def sandcastle(S, name, v):
    """A: castillo con torres y banderín móvil. B: castillo de goteo con conchas."""
    C = pal()
    P = [gs.rounded_cylinder("Mound", (0, 0, 0.15), 1.8, 0.3, 0.12, C["sand2"], seg=18, steps=1)]
    moving = None
    if v == 0:
        P.append(gs.rounded_box("Keep", (0, 0.2, 0.75), (1.6, 1.4, 1.0), 0.15, C["sand"], taper=(0.85, 0.85)))
        for i, (x, y) in enumerate(((-1.0, -0.6), (1.0, -0.6), (-0.9, 0.9), (0.9, 0.9))):
            P.append(gs.lathe(f"Tower{i}", [(0.0, 0.3), (0.42, 0.3), (0.36, 1.5), (0.45, 1.6), (0.45, 1.8), (0.0, 1.8)],
                              C["sand"], loc=(x, y, 0), seg=10))
            for j in range(4):
                a = j / 4 * 2 * math.pi
                P.append(gs.rounded_box(f"Merlon{i}{j}", (x + 0.35 * math.cos(a), y + 0.35 * math.sin(a), 1.92),
                                        (0.16, 0.16, 0.2), 0.04, C["sand"], segments=1))
        P.append(gs.rounded_box("Gate", (0, -0.52, 0.55), (0.45, 0.1, 0.55), 0.18, C["sand2"]))
        P.append(gs.capsule("Pole", (0, 0.2, 1.2), (0, 0.2, 2.6), 0.03, C["bark"], seg=4, cap=1, sub=1))
        moving = [("Flag", (0, 0.2, 2.45), [k.flag("Flag", (0, 0.2, 2.45), 0.6, 0.35, [C["magenta"]], n=6)])]
    else:
        for i, (x, y, h) in enumerate(((0, 0, 2.0), (0.7, 0.4, 1.3), (-0.7, 0.3, 1.5), (0.3, -0.8, 1.0))):
            pts = [(0.0, 0.3)]
            for j in range(6):
                z = 0.3 + h * j / 5
                pts.append(((0.5 - 0.07 * j) * (1.1 if j % 2 else 0.9), z))
            pts.append((0.0, 0.3 + h + 0.15))
            P.append(gs.lathe(f"Drip{i}", pts, C["sand"], loc=(x, y, 0), seg=10))
        for i in range(5):
            a = i * 1.3
            P.append(gs.ellipsoid(f"Shell{i}", (1.3 * math.cos(a), 1.3 * math.sin(a), 0.32), (0.13, 0.1, 0.06),
                                  [C["shell"], M("#FF8FB3")][i % 2], seg=6, rings=3))
        P.append(gs.lathe("Bucket", [(0.0, 0.0), (0.3, 0.0), (0.38, 0.5), (0.0, 0.5)], C["orange"], loc=(1.6, -0.9, 0.0),
                          seg=10))
    k.finish(S, name, P, moving=moving)


def jungle_rock(S, name, v):
    C = pal()
    P = k.rock_variant(v, [C["rock"], C["green2"]], seed=30 + v, scale=1.4)
    P += k.flower("F", (0.9, -0.7, 0.0), C["orange"], C["yellow"], stem=C["green2"], size=0.2, h=0.35)
    k.finish(S, name, P)


# ---------------------------------------------------------------------------
# Relleno
# ---------------------------------------------------------------------------
def bird_flower(S, name):
    """Matas de flores tropicales (estilo ave del paraíso) en naranja y violeta."""
    C = pal()
    P = []
    for i in range(5):
        a = i / 5 * 2 * math.pi
        b = Vector((0.4 * math.cos(a), 0.4 * math.sin(a), 0))
        h = 1.0 + 0.25 * i
        P.append(gs.capsule(f"Stem{i}", b, b + Vector((0, 0, h)), 0.03, C["green2"], seg=4, cap=1, sub=1))
        tip = b + Vector((0, 0, h))
        d = Vector((math.cos(a + 1.2), math.sin(a + 1.2), 0))
        P.append(gs.ellipsoid(f"Beak{i}", tip + d * 0.2, (0.25, 0.06, 0.06), C["green2"], seg=6, rings=3,
                              rot=("Z", math.degrees(a + 1.2))))
        for j in range(3):
            P.append(gs.cone(f"Crest{i}{j}", tip + d * 0.1, tip + d * (0.05 + 0.1 * j) + Vector((0, 0, 0.45 - 0.08 * j)),
                             0.07, 0.01, C["orange"], seg=4))
        P.append(gs.cone(f"Tongue{i}", tip + d * 0.15, tip + d * 0.4 + Vector((0, 0, 0.2)), 0.05, 0.01, C["purple"],
                         seg=4))
        P.append(k.leaf(f"Leaf{i}", [b, b + d * 0.3 + Vector((0, 0, 0.6)), b + d * 0.7 + Vector((0, 0, 0.9))], 0.18,
                        C["leaf"]))
    k.finish(S, name, P)


def fern(S, name):
    C = pal()
    rnd = random.Random(3)
    P = []
    for i in range(9):
        a = i / 9 * 2 * math.pi + rnd.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        L = rnd.uniform(1.2, 1.7)
        P.append(k.leaf(f"Frond{i}", [Vector((0, 0, 0.1)), d * L * 0.4 + Vector((0, 0, 0.9)), d * L + Vector((0, 0, 0.5))],
                        0.22, C["green2"] if i % 2 else C["leaf"]))
    k.finish(S, name, P)


def bamboo(S, name):
    C = pal()
    rnd = random.Random(5)
    P = []
    for i in range(6):
        b = Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.6, 0.6), 0))
        h = rnd.uniform(3.0, 4.6)
        top = b + Vector((rnd.uniform(-0.4, 0.4), rnd.uniform(-0.4, 0.4), h))
        P.append(gs.tube(f"Cane{i}", [b, (b + top) / 2, top], 0.08, [C["bamboo"], C["green2"]], seg=6, cap=1,
                         mat_fn=lambda u: 1 if (u * 6) % 1 < 0.08 else 0, sub=2))
        for j in range(2):
            q = b.lerp(top, 0.6 + 0.2 * j)
            a = rnd.uniform(0, 6.28)
            P.append(k.leaf(f"L{i}{j}", [q, q + Vector((0.4 * math.cos(a), 0.4 * math.sin(a), 0.1)),
                                         q + Vector((0.8 * math.cos(a), 0.8 * math.sin(a), -0.15))], 0.08, C["leaf"]))
    k.finish(S, name, P)


def tiki_torch(S, name):
    C = pal()
    P = [gs.capsule("Pole", (0, 0, 0), (0, 0, 2.2), 0.07, C["bamboo"], seg=6, cap=1, sub=1),
         gs.lathe("Cup", [(0.0, 2.1), (0.12, 2.1), (0.22, 2.45), (0.0, 2.45)], C["bark2"], seg=10),
         gs.torus("Wrap", (0, 0, 2.15), 0.13, 0.03, C["rope"], seg=10, mseg=3),
         gs.lathe("Flame", [(0.0, 2.4), (0.2, 2.5), (0.16, 2.75), (0.06, 3.0), (0.0, 3.15)], C["fire"], seg=8)]
    flame_tip = gs.lathe("FlameIn", [(0.0, 2.45), (0.1, 2.52), (0.06, 2.75), (0.0, 2.85)], E("#FFE27A", 5.0), seg=6,
                         loc=(0.02, -0.06, 0))
    P.append(flame_tip)
    k.finish(S, name, P)


def croc_sign(S, name):
    C = pal()

    def icon(c):
        return [gs.ellipsoid("CrocBody", c + Vector((0, -0.02, -0.05)), (0.35, 0.03, 0.08), C["green2"], seg=8,
                             rings=4),
                gs.cone("CrocJaw", c + Vector((-0.3, -0.03, 0.0)), c + Vector((-0.5, -0.03, 0.08)), 0.07, 0.01,
                        C["green2"], seg=4),
                gs.ellipsoid("CrocEye", c + Vector((-0.2, -0.04, 0.06)), 0.04, C["white"], seg=4, rings=3),
                gs.tube("Bubble", gs.arc_points(c + Vector((0.2, -0.03, 0.2)), 0.1, 0, 360, 9), 0.015, C["white"],
                        seg=4, cap=1)]
    P = k.sign("Sign", C["bark2"], C["yellow"], icon, w=1.2, bh=0.8, tilt=-8)
    P.append(gs.rounded_box("Warn", (0, -0.05, 2.25), (0.3, 0.05, 0.12), 0.03, C["red"]))
    k.finish(S, name, P)


def fruit_crate(S, name, v):
    C = pal()
    if v == 0:
        extra = [gs.ellipsoid(f"Mango{i}", (-0.25 + 0.25 * i, 0.15 * (i % 2), 1.3), (0.14, 0.12, 0.17), C["orange"],
                              seg=8, rings=5, rot=("Y", 30)) for i in range(3)]
    elif v == 1:
        extra = [gs.tube(f"Banana{i}", gs.arc_points((0.1 * i - 0.1, 0.1 * i, 1.3), 0.3, 200, 340, 5,
                                                     plane=("X", "Z")), 0.07, C["yellow"], seg=6, cap=1)
                 for i in range(4)]
    else:
        extra = [gs.ellipsoid(f"Coco{i}", (-0.2 + 0.2 * i, 0.1 * (i % 2), 1.33), 0.17, C["bark2"], seg=8, rings=5)
                 for i in range(3)]
    k.finish(S, name, k.crate(2, C["bamboo"] if v != 2 else C["bark"], C["bark2"], C["magenta"], extra))


PROPS = [("Baobab_A", lambda S, n: baobab(S, n, 0)), ("Baobab_B", lambda S, n: baobab(S, n, 1)),
         ("DJStage", dj_stage), ("Liana", liana), ("Log_A", lambda S, n: log(S, n, 0)),
         ("Log_B", lambda S, n: log(S, n, 1)), ("Lilypad_A", lambda S, n: lilypad(S, n, 0)),
         ("Lilypad_B", lambda S, n: lilypad(S, n, 1)), ("Roots", roots),
         ("Sandcastle_A", lambda S, n: sandcastle(S, n, 0)), ("Sandcastle_B", lambda S, n: sandcastle(S, n, 1)),
         ("TropicalFlower", bird_flower), ("Fern", fern), ("Bamboo", bamboo), ("TikiTorch", tiki_torch),
         ("Sign_Croc", croc_sign)] \
    + [(f"Palm_{c}", (lambda v: lambda S, n: palm_p(S, n, v))(i)) for i, c in enumerate("ABC")] \
    + [(f"Rock_{c}", (lambda v: lambda S, n: jungle_rock(S, n, v))(i)) for i, c in enumerate("ABC")] \
    + [(f"FruitCrate_{c}", (lambda v: lambda S, n: fruit_crate(S, n, v))(i)) for i, c in enumerate("ABC")]

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    k.run_props(W, PROPS, only=set(argv) or None, cols=7)
