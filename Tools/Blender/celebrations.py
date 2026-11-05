"""Celebraciones de la tienda (docs/06 §2): Aplauso, Baile robot, Reverencia, Saludo de capitán, Voltereta y
Paso de conga, para los 5 personajes. Mismo esqueleto que el modelo: un FBX por clip, sin malla:
Tools/Export/CharacterAnims/<Nombre>/<Nombre>@Celebrate_<Celebración>.fbx  (clip "Celebrate_<Celebración>").
Todas son de pie, empiezan y terminan en la postura neutra (brazos abajo) y no hacen bucle: se reproducen una vez.
blender --background --python celebrations.py [-- Alex Marty ...] [--no-verify] [--debug]"""
import importlib
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
from gs_common import Pose, wave
import char_anims as ca
from char_anims import Rig, keyed, mirror

CHARS = ca.CHARS
# nombre del clip -> (nombre en la tienda, duración base en cuadros a 30 fps)
CELEBRATIONS = {
    "Aplauso": ("Aplauso", 66),
    "BaileRobot": ("Baile robot", 84),
    "Reverencia": ("Reverencia", 72),
    "SaludoCapitan": ("Saludo de capitán", 66),
    "Voltereta": ("Voltereta", 66),
    "PasoConga": ("Paso de conga", 80),
}


def smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def env(t, a=0.12, b=0.12):
    """Entrada y salida suaves: 0 al principio y al final, 1 en medio."""
    return smooth(t / a) * smooth((1 - t) / b)


def lerp(a, b, u):
    return a + (b - a) * u


def clip(r, name, base, fn, step=2, loops=False):
    return gs.sample_action(r.arm, "Celebrate_" + name, r.frames(base), fn, loops, step)


def stand_arms(r, p, k=1.0):
    """Brazos colgando, mezclados con k (1 = neutro)."""
    p.sym("UpperArm", ("Y", r.arm_down * k))
    return p


# ---------------------------------------------------------------------------
# 1. Aplauso: ovación con palmas al ritmo, rebote y cabeza que asiente
# ---------------------------------------------------------------------------
def aplauso(r):
    ad = r.arm_down
    claps = 6

    def fn(t):
        e = env(t, 0.14, 0.14)
        u = max(0.0, min(1.0, (t - 0.14) / 0.72))
        c = 0.5 + 0.5 * math.cos(2 * math.pi * claps * u)  # 1 = manos juntas
        c = c * c * (3 - 2 * c)
        bump = abs(math.sin(math.pi * claps * u))
        p = Pose().move("Hips", (0, 0, -0.018 * r.h * bump * e + 0.012 * r.h * e))
        p.rot("Spine", ("X", -5 * e + 3 * c * e))
        p.rot("Chest", ("X", 2 * c * e))
        ua = lerp(ad, 46 + 6 * (1 - c), e)
        p.sym("UpperArm", ("Y", ua), ("Z", -46 * e))
        p.sym("LowerArm", ("Z", -(88 + 38 * c) * e))
        p.sym("Hand", ("Y", -14 * c * e))
        p.sym("UpperLeg", ("X", -8 * bump * e), ("Y", -3 * e))
        p.sym("LowerLeg", ("X", 16 * bump * e))
        r.neck_rot(p, ("X", -8 * e + 7 * c * e), ("Z", 6 * wave(t, 1.5) * e), head=0.6)
        r.nervous(p, t, 0.5)
        r.tail_sway(p, t, 22 * e + 4, 3)
        return p
    return clip(r, "Aplauso", 66, fn, step=1)


