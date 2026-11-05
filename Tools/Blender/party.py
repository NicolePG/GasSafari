"""Arenas y objetos de los minijuegos de Fiesta (docs/06 §1): mini balsa individual (radio 1 m, 4 cámaras separadas),
pelota de playa gigante y porterías (Balsa-Gol), piscina redonda giratoria (Rey de la Balsa), coco-bomba con mecha
(Coco Caliente) y pista modular de obstáculos (Carrera de Burbujas).
blender --background --python party.py [-- MiniRaft BeachBall ...]
Salida: Tools/Export/Party/<Nombre>.fbx (≤ 3000 triángulos, pivote en la base, agua en z = 0, frente -Y,
el cauce de la pista avanza hacia +Y como los canales de W1) y previews/Party_sheet.png.
Las piezas con partes móviles exportan un vacío raíz con 'Static' y cada parte con su pivote."""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import bmesh
import gs_common as gs
import env_kit as k
from gs_common import Vector, Matrix
from env_kit import M, E

OUT = os.path.join(gs.EXPORT_DIR, "Party")
J_COLORS = ["#E69F00", "#56B4E9", "#F0E442", "#CC79A7"]  # docs/07: paleta de jugadores para daltonismo


def pal():
    return dict(mag=M("#F72585"), mag2=M("#B5179E"), lime=M("#B5E48C"), lime2=M("#76C442"), violet=M("#4A1D7A"),
                night=M("#1B1038"), night2=M("#2D1B5E"), cyan=M("#4CC9F0"), yellow=M("#FFD60A"), gold=M("#F5C431", 0.35),
                gold2=M("#C8931C", 0.4), white=M("#F7F4EC"), black=M("#1E1E24"), metal=M("#B9C2CC", 0.3),
                steel=M("#6B7280", 0.4), wood=M("#A0703F"), wood2=M("#6B4A2A"), orange=M("#FF7A1A"), dorange=M("#C9520A"),
                rope=M("#D9B77A", 0.9), blue=M("#3A86FF"), red=M("#FF5A36"), brown=M("#6B4423"), brown2=M("#4A2E16"),
                floor=M("#3E4A57", 0.8), concrete=M("#C9C3D8"), concrete2=M("#8F88A8"), sand=M("#F2D492"),
                glow_mag=E("#F72585", 3.5), glow_lime=E("#B5E48C", 3.0), glow_cyan=E("#4CC9F0", 3.0),
                glow_yellow=E("#FFD60A", 3.5), glow_orange=E("#FF9F1C", 4.0), glow_white=E("#FFFFFF", 3.5),
                water=M("#3AB4E0", 0.1))


class Sheet:
    def __init__(self):
        self.tiles, self.report, self.over = [], [], []

    def stitch(self, cols=7):
        out = os.path.join(gs.PREVIEW_DIR, "Party_sheet.png")
        gs.stitch(self.tiles, out, cols=cols)
        for p in self.tiles:
            os.remove(p)
        print(f"[gs] preview {out}")
        for line in self.report:
            print(line)
        assert not self.over, f"exceden {k.MAX_TRIS} triángulos: {self.over}"
        return out


def fin(S, name, static, moving=None, view=(0.8, -0.75, 0.45), margin=1.12, named=None):
    """static: piezas fijas (lista) o, con named, dict {nombre_objeto: piezas}. moving: [(nombre, pivote, piezas)]."""
    st = k.join("Static" if (moving or named) else name, static) if not named else None
    objs = [st] if st else []
    for nm, parts in (named or {}).items():
        o = k.join(nm, parts)
        objs.append(o)
    for mname, pivot, parts in moving or []:
        o = k.join(mname, parts)
        k.set_origin(o, pivot)
        objs.append(o)
    tris = sum(gs.tri_count(o) for o in objs)
    if tris > k.MAX_TRIS:
        S.over.append(f"{name}: {tris}")
    export = objs
    if len(objs) > 1:
        root = bpy.data.objects.new(name, None)
        gs._link(root)
        for o in objs:
            o.parent = root
        export = [root] + objs
    gs.export_fbx(export, os.path.join(OUT, name + ".fbx"), animated=False)
    sc, cam = gs._setup_render(k.TILE)
    tile = os.path.join(gs.PREVIEW_DIR, f"_tmp_party_{name}.png")
    gs._render_view(tile, objs, view, cam, margin=margin)
    S.tiles.append(tile)
    lo, hi = gs._world_bbox(objs)
    size = hi - lo
    S.report.append(f"[gs] {name}: {tris} tris, {size.x:.1f}x{size.y:.1f}x{size.z:.1f} m"
                    + (f", móviles: {', '.join(m[0] for m in moving)}" if moving else ""))


def ring_lathe(name, prof, mats, seg=36, mat_fn=None, loc=(0, 0, 0), smooth=False):
    """Anillo sólido (perfil cerrado [(r, z)] girado 360°), sin tapas: sirve para paredes de piscina y bordes."""
    bm = bmesh.new()
    rings = [[bm.verts.new((r * math.cos(s / seg * 2 * math.pi), r * math.sin(s / seg * 2 * math.pi), z))
              for s in range(seg)] for r, z in prof]
    n = len(prof)
    for i in range(n):
        a, b = rings[i], rings[(i + 1) % n]
        for s in range(seg):
            f = bm.faces.new((a[s], b[s], b[(s + 1) % seg], a[(s + 1) % seg]))
            f.material_index = mat_fn(i) if mat_fn else 0
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    return gs._obj_from_bm(name, bm, mats, smooth=smooth)


