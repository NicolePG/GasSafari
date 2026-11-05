"""GAS SAFARI · utilidades comunes para generar modelos en Blender 4.5 (modo --background).

Paleta, materiales de color plano, primitivas redondeadas (tubos/cápsulas, elipsoides, tornos),
esqueleto compatible con Unity Humanoid, animaciones por script, exportación FBX y vistas previas.

Convenciones: metros, Z arriba, el personaje mira hacia -Y (vista frontal de Blender),
su izquierda es +X. Origen en los pies.
"""
import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector, Matrix, Quaternion

TOOLS_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PREVIEW_DIR = os.path.join(TOOLS_DIR, "Blender", "previews")
EXPORT_DIR = os.path.join(TOOLS_DIR, "Export")
FPS = 30

# ---------------------------------------------------------------------------
# Paleta (sRGB hex). Colores saturados de juguete.
# ---------------------------------------------------------------------------
PALETTE = {
    "white": "#F7F4EC", "black": "#1E1E24", "pupil": "#1A1A22", "eye_white": "#FFFFFF",
    "mouth": "#5A1E2A", "tongue": "#E86A7A", "blush": "#F29BA6", "nose_dark": "#3A2A2A",
    # Alex
    "lion_fur": "#E8A33A", "lion_cream": "#F8DFA8", "lion_mane": "#B8561E", "lion_mane2": "#D2722A",
    # Marty
    "zebra_white": "#F4F4F0", "zebra_black": "#26262E", "zebra_muzzle": "#5E5E6A",
    # Melman
    "giraffe_yellow": "#F2C94C", "giraffe_patch": "#B5652B", "giraffe_cream": "#FBE7B0", "ossicone": "#8A4A22",
    # Gloria
    "hippo_skin": "#8F8BB8", "hippo_light": "#C9C2E6", "hippo_dark": "#6B6796", "hippo_pink": "#F0A3B8",
    # Julien
    "lemur_grey": "#8E9099", "lemur_light": "#E7E3DA", "lemur_dark": "#2C2C33", "lemur_eye": "#FFB21E",
    "gold": "#F5C431", "gold_dark": "#C8931C", "gem_red": "#E2394B", "gem_green": "#3DBA5A", "leaf": "#4FB346",
    # Props
    "raft_orange": "#FF7A1A", "raft_dark": "#C9520A", "rope": "#D9B77A", "metal": "#B9C2CC",
    "hoof": "#3B3330",
}


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_rgba(h):
    h = h.lstrip("#")
    rgb = [srgb_to_linear(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4)]
    return (*rgb, 1.0)


_MATS = {}


def mat(key, prefix="M", rough=0.65, emission=0.0):
    """Material de color plano (Principled BSDF sin texturas). key = clave de PALETTE o '#RRGGBB'."""
    hexcol = PALETTE.get(key, key)
    name = f"{prefix}_{key}" if not key.startswith("#") else f"{prefix}_{key[1:]}"
    if name in _MATS:
        return _MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    col = hex_rgba(hexcol)
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = col
    bsdf.inputs["Roughness"].default_value = rough
    if emission > 0:
        bsdf.inputs["Emission Color"].default_value = col
        bsdf.inputs["Emission Strength"].default_value = emission
    m.diffuse_color = col
    m.roughness = rough
    _MATS[name] = m
    return m


# ---------------------------------------------------------------------------
# Escena
# ---------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _MATS.clear()
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.unit_settings.system = "METRIC"
    sc.unit_settings.scale_length = 1.0
    return sc


def _link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _obj_from_bm(name, bm, mats, bone=None, smooth=True, sharp_angle=55.0):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        f.smooth = smooth
    if smooth and sharp_angle:
        lim = math.radians(sharp_angle)
        bm.normal_update()
        for e in bm.edges:
            if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > lim:
                e.smooth = False
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    for m in (mats if isinstance(mats, (list, tuple)) else [mats]):
        me.materials.append(m)
    obj["bone"] = bone or "auto"
    return _link(obj)


def _align_matrix(direction):
    """Matriz de rotación que lleva +Z local a 'direction'."""
    d = Vector(direction).normalized()
    return d.to_track_quat("Z", "Y").to_matrix().to_4x4()


# ---------------------------------------------------------------------------
# Primitivas
# ---------------------------------------------------------------------------
def _seg_for(r):
    """Resolución según el tamaño (presupuesto de triángulos)."""
    if r >= 0.15:
        return 22, 12
    if r >= 0.07:
        return 16, 9
    if r >= 0.035:
        return 12, 7
    return 8, 5


def ellipsoid(name, loc, radii, material, bone=None, seg=None, rings=None, rot=None):
    """Elipsoide. radii=(rx, ry, rz) o un escalar. rot=Matrix 3x3/4x4 opcional o (eje, grados)."""
    if not isinstance(radii, (tuple, list, Vector)):
        radii = (radii, radii, radii)
    ds, dr = _seg_for(max(radii))
    seg, rings = seg or ds, rings or dr
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    m = Matrix.Diagonal((*radii, 1.0))
    if rot is not None:
        m = _rot_matrix(rot) @ m
    m = Matrix.Translation(Vector(loc)) @ m
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    return _obj_from_bm(name, bm, material, bone)


def _rot_matrix(rot):
    if isinstance(rot, Matrix):
        return rot.to_4x4()
    if isinstance(rot, Quaternion):
        return rot.to_matrix().to_4x4()
    # lista de (eje, grados)
    if isinstance(rot[0], str):
        rot = [rot]
    q = Quaternion()
    for axis, deg in rot:
        q = Quaternion(_AXES[axis], math.radians(deg)) @ q
    return q.to_matrix().to_4x4()


_AXES = {"X": Vector((1, 0, 0)), "Y": Vector((0, 1, 0)), "Z": Vector((0, 0, 1))}


def _catmull(pts, sub):
    if len(pts) == 2 or sub <= 1:
        out = []
        for i in range(len(pts) - 1):
            for k in range(max(sub, 1)):
                out.append(pts[i].lerp(pts[i + 1], k / max(sub, 1)))
        out.append(pts[-1])
        return out
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for k in range(sub):
            t = k / sub
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(pts[-1])
    return out


