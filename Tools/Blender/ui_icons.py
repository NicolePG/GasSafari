"""Iconos 256x256 (PNG, fondo transparente) para la tienda, los logros, los modos y las dificultades.
blender --background --python ui_icons.py [-- hats rafts gas celebrations achievements modes difficulty]
Salida: Tools/Export/UI/Icons/{Hats,Rafts,Gas,Celebrations,Achievements,Modes,Difficulty}/ y hojas de revisión en
Tools/Blender/previews/UI_Icons_<grupo>_sheet.png. Mismo pipeline toon y contorno que ui_art.py (gs_render).
Cada logro es una medalla con el mismo marco (aro dorado, disco de color por categoría y cinta magenta y lima)."""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
import gs_render as R
import cosmetics
import celebrations
import ui_glyphs as G
from gs_common import Vector
from ui_glyphs import tm

ICONS = os.path.join(R.UI_DIR, "Icons")
DONE = {}


def out(group, name):
    p = os.path.join(ICONS, group, name)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    DONE.setdefault(group, []).append(p)
    return p


def icon_scene():
    R.new_scene((256, 256), transparent=True, samples=32)
    R.set_bands(shadow=(0.62, 0.62, 0.8), mid=(0.86, 0.86, 0.95), rim=0.35)
    R.sun((45, 0, -35), 1.1)
    R.sun((60, 0, 150), 0.4, name="Rim", shadow=False)


# ---------------------------------------------------------------------------
# Tienda: sombreros, balsas, colores de gas y celebraciones
# ---------------------------------------------------------------------------
def hats():
    for name, label, _ in cosmetics.HATS:
        icon_scene()
        o = cosmetics.build_hat(name)
        o.rotation_euler = (0, 0, math.radians(-12))
        R.frame([o], (0.5, -0.9, 0.4), lens=60, margin=0.78)
        R.render(out("Hats", f"Icon_Hat_{name}.png"), px=3.2)


def rafts():
    icon_scene()
    root = R.raft()
    kids = [o for o in root.children_recursive if o.type == "MESH"]
    R.frame(kids, (0.5, -0.8, 0.85), lens=60, margin=0.85)
    R.render(out("Rafts", "Icon_Raft_R00_Orange.png"), px=3.2)
    for name, label, _ in cosmetics.RAFTS:
        icon_scene()
        root, objs = cosmetics.build_raft(name)
        R.frame(objs, (0.5, -0.8, 0.85), lens=60, margin=0.85)
        R.render(out("Rafts", f"Icon_Raft_{name}.png"), px=3.2)


