"""Escenario W2 · Mar Abierto (mar y tormenta). docs/09 §3 fila "Escenario W2", paleta de docs/04:
turquesa #2EC4B6, gris #5C6B73, espuma; acento complementario coral y rojo.
blender --background --python env_w2.py [-- Lighthouse ...]"""
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

W = "W2"


def pal():
    return dict(teal=M("#2EC4B6"), teal2=M("#1E8C84"), grey=M("#5C6B73"), grey2=M("#3E4A50"), foam=M("#F2FBFA"),
                coral=M("#FF7F50"), red=M("#D7263D"), white=M("#F4F1EA"), wood=M("#A0703F"), wood2=M("#6B4A2A"),
                rope=M("#D9B77A"), iron=M("#2B3238", 0.5), rust=M("#A4552C"), sand=M("#E9D29A"), yellow=M("#FFC93C"),
                kelp=M("#4C8A3A"), kelp2=M("#2E6B3A"), pink=M("#FF6FA5"), purple=M("#8E5BD6"), black=M("#1E2226"),
                lamp=E("#FFE27A", 4.0), beam=E("#FFF3B0", 1.5), glass=M("#BFF3FF", 0.1), shell=M("#FBE3D2"),
                barnacle=M("#C9C3B6"), water=M("#7FDBFF", 0.1))


def foam_ring(name, r, C, z=0.02):
    return gs.torus(name, (0, 0, z), r, 0.12, C["foam"], seg=18, mseg=4, scale=(1.0, 1.0, 0.4))


# ---------------------------------------------------------------------------
# Hitos
# ---------------------------------------------------------------------------
def lighthouse(S, name):
    """Faro gigante (30 m) panzón a franjas, sobre rocas. La linterna con haces gira (Beacon)."""
    C = pal()
    P = []
    for i, (x, y, sx, sz) in enumerate(((0, 0, 7.0, 3.0), (5.0, 3.0, 3.5, 2.2), (-4.5, 3.5, 3.0, 1.8),
                                         (3.5, -4.5, 2.8, 1.5))):
        P.append(k.rock(f"Rock{i}", (x, y, -1.0), (sx, sx * 0.85, sz), [C["grey"], C["kelp2"]], seed=i + 2,
                        subdiv=2 if i == 0 else 1, top_index=1, top_dot=0.75))
    tower = gs.lathe("Tower", [(0.0, 1.5), (4.0, 1.5), (4.3, 5.0), (4.0, 10.0), (3.4, 16.0), (2.8, 21.0),
                               (3.0, 21.6), (0.0, 21.6)], [C["white"], C["red"]], seg=20)
    gs.bands_by_plane(tower, (0, 0, 1), (0, 0, 1.5), 5.0, 0.5, 1, cut=True)
    P.append(tower)
    P.append(gs.rounded_cylinder("Gallery", (0, 0, 21.8), 4.0, 0.5, 0.15, C["grey2"], seg=20, steps=1))
    P.append(gs.torus("Railing", (0, 0, 22.9), 3.85, 0.07, C["iron"], seg=24, mseg=4))
    for i in range(12):
        a = i / 12 * 2 * math.pi
        P.append(gs.capsule(f"Baluster{i}", (3.85 * math.cos(a), 3.85 * math.sin(a), 22.0),
                            (3.85 * math.cos(a), 3.85 * math.sin(a), 22.9), 0.05, C["iron"], seg=4, cap=1, sub=1))
    P.append(gs.rounded_cylinder("LanternBase", (0, 0, 22.5), 2.3, 0.8, 0.1, C["red"], seg=16, steps=1))
    P.append(gs.rounded_cylinder("Glass", (0, 0, 24.2), 2.0, 2.6, 0.1, C["glass"], seg=16, steps=1))
    for i in range(6):
        a = i / 6 * 2 * math.pi
        P.append(gs.capsule(f"Mullion{i}", (2.02 * math.cos(a), 2.02 * math.sin(a), 22.9),
                            (2.02 * math.cos(a), 2.02 * math.sin(a), 25.5), 0.07, C["iron"], seg=4, cap=1, sub=1))
    P.append(gs.lathe("Roof", [(0.0, 25.4), (2.6, 25.4), (2.5, 25.7), (1.2, 27.0), (0.3, 28.0), (0.0, 28.2)],
                      C["red"], seg=16))
    P.append(gs.ellipsoid("Finial", (0, 0, 28.6), 0.45, C["yellow"], seg=8, rings=5))
    P.append(gs.rounded_box("Door", (0, -4.15, 3.0), (1.6, 0.5, 2.6), 0.4, C["wood2"], rot=("X", 4)))
    for i, (a, z) in enumerate(((-90, 9.0), (-60, 14.0), (-110, 18.5))):
        r = 4.0 - (z - 5) * 0.075
        P.append(gs.rounded_box(f"Window{i}", (r * math.cos(math.radians(a)), r * math.sin(math.radians(a)), z),
                                (0.8, 0.3, 1.2), 0.25, C["lamp"], rot=("Z", a + 90)))
    P.append(foam_ring("Foam", 7.5, C))
    beacon = [gs.ellipsoid("Lamp", (0, 0, 24.2), (1.0, 1.0, 0.9), C["lamp"], seg=12, rings=7)]
    for s in (1, -1):
        beacon.append(gs.cone(f"BeamCone{s}", (0, 0.8 * s, 24.2), (0, 12.0 * s, 24.2), 0.35, 1.3, C["beam"],
                              seg=10, round_tip=False))
    k.finish(S, name, P, moving=[("Beacon", (0, 0, 24.2), beacon)], view=(0.75, -0.8, 0.3), margin=1.05)