# ---------------------------------------------------------------------------
# 2. Baile robot: poses angulosas que se mantienen y cambian de golpe
# ---------------------------------------------------------------------------
def baile_robot(r):
    ad = r.arm_down
    B = r.frames(8)  # cuadros por pulso

    def stiff(head=0, twist=0, ra=None, la=None, dip=0, step=0, sway=0):
        """Una postura robótica. ra/la = (subida del brazo, flexión del codo, adelante) del brazo derecho/izquierdo."""
        p = Pose().move("Hips", (sway * 0.03 * r.h, 0, -dip * 0.05 * r.h))
        p.rot("Spine", ("Y", twist), ("X", -2))
        p.rot("Chest", ("Y", twist * 0.5))
        for side, a in (("Right", ra), ("Left", la)):
            if a is None:
                p.side(side, "UpperArm", ("Y", ad - 4))
            else:
                p.side(side, "UpperArm", ("Y", a[0]), ("Z", -a[2]))
                p.side(side, "LowerArm", ("Y", a[1]))
        p.sym("UpperLeg", ("X", -22 * dip))
        p.sym("LowerLeg", ("X", 44 * dip))
        p.sym("Foot", ("X", -22 * dip))
        if step:
            side = "Left" if step > 0 else "Right"
            p.side(side, "UpperLeg", ("X", -28), ("Y", 10 * step))
            p.side(side, "LowerLeg", ("X", 10))
        r.neck_rot(p, ("Z", head), ("X", 2), head=0.7)
        for b in r.tail:
            p.rot(b, ("Z", 6 * sway))
        return p

    neutral = stiff()
    beats = [
        stiff(head=-48, twist=-14, ra=(0, -90, 0), la=(0, 90, 0), dip=0.3),
        stiff(head=48, twist=14, ra=(0, 90, 0), la=(0, -90, 0), dip=0.3),
        stiff(head=0, twist=0, ra=(-12, -90, 80), la=(-12, -90, 80), dip=0.5),
        stiff(head=-48, twist=-22, ra=(-70, -20, 0), la=(24, 70, 40), dip=0.2, step=1, sway=-1),
        stiff(head=48, twist=22, ra=(24, 70, 40), la=(-70, -20, 0), dip=0.2, step=-1, sway=1),
        stiff(head=0, twist=0, ra=(-86, 0, 0), la=(-86, 0, 0), dip=0.6),
        stiff(head=-60, twist=0, ra=(0, -90, 0), la=(0, -90, 0), dip=0.0),
        stiff(head=60, twist=0, ra=(0, -90, 0), la=(0, -90, 0), dip=0.0),
    ]
    keys = [(0, neutral)]
    f = int(B * 0.6)
    for i, bp in enumerate(beats):
        keys.append((f, bp))
        keys.append((f + B - 3, bp))  # se queda quieta y cambia de golpe
        f += B
    keys.append((f + 2, neutral))
    # al final, dos pasos de robot para volver
    last = f + 2 + int(B * 0.6)
    keys.append((last, neutral))
    return gs.make_action(r.arm, "Celebrate_BaileRobot", keys, loop=False)


# ---------------------------------------------------------------------------
# 3. Reverencia: mano al pecho, brazo atrás, inclinación profunda y floritura final
# ---------------------------------------------------------------------------
def reverencia(r):
    ad = r.arm_down

    def fn(t, v):
        p = Pose().move("Hips", (0, 0.1 * r.h * v["bow"] * 0.5, -0.02 * r.h * v["bow"]))
        p.rot("Spine", ("X", 28 * v["bow"]))
        p.rot("Chest", ("X", 16 * v["bow"]))
        # mano derecha al pecho
        p.side("Right", "UpperArm", ("Y", lerp(ad, 52, v["hand"])), ("Z", -40 * v["hand"] + -50 * v["flour"]))
        p.side("Right", "LowerArm", ("Z", -112 * v["hand"] + 90 * v["flour"]))
        p.side("Right", "Hand", ("Y", -20 * v["hand"]))
        p.side("Right", "UpperArm", ("Y", -34 * v["flour"]))
        # brazo izquierdo atrás, a la espalda
        p.side("Left", "UpperArm", ("Y", lerp(ad, ad + 6, v["back"])), ("Z", 20 * v["back"]))
        p.side("Left", "LowerArm", ("Z", 70 * v["back"]))
        p.side("Left", "UpperArm", ("X", -46 * v["bow"]))
        p.sym("UpperLeg", ("X", -4 * v["bow"]))
        p.side("Right", "UpperLeg", ("X", -10 * v["bow"]), ("Y", 4 * v["bow"]))
        p.side("Right", "LowerLeg", ("X", 8 * v["bow"]))
        r.neck_rot(p, ("X", 6 * v["bow"] - 8 * v["up"]), ("Z", 6 * v["flour"]), head=0.6)
        r.tail_sway(p, t, 10 * (1 - v["bow"]) + 3, 1)
        return p
    z = dict(bow=0, hand=0, back=0, up=0, flour=0)
    ph = [(0.0, z),
          (0.14, dict(z, hand=1, back=1, up=1)),
          (0.40, dict(z, hand=1, back=1, bow=1)),
          (0.60, dict(z, hand=1, back=1, bow=1)),
          (0.76, dict(z, hand=1, back=1, up=1)),
          (0.88, dict(z, hand=0, back=0, up=1, flour=1)),
          (1.0, z)]
    return keyed(r, "Celebrate_Reverencia", 72, ph, fn)