def gas():
    # G01 Verde clásico
    icon_scene()
    P = R.gas_cloud((0, 0, 0), 1.0, n=7, seed=4)
    R.frame(P, (0.0, -1, 0.1), lens=60, margin=1.12)
    R.render(out("Gas", "Icon_Gas_G01_Classic.png"), px=3.2)
    # G02 Humo negro
    icon_scene()
    P = R.gas_cloud((0, 0, 0), 1.0, color="#33333F", n=7, seed=6)
    P += R.gas_cloud((0.35, 0, 0.85), 0.45, color="#4A4A58", n=3, seed=8, shine=False)
    P += R.gas_cloud((-0.45, 0, 0.7), 0.35, color="#4A4A58", n=3, seed=9, shine=False)
    R.frame(P, (0.0, -1, 0.1), lens=60, margin=1.12)
    R.render(out("Gas", "Icon_Gas_G02_BlackSmoke.png"), px=3.2)
    # G03 Neón rosa
    icon_scene()
    P = R.gas_cloud((0, 0, 0), 1.0, color="#FF4FA3", n=7, seed=3)
    glow = tm("#FFD6EC", 0.9)
    for i, (x, z) in enumerate(((-0.8, 0.5), (0.85, 0.35), (0.7, -0.6))):
        P += G.spark4(f"Sp{i}", (x, z), 0.2, glow)
    R.frame(P, (0.0, -1, 0.1), lens=60, margin=1.1)
    R.render(out("Gas", "Icon_Gas_G03_NeonPink.png"), px=3.2)
    # G04 Burbujas
    icon_scene()
    P = []
    for i, (x, z, r) in enumerate(((-0.3, -0.2, 0.42), (0.35, -0.05, 0.33), (-0.05, 0.38, 0.28), (0.55, 0.45, 0.2), (-0.62, 0.28, 0.16),
                                   (0.12, -0.52, 0.17), (0.0, 0.82, 0.12))):
        P += R.bubble((x, 0, z), r, color="#9FE8FF" if i % 2 else "#C8F7A0")
    R.frame(P, (0.0, -1, 0.1), lens=60, margin=1.12)
    R.render(out("Gas", "Icon_Gas_G04_Bubbles.png"), px=3.2)
    # G05 Arcoíris: arco de bandas con nubes en los extremos
    icon_scene()
    P = []
    for i, c in enumerate(("#E2394B", "#FF9F1C", "#FFD60A", "#6CC24A", "#3A86FF", "#8A4FD6")):
        P.append(G.arc(f"Rb{i}", (0, -0.35), 0.95 - 0.13 * i, 0, 180, 0.07, tm(c, 0.2), n=18))
    P += R.gas_cloud((-0.95, 0.02, -0.4), 0.55, color="#FFFFFF", n=4, seed=2, shine=False)
    P += R.gas_cloud((0.95, 0.02, -0.4), 0.55, color="#FFFFFF", n=4, seed=5, shine=False)
    R.frame(P, (0.0, -1, 0.1), lens=60, margin=1.1)
    R.render(out("Gas", "Icon_Gas_G05_Rainbow.png"), px=3.2)
    # G06 Brillantina dorada
    icon_scene()
    P = R.gas_cloud((0, 0, 0), 1.0, color="#F5C431", n=7, seed=7)
    gl = tm("#FFF6C8", 0.9)
    for i, (x, z, s) in enumerate(((-0.85, 0.45, 0.2), (0.8, 0.5, 0.26), (0.55, -0.62, 0.16), (-0.5, -0.6, 0.12), (0.05, 0.95, 0.14))):
        P += G.spark4(f"Gl{i}", (x, z), s, gl)
    R.frame(P, (0.0, -1, 0.1), lens=60, margin=1.1)
    R.render(out("Gas", "Icon_Gas_G06_GoldGlitter.png"), px=3.2)


# personaje, fracción del clip, cámara (desde dónde), sombrero, color del disco
CELEB = {"Aplauso": ("Alex", 0.42, (0.3, -0.9, 0.12), None, "#FFB703"),
         "BaileRobot": ("Marty", 0.12, (0.35, -0.9, 0.12), None, "#4CC9F0"),
         "Reverencia": ("Gloria", 0.46, (0.6, -0.8, 0.3), None, "#B5179E"),
         "SaludoCapitan": ("Julien", 0.5, (0.35, -0.9, 0.12), "H03_PirateHat", "#3A86FF"),
         "Voltereta": ("Melman", 0.47, (0.95, -0.25, 0.1), None, "#76C442"),
         "PasoConga": ("Gloria", 0.5, (0.4, -0.85, 0.12), None, "#F72585")}


def celebrations_icons():
    for clip, (who, frac, d, hat, col) in CELEB.items():
        icon_scene()
        a = R.Actor(who).pose("Celebrate_" + clip, frac)
        objs = [a.body]
        if hat:
            h = cosmetics.build_hat(hat)
            a.wear(h)
            objs.append(h)
        lo, hi = gs._world_bbox(objs)
        ctr = (lo + hi) / 2
        size = max(hi.x - lo.x, hi.y - lo.y, hi.z - lo.z)
        dv = Vector(d).normalized()
        bg = R.flat_disc("Bg", ctr - dv * 0.4, size * 0.6, col, normal=dv, ring=(size * 0.05, "#F7F4EC"))
        R.frame(objs + bg, d, lens=50, margin=0.8)
        R.render(out("Celebrations", f"Icon_Celebrate_{clip}.png"), px=3.0)