def half_hull(name, L, Wd, H, u0, u1, mats, rise=0.6, jag_seed=0, jag_at=None):
    """Mitad de un casco panzón entre u0 y u1 (0 = popa, 1 = proa) con el borde de rotura dentado."""
    w = Wd / 2
    prof = [(-w * 0.96, H * 0.55), (-w, H * 0.2), (-w * 0.85, -H * 0.2), (-w * 0.45, -H * 0.42), (0, -H * 0.45),
            (w * 0.45, -H * 0.42), (w * 0.85, -H * 0.2), (w, H * 0.2), (w * 0.96, H * 0.55)]
    n = 10
    us = [u0 + (u1 - u0) * i / n for i in range(n + 1)]
    path = [(0, L / 2 - L * u, H * rise * max(0.0, (u - 0.6) / 0.4) ** 2) for u in us]

    def sc(t):
        u = u0 + (u1 - u0) * t
        kf = 1 + 0.12 * math.sin(math.pi * u)
        if u > 0.55:
            kf *= max(0.12, 1 - ((u - 0.55) / 0.45) ** 1.6)
        return kf
    o = k.sweep(name, prof, path, mats, scale_fn=sc, smooth=True)
    rnd = random.Random(jag_seed)
    yb = L / 2 - L * jag_at
    bm = bmesh.new()
    bm.from_mesh(o.data)
    for v in bm.verts:
        if abs(v.co.y - yb) < 1e-3:
            v.co.y += rnd.uniform(-1.2, 1.2)
            v.co.z += rnd.uniform(-0.3, 0.3)
    bm.normal_update()
    for f in bm.faces:
        if abs(f.calc_center_median().y - yb) < 1.3 and abs(f.normal.y) > 0.5:
            f.material_index = 2
    bm.to_mesh(o.data)
    bm.free()
    return o