def tube(name, pts, radii, materials, bone=None, seg=None, cap=4, sub=1, mat_fn=None,
         cap_start=True, cap_end=True, flatten=None):
    """Tubo redondeado a lo largo de una polilínea suave con tapas semiesféricas (cápsula si son 2 puntos).
    radii: escalar o lista por punto. mat_fn(t) -> índice de material según la fracción de longitud t.
    flatten: (vector, factor) aplana la sección en esa dirección (p. ej. cejas)."""
    pts = [Vector(p) for p in pts]
    if not isinstance(radii, (list, tuple)):
        radii = [radii] * len(pts)
    if seg is None:
        seg = 12 if max(radii) >= 0.07 else (10 if max(radii) >= 0.03 else 8)
    path = _catmull(pts, sub)
    # radio interpolado por tramo
    rs = []
    n_seg = len(pts) - 1
    for i in range(len(path)):
        u = i / (len(path) - 1) * n_seg
        j = min(int(u), n_seg - 1)
        rs.append(radii[j] + (radii[j + 1] - radii[j]) * (u - j))
    # marcos por transporte paralelo
    tans = []
    for i in range(len(path)):
        a = path[max(i - 1, 0)]
        b = path[min(i + 1, len(path) - 1)]
        tans.append((b - a).normalized())
    ref = Vector((0, 0, 1)) if abs(tans[0].z) < 0.9 else Vector((1, 0, 0))
    n = (ref - tans[0] * ref.dot(tans[0])).normalized()
    frames = []
    for t in tans:
        n = (n - t * n.dot(t)).normalized()
        frames.append((t, n, t.cross(n)))
    rings = []  # (centro, radio, T, N, B)
    if cap_start:
        t, nn, bb = frames[0]
        for k in range(cap, 0, -1):
            phi = k / cap * math.pi / 2
            rings.append((path[0] - t * rs[0] * math.sin(phi), rs[0] * math.cos(phi), t, nn, bb))
    for i, p in enumerate(path):
        rings.append((p, rs[i], *frames[i]))
    if cap_end:
        t, nn, bb = frames[-1]
        for k in range(1, cap + 1):
            phi = k / cap * math.pi / 2
            rings.append((path[-1] + t * rs[-1] * math.sin(phi), rs[-1] * math.cos(phi), t, nn, bb))
    # longitud acumulada para mat_fn
    lens = [0.0]
    for i in range(1, len(rings)):
        lens.append(lens[-1] + (rings[i][0] - rings[i - 1][0]).length)
    total = lens[-1] or 1.0
    bm = bmesh.new()
    vrings = []
    for (c, r, t, nn, bb) in rings:
        ring = []
        if r < 1e-5:
            ring = [bm.verts.new(c)] * seg
        else:
            for s in range(seg):
                a = s / seg * 2 * math.pi
                off = nn * math.cos(a) * r + bb * math.sin(a) * r
                if flatten is not None:
                    fv, ff = Vector(flatten[0]).normalized(), flatten[1]
                    off = off - fv * off.dot(fv) * (1 - ff)
                ring.append(bm.verts.new(c + off))
        vrings.append(ring)
    for i in range(len(vrings) - 1):
        a, b = vrings[i], vrings[i + 1]
        mi = mat_fn((lens[i] + lens[i + 1]) * 0.5 / total) if mat_fn else 0
        for s in range(seg):
            q = [a[s], a[(s + 1) % seg], b[(s + 1) % seg], b[s]]
            uniq = list(dict.fromkeys(q))
            if len(uniq) >= 3:
                try:
                    f = bm.faces.new(uniq)
                    f.material_index = mi
                except ValueError:
                    pass
    # tapas planas si no hay semiesfera
    if not cap_start:
        f = bm.faces.new(list(dict.fromkeys(vrings[0])))
        f.material_index = mat_fn(0.0) if mat_fn else 0
    if not cap_end:
        f = bm.faces.new(list(dict.fromkeys(vrings[-1])))
        f.material_index = mat_fn(1.0) if mat_fn else 0
    return _obj_from_bm(name, bm, materials, bone)


def capsule(name, p0, p1, r0, material, r1=None, bone=None, seg=None, cap=4, sub=3, mat_fn=None):
    p0, p1 = Vector(p0), Vector(p1)
    pts = [p0.lerp(p1, i / sub) for i in range(sub + 1)]
    r1 = r0 if r1 is None else r1
    radii = [r0 + (r1 - r0) * i / sub for i in range(sub + 1)]
    return tube(name, pts, radii, material, bone=bone, seg=seg, cap=cap, mat_fn=mat_fn)


def lathe(name, profile, materials, loc=(0, 0, 0), axis=(0, 0, 1), seg=24, bone=None, mat_fn=None,
          rot=None, scale=None):
    """Sólido de revolución. profile: [(radio, altura), ...] de abajo arriba; radio 0 = polo.
    mat_fn(i) -> índice de material por tramo del perfil."""
    bm = bmesh.new()
    rings = []
    for (r, z) in profile:
        if r < 1e-6:
            rings.append([bm.verts.new((0, 0, z))] * seg)
        else:
            rings.append([bm.verts.new((r * math.cos(s / seg * 2 * math.pi),
                                        r * math.sin(s / seg * 2 * math.pi), z)) for s in range(seg)])
    for i in range(len(rings) - 1):
        a, b = rings[i], rings[i + 1]
        mi = mat_fn(i) if mat_fn else 0
        for s in range(seg):
            q = list(dict.fromkeys([a[s], a[(s + 1) % seg], b[(s + 1) % seg], b[s]]))
            if len(q) >= 3:
                bm.faces.new(q).material_index = mi
    if profile[0][0] > 1e-6:
        bm.faces.new(rings[0]).material_index = mat_fn(0) if mat_fn else 0
    if profile[-1][0] > 1e-6:
        bm.faces.new(rings[-1]).material_index = mat_fn(len(rings) - 2) if mat_fn else 0
    m = _align_matrix(axis)
    if scale is not None:
        m = m @ Matrix.Diagonal((*scale, 1.0))
    if rot is not None:
        m = _rot_matrix(rot) @ m
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(loc)) @ m, verts=bm.verts)
    return _obj_from_bm(name, bm, materials, bone)


