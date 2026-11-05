"""Glifos 3D para iconos de interfaz (logros, modos y dificultades). Cada glifo es una lista de objetos construidos
alrededor del origen en el plano XZ (de cara a la cámara que mira hacia +Y) con radio ≈ 0.45 m; fit() los escala y
los coloca delante de una medalla o insignia. Materiales toon (gs_render) para que el contorno y las bandas de luz
sean los de las demás ilustraciones. Sin textos: los pone la UI."""
import math
import random
import gs_common as gs
import gs_render as R
import env_kit as k
from gs_common import Vector, Matrix


def tm(col, em=0.0):
    return R.toon_mat(col, emission=em)


def fit(parts, scale=1.0, offset=(0, 0, 0)):
    """Escala los objetos respecto al origen y los traslada."""
    for o in parts:
        o.scale = (scale, scale, scale)
        o.location = Vector(offset) + o.location * scale  # conserva el desplazamiento propio de la pieza
    return parts


# ---------------------------------------------------------------------------
# Polígonos extruidos
# ---------------------------------------------------------------------------
def poly(name, pts, mat, th=0.12, c=(0, 0)):
    o = k.extrude(name, [(x + c[0], z + c[1]) for x, z in pts], -th / 2, th / 2, [mat])
    for p in o.data.polygons:
        p.use_smooth = False
    return o


def star_pts(ro, ri, n=5, rot=0.0):
    return [((ro if i % 2 == 0 else ri) * math.cos(math.pi / 2 + rot + i * math.pi / n),
             (ro if i % 2 == 0 else ri) * math.sin(math.pi / 2 + rot + i * math.pi / n)) for i in range(2 * n)]


def star(name, c, ro, mat, n=5, th=0.12, ri=None, rot=0.0):
    return poly(name, star_pts(ro, ri or ro * (0.45 if n == 5 else 0.55), n, rot), mat, th, c)


def heart_pts(s=1.0, n=48, t0=0.0, t1=2 * math.pi):
    out = []
    for i in range(n + 1):
        t = t0 + (t1 - t0) * i / n
        x = 16 * math.sin(t) ** 3
        z = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        out.append((x / 36 * s, (z + 6) / 36 * s))
    return out


def crown(name, c, w, h, mat, gem, th=0.12):
    pts = [(-w / 2, 0), (w / 2, 0), (w / 2, h * 0.55), (w * 0.3, h * 0.35), (w * 0.25, h), (w * 0.12, h * 0.4),
           (0, h * 0.95), (-w * 0.12, h * 0.4), (-w * 0.25, h), (-w * 0.3, h * 0.35), (-w / 2, h * 0.55)]
    out = [poly(name, pts, mat, th, c)]
    for i, (x, z) in enumerate(((0, h * 1.0), (-w * 0.25, h * 1.04), (w * 0.25, h * 1.04))):
        out.append(gs.ellipsoid(f"{name}_G{i}", (c[0] + x, -th * 0.2, c[1] + z), w * 0.065, gem, seg=7, rings=4))
    out.append(gs.ellipsoid(name + "_J", (c[0], -th * 0.55, c[1] + h * 0.2), w * 0.08, gem, seg=7, rings=4))
    return out


def spark4(name, c, r, mat):
    a = gs.ellipsoid(name + "A", (c[0], 0, c[1]), (r, r * 0.12, r * 0.14), mat, seg=8, rings=4)
    b = gs.ellipsoid(name + "B", (c[0], 0, c[1]), (r * 0.14, r * 0.12, r), mat, seg=8, rings=4)
    return [a, b]


def note(name, c, mat, s=1.0):
    c = Vector((c[0], 0, c[1]))
    out = [gs.ellipsoid(name + "H", c, (0.1 * s, 0.07 * s, 0.075 * s), mat, seg=10, rings=6, rot=("Y", -25))]
    top = c + Vector((0.085 * s, 0, 0.34 * s))
    out.append(gs.capsule(name + "S", c + Vector((0.085 * s, 0, 0.02)), top, 0.02 * s, mat, seg=6, sub=1))
    out.append(gs.tube(name + "F", [top, top + Vector((0.1 * s, 0, -0.06 * s)), top + Vector((0.13 * s, 0, -0.18 * s))],
                       [0.032 * s, 0.028 * s, 0.012 * s], mat, seg=5, sub=2, flatten=((0, 1, 0), 0.4)))
    return out


def arc(name, c, r, a0, a1, rad, mat, n=14):
    pts = [Vector((c[0] + r * math.cos(math.radians(a0 + (a1 - a0) * i / (n - 1))), 0,
                   c[1] + r * math.sin(math.radians(a0 + (a1 - a0) * i / (n - 1))))) for i in range(n)]
    return gs.tube(name, pts, rad, mat, seg=7, cap=3, sub=1)


def speed_lines(name, x0, x1, zs, mat, rad=0.022):
    return [gs.capsule(f"{name}{i}", (x0 - 0.08 * (i % 2), 0, z), (x1 - 0.08 * (i % 2), 0, z), rad, mat, seg=6, sub=1)
            for i, z in enumerate(zs)]