def shipwreck(S, name):
    """Hito W2: barco hundido partido en dos. La proa asoma clavada hacia el cielo; la popa, escorada."""
    C = pal()
    rnd = random.Random(4)
    mats = [M("#8C5A35"), M("#C8553D"), C["black"], C["yellow"]]
    L, Wd, H = 26.0, 9.0, 7.0

    def place(parts, m):
        for o in parts:
            o.data.transform(m)
        return parts
    stern = half_hull("Stern", L, Wd, H, 0.0, 0.46, mats, jag_seed=1, jag_at=0.46)
    bow = half_hull("Bow", L, Wd, H, 0.52, 1.0, mats, jag_seed=2, jag_at=0.52)
    for o in (stern, bow):
        gs.paint_faces(o, lambda c: c.z < -1.2, 1)
        gs.paint_faces(o, lambda c: 1.6 < c.z < 2.3, 3)
    SP = [stern, gs.rounded_box("Cabin", (0, 9.0, 5.6), (6.0, 4.5, 3.4), 0.5, C["wood"], bulge=0.06),
          gs.rounded_box("CabinRoof", (0, 9.0, 7.5), (6.6, 5.0, 0.4), 0.15, C["teal2"]),
          gs.capsule("MastStub", (0, 4.0, 3.5), (0.3, 3.8, 12.5), 0.4, C["wood2"], r1=0.3, seg=8, cap=1, sub=1),
          gs.capsule("Yard", (-3.8, 3.8, 10.8), (4.2, 3.8, 11.2), 0.16, C["wood2"], seg=6, cap=1, sub=1)]
    for i in range(3):
        SP.append(gs.torus(f"Port{i}", (-4.45, 3.5 + 2.6 * i, 1.0), 0.35, 0.1, C["yellow"], axis=(1, 0, 0), seg=10,
                           mseg=4))
    BP = [bow, gs.capsule("BowSprit", (0, -12.5, 6.4), (0, -17.0, 8.5), 0.25, C["wood2"], r1=0.15, seg=6, cap=1,
                          sub=1),
          gs.ellipsoid("Figurehead", (0, -12.9, 5.4), (0.5, 0.7, 0.8), C["yellow"], seg=10, rings=6),
          gs.capsule("ForeMast", (0, -5.0, 3.5), (0, -5.0, 8.5), 0.3, C["wood2"], seg=8, cap=1, sub=1)]
    for i in range(3):
        BP.append(gs.torus(f"BPort{i}", (4.45, -3.0 - 2.4 * i, 1.0), 0.35, 0.1, C["yellow"], axis=(1, 0, 0), seg=10,
                           mseg=4))
    ms = Matrix.Translation(Vector((-2.5, 2.0, -2.0))) @ Matrix.Rotation(math.radians(18), 4, "Y")         @ Matrix.Rotation(math.radians(-6), 4, "X")
    brk = L / 2 - L * 0.52  # borde de rotura de la proa: pivote del giro
    mb = Matrix.Translation(Vector((2.0, -1.5 - brk, -1.8))) @ Matrix.Rotation(math.radians(-14), 4, "Z")         @ Matrix.Rotation(math.radians(-8), 4, "Y") @ Matrix.Translation(Vector((0, brk, 0)))         @ Matrix.Rotation(math.radians(-32), 4, "X") @ Matrix.Translation(Vector((0, -brk, 0)))
    P = place(SP, ms) + place(BP, mb)
    for i in range(14):
        o = stern if i % 2 else bow
        f = rnd.choice(o.data.polygons)
        if -1.5 < f.center.z < 1.5:
            P.append(gs.ellipsoid(f"Barnacle{i}", f.center, 0.4, C["barnacle"], seg=6, rings=4))
    sail_o = Vector((-3.6, 3.8, 10.6))
    sail = k.flag("Sail", sail_o, 4.5, 3.0, [C["white"], C["sand"]], waves=1.2, amp=0.5, stripes=3)
    sail.data.transform(Matrix.Translation(sail_o) @ Matrix.Rotation(math.radians(-90), 4, "Y")
                        @ Matrix.Translation(-sail_o))
    sail.data.transform(ms)
    P.append(gs.torus("Foam", (0, 0, 0.02), 11.0, 0.18, C["foam"], seg=24, mseg=4, scale=(0.7, 1.3, 0.4)))
    k.finish(S, name, P, moving=[("Sail", ms @ sail_o, [sail])], view=(0.9, -0.6, 0.35), margin=1.05)


# ---------------------------------------------------------------------------
# Restos, redes y mástiles
# ---------------------------------------------------------------------------
def rowboat(S, name):
    """Bote volcado medio hundido con remo."""
    C = pal()
    h = k.hull("Hull", 4.2, 1.8, 1.0, [C["coral"], C["white"]], stripe=(0.1, 0.3, 1), bow_rise=0.5)
    h.data.transform(Matrix.Translation(Vector((0, 0, 0.2))) @ Matrix.Rotation(math.radians(160), 4, "Y")
                     @ Matrix.Rotation(math.radians(8), 4, "X"))
    P = [h, gs.capsule("Oar", (-1.4, -1.2, 0.05), (1.2, 1.6, 0.25), 0.05, C["wood"], seg=5, cap=1, sub=1),
         gs.ellipsoid("Blade", (1.35, 1.75, 0.27), (0.18, 0.4, 0.04), C["wood"], seg=8, rings=4, rot=("Z", -40)),
         foam_ring("Foam", 2.0, C)]
    k.finish(S, name, P)


