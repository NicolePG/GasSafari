"""GAS SAFARI · kit de escenario (docs/09 §3): utilidades para los scripts env_w1..env_w5.

Cada prop se arma con piezas de gs_common y termina en finish(): une la parte fija en una malla, deja las partes
móviles (hélices, banderas, ruedas, puertas...) como objetos aparte con su pivote, valida ≤ 3000 triángulos,
exporta Tools/Export/Environment/<Mundo>/<Mundo>_<Nombre>.fbx y renderiza una casilla para la hoja de contacto.
Pivote en la base (z = 0 = suelo o línea de agua). Frente hacia -Y, como los personajes.
"""
import math
import os
import random
import bpy
import bmesh
from mathutils import noise
import gs_common as gs
from gs_common import Vector, Matrix

MAX_TRIS = 3000
TILE = 260


# ---------------------------------------------------------------------------
# Materiales
# ---------------------------------------------------------------------------
def M(col, rough=0.6):
    return gs.mat(col, rough=rough)


def E(col, strength=3.0):
    """Material emisivo (neón, cristales, lava, faroles)."""
    return gs.mat(col, prefix="E", rough=0.2, emission=strength)


# ---------------------------------------------------------------------------
# Ensamblado y exportación
# ---------------------------------------------------------------------------
def join(name, objs):
    objs = [o for o in _flat(objs)]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = o.data.name = name
    for vg in list(o.vertex_groups):
        o.vertex_groups.remove(vg)
    return o


def _flat(x):
    if isinstance(x, (list, tuple)):
        for y in x:
            yield from _flat(y)
    elif x is not None:
        yield x


def set_origin(obj, point):
    p = Vector(point)
    obj.data.transform(Matrix.Translation(-p))
    obj.location = p


class Sheet:
    """Casillas de la hoja de contacto de un mundo."""

    def __init__(self, world):
        self.world = world
        self.tiles = []
        self.report = []
        self.over = []

    def stitch(self, cols=8):
        out = os.path.join(gs.PREVIEW_DIR, f"Env_{self.world}_sheet.png")
        gs.stitch(self.tiles, out, cols=cols)
        for p in self.tiles:
            os.remove(p)
        print(f"[gs] preview {out}")
        for line in self.report:
            print(line)
        assert not self.over, f"exceden {MAX_TRIS} triángulos: {self.over}"
        return out


def finish(sheet, name, static, moving=None, view=(0.8, -0.75, 0.45), margin=1.12, max_tris=MAX_TRIS):
    """static: piezas fijas. moving: [(nombre, pivote, piezas)] partes móviles con su pivote."""
    full = f"{sheet.world}_{name}"
    st = join(full if not moving else "Static", static)
    objs = [st]
    for mname, pivot, parts in moving or []:
        o = join(mname, parts)
        set_origin(o, pivot)
        objs.append(o)
    tris = sum(gs.tri_count(o) for o in objs)
    if tris > max_tris:
        sheet.over.append(f"{full}: {tris}")
    export = objs
    if moving:
        root = bpy.data.objects.new(full, None)
        gs._link(root)
        for o in objs:
            o.parent = root
        export = [root] + objs
    path = os.path.join(gs.EXPORT_DIR, "Environment", sheet.world, full + ".fbx")
    gs.export_fbx(export, path, animated=False)
    sc, cam = gs._setup_render(TILE)
    tile = os.path.join(gs.PREVIEW_DIR, f"_tmp_env_{full}.png")
    gs._render_view(tile, objs, view, cam, margin=margin)
    sheet.tiles.append(tile)
    lo, hi = gs._world_bbox(objs)
    size = hi - lo
    sheet.report.append(f"[gs] {full}: {tris} tris, {size.x:.1f}x{size.y:.1f}x{size.z:.1f} m"
                        + (f", móviles: {', '.join(m[0] for m in moving)}" if moving else ""))
    return objs


