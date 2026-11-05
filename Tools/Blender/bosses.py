"""Jefe W1-5: grúa de puerto con gancho (docs/05 H24, docs/09 §3: ≤ 8k triángulos, rig Generic con huesos simples).
blender --background --python bosses.py

Mira hacia -Y (el brazo apunta al río). Pivote en el suelo, en el centro del pórtico. Piezas rígidas, una por hueso:
Root, Wheel_1..8 (ruedas de los carros), Turret (cabina que gira), Boom (brazo), Trolley (carro del cable) y Hook
(motón y gancho). El cable se estira entre Trolley y Hook. Clips: Idle, Move (en el sitio; el código desplaza la
grúa por los rieles) y Lower_Hook (cae ~4.5 m y vuelve a subir).
Salida: Tools/Export/Bosses/Crane.fbx
"""
import math
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import gs_common as gs
from gs_common import Pose, Vector, wave

OUT = os.path.join(gs.EXPORT_DIR, "Bosses")
HOOK_Y = -10.5
WHEELS = [(sx * 2.8, sy * 1.8 + dy, 0.42) for sx in (1, -1) for sy in (1, -1) for dy in (-0.42, 0.42)]


def hazard(obj, period=0.6):
    """Franjas diagonales negras sobre el material 0 (el objeto debe tener [amarillo, negro])."""
    gs.bands_by_plane(obj, (1, 0, 1), (0, 0, 0), period, 0.5, 1, cut=True)
    return obj