def mast(S, name):
    """Mástil roto que sale del agua con cofa, cuerdas y vela rota (móvil)."""
    C = pal()
    P = [gs.tube("Pole", [(0, 0, -1.0), (0.2, 0.1, 4.0), (0.6, 0.2, 8.0)], [0.35, 0.3, 0.25], C["wood2"], seg=8,
                 sub=3, cap=2),
         gs.lathe("Nest", [(0.0, 6.2), (0.9, 6.2), (1.0, 6.9), (0.85, 7.0), (0.0, 6.6)], C["wood"], loc=(0.48, 0.17, 0),
                  seg=12),
         gs.capsule("Yard", (-1.6, 0.1, 5.4), (2.6, 0.2, 5.6), 0.12, C["wood2"], seg=6, cap=1, sub=1)]
    for s in (1, -1):
        P.append(gs.tube(f"Rope{s}", [(2.5 * s + 0.4, 0.15, 5.5), (2.4 * s, 0.6, 2.0), (2.2 * s, 1.0, 0.0)], 0.04,
                         C["rope"], seg=4, sub=3, cap=1))
    P.append(foam_ring("Foam", 0.9, C))
    flag = k.flag("Flag", (0.6, 0.2, 7.9), 1.3, 0.8, [C["black"], C["white"]], stripes=2)
    sail = k.flag("Sail", (-1.5, 0.1, 5.3), 3.0, 1.8, [C["sand"]], waves=1, amp=0.3)
    sail.data.transform(Matrix.Translation(Vector((-1.5, 0.1, 5.3))) @ Matrix.Rotation(math.radians(-80), 4, "Y")
                        @ Matrix.Translation(Vector((1.5, -0.1, -5.3))))
    k.finish(S, name, P, moving=[("Flag", (0.6, 0.2, 7.9), [flag]), ("Sail", (-1.5, 0.1, 5.3), [sail])])


def net(S, name):
    """Red de pesca colgada entre dos postes, con flotadores."""
    C = pal()
    P = []
    for s in (1, -1):
        P.append(gs.capsule(f"Post{s}", (2.2 * s, 0, -1.0), (2.2 * s, 0.1 * s, 3.2), 0.16, C["wood2"], seg=7, cap=2,
                            sub=1))
    nx, nz = 8, 5
    grid = [[Vector((-2.0 + 4.0 * i / nx, 0.25 * math.sin(i / nx * math.pi), 3.0 - 2.8 * j / nz
                     - 0.6 * math.sin(i / nx * math.pi))) for j in range(nz + 1)] for i in range(nx + 1)]
    for i in range(nx + 1):
        P.append(gs.tube(f"V{i}", grid[i], 0.025, C["rope"], seg=4, cap=1, sub=1))
    for j in range(nz + 1):
        P.append(gs.tube(f"H{j}", [grid[i][j] for i in range(nx + 1)], 0.025, C["rope"], seg=4, cap=1, sub=2))
    for i in range(0, nx + 1, 2):
        P.append(gs.ellipsoid(f"Float{i}", grid[i][0] + Vector((0, -0.05, 0.12)), (0.16, 0.16, 0.12),
                              C["coral"] if i % 4 else C["yellow"], seg=8, rings=5))
    P.append(gs.ellipsoid("Fish", grid[5][3] + Vector((0, -0.1, 0)), (0.35, 0.08, 0.18), C["teal"], seg=8, rings=4))
    P.append(gs.ellipsoid("Starfish", grid[2][4] + Vector((0, -0.08, 0)), (0.2, 0.04, 0.2), C["coral"], seg=6,
                          rings=3))
    k.finish(S, name, P, view=(0.4, -1, 0.25))