# ---------------------------------------------------------------------------
# 4. Saludo de capitán: firmes, saludo militar seco con taconazo y orgullo
# ---------------------------------------------------------------------------
def saludo_capitan(r):
    ad = r.arm_down

    def fn(t, v):
        p = Pose().move("Hips", (0, 0, 0.012 * r.h * v["tall"]))
        p.rot("Spine", ("X", -7 * v["tall"]), ("Y", -4 * v["sal"]))
        p.rot("Chest", ("X", -5 * v["tall"]))
        # brazo derecho al borde de la frente: codo alto, antebrazo plegado
        p.side("Right", "UpperArm", ("Y", lerp(ad, -50, v["sal"])), ("Z", -40 * v["sal"]))
        p.side("Right", "LowerArm", ("Y", -60 * v["sal"]), ("Z", -40 * v["sal"]))
        # el otro, rígido pegado al cuerpo
        p.side("Left", "UpperArm", ("Y", lerp(ad, ad + 12, v["tall"])))
        p.side("Left", "LowerArm", ("Z", -6 * v["tall"]))
        # talones juntos
        p.sym("UpperLeg", ("Y", -7 * v["tall"]))
        p.sym("Foot", ("Y", 7 * v["tall"]))
        r.neck_rot(p, ("X", -18 * v["tall"]), ("Z", -6 * v["sal"]), head=0.6)
        r.tail_sway(p, t, 6 * (1 - v["tall"]) + 2, 2)
        return p
    z = dict(tall=0, sal=0)
    ph = [(0.0, z),
          (0.14, dict(tall=1, sal=0)),
          (0.24, dict(tall=1, sal=1)),
          (0.40, dict(tall=1, sal=1)),
          (0.46, dict(tall=1.0, sal=0.97)),
          (0.76, dict(tall=1, sal=1)),
          (0.84, dict(tall=1, sal=0)),
          (0.94, dict(tall=1, sal=0)),
          (1.0, z)]
    return keyed(r, "Celebrate_SaludoCapitan", 66, ph, fn)