def run_props(world, props, only=None, cols=8):
    """props: [(nombre, fn(sheet, nombre))]. Cada fn arranca con la escena vacía."""
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    sheet = Sheet(world)
    for name, fn in props:
        if only and name not in only:
            continue
        gs.reset_scene()
        fn(sheet, name)
    return sheet.stitch(cols)


# ---------------------------------------------------------------------------
# Geometría: barridos de perfil, hojas, banderas, rocas
# ---------------------------------------------------------------------------
def sweep(name, prof, path, mats, closed=True, caps=True, mat_fn=None, up=(0, 0, 1), sub=1, scale_fn=None,
          smooth=False):
    """Barre un perfil 2D [(x lateral, z arriba)] a lo largo de un camino 3D (prof puede ser fn(u) -> perfil, con
    el mismo número de puntos). mat_fn(j) = material del lado j.
    scale_fn(u) escala el perfil a lo largo del camino. Sirve para canales, túneles, toboganes, puentes y rieles."""
    path = gs._catmull([Vector(p) for p in path], sub) if sub > 1 else [Vector(p) for p in path]
    upv = Vector(up)
    bm = bmesh.new()
    rings = []
    n = len(path)
    for i, p in enumerate(path):
        t = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        side = t.cross(upv)
        side = side.normalized() if side.length > 1e-6 else Vector((1, 0, 0))
        nu = side.cross(t).normalized()
        k = scale_fn(i / (n - 1)) if scale_fn else 1.0
        pr = prof(i / (n - 1)) if callable(prof) else prof
        rings.append([bm.verts.new(p + side * x * k + nu * z * k) for x, z in pr])
    m = len(prof(0.0) if callable(prof) else prof)
    edges = m if closed else m - 1
    for i in range(n - 1):
        a, b = rings[i], rings[i + 1]
        for j in range(edges):
            j2 = (j + 1) % m
            f = bm.faces.new((a[j], a[j2], b[j2], b[j]))
            f.material_index = mat_fn(j) if mat_fn else 0
    if caps and closed:
        for r in (rings[0], rings[-1]):
            f = bm.faces.new(r)
            f.material_index = mat_fn(0) if mat_fn else 0
    return gs._obj_from_bm(name, bm, mats, smooth=smooth)


def extrude(name, prof, y0, y1, mats, mat_fn=None, smooth=False):
    """Extruye un polígono 2D [(x, z)] a lo largo de Y entre y0 e y1."""
    return sweep(name, prof, [(0, y0, 0), (0, y1, 0)], mats, mat_fn=mat_fn, smooth=smooth)


def arc_path(center, radius, a0, a1, n, z=0.0):
    """Camino en arco horizontal (grados, 0 = +X)."""
    c = Vector(center)
    return [c + Vector((radius * math.cos(math.radians(a0 + (a1 - a0) * i / (n - 1))),
                        radius * math.sin(math.radians(a0 + (a1 - a0) * i / (n - 1))), z)) for i in range(n)]


def leaf(name, pts, width, mat, fold=0.35, th=0.015, sub=3, droop_up=(0, 0, 1)):
    """Hoja cerrada (dos caras) a lo largo de una curva, con punta y doblez en V."""
    path = gs._catmull([Vector(p) for p in pts], sub)
    upv = Vector(droop_up)
    bm = bmesh.new()
    rings = []
    n = len(path)
    for i, p in enumerate(path):
        u = i / (n - 1)
        t = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        side = t.cross(upv)
        side = side.normalized() if side.length > 1e-6 else Vector((1, 0, 0))
        nrm = side.cross(t).normalized()
        w = width * max(0.05, math.sin(math.pi * min(1.0, u * 1.15)) ** 0.7)
        L = p + side * w - nrm * w * fold
        R = p - side * w - nrm * w * fold
        rings.append([bm.verts.new(L + nrm * th), bm.verts.new(p + nrm * th), bm.verts.new(R + nrm * th),
                      bm.verts.new(R - nrm * th), bm.verts.new(p - nrm * th), bm.verts.new(L - nrm * th)])
    for i in range(n - 1):
        a, b = rings[i], rings[i + 1]
        for j in range(6):
            bm.faces.new((a[j], a[(j + 1) % 6], b[(j + 1) % 6], b[j]))
    bm.faces.new(rings[0])
    bm.faces.new(rings[-1])
    return gs._obj_from_bm(name, bm, mat, smooth=True)


