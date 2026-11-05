"""Cosméticos de la tienda (docs/06 §2): 8 sombreros y 6 balsas alternativas.
blender --background --python cosmetics.py [-- --hats | --rafts] [--fit]

Sombreros (≤ 800 triángulos): pivote en la base (z = 0), frente hacia -Y, hechos para una cabeza de 0.42 m de
ancho; se cuelgan del hueso Head. hat_anchors.json da, por personaje, la parte superior de la cabeza en reposo
(espacio del modelo en Unity: X derecha, Y arriba, Z adelante) y la escala sugerida.
Balsas (≤ 3000 triángulos, radio ~2 m, borde a 0.6 m como la balsa naranja): vacío raíz Raft_<Nombre> con
Chamber_0_NE, Chamber_1_NO, Chamber_2_SE y Chamber_3_SO (índices de RaftChambers, cada una con su pivote),
Raft_Floor y Raft_Deco. Norte = -Y en Blender (+Z en Unity).
Salida: Tools/Export/Cosmetics/Hats/H0x_*.fbx y Tools/Export/Cosmetics/Rafts/R0x_*.fbx.
"""
import json
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import bmesh
import gs_common as gs
from gs_common import Vector, Matrix
from props import set_origin, join, QUADS

OUT = os.path.join(gs.EXPORT_DIR, "Cosmetics")
HAT_TRIS, RAFT_TRIS = 800, 3000


