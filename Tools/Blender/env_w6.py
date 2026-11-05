"""Escenario W6 · Reino de Julien (fiesta nocturna). docs/04 §W6 (magenta #F72585, lima #B5E48C) y docs/09 §3.
Escenario real, trono de Julien, corona gigante, luces de fiesta, globos, guirnaldas, tambores y relleno, con 3
variantes de los props comunes (globos, guirnaldas, tambores, regalos, boyas, palmeras, rocas).
blender --background --python env_w6.py [-- Stage Throne ...]
Salida: Tools/Export/Environment/W6/W6_<Nombre>.fbx (≤ 3000 triángulos, pivote en la base, frente -Y)."""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
import env_kit as k
from gs_common import Vector, Matrix
from env_kit import M, E

W = "W6"


def pal():
    return dict(mag=M("#F72585"), mag2=M("#B5179E"), lime=M("#B5E48C"), lime2=M("#76C442"), violet=M("#4A1D7A"),
                night=M("#1B1038"), night2=M("#2D1B5E"), cyan=M("#4CC9F0"), yellow=M("#FFD60A"), gold=M("#F5C431", 0.35),
                gold2=M("#C8931C", 0.4), white=M("#F7F4EC"), black=M("#1E1E24"), metal=M("#B9C2CC", 0.3),
                steel=M("#6B7280", 0.4), wood=M("#A0703F"), wood2=M("#6B4A2A"), green=M("#3FA34D"), green2=M("#2D7A3A"),
                rope=M("#D9B77A"), orange=M("#FF7A1A"), pink=M("#FF8FB8"), blue=M("#3A86FF"), red=M("#E2394B"),
                glow_mag=E("#F72585", 3.5), glow_lime=E("#B5E48C", 3.0), glow_cyan=E("#4CC9F0", 3.0),
                glow_yellow=E("#FFD60A", 3.5), glow_white=E("#FFFFFF", 3.5), glow_orange=E("#FF9F1C", 4.0),
                water=M("#3A86FF", 0.1))


def balloon_cols(C):
    return [C["mag"], C["lime"], C["cyan"], C["yellow"], C["pink"], C["blue"], C["orange"]]


# ---------------------------------------------------------------------------
# Piezas reutilizables
# ---------------------------------------------------------------------------
def truss(name, p0, p1, C, w=0.34, step=0.7, r=0.035, mat=None):
    """Cercha triangular (3 cordones + diagonales) entre dos puntos."""
    mat = mat or C["metal"]
    p0, p1 = Vector(p0), Vector(p1)
    d = (p1 - p0)
    L = d.length
    t = d.normalized()
    ref = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
    a = t.cross(ref).normalized()
    b = t.cross(a).normalized()
    offs = [a * w * 0.5 + b * (-w * 0.29), -a * w * 0.5 + b * (-w * 0.29), b * (w * 0.58)]
    out = []
    for i, o in enumerate(offs):
        out.append(gs.capsule(f"{name}_Ch{i}", p0 + o, p1 + o, r, mat, seg=5, cap=0, sub=1))
    n = max(2, int(L / step))
    for j in range(n):
        u0, u1 = j / n, (j + 1) / n
        for i in range(3):
            o0, o1 = offs[i], offs[(i + 1) % 3]
            pa = p0 + d * u0 + o0
            pb = p0 + d * u1 + o1 if j % 2 == 0 else p0 + d * u1 + o0
            if j % 2 == 0:
                out.append(gs.capsule(f"{name}_D{j}{i}", pa, pb, r * 0.7, mat, seg=4, cap=0, sub=1))
    return out


def bulb(name, loc, mat, r=0.09):
    return gs.ellipsoid(name, loc, (r, r, r * 1.2), mat, seg=6, rings=4)


def swag(a, b, sag, n=12):
    """Curva colgante entre dos puntos."""
    a, b = Vector(a), Vector(b)
    return [a.lerp(b, i / n) - Vector((0, 0, sag * math.sin(math.pi * i / n))) for i in range(n + 1)]


def post(name, loc, h, C, r=0.1, mat=None, cap=None):
    x, y, z = loc
    out = [gs.capsule(name, (x, y, z), (x, y, z + h), r, mat or C["wood2"], seg=6, cap=1, sub=1)]
    if cap is not None:
        out.append(gs.ellipsoid(name + "_Cap", (x, y, z + h + r), r * 1.4, cap, seg=8, rings=5))
    return out


def crown_flat(name, loc, w, h, mat, gem, th=0.12):
    """Corona plana de 5 puntas (emblema), frente hacia -Y."""
    pts = [(-w / 2, 0), (w / 2, 0), (w / 2, h * 0.55), (w * 0.3, h * 0.35), (w * 0.25, h), (w * 0.12, h * 0.4),
           (0, h * 0.95), (-w * 0.12, h * 0.4), (-w * 0.25, h), (-w * 0.3, h * 0.35), (-w / 2, h * 0.55)]
    c = k.extrude(name, pts, -th / 2, th / 2, [mat])
    c.data.transform(Matrix.Translation(Vector(loc)))
    out = [c]
    for i, (x, z) in enumerate(((0, h * 0.98), (-w * 0.25, h * 1.02), (w * 0.25, h * 1.02))):
        out.append(gs.ellipsoid(f"{name}_Gem{i}", Vector(loc) + Vector((x, 0, z)), w * 0.06, gem, seg=6, rings=4))
    out.append(gs.ellipsoid(name + "_Jewel", Vector(loc) + Vector((0, -th * 0.5, h * 0.2)), w * 0.07, gem, seg=6,
                            rings=4))
    return out


def real_crown(C, width, loc=(0, 0, 0), rot=0.0):
    """Corona dorada de los cosméticos (H08), escalada al ancho pedido."""
    import cosmetics
    c = cosmetics.build_hat("H08_GoldenCrown")
    lo, hi = gs._world_bbox([c])
    s = width / max(hi.x - lo.x, 1e-3)
    c.scale = (s, s, s)
    c.rotation_euler = (0, 0, math.radians(rot))
    c.location = Vector(loc)
    return c


def drape(name, x0, x1, y, z0, z1, mat, waves=4, amp=0.16, th=0.07, n=16):
    prof = [(x0 + (x1 - x0) * i / n, amp * math.sin(i / n * waves * 2 * math.pi)) for i in range(n + 1)]
    prof += [(x, z + th) for x, z in reversed(prof)]
    return k.sweep(name, prof, [(0, y, z0), (0, y, z1)], [mat], smooth=False)


def disc(name, loc, r, h, mat, axis=(0, 0, 1), seg=10):
    """Disco/cilindro simple (3 tramos) para detalles pequeños."""
    return gs.lathe(name, [(0.0, 0.0), (r, 0.0), (r, h), (0.0, h)], mat, loc=loc, axis=axis, seg=seg)