# ---------------------------------------------------------------------------
# Glifos de logros y modos
# ---------------------------------------------------------------------------
def puff(c=(0, 0), s=0.7, col="#8EDB4F", n=4, seed=2):
    return R.gas_cloud((c[0], 0, c[1]), s, color=col, n=n, seed=seed)


def g_first_fart():
    P = puff((0.08, -0.02), 0.62, n=4, seed=2)
    P += speed_lines("L", -0.5, -0.27, (0.12, -0.02, -0.16), tm("#FFFFFF", 0.5))
    return P


def g_duo():
    P = puff((-0.25, -0.05), 0.4, n=3, seed=1) + puff((0.25, 0.05), 0.4, col="#6FD0F2", n=3, seed=4)
    P += spark4("Sp", (0, 0.28), 0.12, tm("#FFD60A", 0.5))
    return P


def g_orchestra():
    P = []
    for i, (x, z) in enumerate(((-0.28, -0.12), (0.28, -0.12), (-0.18, 0.26), (0.18, 0.26))):
        P += puff((x, z), 0.28, col=("#8EDB4F", "#6FD0F2", "#FFB627", "#FF4FA3")[i], n=3, seed=i)
    P += note("N", (0, -0.14), tm("#FFFFFF", 0.4), 1.25)
    return P


def g_super_fart():
    P = [star("Burst", (0, 0), 0.5, tm("#FFD60A", 0.5), n=8, th=0.1, ri=0.3), star("Burst2", (0, 0), 0.36, tm("#FF9F1C", 0.4), n=8, th=0.1, ri=0.2)]
    P[1].location = Vector((0, -0.04, 0))
    P += puff((0, -0.02), 0.34, n=4, seed=3)
    return P


def g_sling():
    wood = tm("#8A4A22")
    P = [gs.capsule("Handle", (0, 0, -0.46), (0, 0, -0.08), 0.065, wood, seg=8, sub=1)]
    P += [gs.capsule("ProngL", (0, 0, -0.1), (-0.26, 0, 0.3), 0.055, wood, seg=8, sub=1),
          gs.capsule("ProngR", (0, 0, -0.1), (0.26, 0, 0.3), 0.055, wood, seg=8, sub=1)]
    band = tm("#E2394B")
    P.append(gs.tube("Band", [(-0.26, 0, 0.3), (-0.1, -0.02, 0.17), (0.0, -0.02, 0.12), (0.1, -0.02, 0.17), (0.26, 0, 0.3)], 0.022, band, seg=6, cap=2, sub=2))
    P.append(gs.ellipsoid("Stone", (0, -0.05, 0.12), 0.085, tm("#8E9099"), seg=8, rings=5))
    P.append(k.leaf("Leaf", [(0.05, 0, -0.1), (0.25, 0, -0.18), (0.4, 0, -0.1)], 0.1, tm("#3FA34D"), sub=2))
    return P


def g_quake():
    rock, dark = tm("#B07A4A"), tm("#2D1B1B")
    P = [gs.rounded_box("GroundL", (-0.25, 0, -0.24), (0.56, 0.24, 0.3), 0.06, rock, rot=("Y", 14), segments=1),
         gs.rounded_box("GroundR", (0.25, 0, -0.3), (0.56, 0.24, 0.3), 0.06, tm("#9C6838"), rot=("Y", -12), segments=1),
         gs.rounded_box("Crack", (0, 0.06, -0.3), (0.16, 0.12, 0.3), 0.02, dark, segments=1)]
    for i, (x, z, r) in enumerate(((-0.1, 0.0, 0.09), (0.15, 0.1, 0.11), (0.0, 0.3, 0.07))):
        P.append(gs.ellipsoid(f"Chunk{i}", (x, -0.04, z), (r, r * 0.8, r * 0.9), rock, seg=6, rings=4))
    o = tm("#FF9F1C", 0.5)
    P += [arc("W1", (0, -0.3), 0.42, 35, 145, 0.04, o), arc("W2", (0, -0.3), 0.6, 45, 135, 0.04, o)]
    return P


def g_party():
    hat = gs.cone("Hat", (0.0, 0, -0.4), (0.04, 0, 0.34), 0.3, 0.02, [tm("#F72585"), tm("#B5E48C")], seg=12)
    gs.bands_by_plane(hat, (0, 0, 1), (0, 0, -0.4), 0.26, 0.5, 1, cut=True)
    P = [hat, gs.ellipsoid("Pom", (0.04, 0, 0.36), 0.08, tm("#FFD60A"), seg=8, rings=5),
         gs.torus("Brim", (0, 0, -0.4), 0.3, 0.03, tm("#FFD60A"), seg=14, mseg=4)]
    rnd = random.Random(3)
    cols = ["#F72585", "#4CC9F0", "#FFD60A", "#B5E48C"]
    for i, (x, z) in enumerate(((-0.4, 0.2), (0.4, 0.28), (-0.3, -0.1), (0.38, -0.05), (0.2, 0.42), (-0.12, 0.42))):
        P.append(gs.rounded_box(f"Conf{i}", (x, -0.05, z), (0.09, 0.015, 0.05), 0.005, tm(cols[i % 4], 0.3), segments=1,
                                rot=[("Y", rnd.uniform(0, 180))]))
    return P


