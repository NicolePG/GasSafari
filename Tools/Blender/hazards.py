"""GAS SAFARI · modelos propios de obstáculos (docs/05) que usaban primitivas o no tenían modelo.
blender --background --python hazards.py [-- H33_BalloonWall H35_SinkingPlatform ...]
Salida: Tools/Export/Hazards/<Nombre>.fbx (≤ 3000 triángulos, pivote en la base, frente hacia -Y, agua en z = 0) y la
hoja de contacto Tools/Blender/previews/Hazards_sheet.png. Los props con partes móviles exportan un vacío raíz con
`Static` y cada parte móvil como objeto aparte con su pivote (`Gate`, `Sock`, `Spin`, `Balloon_00`...), igual que
Tools/Export/Environment. La lista de qué cubre cada modelo está en Tools/Export/Hazards/README.md."""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
import env_kit as k
from gs_common import Vector, Matrix
from env_kit import M, E

OUT = os.path.join(gs.EXPORT_DIR, "Hazards")
TILE = 300


# ---------------------------------------------------------------------------
# Paleta (la del zoológico de la piscina + acentos de cada mundo)
# ---------------------------------------------------------------------------
def pal():
    return dict(
        stone=M("#F2EFE6"), stone2=M("#B9C2CC"), stone_dk=M("#8F88A8"), tile=M("#4CC9F0"), pink=M("#F72585"),
        yellow=M("#FFD60A"), orange=M("#FF7A1A"), red=M("#E2394B"), white=M("#F7F4EC"), navy=M("#26304F"),
        wood=M("#9C6B3E"), wood2=M("#6B4A2A"), rope=M("#D9B77A", 0.9), chrome=M("#B9C2CC", 0.3),
        black=M("#1E1E24"), green=M("#3FA34D"), lime=M("#B5E48C"), blue=M("#3A86FF"), blue_dk=M("#1B5FBF"),
        water=M("#7FD8F5", 0.2), foam=M("#FFFFFF", 0.4), sand=M("#F2D492"), sand2=M("#E0B864"), bark=M("#7A4A26"),
        bark2=M("#4E3220"), moss=M("#6BBF59"), purple=M("#8A4FD6"), glow_y=E("#FFD60A", 3.0), glow_p=E("#F72585", 3.0),
        glow_c=E("#4CC9F0", 3.0), basalt=M("#3A3540"), lava=E("#FF8C00", 4.0), steam=M("#EAF6FF", 0.9),
        steam2=M("#BFE6F7", 0.8), shadow=M("#3B1D1D"), warn=M("#E2394B"))


def finish(S, name, static, moving=None, view=(0.8, -0.75, 0.45), margin=1.12, tris=3000):
    """static: piezas fijas (o None). moving: [(nombre, pivote, piezas)]."""
    objs = []
    if static:
        objs.append(k.join("Static" if moving else name, static))
    for mname, pivot, parts in moving or []:
        o = k.join(mname, parts)
        k.set_origin(o, pivot)
        objs.append(o)
    n = sum(gs.tri_count(o) for o in objs)
    if n > tris:
        S.over.append(f"{name}: {n}")
    export = objs
    if moving:
        root = bpy.data.objects.new(name, None)
        gs._link(root)
        for o in objs:
            o.parent = root
        export = [root] + objs
    gs.export_fbx(export, os.path.join(OUT, name + ".fbx"), animated=False)
    sc, cam = gs._setup_render(TILE)
    tile = os.path.join(gs.PREVIEW_DIR, f"_tmp_haz_{name}.png")
    gs._render_view(tile, objs, view, cam, margin=margin)
    S.tiles.append(tile)
    lo, hi = gs._world_bbox(objs)
    d = hi - lo
    S.report.append(f"| {name}.fbx | {n} | {d.x:.1f} x {d.y:.1f} x {d.z:.1f} m | "
                    + (", ".join(m[0] for m in moving) if moving else "—") + " |")
    return objs


class Sheet:
    def __init__(self):
        self.tiles, self.report, self.over = [], [], []


# ---------------------------------------------------------------------------
# H01 · pared / orilla de la piscina (tutorial y fuentes del zoo)
# ---------------------------------------------------------------------------
def h01_wall(S, name):
    C = pal()
    wall = gs.rounded_box("Wall", (0, 0, 0.55), (4.0, 0.7, 1.1), 0.12, [C["stone"], C["tile"]], segments=2)
    gs.paint_faces(wall, lambda c: c.z < 0.3 and c.y < 0.0, 1)
    P = [wall, gs.rounded_box("Cap", (0, 0, 1.14), (4.1, 0.95, 0.2), 0.08, C["stone2"], segments=2)]
    bump = gs.rounded_box("Bumper", (0, -0.4, 0.62), (3.7, 0.16, 0.36), 0.07, [C["pink"], C["white"]], segments=1)
    gs.bands_by_plane(bump, (1, 0, 0), (-1.85, 0, 0), 0.74, 0.5, 1, cut=True)
    P.append(bump)
    for x in (-1.5, 0.0, 1.5):
        P.append(gs.rounded_box(f"Joint{x}", (x, -0.355, 0.55), (0.06, 0.04, 1.0), 0.01, C["stone2"], segments=1))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Rivet{s}", (1.7 * s, -0.49, 0.62), 0.06, C["chrome"], seg=6, rings=4))
    finish(S, name, P, view=(0.6, -0.9, 0.4))


# ---------------------------------------------------------------------------
# H04 · oleaje: cresta de ola en media luna (8 m de ancho) con rizo hueco y espuma
# Se barre un perfil que cambia a lo largo de la cresta: en el centro rompe con un rizo que deja un hueco abierto
# hacia el frente; hacia las puntas se aplana y baja hasta el agua, y la línea se curva hacia atrás. Así no hay
# tapas planas ni una masa maciza: se ve como ola desde cualquier lado.
# ---------------------------------------------------------------------------
_WAVE_CURL = [(-3.2, 0.0), (-2.2, 0.18), (-1.2, 0.55), (-0.4, 1.05), (0.2, 1.5), (0.8, 1.82), (1.35, 1.82), (1.85, 1.55),
              (2.1, 1.25), (1.9, 1.0), (1.35, 0.9), (0.85, 0.65), (0.6, 0.35), (0.8, 0.1), (1.7, 0.0)]
