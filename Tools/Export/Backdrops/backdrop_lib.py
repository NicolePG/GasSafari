"""GAS SAFARI · fondos de escenario por mundo (capa media, capa lejana y suelo de relleno).

Librería común de los scripts backdrops_w1.py .. backdrops_w6.py. Usa gs_common (paleta, materiales planos, FBX).
Cada pieza es UNA malla (varios materiales) hecha con el constructor `B`, con la cara hacia fuera bien orientada,
sombreado plano y pivote en la base (z = 0). Metros, Z arriba (Blender); en Unity el FBX sale con Y arriba.

Límites: capa media <= 5000 triángulos, capa lejana <= 2000, suelo <= 2000.
Salida: Tools/Export/Backdrops/<Mundo>/<Mundo>_<Capa>_<Nombre>.fbx y una hoja de contacto en Tools/Blender/previews/.
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(HERE)), "Blender"))
import bpy
import bmesh
from mathutils import noise
import gs_common as gs
from gs_common import Vector, Matrix

LIMITS = {"Mid": 5000, "Far": 2000, "Ground": 2000}
Z = Vector((0, 0, 1))


# ---------------------------------------------------------------------------
# Materiales
# ---------------------------------------------------------------------------
def M(col, rough=0.85):
    return gs.mat(col, rough=rough)


def E(col, strength=3.0):
    """Emisivo (ventanas, neón, cristales, lava)."""
    return gs.mat(col, prefix="E", rough=0.3, emission=strength)


def mix(a, b, t):
    """Mezcla dos colores '#RRGGBB' (t = 0 -> a, 1 -> b)."""
    ca = [int(a[i:i + 2], 16) for i in (1, 3, 5)]
    cb = [int(b[i:i + 2], 16) for i in (1, 3, 5)]
    return "#" + "".join(f"{int(round(x + (y - x) * t)):02X}" for x, y in zip(ca, cb))


# ---------------------------------------------------------------------------
# Ruido
# ---------------------------------------------------------------------------
def fbm(x, y, seed=0.0, octaves=3, freq=1.0):
    s, a, f = 0.0, 1.0, freq
    tot = 0.0
    for _ in range(octaves):
        s += a * noise.noise(Vector((x * f + seed * 7.13, y * f - seed * 3.71, seed)))
        tot += a
        a *= 0.5
        f *= 2.03
    return s / tot


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------------------
# Constructor de mallas
# ---------------------------------------------------------------------------
class B:
    """Acumula caras en un bmesh con una pila de transformaciones (push/pop) y materiales por índice."""

    def __init__(self):
        self.bm = bmesh.new()
        self.mats = []
        self.cur = Matrix.Identity(4)
        self.stack = []

    # -- materiales y transformación ------------------------------------
    def mi(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def push(self, loc=(0, 0, 0), yaw=0.0, s=1.0):
        self.stack.append(self.cur)
        self.cur = self.cur @ Matrix.Translation(Vector(loc)) @ Matrix.Rotation(yaw, 4, "Z") @ Matrix.Scale(s, 4)

    def pop(self):
        self.cur = self.stack.pop()

    def tris(self):
        return sum(len(f.verts) - 2 for f in self.bm.faces)

    # -- caras -----------------------------------------------------------
    def poly(self, pts, m):
        vs = [self.bm.verts.new(self.cur @ Vector(p)) for p in pts]
        try:
            f = self.bm.faces.new(vs)
        except ValueError:
            return None
        f.material_index = self.mi(m)
        f.smooth = False
        return f

    def quad(self, a, b, c, d, m):
        return self.poly((a, b, c, d), m)

    def tri(self, a, b, c, m):
        return self.poly((a, b, c), m)

    def facing(self, pts, m, direction):
        """Polígono cuya cara visible mira hacia 'direction' (corrige el sentido de giro)."""
        pts = [Vector(p) for p in pts]
        n = Vector((0, 0, 0))
        for i in range(len(pts)):
            a, c = pts[i], pts[(i + 1) % len(pts)]
            n += Vector(((a.y - c.y) * (a.z + c.z), (a.z - c.z) * (a.x + c.x), (a.x - c.x) * (a.y + c.y)))
        if n.dot(Vector(direction)) < 0:
            pts.reverse()
        return self.poly(pts, m)

    def flip_all(self):
        bmesh.ops.reverse_faces(self.bm, faces=list(self.bm.faces))

    def wall_quad(self, c, n, hw, hh, m):
        """Cuadrado vertical centrado en c, que mira hacia la normal horizontal n (ventanas, letreros)."""
        n = Vector(n).normalized()
        u = Z.cross(n)
        c = Vector(c)
        return self.quad(c - u * hw - Z * hh, c + u * hw - Z * hh, c + u * hw + Z * hh, c - u * hw + Z * hh, m)

    # -- sólidos ---------------------------------------------------------
    def loft(self, rings, m, top=True, bottom=True, ms=None):
        """Une anillos (listas de puntos en sentido antihorario visto desde arriba). El último puede ser un
        solo punto (ápice). ms: lista de materiales por tramo."""
        for k in range(len(rings) - 1):
            a, b = rings[k], rings[k + 1]
            mm = ms[k] if ms else m
            if isinstance(b, Vector):
                for i in range(len(a)):
                    self.tri(a[i], a[(i + 1) % len(a)], b, mm)
            elif isinstance(a, Vector):
                for i in range(len(b)):
                    self.tri(a, b[(i + 1) % len(b)], b[i], mm)
            else:
                n = len(a)
                for i in range(n):
                    self.quad(a[i], a[(i + 1) % n], b[(i + 1) % n], b[i], mm)
        if top and not isinstance(rings[-1], Vector):
            self.poly(rings[-1], (ms[-1] if ms else m))
        if bottom and not isinstance(rings[0], Vector):
            self.poly(list(reversed(rings[0])), (ms[0] if ms else m))

    def box(self, cx, cy, z0, w, d, h, m, yaw=0.0, taper=1.0, top_m=None, bottom=False, ms=None):
        ring0 = rect_ring(cx, cy, z0, w / 2, d / 2, yaw)
        ring1 = rect_ring(cx, cy, z0 + h, w / 2 * taper, d / 2 * taper, yaw)
        if top_m is not None:
            self.loft([ring0, ring1], m, top=False, bottom=bottom, ms=ms)
            self.poly(ring1, top_m)
        else:
            self.loft([ring0, ring1], m, top=True, bottom=bottom, ms=ms)

    def cyl(self, cx, cy, z0, r0, r1, h, m, n=8, yaw=0.0, top=True, bottom=False, ry_scale=1.0):
        a = ring(cx, cy, z0, r0, r0 * ry_scale, n, yaw)
        if r1 <= 0.0:
            self.loft([a, Vector((cx, cy, z0 + h))], m, bottom=bottom)
        else:
            self.loft([a, ring(cx, cy, z0 + h, r1, r1 * ry_scale, n, yaw)], m, top=top, bottom=bottom)

    def prism_between(self, p0, p1, r0, r1, m, n=4, caps=True):
        """Barra entre dos puntos (cables, mástiles, cañerías, rayos de luz)."""
        p0, p1 = Vector(p0), Vector(p1)
        ax = (p1 - p0)
        if ax.length < 1e-6:
            return
        q = ax.to_track_quat("Z", "Y")
        mat = q.to_matrix()
        ra = [p0 + mat @ Vector((r0 * math.cos(i / n * 2 * math.pi + math.pi / n), r0 * math.sin(i / n * 2 * math.pi + math.pi / n), 0))
              for i in range(n)]
        rb = [p1 + mat @ Vector((r1 * math.cos(i / n * 2 * math.pi + math.pi / n), r1 * math.sin(i / n * 2 * math.pi + math.pi / n), 0))
              for i in range(n)]
        for i in range(n):
            self.quad(ra[i], ra[(i + 1) % n], rb[(i + 1) % n], rb[i], m)
        if caps:
            self.poly(rb, m)
            self.poly(list(reversed(ra)), m)

    def blob(self, c, radii, m, n=7, rings=4, jit=0.18, seed=0, flat_bottom=None, ms=None, rot=0.0):
        """Elipsoide facetado con ruido (copas de árbol, nubes, rocas, globos lisos si jit = 0)."""
        if not isinstance(radii, (tuple, list)):
            radii = (radii, radii, radii)
        rnd = random.Random(seed)
        c = Vector(c)
        rg = [Vector(c + Vector((0, 0, -radii[2])))]
        mid = []
        for r in range(1, rings):
            ph = -math.pi / 2 + r / rings * math.pi
            ring_pts = []
            for i in range(n):
                a = i / n * 2 * math.pi + rot
                k = 1.0 + rnd.uniform(-jit, jit)
                p = Vector((math.cos(ph) * math.cos(a) * radii[0], math.cos(ph) * math.sin(a) * radii[1],
                            math.sin(ph) * radii[2])) * k
                if flat_bottom is not None and p.z < flat_bottom * radii[2]:
                    p.z = flat_bottom * radii[2]
                ring_pts.append(c + p)
            mid.append(ring_pts)
        top = Vector(c + Vector((0, 0, radii[2])))
        if flat_bottom is not None:
            # base plana: un anillo en el piso
            first = mid[0]
            self.poly(list(reversed(first)), ms[0] if ms else m)
            self.loft(mid + [top], m, top=False, bottom=False, ms=ms)
        else:
            self.loft([rg[0]] + mid + [top], m, top=False, bottom=False, ms=ms)

    def crystal(self, base, direction, length, r, m, n=6, tip=0.28):
        """Cristal hexagonal con punta (cuerpo + pirámide)."""
        base = Vector(base)
        d = Vector(direction).normalized()
        q = d.to_track_quat("Z", "Y").to_matrix()
        body = length * (1 - tip)
        a = [base + q @ Vector((r * math.cos(i / n * 2 * math.pi), r * math.sin(i / n * 2 * math.pi), 0)) for i in range(n)]
        b2 = [base + q @ Vector((r * math.cos(i / n * 2 * math.pi), r * math.sin(i / n * 2 * math.pi), body)) for i in range(n)]
        t = base + d * length
        for i in range(n):
            self.quad(a[i], a[(i + 1) % n], b2[(i + 1) % n], b2[i], m)
            self.tri(b2[i], b2[(i + 1) % n], t, m)

    def sheet(self, x0, x1, y0, y1, nx, ny, fn, mfn):
        """Rejilla de alturas z = fn(x, y), con material mfn(x, y, z, pendiente). Normales hacia arriba."""
        zs = [[fn(x0 + (x1 - x0) * i / nx, y0 + (y1 - y0) * j / ny) for j in range(ny + 1)] for i in range(nx + 1)]
        P = lambda i, j: Vector((x0 + (x1 - x0) * i / nx, y0 + (y1 - y0) * j / ny, zs[i][j]))
        for i in range(nx):
            for j in range(ny):
                a, b, c, d = P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1)
                n = (b - a).cross(d - a)
                slope = 1.0 - abs(n.normalized().z)
                cz = (a.z + b.z + c.z + d.z) / 4
                # triángulo en vez de cuadrilátero alabeado
                mm = mfn((a.x + c.x) / 2, (a.y + c.y) / 2, cz, slope)
                if (i + j) % 2:
                    self.tri(a, b, c, mm)
                    self.tri(a, c, d, mm)
                else:
                    self.tri(a, b, d, mm)
                    self.tri(b, c, d, mm)

    def polar(self, r0, r1, nseg, nr, fn, mfn):
        """Anillo (corona) alrededor del origen con z = fn(r, ángulo). Normales hacia arriba."""
        P = lambda i, j: (lambda r, a: Vector((r * math.cos(a), r * math.sin(a), fn(r, a))))(
            r0 + (r1 - r0) * j / nr, i / nseg * 2 * math.pi)
        for i in range(nseg):
            for j in range(nr):
                a, b, c, d = P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1)
                # visto desde arriba (a -> b gira antihorario; j crece hacia fuera): a, d, c, b
                mm = mfn((a.z + c.z) / 2, j, i)
                self.tri(a, d, c, mm)
                self.tri(a, c, b, mm)

    # -- salida ----------------------------------------------------------
    def to_obj(self, name):
        bm = self.bm
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        for m in self.mats:
            me.materials.append(m)
        obj = bpy.data.objects.new(name, me)
        for p in me.polygons:
            p.use_smooth = False
        return gs._link(obj)


def rect_ring(cx, cy, z, rx, ry, yaw=0.0):
    c2, s2 = math.cos(yaw), math.sin(yaw)
    out = []
    for lx, ly in ((rx, ry), (-rx, ry), (-rx, -ry), (rx, -ry)):
        out.append(Vector((cx + lx * c2 - ly * s2, cy + lx * s2 + ly * c2, z)))
    return out


def ring(cx, cy, z, rx, ry, n, yaw=0.0, jit=None):
    out = []
    for i in range(n):
        a = i / n * 2 * math.pi + yaw
        k = 1.0 if jit is None else 1.0 + jit[i % len(jit)]
        out.append(Vector((cx + math.cos(a) * rx * k, cy + math.sin(a) * ry * k, z)))
    return out


# ---------------------------------------------------------------------------
# Elementos reutilizables
# ---------------------------------------------------------------------------
def windows(b, cx, cy, z0, w, d, h, yaw, lit, rnd, bay=4.0, floor=3.6, prob=0.45, off=0.12, skip_low=1,
            sides=(0, 1, 2, 3)):
    """Ventanas emisivas sueltas (solo las encendidas) en las 4 caras de una caja. lit: materiales."""
    b.push((cx, cy, z0), yaw)
    nf = int(h // floor)
    for s in sides:
        n = [Vector((0, -1, 0)), Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((-1, 0, 0))][s]
        width = w if s in (0, 2) else d
        depth = d if s in (0, 2) else w
        nc = max(1, int(width // bay))
        for r in range(skip_low, nf):
            for c in range(nc):
                if rnd.random() < prob:
                    u = Z.cross(n)
                    ctr = n * (depth / 2 + off) + u * ((c + 0.5) / nc - 0.5) * width * 0.9 + Z * (r + 0.55) * floor
                    b.wall_quad(ctr, n, bay * 0.28, floor * 0.26, rnd.choice(lit))
    b.pop()


def tower(b, cx, cy, w, d, h, yaw, wall, roof, lit, rnd, steps=1, prob=0.42, bay=4.0, floor=3.6, windows_on=True,
          crown=None):
    """Edificio en escalones (steps) con ventanas emisivas. crown: función(b, x, y, z, w, d) opcional (antena...)."""
    z = 0.0
    ww, dd, hh = w, d, h
    for s in range(steps):
        seg_h = hh * (0.62 if s < steps - 1 else 1.0) if steps > 1 else hh
        b.box(cx, cy, z, ww, dd, seg_h, wall, yaw=yaw, top_m=roof)
        if windows_on:
            windows(b, cx, cy, z, ww, dd, seg_h, yaw, lit, rnd, bay=bay, floor=floor, prob=prob)
        z += seg_h
        hh -= seg_h
        ww *= 0.72
        dd *= 0.72
    if crown:
        crown(b, cx, cy, z, ww, dd)
    return z


def fit(b, label, limit):
    n = b.tris()
    assert n <= limit, f"{label}: {n} triángulos (máximo {limit})"
    return n


# ---------------------------------------------------------------------------
# Ejecución: construir, validar, exportar y hoja de contacto
# ---------------------------------------------------------------------------
class Sheet:
    def __init__(self, world):
        self.world = world
        self.tiles = []
        self.report = []


def _tile(obj, path, view=(0.0, -1.0, 0.45)):
    sc, cam = gs._setup_render(300)
    sc.render.resolution_x, sc.render.resolution_y = 400, 300
    sc.render.film_transparent = False
    lo, hi = gs._world_bbox([obj])
    ctr = (lo + hi) / 2
    ext = max((hi - lo).x, (hi - lo).z * 1.3, (hi - lo).y * 0.5) * 1.1
    d = Vector(view).normalized()
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.location = ctr + d * (ext * 4)
    cam.data.ortho_scale = ext
    cam.data.clip_start, cam.data.clip_end = 1, ext * 20
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)


def build_all(world, pieces, only=None, cols=6):
    """pieces: [(capa, nombre, fn(b, rnd))]. capa = 'Mid' | 'Far' | 'Ground'."""
    outdir = os.path.join(HERE, world)
    os.makedirs(outdir, exist_ok=True)
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    sheet = Sheet(world)
    for layer, name, fn in pieces:
        if only and name not in only and f"{layer}_{name}" not in only:
            continue
        gs.reset_scene()
        full = f"{world}_{layer}_{name}"
        rnd = random.Random(hash_str(full))
        b = B()
        fn(b, rnd)
        tris = fit(b, full, LIMITS[layer])
        obj = b.to_obj(full)
        path = os.path.join(outdir, full + ".fbx")
        gs.export_fbx([obj], path, animated=False)
        lo, hi = gs._world_bbox([obj])
        sz = hi - lo
        sheet.report.append(f"[bd] {full}: {tris} tris, {sz.x:.0f}x{sz.y:.0f}x{sz.z:.0f} m, z {lo.z:.0f}..{hi.z:.0f}")
        tile = os.path.join(gs.PREVIEW_DIR, f"_tmp_bd_{full}.png")
        _tile(obj, tile)
        sheet.tiles.append(tile)
    if sheet.tiles:
        out = os.path.join(gs.PREVIEW_DIR, f"Backdrops_{world}_sheet.png")
        gs.stitch(sheet.tiles, out, cols=cols)
        for p in sheet.tiles:
            os.remove(p)
        print(f"[bd] hoja {out}")
    for line in sheet.report:
        print(line)
    return sheet


def hash_str(s):
    h = 0
    for ch in s:
        h = (h * 131 + ord(ch)) % 1000003
    return h


def script_args():
    return set(sys.argv[sys.argv.index("--") + 1:]) if "--" in sys.argv else None


# ---------------------------------------------------------------------------
# Suelo de relleno genérico (se parametriza por mundo)
# ---------------------------------------------------------------------------
def ground_tile(b, mats, seed, size=200.0, n=20, amp=0.7, freq=0.02, edge=18.0, patch=0.5):
    """Losa de terreno de size x size con bordes planos (z = 0) para que encaje con las vecinas. mats = 3 materiales
    (base, parche, acento). Parte superior en z = 0 en los bordes."""
    h = size / 2

    def fn(x, y):
        m = min(smooth((h - abs(x)) / edge), smooth((h - abs(y)) / edge))
        return amp * (fbm(x, y, seed, 3, freq) * 0.8 + 0.2) * m - 0.0 * m

    def mfn(x, y, z, s):
        v = fbm(x + 300, y - 100, seed + 5, 2, freq * 1.7)
        if v > patch * 0.6:
            return mats[2]
        if v > -patch * 0.1:
            return mats[1]
        return mats[0]

    b.sheet(-h, h, -h, h, n, n, fn, mfn)


def ground_skirt(b, mats, r0=280.0, r1=3200.0, z=-0.5, rise=1.6):
    """Corona enorme de terreno plano bajo el nivel (z = -0.5 respecto del suelo/orilla), con un leve ascenso."""
    nseg, nr = 48, 5

    def fn(r, a):
        t = (r - r0) / (r1 - r0)
        return z + rise * t * t + 0.9 * fbm(math.cos(a) * r * 0.01, math.sin(a) * r * 0.01, 4.0, 2) * smooth((r - r0) / 400)

    b.polar(r0, r1, nseg, nr, fn, lambda zz, j, i: mats[(i // 3 + j) % len(mats)] if len(mats) > 1 else mats[0])


def horizon_ring(b, mats, r_in=1700.0, depth=700.0, hmin=25.0, hmax=140.0, seed=3.0, nseg=120, pointy=0.0, base=-3.0):
    """Cadena baja de relieve de 360° (cierra el horizonte): colinas/olas/paredes según el material."""
    nr = 3

    def fn(r, a):
        t = (r - r_in) / depth
        prof = math.sin(min(1.0, t * 1.4) * math.pi * 0.5)
        v = 0.5 + 0.5 * fbm(math.cos(a) * 3.0, math.sin(a) * 3.0, seed, 3, 1.0)
        v2 = 0.5 + 0.5 * fbm(math.cos(a) * 11.0, math.sin(a) * 11.0, seed + 9, 2, 1.0)
        hh = hmin + (hmax - hmin) * (v * 0.75 + v2 * 0.25)
        if pointy:
            hh *= 1.0 + pointy * (v2 - 0.5)
        return base + hh * prof

    def mf(zz, j, i):
        return mats[0] if zz < (base + 0.45 * (hmin + hmax)) else mats[min(1, len(mats) - 1)]

    b.polar(r_in, r_in + depth, nseg, nr, fn, mf)


# ---------------------------------------------------------------------------
# Piezas comunes (rocas, árboles, barcos, nubes...)
# ---------------------------------------------------------------------------
def rock_mass(b, x, y, r, h, body, top=None, seed=0, n=7, rings=4, jit=0.28, z0=0.0, flat=0.0, ry=None):
    """Peñasco/isla: media cúpula facetada con base plana; top = material de la parte alta (musgo, nieve, arena)."""
    ms = None
    if top is not None:
        ms = [body] * (rings - 2) + [top]
        if rings - 1 > len(ms):
            ms = [body] * (rings - 1 - len(ms)) + ms
        ms = ms[-(rings - 1):]
    b.blob((x, y, z0), (r, ry or r * 0.85, h), body, n=n, rings=rings, jit=jit, seed=seed, flat_bottom=flat, ms=ms)


def palm(b, x, y, h, trunk, leaf, rnd, z0=0.0, lean=0.18, fronds=6):
    """Palmera de pocos polígonos: tronco curvo de 3 tramos y hojas planas dobles."""
    d = rnd.uniform(0, 2 * math.pi)
    ox, oy = math.cos(d) * lean * h, math.sin(d) * lean * h
    pts = [Vector((x, y, z0)), Vector((x + ox * 0.35, y + oy * 0.35, z0 + h * 0.45)),
           Vector((x + ox * 0.75, y + oy * 0.75, z0 + h * 0.8)), Vector((x + ox, y + oy, z0 + h))]
    for a, c, r0, r1 in zip(pts[:-1], pts[1:], (0.4, 0.33, 0.27), (0.33, 0.27, 0.2)):
        b.prism_between(a, c, r0 * h / 8, r1 * h / 8, trunk, n=4, caps=False)
    top = pts[-1]
    L = h * 0.5
    for i in range(fronds):
        a = i / fronds * 2 * math.pi + rnd.uniform(-0.2, 0.2)
        dv = Vector((math.cos(a), math.sin(a), 0))
        side = Vector((-dv.y, dv.x, 0)) * L * 0.16
        mid = top + dv * L * 0.55 + Vector((0, 0, L * 0.22))
        tip = top + dv * L + Vector((0, 0, -L * 0.28))
        b.quad(top, mid - side, tip, mid + side, leaf)
        b.quad(top, mid + side, tip, mid - side, leaf)


def tree_round(b, x, y, h, trunk, leaves, rnd, seed=0, z0=0.0, crown=0.42):
    b.cyl(x, y, z0, h * 0.05, h * 0.035, h * 0.55, trunk, n=5)
    mat = leaves if not isinstance(leaves, (list, tuple)) else rnd.choice(leaves)
    b.blob((x, y, z0 + h * 0.75), (h * crown, h * crown, h * 0.3), mat, n=7, rings=3, jit=0.2, seed=seed)


def hull(b, x, y, z, L, W, H, yaw, body, deck, trim=None):
    b.push((x, y, z), yaw)
    top = [Vector(p) for p in ((-L / 2, -W / 2, H * 0.7), (L * 0.2, -W / 2, H * 0.7), (L / 2, 0, H * 0.7 + H * 0.15),
                               (L * 0.2, W / 2, H * 0.7), (-L / 2, W / 2, H * 0.7))]
    bot = [Vector(p) for p in ((-L * 0.45, -W * 0.3, -H * 0.3), (L * 0.2, -W * 0.3, -H * 0.3), (L * 0.46, 0, -H * 0.2),
                               (L * 0.2, W * 0.3, -H * 0.3), (-L * 0.45, W * 0.3, -H * 0.3))]
    b.loft([bot, top], body, top=False, bottom=True)
    b.poly(top, deck)
    if trim is not None:
        b.box(0, 0, H * 0.45, L * 0.9, W * 1.01, H * 0.1, trim, taper=0.99)
    b.pop()


def sail(b, p0, p1, p2, m):
    b.tri(p0, p1, p2, m)
    b.tri(p0, p2, p1, m)


def cloud(b, x, y, z, rx, ry, rz, top, bottom, seed=0, n=9, rings=3):
    ms = [bottom] + [top] * (rings - 1)
    b.blob((x, y, z), (rx, ry, rz), top, n=n, rings=rings, jit=0.22, seed=seed, ms=ms)


def spire(b, x, y, z0, r, h, body, top=None, n=6, lean=0.0, seed=0):
    rnd = random.Random(seed)
    jit = [rnd.uniform(-0.18, 0.18) for _ in range(n)]
    a = ring(x, y, z0, r, r, n, rnd.random(), jit=jit)
    c = ring(x + lean * h, y, z0 + h * 0.8, r * 0.45, r * 0.45, n, rnd.random() * 0.2, jit=jit)
    if top is not None:
        b.loft([a, c, Vector((x + lean * h * 1.1, y, z0 + h))], body, top=False, bottom=False, ms=[body, top])
    else:
        b.loft([a, c, Vector((x + lean * h * 1.1, y, z0 + h))], body, top=False, bottom=False)
