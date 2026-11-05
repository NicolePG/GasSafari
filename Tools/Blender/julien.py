"""Rey Julien, el lémur (1.0 m). Caótico y fiestero, se cree el jefe: ojos enormes amarillos,
antifaz negro, una ceja levantada, sonrisa pícara, corona dorada torcida y cola anillada larguísima.
blender --background --python julien.py"""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
from gs_common import X, Vector

NAME = "Julien"
H = 1.0

J = dict(
    hips=(0, 0, 0.42), spine=(0, 0, 0.5), chest=(0, 0, 0.58), neck=(0, 0, 0.64), head=(0, 0, 0.69),
    head_end=(0, 0, 0.9),
    l_shoulder=(0.03, 0, 0.6), l_upperarm=(0.1, 0, 0.6), l_lowerarm=(0.235, 0, 0.6), l_hand=(0.36, 0, 0.6),
    l_hand_end=(0.43, 0, 0.6),
    l_upperleg=(0.06, 0, 0.41), l_lowerleg=(0.07, 0, 0.24), l_foot=(0.07, 0, 0.065), l_toes=(0.07, -0.07, 0.03),
    l_toes_end=(0.07, -0.13, 0.03),
)
TAIL = [(0, 0.1, 0.42), (0, 0.24, 0.34), (0, 0.38, 0.39), (0, 0.46, 0.54), (0, 0.48, 0.71), (0, 0.44, 0.85),
        (0, 0.36, 0.93)]


