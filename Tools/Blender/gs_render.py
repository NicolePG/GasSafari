"""GAS SAFARI · ilustraciones renderizadas en Blender 4.5 (EEVEE): arte de interfaz, viñetas y fondos.

Sombreado toon de 3 bandas (Shader to RGB + rampa), contorno por casco invertido con grosor constante en
píxeles, cielo por rayo de cámara, agua y suelos de dibujo animado, montaje de escenas con los modelos que ya
generan los demás scripts (personajes con sus clips, balsa, power-ups, escenario y fauna en Tools/Export).

Los personajes se construyen con su script (alex.py, ...) más char_anims.py y se guardan en
Tools/Blender/previews/_cache/<Nombre>.blend (no se sube a Git); se reconstruyen solos si cambia un script.
"""
import bpy
import bmesh
import importlib
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
from gs_common import Vector, Matrix, Quaternion

BLENDER_DIR = os.path.dirname(os.path.abspath(__file__))
CACHE_DIR = os.path.join(gs.PREVIEW_DIR, "_cache")
UI_DIR = os.path.join(gs.EXPORT_DIR, "UI")
STORY_DIR = os.path.join(gs.EXPORT_DIR, "Story")
CHARS = ["Alex", "Marty", "Melman", "Gloria", "Julien"]
# docs/03 (CharacterData.uiColor)
UI_COLOR = {"Alex": "#E59A2F", "Marty": "#5C6F82", "Melman": "#F2C94C", "Gloria": "#9B8FD6", "Julien": "#6CC24A"}
GAS_GREEN = "#8EDB4F"


def lin(hexcol, k=1.0):
    r, g, b, _ = gs.hex_rgba(hexcol)
    return (r * k, g * k, b * k)


# ---------------------------------------------------------------------------
# Caché de modelos generados por script
# ---------------------------------------------------------------------------
def _stale(path, sources):
    if not os.path.exists(path):
        return True
    t = os.path.getmtime(path)
    return any(os.path.getmtime(s) > t for s in sources)


def _save_cache(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=path, check_existing=False, compress=True)


def char_cache(name):
    """Construye el personaje con su script, sus clips estándar y los de char_anims, y lo guarda en caché."""
    path = os.path.join(CACHE_DIR, name + ".blend")
    srcs = [os.path.join(BLENDER_DIR, f) for f in ("gs_common.py", "char_anims.py", name.lower() + ".py")]
    if not _stale(path, srcs):
        return path
    import char_anims
    mod = importlib.import_module(name.lower())
    cap = {}
    orig = gs.finish_character
    gs.finish_character = lambda n, body, arm, spec, poses=None: cap.update(body=body, arm=arm, spec=spec)
    try:
        mod.build()
    finally:
        gs.finish_character = orig
    arm, spec = cap["arm"], cap["spec"]
    gs.build_standard_actions(arm, spec)
    char_anims.build_clips(arm, spec, name)
    for a in list(bpy.data.actions):
        a.name = f"{name}|{a.name}"
        a.use_fake_user = True
    arm["gs_char"] = name
    _save_cache(path)
    print(f"[gs] caché {path}")
    return path


def raft_cache():
    path = os.path.join(CACHE_DIR, "Raft.blend")
    if not _stale(path, [os.path.join(BLENDER_DIR, f) for f in ("gs_common.py", "props.py")]):
        return path
    import props
    rs, ex = gs.render_sheet, gs.export_fbx
    gs.render_sheet = lambda *a, **k: None
    gs.export_fbx = lambda *a, **k: None
    try:
        props.build_raft()
    finally:
        gs.render_sheet, gs.export_fbx = rs, ex
    _save_cache(path)
    return path


def ensure_cache(names=CHARS, raft=True):
    for n in names:
        char_cache(n)
    if raft:
        raft_cache()


def append_blend(path):
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.objects = list(src.objects)
        dst.actions = list(src.actions)
    out = []
    for o in dst.objects:
        if o is None or o.type == "CAMERA":
            continue
        bpy.context.scene.collection.objects.link(o)
        out.append(o)
    return out


# ---------------------------------------------------------------------------
# Actores (personajes con pose estática)
# ---------------------------------------------------------------------------
class Actor:
    def __init__(self, name):
        self.name = name
        objs = append_blend(char_cache(name))
        self.arm = next(o for o in objs if o.type == "ARMATURE")
        self.body = next(o for o in objs if o.type == "MESH")
        self.objs = [self.arm, self.body]
        self._head_fit()

    def _head_fit(self):
        """Parte superior de la cabeza en reposo (espacio de la armature) y ancho de cabeza."""
        vg = self.body.vertex_groups.get("Head")
        pts = []
        for v in self.body.data.vertices:
            for g in v.groups:
                if g.group == vg.index and g.weight > 0.5:
                    pts.append(self.body.matrix_local @ v.co)
        xs = sorted(abs(p.x) for p in pts)
        zs = sorted(p.z for p in pts)
        self.head_w = xs[int(len(xs) * 0.95)] * 2
        top = zs[int(len(zs) * 0.97)]
        ys = [p.y for p in pts if p.z > top - 0.08]
        self.head_top = Vector((0, sum(ys) / len(ys), top))

    def place(self, loc=(0, 0, 0), rot=0.0, scale=1.0):
        self.arm.location = Vector(loc)
        self.arm.rotation_euler = (0, 0, math.radians(rot))
        self.arm.scale = (scale, scale, scale)
        return self

    def pose(self, clip, frac=0.0):
        act = bpy.data.actions[f"{self.name}|{clip}"]
        ad = self.arm.animation_data or self.arm.animation_data_create()
        ad.action = act
        if hasattr(ad, "action_slot") and ad.action_slot is None and len(act.slots):
            ad.action_slot = act.slots[0]
        f0, f1 = act.frame_range
        bpy.context.scene.frame_set(int(round(f0 + (f1 - f0) * frac)))
        vals = [(pb.rotation_quaternion.copy(), pb.location.copy()) for pb in self.arm.pose.bones]
        ad.action = None
        for pb, (q, l) in zip(self.arm.pose.bones, vals):
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion, pb.location = q, l
        return self

    def custom(self, pose):
        gs.apply_pose(self.arm, pose)
        return self

    def tweak(self, bone, *rots, move=None):
        """Suma rotaciones (eje de mundo del personaje, grados) sobre la pose actual."""
        pb = self.arm.pose.bones.get(bone)
        if pb is None:
            return self
        q = Quaternion()
        for axis, deg in rots:
            q = Quaternion(gs._AXES[axis], math.radians(deg)) @ q
        R = pb.bone.matrix_local.to_quaternion()
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (R.inverted() @ q @ R) @ pb.rotation_quaternion
        if move is not None:
            pb.location = pb.location + R.inverted() @ Vector(move)
        return self

    def head_matrix(self):
        bpy.context.view_layer.update()
        pb = self.arm.pose.bones["Head"]
        return self.arm.matrix_world @ pb.matrix @ pb.bone.matrix_local.inverted()

    def hat_anchor(self):
        """(punto de la base del sombrero en reposo, escala) — HAT_FIT o, si falta, medido de la malla."""
        if self.name in HAT_FIT:
            y, z, s = HAT_FIT[self.name]
            return Vector((0, y, z)), s
        return self.head_top, self.head_w / 0.42

    def wear(self, root):
        """Coloca un sombrero (objeto raíz con pivote en la base) sobre la cabeza con la pose actual."""
        p, s = self.hat_anchor()
        root.matrix_world = self.head_matrix() @ Matrix.Translation(p) @ Matrix.Scale(s, 4)
        return root

    def world_point(self, bone, t=1.0):
        bpy.context.view_layer.update()
        pb = self.arm.pose.bones[bone]
        return self.arm.matrix_world @ pb.head.lerp(pb.tail, t)