# ---------------------------------------------------------------------------
# 5. Voltereta: agacharse, salto con giro completo hacia adelante y caída con los brazos en V
# ---------------------------------------------------------------------------
def voltereta(r):
    ad = r.arm_down
    t_jump, t_land = 0.26, 0.66

    def fn(t):
        u = max(0.0, min(1.0, (t - t_jump) / (t_land - t_jump)))
        fly = 1.0 if 0.0 < u < 1.0 else 0.0
        rot = 360.0 * smooth(u) if 0 < u < 1 else (360.0 if t >= t_land else 0.0)
        crouch = smooth(t / 0.16) * (1 - smooth((t - 0.16) / 0.10)) if t < t_jump else 0.0
        land = smooth((t - t_land) / 0.06) * (1 - smooth((t - t_land - 0.06) / 0.14)) if t >= t_land else 0.0
        tuck = (smooth((u - 0.08) / 0.2) * (1 - smooth((u - 0.62) / 0.22))) if fly else 0.0
        rise = max(0.0, 1.0 - ((u - 0.5) * 2) ** 2) if fly else 0.0
        z = 0.62 * r.h * rise + 0.04 * r.h * smooth((t - 0.16) / 0.1) * (1 - smooth((t - 0.26) / 0.04)) \
            - 0.14 * r.h * (crouch + land)
        pre = (1 - smooth((t - 0.16) / 0.1)) if t < t_jump else 0.0
        arms_up = smooth((t - 0.18) / 0.08) * (1 - smooth((u - 0.1) / 0.2)) if t < t_land else 0.0
        v_end = smooth((t - 0.80) / 0.08) * (1 - smooth((t - 0.92) / 0.08)) if t >= t_land else 0.0
        p = Pose().move("Hips", (0, 0, z))
        p.rot("Hips", ("X", rot))
        p.rot("Spine", ("X", 20 * crouch + 18 * land - 12 * arms_up + 10 * tuck))
        k = max(crouch, land)
        p.sym("UpperLeg", ("X", -48 * k - 120 * tuck))
        p.sym("LowerLeg", ("X", 96 * k + 130 * tuck))
        p.sym("Foot", ("X", -48 * k))
        # brazos: atrás al agacharse, arriba en el salto, abrazando las rodillas, V al final
        a_y = lerp(ad, ad + 8, crouch) if t < t_jump else ad
        a_y = lerp(a_y, -80, arms_up)
        a_y = lerp(a_y, 20, tuck)
        a_y = lerp(a_y, -48, v_end)
        a_z = 38 * crouch * pre - 52 * tuck - 36 * land
        p.sym("UpperArm", ("Y", a_y), ("Z", a_z))
        p.sym("LowerArm", ("Z", -70 * tuck - 22 * land), ("Y", -20 * v_end))
        r.neck_rot(p, ("X", 28 * tuck - 14 * v_end), head=0.6)
        r.tail_sway(p, t, 25 * fly + 6, 3)
        return p
    return clip(r, "Voltereta", 66, fn, step=1)


# ---------------------------------------------------------------------------
# 6. Paso de conga: uno, dos, tres y patada, con las caderas y los brazos al ritmo
# ---------------------------------------------------------------------------
def paso_conga(r):
    ad = r.arm_down
    cycles = 2

    def fn(t):
        e = env(t, 0.1, 0.1)
        ph = (t * cycles * 4) % 4.0  # 0..4 pulsos
        beat = int(ph)
        fr = ph - beat
        hop = math.sin(math.pi * fr) ** 1.2
        side = [1, -1, 1, -1][beat]  # hacia dónde cae la cadera
        kick = 1.0 if beat == 3 else 0.0
        sway = wave(t, 2 * cycles, 0.0) * e  # una oscilación por dos pulsos
        p = Pose().move("Hips", (0.045 * r.h * sway, 0, 0.022 * r.h * hop * e))
        p.rot("Hips", ("Z", -16 * sway), ("Y", -5 * sway))
        p.rot("Spine", ("Z", 9 * sway), ("X", -3))
        # brazos adelante al nivel de la cintura, empujando al ritmo (como agarrando la cintura del de delante)
        for sd, pr in (("Left", 0.0), ("Right", 0.5)):
            push = wave(t, 4 * cycles, pr) * e
            hi = kick * hop
            p.side(sd, "UpperArm", ("Y", lerp(ad, 40, e) + 8 * push - 40 * hi * e), ("Z", -46 * e - 14 * hi * e))
            p.side(sd, "LowerArm", ("Z", -78 * e + 12 * push + 20 * hi * e), ("Y", -8 * e))
        # piernas: el paso carga el peso y la cuarta cuenta es una patada adelante
        lf = (1 if beat in (0, 2) else 0) * hop
        rf = (1 if beat == 1 else 0) * hop
        p.side("Left", "UpperLeg", ("X", (-34 * lf - 86 * (kick if side == 1 else 0) * hop) * e), ("Y", -4 * e))
        p.side("Left", "LowerLeg", ("X", (44 * lf + 6 * (kick if side == 1 else 0) * hop) * e))
        p.side("Right", "UpperLeg", ("X", (-34 * rf - 86 * kick * hop * (1 if side == -1 else 0)) * e), ("Y", 4 * e))
        p.side("Right", "LowerLeg", ("X", (44 * rf + 6 * kick * hop) * e))
        r.neck_rot(p, ("Z", 10 * sway), ("X", -6 * e), head=0.6)
        r.tail_sway(p, t, 30 * e + 4, 2 * cycles)
        return p
    return clip(r, "PasoConga", 80, fn, step=1)