_WAVE_FLAT = [(-3.2, 0.0), (-2.2, 0.15), (-1.2, 0.45), (-0.4, 0.85), (0.2, 1.15), (0.8, 1.3), (1.15, 1.3), (1.3, 1.1),
              (1.35, 0.9), (1.3, 0.7), (1.2, 0.5), (1.1, 0.35), (1.1, 0.2), (1.2, 0.08), (1.7, 0.0)]


def _wave_blend(u):
    c = math.sin(math.pi * u) ** 0.8
    c = c * c * (3 - 2 * c)
    return c


def _wave_profile(u):
    c = _wave_blend(u)
    return [(a[0] + (b[0] - a[0]) * c, a[1] + (b[1] - a[1]) * c) for a, b in zip(_WAVE_FLAT, _WAVE_CURL)]


def _blob(cx, cz, rx, rz, n=8):
    return [(cx + rx * math.cos(2 * math.pi * i / n), cz + rz * math.sin(2 * math.pi * i / n)) for i in range(n)]


def h04_wave(S, name):
    C = pal()
    deep = M("#2A9DD6", 0.2)
    taper = lambda u: 0.05 + 0.95 * math.sin(math.pi * u) ** 0.5
    path = [(x, 0.95 * (x / 4.0) ** 2, 0) for x in (-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0)]
    body = k.sweep("Swell", _wave_profile, path, [C["water"], deep], scale_fn=taper, sub=3,
                   mat_fn=lambda j: 1 if 9 <= j <= 12 else 0)

    def lip(u):
        c = _wave_blend(u)
        return _blob(1.5 + 0.55 * c, 1.62 - 0.2 * c, 0.3 * (0.6 + 0.4 * c), 0.2)

    def crest(u):
        c = _wave_blend(u)
        return _blob(0.95 + 0.2 * c, 1.28 + 0.52 * c, 0.34, 0.14)

    def toe(u):
        return _blob(1.75, 0.07, 0.3, 0.09)
    def streak(i, rx):
        def pr(u):
            c = _wave_blend(u)
            a, b = _WAVE_FLAT[i], _WAVE_CURL[i]
            return _blob(a[0] + (b[0] - a[0]) * c, a[1] + (b[1] - a[1]) * c + 0.04, rx, 0.07, 6)
        return pr
    foam_s = lambda u: 0.05 + 0.95 * math.sin(math.pi * u) ** 0.5
    P = [body]
    for nm, pr, sc in (("FoamLip", lip, foam_s), ("FoamCrest", crest, foam_s), ("FoamToe", toe, foam_s),
                       ("StreakA", streak(2, 0.26), lambda u: 0.05 + 0.85 * math.sin(math.pi * u) ** 0.9),
                       ("StreakB", streak(3, 0.2), lambda u: 0.05 + 0.9 * math.sin(math.pi * u) ** 0.7)):
        P.append(k.sweep(nm, pr, path, [C["foam"]], scale_fn=sc, sub=3, smooth=False))
    rnd = random.Random(4)
    for i, x in enumerate((-2.9, -1.7, -0.5, 0.7, 1.9, 3.0)):
        y = 0.95 * (x / 4.0) ** 2
        r = 0.2 + 0.1 * (rnd.random())
        P.append(gs.ellipsoid(f"Spray{i}", (x, y - 2.0 - 0.25 * (i % 2), 0.18), (r, r * 0.8, r * 0.6), C["foam"],
                              seg=8, rings=4))
    for i, x in enumerate((-1.6, 0.2, 1.7)):
        y = 0.95 * (x / 4.0) ** 2
        P.append(gs.ellipsoid(f"Drop{i}", (x, y - 2.3, 1.15 + 0.2 * (i % 2)), 0.1, C["water"], seg=6, rings=4))
    finish(S, name, P, view=(0.55, -1.0, 0.3))


# ---------------------------------------------------------------------------
# H08 · viento: manga de viento (la manga gira) y ráfaga
# ---------------------------------------------------------------------------
def h08_windsock(S, name):
    C = pal()
    P = [gs.rounded_cylinder("Base", (0, 0, 0.12), 0.5, 0.24, 0.06, C["stone2"], seg=14, steps=1),
         gs.capsule("Pole", (0, 0, 0.1), (0, 0, 3.3), 0.07, C["chrome"], seg=8, sub=1, cap=2),
         gs.ellipsoid("Knob", (0, 0, 3.4), 0.1, C["yellow"], seg=8, rings=5)]
    S1 = gs.lathe("SockBody", [(0.34, 0.0), (0.31, 0.3), (0.27, 0.6), (0.22, 0.9), (0.17, 1.2), (0.12, 1.5)],
                  [C["orange"], C["white"]], seg=12, axis=(1, 0, 0), loc=(0.1, 0, 3.12),
                  mat_fn=lambda i: i % 2)
    ring = gs.torus("Ring", (0.1, 0, 3.12), 0.36, 0.035, C["chrome"], axis=(1, 0, 0), seg=14, mseg=4)
    arm = gs.capsule("Arm", (0, 0, 3.12), (0.12, 0, 3.12), 0.04, C["chrome"], seg=6, sub=1, cap=1)
    finish(S, name, P, moving=[("Sock", (0, 0, 3.12), [S1, ring, arm])], view=(0.7, -0.8, 0.3))


