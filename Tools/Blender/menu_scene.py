"""Escena 3D del título: la balsa con los 5 personajes en la piscina del zoológico, animada en bucle (12 s a 30 fps)
para usarla de fondo del menú principal.
blender --background --python menu_scene.py [-- --no-render]
Salida (Tools/Export/Menu/):
  TitleScene.blend         escena completa con materiales planos, luces y cámara (Z arriba, metros)
  TitleScene.fbx           mallas, esqueletos y cámara con una sola toma de 360 cuadros (0-360, 30 fps, en bucle)
  TitleScene_preview.png   fotograma de referencia 1920x1080 (toon, como las demás ilustraciones)
Solo llevan curvas de animación los objetos que se mueven (balsa, personajes, nubes, noria, banderas, cámara).
Animación: balsa que cabecea, 4 animales sentados (Idle_Sit) y Julien de pie bailando (Victory) con la misma
duración de bucle, nubes de pedo que salen por la popa, noria y bandera del zoológico girando/ondeando y una cámara
que se mece en un arco corto. Las piezas del escenario vienen de Tools/Export/Environment/W1 (aplanadas: sin raíces
rotadas)."""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
import gs_render as R
import party
import env_kit as k
from gs_common import Vector, Matrix, Quaternion

OUT = os.path.join(gs.EXPORT_DIR, "Menu")
H = 360  # cuadros del bucle
POOL_R = 10.0
TARGET = Vector((0.3, 0.4, 1.1))
AZ = math.radians(-31)  # la cámara está a la izquierda del eje -Y
FWD = Vector((-math.sin(AZ), math.cos(AZ), 0.0))  # hacia el fondo de la escena
RIGHT = Vector((math.cos(AZ), math.sin(AZ), 0.0))


def V(d, s, z=0.55):
    """Punto del escenario: d metros hacia el fondo y s hacia la derecha desde el centro de la piscina."""
    p = Vector((0.3, 0.5, 0)) + FWD * d + RIGHT * s
    return (p.x, p.y, z)


def M(col, rough=0.65, em=0.0):
    return gs.mat(col, rough=rough, emission=em)


# ---------------------------------------------------------------------------
# Piezas del escenario (Principled plano; el render las convierte a toon)
# ---------------------------------------------------------------------------
def flat_asset(rel, loc=(0, 0, 0), rot=0.0, scale=1.0, tag=None):
    """Importa un FBX de Tools/Export, lo aplana (sin raíces rotadas, transformaciones aplicadas) y devuelve
    {nombre original: objeto}. Cada parte conserva su pivote para poder animarla."""
    root = R.asset(rel, loc, rot, scale)
    bpy.context.view_layer.update()
    parts = {}
    meshes = [o for o in root.children_recursive if o.type == "MESH"]
    for o in meshes:
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
    for o in list(root.children_recursive):
        if o.type != "MESH":
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.objects.remove(root, do_unlink=True)
    for o in meshes:
        o.data = o.data.copy()  # las copias comparten malla: hace falta datos propios para aplicar la rotación
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        base = o.name.split(".")[0]
        parts[base] = o
        o.name = f"{tag}_{base}" if tag else base
        o.data.name = o.name
    # el contenedor de plantillas deja copias ocultas; las quitamos al final (clean_templates)
    return parts