BUILDERS = {"Aplauso": aplauso, "BaileRobot": baile_robot, "Reverencia": reverencia,
            "SaludoCapitan": saludo_capitan, "Voltereta": voltereta, "PasoConga": paso_conga}


def build_clips(arm, spec):
    r = Rig(arm, spec)
    return {k: fn(r) for k, fn in BUILDERS.items()}


# fracción de cada clip que mejor muestra la celebración (para iconos y hojas de contacto)
KEY_FRAC = {"Aplauso": 0.42, "BaileRobot": 0.52, "Reverencia": 0.46, "SaludoCapitan": 0.5, "Voltereta": 0.48,
            "PasoConga": 0.5}
SHOT_DIR = {"Aplauso": (0.55, -0.85, 0.3), "BaileRobot": (0.55, -0.85, 0.3), "Reverencia": (0.85, -0.55, 0.25),
            "SaludoCapitan": (0.6, -0.8, 0.3), "Voltereta": (0.95, -0.2, 0.2), "PasoConga": (0.55, -0.85, 0.3)}


def run(names, debug=False):
    tiles, files = [], []
    only = next((a.split("=")[1] for a in sys.argv if a.startswith("--clip=")), None)
    view = next((a.split("=")[1] for a in sys.argv if a.startswith("--view=")), None)
    dirs = {"front": (0, -1, 0.05), "side": (1, 0, 0.05), "3q": (0.55, -0.85, 0.3)}
    for name in names:
        mod = importlib.import_module(name.lower())
        cap = {}
        orig = gs.finish_character
        gs.finish_character = lambda n, body, arm, spec, poses=None: cap.update(body=body, arm=arm, spec=spec)
        mod.build()
        gs.finish_character = orig
        body, arm, spec = cap["body"], cap["arm"], cap["spec"]
        acts = build_clips(arm, spec)
        for k, act in acts.items():
            path = os.path.join(ca.OUT, name, f"{name}@Celebrate_{k}.fbx")
            ca.export_clip(arm, act, path)
            files.append((name, path))
        sc, cam = gs._setup_render(300)
        for k, act in acts.items():
            if only and k != only:
                continue
            fracs = ((0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8) if only else (0.2, 0.4, 0.6, 0.8)) if debug else (KEY_FRAC[k],)
            for fr in fracs:
                arm.animation_data.action = act
                sc.frame_set(int(round(act.frame_range[1] * fr)))
                p = os.path.join(gs.PREVIEW_DIR, f"_tmp_cel_{name}_{k}_{int(fr * 100)}.png")
                gs._render_view(p, [body], dirs[view] if view else SHOT_DIR[k], cam, margin=1.15)
                tiles.append(p)
        arm.animation_data.action = None
    cols = (4 * 6) if debug else 6
    sheet = os.path.join(gs.PREVIEW_DIR, "Celebrations_sheet.png" if not debug else f"Celebrations_{names[0]}_debug.png")
    gs.stitch(tiles, sheet, cols=cols if not debug else 4)
    for p in tiles:
        os.remove(p)
    print(f"[gs] preview {sheet}")
    if "--no-verify" not in sys.argv:
        for name in names:
            ref, _, _ = gs.verify_fbx(os.path.join(ca.OUT, name, f"{name}@Victory.fbx"))  # clip de referencia ya aceptado
            for n, path in files:
                if n == name:
                    bones, clips, tris = gs.verify_fbx(path)
                    assert bones == ref, f"{path}: esqueleto distinto"
                    assert tris == 0 and clips, f"{path}: malla o sin clip"
        print("[gs] verificación OK: mismo esqueleto en todos los clips")


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    run([a for a in argv if not a.startswith("--")] or CHARS, debug="--debug" in argv)