def build():
    gs.reset_scene()
    grey, light, dark = gs.mat("lemur_grey"), gs.mat("lemur_light"), gs.mat("lemur_dark")
    gold, goldd, mouth = gs.mat("gold", rough=0.35), gs.mat("gold_dark", rough=0.35), gs.mat("mouth")
    P = []
    P.append(gs.ellipsoid("Torso", (0, 0.0, 0.52), (0.14, 0.12, 0.155), grey))
    P.append(gs.ellipsoid("Belly", (0, -0.048, 0.5), (0.095, 0.072, 0.115), light))
    P.append(gs.capsule("NeckM", (0, 0, 0.6), (0, 0, 0.7), 0.065, grey))
    # cabeza: gris arriba, cara clara, antifaz negro y hocico oscuro
    hc = Vector((0, -0.01, 0.775))
    P.append(gs.ellipsoid("Head", hc, (0.15, 0.14, 0.13), grey, bone="Head"))
    P.append(gs.ellipsoid("Face", (0, -0.065, 0.75), (0.125, 0.095, 0.1), light, bone="Head"))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Mask{s}", (0.058 * s, -0.112, 0.79), (0.066, 0.035, 0.062), dark, bone="Head",
                              rot=("Y", 25 * s)))
    P.append(gs.ellipsoid("Snout", (0, -0.15, 0.725), (0.052, 0.07, 0.045), dark, bone="Head", rot=("X", 10)))
    P.append(gs.ellipsoid("Nose", (0, -0.214, 0.735), (0.022, 0.014, 0.016), gs.mat("black"), bone="Head"))
    # sonrisa pícara torcida hacia un lado
    grin = gs.on_front((0, -0.065, 0.75), (0.125, 0.095, 0.1),
                       [(-0.05, 0.685), (-0.025, 0.672), (0.0, 0.668), (0.03, 0.674), (0.058, 0.69),
                        (0.07, 0.702)], offset=0.002)
    P.append(gs.tube("Mouth", grin, 0.007, mouth, bone="Head", seg=6, cap=2, sub=2))
    for s in (1, -1):
        P += gs.eye(f"Eye{s}", (0.056 * s, -0.122, 0.79), 0.052, look=(0.15 * s, -1, 0.02),
                    iris_col="lemur_eye", pupil=0.66, side=s)
        # una ceja alta (la izquierda) y otra baja: aire de "yo mando"
        hi = 0.03 if s == 1 else 0.0
        P.append(gs.brow(f"Brow{s}", (0.022 * s, -0.152, 0.852 + hi * 0.4), (0.098 * s, -0.12, 0.86 + hi),
                         0.009, dark, raise_mid=0.01 + hi * 0.3))
        P.append(gs.cone(f"Ear{s}", (0.1 * s, 0.0, 0.83), (0.18 * s, 0.03, 0.9), 0.04, 0.012, grey, bone="Head"))
        P.append(gs.ellipsoid(f"EarIn{s}", (0.13 * s, -0.016, 0.86), (0.022, 0.008, 0.026), light, bone="Head",
                              rot=("Y", -40 * s)))
    # corona dorada torcida con puntas y gemas
    crown_c = Vector((0.015, 0.0, 0.905))
    tilt = [("Y", -12), ("X", -6)]
    rot = gs._rot_matrix(tilt)
    P.append(gs.rounded_cylinder("CrownBand", crown_c, 0.065, 0.04, 0.008, gold, bone="Head", seg=20, rot=rot))
    for k in range(5):
        a = k / 5 * 2 * math.pi + math.pi / 2
        base = crown_c + rot.to_3x3() @ Vector((0.058 * math.cos(a), 0.058 * math.sin(a), 0.015))
        tip = crown_c + rot.to_3x3() @ Vector((0.062 * math.cos(a), 0.062 * math.sin(a), 0.06))
        P.append(gs.cone(f"CrownPt{k}", base, tip, 0.02, 0.006, gold, bone="Head", seg=8))
        P.append(gs.ellipsoid(f"CrownBall{k}", tip, 0.011, goldd, bone="Head"))
    for k, col in enumerate(("gem_red", "gem_green", "gem_red")):
        a = math.radians(-90 + (k - 1) * 40)
        p = crown_c + rot.to_3x3() @ Vector((0.067 * math.cos(a), 0.067 * math.sin(a), 0.0))
        P.append(gs.ellipsoid(f"Gem{k}", p, 0.012, gs.mat(col, rough=0.2), bone="Head"))
    # cola anillada larguísima (6 huesos)
    P.append(gs.tube("Tail", TAIL, [0.035, 0.036, 0.036, 0.035, 0.034, 0.033, 0.03], [light, dark], seg=10,
                     sub=4, mat_fn=lambda t: 1 if (t * 13) % 1.0 > 0.5 else 0))

    def hand(side, s, j):
        c = X(j["l_hand"], s) + Vector((0.035 * s, 0, 0))
        out = [gs.ellipsoid(f"{side}Palm", c, (0.042, 0.036, 0.03), dark, bone=f"{side}Hand")]
        for k in (-1, 0, 1):
            out.append(gs.capsule(f"{side}Finger{k}", c + Vector((0.025 * s, 0.018 * k, 0)),
                                  c + Vector((0.062 * s, 0.022 * k, -0.005)), 0.011, dark, bone=f"{side}Hand",
                                  sub=1))
        out.append(gs.capsule(f"{side}Thumb", c + Vector((0.0, -0.025, 0)), c + Vector((0.02 * s, -0.05, 0.004)),
                              0.011, dark, bone=f"{side}Hand", sub=1))
        return out

    def foot(side, s, j):
        c = X((0.07, -0.035, 0.03), s)
        return [gs.ellipsoid(f"{side}FootM", c, (0.045, 0.08, 0.032), dark, bone=f"{side}Foot")]

    P += gs.standard_limbs(J, grey, grey, dict(ua=(0.048, 0.043), la=(0.043, 0.04), ul=(0.064, 0.056),
                                                ll=(0.054, 0.05)), hand, foot)
    arm = gs.build_armature(NAME, J, tail=TAIL)
    body = gs.join_and_skin(NAME, P, arm)
    spec = dict(height=H, sit_drop=0.36, tail=[f"Tail{i}" for i in range(1, 7)], amp=1.35, speed=1.3)
    return gs.finish_character(NAME, body, arm, spec)


if __name__ == "__main__":
    build()