def disc(name, loc, r, h, mat, axis=(0, 0, 1), seg=10):
    return gs.lathe(name, [(0.0, 0.0), (r, 0.0), (r, h), (0.0, h)], mat, loc=loc, axis=axis, seg=seg)


# ---------------------------------------------------------------------------
# Mini balsa individual (radio 1 m, 4 cámaras separadas)
# ---------------------------------------------------------------------------
QUADS = [("0_NE", 180, 270), ("1_NO", 270, 360), ("2_SE", 90, 180), ("3_SO", 0, 90)]


def mini_raft(S, name, color=None, marker=None):
    """Mismo orden y pivotes que la balsa grande (Chamber_0_NE … 3_SO), a mitad de tamaño. En Blender norte = -Y
    y este = -X. color = cámaras; marker = forma de jugador sobre el piso (círculo, triángulo, cuadrado, rombo)."""
    C = pal()
    body = M(color, 0.45) if color else gs.mat("raft_orange", rough=0.45)
    dark = gs.mat("raft_dark", rough=0.5)
    rope, floor_m = C["rope"], C["floor"]
    R, r, zc = 0.85, 0.15, 0.15
    named = {}
    moving = []
    for qn, a0, a1 in QUADS:
        pts = [Vector((R * math.cos(math.radians(a)), R * math.sin(math.radians(a)), zc))
               for a in [a0 + 5 + (a1 - a0 - 10) * i / 8 for i in range(9)]]
        ch = gs.tube(f"Chamber_{qn}", pts, r, [body], seg=8, cap=2, sub=1)
        am = math.radians((a0 + a1) / 2)
        valve = gs.rounded_cylinder(f"Valve_{qn}", Vector((math.cos(am) * (R + 0.06), math.sin(am) * (R + 0.06), zc + 0.12)),
                                    0.03, 0.04, 0.012, dark, seg=8, axis=(math.cos(am) * 0.4, math.sin(am) * 0.4, 1), steps=1)
        moving.append((f"Chamber_{qn}", (math.cos(am) * R * 0.92, math.sin(am) * R * 0.92, zc), [ch, valve]))
    fl = [gs.rounded_cylinder("Floor", (0, 0, 0.06), 0.81, 0.06, 0.02, floor_m, seg=20, steps=1)]
    fl.append(gs.lathe("Arrow", [(0.0, 0.0), (0.09, 0.0), (0.09, 0.008), (0.0, 0.008)], dark, loc=(0, -0.62, 0.092), seg=3,
                       rot=("Z", -90)))
    if marker:
        mk = M("#F7F4EC")
        z = 0.1
        if marker == "circle":
            fl.append(disc("MkC", (0, 0, z), 0.2, 0.012, mk, seg=16))
        elif marker == "triangle":
            fl.append(disc("MkT", (0, 0, z), 0.24, 0.012, mk, seg=3))
        elif marker == "square":
            fl.append(gs.rounded_box("MkS", (0, 0, z + 0.006), (0.34, 0.34, 0.012), 0.005, mk, segments=1))
        else:
            fl.append(gs.rounded_box("MkD", (0, 0, z + 0.006), (0.3, 0.3, 0.012), 0.005, mk, segments=1, rot=("Z", 45)))
    named["Raft_Floor"] = fl
    # cuerda con caídas y anclajes
    n_anchor, sub = 8, 4
    path = []
    for i in range(n_anchor * sub + 1):
        t = i / (n_anchor * sub)
        a = t * 2 * math.pi + math.radians(20)
        sag = math.sin((i % sub) / sub * math.pi)
        rad = R + r + 0.02 + 0.03 * sag
        path.append(Vector((rad * math.cos(a), rad * math.sin(a), zc + 0.07 - 0.07 * sag)))
    rp = [gs.tube("Rope", path, 0.014, rope, seg=4, cap=0, sub=1)]
    for kk in range(n_anchor):
        a = kk / n_anchor * 2 * math.pi + math.radians(20)
        rp.append(gs.ellipsoid(f"Anchor{kk}", Vector(((R + r + 0.005) * math.cos(a), (R + r + 0.005) * math.sin(a), zc + 0.07)),
                               (0.03, 0.03, 0.025), dark, seg=5, rings=3))
    named["Raft_Rope"] = rp
    fin(S, name, None, moving=moving, named=named, view=(0.75, -0.8, 0.6), margin=1.1)


# ---------------------------------------------------------------------------
# Balsa-Gol: pelota de playa gigante, porterías y arena
# ---------------------------------------------------------------------------
def beach_ball(S, name):
    """Pelota de playa gigante (radio 1.2 m, pivote en el centro): 6 gajos de colores y polos blancos."""
    C = pal()
    cols = [C["mag"], C["white"], C["lime"], C["white"], C["cyan"], C["yellow"]]
    b = gs.ellipsoid("Ball", (0, 0, 0), 1.2, cols, seg=24, rings=14)
    gs.paint_faces(b, lambda c: True, 0)
    for f in b.data.polygons:
        a = math.atan2(f.center.y, f.center.x)
        f.material_index = int(((a + math.pi) / (2 * math.pi)) * 6) % 6
    P = [b]
    for s in (1, -1):
        P.append(disc(f"Pole{s}", (0, 0, 1.17 * s - (0.0 if s == 1 else 0.07)), 0.2, 0.07, C["white"], seg=10))
        P.append(gs.torus(f"PoleRing{s}", (0, 0, 1.2 * s), 0.2, 0.025, C["gold"], seg=10, mseg=4))
    P.append(gs.rounded_cylinder("Valve", (0.5, 0.0, 1.1), 0.07, 0.12, 0.02, C["yellow"], seg=8, steps=1, axis=(0.5, 0, 1)))
    fin(S, name, P, view=(0.5, -0.9, 0.35))