def cut_below(obj, z):
    """Borra la parte de la malla por debajo de z y cierra el hueco."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, 0, z), plane_no=(0, 0, 1), clear_inner=True)
    edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
    if edges:
        bmesh.ops.holes_fill(bm, edges=edges, sides=0)
    bm.to_mesh(obj.data)
    bm.free()
    return obj


def deform(obj, fn):
    for v in obj.data.vertices:
        v.co = fn(v.co.copy())
    return obj


# ---------------------------------------------------------------------------
# Sombreros
# ---------------------------------------------------------------------------
def hat_zoo_cap():
    green, dark = gs.mat("#2F9E57"), gs.mat("#1F7A41")
    yellow, paw = gs.mat("#FFC93C"), gs.mat("#5A3A1E")
    crown = gs.lathe("Crown", [(0.212, 0.0), (0.215, 0.05), (0.2, 0.11), (0.165, 0.165), (0.1, 0.2), (0.0, 0.214)],
                     [green, dark], seg=16, mat_fn=lambda i: 0)
    gs.paint_faces(crown, lambda c: (math.degrees(math.atan2(c.y, c.x)) % 60) < 30, 1)
    P = [crown]
    brim = gs.ellipsoid("Brim", (0, -0.225, 0.03), (0.17, 0.15, 0.022), dark, seg=14, rings=6, rot=("X", 8))
    P.append(brim)
    P.append(gs.ellipsoid("Button", (0, 0, 0.214), (0.028, 0.028, 0.016), dark, seg=8, rings=4))
    P.append(gs.ellipsoid("Badge", (0, -0.184, 0.105), (0.066, 0.018, 0.058), yellow, seg=12, rings=6,
                          rot=("X", -25)))
    P.append(gs.ellipsoid("PawPad", (0, -0.2, 0.09), (0.026, 0.008, 0.02), paw, seg=8, rings=4, rot=("X", -25)))
    for i, (dx, dz) in enumerate(((-0.03, 0.125), (-0.011, 0.137), (0.011, 0.137), (0.03, 0.125))):
        P.append(gs.ellipsoid(f"Toe{i}", (dx, -0.195 + (dz - 0.1) * 0.45, dz), (0.011, 0.006, 0.012), paw, seg=6,
                              rings=3, rot=("X", -25)))
    return P


def hat_goggles():
    strap, frame = gs.mat("#26262E"), gs.mat("#FFC93C")
    lens, tube = gs.mat("#59C3FF", rough=0.1), gs.mat("#FF7A1A")
    P = [gs.torus("Strap", (0, 0.0, 0.05), 0.205, 0.022, strap, seg=18, mseg=4, scale=(1.0, 1.05, 0.75))]
    rot = ("X", -28)
    for s in (1, -1):
        P.append(gs.rounded_cylinder(f"Frame{s}", (0.072 * s, -0.205, 0.105), 0.068, 0.045, 0.014, frame,
                                     axis=(0, -1, 0.5), seg=10, steps=1))
        P.append(gs.ellipsoid(f"Lens{s}", (0.072 * s, -0.232, 0.118), (0.055, 0.016, 0.052), lens, seg=10,
                              rings=4, rot=rot))
        P.append(gs.ellipsoid(f"Glint{s}", (0.072 * s - 0.022, -0.247, 0.138), (0.014, 0.004, 0.01),
                              gs.mat("eye_white", rough=0.1), seg=6, rings=3, rot=rot))
    P.append(gs.capsule("Bridge", (-0.02, -0.215, 0.11), (0.02, -0.215, 0.11), 0.016, frame, seg=6, cap=2, sub=1))
    P.append(gs.tube("Snorkel", [(0.205, -0.1, 0.03), (0.235, -0.06, 0.15), (0.24, -0.02, 0.3), (0.215, 0.02, 0.36)],
                     0.024, tube, seg=6, cap=2, sub=2))
    P.append(gs.ellipsoid("SnorkelTop", (0.212, 0.025, 0.37), (0.03, 0.03, 0.02), strap, seg=8, rings=4))
    P.append(gs.capsule("Clip", (0.205, -0.1, 0.06), (0.205, -0.1, 0.11), 0.018, strap, seg=6, cap=2, sub=1))
    return P


def hat_pirate():
    black, gold = gs.mat("#22222A"), gs.mat("gold")
    white, red = gs.mat("#F7F4EC"), gs.mat("#D7263D")
    body = gs.ellipsoid("Bicorne", (0, 0, 0.02), (0.36, 0.16, 0.27), black, seg=18, rings=10)
    deform(body, lambda c: Vector((c.x, c.y * (1 - 0.45 * max(0.0, c.z) / 0.27),
                                   c.z + 0.1 * (c.x / 0.36) ** 2 - 0.05 * max(0.0, 1 - abs(c.x) / 0.12)
                                   * max(0.0, c.z) / 0.27)))
    cut_below(body, 0.0)
    P = [body]
    P.append(gs.lathe("Band", [(0.205, 0.0), (0.212, 0.045), (0.2, 0.06), (0.0, 0.06)], black, seg=14))
    trim = []
    for a in range(10, 171, 16):
        x = 0.35 * math.cos(math.radians(a))
        z = 0.02 + 0.26 * math.sin(math.radians(a))
        z += 0.1 * (x / 0.36) ** 2 - 0.05 * max(0.0, 1 - abs(x) / 0.12) * max(0.0, z) / 0.27
        trim.append(Vector((x, -0.16 * (1 - 0.45 * max(0.0, z) / 0.27) * 0.55 - 0.012, z - 0.012)))
    P.append(gs.tube("Trim", trim, 0.012, gold, seg=5, cap=1, sub=1))
    P.append(gs.ellipsoid("Skull", (0, -0.15, 0.15), (0.045, 0.02, 0.045), white, seg=10, rings=6))
    P.append(gs.ellipsoid("Jaw", (0, -0.15, 0.113), (0.028, 0.018, 0.018), white, seg=8, rings=4))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Socket{s}", (0.017 * s, -0.168, 0.155), (0.011, 0.005, 0.012), black, seg=6,
                              rings=3))
        P.append(gs.capsule(f"Bone{s}", (-0.06 * s, -0.142, 0.08), (0.06 * s, -0.142, 0.135), 0.011, white, seg=5,
                            cap=1, sub=1))
    P.append(gs.tube("Feather", [(0.17, 0.0, 0.16), (0.26, 0.05, 0.27), (0.36, 0.13, 0.32)], [0.03, 0.04, 0.008],
                     red, seg=5, cap=2, sub=2, flatten=((0, 1, 0), 0.3)))
    return P


def hat_explorer():
    khaki, band, dark = gs.mat("#D9C08B"), gs.mat("#6B4226"), gs.mat("#B79A62")
    P = [gs.lathe("Dome", [(0.21, 0.02), (0.214, 0.1), (0.185, 0.19), (0.12, 0.25), (0.0, 0.27)], khaki, seg=18)]
    brim = gs.lathe("Brim", [(0.2, 0.05), (0.31, 0.022), (0.33, 0.012), (0.32, 0.0), (0.2, 0.025)], dark, seg=18)
    deform(brim, lambda c: Vector((c.x, c.y * 1.08, c.z - 0.03 * (c.y / 0.33) ** 2)))
    P.append(brim)
    P.append(gs.lathe("Band", [(0.214, 0.035), (0.217, 0.08)], band, seg=18))
    P.append(gs.ellipsoid("Vent", (0, 0, 0.272), (0.03, 0.03, 0.018), dark, seg=8, rings=4))
    P.append(gs.ellipsoid("Feather", (0.2, -0.04, 0.1), (0.016, 0.012, 0.08), gs.mat("#D7263D"), seg=6, rings=4,
                          rot=("Y", -25)))
    return P


def hat_flower_crown():
    leaf, leaf2 = gs.mat("#3FA34D"), gs.mat("#2D7A3A")
    cols = ["#FF6FA8", "#FFD23F", "#FFFFFF", "#9B5DE5", "#FF8C42"]
    P = [gs.torus("Vine", (0, 0, 0.035), 0.205, 0.022, leaf2, seg=20, mseg=5, scale=(1.0, 1.05, 1.0))]
    for i, a in enumerate((-130, -50, 10, 70, 150, 215)):
        r = math.radians(a)
        P.append(gs.ellipsoid(f"Leaf{i}", (0.215 * math.cos(r), 0.215 * math.sin(r) * 1.05, 0.05),
                              (0.05, 0.022, 0.01), leaf, seg=6, rings=3, rot=[("Y", 30), ("Z", a + 90)]))
    for i, a in enumerate((-90, -40, -140, 20, -200)):
        r = math.radians(a)
        c = Vector((0.212 * math.cos(r), 0.212 * math.sin(r) * 1.05, 0.06))
        n = Vector((math.cos(r), math.sin(r) * 1.05, 0.9)).normalized()
        fl = gs.lathe(f"Flower{i}", [(0.0, 0.012), (0.055, 0.0), (0.0, -0.006)], gs.mat(cols[i]), seg=15)
        deform(fl, lambda v: Vector((v.x * (0.62 + 0.45 * abs(math.cos(2.5 * math.atan2(v.y, v.x)))),
                                     v.y * (0.62 + 0.45 * abs(math.cos(2.5 * math.atan2(v.y, v.x)))), v.z)))
        fl.data.transform(Matrix.Translation(c) @ n.to_track_quat("Z", "Y").to_matrix().to_4x4())
        P.append(fl)
        P.append(gs.ellipsoid(f"Center{i}", c + n * 0.012, 0.016, gs.mat("#FFB627" if cols[i] != "#FFD23F"
                                                                         else "#E2394B"), seg=6, rings=3))
    return P


def hat_chef():
    white, shade, band = gs.mat("#F7F4EC"), gs.mat("#E2E6EC"), gs.mat("#D7263D")
    prof = [(0.205, 0.0), (0.208, 0.09), (0.215, 0.17), (0.26, 0.24), (0.27, 0.3), (0.24, 0.355), (0.16, 0.385),
            (0.07, 0.39), (0.0, 0.385)]
    top = gs.lathe("Toque", prof, [white, shade], seg=18)

    def pleat(v):
        a = math.atan2(v.y, v.x)
        k = 1 + (0.08 * math.cos(6 * a) if v.z > 0.16 else 0.0)
        return Vector((v.x * k, v.y * k, v.z + (0.015 * math.cos(6 * a) if v.z > 0.34 else 0.0)))
    deform(top, pleat)
    gs.paint_faces(top, lambda c: c.z < 0.17 and (math.degrees(math.atan2(c.y, c.x)) % 30) < 6, 1)
    P = [top, gs.lathe("Band", [(0.211, 0.02), (0.213, 0.05)], band, seg=18)]
    return P


def hat_cowboy():
    brown, dark, band = gs.mat("#9C5A2C"), gs.mat("#7A4420"), gs.mat("#3B2A20")
    crown = gs.lathe("Crown", [(0.19, 0.0), (0.2, 0.08), (0.19, 0.18), (0.165, 0.245), (0.08, 0.235),
                               (0.0, 0.205)], brown, seg=16)
    deform(crown, lambda c: Vector((c.x * (1 - 0.18 * max(0.0, -c.y / 0.19) * max(0.0, c.z - 0.1) / 0.1),
                                    c.y * 1.12, c.z)))
    brim = gs.lathe("Brim", [(0.185, 0.025), (0.34, 0.018), (0.37, 0.03), (0.365, 0.01), (0.185, 0.005)], dark,
                    seg=20)
    deform(brim, lambda c: Vector((c.x * 0.95, c.y * 1.1, c.z + 0.085 * (abs(c.x) / 0.37) ** 2
                                   - 0.025 * (abs(c.y) / 0.37) ** 2)))
    P = [crown, brim, gs.lathe("Band", [(0.198, 0.03), (0.2, 0.065)], band, seg=16)]
    P.append(gs.ellipsoid("Buckle", (0, -0.218, 0.048), (0.024, 0.008, 0.02), gs.mat("gold"), seg=6, rings=3))
    return P


def hat_golden_crown():
    gold = gs.mat("#FFC83A", prefix="M_Golden", rough=0.25, emission=0.35)
    dark = gs.mat("gold_dark", rough=0.3)
    gems = [gs.mat("gem_red", rough=0.15), gs.mat("#3A86FF", rough=0.15), gs.mat("gem_green", rough=0.15)]
    P = [gs.lathe("Band", [(0.2, 0.0), (0.212, 0.0), (0.218, 0.03), (0.215, 0.1), (0.2, 0.1)], gold, seg=16)]
    P.append(gs.torus("Rim", (0, 0, 0.004), 0.214, 0.012, dark, seg=16, mseg=4))
    for k in range(5):
        a = math.radians(-90 + k * 72)
        d = Vector((math.cos(a), math.sin(a), 0))
        base = d * 0.208 + Vector((0, 0, 0.09))
        tip = d * 0.225 + Vector((0, 0, 0.22))
        P.append(gs.cone(f"Point{k}", base, tip, 0.05, 0.01, gold, seg=6, round_tip=False))
        P.append(gs.ellipsoid(f"Ball{k}", tip + Vector((0, 0, 0.015)), 0.022, dark, seg=6, rings=4))
        P.append(gs.ellipsoid(f"Gem{k}", d * 0.222 + Vector((0, 0, 0.05)), (0.026, 0.012, 0.03), gems[k % 3],
                              seg=6, rings=4, rot=("Z", math.degrees(a) + 90)))
    return P


HATS = [("H01_ZooCap", "Gorra del zoológico", hat_zoo_cap), ("H02_DivingGoggles", "Gafas de buceo", hat_goggles),
        ("H03_PirateHat", "Sombrero pirata", hat_pirate), ("H04_ExplorerHelmet", "Casco de explorador", hat_explorer),
        ("H05_FlowerCrown", "Corona de flores", hat_flower_crown), ("H06_ChefHat", "Gorro de chef", hat_chef),
        ("H07_CowboyHat", "Sombrero vaquero", hat_cowboy), ("H08_GoldenCrown", "Corona dorada", hat_golden_crown)]


def build_hat(name):
    """Construye un sombrero en la escena actual y devuelve el objeto (pivote en la base)."""
    fn = dict((n, f) for n, _, f in HATS)[name]
    o = join(name, fn())
    for f in o.data.polygons:
        f.use_smooth = True
    zmin = min(v.co.z for v in o.data.vertices)
    o.data.transform(Matrix.Translation((0, 0, -zmin)))  # pivote exactamente en la base
    return o


# ---------------------------------------------------------------------------
# Balsas
# ---------------------------------------------------------------------------
def circle(R):
    return lambda a: R


def ellipse(rx, ry):
    return lambda a: 1.0 / math.sqrt((math.cos(a) / rx) ** 2 + (math.sin(a) / ry) ** 2)


def superellipse(rx, ry, n=5.0):
    return lambda a: (abs(math.cos(a) / rx) ** n + abs(math.sin(a) / ry) ** n) ** (-1.0 / n)


def boat(rx, ry, bow=0.35):
    """Óvalo con proa en punta hacia el norte (-Y)."""
    e = ellipse(rx, ry)
    return lambda a: e(a) * (1 + bow * max(0.0, -math.sin(a)) ** 6)


def contour(fn, a0, a1, n, z, inset=0.0, zfn=None):
    out = []
    for i in range(n):
        a = math.radians(a0 + (a1 - a0) * i / (n - 1))
        r = fn(a) - inset
        out.append(Vector((r * math.cos(a), r * math.sin(a), z + (zfn(a) if zfn else 0.0))))
    return out


def slab(name, fn, z0, z1, mats, n=40, inset=0.0, skip=None):
    """Piso: polígono del contorno con espesor (abanico desde el centro). skip=(a0, a1) deja una muesca."""
    bm = bmesh.new()
    angs = [i / n * 360.0 for i in range(n)]
    if skip:
        angs = [a for a in angs if not (skip[0] < a < skip[1])] + [skip[0], skip[1]]
        angs.sort()
    top, bot = [], []
    for a in angs:
        r = fn(math.radians(a)) - inset
        c, s = math.cos(math.radians(a)), math.sin(math.radians(a))
        top.append(bm.verts.new((r * c, r * s, z1)))
        bot.append(bm.verts.new((r * c, r * s, z0)))
    ct = bm.verts.new((0, 0, z1))
    cb = bm.verts.new((0, 0, z0))
    m = len(angs)
    for i in range(m):
        j = (i + 1) % m
        if skip and angs[i] == skip[0]:
            continue
        bm.faces.new([ct, top[i], top[j]])
        bm.faces.new([cb, bot[j], bot[i]])
        bm.faces.new([top[i], bot[i], bot[j], top[j]])
    if skip:
        i = angs.index(skip[0])
        j = (i + 1) % m
        bm.faces.new([ct, top[i], bot[i], cb])
        bm.faces.new([ct, cb, bot[j], top[j]])
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    for mm in (mats if isinstance(mats, (list, tuple)) else [mats]):
        me.materials.append(mm)
    bpy.context.scene.collection.objects.link(o)
    o["bone"] = "auto"
    return o


def bowl(name, fn, z_top, depth, mats, n=28, prof=((1.0, 0.0), (0.97, 0.25), (0.86, 0.55), (0.62, 0.82),
                                                     (0.3, 0.97), (0.0, 1.0))):
    """Casco bajo el borde: anillos del contorno que se cierran hacia abajo."""
    bm = bmesh.new()
    rings = []
    for s, d in prof:
        z = z_top - d * depth
        if s < 1e-6:
            rings.append([bm.verts.new((0, 0, z))] * n)
            continue
        ring = []
        for i in range(n):
            a = i / n * 2 * math.pi
            r = fn(a) * s
            ring.append(bm.verts.new((r * math.cos(a), r * math.sin(a), z)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(n):
            q = list(dict.fromkeys([a[i], b[i], b[(i + 1) % n], a[(i + 1) % n]]))
            if len(q) >= 3:
                bm.faces.new(q)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    o = bpy.data.objects.new(name, me)
    for mm in (mats if isinstance(mats, (list, tuple)) else [mats]):
        me.materials.append(mm)
    bpy.context.scene.collection.objects.link(o)
    o["bone"] = "auto"
    return o


def chambers(fn, r, zc, mats, n=9, seg=10, gap=4, mat_fn=None, zfn=None, flatten=None, skip=None, cap=3):
    """Cuatro cámaras a lo largo del contorno, con pivote en el centro de su cuadrante."""
    out = []
    for name, a0, a1 in QUADS:
        lo, hi = a0 + gap, a1 - gap
        if skip:
            if a0 < skip[0] < a1:
                hi = min(hi, skip[0])
            if a0 < skip[1] < a1:
                lo = max(lo, skip[1])
        pts = contour(fn, lo, hi, n, zc, zfn=zfn)
        ch = gs.tube(f"Chamber_{name}", pts, r, mats, seg=seg, cap=cap, sub=1, mat_fn=mat_fn, flatten=flatten)
        out.append((name, a0, a1, ch))
    return out


def finish_chamber(name, a0, a1, parts, fn, zc):
    ch = join(f"Chamber_{name}", parts)
    am = math.radians((a0 + a1) / 2)
    R = fn(am)
    set_origin(ch, (math.cos(am) * R * 0.92, math.sin(am) * R * 0.92, zc))
    return ch


def quad_of(angle_deg):
    a = angle_deg % 360
    for name, a0, a1 in QUADS:
        if a0 <= a < a1 or (a1 == 360 and a0 <= a < 360):
            return name
    return QUADS[0][0]


def raft_lilypad():
    pad, vein = gs.mat("#5DBB4A"), gs.mat("#3E9B35")
    rim, under = gs.mat("#7ED35F"), gs.mat("#2F7A2C")
    pink, pink2, yel = gs.mat("#FF8FB8"), gs.mat("#FFC2D9"), gs.mat("#FFD23F")
    fn = lambda a: 1.95 + 0.06 * math.sin(a * 7)
    notch = (78, 102)
    floor = slab("Raft_Floor", fn, 0.0, 0.14, [pad, vein], n=56, skip=notch)
    gs.paint_faces(floor, lambda c: c.z > 0.13 and (math.degrees(math.atan2(c.y, c.x)) % 30) < 4.5, 1)
    ch = chambers(fn, 0.24, 0.3, [rim, vein], n=10, seg=9, skip=notch,
                  zfn=lambda a: 0.05 * math.sin(a * 5), mat_fn=lambda t: 0)
    decos = {}
    # flor de loto
    fc = Vector((-1.15, -1.1, 0.5))
    lotus = []
    for k in range(8):
        a = k / 8 * 360
        r = math.radians(a)
        lotus.append(gs.ellipsoid(f"Petal{k}", fc + Vector((math.cos(r) * 0.16, math.sin(r) * 0.16, 0.1)),
                                  (0.17, 0.07, 0.05), pink if k % 2 else pink2, seg=8, rings=4,
                                  rot=[("Y", -40), ("Z", a)]))
    lotus.append(gs.ellipsoid("LotusCenter", fc + Vector((0, 0, 0.16)), (0.09, 0.09, 0.05), yel, seg=8, rings=4))
    decos["0_NE"] = lotus
    drops = [gs.ellipsoid(f"Drop{k}", (x, y, 0.16), (0.06, 0.06, 0.03), gs.mat("#BDEBFF", rough=0.1), seg=8,
                          rings=4) for k, (x, y) in enumerate(((0.6, 0.4), (-0.3, 0.9), (0.9, -0.6)))]
    under_o = bowl("Underside", fn, 0.0, 0.12, under, n=28, prof=((1.0, 0.0), (0.7, 0.8), (0.0, 1.0)))
    return fn, 0.3, floor, ch, decos, drops + [under_o]


def raft_crate():
    wood, wood2, dark = gs.mat("#C8955A"), gs.mat("#A8743E"), gs.mat("#6B4A2A")
    stencil, nail = gs.mat("#2B2B33"), gs.mat("metal")
    fn = superellipse(1.85, 1.85, 6.0)
    floor = slab("Raft_Floor", fn, 0.0, 0.14, [wood, wood2], n=24)
    gs.bands_by_plane(floor, (1, 0, 0), (0, 0, 0), 0.46, 0.5, 1)
    ch = []
    for name, a0, a1 in QUADS:
        parts = []
        for zc, rr in ((0.24, 0.12), (0.5, 0.11)):
            pts = contour(fn, a0 + 3, a1 - 3, 12, zc, inset=0.05)
            parts.append(gs.tube(f"Slat{name}{zc}", pts, rr, wood, seg=4, cap=1, sub=1,
                                 flatten=((0, 0, 1), 0.75)))
        corner = math.radians((a0 + a1) / 2)
        cp = Vector((math.cos(corner), math.sin(corner), 0)) * (fn(corner) - 0.12)
        parts.append(gs.rounded_box(f"Post{name}", cp + Vector((0, 0, 0.33)), (0.24, 0.24, 0.66), 0.04, dark,
                                    segments=1, rot=("Z", math.degrees(corner))))
        for zc in (0.24, 0.5):
            parts.append(gs.ellipsoid(f"Nail{name}{zc}", cp * 1.04 + Vector((0, 0, zc)), 0.025, nail, seg=6,
                                      rings=3))
        ch.append((name, a0, a1, parts))
    # estarcidos: huella y flechas "este lado arriba" en las caras exteriores
    decos = {}

    def paw(c, n, s=1.0):
        q = n.to_track_quat("Z", "Y").to_matrix().to_4x4()
        out = [gs.ellipsoid("PawMain", c, (0.11 * s, 0.09 * s, 0.012), stencil, seg=10, rings=4, rot=q)]
        up = Vector((0, 0, 1))
        side = n.cross(up).normalized()
        for k, (dx, dz) in enumerate(((-0.12, 0.1), (-0.045, 0.16), (0.045, 0.16), (0.12, 0.1))):
            out.append(gs.ellipsoid(f"PawToe{k}", c + side * dx * s + up * dz * s, (0.04 * s, 0.035 * s, 0.01),
                                    stencil, seg=8, rings=3, rot=q))
        return out
    for a in (270, 0):  # norte y oeste
        r = math.radians(a)
        n = Vector((math.cos(r), math.sin(r), 0))
        c = n * (fn(r) + 0.075) + Vector((0, 0, 0.32))
        decos.setdefault(quad_of(a - 8), []).extend(paw(c, n))
    for a in (180, 90):  # este y sur: flechas
        r = math.radians(a)
        n = Vector((math.cos(r), math.sin(r), 0))
        side = n.cross(Vector((0, 0, 1))).normalized()
        for k in (-1, 1):
            c = n * (fn(r) + 0.07) + side * 0.22 * k + Vector((0, 0, 0.3))
            arrow = [gs.capsule("ArrowStem", c - Vector((0, 0, 0.12)), c + Vector((0, 0, 0.06)), 0.025, stencil,
                                seg=6, cap=1, sub=1),
                     gs.cone("ArrowHead", c + Vector((0, 0, 0.05)), c + Vector((0, 0, 0.17)), 0.07, 0.0, stencil,
                             seg=4, round_tip=False)]
            decos.setdefault(quad_of(a + 8 * k), []).extend(arrow)
    return fn, 0.37, floor, ch, decos, []


def raft_log():
    bark, bark2 = gs.mat("#7A5232"), gs.mat("#5E3D22")
    wood, ring = gs.mat("#E2B97A"), gs.mat("#C08A4E")
    moss, cap, cap2 = gs.mat("#6FAE3A"), gs.mat("#E2394B"), gs.mat("#F7F4EC")
    fn = ellipse(1.6, 2.2)
    floor = slab("Raft_Floor", fn, 0.02, 0.15, [wood, ring], n=28, inset=0.15)
    gs.bands_by_plane(floor, (1, 0, 0), (0, 0, 0), 0.34, 0.18, 1)
    ch = chambers(fn, 0.32, 0.3, [bark, bark2], n=10, seg=9,
                  mat_fn=lambda t: 1 if int(t * 7) % 2 else 0)
    hull = bowl("Hull", fn, 0.3, 0.45, bark2, n=24, prof=((1.04, 0.0), (1.0, 0.5), (0.8, 0.85), (0.0, 1.0)))
    decos = {}
    # extremos cortados (anillos de crecimiento) en proa y popa
    for a in (270, 90):
        r = math.radians(a)
        d = Vector((math.cos(r), math.sin(r), 0))
        c = d * (fn(r) + 0.24) + Vector((0, 0, 0.3))
        q = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
        parts = [gs.ellipsoid("EndCut", c, (0.32, 0.32, 0.03), wood, seg=12, rings=4, rot=q),
                 gs.ellipsoid("EndRing", c + d * 0.012, (0.2, 0.2, 0.025), ring, seg=12, rings=4, rot=q),
                 gs.ellipsoid("EndCore", c + d * 0.022, (0.08, 0.08, 0.02), wood, seg=8, rings=3, rot=q)]
        decos.setdefault(quad_of(a - 5), []).extend(parts)
    shroom = Vector((1.35, 0.9, 0.55))
    decos.setdefault(quad_of(math.degrees(math.atan2(0.9, 1.35))), []).extend([
        gs.capsule("Stalk", shroom - Vector((0, 0, 0.1)), shroom + Vector((0, 0, 0.08)), 0.04, cap2, seg=6, sub=1),
        gs.ellipsoid("Cap", shroom + Vector((0, 0, 0.1)), (0.13, 0.13, 0.07), cap, seg=10, rings=5),
        gs.ellipsoid("Dot", shroom + Vector((0.05, -0.06, 0.15)), 0.025, cap2, seg=6, rings=3)])
    branch = Vector((-1.5, -0.6, 0.45))
    decos.setdefault(quad_of(math.degrees(math.atan2(-0.6, -1.5))), []).extend([
        gs.tube("Branch", [branch, branch + Vector((-0.15, -0.1, 0.3)), branch + Vector((-0.1, -0.25, 0.55))],
                [0.05, 0.035, 0.02], bark, seg=6, cap=2, sub=2),
        gs.ellipsoid("Leaf", branch + Vector((-0.05, -0.32, 0.6)), (0.1, 0.04, 0.02), moss, seg=8, rings=3,
                     rot=("X", 30))])
    for k in range(3):
        a = math.radians(200 + k * 45)
        p = Vector((math.cos(a), math.sin(a), 0)) * fn(a) + Vector((0, 0, 0.55))
        decos.setdefault(quad_of(math.degrees(a)), []).append(
            gs.ellipsoid(f"Moss{k}", p, (0.2, 0.16, 0.06), moss, seg=8, rings=4))
    return fn, 0.3, floor, ch, decos, [hull]


def raft_coconut():
    shell, shell2 = gs.mat("#6B4226"), gs.mat("#4A2C17")
    flesh, flesh2 = gs.mat("#FFFDF4"), gs.mat("#EDE4CF")
    fn = circle(1.9)
    floor = slab("Raft_Floor", fn, 0.05, 0.16, [flesh, flesh2], n=40, inset=0.18)
    gs.paint_faces(floor, lambda c: c.z > 0.15 and abs(math.hypot(c.x, c.y) - 1.25) < 0.06, 1)
    ch = chambers(fn, 0.28, 0.32, [flesh, shell], n=9, seg=10,
                  flatten=None, mat_fn=lambda t: 0)
    for name, a0, a1, c in ch:
        # mitad exterior marrón, mitad interior blanca
        gs.paint_faces(c, lambda p: math.hypot(p.x, p.y) > 1.9 + 0.05 or p.z < 0.2, 1)
    hull = bowl("Shell", fn, 0.3, 0.8, [shell, shell2], n=28)
    gs.paint_faces(hull, lambda c: int((math.degrees(math.atan2(c.y, c.x)) + 360) / 12) % 3 == 0, 1)
    decos = {}
    # sombrilla de cóctel clavada en el borde
    base = Vector((1.35, -1.25, 0.45))
    top = base + Vector((0.15, -0.05, 1.0))
    umb = [gs.capsule("Stick", base, top, 0.025, gs.mat("#F7F4EC"), seg=6, sub=1, cap=1)]
    cols = [gs.mat("#FF6FA8"), gs.mat("#FFD23F")]
    canopy = gs.lathe("Canopy", [(0.0, 0.18), (0.25, 0.1), (0.48, -0.02), (0.47, -0.04), (0.0, 0.1)], cols,
                      loc=top, seg=12, mat_fn=lambda i: 0)
    gs.paint_faces(canopy, lambda c: int((math.degrees(math.atan2(c.y - top.y, c.x - top.x)) + 360) / 30) % 2 == 0, 1)
    umb.append(canopy)
    decos[quad_of(math.degrees(math.atan2(-1.25, 1.35)))] = umb
    straw = [gs.tube("Straw", [(-1.4, 1.3, 0.3), (-1.55, 1.42, 0.9), (-1.75, 1.42, 1.0)], 0.035,
                     [gs.mat("#3A86FF"), gs.mat("#F7F4EC")], seg=6, cap=1, sub=2,
                     mat_fn=lambda t: int(t * 8) % 2)]
    decos[quad_of(math.degrees(math.atan2(1.3, -1.4)))] = straw
    return fn, 0.32, floor, ch, decos, [hull]


def raft_lifeboat():
    white, red = gs.mat("#F7F4EC"), gs.mat("#E2394B")
    hullc, floor_c, rope = gs.mat("#FF7A1A"), gs.mat("#2F4858"), gs.mat("rope", rough=0.9)
    fn = boat(1.72, 2.0, 0.28)
    floor = slab("Raft_Floor", fn, 0.02, 0.13, [floor_c, gs.mat("#3E5C6F")], n=28, inset=0.15)
    gs.bands_by_plane(floor, (0, 1, 0), (0, 0, 0), 0.3, 0.12, 1)
    ch = chambers(fn, 0.27, 0.36, [hullc, white], n=10, seg=10,
                  mat_fn=lambda t: 1 if (t * 3) % 1 < 0.16 else 0)
    hull = bowl("Hull", fn, 0.3, 0.55, [white, red], n=24,
                prof=((1.1, 0.0), (1.08, 0.2), (1.0, 0.45), (0.8, 0.8), (0.0, 1.0)))
    gs.paint_faces(hull, lambda c: -0.02 < c.z < 0.08, 1)
    decos = {}
    # aro salvavidas y banderín
    lr_c = Vector((1.45, 0.95, 0.55))
    ring = gs.torus("LifeRing", lr_c, 0.24, 0.08, [white, red], seg=16, mseg=5, axis=(1, 0.7, 0.3))
    gs.paint_faces(ring, lambda c: int((math.degrees(math.atan2(c.z - lr_c.z, c.y - lr_c.y)) + 360) / 45) % 2 == 0, 1)
    decos[quad_of(math.degrees(math.atan2(0.95, 1.45)))] = [ring]
    bow = Vector((0, -2.2, 0.55))
    decos[quad_of(272)] = [gs.capsule("Mast", bow, bow + Vector((0, 0, 0.9)), 0.03, white, seg=6, sub=1, cap=1),
                           gs.cone("Pennant", bow + Vector((0, 0, 0.82)), bow + Vector((0.55, 0.05, 0.72)), 0.12, 0.0,
                                   red, seg=4, round_tip=False)]
    rope_pts = contour(fn, 0, 345, 24, 0.3, inset=-0.26)
    sag = [p + Vector((0, 0, -0.06 * abs(math.sin(i * math.pi / 4)))) for i, p in enumerate(rope_pts)]
    deco_rope = gs.tube("GrabLine", sag + [sag[0]], 0.022, rope, seg=5, cap=1, sub=1)
    return fn, 0.32, floor, ch, decos, [hull, deco_rope]


def raft_watermelon():
    rind, stripe = gs.mat("#2E8B3E"), gs.mat("#1B5E2A")
    white, red, seed = gs.mat("#E8F5C8"), gs.mat("#F2545B"), gs.mat("#1E1E24")
    fn = ellipse(1.85, 2.05)
    floor = slab("Raft_Floor", fn, 0.06, 0.16, red, n=44, inset=0.2)
    ch = chambers(fn, 0.28, 0.32, [white, rind], n=10, seg=10, mat_fn=lambda t: 0)
    for name, a0, a1, c in ch:
        gs.paint_faces(c, lambda p, fn=fn: math.hypot(p.x, p.y) > fn(math.atan2(p.y, p.x)) + 0.06 or p.z < 0.22, 1)
    hull = bowl("Rind", fn, 0.3, 0.75, [rind, stripe], n=32,
                prof=((1.0, 0.0), (0.97, 0.25), (0.86, 0.55), (0.62, 0.82), (0.3, 0.97), (0.0, 1.0)))
    gs.paint_faces(hull, lambda c: (math.degrees(math.atan2(c.y, c.x)) + 360 + 8 * math.sin(c.z * 9)) % 30 < 11, 1)
    rnd = random.Random(4)
    seeds = []
    for k in range(14):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(0.35, 1.3)
        p = Vector((math.cos(a) * r, math.sin(a) * r * 1.1, 0.165))
        seeds.append(gs.ellipsoid(f"Seed{k}", p, (0.05, 0.03, 0.02), seed, seg=6, rings=3,
                                  rot=("Z", math.degrees(a))))
    decos = {}
    bite = Vector((-1.7, 0.6, 0.45))
    decos[quad_of(math.degrees(math.atan2(0.6, -1.7)))] = [
        gs.ellipsoid("Juice", bite + Vector((0.1, 0, -0.08)), (0.12, 0.12, 0.05), red, seg=8, rings=4)]
    return fn, 0.32, floor, ch, decos, [hull] + seeds


RAFTS = [("R01_LilyPad", "Hoja de nenúfar", raft_lilypad), ("R02_ZooCrate", "Caja de embalaje del zoológico",
                                                                raft_crate),
         ("R03_HollowLog", "Tronco hueco", raft_log), ("R04_CoconutFloat", "Flotador de coco", raft_coconut),
         ("R05_Lifeboat", "Bote salvavidas", raft_lifeboat), ("R06_WatermelonRind", "Cáscara de sandía",
                                                              raft_watermelon)]


def build_raft(name):
    """Construye una balsa en la escena actual. Devuelve (raíz, [objetos])."""
    fn_build = dict((n, f) for n, _, f in RAFTS)[name]
    fn, zc, floor, ch, decos, extra = fn_build()
    root = bpy.data.objects.new("Raft_" + name.split("_", 1)[1], None)
    gs._link(root)
    objs = []
    for item in ch:
        nm, a0, a1, parts = item
        parts = parts if isinstance(parts, list) else [parts]
        parts = parts + decos.pop(nm, [])
        c = finish_chamber(nm, a0, a1, parts, fn, zc)
        c.parent = root
        objs.append(c)
    floor = join("Raft_Floor", [floor])
    floor.parent = root
    objs.append(floor)
    rest = extra + [p for v in decos.values() for p in v]
    if rest:
        d = join("Raft_Deco", rest)
        d.parent = root
        objs.append(d)
    for o in objs:
        for p in o.data.polygons:
            p.use_smooth = True
    return root, objs


# ---------------------------------------------------------------------------
# Exportación, verificación y hojas de contacto
# ---------------------------------------------------------------------------
def export_hats():
    tiles = []
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    for name, label, _ in HATS:
        gs.reset_scene()
        o = build_hat(name)
        tris = gs.tri_count(o)
        lo, hi = gs._world_bbox([o])
        print(f"[gs] {name}: {tris} triángulos, base z={lo.z:.3f}, alto={hi.z:.3f}")
        assert tris <= HAT_TRIS, f"{name}: {tris}"
        assert abs(lo.z) < 1e-4, f"{name}: pivote no está en la base ({lo.z})"
        gs.export_fbx([o], os.path.join(OUT, "Hats", name + ".fbx"), animated=False)
        sc, cam = gs._setup_render(360)
        p = os.path.join(gs.PREVIEW_DIR, f"_tmp_hat_{name}.png")
        gs._render_view(p, [o], (0.75, -0.9, 0.45), cam, margin=1.3)
        tiles.append(p)
    gs.reset_scene()
    out = os.path.join(gs.PREVIEW_DIR, "Cosmetics_Hats_sheet.png")
    gs.stitch(tiles, out, cols=4)
    for p in tiles:
        os.remove(p)
    print(f"[gs] preview {out}")


def export_rafts():
    tiles = []
    for name, label, _ in RAFTS:
        gs.reset_scene()
        root, objs = build_raft(name)
        tris = sum(gs.tri_count(o) for o in objs)
        names = sorted(o.name for o in objs)
        print(f"[gs] {name}: {tris} triángulos, objetos={names}")
        assert tris <= RAFT_TRIS, f"{name}: {tris}"
        for q, _, _ in QUADS:
            assert f"Chamber_{q}" in names, f"{name}: falta Chamber_{q}"
        gs.export_fbx([root] + objs, os.path.join(OUT, "Rafts", name + ".fbx"), animated=False)
        sc, cam = gs._setup_render(420)
        for vn, d in (("3q", (0.75, -0.8, 0.6)), ("top", (0.001, -0.001, 1))):
            p = os.path.join(gs.PREVIEW_DIR, f"_tmp_raft_{name}_{vn}.png")
            gs._render_view(p, objs, d, cam, margin=1.1)
            tiles.append(p)
    gs.reset_scene()
    out = os.path.join(gs.PREVIEW_DIR, "Cosmetics_Rafts_sheet.png")
    gs.stitch(tiles, out, cols=4)
    for p in tiles:
        os.remove(p)
    print(f"[gs] preview {out}")
    if "--no-verify" not in sys.argv:
        for name, _, _ in RAFTS:
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.fbx(filepath=os.path.join(OUT, "Rafts", name + ".fbx"))
            got = sorted(o.name for o in bpy.context.scene.objects)
            print(f"[gs] verify {name}: {got}")
            for q, _, _ in QUADS:
                assert f"Chamber_{q}" in got


def fit_sheet():
    """Hoja de contacto con los sombreros puestos (toon) y anclajes por personaje (hat_anchors.json)."""
    import gs_render as R
    R.ensure_cache(raft=False)
    anchors = {}
    tiles = []
    combos = [(h, R.CHARS[i % 5]) for i, (h, _, _) in enumerate(HATS)] + [("H07_CowboyHat", "Melman"),
                                                                          ("H02_DivingGoggles", "Gloria")]
    for hat, ch in combos:
        R.new_scene((480, 480), transparent=True, samples=16)
        a = R.Actor(ch).pose("Select_Pose", 0.1)
        o = build_hat(hat)
        a.wear(o)
        if ch not in anchors:
            top, k = a.hat_anchor()
            anchors[ch] = {"headTop": [0.0, round(top.z, 4), round(-top.y, 4)], "scale": round(k, 3)}
        R.sun((45, 0, 30), 1.0)
        hm = a.head_matrix() @ a.hat_anchor()[0]
        R.camera(hm + Vector((0.9, -1.6, 0.25)), hm + Vector((0, 0, -0.12)), 50)
        p = os.path.join(gs.PREVIEW_DIR, f"_tmp_fit_{hat}_{ch}.png")
        R.render(p, px=2.0)
        tiles.append(p)
    out = os.path.join(gs.PREVIEW_DIR, "Cosmetics_HatFit_sheet.png")
    R.contact_sheet(tiles, out, cols=5, cell=(300, 300))
    for p in tiles:
        os.remove(p)
    tiles = []
    for name, _, _ in RAFTS:
        R.new_scene((640, 400), samples=16)
        R.water(120)
        root, objs = build_raft(name)
        root.location.z = 0.05
        R.sun((50, 0, 35), 1.0)
        R.camera((4.2, -5.2, 3.6), (0, 0, 0.1), 40)
        p = os.path.join(gs.PREVIEW_DIR, f"_tmp_rtoon_{name}.png")
        R.render(p, px=2.0)
        tiles.append(p)
    out = os.path.join(gs.PREVIEW_DIR, "Cosmetics_Rafts_toon_sheet.png")
    R.contact_sheet(tiles, out, cols=3, cell=(480, 300))
    for p in tiles:
        os.remove(p)
    path = os.path.join(OUT, "Hats", "hat_anchors.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"_nota": "Parte superior de la cabeza en reposo, espacio del modelo en Unity (m); "
                            "scale = escala sugerida del sombrero (hechos para cabeza de 0.42 m).",
                   "personajes": anchors}, f, indent=2, ensure_ascii=False)
    print(f"[gs] anclajes {path}")


if __name__ == "__main__":
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    if "--rafts" not in sys.argv and "--fit" not in sys.argv:
        export_hats()
    if "--hats" not in sys.argv and "--fit" not in sys.argv:
        export_rafts()
    if "--fit" in sys.argv or ("--hats" not in sys.argv and "--rafts" not in sys.argv):
        fit_sheet()
