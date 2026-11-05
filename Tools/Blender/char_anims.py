"""Animaciones que faltan de los 5 personajes (docs/03): Shuffle_L, Shuffle_R, Grab, Ability, Disappointed,
Victory y Select_Pose. Reconstruye cada esqueleto con el script del personaje (mismos huesos y misma pose de
reposo que su FBX) y exporta un FBX por clip, sin malla: Tools/Export/CharacterAnims/<Nombre>/<Nombre>@<Clip>.fbx
blender --background --python char_anims.py [-- Alex Marty ...]"""
import importlib
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
from gs_common import Pose, wave

OUT = os.path.join(gs.EXPORT_DIR, "CharacterAnims")
CHARS = ["Alex", "Marty", "Melman", "Gloria", "Julien"]
CLIPS = ["Shuffle_L", "Shuffle_R", "Grab", "Ability", "Disappointed", "Victory", "Select_Pose"]
LOOP = {"Shuffle_L": True, "Shuffle_R": True, "Grab": True, "Ability": False, "Disappointed": False,
        "Victory": True, "Select_Pose": True}
# estilo de cada personaje para Ability, Victory y Select_Pose
STYLE = {"Alex": ("roar", "showman", "hero"), "Marty": ("gallop", "bounce", "hyper"),
         "Melman": ("neck_hook", "bounce", "nervous"), "Gloria": ("belly_flop", "groove", "sassy"),
         "Julien": ("party", "groove", "king")}


class Rig:
    def __init__(self, arm, spec):
        self.arm = arm
        self.h = spec["height"]
        self.sp = spec.get("speed", 1.0)
        self.amp = spec.get("amp", 1.0)
        self.jit = spec.get("jitter", 0.0)
        self.tail = spec.get("tail", [])
        self.neck = spec.get("neck", ["Neck"])
        self.arm_down = spec.get("arm_down", 72)
        self.drop = spec["sit_drop"]

    def frames(self, base):
        return max(8, int(round(base / self.sp)))

    def neck_rot(self, p, *rots, head=0.35):
        n = len(self.neck)
        for b in self.neck:
            p.rot(b, *[(a, d / n) for a, d in rots])
        p.rot("Head", *[(a, d * head) for a, d in rots])

    def tail_sway(self, p, t, a=12, f=1.0, curl=0.0):
        for i, b in enumerate(self.tail):
            p.rot(b, ("Z", a * self.amp * wave(t, f, -0.12 * i)), ("X", curl - 4 * wave(t, f * 2, -0.1 * i)))

    def nervous(self, p, t, k=1.0):
        if self.jit:
            p.rot("Head", ("Z", self.jit * 3 * k * wave(t, 7.0)), ("X", self.jit * 2 * k * wave(t, 9.0, 0.3)))

    def stand(self):
        return Pose().rot("LeftUpperArm", ("Y", self.arm_down)).rot("RightUpperArm", ("Y", -self.arm_down))

    def sit(self, lift=0.0):
        """Sentado en el borde (misma base que Idle_Sit)."""
        p = Pose().move("Hips", (0, 0.05 * self.h, -self.drop + lift))
        p.rot("Spine", ("X", 4))
        p.sym("UpperLeg", ("X", -88))
        p.sym("LowerLeg", ("X", 80))
        p.sym("Shoulder", ("Y", 6))
        return p


def mirror(p):
    """Espejo izquierda-derecha de una pose completa."""
    out = Pose()
    sw = lambda n: "Right" + n[4:] if n.startswith("Left") else ("Left" + n[5:] if n.startswith("Right") else n)
    for b, rots in p.items():
        out.rot(sw(b), *[(a, d if a == "X" else -d) for a, d in rots])
    for b, v in p.loc.items():
        out.move(sw(b), (-v.x, v.y, v.z))
    return out


def sample(r, name, base, fn, loop, step=2):
    return gs.sample_action(r.arm, name, r.frames(base), fn, loop, step)