def h08_gust(S, name):
    C = pal()
    P = []
    for i, (z, ln, rr, ph) in enumerate(((0.0, 3.2, 0.22, 0.0), (0.55, 2.4, 0.16, 1.0), (-0.5, 2.7, 0.18, 2.0))):
        pts = [Vector((ln * t / 8 - ln / 2, 0.0, z + 0.18 * math.sin(t * 0.9 + ph))) for t in range(9)]
        # rizo final
        c = pts[-1]
        for a in range(1, 6):
            ang = a / 5 * math.pi * 1.4
            pts.append(c + Vector((0.28 * math.sin(ang), 0.0, 0.28 * (1 - math.cos(ang)) + 0.0)))
        radii = [0.05 + rr * 0.3 * min(1.0, j / 4) * (1 - j / len(pts)) for j in range(len(pts))]
        P.append(gs.tube(f"Streak{i}", pts, radii, [C["white"], C["steam2"]][i % 2], seg=6, sub=2, cap=2, flatten=((0, 1, 0), 0.5)))
    finish(S, name, P, view=(0.1, -1, 0.1))


# ---------------------------------------------------------------------------
# H09 · remolino: embudo con brazos en espiral que giran (6 m de diámetro; Unity escala al radio)
# ---------------------------------------------------------------------------
def h09_whirlpool(S, name):
    C = pal()
    prof_pts = [(3.2, 0.04), (2.6, -0.05), (1.9, -0.28), (1.3, -0.62), (0.8, -1.0), (0.4, -1.4), (0.0, -1.75)]
    funnel = gs.lathe("Funnel", list(reversed(prof_pts)), [C["navy"], C["blue_dk"], C["blue"], C["tile"]], seg=24,
                      mat_fn=lambda i: [0, 0, 1, 1, 2, 3][min(i, 5)])
    # la lathe va de abajo arriba: el primer tramo es el centro (oscuro)

    def z_at(r):
        for (r1, z1), (r2, z2) in zip(prof_pts, prof_pts[1:]):
            if r2 <= r <= r1:
                u = (r1 - r) / (r1 - r2)
                return z1 + (z2 - z1) * u
        return prof_pts[-1][1]
    P = [funnel, gs.torus("Lip", (0, 0, 0.05), 3.2, 0.1, C["foam"], seg=28, mseg=5)]
    arms = []
    for a in range(4):
        pts = []
        for i in range(25):
            t = i / 24
            r = 3.0 * (1 - t) + 0.5 * t
            th = a * math.pi / 2 + t * 2.0 * math.pi
            pts.append(Vector((r * math.cos(th), r * math.sin(th), z_at(r) + 0.2)))
        radii = [0.36 * (1 - 0.7 * (j / 24)) + 0.06 for j in range(25)]
        arms.append(gs.tube(f"Arm{a}", pts, radii, C["foam"] if a % 2 == 0 else C["steam2"], seg=6, sub=1, cap=2,
                            flatten=((0, 0, 1), 0.55)))
    arms.append(gs.ellipsoid("Eye", (0, 0, -1.65), (0.28, 0.28, 0.1), C["navy"], seg=10, rings=4))
    finish(S, name, P, moving=[("Spin", (0, 0, 0), arms)], view=(0.12, -0.25, 1.0), margin=1.05)


# ---------------------------------------------------------------------------
# H11 · banco de arena (W3-1)
# ---------------------------------------------------------------------------
def h11_sandbar(S, name):
    C = pal()
    rnd = random.Random(3)
    prof = [(0.0, 0.62)]
    for i in range(1, 9):
        r = i / 8
        prof.append((r * 3.4, 0.62 * (1 - r ** 2) ** 0.8 * 0.9 + 0.045 * math.sin(i * 2.4) + 0.02 - 0.3 * r ** 6))
    prof.append((3.6, -0.3))
    prof = [(max(rr, 0.0), z) for rr, z in prof]
    mound = gs.lathe("Mound", list(reversed(prof)), [C["sand"], C["sand2"]], seg=22, scale=(1.0, 0.62, 1.0),
                     mat_fn=lambda i: 1 if i < 2 else 0)
    P = [mound]
    for i in range(6):
        a = rnd.uniform(0, 6.28)
        r = rnd.uniform(0.6, 2.0)
        P.append(gs.lathe(f"Shell{i}", [(0.0, 0.0), (0.16, 0.0), (0.12, 0.12), (0.0, 0.2)], C["pink"] if i % 2 else C["white"],
                          loc=(r * math.cos(a), r * math.sin(a) * 0.62, 0.34 * (1 - (r / 3.4) ** 2) + 0.04), seg=8, rot=("X", rnd.uniform(-70, 70))))
    for i, x in enumerate((-2.6, 2.6)):
        P.append(gs.capsule(f"Stake{i}", (x, -0.2, -0.1), (x, -0.2, 1.4), 0.05, C["wood2"], seg=6, sub=1, cap=1))
        P.append(k.flag(f"Flag{i}", (x + 0.05, -0.2, 1.25), 0.55, 0.32, [C["red"], C["white"]], waves=1.2, amp=0.06, th=0.015, n=6, stripes=2))
    for i in range(3):
        P.append(k.leaf(f"Weed{i}", [(-0.4 + 0.4 * i, 0.5, 0.3), (-0.3 + 0.4 * i, 0.5, 0.65), (-0.1 + 0.4 * i, 0.5, 0.85)], 0.08, C["moss"], sub=2))
    finish(S, name, P, view=(0.55, -0.85, 0.5))