def goal(S, name, team):
    """Portería flotante de 6 m de ancho y 2.6 m de alto sobre el agua, red al fondo (profundidad 2.4 m), pontones
    y banderines del color del equipo. Abre hacia -Y; pivote en el centro de la línea de gol, agua en z = 0."""
    C = pal()
    c, c2 = (C["red"], C["orange"]) if team == "A" else (C["blue"], C["cyan"])
    Wd, H, D = 6.0, 2.6, 2.4
    P = []
    for s in (1, -1):
        P.append(gs.capsule(f"Post{s}", (Wd / 2 * s, 0, -0.3), (Wd / 2 * s, 0, H), 0.13, [c], seg=8, cap=2, sub=1))
        P.append(gs.capsule(f"Back{s}", (Wd / 2 * s, D, -0.3), (Wd / 2 * s, D, 1.4), 0.09, [c2], seg=6, cap=1, sub=1))
        P.append(gs.capsule(f"Slant{s}", (Wd / 2 * s, 0, H), (Wd / 2 * s, D, 1.4), 0.07, [c2], seg=5, cap=1, sub=1))
        P.append(gs.ellipsoid(f"Ball{s}", (Wd / 2 * s, 0, H + 0.2), 0.2, C["gold"], seg=8, rings=5))
        P.append(gs.rounded_cylinder(f"Pontoon{s}", (Wd / 2 * s, D / 2, 0.0), 0.28, D + 0.4, 0.1, C["white"], axis=(0, 1, 0), seg=10,
                                     steps=1))
        P.append(gs.torus(f"PontoonBand{s}", (Wd / 2 * s, D * 0.25, 0.0), 0.28, 0.04, c, axis=(0, 1, 0), seg=10, mseg=4))
        P.append(gs.torus(f"PontoonBand2{s}", (Wd / 2 * s, D * 0.75, 0.0), 0.28, 0.04, c, axis=(0, 1, 0), seg=10, mseg=4))
    P.append(gs.capsule("Bar", (-Wd / 2, 0, H), (Wd / 2, 0, H), 0.13, [c], seg=8, cap=2, sub=1))
    P.append(gs.capsule("BackBar", (-Wd / 2, D, 1.4), (Wd / 2, D, 1.4), 0.07, [c2], seg=6, cap=1, sub=1))
    # red: cuerdas verticales y horizontales al fondo y en el techo
    for i in range(0, 13):
        x = -Wd / 2 + Wd * i / 12
        P.append(gs.capsule(f"NetV{i}", (x, D, -0.1), (x, D, 1.4), 0.012, C["white"], seg=3, cap=0, sub=1))
        P.append(gs.capsule(f"NetT{i}", (x, 0, H), (x, D, 1.4), 0.012, C["white"], seg=3, cap=0, sub=1))
    for j in range(0, 5):
        z = -0.1 + 1.5 * j / 4
        P.append(gs.capsule(f"NetH{j}", (-Wd / 2, D, z), (Wd / 2, D, z), 0.012, C["white"], seg=3, cap=0, sub=1))
        for s in (1, -1):
            P.append(gs.capsule(f"NetS{j}{s}", (Wd / 2 * s, 0.0, z * (H / 1.4) if z > 0 else z), (Wd / 2 * s, D, z), 0.012, C["white"], seg=3,
                                cap=0, sub=1))
    # banderines del equipo colgando del travesaño
    for i in range(8):
        x = -Wd / 2 + 0.4 + (Wd - 0.8) * i / 7
        f = k.extrude(f"Flag{i}", [(-0.16, 0.0), (0.16, 0.0), (0.0, -0.4)], -0.012, 0.012, [c if i % 2 else C["white"]])
        f.data.transform(Matrix.Translation(Vector((x, -0.15, H - 0.12))))
        P.append(f)
    P.append(gs.rounded_box("Goalline", (0, 0.02, 0.03), (Wd, 0.1, 0.04), 0.01, C["glow_white"], segments=1))
    fin(S, name, P, view=(0.45, -1, 0.35), margin=1.1)