# base del sombrero en reposo (y, z en m, espacio Blender del personaje) y escala (hechos para cabeza de 0.42 m),
# a partir de la elipse de la cabeza de cada script (alex.py: hc y radios)
HAT_FIT = {"Alex": (-0.03, 1.665, 1.18), "Marty": (-0.02, 1.625, 0.85), "Melman": (-0.08, 2.47, 0.86),
           "Gloria": (-0.04, 1.5, 1.1), "Julien": (-0.01, 0.875, 0.74)}


def raft(loc=(0, 0, 0), rot=0.0, scale=1.0):
    objs = append_blend(raft_cache())
    root = next(o for o in objs if o.type == "EMPTY")
    root.location, root.rotation_euler, root.scale = Vector(loc), (0, 0, math.radians(rot)), (scale,) * 3
    return root


# asientos de la balsa (radio 2 m): en el borde, mirando hacia afuera-adelante
SEATS = [(-1.25, -0.95, 0.12, 200), (1.25, -0.95, 0.12, 160), (-1.3, 0.9, 0.12, 250), (1.3, 0.9, 0.12, 110)]


def crew_on_raft(raft_root, names, clip="Idle_Sit", frac=0.2, seats=None, poses=None):
    """Sienta personajes en el borde de la balsa (posiciones locales de la balsa)."""
    bpy.context.view_layer.update()
    mw = raft_root.matrix_world
    rz = math.degrees(raft_root.rotation_euler.z)
    actors = []
    for i, n in enumerate(names):
        x, y, z, r = (seats or SEATS)[i]
        a = Actor(n)
        p = mw @ Vector((x, y, z + 0.0))
        a.place(p, rz + r, raft_root.scale.x)
        c, f = (poses or {}).get(n, (clip, frac))
        a.pose(c, f)
        actors.append(a)
    return actors


# ---------------------------------------------------------------------------
# Assets FBX ya exportados (escenario, fauna, jefes)
# ---------------------------------------------------------------------------
_TPL = {}


def _templates():
    col = bpy.data.collections.get("GS_Templates")
    if col is None:
        col = bpy.data.collections.new("GS_Templates")
        bpy.context.scene.collection.children.link(col)
        bpy.context.view_layer.layer_collection.children["GS_Templates"].exclude = True
    return col


def asset(rel, loc=(0, 0, 0), rot=0.0, scale=1.0, tilt=None):
    """Instancia un FBX de Tools/Export (p. ej. 'Environment/W1/W1_ZooGate.fbx'). Devuelve el vacío raíz."""
    path = os.path.join(gs.EXPORT_DIR, rel)
    if path not in _TPL:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=path)
        new = [o for o in bpy.data.objects if o not in before]
        col = _templates()
        for o in new:
            for c in list(o.users_collection):
                c.objects.unlink(o)
            col.objects.link(o)
        _TPL[path] = new
    tpl = _TPL[path]
    mp = {}
    for o in tpl:
        c = o.copy()
        bpy.context.scene.collection.objects.link(c)
        mp[o] = c
    root = bpy.data.objects.new(os.path.splitext(os.path.basename(rel))[0], None)
    bpy.context.scene.collection.objects.link(root)
    for o, c in mp.items():
        if o.parent in mp:
            c.parent = mp[o.parent]
        else:
            c.parent = root
        for m in c.modifiers:
            if m.type == "ARMATURE" and m.object in mp:
                m.object = mp[m.object]
    root.location = Vector(loc)
    root.rotation_euler = (math.radians(tilt[0]) if tilt else 0, math.radians(tilt[1]) if tilt else 0,
                           math.radians(rot))
    root.scale = (scale,) * 3 if not isinstance(scale, (tuple, list)) else scale
    root["gs_asset"] = rel
    return root


def env(world, name, *a, **k):
    return asset(f"Environment/{world}/{world}_{name}.fbx", *a, **k)


def pose_fbx(root, clip, frac=0.0):
    """Pose estática de un asset con esqueleto (fauna, grúa): toma el clip cuyo nombre termina en 'clip'."""
    arm = next((o for o in root.children_recursive if o.type == "ARMATURE"), None)
    if arm is None:
        return
    act = next((a for a in bpy.data.actions if a.name.split("|")[-1] == clip or a.name.endswith(clip)), None)
    if act is None:
        print(f"[gs] aviso: sin clip {clip}: {[a.name for a in bpy.data.actions]}")
        return
    ad = arm.animation_data or arm.animation_data_create()
    ad.action = act
    if hasattr(ad, "action_slot") and ad.action_slot is None and len(act.slots):
        ad.action_slot = act.slots[0]
    f0, f1 = act.frame_range
    bpy.context.scene.frame_set(int(round(f0 + (f1 - f0) * frac)))
    vals = [(pb.rotation_quaternion.copy(), pb.rotation_euler.copy(), pb.location.copy(), pb.scale.copy())
            for pb in arm.pose.bones]
    ad.action = None
    for pb, (q, e, l, s) in zip(arm.pose.bones, vals):
        pb.rotation_quaternion, pb.rotation_euler, pb.location, pb.scale = q, e, l, s