def rounded_cylinder(name, loc, radius, height, bevel, material, axis=(0, 0, 1), seg=24, bone=None,
                     steps=3, mat_fn=None, rot=None, scale=None):
    """Cilindro con borde redondeado (bevel), centrado en loc."""
    prof = [(0.0, -height / 2)]
    for k in range(steps + 1):
        a = -math.pi / 2 + k / steps * math.pi / 2
        prof.append((radius - bevel + bevel * math.cos(a), -height / 2 + bevel + bevel * math.sin(a)))
    for k in range(steps + 1):
        a = k / steps * math.pi / 2
        prof.append((radius - bevel + bevel * math.cos(a), height / 2 - bevel + bevel * math.sin(a)))
    prof.append((0.0, height / 2))
    return lathe(name, prof, material, loc=loc, axis=axis, seg=seg, bone=bone, mat_fn=mat_fn, rot=rot,
                 scale=scale)


def torus(name, loc, major, minor, material, axis=(0, 0, 1), seg=32, mseg=12, bone=None, scale=None,
          arc=None):
    """Toro. arc=(desde_grados, hasta_grados) para un toro parcial (con tapas redondeadas)."""
    bm = bmesh.new()
    a0, a1 = (0, 360) if arc is None else arc
    closed = arc is None
    n = seg if closed else seg + 1
    rings = []
    for i in range(n):
        u = math.radians(a0 + (a1 - a0) * i / seg)
        c = Vector((major * math.cos(u), major * math.sin(u), 0))
        out = Vector((math.cos(u), math.sin(u), 0))
        rings.append([bm.verts.new(c + out * minor * math.cos(v / mseg * 2 * math.pi)
                                   + Vector((0, 0, 1)) * minor * math.sin(v / mseg * 2 * math.pi))
                      for v in range(mseg)])
    for i in range(n if closed else n - 1):
        a, b = rings[i], rings[(i + 1) % n]
        for v in range(mseg):
            bm.faces.new([a[v], a[(v + 1) % mseg], b[(v + 1) % mseg], b[v]])
    if not closed:
        bm.faces.new(rings[0])
        bm.faces.new(rings[-1])
    m = _align_matrix(axis)
    if scale is not None:
        m = Matrix.Diagonal((*scale, 1.0)) @ m
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(loc)) @ m, verts=bm.verts)
    return _obj_from_bm(name, bm, material, bone)


def cone(name, p0, p1, r0, r1, material, seg=16, bone=None, round_tip=True):
    p0, p1 = Vector(p0), Vector(p1)
    h = (p1 - p0).length
    prof = [(0.0, 0.0), (r0, 0.0)]
    if round_tip:
        prof += [(r1 + (r0 - r1) * 0.25, h * 0.8), (r1, h * 0.95), (0.0, h)]
    else:
        prof += [(r1, h), (0.0, h)]
    return lathe(name, prof, material, loc=p0, axis=p1 - p0, seg=seg, bone=bone)


def arc_points(center, radius, a0, a1, n, plane=("X", "Z"), depth_axis="Y", depth=0.0, bulge=0.0):
    """Puntos en un arco (grados) en el plano dado; bulge empuja el centro del arco hacia -depth_axis."""
    c = Vector(center)
    u, v, w = _AXES[plane[0]], _AXES[plane[1]], _AXES[depth_axis]
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = math.radians(a0 + (a1 - a0) * t)
        p = c + u * radius * math.cos(a) + v * radius * math.sin(a) + w * depth
        p -= w * bulge * math.sin(t * math.pi)
        out.append(p)
    return out


# ---------------------------------------------------------------------------
# Patrones de color sin texturas (índice de material por cara)
# ---------------------------------------------------------------------------
def bands_by_plane(obj, direction, origin, period, duty, mat_index, wobble=0.0, wobble_axis="X",
                   wobble_freq=1.0, cut=True, phase=0.0):
    """Rayas: corta la malla con planos perpendiculares a 'direction' y asigna mat_index
    a las caras dentro de la franja (fracción 'duty' de cada periodo). wobble ondula las rayas."""
    d = Vector(direction).normalized()
    o = Vector(origin)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    wa = _AXES[wobble_axis]

    def coord(p):
        return (p - o).dot(d) + wobble * math.sin((p.dot(wa)) * wobble_freq * 2 * math.pi) + phase * period

    if cut and wobble == 0.0:
        lo = min(coord(v.co) for v in bm.verts)
        hi = max(coord(v.co) for v in bm.verts)
        k0, k1 = math.floor(lo / period), math.ceil(hi / period)
        for k in range(k0, k1 + 1):
            for off in (0.0, duty * period):
                c = k * period + off - phase * period
                geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
                bmesh.ops.bisect_plane(bm, geom=geom, plane_co=o + d * c, plane_no=d)
    for f in bm.faces:
        u = coord(f.calc_center_median()) / period
        if (u - math.floor(u)) < duty:
            f.material_index = mat_index
    bm.to_mesh(obj.data)
    bm.free()


def patches_voronoi(obj, seeds, patch_index, gap, keep_fn=None):
    """Manchas tipo jirafa: cada cara va a la semilla más cercana; si está lejos del borde de la celda
    se pinta con patch_index, y las líneas entre celdas conservan el color base."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    seeds = [Vector(s) for s in seeds]
    for f in bm.faces:
        c = f.calc_center_median()
        if keep_fn and not keep_fn(c):
            continue
        ds = sorted((c - s).length for s in seeds)
        if len(ds) > 1 and ds[1] - ds[0] > gap:
            f.material_index = patch_index
    bm.to_mesh(obj.data)
    bm.free()


def paint_faces(obj, fn, mat_index):
    """Asigna mat_index a las caras cuyo centro cumple fn(centro)."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    for f in bm.faces:
        if fn(f.calc_center_median()):
            f.material_index = mat_index
    bm.to_mesh(obj.data)
    bm.free()


