"""Gloria, la hipopótama (1.6 m). Segura y con ritmo: cuerpo redondo, mirada coqueta con pestañas,
sonrisa grande y una flor junto a la oreja.
blender --background --python gloria.py"""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_common as gs
from gs_common import X, Vector

NAME = "Gloria"
H = 1.6

J = dict(
    hips=(0, 0, 0.55), spine=(0, 0, 0.72), chest=(0, 0, 0.92), neck=(0, 0, 1.08), head=(0, 0, 1.16),
    head_end=(0, 0, 1.5),
    l_shoulder=(0.08, 0, 1.0), l_upperarm=(0.3, 0, 1.0), l_lowerarm=(0.52, 0, 1.0), l_hand=(0.7, 0, 1.0),
    l_hand_end=(0.8, 0, 1.0),
    l_upperleg=(0.16, 0, 0.52), l_lowerleg=(0.17, 0, 0.31), l_foot=(0.17, 0, 0.12), l_toes=(0.17, -0.11, 0.05),
    l_toes_end=(0.17, -0.21, 0.05),
)
TAIL = [(0, 0.3, 0.52), (0, 0.38, 0.49), (0, 0.43, 0.47), (0, 0.46, 0.46)]


def build():
    gs.reset_scene()
    skin, light, dark = gs.mat("hippo_skin"), gs.mat("hippo_light"), gs.mat("hippo_dark")
    pink, mouth, black = gs.mat("hippo_pink"), gs.mat("mouth"), gs.mat("black")
    P = []
    P.append(gs.ellipsoid("Torso", (0, 0.0, 0.76), (0.36, 0.32, 0.42), skin, seg=26, rings=16))
    P.append(gs.ellipsoid("Belly", (0, -0.125, 0.72), (0.28, 0.22, 0.32), light))
    P.append(gs.capsule("NeckM", (0, 0, 1.02), (0, -0.02, 1.2), 0.17, skin))
    # cabeza ancha con hocico enorme
    hc = Vector((0, -0.04, 1.33))
    P.append(gs.ellipsoid("Head", hc, (0.24, 0.22, 0.21), skin, bone="Head"))
    mc, mr = (0, -0.2, 1.21), (0.235, 0.17, 0.15)
    P.append(gs.ellipsoid("Muzzle", mc, mr, light, bone="Head"))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Nostril{s}", (0.075 * s, -0.31, 1.31), (0.026, 0.02, 0.018), dark, bone="Head",
                              rot=("X", 40)))
        P.append(gs.ellipsoid(f"Cheek{s}", (0.17 * s, -0.25, 1.26), (0.055, 0.03, 0.035), pink, bone="Head",
                              rot=("Z", 35 * s)))
    smile = gs.on_front(mc, mr, gs.arc_points((0, 0, 1.235), 0.13, 205, 335, 11), offset=-0.004)
    P.append(gs.tube("Mouth", smile, 0.013, mouth, bone="Head", seg=8, cap=2, sub=2))
    for s in (1, -1):
        tc = gs.on_front(mc, mr, [(0.028 * s, 1.11)], offset=0.004)[0]
        P.append(gs.ellipsoid(f"Tooth{s}", tc, (0.022, 0.012, 0.024), gs.mat("eye_white"), bone="Head"))
    for s in (1, -1):
        ec = Vector((0.1 * s, -0.17, 1.43))
        P += gs.eye(f"Eye{s}", ec, 0.072, look=(0.2 * s, -1, 0.1), iris_col="#5A3E8C", pupil=0.58,
                    lid_col="hippo_skin", lid_angle=52, lid_tilt=-14, side=s)
        # pestañas
        for k in range(3):
            a = math.radians(30 + k * 28)
            base = ec + Vector((s * 0.06 * math.cos(a), -0.02, 0.07 * math.sin(a) - 0.005))
            tip = base + Vector((s * 0.035 * math.cos(a), -0.008, 0.04 * math.sin(a) + 0.008))
            P.append(gs.capsule(f"Lash{s}_{k}", base, tip, 0.007, black, bone="Head", r1=0.004, sub=1))
        P.append(gs.ellipsoid(f"Ear{s}", (0.18 * s, 0.03, 1.5), (0.055, 0.03, 0.05), skin, bone="Head"))
        P.append(gs.ellipsoid(f"EarIn{s}", (0.18 * s, 0.006, 1.5), (0.034, 0.01, 0.03), pink, bone="Head"))
    # flor junto a la oreja izquierda
    fc = Vector((0.2, -0.05, 1.5))
    for k in range(5):
        a = k / 5 * 2 * math.pi
        P.append(gs.ellipsoid(f"Petal{k}", fc + Vector((0.04 * math.cos(a), -0.02, 0.04 * math.sin(a))),
                              (0.03, 0.014, 0.03), gs.mat("#FF5FA2"), bone="Head"))
    P.append(gs.ellipsoid("FlowerCenter", fc + Vector((0, -0.03, 0)), 0.022, gs.mat("gold"), bone="Head"))
    # colita
    P.append(gs.tube("Tail", TAIL, [0.04, 0.032, 0.026, 0.02], skin, seg=8, sub=2))

    def hand(side, s, j):
        c = X(j["l_hand"], s) + Vector((0.06 * s, 0, 0))
        out = [gs.ellipsoid(f"{side}Mitt", c, (0.12, 0.11, 0.1), skin, bone=f"{side}Hand")]
        for k in (-1, 0, 1):
            out.append(gs.ellipsoid(f"{side}Nail{k}", c + Vector((0.105 * s, -0.04 + 0.04 * k, 0.0)),
                                    (0.02, 0.022, 0.022), light, bone=f"{side}Hand"))
        return out

    def foot(side, s, j):
        c = X((0.17, -0.04, 0.07), s)
        out = [gs.ellipsoid(f"{side}FootM", c, (0.14, 0.17, 0.075), skin, bone=f"{side}Foot")]
        for k in (-1, 0, 1):
            out.append(gs.ellipsoid(f"{side}Nail{k}", c + Vector((0.06 * k, -0.155, 0.0)), (0.03, 0.022, 0.03),
                                    light, bone=f"{side}Toes"))
        return out

    P += gs.standard_limbs(J, skin, skin, dict(ua=(0.11, 0.1), la=(0.1, 0.095), ul=(0.15, 0.135),
                                                ll=(0.13, 0.12)), hand, foot)
    arm = gs.build_armature(NAME, J, tail=TAIL)
    body = gs.join_and_skin(NAME, P, arm)
    spec = dict(height=H, sit_drop=0.43, tail=["Tail1", "Tail2", "Tail3"], amp=1.0, speed=0.9, arm_down=50)
    return gs.finish_character(NAME, body, arm, spec)


if __name__ == "__main__":
    build()