def powerup(name, loc, rot=0.0, scale=1.0):
    """Power-up de props.py (P01_Beans ... P07_GoldenMango) construido en la escena actual."""
    import props
    fn = dict(props.POWERUPS)[name]
    o = props.join(name, fn())
    o.location, o.rotation_euler, o.scale = Vector(loc), (0, 0, math.radians(rot)), (scale,) * 3
    return o


# ---------------------------------------------------------------------------
# Escena, luz y materiales toon
# ---------------------------------------------------------------------------
def new_scene(res=(1920, 1080), transparent=False, samples=24):
    gs.reset_scene()
    _TPL.clear()
    _OUTLINE.clear()
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE_NEXT"
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = transparent
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    sc.render.image_settings.compression = 90
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    sc.eevee.taa_render_samples = samples
    sc.render.filter_size = 1.2
    if hasattr(sc.eevee, "use_shadows"):
        sc.eevee.use_shadows = True
    sc.world = bpy.data.worlds.new("World")
    world_sky("#9AD4F5", "#3E8EDB", ambient=0.32)
    toon_group()
    set_bands()
    return sc


def toon_group():
    ng = bpy.data.node_groups.get("GS_Toon")
    if ng:
        return ng
    ng = bpy.data.node_groups.new("GS_Toon", "ShaderNodeTree")
    ng.interface.new_socket("Color", in_out="INPUT", socket_type="NodeSocketColor")
    ng.interface.new_socket("Emission", in_out="INPUT", socket_type="NodeSocketFloat")
    ng.interface.new_socket("Shader", in_out="OUTPUT", socket_type="NodeSocketShader")
    N, L = ng.nodes, ng.links
    gi, go = N.new("NodeGroupInput"), N.new("NodeGroupOutput")
    dif = N.new("ShaderNodeBsdfDiffuse")
    dif.inputs["Color"].default_value = (1, 1, 1, 1)
    s2r = N.new("ShaderNodeShaderToRGB")
    L.new(dif.outputs[0], s2r.inputs[0])
    ramp = N.new("ShaderNodeValToRGB")
    ramp.name = "Bands"
    ramp.color_ramp.interpolation = "CONSTANT"
    L.new(s2r.outputs["Color"], ramp.inputs["Fac"])
    mul = N.new("ShaderNodeVectorMath")
    mul.operation = "MULTIPLY"
    L.new(ramp.outputs["Color"], mul.inputs[0])
    L.new(gi.outputs["Color"], mul.inputs[1])
    lw = N.new("ShaderNodeLayerWeight")
    lw.inputs["Blend"].default_value = 0.3
    gt = N.new("ShaderNodeMath")
    gt.operation = "GREATER_THAN"
    gt.inputs[1].default_value = 0.78
    L.new(lw.outputs["Facing"], gt.inputs[0])
    lit = N.new("ShaderNodeMath")  # el borde brilla solo donde hay luz
    lit.operation = "GREATER_THAN"
    lit.inputs[1].default_value = 0.45
    L.new(s2r.outputs["Color"], lit.inputs[0])
    rim = N.new("ShaderNodeValue")
    rim.name = "Rim"
    rim.outputs[0].default_value = 0.22
    m1 = N.new("ShaderNodeMath")
    m1.operation = "MULTIPLY"
    L.new(gt.outputs[0], m1.inputs[0])
    L.new(lit.outputs[0], m1.inputs[1])
    m2 = N.new("ShaderNodeMath")
    m2.operation = "MULTIPLY"
    L.new(m1.outputs[0], m2.inputs[0])
    L.new(rim.outputs[0], m2.inputs[1])
    em = N.new("ShaderNodeMath")
    em.operation = "ADD"
    L.new(m2.outputs[0], em.inputs[0])
    L.new(gi.outputs["Emission"], em.inputs[1])
    sc = N.new("ShaderNodeVectorMath")
    sc.operation = "SCALE"
    L.new(gi.outputs["Color"], sc.inputs[0])
    L.new(em.outputs[0], sc.inputs["Scale"])
    add = N.new("ShaderNodeVectorMath")
    add.operation = "ADD"
    L.new(mul.outputs[0], add.inputs[0])
    L.new(sc.outputs[0], add.inputs[1])
    emis = N.new("ShaderNodeEmission")
    L.new(add.outputs[0], emis.inputs["Color"])
    L.new(emis.outputs[0], go.inputs["Shader"])
    return ng


def set_bands(shadow=(0.50, 0.52, 0.72), mid=(0.80, 0.80, 0.90), lit=(1.0, 1.0, 1.0), hi=None,
              t=(0.30, 0.62, 1.35), rim=0.22):
    """Bandas de luz (multiplicadores lineales): sombra < t0 < media < t1 < luz (< t2 < brillo)."""
    ng = toon_group()
    cr = ng.nodes["Bands"].color_ramp
    while len(cr.elements) > 1:
        cr.elements.remove(cr.elements[-1])
    cr.elements[0].position = 0.0
    cr.elements[0].color = (*shadow, 1)
    for pos, col in ((t[0], mid), (t[1], lit), (t[2], hi)):
        if col is None:
            continue
        e = cr.elements.new(min(pos, 1.0) if pos <= 1.0 else 0.999)
        e.color = (*col, 1)
    ng.nodes["Rim"].outputs[0].default_value = rim


def toon_mat(hexcol_or_rgb, name=None, emission=0.0):
    col = lin(hexcol_or_rgb) if isinstance(hexcol_or_rgb, str) else hexcol_or_rgb
    m = bpy.data.materials.new(name or "T_" + (hexcol_or_rgb if isinstance(hexcol_or_rgb, str) else "rgb"))
    _make_toon(m, col, emission)
    return m