def keyed(r, name, base, phases, fn):
    return gs.phase_action(r.arm, name, r.frames(base), phases, fn)


# ---------------------------------------------------------------------------
# Moverse por el borde y agarrarse
# ---------------------------------------------------------------------------
def shuffle_pose(r, t):
    hop = max(0.0, math.sin(2 * math.pi * t))
    p = r.sit(lift=0.03 * r.h * hop)
    p.move("Hips", (0.035 * r.h * wave(t), 0.05 * r.h, -r.drop + 0.03 * r.h * hop))
    p.rot("Spine", ("Y", 9 * r.amp * wave(t)), ("X", 6))
    p.rot("Chest", ("Y", 4 * wave(t, 1, 0.1)))
    # manos apoyadas que empujan, alternadas
    p.side("Left", "UpperArm", ("Y", r.arm_down - 12 + 10 * wave(t)), ("Z", -22))
    p.side("Right", "UpperArm", ("Y", r.arm_down - 12 - 10 * wave(t)), ("Z", -22))
    p.sym("LowerArm", ("Y", 10))
    # la pierna de delante se abre y la otra la sigue
    p.side("Left", "UpperLeg", ("Z", 14 * hop))
    p.side("Right", "UpperLeg", ("Z", -8 * max(0.0, math.sin(2 * math.pi * (t - 0.15)))))
    p.side("Left", "LowerLeg", ("X", -12 * hop))
    r.neck_rot(p, ("Z", 14), ("X", 4 * wave(t, 2)))
    r.nervous(p, t)
    r.tail_sway(p, t, 14, 1)
    return p


def grab_pose(r, t):
    p = Pose().move("Hips", (0, 0.07 * r.h, -r.drop - 0.03 * r.h + 0.006 * r.h * wave(t, 2)))
    shake = 1.5 * wave(t, 6) + 1.0 * wave(t, 9, 0.3)
    p.rot("Spine", ("X", 30), ("Z", shake))
    p.rot("Chest", ("X", 14))
    p.sym("UpperLeg", ("X", -96))
    p.sym("LowerLeg", ("X", 112))
    p.sym("Foot", ("X", 20))
    # brazos abajo y adelante, agarrados a la cuerda del borde
    p.sym("Shoulder", ("Y", 10))
    p.side("Left", "UpperArm", ("Y", r.arm_down + 8), ("Z", -48 + 3 * wave(t, 3)))
    p.side("Right", "UpperArm", ("Y", r.arm_down + 8), ("Z", -48 - 3 * wave(t, 3)))
    p.sym("LowerArm", ("Y", 25), ("Z", -20))
    p.sym("Hand", ("Y", 45))
    # mira a los lados con miedo
    r.neck_rot(p, ("X", -38), ("Z", 22 * wave(t, 1)))
    r.nervous(p, t, 1.5)
    r.tail_sway(p, t, 6, 3, curl=12)
    return p