# ---------------------------------------------------------------------------
# Logros: medalla común (aro dorado, disco de categoría, cinta magenta y lima)
# ---------------------------------------------------------------------------
def medal(inner):
    Rm = 0.66
    P = [gs.torus("Rim", (0, 0, 0), Rm, 0.1, tm("#F5C431"), axis=(0, 1, 0), seg=40, mseg=10),
         gs.lathe("Body", [(0.0, -0.07), (Rm, -0.07), (Rm, 0.07), (0.0, 0.07)], tm("#C8931C"), axis=(0, 1, 0), seg=40),
         gs.lathe("Face", [(0.0, -0.05), (0.57, -0.05), (0.57, 0.05), (0.0, 0.05)], tm(inner), loc=(0, -0.09, 0), axis=(0, 1, 0), seg=40),
         gs.torus("Bevel", (0, -0.12, 0), 0.575, 0.026, tm("#FFFFFF", 0.15), axis=(0, 1, 0), seg=40, mseg=6)]
    rb = (tm("#F72585"), tm("#B5E48C"))
    for i, s in enumerate((-1, 1)):
        pts = [(0.04 * s, 0.5), (0.3 * s, 0.56), (0.62 * s, 1.08), (0.3 * s, 1.2)]
        o = G.poly(f"Ribbon{i}", pts, rb[i], 0.07)
        o.location = Vector((0, 0.14, 0))
        P.append(o)
    return P


def medal_scene():
    icon_scene()


def put_medal(inner, glyph, scale=1.0, off=(0.0, 0.0), name="x"):
    medal_scene()
    medal(inner)
    G.fit(glyph(), scale, (off[0], -0.2, off[1]))
    R.camera((0, -4.3, 0.2), (0, 0, 0.2), 70)
    R.render(name, px=3.0)


ACH = [
    ("01_PrimerPedo", "#6CC24A", G.g_first_fart, 1.0), ("02_Dueto", "#2EC4B6", G.g_duo, 1.0),
    ("03_Orquesta", "#3A86FF", G.g_orchestra, 1.0), ("04_SuperPedo", "#8A4FD6", G.g_super_fart, 1.0),
    ("05_HondaSelva", "#FF7A1A", G.g_sling, 1.0), ("06_Terremoto", "#FF7A1A", G.g_quake, 1.0),
    ("07_FiestaTotal", "#FF7A1A", G.g_party, 1.0), ("08_DuoPesado", "#FF7A1A", G.g_dumbbell, 1.0),
    ("09_RescateReal", "#FF7A1A", G.g_rescue, 1.0), ("10_CapitanTitanic", "#3A86FF", G.g_titanic, 1.0),
    ("11_SinRasguno", "#2EC4B6", G.g_shield, 1.0), ("12_Mundo1", "#1B2A4A", G.g_zoo_gate, 1.0),
    ("13_Mundo2", "#2EC4B6", G.g_lighthouse, 1.0), ("14_Mundo3", "#F77F00", G.g_palm, 1.0),
    ("15_Mundo4", "#4CC9F0", G.g_volcano, 1.0), ("16_Mundo5", "#FFB703", G.g_arch, 1.0),
    ("17_Mundo6", "#F72585", G.g_world6, 1.0), ("18_Perfeccionista", "#23306B", G.g_big_star, 1.0),
    ("19_Constelacion", "#23306B", G.g_constellation, 1.0), ("20_ColeccionistaReal", "#6C3BAA", G.g_collector, 1.0),
    ("21_Velocista", "#FF5A36", G.g_speedster, 1.0), ("22_AmistadesRotas", "#FFB3C1", G.g_broken_friends, 1.0),
    ("23_Fiestero", "#F72585", G.g_fiestero, 1.0), ("24_Kilometro", "#7A4AB8", G.g_kilometer, 1.0),
    ("25_Constante", "#76C442", G.g_constant, 1.0),
]