def _make_toon(m, col, emission=0.0):
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    g = nt.nodes.new("ShaderNodeGroup")
    g.node_tree = toon_group()
    g.inputs["Color"].default_value = (*col[:3], 1)
    g.inputs["Emission"].default_value = emission
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(g.outputs[0], out.inputs["Surface"])
    m.diffuse_color = (*col[:3], 1)
    m["gs_toon"] = True


def toonify_all():
    """Convierte todos los materiales Principled (gs.mat y FBX importados) en toon."""
    for m in bpy.data.materials:
        if m.get("gs_toon") or m.get("gs_keep"):
            continue
        col, emis = tuple(m.diffuse_color)[:3], 0.0
        if m.use_nodes and m.node_tree:
            b = m.node_tree.nodes.get("Principled BSDF") or next(
                (n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
            if b:
                col = tuple(b.inputs["Base Color"].default_value)[:3]
                es = b.inputs.get("Emission Strength")
                ec = b.inputs.get("Emission Color")
                if es and ec and es.default_value > 0 and max(ec.default_value[:3]) > 0:
                    emis = min(es.default_value, 1.2) * 0.6
        _make_toon(m, col, emis)


_OUTLINE = {}


def outline_mat(hexcol="#1C1A26"):
    if hexcol in _OUTLINE and _OUTLINE[hexcol].name in bpy.data.materials:
        return _OUTLINE[hexcol]
    m = bpy.data.materials.new("GS_Outline")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Color"].default_value = (*lin(hexcol), 1)
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(e.outputs[0], o.inputs["Surface"])
    m.use_backface_culling = True
    if hasattr(m, "use_backface_culling_shadow"):
        m.use_backface_culling_shadow = True
    m["gs_toon"] = True
    _OUTLINE[hexcol] = m
    return m


def add_outlines(px=2.6, color="#1C1A26", objs=None, min_t=0.0015):
    """Casco invertido con grosor ~px píxeles a la distancia de cada objeto."""
    sc = bpy.context.scene
    cam = sc.camera
    bpy.context.view_layer.update()
    res_y = sc.render.resolution_y * sc.render.resolution_percentage / 100
    res_x = sc.render.resolution_x * sc.render.resolution_percentage / 100
    if cam.data.type == "ORTHO":
        per_px = cam.data.ortho_scale / max(res_x, res_y)
    else:
        fov = cam.data.angle
        big = max(res_x, res_y)
        per_px = 2 * math.tan(fov / 2) / big
    om = outline_mat(color)
    camp = cam.matrix_world.translation
    fwd = (cam.matrix_world.to_quaternion() @ Vector((0, 0, -1))).normalized()
    for o in (objs or sc.objects):
        if o.type != "MESH" or o.get("gs_no_outline") or not o.visible_get():
            continue
        if any(m and m.name.startswith("GS_Outline") for m in o.data.materials):
            continue
        bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
        ctr = sum(bb, Vector()) / 8
        if cam.data.type == "ORTHO":
            t = px * per_px
        else:
            # distancia al punto más cercano de la caja (a lo largo de la vista)
            d = max(0.3, min((p - camp).dot(fwd) for p in bb + [ctr]))
            t = px * per_px * d
        sx = sum(o.matrix_world.to_scale()) / 3
        n = len(o.data.materials)
        if n == 0:
            o.data.materials.append(toon_mat("#CCCCCC"))
            n = 1
        o.data.materials.append(om)
        mod = o.modifiers.new("GS_Outline", "SOLIDIFY")
        mod.thickness = -max(t, min_t) / max(sx, 1e-4)
        mod.offset = -1.0
        mod.use_flip_normals = True
        mod.use_even_offset = False
        mod.use_rim = False
        mod.material_offset = n
        mod.use_quality_normals = True


def world_sky(top, horizon, ground=None, ambient=0.32, ambient_col=None, stars=0.0):
    """Cielo en degradado visto por la cámara; luz ambiental plana para el sombreado."""
    w = bpy.context.scene.world
    w.use_nodes = True
    nt = w.node_tree
    nt.nodes.clear()
    N, L = nt.nodes, nt.links
    tc = N.new("ShaderNodeTexCoord")
    sep = N.new("ShaderNodeSeparateXYZ")
    L.new(tc.outputs["Generated"], sep.inputs[0])
    mr = N.new("ShaderNodeMapRange")
    mr.inputs["From Min"].default_value = -0.05
    mr.inputs["From Max"].default_value = 0.65
    L.new(sep.outputs["Z"], mr.inputs["Value"])
    ramp = N.new("ShaderNodeValToRGB")
    cr = ramp.color_ramp
    cr.elements[0].color = (*lin(ground or horizon), 1)
    cr.elements[0].position = 0.0
    e = cr.elements.new(0.08)
    e.color = (*lin(horizon), 1)
    cr.elements[-1].color = (*lin(top), 1)
    L.new(mr.outputs["Result"], ramp.inputs["Fac"])
    sky_col = ramp.outputs["Color"]
    if stars > 0:
        vor = N.new("ShaderNodeTexVoronoi")
        vor.inputs["Scale"].default_value = 140
        L.new(tc.outputs["Generated"], vor.inputs["Vector"])
        st = N.new("ShaderNodeMath")
        st.operation = "LESS_THAN"
        st.inputs[1].default_value = 0.035
        L.new(vor.outputs["Distance"], st.inputs[0])
        hz = N.new("ShaderNodeMath")
        hz.operation = "GREATER_THAN"
        hz.inputs[1].default_value = 0.12
        L.new(sep.outputs["Z"], hz.inputs[0])
        sm = N.new("ShaderNodeMath")
        sm.operation = "MULTIPLY"
        L.new(st.outputs[0], sm.inputs[0])
        L.new(hz.outputs[0], sm.inputs[1])
        sm2 = N.new("ShaderNodeMath")
        sm2.operation = "MULTIPLY"
        sm2.inputs[1].default_value = stars
        L.new(sm.outputs[0], sm2.inputs[0])
        addc = N.new("ShaderNodeVectorMath")
        addc.operation = "ADD"
        L.new(ramp.outputs["Color"], addc.inputs[0])
        L.new(sm2.outputs[0], addc.inputs[1])
        sky_col = addc.outputs[0]
    bg_sky = N.new("ShaderNodeBackground")
    L.new(sky_col, bg_sky.inputs["Color"])
    bg_amb = N.new("ShaderNodeBackground")
    bg_amb.inputs["Color"].default_value = (*(lin(ambient_col) if ambient_col else (1, 1, 1)), 1)
    bg_amb.inputs["Strength"].default_value = ambient
    lp = N.new("ShaderNodeLightPath")
    mix = N.new("ShaderNodeMixShader")
    L.new(lp.outputs["Is Camera Ray"], mix.inputs["Fac"])
    L.new(bg_amb.outputs[0], mix.inputs[1])
    L.new(bg_sky.outputs[0], mix.inputs[2])
    out = N.new("ShaderNodeOutputWorld")
    L.new(mix.outputs[0], out.inputs["Surface"])


def sun(rot=(50, 0, 30), strength=1.0, color="#FFFFFF", angle=1.5, name="Sun", shadow=True):
    ld = bpy.data.lights.new(name, "SUN")
    ld.energy = strength
    ld.color = lin(color) if isinstance(color, str) else color
    ld.angle = math.radians(angle)
    ld.use_shadow = shadow
    o = bpy.data.objects.new(name, ld)
    bpy.context.scene.collection.objects.link(o)
    o.rotation_euler = tuple(math.radians(a) for a in rot)
    return o


def point_light(loc, power=200, color="#FFFFFF", radius=0.2, name="Point", shadow=True):
    ld = bpy.data.lights.new(name, "POINT")
    ld.energy = power
    ld.color = lin(color)
    ld.shadow_soft_size = radius
    ld.use_shadow = shadow
    o = bpy.data.objects.new(name, ld)
    bpy.context.scene.collection.objects.link(o)
    o.location = Vector(loc)
    return o


def spot_light(loc, target, power=800, color="#FFFFFF", size=35, blend=0.1, name="Spot"):
    ld = bpy.data.lights.new(name, "SPOT")
    ld.energy = power
    ld.color = lin(color)
    ld.spot_size = math.radians(size)
    ld.spot_blend = blend
    ld.shadow_soft_size = 0.1
    o = bpy.data.objects.new(name, ld)
    bpy.context.scene.collection.objects.link(o)
    o.location = Vector(loc)
    o.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    return o


def camera(loc, target, lens=35.0, ortho=None, roll=0.0, shift=(0.0, 0.0), clip=(0.05, 600)):
    sc = bpy.context.scene
    cd = bpy.data.cameras.new("Cam")
    if ortho:
        cd.type = "ORTHO"
        cd.ortho_scale = ortho
    cd.lens = lens
    cd.sensor_width = 36
    cd.clip_start, cd.clip_end = clip
    cd.shift_x, cd.shift_y = shift
    o = bpy.data.objects.new("Cam", cd)
    sc.collection.objects.link(o)
    d = Vector(target) - Vector(loc)
    q = d.to_track_quat("-Z", "Y")
    if roll:
        q = q @ Quaternion((0, 0, 1), math.radians(roll))
    o.location = Vector(loc)
    o.rotation_euler = q.to_euler()
    sc.camera = o
    return o


def frame(objs, direction, lens=50.0, margin=1.1, aim=None, shift=(0.0, 0.0)):
    """Cámara en perspectiva que encuadra 'objs' mirando desde 'direction'."""
    lo, hi = gs._world_bbox(objs)
    ctr = (lo + hi) / 2 if aim is None else Vector(aim)
    d = Vector(direction).normalized()
    m = (-d).to_track_quat("-Z", "Y").to_matrix()
    right, up = m.col[0], m.col[1]
    corners = [Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]
    er = max(abs((c - ctr).dot(right)) for c in corners)
    eu = max(abs((c - ctr).dot(up)) for c in corners)
    ed = max((c - ctr).dot(d) for c in corners)
    sc = bpy.context.scene
    ar = sc.render.resolution_x / sc.render.resolution_y
    th = 18.0 / lens  # tan(fov/2) del lado mayor
    tw, tv = (th, th / ar) if ar >= 1 else (th * ar, th)
    dist = max(er / tw, eu / tv) * margin + ed
    return camera(ctr + d * dist, ctr, lens, shift=shift)


def render(path, outline=True, px=2.6, outline_col="#1C1A26"):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    toonify_all()
    if outline:
        add_outlines(px, outline_col)
    sc = bpy.context.scene
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[gs] render {path}")
    return path


# ---------------------------------------------------------------------------
# Piezas de escena
# ---------------------------------------------------------------------------
def _mesh_obj(name, bm, mats, no_outline=False):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    for m in (mats if isinstance(mats, (list, tuple)) else [mats]):
        me.materials.append(m)
    bpy.context.scene.collection.objects.link(o)
    if no_outline:
        o["gs_no_outline"] = True
    return o


def water(size=400, deep="#1F7FB8", light="#3FB4E0", foam="#E8FBFF", z=0.0, scale=0.08, waves=0.0,
          loc=(0, 0, 0), foam_amt=0.06, name="Water", res=120, emission=0.0):
    """Plano de agua con manchas de color y líneas de espuma (dibujo animado). Recibe sombras."""
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=res, y_segments=res, size=size / 2)
    rnd = random.Random(7)
    ph = [rnd.uniform(0, 6.28) for _ in range(4)]
    for v in bm.verts:
        x, y = v.co.x, v.co.y
        v.co.z = z + waves * (math.sin(x * 0.35 + ph[0]) * 0.6 + math.sin(y * 0.27 + x * 0.1 + ph[1]) * 0.4)
    o = _mesh_obj(name, bm, [], no_outline=True)
    o.location = Vector(loc)
    m = bpy.data.materials.new(name + "_Mat")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    N, L = nt.nodes, nt.links
    tc = N.new("ShaderNodeTexCoord")
    mp = N.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (scale, scale * 1.6, scale)
    L.new(tc.outputs["Object"], mp.inputs["Vector"])
    noise = N.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 1.0
    noise.inputs["Detail"].default_value = 1.0
    L.new(mp.outputs[0], noise.inputs["Vector"])
    ramp = N.new("ShaderNodeValToRGB")
    cr = ramp.color_ramp
    cr.interpolation = "CONSTANT"
    cr.elements[0].color = (*lin(deep), 1)
    cr.elements[1].position = 0.5
    cr.elements[1].color = (*lin(light), 1)
    e = cr.elements.new(0.5 - foam_amt * 0.5)
    e.color = (*lin(foam), 1)
    e2 = cr.elements.new(0.5 + foam_amt * 0.5)
    e2.color = (*lin(light), 1)
    cr.elements[1].position = 0.5 - foam_amt * 0.5 - 0.0001
    cr.elements[1].color = (*lin(deep), 1)
    L.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    g = N.new("ShaderNodeGroup")
    g.node_tree = toon_group()
    g.inputs["Emission"].default_value = emission
    L.new(ramp.outputs["Color"], g.inputs["Color"])
    out = N.new("ShaderNodeOutputMaterial")
    L.new(g.outputs[0], out.inputs["Surface"])
    m["gs_toon"] = True
    o.data.materials.append(m)
    return o


def ground(verts_fn, size, res, mat, name="Ground", loc=(0, 0, 0), no_outline=True):
    """Terreno: rejilla con altura verts_fn(x, y) -> z."""
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=res, y_segments=res, size=size / 2)
    for v in bm.verts:
        v.co.z = verts_fn(v.co.x, v.co.y)
    for f in bm.faces:
        f.smooth = True
    o = _mesh_obj(name, bm, mat if isinstance(mat, (list, tuple)) else [mat], no_outline)
    o.location = Vector(loc)
    return o