# ---------------------------------------------------------------------------
# Habilidades
# ---------------------------------------------------------------------------
def ability(r, style):
    ad = r.arm_down
    if style == "roar":
        def fn(t, v):
            p = Pose().move("Hips", (0, 0.03 * r.h * v["lean"] / 20, -0.05 * r.h * v["crouch"]))
            p.rot("Spine", ("X", v["lean"]))
            p.rot("Chest", ("X", v["lean"] * 0.5))
            p.sym("UpperArm", ("Y", v["arm"]), ("Z", v["fwd"]))
            p.sym("LowerArm", ("Y", -v["claw"]))
            p.sym("Hand", ("Y", -v["claw"] * 0.8))
            p.sym("UpperLeg", ("X", -25 * v["crouch"]), ("Y", -14 * v["crouch"]))
            p.sym("LowerLeg", ("X", 45 * v["crouch"]))
            p.sym("Foot", ("X", -20 * v["crouch"]))
            r.neck_rot(p, ("X", v["head"]), ("Z", v["shake"] * 6 * wave(t, 9)))
            r.tail_sway(p, t, 10 + 20 * v["shake"], 3, curl=25 * v["crouch"])
            return p
        ph = [(0.0, dict(lean=0, crouch=0, arm=ad, fwd=0, claw=0, head=0, shake=0)),
              (0.22, dict(lean=-16, crouch=0.4, arm=-25, fwd=30, claw=10, head=-22, shake=0)),
              (0.38, dict(lean=24, crouch=1, arm=8, fwd=-38, claw=35, head=-4, shake=1)),
              (0.75, dict(lean=20, crouch=1, arm=12, fwd=-30, claw=30, head=-8, shake=1)),
              (1.0, dict(lean=0, crouch=0, arm=ad, fwd=0, claw=0, head=0, shake=0))]
        return keyed(r, "Ability", 42, ph, fn)

    if style == "gallop":
        def fn(t):
            run = min(1.0, max(0.0, (t - 0.1) / 0.1)) * min(1.0, max(0.0, (0.95 - t) / 0.1))
            p = Pose().move("Hips", (0, -0.03 * r.h * run, 0.05 * r.h * run * abs(wave(t, 4)) - 0.03 * r.h * run))
            p.rot("Spine", ("X", 28 * run))
            p.rot("Chest", ("X", 6 * run))
            r.neck_rot(p, ("X", -18 * run))
            for side, phs in (("Left", 0.0), ("Right", 0.5)):
                p.side(side, "UpperLeg", ("X", -55 * run * (0.5 + 0.5 * wave(t, 4, phs)) + 15 * run))
                p.side(side, "LowerLeg", ("X", 70 * run * (0.5 + 0.5 * wave(t, 4, phs + 0.25))))
                p.side(side, "UpperArm", ("Y", ad - 10 * run), ("Z", 45 * run * wave(t, 4, phs + 0.5)))
                p.side(side, "LowerArm", ("Z", -50 * run))
            r.tail_sway(p, t, 25, 4, curl=30 * run)
            return p
        return sample(r, "Ability", 36, fn, False)

    if style == "neck_hook":
        def fn(t, v):
            p = Pose().move("Hips", (0, -0.02 * r.h * v["reach"], -0.02 * r.h * v["reach"]))
            p.rot("Spine", ("X", v["lean"]))
            n = len(r.neck)
            for b in r.neck:
                p.rot(b, ("X", v["neck"] / n), ("Z", v["tug"] * 2 * wave(t, 6)))
            p.rot("Head", ("X", -v["neck"] * 0.75 - v["lean"]))
            p.side("Right", "UpperArm", ("Y", ad - (ad + 15) * v["reach"]), ("Z", -75 * v["reach"]))
            p.side("Right", "LowerArm", ("Z", -10 * v["reach"]))
            p.side("Left", "UpperArm", ("Y", ad + 10 * v["reach"]), ("Z", 30 * v["reach"]))
            p.sym("UpperLeg", ("X", -15 * v["reach"]))
            p.sym("LowerLeg", ("X", 25 * v["reach"]))
            r.nervous(p, t, 2.0)
            r.tail_sway(p, t, 15, 2)
            return p
        ph = [(0.0, dict(lean=0, neck=0, reach=0, tug=0)),
              (0.2, dict(lean=-8, neck=-25, reach=0.2, tug=0)),
              (0.42, dict(lean=22, neck=95, reach=1, tug=0.5)),
              (0.7, dict(lean=18, neck=80, reach=1, tug=1)),
              (0.82, dict(lean=-6, neck=10, reach=0.5, tug=0.3)),
              (1.0, dict(lean=0, neck=0, reach=0, tug=0))]
        return keyed(r, "Ability", 40, ph, fn)

    if style == "belly_flop":
        def fn(t, v):
            p = Pose().move("Hips", (0, -0.1 * r.h * v["flip"], v["z"] * r.h))
            p.rot("Hips", ("X", 80 * v["flip"]))
            p.rot("Spine", ("X", -10 * v["flip"]))
            p.sym("UpperArm", ("Y", v["arm"]), ("Z", 15 * v["flip"]))
            p.sym("LowerArm", ("Y", -20 * v["flip"]))
            p.sym("UpperLeg", ("X", -v["tuck"] + 35 * v["flip"]), ("Y", -10 * v["flip"]))
            p.sym("LowerLeg", ("X", v["tuck"] * 1.6))
            r.neck_rot(p, ("X", -45 * v["flip"] - 10 * v["tuck"] / 45))
            r.tail_sway(p, t, 20, 2)
            return p
        ph = [(0.0, dict(z=0, flip=0, arm=ad, tuck=0)),
              (0.15, dict(z=-0.12, flip=0, arm=ad + 15, tuck=45)),
              (0.35, dict(z=0.4, flip=0.2, arm=-55, tuck=55)),
              (0.5, dict(z=0.3, flip=0.9, arm=-5, tuck=10)),
              (0.6, dict(z=-0.12, flip=1.0, arm=5, tuck=0)),
              (0.7, dict(z=-0.06, flip=0.95, arm=0, tuck=0)),
              (1.0, dict(z=0, flip=0, arm=ad, tuck=0))]
        return keyed(r, "Ability", 40, ph, fn)

    # party (Julien): baile de la Fiesta Real
    def fn(t):
        b = abs(wave(t, 2))
        p = Pose().move("Hips", (0.04 * r.h * wave(t, 1), 0, -0.05 * r.h * b))
        p.rot("Hips", ("Z", 18 * wave(t, 1)), ("Y", 6 * wave(t, 1)))
        p.rot("Spine", ("Y", -10 * wave(t, 1)), ("X", -6))
        p.rot("Chest", ("Z", -10 * wave(t, 1)))
        for side, phs in (("Left", 0.0), ("Right", 0.5)):
            p.side(side, "UpperArm", ("Y", -55 + 30 * wave(t, 2, phs)), ("Z", -15 * wave(t, 1, phs)))
            p.side(side, "LowerArm", ("Y", -35 + 25 * wave(t, 4, phs)))
            p.side(side, "Hand", ("Y", -20 * wave(t, 4, phs)))
            p.side(side, "UpperLeg", ("X", -20 * b), ("Y", -8))
            p.side(side, "LowerLeg", ("X", 40 * b))
        r.neck_rot(p, ("X", 12 * wave(t, 2)), ("Z", 15 * wave(t, 1, 0.25)), head=0.6)
        r.tail_sway(p, t, 40, 2)
        return p
    return sample(r, "Ability", 48, fn, False)