def achievements(only=None):
    for name, inner, fn, sc in ACH:
        if only and not any(o in name for o in only):
            continue
        put_medal(inner, fn, sc, name=out("Achievements", f"Icon_Ach_{name}.png"))


# ---------------------------------------------------------------------------
# Modos y dificultades: insignia redonda
# ---------------------------------------------------------------------------
def badge(inner, ring="#F7F4EC"):
    Rm = 0.66
    P = [gs.torus("Rim", (0, 0, 0), Rm, 0.07, tm(ring), axis=(0, 1, 0), seg=40, mseg=8),
         gs.lathe("Disc", [(0.0, -0.05), (Rm, -0.05), (Rm, 0.05), (0.0, 0.05)], tm(inner), axis=(0, 1, 0), seg=40)]
    P.append(G.arc("Shine", (0, 0), 0.52, 120, 175, 0.03, tm("#FFFFFF", 0.5), n=8))
    P[-1].location = Vector((0, -0.1, 0))
    return P


def put_badge(inner, glyph, scale=1.0, off=(0.0, 0.0), name="x"):
    icon_scene()
    badge(inner)
    G.fit(glyph(), scale, (off[0], -0.17, off[1]))
    R.camera((0, -3.6, 0), (0, 0, 0), 70)
    R.render(name, px=3.0)


MODES = [("M1_Historia", "#E59A2F", G.g_book), ("M2_Contrarreloj", "#2EC4B6", G.g_timeattack), ("M3_Espejo", "#3A86FF", G.g_mirror),
         ("M4_Caos", "#FF5A36", G.g_chaos), ("M5_Fiesta", "#7A4AB8", G.g_party_popper), ("M6_Infinito", "#23306B", G.g_infinite),
         ("M7_DesafioDiario", "#FFB703", G.g_daily), ("M8_CarreraEquipos", "#76C442", G.g_teams)]


def modes():
    for name, inner, fn in MODES:
        put_badge(inner, fn, 1.0, name=out("Modes", f"Icon_Mode_{name}.png"))


def difficulty():
    for name, kind in (("D1_Familiar", "easy"), ("D2_Normal", "normal"), ("D3_AmistadesRotas", "hard")):
        put_badge("#23306B", lambda kind=kind: G.g_face(kind), 0.95, (0, -0.02), name=out("Difficulty", f"Icon_Difficulty_{name}.png"))


def sheets():
    labels = {"Hats": (4, (200, 200)), "Rafts": (4, (200, 200)), "Gas": (3, (200, 200)), "Celebrations": (3, (200, 200)),
              "Achievements": (5, (200, 200)), "Modes": (4, (200, 200)), "Difficulty": (3, (200, 200))}
    for g, (cols, cell) in labels.items():
        fs = []
        for root, _, files in os.walk(os.path.join(ICONS, g)):
            fs += [os.path.join(root, f) for f in sorted(files) if f.endswith(".png")]
        if fs:
            R.contact_sheet(fs, os.path.join(gs.PREVIEW_DIR, f"UI_Icons_{g}_sheet.png"), cols=cols, cell=cell)


if __name__ == "__main__":
    want = [a for a in R.args() if not a.startswith("--")] or ["hats", "rafts", "gas", "celebrations", "achievements", "modes", "difficulty"]
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    R.ensure_cache()
    if "hats" in want:
        hats()
    if "rafts" in want:
        rafts()
    if "gas" in want:
        gas()
    if "celebrations" in want:
        celebrations_icons()
    if "achievements" in want:
        achievements()
    if "modes" in want:
        modes()
    if "difficulty" in want:
        difficulty()
    sheets()
    print("[gs] iconos listos:", {k: len(v) for k, v in DONE.items()})
