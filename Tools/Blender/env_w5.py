"""Escenario W5 · El Gran Regreso (cañón al atardecer). docs/09 §3 fila "Escenario W5", paleta de docs/04:
dorado #FFB703, rojo #9C3D1C, naranja #FB8500; acento complementario turquesa y azul noche.
blender --background --python env_w5.py [-- NaturalArch Slide_Straight ...]"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
import env_kit as k
from gs_common import Vector, Matrix
from mathutils import noise
from env_kit import M, E

W = "W5"


def pal():
    return dict(gold=M("#FFB703"), red=M("#9C3D1C"), orange=M("#FB8500"), sand=M("#E9B872"), rock=M("#B5562A"),
                rock2=M("#7A3418"), teal=M("#219EBC"), navy=M("#023047"), wood=M("#A0703F"), wood2=M("#6B4A2A"),
                rope=M("#D9B77A"), white=M("#F4F1EA"), green=M("#5A8F3A"), green2=M("#3E6B2A"), cactus=M("#4E9A5A"),
                pink=M("#FF6FA5"), concrete=M("#B8B2A8"), concrete2=M("#8C867C"), black=M("#22232B"),
                water=M("#7FDBFF", 0.1), lamp=E("#FFB238", 4.0), neon=E("#3AE1FF", 2.5), yellow=M("#FFD23F"))


def flat_top(obj, frac=0.72):
    """Corta la parte de arriba de una roca a una fracción de su altura (meseta)."""
    zmax = max(v.co.z for v in obj.data.vertices)
    zmin = min(v.co.z for v in obj.data.vertices)
    cut = zmin + (zmax - zmin) * frac
    for v in obj.data.vertices:
        v.co.z = min(v.co.z, cut)
    return obj


def strata(obj, z0=0.0, period=0.9, idx=1):
    """Capas de roca horizontales (mesas del cañón)."""
    gs.bands_by_plane(obj, (0, 0, 1), (0, 0, z0), period, 0.45, idx, cut=True)
    return obj


def roughen(obj, amp, freq=0.4, seed=0):
    off = Vector((seed * 2.1, seed * 1.3, seed * 0.7))
    for v in obj.data.vertices:
        v.co += Vector((noise.noise(v.co * freq + off), noise.noise(v.co * freq + off + Vector((5, 0, 0))),
                        noise.noise(v.co * freq + off + Vector((0, 5, 0))) * 0.4)) * amp
    return obj


# ---------------------------------------------------------------------------
# Hitos
# ---------------------------------------------------------------------------
def natural_arch(S, name):
    """Hito W5: arco de roca natural a capas sobre el río (luz ≈ 16 m, 15 m de alto)."""
    C = pal()
    path = [Vector((10.0 * math.cos(math.radians(a)), 0, 1.0 + 13.0 * max(0.0, math.sin(math.radians(a))) ** 0.85))
            for a in range(-5, 186, 10)]
    prof = [(-2.4, -1.6), (2.4, -1.6), (2.9, 0.4), (2.0, 1.8), (-1.8, 2.0), (-2.9, 0.2)]
    arch = k.sweep("Arch", lambda u: [(x * (1.25 - 0.45 * math.sin(math.pi * u)), z * (1.2 - 0.35 * math.sin(math.pi * u)))
                                      for x, z in prof], path, [C["rock"], C["orange"]], up=(0, 1, 0))
    roughen(arch, 0.8, 0.35, seed=3)
    strata(arch, 0.0, 1.6)
    P = [arch]
    for i, (x, y, sx, sz) in enumerate(((-11.5, 0.5, 4.0, 3.0), (11.0, -0.5, 3.6, 2.6), (-9.0, -3.0, 2.0, 1.4),
                                         (9.5, 3.0, 2.2, 1.6))):
        r = k.rock(f"Base{i}", (x, y, -0.5), (sx, sx * 0.8, sz), [C["rock2"], C["sand"]], seed=i + 4, top_index=1)
        P.append(r)
    for i, (x, z) in enumerate(((-6.0, 12.6), (-2.0, 14.6), (3.0, 14.4))):
        P.append(k.bush(f"Shrub{i}", (x, 0.3, z), 0.8, [C["green"], C["green2"]], seed=i, n=2)[0])
    P.append(gs.capsule("Vine", (-1.0, -1.8, 13.5), (-1.3, -2.0, 9.5), 0.08, C["green2"], seg=5, cap=1, sub=1))
    k.finish(S, name, P, view=(0.45, -1, 0.25), margin=1.05)


def slide_tower(S, name):
    """Hito W5-5: torre de salida del megatobogán (≈ 22 m) sobre una aguja de roca, con plataforma, compuerta de
    salida, escalera y banderas móviles. La rampa de arranque sale hacia -Y a la altura de la plataforma."""
    C = pal()
    P = [k.rock("Spire", (0, 0, 0), (5.0, 4.5, 11.0), [C["rock"], C["orange"]], seed=9, rough=0.25, subdiv=2,
                top_index=1, top_dot=0.8)]
    strata(P[0], 0.0, 2.0)
    for s in (1, -1):
        for t in (1, -1):
            P.append(gs.capsule(f"Leg{s}{t}", (3.2 * s, 3.2 * t, 9.0), (2.6 * s, 2.6 * t, 17.0), 0.32, C["wood2"],
                                seg=6, cap=1, sub=1))
        P.append(gs.capsule(f"BraceA{s}", (3.1 * s, -3.1, 11.0), (2.7 * s, 2.7, 16.0), 0.15, C["wood"], seg=5, cap=1,
                            sub=1))
    P.append(gs.rounded_box("Platform", (0, 0, 17.3), (7.4, 7.4, 0.6), 0.15, C["wood"]))
    P.append(gs.rounded_box("Hut", (0, 1.2, 19.2), (5.0, 4.0, 3.4), 0.4, C["teal"], taper=(0.92, 0.92), bulge=0.05))
    P.append(gs.cone("Roof", (0, 1.2, 20.8), (0, 1.2, 24.2), 4.4, 0.1, C["orange"], seg=8))
    P.append(gs.ellipsoid("RoofBall", (0, 1.2, 24.4), 0.4, C["gold"], seg=8, rings=5))
    P.append(gs.rounded_box("Window", (0, -0.85, 19.6), (2.6, 0.12, 1.2), 0.3, C["lamp"]))
    gate = gs.rounded_box("StartGate", (0, -3.7, 18.6), (6.2, 0.4, 2.2), 0.2, [C["gold"], C["navy"]])
    gs.bands_by_plane(gate, (1, 0, 0.8), (0, 0, 0), 0.8, 0.5, 1, cut=True)
    P.append(gate)
    for s in (1, -1):
        P.append(gs.capsule(f"GatePost{s}", (3.3 * s, -3.7, 17.4), (3.3 * s, -3.7, 20.6), 0.22, C["navy"], seg=6,
                            cap=1, sub=1))
        P.append(gs.ellipsoid(f"GateLamp{s}", (3.3 * s, -3.7, 20.9), 0.32, C["lamp"], seg=8, rings=5))
    chute = k.sweep("Chute", slide_profile(), [(0, -3.7, 17.6), (0, -8.0, 16.4), (0, -11.5, 14.2)],
                    [C["teal"], C["orange"]], sub=3)
    P.append(chute)
    for j in range(7):
        z = 9.6 + 1.1 * j
        P.append(gs.capsule(f"Rung{j}", (3.0, 3.7, z), (3.0, 4.4, z), 0.05, C["wood2"], seg=4, cap=1, sub=1))
    for y in (3.7, 4.4):
        P.append(gs.capsule(f"Rail{y}", (3.0, y, 9.0), (3.0, y, 17.3), 0.07, C["wood2"], seg=4, cap=1, sub=1))
    moving = []
    for i, (x, y) in enumerate(((3.5, 3.5), (-3.5, 3.5))):
        P.append(gs.capsule(f"Pole{i}", (x, y, 17.6), (x, y, 22.6), 0.08, C["white"], seg=5, cap=1, sub=1))
        f = k.flag(f"Flag{i}", (x, y, 22.2), 2.0, 1.1, [C["teal"], C["gold"]], stripes=3)
        moving.append((f"Flag_{i}", (x, y, 22.2), [f]))
    k.finish(S, name, P, moving=moving, view=(0.75, -0.8, 0.3), margin=1.05)


# ---------------------------------------------------------------------------
# Megatobogán modular (canal en U de 6 m, agua en el fondo)
# ---------------------------------------------------------------------------
def slide_profile(half=3.2, th=0.35, n=9):
    inner = [(half * math.cos(math.radians(180 + 180 * i / n)), 1.6 + half * 0.75 * math.sin(math.radians(180 + 180 * i / n)))
             for i in range(n + 1)]
    outer = [((half + th) * math.cos(math.radians(-180 * i / n)), 1.6 + (half * 0.75 + th) * math.sin(math.radians(-180 * i / n)))
             for i in range(n + 1)]
    return ([(-half - th - 0.2, 1.9), (-half + 0.05, 1.9)] + inner[1:-1] + [(half - 0.05, 1.9), (half + th + 0.2, 1.9)]
            + outer[1:-1])


def slide(S, name, kind):
    """Pieza del megatobogán (pivote en el centro del fondo de entrada): recta 10 m, curva 45° (radio 15 m),
    caída de 5 m en 10 m o rampa de salto."""
    C = pal()
    if kind == "straight":
        path = [(0, -5 + 10 * i / 4, 0) for i in range(5)]
    elif kind == "curve":
        path = [Vector((p.x, p.y, 0)) for p in k.arc_path((-15, 0, 0), 15, 0, 45, 9)]
    elif kind == "drop":
        path = [(0, -5 + 10 * i / 8, -5.0 * (0.5 - 0.5 * math.cos(math.pi * i / 8))) for i in range(9)]
    else:
        path = [(0, -5 + 8 * i / 8, 2.2 * (i / 8) ** 2) for i in range(9)]
    chute = k.sweep("Chute", slide_profile(), path, [C["teal"], C["orange"], C["water"]])
    pts = [Vector(p) for p in path]
    P = [chute]
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        side = t.cross(Vector((0, 0, 1))).normalized()
        for s in (1, -1):
            if i % 2 == 0:
                P.append(gs.ellipsoid(f"Stud{i}{s}", p + side * s * 3.7 + Vector((0, 0, 1.95)), 0.22,
                                      C["orange"] if (i // 2) % 2 else C["gold"], seg=6, rings=4))
    water = k.sweep("Water", [(-2.6, -0.32), (2.6, -0.32), (2.6, -0.22), (-2.6, -0.22)], path, [C["water"]])
    P.append(water)
    for i, p in enumerate(pts[::2]):
        base_z = -7.0
        for s in (1, -1):
            t = (pts[-1] - pts[0]).normalized()
            side = t.cross(Vector((0, 0, 1))).normalized()
            q = p + side * s * 2.2
            P.append(gs.capsule(f"Trestle{i}{s}", q + Vector((0, 0, -0.6)), Vector((q.x * 1.15, q.y, base_z)), 0.22,
                                C["wood2"], seg=6, cap=1, sub=1))
        P.append(gs.capsule(f"Cross{i}", p + Vector((0, 0, -3.0)) - side * 2.4, p + Vector((0, 0, -3.0)) + side * 2.4,
                            0.12, C["wood"], seg=5, cap=1, sub=1))
    if kind == "ramp":
        P.append(gs.rounded_box("Kicker", Vector(path[-1]) + Vector((0, -0.4, 0.2)), (5.6, 0.8, 0.5), 0.15,
                                [C["gold"]], rot=("X", -25)))
    k.finish(S, name, P, view=(0.7, -0.8, 0.5))


# ---------------------------------------------------------------------------
# Cañón, puentes, rampas, manglar y represa
# ---------------------------------------------------------------------------
def canyon_rock(S, name, v):
    C = pal()
    if v == 0:
        o = k.rock("Mesa", (0, 0, -0.5), (3.6, 3.0, 4.5), [C["rock"], C["orange"]], seed=60, rough=0.18, subdiv=3)
        flat_top(o, 0.68)
        top = max(v.co.z for v in o.data.vertices)
        P = [strata(o, 0.0, 1.1)]
        P.append(k.bush("Shrub", (0.5, 0.3, top), 0.7, [C["green"], C["green2"]], seed=1, n=2)[0])
        P.append(k.flower("Fl", (-0.8, -0.4, top), C["pink"], C["yellow"], stem=C["green2"], size=0.2, h=0.3))
    elif v == 1:
        P = []
        for i, (x, y, h, r) in enumerate(((0, 0, 7.0, 1.4), (1.6, 0.8, 4.5, 1.0), (-1.2, 1.0, 3.2, 0.9))):
            o = gs.lathe(f"Hoodoo{i}", [(0.0, 0.0), (r * 1.3, 0.0), (r, h * 0.3), (r * 1.15, h * 0.5), (r * 0.8, h * 0.75),
                                        (r * 0.9, h * 0.9), (0.0, h)], [C["rock"], C["orange"]], loc=(x, y, 0), seg=10)
            roughen(o, 0.15, 0.8, seed=i)
            strata(o, 0.0, 0.8)
            P.append(o)
            P.append(k.rock(f"Cap{i}", (x, y, h - 0.3), (r * 1.4, r * 1.3, 0.6), [C["rock2"]], seed=i, subdiv=1))
    else:
        P = k.rock_variant(2, [C["rock"], C["sand"]], seed=62, scale=1.6, subdiv=2)
        for o in P:
            strata(o, 0.0, 0.5)
    k.finish(S, name, P)


def broken_bridge(S, name):
    """Puente colgante roto (W5-2): tablones y cuerdas colgando desde cada orilla, hueco de 14 m."""
    C = pal()
    P = []
    for s in (1, -1):
        x = 9.0 * s
        cliff = k.rock(f"Cliff{s}", (x + 2.0 * s, 0, -4.0), (4.0, 4.5, 8.0), [C["rock"], C["orange"]], seed=s + 2,
                       rough=0.2, subdiv=2)
        for vv in cliff.data.vertices:
            vv.co.z = min(vv.co.z, 4.6)
        P.append(strata(cliff, -4.0, 1.4))
        for y in (-1.3, 1.3):
            P.append(gs.capsule(f"Post{s}{y}", (x + 0.4 * s, y, 4.2), (x + 0.4 * s, y, 6.6), 0.2, C["wood2"], seg=6,
                                cap=1, sub=1))
        hang = [Vector((x, 0, 4.7))]
        for j in range(1, 7):
            hang.append(Vector((x - s * 0.55 * j, 0.1 * j, 4.7 - 0.95 * j)))
        for y in (-1.3, 1.3):
            P.append(gs.tube(f"Rope{s}{y}", [h + Vector((0, y, 0.9)) for h in hang], 0.09, C["rope"], seg=5, sub=1,
                             cap=1))
            P.append(gs.tube(f"RopeLow{s}{y}", [h + Vector((0, y, 0.0)) for h in hang], 0.09, C["rope"], seg=5, sub=1,
                             cap=1))
        for j, h in enumerate(hang[1:]):
            P.append(gs.rounded_box(f"Plank{s}{j}", h + Vector((0, 0, -0.05)), (0.55, 2.6, 0.14), 0.04,
                                    C["wood"] if j % 2 else C["gold"], rot=[("Y", -55 * s), ("X", 10 * (j % 3 - 1))],
                                    segments=1))
    P.append(gs.rounded_box("Floating", (1.0, 1.5, 0.1), (0.35, 2.2, 0.1), 0.03, C["wood"], rot=("Z", 40)))
    k.finish(S, name, P, view=(0.4, -1, 0.25))


def ramp(S, name, v):
    """A: rampa de madera para saltar (sale del agua a 2.6 m). B: rampa natural de roca lisa."""
    C = pal()
    if v == 0:
        prof = [(-3.0, 0.0), (3.0, 0.0), (3.0, 0.3), (-3.0, 0.3)]
        path = [(0, -4 + 8 * i / 6, -0.6 + 3.2 * (i / 6) ** 1.6) for i in range(7)]
        deck = k.sweep("Deck", prof, path, [C["wood"], C["wood2"]], up=(0, 0, 1))
        gs.bands_by_plane(deck, (0, 1, 0), (0, -4, 0), 0.8, 0.12, 1, cut=True)
        P = [deck]
        for s in (1, -1):
            P.append(gs.tube(f"Rail{s}", [Vector(p) + Vector((3.05 * s, 0, 0.35)) for p in path], 0.12, C["gold"],
                             seg=6, cap=1))
            for j in (2, 4, 6):
                p = Vector(path[j])
                P.append(gs.capsule(f"Leg{s}{j}", p + Vector((2.6 * s, 0, 0)), Vector((2.6 * s, p.y, -1.5)), 0.15,
                                    C["wood2"], seg=5, cap=1, sub=1))
        P.append(gs.rounded_box("Arrow", (0, 2.0, 1.6), (1.0, 1.6, 0.06), 0.03, C["teal"], rot=("X", -22)))
    else:
        o = k.rock("Slab", (0, 0, -1.0), (3.5, 5.0, 2.0), [C["rock"], C["sand"]], seed=70, rough=0.15, subdiv=3,
                   top_index=1, top_dot=0.4)
        for vv in o.data.vertices:  # cuña: sube hacia +Y
            u = max(0.0, min(1.0, (vv.co.y + 5) / 10))
            if vv.co.z > 0:
                vv.co.z *= 0.15 + 1.6 * u * u
        P = [strata(o, -1.0, 0.7, 1)]
    k.finish(S, name, P)


def mangrove(S, name, v):
    """Árbol de manglar sobre raíces zanco (W5-3). B más bajo y ancho."""
    C = pal()
    rnd = random.Random(80 + v)
    H = 6.0 if v == 0 else 4.5
    P = [gs.tube("Trunk", [(0, 0, 1.8), (0.3, 0.1, H * 0.6), (0.1, 0.2, H)], [0.45, 0.38, 0.3], C["wood2"], seg=8,
                 sub=2, cap=2)]
    for i in range(7):
        a = i / 7 * 2 * math.pi + rnd.uniform(-0.2, 0.2)
        foot = Vector((2.4 * math.cos(a), 2.4 * math.sin(a), -0.8))
        top = Vector((0.3 * math.cos(a), 0.3 * math.sin(a), 2.2))
        mid = (foot + top) / 2 + Vector((math.cos(a), math.sin(a), 0)) * 0.6 + Vector((0, 0, 0.9))
        P.append(gs.tube(f"Root{i}", [top, mid, foot], [0.2, 0.15, 0.1], C["wood"], seg=6, sub=2, cap=2))
    for i in range(6 if v == 0 else 7):
        a = i / 6 * 2 * math.pi
        r = 1.8 if v == 0 else 2.6
        c = Vector((r * math.cos(a), r * math.sin(a), H + rnd.uniform(-0.3, 0.6)))
        P.append(gs.ellipsoid(f"Leaf{i}", c, (1.6, 1.5, 0.9), C["green"] if i % 2 else C["green2"], seg=9, rings=5))
    P.append(gs.ellipsoid("Top", (0, 0, H + 0.9), (1.8, 1.8, 1.0), C["green"], seg=10, rings=5))
    for i in range(3):
        P.append(gs.capsule(f"Pod{i}", (1.0 * math.cos(i * 2), 1.0 * math.sin(i * 2), H - 0.4),
                            (1.0 * math.cos(i * 2), 1.0 * math.sin(i * 2), H - 1.3), 0.06, C["gold"], seg=4, cap=1,
                            sub=1))
    k.finish(S, name, P)


def dam(S, name):
    """Represa modular (W5-4): muro curvo de 14 m de ancho y 11 m de alto con aliviadero central, compuerta móvil
    (pivote arriba al centro), pasarela, luces y franjas de aviso."""
    C = pal()
    prof = [(-1.5, -1.0), (2.8, -1.0), (2.2, 3.0), (1.0, 8.0), (0.9, 11.0), (-1.2, 11.0), (-1.4, 6.0)]
    P = []
    prof_yz = [(y, z) for y, z in prof]
    wall = k.sweep("Wall", [(-y, z) for y, z in prof_yz], [Vector((-7.0, 0, 0)), Vector((-1.6, 0, 0))],
                   [C["concrete"], C["concrete2"]], up=(0, 0, 1))
    wall2 = k.sweep("Wall2", [(-y, z) for y, z in prof_yz], [Vector((1.6, 0, 0)), Vector((7.0, 0, 0))],
                    [C["concrete"], C["concrete2"]], up=(0, 0, 1))
    for w in (wall, wall2):
        gs.bands_by_plane(w, (0, 0, 1), (0, 0, 0), 2.2, 0.1, 1, cut=True)
    P += [wall, wall2]
    spill = k.sweep("Spillway", [(-1.4, -1.0), (2.6, -1.0), (2.6, 0.0), (-1.4, 3.0)], [Vector((-1.6, 0, 0)),
                                                                                       Vector((1.6, 0, 0))],
                    [C["concrete2"]], up=(0, 0, 1))
    P.append(spill)
    P.append(gs.rounded_box("WaterSheet", (0, -0.6, 1.7), (3.2, 0.25, 3.4), 0.1, C["water"], rot=("X", 25)))
    for s in (1, -1):
        P.append(gs.rounded_box(f"Pier{s}", (1.9 * s, 0.0, 6.0), (0.8, 3.2, 12.0), 0.2, [C["gold"], C["navy"]]))
        P.append(gs.ellipsoid(f"Light{s}", (1.9 * s, -1.7, 11.6), 0.3, C["lamp"], seg=8, rings=5))
    beam = gs.rounded_box("Walkway", (0, 0.0, 11.3), (14.4, 2.2, 0.5), 0.15, [C["gold"], C["navy"]])
    gs.bands_by_plane(beam, (1, 0, 0.5), (0, 0, 0), 1.2, 0.5, 1, cut=True)
    P.append(beam)
    P.append(gs.tube("Railing", [(-7.0, -1.0, 12.3), (7.0, -1.0, 12.3)], 0.07, C["navy"], seg=5, cap=1))
    for i in range(8):
        x = -7.0 + 2.0 * i
        P.append(gs.capsule(f"RPost{i}", (x, -1.0, 11.5), (x, -1.0, 12.3), 0.05, C["navy"], seg=4, cap=1, sub=1))
    for x in (-5.0, 5.0):
        P.append(gs.rounded_box(f"Pipe{x}", (x, -1.6, 2.0), (1.0, 0.6, 1.0), 0.3, C["teal"]))
        P.append(gs.ellipsoid(f"PipeHole{x}", (x, -1.95, 2.0), (0.35, 0.05, 0.35), C["black"], seg=8, rings=4))
    gate = [gs.rounded_box("GatePanel", (0, -1.0, 7.0), (3.0, 0.45, 6.0), 0.15, [C["teal"], C["gold"]])]
    gs.paint_faces(gate[0], lambda c: c.z < 4.6, 1)
    for z in (5.0, 7.0, 9.0):
        gate.append(gs.rounded_box(f"GateRib{z}", (0, -1.3, z), (2.8, 0.12, 0.25), 0.05, C["navy"]))
    k.finish(S, name, P, moving=[("Gate", (0, -1.0, 10.0), gate)], view=(0.6, -1, 0.3))


# ---------------------------------------------------------------------------
# Relleno
# ---------------------------------------------------------------------------
def cactus(S, name, v):
    """A: cactus alto de brazos con flores. B: nopal de paletas con tunas."""
    C = pal()
    P = []
    if v == 0:
        P.append(gs.capsule("Trunk", (0, 0, 0), (0, 0, 3.6), 0.38, [C["cactus"]], seg=10, cap=3, sub=2))
        for s, z, h in ((1, 1.4, 1.3), (-1, 2.0, 1.0)):
            P.append(gs.tube(f"Arm{s}", [(0.3 * s, 0, z), (0.9 * s, 0, z + 0.1), (1.0 * s, 0, z + 0.6),
                                         (1.0 * s, 0, z + h)], 0.24, C["cactus"], seg=8, sub=2, cap=3))
        for i, p in enumerate(((0, 0, 3.95), (1.0, 0, 1.4 + 1.55), (-1.0, 0, 2.0 + 1.25))):
            P += k.flower(f"Fl{i}", p, C["pink"], C["yellow"], size=0.16, h=0.0)
    else:
        for i, (x, z, r, rot) in enumerate(((0, 0.55, 0.55, 0), (0.55, 1.3, 0.42, -25), (-0.5, 1.2, 0.4, 30),
                                            (0.2, 1.9, 0.35, 10))):
            P.append(gs.ellipsoid(f"Pad{i}", (x, 0, z), (r, 0.12, r * 1.2), C["cactus"], seg=10, rings=6,
                                  rot=("Y", rot)))
        for i, (x, z) in enumerate(((0.75, 1.8), (-0.75, 1.65), (0.35, 2.3))):
            P.append(gs.ellipsoid(f"Tuna{i}", (x, 0, z), (0.1, 0.1, 0.13), C["pink"], seg=6, rings=4))
    P.append(gs.ellipsoid("Sand", (0, 0, 0.0), (0.9, 0.9, 0.15), C["sand"], seg=10, rings=3))
    k.finish(S, name, P)


def desert_flowers(S, name):
    C = pal()
    P = [gs.ellipsoid("Mound", (0, 0, 0), (1.2, 1.0, 0.25), C["sand"], seg=10, rings=4)]
    cols = [C["orange"], C["pink"], C["gold"], C["teal"]]
    for i in range(8):
        a = i / 8 * 2 * math.pi
        r = 0.4 + 0.3 * (i % 2)
        P += k.flower(f"F{i}", (r * math.cos(a), r * math.sin(a), 0.15), cols[i % 4], C["yellow"] if i % 4 else C["red"],
                      stem=C["green2"], size=0.16, h=0.3 + 0.15 * (i % 3))
    k.finish(S, name, P)


def wood_sign(S, name):
    """Cartel de flechas del Gran Regreso (sin texto): flecha turquesa y dibujo del zoológico."""
    C = pal()

    def icon(c):
        out = [gs.rounded_box("House", c + Vector((-0.15, -0.02, -0.05)), (0.4, 0.04, 0.3), 0.04, C["white"]),
               gs.cone("HouseRoof", c + Vector((-0.15, -0.03, 0.1)), c + Vector((-0.15, -0.03, 0.32)), 0.3, 0.01,
                       C["red"], seg=3, round_tip=False)]
        out.append(gs.ellipsoid("Sun", c + Vector((0.3, -0.03, 0.15)), (0.12, 0.03, 0.12), C["orange"], seg=8, rings=4))
        return out
    P = k.sign("Sign", C["wood2"], C["wood"], icon, w=1.3, bh=0.8, tilt=-5)
    P.append(gs.rounded_box("Arrow", (0.2, -0.05, 1.1), (1.1, 0.08, 0.35), 0.06, C["teal"], rot=("Y", 8)))
    P.append(gs.cone("ArrowTip", (0.75, -0.05, 1.18), (1.05, -0.05, 1.2), 0.3, 0.01, C["teal"], seg=3,
                     round_tip=False))
    k.finish(S, name, P)


def tumbleweed(S, name):
    C = pal()
    rnd = random.Random(4)
    P = []
    for i in range(10):
        a, b = rnd.uniform(0, 6.28), rnd.uniform(-1.2, 1.2)
        n = Vector((math.cos(a) * math.cos(b), math.sin(a) * math.cos(b), math.sin(b)))
        P.append(gs.torus(f"Twig{i}", (0, 0, 0.6), 0.5 + rnd.uniform(-0.08, 0.08), 0.025, C["wood"], axis=tuple(n),
                          seg=12, mseg=3))
    k.finish(S, name, P)


def lanterns(S, name):
    """Guirnalda de farolillos entre dos postes (10 m), para el campamento del cañón."""
    C = pal()
    P = []
    for s in (1, -1):
        P.append(gs.capsule(f"Post{s}", (5.0 * s, 0, 0), (5.0 * s, 0, 4.0), 0.12, C["wood2"], seg=6, cap=1, sub=1))
    rope = [Vector((5.0 * (i / 10 * 2 - 1), 0, 3.9 - 1.0 * math.sin(i / 10 * math.pi))) for i in range(11)]
    P.append(gs.tube("Rope", rope, 0.03, C["rope"], seg=4, cap=1))
    cols = [C["lamp"], E("#3AE1FF", 3.0), E("#FF6FA5", 3.0)]
    for i in range(1, 10):
        p = rope[i] + Vector((0, 0, -0.35))
        P.append(gs.ellipsoid(f"Lantern{i}", p, (0.22, 0.22, 0.3), cols[i % 3], seg=8, rings=5))
        P.append(gs.rounded_cylinder(f"Cap{i}", p + Vector((0, 0, 0.3)), 0.12, 0.08, 0.02, C["navy"], seg=6, steps=1))
    k.finish(S, name, P, view=(0.4, -1, 0.25))


PROPS = [("NaturalArch", natural_arch), ("SlideTower", slide_tower),
         ("Slide_Straight", lambda S, n: slide(S, n, "straight")), ("Slide_Curve45", lambda S, n: slide(S, n, "curve")),
         ("Slide_Drop", lambda S, n: slide(S, n, "drop")), ("Slide_Ramp", lambda S, n: slide(S, n, "ramp")),
         ("BrokenBridge", broken_bridge), ("Ramp_A", lambda S, n: ramp(S, n, 0)), ("Ramp_B", lambda S, n: ramp(S, n, 1)),
         ("Mangrove_A", lambda S, n: mangrove(S, n, 0)), ("Mangrove_B", lambda S, n: mangrove(S, n, 1)), ("Dam", dam),
         ("Cactus_A", lambda S, n: cactus(S, n, 0)), ("Cactus_B", lambda S, n: cactus(S, n, 1)),
         ("DesertFlowers", desert_flowers), ("Sign", wood_sign), ("Tumbleweed", tumbleweed), ("Lanterns", lanterns)] \
    + [(f"CanyonRock_{c}", (lambda v: lambda S, n: canyon_rock(S, n, v))(i)) for i, c in enumerate("ABC")]

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    k.run_props(W, PROPS, only=set(argv) or None, cols=7)