def blob(name, loc, radii, mat, seed=0, rough=0.18, seg=24, rings=14, flat_bottom=None, rot=0.0):
    """Masa orgánica (isla, roca, nube): elipsoide con ruido suave."""
    rnd = random.Random(seed)
    ph = [rnd.uniform(0, 6.28) for _ in range(6)]
    o = gs.ellipsoid(name, (0, 0, 0), radii, mat, seg=seg, rings=rings)
    for v in o.data.vertices:
        n = v.co.normalized()
        k = 1 + rough * (math.sin(n.x * 3.1 + ph[0]) * math.sin(n.y * 2.7 + ph[1]) * 0.6
                         + math.sin(n.z * 4.3 + n.x * 2 + ph[2]) * 0.4 + math.sin(n.y * 6 + ph[3]) * 0.25)
        v.co *= k
        if flat_bottom is not None and v.co.z < flat_bottom:
            v.co.z = flat_bottom
    o.location = Vector(loc)
    o.rotation_euler = (0, 0, math.radians(rot))
    o["bone"] = "auto"
    return o


def gas_cloud(loc, size=1.0, color=GAS_GREEN, n=7, seed=1, name="Gas", stretch=(1, 1, 1), shine=True):
    """Nube de pedo: bolas verdes con brillo."""
    rnd = random.Random(seed)
    m = toon_mat(color, "T_Gas", emission=0.25)
    ms = toon_mat("#E9FFD0", "T_GasShine", emission=0.6)
    out = []
    c = Vector(loc)
    for i in range(n):
        off = Vector((rnd.uniform(-1, 1) * stretch[0], rnd.uniform(-1, 1) * stretch[1],
                      rnd.uniform(-0.6, 1) * stretch[2])) * size * 0.55
        r = size * rnd.uniform(0.35, 0.6)
        p = c + off
        out.append(gs.ellipsoid(f"{name}{i}", p, r, m, seg=14, rings=8))
        if shine and i % 2 == 0:
            o = gs.ellipsoid(f"{name}S{i}", p + Vector((-0.35, -0.5, 0.45)) * r, r * 0.16, ms, seg=8, rings=5)
            o["gs_no_outline"] = True
            out.append(o)
    return out