def g_dumbbell():
    steel, dark = tm("#B9C2CC"), tm("#3B4350")
    P = [gs.capsule("Bar", (-0.3, 0, 0), (0.3, 0, 0), 0.05, steel, seg=8, sub=1)]
    for s in (1, -1):
        P.append(gs.rounded_cylinder(f"Big{s}", (0.3 * s, 0, 0), 0.26, 0.1, 0.03, dark, axis=(1, 0, 0), seg=14, steps=1))
        P.append(gs.rounded_cylinder(f"Small{s}", (0.42 * s, 0, 0), 0.19, 0.09, 0.03, tm("#F72585"), axis=(1, 0, 0), seg=14, steps=1))
        P.append(gs.rounded_cylinder(f"Cap{s}", (0.5 * s, 0, 0), 0.07, 0.06, 0.02, steel, axis=(1, 0, 0), seg=8, steps=1))
    return P


def g_rescue():
    P = []
    for i in range(8):
        P.append(gs.torus(f"Buoy{i}", (0, 0, -0.07), 0.3, 0.12, tm("#E2394B" if i % 2 else "#F7F4EC"), axis=(0, 1, 0), seg=4, mseg=10,
                          arc=(i * 45, i * 45 + 45)))
    P += crown("Crown", (0, 0.2), 0.42, 0.28, tm("#F5C431"), tm("#4CC9F0", 0.4))
    return P


def g_titanic():
    P = [gs.torus("Raft", (0.0, 0, 0.08), 0.3, 0.1, tm("#FF7A1A"), axis=(0.3, -0.7, 0.6), seg=18, mseg=8)]
    w = tm("#3A86FF")
    P += [gs.ellipsoid("W1", (-0.05, -0.12, -0.2), (0.5, 0.1, 0.15), w, seg=12, rings=6),
          gs.ellipsoid("W2", (0.25, -0.2, -0.1), (0.25, 0.08, 0.1), tm("#59C3FF"), seg=10, rings=5)]
    for i, (x, z, r) in enumerate(((-0.3, 0.1, 0.06), (-0.38, 0.25, 0.045), (-0.25, 0.38, 0.07))):
        P += R.bubble((x, -0.15, z), r)
    return P


def g_shield():
    pts = [(-0.34, 0.36), (0.34, 0.36), (0.34, 0.0), (0.24, -0.22), (0, -0.42), (-0.24, -0.22), (-0.34, 0.0)]
    P = [poly("Shield", pts, tm("#3A86FF"), 0.14), poly("Inner", [(x * 0.78, z * 0.78 + 0.02) for x, z in pts], tm("#59C3FF"), 0.14)]
    P[1].location = Vector((0, -0.04, 0))
    P.append(gs.tube("Check", [(-0.16, -0.08, 0.0), (-0.04, -0.08, -0.13), (0.18, -0.08, 0.17)], 0.05, tm("#FFFFFF", 0.4), seg=8, cap=3, sub=2))
    return P


def g_zoo_gate():
    iron, pink = tm("#E7E3F2"), tm("#FF4FA3", 0.5)
    P = []
    for s in (1, -1):
        P.append(gs.rounded_box(f"Pillar{s}", (0.34 * s, 0, -0.12), (0.14, 0.14, 0.68), 0.04, tm("#8F88A8"), segments=1))
        P.append(gs.ellipsoid(f"Ball{s}", (0.34 * s, 0, 0.26), 0.1, pink, seg=8, rings=5))
    P.append(arc("Arch", (0, 0.0), 0.34, 0, 180, 0.04, iron))
    for i in range(5):
        x = -0.24 + 0.12 * i
        P.append(gs.capsule(f"Bar{i}", (x, 0, -0.44), (x, 0, math.sqrt(max(0.34 ** 2 - x * x, 0)) - 0.02), 0.025, iron, seg=5, sub=1))
    P.append(gs.rounded_box("Base", (0, 0, -0.46), (0.9, 0.16, 0.06), 0.02, tm("#C9C3D8"), segments=1))
    return P


def g_lighthouse():
    tower = gs.lathe("Tower", [(0.0, -0.4), (0.24, -0.4), (0.15, 0.2), (0.0, 0.2)], [tm("#F7F4EC"), tm("#E2394B")], seg=12)
    gs.bands_by_plane(tower, (0, 0, 1), (0, 0, -0.4), 0.24, 0.5, 1, cut=True)
    P = [tower, gs.rounded_cylinder("Gallery", (0, 0, 0.22), 0.2, 0.05, 0.015, tm("#26304F"), seg=12, steps=1),
         gs.ellipsoid("Lamp", (0, 0, 0.32), (0.1, 0.1, 0.12), tm("#FFD60A", 0.7), seg=10, rings=6),
         gs.cone("Roof", (0, 0, 0.4), (0, 0, 0.55), 0.14, 0.01, tm("#E2394B"), seg=10)]
    P.append(gs.ellipsoid("Wave1", (-0.1, -0.1, -0.42), (0.5, 0.15, 0.1), tm("#2EC4B6"), seg=12, rings=6))
    P.append(gs.ellipsoid("Wave2", (0.25, -0.15, -0.34), (0.28, 0.1, 0.09), tm("#7FE6DD"), seg=10, rings=5))
    return P


