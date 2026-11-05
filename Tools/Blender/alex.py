"""Alex, el león (1.8 m). Artista seguro de sí mismo: melena enorme, cejas arriba, sonrisa de showman.
blender --background --python alex.py"""
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
from gs_common import X, Vector

NAME = "Alex"
H = 1.8

J = dict(
    hips=(0, 0, 0.80), spine=(0, 0, 0.95), chest=(0, 0, 1.10), neck=(0, 0, 1.26), head=(0, 0, 1.36),
    head_end=(0, 0, 1.72),
    l_shoulder=(0.06, 0, 1.20), l_upperarm=(0.22, 0, 1.20), l_lowerarm=(0.46, 0, 1.20), l_hand=(0.66, 0, 1.20),
    l_hand_end=(0.78, 0, 1.20),
    l_upperleg=(0.12, 0, 0.78), l_lowerleg=(0.13, 0, 0.44), l_foot=(0.13, 0, 0.11), l_toes=(0.13, -0.15, 0.05),
    l_toes_end=(0.13, -0.25, 0.05),
)
TAIL = [(0, 0.18, 0.80), (0, 0.38, 0.68), (0, 0.55, 0.74), (0, 0.63, 0.92), (0, 0.64, 1.04)]


def build():
    gs.reset_scene()
    fur, cream, mane, mane2 = gs.mat("lion_fur"), gs.mat("lion_cream"), gs.mat("lion_mane"), gs.mat("lion_mane2")
    dark, mouth = gs.mat("nose_dark"), gs.mat("mouth")
    P = []
    # tronco
    P.append(gs.ellipsoid("Torso", (0, 0.01, 1.0), (0.25, 0.21, 0.31), fur))
    P.append(gs.ellipsoid("Belly", (0, -0.085, 0.96), (0.185, 0.155, 0.235), cream))
    P.append(gs.capsule("NeckM", (0, 0, 1.18), (0, 0, 1.36), 0.11, fur))
    # cabeza
    hc = Vector((0, -0.03, 1.46))
    P.append(gs.ellipsoid("Head", hc, (0.215, 0.2, 0.2), fur, bone="Head"))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Cheek{s}", (0.068 * s, -0.185, 1.395), (0.085, 0.075, 0.07), cream, bone="Head"))
    P.append(gs.ellipsoid("Chin", (0, -0.165, 1.335), (0.075, 0.065, 0.05), cream, bone="Head"))
    P.append(gs.ellipsoid("Nose", (0, -0.258, 1.448), (0.052, 0.036, 0.034), dark, bone="Head"))
    P.append(gs.tube("Mouth", gs.arc_points((0, -0.24, 1.395), 0.075, 205, 335, 9, bulge=0.012),
                     0.011, mouth, bone="Head", seg=8, cap=2, sub=2))
    for s in (1, -1):
        P += gs.eye(f"Eye{s}", (0.088 * s, -0.165, 1.535), 0.072, look=(0.18 * s, -1, 0.02), iris_col="#B86B12",
                    pupil=0.6, side=s)
        P.append(gs.brow(f"Brow{s}", (0.04 * s, -0.205, 1.625), (0.15 * s, -0.165, 1.645), 0.017, mane,
                         raise_mid=0.022))
        # orejas sobre la melena
        P.append(gs.ellipsoid(f"Ear{s}", (0.165 * s, 0.02, 1.66), (0.06, 0.035, 0.062), fur, bone="Head"))
        P.append(gs.ellipsoid(f"EarIn{s}", (0.165 * s, -0.008, 1.655), (0.036, 0.012, 0.04), cream, bone="Head"))
    # melena: anillo de mechones alrededor de la cara + volumen trasero + pechera
    import math
    n = 14
    for i in range(n):
        a = i / n * 2 * math.pi
        r = 0.235
        p = hc + Vector((math.sin(a) * r, 0.05, math.cos(a) * r * 1.02))
        P.append(gs.ellipsoid(f"Mane{i}", p, (0.105, 0.1, 0.105), mane if i % 2 == 0 else mane2, bone="Head",
                              seg=12, rings=7))
    P.append(gs.ellipsoid("ManeBack", hc + Vector((0, 0.12, 0.0)), (0.27, 0.16, 0.28), mane, bone="Head"))
    for i, (x, z) in enumerate(((0, 1.24), (0.08, 1.27), (-0.08, 1.27))):
        P.append(gs.ellipsoid(f"Bib{i}", (x, -0.11, z), (0.08, 0.07, 0.08), mane2, bone="Chest", seg=12, rings=8))
    # cola con borla
    P.append(gs.tube("Tail", TAIL[:-1], [0.035, 0.03, 0.028, 0.026], fur, seg=10, sub=3))
    P.append(gs.ellipsoid("TailTuft", TAIL[-1], (0.065, 0.065, 0.085), mane, bone="Tail4", seg=12, rings=8))

    def hand(side, s, j):
        c = X(j["l_hand"], s) + Vector((0.07 * s, 0, 0))
        out = [gs.ellipsoid(f"{side}Paw", c, (0.085, 0.075, 0.07), fur, bone=f"{side}Hand")]
        out.append(gs.ellipsoid(f"{side}Thumb", c + Vector((-0.03 * s, -0.065, 0.01)), 0.032, fur,
                                bone=f"{side}Hand", seg=10, rings=6))
        out.append(gs.ellipsoid(f"{side}Pad", c + Vector((0.0, 0, -0.055)), (0.05, 0.045, 0.02), cream,
                                bone=f"{side}Hand", seg=12, rings=6))
        return out

    def foot(side, s, j):
        c = X((0.13, -0.07, 0.065), s)
        out = [gs.ellipsoid(f"{side}FootM", c, (0.1, 0.155, 0.068), fur, bone=f"{side}Foot")]
        for k in (-1, 0, 1):
            out.append(gs.ellipsoid(f"{side}Toe{k}", c + Vector((0.048 * k, -0.135, -0.01)), 0.034, cream,
                                    bone=f"{side}Toes", seg=10, rings=6))
        return out

    P += gs.standard_limbs(J, fur, fur, dict(ua=(0.088, 0.078), la=(0.078, 0.07), ul=(0.12, 0.1), ll=(0.098, 0.088)),
                           hand, foot)
    arm = gs.build_armature(NAME, J, tail=TAIL)
    body = gs.join_and_skin(NAME, P, arm)
    spec = dict(height=H, sit_drop=0.66, tail=["Tail1", "Tail2", "Tail3", "Tail4"], amp=1.1, speed=1.0)
    return gs.finish_character(NAME, body, arm, spec)


if __name__ == "__main__":
    build()