# ---------------------------------------------------------------------------
# Resultado: hundidos y victoria
# ---------------------------------------------------------------------------
def disappointed(r):
    ad = r.arm_down

    def fn(t, v):
        p = Pose().move("Hips", (0, 0, -0.04 * r.h * v["slump"]))
        p.rot("Spine", ("X", 26 * v["slump"] - 6 * v["sigh"]))
        p.rot("Chest", ("X", 12 * v["slump"]))
        p.sym("Shoulder", ("Y", 12 * v["slump"] - 10 * v["sigh"]))
        p.sym("UpperArm", ("Y", ad + 10 * v["slump"] - 15 * v["sigh"]), ("Z", -14 * v["slump"]))
        p.sym("LowerArm", ("Z", -12 * v["slump"]))
        p.sym("UpperLeg", ("X", -12 * v["slump"]))
        p.sym("LowerLeg", ("X", 24 * v["slump"]))
        p.sym("Foot", ("X", -12 * v["slump"]))
        r.neck_rot(p, ("X", 42 * v["slump"] - 15 * v["sigh"]), ("Z", v["shake"] * 9 * wave(t, 3)), head=0.6)
        for i, b in enumerate(r.tail):
            p.rot(b, ("X", -16 * v["slump"]), ("Z", 4 * wave(t, 1, -0.1 * i)))
        return p
    ph = [(0.0, dict(slump=0, sigh=0, shake=0)),
          (0.25, dict(slump=0, sigh=1, shake=0)),
          (0.55, dict(slump=1, sigh=0, shake=0)),
          (0.7, dict(slump=1, sigh=0, shake=1)),
          (0.9, dict(slump=1, sigh=0, shake=0)),
          (1.0, dict(slump=1, sigh=0, shake=0))]
    return keyed(r, "Disappointed", 50, ph, fn)