def flag(name, origin, length, height, mats, waves=1.5, amp=0.12, th=0.03, n=10, stripes=None):
    """Bandera ondulada (sólido fino) que sale de 'origin' hacia +X. stripes = nº de franjas verticales."""
    o = Vector(origin)
    path = [o + Vector((length * i / n, amp * math.sin(i / n * waves * 2 * math.pi) * (i / n), 0))
            for i in range(n + 1)]
    prof = [(-height / 2, -th), (height / 2, -th), (height / 2, th), (-height / 2, th)]
    f = sweep(name, prof, path, mats, up=(0, 1, 0))
    if stripes:
        gs.bands_by_plane(f, (0, 0, 1), o - Vector((0, 0, height / 2)), height / stripes, 0.5, 1, cut=True)
    return f


def rock(name, loc, size, mats, seed=0, rough=0.35, freq=1.3, subdiv=2, flat_bottom=0.35, top_index=None,
         top_dot=0.55, rot=0.0):
    """Roca facetada: icosfera con ruido; top_index pinta las caras de arriba (musgo, arena, nieve)."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
    off = Vector((seed * 3.1, seed * 1.7, seed * 2.3))
    for v in bm.verts:
        d = 1.0 + rough * noise.noise(v.co * freq + off) + 0.5 * rough * noise.noise(v.co * freq * 2.3 + off)
        v.co *= d
        if v.co.z < -flat_bottom:
            v.co.z = -flat_bottom - (v.co.z + flat_bottom) * 0.15
    m = Matrix.Translation(Vector(loc)) @ Matrix.Rotation(rot, 4, "Z") @ Matrix.Diagonal((*size, 1.0)) \
        @ Matrix.Translation(Vector((0, 0, flat_bottom)))
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    bm.normal_update()
    if top_index is not None:
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        for f in bm.faces:
            if f.normal.z > top_dot:
                f.material_index = top_index
    return gs._obj_from_bm(name, bm, mats, smooth=False)


def ring_of(n, r, z=0.0, a0=0.0):
    return [(r * math.cos(a0 + i / n * 2 * math.pi), r * math.sin(a0 + i / n * 2 * math.pi), z) for i in range(n)]


def stripes_z(obj, z0, period, idx=1, duty=0.5):
    gs.bands_by_plane(obj, (0, 0, 1), (0, 0, z0), period, duty, idx, cut=True)
    return obj


# ---------------------------------------------------------------------------
# Props comunes con 3 variantes (cajas, barriles, boyas, rocas, palmeras)
# ---------------------------------------------------------------------------
def crate(v, wood, wood_dark, accent, extra=None):
    """A: cajón de listones. B: caja de cartón abierta. C: cajón alto con carga encima."""
    P = []
    if v == 0:
        P.append(gs.rounded_box("Box", (0, 0, 0.5), (1.0, 1.0, 1.0), 0.06, wood, bulge=0.04))
        for z in (0.15, 0.85):
            for y in (-0.51, 0.51):
                P.append(gs.rounded_box(f"Slat{z}{y}", (0, y, z), (1.04, 0.06, 0.14), 0.03, wood_dark))
            for x in (-0.51, 0.51):
                P.append(gs.rounded_box(f"SlatS{z}{x}", (x, 0, z), (0.06, 1.04, 0.14), 0.03, wood_dark))
        P.append(gs.rounded_box("Diag", (0, -0.52, 0.5), (1.15, 0.05, 0.12), 0.02, wood_dark, rot=("Y", 45)))
        P.append(gs.rounded_box("Stamp", (0.2, -0.53, 0.62), (0.28, 0.02, 0.2), 0.03, accent))
    elif v == 1:
        P.append(gs.rounded_box("Box", (0, 0, 0.42), (1.1, 0.9, 0.84), 0.04, wood, taper=(1.04, 1.04), bulge=0.05))
        for s, ax, ang in ((1, "X", -1), (-1, "X", 1)):
            P.append(gs.rounded_box(f"FlapY{s}", (0, 0.45 * s + 0.12 * s, 0.9), (1.08, 0.3, 0.03), 0.01, wood,
                                    rot=("X", 55 * ang)))
        for s in (1, -1):
            P.append(gs.rounded_box(f"FlapX{s}", (0.55 * s + 0.1 * s, 0, 0.88), (0.25, 0.86, 0.03), 0.01, wood,
                                    rot=("Y", 60 * s)))
        P.append(gs.rounded_box("Tape", (0, -0.455, 0.45), (0.16, 0.02, 0.85), 0.01, accent))
        P.append(gs.rounded_box("Arrow", (0.32, -0.46, 0.55), (0.08, 0.02, 0.22), 0.02, wood_dark))
    else:
        P.append(gs.rounded_box("Box", (0, 0, 0.6), (0.9, 0.9, 1.2), 0.08, wood, taper=(0.94, 0.94)))
        for z in (0.12, 0.6, 1.1):
            P.append(gs.rounded_box(f"Band{z}", (0, 0, z), (0.95, 0.95, 0.1), 0.04, wood_dark, taper=(0.98, 0.98)))
        P += extra or []
    return P


def barrel(v, wood, metal, accent):
    """A: tonel de madera panzón. B: bidón con franjas de peligro. C: barril chico tumbado con tapón."""
    P = []
    if v == 0:
        b = gs.lathe("Barrel", [(0.0, 0.0), (0.38, 0.0), (0.47, 0.35), (0.49, 0.6), (0.46, 0.9), (0.38, 1.2),
                                (0.0, 1.2)], wood, seg=16)
        P.append(b)
        for z, r in ((0.2, 0.44), (1.0, 0.43), (0.6, 0.5)):
            P.append(gs.torus(f"Hoop{z}", (0, 0, z), r, 0.03, metal, seg=18, mseg=4))
        P.append(gs.rounded_cylinder("Lid", (0, 0, 1.2), 0.37, 0.05, 0.02, wood, seg=14, steps=1))
    elif v == 1:
        b = gs.rounded_cylinder("Drum", (0, 0, 0.55), 0.42, 1.1, 0.06, [accent, metal], seg=16, steps=2)
        gs.bands_by_plane(b, (1, 0, 1), (0, 0, 0), 0.3, 0.5, 1, cut=True)
        P.append(b)
        for z in (0.3, 0.8):
            P.append(gs.torus(f"Rib{z}", (0, 0, z), 0.43, 0.025, metal, seg=18, mseg=4))
        P.append(gs.rounded_cylinder("Cap", (0.2, 0.1, 1.12), 0.07, 0.05, 0.015, metal, seg=8, steps=1))
    else:
        b = gs.lathe("Keg", [(0.0, 0.0), (0.3, 0.0), (0.37, 0.3), (0.38, 0.45), (0.37, 0.6), (0.3, 0.9),
                             (0.0, 0.9)], wood, seg=14, loc=(0, -0.45, 0.38), axis=(0, 1, 0))
        P.append(b)
        for y in (-0.3, 0.3):
            P.append(gs.torus(f"Hoop{y}", (0, y, 0.38), 0.36, 0.03, metal, axis=(0, 1, 0), seg=16, mseg=4))
        P.append(gs.rounded_cylinder("Tap", (0, -0.47, 0.38), 0.06, 0.08, 0.02, accent, axis=(0, 1, 0), seg=8))
        for s in (1, -1):
            P.append(gs.rounded_box(f"Chock{s}", (0.3 * s, 0, 0.08), (0.15, 0.6, 0.16), 0.04, metal))
    return P


def buoy(v, a, b, light, metal):
    """A: cono a franjas con luz. B: bola con aro. C: boya alta con campana y banderín (parte móvil aparte)."""
    P, moving = [], []
    if v == 0:
        body = gs.lathe("Buoy", [(0.0, -0.4), (0.55, -0.4), (0.7, -0.1), (0.62, 0.3), (0.35, 1.3), (0.15, 1.5),
                                 (0.0, 1.5)], [a, b], seg=16)
        stripes_z(body, -0.4, 0.5)
        P.append(body)
        P.append(gs.torus("Fender", (0, 0, 0.0), 0.68, 0.1, metal, seg=18, mseg=6))
        P.append(gs.ellipsoid("Light", (0, 0, 1.62), (0.14, 0.14, 0.18), light, seg=10, rings=6))
        P.append(gs.rounded_cylinder("Cage", (0, 0, 1.52), 0.18, 0.06, 0.02, metal, seg=10, steps=1))
    elif v == 1:
        body = gs.ellipsoid("Ball", (0, 0, 0.35), (0.75, 0.75, 0.68), [a, b], seg=16, rings=10)
        gs.bands_by_plane(body, (0, 0, 1), (0, 0, 0.32), 2.0, 0.08, 1, cut=True)
        P.append(body)
        P.append(gs.torus("Ring", (0, 0, 1.12), 0.18, 0.05, metal, axis=(0, 1, 0), seg=14, mseg=5))
        P.append(gs.rounded_cylinder("Eye", (0, 0, 1.0), 0.08, 0.1, 0.02, metal, seg=8))
        for k in range(4):
            ang = k * math.pi / 2
            P.append(gs.ellipsoid(f"Dot{k}", (0.66 * math.cos(ang), 0.66 * math.sin(ang), 0.52), 0.1, light, seg=8,
                                  rings=4))
    else:
        P.append(gs.rounded_cylinder("Float", (0, 0, 0.0), 0.6, 0.6, 0.15, [b], seg=16, steps=2))
        mast = gs.lathe("Mast", [(0.0, 0.3), (0.32, 0.3), (0.12, 2.2), (0.0, 2.25)], [a, b], seg=12)
        stripes_z(mast, 0.3, 0.6)
        P.append(mast)
        P.append(gs.lathe("Bell", [(0.0, 1.35), (0.2, 1.35), (0.17, 1.5), (0.1, 1.68), (0.0, 1.7)], metal,
                          loc=(0.0, 0.0, 0.0), seg=10))
        P.append(gs.ellipsoid("Top", (0, 0, 2.3), 0.12, light, seg=8, rings=5))
        moving.append(("Pennant", (0.04, 0, 2.05), [flag("Pennant", (0.04, 0, 2.05), 0.7, 0.32, [a], waves=1,
                                                         amp=0.08, n=6)]))
    return P, moving


def rock_variant(v, mats, seed=0, scale=1.0, top_index=1):
    """A: canto rodado. B: aguja inclinada. C: grupo de lajas."""
    s = scale
    if v == 0:
        return [rock("Boulder", (0, 0, 0), (1.6 * s, 1.3 * s, 1.1 * s), mats, seed=seed, top_index=top_index)]
    if v == 1:
        return [rock("Spire", (0, 0, 0), (0.8 * s, 0.7 * s, 2.6 * s), mats, seed=seed + 1, rough=0.25,
                     top_index=top_index, top_dot=0.7),
                rock("Base", (0.6 * s, 0.3 * s, 0), (0.7 * s, 0.6 * s, 0.45 * s), mats, seed=seed + 2, subdiv=1,
                     top_index=top_index)]
    out = []
    for i, (x, y, w, h, r) in enumerate(((0, 0, 1.3, 0.5, 0.3), (0.9, 0.5, 0.9, 0.85, 1.2), (-0.8, 0.4, 0.8, 0.35, 2.0),
                                         (0.2, -0.8, 0.6, 0.3, 0.7))):
        out.append(rock(f"Slab{i}", (x * s, y * s, 0), (w * s, w * 0.8 * s, h * s), mats, seed=seed + 3 + i,
                        subdiv=1, rough=0.2, top_index=top_index, rot=r))
    return out


def palm(v, trunk, trunk_dark, leaf_mats, nut, seed=0):
    """A: palmera curva alta. B: dos troncos en V. C: palmera baja y frondosa."""
    rnd = random.Random(seed)
    P = []
    specs = {0: [((0, 0, 0), (0.9, 0.2, 2.5), (2.2, 0.4, 5.0), (2.6, 0.5, 6.4))],
             1: [((0, 0, 0), (-0.6, 0, 2.0), (-1.5, 0.2, 3.8), (-1.6, 0.3, 4.6)),
                 ((0.1, 0, 0), (0.8, -0.1, 2.4), (1.8, -0.2, 4.6), (2.0, -0.3, 5.6))],
             2: [((0, 0, 0), (0.3, 0, 1.2), (0.7, 0.1, 2.3), (0.8, 0.1, 2.8))]}[v]
    for k, pts in enumerate(specs):
        pts = [Vector(p) for p in pts]
        tr = gs.tube(f"Trunk{k}", pts, [0.32, 0.25, 0.2, 0.18], [trunk, trunk_dark], seg=9, sub=4, cap=2,
                     mat_fn=lambda u: int(u * 14) % 2)
        P.append(tr)
        top = pts[-1] + Vector((0, 0, 0.1))
        for i in range(3):
            a = i / 3 * 2 * math.pi + rnd.uniform(0, 1)
            P.append(gs.ellipsoid(f"Nut{k}{i}", top + Vector((0.22 * math.cos(a), 0.22 * math.sin(a), -0.2)), 0.17,
                                  nut, seg=8, rings=5))
        nf = 7 if v != 2 else 9
        L = 2.6 if v == 0 else (2.2 if v == 1 else 2.0)
        for i in range(nf):
            a = i / nf * 2 * math.pi + rnd.uniform(-0.2, 0.2)
            d = Vector((math.cos(a), math.sin(a), 0))
            rise = rnd.uniform(0.3, 0.7)
            fpts = [top, top + d * L * 0.35 + Vector((0, 0, rise)), top + d * L * 0.75 + Vector((0, 0, rise * 0.4)),
                    top + d * L + Vector((0, 0, -0.6 - rnd.uniform(0, 0.5)))]
            P.append(leaf(f"Frond{k}{i}", fpts, rnd.uniform(0.32, 0.42), leaf_mats[i % len(leaf_mats)], sub=3))
    return P


def flower(name, loc, petal, center, stem=None, size=0.18, n=5, h=0.4, tilt=0.0):
    c = Vector(loc) + Vector((0, 0, h))
    out = []
    if stem is not None:
        out.append(gs.capsule(name + "_Stem", loc, c, 0.02, stem, seg=4, cap=1, sub=1))
    for i in range(n):
        a = i / n * 2 * math.pi
        out.append(gs.ellipsoid(f"{name}_P{i}", c + Vector((math.cos(a), math.sin(a), 0)) * size * 0.6,
                                (size * 0.55, size * 0.3, size * 0.12), petal, seg=6, rings=3,
                                rot=[("X", tilt), ("Z", math.degrees(a))]))
    out.append(gs.ellipsoid(name + "_C", c + Vector((0, 0, 0.03)), size * 0.35, center, seg=6, rings=4))
    return out


def bush(name, loc, r, mats, seed=0, n=6):
    rnd = random.Random(seed)
    out = [gs.ellipsoid(name, Vector(loc) + Vector((0, 0, r * 0.7)), (r, r, r * 0.8), mats[0], seg=10, rings=6)]
    for i in range(n):
        a = i / n * 2 * math.pi + rnd.uniform(0, 0.5)
        p = Vector(loc) + Vector((math.cos(a) * r * 0.7, math.sin(a) * r * 0.7, r * rnd.uniform(0.6, 1.2)))
        out.append(gs.ellipsoid(f"{name}{i}", p, r * rnd.uniform(0.45, 0.6), mats[i % len(mats)], seg=8, rings=5))
    return out


def sign(name, post, board, icon_fn, h=2.0, w=1.2, bh=0.9, tilt=6.0):
    """Cartel sin texto: poste + tablero torcido; icon_fn(centro_frontal) devuelve las piezas del dibujo."""
    out = [gs.capsule(name + "_Post", (0, 0, 0), (0, 0.02, h), 0.06, post, seg=6, cap=1, sub=1)]
    c = Vector((0, -0.06, h - bh * 0.3))
    out.append(gs.rounded_box(name + "_Board", c, (w, 0.08, bh), 0.06, board, rot=("Y", tilt)))
    out += icon_fn(c + Vector((0, -0.05, 0)))
    return out


def lamp_post(name, iron, glow, h=4.0, arms=1, curl=True):
    out = [gs.rounded_cylinder(name + "_Base", (0, 0, 0.2), 0.3, 0.4, 0.08, iron, seg=10, steps=1)]
    out.append(gs.tube(name + "_Pole", [(0, 0, 0.3), (0, 0, h * 0.5), (0.05, 0, h * 0.9), (0.3, 0, h)],
                       [0.1, 0.08, 0.07, 0.06], iron, seg=8, sub=3, cap=2))
    for k in range(arms):
        s = 1 if k == 0 else -1
        tip = Vector((0.75 * s, 0, h - 0.15))
        out.append(gs.tube(f"{name}_Arm{k}", [(0.2 * s if s == 1 else -0.1, 0, h - 0.05), (0.5 * s, 0, h + 0.15), tip],
                           0.045, iron, seg=6, sub=3, cap=1))
        out.append(gs.lathe(f"{name}_Shade{k}", [(0.0, 0.2), (0.32, 0.0), (0.3, -0.05), (0.0, -0.02)], iron,
                            loc=tip + Vector((0, 0, -0.05)), seg=10))
        out.append(gs.ellipsoid(f"{name}_Glow{k}", tip + Vector((0, 0, -0.25)), (0.2, 0.2, 0.24), glow, seg=10,
                                rings=6))
    if curl:
        out.append(gs.tube(name + "_Curl", gs.arc_points((-0.18, 0, h * 0.72), 0.15, -90, 200, 8), 0.03, iron,
                           seg=5, cap=1))
    return out


def hull(name, length, width, height, mats, bow_rise=0.6, belly=0.12, stripe=None, bottom=None, flat_stern=True):
    """Casco panzón de caricatura: proa (-Y) levantada y afinada, popa (+Y) ancha. Base en z = -0.45*height.
    stripe=(z0, z1, idx) pinta una franja; bottom=(z, idx) pinta la obra viva."""
    h, w = height, width / 2
    prof = [(-w * 0.96, h * 0.55), (-w, h * 0.2), (-w * 0.85, -h * 0.2), (-w * 0.45, -h * 0.42), (0, -h * 0.45),
            (w * 0.45, -h * 0.42), (w * 0.85, -h * 0.2), (w, h * 0.2), (w * 0.96, h * 0.55)]
    L = length / 2
    path = [(0, L, 0), (0, L * 0.4, 0), (0, -L * 0.3, 0), (0, -L * 0.75, h * bow_rise * 0.35), (0, -L, h * bow_rise)]

    def sc(u):
        k = 1 + belly * math.sin(math.pi * u)
        if u > 0.55:
            k *= max(0.12, 1 - ((u - 0.55) / 0.45) ** 1.6)
        if flat_stern and u < 0.05:
            k *= 0.94
        return k
    o = sweep(name, prof, path, mats, sub=4, scale_fn=sc, smooth=True)
    if bottom:
        gs.paint_faces(o, lambda c: c.z < bottom[0], bottom[1])
    if stripe:
        gs.paint_faces(o, lambda c: stripe[0] < c.z < stripe[1], stripe[2])
    return o