def anchor_post(S, name, v):
    """A: poste de amarre con aro (Anchor del Cuello Gancho). B: ancla gigante clavada en una roca."""
    C = pal()
    if v == 0:
        P = [gs.capsule("Pile", (0, 0, -1.5), (0, 0, 2.6), 0.35, C["wood2"], r1=0.3, seg=10, cap=2, sub=1),
             gs.rounded_cylinder("Cap", (0, 0, 2.75), 0.38, 0.2, 0.06, C["red"], seg=10, steps=1),
             gs.torus("Ring", (0, -0.42, 2.0), 0.35, 0.07, C["yellow"], axis=(0, 1, 0), seg=14, mseg=5),
             gs.torus("Coil", (0, 0, 1.2), 0.38, 0.08, C["rope"], seg=12, mseg=4),
             gs.torus("Coil2", (0, 0, 1.38), 0.37, 0.08, C["rope"], seg=12, mseg=4),
             foam_ring("Foam", 0.6, C)]
        for i in range(4):
            a = i * 1.7
            P.append(gs.ellipsoid(f"Barnacle{i}", (0.33 * math.cos(a), 0.33 * math.sin(a), 0.2 + 0.15 * i), 0.1,
                                  C["barnacle"], seg=6, rings=3))
    else:
        P = [k.rock("Rock", (0, 0, -0.6), (2.0, 1.8, 1.4), [C["grey"], C["kelp2"]], seed=7, top_index=1),
             gs.capsule("Shank", (0.2, 0, 0.6), (0.6, 0.2, 4.2), 0.2, C["iron"], seg=8, cap=1, sub=1),
             gs.torus("Eye", (0.65, 0.22, 4.6), 0.35, 0.09, C["iron"], axis=(1, 0, 0), seg=12, mseg=4),
             gs.capsule("Stock", (-0.8, 0.15, 3.8), (2.0, 0.25, 4.0), 0.12, C["wood2"], seg=6, cap=1, sub=1),
             gs.tube("Arms", gs.arc_points((0.25, 0, 1.6), 1.3, 200, 340, 8), 0.18, C["iron"], seg=8, cap=1),
             gs.torus("Chain", (0.65, -0.1, 4.0), 0.25, 0.06, C["rust"], axis=(0, 1, 0), seg=10, mseg=4)]
        for s in (1, -1):
            P.append(gs.cone(f"Fluke{s}", (0.25 + 1.22 * s, 0, 1.15), (0.25 + 1.45 * s, 0, 1.8), 0.28, 0.02,
                             C["iron"], seg=4))
        P.append(foam_ring("Foam", 2.0, C))
    k.finish(S, name, P)


# ---------------------------------------------------------------------------
# Rocas, corales y relleno de playa
# ---------------------------------------------------------------------------
def sea_rock(S, name, v):
    C = pal()
    P = k.rock_variant(v, [C["grey"], C["foam"] if v == 1 else C["kelp2"]], seed=10 + v, scale=1.6)
    for o in P:
        o.data.transform(Matrix.Translation(Vector((0, 0, -0.6))))
    P.append(foam_ring("Foam", 2.6 if v != 1 else 1.6, C))
    if v == 0:
        P += [gs.ellipsoid(f"Shell{i}", (0.8 * math.cos(i), 0.8 * math.sin(i), 1.25 - 0.2 * i), (0.14, 0.1, 0.08),
                           C["shell"], seg=6, rings=3) for i in range(3)]
    k.finish(S, name, P)


def coral(S, name, v):
    """A: coral ramificado. B: coral cerebro con esponjas tubo. C: abanicos de mar."""
    C = pal()
    rnd = random.Random(20 + v)
    P = [k.rock("Base", (0, 0, -0.3), (1.0, 0.9, 0.4), [M("#7A8A91")], seed=v, subdiv=1)]
    if v == 0:
        def branch(p, d, r, depth, i):
            q = p + d * (0.9 - 0.15 * depth)
            P.append(gs.tube(f"Br{i}", [p, (p + q) / 2 + Vector((0, 0, 0.1)), q], [r, r * 0.85, r * 0.7], C["coral"],
                             seg=6, sub=2, cap=2))
            if depth < 2:
                for j in range(2):
                    a = rnd.uniform(0, 6.28)
                    nd = (d + Vector((math.cos(a) * 0.7, math.sin(a) * 0.7, 0.3))).normalized()
                    branch(q, nd, r * 0.7, depth + 1, i * 3 + j + 1)
        for j in range(3):
            a = j * 2.1
            branch(Vector((0.3 * math.cos(a), 0.3 * math.sin(a), 0.1)),
                   Vector((0.4 * math.cos(a), 0.4 * math.sin(a), 1)).normalized(), 0.12, 0, 10 * (j + 1))
    elif v == 1:
        brain = gs.ellipsoid("Brain", (0, 0, 0.45), (0.9, 0.8, 0.55), [C["pink"], M("#C2416F")], seg=16, rings=9)
        gs.bands_by_plane(brain, (1, 0.4, 0), (0, 0, 0), 0.22, 0.4, 1, cut=True, wobble=0.0)
        P.append(brain)
        for i in range(4):
            a = i * 1.6 + 0.5
            b = Vector((0.8 * math.cos(a), 0.8 * math.sin(a), 0))
            P.append(gs.lathe(f"Tube{i}", [(0.0, 0.0), (0.16, 0.0), (0.2, 0.7 + 0.25 * i), (0.12, 0.72 + 0.25 * i),
                                          (0.0, 0.5)], C["purple"] if i % 2 else C["yellow"], loc=b, seg=8))
    else:
        for i in range(3):
            a = i * 1.2 - 1.2
            base = Vector((0.4 * math.sin(a), 0.2 * math.cos(a), 0.1))
            fan = gs.ellipsoid(f"Fan{i}", base + Vector((0, 0, 0.9)), (0.75, 0.06, 0.85),
                               [C["purple"], M("#B794F4")], seg=12, rings=7, rot=("Z", math.degrees(a) * 0.6))
            gs.bands_by_plane(fan, (1, 0, 0.5), (0, 0, 0), 0.2, 0.3, 1, cut=True)
            P.append(fan)
            P.append(gs.capsule(f"Stem{i}", base, base + Vector((0, 0, 0.3)), 0.06, C["purple"], seg=5, cap=1, sub=1))
        for i in range(5):
            a = i * 1.3
            P.append(k.leaf(f"Weed{i}", [(0.9 * math.cos(a), 0.9 * math.sin(a), 0), (0.95 * math.cos(a),
                                         0.95 * math.sin(a), 0.6), (0.8 * math.cos(a), 0.8 * math.sin(a), 1.2)],
                            0.1, C["kelp"]))
    k.finish(S, name, P)


