"""Marty, la cebra (1.7 m). Hiperactivo: cresta en punta, sonrisa enorme, cejas muy arriba.
blender --background --python marty.py"""
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
from gs_common import X, Vector

NAME = "Marty"
H = 1.7

J = dict(
    hips=(0, 0, 0.78), spine=(0, 0, 0.92), chest=(0, 0, 1.06), neck=(0, 0, 1.18), head=(0, 0, 1.36),
    head_end=(0, 0, 1.64),
    l_shoulder=(0.06, 0, 1.14), l_upperarm=(0.2, 0, 1.14), l_lowerarm=(0.42, 0, 1.14), l_hand=(0.62, 0, 1.14),
    l_hand_end=(0.74, 0, 1.14),
    l_upperleg=(0.11, 0, 0.76), l_lowerleg=(0.12, 0, 0.43), l_foot=(0.12, 0, 0.14), l_toes=(0.12, -0.1, 0.05),
    l_toes_end=(0.12, -0.19, 0.05),
)
TAIL = [(0, 0.17, 0.80), (0, 0.31, 0.72), (0, 0.41, 0.62), (0, 0.46, 0.52), (0, 0.47, 0.42)]


def stripes(n=5, duty=0.45):
    return lambda t: 1 if ((t * n) % 1.0) > (1 - duty) and 0.08 < t < 0.92 else 0


def build():
    gs.reset_scene()
    white, black, muzzle = gs.mat("zebra_white"), gs.mat("zebra_black"), gs.mat("zebra_muzzle")
    hoof, mouth, eyew = gs.mat("hoof"), gs.mat("mouth"), gs.mat("eye_white")
    WB = [white, black]
    P = []
    # tronco con rayas (cortes limpios) y panza blanca
    torso = gs.ellipsoid("Torso", (0, 0.01, 0.98), (0.22, 0.19, 0.29), WB, seg=24, rings=16)
    gs.bands_by_plane(torso, (0, 0.35, 1), (0, 0, 0.98), 0.085, 0.42, 1)
    gs.paint_faces(torso, lambda c: c.y < -0.1 and abs(c.x) < 0.13, 0)
    P.append(torso)
    neck = gs.capsule("NeckM", (0, 0.01, 1.12), (0, -0.01, 1.42), 0.095, WB, sub=8, mat_fn=stripes(4, 0.4))
    P.append(neck)
    # cabeza alargada con hocico gris
    hc = Vector((0, -0.02, 1.47))
    head = gs.ellipsoid("Head", hc, (0.17, 0.17, 0.18), WB, bone="Head", seg=22, rings=14)
    gs.bands_by_plane(head, (0, 0.6, 1), hc, 0.07, 0.38, 1)
    gs.paint_faces(head, lambda c: c.y < -0.08 and c.z < 1.56, 0)
    P.append(head)
    P.append(gs.ellipsoid("Muzzle", (0, -0.17, 1.38), (0.13, 0.16, 0.11), muzzle, bone="Head",
                          rot=("X", -12)))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Nostril{s}", (0.05 * s, -0.315, 1.405), (0.022, 0.012, 0.016), black,
                              bone="Head"))
    # sonrisa enorme con dientes
    P.append(gs.ellipsoid("Grin", (0, -0.285, 1.33), (0.085, 0.035, 0.038), mouth, bone="Head",
                          rot=("X", -15)))
    P.append(gs.ellipsoid("Teeth", (0, -0.312, 1.345), (0.062, 0.014, 0.017), eyew, bone="Head",
                          rot=("X", -15)))
    P.append(gs.ellipsoid("Tongue", (0, -0.3, 1.315), (0.04, 0.018, 0.014), gs.mat("tongue"), bone="Head"))
    for s in (1, -1):
        P += gs.eye(f"Eye{s}", (0.083 * s, -0.105, 1.53), 0.07, look=(0.28 * s, -1, 0.06), iris_col="#6A3E1C",
                    pupil=0.62, side=s)
        P.append(gs.brow(f"Brow{s}", (0.035 * s, -0.15, 1.625), (0.13 * s, -0.11, 1.645), 0.015, black,
                         raise_mid=0.03))
        # orejas en punta, blancas con punta negra
        P.append(gs.cone(f"Ear{s}", (0.085 * s, 0.01, 1.6), (0.16 * s, 0.05, 1.74), 0.048, 0.012, white,
                         bone="Head"))
        P.append(gs.ellipsoid(f"EarTip{s}", (0.152 * s, 0.045, 1.725), (0.022, 0.018, 0.03), black, bone="Head"))
        P.append(gs.ellipsoid(f"EarIn{s}", (0.115 * s, -0.003, 1.65), (0.02, 0.01, 0.045), gs.mat("blush"),
                              bone="Head", rot=("Y", -25 * s)))
    # cresta: mechones negros desde la frente hasta la nuca y el cuello
    crest = [((0, -0.1, 1.64), "Head", 0.06), ((0, -0.02, 1.68), "Head", 0.07), ((0, 0.07, 1.66), "Head", 0.068),
             ((0, 0.14, 1.58), "Head", 0.06), ((0, 0.12, 1.42), "Neck", 0.055), ((0, 0.11, 1.30), "Neck", 0.05),
             ((0, 0.1, 1.19), "Chest", 0.045)]
    for i, (p, b, r) in enumerate(crest):
        P.append(gs.ellipsoid(f"Crest{i}", p, (0.028, r, r * 1.1), black, bone=b, rot=("X", -25 + i * 18)))
    # cola con borla negra
    P.append(gs.tube("Tail", TAIL[:-1], [0.03, 0.026, 0.024, 0.022], WB, seg=8, sub=3, mat_fn=stripes(4, 0.45)))
    P.append(gs.ellipsoid("TailTuft", (0, 0.468, 0.455), (0.042, 0.042, 0.08), black, bone="Tail4"))

    def hand(side, s, j):
        c = X(j["l_hand"], s) + Vector((0.05 * s, 0, 0))
        return [gs.rounded_cylinder(f"{side}Hoof", c, 0.068, 0.11, 0.025, hoof, axis=(s, 0, 0),
                                    bone=f"{side}Hand", seg=16)]

    def foot(side, s, j):
        c = X((0.12, -0.03, 0.065), s)
        return [gs.rounded_cylinder(f"{side}FootHoof", c, 0.085, 0.13, 0.03, hoof, bone=f"{side}Foot", seg=16,
                                    scale=(1.0, 1.25, 1.0))]

    P += gs.standard_limbs(J, WB, WB, dict(ua=(0.075, 0.068), la=(0.068, 0.062), ul=(0.105, 0.088),
                                            ll=(0.085, 0.075)), hand, foot,
                           arm_mat_fn=stripes(3, 0.4), leg_mat_fn=stripes(3, 0.4), sub=9)
    arm = gs.build_armature(NAME, J, tail=TAIL)
    body = gs.join_and_skin(NAME, P, arm)
    spec = dict(height=H, sit_drop=0.64, tail=["Tail1", "Tail2", "Tail3", "Tail4"], amp=1.25, speed=1.4)
    return gs.finish_character(NAME, body, arm, spec)


if __name__ == "__main__":
    build()
