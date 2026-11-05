"""Viñetas de la historia (docs/01, tabla "Historia") y fondos de pantalla de carga, renderizados en Blender.
blender --background --python story.py [-- Intro W1_End ... | --loading | --story]

Viñetas: 3 a 5 imágenes 1920x1080 por momento en Tools/Export/Story/<Momento>/<Momento>_0N.png, armadas con los
personajes (y sus clips), la balsa, los power-ups, el escenario W1-W5 y la fauna ya exportados. Encuadre y luz de
cómic: ángulos picados/contrapicados, cámara inclinada en la acción, luz de contorno, fondos de rayos.
Fondos de carga: Tools/Export/UI/Loading/Loading_W1..W6.png (1920x1080, opacos, el tercio inferior más tranquilo
para el texto y la barra).
Hojas de revisión: Tools/Blender/previews/Story_<Momento>_sheet.png y Loading_sheet.png.
"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import bmesh
import gs_common as gs
import gs_render as R
from gs_common import Vector, Pose

W, H = 1920, 1080


# ---------------------------------------------------------------------------
# Ambientes (luz, cielo, bandas toon)
# ---------------------------------------------------------------------------
def mood(name):
    if name == "night":
        R.world_sky("#081033", "#22386E", ambient=0.24, ambient_col="#7C8CE0", stars=1.2)
        R.sun((52, 0, -40), 0.7, "#A9BCFF", angle=1.0)
        R.sun((70, 0, 150), 0.35, "#FF4FA3", name="Rim", shadow=False)
        R.set_bands(shadow=(0.30, 0.34, 0.62), mid=(0.58, 0.62, 0.90), lit=(0.92, 0.94, 1.08), rim=0.32)
    elif name == "day":
        R.world_sky("#4DB0F0", "#D2F1FF", ambient=0.36)
        R.sun((48, 0, 35), 1.0, "#FFF4DE")
        R.sun((70, 0, 200), 0.3, name="Rim", shadow=False)
        R.set_bands(rim=0.22)
    elif name == "dawn":
        R.world_sky("#3E4F9E", "#FFB38A", ground="#FF9E7A", ambient=0.3, ambient_col="#B9A6FF")
        R.sun((78, 0, 10), 1.0, "#FFC58A")
        R.sun((70, 0, 190), 0.35, "#FF8FB8", name="Rim", shadow=False)
        R.set_bands(shadow=(0.45, 0.4, 0.7), mid=(0.78, 0.7, 0.88), lit=(1.05, 0.98, 0.95), rim=0.3)
    elif name == "storm":
        R.world_sky("#1C2330", "#4A5566", ambient=0.3, ambient_col="#9FB0C8")
        R.sun((55, 0, -30), 0.6, "#C8D6FF")
        R.sun((60, 0, 160), 0.5, "#E8F4FF", name="Rim", shadow=False)
        R.set_bands(shadow=(0.32, 0.38, 0.5), mid=(0.58, 0.64, 0.76), lit=(0.9, 0.95, 1.05), rim=0.4)
    elif name == "party":
        R.world_sky("#120726", "#4A0F4F", ambient=0.26, ambient_col="#B46CFF", stars=0.8)
        R.sun((55, 0, -30), 0.55, "#C9A6FF")
        R.sun((65, 0, 160), 0.45, "#B5E48C", name="Rim", shadow=False)
        R.set_bands(shadow=(0.38, 0.28, 0.6), mid=(0.68, 0.55, 0.9), lit=(1.0, 0.92, 1.08), rim=0.35)
    elif name == "cave":
        R.world_sky("#12061E", "#3C1642", ambient=0.22, ambient_col="#9B5DE5")
        R.sun((60, 0, -20), 0.35, "#4CC9F0")
        R.sun((-60, 0, 0), 0.5, "#FF6A00", name="LavaUp", shadow=False)
        R.set_bands(shadow=(0.4, 0.22, 0.45), mid=(0.75, 0.5, 0.65), lit=(1.1, 0.85, 0.8), rim=0.35)
    elif name == "sunset":
        R.world_sky("#5B3E8E", "#FFB703", ground="#FB8500", ambient=0.3, ambient_col="#FFB38A")
        R.sun((72, 0, 20), 1.0, "#FFB060")
        R.sun((65, 0, 200), 0.4, "#FF5FA0", name="Rim", shadow=False)
        R.set_bands(shadow=(0.5, 0.36, 0.6), mid=(0.85, 0.66, 0.78), lit=(1.08, 0.95, 0.88), rim=0.3)


WATER = {"night": ("#0D2756", "#1B4A8E", "#8FB6FF"), "day": ("#1F7FB8", "#3FB4E0", "#E8FBFF"),
         "dawn": ("#2E4E8F", "#E3899B", "#FFE0C8"), "storm": ("#1E3A48", "#35596A", "#DDEBF2"),
         "party": ("#2A0E4A", "#5B1E7A", "#F72585"), "sunset": ("#3A4F9A", "#F08A5D", "#FFE3B0"),
         "jungle": ("#1E6B5C", "#2EA08A", "#D8FFF2")}


def sea(kind, waves=0.0, size=400, z=0.0, scale=0.07):
    d, l, f = WATER[kind]
    return R.water(size, deep=d, light=l, foam=f, waves=waves, z=z, scale=scale)


def face(src, dst):
    d = Vector(dst) - Vector(src)
    return math.degrees(math.atan2(d.x, -d.y))


def actor(name, loc, rot=0.0, clip=None, frac=0.0, pose=None, look=None, scale=1.0, parent=None):
    a = R.Actor(name)
    if look is not None:
        rot = face(loc, look)
    if parent is not None:
        a.arm.parent = parent
    a.place(loc, rot, scale)
    if clip:
        a.pose(clip, frac)
    if pose is not None:
        a.custom(pose)
    return a


def sneak():
    p = Pose().move("Hips", (0, 0, -0.1))
    p.rot("Spine", ("X", 22))
    p.rot("Chest", ("X", 8))
    p.sym("UpperArm", ("Y", 25), ("Z", -55))
    p.sym("LowerArm", ("Z", -70))
    p.side("Left", "UpperLeg", ("X", -40))
    p.side("Left", "LowerLeg", ("X", 50))
    p.side("Right", "UpperLeg", ("X", 18))
    p.side("Right", "LowerLeg", ("X", 25))
    p.rot("Head", ("X", -12))
    return p


def point(raise_deg=25, side="Right", other_down=70):
    """Señala al frente con un brazo; el otro abajo."""
    p = Pose()
    z = 90 if side == "Right" else -90
    p.rot(f"{side}UpperArm", ("Z", z), ("X", -raise_deg))
    other = "Left" if side == "Right" else "Right"
    p.rot(f"{other}UpperArm", ("Y", other_down if other == "Left" else -other_down))
    p.rot("Spine", ("X", -4))
    p.rot("Head", ("X", -6))
    return p


def shush():
    """Dedo en la boca (silencio)."""
    p = Pose()
    p.rot("RightUpperArm", ("Y", -60), ("Z", 70))
    p.rot("RightLowerArm", ("Z", 130))
    p.rot("LeftUpperArm", ("Y", 70))
    p.rot("Spine", ("X", 10))
    p.rot("Head", ("X", 6))
    return p


def raft_crew(root, spec):
    """spec: [(nombre, (x, y, z) local, rot, clip, frac)]; los actores quedan hijos de la balsa."""
    out = []
    for n, loc, rot, clip, frac in spec:
        a = R.Actor(n)
        a.arm.parent = root
        a.arm.location = Vector(loc)
        a.arm.rotation_euler = (0, 0, math.radians(rot))
        if isinstance(clip, Pose):
            a.custom(clip)
        else:
            a.pose(clip, frac)
        out.append(a)
    return out


# asientos en el borde de la balsa mirando hacia -Y (frente a la cámara)
SIT_FRONT = [(-0.9, -1.3, 0.5), (0.9, -1.3, 0.5), (-1.45, 0.2, 0.5), (1.45, 0.2, 0.5), (0.0, 1.25, 0.5)]


def seated(root, names, clip="Idle_Sit", frac=0.2, rot=0.0, extra=None):
    spec = []
    for i, n in enumerate(names):
        x, y, z = SIT_FRONT[i]
        c, f = (extra or {}).get(n, (clip, frac + 0.13 * i))
        spec.append((n, (x, y, z), rot, c, f))
    return raft_crew(root, spec)


def beans_barrel(loc, rot=0.0):
    b = R.env("W1", "Barrel_A", loc, rot, scale=1.2)
    bm = R.toon_mat("#8A3B1F", "T_Bean")
    rnd = random.Random(3)
    top = Vector(loc) + Vector((0, 0, 1.45))
    for k in range(16):
        a = rnd.uniform(0, 6.28)
        r = rnd.uniform(0, 0.4)
        gs.ellipsoid(f"Bean{k}", top + Vector((math.cos(a) * r, math.sin(a) * r, rnd.uniform(-0.05, 0.12))),
                     (0.09, 0.055, 0.05), bm, seg=8, rings=5, rot=("Z", rnd.uniform(0, 180)))
    return b


def dock(loc, size=(7.0, 9.0), h=1.0, rot=0.0):
    """Muelle de tablones con postes (superficie a z = h)."""
    wood, wood2, post = R.toon_mat("#9C6B3E", "T_Plank"), R.toon_mat("#B88250", "T_Plank2"), R.toon_mat("#6B4A2A")
    c = Vector(loc)
    sx, sy = size
    n = int(sy / 0.55)
    root = bpy.data.objects.new("Dock", None)
    bpy.context.scene.collection.objects.link(root)
    parts = []
    for k in range(n):
        y = -sy / 2 + (k + 0.5) * sy / n
        parts.append(gs.rounded_box(f"Plank{k}", (0, y, h - 0.08), (sx, sy / n - 0.06, 0.16), 0.04,
                                    wood if k % 2 else wood2, segments=1))
    for x in (-sx / 2 + 0.2, sx / 2 - 0.2):
        for y in (-sy / 2 + 0.2, 0, sy / 2 - 0.2):
            parts.append(gs.capsule("Post", (x, y, -1.5), (x, y, h + 0.25), 0.2, post, seg=10, sub=1))
    for p in parts:
        p.parent = root
    root.location = c
    root.rotation_euler = (0, 0, math.radians(rot))
    return root


def lamp_glow(loc, color="#FFB347", power=350, radius=0.3):
    return R.point_light(loc, power, color, radius, shadow=False)


def beam(src, dst, radius=2.0, color="#FFF2B0", alpha=0.18, name="Beam"):
    """Haz de luz translúcido (reflector)."""
    m = bpy.data.materials.new(name + "_Mat")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Color"].default_value = (*R.lin(color), 1)
    e.inputs["Strength"].default_value = 1.2
    t = nt.nodes.new("ShaderNodeBsdfTransparent")
    mx = nt.nodes.new("ShaderNodeMixShader")
    mx.inputs["Fac"].default_value = alpha
    nt.links.new(t.outputs[0], mx.inputs[1])
    nt.links.new(e.outputs[0], mx.inputs[2])
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(mx.outputs[0], o.inputs["Surface"])
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "BLENDED"
    m["gs_toon"] = True
    c = gs.cone(name, src, dst, 0.15, radius, m, seg=24, round_tip=False)
    c["gs_no_outline"] = True
    return c


def skyline(x0, x1, y, seed=0, h=(8, 22), col="#1B2547", win="#FFD27A", depth=5.0):
    rnd = random.Random(seed)
    wm = R.toon_mat(win, "T_Window", emission=1.4)
    x = x0
    while x < x1:
        w = rnd.uniform(4, 8)
        hh = rnd.uniform(*h)
        c = R.toon_mat(col if rnd.random() < 0.6 else "#26305C")
        gs.rounded_box("Bld", (x + w / 2, y, hh / 2 - 1), (w, depth, hh), 0.3, c, segments=1)
        for k in range(int(hh / 2.2)):
            for j in range(int(w / 1.6)):
                if rnd.random() < 0.45:
                    o = gs.rounded_box("Win", (x + 0.9 + j * 1.6, y - depth / 2 - 0.02, 1.0 + k * 2.2),
                                       (0.7, 0.08, 0.9), 0.05, wm, segments=1)
                    o["gs_no_outline"] = True
        x += w + rnd.uniform(0.3, 1.5)


def sand(size=200, z=0.0, color="#F2D492", loc=(0, 0, 0), slope=None):
    def fn(x, y):
        h = 0.15 * math.sin(x * 0.3) * math.sin(y * 0.25)
        if slope:
            h += slope(x, y)
        return z + h
    return R.ground(fn, size, 80, R.toon_mat(color, "T_Sand"), loc=loc)


def plaza(size=120, color="#7E8AA3", z=0.0):
    o = gs.rounded_box("Plaza", (0, 0, z - 0.5), (size, size, 1.0), 0.1, R.toon_mat(color, "T_Plaza"), segments=1)
    o["gs_no_outline"] = True
    return o


def confetti(center, spread=(8, 3, 5), n=60, seed=1):
    rnd = random.Random(seed)
    cols = ["#F72585", "#B5E48C", "#3A86FF", "#FFD23F", "#FF7A1A"]
    for k in range(n):
        p = Vector(center) + Vector((rnd.uniform(-1, 1) * spread[0], rnd.uniform(-1, 1) * spread[1],
                                     rnd.uniform(0, 1) * spread[2]))
        o = gs.ellipsoid(f"Conf{k}", p, (0.12, 0.03, 0.07), R.toon_mat(cols[k % 5], f"T_Conf{k % 5}", emission=0.3),
                         seg=6, rings=3, rot=[("X", rnd.uniform(0, 180)), ("Z", rnd.uniform(0, 180))])
        o["gs_no_outline"] = True


def lightning(top, bottom, seed=0):
    rnd = random.Random(seed)
    t, b = Vector(top), Vector(bottom)
    pts = [t]
    for i in range(1, 7):
        p = t.lerp(b, i / 7) + Vector((rnd.uniform(-1.5, 1.5), rnd.uniform(-0.5, 0.5), 0))
        pts.append(p)
    pts.append(b)
    m = R.toon_mat("#FFFBD0", "T_Bolt", emission=3.0)
    for i in range(len(pts) - 1):
        o = gs.capsule(f"Bolt{i}", pts[i], pts[i + 1], 0.22 - 0.02 * i, m, seg=6, sub=1)
        o["gs_no_outline"] = True


def rain(center, size=(40, 30, 25), n=220, seed=0, slant=(0.15, 0.0)):
    rnd = random.Random(seed)
    m = R.toon_mat("#C8DAF0", "T_Rain", emission=0.6)
    for k in range(n):
        p = Vector(center) + Vector((rnd.uniform(-1, 1) * size[0], rnd.uniform(-1, 1) * size[1],
                                     rnd.uniform(0, 1) * size[2]))
        o = gs.capsule(f"Rain{k}", p, p + Vector((slant[0], slant[1], -1.4)), 0.025, m, seg=4, cap=1, sub=1)
        o["gs_no_outline"] = True


def wave_crest(loc, length=14, height=4, rot=0.0, col="#2E6A80", foam="#EAF6FF"):
    m, f = R.toon_mat(col, "T_Wave"), R.toon_mat(foam, "T_WaveFoam", emission=0.2)
    pts = [Vector((x, 0, 0)) for x in (-length / 2, -length / 4, 0, length / 4, length / 2)]
    hs = [0.4, 0.8, 1.0, 0.8, 0.4]
    out = []
    for i, (p, k) in enumerate(zip(pts, hs)):
        out.append(R.blob(f"WaveBody{i}", p + Vector((0, 0, height * k * 0.35)),
                          (length / 6, height * 0.5, height * k * 0.6), m, seed=i, rough=0.1, seg=16, rings=10))
        out.append(gs.ellipsoid(f"WaveFoam{i}", p + Vector((0, -height * 0.25, height * k * 0.85)),
                                (length / 7, height * 0.3, height * k * 0.18), f, seg=12, rings=6))
    root = bpy.data.objects.new("Wave", None)
    bpy.context.scene.collection.objects.link(root)
    for o in out:
        o.parent = root
    root.location = Vector(loc)
    root.rotation_euler = (0, 0, math.radians(rot))
    return root


def lava_sea(size=200, z=0.0):
    return R.water(size, deep="#C1121F", light="#FF6A00", foam="#FFD23F", z=z, scale=0.1, emission=1.0,
                   foam_amt=0.12, name="Lava")


def throne(loc, rot=0.0, s=1.0):
    g, red = R.toon_mat("#F5C431", "T_ThroneGold", emission=0.2), R.toon_mat("#C1121F", "T_ThroneRed")
    c = Vector(loc)
    parts = [gs.rounded_box("ThroneSeat", c + Vector((0, 0, 0.5 * s)), (1.6 * s, 1.3 * s, 1.0 * s), 0.12 * s, g),
             gs.rounded_box("ThroneCushion", c + Vector((0, -0.05 * s, 1.05 * s)), (1.3 * s, 1.1 * s, 0.22 * s),
                            0.1 * s, red),
             gs.rounded_box("ThroneBack", c + Vector((0, 0.55 * s, 1.9 * s)), (1.6 * s, 0.25 * s, 2.0 * s), 0.12 * s,
                            g),
             gs.rounded_box("ThroneBackRed", c + Vector((0, 0.43 * s, 1.8 * s)), (1.2 * s, 0.05 * s, 1.5 * s),
                            0.05 * s, red)]
    for k in range(3):
        parts.append(gs.ellipsoid(f"ThroneBall{k}", c + Vector(((k - 1) * 0.6 * s, 0.55 * s, 3.0 * s)), 0.16 * s, g,
                                  seg=10, rings=6))
    root = bpy.data.objects.new("Throne", None)
    bpy.context.scene.collection.objects.link(root)
    for p in parts:
        p.parent = root
        p.location = p.location - c
    root.location = c
    root.rotation_euler = (0, 0, math.radians(rot))
    return root


def party_lights(center, radius=8, n=10, z=4.0, seed=0):
    cols = ["#F72585", "#B5E48C", "#3AE1FF", "#FFD23F"]
    for k in range(n):
        a = k / n * 2 * math.pi
        p = Vector(center) + Vector((math.cos(a) * radius, math.sin(a) * radius, z + 0.6 * math.sin(k * 1.3)))
        c = cols[k % 4]
        o = gs.ellipsoid(f"Bulb{k}", p, 0.22, R.toon_mat(c, f"T_Bulb{k % 4}", emission=1.6), seg=10, rings=6)
        o["gs_no_outline"] = True
        if k % 3 == 0:
            R.point_light(p, 300, c, 0.2, shadow=False)


def pool(loc, size=(16, 10), depth=1.2, rot=0.0):
    tile, rim = R.toon_mat("#BFE9F5", "T_Tile"), R.toon_mat("#F7F4EC", "T_PoolRim")
    c = Vector(loc)
    sx, sy = size
    for k, (dx, dy, wx, wy) in enumerate(((0, sy / 2, sx + 1.2, 0.6), (0, -sy / 2, sx + 1.2, 0.6),
                                         (sx / 2, 0, 0.6, sy), (-sx / 2, 0, 0.6, sy))):
        gs.rounded_box(f"PoolRim{k}", c + Vector((dx, dy, 0.15)), (wx, wy, depth + 0.3), 0.1, rim, segments=1)
    floor = gs.rounded_box("PoolFloor", c + Vector((0, 0, -depth / 2)), (sx, sy, 0.2), 0.05, tile, segments=1)
    floor["gs_no_outline"] = True
    return R.water(max(sx, sy), deep="#29B6E8", light="#7FDBFF", foam="#FFFFFF", z=0.0, scale=0.18,
                   loc=(c.x, c.y, c.z + 0.1), res=40, name="PoolWater")


# ---------------------------------------------------------------------------
# Viñetas
# ---------------------------------------------------------------------------
def intro_1():  # noche en el zoológico: se escapan
    mood("night")
    plaza(160, "#56618A")
    R.env("W1", "ZooGate", (0, 14, 0), 0, scale=1.0)
    for x in (-12, 12):
        R.env("W1", "Fence", (x, 14.2, 0), 0, scale=1.6)
        R.env("W1", "Fence", (x * 1.4, 14.2, 0), 0, scale=1.6)
    for x, y in ((-7, 4), (7, 2)):
        R.env("W1", "Lamp_A", (x, y, 0), 0)
        lamp_glow((x, y, 4.2), power=500)
    R.env("W1", "Topiary_A", (-9, 9, 0), 0)
    R.env("W1", "Topiary_B", (9, 9, 0), 30)
    R.moon((-18, 60, 30), 5)
    beam((26, 34, 20), (3, 6, 0), 2.6, "#FFF2B0", 0.08)
    names = ["Gloria", "Alex", "Melman", "Marty", "Julien"]
    xs = [-3.6, -1.5, 0.6, 2.6, 4.3]
    for i, (n, x) in enumerate(zip(names, xs)):
        actor(n, (x, 4.5 - i * 0.9, 0), -70, pose=sneak())
    R.camera((-2.8, -6.0, 0.8), (0.6, 3.0, 1.7), 28, roll=-4)


def intro_2():  # llegan al muelle: solo una balsa y un barril de frijoles
    mood("night")
    sea("night")
    dock((0, 2.5, 0), (8, 10), 1.0)
    for x, y in ((-3.6, 3), (3.6, 6)):
        R.env("W1", "Lamp_B", (x, y, 1.0), 180 if x > 0 else 0)
        lamp_glow((x + (0.8 if x < 0 else -0.8), y, 4.4), power=450)
    r = R.raft((0, -6.2, 0.0), 15)
    beans_barrel((2.2, -1.2, 1.05))
    R.powerup("P01_Beans", (1.2, -2.1, 1.35), 30, 1.4)
    actor("Alex", (-1.2, 0.5, 1.05), look=(0, -6, 0.5), clip="Disappointed", frac=0.5)
    actor("Marty", (0.5, 1.3, 1.05), look=(0, -6, 0.5), clip="Disappointed", frac=0.7)
    actor("Melman", (-2.6, 2.2, 1.05), look=(0, -6, 0.5), clip="Hit", frac=0.2)
    actor("Gloria", (2.3, 1.6, 1.05), look=(2.2, -1.2, 1), clip="Select_Pose", frac=0.1)
    actor("Julien", (3.4, -0.6, 1.05), look=(0, -6, 0), clip="Idle_Sit", frac=0.1, scale=1.0).arm.location.z = 1.45
    skyline(-50, 50, 45, seed=3)
    R.moon((22, 70, 34), 5)
    R.camera((5.5, -10.5, 4.0), (0, -1.2, 1.6), 30)


def intro_3():  # descubren el "método": Gloria come frijoles y... ¡pedo!
    mood("night")
    sea("night")
    dock((0, 2.0, 0), (9, 10), 1.0)
    beans_barrel((-2.2, 1.6, 1.05))
    g = actor("Gloria", (0.3, 0.2, 1.05), look=(-3, -8, 1), clip="Celebrate", frac=0.45)
    R.powerup("P01_Beans", (-0.9, -0.7, 2.7), -20, 1.5)
    R.gas_cloud((1.9, 1.6, 1.6), 1.4, n=9, seed=4)
    R.gas_cloud((3.1, 2.4, 2.3), 0.9, n=5, seed=6)
    actor("Alex", (-2.6, -1.1, 1.05), look=(1.5, 1.5, 1.5), clip="Hit", frac=0.25)
    actor("Marty", (2.9, -0.9, 1.05), look=(1.5, 1.5, 1.5), clip="Hit", frac=0.2)
    cam = R.camera((-1.2, -6.6, 2.0), (0.4, 0.4, 2.0), 30, roll=6)
    R.backdrop(cam, "#FFE45C", "#FF7A1A", depth=40, rays=26, ray_col="#FFD23F")
    R.speed_lines(cam, n=30, seed=2, inner=0.55, length=0.45)


def intro_4():  # el método funciona: la balsa sale disparada con gas
    mood("night")
    sea("night", waves=0.25)
    r = R.raft((0, 0, 0.05), 25)
    r.rotation_euler = (math.radians(-6), math.radians(4), math.radians(25))
    seated(r, ["Alex", "Marty", "Gloria", "Melman", "Julien"], extra={
        "Alex": ("Hit", 0.2), "Marty": ("Celebrate", 0.45), "Melman": ("Fall", 0.4)})
    for k, (x, y, s) in enumerate(((1.6, 3.4, 1.4), (2.6, 5.2, 1.9), (3.4, 7.5, 2.4))):
        R.gas_cloud((x, y, 0.9 + 0.3 * k), s, n=8, seed=10 + k)
    R.splash((-0.8, -2.2, 0), 1.3, seed=9)
    cam = R.camera((-6.5, -8.0, 2.6), (0.6, 1.0, 1.0), 30, roll=-8)
    R.speed_lines(cam, n=34, seed=4, inner=0.5)
    skyline(-60, 60, 70, seed=5)


def intro_5():  # todos a bordo, se alejan de la ciudad bajo la luna
    mood("night")
    sea("night")
    r = R.raft((0, 0, 0.05), 200)
    raft_crew(r, [("Alex", (-0.9, 0.2, 0.17), 180, "Celebrate", 0.45), ("Marty", (0.6, -0.6, 0.17), 160, "Victory", 0.2),
                  ("Gloria", (0.9, 0.8, 0.17), 200, "Celebrate", 0.3), ("Melman", (-0.4, -1.1, 0.17), 180, "Victory",
                                                                           0.5),
                  ("Julien", (0.0, 1.3, 0.5), 180, "Victory", 0.1)])
    R.gas_cloud((0.4, 3.0, 0.9), 1.3, n=8, seed=3)
    R.env("W1", "FerrisWheel", (-25, 70, 0), 15, scale=1.2)
    R.env("W1", "Bridge", (18, 55, 0), 0, scale=1.5)
    skyline(-90, 90, 85, seed=7, h=(10, 30))
    R.moon((10, 120, 38), 9)
    R.camera((2.6, -8.5, 3.2), (0, 6, 3.4), 30)


def w1_end_1():  # escondidos tras el barco de carga, en el puerto
    mood("night")
    sea("night")
    R.env("W1", "CargoShip", (6, 6, 0), 8)
    crane = R.asset("Bosses/Crane.fbx", (-10, 26, 0), 160, scale=1.0)
    R.pose_fbx(crane, "Idle", 0.3)
    for k, (x, y) in enumerate(((-16, 18), (-19, 12), (-13, 22))):
        R.env("W1", "Container_" + "ABC"[k], (x, y, 0), 90 * (k % 2), scale=1.3)
    R.spot_light((-8, 40, 22), (-3, 0, 0), 6000, "#FFF2B0", 18)
    r = R.raft((-0.6, -2.5, 0.05), 80)
    raft_crew(r, [("Alex", (-0.4, -0.8, 0.17), 60, "Grab", 0.3), ("Marty", (0.8, 0.2, 0.17), 60, "Grab", 0.6),
                  ("Melman", (-0.6, 0.8, 0.17), 70, "Hit", 0.15), ("Gloria", (0.5, 1.2, 0.17), 80, "Grab", 0.1),
                  ("Julien", (1.0, -1.0, 0.5), 60, "Idle_Sit", 0.3)])
    R.camera((-5.5, -8.0, 1.6), (0, 0.5, 3.2), 24, roll=5)


def w1_end_2():  # en silencio a la sombra del casco
    mood("night")
    sea("night")
    R.env("W1", "CargoShip", (7.5, 4, 0), 0)
    r = R.raft((0, 0, 0.05), 0)
    raft_crew(r, [("Alex", (-0.5, -0.6, 0.17), -20, shush(), 0), ("Melman", (0.7, 0.4, 0.17), -15, "Hit", 0.18),
                  ("Marty", (-1.0, 0.7, 0.17), -10, "Grab", 0.4), ("Gloria", (0.9, -0.9, 0.5), -25, "Idle_Sit", 0.1),
                  ("Julien", (-0.2, 1.3, 0.5), 0, "Idle_Sit", 0.6)])
    R.point_light((6.0, -2.0, 10.0), 900, "#FFF2B0", 0.5)
    R.camera((-2.2, -6.8, 3.6), (0.0, 0.3, 1.4), 30, roll=-3)


def w1_end_3():  # mar abierto al amanecer
    mood("dawn")
    sea("dawn", waves=0.15)
    r = R.raft((0, 0, 0.05), 0)
    raft_crew(r, [("Alex", (0.0, -0.6, 0.17), 180, point(20), 0), ("Marty", (0.9, 0.3, 0.5), 180, "Idle_Sit", 0.3),
                  ("Gloria", (-0.9, 0.4, 0.5), 180, "Idle_Sit", 0.5), ("Melman", (-0.5, 1.2, 0.17), 180, "Select_Pose",
                                                                        0.3),
                  ("Julien", (0.8, 1.2, 0.5), 180, "Idle_Sit", 0.8)])
    R.env("W1", "CargoShip", (-30, -50, 0), 40)
    skyline(-80, -10, -70, seed=11, col="#3B3F6E", win="#FFB38A")
    sun_disc = gs.ellipsoid("SunDisc", (8, 160, 6), 12, R.toon_mat("#FFE29A", "T_SunDisc", emission=2.0), seg=24,
                            rings=12)
    sun_disc["gs_no_outline"] = True
    for k, (x, z) in enumerate(((-40, 25), (30, 30), (-10, 40))):
        R.cloud((x, 140, z), 18, seed=k, color="#FFD0B8", shade="#C99BD8")
    R.camera((-2.4, -7.5, 3.0), (1.0, 60, 2.5), 26)


def w2_end_1():  # la tormenta
    mood("storm")
    sea("storm", waves=0.9, scale=0.09)
    wave_crest((6, 14, -1), 26, 9, 15)
    wave_crest((-14, 22, -1), 22, 7, -10)
    r = R.raft((0, 0, 0.6), 20)
    r.rotation_euler = (math.radians(18), math.radians(-14), math.radians(20))
    raft_crew(r, [("Alex", (-0.6, -0.4, 0.17), 0, "Fall", 0.4), ("Marty", (0.9, 0.5, 0.17), 30, "Grab", 0.2),
                  ("Gloria", (-0.6, 0.9, 0.17), -20, "Grab", 0.7), ("Melman", (0.5, -0.9, 0.17), 10, "Fall", 0.7),
                  ("Julien", (0.0, 0.2, 1.6), 0, "Fall", 0.2)])
    lightning((12, 40, 30), (9, 30, 2), seed=3)
    rain((0, 8, 0), (28, 20, 18), 260, seed=2, slant=(0.4, 0.1))
    R.splash((1.5, -1.2, 0.2), 1.6, seed=4)
    cam = R.camera((-7, -9, 1.5), (0.5, 1, 2.0), 26, roll=-12)


def w2_end_2():  # la tormenta los deja en la playa de una isla tropical
    mood("day")
    sea("jungle")
    sand(200, 0.25, loc=(0, 0, 0), slope=lambda x, y: min(0.0, -(y + 6) * 0.08) if y < -6 else 0.0)
    r = R.raft((-1.2, 1.0, 0.25), 30)
    r.rotation_euler = (math.radians(4), math.radians(-12), math.radians(30))
    actor("Alex", (2.4, -1.2, 0.3), 70, clip="Fall", frac=0.9)
    actor("Marty", (-3.4, -1.8, 0.3), -40, clip="Disappointed", frac=0.9)
    actor("Melman", (0.8, 3.5, 0.3), 160, clip="Disappointed", frac=0.6)
    actor("Gloria", (-1.3, 1.1, 0.75), 20, clip="Idle_Sit", frac=0.4)
    actor("Julien", (3.6, 1.8, 0.3), -120, clip="Select_Pose", frac=0.25)
    for k, (x, y, rot) in enumerate(((-8, 9, 10), (7, 11, -20), (12, 6, 40), (-13, 14, 0))):
        R.env("W3", "Palm_" + "ABC"[k % 3], (x, y, 0.2), rot * 4)
    R.env("W2", "Crate_A", (5.5, -2.5, 0.25), 25)
    R.env("W2", "Barrel_B", (-5.8, -3.6, 0.25), 0, tilt=(80, 0))
    R.env("W2", "Starfish", (1.2, -3.2, 0.3), 30)
    R.env("W3", "Baobab_A", (-10, 40, 0), 0, scale=0.8)
    R.camera((0.8, -9.0, 3.4), (0, 1.0, 0.9), 30, roll=-3)


def w2_end_3():  # miran la selva desde la playa
    mood("day")
    sand(200, 0.2)
    sea("jungle", z=0.0)
    names = ["Gloria", "Alex", "Julien", "Marty", "Melman"]
    for i, n in enumerate(names):
        x = -3.2 + i * 1.6
        actor(n, (x, -2 + 0.4 * abs(i - 2), 0.25), 180 + (i - 2) * 8,
              clip="Select_Pose" if n == "Julien" else "Disappointed", frac=0.15 if n == "Julien" else 0.05)
    R.env("W3", "Baobab_B", (2, 26, 0), 0, scale=1.2)
    R.env("W3", "Baobab_A", (-18, 30, 0), 50)
    for k in range(9):
        R.env("W3", "Palm_" + "ABC"[k % 3], (-24 + k * 6, 12 + (k % 2) * 3, 0.2), k * 40, scale=1.3)
    for k in range(6):
        R.env("W3", "Fern", (-14 + k * 5.5, 9, 0.2), k * 60, scale=1.6)
    R.camera((0.8, -9.5, 0.9), (0, 12, 7.5), 24)


def w3_end_1():  # ganan la fiesta de los lémures
    mood("party")
    sand(200, 0.0, color="#D9B577")
    stage = R.env("W3", "DJStage", (0, 6, 0), 180, scale=1.2)
    for x in (-6, 6, -9, 9):
        R.env("W3", "TikiTorch", (x, 2 + abs(x) * 0.3, 0), 0)
        R.point_light((x, 2 + abs(x) * 0.3, 3.4), 220, "#FF8C1A", 0.2)
    party_lights((0, 4, 0), 10, 12, 6)
    actor("Julien", (0, -1.6, 0), 0, clip="Victory", frac=0.3, scale=1.15)
    actor("Alex", (-3.2, 0.5, 0), 20, clip="Celebrate", frac=0.45)
    actor("Gloria", (3.0, 0.2, 0), -20, clip="Victory", frac=0.6)
    actor("Marty", (-1.6, -0.6, 0), 10, clip="Celebrate", frac=0.6)
    actor("Melman", (1.7, -0.8, 0), -10, clip="Victory", frac=0.15)
    confetti((0, 2, 2), (8, 4, 6), 80)
    R.env("W3", "Palm_A", (-12, 10, 0), 0)
    R.env("W3", "Palm_B", (12, 12, 0), 90)
    R.camera((0, -9.5, 1.6), (0, 2, 2.6), 28)


def w3_end_2():  # el Rey Julien se une "oficialmente"
    mood("party")
    sand(200, 0.0, color="#D9B577")
    j = actor("Julien", (0, 0, 0.9), 0, clip="Select_Pose", frac=0.2)
    gs.rounded_cylinder("Pedestal", (0, 0, 0.45), 1.1, 0.9, 0.1, R.toon_mat("#7B2CBF", "T_Ped"), seg=32)
    R.spot_light((0, -4, 12), (0, 0, 1), 3000, "#FFF2B0", 30)
    for i, (n, x, y) in enumerate((("Alex", -2.6, -1.4), ("Gloria", 2.7, -1.2), ("Marty", -1.4, -2.6),
                                   ("Melman", 1.6, -2.8))):
        actor(n, (x, y, 0), 180 + (12 if x < 0 else -12), clip="Victory", frac=0.2 + 0.15 * i)
    cam = R.camera((0.2, -5.0, 1.0), (0, 0, 1.55), 30)
    R.backdrop(cam, "#B5E48C", "#F72585", depth=30, rays=24, ray_col="#FF6FC8")
    confetti((0, 0, 2.5), (4, 2, 3), 50, seed=3)


def w3_end_3():  # Julien señala el volcán como atajo a casa
    mood("day")
    sand(300, 0.0, color="#8DBF6A")
    R.env("W3", "Rock_C", (0.4, 0, 0), 20, scale=0.55)
    actor("Julien", (0.5, 0.0, 0.9), look=(20, 70, 0), pose=point(25), scale=1.15)
    actor("Alex", (-2.4, -1.8, 0), look=(20, 70, 0), clip="Select_Pose", frac=0.1)
    actor("Melman", (-3.6, -0.4, 0), look=(20, 70, 0), clip="Hit", frac=0.15)
    actor("Gloria", (2.6, -2.2, 0), look=(20, 70, 0), clip="Disappointed", frac=0.05)
    actor("Marty", (3.4, -0.4, 0), look=(20, 70, 0), clip="Select_Pose", frac=0.2)
    v = R.env("W4", "VolcanoCrater", (30, 110, -2), 0, scale=2.2)
    R.gas_cloud((30, 110, 56), 7, color="#6B6B78", n=7, seed=3, shine=False)
    for k in range(7):
        R.env("W3", "Palm_" + "ABC"[k % 3], (-26 + k * 9, 30 + (k % 3) * 6, 0), k * 50, scale=1.4)
    R.cloud((-30, 120, 40), 22, seed=2)
    R.camera((-2.6, -5.2, 1.3), (3, 12, 3.6), 28, roll=3)


def w3_end_4():  # rumbo al volcán por el río de la selva
    mood("day")
    sea("jungle")
    for side in (-1, 1):
        R.ground(lambda x, y, s=side: 0.4 + 0.3 * math.sin(y * 0.2), 300, 20, R.toon_mat("#3E9B45", "T_Bank"),
                 loc=(side * 162, 0, 0))
    r = R.raft((0, 0, 0.05), 180)
    seated(r, ["Alex", "Marty", "Gloria", "Melman", "Julien"], rot=180)
    for k in range(10):
        side = -1 if k % 2 else 1
        R.env("W3", "Palm_" + "ABC"[k % 3], (side * (13 + (k % 3) * 2), 4 + k * 8, 0.4), k * 37, scale=1.3)
    for k in range(4):
        R.env("W3", "Lilypad_" + "AB"[k % 2], ((k - 2) * 3.5, 8 + k * 5, 0), k * 50)
    R.env("W4", "VolcanoCrater", (8, 140, -2), 0, scale=2.4)
    R.gas_cloud((8, 140, 58), 8, color="#6B6B78", n=7, seed=3, shine=False)
    R.gas_cloud((0.2, -3.0, 0.8), 1.1, n=6, seed=7)
    R.camera((0, -10, 3.6), (0, 30, 4), 28)


def w4_end_1():  # dentro del volcán: géiser bajo la balsa
    mood("cave")
    lava_sea(200, z=-3)
    for k, (x, y, s) in enumerate(((-9, 6, 2.5), (10, 8, 3.0), (-14, 18, 3.5), (14, 20, 3.2), (0, 26, 4))):
        R.env("W4", "CaveRock_" + "ABC"[k % 3], (x, y, -3), k * 70, scale=s)
    for k, (x, y) in enumerate(((-6, 3), (7, 4), (-3, 14), (5, 15))):
        R.env("W4", "Crystal_" + "ABC"[k % 3], (x, y, -2.6), k * 40, scale=1.6)
    R.env("W4", "GeyserVent", (0, 3, -3), 0, scale=1.4)
    col = R.toon_mat("#E6FBFF", "T_Geyser", emission=0.5)
    gs.capsule("GeyserCol", (0, 3, -3), (0, 3, 3.0), 1.0, col, seg=16, sub=4)
    R.gas_cloud((0, 3, 2.8), 1.5, color="#F2FBFF", n=8, seed=5, shine=False)
    r = R.raft((0, 3, 3.6), 30)
    r.rotation_euler = (math.radians(-10), math.radians(8), math.radians(30))
    raft_crew(r, [("Alex", (-0.6, -0.4, 0.17), 0, "Fall", 0.3), ("Marty", (0.8, 0.4, 0.17), 30, "Fall", 0.6),
                  ("Gloria", (-0.4, 0.9, 0.17), -20, "Grab", 0.5), ("Melman", (0.6, -0.9, 0.17), 10, "Hit", 0.2),
                  ("Julien", (0.0, 0.0, 1.8), 0, "Fall", 0.5)])
    for k, (x, y) in enumerate(((-5, 0), (5, -1), (-2, 8))):
        R.point_light((x, y, -1.5), 900, "#FF6A00", 1.0, shadow=False)
    cam = R.camera((-4.0, -6.5, 5.5), (0, 3, 3.9), 30, roll=8)


def w4_end_2():  # salen disparados del cráter
    mood("sunset")
    R.ground(lambda x, y: 0.0, 600, 10, R.toon_mat("#5E3B4E", "T_Ash"))
    R.env("W4", "VolcanoCrater", (0, 0, 0), 0, scale=1.0)
    lm = R.toon_mat("#FF4D00", "T_LavaBlob", emission=1.0)
    rnd = random.Random(5)
    for k in range(16):
        p = Vector((rnd.uniform(-6, 6), rnd.uniform(-3, 3), 24 + rnd.uniform(0, 14)))
        gs.ellipsoid(f"LavaBlob{k}", p, rnd.uniform(0.5, 1.2), lm, seg=10, rings=6)
    R.gas_cloud((1, 3, 30), 5, color="#5E5A66", n=8, seed=2, shine=False)
    r = R.raft((-3.0, -4, 37), 20, 1.6)
    r.rotation_euler = (math.radians(25), math.radians(-15), 0)
    raft_crew(r, [("Alex", (-0.6, -0.5, 0.17), 0, "Celebrate", 0.45), ("Marty", (0.8, 0.4, 0.17), 30, "Fall", 0.5),
                  ("Gloria", (-0.4, 0.9, 0.17), -20, "Celebrate", 0.5), ("Melman", (0.7, -0.8, 0.17), 10, "Fall", 0.2),
                  ("Julien", (0.1, 0.1, 1.5), 0, "Celebrate", 0.45)])
    for k, (x, z) in enumerate(((-60, 40), (50, 55))):
        R.cloud((x, 120, z), 22, seed=k, color="#FFD0B8", shade="#C99BD8")
    cam = R.camera((-30, -62, 12), (-1, 0, 20), 28, roll=-8)
    R.speed_lines(cam, n=30, seed=8, inner=0.6)


def w4_end_3():  # vuelan sobre el cañón al atardecer
    mood("sunset")
    R.ground(lambda x, y: 0.0, 400, 10, R.toon_mat("#C46A2E", "T_CanyonFloor"))
    for k in range(6):
        R.env("W5", "CanyonRock_" + "ABC"[k % 3], ((-1) ** k * (12 + 3 * k), 30 + 12 * k, 0), k * 45, scale=2.2)
    for k, (x, y, s) in enumerate(((-30, 40, 3.0), (32, 50, 3.4), (-20, 90, 4.0), (26, 100, 3.8), (-45, 70, 3.5),
                                   (45, 80, 4.0))):
        R.env("W5", "CanyonRock_" + "ABC"[k % 3], (x, y, 0), k * 60, scale=s)
    R.env("W5", "NaturalArch", (0, 130, 0), 0, scale=1.6)
    r = R.raft((0, 20, 26), 10)
    r.rotation_euler = (math.radians(-12), math.radians(8), math.radians(10))
    seated(r, ["Alex", "Marty", "Gloria", "Melman", "Julien"], rot=180, extra={
        "Alex": ("Celebrate", 0.45), "Julien": ("Victory", 0.2)})
    R.gas_cloud((0.5, 24, 25.6), 1.6, n=7, seed=4)
    for k, (x, z) in enumerate(((-40, 40), (35, 50), (0, 60))):
        R.cloud((x, 160, z), 22, seed=k, color="#FFD0B8", shade="#C99BD8")
    R.camera((4, 6, 28.5), (0, 60, 18), 28, roll=4)


def w5_end_1():  # el megatobogán
    mood("day")
    sea("day")
    R.env("W5", "SlideTower", (6, 30, 0), 180, scale=1.0)
    for k in range(4):
        R.env("W5", "Slide_Straight", (0, 6 + k * 10.4, 8 - k * 3.2), 0, tilt=(-17, 0))
    r = R.raft((0, 1, 9.6), 180)
    r.rotation_euler = (math.radians(17), 0, math.radians(180))
    seated(r, ["Alex", "Marty", "Gloria", "Melman", "Julien"], rot=180, extra={
        "Alex": ("Celebrate", 0.45), "Marty": ("Celebrate", 0.5), "Melman": ("Fall", 0.4)})
    R.splash((0, -2.5, 9.0), 1.2, seed=5)
    for k in range(3):
        R.env("W5", "CanyonRock_" + "ABC"[k], (-16 + k * 16, 60, 0), k * 80, scale=3)
    cam = R.camera((-2.6, -6.5, 11.5), (0, 2, 10.0), 28, roll=-10)
    R.speed_lines(cam, n=30, seed=3, inner=0.55)


def w5_end_2():  # caen frente al barco de regreso
    mood("day")
    sea("day")
    R.env("W1", "CargoShip", (0, 30, 0), 90)
    r = R.raft((0, 0, 0.05), 180)
    seated(r, ["Alex", "Marty", "Gloria", "Melman", "Julien"], rot=180, extra={
        "Alex": ("Victory", 0.2), "Melman": ("Victory", 0.5), "Julien": ("Celebrate", 0.45)})
    R.splash((0, 2.2, 0), 1.6, seed=2, n=10)
    R.cloud((-30, 80, 25), 18, seed=1)
    R.cloud((40, 90, 30), 16, seed=2)
    R.camera((5, -11, 3.0), (0, 10, 5), 26)


def w5_end_3():  # a bordo, rumbo a casa
    mood("sunset")
    sea("sunset")
    R.env("W1", "CargoShip", (0, 0, 0), 30)
    deck_z = 5.0
    for i, (n, x) in enumerate((("Alex", -1.6), ("Gloria", 0.0), ("Marty", 1.6), ("Melman", -3.0))):
        actor(n, (x * 0.9 - 2.2, -6.5 + x * 0.5, deck_z), 120, clip="Victory", frac=0.2 * i)
    actor("Julien", (-1.0, -8.0, deck_z), 120, clip="Celebrate", frac=0.45)
    skyline(-150, -60, 120, seed=7, col="#4A3B6E", win="#FFD27A")
    R.env("W1", "FerrisWheel", (-110, 110, 0), 20, scale=1.4)
    R.cloud((-60, 150, 40), 25, seed=4, color="#FFD0B8", shade="#C99BD8")
    R.camera((3.5, -15, 7.0), (-2.2, -6.2, 6.2), 30)


def w5_end_4():  # de vuelta en el zoológico, ahora con piscina de olas
    mood("day")
    plaza(200, "#B9C2CC")
    pool((0, 0, 0), (22, 14), 1.4)
    R.splash((1.5, 3.4, 0.2), 1.2, seed=6)
    r = R.raft((1.5, 1.5, 1.6), 200)
    r.rotation_euler = (math.radians(-12), math.radians(6), math.radians(200))
    raft_crew(r, [("Alex", (-0.4, -0.4, 0.17), 180, "Celebrate", 0.45), ("Marty", (0.8, 0.4, 0.17), 160, "Victory", 0.3),
                  ("Gloria", (-0.6, 0.9, 0.17), 200, "Victory", 0.5), ("Melman", (0.5, -1.0, 0.17), 180, "Celebrate",
                                                                         0.5),
                  ("Julien", (0.0, 0.3, 1.6), 180, "Victory", 0.2)])
    R.env("W1", "ZooGate", (0, 22, 0), 0)
    for x in (-14, 14):
        R.env("W1", "Topiary_A", (x, 12, 0), 0, scale=1.3)
        R.env("W1", "Lamp_A", (x * 0.7, 9, 0), 0)
    R.env("W1", "Flagpole", (-9, 16, 0), 0)
    R.camera((-4.5, -9.5, 4.0), (1.2, 2, 2.0), 30)


def w5_end_5():  # final feliz: foto de grupo
    mood("day")
    plaza(200, "#B9C2CC")
    R.env("W1", "ZooGate", (0, 8, 0), 0)
    poses = [("Melman", -2.6, "Select_Pose", 0.3), ("Alex", -1.3, "Victory", 0.25), ("Julien", 0.0, "Victory", 0.1),
             ("Gloria", 1.3, "Select_Pose", 0.15), ("Marty", 2.6, "Victory", 0.4)]
    for i, (n, x, c, f) in enumerate(poses):
        a = actor(n, (x, -abs(x) * 0.25 + (0.6 if n == "Julien" else 0), 0), 0, clip=c, frac=f)
    confetti((0, 0, 1), (6, 3, 5), 90, seed=6)
    cam = R.camera((0, -9.5, 1.6), (0, 1, 1.9), 30)
    R.backdrop(cam, "#FFE45C", "#3AB0FF", depth=60, rays=28, ray_col="#FFD23F", center=(0, 0.1))


def w6_1():  # el Rey Julien organiza "El Reino de Julien"
    mood("party")
    plaza(200, "#3B1F5C")
    gs.rounded_cylinder("Stage", (0, 3, 0.4), 6, 0.8, 0.2, R.toon_mat("#7B2CBF", "T_Stage"), seg=48)
    throne((0, 4, 0.8), 0, 1.0)
    import cosmetics
    crown = cosmetics.build_hat("H08_GoldenCrown")
    crown.location, crown.scale = (0, 9, 5.5), (9, 9, 9)
    actor("Julien", (0, 3.55, 1.85), 0, clip="Select_Pose", frac=0.2, scale=1.3)
    party_lights((0, 3, 0), 7, 12, 5)
    R.spot_light((0, -6, 14), (0, 3.5, 2), 4000, "#FFF2B0", 25)
    confetti((0, 2, 2), (7, 3, 6), 70, seed=9)
    R.gas_cloud((-3.5, 4, 1.2), 1.0, color="#F72585", n=5, seed=1)
    R.gas_cloud((3.5, 4, 1.2), 1.0, color="#B5E48C", n=5, seed=2)
    R.camera((0, -4.6, 2.2), (0, 3.6, 2.9), 30)


def w6_2():  # desafíos remix
    mood("party")
    sea("party")
    t1 = R.asset("Fauna/Tentacle.fbx", (-5, 6, 0), 30, scale=2.0)
    R.pose_fbx(t1, "Attack", 0.4)
    gc = R.asset("Fauna/GiantCroc.fbx", (5, 8, -0.4), -140, scale=1.0)
    R.pose_fbx(gc, "Bite", 0.5)
    crane = R.asset("Bosses/Crane.fbx", (12, 24, 0), 150)
    R.pose_fbx(crane, "Lower_Hook", 0.6)
    r = R.raft((0, 0, 0.05), 200)
    raft_crew(r, [("Alex", (-0.5, -0.4, 0.17), 180, "Ability", 0.45), ("Gloria", (0.7, 0.5, 0.17), 160, "Grab", 0.3),
                  ("Marty", (-0.8, 0.8, 0.17), 200, "Hit", 0.2), ("Melman", (0.6, -0.9, 0.17), 180, "Fall", 0.3),
                  ("Julien", (0.0, 0.3, 1.5), 180, "Celebrate", 0.45)])
    party_lights((0, 8, 0), 14, 14, 3, seed=2)
    R.gas_cloud((0, 3.0, 0.9), 1.2, color="#F72585", n=7, seed=8)
    cam = R.camera((2.5, -10, 3.2), (0, 5, 2.5), 26, roll=6)


def w6_3():  # coronan al mejor equipo
    mood("party")
    plaza(200, "#3B1F5C")
    gs.rounded_cylinder("Stage", (0, 2, 0.4), 6, 0.8, 0.2, R.toon_mat("#7B2CBF", "T_Stage"), seg=48)
    import cosmetics
    team = [("Gloria", -2.4, "Victory", 0.5), ("Alex", -0.8, "Victory", 0.25), ("Marty", 0.8, "Celebrate", 0.45),
            ("Melman", 2.4, "Victory", 0.1)]
    for n, x, c, f in team:
        a = actor(n, (x, 1.5, 0.8), 0, clip=c, frac=f)
        if n == "Alex":
            a.wear(cosmetics.build_hat("H08_GoldenCrown"))
    actor("Julien", (0, -0.6, 0.8), 180, pose=point(40, "Right"))
    party_lights((0, 2, 0), 7, 12, 5)
    confetti((0, 1, 2), (6, 3, 6), 100, seed=4)
    cam = R.camera((-1.5, -8.5, 2.4), (0, 1.5, 2.2), 28)
    R.backdrop(cam, "#B5E48C", "#F72585", depth=50, rays=30, ray_col="#FF6FC8")


MOMENTS = {
    "Intro": [intro_1, intro_2, intro_3, intro_4, intro_5],
    "W1_End": [w1_end_1, w1_end_2, w1_end_3],
    "W2_End": [w2_end_1, w2_end_2, w2_end_3],
    "W3_End": [w3_end_1, w3_end_2, w3_end_3, w3_end_4],
    "W4_End": [w4_end_1, w4_end_2, w4_end_3],
    "W5_End": [w5_end_1, w5_end_2, w5_end_3, w5_end_4, w5_end_5],
    "W6_Secret": [w6_1, w6_2, w6_3],
}


# ---------------------------------------------------------------------------
# Fondos de carga (uno por mundo)
# ---------------------------------------------------------------------------
def crew4(r, rot=180, names=("Alex", "Marty", "Gloria", "Melman")):
    return seated(r, list(names), rot=rot)


def load_w1():
    mood("night")
    sea("night")
    for side in (-1, 1):
        o = gs.rounded_box("Quay", (side * 9.5, 30, 0.6), (6, 120, 2.4), 0.2, R.toon_mat("#56618A", "T_Quay"))
    for k in range(6):
        for side in (-1, 1):
            R.env("W1", "Lamp_A", (side * 7.5, 4 + k * 9, 1.8), 90 * side)
            lamp_glow((side * 6.5, 4 + k * 9, 6.0), power=380)
    R.env("W1", "Bridge", (0, 30, 0), 0)
    R.env("W1", "ZooGate", (0, 70, 1.8), 0)
    skyline(-60, 60, 80, seed=2, h=(12, 34))
    R.env("W1", "FerrisWheel", (-28, 60, 1.8), 10)
    r = R.raft((0, 8.5, 0.05), 180)
    crew4(r)
    R.gas_cloud((0.3, 11.8, 0.9), 1.0, n=6, seed=3)
    R.moon((22, 140, 50), 8)
    R.camera((3.5, -8, 4.5), (0, 20, 3.5), 28)


def load_w2():
    mood("day")
    sea("day", waves=0.35)
    R.env("W2", "Lighthouse", (-22, 60, -1), 20)
    for k, (x, y) in enumerate(((-12, 40), (14, 34), (24, 60), (-30, 30))):
        R.env("W2", "Rock_" + "ABC"[k % 3], (x, y, 0), k * 50, scale=1.5)
    R.env("W2", "Shipwreck", (26, 80, 0), -40, tilt=(0, 12))
    R.env("W2", "Buoy_A", (6, 18, 0), 0)
    R.env("W2", "Buoy_C", (-7, 26, 0), 0)
    r = R.raft((0, 8.5, 0.05), 190)
    crew4(r)
    R.gas_cloud((0.3, 11.8, 0.9), 1.0, n=6, seed=3)
    for k, (x, z) in enumerate(((-50, 30), (40, 38), (0, 45))):
        R.cloud((x, 160, z), 22, seed=k)
    R.camera((-3.5, -8, 4.0), (0, 22, 3.5), 28)


def load_w3():
    mood("day")
    sea("jungle")
    for side in (-1, 1):
        R.ground(lambda x, y: 0.5 + 0.4 * math.sin(y * 0.15), 300, 20, R.toon_mat("#3E9B45", "T_Bank"),
                 loc=(side * 161, 0, 0))
    for k in range(12):
        side = -1 if k % 2 else 1
        R.env("W3", "Palm_" + "ABC"[k % 3], (side * (12 + (k % 3) * 3), 4 + k * 7, 0.5), k * 37, scale=1.4)
    R.env("W3", "Baobab_A", (-26, 70, 0), 0)
    R.env("W3", "Baobab_B", (30, 85, 0), 40)
    for k in range(6):
        R.env("W3", "Lilypad_" + "AB"[k % 2], ((k - 3) * 3.2 + 1, 12 + k * 4, 0), k * 50)
    R.env("W3", "Liana", (0, 28, 2), 0, scale=1.3)
    croc = R.asset("Fauna/Croc.fbx", (7, 22, -0.3), -150)
    R.pose_fbx(croc, "Sleep", 0.3)
    r = R.raft((0, 8.5, 0.05), 180)
    crew4(r, names=("Alex", "Julien", "Gloria", "Marty"))
    R.gas_cloud((0.3, 11.8, 0.9), 1.0, n=6, seed=3)
    R.camera((3, -8, 4.2), (0, 22, 3.0), 28)


def load_w4():
    mood("cave")
    sea("party", z=0.0)
    for side in (-1, 1):
        for k in range(6):
            R.env("W4", "CaveRock_" + "ABC"[k % 3], (side * (10 + (k % 2) * 3), 6 + k * 10, -0.5), k * 70,
                  scale=2.6)
            R.env("W4", "Crystal_" + "ABC"[k % 3], (side * (7.5 + (k % 2)), 10 + k * 10, 0), k * 40, scale=1.4)
    for k in range(8):
        R.env("W4", "Stalactite_" + "AB"[k % 2], ((k % 4 - 1.5) * 5, 10 + k * 6, 16), k * 40, scale=1.5)
    R.env("W4", "GiantCrystal", (0, 75, -1), 0)
    gs.rounded_box("CaveRoof", (0, 40, 18), (60, 120, 2), 0.5, R.toon_mat("#2A1033", "T_Roof"))
    R.env("W4", "LavaPool", (-6, 30, 0.1), 0, scale=1.5)
    R.env("W4", "GlowMushrooms", (6.5, 14, 0.1), 0, scale=1.6)
    for k, (x, y, c) in enumerate(((-6, 30, "#FF6A00"), (0, 75, "#4CC9F0"), (6, 14, "#4CC9F0"))):
        R.point_light((x, y, 3), 1500, c, 1.0, shadow=False)
    r = R.raft((0, 8.5, 0.05), 180)
    crew4(r, names=("Alex", "Melman", "Gloria", "Marty"))
    R.point_light((0, 3, 4), 400, "#FFD27A", 0.5)
    R.camera((-3, -8, 4.2), (0, 22, 4.5), 28)


def load_w5():
    mood("sunset")
    sea("sunset")
    for side in (-1, 1):
        for k in range(5):
            R.env("W5", "CanyonRock_" + "ABC"[k % 3], (side * (14 + (k % 2) * 4), 10 + k * 14, 0), k * 60,
                  scale=3.2)
    R.env("W5", "NaturalArch", (0, 60, 0), 0, scale=1.1)
    R.env("W5", "Dam", (0, 110, 0), 0, scale=1.6)
    R.env("W5", "Cactus_A", (-9, 18, 0.2), 0, scale=1.2)
    R.env("W5", "Mangrove_A", (10, 26, 0), 30)
    r = R.raft((0, 8.5, 0.05), 180)
    crew4(r, names=("Alex", "Marty", "Gloria", "Julien"))
    R.gas_cloud((0.3, 11.8, 0.9), 1.0, n=6, seed=3)
    R.camera((3, -8, 4.5), (0, 24, 5), 28)


def load_w6():
    mood("party")
    sea("party")
    gs.rounded_cylinder("Stage", (0, 34, 0.6), 9, 1.4, 0.3, R.toon_mat("#7B2CBF", "T_Stage"), seg=48)
    import cosmetics
    crown = cosmetics.build_hat("H08_GoldenCrown")
    crown.location, crown.scale = (0, 36, 1.3), (22, 22, 22)
    throne((0, 31, 1.3), 0, 1.6)
    party_lights((0, 30, 0), 14, 16, 4)
    for k in range(6):
        side = -1 if k % 2 else 1
        R.palm((side * (12 + k), 8 + k * 6, 0), 7, seed=k)
        R.point_light((side * 10, 8 + k * 6, 3), 250, ["#F72585", "#B5E48C"][k % 2], 0.4, shadow=False)
    r = R.raft((0, 8.5, 0.05), 180)
    crew4(r, names=("Alex", "Julien", "Gloria", "Melman"))
    R.gas_cloud((0.3, 11.8, 0.9), 1.0, color="#F72585", n=6, seed=3)
    confetti((0, 30, 4), (12, 4, 8), 70)
    R.camera((3, -8, 4.4), (0, 22, 4.5), 28)


LOADING = [("W1", load_w1), ("W2", load_w2), ("W3", load_w3), ("W4", load_w4), ("W5", load_w5), ("W6", load_w6)]


# ---------------------------------------------------------------------------
def run_story(names):
    for m in names:
        paths = []
        for i, fn in enumerate(MOMENTS[m]):
            R.new_scene((W, H), samples=24)
            fn()
            p = os.path.join(R.STORY_DIR, m, f"{m}_{i + 1:02d}.png")
            R.render(p, px=2.6)
            R.vignette(p, 0.32)
            paths.append(p)
        R.contact_sheet(paths, os.path.join(gs.PREVIEW_DIR, f"Story_{m}_sheet.png"), cols=3, cell=(640, 360))


def run_loading(only=None):
    paths = []
    for wid, fn in LOADING:
        if only and wid not in only:
            continue
        R.new_scene((W, H), samples=24)
        fn()
        p = os.path.join(R.UI_DIR, "Loading", f"Loading_{wid}.png")
        R.render(p, px=2.4)
        R.vignette(p, 0.3)
        paths.append(p)
    allp = [os.path.join(R.UI_DIR, "Loading", f"Loading_{w}.png") for w, _ in LOADING]
    R.contact_sheet([p for p in allp if os.path.exists(p)], os.path.join(gs.PREVIEW_DIR, "Loading_sheet.png"),
                    cols=3, cell=(640, 360))


if __name__ == "__main__":
    a = R.args()
    R.ensure_cache()
    moments = [x for x in a if x in MOMENTS]
    worlds = [x for x in a if x in dict(LOADING)]
    if "--loading" in a or worlds:
        run_loading(worlds or None)
    if "--story" in a or moments or not a:
        run_story(moments or list(MOMENTS))
    if not a:
        run_loading()