def kelp(S, name):
    C = pal()
    rnd = random.Random(31)
    P = [k.rock("Base", (0, 0, -0.2), (0.6, 0.5, 0.3), [C["grey2"]], seed=3, subdiv=1)]
    for i in range(6):
        a = i / 6 * 2 * math.pi
        b = Vector((0.35 * math.cos(a), 0.35 * math.sin(a), 0))
        h = rnd.uniform(1.8, 3.0)
        pts = [b + Vector((0.25 * math.sin(z * 2 + i), 0.2 * math.cos(z * 2 + i), z)) for z in (0, h * 0.35, h * 0.7,
                                                                                               h)]
        P.append(k.leaf(f"Blade{i}", pts, 0.16, C["kelp"] if i % 2 else C["kelp2"], droop_up=(1, 0, 0)))
        P.append(gs.ellipsoid(f"Bulb{i}", pts[-1], 0.08, C["yellow"], seg=6, rings=4))
    k.finish(S, name, P)


def beach_bits(S, name, v):
    """A: estrella de mar y conchas. B: salvavidas flotando. C: botella con mensaje entre tablones."""
    C = pal()
    P = []
    if v == 0:
        for i in range(5):
            a = i / 5 * 2 * math.pi
            P.append(gs.capsule(f"Arm{i}", (0, 0, 0.08), (0.45 * math.cos(a), 0.45 * math.sin(a), 0.05), 0.12,
                                C["coral"], r1=0.05, seg=6, cap=2, sub=1))
        P.append(gs.ellipsoid("Center", (0, 0, 0.1), (0.18, 0.18, 0.08), C["coral"], seg=8, rings=4))
        for i in range(3):
            P.append(gs.ellipsoid(f"Dot{i}", (0.15 * math.cos(i * 2), 0.15 * math.sin(i * 2), 0.17), 0.03, C["yellow"],
                                  seg=4, rings=3))
        for i, (x, y) in enumerate(((0.8, 0.3), (-0.6, 0.6), (0.4, -0.7))):
            sh = gs.lathe(f"Shell{i}", [(0.0, 0.0), (0.18, 0.0), (0.12, 0.12), (0.0, 0.25)], [C["shell"], C["pink"]],
                          loc=(x, y, 0.0), axis=(0.3, 0.2, 1), seg=8)
            gs.bands_by_plane(sh, (1, 0, 0), (x, y, 0), 0.08, 0.5, 1, cut=True)
            P.append(sh)
    elif v == 1:
        ring = gs.torus("Ring", (0, 0, 0.0), 0.5, 0.17, [C["red"], C["white"]], seg=16, mseg=8)
        gs.bands_by_plane(ring, (1, 1, 0), (0, 0, 0), 0.7, 0.5, 1, cut=True)
        P += [ring, gs.torus("Rope", (0, 0, 0.0), 0.5, 0.185, C["rope"], seg=16, mseg=3, scale=(1, 1, 0.3)),
              foam_ring("Foam", 0.75, C)]
    else:
        for i in range(3):
            P.append(gs.rounded_box(f"Plank{i}", (0.1 * i, 0.4 * i - 0.4, 0.05), (2.0 - 0.4 * i, 0.3, 0.12), 0.04,
                                    C["wood"], rot=("Z", 15 * i - 10)))
        P.append(gs.lathe("Bottle", [(0.0, -0.3), (0.13, -0.3), (0.14, 0.05), (0.06, 0.15), (0.05, 0.3), (0.0, 0.3)],
                          C["glass"], loc=(0.3, 0.1, 0.25), axis=(1, 0.3, 0.3), seg=10))
        P.append(gs.capsule("Letter", (0.15, 0.05, 0.22), (0.45, 0.15, 0.31), 0.06, C["sand"], seg=6, cap=1, sub=1))
        P.append(gs.ellipsoid("Cork", (0.62, 0.2, 0.36), 0.06, C["wood"], seg=6, rings=4))
    k.finish(S, name, P)