def spot_head(name, loc, aim, C, size=1.0, lens=None):
    """Foco de escenario: cuerpo cónico, tapa y lente emisiva mirando hacia 'aim'."""
    loc, aim = Vector(loc), Vector(aim)
    d = (aim - loc).normalized()
    out = [gs.cone(name + "_Body", loc, loc + d * 0.55 * size, 0.14 * size, 0.24 * size, C["night"], seg=8,
                   round_tip=False),
           disc(name + "_Lens", loc + d * 0.55 * size, 0.2 * size, 0.04 * size, lens or C["glow_white"], axis=d, seg=8)]
    return out


def speaker_box(name, loc, size, C, trim, cone_r=0.34, facing=-1):
    x, y, z = loc
    sx, sy, sz = size
    out = [gs.rounded_box(name + "_Box", loc, size, 0.06, [C["night"]], segments=1)]
    fy = y + facing * (sy / 2 + 0.01)
    out.append(disc(name + "_Woofer", (x, fy, z - sz * 0.12), cone_r, 0.06 * facing, C["steel"], axis=(0, 1, 0), seg=12))
    out.append(disc(name + "_WooferCap", (x, fy + facing * 0.06, z - sz * 0.12), cone_r * 0.45, 0.04 * facing, C["black"],
                    axis=(0, 1, 0), seg=8))
    out.append(disc(name + "_Tweeter", (x, fy, z + sz * 0.28), cone_r * 0.36, 0.05 * facing, C["steel"], axis=(0, 1, 0), seg=8))
    out.append(gs.rounded_box(name + "_Trim", (x, fy, z + sz * 0.5 - 0.03), (sx * 0.98, 0.04, 0.05), 0.01, trim,
                              segments=1))
    return out


def tiki(name, loc, h, C, flame=True, glow=None, rings=True):
    x, y, z = loc
    out = [gs.capsule(name + "_Pole", (x, y, z), (x, y, z + h), 0.07, C["wood2"], seg=6, cap=1, sub=1)]
    out.append(gs.lathe(name + "_Bowl", [(0.0, 0.0), (0.12, 0.0), (0.26, 0.2), (0.28, 0.26), (0.0, 0.22)], C["mag2"],
                        loc=(x, y, z + h), seg=10))
    if flame:
        out.append(gs.lathe(name + "_Flame", [(0.0, 0.0), (0.17, 0.02), (0.15, 0.25), (0.07, 0.5), (0.0, 0.7)],
                            glow or C["glow_orange"], loc=(x, y, z + h + 0.18), seg=8))
    for i, zz in enumerate((0.25, 0.55) if rings else ()):
        out.append(gs.torus(f"{name}_Ring{i}", (x, y, z + zz * h), 0.09, 0.025, C["lime"] if i else C["mag"], seg=10,
                            mseg=4))
    return out


def confetti(loc, spread, n, C, seed=0, r=0.07):
    rnd = random.Random(seed)
    cols = [C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["glow_yellow"]]
    out = []
    for i in range(n):
        p = Vector(loc) + Vector((rnd.uniform(-1, 1) * spread[0], rnd.uniform(-1, 1) * spread[1],
                                  rnd.uniform(-1, 1) * spread[2]))
        out.append(gs.rounded_box(f"Conf{i}", p, (r * 1.6, r * 0.12, r), 0.01, cols[i % 4], segments=1,
                                  rot=[("X", rnd.uniform(0, 180)), ("Z", rnd.uniform(0, 180))]))
    return out


def faceted_ball(name, loc, r, mats, seed=0, subdiv=2):
    """Bola de espejos: icosfera de caras planas con colores repartidos al azar."""
    import bmesh
    rnd = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=r)
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    for f in bm.faces:
        f.material_index = rnd.choice([0, 0, 1, 2, 3])
    return gs._obj_from_bm(name, bm, mats, smooth=False)


# ---------------------------------------------------------------------------
# Hitos
# ---------------------------------------------------------------------------
def stage(S, name):
    """Escenario real (14 x 7 m): tarima, arco de proscenio con cerchas, pantalla LED con corona, cortinas, focos,
    altavoces, cabina de DJ y escalera. Focos laterales móviles (SpotL / SpotR, pivote en la base)."""
    C = pal()
    Wd, D, H = 14.0, 7.0, 1.2
    deck = gs.rounded_box("Deck", (0, 0, H / 2), (Wd, D, H), 0.12, [C["night2"], C["mag"]], segments=1)
    gs.paint_faces(deck, lambda c: c.y < -D / 2 + 0.25 and c.z < H - 0.2, 1)
    P = [deck]
    for i in range(7):  # luces de pie a lo largo del borde
        P.append(bulb(f"Foot{i}", (-6.0 + 2.0 * i, -D / 2 - 0.05, H + 0.06), C["glow_lime"] if i % 2 else C["glow_mag"], 0.1))
    # escalera central
    for i in range(3):
        P.append(gs.rounded_box(f"Step{i}", (0, -D / 2 - 0.45 - (2 - i) * 0.9 + 0.0, 0.2 + i * 0.4 - 0.0),
                                (4.8, 0.9, 0.4), 0.05, C["mag2"] if i % 2 else C["night"], segments=1))
    # arco de proscenio: dos torres de cercha y viga superior
    for s in (1, -1):
        P += truss(f"Tower{s}", (6.7 * s, -D / 2 + 0.6, H), (6.7 * s, -D / 2 + 0.6, H + 7.4), C, w=0.5, step=1.5)
    P += truss("Beam", (-6.7, -D / 2 + 0.6, H + 7.4), (6.7, -D / 2 + 0.6, H + 7.4), C, w=0.5, step=1.5)
    for i in range(5):  # focos colgados de la viga
        col = (C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["glow_yellow"])[i % 4]
        P += spot_head(f"Par{i}", (-5.0 + i * 2.5, -D / 2 + 0.6, H + 7.1), (-5.0 + i * 2.5, -1.5, H + 1.0), C, 0.9, col)
    # fondo: marco de la pantalla LED con corona
    P.append(gs.rounded_box("ScreenFrame", (0, D / 2 - 0.5, H + 3.4), (12.6, 0.5, 6.0), 0.15, C["violet"], segments=1))
    scr = gs.rounded_box("Screen", (0, D / 2 - 0.82, H + 3.4), (11.8, 0.16, 5.3), 0.05, [C["glow_mag"], C["glow_lime"]],
                         segments=1)
    gs.bands_by_plane(scr, (1, 0, 0.4), (0, 0, 0), 1.6, 0.5, 1, cut=True)
    P.append(scr)
    P += crown_flat("Emblem", (0, D / 2 - 0.95, H + 2.3), 3.4, 2.4, C["gold"], C["glow_cyan"])
    # cortinas laterales con pliegues y recogidas
    for s in (1, -1):
        x0, x1 = (4.8, 6.6) if s == 1 else (-6.6, -4.8)
        P.append(drape(f"Curtain{s}", x0, x1, -D / 2 + 1.0, H, H + 7.0, C["mag"], waves=5, amp=0.18))
        P.append(disc(f"Tie{s}", (5.8 * s, -D / 2 + 1.0, H + 3.0), 0.12, 0.5, C["gold"], axis=(1, 0, 0), seg=6))
    P.append(drape("Valance", -4.8, 4.8, -D / 2 + 0.9, H + 6.5, H + 7.2, C["mag2"], waves=7, amp=0.1))
    # altavoces a los lados y cabina de DJ al centro
    for s in (1, -1):
        P += speaker_box(f"Spk{s}", (5.6 * s, -1.2, H + 1.0), (1.4, 1.0, 2.0), C, C["lime"], 0.42)
    P.append(gs.rounded_box("Riser", (0, 1.4, H + 0.2), (4.2, 2.4, 0.4), 0.06, C["violet"], segments=1))
    P.append(gs.rounded_box("DJTable", (0, 1.5, H + 0.85), (3.0, 0.9, 0.9), 0.08, [C["night"], C["glow_lime"]], segments=1))
    for s in (1, -1):
        P.append(disc(f"Deck{s}", (0.9 * s, 1.45, H + 1.3), 0.38, 0.08, C["black"], seg=12))
        P.append(disc(f"Disc{s}", (0.9 * s, 1.45, H + 1.38), 0.3, 0.03, C["glow_mag"], seg=12))
    P.append(gs.rounded_box("Mixer", (0, 1.45, H + 1.34), (0.7, 0.55, 0.1), 0.03, C["steel"], segments=1))
    moving = []
    for s, nm in ((1, "SpotL"), (-1, "SpotR")):
        piv = (6.2 * s, -D / 2 + 0.9, H + 1.0)
        parts = [disc(f"{nm}_Base", piv, 0.28, 0.2, C["steel"], seg=8)]
        parts += spot_head(nm, (piv[0], piv[1], piv[2] + 0.5), (piv[0] * 0.3, piv[1] - 3, piv[2] + 3.5), C, 1.1,
                           C["glow_yellow"])
        moving.append((nm, piv, parts))
    k.finish(S, name, P, moving=moving, view=(0.6, -1, 0.4), margin=1.05)