# ---------------------------------------------------------------------------
# H13 · tronco giratorio (el tronco gira alrededor de un poste; versión podrida = frágil)
# ---------------------------------------------------------------------------
def h13_log(S, name, rotten=False):
    C = pal()
    bark = C["bark2"] if rotten else C["bark"]
    P = [gs.capsule("Post", (0, 0, -1.2), (0, 0, 0.95), 0.2, C["wood2"], seg=10, sub=1, cap=2),
         gs.rounded_cylinder("Collar", (0, 0, 0.55), 0.34, 0.14, 0.04, C["stone2"], seg=12, steps=1),
         gs.ellipsoid("PostCap", (0, 0, 1.0), (0.2, 0.2, 0.14), C["chrome"], seg=10, rings=5)]
    L = [gs.capsule("Log", (-3.0, 0, 0.8), (3.0, 0, 0.8), 0.42, bark, seg=12, cap=3, sub=3),
         gs.rounded_cylinder("EndA", (-3.38, 0, 0.8), 0.36, 0.05, 0.02, C["sand2"], axis=(1, 0, 0), seg=10, steps=1),
         gs.rounded_cylinder("EndB", (3.38, 0, 0.8), 0.36, 0.05, 0.02, C["sand2"], axis=(1, 0, 0), seg=10, steps=1),
         gs.torus("Bind", (0, 0, 0.8), 0.44, 0.05, C["rope"], axis=(1, 0, 0), seg=14, mseg=4),
         gs.rounded_cylinder("Hub", (0, 0, 1.2), 0.2, 0.1, 0.03, C["chrome"], seg=10, steps=1)]
    for i, (x, s, ang) in enumerate(((-1.7, 1, 40), (1.5, -1, 30))):
        L.append(gs.capsule(f"Knot{i}", (x, 0.3 * s, 0.9), (x + 0.1, 0.62 * s, 1.15), 0.1, bark, seg=6, sub=1, cap=2))
    if rotten:
        for i, x in enumerate((-2.2, -0.9, 2.2)):
            L.append(gs.ellipsoid(f"Hole{i}", (x, -0.36, 0.86), (0.13, 0.08, 0.17), C["black"], seg=8, rings=4))
        for i, (x, s) in enumerate(((1.0, 0.2), (-1.3, 0.16))):
            L.append(gs.capsule(f"MStem{i}", (x, -0.3, 1.1), (x, -0.38, 1.1 + s * 1.4), s * 0.3, C["white"], seg=6, cap=1, sub=1))
            L.append(gs.ellipsoid(f"MCap{i}", (x, -0.38, 1.12 + s * 1.4), (s, s, s * 0.55), C["red"], seg=8, rings=4))
    else:
        for i, x in enumerate((-2.4, 2.1)):
            L.append(gs.torus(f"Ring{i}", (x, 0, 0.8), 0.43, 0.025, C["bark2"], axis=(1, 0, 0), seg=12, mseg=3))
    finish(S, name, P, moving=[("Spinner", (0, 0, 0.8), L)], view=(0.5, -0.9, 0.5))


# ---------------------------------------------------------------------------
# H16 · obstáculo rítmico: pilar-altavoz que late al compás
# ---------------------------------------------------------------------------
def h16_beat(S, name):
    C = pal()
    P = [gs.rounded_cylinder("Base", (0, 0, 0.2), 1.25, 0.4, 0.1, C["navy"], seg=18, steps=2),
         gs.torus("BaseRing", (0, 0, 0.42), 1.0, 0.07, C["glow_p"], seg=24, mseg=4)]
    pillar = [gs.rounded_cylinder("Pillar", (0, 0, 1.2), 0.8, 1.5, 0.18, [C["pink"], C["white"]], seg=16, steps=2),
              gs.rounded_cylinder("Top", (0, 0, 2.0), 0.7, 0.18, 0.06, C["navy"], seg=16, steps=1)]
    gs.bands_by_plane(pillar[0], (0, 0, 1), (0, 0, 0.45), 0.5, 0.5, 1, cut=True)
    for s, z in ((-1, 1.45), (1, 0.85)):
        pillar.append(gs.torus(f"Cone{z}", (0, -0.78, z), 0.28, 0.07, C["black"], axis=(0, 1, 0), seg=14, mseg=4))
        pillar.append(gs.ellipsoid(f"Dome{z}", (0, -0.74, z), (0.17, 0.08, 0.17), C["glow_y"], seg=10, rings=5))
    pillar.append(gs.ellipsoid("Crown", (0, 0, 2.2), (0.35, 0.35, 0.22), C["glow_c"], seg=10, rings=5))
    ring = [gs.torus("BeatRing", (0, 0, 0.0), 1.55, 0.06, C["glow_c"], seg=28, mseg=4)]
    finish(S, name, P, moving=[("Pulse", (0, 0, 0.4), pillar), ("BeatRing", (0, 0, 0.05), ring)], view=(0.6, -0.8, 0.45))


# ---------------------------------------------------------------------------
# H17 · cascada / caída (10 m de cauce)
# ---------------------------------------------------------------------------
def h17_waterfall(S, name):
    C = pal()
    P = []
    for s in (1, -1):
        sh = k.rock(f"Shoulder{s}", (6.4 * s, 0.0, -0.8), (2.4, 2.8, 1.5), [C["stone_dk"], C["moss"]], seed=4 + s, subdiv=2,
                    rough=0.18, top_index=1, top_dot=0.7)
        P.append(sh)
        P.append(gs.capsule(f"WarnPost{s}", (4.35 * s, -1.0, 0.0), (4.35 * s, -1.0, 1.5), 0.07, C["wood2"], seg=6, sub=1, cap=1))
        board = gs.rounded_box(f"WarnBoard{s}", (4.35 * s, -1.05, 1.45), (0.7, 0.08, 0.5), 0.05, [C["yellow"], C["black"]], segments=1)
        gs.bands_by_plane(board, (1, 0, 1), (0, 0, 0), 0.3, 0.5, 1, cut=True)
        P.append(board)
    P.append(gs.rounded_box("Lip", (0, 0.2, -0.15), (9.4, 1.4, 0.3), 0.12, C["stone2"], segments=1))
    P.append(gs.rounded_box("Bed", (0, 0.9, -0.5), (9.4, 3.0, 0.4), 0.1, C["stone_dk"], segments=1))
    curtain = [(0.0, 0.14), (0.55, 0.08), (0.95, -0.55), (1.1, -2.3), (1.15, -4.2), (0.98, -4.2), (0.93, -2.3), (0.78, -0.6), (0.5, -0.06), (0.0, 0.0)]
    sheet = k.sweep("Sheet", curtain, [(-4.5, 0, 0), (-1.5, 0, 0), (1.5, 0, 0), (4.5, 0, 0)], [C["water"]])
    mist = [gs.ellipsoid(f"Mist{i}", (x, -1.35 - 0.1 * (i % 2), -4.1), (0.8, 0.55, 0.4), C["foam"], seg=8, rings=5)
            for i, x in enumerate((-3.4, -1.7, 0.0, 1.7, 3.4))]
    finish(S, name, P, moving=[("Sheet", (0, 0.1, 0.0), [sheet]), ("Mist", (0, -1.3, -4.1), mist)], view=(0.8, -0.9, 0.2), margin=1.05)