def bubble(loc, r, color="#C8F7A0", name="Bubble"):
    m = toon_mat(color, "T_Bubble", emission=0.35)
    ms = toon_mat("#FFFFFF", "T_BubbleShine", emission=1.0)
    b = gs.ellipsoid(name, loc, r, m, seg=16, rings=10)
    s = gs.ellipsoid(name + "S", Vector(loc) + Vector((-0.4 * r, -0.75 * r, 0.4 * r)), (r * 0.28, r * 0.1, r * 0.2),
                     ms, seg=8, rings=5, rot=("Y", -30))
    s["gs_no_outline"] = True
    return [b, s]


def splash(loc, size=1.0, seed=3, color="#E8FBFF", n=9):
    rnd = random.Random(seed)
    m = toon_mat(color, "T_Splash", emission=0.2)
    out = []
    for i in range(n):
        a = i / n * 2 * math.pi + rnd.uniform(-0.2, 0.2)
        h = rnd.uniform(0.5, 1.2) * size
        p0 = Vector(loc) + Vector((math.cos(a), math.sin(a), 0)) * size * 0.5
        p1 = p0 + Vector((math.cos(a) * 0.5, math.sin(a) * 0.5, h))
        out.append(gs.tube(f"Splash{i}", [p0, (p0 + p1) / 2 + Vector((0, 0, 0.15 * size)), p1],
                           [0.12 * size, 0.1 * size, 0.05 * size], m, seg=8, cap=3, sub=2))
        out.append(gs.ellipsoid(f"Drop{i}", p1 + Vector((math.cos(a), math.sin(a), 0.3)) * 0.3 * size,
                                0.07 * size, m, seg=8, rings=5))
    return out