def g_palm():
    P = [gs.tube("Trunk", [(0, 0, -0.45), (0.06, 0, -0.1), (0.14, 0, 0.14)], [0.07, 0.06, 0.05], tm("#9C6B3E"), seg=8, sub=3, cap=2)]
    top = Vector((0.14, 0, 0.14))
    for i in range(6):
        a = i / 6 * 2 * math.pi
        d = Vector((math.cos(a), 0, math.sin(a) * 0.55))
        P.append(k.leaf(f"Frond{i}", [top, top + d * 0.2 + Vector((0, 0, 0.12)), top + d * 0.42 + Vector((0, 0, -0.05))], 0.1,
                        tm("#2D8A4E" if i % 2 else "#3FA34D"), sub=2, droop_up=(0, -1, 0)))
    for i in range(3):
        P.append(gs.ellipsoid(f"Nut{i}", top + Vector((0.07 * (i - 1), -0.05, -0.08)), 0.055, tm("#F77F00"), seg=7, rings=4))
    P.append(gs.ellipsoid("Sand", (0, 0, -0.46), (0.4, 0.2, 0.07), tm("#F2D492"), seg=10, rings=4))
    return P


def g_volcano():
    P = [gs.cone("Mountain", (0, 0, -0.42), (0, 0, 0.2), 0.52, 0.17, tm("#5A2E6B"), seg=14, round_tip=False),
         gs.lathe("Lava", [(0.0, 0.2), (0.17, 0.2), (0.12, 0.26), (0.0, 0.26)], tm("#FF4D00", 0.9), seg=12)]
    lava = tm("#FF9F1C", 0.8)
    P.append(gs.tube("Flow", [(0.05, -0.02, 0.22), (0.1, -0.1, 0.05), (0.08, -0.2, -0.15), (0.12, -0.24, -0.38)], [0.04, 0.045, 0.05, 0.06], lava, seg=6, sub=2, cap=2))
    P.append(gs.tube("Flow2", [(-0.05, -0.02, 0.22), (-0.12, -0.1, 0.0), (-0.2, -0.2, -0.3)], [0.03, 0.035, 0.045], lava, seg=6, sub=2, cap=2))
    smoke = tm("#6B6B7B")
    for i, (x, z, r) in enumerate(((0.0, 0.36, 0.11), (0.1, 0.5, 0.09), (-0.05, 0.55, 0.07))):
        P.append(gs.ellipsoid(f"Smoke{i}", (x, 0, z), r, smoke, seg=9, rings=6))
    P.append(gs.cone("Crystal", (-0.4, 0, -0.42), (-0.36, 0, -0.18), 0.07, 0.01, tm("#4CC9F0", 0.5), seg=5, round_tip=False))
    return P


def g_arch():
    rock, rock2 = tm("#B5562A"), tm("#9C3D1C")
    P = [arc("Arch", (0, -0.1), 0.34, 0, 180, 0.12, rock, n=16)]
    P += [gs.ellipsoid("RockL", (-0.34, 0, -0.32), (0.18, 0.12, 0.2), rock2, seg=8, rings=5),
          gs.ellipsoid("RockR", (0.34, 0, -0.32), (0.18, 0.12, 0.2), rock2, seg=8, rings=5)]
    P.append(gs.ellipsoid("Sun", (0.0, 0.1, 0.0), (0.18, 0.04, 0.18), tm("#FFF1B8", 0.8), seg=12, rings=6))
    P.append(gs.ellipsoid("Ground", (0, 0, -0.45), (0.5, 0.15, 0.07), tm("#FB8500"), seg=10, rings=4))
    return P


def g_world6():
    P = crown("Crown", (0, -0.28), 0.84, 0.56, tm("#F5C431"), tm("#F72585", 0.4), th=0.16)
    P += [gs.ellipsoid(f"Gem{i}", (x, -0.1, -0.2), 0.07, tm(("#B5E48C", "#4CC9F0", "#B5E48C")[i], 0.5), seg=7, rings=4)
          for i, x in enumerate((-0.25, 0.0, 0.25))]
    P += spark4("S1", (0.4, 0.38), 0.1, tm("#B5E48C", 0.6)) + spark4("S2", (-0.42, 0.3), 0.08, tm("#F72585", 0.6))
    return P