# ---------------------------------------------------------------------------
# H19 · géiser: cráter + columna de agua/vapor que sube (pivote en la base de la columna)
# ---------------------------------------------------------------------------
def h19_geyser(S, name):
    C = pal()
    mound = gs.lathe("Mound", [(0.0, -0.3), (2.4, -0.3), (2.0, 0.4), (1.2, 0.9), (0.85, 1.0), (0.62, 0.7), (0.0, 0.45)],
                     [C["basalt"], C["lava"]], seg=16)
    gs.paint_faces(mound, lambda c: c.z > 0.65 and math.hypot(c.x, c.y) < 0.9, 1)
    P = [mound, gs.torus("Rim", (0, 0, 0.98), 0.76, 0.1, C["yellow"], seg=14, mseg=4)]
    for i in range(5):
        a = i / 5 * 2 * math.pi + 0.3
        P.append(gs.ellipsoid(f"Rock{i}", (1.6 * math.cos(a), 1.6 * math.sin(a), 0.2), (0.35, 0.3, 0.22), C["stone_dk"], seg=6, rings=4))
    plume = [gs.lathe("Column", [(0.0, 0.0), (0.7, 0.0), (0.55, 1.5), (0.5, 3.0), (0.56, 4.5), (0.66, 5.4), (0.0, 5.6)],
                      [C["steam2"], C["steam"]], seg=14, mat_fn=lambda i: 0 if i < 3 else 1)]
    plume.append(gs.lathe("Flare", [(0.0, 4.9), (0.6, 4.9), (1.25, 5.35), (1.0, 5.85), (0.0, 6.15)], C["foam"], seg=14))
    for i in range(6):
        a = i / 6 * 2 * math.pi
        plume.append(gs.ellipsoid(f"Cloud{i}", (0.95 * math.cos(a), 0.95 * math.sin(a), 5.75), (0.42, 0.42, 0.3), C["steam"], seg=8, rings=4))
    ring = [gs.torus("ShockRing", (0, 0, 0.0), 1.2, 0.08, C["foam"], seg=24, mseg=4)]
    finish(S, name, P, moving=[("Plume", (0, 0, 1.0), plume), ("ShockRing", (0, 0, 1.0), ring)], view=(0.55, -0.85, 0.3), margin=1.0)


# ---------------------------------------------------------------------------
# H36 · géiser de gas dorado: cráter con pozo de gas, burbujas doradas sueltas y un chorro brillante.
# Mismo tamaño y pivotes que H19 (boca del cráter en z = 1.0): `Jet` escala en Z desde la boca, `GasRing` se expande,
# `Bubble_00..11` suben y revientan (pivote en su centro).
# ---------------------------------------------------------------------------
def h36_gold_geyser(S, name):
    C = pal()
    gold = E("#FFCF2E", 2.5)
    gold_lt = E("#FFEE90", 3.5)
    glint = E("#FFFBE0", 5.0)
    amber = M("#E39A22", 0.5)
    crust = M("#9A6A3C")
    crust2 = M("#C79A60")
    mound = gs.lathe("Mound", [(0.0, -0.3), (2.6, -0.3), (2.2, 0.4), (1.7, 0.85), (1.35, 1.0), (1.1, 0.8), (0.0, 0.62)],
                     [crust, gold], seg=16)
    gs.paint_faces(mound, lambda c: c.z > 0.6 and math.hypot(c.x, c.y) < 1.3, 1)
    P = [mound, gs.torus("Rim", (0, 0, 1.0), 1.28, 0.16, amber, seg=18, mseg=4),
         gs.torus("RimGlow", (0, 0, 1.09), 1.2, 0.05, gold_lt, seg=18, mseg=4)]
    for i in range(7):
        a = i / 7 * 2 * math.pi + 0.25
        P.append(gs.ellipsoid(f"Rock{i}", (2.55 * math.cos(a), 2.55 * math.sin(a), 0.05), (0.5, 0.42, 0.3), crust2,
                              seg=6, rings=4))
    for i in range(6):  # burbujas medio hundidas en el pozo
        a = i / 6 * 2 * math.pi + 0.9
        P.append(gs.ellipsoid(f"Pool{i}", (1.0 * math.cos(a), 1.0 * math.sin(a), 0.82), 0.18 + 0.05 * (i % 3),
                              glint if i % 2 else gold_lt, seg=8, rings=4))
    # chorro: columna con bandas, destellos verticales y gotas en la punta
    col = gs.lathe("Column", [(0.0, 0.0), (0.62, 0.0), (0.46, 1.3), (0.42, 2.7), (0.52, 4.1), (0.58, 4.9), (0.46, 5.5), (0.0, 5.75)],
                   [gold, gold_lt], seg=12, mat_fn=lambda i: i % 2)
    jet = [col]
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.4
        r = 0.43
        jet.append(gs.capsule(f"Glint{i}", (r * math.cos(a), r * math.sin(a), 0.6),
                              (r * 0.8 * math.cos(a), r * 0.8 * math.sin(a), 4.4), 0.07, glint, seg=6, sub=1, cap=1))
    for i in range(7):
        a = i / 7 * 2 * math.pi
        up = 5.1 + 0.5 * (i % 3)
        jet.append(gs.ellipsoid(f"Drop{i}", (0.95 * math.cos(a), 0.95 * math.sin(a), up), (0.2, 0.2, 0.32), gold_lt,
                                seg=8, rings=4))
    ring = [gs.torus("GasRing", (0, 0, 0.0), 1.5, 0.1, gold_lt, seg=24, mseg=4)]
    rnd = random.Random(36)
    mov = [("Jet", (0, 0, 1.0), jet), ("GasRing", (0, 0, 1.0), ring)]
    for i in range(12):
        a = rnd.uniform(0, 2 * math.pi)
        rad = 0.7 + 1.0 * (i / 11)
        z = 1.3 + 3.4 * ((i * 7) % 12) / 11
        r = 0.18 + 0.26 * rnd.random()
        c = Vector((rad * math.cos(a), rad * math.sin(a), z))
        mov.append((f"Bubble_{i:02d}", c, [
            gs.ellipsoid(f"B{i}", c, r, gold, seg=8, rings=5),
            gs.ellipsoid(f"Hi{i}", c + Vector((-0.35 * r, -0.45 * r, 0.45 * r)), r * 0.28, glint, seg=6, rings=3)]))
    finish(S, name, P, moving=mov, view=(0.55, -0.85, 0.3), margin=1.0)


