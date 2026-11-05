"""Melman, la jirafa (2.6 m). Nervioso e hipocondríaco: párpados caídos, cejas de preocupación,
boca temblorosa y una curita en el cuello. Cuello con 3 huesos extra (Neck1..Neck3).
blender --background --python melman.py"""
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
from gs_common import X, Vector

NAME = "Melman"
H = 2.6

J = dict(
    hips=(0, 0, 0.95), spine=(0, 0, 1.07), chest=(0, 0, 1.2), neck=(0, 0, 1.32), head=(0, -0.05, 2.25),
    head_end=(0, -0.05, 2.5),
    l_shoulder=(0.05, 0, 1.24), l_upperarm=(0.17, 0, 1.24), l_lowerarm=(0.42, 0, 1.24), l_hand=(0.66, 0, 1.24),
    l_hand_end=(0.77, 0, 1.24),
    l_upperleg=(0.1, 0, 0.93), l_lowerleg=(0.11, 0, 0.5), l_foot=(0.11, 0, 0.14), l_toes=(0.11, -0.1, 0.05),
    l_toes_end=(0.11, -0.18, 0.05),
)
NECK = [(0, 0, 1.32), (0, -0.02, 1.56), (0, -0.04, 1.8), (0, -0.05, 2.04), (0, -0.05, 2.25)]
TAIL = [(0, 0.16, 0.98), (0, 0.3, 0.86), (0, 0.38, 0.72), (0, 0.41, 0.6), (0, 0.42, 0.5)]


def build():
    gs.reset_scene()
    rnd = random.Random(7)
    yel, patch, cream = gs.mat("giraffe_yellow"), gs.mat("giraffe_patch"), gs.mat("giraffe_cream")
    hoof, osc, mouth = gs.mat("hoof"), gs.mat("ossicone"), gs.mat("mouth")
    P = []

    spots = []

    def spotted(obj, count, size, keep=None):
        spots.extend(gs.stickers(obj, count, size, patch, rnd, keep_fn=keep))
        return obj

    torso = gs.ellipsoid("Torso", (0, 0.0, 1.12), (0.2, 0.18, 0.24), yel)
    P.append(spotted(torso, 12, 0.08, keep=lambda c: not (c.y < -0.06 and abs(c.x) < 0.14)))
    P.append(gs.ellipsoid("Belly", (0, -0.075, 1.09), (0.14, 0.12, 0.17), cream))
    # cuello largo (pesos automáticos repartidos en Neck..Neck3)
    neck_pts = [Vector((0, 0.0, 1.22))] + [Vector(p) for p in NECK[1:]] + [Vector((0, -0.06, 2.3))]
    neck = gs.tube("NeckM", neck_pts, [0.115, 0.1, 0.09, 0.083, 0.08, 0.08], yel, seg=14, sub=4)
    P.append(spotted(neck, 16, 0.065, keep=lambda c: (c.y > -0.05 or c.z < 1.3) and c.z < 2.15))
    mane = [p + Vector((0, 0.085 - 0.012 * i, 0)) for i, p in enumerate(neck_pts[1:])]
    P.append(gs.tube("Mane", mane, 0.028, patch, seg=8, sub=3, flatten=((1, 0, 0), 0.55)))
    # curita en el cuello (hipocondríaco)
    P.append(gs.rounded_cylinder("BandAid", (0.0, -0.13, 1.68), 0.05, 0.012, 0.005, gs.mat("#F1C9A0"),
                                 axis=(0.2, -1, 0), bone="Neck1", seg=12, scale=(1.0, 0.45, 1.0)))
    # cabeza
    hc = Vector((0, -0.08, 2.34))
    P.append(gs.ellipsoid("Head", hc, (0.175, 0.18, 0.17), yel, bone="Head"))
    P.append(gs.ellipsoid("Muzzle", (0, -0.23, 2.27), (0.12, 0.14, 0.095), cream, bone="Head", rot=("X", -8)))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Nostril{s}", (0.048 * s, -0.362, 2.3), (0.016, 0.01, 0.012), patch, bone="Head"))
    P.append(gs.tube("Mouth", [(-0.055, -0.35, 2.228), (-0.022, -0.358, 2.241), (0.013, -0.358, 2.228),
                               (0.05, -0.35, 2.24)], 0.009, mouth, bone="Head", seg=6, cap=2, sub=3))
    for s in (1, -1):
        P += gs.eye(f"Eye{s}", (0.085 * s, -0.185, 2.4), 0.08, look=(0.22 * s, -1, 0.0), iris_col="#5B3A1E",
                    pupil=0.38, lid_col="giraffe_yellow", lid_angle=38, lid_tilt=-16, side=s)
        P.append(gs.brow(f"Brow{s}", (0.03 * s, -0.235, 2.508), (0.135 * s, -0.195, 2.47), 0.014, patch,
                         raise_mid=0.006))
        P.append(gs.capsule(f"Ossicone{s}", (0.06 * s, -0.05, 2.47), (0.075 * s, -0.035, 2.55), 0.024, yel,
                            bone="Head", sub=2))
        P.append(gs.ellipsoid(f"OssTip{s}", (0.077 * s, -0.033, 2.562), 0.036, osc, bone="Head"))
        P.append(gs.ellipsoid(f"Ear{s}", (0.18 * s, -0.04, 2.42), (0.075, 0.022, 0.036), yel, bone="Head",
                              rot=("Y", -20 * s)))
        P.append(gs.ellipsoid(f"EarIn{s}", (0.185 * s, -0.058, 2.42), (0.05, 0.008, 0.022), gs.mat("blush"),
                              bone="Head", rot=("Y", -20 * s)))
    # cola con borla
    P.append(gs.tube("Tail", TAIL[:-1], [0.026, 0.022, 0.02, 0.019], yel, seg=8, sub=3))
    P.append(gs.ellipsoid("TailTuft", (0, 0.418, 0.52), (0.04, 0.04, 0.075), osc, bone="Tail4"))

    def hand(side, s, j):
        c = X(j["l_hand"], s) + Vector((0.045 * s, 0, 0))
        return [gs.rounded_cylinder(f"{side}Hoof", c, 0.058, 0.1, 0.022, hoof, axis=(s, 0, 0), bone=f"{side}Hand",
                                    seg=14)]

    def foot(side, s, j):
        c = X((0.11, -0.03, 0.065), s)
        return [gs.rounded_cylinder(f"{side}FootHoof", c, 0.075, 0.13, 0.028, hoof, bone=f"{side}Foot", seg=14,
                                    scale=(1.0, 1.25, 1.0))]

    limbs = gs.standard_limbs(J, yel, yel, dict(ua=(0.066, 0.06), la=(0.06, 0.055), ul=(0.094, 0.08),
                                               ll=(0.076, 0.068)), hand, foot, sub=4)
    for o in limbs:
        if o.name.endswith("_m"):
            spotted(o, 5 if "Leg" in o.name else 3, 0.055)
    P += limbs + spots
    arm = gs.build_armature(NAME, J, neck_chain=NECK, tail=TAIL)
    body = gs.join_and_skin(NAME, P, arm)
    spec = dict(height=H, sit_drop=0.83, tail=["Tail1", "Tail2", "Tail3", "Tail4"],
                neck=["Neck", "Neck1", "Neck2", "Neck3"], amp=1.0, speed=1.1, jitter=1.0)
    return gs.finish_character(NAME, body, arm, spec)


if __name__ == "__main__":
    build()