def build_crane():
    gs.reset_scene()
    yel, blk = gs.mat("#FFC21A", rough=0.5), gs.mat("#22232B", rough=0.6)
    grey, dark = gs.mat("#5C6470", rough=0.6), gs.mat("#33363F", rough=0.6)
    red, white = gs.mat("#E2283B", rough=0.4), gs.mat("#F7F4EC", rough=0.4)
    glass = gs.mat("#3AE1FF", rough=0.15, emission=2.0)
    neon = gs.mat("#FF4FA3", rough=0.2, emission=3.0)
    amber = gs.mat("#FFB238", rough=0.2, emission=3.0)
    cable = gs.mat("#2B2B30", rough=0.8)

    bones = [("Root", (0, 0, 0), (0, 0, 1.0), None)]
    for i, w in enumerate(WHEELS):
        bones.append((f"Wheel_{i + 1}", w, (w[0] + 0.4 * (1 if w[0] > 0 else -1), w[1], w[2]), "Root"))
    bones += [("Turret", (0, 0, 6.9), (0, 0, 8.0), "Root"),
              ("Boom", (0, -1.9, 7.15), (0, -12.6, 8.9), "Turret"),
              ("Trolley", (0, HOOK_Y, 8.2), (0, HOOK_Y, 7.6), "Boom"),
              ("Hook", (0, HOOK_Y, 3.3), (0, HOOK_Y, 2.3), "Trolley")]
    arm = gs.build_generic_armature("Crane", bones)

    P = []

    def add(o, bone):
        o["bone"] = bone
        P.append(o)
        return o

    # --- pórtico: carros con ruedas, patas en A y plataforma
    for sx in (1, -1):
        for sy in (1, -1):
            add(gs.rounded_box(f"Bogie{sx}{sy}", (sx * 2.8, sy * 1.8, 0.62), (0.9, 1.6, 0.55), 0.12, [yel, blk]),
                "Root")
            add(hazard(gs.rounded_box(f"LegFoot{sx}{sy}", (sx * 2.8, sy * 1.8, 1.25), (0.85, 0.85, 0.7), 0.15,
                                      [yel, blk])), "Root")
            add(gs.capsule(f"Leg{sx}{sy}", (sx * 2.8, sy * 1.8, 1.4), (sx * 2.3, sy * 1.5, 6.1), 0.36, yel,
                           r1=0.3, seg=8, cap=2, sub=1), "Root")
    for i, w in enumerate(WHEELS):
        add(gs.rounded_cylinder(f"Wheel{i}", w, 0.36, 0.26, 0.06, [dark], axis=(1, 0, 0), seg=12, steps=1),
            f"Wheel_{i + 1}")
        add(gs.rounded_cylinder(f"Hub{i}", (w[0] + 0.14 * (1 if w[0] > 0 else -1), w[1], w[2]), 0.13, 0.04, 0.015,
                                [yel], axis=(1, 0, 0), seg=8, steps=1), f"Wheel_{i + 1}")
    for sx in (1, -1):
        add(gs.capsule(f"BraceX{sx}", (sx * 2.65, -1.7, 3.2), (sx * 2.65, 1.7, 3.2), 0.18, yel, seg=6, cap=1, sub=1),
            "Root")
        add(gs.capsule(f"BraceD{sx}", (sx * 2.75, -1.75, 1.6), (sx * 2.45, 1.55, 5.4), 0.13, yel, seg=6, cap=1,
                       sub=1), "Root")
    add(gs.rounded_box("Platform", (0, 0, 6.35), (5.6, 3.8, 0.55), 0.18, [yel]), "Root")
    add(gs.rounded_box("PlatformTrim", (0, 0, 6.05), (5.7, 3.9, 0.12), 0.05, neon), "Root")
    add(gs.rounded_cylinder("TurretRing", (0, 0, 6.85), 1.7, 0.4, 0.1, [dark], seg=20, steps=1), "Turret")

    # --- cabina con cara enojada (ventanas = ojos, visera = ceño, rejilla = boca)
    add(gs.rounded_box("Cab", (0, -0.7, 8.35), (3.0, 2.8, 2.6), 0.45, [yel], segments=3, taper=(0.86, 0.9),
                       bulge=0.06), "Turret")
    for s in (1, -1):
        add(gs.ellipsoid(f"EyeWin{s}", (0.68 * s, -2.04, 8.62), (0.58, 0.1, 0.46), glass, seg=12, rings=6,
                         rot=("Y", -14 * s)), "Turret")
        add(gs.ellipsoid(f"Pupil{s}", (0.55 * s, -2.13, 8.5), (0.2, 0.05, 0.24), blk, seg=8, rings=4), "Turret")
        add(gs.rounded_box(f"Brow{s}", (0.72 * s, -2.12, 9.25), (1.3, 0.35, 0.3), 0.12, blk, rot=("Y", 18 * s)),
            "Turret")
    add(gs.rounded_box("Mouth", (0, -2.0, 7.6), (1.8, 0.3, 0.55), 0.14, blk), "Turret")
    for k in range(5):
        add(gs.rounded_box(f"Tooth{k}", (-0.68 + 0.34 * k, -2.14, 7.74), (0.22, 0.08, 0.24), 0.04, white),
            "Turret")
    add(gs.rounded_box("CabTrim", (0, -0.7, 7.12), (3.15, 2.95, 0.12), 0.05, neon), "Turret")
    add(gs.rounded_cylinder("Beacon", (0.8, -0.4, 9.85), 0.22, 0.32, 0.08, amber, seg=10, steps=1), "Turret")
    add(gs.rounded_cylinder("BeaconBase", (0.8, -0.4, 9.65), 0.26, 0.1, 0.03, dark, seg=10, steps=1), "Turret")
    add(gs.tube("Exhaust", [(-1.45, 0.1, 7.6), (-1.6, 0.2, 9.0), (-1.55, 0.25, 10.1)], 0.13, dark, seg=8, cap=1,
                sub=2), "Turret")
    add(gs.rounded_cylinder("ExhaustCap", (-1.55, 0.25, 10.2), 0.18, 0.1, 0.03, red, seg=8, steps=1), "Turret")
    # contrapeso con franjas y mástil en A
    add(hazard(gs.rounded_box("Counterweight", (0, 2.3, 8.0), (2.6, 1.8, 1.7), 0.3, [grey, blk], bulge=0.08),
               period=0.7), "Turret")
    add(gs.rounded_box("CounterArm", (0, 1.4, 7.4), (1.6, 2.4, 0.5), 0.15, [yel]), "Turret")
    for s in (1, -1):
        add(gs.capsule(f"Mast{s}", (0.9 * s, 0.4, 9.4), (0.12 * s, 0.6, 12.4), 0.16, yel, seg=6, cap=1, sub=1),
            "Turret")
    add(gs.ellipsoid("MastTop", (0, 0.6, 12.5), 0.3, red, seg=8, rings=5), "Turret")

    # --- brazo en celosía (sección triangular) con tirantes al mástil
    a, b = Vector((0, -1.9, 7.15)), Vector((0, -12.6, 8.9))
    chords = [(-0.55, 0.5), (0.55, 0.5), (0.0, -0.45)]
    for i, (x, z) in enumerate(chords):
        add(gs.capsule(f"Chord{i}", a + Vector((x, 0, z)), b + Vector((x * 0.6, 0, z * 0.6)), 0.15, yel, r1=0.12,
                       seg=6, cap=1, sub=1), "Boom")
    bays = 9
    for k in range(bays):
        u0, u1 = k / bays, (k + 1) / bays
        p0, p1 = a.lerp(b, u0), a.lerp(b, u1)
        for i in range(3):
            x0, z0 = chords[i]
            x1, z1 = chords[(i + 1) % 3]
            f0, f1 = 1 - 0.4 * u0, 1 - 0.4 * u1
            q0 = p0 + Vector((x0 * f0, 0, z0 * f0)) if k % 2 == 0 else p0 + Vector((x1 * f0, 0, z1 * f0))
            q1 = p1 + Vector((x1 * f1, 0, z1 * f1)) if k % 2 == 0 else p1 + Vector((x0 * f1, 0, z0 * f1))
            add(gs.capsule(f"Strut{k}{i}", q0, q1, 0.06, [gs.mat("#F2A900")], seg=5, cap=1, sub=1), "Boom")
    add(hazard(gs.rounded_box("BoomTip", (0, -12.75, 8.95), (1.0, 0.8, 1.0), 0.2, [yel, blk]), 0.4), "Boom")
    add(gs.rounded_cylinder("TipLight", (0, -13.2, 9.2), 0.16, 0.12, 0.04, neon, axis=(0, 1, 0), seg=8, steps=1),
        "Boom")
    for s in (1, -1):
        add(gs.capsule(f"Pendant{s}", (0.05 * s, 0.6, 12.4), (0.35 * s, -12.5, 9.4), 0.04, cable, seg=4, cap=1,
                       sub=1), "Boom")

    # --- carro, cable, motón y gancho
    add(gs.rounded_box("Trolley", (0, HOOK_Y, 8.0), (1.1, 1.3, 0.5), 0.12, [dark]), "Trolley")
    for s in (1, -1):
        add(gs.rounded_cylinder(f"Sheave{s}", (0.3 * s, HOOK_Y, 7.75), 0.22, 0.1, 0.03, [yel], axis=(1, 0, 0),
                                seg=10, steps=1), "Trolley")
        add(gs.tube(f"Cable{s}", [(0.3 * s, HOOK_Y, 7.6), (0.3 * s, HOOK_Y, 3.85)], 0.045, cable, seg=6, cap=1,
                    sub=1), "CABLE")
    add(hazard(gs.rounded_box("Block", (0, HOOK_Y, 3.35), (1.4, 0.85, 1.2), 0.28, [yel, blk], bulge=0.08), 0.45),
        "Hook")
    for s in (1, -1):
        add(gs.rounded_cylinder(f"BlockWheel{s}", (0.35 * s, HOOK_Y - 0.45, 3.55), 0.28, 0.08, 0.03, [dark],
                                axis=(0, 1, 0), seg=10, steps=1), "Hook")
    add(gs.rounded_cylinder("Swivel", (0, HOOK_Y, 2.65), 0.2, 0.3, 0.06, [dark], seg=10, steps=1), "Hook")
    hook = [Vector((-0.2, HOOK_Y, 2.6))] + gs.arc_points((0.5, HOOK_Y, 1.0), 0.7, 180, 395, 9)
    add(gs.tube("HookJ", hook, [0.27, 0.26, 0.25, 0.23, 0.21, 0.2, 0.18, 0.17, 0.15, 0.13], red, seg=10, cap=3,
                sub=2), "Hook")
    tip = hook[-1]
    add(gs.cone("HookTip", tip, tip + Vector((-0.18, 0, 0.4)), 0.16, 0.015, red, seg=8), "Hook")

    split = {f"Cable{s}": (lambda co: "Trolley" if co.z > 6.0 else "Hook") for s in (1, -1)}
    body = gs.rigid_skin("Crane", P, arm, split)
    return arm, body


