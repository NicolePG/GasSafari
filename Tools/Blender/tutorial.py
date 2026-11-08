"""GAS SAFARI · señalética del tutorial T-0 Piscina del Zoológico (docs/04 T-0, docs/07 §8): carteles de madera del
zoológico SIN texto (el texto y los iconos los pone Unity sobre la pieza `Panel` y las placas `Icon_0..3`), barrera de
burbujas de cada lección y boyas de lección.
blender --background --python tutorial.py [-- Tutorial_BigSign ...]
Salida: Tools/Export/Tutorial/<Nombre>.fbx (≤ 3000 triángulos, pivote en la base, frente hacia -Y, agua en z = 0) y la
hoja de contacto Tools/Blender/previews/Tutorial_sheet.png. Mismo formato que Tools/Export/Hazards (vacío raíz con
`Static` y las partes móviles/intercambiables como objetos aparte con su pivote)."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
import env_kit as k
import hazards as H
from gs_common import Vector, Matrix
from env_kit import M, E

H.OUT = os.path.join(gs.EXPORT_DIR, "Tutorial")
pal = H.pal


def tpal():
    C = pal()
    C.update(paper=M("#F6E6BE", 0.9), paper2=M("#EBD39A", 0.9), leaf2=M("#2D8A4E"), plate=M("#FFF4D6", 0.7))
    return C


def paw(name, loc, s, mat):
    """Huella de pata (almohadilla + 4 dedos) sobre un plano XZ de cara a -Y."""
    c = Vector(loc)
    P = [gs.ellipsoid(name + "_pad", c, (0.5 * s, 0.1 * s, 0.4 * s), mat, seg=8, rings=4)]
    for i, (dx, dz) in enumerate(((-0.62, 0.55), (-0.22, 0.85), (0.22, 0.85), (0.62, 0.55))):
        P.append(gs.ellipsoid(f"{name}_t{i}", c + Vector((dx * s, 0, dz * s)), (0.17 * s, 0.09 * s, 0.2 * s), mat, seg=6, rings=4))
    return P


def plank_frame(name, center, w, h, th, wood, wood2, bev=0.05):
    """Marco de 4 listones alrededor de un panel de w x h (en el plano XZ)."""
    cx, cy, cz = center
    t = 0.18
    return [gs.rounded_box(name + "_T", (cx, cy, cz + h / 2 + t / 2), (w + 2 * t, th, t), bev, wood, segments=1),
            gs.rounded_box(name + "_B", (cx, cy, cz - h / 2 - t / 2), (w + 2 * t, th, t), bev, wood, segments=1),
            gs.rounded_box(name + "_L", (cx - w / 2 - t / 2, cy, cz), (t, th, h), bev, wood2, segments=1),
            gs.rounded_box(name + "_R", (cx + w / 2 + t / 2, cy, cz), (t, th, h), bev, wood2, segments=1)]


# ---------------------------------------------------------------------------
# Cartel grande de lección: dos postes, panel en blanco para el título/texto y 4 placas para los iconos de jugador
# ---------------------------------------------------------------------------
def big_sign(S, name):
    C = tpal()
    P = []
    for s in (1, -1):
        P.append(gs.capsule(f"Post{s}", (3.1 * s, 0.15, -0.3), (3.1 * s, 0.15, 4.5), 0.16, C["wood2"], seg=8, sub=1, cap=2))
        P.append(gs.ellipsoid(f"PostTop{s}", (3.1 * s, 0.15, 4.6), (0.22, 0.22, 0.18), C["yellow"], seg=8, rings=5))
        P.append(gs.rounded_cylinder(f"Foot{s}", (3.1 * s, 0.15, 0.0), 0.5, 0.3, 0.08, C["stone2"], seg=10, steps=1))
    P += plank_frame("Frame", (0, 0, 3.0), 4.6, 1.9, 0.3, C["wood"], C["wood2"])
    # barra superior con tejadillo verde, con hojas y huellas
    P.append(gs.rounded_box("Roof", (0, 0.02, 4.38), (6.6, 0.55, 0.22), 0.08, C["green"], segments=1))
    P.append(gs.rounded_box("RoofBand", (0, -0.24, 4.2), (6.4, 0.1, 0.14), 0.04, C["leaf2"], segments=1))
    # travesaño inferior para las placas de icono
    P.append(gs.rounded_box("Shelf", (0, -0.04, 1.58), (5.6, 0.34, 0.2), 0.06, C["wood"], segments=1))
    for s in (1, -1):
        P.append(gs.capsule(f"Brace{s}", (2.5 * s, 0, 1.5), (3.0 * s, 0.1, 2.1), 0.06, C["wood2"], seg=6, sub=1, cap=1))
    for i, (x, sgn) in enumerate(((-2.95, 1), (2.95, -1))):
        P += paw(f"Paw{i}", (x * 0.8, -0.2, 3.1), 0.28, C["wood2"])
    for i, x in enumerate((-2.7, 2.7)):
        P.append(k.leaf(f"Sprig{i}", [(x, -0.2, 4.0), (x * 0.9, -0.28, 3.8), (x * 0.78, -0.3, 3.55)], 0.13, C["leaf2"], sub=2, droop_up=(0, -1, 0)))
    # luces de feria
    pts = [(-3.1 + 6.2 * t / 8, -0.32, 4.15 - 0.2 * math.sin(math.pi * t / 8)) for t in range(9)]
    P.append(gs.tube("Wire", pts, 0.02, C["black"], seg=4, sub=1, cap=1))
    for i, p in enumerate(pts[1:-1]):
        P.append(gs.ellipsoid(f"Bulb{i}", (p[0], p[1], p[2] - 0.1), 0.07, [C["glow_y"], C["glow_p"], C["glow_c"]][i % 3], seg=6, rings=4))
    panel = [gs.rounded_box("Panel", (0, -0.1, 3.0), (4.6, 0.1, 1.9), 0.03, [C["paper"], C["paper2"]], segments=1)]
    gs.paint_faces(panel[0], lambda c: abs(c.x) > 2.15 and False, 1)
    movs = [("Panel", (0, -0.1, 3.0), panel)]
    for i in range(4):
        x = -2.1 + 1.4 * i
        pl = [gs.rounded_cylinder(f"Plate{i}", (x, -0.2, 1.15), 0.45, 0.1, 0.03, C["plate"], axis=(0, 1, 0), seg=16, steps=1),
              gs.torus(f"Rim{i}", (x, -0.25, 1.15), 0.45, 0.05, C["yellow"], axis=(0, 1, 0), seg=16, mseg=4)]
        movs.append((f"Icon_{i}", (x, -0.2, 1.15), pl))
        P.append(gs.capsule(f"Hook{i}", (x, -0.05, 1.5), (x, -0.15, 1.45), 0.025, C["chrome"], seg=4, sub=1, cap=1))
        P.append(gs.capsule(f"Chain{i}", (x, -0.15, 1.45), (x, -0.2, 1.58), 0.02, C["chrome"], seg=4, sub=1, cap=1))
    H.finish(S, name, P, moving=movs, view=(0.35, -1.0, 0.25), margin=1.05)


# ---------------------------------------------------------------------------
# Carteles pequeños: de poste (tablón en blanco) y flecha direccional (se puede espejar con escala X = -1)
# ---------------------------------------------------------------------------
def post_sign(S, name):
    C = tpal()
    P = [gs.capsule("Post", (0, 0.1, -0.3), (0, 0.1, 2.2), 0.09, C["wood2"], seg=8, sub=1, cap=2),
         gs.rounded_cylinder("Foot", (0, 0.1, 0.0), 0.3, 0.2, 0.06, C["stone2"], seg=10, steps=1),
         gs.ellipsoid("Cap", (0, 0.1, 2.3), (0.13, 0.13, 0.1), C["yellow"], seg=8, rings=4)]
    P += plank_frame("Frame", (0, 0, 1.6), 1.0, 0.7, 0.16, C["wood"], C["wood2"], 0.04)
    panel = [gs.rounded_box("Panel", (0, -0.05, 1.6), (1.0, 0.07, 0.7), 0.02, C["paper"], segments=1)]
    P += paw("Paw", (0.0, -0.14, 0.95), 0.1, C["wood2"])
    for sx in (-1, 1):
        P.append(gs.ellipsoid(f"Nail{sx}", (0.5 * sx + 0.0, -0.1, 1.98), 0.03, C["chrome"], seg=4, rings=3))
    H.finish(S, name, P, moving=[("Panel", (0, -0.05, 1.6), panel)], view=(0.4, -1.0, 0.2))


def arrow_sign(S, name):
    C = tpal()
    P = [gs.capsule("Post", (0, 0.1, -0.3), (0, 0.1, 2.0), 0.09, C["wood2"], seg=8, sub=1, cap=2),
         gs.rounded_cylinder("Foot", (0, 0.1, 0.0), 0.3, 0.2, 0.06, C["stone2"], seg=10, steps=1)]
    pts = [(-0.9, -0.28), (0.4, -0.28), (0.4, -0.5), (1.15, 0.0), (0.4, 0.5), (0.4, 0.28), (-0.9, 0.28)]
    board = k.extrude("ArrowBoard", [(x, z + 1.7) for x, z in pts], -0.1, 0.1, [C["wood"]])
    inner = [(-0.8, -0.18), (0.5, -0.18), (0.5, -0.3), (0.95, 0.0), (0.5, 0.3), (0.5, 0.18), (-0.8, 0.18)]
    face = k.extrude("ArrowFace", [(x, z + 1.7) for x, z in inner], -0.12, -0.08, [C["paper"]])
    P += [board, gs.ellipsoid("Cap", (0, 0.1, 2.1), (0.12, 0.12, 0.1), C["yellow"], seg=8, rings=4)]
    movs = [("Panel", (0, -0.1, 1.7), [face])]
    H.finish(S, name, P, moving=movs, view=(0.4, -1.0, 0.2))


# ---------------------------------------------------------------------------
# Cartel colgante de zona: arco con cadenas y tablón en blanco, sobre la piscina
# ---------------------------------------------------------------------------
def hanging_sign(S, name):
    C = tpal()
    P = []
    for s in (1, -1):
        P.append(gs.capsule(f"Post{s}", (2.6 * s, 0, -0.3), (2.6 * s, 0, 4.0), 0.14, C["wood2"], seg=8, sub=1, cap=2))
        P.append(gs.ellipsoid(f"Top{s}", (2.6 * s, 0, 4.1), (0.2, 0.2, 0.16), C["yellow"], seg=8, rings=4))
        P.append(gs.rounded_cylinder(f"Foot{s}", (2.6 * s, 0, 0.0), 0.45, 0.3, 0.08, C["stone2"], seg=10, steps=1))
        P.append(gs.capsule(f"Brace{s}", (2.6 * s, 0, 3.4), (1.9 * s, 0, 3.95), 0.06, C["wood2"], seg=6, sub=1, cap=1))
    P.append(gs.rounded_box("Beam", (0, 0, 3.95), (5.8, 0.3, 0.26), 0.08, C["wood"], segments=1))
    board_parts = [gs.rounded_box("Plank", (0, -0.05, 3.0), (3.8, 0.2, 1.1), 0.06, C["wood"], segments=1),
                   gs.rounded_box("Panel", (0, -0.17, 3.0), (3.4, 0.06, 0.8), 0.02, C["paper"], segments=1)]
    for s in (1, -1):
        board_parts.append(gs.ellipsoid(f"Eye{s}", (1.7 * s, -0.12, 3.4), 0.06, C["chrome"], seg=5, rings=3))
        for j in range(4):
            P.append(gs.capsule(f"Chain{s}_{j}", (1.7 * s, -0.12, 3.95 - 0.15 * j - 0.02), (1.7 * s, -0.12, 3.95 - 0.15 * (j + 1) - 0.02), 0.025, C["chrome"], seg=4, sub=1, cap=1))
    board_parts += paw("PawA", (-1.65, -0.17, 2.75), 0.12, C["wood2"]) + paw("PawB", (1.65, -0.17, 2.75), 0.12, C["wood2"])
    for i, x in enumerate((-1.4, 0.0, 1.4)):
        P.append(gs.ellipsoid(f"Bulb{i}", (x, -0.18, 3.95 - 0.28), 0.09, [C["glow_y"], C["glow_p"], C["glow_c"]][i], seg=6, rings=4))
    H.finish(S, name, P, moving=[("Board", (0, -0.05, 3.55), board_parts)], view=(0.4, -1.0, 0.2), margin=1.05)


# ---------------------------------------------------------------------------
# Barrera de burbujas: dos surtidores de burbujas y una pared de burbujas sueltas (cada una revienta sola)
# ---------------------------------------------------------------------------
def bubble(name, loc, r, col, C):
    parts = [gs.ellipsoid(name + "_B", (0, 0, 0), r, col, seg=8, rings=5),
             gs.ellipsoid(name + "_S", (-0.32 * r, -0.62 * r, 0.42 * r), (0.26 * r, 0.1 * r, 0.34 * r), C["foam"], seg=5, rings=3)]
    o = k.join(name, parts)
    o.location = Vector(loc)
    return o


def bubble_barrier(S, name):
    C = tpal()
    P = []
    for s in (1, -1):
        P.append(gs.rounded_cylinder(f"Base{s}", (3.6 * s, 0, 0.1), 0.6, 0.3, 0.08, [C["pink"], C["white"]], seg=14, steps=1))
        P.append(gs.rounded_cylinder(f"Tank{s}", (3.6 * s, 0, 0.9), 0.45, 1.3, 0.1, [C["tile"], C["white"]], seg=14, steps=2))
        P.append(gs.torus(f"Wand{s}", (3.6 * s, -0.05, 1.95), 0.32, 0.05, C["yellow"], axis=(0, 1, 0), seg=14, mseg=4))
        P.append(gs.capsule(f"Handle{s}", (3.6 * s, 0, 1.55), (3.6 * s, -0.02, 1.65), 0.05, C["chrome"], seg=5, sub=1, cap=1))
        P.append(gs.ellipsoid(f"Light{s}", (3.6 * s, -0.4, 0.95), 0.1, C["glow_c"], seg=6, rings=4))
    P.append(gs.tube("Tether", [(-3.6, 0, 0.12), (0, 0, 0.08), (3.6, 0, 0.12)], 0.04, C["rope"], seg=4, sub=3, cap=1))
    cols = [M("#C4F1FF", 0.2), M("#FFDDF2", 0.2), M("#E2FBC8", 0.2)]
    bubbles = []
    i = 0
    for r in range(3):
        for c in range(7):
            x = -3.0 + c * 1.0 + (0.5 if r % 2 else 0.0) - (0.25 if r % 2 else 0.0)
            z = 0.85 + r * 0.95
            bubbles.append(bubble(f"Bubble_{i:02d}", (x, 0, z), 0.52 + 0.04 * ((c + r) % 3), cols[(c + r) % 3], C))
            i += 1
    objs = [k.join("Static", P)] + bubbles
    n = sum(gs.tri_count(o) for o in objs)
    root = bpy.data.objects.new(name, None)
    gs._link(root)
    for o in objs:
        o.parent = root
    gs.export_fbx([root] + objs, os.path.join(H.OUT, name + ".fbx"), animated=False)
    sc, cam = gs._setup_render(H.TILE)
    tile = os.path.join(gs.PREVIEW_DIR, f"_tmp_haz_{name}.png")
    gs._render_view(tile, objs, (0.3, -1, 0.2), cam, margin=1.08)
    S.tiles.append(tile)
    lo, hi = gs._world_bbox(objs)
    d = hi - lo
    if n > 3000:
        S.over.append(f"{name}: {n}")
    S.report.append(f"| {name}.fbx | {n} | {d.x:.1f} x {d.y:.1f} x {d.z:.1f} m | Bubble_00..{i - 1:02d} |")


# ---------------------------------------------------------------------------
# Boyas de lección: boya de giro (lección 3), 3 boyas de ruta numeradas por puntos y boya de inicio de zona
# ---------------------------------------------------------------------------
def buoy_body(C, body, band, r=0.7, h=1.6):
    prof = [(0.0, -0.4), (r * 0.8, -0.4), (r, -0.1), (r * 0.95, h * 0.35), (r * 0.62, h * 0.7), (r * 0.3, h * 0.95), (0.0, h)]
    b = gs.lathe("Body", prof, [body, band], seg=16, mat_fn=lambda i: 1 if i in (1, 2, 4) else 0)
    return b


def lesson_buoy_turn(S, name):
    C = tpal()
    P = [buoy_body(C, C["orange"], C["white"], 0.95, 2.4),
         gs.torus("Collar", (0, 0, 0.05), 0.98, 0.14, C["navy"], seg=20, mseg=6),
         gs.capsule("Mast", (0, 0, 2.2), (0, 0, 3.4), 0.07, C["chrome"], seg=6, sub=1, cap=1),
         gs.ellipsoid("Lamp", (0, 0, 3.55), (0.2, 0.2, 0.22), C["glow_y"], seg=10, rings=6),
         gs.torus("Base", (0, 0, -0.35), 0.8, 0.06, C["chrome"], seg=14, mseg=4)]
    for i in range(3):
        a = i / 3 * 2 * math.pi
        P.append(gs.torus(f"Eye{i}", (1.0 * math.cos(a), 1.0 * math.sin(a), 0.9), 0.1, 0.03, C["chrome"], axis=(math.cos(a), math.sin(a), 0), seg=8, mseg=3))
    flag = [gs.rounded_box("FlagPanel", (0.6, -0.02, 3.05), (1.0, 0.06, 0.6), 0.02, [C["paper"]], segments=1)]
    flag += [gs.capsule("FlagBar", (0.04, 0, 3.4), (0.04, 0, 2.7), 0.025, C["chrome"], seg=4, sub=1, cap=1)]
    H.finish(S, name, P, moving=[("Flag", (0.04, 0, 3.05), flag)], view=(0.5, -0.85, 0.3))


def lesson_buoy_marker(S, name, n):
    C = tpal()
    cols = [C["red"], C["yellow"], C["lime"]][n - 1]
    P = [buoy_body(C, cols, C["white"], 0.55, 1.3),
         gs.torus("Collar", (0, 0, 0.05), 0.58, 0.09, C["navy"], seg=16, mseg=5),
         gs.torus("Ring", (0, 0, 1.55), 0.3, 0.05, C["chrome"], seg=14, mseg=4),
         gs.capsule("Post", (0, 0, 1.2), (0, 0, 1.5), 0.05, C["chrome"], seg=5, sub=1, cap=1)]
    # n puntos en relieve sobre la banda frontal (1, 2 o 3) para distinguir el orden sin texto
    xs = {1: [0.0], 2: [-0.16, 0.16], 3: [-0.24, 0.0, 0.24]}[n]
    for i, x in enumerate(xs):
        P.append(gs.ellipsoid(f"Pip{i}", (x, -0.56, 0.5), (0.08, 0.05, 0.08), C["navy"], seg=6, rings=4))
    P.append(gs.ellipsoid("Lamp", (0, 0, 1.75), (0.14, 0.14, 0.14), [C["glow_p"], C["glow_y"], C["glow_c"]][n - 1], seg=8, rings=5))
    H.finish(S, name, P, view=(0.5, -0.9, 0.3))


def lesson_buoy_zone(S, name):
    C = tpal()
    P = [buoy_body(C, C["pink"], C["white"], 0.8, 1.8),
         gs.torus("Collar", (0, 0, 0.05), 0.84, 0.12, C["navy"], seg=18, mseg=5),
         gs.capsule("Mast", (0, 0, 1.6), (0, 0, 2.3), 0.06, C["chrome"], seg=6, sub=1, cap=1)]
    star = k.extrude("Star", [((1.0 if i % 2 == 0 else 0.45) * 0.42 * math.cos(math.pi / 2 + i * math.pi / 5),
                              2.75 + (1.0 if i % 2 == 0 else 0.45) * 0.42 * math.sin(math.pi / 2 + i * math.pi / 5)) for i in range(10)],
                     -0.07, 0.07, [C["glow_y"]])
    spin = [star]
    H.finish(S, name, P, moving=[("Star", (0, 0, 2.75), spin)], view=(0.5, -0.9, 0.3))


PROPS = [("Tutorial_BigSign", big_sign), ("Tutorial_PostSign", post_sign), ("Tutorial_ArrowSign", arrow_sign),
         ("Tutorial_HangingSign", hanging_sign), ("Tutorial_BubbleBarrier", bubble_barrier),
         ("Tutorial_LessonBuoy_Turn", lesson_buoy_turn),
         ("Tutorial_LessonBuoy_Marker1", lambda S, n: lesson_buoy_marker(S, n, 1)),
         ("Tutorial_LessonBuoy_Marker2", lambda S, n: lesson_buoy_marker(S, n, 2)),
         ("Tutorial_LessonBuoy_Marker3", lambda S, n: lesson_buoy_marker(S, n, 3)),
         ("Tutorial_LessonBuoy_Zone", lesson_buoy_zone)]

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    H.run(PROPS, only=set(argv) or None, sheet_name="Tutorial", cols=5)