def goal_arena(S, name):
    """Arena de Balsa-Gol: piscina rectangular de 36 x 22 m con muros acolchados, borde con luces, línea y círculo de
    saque flotantes y cuatro antorchas en las esquinas. Dentro del muro caben las porterías (Goal_A / Goal_B)."""
    C = pal()
    L, Wd = 36.0, 22.0
    P = []
    h, t = 1.3, 1.2
    walls = [("N", (0, -Wd / 2 - t / 2, 0), (L + 2 * t, t)), ("S", (0, Wd / 2 + t / 2, 0), (L + 2 * t, t)),
             ("E", (-L / 2 - t / 2, 0, 0), (t, Wd)), ("W", (L / 2 + t / 2, 0, 0), (t, Wd))]
    for nm, (x, y, _), (sx, sy) in walls:
        w = gs.rounded_box(f"Wall{nm}", (x, y, (h - 1.6) / 2), (sx, sy, h + 1.6), 0.2, [C["night2"], C["mag"]], segments=1)
        gs.paint_faces(w, lambda c: c.z > h - 0.5, 1)
        P.append(w)
    P.append(gs.rounded_box("Floor", (0, 0, -1.65), (L, Wd, 0.1), 0.02, C["floor"], segments=1))
    # cubierta exterior (deck) en cuatro tiras
    for nm, (x, y), (sx, sy) in (("N", (0, -Wd / 2 - t - 1.5), (L + 2 * t + 6, 3.0)), ("S", (0, Wd / 2 + t + 1.5), (L + 2 * t + 6, 3.0)),
                                 ("E", (-L / 2 - t - 1.5, 0), (3.0, Wd)), ("W", (L / 2 + t + 1.5, 0), (3.0, Wd))):
        P.append(gs.rounded_box(f"Deck{nm}", (x, y, h - 0.35), (sx, sy, 0.5), 0.08, C["concrete"], segments=1))
    P.append(gs.rounded_box("Midline", (0, 0, 0.02), (0.3, Wd - 0.2, 0.05), 0.01, C["glow_white"], segments=1))
    P.append(gs.torus("Center", (0, 0, 0.02), 4.0, 0.12, C["glow_white"], seg=36, mseg=4))
    for sx in (1, -1):
        P.append(gs.rounded_box(f"Crease{sx}", (sx * (L / 2 - 4.0), 0, 0.02), (0.2, 12.0, 0.05), 0.01, C["glow_lime"] if sx > 0 else C["glow_mag"],
                                segments=1))
    for sx in (1, -1):
        for sy in (1, -1):
            x, y = sx * (L / 2 + t + 0.3), sy * (Wd / 2 + t + 0.3)
            P += [gs.capsule(f"Torch{sx}{sy}", (x, y, h - 0.1), (x, y, h + 2.8), 0.1, C["wood2"], seg=6, cap=1, sub=1),
                             gs.lathe(f"Bowl{sx}{sy}", [(0.0, 0.0), (0.1, 0.0), (0.24, 0.2), (0.0, 0.2)], C["mag2"], loc=(x, y, h + 2.8), seg=8),
                             gs.lathe(f"Flame{sx}{sy}", [(0.0, 0.0), (0.15, 0.02), (0.07, 0.4), (0.0, 0.6)], C["glow_orange"],
                                      loc=(x, y, h + 2.95), seg=6)]
    for i in range(10):  # luces en el borde de los muros largos
        for sy in (1, -1):
            P.append(gs.ellipsoid(f"Lt{i}{sy}", (-L / 2 + 2 + (L - 4) * i / 9, sy * (Wd / 2 - 0.1), h - 0.1), 0.12,
                                  C["glow_lime"] if i % 2 else C["glow_mag"], seg=6, rings=4))
    fin(S, name, P, view=(0.4, -1, 0.75), margin=1.0)


def royal_pool(S, name):
    """Piscina redonda del Rey de la Balsa: agua de 7 m de radio, muro de 1 m, deck de 3 m, cuatro escaleras, farolas
    y una rueda de corriente (Rotor, bajo el agua, con aspas curvas y flechas) que se hace girar por código."""
    C = pal()
    Ri = 7.0
    wall = ring_lathe("Wall", [(Ri, -1.2), (Ri + 1.2, -1.2), (Ri + 1.2, 1.0), (Ri + 0.7, 1.15), (Ri + 0.15, 1.15), (Ri, 1.0)],
                      [C["night2"], C["mag"], C["lime"]], seg=32, mat_fn=lambda i: 0)
    gs.paint_faces(wall, lambda c: c.z > 1.05, 1)
    P = [wall]
    P.append(ring_lathe("Deck", [(Ri + 1.2, 0.2), (Ri + 4.4, 0.2), (Ri + 4.4, 0.9), (Ri + 1.2, 0.9)], [C["concrete"]], seg=32))
    P.append(gs.lathe("Floor", [(0.0, -1.2), (Ri, -1.2), (Ri, -1.15), (0.0, -1.15)], C["floor"], seg=32))
    for i in range(4):  # escalerillas cromadas
        a = (i / 4 + 0.125) * 2 * math.pi
        c, s = math.cos(a), math.sin(a)
        for dx in (-0.35, 0.35):
            x, y = (Ri + 0.4) * c - s * dx, (Ri + 0.4) * s + c * dx
            P.append(gs.tube(f"Lad{i}{dx}", [(x * 0.97, y * 0.97, -0.5), (x * 0.97, y * 0.97, 1.7), (x * 1.0, y * 1.0, 1.9)], 0.04, C["metal"],
                             seg=5, cap=1, sub=1))
        for j in range(3):
            P.append(gs.capsule(f"Rung{i}{j}", ((Ri + 0.4) * c - s * 0.35, (Ri + 0.4) * s + c * 0.35, -0.4 + 0.5 * j),
                                ((Ri + 0.4) * c + s * 0.35, (Ri + 0.4) * s - c * 0.35, -0.4 + 0.5 * j), 0.03, C["metal"], seg=4, cap=0, sub=1))
    for i in range(8):  # farolas de fiesta en el deck
        a = (i + 0.5) / 8 * 2 * math.pi
        x, y = (Ri + 3.6) * math.cos(a), (Ri + 3.6) * math.sin(a)
        P.append(gs.capsule(f"Lamp{i}", (x, y, 0.9), (x, y, 3.6), 0.08, C["wood2"], seg=5, cap=1, sub=1))
        P.append(gs.ellipsoid(f"LampGlow{i}", (x, y, 3.85), (0.24, 0.24, 0.3), (C["glow_mag"], C["glow_lime"])[i % 2], seg=8, rings=5))
    for i in range(16):  # luces en el borde del muro
        a = (i + 0.5) / 16 * 2 * math.pi
        P.append(gs.ellipsoid(f"Rim{i}", ((Ri + 0.42) * math.cos(a), (Ri + 0.42) * math.sin(a), 1.2), 0.09,
                              (C["glow_lime"], C["glow_mag"])[i % 2], seg=5, rings=3))
    # rotor: cuatro aspas curvas, aro exterior y flechas de corriente en la superficie
    rot = [gs.lathe("Hub", [(0.0, -0.05), (0.7, -0.05), (0.55, 0.12), (0.0, 0.18)], C["gold"], loc=(0, 0, -0.18), seg=16),
           gs.torus("Rim", (0, 0, -0.1), 5.9, 0.12, C["mag"], seg=30, mseg=4)]
    for i in range(4):
        a0 = i / 4 * 2 * math.pi
        pts = [Vector(((0.6 + 5.3 * u) * math.cos(a0 + 1.1 * u), (0.6 + 5.3 * u) * math.sin(a0 + 1.1 * u), -0.12)) for u in (0, 0.25, 0.5, 0.75, 1.0)]
        rot.append(gs.tube(f"Blade{i}", pts, [0.14, 0.12, 0.11, 0.1, 0.09], C["lime2"] if i % 2 else C["mag2"], seg=6, cap=1, sub=1,
                           flatten=((0, 0, 1), 0.35)))
        mid = Vector(((3.4) * math.cos(a0 + 0.65), 3.4 * math.sin(a0 + 0.65), -0.02))
        d = Vector((-math.sin(a0 + 0.65), math.cos(a0 + 0.65), 0))
        rot.append(gs.cone(f"Arrow{i}", mid - d * 0.4, mid + d * 0.5, 0.35, 0.02, C["glow_cyan"], seg=3, round_tip=False))
    fin(S, name, P, moving=[("Rotor", (0, 0, -0.1), rot)], view=(0.3, -1, 0.85), margin=1.0)


