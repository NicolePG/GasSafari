"""Escenario W4 · Cuevas y Volcán. docs/09 §3 fila "Escenario W4", paleta de docs/04:
morado #3C1642, cian #4CC9F0, rojo magma #D00000; cristales y lava emisivos, acento amarillo-naranja.
blender --background --python env_w4.py [-- GiantCrystal ...]"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bmesh
import gs_common as gs
import env_kit as k
from gs_common import Vector, Matrix
from mathutils import noise
from env_kit import M, E

W = "W4"


def pal():
    return dict(purple=M("#3C1642"), purple2=M("#5A2A6B"), rock=M("#4A3B5C"), rock2=M("#2E2438"),
                basalt=M("#3A3540"), ash=M("#6B6470"), obsidian=M("#1B1424", 0.3), cyan=E("#4CC9F0", 3.0),
                cyan2=E("#9BE7FF", 2.0), magenta=E("#F72585", 3.0), lava=E("#FF8C00", 4.0), lava2=E("#FFC300", 5.0),
                magma=E("#D00000", 3.0), steam=M("#E8E4F0", 0.9), wood=M("#7A4A26"), iron=M("#2B2B33", 0.5),
                lamp=E("#FFB238", 4.0), yellow=M("#FFC300"), white=M("#F4F1EA"), red=M("#D00000"))


def crystal(name, base, h, r, mat, tilt=(0.0, 0.0), twist=0.0):
    """Prisma hexagonal con punta, inclinado."""
    o = gs.lathe(name, [(0.0, 0.0), (r, 0.0), (r * 1.05, h * 0.7), (r * 0.6, h * 0.86), (0.0, h)], mat, seg=6)
    o.data.transform(Matrix.Translation(Vector(base)) @ Matrix.Rotation(math.radians(tilt[1]), 4, "Y")
                     @ Matrix.Rotation(math.radians(tilt[0]), 4, "X") @ Matrix.Rotation(twist, 4, "Z"))
    return o


def cluster(prefix, specs, mats, seed=0):
    rnd = random.Random(seed)
    out = []
    for i, (x, y, h, r, tx, ty) in enumerate(specs):
        out.append(crystal(f"{prefix}{i}", (x, y, 0), h, r, mats[i % len(mats)], (tx, ty), rnd.uniform(0, 1)))
    return out


def roughen(obj, amp, freq=0.6, seed=0):
    off = Vector((seed * 2.1, seed * 1.3, seed * 0.7))
    for v in obj.data.vertices:
        v.co += Vector((noise.noise(v.co * freq + off), noise.noise(v.co * freq + off + Vector((5, 0, 0))),
                        noise.noise(v.co * freq + off + Vector((0, 5, 0))))) * amp
    return obj


def spike(name, base, h, r, mats, down=False, rings=7, seed=0):
    """Estalactita (down) o estalagmita con anillos de goteo y franjas alternas."""
    rnd = random.Random(seed)
    sg = -1 if down else 1
    prof = [(0.0, 0.0)]
    for i in range(rings + 1):
        u = i / rings
        rr = r * (1 - u) ** 0.9 * (1.12 if i % 2 else 0.95) * rnd.uniform(0.95, 1.05)
        prof.append((max(rr, 0.02), sg * h * u ** 0.9))
    prof.append((0.0, sg * h * 1.04))
    return gs.lathe(name, prof, mats, loc=base, seg=10, mat_fn=lambda i: (i // 2) % 2)


# ---------------------------------------------------------------------------
# Hitos
# ---------------------------------------------------------------------------
def giant_crystal(S, name):
    """Hito W4-2: cristales gigantes brillantes (≈ 14 m) sobre una base de roca, con fragmentos alrededor."""
    C = pal()
    P = [k.rock("Base", (0, 0, -0.8), (6.5, 5.5, 2.4), [C["rock"], C["purple2"]], seed=3, top_index=1)]
    P += cluster("Big", [(0, 0, 14.0, 1.6, 4, -6), (2.4, 1.0, 10.0, 1.2, -10, 22), (-2.3, 0.6, 9.0, 1.1, 8, -24),
                         (0.8, -2.2, 7.0, 0.9, 28, 10), (-1.0, 2.2, 8.0, 1.0, -26, -8), (3.6, -1.8, 5.0, 0.7, 20, 35),
                         (-3.8, -1.5, 5.5, 0.75, 18, -38)],
                 [C["cyan"], C["magenta"], C["cyan2"]], seed=1)
    rnd = random.Random(5)
    for i in range(7):
        a = i / 7 * 2 * math.pi + 0.3
        r = rnd.uniform(5.5, 7.5)
        P.append(crystal(f"Shard{i}", (r * math.cos(a), r * math.sin(a), -0.2), rnd.uniform(1.0, 2.2),
                         rnd.uniform(0.25, 0.4), [C["cyan"], C["magenta"]][i % 2],
                         (rnd.uniform(-30, 30), rnd.uniform(-30, 30))))
    k.finish(S, name, P, view=(0.75, -0.8, 0.3), margin=1.05)


def volcano_crater(S, name):
    """Hito W4-5: cráter del volcán (Ø ≈ 34 m, 12 m de alto) con lago y fuente de lava, coladas y humo."""
    C = pal()
    cone = gs.lathe("Cone", [(0.0, -1.0), (17.0, -1.0), (16.0, 1.0), (12.0, 6.0), (9.0, 10.5), (8.0, 11.6),
                             (7.2, 11.2), (6.6, 8.5), (0.0, 8.0)], [C["basalt"], C["ash"], C["magma"]], seg=32)
    roughen(cone, 0.9, 0.3, seed=2)
    gs.paint_faces(cone, lambda c: c.z > 10.8, 1)
    gs.paint_faces(cone, lambda c: 8.0 < c.z < 11.0 and math.hypot(c.x, c.y) < 8.0, 2)
    P = [cone, gs.rounded_cylinder("LavaLake", (0, 0, 8.6), 6.9, 0.5, 0.15, [C["lava"]], seg=24, steps=1),
         gs.lathe("Fountain", [(0.0, 8.6), (1.6, 8.7), (0.9, 9.6), (1.2, 10.8), (0.5, 12.0), (0.0, 12.6)],
                  [C["lava2"]], seg=10)]
    rnd = random.Random(7)
    for i in range(6):
        a = i / 6 * 2 * math.pi + rnd.uniform(-0.3, 0.3)
        r = rnd.uniform(2.5, 5.0)
        P.append(k.rock(f"Float{i}", (r * math.cos(a), r * math.sin(a), 8.7), (0.9, 0.8, 0.5), [C["obsidian"]],
                        seed=i, subdiv=1))
        P.append(gs.ellipsoid(f"Blob{i}", (1.4 * math.cos(a + 0.5), 1.4 * math.sin(a + 0.5), 12.5 + 0.8 * (i % 3)),
                              0.35, C["lava2"], seg=6, rings=4))
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.8
        pts = []
        for j in range(8):
            u = j / 7
            rr = 7.8 + 9.0 * u
            ang = a + 0.25 * math.sin(u * 4 + i)
            z = 11.6 - 11.4 * (u ** 0.8)
            pts.append(Vector((rr * math.cos(ang), rr * math.sin(ang), z + 0.25)))
        P.append(gs.tube(f"Flow{i}", pts, [1.3, 1.2, 1.1, 1.0, 1.0, 1.1, 1.3, 1.6], [C["lava"]], seg=6, sub=2, cap=2,
                         flatten=((0, 0, 1), 0.35)))
        P.append(gs.ellipsoid(f"Pool{i}", pts[-1], (2.2, 2.2, 0.3), C["lava2"], seg=10, rings=4))
    for i, (x, y, z, r) in enumerate(((0.5, 0.3, 15.0, 2.2), (-1.5, 1.0, 17.5, 2.8), (1.0, 2.5, 20.5, 3.2))):
        P.append(gs.ellipsoid(f"Smoke{i}", (x, y, z), r, M("#5E5866"), seg=10, rings=6))
    for i in range(10):
        a = rnd.uniform(0, 6.28)
        r = rnd.uniform(11, 16)
        P.append(k.rock(f"Boulder{i}", (r * math.cos(a), r * math.sin(a), 0.5 + (17 - r) * 0.45), (1.1, 1.0, 0.9),
                        [C["basalt"], C["magma"]], seed=10 + i, subdiv=1, top_index=1, top_dot=0.9))
    k.finish(S, name, P, view=(0.7, -0.8, 0.45), margin=1.0)


# ---------------------------------------------------------------------------
# Cueva
# ---------------------------------------------------------------------------
def cave_profile(n=12):
    outer = [(7.5 * math.cos(math.radians(180 - 180 * i / n)), 0.0 + 7.8 * math.sin(math.radians(180 - 180 * i / n)))
             for i in range(n + 1)]
    inner = [(6.0 * math.cos(math.radians(180 * i / n)), 0.0 + 6.4 * math.sin(math.radians(180 * i / n)))
             for i in range(n + 1)]
    return [(-7.5, -1.6)] + outer + [(7.5, -1.6), (6.0, -1.6)] + inner + [(-6.0, -1.6)]


def cave_tunnel(S, name, curve=False):
    """Túnel de cueva modular (cauce de 10-12 m): recto de 10 m o curva de 45°. Con cristales y estalactitas."""
    C = pal()
    if curve:
        path = [Vector((p.x, p.y, 0)) for p in k.arc_path((-20, 0, 0), 20, 0, 45, 9)]
    else:
        path = [(0, -5 + 10 * i / 4, 0) for i in range(5)]
    shell = k.sweep("Shell", cave_profile(), path, [C["rock"], C["purple2"]])
    roughen(shell, 0.45, 0.35, seed=4)
    rad = (lambda c: math.hypot(math.hypot(c.x + 20, c.y) - 20, c.z)) if curve else (lambda c: math.hypot(c.x, c.z))
    gs.paint_faces(shell, lambda c: rad(c) < 6.9 and c.z > 0.3, 1)
    P = [shell]
    for j, ang in enumerate((35, 145, 80)):
        off = 5.7 * math.cos(math.radians(ang))
        zz = 5.9 * math.sin(math.radians(ang))
        if curve:
            vein = [Vector((-20, 0, 0)) + (Vector(p) - Vector((-20, 0, 0))).normalized() * (20 + off)
                    + Vector((0, 0, zz)) for p in path]
        else:
            vein = [Vector(p) + Vector((off, 0, zz)) for p in path]
        vein = [v + Vector((0.2 * math.sin(i * 1.7 + j), 0, 0.3 * math.cos(i * 2.3 + j))) for i, v in enumerate(vein)]
        P.append(gs.tube(f"Vein{j}", vein, 0.12, [C["cyan"], C["magenta"], C["cyan2"]][j], seg=5, sub=2, cap=1))
    rnd = random.Random(9 + curve)
    pts = [Vector(p) for p in path]
    for i in range(5):
        p = pts[rnd.randrange(len(pts))]
        out = (p - Vector((-20, 0, 0))).normalized() if curve else Vector((1, 0, 0))
        top = p + Vector((0, 0, 6.2)) + out * rnd.uniform(-3, 3)
        P.append(gs.lathe(f"Stal{i}", [(0.0, 0.0), (0.5, 0.0), (0.25, -1.2), (0.0, -2.2 - rnd.uniform(0, 1))],
                          C["rock2"], loc=top, seg=7))
    for i in range(4):
        p = pts[rnd.randrange(len(pts))]
        s = 1 if i % 2 else -1
        out = (p - Vector((-20, 0, 0))).normalized() if curve else Vector((1, 0, 0))
        base = p + out * 5.6 * s + Vector((0, 0, rnd.uniform(1.0, 3.5)))
        P.append(crystal(f"Wall{i}", base, rnd.uniform(1.2, 2.0), 0.3, [C["cyan"], C["magenta"]][i % 2],
                         (0, -60 * s)))
    k.finish(S, name, P, view=(0.55, -0.8, 0.35))


def cave_rock(S, name, v):
    C = pal()
    P = k.rock_variant(v, [C["rock"], C["purple2"]], seed=50 + v, scale=1.5, subdiv=3)
    P.append(crystal("Gem", (0.6, -0.5, 0.3 if v != 1 else 0.6), 0.9, 0.18, C["cyan"], (20, 15)))
    k.finish(S, name, P)


def crystal_cluster(S, name, v):
    C = pal()
    mats = [[C["cyan"], C["cyan2"]], [C["magenta"], E("#FF8AC4", 2.5)], [C["cyan"], C["magenta"], C["cyan2"]]][v]
    specs = [[(0, 0, 2.4, 0.4, 5, 0), (0.5, 0.2, 1.6, 0.3, -10, 25), (-0.45, 0.25, 1.4, 0.28, 8, -25),
              (0.1, -0.45, 1.1, 0.24, 25, 5)],
             [(0, 0, 1.8, 0.45, 0, 10), (0.6, -0.1, 2.6, 0.38, 0, -15), (-0.6, 0.3, 1.2, 0.3, -15, -30),
              (0.2, 0.6, 0.9, 0.25, -30, 10), (-0.3, -0.55, 0.8, 0.22, 30, -10)],
             [(0, 0, 3.0, 0.5, -5, 8), (0.8, 0.4, 2.0, 0.35, -18, 30), (-0.7, -0.3, 2.2, 0.38, 15, -28)]][v]
    P = [k.rock("Base", (0, 0, -0.3), (1.3, 1.1, 0.5), [C["rock"]], seed=v, subdiv=1)]
    P += cluster("C", specs, mats, seed=v)
    k.finish(S, name, P)


def stalactite(S, name, v):
    """Cuelga del techo: pivote arriba (z = 0) y la punta hacia abajo. A: una con gota. B: racimo."""
    C = pal()
    mats = [C["rock"], C["purple2"]]
    if v == 0:
        P = [spike("Stal", (0, 0, 0), 4.8, 0.95, mats, down=True, seed=1),
             gs.ellipsoid("Drop", (0, 0, -5.25), (0.12, 0.12, 0.18), C["cyan2"], seg=6, rings=4),
             crystal("Gem", (0.6, 0.2, -0.6), 0.9, 0.18, C["cyan"], (180 + 25, 15))]
    else:
        P = [k.rock("Root", (0, 0, -0.6), (1.7, 1.5, 0.6), [C["rock"]], seed=6, subdiv=2, flat_bottom=0.0)]
        for i in range(5):
            a = i / 5 * 2 * math.pi
            r = 0.3 + 0.7 * (i % 2)
            P.append(spike(f"Stal{i}", (r * math.cos(a), r * math.sin(a), -0.2), 1.8 + 0.5 * i, 0.5, mats, down=True,
                           rings=5, seed=i))
    k.finish(S, name, P, view=(0.75, -0.8, -0.1))


def stalagmite(S, name, v):
    C = pal()
    mats = [C["rock"], C["purple2"]]
    if v == 0:
        P = [spike("Stag", (0, 0, 0), 4.2, 1.2, mats, seed=2),
             crystal("Gem", (0.7, -0.5, 0.2), 1.1, 0.22, C["magenta"], (25, 20)),
             crystal("Gem2", (-0.6, -0.4, 0.1), 0.8, 0.18, C["cyan"], (20, -30))]
    else:
        P = []
        for i, (x, y, h) in enumerate(((0, 0, 3.2), (0.9, 0.3, 2.0), (-0.7, 0.5, 1.5), (0.2, -0.8, 1.1))):
            P.append(spike(f"Stag{i}", (x, y, 0), h, 0.55 * h / 3 + 0.15, mats, rings=5, seed=i + 3))
        P.append(gs.ellipsoid("Puddle", (0.3, -0.2, 0.02), (1.2, 0.9, 0.05), C["cyan2"], seg=10, rings=3))
    k.finish(S, name, P)


def geyser_vent(S, name):
    """Boca de géiser (H del W4-3): montículo con borde caliente y vapor quieto en la base."""
    C = pal()
    mound = gs.lathe("Mound", [(0.0, -0.3), (2.6, -0.3), (2.2, 0.5), (1.2, 1.2), (0.75, 1.3), (0.6, 0.9),
                               (0.0, 0.6)], [C["ash"], C["lava"]], seg=16)
    roughen(mound, 0.15, 0.8, seed=5)
    gs.paint_faces(mound, lambda c: c.z > 0.8 and math.hypot(c.x, c.y) < 0.85, 1)
    P = [mound, gs.torus("Rim", (0, 0, 1.25), 0.85, 0.12, C["yellow"], seg=14, mseg=4)]
    for i in range(6):
        a = i / 6 * 2 * math.pi
        P.append(gs.ellipsoid(f"Crust{i}", (1.8 * math.cos(a), 1.8 * math.sin(a), 0.25), (0.35, 0.3, 0.12),
                              M("#C9B46A"), seg=6, rings=3))
    for i in range(3):
        a = i * 2.2
        P.append(gs.ellipsoid(f"Puff{i}", (0.5 * math.cos(a), 0.5 * math.sin(a), 1.6 + 0.3 * i), 0.4 + 0.1 * i,
                              C["steam"], seg=8, rings=5))
    k.finish(S, name, P)


def crack(S, name):
    """Grieta de lava en una laja de roca (6 m)."""
    C = pal()
    slab = k.rock("Slab", (0, 0, -0.6), (4.0, 2.6, 0.7), [C["basalt"]], seed=8, subdiv=2, rough=0.15)
    P = [slab]
    rnd = random.Random(2)
    pts = [Vector((-3.5 + 7.0 * i / 8, rnd.uniform(-0.5, 0.5), 0.12)) for i in range(9)]
    P.append(gs.tube("Glow", pts, [0.12, 0.32, 0.48, 0.55, 0.48, 0.55, 0.4, 0.24, 0.1], [C["lava2"]], seg=5, cap=1,
                     flatten=((0, 0, 1), 0.25)))
    P.append(gs.tube("GlowOuter", [p + Vector((0, 0, -0.03)) for p in pts], [0.2, 0.55, 0.8, 0.9, 0.8, 0.9, 0.65,
                                                                             0.4, 0.16], [C["magma"]], seg=5, cap=1,
                     flatten=((0, 0, 1), 0.2)))
    for j in (2, 5):
        q = pts[j]
        P.append(gs.tube(f"Branch{j}", [q, q + Vector((0.6, 0.8, 0)), q + Vector((0.8, 1.6, -0.02))], [0.15, 0.1, 0.04],
                         [C["lava"]], seg=4, cap=1, flatten=((0, 0, 1), 0.25)))
    k.finish(S, name, P, view=(0.5, -0.8, 0.8))


def lava_rock(S, name, v):
    """Rocas con vetas de lava (A) o charco de lava rodeado de piedras (B)."""
    C = pal()
    if v == 0:
        r = k.rock("Rock", (0, 0, 0), (1.8, 1.5, 1.4), [C["basalt"], C["lava"]], seed=12, subdiv=3)
        rnd = random.Random(1)
        bm = bmesh.new()
        bm.from_mesh(r.data)
        for f in bm.faces:
            c = f.calc_center_median()
            if abs(math.sin(c.x * 2.2 + c.z * 1.5)) < 0.3:
                f.material_index = 1
        bm.to_mesh(r.data)
        bm.free()
        P = [r]
    else:
        P = [gs.ellipsoid("Pool", (0, 0, 0.05), (1.8, 1.4, 0.12), [C["lava"]], seg=14, rings=5)]
        for i in range(9):
            a = i / 9 * 2 * math.pi
            P.append(k.rock(f"Ring{i}", (2.0 * math.cos(a), 1.6 * math.sin(a), 0), (0.6, 0.5, 0.45),
                            [C["basalt"], C["magma"]], seed=i, subdiv=1, top_index=1, top_dot=0.95))
        P.append(gs.ellipsoid("Bubble", (0.4, 0.2, 0.15), 0.25, C["lava2"], seg=8, rings=5))
    k.finish(S, name, P)


def glow_mushrooms(S, name):
    C = pal()
    P = []
    for i, (x, y, h, r) in enumerate(((0, 0, 0.9, 0.45), (0.6, 0.3, 0.6, 0.3), (-0.5, 0.35, 0.5, 0.28),
                                      (0.2, -0.55, 0.4, 0.22), (-0.3, -0.4, 0.7, 0.32))):
        P.append(gs.capsule(f"Stem{i}", (x, y, 0), (x + 0.05, y, h), r * 0.25, C["white"], seg=6, cap=1, sub=1))
        P.append(gs.ellipsoid(f"Cap{i}", (x + 0.05, y, h), (r, r, r * 0.5), [C["cyan"], C["magenta"]][i % 2], seg=10,
                              rings=5))
        P.append(gs.ellipsoid(f"Gill{i}", (x + 0.05, y, h - r * 0.15), (r * 0.85, r * 0.85, r * 0.15), C["purple2"],
                              seg=8, rings=3))
    k.finish(S, name, P)


def lantern(S, name):
    """Farol de minero en un poste con cartel de lava (sin texto)."""
    C = pal()
    P = [gs.capsule("Post", (0, 0, 0), (0, 0, 2.6), 0.1, C["wood"], seg=6, cap=1, sub=1),
         gs.capsule("Arm", (0, 0, 2.4), (0.6, 0, 2.5), 0.06, C["wood"], seg=5, cap=1, sub=1),
         gs.capsule("Hook", (0.6, 0, 2.5), (0.6, 0, 2.25), 0.02, C["iron"], seg=4, cap=1, sub=1),
         gs.rounded_box("LampFrame", (0.6, 0, 1.95), (0.32, 0.32, 0.45), 0.06, C["iron"]),
         gs.ellipsoid("LampGlow", (0.6, 0, 1.95), (0.17, 0.17, 0.22), C["lamp"], seg=8, rings=5),
         gs.rounded_box("Board", (0, -0.12, 1.4), (0.9, 0.08, 0.7), 0.06, C["yellow"], rot=("Y", -6)),
         gs.lathe("Flame", [(0.0, 0.0), (0.18, 0.05), (0.12, 0.25), (0.0, 0.42)], C["red"], loc=(0, -0.18, 1.18),
                  seg=6)]
    k.finish(S, name, P)


PROPS = [("GiantCrystal", giant_crystal), ("VolcanoCrater", volcano_crater),
         ("CaveTunnel_Straight", cave_tunnel), ("CaveTunnel_Curve45", lambda S, n: cave_tunnel(S, n, True)),
         ("Stalactite_A", lambda S, n: stalactite(S, n, 0)), ("Stalactite_B", lambda S, n: stalactite(S, n, 1)),
         ("Stalagmite_A", lambda S, n: stalagmite(S, n, 0)), ("Stalagmite_B", lambda S, n: stalagmite(S, n, 1)),
         ("GeyserVent", geyser_vent), ("Crack", crack), ("LavaRock", lambda S, n: lava_rock(S, n, 0)),
         ("LavaPool", lambda S, n: lava_rock(S, n, 1)), ("GlowMushrooms", glow_mushrooms), ("Lantern", lantern)] \
    + [(f"CaveRock_{c}", (lambda v: lambda S, n: cave_rock(S, n, v))(i)) for i, c in enumerate("ABC")] \
    + [(f"Crystal_{c}", (lambda v: lambda S, n: crystal_cluster(S, n, v))(i)) for i, c in enumerate("ABC")]

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    k.run_props(W, PROPS, only=set(argv) or None, cols=7)