def hand_on_hip(p, side):
    p.side(side, "UpperArm", ("Y", 42), ("Z", 18))
    p.side(side, "LowerArm", ("Y", 100))
    p.side(side, "Hand", ("Y", 20))


def victory(r, style):
    if style == "showman":
        def fn(t):
            b = abs(wave(t, 2))
            p = Pose().move("Hips", (0, 0, -0.03 * r.h * b))
            p.rot("Spine", ("X", -8), ("Y", 4 * wave(t, 1)))
            p.side("Right", "UpperArm", ("Y", -80 + 14 * wave(t, 2)), ("Z", -10))
            p.side("Right", "LowerArm", ("Y", -25 + 20 * wave(t, 2, 0.15)))
            hand_on_hip(p, "Left")
            p.sym("UpperLeg", ("X", -14 * b), ("Y", -8))
            p.sym("LowerLeg", ("X", 28 * b))
            r.neck_rot(p, ("X", -12), ("Z", 10 * wave(t, 1)))
            r.tail_sway(p, t, 20, 2)
            return p
    elif style == "bounce":
        def fn(t):
            up = max(0.0, math.sin(2 * math.pi * t * 2))
            p = Pose().move("Hips", (0, 0, 0.09 * r.h * up * r.amp - 0.02 * r.h))
            p.rot("Spine", ("X", -6 * up))
            p.sym("UpperArm", ("Y", -50 + 15 * wave(t, 2)), ("Z", -12))
            p.sym("LowerArm", ("Y", -20 * up))
            p.sym("UpperLeg", ("X", -28 * up))
            p.sym("LowerLeg", ("X", 50 * up))
            r.neck_rot(p, ("X", -15 * up), ("Z", 14 * wave(t, 1)))
            r.nervous(p, t)
            r.tail_sway(p, t, 30, 2)
            return p
    else:  # groove
        hi = 25 if r.h < 1.2 else 0

        def fn(t):
            b = abs(wave(t, 2))
            p = Pose().move("Hips", (0.035 * r.h * wave(t, 1), 0, -0.04 * r.h * b))
            p.rot("Hips", ("Y", 8 * wave(t, 1)), ("Z", 10 * wave(t, 1, 0.25)))
            p.rot("Spine", ("Y", -12 * wave(t, 1)))
            for side, phs in (("Left", 0.0), ("Right", 0.5)):
                p.side(side, "UpperArm", ("Y", 15 - hi + 30 * wave(t, 2, phs)), ("Z", -30))
                p.side(side, "LowerArm", ("Y", -60), ("Z", -20 * wave(t, 2, phs)))
                p.side(side, "Hand", ("Y", -25 * wave(t, 4, phs)))
                p.side(side, "UpperLeg", ("X", -12 * b))
                p.side(side, "LowerLeg", ("X", 24 * b))
            r.neck_rot(p, ("X", 10 * wave(t, 2)), ("Z", 12 * wave(t, 1, 0.25)), head=0.6)
            r.tail_sway(p, t, 30, 2)
            return p
    return sample(r, "Victory", 48, fn, True)