def crate_p(S, name, v):
    C = pal()
    extra = None if v != 2 else [gs.tube(f"Banana{i}", gs.arc_points((0.1 * i - 0.1, 0.1 * i, 1.32), 0.3, 200, 340,
                                                                    5, plane=("X", "Z")), 0.07, C["yellow"], seg=6,
                                         cap=1) for i in range(3)]
    P = k.crate(v, C["wood"] if v != 1 else M("#C9A36B"), C["wood2"], C["teal"] if v == 0 else C["coral"], extra)
    if v == 0:
        for z in (0.32, 0.68):
            sq = [(0.56, -0.56, z), (0.56, 0.56, z), (-0.56, 0.56, z), (-0.56, -0.56, z), (0.56, -0.56, z)]
            P.append(gs.tube(f"RopeWrap{z}", sq, 0.04, C["rope"], seg=5, cap=1))
    P.append(k.leaf("Weed", [(0.5, -0.3, 1.0), (0.55, -0.45, 0.6), (0.5, -0.5, 0.2)], 0.12, C["kelp"],
                    droop_up=(0, 1, 0)))
    k.finish(S, name, P)


def barrel_p(S, name, v):
    C = pal()
    P = k.barrel(v, C["wood"], C["iron"], [C["red"], C["yellow"], C["coral"]][v])
    if v == 1:
        P.append(gs.rounded_box("Skull", (0, -0.43, 0.6), (0.3, 0.04, 0.3), 0.1, C["white"]))
    P.append(foam_ring("Foam", 0.6, C, z=0.05))
    k.finish(S, name, P)


def buoy_p(S, name, v):
    C = pal()
    P, mv = k.buoy(v, [C["yellow"], C["teal"], C["coral"]][v], C["grey2"] if v != 1 else C["white"], C["lamp"],
                   C["iron"])
    P.append(foam_ring("Foam", 0.9, C))
    k.finish(S, name, P, moving=mv or None)


PROPS = [("Lighthouse", lighthouse), ("Shipwreck", shipwreck), ("Rowboat", rowboat), ("Mast", mast), ("Net", net),
         ("AnchorPost", lambda S, n: anchor_post(S, n, 0)), ("Anchor", lambda S, n: anchor_post(S, n, 1)),
         ("Kelp", kelp), ("Starfish", lambda S, n: beach_bits(S, n, 0)),
         ("LifeRing", lambda S, n: beach_bits(S, n, 1)), ("Bottle", lambda S, n: beach_bits(S, n, 2))] \
    + [(f"Rock_{c}", (lambda v: lambda S, n: sea_rock(S, n, v))(i)) for i, c in enumerate("ABC")] \
    + [(f"Coral_{c}", (lambda v: lambda S, n: coral(S, n, v))(i)) for i, c in enumerate("ABC")] \
    + [(f"Crate_{c}", (lambda v: lambda S, n: crate_p(S, n, v))(i)) for i, c in enumerate("ABC")] \
    + [(f"Barrel_{c}", (lambda v: lambda S, n: barrel_p(S, n, v))(i)) for i, c in enumerate("ABC")] \
    + [(f"Buoy_{c}", (lambda v: lambda S, n: buoy_p(S, n, v))(i)) for i, c in enumerate("ABC")]

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    k.run_props(W, PROPS, only=set(argv) or None, cols=7)