def g_big_star():
    P = [star("Star", (0, 0.04), 0.46, tm("#FFD60A", 0.2), th=0.14)]
    for i in range(3):
        P.append(star(f"Mini{i}", ((i - 1) * 0.28, -0.4), 0.11, tm("#FFD60A", 0.2), th=0.1))
    P += spark4("Sp", (0.4, 0.36), 0.1, tm("#FFFFFF", 0.7))
    return P


def g_constellation():
    pts = [(-0.35, -0.2), (-0.15, 0.1), (0.1, 0.0), (0.3, 0.3), (0.38, -0.2), (0.05, -0.33)]
    P = []
    w = tm("#C8D6FF", 0.4)
    for i in range(len(pts) - 1):
        P.append(gs.capsule(f"Ln{i}", (pts[i][0], 0.02, pts[i][1]), (pts[i + 1][0], 0.02, pts[i + 1][1]), 0.014, w, seg=4, sub=1))
    for i, p in enumerate(pts):
        P.append(star(f"St{i}", p, 0.09 if i % 2 else 0.12, tm("#FFD60A", 0.6), th=0.08))
    return P


def mango_obj(name="Mango", c=(0, 0), s=1.0, col="#F5C431", em=0.1):
    o = gs.ellipsoid(name, (0, 0, 0), (0.24 * s, 0.2 * s, 0.31 * s), tm(col, em), seg=14, rings=9)
    for v in o.data.vertices:
        v.co.x += 0.07 * s * (v.co.z / (0.31 * s)) ** 2 - 0.02 * s
    o.data.transform(Matrix.Translation(Vector((c[0], 0, c[1]))) @ gs._rot_matrix(("Y", 18)))
    return o


def g_collector():
    P = [mango_obj("Mango", (0, -0.08), 1.25, "#F5C431", 0.25)]
    P += crown("Crown", (0.0, 0.2), 0.36, 0.22, tm("#E2394B"), tm("#FFD60A", 0.5), th=0.1)
    P.append(gs.tube("Stem", [(0.0, 0, 0.26), (0.0, 0, 0.3)], 0.03, tm("#3FA34D"), seg=5, cap=1, sub=1))
    P += spark4("S1", (-0.4, 0.3), 0.09, tm("#FFFFFF", 0.7)) + spark4("S2", (0.4, -0.05), 0.07, tm("#FFFFFF", 0.7))
    return P


def stopwatch(c=(0, 0), s=1.0, face="#F7F4EC", ring="#B9C2CC", hand="#E2394B"):
    P = [gs.torus("Ring", (c[0], 0, c[1]), 0.34 * s, 0.065 * s, tm(ring), axis=(0, 1, 0), seg=26, mseg=8),
         gs.lathe("Face", [(0.0, -0.05 * s), (0.33 * s, -0.05 * s), (0.33 * s, 0.05 * s), (0.0, 0.05 * s)], tm(face), loc=(c[0], 0, c[1]), axis=(0, -1, 0), seg=26),
         gs.rounded_cylinder("Button", (c[0], 0, c[1] + 0.43 * s), 0.06 * s, 0.1 * s, 0.02, tm(ring), seg=8, steps=1),
         gs.rounded_cylinder("Crown", (c[0] + 0.0, 0, c[1] + 0.39 * s), 0.1 * s, 0.05 * s, 0.015, tm(ring), seg=10, steps=1)]
    P.append(gs.capsule("HandA", (c[0], -0.06 * s, c[1]), (c[0] + 0.0, -0.06 * s, c[1] + 0.25 * s), 0.025 * s, tm(hand), seg=6, sub=1))
    P.append(gs.capsule("HandB", (c[0], -0.06 * s, c[1]), (c[0] + 0.16 * s, -0.06 * s, c[1] - 0.06 * s), 0.025 * s, tm("#26304F"), seg=6, sub=1))
    P.append(gs.ellipsoid("Pin", (c[0], -0.07 * s, c[1]), 0.04 * s, tm("#26304F"), seg=6, rings=4))
    for i in range(12):
        a = i / 12 * 2 * math.pi
        P.append(gs.ellipsoid(f"Tick{i}", (c[0] + 0.28 * s * math.cos(a), -0.055 * s, c[1] + 0.28 * s * math.sin(a)),
                              (0.018 * s, 0.012, 0.018 * s), tm("#26304F"), seg=4, rings=3))
    return P


def g_speedster():
    P = stopwatch((0.1, -0.02), 0.95)
    P += speed_lines("Sp", -0.52, -0.3, (0.18, 0.0, -0.18), tm("#FFFFFF", 0.5))
    return P