def speed_lines(cam, n=26, seed=5, color="#FFFFFF", depth=6.0, inner=0.42, length=0.5, width=0.012):
    """Líneas de velocidad radiales en el borde del cuadro (placas delante de la cámara)."""
    rnd = random.Random(seed)
    m = toon_mat(color, "T_Speed", emission=1.0)
    sc = bpy.context.scene
    ar = sc.render.resolution_x / sc.render.resolution_y
    hh = math.tan(cam.data.angle / 2) * depth / max(ar, 1)  # media altura del cuadro a 'depth'
    hw = hh * ar
    out = []
    for i in range(n):
        a = rnd.uniform(0, 2 * math.pi)
        r0 = rnd.uniform(inner, inner + 0.2)
        r1 = r0 + rnd.uniform(length * 0.6, length)
        w = width * rnd.uniform(0.6, 1.4)
        d = Vector((math.cos(a) * hw * 2, math.sin(a) * hh * 2, 0))
        p0, p1 = d * r0 * 0.5, d * r1 * 0.5
        side = Vector((-math.sin(a), math.cos(a), 0)) * w * hh * 2
        bm = bmesh.new()
        vs = [bm.verts.new(p0 - side * 0.1), bm.verts.new(p1 - side), bm.verts.new(p1 + side),
              bm.verts.new(p0 + side * 0.1)]
        bm.faces.new(vs)
        o = _mesh_obj(f"Speed{i}", bm, [m], no_outline=True)
        o.parent = cam
        o.location = (0, 0, -depth)
        out.append(o)
    return out


def flat_disc(name, loc, radius, hexcol, normal=(0, -1, 0), seg=64, emission=1.0, ring=None):
    """Disco plano de color fijo (fondo de retrato). ring=(ancho, color) añade un aro."""
    m = bpy.data.materials.new(name + "_Mat")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    e = nt.nodes.new("ShaderNodeEmission")
    e.inputs["Color"].default_value = (*lin(hexcol), 1)
    e.inputs["Strength"].default_value = emission
    o2 = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(e.outputs[0], o2.inputs["Surface"])
    m["gs_toon"] = True
    bm = bmesh.new()
    bmesh.ops.create_circle(bm, cap_ends=True, radius=radius, segments=seg)
    o = _mesh_obj(name, bm, [m], no_outline=True)
    o.location = Vector(loc)
    o.rotation_euler = Vector(normal).to_track_quat("Z", "Y").to_euler()
    out = [o]
    if ring:
        w, rc = ring
        r = flat_disc(name + "_Ring", Vector(loc) - Vector(normal).normalized() * 0.002, radius + w, rc,
                      normal, seg, emission)
        out += r
    return out


def disc_gradient(name, loc, radius, inner, outer, normal=(0, -1, 0), seg=64):
    """Disco con degradado radial (fondo de retrato)."""
    m = bpy.data.materials.new(name + "_Mat")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    N, L = nt.nodes, nt.links
    tc = N.new("ShaderNodeTexCoord")
    gr = N.new("ShaderNodeTexGradient")
    gr.gradient_type = "SPHERICAL"
    mp = N.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1 / radius, 1 / radius, 1 / radius)
    L.new(tc.outputs["Object"], mp.inputs["Vector"])
    L.new(mp.outputs[0], gr.inputs["Vector"])
    ramp = N.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (*lin(outer), 1)
    ramp.color_ramp.elements[1].color = (*lin(inner), 1)
    ramp.color_ramp.elements[1].position = 0.85
    L.new(gr.outputs["Fac"], ramp.inputs["Fac"])
    e = N.new("ShaderNodeEmission")
    L.new(ramp.outputs["Color"], e.inputs["Color"])
    o2 = N.new("ShaderNodeOutputMaterial")
    L.new(e.outputs[0], o2.inputs["Surface"])
    m["gs_toon"] = True
    bm = bmesh.new()
    bmesh.ops.create_circle(bm, cap_ends=True, radius=radius, segments=seg)
    o = _mesh_obj(name, bm, [m], no_outline=True)
    o.location = Vector(loc)
    o.rotation_euler = Vector(normal).to_track_quat("Z", "Y").to_euler()
    return o


def backdrop(cam, inner, outer, depth=80.0, rays=0, ray_col=None, seed=2, center=(0.0, 0.0)):
    """Fondo plano de cómic detrás de todo: degradado radial y rayos opcionales (sunburst)."""
    sc = bpy.context.scene
    ar = sc.render.resolution_x / sc.render.resolution_y
    hh = math.tan(cam.data.angle / 2) * depth / max(ar, 1)
    R = hh * ar * 1.6
    o = disc_gradient("Backdrop", (0, 0, 0), R, inner, outer, normal=(0, 0, 1))
    o.parent = cam
    o.location = (center[0] * hh * ar, center[1] * hh, -depth)
    o.rotation_euler = (0, 0, 0)
    out = [o]
    if rays:
        m = bpy.data.materials.new("Rays_Mat")
        m.use_nodes = True
        nt = m.node_tree
        nt.nodes.clear()
        e = nt.nodes.new("ShaderNodeEmission")
        e.inputs["Color"].default_value = (*lin(ray_col or inner), 1)
        o2 = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(e.outputs[0], o2.inputs["Surface"])
        m["gs_toon"] = True
        bm = bmesh.new()
        c = bm.verts.new((0, 0, 0))
        for i in range(rays):
            a0 = (i + 0.0) / rays * 2 * math.pi
            a1 = (i + 0.5) / rays * 2 * math.pi
            v0 = bm.verts.new((math.cos(a0) * R, math.sin(a0) * R, 0))
            v1 = bm.verts.new((math.cos(a1) * R, math.sin(a1) * R, 0))
            bm.faces.new([c, v0, v1])
        r = _mesh_obj("Rays", bm, [m], no_outline=True)
        r.parent = cam
        r.location = (center[0] * hh * ar, center[1] * hh, -depth + 0.5)
        out.append(r)
    return out