# ---------------------------------------------------------------------------
# Cara: ojos grandes, cejas, boca
# ---------------------------------------------------------------------------
def eye(name, center, radius, look=(0, -1, 0), bone="Head", pupil=0.55, pupil_col="pupil",
        iris_col=None, lid_col=None, lid_angle=None, lid_tilt=0.0, squash=0.85, side=1):
    """Ojo grande de caricatura: globo blanco, iris/pupila, brillo y párpado opcional.
    lid_angle: grados que baja el párpado superior desde arriba (90 = cubre medio ojo)."""
    c = Vector(center)
    lk = Vector(look).normalized()
    rot = lk.to_track_quat("-Y", "Z").to_matrix().to_4x4()
    parts = [ellipsoid(name + "_White", c, (radius, radius * squash, radius * 1.08), mat("eye_white"),
                       bone=bone, seg=18, rings=10, rot=rot)]
    pr = radius * pupil
    front = c + lk * radius * squash * 0.86
    if iris_col:
        parts.append(ellipsoid(name + "_Iris", c + lk * radius * squash * 0.8,
                               (pr * 1.15, pr * 0.35, pr * 1.25), mat(iris_col), bone=bone, seg=16, rings=8,
                               rot=rot))
        front = c + lk * radius * squash * 0.93
        pr *= 0.6
    parts.append(ellipsoid(name + "_Pupil", front, (pr, pr * 0.4, pr * 1.1), mat(pupil_col), bone=bone,
                           seg=16, rings=8, rot=rot))
    up = Vector((0, 0, 1))
    sidev = lk.cross(up).normalized()
    hl = front + lk * pr * 0.35 + up * pr * 0.45 - sidev * pr * 0.35 * side
    parts.append(ellipsoid(name + "_Shine", hl, pr * 0.32, mat("eye_white", rough=0.2), bone=bone,
                           seg=8, rings=6))
    if lid_col is not None and lid_angle is not None:
        # párpado superior: casquete de esfera algo mayor que el globo, rotado hacia abajo
        prof = []
        start = math.radians(90 - lid_angle)
        steps = 8
        for k in range(steps + 1):
            a = start + k / steps * (math.pi / 2 - start)
            prof.append((radius * 1.08 * math.cos(a), radius * 1.08 * math.sin(a)))
        prof[-1] = (0.0, radius * 1.08)
        prof.insert(0, (0.0, prof[0][1]))
        lid = lathe(name + "_Lid", prof, mat(lid_col), loc=c, seg=20, bone=bone,
                    scale=(1.0, squash * 1.06, 1.0))
        # inclinar hacia adelante y torcer (expresión)
        m = Matrix.Translation(c) @ Matrix.Rotation(math.radians(lid_tilt) * side, 4, lk) \
            @ Matrix.Translation(-c)
        lid.data.transform(m)
        parts.append(lid)
    return parts


def brow(name, p_in, p_out, thick, material, bone="Head", raise_mid=0.01):
    p_in, p_out = Vector(p_in), Vector(p_out)
    mid = (p_in + p_out) / 2 + Vector((0, 0, raise_mid))
    return tube(name, [p_in, mid, p_out], [thick * 0.8, thick, thick * 0.7], material, bone=bone, seg=10,
                cap=3, sub=3, flatten=((0, 1, 0), 0.5))


# ---------------------------------------------------------------------------
# Esqueleto Humanoid
# ---------------------------------------------------------------------------
def mirror_x(p):
    return Vector((-p[0], p[1], p[2]))


def build_armature(name, j, neck_chain=None, tail=None, extra=None):
    """j: dict con puntos (lado izquierdo, +X):
    hips, spine, chest, neck, head, head_end, l_shoulder, l_upperarm, l_lowerarm, l_hand, l_hand_end,
    l_upperleg, l_lowerleg, l_foot, l_toes, l_toes_end.
    neck_chain: lista de puntos de neck a head (más de 2 = huesos extra Neck1..).
    tail: lista de puntos desde la cadera (Tail1..TailN)."""
    arm_data = bpy.data.armatures.new(name + "_Armature")
    arm = bpy.data.objects.new(name, arm_data)
    _link(arm)
    arm.show_in_front = True
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_data.edit_bones
    J = {k: Vector(v) for k, v in j.items()}

    def bone(nm, h, t, parent=None, connect=False):
        b = eb.new(nm)
        b.head, b.tail = Vector(h), Vector(t)
        b.roll = 0.0
        if parent:
            b.parent = eb[parent]
            b.use_connect = connect
        return b

    bone("Hips", J["hips"], J["spine"])
    bone("Spine", J["spine"], J["chest"], "Hips", True)
    bone("Chest", J["chest"], J["neck"], "Spine", True)
    chain = neck_chain or [J["neck"], J["head"]]
    names = ["Neck"] + [f"Neck{i}" for i in range(1, len(chain) - 1)]
    parent = "Chest"
    for i, nm in enumerate(names):
        bone(nm, chain[i], chain[i + 1], parent, i > 0)
        parent = nm
    bone("Head", chain[-1], J["head_end"], parent, True)
    for side, f in (("Left", lambda p: Vector(p)), ("Right", mirror_x)):
        bone(f"{side}Shoulder", f(J["l_shoulder"]), f(J["l_upperarm"]), "Chest")
        bone(f"{side}UpperArm", f(J["l_upperarm"]), f(J["l_lowerarm"]), f"{side}Shoulder", True)
        bone(f"{side}LowerArm", f(J["l_lowerarm"]), f(J["l_hand"]), f"{side}UpperArm", True)
        bone(f"{side}Hand", f(J["l_hand"]), f(J["l_hand_end"]), f"{side}LowerArm", True)
        bone(f"{side}UpperLeg", f(J["l_upperleg"]), f(J["l_lowerleg"]), "Hips")
        bone(f"{side}LowerLeg", f(J["l_lowerleg"]), f(J["l_foot"]), f"{side}UpperLeg", True)
        bone(f"{side}Foot", f(J["l_foot"]), f(J["l_toes"]), f"{side}LowerLeg", True)
        bone(f"{side}Toes", f(J["l_toes"]), f(J["l_toes_end"]), f"{side}Foot", True)
    if tail:
        parent = "Hips"
        for i in range(len(tail) - 1):
            bone(f"Tail{i + 1}", tail[i], tail[i + 1], parent, i > 0)
            parent = f"Tail{i + 1}"
    for nm, h, t, par in (extra or []):
        bone(nm, h, t, par)
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def join_and_skin(name, parts, arm):
    """Une las piezas en una sola malla, aplica pesos automáticos (heat) y fija las piezas rígidas
    (ojos, accesorios, extremidades de juguete) al 100 % en su hueso."""
    for p in parts:
        bone = p.get("bone", "auto")
        if bone != "auto":
            vg = p.vertex_groups.new(name="RIGID__" + bone)
            vg.add(list(range(len(p.data.vertices))), 1.0, "REPLACE")
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.view_layer.objects.active
    body.name = name + "_Body"
    body.data.name = name + "_Body"
    # soldar vértices duplicados de polos
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bm.to_mesh(body.data)
    bm.free()
    # pesos automáticos
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    deform = [b.name for b in arm.data.bones if b.use_deform]
    groups = {vg.name: vg for vg in body.vertex_groups}
    for b in deform:
        if b not in groups:
            groups[b] = body.vertex_groups.new(name=b)
    rigid = {vg.index: vg.name[len("RIGID__"):] for vg in body.vertex_groups if vg.name.startswith("RIGID__")}
    deform_idx = {groups[b].index: b for b in deform}
    mw = body.matrix_world
    segs = {b.name: (arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local) for b in arm.data.bones}
    fixed = unweighted = 0
    for v in body.data.vertices:
        rb = None
        for g in v.groups:
            if g.group in rigid and g.weight > 0.5:
                rb = rigid[g.group]
        if rb:
            for b in deform:
                groups[b].remove([v.index])
            groups[rb].add([v.index], 1.0, "REPLACE")
            fixed += 1
            continue
        tot = sum(g.weight for g in v.groups if g.group in deform_idx)
        if tot < 1e-4:
            # respaldo suave: 2 huesos más cercanos con peso por distancia inversa
            co = mw @ v.co
            ds = sorted((max(_dist_seg(co, *segs[b]), 1e-4), b) for b in deform)[:2]
            ws = [1.0 / d ** 4 for d, _ in ds]
            for (d, b), w in zip(ds, ws):
                groups[b].add([v.index], w / sum(ws), "REPLACE")
            unweighted += 1
    for vg in list(body.vertex_groups):
        if vg.name.startswith("RIGID__"):
            body.vertex_groups.remove(vg)
    print(f"[gs] skin {name}: rigid={fixed} sin_peso_corregidos={unweighted}")
    return body