def select_pose(r, style):
    ad = r.arm_down
    if style == "hero":
        def fn(t):
            p = Pose().move("Hips", (0.01 * r.h * wave(t, 1), 0, 0.004 * r.h * wave(t, 2)))
            p.rot("Spine", ("X", -7 + 2 * wave(t, 2)))
            p.rot("Chest", ("X", -3 * wave(t, 2, 0.1)))
            hand_on_hip(p, "Left")
            hand_on_hip(p, "Right")
            p.sym("UpperLeg", ("Y", -9))
            p.sym("Foot", ("Y", 9))
            r.neck_rot(p, ("X", -10), ("Z", 14 * wave(t, 1)))
            r.tail_sway(p, t, 15, 1)
            return p
    elif style == "hyper":
        def fn(t):
            b = abs(math.sin(2 * math.pi * t * 4))
            p = Pose().move("Hips", (0, 0, 0.025 * r.h * b))
            p.rot("Spine", ("X", 4), ("Y", 5 * wave(t, 2)))
            p.side("Left", "UpperLeg", ("X", -30 * max(0.0, wave(t, 4))))
            p.side("Left", "LowerLeg", ("X", 50 * max(0.0, wave(t, 4))))
            p.side("Right", "UpperArm", ("Y", -40), ("Z", -20 + 10 * wave(t, 4)))
            p.side("Right", "LowerArm", ("Y", -60))
            p.side("Left", "UpperArm", ("Y", ad - 10), ("Z", 25 * wave(t, 2)))
            r.neck_rot(p, ("X", -6 + 6 * wave(t, 4)), ("Z", -12))
            r.tail_sway(p, t, 30, 4)
            return p
    elif style == "nervous":
        def fn(t):
            p = Pose().move("Hips", (0, 0, -0.02 * r.h))
            p.rot("Spine", ("X", 10 + 2 * wave(t, 2)))
            p.sym("UpperArm", ("Y", 55), ("Z", -42))
            p.sym("LowerArm", ("Z", -112 + 6 * wave(t, 4)))
            p.sym("Hand", ("Y", 10 * wave(t, 4, 0.2)))
            p.sym("UpperLeg", ("X", -10), ("Y", 6))
            p.sym("LowerLeg", ("X", 20))
            r.neck_rot(p, ("X", 12), ("Z", 25 * wave(t, 1)))
            r.nervous(p, t, 1.5)
            r.tail_sway(p, t, 10, 2)
            return p
    elif style == "sassy":
        def fn(t):
            p = Pose().move("Hips", (0.035 * r.h, 0, 0.005 * r.h * wave(t, 2)))
            p.rot("Hips", ("Y", -7))
            p.rot("Spine", ("Y", 12), ("X", -4))
            hand_on_hip(p, "Left")
            p.side("Right", "UpperArm", ("Y", -60), ("Z", -15))
            p.side("Right", "LowerArm", ("Y", -35 + 30 * wave(t, 3)))
            p.side("Right", "Hand", ("Y", -25 * wave(t, 3, 0.2)))
            p.side("Left", "UpperLeg", ("Y", -6))
            p.side("Right", "UpperLeg", ("X", -12), ("Y", 6))
            p.side("Right", "LowerLeg", ("X", 18))
            r.neck_rot(p, ("Z", -12), ("Y", -10 + 4 * wave(t, 1)), head=0.6)
            r.tail_sway(p, t, 25, 2)
            return p
    else:  # king
        def fn(t):
            p = Pose().move("Hips", (0.02 * r.h * wave(t, 1), 0, 0.006 * r.h * wave(t, 2)))
            p.rot("Hips", ("Z", 6 * wave(t, 1)))
            p.rot("Spine", ("X", -10))
            p.sym("UpperArm", ("Y", -38 + 8 * wave(t, 2)), ("Z", -8))
            p.sym("LowerArm", ("Y", -25))
            p.sym("Hand", ("Y", -20 * wave(t, 2, 0.1)))
            p.side("Left", "UpperLeg", ("X", -14), ("Y", -8))
            p.side("Left", "Foot", ("X", 14))
            r.neck_rot(p, ("X", -14), ("Z", 10 * wave(t, 1)), head=0.6)
            r.tail_sway(p, t, 35, 1)
            return p
    return sample(r, "Select_Pose", 72, fn, True)