def throne(S, name):
    """Hito: trono de Julien sobre una tarima de tres escalones, respaldo dorado con corona, cojines magenta,
    abanicos de hojas de palma, antorchas y regalos de fruta."""
    C = pal()
    P = []
    for i, (w, d) in enumerate(((6.2, 5.0), (4.9, 3.9), (3.6, 2.9))):
        P.append(gs.rounded_box(f"Step{i}", (0, 0, 0.2 + 0.4 * i), (w, d, 0.4), 0.08, [C["violet"], C["mag"]], segments=1))
    P.append(gs.rounded_box("Seat", (0, 0.2, 1.55), (1.9, 1.5, 0.75), 0.1, C["gold"], segments=1))
    P.append(gs.rounded_box("Cushion", (0, -0.05, 2.0), (1.6, 1.25, 0.28), 0.12, C["mag"], bulge=0.1, segments=1))
    P.append(gs.rounded_box("Back", (0, 0.85, 3.4), (2.0, 0.4, 3.2), 0.12, C["gold"], segments=1))
    P.append(gs.ellipsoid("BackTop", (0, 0.85, 5.0), (1.0, 0.2, 0.75), C["gold"], seg=14, rings=8))
    P.append(gs.rounded_box("BackPad", (0, 0.62, 3.45), (1.5, 0.12, 2.6), 0.1, C["mag"], bulge=0.1, segments=1))
    P.append(gs.ellipsoid("BackPadTop", (0, 0.62, 4.75), (0.75, 0.1, 0.55), C["mag"], seg=12, rings=6))
    for s in (1, -1):
        P.append(gs.rounded_box(f"Arm{s}", (1.15 * s, 0.1, 2.25), (0.4, 1.4, 0.3), 0.08, C["gold"], segments=1))
        P.append(gs.ellipsoid(f"ArmBall{s}", (1.15 * s, -0.7, 2.28), 0.27, C["gold2"], seg=8, rings=5))
        P.append(gs.rounded_box(f"ArmSupport{s}", (1.15 * s, -0.4, 1.8), (0.3, 0.3, 0.7), 0.05, C["gold"], segments=1))
        P.append(gs.ellipsoid(f"Gem{s}", (0.85 * s, 0.62, 5.0), 0.2, C["glow_cyan"], seg=8, rings=5))
        P.append(gs.ellipsoid(f"GemB{s}", (0.95 * s, 0.62, 3.6), 0.2, C["glow_lime"], seg=8, rings=5))
        P += tiki(f"Torch{s}", (2.5 * s, -1.0, 1.2), 3.0, C, rings=False)
    P.append(gs.ellipsoid("GemTop", (0, 0.62, 5.35), 0.28, C["glow_mag"], seg=8, rings=5))
    P.append(real_crown(C, 1.7, (0, 0.85, 5.55)))
    # abanico de hojas detrás
    for i in range(5):
        a = math.radians(-64 + i * 32)
        base = Vector((0, 1.3, 2.6))
        tip = base + Vector((math.sin(a) * 3.4, 0.5, math.cos(a) * 3.6 + 1.0))
        P.append(k.leaf(f"Fan{i}", [base, base.lerp(tip, 0.5) + Vector((0, 0.3, 0.2)), tip], 0.38,
                        C["lime2"] if i % 2 else C["green"], sub=2))
    P.append(gs.rounded_cylinder("Stool", (0, -1.6, 1.0), 0.55, 0.35, 0.1, C["mag"], seg=12, steps=1))
    P.append(gs.rounded_cylinder("StoolBase", (0, -1.6, 0.85), 0.45, 0.2, 0.05, C["gold"], seg=12, steps=1))
    for i, (x, y) in enumerate(((2.0, -1.2), (-2.1, -0.8))):  # regalos al pie de la tarima
        P.append(gs.ellipsoid(f"Mango{i}", (x * 1.1, y - 0.5, 1.38 + 0.0), (0.2, 0.17, 0.26), C["orange"], seg=8, rings=5))
    k.finish(S, name, P, view=(0.55, -1, 0.4), margin=1.05)