def palm(loc, h=6.0, seed=0, lean=12, trunk="#9C6B3E", leaf="#3FA34D", leaf2="#2D7A3A", rot=0.0):
    """Palmera sencilla (tronco curvo + hojas)."""
    rnd = random.Random(seed)
    tm, lm, lm2 = toon_mat(trunk), toon_mat(leaf), toon_mat(leaf2)
    a = math.radians(rot)
    dirv = Vector((math.cos(a), math.sin(a), 0))
    pts = [Vector(loc) + dirv * (math.sin(t * math.radians(lean)) * h * t * 0.6) + Vector((0, 0, h * t))
           for t in (0, 0.33, 0.66, 1.0)]
    out = [gs.tube("PalmTrunk", pts, [0.28 * h / 6, 0.22 * h / 6, 0.19 * h / 6, 0.17 * h / 6], tm, seg=8, sub=3)]
    top = pts[-1]
    for i in range(7):
        b = i / 7 * 2 * math.pi + rnd.uniform(-0.2, 0.2)
        d = Vector((math.cos(b), math.sin(b), 0))
        L = h * rnd.uniform(0.38, 0.5)
        fp = [top, top + d * L * 0.45 + Vector((0, 0, L * 0.18)), top + d * L * 0.85 + Vector((0, 0, -L * 0.05)),
              top + d * L + Vector((0, 0, -L * 0.3))]
        out.append(gs.tube(f"Frond{i}", fp, [0.06 * h / 6, 0.32 * h / 6, 0.22 * h / 6, 0.02], lm if i % 2 else lm2,
                           seg=6, sub=3, flatten=((0, 0, 1), 0.25)))
    for k in range(3):
        out.append(gs.ellipsoid(f"Coco{k}", top + Vector((math.cos(k * 2.1), math.sin(k * 2.1), -0.6)) * 0.25 * h / 6,
                                0.16 * h / 6, toon_mat("#6B4226"), seg=8, rings=5))
    return out


def tree_blob(loc, h=4.0, seed=0, trunk="#7A5232", leaf="#3E9B45", leaf2="#58B85A"):
    rnd = random.Random(seed)
    out = [gs.capsule("Trunk", loc, Vector(loc) + Vector((0, 0, h * 0.6)), 0.12 * h / 4, toon_mat(trunk), sub=1,
                      seg=8)]
    for k in range(3):
        c = Vector(loc) + Vector((rnd.uniform(-0.5, 0.5), rnd.uniform(-0.5, 0.5), h * rnd.uniform(0.65, 0.95)))
        out.append(blob(f"Leaves{k}", c, (h * 0.32,) * 3, toon_mat(leaf if k % 2 else leaf2), seed=seed + k,
                        rough=0.12, seg=14, rings=8))
    return out


def cloud(loc, size=6.0, seed=0, color="#FFFFFF", shade="#D6E4F5", n=5, flat=0.55):
    rnd = random.Random(seed)
    out = []
    for i in range(n):
        p = Vector(loc) + Vector((rnd.uniform(-1, 1) * size * 0.6, rnd.uniform(-0.3, 0.3) * size * 0.4,
                                  rnd.uniform(-0.1, 0.4) * size * 0.3))
        r = size * rnd.uniform(0.28, 0.42)
        o = gs.ellipsoid(f"Cloud{i}", p, (r, r * 0.8, r * flat), toon_mat(color if i % 3 else shade, emission=0.15),
                         seg=14, rings=8)
        out.append(o)
    return out


def moon(loc, r=4.0, color="#FFF4C2"):
    m = toon_mat(color, "T_Moon", emission=1.6)
    o = gs.ellipsoid("Moon", loc, r, m, seg=24, rings=14)
    o["gs_no_outline"] = True
    for i, (dx, dz, rr) in enumerate(((-0.3, 0.2, 0.2), (0.25, -0.25, 0.14), (0.1, 0.35, 0.09))):
        c = gs.ellipsoid(f"Crater{i}", Vector(loc) + Vector((dx * r, -0.9 * r, dz * r)), (rr * r, 0.08 * r, rr * r),
                         toon_mat("#EADB9E", "T_Crater", emission=1.2), seg=12, rings=6)
        c["gs_no_outline"] = True
    return o


# ---------------------------------------------------------------------------
# Hojas de contacto y posproceso
# ---------------------------------------------------------------------------
def _load_px(path):
    import numpy as np
    im = bpy.data.images.load(path)
    w, h = im.size
    a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(im)
    return a


def _resize(a, w, h):
    import numpy as np
    H, W = a.shape[:2]
    ys = (np.arange(h) * H / h).astype(int)
    xs = (np.arange(w) * W / w).astype(int)
    return a[ys][:, xs]


def save_px(a, path):
    h, w = a.shape[:2]
    img = bpy.data.images.new("px", w, h, alpha=True)
    img.pixels = a.ravel()
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def contact_sheet(paths, out, cols=4, cell=(480, 270)):
    """Hoja de contacto: cada imagen escalada a 'cell' sobre un damero (muestra la transparencia)."""
    import numpy as np
    w, h = cell
    rows = (len(paths) + cols - 1) // cols
    big = np.zeros((rows * (h + 6) + 6, cols * (w + 6) + 6, 4), dtype=np.float32)
    big[..., :3] = 0.12
    big[..., 3] = 1
    yy, xx = np.mgrid[0:h, 0:w]
    chk = (((yy // 16) + (xx // 16)) % 2)[..., None] * 0.12 + 0.38
    for i, p in enumerate(paths):
        a = _load_px(p)
        H, W = a.shape[:2]
        k = min(w / W, h / H)
        tw, th = max(1, int(W * k)), max(1, int(H * k))
        a = _resize(a, tw, th)
        r, c = divmod(i, cols)
        tile = np.repeat(chk, 3, axis=2).copy()
        oy, ox = (h - th) // 2, (w - tw) // 2
        al = a[..., 3:4]
        tile[oy:oy + th, ox:ox + tw] = a[..., :3] * al + tile[oy:oy + th, ox:ox + tw] * (1 - al)
        y0 = big.shape[0] - (r + 1) * (h + 6)
        x0 = 6 + c * (w + 6)
        big[y0:y0 + h, x0:x0 + w, :3] = tile
    save_px(big, out)
    print(f"[gs] hoja de contacto {out}")
    return out


def comic_frame(path, border=14, color=(0.07, 0.06, 0.1)):
    """Marco de viñeta: borde oscuro alrededor de la imagen (opaca)."""
    a = _load_px(path)
    a[:border, :, :3] = color
    a[-border:, :, :3] = color
    a[:, :border, :3] = color
    a[:, -border:, :3] = color
    a[..., 3] = 1
    save_px(a, path)


def vignette(path, strength=0.35, color=(0.05, 0.03, 0.12)):
    import numpy as np
    a = _load_px(path)
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2) / 1.41
    k = np.clip((d - 0.55) / 0.45, 0, 1)[..., None] ** 1.6 * strength
    a[..., :3] = a[..., :3] * (1 - k) + np.array(color) * k
    save_px(a, path)


def args():
    return [a for a in (sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])]