# ---------------------------------------------------------------------------
# H20 · roca que cae: roca + marca de sombra de aviso (disco plano)
# ---------------------------------------------------------------------------
def shadow_mark(C, r=1.3, mat=None):
    mat = mat or C["shadow"]
    return [gs.lathe("ShadowDisc", [(0.0, 0.0), (r, 0.0), (r, 0.015), (0.0, 0.015)], mat, seg=24),
            gs.torus("ShadowRing", (0, 0, 0.02), r * 0.92, 0.035, C["warn"], seg=28, mseg=4)]


def h20_rock(S, name):
    C = pal()
    rock = k.rock("Boulder", (0, 0, 0.0), (1.0, 0.92, 0.95), [C["stone_dk"], C["stone2"]], seed=11, subdiv=2, rough=0.28,
                  top_index=1, top_dot=0.6, flat_bottom=0.9)
    parts = [rock]
    for i, (a, l) in enumerate(((0.5, 0.9), (2.4, 0.7), (4.2, 0.8))):
        pts = [Vector((0.2 * math.cos(a), 0.2 * math.sin(a), 0.8)), Vector((0.55 * math.cos(a + 0.2), 0.55 * math.sin(a + 0.2), 0.7)),
               Vector((l * 0.9 * math.cos(a), l * 0.9 * math.sin(a), 0.2))]
        parts.append(gs.tube(f"Crack{i}", pts, [0.025, 0.03, 0.02], C["black"], seg=4, sub=2, cap=1))
    parts += [gs.ellipsoid(f"Pebble{i}", (1.0 * math.cos(a), 1.0 * math.sin(a), -0.5), 0.16, C["stone_dk"], seg=6, rings=4)
              for i, a in enumerate((1.0, 3.0, 5.0))]
    finish(S, name, shadow_mark(C, 1.4), moving=[("Rock", (0, 0, 0.0), parts)], view=(0.6, -0.8, 0.5))


# ---------------------------------------------------------------------------
# H27 · chorro de fuente (empuja hacia arriba) y trampolín flotante
# ---------------------------------------------------------------------------
def h27_jet(S, name):
    C = pal()
    P = [gs.rounded_cylinder("Plate", (0, 0, 0.1), 1.0, 0.2, 0.06, C["stone2"], seg=18, steps=1),
         gs.torus("PlateRing", (0, 0, 0.2), 0.82, 0.07, C["tile"], seg=20, mseg=4)]
    for i in range(6):
        a = i / 6 * 2 * math.pi
        P.append(gs.ellipsoid(f"Nozzle{i}", (0.58 * math.cos(a), 0.58 * math.sin(a), 0.22), (0.1, 0.1, 0.06), C["chrome"], seg=8, rings=4))
    jet = [gs.lathe("Column", [(0.0, 0.0), (0.5, 0.0), (0.4, 1.0), (0.33, 2.4), (0.3, 3.6), (0.4, 4.4), (0.0, 4.8)],
                    [C["water"], C["foam"]], seg=12, mat_fn=lambda i: 1 if i >= 4 else 0)]
    jet.append(gs.lathe("Fan", [(0.0, 4.3), (0.4, 4.3), (1.05, 4.75), (0.8, 5.1), (0.0, 5.3)], C["foam"], seg=12))
    for i in range(5):
        a = i / 5 * 2 * math.pi
        jet.append(gs.ellipsoid(f"Drop{i}", (0.85 * math.cos(a), 0.85 * math.sin(a), 4.2), (0.12, 0.12, 0.16), C["water"], seg=6, rings=4))
    splash = [gs.torus("Splash", (0, 0, 0.0), 0.95, 0.09, C["foam"], seg=22, mseg=4)]
    finish(S, name, P, moving=[("Jet", (0, 0, 0.2), jet), ("Splash", (0, 0, 0.24), splash)], view=(0.5, -0.85, 0.3), margin=1.0)


def h27_trampoline(S, name):
    C = pal()
    ring = gs.torus("Float", (0, 0, 0.35), 2.0, 0.42, [C["red"], C["white"]], seg=32, mseg=10)
    gs.paint_faces(ring, lambda c: int((math.atan2(c.y, c.x) + math.pi) / (2 * math.pi) * 12) % 2 == 0, 1)
    P = [ring, gs.torus("Valve", (0, 0, 0.0), 0.01, 0.01, C["white"], seg=3, mseg=3)]
    P.pop()
    for i in range(8):
        a = i / 8 * 2 * math.pi
        P.append(gs.capsule(f"Strap{i}", (1.66 * math.cos(a), 1.66 * math.sin(a), 0.62), (1.35 * math.cos(a), 1.35 * math.sin(a), 0.55),
                            0.04, C["navy"], seg=5, sub=1, cap=1))
    mat_ = [gs.lathe("Mat", [(0.0, 0.42), (1.3, 0.42), (1.62, 0.5), (1.66, 0.58), (1.66, 0.6)], [C["blue"], C["navy"]], seg=28,
                     mat_fn=lambda i: 1 if i > 1 else 0),
            gs.torus("Target", (0, 0, 0.44), 0.8, 0.06, C["yellow"], seg=24, mseg=4),
            gs.torus("Target2", (0, 0, 0.44), 0.35, 0.05, C["yellow"], seg=18, mseg=4)]
    for i in range(5):
        a = i / 5 * 2 * math.pi + 0.3
        mat_.append(gs.ellipsoid(f"Dot{i}", (1.05 * math.cos(a), 1.05 * math.sin(a), 0.44), (0.11, 0.11, 0.035), C["yellow"], seg=6, rings=3))
    finish(S, name, P, moving=[("Mat", (0, 0, 0.42), mat_)], view=(0.5, -0.85, 0.6))