# ---------------------------------------------------------------------------
# Coco Caliente
# ---------------------------------------------------------------------------
def coconut_bomb(S, name):
    """Coco-bomba (Ø 0.6 m, pivote en el centro): cáscara peluda con tres ojos, aro magenta, casquete de metal y una
    mecha de cuerda con chispa (Spark, pivote en la punta) para escalarla/animarla."""
    C = pal()
    body = gs.ellipsoid("Coco", (0, 0, 0), (0.3, 0.3, 0.32), [C["brown"]], seg=14, rings=9)
    rnd = random.Random(4)
    for v in body.data.vertices:
        v.co *= 1 + 0.025 * rnd.uniform(-1, 1)
    P = [body]
    for i, (x, z) in enumerate(((-0.1, 0.05), (0.1, 0.05), (0.0, -0.1))):
        P.append(gs.ellipsoid(f"Eye{i}", (x, -0.285 + abs(x) * 0.15, z), (0.05, 0.02, 0.05), C["brown2"], seg=7, rings=4))
    P.append(gs.torus("Band", (0, 0, 0.0), 0.31, 0.035, C["mag"], seg=16, mseg=5))
    P.append(gs.torus("Band2", (0, 0, 0.14), 0.285, 0.03, C["lime"], seg=16, mseg=5))
    P.append(gs.lathe("Cap", [(0.0, 0.28), (0.13, 0.28), (0.1, 0.34), (0.06, 0.36), (0.0, 0.36)], C["metal"], seg=10))
    P.append(gs.torus("CapRing", (0, 0, 0.3), 0.13, 0.025, C["gold"], seg=10, mseg=4))
    path = [Vector(p) for p in ((0, 0, 0.35), (0.03, 0, 0.5), (0.12, 0, 0.62), (0.2, 0, 0.7), (0.16, 0, 0.8), (0.08, 0, 0.84))]
    P.append(gs.tube("Fuse", path, 0.022, C["rope"], seg=5, cap=1, sub=3))
    tip = path[-1] + Vector((0, 0, 0.03))
    spark = [gs.ellipsoid("Core", tip, (0.06, 0.06, 0.06), C["glow_yellow"], seg=6, rings=4)]
    for i in range(6):
        a = i / 6 * 2 * math.pi
        d = Vector((math.cos(a), 0.0, math.sin(a)))
        spark.append(gs.cone(f"Ray{i}", tip + d * 0.04, tip + d * 0.17, 0.035, 0.005, C["glow_orange"], seg=4, round_tip=False))
    fin(S, name, P, moving=[("Spark", tuple(tip), spark)], view=(0.6, -1, 0.3), margin=1.15)


# ---------------------------------------------------------------------------
# Carrera de Burbujas: pista modular (cauce de 8 m, agua en z = 0, avanza hacia +Y)
# ---------------------------------------------------------------------------
HALF = 4.0


def chan_profile(half=HALF, wall=0.7, top=1.3, depth=1.4):
    o = half + wall
    return [(-o, top), (-half, top), (-half, -depth), (half, -depth), (half, top), (o, top), (o, -depth - 0.4), (-o, -depth - 0.4)]