def giant_crown(S, name):
    """Hito W6-5: corona gigante (≈ 6 m) con gemas sobre un zócalo; la gema de arriba gira (Jewel)."""
    C = pal()
    R = 3.0
    band = gs.lathe("Band", [(R * 0.92, 0.0), (R, 0.0), (R * 1.04, 0.5), (R * 1.0, 1.4), (R * 0.9, 1.4), (R * 0.9, 0.0)],
                    [C["gold"]], seg=30)
    P = [band]
    n = 6
    for i in range(n):
        a = i / n * 2 * math.pi
        c, s = math.cos(a), math.sin(a)
        p0, p1 = Vector((R * c, R * s, 1.2)), Vector((R * 1.08 * c, R * 1.08 * s, 3.6))
        P.append(gs.cone(f"Spike{i}", p0, p1, 0.42, 0.16, C["gold"], seg=10))
        P.append(gs.ellipsoid(f"Ball{i}", p1 + Vector((0, 0, 0.12)), 0.24, C["glow_cyan"] if i % 2 else C["glow_mag"], seg=10, rings=6))
        # gemas del aro
        q = Vector((R * 1.05 * c, R * 1.05 * s, 0.7))
        P.append(gs.ellipsoid(f"Gem{i}", q, (0.36, 0.36, 0.36), (C["glow_mag"], C["glow_lime"], C["glow_cyan"])[i % 3],
                              seg=8, rings=5))
    for i in range(12):
        a = (i + 0.5) / 12 * 2 * math.pi
        P.append(gs.ellipsoid(f"Pearl{i}", (R * 1.02 * math.cos(a), R * 1.02 * math.sin(a), 1.25), 0.17, C["white"], seg=6, rings=4))
    P.append(gs.torus("Rim", (0, 0, 0.05), R * 0.98, 0.14, C["gold2"], seg=32, mseg=6))
    P.append(gs.lathe("Cushion", [(0.0, 0.55), (R * 0.9, 0.55), (R * 0.8, 1.2), (R * 0.4, 1.8), (0.0, 1.9)], C["mag"],
                      seg=24))
    jewel = [gs.ellipsoid("Jewel", (0, 0, 2.2), (0.5, 0.5, 0.65), C["glow_yellow"], seg=8, rings=6),
             gs.torus("JewelRing", (0, 0, 2.2), 0.45, 0.06, C["gold2"], seg=12, mseg=4)]
    k.finish(S, name, P, moving=[("Jewel", (0, 0, 2.2), jewel)], view=(0.55, -1, 0.5), margin=1.05)


def disco_ball(S, name):
    """Bola de espejos colgante: la bola (Ball) gira sobre su eje; cadena y placa de techo fijas."""
    C = pal()
    P = [gs.rounded_cylinder("Plate", (0, 0, 4.0), 0.3, 0.12, 0.04, C["steel"], seg=10, steps=1)]
    for i in range(4):
        P.append(gs.torus(f"Link{i}", (0, 0, 3.7 - 0.28 * i), 0.07, 0.02, C["metal"], axis=(1, 0, 0) if i % 2 else (0, 1, 0), seg=8, mseg=4))
    ball = [faceted_ball("Ball", (0, 0, 1.4), 1.4, [C["metal"], C["pink"], C["cyan"], C["lime"]], seed=4, subdiv=2),
            gs.rounded_cylinder("Cap", (0, 0, 2.82), 0.18, 0.12, 0.04, C["steel"], seg=8, steps=1)]
    k.finish(S, name, P, moving=[("Ball", (0, 0, 1.4), ball)], view=(0.6, -1, 0.3), margin=1.1)


def light_truss(S, name):
    """Cercha de luces entre dos postes (9 m): 5 focos giratorios (Head_0..4, pivote en el soporte)."""
    C = pal()
    P = []
    for s in (1, -1):
        P += truss(f"Post{s}", (4.6 * s, 0, 0), (4.6 * s, 0, 5.2), C, w=0.5, step=1.0)
        P.append(gs.rounded_box(f"Foot{s}", (4.6 * s, 0, 0.1), (1.2, 1.2, 0.2), 0.05, C["steel"], segments=1))
    P += truss("Beam", (-4.6, 0, 5.2), (4.6, 0, 5.2), C, w=0.5, step=0.9)
    moving = []
    cols = [C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["glow_yellow"], C["glow_mag"]]
    for i in range(5):
        x = -3.6 + 1.8 * i
        parts = [gs.capsule(f"Arm{i}", (x, 0, 5.0), (x, 0, 4.7), 0.05, C["steel"], seg=5, cap=1, sub=1)]
        parts += spot_head(f"Head{i}", (x, 0, 4.6), (x + (i - 2) * 0.8, -1.5, 3.0), C, 0.9, cols[i])
        moving.append((f"Head_{i}", (x, 0, 4.9), parts))
    k.finish(S, name, P, moving=moving, view=(0.35, -1, 0.15), margin=1.05)


def searchlight(S, name):
    """Reflector de búsqueda: base, horquilla y foco; el conjunto Lamp+Beam gira y el haz es una pieza emisiva."""
    C = pal()
    P = [gs.rounded_cylinder("Base", (0, 0, 0.25), 0.9, 0.5, 0.12, [C["night2"]], seg=12, steps=2),
         gs.rounded_cylinder("Plate", (0, 0, 0.55), 0.75, 0.1, 0.03, C["mag"], seg=12, steps=1),
         gs.rounded_cylinder("Mast", (0, 0, 1.0), 0.22, 0.8, 0.05, C["steel"], seg=8, steps=1)]
    for s in (1, -1):
        P.append(gs.capsule(f"Fork{s}", (0.55 * s, 0, 1.3), (0.55 * s, 0, 2.1), 0.07, C["steel"], seg=6, cap=1, sub=1))
    P.append(gs.rounded_box("Yoke", (0, 0, 1.3), (1.2, 0.2, 0.2), 0.05, C["steel"], segments=1))
    lamp = [gs.rounded_cylinder("Lamp", (0, 0, 2.1), 0.55, 1.3, 0.1, C["night"], axis=(0, -1, 0.5), seg=14, steps=2),
            gs.rounded_cylinder("Lens", (0, -0.62, 2.43), 0.5, 0.06, 0.02, C["glow_white"], axis=(0, -1, 0.5), seg=14, steps=1),
            gs.rounded_cylinder("Rim", (0, -0.55, 2.38), 0.6, 0.12, 0.04, C["mag"], axis=(0, -1, 0.5), seg=14, steps=1)]
    beam = [gs.cone("BeamCone", (0, -0.7, 2.45), (0, -5.2, 7.2), 0.45, 1.3, C["glow_white"], seg=12, round_tip=False)]
    k.finish(S, name, P, moving=[("Lamp", (0, 0, 2.1), lamp), ("Beam", (0, -0.7, 2.45), beam)],
             view=(0.7, -1, 0.35), margin=1.05)


def speaker_stack(S, name):
    C = pal()
    P = []
    for j, (w, h) in enumerate(((1.8, 1.4), (1.6, 1.2), (1.4, 1.0))):
        z = sum(hh for _, hh in ((1.8, 1.4), (1.6, 1.2), (1.4, 1.0))[:j]) + h / 2
        P += speaker_box(f"Spk{j}", (0, 0, z), (w, 1.0, h), C, C["mag"] if j % 2 else C["lime"], 0.38 - 0.03 * j)
    P.append(gs.rounded_box("Base", (0, 0, 0.06), (2.0, 1.2, 0.12), 0.03, C["steel"], segments=1))
    P += tiki("Beacon", (0, 0.2, 3.6), 0.4, C, flame=False)
    P.append(gs.ellipsoid("BeaconLight", (0, 0.2, 4.15), 0.14, C["glow_mag"], seg=8, rings=5))
    k.finish(S, name, P)


