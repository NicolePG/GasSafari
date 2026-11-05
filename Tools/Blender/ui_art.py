"""Arte de interfaz renderizado en Blender (docs/07): retratos, poses de selección, iconos, logo y mapa de mundos.
blender --background --python ui_art.py [-- portraits poses icons logo map]

Salida en Tools/Export/UI/ (explicada en Tools/Export/UI/README.md). PNG con fondo transparente salvo el mapa.
Hoja de revisión: Tools/Blender/previews/UI_sheet.png.
"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
import gs_render as R
from gs_common import Vector, Pose

UI = R.UI_DIR
DONE = []


def out(*parts):
    p = os.path.join(UI, *parts)
    DONE.append(p)
    return p


def stand_pose(tilt=4.0, arm=70.0):
    """De pie, brazos abajo y algo separados, cabeza ladeada (para retratos)."""
    p = Pose()
    p.sym("UpperArm", ("Y", arm), ("Z", -8))
    p.sym("LowerArm", ("Z", -18))
    p.rot("Head", ("Y", tilt))
    p.rot("Spine", ("X", 2))
    return p


# ---------------------------------------------------------------------------
# Retratos 512x512: de frente, sonriendo, sobre un círculo del color de UI
# ---------------------------------------------------------------------------
PORTRAIT = {  # (alto de encuadre en m, bajada del centro respecto de la cabeza en m)
    "Alex": (0.95, 0.12), "Marty": (0.85, 0.12), "Melman": (0.78, 0.03), "Gloria": (0.95, 0.12),
    "Julien": (0.62, 0.08)}


def lighten(hexcol, k):
    h = hexcol.lstrip("#")
    c = [int(h[i:i + 2], 16) for i in (0, 2, 4)]
    c = [round(v + (255 - v) * k) if k > 0 else round(v * (1 + k)) for v in c]
    return "#%02X%02X%02X" % tuple(c)


def melman_smile(a):
    """Melman tiene boca nerviosa: para el retrato se tapa con el hocico y se dibuja una sonrisa."""
    cream, mouth = R.toon_mat("#FBE7B0", "T_SmileCream"), R.toon_mat("#5A1E2A", "T_SmileMouth")
    parts = [gs.ellipsoid("SmilePatch", (0, -0.352, 2.236), (0.072, 0.02, 0.026), cream, seg=12, rings=6)]
    pts = gs.arc_points((0, -0.372, 2.268), 0.062, 212, 328, 9, bulge=0.004)
    parts.append(gs.tube("Smile", pts, 0.0095, mouth, seg=6, cap=2, sub=2))
    m = a.head_matrix()
    for o in parts:
        o.matrix_world = m @ o.matrix_world
        o["gs_no_outline"] = True
    return parts


def portraits():
    import numpy as np
    for n in R.CHARS:
        R.new_scene((512, 512), transparent=True, samples=32)
        R.set_bands(rim=0.3)
        a = R.Actor(n).custom(stand_pose(tilt=5 if n != "Melman" else -4))
        if n == "Melman":
            melman_smile(a)
        top, _ = a.hat_anchor()
        hc = a.head_matrix() @ (top - Vector((0, 0, 0.16 if n != "Julien" else 0.1)))
        F, drop = PORTRAIT[n]
        tgt = hc - Vector((0, 0, drop))
        lens = 85.0
        dist = F / 2 / (18.0 / lens)
        cam = R.camera(tgt + Vector((0.08, -dist, 0.12)), tgt, lens)
        # círculo de fondo detrás del personaje (llena el 86 % del cuadro)
        back = 0.9
        rad = 0.43 * F * (dist + back) / dist
        ui = R.UI_COLOR[n]
        disc_c = tgt + Vector((-0.08 * back / dist, back, -0.12 * back / dist))
        R.disc_gradient("Circle", disc_c, rad, lighten(ui, 0.35), ui, normal=(0, -1, 0))
        ring = R.flat_disc("Ring", disc_c + Vector((0, 0.01, 0)), rad * 1.045, "#FFFFFF")[0]
        R.flat_disc("RingOut", disc_c + Vector((0, 0.02, 0)), rad * 1.075, "#1C1A26")
        R.sun((55, 0, -25), 1.0)
        R.sun((60, 0, 160), 0.35, name="Rim", shadow=False)
        p = R.render(out("Portraits", f"Portrait_{n}.png"), px=2.2)
        # máscara: fuera del círculo solo queda la mitad superior (la cabeza sale del borde)
        img = R._load_px(p)
        h, w = img.shape[:2]
        yy, xx = np.mgrid[0:h, 0:w]
        r = np.hypot(xx - w / 2, yy - h / 2)
        keep = (r <= 0.43 * w * 1.075 + 0.5) | (yy > h / 2)
        img[..., 3] *= keep
        R.save_px(img, p)


# ---------------------------------------------------------------------------
# Poses de selección 1024x1024 (cuerpo entero, clip Select_Pose)
# ---------------------------------------------------------------------------
SELECT_FRAC = {"Alex": 0.15, "Marty": 0.2, "Melman": 0.3, "Gloria": 0.15, "Julien": 0.25}


def poses():
    for n in R.CHARS:
        R.new_scene((1024, 1024), transparent=True, samples=32)
        R.set_bands(rim=0.3)
        a = R.Actor(n).pose("Select_Pose", SELECT_FRAC[n])
        R.frame([a.body], (0.45, -0.9, 0.18), lens=50, margin=1.08)
        R.sun((50, 0, -30), 1.0)
        R.sun((65, 0, 150), 0.35, name="Rim", shadow=False)
        R.render(out("CharacterSelect", f"Select_{n}.png"), px=3.0)


# ---------------------------------------------------------------------------
# Iconos 256x256: 7 power-ups y 5 habilidades
# ---------------------------------------------------------------------------
POWERUP_NAMES = ["P01_Beans", "P02_Locoto", "P03_Mango", "P04_CoconutShield", "P05_Soda", "P06_Onion",
                 "P07_GoldenMango"]


def icon_scene():
    R.new_scene((256, 256), transparent=True, samples=32)
    R.set_bands(shadow=(0.62, 0.62, 0.8), mid=(0.86, 0.86, 0.95), rim=0.35)
    R.sun((45, 0, -35), 1.1)
    R.sun((60, 0, 150), 0.4, name="Rim", shadow=False)


def icons_powerups():
    for name in POWERUP_NAMES:
        icon_scene()
        o = R.powerup(name, (0, 0, 0), rot=-15)
        R.frame([o], (0.45, -0.9, 0.35), lens=60, margin=1.12)
        R.render(out("Icons", "PowerUps", f"Icon_{name}.png"), px=3.2)


def note(loc, col, s=1.0, rot=0.0, beam=False):
    m = R.toon_mat(col)
    c = Vector(loc)
    P = [gs.ellipsoid("NoteHead", c, (0.13 * s, 0.09 * s, 0.1 * s), m, seg=12, rings=7, rot=("Y", -25))]
    top = c + Vector((0.11 * s, 0, 0.42 * s))
    P.append(gs.capsule("Stem", c + Vector((0.11 * s, 0, 0.02 * s)), top, 0.025 * s, m, seg=8, sub=1))
    P.append(gs.tube("Flag", [top, top + Vector((0.12 * s, 0, -0.08 * s)), top + Vector((0.16 * s, 0, -0.22 * s))],
                     [0.04 * s, 0.035 * s, 0.015 * s], m, seg=6, sub=2, flatten=((0, 1, 0), 0.4)))
    for o in P:
        o.rotation_euler = (0, 0, math.radians(rot))
    return P


def icons_abilities():
    import cosmetics
    # Alex · Rugido de la Selva: cabeza rugiendo + ondas en cono
    icon_scene()
    a = R.Actor("Alex").pose("Ability", 0.45)
    head = a.head_matrix()
    mouth = head @ Vector((0, -0.3, 1.42))
    fwd = (head.to_3x3() @ Vector((0, -1, 0))).normalized()
    up = Vector((0, 0, 1))
    waves = []
    wm = [R.toon_mat("#FFB627", emission=0.3), R.toon_mat("#FF7A1A", emission=0.3)]
    for k, r in enumerate((0.22, 0.38, 0.54)):
        pts = [mouth + fwd * (r * math.cos(math.radians(t))) + up * (r * math.sin(math.radians(t)))
               for t in range(-38, 39, 8)]
        waves.append(gs.tube(f"Wave{k}", pts, 0.035 + 0.006 * k, wm[k % 2], seg=8, cap=3, sub=1))
    tgt = mouth + fwd * 0.08 + Vector((0, 0, 0.04))
    side = fwd.cross(up).normalized()
    R.camera(tgt + (side * -0.75 + fwd * 0.55 + up * 0.18).normalized() * 2.7, tgt, 50)
    R.render(out("Icons", "Abilities", "Icon_Ability_Alex_Roar.png"), px=3.0)

    # Marty · Galope Turbo: herradura con estela de rayas
    icon_scene()
    shoe = [Vector((0.32 * math.cos(math.radians(t)), 0, 0.32 * math.sin(math.radians(t))))
            for t in range(-30, 211, 15)]
    P = [gs.tube("Horseshoe", shoe, 0.075, R.toon_mat("#D9E2EA"), seg=10, cap=3, sub=1, flatten=((0, 1, 0), 0.6))]
    for k, t in enumerate(range(-10, 200, 35)):
        p = Vector((0.32 * math.cos(math.radians(t)), -0.055, 0.32 * math.sin(math.radians(t))))
        P.append(gs.ellipsoid(f"Nail{k}", p, (0.022, 0.012, 0.022), R.toon_mat("#5C6F82"), seg=8, rings=4))
    for k, (z, L) in enumerate(((0.22, 0.75), (0.0, 1.0), (-0.22, 0.7))):
        stripes = [R.toon_mat("#26262E"), R.toon_mat("#F4F4F0")]
        P.append(gs.tube(f"Streak{k}", [Vector((0.42, 0.05, z)), Vector((0.42 + L, 0.05, z - 0.05))],
                         [0.05, 0.012], stripes, seg=8, cap=2, sub=6, mat_fn=lambda t: int(t * 6) % 2))
    for o in P:
        o.rotation_euler = (0, math.radians(-12), 0)
    R.gas_cloud((0.95, 0.15, -0.35), 0.28, color="#E8E2D0", n=4, seed=3, shine=False)
    R.frame(P, (0.15, -1, 0.15), lens=60, margin=1.12)
    R.render(out("Icons", "Abilities", "Icon_Ability_Marty_GallopTurbo.png"), px=3.0)

    # Melman · Cuello Gancho: cuello en forma de gancho que atrapa un aro
    icon_scene()
    yel, patch = R.toon_mat("#F2C94C"), R.toon_mat("#B5652B")
    neck = [Vector((0.0, 0, -0.6)), Vector((0.0, 0, 0.1)), Vector((0.12, 0, 0.45)), Vector((0.4, 0, 0.55)),
            Vector((0.62, 0, 0.38)), Vector((0.62, 0, 0.12))]
    P = [gs.tube("Neck", neck, [0.12, 0.11, 0.1, 0.095, 0.09, 0.09], yel, seg=12, cap=3, sub=3)]
    P += gs.stickers(P[0], 9, 0.06, patch, random.Random(2), min_dist=0.13)
    hd = Vector((0.66, -0.02, 0.02))
    P.append(gs.ellipsoid("Head", hd, (0.13, 0.12, 0.15), yel, seg=14, rings=8))
    P.append(gs.ellipsoid("Muzzle", hd + Vector((0, -0.08, -0.08)), (0.09, 0.08, 0.07), R.toon_mat("#FBE7B0"),
                          seg=12, rings=6))
    for s in (1, -1):
        P += gs.eye(f"Eye{s}", hd + Vector((0.055 * s, -0.1, 0.05)), 0.045, look=(0.1 * s, -1, -0.2), bone=None,
                    side=s)
        P.append(gs.capsule(f"Oss{s}", hd + Vector((0.05 * s, 0.03, 0.1)), hd + Vector((0.07 * s, 0.03, 0.22)),
                            0.02, R.toon_mat("#8A4A22"), seg=6, sub=1))
    P.append(gs.torus("Ring", (0.36, -0.02, 0.3), 0.2, 0.045, R.toon_mat("#F5C431", emission=0.2), seg=24, mseg=8,
                      axis=(0.35, -1, 0.15)))
    R.frame(P, (0.3, -1, 0.12), lens=60, margin=1.1)
    R.render(out("Icons", "Abilities", "Icon_Ability_Melman_NeckHook.png"), px=3.0)

    # Gloria · Panzazo: panza contra el agua con salpicadura
    icon_scene()
    g = R.Actor("Gloria").pose("Ability", 0.45)
    lo, hi = gs._world_bbox([g.body])
    base = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z - 0.15))
    sp = R.splash(base, 0.5, seed=5)
    rings = [gs.torus("Impact", base, 0.6, 0.04, R.toon_mat("#59C3FF", emission=0.3), seg=32, mseg=6),
             gs.torus("Impact2", base, 0.8, 0.03, R.toon_mat("#BDEBFF", emission=0.3), seg=32, mseg=6)]
    for k in range(3):
        x = (k - 1) * 0.35
        gs.capsule(f"Motion{k}", Vector((x, 0.1, hi.z + 0.15)), Vector((x, 0.1, hi.z + 0.45 - 0.08 * abs(k - 1))),
                   0.03, R.toon_mat("#FFFFFF", emission=0.6), seg=8, sub=1)
    R.frame([g.body] + sp + rings, (0.45, -0.9, 0.12), lens=50, margin=1.05)
    R.render(out("Icons", "Abilities", "Icon_Ability_Gloria_BellyFlop.png"), px=3.0)

    # Julien · Fiesta Real: corona dorada, notas y confeti
    icon_scene()
    crown = cosmetics.build_hat("H08_GoldenCrown")
    crown.scale = (2.2, 2.2, 2.2)
    crown.rotation_euler = (math.radians(-12), 0, math.radians(-10))
    P = [crown]
    P += note((-0.55, -0.1, 0.55), "#6CC24A", 0.9, 10)
    P += note((0.6, -0.1, 0.75), "#F72585", 0.75, -12)
    rnd = random.Random(8)
    cols = ["#F72585", "#B5E48C", "#3A86FF", "#FFD23F"]
    for k in range(9):
        p = Vector((rnd.uniform(-0.75, 0.75), -0.3, rnd.uniform(0.05, 1.05)))
        P.append(gs.ellipsoid(f"Confetti{k}", p, (0.045, 0.012, 0.025), R.toon_mat(cols[k % 4], emission=0.2),
                              seg=6, rings=3, rot=[("Y", rnd.uniform(0, 180)), ("X", rnd.uniform(0, 180))]))
    R.frame(P, (0.1, -1, 0.3), lens=60, margin=1.08)
    R.render(out("Icons", "Abilities", "Icon_Ability_Julien_RoyalParty.png"), px=3.0)


# ---------------------------------------------------------------------------
# Logo "GAS SAFARI" 2048x1024 con burbujas de gas
# ---------------------------------------------------------------------------
def _arc(cx, cz, rx, rz, a0, a1, n=14):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * i / (n - 1))),
             cz + rz * math.sin(math.radians(a0 + (a1 - a0) * i / (n - 1)))) for i in range(n)]


# trazos por letra (altura 1): ("c", puntos) curva, ("l", puntos) segmentos rectos; y ancho
LETTERS = {
    "G": ([("c", _arc(0.43, 0.5, 0.43, 0.5, 42, 330, 18) + [(0.86, 0.26), (0.86, 0.46)]),
           ("l", [(0.86, 0.46), (0.5, 0.46)])], 0.86),
    "A": ([("l", [(0.0, 0.0), (0.4, 1.0), (0.8, 0.0)]), ("l", [(0.17, 0.36), (0.63, 0.36)])], 0.8),
    "S": ([("c", _arc(0.36, 0.75, 0.33, 0.25, 25, 270, 12) + _arc(0.36, 0.25, 0.36, 0.25, 90, -155, 13)[1:])],
          0.72),
    "F": ([("l", [(0.62, 1.0), (0.0, 1.0), (0.0, 0.0)]), ("l", [(0.0, 0.53), (0.5, 0.53)])], 0.62),
    "R": ([("l", [(0.0, 0.0), (0.0, 1.0), (0.36, 1.0)]),
           ("c", _arc(0.36, 0.75, 0.3, 0.25, 90, -90, 11) + [(0.0, 0.5)]),
           ("l", [(0.32, 0.5), (0.72, 0.0)])], 0.72),
    "I": ([("l", [(0.0, 0.0), (0.0, 1.0)])], 0.0),
}


def letter(ch, origin, h, r, mat, depth_mat, tilt=0.0, name="L"):
    """Letra de tubos gordos (burbuja) + copia trasera oscura para volumen."""
    strokes, w = LETTERS[ch]
    o = Vector(origin)
    ca, sa = math.cos(math.radians(tilt)), math.sin(math.radians(tilt))

    def P(x, z, dy=0.0):
        x -= w / 2
        z -= 0.5
        return o + Vector(((x * ca - z * sa) * h, dy, (x * sa + z * ca) * h))
    parts = []
    for layer, (dy, dz, mm) in enumerate(((0.0, 0.0, mat), (r * 0.9, -r * 0.55, depth_mat))):
        for kind, pts in strokes:
            pp = [P(x, z, dy) + Vector((0, 0, dz)) for x, z in pts]
            if kind == "c":
                parts.append(gs.tube(f"{name}{ch}{layer}", pp, r, mm, seg=14, cap=5, sub=1,
                                     flatten=((0, 1, 0), 0.75)))
            else:
                for i in range(len(pp) - 1):
                    parts.append(gs.capsule(f"{name}{ch}{layer}_{i}", pp[i], pp[i + 1], r, mm, seg=14, cap=5, sub=1))
    # brillo en el trazo principal
    kind, pts = strokes[0]
    x, z = pts[len(pts) // 3]
    s = gs.ellipsoid(f"{name}{ch}Shine", P(x, z, -r * 0.8) + Vector((-r * 0.25, 0, r * 0.35)),
                     (r * 0.3, r * 0.1, r * 0.18), R.toon_mat("#FFFFFF", "T_Shine", emission=1.0), seg=8, rings=5)
    s["gs_no_outline"] = True
    parts.append(s)
    return parts, w * h


def word(text, center, h, r, mats, depth_mats, spacing=0.34, seed=0, tilt=6.0, bob=0.06):
    rnd = random.Random(seed)
    widths = [LETTERS[c][1] * h for c in text]
    total = sum(widths) + spacing * h * (len(text) - 1)
    x = Vector(center).x - total / 2
    parts = []
    for i, c in enumerate(text):
        cx = x + widths[i] / 2
        z = Vector(center).z + bob * h * math.sin(i * 1.7 + seed)
        ps, _ = letter(c, (cx, Vector(center).y, z), h, r, mats[i % len(mats)], depth_mats[i % len(depth_mats)],
                       tilt=rnd.uniform(-tilt, tilt), name=f"{text}{i}")
        parts += ps
        x += widths[i] + spacing * h
    return parts


def logo():
    R.new_scene((2048, 1024), transparent=True, samples=48)
    R.set_bands(shadow=(0.62, 0.62, 0.82), mid=(0.86, 0.86, 0.96), rim=0.3)
    green = [R.toon_mat("#7ED957", "T_LogoGreen", emission=0.1), R.toon_mat("#5CC94A", "T_LogoGreen2", emission=0.1)]
    gdepth = [R.toon_mat("#2E7D32", "T_LogoGreenD")]
    orange = [R.toon_mat("#FFB627", "T_LogoOrange", emission=0.1), R.toon_mat("#FF8C1A", "T_LogoOrange2",
                                                                                emission=0.1)]
    odepth = [R.toon_mat("#B5501A", "T_LogoOrangeD")]
    gas = word("GAS", (0, 0, 1.15), 1.25, 0.15, green, gdepth, seed=1)
    safari = word("SAFARI", (0, 0, -0.42), 1.05, 0.13, orange, odepth, seed=4, spacing=0.3)
    # nube de gas detrás de "GAS" y burbujas
    cloud = R.gas_cloud((0, 0.9, 1.15), 1.2, color="#C9F59A", n=9, seed=11, stretch=(2.0, 0.3, 0.7), shine=False)
    for c in cloud:
        c.scale = (1, 0.5, 1)
    rnd = random.Random(3)
    bubbles = []
    spots = [(-2.2, 1.9, 0.2), (-2.6, 1.2, 0.13), (2.25, 1.85, 0.22), (2.7, 1.1, 0.12), (1.9, 2.4, 0.1),
             (-1.7, 2.45, 0.09), (-3.3, -0.2, 0.16), (3.4, -0.1, 0.18), (3.0, -0.95, 0.1), (-3.0, -1.0, 0.11),
             (0.3, 2.35, 0.12), (-0.6, 2.55, 0.07), (2.95, 0.45, 0.07), (-2.95, 0.5, 0.08)]
    for i, (x, z, r) in enumerate(spots):
        bubbles += R.bubble((x, -0.2 + rnd.uniform(-0.2, 0.2), z), r, color="#B9F28A", name=f"Bub{i}")
    # hojas en las puntas de SAFARI
    leaf = [R.toon_mat("#3FA34D"), R.toon_mat("#2D7A3A")]
    for s in (1, -1):
        for k in range(3):
            a = math.radians(90 + s * (35 + 28 * k))
            base = Vector((s * 2.85, 0.15, -0.7))
            tip = base + Vector((math.cos(a) * -s * 0.1 + s * 0.45 * math.cos(math.radians(25 * k)), 0.1,
                                 0.25 + 0.3 * math.sin(math.radians(30 * k + 20))))
            gs.tube(f"Leaf{s}{k}", [base, (base + tip) / 2 + Vector((0, 0, 0.12)), tip], [0.03, 0.13, 0.02],
                    leaf[k % 2], seg=8, sub=3, flatten=((0, 1, 0), 0.3))
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    R.frame(objs, (0, -1, 0.12), lens=85, margin=1.04)
    R.sun((40, 0, -20), 1.0)
    R.sun((70, 0, 160), 0.4, name="Rim", shadow=False)
    R.render(out("Logo", "Logo_GasSafari.png"), px=6.0, outline_col="#1B1230")


# ---------------------------------------------------------------------------
# Mapa de mundos 1920x1080: 6 islas unidas por un río, cada una con su hito
# ---------------------------------------------------------------------------
ISLANDS = [  # centro, radio, color de la isla, color del terreno alrededor
    ("W1", (-33.0, -12.0), 7.0, "#B9C2CC", "#8DBF6A"),
    ("W2", (-19.0, 7.0), 7.0, "#F2D492", "#E9D59A"),
    ("W3", (-3.0, -7.0), 7.5, "#4FB346", "#3E9B45"),
    ("W4", (11.0, 9.5), 7.5, "#5A3D6E", "#6B4C7A"),
    ("W5", (25.0, -6.0), 7.5, "#E39A4A", "#D9894A"),
    ("W6", (37.0, 12.0), 6.5, "#E0559F", "#B5E48C"),
]


def river_path():
    import numpy as np
    pts = [Vector((-58, -26, 0))] + [Vector((c[0], c[1], 0)) for _, c, _, _, _ in ISLANDS] + [Vector((60, 22, 0))]
    mids = []
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        m = (a + b) / 2
        side = Vector((-(b - a).y, (b - a).x, 0)).normalized()
        mids += [a, m + side * (4.5 if i % 2 else -4.5)]
    mids.append(pts[-1])
    dense = gs._catmull(mids, 12)
    return np.array([(p.x, p.y) for p in dense])


def map_scene():
    import numpy as np
    import cosmetics
    R.new_scene((1920, 1080), samples=32)
    R.world_sky("#7EC8F2", "#CFEFFF", ambient=0.35)
    R.set_bands(shadow=(0.6, 0.6, 0.8), mid=(0.85, 0.85, 0.94), rim=0.15)
    R.water(260, deep="#2C8FCB", light="#4CB5E6", foam="#E8FBFF", scale=0.06, z=-0.05)
    riv = river_path()
    # terreno: colinas, cauce del río, lagos alrededor de las islas y costa hacia el mar
    ctrs = np.array([c for _, c, _, _, _ in ISLANDS])
    import bmesh
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=230, y_segments=230, size=60)
    V = np.array([(v.co.x * 1.0, v.co.y * 0.62) for v in bm.verts])
    d_riv = np.full(len(V), 1e9)
    for k in range(0, len(riv), 64):
        seg = riv[k:k + 64]
        d = np.sqrt(((V[:, None, :] - seg[None, :, :]) ** 2).sum(-1)).min(1)
        d_riv = np.minimum(d_riv, d)
    d_isl = np.sqrt(((V[:, None, :] - ctrs[None, :, :]) ** 2).sum(-1))
    near = d_isl.argmin(1)
    lake = d_isl.min(1) - np.array([ISLANDS[i][2] for i in near]) - 2.6
    edge = np.maximum(np.abs(V[:, 0]) / 56.0, np.abs(V[:, 1]) / 33.0)
    rnd = np.random.default_rng(4)
    ph = rnd.uniform(0, 6.28, 6)
    hills = (np.sin(V[:, 0] * 0.21 + ph[0]) * np.sin(V[:, 1] * 0.27 + ph[1]) * 0.9
             + np.sin(V[:, 0] * 0.09 + V[:, 1] * 0.13 + ph[2]) * 0.8 + 2.1)
    water_d = np.minimum(d_riv - 3.0, lake)
    z = np.maximum(-0.9, np.minimum(hills, 0.35 + water_d * 0.55))
    z = np.where(edge > 0.86, z - (edge - 0.86) * 40, z)
    for v, (x, y), zz in zip(bm.verts, V, z):
        v.co = Vector((x, y, float(zz)))
    for f in bm.faces:
        f.smooth = True
    land = R._mesh_obj("Land", bm, [], no_outline=True)
    # color por vértice: terreno del mundo más cercano y orilla de arena (bordes suaves, sin escalones)
    reg = np.array([R.lin(c[4]) for c in ISLANDS])
    sand_c, wet_c = np.array(R.lin("#F2DFA7")), np.array(R.lin("#D8C48A"))
    w = 1.0 / np.maximum(d_isl - np.array([c[2] for c in ISLANDS])[None, :], 0.5) ** 6
    col = (w[:, :, None] * reg[None, :, :]).sum(1) / w.sum(1)[:, None]
    k_sand = np.clip((0.75 - z) / 0.35, 0, 1)[:, None]
    col = col * (1 - k_sand) + np.where(z[:, None] < 0, wet_c, sand_c) * k_sand
    ca = land.data.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
    for i, c in enumerate(col):
        ca.data[i].color = (*c, 1.0)
    lm = bpy.data.materials.new("T_Land")
    lm.use_nodes = True
    nt = lm.node_tree
    nt.nodes.clear()
    attr = nt.nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    g = nt.nodes.new("ShaderNodeGroup")
    g.node_tree = R.toon_group()
    o2 = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(attr.outputs["Color"], g.inputs["Color"])
    nt.links.new(g.outputs[0], o2.inputs["Surface"])
    lm["gs_toon"] = True
    land.data.materials.append(lm)
    # islas con playa y su hito
    for wid, (cx, cy), rad, top, _ in ISLANDS:
        R.blob(f"Beach_{wid}", (cx, cy, -0.6), (rad + 0.9, rad + 0.9, 1.1), R.toon_mat("#F7E3A8"), seed=int(wid[1]) * 13,
               rough=0.08, flat_bottom=-0.5)
        R.blob(f"Island_{wid}", (cx, cy, -0.2), (rad, rad, 1.3), R.toon_mat(top, f"T_Isl_{wid}"),
               seed=int(wid[1]) * 7, rough=0.1, flat_bottom=-0.3)
    s = 0.42
    P = {wid: Vector((c[0], c[1], 0.95)) for wid, c, _, _, _ in ISLANDS}
    R.env("W1", "ZooGate", P["W1"] + Vector((-1.5, -1.5, 0)), 8, scale=s)
    R.env("W1", "FerrisWheel", P["W1"] + Vector((3.2, 2.5, 0)), 20, scale=0.33)
    for k, (dx, dy) in enumerate(((-4.5, 2.0), (1.0, -4.8), (4.8, -1.5))):
        R.env("W1", "Lamp_A", P["W1"] + Vector((dx, dy, 0)), k * 50, scale=0.6)
    R.env("W2", "Lighthouse", P["W2"] + Vector((0.5, 1.5, -0.6)), 0, scale=0.3)
    R.env("W2", "Rock_A", P["W2"] + Vector((-4.5, -2.5, 0)), 30, scale=0.7)
    R.env("W2", "Mast", P["W2"] + Vector((-8.5, -3.0, -1.2)), 70, scale=0.6, tilt=(18, 0))
    R.env("W3", "DJStage", P["W3"] + Vector((0.5, 1.0, 0)), -10, scale=0.5)
    for k, (dx, dy, n) in enumerate(((-4.5, -1.0, "Palm_A"), (4.5, -2.5, "Palm_B"), (-2.5, 4.5, "Palm_C"),
                                     (3.5, 4.2, "Palm_A"))):
        R.env("W3", n, P["W3"] + Vector((dx, dy, 0)), k * 80, scale=0.65)
    R.env("W4", "VolcanoCrater", P["W4"] + Vector((0, 1.0, -0.3)), 0, scale=0.3)
    for k, (dx, dy) in enumerate(((-5.0, -3.0), (4.8, -3.2), (-5.5, 2.5))):
        R.env("W4", "Crystal_" + "ABC"[k], P["W4"] + Vector((dx, dy, 0)), k * 40, scale=0.8)
    R.env("W5", "NaturalArch", P["W5"] + Vector((0, 1.5, 0)), 15, scale=0.32)
    R.env("W5", "SlideTower", P["W5"] + Vector((-3.5, -2.0, 0)), -30, scale=0.25)
    for k, (dx, dy) in enumerate(((4.5, -3.0), (-5.0, 3.0))):
        R.env("W5", "Cactus_" + "AB"[k], P["W5"] + Vector((dx, dy, 0)), k * 70, scale=0.8)
    # Reino de Julien: escenario, corona gigante y Julien
    stage = gs.rounded_cylinder("Stage", P["W6"] + Vector((0, 0.5, 0.3)), 4.2, 0.6, 0.15,
                                R.toon_mat("#7B2CBF"), seg=32)
    crown = cosmetics.build_hat("H08_GoldenCrown")
    crown.location = P["W6"] + Vector((0, 2.2, 0.6))
    crown.scale = (10, 10, 10)
    j = R.Actor("Julien").place(P["W6"] + Vector((0.3, -1.2, 0.6)), 0, 5.0).pose("Victory", 0.25)
    for k in range(6):
        a = k / 6 * 2 * math.pi
        gs.ellipsoid(f"PartyLight{k}", P["W6"] + Vector((math.cos(a) * 5.2, math.sin(a) * 5.2, 0.9)), 0.45,
                     R.toon_mat(["#F72585", "#B5E48C", "#3AE1FF"][k % 3], emission=1.0), seg=10, rings=6)
    gs.ellipsoid("Disco", P["W6"] + Vector((-3.5, 3.0, 5.5)), 0.9, R.toon_mat("#D9E2EA", emission=0.4), seg=12,
                 rings=8)
    gs.capsule("DiscoPole", P["W6"] + Vector((-3.5, 3.0, 0.6)), P["W6"] + Vector((-3.5, 3.0, 4.6)), 0.12,
               R.toon_mat("#2B2B33"), seg=8, sub=1)
    # árboles y rocas por la tierra según el mundo más cercano
    rnd2 = random.Random(9)
    placed = 0
    tries = 0
    while placed < 70 and tries < 3000:
        tries += 1
        x, y = rnd2.uniform(-52, 52), rnd2.uniform(-30, 30)
        dv = np.sqrt(((riv - np.array([x, y])) ** 2).sum(1)).min()
        di = np.sqrt(((ctrs - np.array([x, y])) ** 2).sum(1))
        i = int(di.argmin())
        if dv < 4.5 or di[i] < ISLANDS[i][2] + 4.5 or abs(x) > 48 or abs(y) > 26:
            continue
        hz = float(np.sin(x * 0.21 + ph[0]) * np.sin(y * 0.27 + ph[1]) * 0.9
                   + np.sin(x * 0.09 + y * 0.13 + ph[2]) * 0.8 + 2.1)
        hz = min(hz, 0.35 + (dv - 2.2) * 0.55) - 0.1
        wid = ISLANDS[i][0]
        loc = Vector((x, y, hz))
        if wid in ("W1", "W3"):
            R.tree_blob(loc, rnd2.uniform(2.2, 3.5), seed=placed)
        elif wid == "W2":
            R.palm(loc, rnd2.uniform(3.0, 4.0), seed=placed, rot=rnd2.uniform(0, 360))
        elif wid == "W4":
            R.env("W4", "Stalagmite_" + "AB"[placed % 2], loc, rnd2.uniform(0, 360), scale=rnd2.uniform(0.5, 0.8))
        elif wid == "W5":
            R.env("W5", "CanyonRock_" + "ABC"[placed % 3], loc, rnd2.uniform(0, 360), scale=rnd2.uniform(0.4, 0.7))
        else:
            R.tree_blob(loc, rnd2.uniform(2.0, 3.0), seed=placed, leaf="#F72585", leaf2="#B5E48C")
        placed += 1
    # balsa en el río, al inicio del viaje
    rv = riv[int(np.sqrt(((riv - np.array([-27.5, -3.0])) ** 2).sum(1)).argmin())]
    r = R.raft((float(rv[0]), float(rv[1]), -0.1), 40, 1.1)
    R.gas_cloud((float(rv[0]) - 1.2, float(rv[1]) - 1.8, 0.6), 0.9, n=5, seed=2)
    for k, (x, y, z) in enumerate(((-52, 26, 14), (50, -25, 13), (-8, 30, 15), (30, 28, 16))):
        R.cloud((x, y, z), 9, seed=k)
    R.camera((0, -46, 64), (0, -1.5, 0), 31)
    R.sun((48, 0, 25), 1.0, angle=1.0)
    p = R.render(out("WorldMap", "WorldMap.png"), px=2.2)
    R.vignette(p, 0.3)


# ---------------------------------------------------------------------------
def sheet():
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    groups = {"Portraits": (5, (256, 256)), "CharacterSelect": (5, (300, 300)), "Icons": (6, (180, 180)),
              "Logo": (1, (900, 450)), "WorldMap": (1, (960, 540))}
    paths = []
    for g, (cols, cell) in groups.items():
        fs = []
        for root, _, files in os.walk(os.path.join(UI, g)):
            fs += [os.path.join(root, f) for f in sorted(files) if f.endswith(".png")]
        if fs:
            p = os.path.join(gs.PREVIEW_DIR, f"UI_{g}_sheet.png")
            R.contact_sheet(fs, p, cols=cols, cell=cell)
            paths.append(p)
    return paths


if __name__ == "__main__":
    want = R.args() or ["portraits", "poses", "icons", "logo", "map"]
    R.ensure_cache()
    if "portraits" in want:
        portraits()
    if "poses" in want:
        poses()
    if "icons" in want:
        icons_powerups()
        icons_abilities()
    if "logo" in want:
        logo()
    if "map" in want:
        map_scene()
    sheet()
    print("[gs] UI listo:", len(DONE), "imágenes")