def track_piece(S, name, kind):
    """Cauce recto de 10 m, curva de 90° (radio 15 m); lleva franjas magenta y lima en el borde y flechas de avance."""
    C = pal()
    if kind == "straight":
        path = [(0, -5 + i, 0) for i in range(11)]
        cen = None
    else:
        path = [Vector((p.x, p.y, 0)) for p in k.arc_path((-15, 0, 0), 15, 0, 90, 16)]
    o = k.sweep("Track", chan_profile(), path, [C["mag"], C["lime"], C["floor"], C["night2"]],
                mat_fn=lambda j: (0, 1, 2, 1, 0, 3, 3, 3)[j])
    # franjas alternas en el borde y en las paredes interiores
    for f in o.data.polygons:
        if f.material_index == 0 or f.material_index == 1:
            ctr = f.center
            if kind == "straight":
                u = ctr.y + 5
            else:
                u = math.atan2(ctr.y, ctr.x + 15) * 4
            if int(math.floor(u)) % 2 == 0:
                f.material_index = 3 if f.material_index == 1 else 1
    P = [o]
    pts = [Vector(p) for p in path]
    step = 3 if kind == "straight" else 4
    for i in range(1, len(pts) - 1, step):
        p = pts[i]
        t = (pts[i + 1] - pts[i - 1]).normalized()
        side = t.cross(Vector((0, 0, 1))).normalized()
        a = gs.ellipsoid(f"Dot{i}", p + Vector((0, 0, -1.3)), (0.4, 0.4, 0.04), C["glow_cyan"], seg=6, rings=3)
        P.append(a)
        for s in (1, -1):  # luces en lo alto del muro
            P.append(gs.ellipsoid(f"Lamp{i}{s}", p + side * s * (HALF + 0.35) + Vector((0, 0, 1.4)), 0.12,
                                  C["glow_lime"] if (i // step) % 2 else C["glow_mag"], seg=6, rings=4))
    fin(S, name, P, view=(0.6, -0.9, 0.75), margin=1.05)


def track_start(S, name):
    """Arco de salida: dos columnas con aro de luces, 4 casillas de salida numeradas con formas (círculo, triángulo,
    cuadrado y rombo, como los jugadores) y semáforo de 3 luces (Light_0..2, pivote en cada lámpara)."""
    C = pal()
    P = []
    for s in (1, -1):
        P.append(gs.rounded_box(f"Col{s}", ((HALF + 1.4) * s, 0, 2.2), (1.0, 1.0, 5.0), 0.2, [C["night2"], C["mag"]], taper=(0.9, 0.9), segments=1))
        P.append(gs.ellipsoid(f"ColTop{s}", ((HALF + 1.4) * s, 0, 4.9), 0.45, C["gold"], seg=8, rings=5))
    P.append(gs.rounded_box("Beam", (0, 0, 5.0), (2 * HALF + 3.2, 1.0, 0.9), 0.2, [C["mag"], C["lime"]], segments=1))
    for i in range(9):
        P.append(gs.ellipsoid(f"Bulb{i}", (-HALF - 0.8 + (2 * HALF + 1.6) * i / 8, -0.55, 5.0), 0.12, C["glow_yellow"], seg=6, rings=4))
    cols = [C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["glow_yellow"]]
    for i in range(4):  # casillas
        x = -3.0 + 2.0 * i
        P.append(gs.rounded_box(f"Slot{i}", (x, 0.0, 0.03), (1.6, 3.0, 0.06), 0.02, C["night"], segments=1))
        P.append(gs.rounded_box(f"SlotLine{i}", (x, -1.4, 0.07), (1.6, 0.12, 0.03), 0.01, cols[i], segments=1))
        z = 0.08
        if i == 0:
            P.append(disc(f"Mk{i}", (x, 0.2, z), 0.4, 0.02, C["white"], seg=14))
        elif i == 1:
            P.append(disc(f"Mk{i}", (x, 0.2, z), 0.46, 0.02, C["white"], seg=3))
        elif i == 2:
            P.append(gs.rounded_box(f"Mk{i}", (x, 0.2, z + 0.01), (0.7, 0.7, 0.02), 0.01, C["white"], segments=1))
        else:
            P.append(gs.rounded_box(f"Mk{i}", (x, 0.2, z + 0.01), (0.62, 0.62, 0.02), 0.01, C["white"], segments=1, rot=("Z", 45)))
    P.append(gs.rounded_box("Bar", (0, 0, 0.05), (2 * HALF, 0.3, 0.1), 0.02, C["white"], segments=1))
    moving = []
    for i, col in enumerate((C["glow_mag"], C["glow_yellow"], C["glow_lime"])):
        pv = (-1.0 + 1.0 * i, -0.55, 3.9)
        moving.append((f"Light_{i}", pv, [disc(f"Ring{i}", (pv[0], pv[1] + 0.06, pv[2]), 0.36, 0.08, C["black"], axis=(0, -1, 0), seg=10),
                                          gs.ellipsoid(f"Lamp{i}", pv, (0.28, 0.1, 0.28), col, seg=10, rings=6)]))
    P.append(gs.rounded_box("LightBox", (0, -0.45, 3.9), (3.3, 0.3, 0.9), 0.1, C["night"], segments=1))
    fin(S, name, P, moving=moving, view=(0.4, -1, 0.3), margin=1.05)


def track_finish(S, name):
    """Arco de meta con banderín de cuadros (Flag) y cañones de confeti."""
    C = pal()
    P = []
    for s in (1, -1):
        P.append(gs.rounded_box(f"Col{s}", ((HALF + 1.4) * s, 0, 2.4), (1.0, 1.0, 5.4), 0.2, [C["night2"], C["lime"]], taper=(0.9, 0.9), segments=1))
        P.append(gs.rounded_cylinder(f"Cannon{s}", ((HALF + 1.4) * s, 0, 5.4), 0.28, 0.8, 0.06, C["mag"], seg=8, steps=1,
                                     axis=(-0.3 * s, 0, 1)))
    P.append(gs.rounded_box("Beam", (0, 0, 5.3), (2 * HALF + 3.2, 1.0, 1.1), 0.2, C["white"], segments=1))
    gs.bands_by_plane(P[-1], (1, 0, 0), (-HALF - 1.6, 0, 0), 1.1, 0.5, 0, cut=True)
    gs.bands_by_plane(P[-1], (0, 0, 1), (0, 0, 0), 1.1, 0.5, 0, cut=True)
    P[-1].data.materials.append(C["black"])
    for f in P[-1].data.polygons:
        f.material_index = 1 if (int(math.floor((f.center.x + HALF + 1.6) / 0.55)) + int(f.center.z // 0.55)) % 2 == 0 else 0
    # línea de meta de cuadros sobre el agua
    for i in range(16):
        for j in range(2):
            P.append(gs.rounded_box(f"Chk{i}{j}", (-HALF + 0.25 + 0.5 * i, -0.25 + 0.5 * j, 0.03), (0.5, 0.5, 0.05), 0.01,
                                    C["white"] if (i + j) % 2 else C["black"], segments=1))
    fl = [k.flag("Cloth", (0, 0, 6.1), 2.2, 1.2, [C["white"], C["black"]], waves=1.5, amp=0.15, th=0.04, n=8, stripes=5)]
    pole = gs.capsule("Pole", (0, 0, 5.8), (0, 0, 7.4), 0.07, C["gold2"], seg=5, cap=1, sub=1)
    P.append(pole)
    P.append(gs.ellipsoid("PoleTop", (0, 0, 7.5), 0.12, C["gold"], seg=6, rings=4))
    fin(S, name, P, moving=[("Flag", (0, 0, 6.1), fl)], view=(0.4, -1, 0.25), margin=1.05)


def track_gates(S, name):
    """Tres aros de burbujas (Ø 2.8 m) en arcos de cada pared, para pasar la balsa a través; posición en zigzag."""
    C = pal()
    P = []
    for i, (y, x) in enumerate(((-3.2, -1.6), (0.0, 1.6), (3.2, -1.6))):
        col = (C["glow_mag"], C["glow_lime"], C["glow_cyan"])[i]
        P.append(gs.torus(f"Hoop{i}", (x, y, 1.45), 1.4, 0.13, col, axis=(0, 1, 0), seg=24, mseg=8))
        P.append(gs.torus(f"HoopOuter{i}", (x, y, 1.45), 1.62, 0.05, C["white"], axis=(0, 1, 0), seg=24, mseg=4))
        sgn = 1 if x > 0 else -1
        P.append(gs.tube(f"Arm{i}", [(x + 1.4 * sgn, y, 1.45), (x + 1.9 * sgn, y, 2.1), (sgn * HALF, y, 2.2)], 0.06, C["steel"], seg=5,
                         sub=2, cap=1))
        P.append(gs.rounded_box(f"Mount{i}", (sgn * (HALF + 0.35), y, 1.7), (0.5, 0.5, 1.2), 0.1, C["mag2"], segments=1))
        for kk in range(5):
            a = kk / 5 * 2 * math.pi
            P.append(gs.ellipsoid(f"Bub{i}{kk}", (x + 1.4 * math.cos(a), y, 1.45 + 1.4 * math.sin(a)), 0.14, C["white"], seg=6, rings=4))
    fin(S, name, P, view=(0.7, -0.8, 0.45), margin=1.05)


def track_ramp(S, name):
    """Rampa de salto flotante de 4 m de ancho (sale del agua a 1.6 m en 5 m), con barandas y flechas luminosas."""
    C = pal()
    prof = [(-2.2, 0.0), (2.2, 0.0), (2.2, 0.35), (-2.2, 0.35)]
    path = [(0, -2.5 + 5.0 * i / 8, -0.5 + 2.1 * (i / 8) ** 1.7) for i in range(9)]
    deck = k.sweep("Deck", prof, path, [C["mag"], C["lime"]])
    for f in deck.data.polygons:
        if f.normal.z > 0.5 and int(math.floor((f.center.y + 2.5) / 0.62)) % 2 == 0:
            f.material_index = 1
    P = [deck]
    for s in (1, -1):
        P.append(gs.tube(f"Rail{s}", [Vector(p) + Vector((2.15 * s, 0, 0.55)) for p in path], 0.08, C["gold"], seg=6, cap=1))
        for j in (1, 4, 7):
            p = Vector(path[j])
            P.append(gs.capsule(f"Post{s}{j}", p + Vector((2.15 * s, 0, 0.3)), p + Vector((2.15 * s, 0, 0.62)), 0.07, C["gold2"], seg=5, cap=1, sub=1))
        P.append(gs.rounded_cylinder(f"Float{s}", (2.3 * s, 0, -0.3), 0.42, 5.2, 0.15, C["white"], axis=(0, 1, 0), seg=10, steps=1))
    for i in range(3):
        P.append(gs.cone(f"Arrow{i}", Vector(path[2 + 2 * i]) + Vector((0, -0.4, 0.4)), Vector(path[2 + 2 * i]) + Vector((0, 0.35, 0.5)), 0.5, 0.02,
                         C["glow_cyan"], seg=3, round_tip=False))
    fin(S, name, P, view=(0.7, -0.8, 0.4))


def track_spinner(S, name):
    """Barra giratoria (Arm, 6.4 m con bolas acolchadas) sobre un pilar central: hay que esquivar el giro."""
    C = pal()
    P = [gs.rounded_cylinder("Base", (0, 0, -0.6), 0.9, 1.6, 0.2, [C["night2"]], seg=14, steps=2),
         gs.rounded_cylinder("Collar", (0, 0, 0.25), 0.7, 0.2, 0.06, C["gold"], seg=14, steps=1)]
    P.append(gs.torus("Warn", (0, 0, 0.0), 0.92, 0.05, C["glow_yellow"], seg=16, mseg=4))
    arm = [gs.rounded_cylinder("Hub", (0, 0, 0.55), 0.5, 0.5, 0.1, C["mag"], seg=12, steps=1),
           gs.rounded_box("Bar", (0, 0, 0.55), (6.0, 0.35, 0.35), 0.1, [C["mag2"], C["lime"]], segments=1)]
    gs.bands_by_plane(arm[1], (1, 0, 0), (0, 0, 0), 1.0, 0.5, 1, cut=True)
    for s in (1, -1):
        arm.append(gs.ellipsoid(f"Bumper{s}", (3.2 * s, 0, 0.55), 0.55, C["glow_lime"] if s > 0 else C["glow_mag"], seg=12, rings=8))
        arm.append(gs.torus(f"BumperRing{s}", (3.2 * s, 0, 0.55), 0.56, 0.06, C["white"], seg=14, mseg=4))
    fin(S, name, P, moving=[("Arm", (0, 0, 0.55), arm)], view=(0.5, -0.9, 0.7), margin=1.05)


def track_slalom(S, name):
    """Cinco boyas de eslalon en zigzag (pivote en la línea de agua) unidas por una cuerda con banderines."""
    C = pal()
    P = []
    pts = []
    for i in range(5):
        x = (-2.0 if i % 2 == 0 else 2.0)
        y = -4.0 + 2.0 * i
        col = C["mag"] if i % 2 == 0 else C["lime2"]
        body = gs.lathe(f"Buoy{i}", [(0.0, -0.5), (0.5, -0.5), (0.62, -0.1), (0.5, 0.3), (0.3, 1.2), (0.12, 1.5), (0.0, 1.55)],
                        [col, C["white"]], loc=(x, y, 0), seg=12)
        gs.bands_by_plane(body, (0, 0, 1), (0, 0, -0.5), 0.5, 0.5, 1, cut=True)
        P.append(body)
        P.append(gs.ellipsoid(f"Light{i}", (x, y, 1.7), (0.16, 0.16, 0.2), C["glow_yellow"], seg=7, rings=5))
        P.append(gs.torus(f"Fender{i}", (x, y, 0.0), 0.64, 0.08, C["metal"], seg=14, mseg=5))
        pts.append(Vector((x, y, 1.0)))
    P.append(gs.tube("Line", [p + Vector((0, 0, 0.0)) for p in pts], 0.025, C["rope"], seg=4, cap=1, sub=3))
    fin(S, name, P, view=(0.7, -0.6, 0.5), margin=1.05)


def track_booster(S, name):
    """Plataforma de impulso flotante (pad 4 x 3 m) con tres flechas emisivas; la balsa recibe un empujón al pasar."""
    C = pal()
    P = [gs.rounded_box("Pad", (0, 0, 0.1), (4.0, 3.0, 0.2), 0.1, C["night"], segments=1),
         gs.rounded_box("Rim", (0, 0, 0.18), (4.2, 3.2, 0.1), 0.05, C["gold"], segments=1)]
    P.append(gs.rounded_box("PadTop", (0, 0, 0.2), (3.7, 2.7, 0.06), 0.03, C["night2"], segments=1))
    for i in range(3):
        y = -0.8 + 0.8 * i
        P.append(gs.cone(f"Arrow{i}", (0, y - 0.3, 0.26), (0, y + 0.35, 0.26), 0.7, 0.02, C["glow_lime"] if i % 2 else C["glow_cyan"], seg=3,
                         round_tip=False))
    for s in (1, -1):
        P.append(gs.rounded_cylinder(f"Float{s}", (2.2 * s, 0, 0.0), 0.28, 3.2, 0.1, C["white"], axis=(0, 1, 0), seg=10, steps=1))
        P.append(gs.ellipsoid(f"Lamp{s}", (2.0 * s, 1.3, 0.36), 0.12, C["glow_yellow"], seg=6, rings=4))
    fin(S, name, P, view=(0.7, -0.8, 0.7))


def build_props():
    props = [("MiniRaft", lambda S, n: mini_raft(S, n))]
    for i, (c, m) in enumerate(zip(J_COLORS, ("circle", "triangle", "square", "diamond"))):
        props.append((f"MiniRaft_J{i + 1}", (lambda c_, m_: lambda S, n: mini_raft(S, n, c_, m_))(c, m)))
    props += [("BeachBall", beach_ball), ("Goal_A", lambda S, n: goal(S, n, "A")), ("Goal_B", lambda S, n: goal(S, n, "B")),
              ("GoalArena", goal_arena), ("RoyalPool", royal_pool), ("CoconutBomb", coconut_bomb),
              ("Track_Straight", lambda S, n: track_piece(S, n, "straight")), ("Track_Curve90", lambda S, n: track_piece(S, n, "curve")),
              ("Track_Start", track_start), ("Track_Finish", track_finish), ("Track_Gates", track_gates),
              ("Track_Ramp", track_ramp), ("Track_Spinner", track_spinner), ("Track_Slalom", track_slalom),
              ("Track_Booster", track_booster)]
    return props


PROPS = build_props()

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    S = Sheet()
    for nm, fn in PROPS:
        if argv and nm not in argv:
            continue
        gs.reset_scene()
        fn(S, nm)
    S.stitch(7)