# ---------------------------------------------------------------------------
# H30 · coco que cae: coco + sombra de aviso
# ---------------------------------------------------------------------------
def h30_coconut(S, name):
    C = pal()
    P = [gs.ellipsoid("Husk", (0, 0, 0), (0.42, 0.4, 0.45), C["bark2"], seg=14, rings=9),
         gs.ellipsoid("Cap", (0, 0, 0.3), (0.28, 0.27, 0.2), C["bark"], seg=10, rings=5)]
    for i, (x, z) in enumerate(((-0.14, 0.12), (0.14, 0.12), (0.0, -0.05))):
        P.append(gs.ellipsoid(f"Eye{i}", (x, -0.36, z), (0.06, 0.04, 0.07), C["black"], seg=6, rings=4))
    for i in range(7):
        a = i / 7 * 2 * math.pi
        P.append(gs.capsule(f"Fiber{i}", (0.1 * math.cos(a), 0.1 * math.sin(a), 0.42), (0.22 * math.cos(a), 0.22 * math.sin(a), 0.5), 0.015, C["rope"], seg=4, sub=1, cap=1))
    finish(S, name, shadow_mark(C, 0.9), moving=[("Coconut", (0, 0, 0.0), P)], view=(0.5, -0.85, 0.5))


# ---------------------------------------------------------------------------
# H33 · pared de globos: marco con red y 18 globos sueltos (cada uno se revienta aparte)
# ---------------------------------------------------------------------------
def balloon(name, loc, col, C, knot):
    parts = [gs.ellipsoid(name + "_B", (0, 0, 0), (0.5, 0.5, 0.6), col, seg=8, rings=5),
             gs.cone(name + "_K", (0, 0, -0.62), (0, 0, -0.52), 0.1, 0.05, knot, seg=5, round_tip=False),
             gs.ellipsoid(name + "_S", (-0.15, -0.38, 0.22), (0.1, 0.05, 0.14), C["foam"], seg=4, rings=3),
             gs.tube(name + "_T", [(0, 0, -0.64), (0.05, 0, -0.9), (-0.04, 0, -1.1)], 0.012, C["rope"], seg=3, sub=1, cap=0)]
    o = k.join(name, parts)
    o.location = Vector(loc)
    return o


def h33_balloon_wall(S, name):
    C = pal()
    P = []
    for s in (1, -1):
        P.append(gs.capsule(f"Post{s}", (3.5 * s, 0, -0.2), (3.5 * s, 0, 4.2), 0.12, C["wood"], seg=8, sub=1, cap=2))
        P.append(gs.ellipsoid(f"Top{s}", (3.5 * s, 0, 4.35), 0.2, C["yellow"], seg=8, rings=5))
        P.append(gs.rounded_cylinder(f"Float{s}", (3.5 * s, 0, 0.05), 0.55, 0.3, 0.08, [C["pink"], C["white"]], seg=12, steps=1))
    P.append(gs.tube("Rope", [(-3.5, 0, 3.9), (-1.7, 0, 3.75), (0, 0, 3.7), (1.7, 0, 3.75), (3.5, 0, 3.9)], 0.04, C["rope"], seg=5, sub=3, cap=1))
    P.append(gs.tube("RopeLow", [(-3.5, 0, 0.45), (0, 0, 0.4), (3.5, 0, 0.45)], 0.035, C["rope"], seg=5, sub=2, cap=1))
    cols = [C["pink"], C["yellow"], C["tile"], C["lime"], C["orange"], C["purple"]]
    knots = C["navy"]
    mov = []
    i = 0
    for r in range(3):
        for c in range(6):
            x = -2.75 + c * 1.1 + (0.55 if r % 2 else 0.0) * 0.0
            z = 1.35 + r * 1.05
            b = balloon(f"Balloon_{i:02d}", (x, 0.0, z), cols[(c + r * 2) % 6], C, knots)
            mov.append(b)
            i += 1
    # los globos ya son objetos: exportamos como móviles con pivote en su centro
    objs = [k.join("Static", P)]
    objs += mov
    n = sum(gs.tri_count(o) for o in objs)
    root = bpy.data.objects.new(name, None)
    gs._link(root)
    for o in objs:
        o.parent = root
    gs.export_fbx([root] + objs, os.path.join(OUT, name + ".fbx"), animated=False)
    sc, cam = gs._setup_render(TILE)
    tile = os.path.join(gs.PREVIEW_DIR, f"_tmp_haz_{name}.png")
    gs._render_view(tile, objs, (0.35, -1, 0.2), cam, margin=1.08)
    S.tiles.append(tile)
    lo, hi = gs._world_bbox(objs)
    d = hi - lo
    if n > 3000:
        S.over.append(f"{name}: {n}")
    S.report.append(f"| {name}.fbx | {n} | {d.x:.1f} x {d.y:.1f} x {d.z:.1f} m | Balloon_00..17 |")