def g_broken_friends():
    left = [p for p in heart_pts(1.1, 48, math.pi, 2 * math.pi)]
    right = [p for p in heart_pts(1.1, 48, 0.0, math.pi)]
    zig = [(0.0, 0.336), (0.05, 0.2), (-0.05, 0.08), (0.05, -0.05), (-0.04, -0.17), (0.04, -0.27), (0.0, -0.336)]
    # el corte en zig-zag recorre de arriba (cleft) a la punta
    zz = [(x, z) for x, z in zig]
    red = tm("#E2394B")
    # mitad derecha: contorno t en [0, pi] termina en la punta; volver por el zig-zag
    pr = right + [(x + 0.0, z) for x, z in reversed(zz[1:-1])]
    pl = left + [(x, z) for x, z in zz[1:-1]]
    R_ = poly("HeartR", [(x, z) for x, z in pr], red, 0.16)
    L_ = poly("HeartL", [(x, z) for x, z in pl], tm("#C12B3D"), 0.16)
    R_.location = Vector((0.06, 0, -0.03))
    R_.rotation_euler = (0, math.radians(-10), 0)
    L_.location = Vector((-0.06, 0, -0.03))
    L_.rotation_euler = (0, math.radians(10), 0)
    return [R_, L_]


def g_beach_ball(c=(0, 0), s=1.0):
    cols = [tm("#F72585"), tm("#F7F4EC"), tm("#B5E48C"), tm("#F7F4EC"), tm("#4CC9F0"), tm("#FFD60A")]
    b = gs.ellipsoid("Ball", (c[0], 0, c[1]), 0.34 * s, cols, seg=24, rings=14)
    for f in b.data.polygons:
        a = math.atan2(f.center.y, f.center.x - 0 * c[0])
        f.material_index = int(((a + math.pi) / (2 * math.pi)) * 6) % 6
    b.rotation_euler = (math.radians(75), 0, math.radians(20))
    return [b]


def g_fiestero():
    P = g_beach_ball((0, -0.02), 1.15)
    P += spark4("S1", (0.4, 0.34), 0.1, tm("#FFD60A", 0.7)) + spark4("S2", (-0.4, 0.32), 0.08, tm("#F72585", 0.7))
    P += [gs.rounded_box("Conf1", (-0.42, -0.05, -0.2), (0.09, 0.015, 0.05), 0.005, tm("#4CC9F0", 0.4), segments=1, rot=("Y", 30)),
          gs.rounded_box("Conf2", (0.43, -0.05, -0.18), (0.09, 0.015, 0.05), 0.005, tm("#B5E48C", 0.4), segments=1, rot=("Y", -40))]
    return P


def infinity(scale=1.0, mat=None, th=0.075, n=44):
    pts = []
    for i in range(n + 1):
        t = i / n * 2 * math.pi
        d = 1 + math.sin(t) ** 2
        pts.append(Vector((0.48 * scale * math.cos(t) / d * 1.3, 0, 0.48 * scale * math.sin(t) * math.cos(t) / d * 1.3)))
    m = mat or tm("#4CC9F0", 0.2)
    return [gs.tube("Inf", pts, th * scale, m, seg=10, cap=3, sub=1)]


def g_kilometer():
    P = infinity(0.78, tm("#4CC9F0", 0.2), 0.085)
    for i in range(7):
        h = 0.1 if i % 3 == 0 else 0.06
        P.append(gs.rounded_box(f"Tick{i}", (-0.36 + 0.12 * i, 0, -0.34), (0.035, 0.04, h), 0.01, tm("#F7F4EC"), segments=1))
    P.append(gs.rounded_box("Rule", (0, 0.02, -0.3), (0.9, 0.03, 0.03), 0.008, tm("#F7F4EC"), segments=1))
    return P