def crane_actions(arm):
    loc = ("Root", "Hook")

    def wheels(p, ang):
        for i in range(len(WHEELS)):
            p.rot(f"Wheel_{i + 1}", ("X", ang))

    def idle(t):
        p = Pose()
        p.rot("Turret", ("Z", 3 * wave(t, 1)))
        p.rot("Boom", ("X", -0.6 * wave(t, 2)))
        p.rot("Trolley", ("X", 3 * wave(t, 1, 0.2)), ("Y", 2 * wave(t, 0.5)))
        return p

    def move(t):
        p = Pose()
        wheels(p, -360 * t)
        p.rot("Root", ("Y", 0.6 * wave(t, 4)))
        p.rot("Turret", ("Y", 1.2 * wave(t, 2)), ("Z", 2 * wave(t, 1)))
        p.rot("Boom", ("X", -0.8 * wave(t, 2, 0.2)))
        p.rot("Trolley", ("Y", 7 * wave(t, 1, 0.25)), ("X", 2 * wave(t, 2)))
        return p

    def lower(t, v):
        p = Pose().move("Hook", (0, 0, v["z"]))
        p.rot("Boom", ("X", v["dip"]))
        p.rot("Turret", ("Z", v["aim"]), ("Y", 1.5 * v["shake"] * wave(t, 8)))
        p.rot("Trolley", ("X", 2 * v["shake"] * wave(t, 6)))
        return p
    return [gs.sample_action(arm, "Idle", 60, idle, True, loc_bones=loc),
            gs.sample_action(arm, "Move", 30, move, True, step=1, loc_bones=loc),
            gs.phase_action(arm, "Lower_Hook", 60, [
                (0.0, dict(z=0, dip=0, aim=0, shake=0)),
                (0.25, dict(z=0.6, dip=-1.5, aim=4, shake=0.3)),
                (0.4, dict(z=-4.6, dip=2.5, aim=0, shake=0)),
                (0.45, dict(z=-4.3, dip=1.5, aim=0, shake=1)),
                (0.5, dict(z=-4.5, dip=1.0, aim=0, shake=0.5)),
                (0.7, dict(z=-4.5, dip=0.5, aim=0, shake=0)),
                (1.0, dict(z=0, dip=0, aim=0, shake=0))], lower, loc_bones=loc)]


def run():
    arm, body = build_crane()
    tris = gs.tri_count(body)
    print(f"[gs] Crane: {tris} triángulos (máx. 8000)")
    assert tris <= 8000, tris
    acts = crane_actions(arm)
    path = os.path.join(OUT, "Crane.fbx")
    gs.export_fbx([arm, body], path)
    poses = [("Idle", 15, (0.9, -0.7, 0.3)), ("Move", 8, (1, -0.3, 0.25)), ("Lower_Hook", 26, (0.9, -0.7, 0.3))]
    gs.render_sheet("Bosses", [body], arm, poses=poses,
                    views=[("front", (0, -1, 0.1)), ("side", (1, 0, 0.1)), ("3q", (0.75, -0.8, 0.35))])
    if "--no-verify" not in sys.argv:
        gs.verify_fbx(path)


if __name__ == "__main__":
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    run()