def confetti_cannon(S, name):
    C = pal()
    P = [gs.rounded_cylinder("Base", (0, 0, 0.1), 0.6, 0.2, 0.05, C["night2"], seg=10, steps=1)]
    P.append(gs.capsule("Leg1", (0, 0, 0.2), (-0.4, 0.4, 0.95), 0.05, C["steel"], seg=5, cap=1, sub=1))
    P.append(gs.capsule("Leg2", (0, 0, 0.2), (0.4, 0.4, 0.95), 0.05, C["steel"], seg=5, cap=1, sub=1))
    P.append(gs.lathe("Barrel", [(0.0, 0.0), (0.22, 0.0), (0.3, 0.9), (0.36, 1.25), (0.0, 1.25)], [C["mag"]],
                      loc=(0, 0.3, 0.9), axis=(0, -0.5, 1), seg=14))
    P.append(gs.torus("Hoop", (0, 0.0, 1.45), 0.31, 0.04, C["lime"], axis=(0, -0.5, 1), seg=14, mseg=5))
    P.append(gs.torus("Hoop2", (0, -0.12, 1.75), 0.34, 0.04, C["gold"], axis=(0, -0.5, 1), seg=14, mseg=5))
    P += confetti((0, -1.4, 3.0), (1.4, 1.0, 1.1), 30, C, seed=3)
    k.finish(S, name, P, view=(0.7, -1, 0.35))


def tiki_torch(S, name):
    C = pal()
    P = tiki("Torch", (0, 0, 0), 2.6, C)
    P.append(gs.rounded_cylinder("Base", (0, 0, 0.08), 0.35, 0.16, 0.04, C["mag2"], seg=8, steps=1))
    P.append(gs.ellipsoid("Glow", (0, 0, 2.95), (0.22, 0.22, 0.4), C["glow_yellow"], seg=8, rings=5))
    k.finish(S, name, P)


def banner(S, name):
    """Pancarta sin texto entre dos postes (8 m): paño con corona y franjas, bolas doradas arriba."""
    C = pal()
    P = []
    for s in (1, -1):
        P += post(f"Post{s}", (4.2 * s, 0, 0), 5.0, C, 0.12, C["gold2"], C["gold"])
    P.append(gs.capsule("Rod", (-4.2, 0, 4.7), (4.2, 0, 4.7), 0.07, C["gold2"], seg=6, cap=1, sub=1))
    f = k.extrude("Cloth", [(-3.9, 1.35), (3.9, 1.35), (3.9, 4.65), (-3.9, 4.65)], -0.04, 0.04, [C["mag"]])
    P.append(f)
    for i, x in enumerate((-3.0, 3.0)):
        P.append(gs.rounded_box(f"Stripe{i}", (x, -0.06, 3.0), (1.0, 0.05, 3.1), 0.01, C["lime"], segments=1))
    P += crown_flat("Crown", (0, -0.07, 2.4), 2.4, 1.7, C["gold"], C["glow_cyan"], th=0.1)
    for i in range(7):
        x = -3.6 + 1.2 * i
        P.append(gs.ellipsoid(f"Tassel{i}", (x, 0, 1.15), (0.14, 0.14, 0.24), C["gold"] if i % 2 else C["lime"], seg=6, rings=4))
    k.finish(S, name, P, view=(0.35, -1, 0.15))


def buffet(S, name):
    """Mesa de buffet con mantel magenta, pirámide de mangos, bebidas con sombrilla y un pastel."""
    C = pal()
    P = [gs.rounded_box("Top", (0, 0, 1.0), (4.0, 1.5, 0.12), 0.04, C["wood"], segments=1)]
    P.append(gs.rounded_box("Cloth", (0, 0, 0.55), (4.1, 1.6, 0.9), 0.1, [C["mag"], C["white"]], segments=1))
    gs.paint_faces(P[-1], lambda c: c.z < 0.25, 1)
    for i, (x, y, z) in enumerate(((-0.4, 0, 1.15), (0.4, 0, 1.15), (0, 0.38, 1.15), (0, -0.38, 1.15), (0, 0, 1.55))):
        P.append(gs.ellipsoid(f"Mango{i}", (x * 0.8 - 1.3, y, z + 0.1), (0.2, 0.17, 0.27), C["orange"] if i % 2 else C["yellow"], seg=8, rings=5))
    for i in range(3):  # vasos con sombrilla
        x = 0.4 + 0.35 * i
        P.append(gs.rounded_cylinder(f"Cup{i}", (x, -0.35, 1.25), 0.11, 0.3, 0.04, C["glow_lime"] if i % 2 else C["glow_mag"], seg=8, steps=1))
        P.append(gs.capsule(f"Straw{i}", (x, -0.35, 1.35), (x + 0.04, -0.33, 1.7), 0.015, C["white"], seg=4, cap=1, sub=1))
        P.append(gs.cone(f"Umb{i}", (x + 0.02, -0.4, 1.58), (x + 0.02, -0.4, 1.75), 0.26, 0.01, [C["pink"], C["cyan"], C["yellow"]][i], seg=8,
                         round_tip=False))
    cake = [gs.rounded_cylinder("CakeA", (1.4, 0.1, 1.28), 0.4, 0.4, 0.06, C["white"], seg=12, steps=1),
            gs.rounded_cylinder("CakeB", (1.4, 0.1, 1.62), 0.28, 0.3, 0.05, C["mag"], seg=12, steps=1),
            gs.cone("Candle", (1.4, 0.1, 1.78), (1.4, 0.1, 2.0), 0.03, 0.02, C["yellow"], seg=5)]
    P += cake
    P.append(gs.ellipsoid("Flame", (1.4, 0.1, 2.08), (0.04, 0.04, 0.07), C["glow_orange"], seg=6, rings=4))
    for s in (1, -1):
        P.append(gs.rounded_box(f"Leg{s}", (1.9 * s, 0, 0.1), (0.2, 1.4, 0.2), 0.03, C["wood2"], segments=1))
    k.finish(S, name, P)


def mango_statue(S, name):
    """Estatua: mango dorado con corona sobre un pedestal de escalones; sale chispas (gemas) alrededor."""
    C = pal()
    P = [gs.rounded_box("Plinth0", (0, 0, 0.2), (2.4, 2.4, 0.4), 0.08, C["violet"], segments=1),
         gs.rounded_box("Plinth1", (0, 0, 0.6), (1.8, 1.8, 0.4), 0.08, C["mag"], segments=1),
         gs.rounded_cylinder("Plinth2", (0, 0, 1.25), 0.7, 0.9, 0.1, [C["night2"], C["gold"]], seg=12, steps=2)]
    m = gs.ellipsoid("Mango", (0, 0, 2.5), (0.7, 0.6, 0.95), C["gold"], seg=16, rings=10)
    for v in m.data.vertices:
        v.co.x += 0.14 * ((v.co.z - 2.5) / 0.95) ** 2 - 0.05
    P.append(m)
    P += [gs.tube("Stem", [(0.0, 0, 3.4), (0.06, 0, 3.6), (0.2, 0, 3.7)], 0.05, C["lime2"], seg=6, cap=2, sub=2),
          k.leaf("Leaf", [(0.15, 0, 3.55), (0.6, 0, 3.8), (1.1, 0, 3.65)], 0.22, C["lime2"], sub=2)]
    P.append(real_crown(C, 0.9, (0.02, -0.0, 3.55)))
    rnd = random.Random(2)
    for i in range(8):
        a = i / 8 * 2 * math.pi
        P.append(gs.ellipsoid(f"Spark{i}", (1.2 * math.cos(a), 1.2 * math.sin(a), 1.5 + 1.8 * ((i * 5) % 8) / 8), 0.1,
                              (C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["glow_yellow"])[i % 4], seg=6, rings=4))
    k.finish(S, name, P, view=(0.6, -1, 0.3))


