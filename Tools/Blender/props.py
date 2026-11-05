"""Balsa naranja (4 cámaras de aire + cuerda) y los 7 power-ups de docs/05.
blender --background --python props.py

Balsa: radio 2 m, alto 0.6 m (docs/02 §1). Cada cámara es un objeto aparte con pivote en su centro,
para la deformación por escala del cuadrante reventado. Nombres con el índice de RaftChambers:
0 NE, 1 NO, 2 SE, 3 SO (en Unity norte = +Z y este = +X; en Blender norte = -Y y este = -X).
Power-ups: ~0.5 m, pivote en el centro (flotan y giran por código), ≤ 600 triángulos.
"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
from gs_common import Vector

PROPS = os.path.join(gs.EXPORT_DIR, "Props")


def set_origin(obj, point):
    """Mueve el pivote del objeto a 'point' (coordenadas de mundo) sin mover la malla."""
    p = Vector(point)
    obj.data.transform(gs.Matrix.Translation(-p))
    obj.location = p


def join(name, parts):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    o.data.name = name
    for vg in list(o.vertex_groups):
        o.vertex_groups.remove(vg)
    return o


# ---------------------------------------------------------------------------
# Balsa
# ---------------------------------------------------------------------------
QUADS = [("0_NE", 180, 270), ("1_NO", 270, 360), ("2_SE", 90, 180), ("3_SO", 0, 90)]


def build_raft():
    gs.reset_scene()
    orange, dark = gs.mat("raft_orange", rough=0.45), gs.mat("raft_dark", rough=0.5)
    rope, floor_m = gs.mat("rope", rough=0.9), gs.mat("#3E4A57", rough=0.8)
    R, r, zc = 1.7, 0.3, 0.3
    root = bpy.data.objects.new("Raft", None)
    gs._link(root)
    objs = []
    for name, a0, a1 in QUADS:
        pts = [Vector((R * math.cos(math.radians(a)), R * math.sin(math.radians(a)), zc))
               for a in [a0 + 4 + (a1 - a0 - 8) * i / 8 for i in range(9)]]
        ch = gs.tube(f"Chamber_{name}", pts, r, [orange], seg=10, cap=3, sub=1)
        am = math.radians((a0 + a1) / 2)
        # válvula en la parte superior exterior
        vp = Vector((math.cos(am) * (R + 0.12), math.sin(am) * (R + 0.12), zc + 0.24))
        valve = gs.rounded_cylinder(f"Valve_{name}", vp, 0.055, 0.06, 0.02, dark, seg=10,
                                    axis=(math.cos(am) * 0.4, math.sin(am) * 0.4, 1))
        ch = join(f"Chamber_{name}", [ch, valve])
        set_origin(ch, (math.cos(am) * R * 0.92, math.sin(am) * R * 0.92, zc))
        ch.parent = root
        objs.append(ch)
    floor = gs.rounded_cylinder("Raft_Floor", (0, 0, 0.12), 1.62, 0.1, 0.04, floor_m, seg=24, steps=2)
    # flecha de proa (norte = -Y en Blender)
    arrow = gs.lathe("Raft_Arrow", [(0.0, 0.0), (0.16, 0.0), (0.16, 0.012), (0.0, 0.012)], dark,
                     loc=(0, -1.25, 0.171), seg=3, rot=("Z", -90))
    floor = join("Raft_Floor", [floor, arrow])
    floor.parent = root
    objs.append(floor)
    # cuerda alrededor, con caídas entre anclajes
    n_anchor, sub = 12, 4
    path = []
    for i in range(n_anchor * sub + 1):
        t = i / (n_anchor * sub)
        a = t * 2 * math.pi + math.radians(15)
        sag = math.sin((i % sub) / sub * math.pi)
        rad = R + r + 0.03 + 0.05 * sag
        path.append(Vector((rad * math.cos(a), rad * math.sin(a), zc + 0.13 - 0.12 * sag)))
    rope_parts = [gs.tube("Rope", path, 0.024, rope, seg=5, cap=1, sub=1)]
    for k in range(n_anchor):
        a = k / n_anchor * 2 * math.pi + math.radians(15)
        p = Vector(((R + r + 0.01) * math.cos(a), (R + r + 0.01) * math.sin(a), zc + 0.13))
        rope_parts.append(gs.ellipsoid(f"Anchor{k}", p, (0.05, 0.05, 0.04), dark, seg=6, rings=3,
                                       rot=("Z", math.degrees(a))))
    rope_o = join("Raft_Rope", rope_parts)
    rope_o.parent = root
    objs.append(rope_o)
    tris = sum(gs.tri_count(o) for o in objs)
    print(f"[gs] Raft: {tris} triángulos")
    assert tris <= 3000, tris
    sheet = gs.render_sheet("Raft", objs, views=[("front", (0, -1, 0.35)), ("top", (0.001, -0.001, 1)),
                                                 ("3q", (0.75, -0.8, 0.6))])
    gs.export_fbx([root] + objs, os.path.join(PROPS, "Raft", "Raft.fbx"), animated=False)
    return sheet


# ---------------------------------------------------------------------------
# Power-ups
# ---------------------------------------------------------------------------
def mango_shape(name, mats, rnd_tilt=18):
    o = gs.ellipsoid(name, (0, 0, 0), (0.16, 0.13, 0.21), mats, seg=16, rings=10)
    for v in o.data.vertices:  # forma de riñón
        v.co.x += 0.05 * (v.co.z / 0.21) ** 2 - 0.02
    o.data.transform(gs._rot_matrix(("Y", rnd_tilt)))
    return o


def pu_beans():
    can, label = gs.mat("metal", rough=0.35), gs.mat("#D9452B")
    bean = gs.mat("#8A3B1F", rough=0.4)
    P = [gs.rounded_cylinder("Can", (0, 0, 0), 0.16, 0.4, 0.03, can, seg=14, steps=2)]
    P.append(gs.lathe("Label", [(0.163, -0.12), (0.163, 0.12)], label, seg=14))
    P.append(gs.ellipsoid("Emblem", (0, -0.163, 0), (0.07, 0.01, 0.05), gs.mat("#FBE7B0"), seg=8, rings=4))
    for i, (x, y, rz) in enumerate(((-0.04, -0.02, 20), (0.05, 0.01, -35), (0.0, 0.06, 70))):
        P.append(gs.ellipsoid(f"Bean{i}", (x, y, 0.225), (0.045, 0.028, 0.026), bean, seg=6, rings=4,
                              rot=("Z", rz)))
    P.append(gs.ellipsoid("BeanE", (0, -0.168, 0.0), (0.035, 0.008, 0.022), bean, seg=8, rings=4))
    return P


def pu_locoto():
    red, green = gs.mat("#E0281E", rough=0.35), gs.mat("#3E9B35")
    body = gs.lathe("Pepper", [(0, -0.21), (0.07, -0.205), (0.14, -0.15), (0.175, -0.05), (0.18, 0.04),
                               (0.155, 0.11), (0.08, 0.15), (0, 0.155)], red, seg=16)
    for v in body.data.vertices:  # 4 lóbulos suaves
        a = math.atan2(v.co.y, v.co.x)
        k = 1 + 0.07 * math.cos(4 * a) * max(0.0, (0.1 - v.co.z) / 0.3)
        v.co.x *= k
        v.co.y *= k
    P = [body]
    P.append(gs.ellipsoid("Calyx", (0, 0, 0.15), (0.1, 0.1, 0.035), green, seg=10, rings=5))
    P.append(gs.tube("Stem", [(0, 0, 0.16), (0.01, 0, 0.23), (0.06, 0, 0.27)], 0.022, green, seg=6, cap=2, sub=2))
    P.append(gs.ellipsoid("Shine", (-0.08, -0.13, 0.06), (0.03, 0.012, 0.06), gs.mat("#FFB0A0", rough=0.2),
                          seg=8, rings=4, rot=("Z", 30)))
    return P


def pu_mango(golden=False):
    if golden:
        body = [gs.mat("#FFD23F", prefix="M_Golden", rough=0.25, emission=0.6)]
        leaf = gs.mat("#FFE680", prefix="M_Golden", rough=0.25, emission=0.6)
        stem = gs.mat("gold_dark", rough=0.3)
    else:
        body = [gs.mat("#FFB627"), gs.mat("#F2542D"), gs.mat("#9BC53D")]
        leaf, stem = gs.mat("leaf"), gs.mat("#6B4226")
    m = mango_shape("Mango", body)
    if not golden:
        gs.paint_faces(m, lambda c: c.x > 0.06 and c.z < 0.1, 1)
        gs.paint_faces(m, lambda c: c.z > 0.15, 2)
    top = Vector((0.03, 0, 0.215))
    P = [m, gs.capsule("Stem", top, top + Vector((0.005, 0, 0.05)), 0.015, stem, sub=1, seg=6, cap=2)]
    P.append(gs.ellipsoid("Leaf", top + Vector((-0.07, 0, 0.05)), (0.08, 0.03, 0.012), leaf, seg=8, rings=4,
                          rot=[("Y", 25), ("Z", 10)]))
    if golden:
        star = gs.mat("#FFF6C8", prefix="M_Golden", rough=0.2, emission=1.0)
        for i, (p, s) in enumerate((((0.17, -0.1, 0.16), 0.06), ((-0.17, -0.06, -0.12), 0.045))):
            for ax in ("X", "Z"):
                P.append(gs.ellipsoid(f"Star{i}{ax}", p, (s, 0.008, s * 0.18), star, seg=6, rings=3,
                                      rot=("Y", 0 if ax == "X" else 90)))
    return P


def pu_coconut():
    brown, dark = gs.mat("#6B4226"), gs.mat("#2E1C10")
    shield, rim = gs.mat("#59C3FF", rough=0.3), gs.mat("eye_white", rough=0.3)
    P = [gs.ellipsoid("Coconut", (0, 0, 0), (0.19, 0.19, 0.2), [brown], seg=14, rings=9)]
    for k in range(3):
        a = k / 3 * 2 * math.pi + 0.5
        P.append(gs.ellipsoid(f"Eye{k}", (0.045 * math.cos(a), 0.045 * math.sin(a), 0.195), (0.022, 0.022, 0.01),
                              dark, seg=8, rings=4))
    # escudo frontal
    P.append(gs.lathe("ShieldRim", [(0.0, 0.0), (0.12, 0.0), (0.12, 0.025), (0.0, 0.03)], rim, loc=(0, -0.17, 0),
                      axis=(0, -1, 0), seg=14, scale=(0.9, 1.0, 1.0)))
    P.append(gs.lathe("Shield", [(0.0, 0.0), (0.095, 0.0), (0.095, 0.022), (0.0, 0.035)], shield,
                      loc=(0, -0.182, 0), axis=(0, -1, 0), seg=14, scale=(0.9, 1.0, 1.0)))
    P.append(gs.tube("Cross", [(0, -0.222, -0.05), (0, -0.222, 0.05)], 0.014, rim, seg=6, cap=2))
    P.append(gs.tube("Cross2", [(-0.04, -0.222, 0.015), (0.04, -0.222, 0.015)], 0.014, rim, seg=6, cap=2))
    return P


def pu_soda():
    glass, label = gs.mat("#36C98E", rough=0.2), gs.mat("#FF8C1A")
    cap, bub = gs.mat("#E2394B", rough=0.3), gs.mat("#BDEBFF", rough=0.1)
    P = [gs.lathe("Bottle", [(0, -0.25), (0.1, -0.25), (0.115, -0.23), (0.115, 0.03), (0.1, 0.1), (0.055, 0.16),
                             (0.045, 0.2), (0.048, 0.215), (0, 0.215)], glass, seg=14)]
    P.append(gs.lathe("Label", [(0.118, -0.16), (0.118, -0.02)], label, seg=14))
    P.append(gs.ellipsoid("LabelDot", (0, -0.118, -0.09), (0.05, 0.008, 0.04), gs.mat("eye_white"), seg=8, rings=4))
    P.append(gs.rounded_cylinder("Cap", (0, 0, 0.235), 0.052, 0.045, 0.012, cap, seg=10, steps=1))
    for i, (p, s) in enumerate((((0.12, -0.05, 0.2), 0.035), ((0.17, 0.0, 0.29), 0.025),
                                ((0.1, -0.02, 0.34), 0.018))):
        P.append(gs.ellipsoid(f"Bubble{i}", p, s, bub, seg=8, rings=5))
    return P


def pu_onion():
    purple, light = gs.mat("#8E3E8C"), gs.mat("#C58AC0")
    green, root = gs.mat("#5DBB4A"), gs.mat("#E8D9B0")
    o = gs.lathe("Onion", [(0, -0.17), (0.08, -0.165), (0.16, -0.1), (0.19, -0.01), (0.17, 0.07), (0.11, 0.14),
                           (0.05, 0.2), (0.025, 0.24), (0, 0.25)], [purple, light], seg=16)
    gs.paint_faces(o, lambda c: (math.degrees(math.atan2(c.y, c.x)) % 45) < 11, 1)
    P = [o]
    for i, (dx, h) in enumerate(((0.0, 0.14), (0.03, 0.1))):
        P.append(gs.tube(f"Sprout{i}", [(dx * 0.3, 0, 0.23), (dx, 0, 0.23 + h * 0.6), (dx * 2.5, 0, 0.23 + h)],
                         [0.018, 0.014, 0.008], green, seg=6, cap=2, sub=2))
    for k in range(4):
        a = k / 4 * 2 * math.pi
        P.append(gs.capsule(f"Root{k}", (0.02 * math.cos(a), 0.02 * math.sin(a), -0.165),
                            (0.06 * math.cos(a), 0.06 * math.sin(a), -0.21), 0.007, root, seg=5, cap=1, sub=1))
    return P


POWERUPS = [("P01_Beans", pu_beans), ("P02_Locoto", pu_locoto), ("P03_Mango", lambda: pu_mango(False)),
            ("P04_CoconutShield", pu_coconut), ("P05_Soda", pu_soda), ("P06_Onion", pu_onion),
            ("P07_GoldenMango", lambda: pu_mango(True))]


def build_powerups():
    tmp = []
    for name, fn in POWERUPS:
        gs.reset_scene()
        obj = join(name, fn())
        tris = gs.tri_count(obj)
        print(f"[gs] {name}: {tris} triángulos")
        assert tris <= 600, f"{name}: {tris}"
        gs.export_fbx([obj], os.path.join(PROPS, "PowerUps", name + ".fbx"), animated=False)
        p = os.path.join(gs.PREVIEW_DIR, f"_tmp_pu_{name}.png")
        sc, cam = gs._setup_render(360)
        gs._render_view(p, [obj], (0.6, -0.9, 0.35), cam, margin=1.25)
        tmp.append(p)
    out = os.path.join(gs.PREVIEW_DIR, "PowerUps_sheet.png")
    gs.reset_scene()
    gs.stitch(tmp, out, cols=4)
    for p in tmp:
        os.remove(p)
    print(f"[gs] preview {out}")


if __name__ == "__main__":
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    if "--powerups" not in sys.argv:
        build_raft()
    if "--raft" not in sys.argv:
        build_powerups()