def calendar(c=(0, 0), s=1.0, mark=None):
    P = [gs.rounded_box("Page", (c[0], 0, c[1] - 0.03 * s), (0.72 * s, 0.1 * s, 0.62 * s), 0.06 * s, tm("#F7F4EC"), segments=1),
         gs.rounded_box("Header", (c[0], -0.02 * s, c[1] + 0.22 * s), (0.72 * s, 0.11 * s, 0.17 * s), 0.05 * s, tm("#F72585"), segments=1)]
    for x in (-0.2, 0.2):
        P.append(gs.rounded_cylinder(f"Ring{x}", (c[0] + x * s, -0.03 * s, c[1] + 0.34 * s), 0.035 * s, 0.14 * s, 0.015, tm("#B9C2CC"), seg=6, steps=1))
    if mark == "star":
        st = star("Star", (c[0], c[1] - 0.1 * s), 0.2 * s, tm("#FFD60A", 0.3), th=0.07)
        st.location = Vector((0, -0.1 * s, 0))
        P.append(st)
    else:
        for i in range(6):
            x, z = (i % 3 - 1) * 0.2 * s, -0.04 * s - (i // 3) * 0.2 * s
            P.append(gs.ellipsoid(f"Dot{i}", (c[0] + x, -0.06 * s, c[1] + z), (0.065 * s, 0.025 * s, 0.065 * s), tm("#B5E48C" if i < 5 else "#FF9F1C", 0.2), seg=8, rings=4))
    return P


def g_constant():
    P = calendar((0, -0.03), 1.0)
    P.append(gs.lathe("Flame", [(0.0, 0.0), (0.12, 0.02), (0.1, 0.16), (0.04, 0.3), (0.0, 0.36)], tm("#FF9F1C", 0.7), loc=(0.0, -0.1, -0.2), axis=(0, 0, 1), seg=8, scale=(1, 0.3, 1)))
    return P


def g_book():
    P = []
    for s in (1, -1):
        P.append(gs.rounded_box(f"Cover{s}", (0.2 * s, 0.04, -0.04), (0.42, 0.05, 0.62), 0.03, tm("#7A4AB8"), rot=("Y", -10 * s), segments=1))
        P.append(gs.rounded_box(f"Page{s}", (0.2 * s, -0.02, -0.02), (0.38, 0.04, 0.56), 0.02, tm("#F7F4EC"), rot=("Y", -10 * s), segments=1))
        for j in range(4):
            P.append(gs.rounded_box(f"Ln{s}{j}", (0.2 * s, -0.05, 0.16 - 0.1 * j), (0.26 if j < 3 else 0.16, 0.012, 0.025), 0.005, tm("#8E9099"),
                                    rot=("Y", -10 * s), segments=1))
    P.append(gs.tube("Marker", [(0.12, -0.05, 0.28), (0.12, -0.05, -0.2), (0.12, -0.05, -0.34)], 0.025, tm("#F72585"), seg=5, cap=2, sub=1))
    return P


def g_timeattack():
    P = stopwatch((0.08, -0.02), 0.88)
    gh = tm("#F7F4EC", 0.15)
    P.append(gs.ellipsoid("GhostBody", (-0.34, 0.12, 0.0), (0.16, 0.08, 0.2), gh, seg=10, rings=7))
    for i, x in enumerate((-0.45, -0.34, -0.23)):
        P.append(gs.ellipsoid(f"GhostTail{i}", (x, 0.12, -0.17), 0.055, gh, seg=6, rings=4))
    for x in (-0.4, -0.28):
        P.append(gs.ellipsoid(f"GhostEye{x}", (x, 0.04, 0.04), (0.025, 0.012, 0.035), tm("#26304F"), seg=6, rings=4))
    return P


def g_mirror():
    pts = [Vector((0.3 * math.cos(i / 32 * 2 * math.pi), 0, 0.02 + 0.42 * math.sin(i / 32 * 2 * math.pi))) for i in range(33)]
    P = [gs.tube("Frame", pts, 0.06, tm("#F5C431"), seg=8, cap=3, sub=1)]
    glass = gs.ellipsoid("Glass", (0, 0.03, 0.02), (0.29, 0.03, 0.4), tm("#9FE8FF", 0.35), seg=18, rings=10)
    P.append(glass)
    P += [gs.rounded_box("Shine1", (-0.08, -0.03, 0.1), (0.06, 0.01, 0.34), 0.01, tm("#FFFFFF", 0.8), rot=("Y", 25), segments=1),
          gs.rounded_box("Shine2", (0.04, -0.03, 0.0), (0.03, 0.01, 0.22), 0.01, tm("#FFFFFF", 0.8), rot=("Y", 25), segments=1)]
    P.append(gs.rounded_box("Stand", (0, 0.04, -0.5), (0.26, 0.1, 0.07), 0.025, tm("#F5C431"), segments=1))
    P.append(gs.ellipsoid("Gem", (0, -0.05, 0.47), 0.07, tm("#F72585", 0.4), seg=7, rings=4))
    return P


def g_chaos():
    d = gs.rounded_box("Die", (0, 0, 0), (0.56, 0.56, 0.56), 0.1, tm("#F7F4EC"), rot=[("Z", 25), ("X", -20)], segments=2)
    P = [d]
    face = Matrix.Rotation(math.radians(25), 3, "Z") @ Matrix.Rotation(math.radians(-20), 3, "X")
    pips = [(-0.14, -0.14), (0.14, 0.14), (0, 0), (-0.14, 0.14), (0.14, -0.14)]
    for i, (x, z) in enumerate(pips):
        p = face @ Vector((x, -0.285, z))
        P.append(gs.ellipsoid(f"Pip{i}", p, 0.055, tm("#26304F"), seg=8, rings=5))
    bolt = [(0.1, 0.45), (-0.08, 0.1), (0.04, 0.1), (-0.12, -0.28), (0.18, 0.18), (0.04, 0.18)]
    b = poly("Bolt", [(x + 0.28, z + 0.12) for x, z in bolt], tm("#FFD60A", 0.6), 0.08)
    b.location = Vector((0, -0.34, 0))
    P.append(b)
    return P


def g_party_popper():
    cone = gs.cone("Popper", (-0.3, 0, -0.38), (0.02, 0, 0.0), 0.2, 0.06, [tm("#F72585"), tm("#FFD60A")], seg=10)
    gs.bands_by_plane(cone, (1, 0, 1), (-0.3, 0, -0.38), 0.22, 0.5, 1, cut=True)
    cone.rotation_euler = (0, 0, 0)
    P = [cone]
    cols = ["#4CC9F0", "#B5E48C", "#F72585", "#FFD60A", "#FF9F1C"]
    rnd = random.Random(5)
    for i in range(12):
        a = math.radians(rnd.uniform(5, 80))
        r = rnd.uniform(0.18, 0.55)
        P.append(gs.rounded_box(f"Conf{i}", (0.02 + r * math.cos(a), -0.05, 0.0 + r * math.sin(a)), (0.08, 0.015, 0.045), 0.005, tm(cols[i % 5], 0.4),
                                rot=[("Y", rnd.uniform(0, 180))], segments=1))
    for i, (x, z) in enumerate(((0.3, 0.28), (0.45, 0.02), (0.1, 0.45))):
        P += spark4(f"Sp{i}", (x, z), 0.09, tm("#FFFFFF", 0.7))
    return P


def g_infinite():
    mats = [tm("#B5E48C", 0.2), tm("#F72585", 0.2)]
    o = infinity(0.9, mats, 0.1)[0]
    for f in o.data.polygons:
        f.material_index = 0 if f.center.x >= 0 else 1
    return [o]


def g_daily():
    P = calendar((0, -0.02), 1.15, mark="star")
    return P


def g_teams():
    P = []
    for i, (x, col) in enumerate(((-0.24, "#FF5A36"), (0.2, "#3A86FF"))):
        P.append(gs.torus(f"Raft{i}", (x, 0, -0.22 + 0.06 * i), 0.22, 0.085, tm(col), axis=(0.1, -0.7, 0.7), seg=16, mseg=8))
    P.append(gs.capsule("Pole", (0.0, 0.0, -0.05), (0.0, 0.0, 0.46), 0.025, tm("#B9C2CC"), seg=6, sub=1))
    fl = gs.rounded_box("Flag", (0.17, 0, 0.34), (0.34, 0.03, 0.24), 0.01, [tm("#F7F4EC"), tm("#26304F")], segments=1)
    gs.bands_by_plane(fl, (1, 0, 0), (0, 0, 0), 0.16, 0.5, 1, cut=True)
    for f in fl.data.polygons:
        if f.material_index == 1 and f.center.z > 0.34:
            f.material_index = 0
        elif f.material_index == 0 and f.center.z > 0.34:
            f.material_index = 1
    P.append(fl)
    return P


# ---------------------------------------------------------------------------
# Dificultad: caras
# ---------------------------------------------------------------------------
def face_base(col, em=0.0):
    return [gs.ellipsoid("Face", (0, 0, 0), (0.46, 0.2, 0.46), tm(col, em), seg=24, rings=14)]


def g_face(kind):
    if kind == "easy":
        P = face_base("#7BD65A", 0.1)
        mouth = [Vector((0.2 * math.cos(math.radians(a)), -0.2, -0.04 + 0.2 * math.sin(math.radians(a)))) for a in range(200, 341, 12)]
    elif kind == "normal":
        P = face_base("#FFB627", 0.1)
        mouth = [Vector((x, -0.2, -0.17 + 0.015 * x)) for x in (-0.17, -0.08, 0.0, 0.1, 0.19)]
    else:
        P = face_base("#E2394B", 0.1)
        mouth = [Vector((0.19 * math.cos(math.radians(a)), -0.2, -0.26 + 0.17 * math.sin(math.radians(a)))) for a in range(30, 151, 12)]
    P.append(gs.tube("Mouth", [Vector(p) + Vector((0, -0.01, 0)) for p in mouth], 0.034, tm("#3B1E1E"), seg=7, cap=3, sub=1))
    for s in (1, -1):
        e = gs.eye(f"Eye{s}", (0.16 * s, -0.17, 0.14), 0.095, look=(0.05 * s, -1, 0.0), bone=None, side=s)
        P += e
        if kind == "hard":
            P.append(gs.capsule(f"Brow{s}", (0.06 * s, -0.22, 0.29), (0.27 * s, -0.22, 0.2), 0.03, tm("#3B1E1E"), seg=6, sub=1))
        elif kind == "easy":
            P.append(gs.ellipsoid(f"Blush{s}", (0.3 * s, -0.17, -0.04), (0.07, 0.02, 0.045), tm("#FF8FA3"), seg=7, rings=4))
        else:
            P.append(gs.capsule(f"Brow{s}", (0.07 * s, -0.22, 0.26), (0.26 * s, -0.22, 0.28), 0.028, tm("#3B1E1E"), seg=6, sub=1))
    if kind == "easy":
        P += [gs.cone("Hat", (0.12, -0.02, 0.4), (0.2, -0.02, 0.72), 0.14, 0.01, tm("#F72585"), seg=8, round_tip=False),
              gs.ellipsoid("Pom", (0.2, -0.02, 0.74), 0.045, tm("#FFD60A"), seg=6, rings=4)]
    if kind == "hard":
        crack = tm("#26304F")
        P.append(gs.tube("Crack", [(-0.12, -0.22, 0.46), (0.02, -0.22, 0.3), (-0.06, -0.22, 0.18), (0.08, -0.22, 0.06)], 0.022, crack, seg=5, cap=2, sub=1))
        P.append(gs.tube("Crack2", [(0.02, -0.22, 0.3), (0.16, -0.22, 0.34)], 0.018, crack, seg=5, cap=2, sub=1))
    return P


GLYPHS = {}