def floating_lanterns(S, name):
    """Farolillos que flotan sobre el agua (línea de agua = z 0): tres tamaños con base de nenúfar."""
    C = pal()
    P = []
    for i, (x, y, r, col) in enumerate(((0, 0, 0.5, "glow_mag"), (1.6, 0.8, 0.38, "glow_lime"), (-1.3, 0.9, 0.42, "glow_cyan"))):
        P.append(gs.rounded_cylinder(f"Float{i}", (x, y, 0.02), r * 1.5, 0.12, 0.05, C["lime2"], seg=12, steps=1))
        P.append(gs.rounded_cylinder(f"Base{i}", (x, y, 0.16), r * 1.1, 0.1, 0.03, C["gold2"], seg=10, steps=1))
        P.append(gs.ellipsoid(f"Lantern{i}", (x, y, 0.2 + r * 0.9), (r, r, r * 1.1), C[col], seg=12, rings=8))
        P.append(gs.rounded_cylinder(f"Top{i}", (x, y, 0.2 + r * 2.0), r * 0.55, 0.08, 0.03, C["gold2"], seg=10, steps=1))
        P.append(gs.torus(f"Ring{i}", (x, y, 0.2 + r * 2.1), 0.1, 0.02, C["gold"], axis=(1, 0, 0), seg=8, mseg=4))
        P.append(gs.torus(f"Band{i}", (x, y, 0.2 + r * 0.9), r * 1.03, 0.025, C["gold"], seg=14, mseg=4))
    k.finish(S, name, P, view=(0.5, -1, 0.5))


def dance_floor(S, name):
    """Pista de baile 6 x 6 m con baldosas emisivas magenta, lima y cian en tablero, borde dorado."""
    C = pal()
    P = [gs.rounded_box("Frame", (0, 0, 0.1), (6.4, 6.4, 0.2), 0.05, C["gold2"], segments=1)]
    mats = [C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["night2"]]
    for i in range(6):
        for j in range(6):
            P.append(gs.rounded_box(f"T{i}{j}", (-2.5 + i, -2.5 + j, 0.22), (0.94, 0.94, 0.06), 0.012, mats[3 if (i * 3 + j) % 5 == 0 else (i + j) % 2], segments=1))
    k.finish(S, name, P, view=(0.6, -1, 0.7))


# ---------------------------------------------------------------------------
# Comunes con 3 variantes
# ---------------------------------------------------------------------------
def balloon(name, loc, r, mat, C, rot=0.0, rib_len=1.2, curl=0.25, rib_mat=None):
    loc = Vector(loc)
    out = [gs.ellipsoid(name, loc, (r, r, r * 1.22), mat, seg=10, rings=7, rot=("Y", rot) if rot else None)]
    knot = loc - Vector((0, 0, r * 1.22))
    out.append(gs.cone(name + "_Knot", knot - Vector((0, 0, 0.08)), knot + Vector((0, 0, 0.03)), 0.06, 0.02, mat, seg=5, round_tip=False))
    pts = [knot - Vector((0, 0, 0.05 + rib_len * i / 4)) + Vector((curl * math.sin(i * 1.7), curl * 0.7 * math.cos(i * 1.7), 0)) * (i / 4)
           for i in range(5)]
    out.append(gs.tube(name + "_Rib", pts, 0.012, rib_mat or C["white"], seg=4, cap=1, sub=1))
    out.append(gs.ellipsoid(name + "_Shine", loc + Vector((-r * 0.45, -r * 0.7, r * 0.55)), (r * 0.12, r * 0.05, r * 0.2),
                            C["white"], seg=5, rings=3))
    return out


def balloons(S, name, v):
    """A: ramo de 7 globos atado a una bolsa de arena. B: arco de globos de 4 m. C: globo gigante con estrella."""
    C = pal()
    cols = balloon_cols(C)
    rnd = random.Random(10 + v)
    P = []
    if v == 0:
        P.append(gs.rounded_box("Weight", (0, 0, 0.18), (0.6, 0.5, 0.36), 0.12, C["wood"], bulge=0.1, segments=1))
        for i in range(7):
            a = i / 7 * 2 * math.pi
            rad = 0.0 if i == 0 else 0.65
            p = Vector((rad * math.cos(a), rad * math.sin(a), 3.1 + (0.6 if i == 0 else rnd.uniform(-0.3, 0.3))))
            P += balloon(f"B{i}", p, 0.42, cols[i], C, rib_len=2.4, curl=0.0, rib_mat=C["white"])
        P[0].location = (0, 0, 0)
        # cintas que reunen los globos
        for i in range(7):
            a = i / 7 * 2 * math.pi
            P.append(gs.tube(f"Tie{i}", [(0, 0, 0.36), (0.12 * math.cos(a), 0.12 * math.sin(a), 1.2),
                                         (0.3 * math.cos(a) * (0 if i == 0 else 1), 0.3 * math.sin(a) * (0 if i == 0 else 1), 2.3)],
                             0.012, C["white"], seg=4, cap=1, sub=1))
        k.finish(S, name, P)
        return
    if v == 1:
        for s in (1, -1):
            P += post(f"Post{s}", (2.0 * s, 0, 0), 0.9, C, 0.09, C["gold2"], C["gold"])
        for i in range(15):
            u = i / 14
            a = math.pi * u
            p = Vector((2.0 * math.cos(a), 0, 0.9 + 2.5 * math.sin(a)))
            P.append(gs.ellipsoid(f"B{i}", p, (0.3, 0.3, 0.3), cols[i % 7], seg=9, rings=6))
            if i % 2:
                P.append(gs.ellipsoid(f"Bs{i}", p + Vector((0, -0.2, 0.18)), (0.22, 0.22, 0.22), cols[(i + 3) % 7], seg=8, rings=5))
        P.append(gs.tube("Cord", [Vector((2.0 * math.cos(math.pi * i / 14), 0.02, 0.9 + 2.5 * math.sin(math.pi * i / 14))) for i in range(15)],
                         0.02, C["white"], seg=4, cap=1))
        k.finish(S, name, P, view=(0.35, -1, 0.15))
        return
    # C: globo gigante con forma de estrella y ancla
    P.append(gs.rounded_cylinder("Anchor", (0, 0, 0.15), 0.5, 0.3, 0.08, C["gold2"], seg=10, steps=1))
    P.append(gs.tube("Cord", [(0, 0, 0.3), (0.15, 0.05, 1.5), (-0.1, 0, 2.6)], 0.03, C["white"], seg=5, cap=1, sub=2))
    star = []
    c0 = Vector((0, 0, 4.2))
    for i in range(5):
        a = math.radians(90 + i * 72)
        tip = c0 + Vector((math.cos(a) * 1.7, 0, math.sin(a) * 1.7))
        star.append(gs.tube(f"Arm{i}", [c0, c0.lerp(tip, 0.6), tip], [0.62, 0.45, 0.18], C["mag"], seg=12, cap=3, sub=2))
    P += star
    P.append(gs.ellipsoid("Core", c0, (0.85, 0.5, 0.85), C["mag2"], seg=12, rings=8))
    P.append(gs.ellipsoid("Shine", c0 + Vector((-0.4, -0.5, 0.5)), (0.18, 0.06, 0.3), C["white"], seg=6, rings=4))
    P.append(gs.ellipsoid("Gem", c0 + Vector((0, -0.52, 0.0)), 0.16, C["glow_cyan"], seg=6, rings=4))
    k.finish(S, name, P, view=(0.4, -1, 0.2))


