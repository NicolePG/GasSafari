"""Escenario W1 · Fuga del Zoológico (ciudad de noche). docs/09 §3 fila "Escenario W1", paleta de docs/04:
azul #1B2A4A, neón #FF4FA3 / #3AE1FF, farolas ámbar; acento complementario naranja.
blender --background --python env_w1.py [-- Bench_A Ferry ...]
Salida: Tools/Export/Environment/W1/W1_<Nombre>.fbx y previews/Env_W1_sheet.png"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
import env_kit as k
from gs_common import Vector
from env_kit import M, E

W = "W1"


def pal():
    return dict(navy=M("#1B2A4A"), navy2=M("#2E4272"), iron=M("#26304F", 0.5), pink=E("#FF4FA3"),
                cyan=E("#3AE1FF"), amber=E("#FFB238"), stone=M("#C9C3D8"), stone2=M("#8F88A8"), brick=M("#9A4E63"),
                concrete=M("#A7AFBF"), gold=M("#F5C431", 0.35), wood=M("#C47A3A"), wood2=M("#7A4A26"),
                orange=M("#FF8C1A"), red=M("#E2394B"), white=M("#F4F1EA"), green=M("#3FA34D"), leaf=M("#5BC85B"),
                water=M("#7FDBFF", 0.1), glass=M("#9FE8FF", 0.1), algae=M("#2E5E66"), yellow=M("#FFD23F"),
                black=M("#22232B"), card=M("#D9A066"))


# ---------------------------------------------------------------------------
# Parque del zoológico
# ---------------------------------------------------------------------------
def bench(S, name, v):
    C = pal()
    P = []
    if v == 0:
        for i, y in enumerate((-0.27, -0.12, 0.03, 0.18)):
            P.append(gs.rounded_box(f"Seat{i}", (0, y, 0.48 - 0.01 * i), (2.3, 0.13, 0.07), 0.03, C["wood"],
                                    bulge=0.0))
        for i, z in enumerate((0.72, 0.88, 1.04)):
            P.append(gs.rounded_box(f"Back{i}", (0, 0.32 + 0.04 * i, z), (2.3, 0.07, 0.13), 0.03, C["wood"],
                                    rot=("X", -14)))
        for s in (1, -1):
            x = 1.0 * s
            P.append(gs.tube(f"Leg{s}", [(x, -0.35, 0.0), (x, -0.3, 0.42), (x, 0.25, 0.44), (x, 0.38, 1.1)],
                             0.05, C["iron"], seg=6, sub=3, cap=1))
            P.append(gs.tube(f"Curl{s}", gs.arc_points((x, -0.38, 0.55), 0.13, -90, 220, 8, plane=("Y", "Z"),
                                                      depth_axis="X"), 0.035, C["iron"], seg=5, cap=1))
            P.append(gs.tube(f"Rear{s}", [(x, 0.25, 0.44), (x, 0.32, 0.0)], 0.045, C["iron"], seg=6, cap=1))
            P.append(gs.ellipsoid(f"Knob{s}", (x, 0.39, 1.14), 0.07, C["gold"], seg=8, rings=5))
    else:
        for s in (1, -1):
            P.append(gs.rounded_box(f"Planter{s}", (1.55 * s, 0, 0.4), (0.8, 0.8, 0.8), 0.1, C["concrete"],
                                    taper=(1.1, 1.1)))
            P += k.bush(f"Bush{s}", (1.55 * s, 0, 0.75), 0.42, [C["green"], C["leaf"]], seed=3 + s, n=4)
            for i in range(3):
                a = i * 2.1 + s
                P += k.flower(f"Fl{s}{i}", (1.55 * s + 0.3 * math.cos(a), 0.3 * math.sin(a), 1.05),
                              C["red"] if i % 2 else C["yellow"], C["orange"], size=0.14, h=0.12)
        for i, y in enumerate((-0.2, 0.0, 0.2)):
            P.append(gs.rounded_box(f"Seat{i}", (0, y, 0.5), (2.3, 0.18, 0.08), 0.03, C["wood"]))
        P.append(gs.rounded_box("Beam", (0, 0, 0.4), (2.3, 0.3, 0.1), 0.03, C["navy2"]))
    k.finish(S, name, P)


def statue(S, name):
    """Estatua de la rotonda: foca equilibrando una pelota sobre pedestal (W1-1)."""
    C = pal()
    P = [gs.rounded_box("Step", (0, 0, 0.2), (3.4, 3.4, 0.4), 0.12, C["stone2"]),
         gs.rounded_box("Pedestal", (0, 0, 1.2), (2.4, 2.4, 1.6), 0.2, C["stone2"], taper=(0.85, 0.85), bulge=0.05),
         gs.rounded_box("Cornice", (0, 0, 2.05), (2.5, 2.5, 0.25), 0.1, C["stone"]),
         gs.rounded_box("Plaque", (0, -1.12, 1.25), (1.0, 0.06, 0.55), 0.06, C["gold"], rot=("X", -5))]
    body = gs.tube("Seal", [(0, 0.45, 2.3), (0, 0.15, 2.9), (0, -0.05, 3.7), (0, -0.12, 4.25)], [0.62, 0.66, 0.46, 0.33],
                   C["stone"], seg=14, sub=4)
    P.append(body)
    P.append(gs.ellipsoid("Head", (0, -0.17, 4.5), (0.36, 0.4, 0.33), C["stone"], seg=14, rings=8))
    P.append(gs.ellipsoid("Snout", (0, -0.5, 4.48), (0.2, 0.22, 0.15), C["stone"], seg=10, rings=6, rot=("X", 25)))
    P.append(gs.ellipsoid("Nose", (0, -0.66, 4.58), 0.07, C["stone2"], seg=8, rings=4))
    for s in (1, -1):
        P.append(gs.tube(f"Eye{s}", gs.arc_points((0.16 * s, -0.47, 4.66), 0.06, 20, 160, 5), 0.018, C["stone2"],
                         seg=5, cap=1))
        P.append(k.leaf(f"Flipper{s}", [(0.45 * s, -0.1, 3.1), (0.85 * s, -0.4, 2.95), (1.05 * s, -0.55, 2.6)],
                        0.22, C["stone"], droop_up=(0, -1, 0)))
        P.append(k.leaf(f"Tail{s}", [(0.1 * s, 0.75, 2.25), (0.45 * s, 1.0, 2.2), (0.75 * s, 1.15, 2.25)], 0.2,
                        C["stone"]))
    ball = gs.ellipsoid("Ball", (0, -0.62, 5.2), 0.48, [C["orange"], C["white"]], seg=14, rings=8)
    gs.bands_by_plane(ball, (1, 0, 0.3), (0, -0.62, 5.2), 0.32, 0.5, 1, cut=True)
    P.append(ball)
    k.finish(S, name, P)


def fountain_tiered(S, name):
    C = pal()
    P = [gs.lathe("Basin", [(0.0, 0.0), (2.6, 0.0), (2.75, 0.15), (2.75, 0.6), (2.55, 0.75), (2.35, 0.65),
                            (2.35, 0.3), (0.0, 0.3)], C["stone"], seg=28),
         gs.rounded_cylinder("Pool", (0, 0, 0.45), 2.36, 0.12, 0.03, C["water"], seg=28, steps=1),
         gs.lathe("Column", [(0.0, 0.3), (0.55, 0.3), (0.32, 0.8), (0.24, 1.5), (0.4, 1.65), (0.0, 1.7)], C["stone2"],
                  seg=12),
         gs.lathe("Bowl1", [(0.0, 1.6), (0.35, 1.6), (1.35, 1.95), (1.45, 2.08), (1.3, 2.1), (0.0, 1.9)], C["stone"],
                  seg=20),
         gs.lathe("Fall1", [(1.38, 2.05), (1.5, 1.7), (1.62, 1.0), (1.7, 0.5), (1.6, 0.5), (1.5, 1.0), (1.38, 1.7),
                            (1.3, 2.0), (1.38, 2.05)], C["water"], seg=20),
         gs.lathe("Column2", [(0.0, 1.9), (0.22, 1.9), (0.15, 2.6), (0.25, 2.75), (0.0, 2.8)], C["stone2"], seg=10),
         gs.lathe("Bowl2", [(0.0, 2.7), (0.2, 2.7), (0.8, 2.95), (0.86, 3.05), (0.75, 3.07), (0.0, 2.9)], C["stone"],
                  seg=16),
         gs.lathe("Fall2", [(0.8, 3.0), (0.9, 2.7), (0.98, 2.15), (0.9, 2.15), (0.82, 2.7), (0.75, 2.98), (0.8, 3.0)],
                  C["water"], seg=16),
         gs.lathe("Jet", [(0.0, 2.95), (0.12, 2.95), (0.08, 3.5), (0.22, 3.75), (0.0, 3.95)], C["water"], seg=10)]
    rnd = random.Random(4)
    for i in range(6):
        a = i / 6 * 2 * math.pi
        P.append(gs.ellipsoid(f"Drop{i}", (0.3 * math.cos(a), 0.3 * math.sin(a), 3.75 + rnd.uniform(0, 0.2)), 0.07,
                              C["water"], seg=6, rings=4))
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.4
        P.append(gs.ellipsoid(f"Fish{i}", (2.5 * math.cos(a), 2.5 * math.sin(a), 0.82), (0.22, 0.12, 0.12),
                              C["gold"], seg=8, rings=5, rot=("Z", math.degrees(a) + 90)))
    k.finish(S, name, P)


def fountain_launch(S, name):
    """Fuente central que lanza la balsa (H27, W1-1): pileta ancha, flor-boquilla y domo de agua."""
    C = pal()
    P = [gs.lathe("Basin", [(0.0, -0.4), (5.0, -0.4), (5.2, 0.0), (5.2, 0.7), (4.9, 0.9), (4.6, 0.7), (4.6, 0.0),
                            (0.0, 0.0)], C["stone"], seg=32),
         gs.lathe("BasinTrim", [(5.22, 0.45), (5.28, 0.5), (5.22, 0.55)], C["pink"], seg=32),
         gs.rounded_cylinder("Pool", (0, 0, 0.2), 4.62, 0.2, 0.05, C["water"], seg=32, steps=1),
         gs.rounded_cylinder("Nozzle", (0, 0, 0.5), 1.0, 0.8, 0.2, C["stone2"], seg=16, steps=2),
         gs.ellipsoid("Dome", (0, 0, 1.1), (1.9, 1.9, 1.1), C["water"], seg=18, rings=8)]
    for i in range(8):
        a = i / 8 * 2 * math.pi
        d = Vector((math.cos(a), math.sin(a), 0))
        P.append(k.leaf(f"Petal{i}", [d * 0.8 + Vector((0, 0, 0.6)), d * 1.6 + Vector((0, 0, 1.0)),
                                      d * 2.2 + Vector((0, 0, 1.6))], 0.45, C["pink"] if i % 2 else C["cyan"]))
    for i in range(6):
        a = i / 6 * 2 * math.pi + 0.3
        base = Vector((4.4 * math.cos(a), 4.4 * math.sin(a), 0.8))
        tip = Vector((2.6 * math.cos(a), 2.6 * math.sin(a), 0.6))
        P.append(gs.tube(f"Arc{i}", [base, (base + tip) / 2 + Vector((0, 0, 1.4)), tip], [0.12, 0.1, 0.06],
                         C["water"], seg=6, sub=3, cap=1))
        P.append(gs.ellipsoid(f"Spout{i}", base, (0.25, 0.25, 0.18), C["gold"], seg=8, rings=4))
    k.finish(S, name, P)


def fence(S, name):
    """Reja modular de 4 m (pivote en el centro de la base)."""
    C = pal()
    P = []
    for s in (1, -1):
        P.append(gs.rounded_box(f"Post{s}", (2.0 * s, 0, 0.95), (0.32, 0.32, 1.9), 0.06, C["navy2"],
                                taper=(0.85, 0.85)))
        P.append(gs.ellipsoid(f"Ball{s}", (2.0 * s, 0, 2.05), 0.2, C["gold"], seg=10, rings=6))
    for z in (0.3, 1.45):
        P.append(gs.capsule(f"Rail{z}", (-1.9, 0, z), (1.9, 0, z), 0.05, C["iron"], seg=6, cap=1, sub=1))
    for i in range(9):
        x = -1.6 + 0.4 * i
        top = 1.65 + 0.15 * math.sin(i / 8 * math.pi)
        P.append(gs.capsule(f"Bar{i}", (x, 0, 0.15), (x, 0, top), 0.035, C["iron"], seg=5, cap=1, sub=1))
        P.append(gs.cone(f"Spear{i}", (x, 0, top), (x, 0, top + 0.22), 0.07, 0.005, C["gold"], seg=5))
    for i in range(4):
        x = -1.4 + 0.8 * i + 0.2
        P.append(gs.tube(f"Curl{i}", gs.arc_points((x, 0, 1.2), 0.14, 0, 300, 8), 0.025, C["iron"], seg=5, cap=1))
    k.finish(S, name, P, view=(0.4, -1, 0.3))


def zoo_gate(S, name):
    """Hito W1: portón del zoológico con letrero de neón (sin texto) y puertas móviles."""
    C = pal()
    P = []
    for s in (1, -1):
        x = 7.0 * s
        P.append(gs.rounded_box(f"Pillar{s}", (x, 0, 4.0), (2.2, 2.2, 8.0), 0.3, C["stone"], taper=(0.82, 0.82),
                                bulge=0.07))
        P.append(gs.rounded_box(f"PillarBase{s}", (x, 0, 0.5), (2.8, 2.8, 1.0), 0.2, C["stone2"]))
        P.append(gs.rounded_box(f"PillarCap{s}", (x, 0, 8.2), (2.4, 2.4, 0.6), 0.2, C["stone2"]))
        P.append(gs.ellipsoid(f"Globe{s}", (x, 0, 9.2), 0.8, C["gold"], seg=10, rings=6))
        P.append(gs.cone(f"Spike{s}", (x, 0, 9.9), (x, 0, 10.8), 0.25, 0.02, C["gold"], seg=6))
        P.append(gs.ellipsoid(f"Lamp{s}", (x - 1.25 * s, -0.4, 6.2), 0.3, C["amber"], seg=8, rings=5))
    arch = [Vector((x, 0, 8.2 + 2.8 * math.cos(x / 7.0 * math.pi / 2) ** 0.7)) for x in [-6.4 + 12.8 * i / 10
                                                                                         for i in range(11)]]
    P.append(k.sweep("Arch", [(-0.55, -0.5), (0.55, -0.5), (0.55, 0.5), (-0.55, 0.5)], arch, C["navy2"],
                     up=(0, 1, 0)))
    board_c = Vector((0, -0.3, 12.6))
    P.append(gs.rounded_box("Board", board_c, (9.5, 0.5, 3.4), 0.4, C["navy"], bulge=0.04))
    # neón: marco, cara de león con melena en zigzag, estrellas y huellas
    frame = [board_c + Vector((x, -0.3, z)) for x, z in ((-4.4, -1.4), (4.4, -1.4), (4.4, 1.4), (-4.4, 1.4),
                                                         (-4.4, -1.4))]
    P.append(gs.tube("NeonFrame", frame, 0.07, C["pink"], seg=5, cap=1))
    fc = board_c + Vector((0, -0.3, 0))
    mane = [fc + Vector(((1.15 + 0.22 * (i % 2)) * math.cos(a), 0, (1.15 + 0.22 * (i % 2)) * math.sin(a)))
            for i, a in enumerate([j / 16 * 2 * math.pi for j in range(17)])]
    P.append(gs.tube("NeonMane", mane, 0.07, C["amber"], seg=5, cap=1))
    P.append(gs.tube("NeonFace", gs.arc_points(fc, 0.8, 0, 360, 17), 0.06, C["cyan"], seg=5, cap=1))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"NeonEye{s}", fc + Vector((0.3 * s, 0, 0.2)), 0.1, C["cyan"], seg=6, rings=3))
        star = []
        sc = fc + Vector((3.0 * s, 0, 0.1))
        for j in range(11):
            r = 0.75 if j % 2 == 0 else 0.32
            a = math.pi / 2 + j / 10 * 2 * math.pi
            star.append(sc + Vector((r * math.cos(a), 0, r * math.sin(a))))
        P.append(gs.tube(f"NeonStar{s}", star, 0.06, C["pink"] if s == 1 else C["cyan"], seg=5, cap=1))
    P.append(gs.tube("NeonSmile", gs.arc_points(fc + Vector((0, 0, -0.1)), 0.35, 200, 340, 7), 0.05, C["cyan"],
                     seg=5, cap=1))
    P.append(gs.ellipsoid("NeonNose", fc + Vector((0, 0, -0.05)), (0.12, 0.05, 0.09), C["pink"], seg=6, rings=4))
    doors = []
    for s, nm in ((1, "Door_L"), (-1, "Door_R")):
        D = []
        hx = 5.9 * s
        for i in range(6):
            x = hx - s * (0.5 + 0.9 * i)
            top = 4.6 + 0.7 * math.sin((i + 0.5) / 6 * math.pi / 2 + (0 if s == 1 else 0))
            D.append(gs.capsule(f"{nm}Bar{i}", (x, 0, 0.3), (x, 0, top), 0.06, C["iron"], seg=5, cap=1, sub=1))
            D.append(gs.cone(f"{nm}Tip{i}", (x, 0, top), (x, 0, top + 0.35), 0.11, 0.01, C["gold"], seg=5))
        for z in (0.4, 2.4):
            D.append(gs.capsule(f"{nm}Rail{z}", (hx, 0, z), (hx - 5.8 * s, 0, z), 0.08, C["iron"], seg=6, cap=1,
                                sub=1))
        D.append(gs.tube(f"{nm}Ring", gs.arc_points((hx - 2.9 * s, -0.05, 3.3), 0.6, 0, 360, 13), 0.06, C["gold"],
                         seg=5, cap=1))
        doors.append((nm, (hx, 0, 0), D))
    k.finish(S, name, P, moving=doors, view=(0.55, -1, 0.3), margin=1.05)


def ferris_wheel(S, name):
    """Hito W1: noria del puerto. Rueda (pivote en el eje) y 8 cabinas móviles (pivote en el gancho)."""
    C = pal()
    H, R = 11.0, 8.5
    P = [gs.rounded_box("Base", (0, 0, 0.4), (12.0, 4.6, 0.8), 0.3, C["navy2"])]
    for y in (-1.7, 1.7):
        for s in (1, -1):
            P.append(gs.capsule(f"Leg{y}{s}", (5.2 * s, y, 0.6), (0.2 * s, y * 0.7, H), 0.32, C["white"], r1=0.22,
                                seg=7, cap=1, sub=1))
        P.append(gs.capsule(f"Brace{y}", (-3.2, y * 0.9, 4.0), (3.2, y * 0.9, 4.0), 0.15, C["white"], seg=6, cap=1,
                            sub=1))
    P.append(gs.rounded_box("Booth", (4.0, -1.6, 1.6), (1.6, 1.2, 1.6), 0.2, C["orange"]))
    P.append(gs.rounded_box("BoothRoof", (4.0, -1.6, 2.6), (2.0, 1.6, 0.35), 0.15, C["pink"]))
    hub = Vector((0, 0, H))
    Wh = [gs.rounded_cylinder("Hub", hub, 0.7, 2.6, 0.2, C["gold"], axis=(0, 1, 0), seg=12, steps=1)]
    for y, m in ((-0.9, C["pink"]), (0.9, C["cyan"])):
        Wh.append(gs.torus(f"Rim{y}", hub + Vector((0, y, 0)), R, 0.18, m, axis=(0, 1, 0), seg=28, mseg=5))
        for i in range(8):
            a = i / 8 * 2 * math.pi + math.pi / 8
            Wh.append(gs.capsule(f"Spoke{y}{i}", hub + Vector((0, y * 0.9, 0)),
                                 hub + Vector((R * math.cos(a), y, R * math.sin(a))), 0.08, C["white"], seg=4, cap=1,
                                 sub=1))
    moving = [("Wheel", hub, Wh)]
    cols = [C["cyan"], C["orange"], C["pink"], C["yellow"]]
    for i in range(8):
        a = i / 8 * 2 * math.pi
        hook = hub + Vector((R * math.cos(a), 0, R * math.sin(a)))
        Wh.append(gs.capsule(f"Axle{i}", hook + Vector((0, -0.95, 0)), hook + Vector((0, 0.95, 0)), 0.07, C["iron"],
                             seg=4, cap=1, sub=1))
        G = [gs.capsule(f"Hanger{i}", hook, hook + Vector((0, 0, -0.9)), 0.06, C["iron"], seg=4, cap=1, sub=1),
             gs.rounded_box(f"Cabin{i}", hook + Vector((0, 0, -1.7)), (1.3, 1.3, 1.1), 0.3, cols[i % 4], segments=1,
                            bulge=0.12),
             gs.rounded_box(f"Roof{i}", hook + Vector((0, 0, -1.0)), (1.4, 1.4, 0.25), 0.1, C["white"], segments=1)]
        moving.append((f"Gondola_{i}", hook, G))
    k.finish(S, name, P, moving=moving, view=(0.5, -1, 0.25), margin=1.05)


# ---------------------------------------------------------------------------
# Alcantarillas y canales (modulares)
# ---------------------------------------------------------------------------
def tunnel_profile(n=14):
    outer = [(7.0 * math.cos(math.radians(180 - 180 * i / n)), 0.6 + 7.0 * math.sin(math.radians(180 - 180 * i / n)))
             for i in range(n + 1)]
    inner = [(6.3 * math.cos(math.radians(180 * i / n)), 0.6 + 6.3 * math.sin(math.radians(180 * i / n)))
             for i in range(n + 1)]
    return ([(-7.0, -1.4)] + outer + [(7.0, -1.4), (5.0, -1.4), (5.0, 0.6)] + inner[:1] + inner[1:]
            + [(-5.0, 0.6), (-5.0, -1.4)])


def tunnel(S, name, curve=False):
    """Túnel de alcantarilla modular: recto de 8 m o curva de 45° (radio 20 m). Cauce interior de 10 m."""
    C = pal()
    path = k.arc_path((-20, 0, 0), 20, 0, 45, 8) if curve else [(0, -4, 0), (0, 4, 0)]
    if curve:
        path = [Vector((p.x, p.y, 0)) for p in path]
    shell = k.sweep("Shell", tunnel_profile(), path, [C["brick"], C["concrete"]])
    gs.paint_faces(shell, lambda c: c.z < 0.7, 1)
    P = [shell]
    off = lambda d, z: [Vector(p) + (Vector((1, 0, 0)) if not curve else (Vector(p) - Vector((-20, 0, 0))).normalized())
                        * d + Vector((0, 0, z)) for p in path]
    for d, z, m, r in ((5.95, 1.9, C["navy2"], 0.22), (-5.95, 1.9, C["navy2"], 0.22), (4.4, 4.6, C["pink"], 0.06),
                       (-4.4, 4.6, C["cyan"], 0.06)):
        pts = off(d, z)
        P.append(gs.tube(f"Run{d}", pts, r, m, seg=8 if r > 0.1 else 5, cap=1, sub=2 if curve else 1))
    mids = [Vector(p) for p in (path if curve else [(0, -2, 0), (0, 2, 0)])]
    for i, c in enumerate(mids[::3] if curve else mids):
        P.append(gs.rounded_box(f"Grate{i}", c + Vector((0, 0, 6.75)), (1.6, 1.2, 0.15), 0.05, C["cyan"]))
        for j in range(3):
            P.append(gs.rounded_box(f"GrateBar{i}{j}", c + Vector((-0.5 + 0.5 * j, 0, 6.66)), (0.1, 1.3, 0.12), 0.03,
                                    C["iron"]))
    if not curve:
        for y in (-3.85, 3.85):
            P.append(gs.tube(f"Rib{y}", gs.arc_points((0, y, 0.6), 6.65, 0, 180, 15), 0.3, C["stone2"], seg=6,
                             cap=1))
    k.finish(S, name, P, view=(0.55, -0.8, 0.35))


def canal_profile(half=5.0, wall=0.8):
    o = half + wall
    return [(-o - 0.1, 1.0), (-o - 0.1, 1.25), (-half + 0.15, 1.25), (-half + 0.15, 1.0), (-half, 1.0),
            (-half, -1.6), (half, -1.6), (half, 1.0), (half - 0.15, 1.0), (half - 0.15, 1.25), (o + 0.1, 1.25),
            (o + 0.1, 1.0), (o, 1.0), (o, -2.1), (-o, -2.1), (-o, 1.0)]


def canal(S, name, kind):
    """Canal de piedra modular (pivote en el centro del cauce, agua en z = 0). Recto 10 m, curva 90° de radio 15 m,
    paso estrecho de 5 m, transición 10 → 5 m y rampa con bajada de 3 m."""
    C = pal()
    if kind == "straight":
        prof, path = canal_profile(), [(0, -5, 0), (0, 5, 0)]
    elif kind == "curve":
        prof, path = canal_profile(), [Vector((p.x, p.y, 0)) for p in k.arc_path((-15, 0, 0), 15, 0, 90, 13)]
    elif kind == "narrow":
        prof, path = canal_profile(2.5), [(0, -5, 0), (0, 5, 0)]
    elif kind == "taper":
        a, b = canal_profile(5.0), canal_profile(2.5)
        prof = lambda u: [(pa[0] + (pb[0] - pa[0]) * u, pa[1]) for pa, pb in zip(a, b)]
        path = [(0, -5 + 10 * i / 6, 0) for i in range(7)]
    else:
        prof = canal_profile()
        path = [(0, -5 + 10 * i / 8, -3.0 * (0.5 - 0.5 * math.cos(math.pi * i / 8))) for i in range(9)]
    o = k.sweep("Canal", prof, path, [M("#6F7FA6"), M("#E0D9EE"), C["algae"]])
    if kind != "ramp":
        gs.bands_by_plane(o, (0, 0, 1), (0, 0, -0.35), 20.0, 0.0225, 2, cut=True)
    gs.paint_faces(o, lambda c: c.z - _floor(c, kind) > 1.05, 1)
    P = [o]
    if kind != "taper":
        for i, p in enumerate(path[1:-1:3] if len(path) > 2 else [Vector((0, 0, 0))]):
            p = Vector(p)
            t = (Vector(path[-1]) - Vector(path[0])).normalized() if len(path) == 2 else None
            hw = 2.5 if kind == "narrow" else 5.0
            side = Vector((1, 0, 0)) if kind != "curve" else (p - Vector((-15, 0, 0))).normalized()
            for s in (1, -1):
                c = p + side * s * (hw - 0.02) + Vector((0, 0, 0.45 + (0.0 if kind != "ramp" else 0)))
                P.append(gs.torus(f"Drain{i}{s}", c, 0.32, 0.08, M("#E0D9EE"), axis=tuple(side), seg=12, mseg=4))
                P.append(gs.ellipsoid(f"Hole{i}{s}", c, (0.26, 0.26, 0.26), C["black"], seg=8, rings=5))
    if kind == "straight":
        for j in range(6):
            P.append(gs.capsule(f"Rung{j}", (-4.92, 2.3, -1.0 + 0.4 * j), (-4.92, 3.0, -1.0 + 0.4 * j), 0.035,
                                C["gold"], seg=4, cap=1, sub=1))
        for y in (2.3, 3.0):
            P.append(gs.capsule(f"Rail{y}", (-4.92, y, -1.2), (-4.92, y, 1.5), 0.045, C["gold"], seg=4, cap=1, sub=1))
    if kind in ("straight", "narrow"):
        hw = 5.0 if kind == "straight" else 2.5
        for s in (1, -1):
            P.append(gs.torus(f"Ring{s}", (s * (hw - 0.05), 0, 0.5), 0.25, 0.04, C["gold"], axis=(1, 0, 0), seg=10,
                              mseg=4))
            P.append(gs.rounded_box(f"RingPlate{s}", (s * (hw + 0.02), 0, 0.8), (0.06, 0.3, 0.3), 0.03, C["iron"]))
    k.finish(S, name, P, view=(0.7, -0.9, 0.7))


def _floor(c, kind):
    if kind != "ramp":
        return 0.0
    u = (c.y + 5) / 10
    return -3.0 * (0.5 - 0.5 * math.cos(math.pi * max(0.0, min(1.0, u))))


def sluice_gate(S, name):
    """Compuerta (H03) para un cauce de 10 m: torres, viga con franjas y hoja móvil (pivote abajo al centro)."""
    C = pal()
    P = []
    for s in (1, -1):
        x = 5.9 * s
        t = gs.rounded_box(f"Tower{s}", (x, 0, 2.6), (1.6, 1.8, 6.8), 0.25, [C["concrete"], C["black"]],
                           taper=(0.88, 0.88))
        gs.paint_faces(t, lambda c: 0.0 < c.z < 0.8, 1)
        P.append(t)
        P.append(gs.ellipsoid(f"Lamp{s}", (x, 0, 6.6), 0.35, C["amber"], seg=10, rings=6))
        P.append(gs.rounded_cylinder(f"LampCage{s}", (x, 0, 6.35), 0.45, 0.12, 0.04, C["iron"], seg=10, steps=1))
        gear = gs.torus(f"Gear{s}", (x, -0.95, 4.4), 0.55, 0.12, C["gold"], axis=(0, 1, 0), seg=16, mseg=4)
        P.append(gear)
        for j in range(8):
            a = j / 8 * 2 * math.pi
            P.append(gs.rounded_box(f"Tooth{s}{j}", (x + 0.7 * math.cos(a), -0.95, 4.4 + 0.7 * math.sin(a)),
                                    (0.18, 0.18, 0.18), 0.04, C["gold"], segments=1))
        P.append(gs.capsule(f"Guide{s}", (x - 0.95 * s, -0.35, -1.0), (x - 0.95 * s, -0.35, 5.6), 0.1, C["iron"],
                            seg=6, cap=1, sub=1))
    beam = gs.rounded_box("Beam", (0, 0, 5.9), (13.4, 1.3, 1.0), 0.25, [C["yellow"], C["black"]])
    gs.bands_by_plane(beam, (1, 0, 0.6), (0, 0, 0), 0.9, 0.5, 1, cut=True)
    P.append(beam)
    G = [gs.rounded_box("Panel", (0, -0.35, 1.5), (10.2, 0.45, 4.2), 0.15, [C["navy2"], C["orange"]], bulge=0.03)]
    gs.paint_faces(G[0], lambda c: c.z > 3.2, 1)
    for x in (-3.4, 0.0, 3.4):
        G.append(gs.rounded_box(f"Rib{x}", (x, -0.62, 1.4), (0.3, 0.12, 3.9), 0.05, C["iron"]))
    for i in range(6):
        G.append(gs.ellipsoid(f"Rivet{i}", (-4.3 + 1.72 * i, -0.6, 3.4), 0.08, C["gold"], seg=6, rings=3))
    k.finish(S, name, P, moving=[("Gate", (0, -0.35, -0.6), G)], view=(0.55, -1, 0.35))


# ---------------------------------------------------------------------------
# Río y puerto
# ---------------------------------------------------------------------------
def boat(S, name):
    """Lancha panzona (H04 deja ola). Hélice móvil."""
    C = pal()
    P = [k.hull("Hull", 5.8, 2.6, 1.3, [C["white"], C["orange"], C["navy"]], stripe=(0.1, 0.35, 1),
                bottom=(-0.25, 2))]
    P.append(gs.rounded_box("Deck", (0, 0.4, 0.66), (2.1, 3.6, 0.12), 0.05, C["wood"]))
    P.append(gs.rounded_box("Windshield", (0, -0.75, 1.05), (1.9, 0.12, 0.6), 0.08, C["glass"], rot=("X", 30)))
    P.append(gs.rounded_box("Frame", (0, -0.7, 0.8), (2.0, 0.3, 0.2), 0.08, C["navy2"]))
    P.append(gs.rounded_box("Seat", (0, 0.7, 0.95), (1.6, 0.6, 0.5), 0.15, C["red"], bulge=0.1))
    P.append(gs.torus("Wheel", (0.4, -0.35, 1.1), 0.2, 0.035, C["black"], axis=(0, 0.6, 1), seg=12, mseg=4))
    P.append(gs.rounded_box("Motor", (0, 2.95, 0.75), (0.6, 0.6, 1.0), 0.2, C["red"], bulge=0.1))
    P.append(gs.rounded_box("MotorCap", (0, 2.95, 1.3), (0.62, 0.62, 0.2), 0.08, C["white"]))
    P.append(gs.capsule("Shaft", (0, 3.0, 0.3), (0, 3.05, -0.55), 0.08, C["iron"], seg=6, cap=1, sub=1))
    for s in (1, -1):
        P.append(gs.capsule(f"Neon{s}", (1.27 * s, -1.5, 0.5), (1.33 * s, 2.2, 0.5), 0.04, C["cyan"], seg=4, cap=1,
                            sub=1))
    prop = [gs.rounded_cylinder("Hub", (0, 3.2, -0.55), 0.09, 0.2, 0.04, C["gold"], axis=(0, 1, 0), seg=8)]
    for j in range(3):
        a = j / 3 * 2 * math.pi
        prop.append(gs.ellipsoid(f"Blade{j}", (0.2 * math.cos(a), 3.22, -0.55 + 0.2 * math.sin(a)),
                                 (0.18, 0.03, 0.09), C["gold"], seg=8, rings=4,
                                 rot=[("Y", -math.degrees(a)), ("Z", 20)]))
    k.finish(S, name, P, moving=[("Propeller", (0, 3.2, -0.55), prop)])


def ferry(S, name):
    """Ferry (H28): casco panzón, dos cubiertas con ventanas ámbar, chimenea, bandera y hélice móviles."""
    C = pal()
    P = [k.hull("Hull", 16.0, 6.4, 3.0, [C["navy2"], C["white"], C["red"]], stripe=(0.9, 1.4, 1), bottom=(-0.4, 2),
                belly=0.1)]
    P.append(gs.rounded_box("Deck", (0, 0.6, 1.6), (5.6, 12.0, 0.25), 0.1, C["wood"]))
    P.append(gs.rounded_box("Cabin1", (0, 1.0, 2.8), (5.0, 9.4, 2.2), 0.45, C["white"], bulge=0.04))
    P.append(gs.rounded_box("Cabin2", (0, 1.8, 4.6), (4.2, 6.0, 1.6), 0.4, C["navy"], bulge=0.04))
    P.append(gs.rounded_box("Bridge", (0, -1.6, 4.9), (3.4, 2.0, 1.6), 0.4, C["white"]))
    P.append(gs.rounded_box("BridgeWin", (0, -2.55, 5.1), (2.8, 0.15, 0.6), 0.1, C["cyan"]))
    for s in (1, -1):
        P.append(gs.rounded_box(f"Windows{s}", (2.52 * s, 1.0, 3.0), (0.1, 8.4, 0.7), 0.12, C["amber"]))
        P.append(gs.rounded_box(f"Windows2{s}", (2.12 * s, 1.8, 4.7), (0.1, 5.0, 0.5), 0.1, C["amber"]))
        for y in (-3.0, 4.6):
            P.append(gs.torus(f"Ring{s}{y}", (3.2 * s, y, 1.25), 0.35, 0.1, C["orange"], axis=(1, 0, 0), seg=12,
                              mseg=5))
    funnel = gs.lathe("Funnel", [(0.0, 5.3), (0.85, 5.3), (0.95, 6.5), (1.05, 7.4), (0.0, 7.4)],
                      [C["orange"], C["white"]], loc=(0, 3.6, 0), seg=14)
    gs.bands_by_plane(funnel, (0, 0, 1), (0, 0, 6.3), 3.0, 0.15, 1, cut=True)
    P.append(funnel)
    P.append(gs.rounded_cylinder("FunnelTop", (0, 3.6, 7.45), 1.06, 0.2, 0.06, C["black"], seg=14, steps=1))
    P.append(gs.capsule("FlagPole", (0, 7.3, 1.7), (0, 7.3, 5.2), 0.06, C["white"], seg=5, cap=1, sub=1))
    P.append(gs.capsule("Radar", (0, -1.6, 5.7), (0, -1.6, 6.6), 0.05, C["white"], seg=5, cap=1, sub=1))
    P.append(gs.ellipsoid("RadarDish", (0, -1.6, 6.7), (0.5, 0.15, 0.15), C["pink"], seg=8, rings=4))
    flag = k.flag("Flag", (0, 7.3, 4.8), 1.6, 0.9, [C["pink"], C["cyan"]], stripes=3)
    prop = [gs.rounded_cylinder("Hub", (0, 8.1, -1.0), 0.18, 0.3, 0.06, C["gold"], axis=(0, 1, 0), seg=8)]
    for j in range(4):
        a = j / 4 * 2 * math.pi
        prop.append(gs.ellipsoid(f"Blade{j}", (0.45 * math.cos(a), 8.12, -1.0 + 0.45 * math.sin(a)),
                                 (0.4, 0.05, 0.18), C["gold"], seg=8, rings=4,
                                 rot=[("Y", -math.degrees(a)), ("Z", 20)]))
    k.finish(S, name, P, moving=[("Flag", (0, 7.3, 4.8), [flag]), ("Propeller", (0, 8.1, -1.0), prop)])


def bridge(S, name):
    """Puente de arco modular que cruza un cauce de 12 m (luz entre estribos 13 m, ancho 4 m)."""
    C = pal()
    top = [(-9.0, 5.0), (-4.5, 5.3), (0.0, 5.45), (4.5, 5.3), (9.0, 5.0)]
    arch = [(6.5 * math.cos(math.radians(a)), 1.4 + 2.7 * math.sin(math.radians(a))) for a in range(0, 181, 15)]
    prof = [(-9.0, -1.5)] + top + [(9.0, -1.5), (6.8, -1.5)] + arch + [(-6.8, -1.5)]
    body = k.extrude("Bridge", [(x, z) for x, z in prof], -2.0, 2.0, [C["stone"]])
    P = [body]
    for y in (-2.05, 2.05):
        P.append(gs.tube(f"Voussoir{y}", [(x, y, z) for x, z in arch], 0.28, C["stone2"], seg=6, cap=1))
        rail = [(x, y * 0.92, z + 1.05) for x, z in top]
        P.append(gs.tube(f"Rail{y}", rail, 0.09, C["iron"], seg=6, sub=3, cap=1))
        for i in range(9):
            x = -8.0 + 2.0 * i
            z = 5.0 + 0.45 * (1 - (x / 9.0) ** 2)
            P.append(gs.capsule(f"Post{y}{i}", (x, y * 0.92, z), (x, y * 0.92, z + 1.05), 0.07, C["iron"], seg=5,
                                cap=1, sub=1))
    for x, y in ((-8.6, -1.9), (8.6, 1.9)):
        P.append(gs.capsule(f"LampPole{x}", (x, y, 5.0), (x, y, 7.6), 0.09, C["iron"], seg=6, cap=1, sub=1))
        P.append(gs.ellipsoid(f"LampGlow{x}", (x, y, 7.85), (0.32, 0.32, 0.38), C["amber"], seg=10, rings=6))
        P.append(gs.cone(f"LampHat{x}", (x, y, 8.1), (x, y, 8.6), 0.4, 0.03, C["iron"], seg=8))
    k.finish(S, name, P, view=(0.6, -0.9, 0.3))


def dock(S, name):
    """Muelle modular de 4 x 3 m (pivote en el centro, cubierta a z = 1.0)."""
    C = pal()
    rnd = random.Random(2)
    P = []
    for i in range(6):
        y = -1.25 + 0.5 * i
        P.append(gs.rounded_box(f"Plank{i}", (rnd.uniform(-0.06, 0.06), y, 1.0 + rnd.uniform(-0.02, 0.02)),
                                (4.0, 0.46, 0.14), 0.04, C["wood"] if i % 2 else C["wood2"],
                                rot=("Z", rnd.uniform(-1.5, 1.5))))
    for x in (-1.7, 1.7):
        for y in (-1.3, 1.3):
            P.append(gs.capsule(f"Pile{x}{y}", (x, y, -2.5), (x, y, 1.25), 0.2, C["wood2"], seg=8, cap=2, sub=1))
        P.append(gs.torus(f"Tire{x}", (x, -1.55, 0.5), 0.38, 0.14, C["black"], axis=(0, 1, 0), seg=12, mseg=6))
    P.append(gs.capsule("Beam", (-2.0, -1.4, 0.75), (2.0, -1.4, 0.75), 0.12, C["wood2"], seg=6, cap=1, sub=1))
    P.append(gs.lathe("Bollard", [(0.0, 1.07), (0.22, 1.07), (0.15, 1.35), (0.15, 1.5), (0.26, 1.6), (0.0, 1.66)],
                      C["navy2"], loc=(1.2, 0.2, 0), seg=10))
    P.append(gs.torus("Coil", (-1.0, 0.5, 1.13), 0.3, 0.06, C["rope"] if "rope" in C else M("#D9B77A"), seg=12,
                      mseg=4))
    P.append(gs.torus("Coil2", (-1.0, 0.5, 1.22), 0.22, 0.06, M("#D9B77A"), seg=12, mseg=4))
    k.finish(S, name, P)


def stall(S, name):
    """Puesto del mercado flotante: plataforma, mostrador, montañas de fruta y toldo a rayas."""
    C = pal()
    rnd = random.Random(6)
    P = [gs.rounded_box("Deck", (0, 0, 0.35), (3.4, 2.6, 0.3), 0.1, C["wood"])]
    for s in (1, -1):
        P.append(gs.capsule(f"Pontoon{s}", (1.2 * s, -1.4, 0.0), (1.2 * s, 1.4, 0.0), 0.35, C["navy2"], seg=10,
                            cap=3, sub=1))
    P.append(gs.rounded_box("Counter", (0, -0.6, 0.95), (3.0, 0.9, 0.9), 0.15, C["wood2"], bulge=0.05))
    P.append(gs.rounded_box("CounterTop", (0, -0.6, 1.43), (3.2, 1.1, 0.08), 0.04, C["white"]))
    fruit = [(C["orange"], 0.13), (C["yellow"], 0.11), (C["red"], 0.12), (C["green"], 0.2)]
    for pile in range(3):
        col, r = fruit[pile]
        cx = -1.0 + 1.0 * pile
        for lvl, cnt in ((0, 5), (1, 3), (2, 1)):
            for j in range(cnt):
                a = j / max(cnt, 1) * 2 * math.pi + lvl
                rr = (0.28 - 0.12 * lvl) if cnt > 1 else 0.0
                P.append(gs.ellipsoid(f"F{pile}{lvl}{j}", (cx + rr * math.cos(a), -0.6 + rr * math.sin(a),
                                                          1.47 + r + lvl * r * 1.5), r, col, seg=6, rings=4))
    for j in range(3):
        P.append(gs.ellipsoid(f"Melon{j}", (1.1 - 0.5 * j, 0.5, 0.75), (0.3, 0.22, 0.22), C["green"], seg=8, rings=5,
                              rot=("Z", rnd.uniform(0, 90))))
    for x in (-1.5, 1.5):
        for y in (-1.1, 1.1):
            P.append(gs.capsule(f"Pole{x}{y}", (x, y, 0.5), (x, y, 2.9 + (0.3 if y > 0 else 0)), 0.05, C["wood2"],
                                seg=5, cap=1, sub=1))
    canopy = k.sweep("Awning", [(-1.75, 0), (1.75, 0), (1.75, 0.08), (-1.75, 0.08)],
                     [(0, -1.5, 2.85), (0, -0.4, 3.1), (0, 0.7, 3.25), (0, 1.3, 3.25)], [C["pink"], C["white"]],
                     up=(0, 0, 1), sub=3)
    gs.bands_by_plane(canopy, (1, 0, 0), (-1.75, 0, 0), 0.5, 0.5, 1, cut=True)
    P.append(canopy)
    for i in range(7):
        P.append(gs.ellipsoid(f"Scallop{i}", (-1.5 + 0.5 * i, -1.52, 2.78), (0.25, 0.04, 0.16),
                              C["pink"] if i % 2 == 0 else C["white"], seg=6, rings=3))
    P.append(gs.tube("Lights", [(-1.5, -1.12, 2.7), (0, -1.15, 2.45), (1.5, -1.12, 2.7)], 0.015, C["iron"], seg=4,
                     sub=3, cap=1))
    for i in range(5):
        x = -1.2 + 0.6 * i
        P.append(gs.ellipsoid(f"Bulb{i}", (x, -1.15, 2.62 - 0.2 * math.cos(x / 1.5 * math.pi / 2) ** 2 + 0.1), 0.07,
                              C["amber"], seg=6, rings=4))
    k.finish(S, name, P)


def awning(S, name):
    """Toldo trampolín (H27, W1-4): lona a rayas bien inflada sobre 4 postes."""
    C = pal()
    P = []
    for x in (-1.9, 1.9):
        for y in (-1.4, 1.4):
            P.append(gs.capsule(f"Pole{x}{y}", (x, y, 0), (x, y, 2.5), 0.07, C["iron"], seg=6, cap=1, sub=1))
            P.append(gs.ellipsoid(f"Cap{x}{y}", (x, y, 2.55), 0.11, C["gold"], seg=6, rings=4))
    can = k.sweep("Canvas", lambda u: [(-2.1, 0.0), (2.1, 0.0), (2.1, 0.12 + 0.35 * math.sin(math.pi * u)),
                                       (-2.1, 0.12 + 0.35 * math.sin(math.pi * u))],
                  [(0, -1.6, 2.45), (0, 0, 2.95), (0, 1.6, 2.45)], [C["cyan"], C["white"]], sub=4)
    gs.bands_by_plane(can, (1, 0, 0), (-2.1, 0, 0), 0.6, 0.5, 1, cut=True)
    P.append(can)
    for y in (-1.6, 1.6):
        for i in range(8):
            P.append(gs.ellipsoid(f"Scallop{y}{i}", (-1.85 + 0.53 * i, y * 1.01, 2.38), (0.27, 0.04, 0.17),
                                  C["cyan"] if i % 2 == 0 else C["white"], seg=6, rings=3))
    k.finish(S, name, P)


def container(S, name, v):
    C = pal()
    col = [C["orange"], M("#2EA8C9"), M("#D6457F")][v]
    L, Wd, H = 6.1, 2.44, 2.6
    P = [gs.rounded_box("Box", (0, 0, H / 2), (Wd, L, H), 0.08, col)]
    for s in (1, -1):
        for i in range(9):
            P.append(gs.rounded_box(f"Rib{s}{i}", (s * (Wd / 2 + 0.03), -2.6 + 0.65 * i, H / 2), (0.08, 0.28, H - 0.3),
                                    0.03, col, segments=1))
    for x in (-0.75, -0.25, 0.25, 0.75):
        P.append(gs.capsule(f"Lock{x}", (x, -L / 2 - 0.05, 0.2), (x, -L / 2 - 0.05, H - 0.2), 0.04, C["iron"], seg=4,
                            cap=1, sub=1))
    for z in (0.08, H - 0.08):
        P.append(gs.rounded_box(f"Frame{z}", (0, 0, z), (Wd + 0.06, L + 0.06, 0.16), 0.04, C["black"], segments=1))
    if v == 2:
        for s in (1, -1):
            phi = math.radians(110 * s)
            off = Vector((-s * Wd / 4 * math.cos(phi), -s * Wd / 4 * math.sin(phi), 0))
            P.append(gs.rounded_box(f"Door{s}", Vector((s * Wd / 2, -L / 2 - 0.05, H / 2)) + off,
                                    (Wd / 2 - 0.05, 0.08, H - 0.25), 0.04, col, rot=("Z", 110 * s)))
        P.append(gs.rounded_box("Hole", (0, -L / 2 + 0.02, H / 2), (Wd - 0.25, 0.1, H - 0.3), 0.04, C["black"]))
        for j in range(3):
            P.append(gs.rounded_box(f"Crate{j}", (-0.5 + 0.55 * j, -L / 2 + 0.6, 0.3 + 0.55 * (j == 1)),
                                    (0.5, 0.5, 0.5), 0.05, C["card"]))
    k.finish(S, name, P)


def cargo_ship(S, name):
    """Barco de carga del final de W1-5, con rampa trasera bajada al agua. Bandera móvil."""
    C = pal()
    P = [k.hull("Hull", 30.0, 10.0, 5.0, [C["navy2"], C["white"], C["red"]], stripe=(1.4, 1.9, 1), bottom=(-0.8, 2),
                bow_rise=0.5, belly=0.1)]
    P.append(gs.rounded_box("Deck", (0, 0, 2.6), (9.2, 25.0, 0.3), 0.1, C["concrete"]))
    cols = [C["orange"], M("#2EA8C9"), M("#D6457F"), C["yellow"]]
    rnd = random.Random(9)
    for i in range(4):
        for j in range(3):
            h = rnd.choice((1, 1, 2))
            for lv in range(h):
                P.append(gs.rounded_box(f"Cont{i}{j}{lv}", (-2.9 + 2.9 * j, -9.0 + 4.0 * i, 3.9 + 2.5 * lv),
                                        (2.6, 3.7, 2.4), 0.12, cols[(i + j + lv) % 4], segments=1))
    P.append(gs.rounded_box("Tower", (0, 9.0, 6.0), (7.0, 4.0, 6.5), 0.5, C["white"], taper=(0.9, 0.9)))
    P.append(gs.rounded_box("TowerWin", (0, 6.95, 8.0), (6.0, 0.15, 0.9), 0.15, C["cyan"]))
    for z in (5.2, 6.6):
        P.append(gs.rounded_box(f"Win{z}", (0, 6.95, z), (5.4, 0.12, 0.5), 0.1, C["amber"]))
    P.append(gs.lathe("Funnel", [(0.0, 9.0), (1.0, 9.0), (1.1, 11.5), (0.0, 11.5)], [C["orange"]], loc=(0, 10.0, 0),
                      seg=12))
    P.append(gs.rounded_cylinder("FunnelBand", (0, 10.0, 10.6), 1.1, 0.4, 0.05, C["navy"], seg=12, steps=1))
    ramp = gs.rounded_box("Ramp", (0, 18.2, 1.2), (6.0, 7.6, 0.35), 0.12, [C["yellow"], C["black"]],
                          rot=("X", 22))
    gs.bands_by_plane(ramp, (1, 1, 0), (0, 0, 0), 0.8, 0.5, 1, cut=True)
    P.append(ramp)
    for s in (1, -1):
        P.append(gs.capsule(f"RampRail{s}", (3.1 * s, 14.8, 3.2), (3.1 * s, 21.6, 0.4), 0.08, C["iron"], seg=5,
                            cap=1, sub=1))
        P.append(gs.capsule(f"Chain{s}", (3.1 * s, 13.5, 6.0), (3.1 * s, 19.5, 1.9), 0.05, C["iron"], seg=4, cap=1,
                            sub=1))
    P.append(gs.capsule("FlagPole", (0, -13.2, 3.4), (0, -13.2, 7.4), 0.08, C["white"], seg=5, cap=1, sub=1))
    flag = k.flag("Flag", (0, -13.2, 7.0), 2.0, 1.1, [C["orange"], C["navy"]], stripes=2)
    k.finish(S, name, P, moving=[("Flag", (0, -13.2, 7.0), [flag])], view=(0.8, -0.6, 0.4))


# ---------------------------------------------------------------------------
# Relleno: faroles, plantas, carteles, fruta, cajas, barriles, boyas
# ---------------------------------------------------------------------------
def lamp(S, name, v):
    C = pal()
    P = k.lamp_post("Lamp", C["iron"], C["amber"], h=4.2 if v == 0 else 3.4, arms=2 if v == 0 else 1)
    if v == 1:
        P.append(gs.rounded_box("Flyer", (0, -0.12, 1.8), (0.4, 0.04, 0.55), 0.03, C["pink"], rot=("Y", 8)))
        P.append(gs.ellipsoid("FlyerIcon", (0, -0.15, 1.85), (0.1, 0.02, 0.1), C["cyan"], seg=6, rings=3))
    k.finish(S, name, P)


def planter(S, name):
    C = pal()
    P = [gs.rounded_box("Box", (0, 0, 0.35), (2.2, 0.9, 0.7), 0.15, C["brick"], taper=(1.08, 1.12), bulge=0.05),
         gs.rounded_box("Soil", (0, 0, 0.7), (2.1, 0.85, 0.08), 0.04, C["wood2"])]
    cols = [C["red"], C["yellow"], C["pink"], C["white"], C["orange"]]
    for i in range(7):
        x = -0.85 + 0.28 * i
        P += k.flower(f"Fl{i}", (x, 0.1 * ((-1) ** i), 0.72), cols[i % 5], C["yellow"] if i % 5 else C["orange"],
                      stem=C["green"], size=0.17, h=0.35 + 0.15 * (i % 3), tilt=10)
    for x in (-0.7, 0.7):
        P.append(k.leaf(f"Leaf{x}", [(x, 0, 0.72), (x * 1.3, -0.3, 1.0), (x * 1.6, -0.5, 0.85)], 0.15, C["leaf"]))
    k.finish(S, name, P)


def topiary(S, name, v):
    """Arbusto podado: bola (A) o elefante del zoológico (B)."""
    C = pal()
    P = [gs.lathe("Pot", [(0.0, 0.0), (0.5, 0.0), (0.62, 0.7), (0.66, 0.75), (0.0, 0.75)], C["orange"], seg=14)]
    if v == 0:
        P.append(gs.capsule("Trunk", (0, 0, 0.7), (0, 0, 1.5), 0.08, C["wood2"], seg=6, cap=1, sub=1))
        P += k.bush("Ball", (0, 0, 1.2), 0.7, [C["green"], C["leaf"]], seed=1, n=5)
    else:
        g = [C["green"]]
        P.append(gs.ellipsoid("Body", (0, 0.1, 1.55), (0.55, 0.75, 0.55), g, seg=12, rings=7))
        P.append(gs.ellipsoid("Head", (0, -0.65, 1.95), 0.42, g, seg=12, rings=7))
        P.append(gs.tube("Trunk", [(0, -0.95, 1.85), (0, -1.15, 1.5), (0, -1.1, 1.2), (0, -0.95, 1.15)],
                         [0.14, 0.11, 0.09, 0.08], g, seg=8, sub=3, cap=2))
        for s in (1, -1):
            P.append(gs.ellipsoid(f"Ear{s}", (0.42 * s, -0.55, 2.0), (0.12, 0.35, 0.38), C["leaf"], seg=8, rings=5))
            for y in (-0.35, 0.5):
                P.append(gs.capsule(f"Leg{s}{y}", (0.3 * s, y, 1.4), (0.3 * s, y, 0.8), 0.15, g, seg=8, cap=2, sub=1))
        P += k.flower("Bow", (0, -0.6, 2.32), C["pink"], C["yellow"], size=0.15, h=0.0)
    k.finish(S, name, P)


def funny_sign(S, name, v):
    """Carteles sin texto: A prohibido tirarse pedos; B flecha con pescado; C cuidado con la balsa voladora."""
    C = pal()

    def no_fart(c):
        out = [gs.torus("Ring", c, 0.36, 0.06, C["red"], axis=(0, 1, 0), seg=18, mseg=4)]
        for i, (dx, dz, r) in enumerate(((-0.12, -0.05, 0.13), (0.06, 0.02, 0.17), (0.18, -0.06, 0.11))):
            out.append(gs.ellipsoid(f"Cloud{i}", c + Vector((dx, 0.02, dz)), (r, 0.03, r * 0.8), M("#8B6B3A"),
                                    seg=8, rings=4))
        out.append(gs.rounded_box("Slash", c + Vector((0, -0.04, 0)), (0.75, 0.04, 0.08), 0.02, C["red"],
                                  rot=("Y", 40)))
        return out

    def fish(c):
        return [gs.ellipsoid("Fish", c + Vector((0.08, 0, 0)), (0.28, 0.03, 0.16), C["orange"], seg=10, rings=5),
                gs.cone("FishTail", c + Vector((-0.15, 0, 0)), c + Vector((-0.4, 0, 0)), 0.15, 0.01, C["orange"],
                        seg=4),
                gs.ellipsoid("FishEye", c + Vector((0.25, -0.03, 0.04)), 0.035, C["black"], seg=6, rings=3)]

    def raft(c):
        return [gs.torus("Raft", c + Vector((0, 0, -0.1)), 0.24, 0.07, C["orange"], axis=(0, 1, 0.4), seg=12,
                         mseg=4),
                gs.tube("Arc", gs.arc_points(c + Vector((0, -0.02, -0.1)), 0.38, 20, 160, 6, plane=("X", "Z")),
                        0.03, C["black"], seg=4, cap=1),
                gs.cone("Warn", c + Vector((0, 0.01, 0.25)), c + Vector((0, 0.01, 0.42)), 0.12, 0.02, C["yellow"],
                        seg=3)]
    board = [C["white"], C["yellow"], M("#2EA8C9")][v]
    P = k.sign("Sign", C["iron"], board, [no_fart, fish, raft][v], w=1.1 if v != 1 else 1.4, bh=1.0 if v != 1 else 0.7)
    if v == 1:
        P.append(gs.cone("ArrowTip", (0.7, -0.06, 1.75), (1.05, -0.06, 1.75), 0.38, 0.01, board, seg=3,
                         round_tip=False))
    k.finish(S, name, P)


def fruit_basket(S, name):
    C = pal()
    b = gs.lathe("Basket", [(0.0, 0.0), (0.4, 0.0), (0.55, 0.3), (0.58, 0.35), (0.5, 0.36), (0.0, 0.2)],
                 [C["wood"], C["wood2"]], seg=16)
    gs.bands_by_plane(b, (0, 0, 1), (0, 0, 0), 0.1, 0.5, 1, cut=True)
    P = [b, gs.tube("Handle", gs.arc_points((0, 0, 0.36), 0.45, 0, 180, 9, plane=("Y", "Z"), depth_axis="X"), 0.03,
                         C["wood2"], seg=5, cap=1)]
    fr = [(C["orange"], 0.12), (C["red"], 0.11), (M("#FFB627"), 0.13), (C["green"], 0.1)]
    for i in range(7):
        a = i / 7 * 2 * math.pi
        col, r = fr[i % 4]
        P.append(gs.ellipsoid(f"Fruit{i}", (0.28 * math.cos(a), 0.28 * math.sin(a), 0.38), r, col, seg=8, rings=5))
    for i in range(3):
        P.append(gs.tube(f"Banana{i}", gs.arc_points((0, 0.05 * i, 0.4), 0.25, 210, 330, 5, plane=("X", "Z")),
                         [0.04, 0.05, 0.05, 0.04, 0.02], C["yellow"], seg=6, cap=1))
    k.finish(S, name, P)


def flagpole(S, name):
    C = pal()
    P = [gs.rounded_cylinder("Base", (0, 0, 0.25), 0.5, 0.5, 0.1, C["concrete"], seg=12, steps=1),
         gs.capsule("Pole", (0, 0, 0.4), (0, 0, 7.0), 0.08, C["white"], r1=0.05, seg=6, cap=1, sub=1),
         gs.ellipsoid("Top", (0, 0, 7.1), 0.15, C["gold"], seg=8, rings=5)]
    f = k.flag("Flag", (0.05, 0, 6.4), 2.4, 1.4, [C["pink"], C["navy"]], stripes=4, amp=0.2)
    k.finish(S, name, P, moving=[("Flag", (0.05, 0, 6.4), [f])])


def crate_p(S, name, v):
    C = pal()
    extra = None if v != 2 else [gs.ellipsoid(f"Fish{i}", (-0.2 + 0.2 * i, 0.1 * (i % 2), 1.3), (0.25, 0.08, 0.1), M("#7FA7C9"), seg=8,
                          rings=4, rot=("Z", 30 * i)) for i in range(3)]
    P = k.crate(v, C["card"] if v == 1 else C["wood"], C["wood2"], C["pink"] if v == 0 else C["orange"], extra)
    k.finish(S, name, P)


def barrel_p(S, name, v):
    C = pal()
    k.finish(S, name, k.barrel(v, C["wood"], C["iron"], [C["orange"], M("#2EA8C9"), C["red"]][v]))


def buoy_p(S, name, v):
    C = pal()
    P, mv = k.buoy(v, C["red"] if v != 1 else C["orange"], C["white"], C["amber"] if v != 1 else C["cyan"],
                   C["iron"])
    k.finish(S, name, P, moving=mv or None)


PROPS = [
    ("ZooGate", zoo_gate), ("FerrisWheel", ferris_wheel), ("Statue", statue), ("Fountain_Tiered", fountain_tiered),
    ("Fountain_Launch", fountain_launch), ("Bench_A", lambda S, n: bench(S, n, 0)),
    ("Bench_B", lambda S, n: bench(S, n, 1)), ("Fence", fence),
    ("Tunnel_Straight", tunnel), ("Tunnel_Curve45", lambda S, n: tunnel(S, n, True)),
    ("Canal_Straight", lambda S, n: canal(S, n, "straight")), ("Canal_Curve90", lambda S, n: canal(S, n, "curve")),
    ("Canal_Narrow", lambda S, n: canal(S, n, "narrow")), ("Canal_Taper", lambda S, n: canal(S, n, "taper")),
    ("Canal_Ramp", lambda S, n: canal(S, n, "ramp")), ("SluiceGate", sluice_gate),
    ("Speedboat", boat), ("Ferry", ferry), ("Bridge", bridge), ("Dock", dock), ("MarketStall", stall),
    ("Awning", awning), ("Container_A", lambda S, n: container(S, n, 0)),
    ("Container_B", lambda S, n: container(S, n, 1)), ("Container_C", lambda S, n: container(S, n, 2)),
    ("CargoShip", cargo_ship), ("Lamp_A", lambda S, n: lamp(S, n, 0)), ("Lamp_B", lambda S, n: lamp(S, n, 1)),
    ("Planter", planter), ("Topiary_A", lambda S, n: topiary(S, n, 0)), ("Topiary_B", lambda S, n: topiary(S, n, 1)),
    ("Sign_A", lambda S, n: funny_sign(S, n, 0)), ("Sign_B", lambda S, n: funny_sign(S, n, 1)),
    ("Sign_C", lambda S, n: funny_sign(S, n, 2)), ("FruitBasket", fruit_basket), ("Flagpole", flagpole),
] + [(f"Crate_{c}", (lambda v: lambda S, n: crate_p(S, n, v))(i)) for i, c in enumerate("ABC")] \
  + [(f"Barrel_{c}", (lambda v: lambda S, n: barrel_p(S, n, v))(i)) for i, c in enumerate("ABC")] \
  + [(f"Buoy_{c}", (lambda v: lambda S, n: buoy_p(S, n, v))(i)) for i, c in enumerate("ABC")]

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    k.run_props(W, PROPS, only=set(argv) or None)