def _dist_seg(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length


def tri_count(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    me = obj.evaluated_get(dg).to_mesh()
    me.calc_loop_triangles()
    n = len(me.loop_triangles)
    obj.evaluated_get(dg).to_mesh_clear()
    return n


# ---------------------------------------------------------------------------
# Animación por script
# ---------------------------------------------------------------------------
def _mirror_rots(rots):
    return [(a, d if a == "X" else -d) for a, d in rots]


class Pose(dict):
    """Pose: {hueso: [(eje_mundo, grados), ...]} + loc {hueso: Vector en espacio armature}."""

    def __init__(self):
        super().__init__()
        self.loc = {}

    def rot(self, bone, *rots):
        self.setdefault(bone, []).extend(rots)
        return self

    def sym(self, suffix, *rots):
        """Rotación del lado izquierdo y su espejo en el derecho."""
        self.rot("Left" + suffix, *rots)
        self.rot("Right" + suffix, *_mirror_rots(rots))
        return self

    def side(self, side, suffix, *rots):
        self.rot(side + suffix, *(rots if side == "Left" else _mirror_rots(rots)))
        return self

    def move(self, bone, vec):
        self.loc[bone] = Vector(vec)
        return self


def apply_pose(arm, pose):
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = Quaternion()
        pb.location = Vector()
    for bname, rots in pose.items():
        pb = arm.pose.bones.get(bname)
        if pb is None:
            continue
        q = Quaternion()
        for axis, deg in rots:
            q = Quaternion(_AXES[axis], math.radians(deg)) @ q
        R = pb.bone.matrix_local.to_quaternion()
        pb.rotation_quaternion = R.inverted() @ q @ R
    for bname, v in pose.loc.items():
        pb = arm.pose.bones.get(bname)
        if pb is None:
            continue
        R = pb.bone.matrix_local.to_quaternion()
        pb.location = R.inverted() @ v


def make_action(arm, name, keys, loop=False, loc_bones=("Hips",)):
    """keys: lista de (frame, Pose). Inserta claves en todos los huesos para clips limpios.
    loc_bones: huesos que además llevan claves de posición."""
    arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    for frame, pose in keys:
        apply_pose(arm, pose)
        for pb in arm.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=frame, group=pb.name)
            if pb.name in loc_bones:
                pb.keyframe_insert("location", frame=frame, group=pb.name)
    act.use_cyclic = loop
    act["gs_loop"] = loop
    arm.animation_data.action = None
    apply_pose(arm, Pose())
    return act


def wave(t, freq=1.0, phase=0.0):
    return math.sin(2 * math.pi * (t * freq + phase))


def build_standard_actions(arm, spec):
    """Idle_Sit, Swim, Fall, Climb, Celebrate y Hit para cualquier bípedo.
    spec: dict con 'sit_drop' (m que baja la cadera al sentarse), 'height', 'tail' (lista huesos cola),
    'neck' (lista huesos de cuello), 'speed' (x tempo), 'amp' (x amplitud), 'jitter' (temblor nervioso),
    'arm_down' (grados para bajar brazos en reposo)."""
    h = spec["height"]
    sp = spec.get("speed", 1.0)
    amp = spec.get("amp", 1.0)
    jit = spec.get("jitter", 0.0)
    tail = spec.get("tail", [])
    neck = spec.get("neck", ["Neck"])
    arm_down = spec.get("arm_down", 72)
    drop = spec["sit_drop"]
    acts = {}

    def tail_sway(p, t, a=12, f=1.0):
        for i, b in enumerate(tail):
            p.rot(b, ("Z", a * amp * wave(t, f, -0.12 * i)), ("X", -4 * wave(t, f * 2, -0.1 * i)))

    def neck_rot(p, *rots):
        n = len(neck)
        for b in neck:
            p.rot(b, *[(a, d / n) for a, d in rots])
        p.rot("Head", *[(a, d * 0.35) for a, d in rots])

    def nervous(p, t):
        if jit:
            p.rot("Head", ("Z", jit * 3 * wave(t, 7.0)), ("X", jit * 2 * wave(t, 9.0, 0.3)))

    # Idle_Sit: sentado en el borde, piernas colgando, respiración y mirada
    n = int(60 / sp)
    keys = []
    for i in range(0, n + 1, max(1, n // 12)):
        t = i / n
        p = Pose().move("Hips", (0, 0.05 * h, -drop + 0.006 * h * wave(t, 2)))
        p.rot("Spine", ("X", 4 + 2 * wave(t, 2)))
        p.rot("Chest", ("X", 2 * wave(t, 2, 0.1)))
        p.sym("UpperLeg", ("X", -88))
        p.sym("LowerLeg", ("X", 80 + 6 * amp * wave(t, 1.0 * sp)))
        p.side("Left", "LowerLeg", ("X", 5 * amp * wave(t, 1.0 * sp, 0.5)))
        p.sym("Shoulder", ("Y", 6))
        p.sym("UpperArm", ("Y", arm_down - 6), ("Z", -28))
        p.sym("LowerArm", ("Z", -30))
        neck_rot(p, ("Z", 14 * amp * wave(t, 1)), ("X", 4 * wave(t, 2)))
        nervous(p, t)
        tail_sway(p, t, 10, 1)
        keys.append((i, p))
    keys[-1] = (n, keys[0][1])
    acts["Idle_Sit"] = make_action(arm, "Idle_Sit", keys, loop=True)

    # Swim: perrito, brazos alternos, piernas pataleando
    n = int(30 / sp)
    keys = []
    for i in range(0, n + 1, max(1, n // 10)):
        t = i / n
        p = Pose().move("Hips", (0, 0, -0.05 * h + 0.02 * h * wave(t, 2)))
        p.rot("Hips", ("X", 40))
        p.rot("Spine", ("X", 8))
        neck_rot(p, ("X", -45))
        nervous(p, t)
        for side, ph in (("Left", 0.0), ("Right", 0.5)):
            p.side(side, "UpperArm", ("Z", -75), ("X", 0), ("Y", 35 + 35 * amp * wave(t, 1, ph)))
            p.side(side, "LowerArm", ("Y", 20 + 25 * wave(t, 1, ph + 0.25)))
            p.side(side, "UpperLeg", ("X", 22 * amp * wave(t, 2, ph)))
            p.side(side, "LowerLeg", ("X", 25 + 15 * wave(t, 2, ph + 0.2)))
        tail_sway(p, t, 18, 2)
        keys.append((i, p))
    keys[-1] = (n, keys[0][1])
    acts["Swim"] = make_action(arm, "Swim", keys, loop=True)

    # Fall: aspavientos hacia atrás
    n = int(30 / sp)
    keys = []
    for i in range(0, n + 1, max(1, n // 10)):
        t = i / n
        e = min(1.0, t * 3)
        p = Pose().move("Hips", (0, 0, 0.04 * h * math.sin(t * math.pi)))
        p.rot("Spine", ("X", -18 * e))
        neck_rot(p, ("X", -20 * e))
        nervous(p, t)
        for side, ph in (("Left", 0.0), ("Right", 0.5)):
            p.side(side, "UpperArm", ("Y", -50 * e + 30 * amp * wave(t, 3, ph)), ("Z", 15 * wave(t, 3, ph)))
            p.side(side, "LowerArm", ("Y", -30 * e * (0.5 + 0.5 * wave(t, 3, ph))))
            p.side(side, "UpperLeg", ("X", -35 * e * (0.5 + 0.5 * wave(t, 2, ph))))
            p.side(side, "LowerLeg", ("X", 50 * e * (0.5 + 0.5 * wave(t, 2, ph + 0.3))))
        tail_sway(p, t, 30, 3)
        keys.append((i, p))
    acts["Fall"] = make_action(arm, "Fall", keys)

    # Climb: desde el agua, se agarra al borde, sube y queda de pie
    n = int(40 / sp)
    keys = []
    phases = [
        (0.00, dict(z=-0.55, arms=-55, fwd=-60, elbow=0, thigh=0, knee=10, lean=10)),
        (0.25, dict(z=-0.50, arms=-35, fwd=-80, elbow=-40, thigh=-40, knee=60, lean=25)),
        (0.55, dict(z=-0.25, arms=40, fwd=-60, elbow=-80, thigh=-80, knee=90, lean=35)),
        (0.80, dict(z=-0.05, arms=60, fwd=-20, elbow=-30, thigh=-30, knee=40, lean=10)),
        (1.00, dict(z=0.0, arms=arm_down, fwd=0, elbow=0, thigh=0, knee=0, lean=0)),
    ]
    for t, ph in phases:
        p = Pose().move("Hips", (0, -0.05 * h * (1 - t), ph["z"] * h * 0.5))
        p.rot("Spine", ("X", ph["lean"]))
        neck_rot(p, ("X", -ph["lean"] * 0.6))
        p.sym("UpperArm", ("Y", ph["arms"]), ("Z", ph["fwd"]))
        p.sym("LowerArm", ("Y", ph["elbow"]))
        p.side("Left", "UpperLeg", ("X", ph["thigh"]))
        p.side("Left", "LowerLeg", ("X", ph["knee"]))
        p.side("Right", "UpperLeg", ("X", ph["thigh"] * 0.4))
        p.side("Right", "LowerLeg", ("X", ph["knee"] * 0.5))
        tail_sway(p, t, 10, 1)
        keys.append((int(round(t * n)), p))
    acts["Climb"] = make_action(arm, "Climb", keys)

    # Celebrate: agacharse, saltar con brazos en V, puños
    n = int(36 / sp)
    keys = []
    phases = [
        (0.0, 0.0, 0, arm_down, 0),
        (0.2, -0.08, 45, 30, 0),
        (0.45, 0.18, -5, -45, 1),
        (0.6, 0.12, 0, -55, -1),
        (0.8, -0.04, 30, -40, 1),
        (1.0, 0.0, 0, arm_down, 0),
    ]
    for t, z, crouch, arms, pump in phases:
        p = Pose().move("Hips", (0, 0, z * h * amp))
        p.sym("UpperLeg", ("X", -crouch))
        p.sym("LowerLeg", ("X", crouch * 2))
        p.sym("Foot", ("X", -crouch))
        p.rot("Spine", ("X", crouch * 0.3), ("Y", 4 * pump))
        neck_rot(p, ("X", -15 if z > 0.05 else 0), ("Z", 10 * pump))
        p.sym("UpperArm", ("Y", arms), ("Z", -10))
        p.sym("LowerArm", ("Y", -25 * abs(pump)))
        p.sym("Hand", ("Y", -20 * abs(pump)))
        tail_sway(p, t, 25, 2)
        keys.append((int(round(t * n)), p))
    acts["Celebrate"] = make_action(arm, "Celebrate", keys)

    # Hit: golpe, retroceso y vuelta a la postura neutra
    n = int(18 / sp)
    keys = []
    for t, e in ((0.0, 0.0), (0.15, 1.0), (0.4, 0.7), (0.7, 0.25), (1.0, 0.0)):
        p = Pose().move("Hips", (0, 0.06 * h * e, -0.02 * h * e))
        p.rot("Spine", ("X", -20 * e * amp))
        p.rot("Chest", ("X", -10 * e), ("Z", 8 * e))
        neck_rot(p, ("X", -28 * e), ("Z", -12 * e))
        p.sym("UpperArm", ("Y", arm_down * (1 - e) - 40 * e), ("Z", 30 * e))
        p.sym("LowerArm", ("Y", -40 * e))
        p.sym("UpperLeg", ("X", -15 * e))
        p.sym("LowerLeg", ("X", 20 * e))
        tail_sway(p, t, 30 * e, 2)
        keys.append((int(round(t * n)), p))
    acts["Hit"] = make_action(arm, "Hit", keys)
    return acts


# ---------------------------------------------------------------------------
# Exportación y vistas previas
# ---------------------------------------------------------------------------
def export_fbx(objs, path, animated=True):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    for o in objs:
        if o.type == "ARMATURE" and o.animation_data:
            o.animation_data.action = None
            apply_pose(o, Pose())
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type="FACE",
        add_leaf_bones=False, use_armature_deform_only=False, primary_bone_axis="Y",
        secondary_bone_axis="X", bake_anim=animated, bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, path_mode="AUTO", embed_textures=False)
    print(f"[gs] exportado {path} ({os.path.getsize(path) // 1024} KB)")


def _setup_render(res=560):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x = res
    sc.render.resolution_y = res
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "Standard"
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "MATERIAL"
    sh.show_object_outline = True
    sh.object_outline_color = (0.05, 0.05, 0.08)
    sh.show_cavity = True
    sh.cavity_type = "WORLD"
    sh.show_shadows = True
    sh.shadow_intensity = 0.35
    sc.display.light_direction = (0.4, -0.6, 0.7)
    if sc.world is None:
        sc.world = bpy.data.worlds.new("World")
    sc.world.color = (0.62, 0.70, 0.78)
    cam = bpy.data.objects.get("PreviewCam")
    if cam is None:
        cd = bpy.data.cameras.new("PreviewCam")
        cd.type = "ORTHO"
        cam = bpy.data.objects.new("PreviewCam", cd)
        _link(cam)
    sc.camera = cam
    return sc, cam


def _world_bbox(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    lo = Vector((1e9, 1e9, 1e9))
    hi = -lo
    for o in objs:
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            w = o.matrix_world @ v.co
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
        ev.to_mesh_clear()
    return lo, hi


def _render_view(path, objs, direction, cam, margin=1.12, focus=None):
    lo, hi = _world_bbox(objs)
    if focus is not None:
        fc, fs = focus
        lo, hi = fc - Vector((fs, fs, fs)) / 2, fc + Vector((fs, fs, fs)) / 2
    center = (lo + hi) / 2
    d = Vector(direction).normalized()
    rot = (-d).to_track_quat("-Z", "Y")
    cam.rotation_euler = rot.to_euler()
    cam.location = center + d * 20
    # tamaño ortográfico según la proyección de la caja
    mr = rot.to_matrix()
    right, up = mr.col[0], mr.col[1]
    corners = [Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]
    ext = max(max(abs((c - center).dot(right)) for c in corners),
              max(abs((c - center).dot(up)) for c in corners)) * 2
    cam.data.ortho_scale = ext * margin
    cam.data.clip_end = 100
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


VIEWS = [("front", (0, -1, 0.05)), ("side", (1, 0, 0.05)), ("3q", (0.75, -0.8, 0.35))]


def render_sheet(name, objs, arm=None, poses=(), res=520, views=VIEWS, out_name=None):
    """Hoja de vistas previas: fila 1 = frente, perfil, 3/4; fila 2 = poses (acción, frame, dirección)."""
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    sc, cam = _setup_render(res)
    tmp = []
    for vn, d in views:
        p = os.path.join(PREVIEW_DIR, f"_tmp_{name}_{vn}.png")
        _render_view(p, objs, d, cam)
        tmp.append(p)
    if arm is not None and "Head" in arm.data.bones:
        hb = arm.data.bones["Head"]
        fc = arm.matrix_world @ ((hb.head_local + hb.tail_local) / 2)
        fs = max((hb.tail_local - hb.head_local).length * 1.9, 0.45)
        for vn, d in (("face", (0, -1, 0.05)), ("face3q", (0.6, -0.8, 0.15))):
            p = os.path.join(PREVIEW_DIR, f"_tmp_{name}_{vn}.png")
            _render_view(p, objs, d, cam, margin=1.0, focus=(fc + Vector((0, -0.05, 0)), fs))
            tmp.append(p)
    for (act, frame, d) in poses:
        arm.animation_data.action = bpy.data.actions[act]
        sc.frame_set(frame)
        p = os.path.join(PREVIEW_DIR, f"_tmp_{name}_{act}.png")
        _render_view(p, objs, d, cam)
        tmp.append(p)
    if arm is not None and arm.animation_data:
        arm.animation_data.action = None
        apply_pose(arm, Pose())
    sheet = os.path.join(PREVIEW_DIR, (out_name or name) + "_sheet.png")
    stitch(tmp, sheet, cols=len(views))
    for p in tmp:
        os.remove(p)
    print(f"[gs] preview {sheet}")
    return sheet


def stitch(paths, out, cols=3):
    import numpy as np
    imgs = []
    for p in paths:
        im = bpy.data.images.load(p)
        w, h = im.size
        a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
        imgs.append(a)
        bpy.data.images.remove(im)
    h, w = imgs[0].shape[:2]
    rows = (len(imgs) + cols - 1) // cols
    big = np.ones((rows * h, cols * w, 4), dtype=np.float32)
    for i, a in enumerate(imgs):
        r, c = divmod(i, cols)
        # las filas de Blender empiezan abajo
        y0 = (rows - 1 - r) * h
        big[y0:y0 + h, c * w:(c + 1) * w] = a
        big[y0:y0 + h, c * w:c * w + 2] = 0.2
        big[y0:y0 + 2, c * w:(c + 1) * w] = 0.2
    img = bpy.data.images.new("sheet", cols * w, rows * h, alpha=True)
    img.pixels = big.ravel()
    img.filepath_raw = out
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def verify_fbx(path):
    """Reimporta el FBX en una escena vacía y devuelve (huesos, clips, triángulos)."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    tris = sum(tri_count(m) for m in meshes)
    clips = sorted(a.name for a in bpy.data.actions)
    bones = [b.name for b in arms[0].data.bones] if arms else []
    print(f"[gs] verify {os.path.basename(path)}: huesos={len(bones)} clips={clips} tris={tris}")
    return bones, clips, tris


def finish_character(name, body, arm, spec, poses=None):
    tris = tri_count(body)
    print(f"[gs] {name}: {tris} triángulos")
    assert tris <= 15000, f"{name} excede 15k triángulos ({tris})"
    build_standard_actions(arm, spec)
    poses = poses or [("Idle_Sit", 15, (0.75, -0.8, 0.35)), ("Celebrate", int(16 / spec.get("speed", 1)),
                                                             (0.75, -0.8, 0.35)),
                      ("Swim", 5, (1, -0.3, 0.3))]
    sheet = render_sheet(name, [body], arm, poses=poses)
    path = os.path.join(EXPORT_DIR, "Characters", name, name + ".fbx")
    export_fbx([arm, body], path)
    if "--no-verify" not in sys.argv:
        verify_fbx(path)
    return sheet, path


# ---------------------------------------------------------------------------
# Extremidades estándar (piezas rígidas de juguete, una por hueso)
# ---------------------------------------------------------------------------
def standard_limbs(j, arm_mat, leg_mat, r, hand_fn=None, foot_fn=None, arm_mat_fn=None, leg_mat_fn=None, sub=4):
    """Crea brazos y piernas a ambos lados a partir de los puntos del lado izquierdo.
    r: dict con ua (r0, r1), la (r0, r1), ul (r0, r1), ll (r0, r1).
    hand_fn(side, s, j) / foot_fn(side, s, j) devuelven listas de piezas (s = +1 izq, -1 der).
    arm_mat / leg_mat pueden ser listas de materiales con *_mat_fn(t) para rayas."""
    parts = []
    for side, s in (("Left", 1), ("Right", -1)):
        f = (lambda p: Vector(p)) if s == 1 else mirror_x
        parts.append(capsule(f"{side}UpperArm_m", f(j["l_upperarm"]), f(j["l_lowerarm"]), r["ua"][0], arm_mat,
                             r1=r["ua"][1], bone=f"{side}UpperArm", mat_fn=arm_mat_fn, sub=sub))
        parts.append(capsule(f"{side}LowerArm_m", f(j["l_lowerarm"]), f(j["l_hand"]), r["la"][0], arm_mat,
                             r1=r["la"][1], bone=f"{side}LowerArm", mat_fn=arm_mat_fn, sub=sub))
        parts.append(capsule(f"{side}UpperLeg_m", f(j["l_upperleg"]), f(j["l_lowerleg"]), r["ul"][0], leg_mat,
                             r1=r["ul"][1], bone=f"{side}UpperLeg", mat_fn=leg_mat_fn, sub=sub))
        parts.append(capsule(f"{side}LowerLeg_m", f(j["l_lowerleg"]), f(j["l_foot"]), r["ll"][0], leg_mat,
                             r1=r["ll"][1], bone=f"{side}LowerLeg", mat_fn=leg_mat_fn, sub=sub))
        if hand_fn:
            parts += hand_fn(side, s, j)
        if foot_fn:
            parts += foot_fn(side, s, j)
    return parts


def X(p, s):
    """Refleja un punto del lado izquierdo al lado s (+1 izq, -1 der)."""
    return Vector((p[0] * s, p[1], p[2]))


def stickers(obj, count, size, material, rnd, keep_fn=None, min_dist=None, seg=7, rings=4, flat=0.22):
    """Parches poligonales (manchas) pegados a la superficie de 'obj': elipsoides aplanados según la normal,
    con pocas caras para que parezcan polígonos de jirafa. Heredan el hueso de 'obj'."""
    me = obj.data
    faces = [(p.center.copy(), p.normal.copy(), p.area) for p in me.polygons
             if keep_fn is None or keep_fn(p.center)]
    total = sum(a for _, _, a in faces)
    min_dist = min_dist or size * 1.7
    out, placed = [], []
    tries = 0
    while len(out) < count and tries < count * 60:
        tries += 1
        r = rnd.uniform(0, total)
        acc = 0.0
        for c, n, a in faces:
            acc += a
            if acc >= r:
                break
        if any((c - q).length < min_dist for q in placed):
            continue
        placed.append(c)
        sz = size * rnd.uniform(0.75, 1.2)
        rot = n.to_track_quat("Z", "Y") @ gs_quat_z(rnd.uniform(0, 6.28))
        out.append(ellipsoid(f"{obj.name}_spot{len(out)}", c - n * sz * flat * 0.35,
                             (sz * rnd.uniform(0.8, 1.15), sz * rnd.uniform(0.7, 1.0), sz * flat),
                             material, bone=obj.get("bone", "auto"), seg=seg, rings=rings,
                             rot=rot.to_matrix().to_4x4()))
    return out


def gs_quat_z(a):
    return Quaternion((0, 0, 1), a)


def on_front(center, radii, pts, offset=0.0):
    """Proyecta puntos (x, z) sobre la cara frontal (-Y) de un elipsoide; offset hacia afuera."""
    cx, cy, cz = center
    rx, ry, rz = radii
    out = []
    for p in pts:
        x, z = p[0], p[-1]
        k = max(0.0, 1 - ((x - cx) / rx) ** 2 - ((z - cz) / rz) ** 2)
        out.append(Vector((x, cy - ry * math.sqrt(k) - offset, z)))
    return out