def garland(S, name, v):
    """A: banderines triangulares entre dos postes. B: cuerda de bombillas. C: guirnalda de hojas y flores."""
    C = pal()
    P = []
    span = 4.6
    for s in (1, -1):
        P += post(f"Post{s}", (span * s, 0, 0), 4.0, C, 0.11, C["wood2"], C["gold"])
    pts = swag((-span, 0, 3.9), (span, 0, 3.9), 1.0, 20)
    P.append(gs.tube("Rope", pts, 0.03, C["rope"] if v != 2 else C["green2"], seg=4, cap=1))
    cols = balloon_cols(C)
    if v == 0:
        for i in range(1, 20):
            p = Vector(pts[i])
            tri = [(-0.22, 0.0), (0.22, 0.0), (0.0, -0.5)]
            f = k.extrude(f"Flag{i}", tri, -0.015, 0.015, [cols[i % 5]])
            f.data.transform(Matrix.Translation(p + Vector((0, 0, -0.02))))
            P.append(f)
    elif v == 1:
        for i in range(1, 20):
            p = Vector(pts[i]) - Vector((0, 0, 0.16))
            P.append(gs.rounded_cylinder(f"Sock{i}", p + Vector((0, 0, 0.12)), 0.05, 0.08, 0.02, C["steel"], seg=6, steps=1))
            P.append(bulb(f"Bulb{i}", p, (C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["glow_yellow"])[i % 4], 0.12))
    else:
        rnd = random.Random(3)
        for i in range(1, 20):
            p = Vector(pts[i])
            a = rnd.uniform(0, 6.28)
            P.append(gs.ellipsoid(f"Leaf{i}", p + Vector((0, 0, -0.1)), (0.4, 0.12, 0.1), C["lime2"] if i % 2 else C["green"], seg=6, rings=4,
                                  rot=("Z", math.degrees(a))))
            if i % 3 == 0:
                P += k.flower(f"Fl{i}", p + Vector((0, -0.05, -0.05)), C["mag"] if i % 2 else C["pink"], C["yellow"], size=0.26, h=0.0, n=5)
    k.finish(S, name, P, view=(0.3, -1, 0.15))


def drum(S, name, v):
    """A: par de bongós sobre un soporte. B: conga alta atada con cuerda. C: bombo grande con baquetas y estrella."""
    C = pal()
    P = []
    if v == 0:
        for s, (r, h) in ((1, (0.28, 0.55)), (-1, (0.24, 0.5))):
            body = gs.lathe(f"Bongo{s}", [(0.0, 0.0), (r * 0.8, 0.0), (r, h * 0.5), (r * 1.05, h), (0.0, h)], [C["wood"], C["mag"]],
                            loc=(0.3 * s, 0, 0.55), seg=14, mat_fn=lambda i: 0)
            P.append(body)
            P.append(gs.rounded_cylinder(f"Skin{s}", (0.3 * s, 0, 0.55 + h + 0.02), r * 1.04, 0.05, 0.02, C["white"], seg=14, steps=1))
            P.append(gs.torus(f"Rim{s}", (0.3 * s, 0, 0.55 + h + 0.02), r * 1.04, 0.025, C["gold"], seg=14, mseg=4))
            P.append(gs.torus(f"Band{s}", (0.3 * s, 0, 0.55 + h * 0.55), r * 1.02, 0.03, C["mag"], seg=14, mseg=4))
        for s in (1, -1):
            for t in (1, -1):
                P.append(gs.capsule(f"Leg{s}{t}", (0.55 * s, 0.25 * t, 0.0), (0.2 * s, 0.05 * t, 0.65), 0.035, C["steel"], seg=5, cap=1, sub=1))
        P.append(gs.capsule("Bar", (-0.5, 0, 0.62), (0.5, 0, 0.62), 0.03, C["steel"], seg=5, cap=1, sub=1))
    elif v == 1:
        b = gs.lathe("Conga", [(0.0, 0.0), (0.2, 0.0), (0.28, 0.4), (0.36, 0.9), (0.38, 1.15), (0.0, 1.15)], [C["lime2"], C["wood"]], seg=16)
        gs.bands_by_plane(b, (0, 0, 1), (0, 0, 0), 0.34, 0.5, 1, cut=True)
        P.append(b)
        P.append(gs.rounded_cylinder("Skin", (0, 0, 1.17), 0.38, 0.06, 0.02, C["white"], seg=16, steps=1))
        P.append(gs.torus("Rim", (0, 0, 1.17), 0.39, 0.03, C["gold"], seg=16, mseg=4))
        P.append(gs.torus("Band", (0, 0, 0.55), 0.31, 0.03, C["mag"], seg=14, mseg=4))
        for i in range(8):
            a = i / 8 * 2 * math.pi
            P.append(gs.capsule(f"Rope{i}", (0.4 * math.cos(a), 0.4 * math.sin(a), 1.12), (0.31 * math.cos(a + 0.25), 0.31 * math.sin(a + 0.25), 0.55),
                                0.012, C["rope"], seg=4, cap=1, sub=1))
        P.append(gs.rounded_cylinder("Base", (0, 0, 0.04), 0.24, 0.08, 0.02, C["gold2"], seg=10, steps=1))
    else:
        P.append(gs.rounded_cylinder("Bass", (0, 0, 0.95), 0.8, 0.55, 0.12, [C["mag"], C["white"]], axis=(0, 1, 0), seg=20, steps=2,
                                     mat_fn=lambda i: 0))
        for s in (1, -1):
            P.append(gs.rounded_cylinder(f"Skin{s}", (0, 0.3 * s, 0.95), 0.74, 0.04, 0.015, C["white"], axis=(0, 1, 0), seg=20, steps=1))
            P.append(gs.torus(f"Hoop{s}", (0, 0.31 * s, 0.95), 0.76, 0.045, C["gold"], axis=(0, 1, 0), seg=20, mseg=4))
        P += crown_flat("Star", (0, -0.34, 0.75), 0.9, 0.6, C["gold"], C["glow_cyan"], th=0.05)
        for i in range(2):
            P.append(gs.rounded_box(f"Foot{i}", (0.5 * (1 if i else -1), 0, 0.12), (0.18, 0.9, 0.24), 0.05, C["steel"], segments=1))
        for s in (1, -1):
            P.append(gs.capsule(f"Stick{s}", (0.95 * s, -0.5, 0.1), (0.65 * s, -0.55, 1.4), 0.025, C["wood2"], seg=5, cap=1, sub=1))
            P.append(gs.ellipsoid(f"Mallet{s}", (0.65 * s, -0.55, 1.45), 0.09, C["lime"], seg=7, rings=5))
    k.finish(S, name, P)