# ---------------------------------------------------------------------------
# H34 · chorro de manguera (empuje lateral): pedestal con manguera y boquilla; el chorro es una pieza aparte
# ---------------------------------------------------------------------------
def h34_hose(S, name):
    C = pal()
    P = [gs.rounded_cylinder("Pedestal", (0, 0, 0.45), 0.6, 0.9, 0.12, [C["stone"], C["tile"]], seg=16, steps=2),
         gs.rounded_cylinder("Top", (0, 0, 0.95), 0.68, 0.14, 0.05, C["stone2"], seg=16, steps=1),
         gs.torus("Coil", (0, 0, 0.55), 0.62, 0.1, C["orange"], seg=20, mseg=6),
         gs.torus("Coil2", (0, 0, 0.8), 0.6, 0.1, C["orange"], seg=20, mseg=6),
         gs.capsule("ValveStem", (0.4, 0.3, 0.98), (0.4, 0.3, 1.25), 0.04, C["chrome"], seg=6, sub=1, cap=1),
         gs.torus("Wheel", (0.4, 0.3, 1.28), 0.14, 0.035, C["red"], seg=10, mseg=4)]
    head = [gs.tube("Hose", [(0, 0, 1.0), (0.2, 0, 1.45), (0.65, 0, 1.55)], [0.09, 0.09, 0.08], C["orange"], seg=8, sub=3, cap=2),
            gs.rounded_cylinder("Nozzle", (0.9, 0, 1.55), 0.15, 0.4, 0.05, C["chrome"], axis=(1, 0, 0), seg=10, steps=1),
            gs.torus("NozzleRing", (1.05, 0, 1.55), 0.17, 0.04, C["red"], axis=(1, 0, 0), seg=10, mseg=4)]
    pts = [Vector((1.15 + 2.0 * t, 0, 1.55 + 0.55 * math.sin(math.pi * t) - 0.9 * t * t)) for t in [i / 9 for i in range(10)]]
    stream = [gs.tube("Stream", pts, [0.12 + 0.2 * (i / 9) for i in range(10)], C["water"], seg=8, sub=2, cap=2)]
    for i, t in enumerate((0.45, 0.8)):
        p = pts[int(t * 9)]
        stream.append(gs.ellipsoid(f"Drop{i}", p + Vector((0.1, 0.0, 0.15)), (0.1, 0.1, 0.12), C["foam"], seg=6, rings=4))
    finish(S, name, P, moving=[("Nozzle", (0.0, 0, 1.0), head), ("Stream", (1.15, 0, 1.55), stream)], view=(0.45, -0.85, 0.35))


# ---------------------------------------------------------------------------
# H35 · plataforma que se hunde: cubierta de tablones sobre flotadores y 4 postes de amarre fijos
# ---------------------------------------------------------------------------
def h35_platform(S, name):
    C = pal()
    P = []
    for i, (x, y) in enumerate(((-2.35, -2.35), (2.35, -2.35), (-2.35, 2.35), (2.35, 2.35))):
        P.append(gs.capsule(f"Post{i}", (x, y, -1.0), (x, y, 0.9), 0.09, C["wood2"], seg=6, sub=1, cap=1))
        P.append(gs.torus(f"Ring{i}", (x, y, 0.7), 0.13, 0.03, C["rope"], seg=8, mseg=3))
    deck = []
    for j in range(5):
        deck.append(gs.rounded_box(f"Plank{j}", (-1.6 + j * 0.8, 0, 0.32), (0.74, 3.8, 0.2), 0.04, C["wood"] if j % 2 else C["bark"], segments=1))
    for y in (-1.5, 1.5):
        deck.append(gs.rounded_box(f"Beam{y}", (0, y, 0.18), (4.1, 0.28, 0.16), 0.04, C["wood2"], segments=1))
    for s in (1, -1):
        edge = gs.rounded_box(f"Edge{s}", (0, 2.0 * s, 0.45), (4.0, 0.14, 0.1), 0.03, [C["yellow"], C["black"]], segments=1)
        gs.bands_by_plane(edge, (1, 0, 0), (-2, 0, 0), 0.5, 0.5, 1, cut=True)
        deck.append(edge)
    for i, (x, y) in enumerate(((-1.5, -1.3), (1.5, -1.3), (-1.5, 1.3), (1.5, 1.3))):
        deck.append(gs.rounded_cylinder(f"Drum{i}", (x, y, -0.12), 0.45, 0.6, 0.08, [C["blue"], C["white"]], seg=10, steps=1))
    deck.append(gs.ellipsoid("Light", (0, 0, 0.5), (0.2, 0.2, 0.14), C["glow_y"], seg=8, rings=4))
    finish(S, name, P, moving=[("Deck", (0, 0, 0.3), deck)], view=(0.6, -0.8, 0.6))


PROPS = [("H01_Wall", h01_wall), ("H04_WaveCrest", h04_wave), ("H08_WindSock", h08_windsock), ("H08_GustStreak", h08_gust),
         ("H09_Whirlpool", h09_whirlpool), ("H11_Sandbar", h11_sandbar), ("H13_SpinningLog", lambda S, n: h13_log(S, n)),
         ("H13_SpinningLog_Rotten", lambda S, n: h13_log(S, n, True)), ("H16_BeatPillar", h16_beat),
         ("H17_WaterfallEdge", h17_waterfall), ("H19_Geyser", h19_geyser), ("H36_GoldGasGeyser", h36_gold_geyser), ("H20_FallingRock", h20_rock),
         ("H27_Jet", h27_jet), ("H27_Trampoline", h27_trampoline), ("H30_Coconut", h30_coconut),
         ("H33_BalloonWall", h33_balloon_wall), ("H34_HoseJet", h34_hose), ("H35_SinkingPlatform", h35_platform)]


def run(props, only=None, sheet_name="Hazards", cols=6):
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    S = Sheet()
    for name, fn in props:
        if only and name not in only:
            continue
        gs.reset_scene()
        fn(S, name)
    out = os.path.join(gs.PREVIEW_DIR, f"{sheet_name}_sheet.png")
    gs.stitch(S.tiles, out, cols=cols)
    for p in S.tiles:
        os.remove(p)
    print(f"[gs] preview {out}")
    print("| Archivo | Triángulos | Tamaño | Partes móviles |")
    for line in S.report:
        print(line)
    assert not S.over, f"exceden 3000 triángulos: {S.over}"


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    run(PROPS, only=set(argv) or None)