def clean_templates():
    col = bpy.data.collections.get("GS_Templates")
    if col:
        for o in list(col.objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(col)


def pool_and_plaza():
    Rr = POOL_R
    stone, rim, tile = M("#F2EFE6"), M("#E7E3F2"), M("#9FE3F0")
    P = [party.ring_lathe("PoolWall", [(Rr, -1.6), (Rr + 0.7, -1.6), (Rr + 0.7, 0.55), (Rr + 0.45, 0.65), (Rr, 0.55)], [stone], seg=48),
         party.ring_lathe("Plaza", [(Rr + 0.7, 0.0), (90.0, 0.0), (90.0, 0.55), (Rr + 0.7, 0.55)], [M("#B9C2CC")], seg=48)]
    P.append(gs.lathe("PoolFloor", [(0.0, -1.6), (Rr, -1.6), (Rr, -1.5), (0.0, -1.5)], tile, seg=48))
    P.append(gs.lathe("Water", [(0.0, -0.05), (Rr - 0.02, -0.05), (Rr - 0.02, 0.0), (0.0, 0.0)], M("#2DB6E8", 0.1), seg=48))
    P.append(party.ring_lathe("PlazaTrim", [(Rr + 0.7, 0.55), (Rr + 3.2, 0.55), (Rr + 3.2, 0.6), (Rr + 0.7, 0.6)], [M("#F7C6A0")], seg=48))
    # cuerda de carril con flotadores alternados alrededor de la balsa
    rope = []
    pts = [Vector(((Rr - 1.4) * math.cos(a), (Rr - 1.4) * math.sin(a), 0.02)) for a in [i / 60 * 2 * math.pi for i in range(61)]]
    rope.append(gs.tube("LaneRope", pts, 0.025, M("#D9B77A", 0.9), seg=4, cap=0, sub=1))
    for i in range(30):
        a = (i + 0.5) / 30 * 2 * math.pi
        rope.append(gs.ellipsoid(f"Float{i}", ((Rr - 1.4) * math.cos(a), (Rr - 1.4) * math.sin(a), 0.03), (0.2, 0.2, 0.13),
                                 M("#F72585" if i % 2 else "#F7F4EC"), seg=8, rings=5))
    k.join("LaneRope", rope)
    # escaleras cromadas en dos lados
    chrome = M("#B9C2CC", 0.3)
    lad = []
    for a in (math.radians(-30), math.radians(215)):
        c, s = math.cos(a), math.sin(a)
        for dx in (-0.35, 0.35):
            x, y = (Rr + 0.15) * c - s * dx, (Rr + 0.15) * s + c * dx
            lad.append(gs.tube(f"Lad{a:.2f}{dx}", [(x * 0.96, y * 0.96, -0.9), (x * 0.96, y * 0.96, 1.3), (x * 1.01, y * 1.01, 1.45)], 0.04, chrome,
                               seg=5, cap=1, sub=1))
    k.join("Ladders", lad)
    return P


def wave_machine():
    """Máquina de olas en el borde lejano de la piscina: bloque con tres bocas arqueadas y espuma."""
    y = POOL_R - 0.3
    white, blue, foam = M("#F7F4EC"), M("#3A86FF"), M("#FFFFFF", 0.4)
    P = [gs.rounded_box("WaveBlock", (0, y + 1.6, 1.5), (9.0, 3.2, 3.0), 0.25, [white, blue], segments=1)]
    gs.paint_faces(P[0], lambda c: c.y < y + 0.2 and c.z < 1.0, 1)
    for i, x in enumerate((-2.8, 0.0, 2.8)):
        P.append(gs.ellipsoid(f"Mouth{i}", (x, y - 0.1, 0.85), (1.0, 0.25, 0.85), M("#1B5FBF"), seg=14, rings=8))
        P.append(gs.ellipsoid(f"Foam{i}", (x, y - 1.2, 0.12), (1.1, 0.8, 0.22), foam, seg=12, rings=6))
        P.append(gs.ellipsoid(f"Crest{i}", (x, y - 2.4, 0.1), (1.6, 0.6, 0.18), foam, seg=12, rings=6))
    P.append(gs.rounded_box("Sign", (0, y + 3.35, 3.5), (6.0, 0.4, 1.4), 0.2, [M("#F72585")], segments=1))
    P.append(gs.ellipsoid("SignStar", (0, y + 3.0, 3.55), (0.5, 0.1, 0.5), M("#FFD60A", 0.5, 0.0), seg=8, rings=5))
    wm = k.join("WaveMachine", P)
    wm.rotation_euler = (0, 0, math.radians(-72))
    return [wm]


def loungers():
    P = []
    spots = [(*V(3, -13.5)[:2], -31 + 80), (*V(10, -13.0)[:2], -31 + 100), (*V(2, 13.5)[:2], -31 - 80), (*V(9, 13.2)[:2], -31 - 100)]
    for i, (x, y, rot) in enumerate(spots):
        col = M(("#F72585", "#4CC9F0", "#FFB703", "#B5E48C")[i])
        a = [gs.rounded_box(f"Lounger{i}", (0, 0, 0.35), (0.9, 2.0, 0.22), 0.08, col, segments=1),
             gs.rounded_box(f"LoungerBack{i}", (0, 0.85, 0.7), (0.9, 0.75, 0.2), 0.08, col, rot=("X", -55), segments=1)]
        for sx in (1, -1):
            for sy in (1, -1):
                a.append(gs.capsule(f"Leg{i}{sx}{sy}", (0.38 * sx, 0.9 * sy, 0.0), (0.38 * sx, 0.9 * sy, 0.28), 0.04, M("#B9C2CC"), seg=4, cap=0, sub=1))
        a.append(gs.capsule(f"Pole{i}", (1.3, 0.0, 0.0), (1.3, 0.0, 3.0), 0.05, M("#E7E3F2"), seg=5, cap=1, sub=1))
        can = gs.cone(f"Canopy{i}", (1.3, 0, 2.55), (1.3, 0, 3.35), 1.6, 0.05, [M("#F7F4EC"), col], seg=12, round_tip=False)
        gs.bands_by_plane(can, (1, 0, 0), (0, 0, 0), 0.7, 0.5, 1, cut=True)
        a.append(can)
        for o in a:
            o.location = Vector((x, y, 0.55))
            o.rotation_euler = (0, 0, math.radians(rot))
        P += a
    return [k.join("Loungers", P)]


def beach_balls():
    cols = [M("#F72585"), M("#F7F4EC"), M("#B5E48C"), M("#F7F4EC"), M("#4CC9F0"), M("#FFD60A")]
    out = []
    for i, (x, y) in enumerate((V(-2.5, -4.2)[:2], V(2.0, 5.5)[:2], V(8, -6)[:2])):
        b = gs.ellipsoid(f"BeachBall{i}", (x, y, 0.28), 0.5, cols, seg=18, rings=10)
        for f in b.data.polygons:
            f.material_index = int(((math.atan2(f.center.y - y, f.center.x - x) + math.pi) / (2 * math.pi)) * 6) % 6
        out.append(b)
    return out


def party_palms():
    P = []
    pal = dict(trunk=M("#9C6B3E"), trunk2=M("#6B4A2A"), l1=M("#3FA34D"), l2=M("#2D7A3A"), nut=M("#F77F00"))
    for i, (x, y, v) in enumerate(((*V(-4, -17)[:2], 0), (*V(0, 18)[:2], 2), (*V(16, -17)[:2], 1), (*V(18, 17)[:2], 0))):
        parts = k.palm(v, pal["trunk"], pal["trunk2"], [pal["l1"], pal["l2"]], pal["nut"], seed=10 + i)
        for o in parts:
            o.location = Vector((x, y, 0.55))
            o.scale = (1.15, 1.15, 1.15)
        P += parts
    return [k.join("Palms", P)]


def skyline():
    """Anillo de edificios pastel a 95 m: la ciudad del zoológico al fondo."""
    import random
    rnd = random.Random(7)
    cols = ["#F2B5D4", "#B8E0F6", "#FFE4A1", "#C9B6F2", "#BEE8C6", "#FFC9A8"]
    P = []
    for i in range(46):
        a = i / 46 * 2 * math.pi
        r = 95 + rnd.uniform(-5, 8)
        h = rnd.uniform(10, 34)
        w = rnd.uniform(7, 13)
        P.append(gs.rounded_box(f"Tower{i}", (r * math.cos(a), r * math.sin(a), h / 2), (w, w, h), 0.4, M(cols[i % 6]), segments=1, rot=("Z", math.degrees(a))))
        if i % 3 == 0:
            P.append(gs.cone(f"Spire{i}", (r * math.cos(a), r * math.sin(a), h), (r * math.cos(a), r * math.sin(a), h + 6), w * 0.35, 0.05, M("#F72585"), seg=6))
    k.join("Skyline", P)


def gas_puffs(root):
    """Tres nubes por la popa que crecen, se alejan y se disuelven (bucle de 120 cuadros, desfasadas)."""
    puff = M("#8EDB4F", 0.5)
    out = []
    for i in range(3):
        parts = [gs.ellipsoid(f"Puff{i}_{j}", (0.0, 0.0, 0.0), (0.32 - 0.06 * j, 0.32 - 0.06 * j, 0.27 - 0.05 * j), puff, seg=10, rings=6) for j in range(3)]
        for j, o in enumerate(parts):
            o.location = Vector(((j - 1) * 0.3, 0.0, 0.1 * j))
        joined = k.join(f"Puff_{i}", parts)
        k.set_origin(joined, (0, 0, 0))
        joined.parent = root
        out.append(joined)
    return out


# ---------------------------------------------------------------------------
# Animación en bucle
# ---------------------------------------------------------------------------
def key_obj(o, frame, loc=None, rot=None, scale=None):
    if loc is not None:
        o.location = Vector(loc)
        o.keyframe_insert("location", frame=frame)
    if rot is not None:
        o.rotation_euler = rot
        o.keyframe_insert("rotation_euler", frame=frame)
    if scale is not None:
        o.scale = Vector(scale) if not isinstance(scale, (int, float)) else (scale,) * 3
        o.keyframe_insert("scale", frame=frame)


def anim_raft(root):
    base = Vector(root.location)
    z0 = root.rotation_euler.z
    for f in range(0, H + 1, 10):
        u = f / H * 3  # 3 cabeceos
        s = math.sin(2 * math.pi * u)
        c = math.cos(2 * math.pi * u)
        key_obj(root, f, loc=base + Vector((0.0, 0.0, 0.045 * s)),
                rot=(math.radians(1.6 * c), math.radians(1.2 * s), z0 + math.radians(1.2 * math.sin(2 * math.pi * u * 0.5))))


def anim_puffs(puffs, stern):
    """stern: punto local de la popa (+Y de la balsa)."""
    for i, p in enumerate(puffs):
        for f in range(0, H + 1, 6):
            u = ((f / 120.0) + i / 3.0) % 1.0
            s = (math.sin(math.pi * u) ** 0.8) * 1.15
            loc = Vector(stern) + Vector((0.15 * math.sin(u * 5 + i), 2.6 * u, 0.3 + 0.9 * u))
            key_obj(p, f, loc=loc, scale=max(s, 0.001))


def bake_loop(actor, clip, cycles, phase=0.0, step=2):
    """Toma un clip en bucle del personaje y lo vuelve a muestrear en H cuadros con 'cycles' repeticiones exactas, para que
    el bucle de toda la escena cierre sin salto. Deja el resultado como acción Title_<Nombre>."""
    arm = actor.arm
    sc = bpy.context.scene
    src = bpy.data.actions[f"{actor.name}|{clip}"]
    f0, f1 = src.frame_range
    ad = arm.animation_data or arm.animation_data_create()
    samples = []
    for f in range(0, H + 1, step):
        u = (f / H * cycles + phase) % 1.0
        fr = f0 + u * (f1 - f0)
        ad.action = src
        if hasattr(ad, "action_slot") and ad.action_slot is None and len(src.slots):
            ad.action_slot = src.slots[0]
        sc.frame_set(int(fr), subframe=fr - int(fr))
        samples.append((f, [(pb.rotation_quaternion.copy(), pb.location.copy()) for pb in arm.pose.bones]))
    ad.action = None
    act = bpy.data.actions.new(f"Title_{actor.name}")
    ad.action = act
    for f, vals in samples:
        for pb, (q, l) in zip(arm.pose.bones, vals):
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion, pb.location = q, l
            pb.keyframe_insert("rotation_quaternion", frame=f, group=pb.name)
            if pb.name == "Hips":
                pb.keyframe_insert("location", frame=f, group=pb.name)
    return act


def anim_wheel(parts):
    wheel = parts["Wheel"]
    c = Vector(wheel.location)
    gond = [(n, o, Vector(o.location) - c) for n, o in parts.items() if n.startswith("Gondola_")]
    for f in range(0, H + 1, 12):
        th = 2 * math.pi * f / H
        key_obj(wheel, f, rot=(0, th, 0))
        for n, o, d in gond:
            x = d.x * math.cos(th) + d.z * math.sin(th)
            z = -d.x * math.sin(th) + d.z * math.cos(th)
            key_obj(o, f, loc=c + Vector((x, d.y, z)))


def anim_flag(flag):
    r0 = flag.rotation_euler.copy()
    for f in range(0, H + 1, 10):
        u = f / H * 6
        key_obj(flag, f, rot=(r0.x, r0.y, r0.z + math.radians(9) * math.sin(2 * math.pi * u)))


def anim_camera(cam, target):
    prev = None
    for f in range(0, H + 1, 6):
        u = f / H
        az = AZ + math.radians(9 * math.sin(2 * math.pi * u))
        el = math.radians(11 + 1.5 * math.sin(2 * math.pi * u * 2))
        dist = 8.9 + 0.5 * math.sin(2 * math.pi * u + 1.3)
        pos = target + Vector((dist * math.cos(el) * math.sin(az), -dist * math.cos(el) * math.cos(az), dist * math.sin(el)))
        q = (target - pos).to_track_quat("-Z", "Y")
        e = q.to_euler("XYZ", prev) if prev else q.to_euler("XYZ")
        prev = e
        key_obj(cam, f, loc=pos, rot=e)


# ---------------------------------------------------------------------------
# Montaje
# ---------------------------------------------------------------------------
SEATS = [("Alex", (-0.9, -1.3, 0.5), 8, 0.00), ("Marty", (0.9, -1.3, 0.5), -8, 0.25), ("Gloria", (-1.15, 1.0, 0.5), -22, 0.5),
         ("Melman", (1.45, 0.2, 0.5), 40, 0.75)]


def build():
    R.new_scene((1920, 1080), samples=32)
    sc = bpy.context.scene
    sc.frame_start, sc.frame_end = 0, H
    sc.render.fps = 30
    # suelo y piscina
    pool_and_plaza()
    wave_machine()
    loungers()
    beach_balls()
    party_palms()
    # estructuras del zoológico (FBX ya exportados de W1)
    gx, gy, _ = V(40, 0)
    gate = flat_asset("Environment/W1/W1_ZooGate.fbx", (gx, gy, 0.55), -31, 1.1, "ZooGate")
    wx, wy, _ = V(88, -22)
    wheel = flat_asset("Environment/W1/W1_FerrisWheel.fbx", (wx, wy, 0.55), 0, 1.5, "Wheel")
    flags = []
    for i, sd in enumerate((-13, 13)):
        px, py, _ = V(37, sd)
        flags.append(flat_asset("Environment/W1/W1_Flagpole.fbx", (px, py, 0.55), 0, 1.3, f"Flagpole{i}"))
    for i, (d, sd) in enumerate(((22, -10), (22, 10), (30, -20), (30, 20), (14, -21), (13, 22))):
        px, py, _ = V(d, sd)
        flat_asset("Environment/W1/W1_Topiary_A.fbx" if i % 2 == 0 else "Environment/W1/W1_Topiary_B.fbx", (px, py, 0.55), -31 + 15 * (i % 3), 1.6, f"Topiary{i}")
    for i, (d, sd) in enumerate(((16, -12.5), (16, 12.5), (6, -16), (6, 16), (24, -5), (24, 5))):
        px, py, _ = V(d, sd)
        flat_asset("Environment/W1/W1_Lamp_A.fbx", (px, py, 0.6), -31 + (90 if sd < 0 else -90), 1.0, f"Lamp{i}")
    skyline()
    # balsa y tripulación
    raft = R.raft((0, -0.5, 0.06), -31)
    crew = {}
    for n, loc, rot, ph in SEATS:
        a = R.Actor(n)
        a.arm.parent = raft
        a.arm.location = Vector(loc)
        a.arm.rotation_euler = (0, 0, math.radians(rot))
        crew[n] = (a, "Idle_Sit", 6, ph)
    barrel = gs.lathe("BeansBarrel", [(0.0, 0.0), (0.34, 0.0), (0.42, 0.25), (0.43, 0.5), (0.4, 0.75), (0.32, 0.9), (0.0, 0.9)], [M("#F72585")], seg=14)
    barrel.location = Vector((0.0, 0.45, 0.17))
    hoops = [gs.torus(f"BarrelHoop{i}", (0, 0, z), r, 0.03, M("#F5C431", 0.35), seg=14, mseg=4) for i, (z, r) in enumerate(((0.12, 0.38), (0.45, 0.435), (0.78, 0.36)))]
    for h in hoops:
        h.location = Vector((0.0, 0.45, 0.17))
    barrel = k.join("BeansBarrel", [barrel] + hoops)
    barrel.parent = raft
    j = R.Actor("Julien")
    j.arm.parent = raft
    j.arm.location = Vector((0.0, 0.45, 1.07))
    j.arm.rotation_euler = (0, 0, math.radians(-4))
    foam = [gs.torus("RaftFoam", (0, 0, 0.02), 2.2, 0.05, M("#FFFFFF", 0.4), seg=36, mseg=4), gs.torus("RaftFoam2", (0, 0, 0.02), 2.55, 0.035, M("#E8FBFF", 0.4), seg=36, mseg=4)]
    foam = k.join("RaftFoam", foam)
    foam.parent = raft
    crew["Julien"] = (j, "Victory", 8, 0.0)
    clean_templates()
    puffs = gas_puffs(raft)
    # cámara
    target = TARGET
    cam = R.camera((0, 0, 0), target, 32)
    cam.name = "TitleCamera"
    # animación
    anim_raft(raft)
    anim_puffs(puffs, (0.0, 2.0, 0.3))
    anim_wheel(wheel)
    for fp in flags:
        anim_flag(fp["Flag"])
    anim_camera(cam, target)
    for n, (a, clip, cycles, ph) in crew.items():
        bake_loop(a, clip, cycles, ph)
    sc.frame_set(0)
    for act in list(bpy.data.actions):
        if "|" in act.name:  # clips de los personajes: ya están horneados en Title_<Nombre>
            bpy.data.actions.remove(act)
    # el cielo y la luz solo para ver el .blend (el FBX no lleva luces)
    R.world_sky("#4DB0F0", "#D2F1FF", ambient=0.36)
    R.sun((48, 0, 35), 1.0, "#FFF4DE")
    R.sun((70, 0, 200), 0.3, name="Rim", shadow=False)
    return cam, raft, crew


def export(cam):
    os.makedirs(OUT, exist_ok=True)
    sc = bpy.context.scene
    bpy.ops.object.select_all(action="DESELECT")
    objs = [o for o in sc.objects if o.type in ("MESH", "ARMATURE", "EMPTY", "CAMERA") and o.visible_get()]
    for o in objs:
        o.select_set(True)
    sc.frame_start, sc.frame_end = 0, H
    path = os.path.join(OUT, "TitleScene.fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY", "CAMERA"}, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="FACE", add_leaf_bones=False, use_armature_deform_only=False,
        primary_bone_axis="Y", secondary_bone_axis="X", bake_anim=True, bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_step=2.0,
        # factor > 0: el exportador descarta las curvas constantes (objetos estáticos sin animación); los huesos las conservan
        bake_anim_simplify_factor=0.5, path_mode="AUTO", embed_textures=False)
    print(f"[gs] exportado {path} ({os.path.getsize(path) // 1024} KB)")
    blend = os.path.join(OUT, "TitleScene.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend, check_existing=False, compress=True)
    print(f"[gs] guardado {blend} ({os.path.getsize(blend) // 1024} KB)")


def stats():
    sc = bpy.context.scene
    meshes = [o for o in sc.objects if o.type == "MESH"]
    tris = 0
    for o in meshes:
        tris += gs.tri_count(o)
    arms = [o for o in sc.objects if o.type == "ARMATURE"]
    print(f"[gs] TitleScene: {len(meshes)} mallas, {tris} triángulos, {len(arms)} esqueletos, {len(sc.objects)} objetos")
    for a in arms:
        print(f"[gs]   {a.name}: acción {a.animation_data.action.name if a.animation_data and a.animation_data.action else None}")


def render_preview(frames=(60,), sheet=True):
    sc = bpy.context.scene
    R.world_sky("#7FC8F5", "#EAF8FF", ambient=0.4)
    R.sun((48, 0, 35), 1.05, "#FFF4DE")
    R.set_bands(rim=0.22)
    tiles = []
    for f in frames:
        sc.frame_set(f)
        p = os.path.join(gs.PREVIEW_DIR, f"_tmp_title_{f}.png") if sheet else os.path.join(OUT, "TitleScene_preview.png")
        R.render(p, px=2.2)
        tiles.append(p)
    return tiles


if __name__ == "__main__":
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    R.ensure_cache()
    cam, raft, crew = build()
    stats()
    export(cam)
    if "--no-render" not in sys.argv:
        tiles = render_preview((0, 120, 240))
        R.contact_sheet(tiles, os.path.join(gs.PREVIEW_DIR, "Menu_TitleScene_sheet.png"), cols=3, cell=(640, 360))
        # el fotograma de referencia es el del medio
        import shutil
        shutil.copyfile(tiles[1], os.path.join(OUT, "TitleScene_preview.png"))
        for t in tiles:
            os.remove(t)