def gift_box(S, name, v):
    """A: caja con cinta y moño. B: pila de tres regalos. C: caja redonda de sombrero con lazo."""
    C = pal()
    P = []

    def bow(c, s=1.0, mat=None):
        mat = mat or C["gold"]
        out = []
        for sd in (1, -1):
            out.append(gs.ellipsoid(f"Loop{sd}{c[2]:.2f}", Vector(c) + Vector((0.2 * s * sd, 0, 0.1 * s)), (0.2 * s, 0.07 * s, 0.14 * s), mat, seg=8, rings=5,
                                    rot=("Y", -30 * sd)))
        out.append(gs.ellipsoid(f"Knot{c[2]:.2f}", Vector(c) + Vector((0, 0, 0.02 * s)), 0.09 * s, mat, seg=7, rings=5))
        return out
    if v == 0:
        P.append(gs.rounded_box("Box", (0, 0, 0.5), (1.0, 1.0, 1.0), 0.07, C["mag"], bulge=0.03, segments=1))
        P.append(gs.rounded_box("RibbonX", (0, 0, 0.5), (0.2, 1.04, 1.04), 0.03, C["lime"], segments=1))
        P.append(gs.rounded_box("RibbonY", (0, 0, 0.5), (1.04, 0.2, 1.04), 0.03, C["lime"], segments=1))
        P += bow((0, 0, 1.05), 1.2, C["lime"])
    elif v == 1:
        for i, (z, w, col, rb) in enumerate(((0.0, 1.1, C["blue"], C["yellow"]), (0.8, 0.85, C["lime"], C["mag"]), (1.45, 0.6, C["yellow"], C["mag"]))):
            h = 0.7 if i == 0 else (0.62 if i == 1 else 0.5)
            P.append(gs.rounded_box(f"Box{i}", (0.0 + 0.05 * i, 0, z + h / 2), (w, w, h), 0.05, col, segments=1, rot=("Z", 14 * i)))
            P.append(gs.rounded_box(f"Rib{i}", (0.05 * i, 0, z + h / 2), (w * 0.2, w * 1.04, h * 1.03), 0.02, rb, segments=1, rot=("Z", 14 * i)))
        P += bow((0.1, 0, 2.0), 0.8, C["mag"])
    else:
        P.append(gs.rounded_cylinder("Hat", (0, 0, 0.4), 0.55, 0.8, 0.08, [C["cyan"], C["white"]], seg=16, steps=2, mat_fn=lambda i: 0))
        P.append(gs.rounded_cylinder("Lid", (0, 0, 0.84), 0.6, 0.12, 0.05, C["mag"], seg=16, steps=1))
        P.append(gs.torus("Ribbon", (0, 0, 0.55), 0.56, 0.05, C["gold"], seg=16, mseg=5))
        P += bow((0, 0, 0.92), 1.4, C["gold"])
    k.finish(S, name, P)


def party_buoy(S, name, v):
    C = pal()
    P, moving = k.buoy(v, C["mag"], C["white"], C["glow_lime"], C["gold2"])
    k.finish(S, name, P, moving=moving, view=(0.8, -0.75, 0.4))


def party_palm(S, name, v):
    """Palmeras con luces: A curva alta, B en V, C baja y frondosa; cadena de bombillas enrollada al tronco."""
    C = pal()
    P = k.palm(v, C["wood"], C["wood2"], [C["lime2"], C["green"]], C["gold"], seed=20 + v)
    cols = [C["glow_mag"], C["glow_lime"], C["glow_cyan"], C["glow_yellow"]]
    H = (4.2, 3.4, 2.0)[v]
    for i in range(10):
        u = i / 9
        a = u * 5.0
        r = 0.34 - 0.1 * u
        P.append(bulb(f"Bulb{i}", (r * math.cos(a) * (1.0 if v != 1 else 1.3) - (0.8 * u if v == 1 else 0.0), r * math.sin(a), 0.3 + u * H), cols[i % 4], 0.08))
    k.finish(S, name, P, margin=1.12)


def party_rock(S, name, v):
    """Rocas violetas con cristales de neón (magenta y lima) en la cima."""
    C = pal()
    P = k.rock_variant(v, [C["night2"], C["violet"]], seed=40, scale=1.0, top_index=1)
    rnd = random.Random(6 + v)
    spots = ((0.0, 0.0, 0.9), (0.4, 0.3, 0.7), (-0.5, 0.2, 0.6)) if v == 0 else ((0.1, 0.0, 2.2), (0.6, 0.35, 0.5), (-0.4, 0.3, 0.4)) if v == 1 else ((0.9, 0.5, 0.9), (0.2, 0.0, 0.5), (-0.8, 0.4, 0.4))
    for i, (x, y, z) in enumerate(spots):
        col = C["glow_mag"] if i % 2 == 0 else C["glow_lime"]
        tilt = Vector((rnd.uniform(-0.3, 0.3), rnd.uniform(-0.3, 0.3), 1.0)).normalized()
        L = rnd.uniform(0.7, 1.2)
        base = Vector((x, y, z))
        P.append(gs.cone(f"Crystal{i}", base, base + tilt * L, 0.2, 0.06, col, seg=6, round_tip=False))
        P.append(gs.cone(f"CrystalB{i}", base + Vector((0.22, 0.05, 0)), base + Vector((0.22, 0.05, 0)) + tilt * L * 0.6, 0.13, 0.04, col, seg=6, round_tip=False))
    k.finish(S, name, P)


def build_props():
    props = [("Stage", stage), ("Throne", throne), ("GiantCrown", giant_crown), ("DiscoBall", disco_ball),
             ("LightTruss", light_truss), ("Searchlight", searchlight), ("SpeakerStack", speaker_stack),
             ("ConfettiCannon", confetti_cannon), ("TikiTorch", tiki_torch), ("Banner", banner), ("Buffet", buffet),
             ("MangoStatue", mango_statue), ("FloatingLanterns", floating_lanterns), ("DanceFloor", dance_floor)]
    for base, fn in (("Balloons", balloons), ("Garland", garland), ("Drum", drum), ("GiftBox", gift_box),
                     ("Buoy", party_buoy), ("Palm", party_palm), ("Rock", party_rock)):
        for i, c in enumerate("ABC"):
            props.append((f"{base}_{c}", (lambda f, v: lambda S, n: f(S, n, v))(fn, i)))
    return props


PROPS = build_props()

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    k.run_props(W, PROPS, only=set(argv) or None, cols=7)