# ---------------------------------------------------------------------------
def build_clips(arm, spec, name):
    r = Rig(arm, spec)
    ab, vi, se = STYLE[name]
    acts = {}
    acts["Shuffle_L"] = sample(r, "Shuffle_L", 24, lambda t: shuffle_pose(r, t), True)
    acts["Shuffle_R"] = sample(r, "Shuffle_R", 24, lambda t: mirror(shuffle_pose(r, t)), True)
    acts["Grab"] = sample(r, "Grab", 30, lambda t: grab_pose(r, t), True)
    acts["Ability"] = ability(r, ab)
    acts["Disappointed"] = disappointed(r)
    acts["Victory"] = victory(r, vi)
    acts["Select_Pose"] = select_pose(r, se)
    return acts


def export_clip(arm, act, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sc = bpy.context.scene
    f0, f1 = (int(round(x)) for x in act.frame_range)
    sc.frame_start, sc.frame_end = f0, f1
    sc.name = act.name  # nombre de la toma en Unity
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    arm.animation_data.action = act
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        bake_space_transform=True, add_leaf_bones=False, use_armature_deform_only=False, primary_bone_axis="Y",
        secondary_bone_axis="X", bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0)
    arm.animation_data.action = None


SHOT = {"Shuffle_L": (0.25, (0.9, -0.6, 0.3)), "Shuffle_R": (0.25, (-0.9, -0.6, 0.3)), "Grab": (0.3, (0.9, -0.6, 0.3)),
        "Ability": (0.5, (0.9, -0.6, 0.3)), "Disappointed": (1.0, (0.75, -0.8, 0.35)),
        "Victory": (0.25, (0.6, -0.9, 0.3)), "Select_Pose": (0.15, (0.6, -0.9, 0.3))}
ABILITY_SHOT = {"Alex": 0.5, "Marty": 0.45, "Melman": 0.55, "Gloria": 0.42, "Julien": 0.3}


def run(names):
    tiles = []
    files = []
    for name in names:
        mod = importlib.import_module(name.lower())
        captured = {}

        def fake_finish(n, body, arm, spec, poses=None):
            captured.update(body=body, arm=arm, spec=spec)
        gs.finish_character = fake_finish
        mod.build()
        body, arm, spec = captured["body"], captured["arm"], captured["spec"]
        acts = build_clips(arm, spec, name)
        for clip in CLIPS:
            path = os.path.join(OUT, name, f"{name}@{clip}.fbx")
            export_clip(arm, acts[clip], path)
            files.append((name, path))
        sc, cam = gs._setup_render(300)
        for clip in CLIPS:
            frac, d = SHOT[clip]
            if clip == "Ability":
                frac = ABILITY_SHOT[name]
            act = acts[clip]
            arm.animation_data.action = act
            sc.frame_set(int(round(act.frame_range[1] * frac)))
            p = os.path.join(gs.PREVIEW_DIR, f"_tmp_anim_{name}_{clip}.png")
            gs._render_view(p, [body], d, cam, margin=1.15)
            tiles.append(p)
        arm.animation_data.action = None
    sheet = os.path.join(gs.PREVIEW_DIR, "CharacterAnims_sheet.png" if len(names) > 1 else f"Anims_{names[0]}.png")
    gs.stitch(tiles, sheet, cols=len(CLIPS))
    for p in tiles:
        os.remove(p)
    print(f"[gs] preview {sheet}")
    if "--no-verify" not in sys.argv:
        for name in names:
            ref, _, _ = gs.verify_fbx(os.path.join(gs.EXPORT_DIR, "Characters", name, name + ".fbx"))
            for n, path in files:
                if n == name:
                    bones, clips, tris = gs.verify_fbx(path)
                    assert bones == ref, f"{path}: esqueleto distinto"
                    assert tris == 0 and clips, f"{path}: malla o sin clip"
        print("[gs] verificación OK: mismo esqueleto en todos los clips")


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    run([a for a in argv if not a.startswith("--")] or CHARS)
