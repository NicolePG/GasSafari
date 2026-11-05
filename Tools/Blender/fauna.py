"""Fauna con rig Generic (docs/09 §3, docs/05): fosa, cocodrilo, cocodrilo gigante, cangrejo, murciélago y tentáculo.
blender --background --python fauna.py [-- Croc Crab ...]

Miran hacia -Y (como los personajes), pivote en los pies o en la base del agua; el murciélago, en el centro del
cuerpo. El hueso raíz se llama Root y es el único con claves de posición. Salida: Tools/Export/Fauna/<Nombre>.fbx
(malla + esqueleto + clips). Presupuestos: fosa 5k, cocodrilo 6k, gigante 12k, cangrejo 1.5k, murciélago 1k,
tentáculo 3k triángulos.
"""
import math
import os
import random
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import bmesh
import gs_common as gs
from gs_common import Pose, Vector, X, wave

OUT = os.path.join(gs.EXPORT_DIR, "Fauna")
LOC = ("Root",)
SIDES = (("L", 1), ("R", -1))


def act(arm, name, n, fn, loop):
    return gs.sample_action(arm, name, n, fn, loop, loc_bones=LOC)


def phases(arm, name, n, ph, fn):
    return gs.phase_action(arm, name, n, ph, fn, loc_bones=LOC)


def srot(p, bone, s, *rots):
    """Rotación con espejo: en el lado derecho (s = -1) se invierten Y y Z."""
    p.rot(bone, *[(a, d if (a == "X" or s == 1) else -d) for a, d in rots])


def toon_eye(name, c, r, look, bone, iris="#F2C230", slit=False, lid_mat=None, lid_bone=None, lid_cover=75,
             seg=12):
    """Ojo de caricatura con pocos polígonos; párpado opcional como casquete en su propio hueso."""
    c = Vector(c)
    lk = Vector(look).normalized()
    rot = lk.to_track_quat("-Y", "Z").to_matrix().to_4x4()
    rs = max(6, seg * 2 // 3)
    out = [gs.ellipsoid(name + "_W", c, (r, r * 0.9, r * 1.05), gs.mat("eye_white", rough=0.3), bone=bone,
                        seg=seg, rings=max(5, seg // 2), rot=rot)]
    ir = r * 0.62
    out.append(gs.ellipsoid(name + "_I", c + lk * r * 0.7, (ir, ir * 0.35, ir * 1.1), gs.mat(iris), bone=bone,
                            seg=rs, rings=max(4, rs // 2), rot=rot))
    pr = ir * 0.6
    out.append(gs.ellipsoid(name + "_P", c + lk * r * 0.84, (pr * (0.35 if slit else 1.0), pr * 0.35, pr * 1.25),
                            gs.mat("pupil"), bone=bone, seg=8, rings=4, rot=rot))
    up = Vector((0, 0, 1))
    side = lk.cross(up).normalized()
    out.append(gs.ellipsoid(name + "_S", c + lk * r * 0.95 + up * pr * 0.6 + side * pr * 0.4, pr * 0.32,
                            gs.mat("eye_white", rough=0.2), bone=bone, seg=6, rings=3))
    if lid_mat is not None:
        R = r * 1.12
        prof = [(0.0, R)]
        for k in range(1, 6):
            a = math.radians(90 - lid_cover * k / 5)
            prof.append((R * math.cos(a), R * math.sin(a)))
        prof.append((prof[-1][0] * 0.98, prof[-1][1] - 0.001))
        lid = gs.lathe(name + "_Lid", prof, lid_mat, loc=c, seg=seg, bone=lid_bone or bone,
                       rot=("X", -15), scale=(1.0, 0.95, 1.0))
        out.append(lid)
    return out


def blank_tile(path, res):
    import numpy as np
    img = bpy.data.images.new("blank", res, res, alpha=True)
    a = np.ones((res, res, 4), dtype=np.float32)
    a[..., :3] = (0.62, 0.70, 0.78)
    img.pixels = a.ravel()
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


# ---------------------------------------------------------------------------
# Cocodrilo (normal y gigante)
# ---------------------------------------------------------------------------
def build_croc(name, S=1.0, giant=False):
    gs.reset_scene()
    rnd = random.Random(11 if giant else 5)
    pal = dict(skin="#2E6B4A", dark="#173D2C", belly="#E0A45E", spot="#8DB83A", iris="#FF4A1A") if giant else \
        dict(skin="#5BA33F", dark="#33702A", belly="#F0E3A0", spot="#477F33", iris="#F2C230")
    skin, dark = gs.mat(pal["skin"]), gs.mat(pal["dark"])
    belly, spot = gs.mat(pal["belly"]), gs.mat(pal["spot"])
    tooth, mouth = gs.mat("#FFF8E6", rough=0.3), gs.mat("#C8505A")

    def V(x, y, z):
        return Vector((x, y, z)) * S

    tail_pts = [V(0, 0.38, 0.3), V(0, 0.78, 0.29), V(0, 1.18, 0.27), V(0, 1.58, 0.25), V(0, 1.98, 0.22)]
    bones = [("Root", V(0, 0.38, 0.3), V(0, 0.05, 0.3), None),
             ("Spine", V(0, 0.05, 0.3), V(0, -0.35, 0.32), "Root", True),
             ("Neck", V(0, -0.35, 0.32), V(0, -0.6, 0.36), "Spine", True),
             ("Head", V(0, -0.6, 0.36), V(0, -1.35, 0.36), "Neck", True),
             ("Jaw", V(0, -0.62, 0.28), V(0, -1.3, 0.24), "Head"),
             ("Lids", V(0, -0.76, 0.56), V(0, -0.76, 0.66), "Head")]
    par = "Root"
    for i in range(4):
        bones.append((f"Tail{i + 1}", tail_pts[i], tail_pts[i + 1], par, i > 0))
        par = f"Tail{i + 1}"
    for sd, s in SIDES:
        for f, y, p in (("F", -0.25, "Spine"), ("B", 0.3, "Root")):
            bones.append((f"{f}{sd}_Upper", X(V(0.22, y, 0.28), s), X(V(0.42, y - 0.03, 0.17), s), p))
            bones.append((f"{f}{sd}_Lower", X(V(0.42, y - 0.03, 0.17), s), X(V(0.48, y - 0.07, 0.04), s),
                          f"{f}{sd}_Upper", True))
    arm = gs.build_generic_armature(name, bones)

    P = []
    q = 1.4 if giant else 1.0  # resolución

    def res(a, b):
        return dict(seg=int(a * q), rings=int(b * q))
    torso = gs.ellipsoid("Torso", V(0, 0, 0.3), (0.37 * S, 0.62 * S, 0.22 * S), [skin, belly], **res(18, 10))
    neck = gs.ellipsoid("NeckM", V(0, -0.47, 0.33), (0.29 * S, 0.27 * S, 0.19 * S), [skin, belly], **res(14, 8))
    tail = gs.tube("Tail", [V(0, 0.4, 0.3)] + tail_pts[1:], [0.27 * S, 0.2 * S, 0.13 * S, 0.075 * S, 0.025 * S],
                   [skin, belly], seg=14 if giant else 12, sub=3, flatten=((1, 0, 0), 0.72))
    for o in (torso, neck, tail):
        gs.paint_faces(o, lambda c: c.z < 0.24 * S, 1)
    P += [torso, neck, tail]
    # cabeza: cráneo, hocico ancho con punta bulbosa y mandíbula en su hueso
    P.append(gs.ellipsoid("Skull", V(0, -0.72, 0.4), (0.27 * S, 0.24 * S, 0.17 * S), skin, bone="Head", **res(16, 9)))
    P.append(gs.ellipsoid("Snout", V(0, -1.08, 0.36), (0.19 * S, 0.4 * S, 0.085 * S), skin, bone="Head", **res(16, 8)))
    P.append(gs.ellipsoid("NoseBulb", V(0, -1.37, 0.375), (0.14 * S, 0.1 * S, 0.075 * S), skin, bone="Head", **res(12, 6)))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Nostril{s}", V(0.045 * s, -1.4, 0.44), (0.025 * S, 0.02 * S, 0.012 * S), dark,
                              bone="Head", seg=8, rings=4))
    P.append(gs.ellipsoid("JawBase", V(0, -0.7, 0.26), (0.23 * S, 0.2 * S, 0.08 * S), [skin, belly], bone="Jaw", **res(12, 6)))
    jaw = gs.ellipsoid("JawM", V(0, -1.0, 0.245), (0.18 * S, 0.43 * S, 0.06 * S), [skin, belly], bone="Jaw", **res(14, 7))
    gs.paint_faces(jaw, lambda c: c.z < 0.23 * S, 1)
    P.append(jaw)
    P.append(gs.ellipsoid("Tongue", V(0, -0.95, 0.29), (0.13 * S, 0.33 * S, 0.02 * S), mouth, bone="Jaw",
                          seg=12, rings=5))
    # dientes de caricatura (colmillos más grandes alternados; al gigante le falta uno)
    n = 8 if giant else 6
    for i in range(n):
        u = i / (n - 1)
        y = -0.8 - 0.55 * u
        big = 1.5 if i % 3 == 1 else 1.0
        for s in (1, -1):
            if giant and i == 4 and s == -1:
                continue
            x = (0.165 - 0.05 * u) * s
            P.append(gs.cone(f"ToothU{i}{s}", V(x, y, 0.305), V(x * 1.02, y, 0.305 - 0.06 * big),
                             0.022 * S * big, 0.002 * S, tooth, seg=5, bone="Head", round_tip=False))
            if i % 2 == 0:
                P.append(gs.cone(f"ToothD{i}{s}", V(x * 0.95, y + 0.04, 0.285), V(x * 0.95, y + 0.04, 0.335),
                                 0.016 * S, 0.002 * S, tooth, seg=5, bone="Jaw", round_tip=False))
    # ojos saltones arriba con párpados (hueso Lids) y cejas
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Socket{s}", V(0.125 * s, -0.74, 0.5), (0.12 * S, 0.12 * S, 0.09 * S), skin,
                              bone="Head", seg=12, rings=7))
        P += toon_eye(f"Eye{s}", V(0.125 * s, -0.76, 0.56), 0.095 * S, (0.25 * s, -1, 0.35), "Head",
                      iris=pal["iris"], slit=True, lid_mat=skin, lid_bone="Lids", lid_cover=55,
                      seg=14 if giant else 12)
        tilt = -0.05 if giant else 0.02
        P.append(gs.tube(f"Brow{s}", [V(0.05 * s, -0.83, 0.65 + tilt), V(0.13 * s, -0.84, 0.67),
                                      V(0.2 * s, -0.8, 0.65 - tilt)], (0.034 if giant else 0.022) * S, dark,
                         bone="Head", seg=7, cap=2, sub=2))
        if giant:  # cuernos óseos sobre los ojos
            P.append(gs.cone(f"Horn{s}", V(0.17 * s, -0.74, 0.64), V(0.23 * s, -0.66, 0.78), 0.045 * S, 0.006 * S,
                             dark, seg=7))
    # escamas del lomo: dos filas de placas y una cresta de púas en la cola
    for i in range(9):
        y = -0.4 + 0.12 * i
        z = 0.3 + 0.22 * math.sqrt(max(0.0, 1 - (y / 0.62) ** 2))
        for s in (1, -1):
            if giant:  # placas puntiagudas
                b0 = V(0.11 * s, y, z - 0.02)
                P.append(gs.cone(f"Scute{i}{s}", b0, b0 + Vector((0.02 * s, 0.02, 0.1)) * S, 0.05 * S, 0.006 * S,
                                 dark, seg=6))
            else:
                P.append(gs.ellipsoid(f"Scute{i}{s}", V(0.1 * s, y, z - 0.01), (0.05 * S, 0.055 * S, 0.035 * S),
                                      dark, seg=6, rings=4))
    spikes = 11 if giant else 7
    for i in range(spikes):
        u = i / (spikes - 1)
        y = 0.45 + 1.4 * u
        rad = 0.27 - 0.24 * u
        h = (0.2 if giant else 0.07) * (1 - 0.6 * u)
        base = V(0, y, 0.3 - 0.07 * u + rad * 0.72)
        P.append(gs.cone(f"Spike{i}", base, base + Vector((0, 0.03 * S, h * S)), (0.05 - 0.025 * u) * S, 0.004 * S,
                         dark, seg=6))
    P += gs.stickers(torso, 12 if giant else 6, (0.09 if giant else 0.07) * S, spot, rnd, keep_fn=lambda c: c.z > 0.42 * S, seg=6,
                     rings=3)
    if giant:
        P.append(gs.tube("Scar", [V(0.07, -0.95, 0.44), V(0.02, -1.05, 0.452), V(-0.05, -1.13, 0.445)],
                         0.012 * S, gs.mat("#E89AA0"), bone="Head", seg=5, cap=2, sub=2))
    # patas cortas y abiertas
    for sd, s in SIDES:
        for f, y in (("F", -0.25), ("B", 0.3)):
            up, kn, ft = X(V(0.2, y, 0.28), s), X(V(0.42, y - 0.03, 0.17), s), X(V(0.48, y - 0.07, 0.04), s)
            P.append(gs.capsule(f"{f}{sd}_U", up, kn, 0.075 * S, skin, r1=0.06 * S, bone=f"{f}{sd}_Upper", seg=8,
                                cap=2, sub=1))
            P.append(gs.capsule(f"{f}{sd}_L", kn, ft, 0.055 * S, skin, r1=0.05 * S, bone=f"{f}{sd}_Lower", seg=8,
                                cap=2, sub=1))
            fc = ft + Vector((0.02 * s, -0.06, -0.015)) * S
            P.append(gs.ellipsoid(f"{f}{sd}_Foot", fc, (0.08 * S, 0.1 * S, 0.03 * S), skin, bone=f"{f}{sd}_Lower",
                                  seg=8, rings=4))
            for k in (-1, 0, 1):
                tp = fc + Vector((0.045 * k * s + 0.01 * s, -0.09, 0)) * S
                P.append(gs.cone(f"{f}{sd}_Claw{k}", tp, tp + Vector((0.01 * k * s, -0.04, -0.01)) * S,
                                 0.014 * S, 0.002 * S, tooth, seg=4, bone=f"{f}{sd}_Lower", round_tip=False))
    body = gs.join_and_skin(name, P, arm)
    return arm, body


def croc_base(S, p=None, t=0.0, lids=0.0):
    p = p or Pose()
    p.rot("Lids", ("X", lids))
    return p


def croc_legs(p, S, tuck=0.0, paddle=0.0, t=0.0, f=1.0):
    for sd, s in SIDES:
        for fr, ph in (("F", 0.0), ("B", 0.5)):
            ph2 = ph + (0.25 if s == -1 else 0.0)
            srot(p, f"{fr}{sd}_Upper", s, ("Z", (55 if fr == "F" else 40) * tuck + paddle * 30 * wave(t, f, ph2)),
                 ("Y", -20 * tuck))
            srot(p, f"{fr}{sd}_Lower", s, ("Z", 30 * tuck + paddle * 20 * wave(t, f, ph2 + 0.2)))


def croc_tail(p, t, amp=8.0, f=1.0, lag=0.15):
    for i in range(4):
        p.rot(f"Tail{i + 1}", ("Z", amp * (1 + 0.3 * i) * wave(t, f, -lag * i)))


def croc_actions(arm, S, giant=False):
    acts = []
    if not giant:
        def sleep(t):
            p = croc_base(S, lids=78).move("Root", (0, 0, -0.03 * S + 0.012 * S * wave(t, 1)))
            p.rot("Head", ("X", 5))
            p.rot("Spine", ("X", 1.5 * wave(t, 1)))
            croc_tail(p, t, 3, 1)
            croc_legs(p, S, tuck=0.4)
            return p
        acts.append(act(arm, "Sleep", 90, sleep, True))

        def wake(t, v):
            p = croc_base(S, lids=v["lids"] + v["twitch"] * 15 * wave(t, 6))
            p.move("Root", (0, 0, (-0.03 + 0.08 * v["up"]) * S))
            p.rot("Head", ("X", 5 - 22 * v["up"]), ("Z", 10 * v["look"]))
            p.rot("Neck", ("X", -6 * v["up"]))
            p.rot("Jaw", ("X", 14 * v["growl"]))
            croc_tail(p, t, 3 + 22 * v["flick"], 3)
            croc_legs(p, S, tuck=0.4 * (1 - v["up"]))
            return p
        acts.append(phases(arm, "Wake", 45, [
            (0.0, dict(lids=78, twitch=0, up=0, look=0, growl=0, flick=0)),
            (0.2, dict(lids=60, twitch=1, up=0, look=0, growl=0, flick=0)),
            (0.32, dict(lids=-5, twitch=0, up=0.2, look=0, growl=0, flick=0.3)),
            (0.55, dict(lids=-10, twitch=0, up=1, look=-1, growl=1, flick=1)),
            (0.75, dict(lids=-5, twitch=0, up=1, look=1, growl=0.6, flick=0.6)),
            (1.0, dict(lids=0, twitch=0, up=0.6, look=0, growl=0, flick=0))], wake))

    def idle(t):
        p = croc_base(S).move("Root", (0, 0, 0.008 * S * wave(t, 1)))
        p.rot("Neck", ("Z", 8 * wave(t, 0.5)), ("X", -2 * wave(t, 1)))
        p.rot("Head", ("X", -4))
        p.rot("Jaw", ("X", 4 + 3 * wave(t, 1, 0.3)))
        if giant:
            p.rot("Lids", ("X", 25))  # mirada de enojo
        croc_tail(p, t, 6, 1)
        croc_legs(p, S, tuck=0.2, paddle=0.3, t=t)
        return p
    acts.append(act(arm, "Idle", 60, idle, True))

    def swim(t):
        p = croc_base(S, lids=25 if giant else 0).move("Root", (0, 0, 0.01 * S * wave(t, 2)))
        p.rot("Root", ("Z", 5 * wave(t, 1, 0.1)))
        p.rot("Spine", ("Z", -6 * wave(t, 1)))
        p.rot("Neck", ("Z", 4 * wave(t, 1, 0.2)))
        p.rot("Head", ("X", -3), ("Z", 2 * wave(t, 1, 0.3)))
        if giant:
            p.rot("Jaw", ("X", 10 + 5 * wave(t, 2)))
        croc_tail(p, t, 14 if giant else 18, 1, 0.18)
        croc_legs(p, S, tuck=1.0)
        return p
    acts.append(act(arm, "Swim", 24 if giant else 30, swim, True))

    def bite(t, v):
        p = croc_base(S, lids=-8 + (25 if giant else 0) * (1 - v["open"]))
        p.move("Root", (0, v["dy"] * S, v["dz"] * S))
        p.rot("Head", ("X", v["head"]), ("Z", v["shake"] * 12 * wave(t, 5)))
        p.rot("Neck", ("X", v["head"] * 0.4))
        p.rot("Jaw", ("X", v["open"] * (55 if giant else 48)))
        p.rot("Spine", ("X", -4 * v["open"]))
        croc_tail(p, t, 8 + 14 * v["shake"], 2)
        croc_legs(p, S, tuck=0.3, paddle=v["open"], t=t, f=3)
        return p
    acts.append(phases(arm, "Bite", 36 if giant else 30, [
        (0.0, dict(dy=0, dz=0, head=-3, open=0.1, shake=0)),
        (0.3, dict(dy=0.15, dz=0.08, head=-28, open=1, shake=0)),
        (0.48, dict(dy=-0.55, dz=0.02, head=2, open=1, shake=0)),
        (0.56, dict(dy=-0.6, dz=0, head=8, open=0, shake=0)),
        (0.8, dict(dy=-0.5, dz=0, head=4, open=0, shake=1)),
        (1.0, dict(dy=0, dz=0, head=-3, open=0.1, shake=0))], bite))

    def retreat(t, v):
        p = croc_base(S, lids=-18 * v["scare"])
        p.move("Root", (0, v["back"] * S, 0.1 * S * v["jump"]))
        p.rot("Head", ("X", -16 * v["scare"]), ("Z", 10 * v["scare"] * wave(t, 4)))
        p.rot("Jaw", ("X", 18 * v["scare"]))
        p.rot("Root", ("Z", 10 * v["back"] * wave(t, 2)))
        croc_tail(p, t, 10 + 20 * v["scare"], 3)
        croc_legs(p, S, paddle=1.0, t=t, f=4)
        return p
    acts.append(phases(arm, "Retreat", 36, [
        (0.0, dict(scare=0, back=0, jump=0)), (0.15, dict(scare=1, back=0.05, jump=1)),
        (0.35, dict(scare=1, back=0.3, jump=0)), (1.0, dict(scare=0.6, back=1.0, jump=0))], retreat))
    return acts


# ---------------------------------------------------------------------------
# Fosa: felino delgado, cola larga, sonrisa con colmillos
# ---------------------------------------------------------------------------
def build_fossa():
    gs.reset_scene()
    rust, back, cream = gs.mat("#C2602C"), gs.mat("#7E3A1A"), gs.mat("#F3D3A0")
    dark, pink, tooth = gs.mat("nose_dark"), gs.mat("#F29BA6"), gs.mat("#FFF8E6", rough=0.3)
    tail_pts = [Vector(p) for p in ((0, 0.3, 0.52), (0, 0.5, 0.58), (0, 0.7, 0.62), (0, 0.88, 0.7), (0, 0.98, 0.86))]
    bones = [("Root", (0, 0.25, 0.5), (0, 0.0, 0.52), None),
             ("Spine", (0, 0.0, 0.52), (0, -0.26, 0.55), "Root", True),
             ("Neck", (0, -0.26, 0.55), (0, -0.38, 0.68), "Spine", True),
             ("Head", (0, -0.38, 0.68), (0, -0.66, 0.72), "Neck", True),
             ("Jaw", (0, -0.46, 0.64), (0, -0.62, 0.62), "Head")]
    par = "Root"
    for i in range(4):
        bones.append((f"Tail{i + 1}", tail_pts[i], tail_pts[i + 1], par, i > 0))
        par = f"Tail{i + 1}"
    LEG = {"F": ((0.1, -0.2, 0.47), (0.11, -0.2, 0.26), (0.11, -0.23, 0.06), "Spine"),
           "B": ((0.11, 0.22, 0.48), (0.12, 0.3, 0.27), (0.12, 0.22, 0.06), "Root")}
    for sd, s in SIDES:
        for f, (a, b, c, p) in LEG.items():
            bones.append((f"{f}{sd}_Upper", X(a, s), X(b, s), p))
            bones.append((f"{f}{sd}_Lower", X(b, s), X(c, s), f"{f}{sd}_Upper", True))
            bones.append((f"{f}{sd}_Paw", X(c, s), X(c, s) + Vector((0, -0.08, -0.02)), f"{f}{sd}_Lower", True))
    arm = gs.build_generic_armature("Fossa", bones)

    P = []
    body = gs.tube("BodyM", [(0, 0.34, 0.5), (0, 0.1, 0.52), (0, -0.16, 0.55), (0, -0.33, 0.62)],
                   [0.15, 0.16, 0.17, 0.12], [rust, cream], seg=14, sub=3)
    gs.paint_faces(body, lambda c: c.z < 0.43, 1)
    P.append(body)
    head_from = len(P) + 1
    P.append(gs.ellipsoid("Chest", (0, -0.24, 0.5), (0.12, 0.1, 0.11), cream, seg=12, rings=7))
    hc = Vector((0, -0.47, 0.72))
    P.append(gs.ellipsoid("Head", hc, (0.135, 0.13, 0.115), rust, bone="Head"))
    P.append(gs.ellipsoid("Muzzle", (0, -0.585, 0.685), (0.08, 0.085, 0.058), cream, bone="Head", seg=12, rings=7))
    P.append(gs.ellipsoid("Nose", (0, -0.665, 0.71), (0.03, 0.02, 0.02), dark, bone="Head", seg=8, rings=4))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Cheek{s}", (0.1 * s, -0.5, 0.67), (0.06, 0.06, 0.05), cream, bone="Head", seg=10,
                              rings=5))
        P.append(gs.ellipsoid(f"Ear{s}", (0.105 * s, -0.42, 0.83), (0.06, 0.025, 0.065), rust, bone="Head",
                              seg=10, rings=5, rot=("Y", -18 * s)))
        P.append(gs.ellipsoid(f"EarIn{s}", (0.105 * s, -0.443, 0.825), (0.038, 0.01, 0.042), pink, bone="Head",
                              seg=8, rings=4, rot=("Y", -18 * s)))
        P += toon_eye(f"Eye{s}", (0.06 * s, -0.565, 0.765), 0.045, (0.2 * s, -1, 0.05), "Head", iris="#F7B32B",
                      slit=True, seg=10)
        P.append(gs.tube(f"Brow{s}", [(0.02 * s, -0.6, 0.805), (0.065 * s, -0.6, 0.8), (0.105 * s, -0.575, 0.815)],
                         0.011, back, bone="Head", seg=6, cap=2, sub=2))
        P.append(gs.cone(f"Fang{s}", (0.035 * s, -0.635, 0.66), (0.035 * s, -0.64, 0.615), 0.012, 0.002, tooth,
                         seg=5, bone="Head", round_tip=False))
    P.append(gs.ellipsoid("JawM", (0, -0.555, 0.635), (0.065, 0.075, 0.03), cream, bone="Jaw", seg=10, rings=5))
    P.append(gs.tube("Grin", gs.arc_points((0, -0.64, 0.68), 0.06, 200, 340, 8, bulge=0.01), 0.008,
                     gs.mat("mouth"), bone="Head", seg=6, cap=2, sub=2))
    for o in P[head_from:]:  # cabeza grande de caricatura
        o.data.transform(gs.Matrix.Translation(Vector((0, -0.4, 0.68))) @ gs.Matrix.Scale(1.3, 4)
                         @ gs.Matrix.Translation(Vector((0, 0.4, -0.68))))
    P.append(gs.tube("TailM", [(0, 0.32, 0.52)] + [tuple(p) for p in tail_pts[1:]], [0.07, 0.065, 0.058, 0.05, 0.04],
                     [rust, back], seg=10, sub=3, mat_fn=lambda u: 1 if u > 0.82 else 0))
    for sd, s in SIDES:
        for f, (a, b, c, _) in LEG.items():
            P.append(gs.capsule(f"{f}{sd}_U", X(a, s), X(b, s), 0.08 if f == "B" else 0.065, rust, r1=0.05,
                                bone=f"{f}{sd}_Upper", seg=8, cap=2, sub=2))
            P.append(gs.capsule(f"{f}{sd}_L", X(b, s), X(c, s), 0.048, rust, r1=0.04, bone=f"{f}{sd}_Lower",
                                seg=8, cap=2, sub=2))
            pc = X(c, s) + Vector((0, -0.04, -0.025))
            P.append(gs.ellipsoid(f"{f}{sd}_PawM", pc, (0.06, 0.08, 0.04), back, bone=f"{f}{sd}_Paw", seg=10,
                                  rings=5))
            for k in (-1, 1):
                tp = pc + Vector((0.02 * k, -0.06, 0))
                P.append(gs.cone(f"{f}{sd}_Claw{k}", tp, tp + Vector((0, -0.03, -0.012)), 0.008, 0.001, tooth, seg=4,
                                 bone=f"{f}{sd}_Paw", round_tip=False))
    body = gs.join_and_skin("Fossa", P, arm)
    return arm, body


def fossa_legs(p, stride=0.0, t=0.0, f=1.0, crouch=0.0, reach=0.0):
    for sd, s in SIDES:
        for fr, ph in (("F", 0.0), ("B", 0.5)):
            phs = ph + (0.5 if s == -1 else 0.0)
            sw = stride * wave(t, f, phs)
            if fr == "F":
                srot(p, f"F{sd}_Upper", s, ("X", 25 * crouch - 25 * sw - 70 * reach))
                srot(p, f"F{sd}_Lower", s, ("X", -45 * crouch + 20 * max(0.0, sw)))
            else:
                srot(p, f"B{sd}_Upper", s, ("X", -35 * crouch + 25 * sw + 55 * reach))
                srot(p, f"B{sd}_Lower", s, ("X", 50 * crouch - 15 * max(0.0, -sw) + 20 * reach))


def fossa_tail(p, t, a=15, f=1.0, up=0.0):
    for i in range(4):
        p.rot(f"Tail{i + 1}", ("Z", a * wave(t, f, -0.12 * i)), ("X", up))


def fossa_actions(arm):
    def idle(t):
        p = Pose().move("Root", (0, 0, 0.004 * wave(t, 2)))
        p.rot("Spine", ("X", 1.5 * wave(t, 2)))
        p.rot("Neck", ("Z", 14 * wave(t, 0.5)), ("X", 3 * wave(t, 1)))
        p.rot("Head", ("Z", 6 * wave(t, 0.5, 0.1)))
        p.rot("Jaw", ("X", 3 + 3 * wave(t, 1)))
        fossa_legs(p, crouch=0.15)
        fossa_tail(p, t, 22, 1)
        return p

    def attack(t, v):
        p = Pose().move("Root", (0, v["dy"], v["dz"]))
        p.rot("Root", ("X", v["pitch"]), ("Z", v["wig"] * 6 * wave(t, 6)))
        p.rot("Neck", ("X", -10 * v["reach"]))
        p.rot("Head", ("X", -12 * v["reach"] + 8 * v["crouch"]))
        p.rot("Jaw", ("X", 30 * v["reach"]))
        fossa_legs(p, crouch=v["crouch"], reach=v["reach"])
        fossa_tail(p, t, 10 + 25 * v["wig"], 3, up=15 * v["reach"])
        return p

    def retreat(t, v):
        p = Pose().move("Root", (0, v["back"], 0.18 * v["hop"] * abs(wave(t, 3))))
        p.rot("Root", ("X", -10 * v["scare"]))
        p.rot("Neck", ("X", -15 * v["scare"]), ("Z", 15 * v["scare"] * wave(t, 2)))
        p.rot("Jaw", ("X", 15 * v["scare"]))
        fossa_legs(p, stride=v["hop"], t=t, f=3, crouch=0.3 * v["scare"])
        fossa_tail(p, t, 30, 3, up=25 * v["scare"])
        return p
    return [act(arm, "Idle", 60, idle, True),
            phases(arm, "Attack", 36, [
                (0.0, dict(dy=0, dz=0, pitch=0, wig=0, crouch=0.15, reach=0)),
                (0.25, dict(dy=0.05, dz=-0.08, pitch=4, wig=1, crouch=1, reach=0)),
                (0.45, dict(dy=-0.35, dz=0.35, pitch=-12, wig=0, crouch=0, reach=1)),
                (0.65, dict(dy=-0.6, dz=0.0, pitch=10, wig=0, crouch=0.6, reach=0.2)),
                (1.0, dict(dy=0, dz=0, pitch=0, wig=0, crouch=0.15, reach=0))], attack),
            phases(arm, "Retreat", 36, [
                (0.0, dict(back=0, hop=0, scare=0)), (0.15, dict(back=0.15, hop=1, scare=1)),
                (1.0, dict(back=0.9, hop=1, scare=0.7))], retreat)]


# ---------------------------------------------------------------------------
# Cangrejo: caparazón rojo, ojos en antenas y una pinza enorme
# ---------------------------------------------------------------------------
def build_crab():
    gs.reset_scene()
    red, light, tip = gs.mat("#E8442E"), gs.mat("#FFB08A"), gs.mat("#8E1E18")
    bones = [("Root", (0, 0.04, 0.18), (0, -0.08, 0.18), None)]
    for sd, s in SIDES:
        k = 1.3 if s == 1 else 1.0
        bones.append((f"Eye{sd}", X((0.06, -0.12, 0.25), s), X((0.075, -0.14, 0.37), s), "Root"))
        bones.append((f"Claw{sd}_Arm", X((0.17, -0.1, 0.18), s), X((0.3, -0.24, 0.2), s), "Root"))
        bones.append((f"Claw{sd}_Hand", X((0.3, -0.24, 0.2), s), X((0.3 + 0.05 * k, -0.24 - 0.17 * k, 0.21), s),
                      f"Claw{sd}_Arm", True))
        bones.append((f"Claw{sd}_Pincer", X((0.3, -0.24 - 0.07 * k, 0.18), s),
                      X((0.3 + 0.04 * k, -0.24 - 0.2 * k, 0.17), s), f"Claw{sd}_Hand"))
        for i, y in enumerate((-0.05, 0.04, 0.12)):
            bones.append((f"Leg{sd}{i + 1}", X((0.18, y, 0.17), s), X((0.32, y + 0.05 * i, 0.01), s), "Root"))
    arm = gs.build_generic_armature("Crab", bones)
    P = []
    shell = gs.ellipsoid("Shell", (0, 0, 0.2), (0.25, 0.19, 0.1), [red, light], seg=16, rings=8)
    gs.paint_faces(shell, lambda c: c.z < 0.17, 1)
    P.append(shell)
    P.append(gs.ellipsoid("Hump", (0, 0.02, 0.26), (0.15, 0.12, 0.06), red, seg=10, rings=5))
    for s in (1, -1):
        P.append(gs.cone(f"Spike{s}", (0.21 * s, -0.08, 0.22), (0.28 * s, -0.1, 0.25), 0.03, 0.003, red, seg=5))
    P.append(gs.tube("Mouth", gs.arc_points((0, -0.185, 0.2), 0.05, 200, 340, 6, bulge=0.005), 0.008,
                     gs.mat("mouth"), seg=5, cap=1, sub=1))
    for sd, s in SIDES:
        k = 1.3 if s == 1 else 1.0
        P.append(gs.capsule(f"Stalk{sd}", X((0.06, -0.12, 0.24), s), X((0.075, -0.14, 0.33), s), 0.015, red,
                            bone=f"Eye{sd}", seg=6, cap=1, sub=1))
        P += [gs.ellipsoid(f"Eye{sd}W", X((0.077, -0.145, 0.365), s), (0.042, 0.038, 0.048),
                           gs.mat("eye_white", rough=0.3), bone=f"Eye{sd}", seg=10, rings=6),
              gs.ellipsoid(f"Eye{sd}P", X((0.08, -0.18, 0.37), s), (0.02, 0.01, 0.025), gs.mat("pupil"),
                           bone=f"Eye{sd}", seg=6, rings=4)]
        P.append(gs.capsule(f"Arm{sd}", X((0.17, -0.1, 0.18), s), X((0.3, -0.24, 0.2), s), 0.03, red,
                            bone=f"Claw{sd}_Arm", seg=6, cap=1, sub=1))
        hc = X((0.3 + 0.02 * k, -0.24 - 0.08 * k, 0.21), s)
        P.append(gs.ellipsoid(f"Hand{sd}", hc, (0.065 * k, 0.095 * k, 0.06 * k), red, bone=f"Claw{sd}_Hand",
                              seg=10, rings=6))
        P.append(gs.cone(f"Finger{sd}", hc + Vector((0.01 * s, -0.07 * k, 0.02)),
                         hc + Vector((0.03 * s * k, -0.18 * k, 0.02)), 0.032 * k, 0.006, tip, seg=6,
                         bone=f"Claw{sd}_Hand"))
        P.append(gs.cone(f"Pincer{sd}", X((0.3, -0.24 - 0.07 * k, 0.185), s),
                         X((0.3 + 0.04 * k, -0.24 - 0.19 * k, 0.17), s), 0.024 * k, 0.005, tip, seg=6,
                         bone=f"Claw{sd}_Pincer"))
        for i, y in enumerate((-0.05, 0.04, 0.12)):
            p0 = X((0.18, y, 0.17), s)
            p1 = X((0.27, y + 0.03 * i, 0.23), s)
            p2 = X((0.32, y + 0.05 * i, 0.01), s)
            P.append(gs.tube(f"Leg{sd}{i}", [p0, p1, p2], [0.042, 0.034, 0.016], [red, tip], seg=5, cap=1, sub=1,
                             mat_fn=lambda u: 1 if u > 0.8 else 0, bone=f"Leg{sd}{i + 1}"))
    body = gs.join_and_skin("Crab", P, arm)
    return arm, body


def crab_actions(arm):
    def legs(p, t, f, a):
        for sd, s in SIDES:
            for i in range(3):
                ph = 0.5 * (i % 2) + (0.25 if s == -1 else 0)
                srot(p, f"Leg{sd}{i + 1}", s, ("Y", -a * max(0.0, wave(t, f, ph))), ("Z", 0.6 * a * wave(t, f, ph + 0.25)))

    def claws(p, t, raise_=0.0, snap=0.0, f=2.0):
        for sd, s in SIDES:
            srot(p, f"Claw{sd}_Arm", s, ("Y", -35 * raise_), ("Z", -10 * raise_))
            srot(p, f"Claw{sd}_Hand", s, ("X", -30 * raise_))
            p.rot(f"Claw{sd}_Pincer", ("X", 28 * snap * max(0.0, wave(t, f, 0.25 if s == -1 else 0))))

    def eyes(p, t, a=10, f=1.0):
        for sd, s in SIDES:
            srot(p, f"Eye{sd}", s, ("X", a * 0.5 * wave(t, f, 0.3 * s)), ("Z", a * wave(t, f * 0.5, 0.2 * s)))

    def idle(t):
        p = Pose().move("Root", (0, 0, 0.006 * wave(t, 2)))
        claws(p, t, 0.2 + 0.1 * wave(t, 1), 1.0, 2)
        eyes(p, t)
        legs(p, t, 1, 4)
        return p

    def walk(t):
        p = Pose().move("Root", (0, 0, 0.012 * abs(wave(t, 4))))
        p.rot("Root", ("Y", 4 * wave(t, 2)))
        claws(p, t, 0.4, 0.5, 4)
        eyes(p, t, 6, 2)
        legs(p, t, 4, 22)
        return p

    def attack(t):
        e = min(1.0, t * 5) * min(1.0, (1 - t) * 5)
        p = Pose().move("Root", (0, -0.04 * e, 0.02 * e))
        p.rot("Root", ("X", 8 * e))
        claws(p, t, e * (0.8 + 0.2 * wave(t, 6)), e, 6)
        eyes(p, t, 4 * e, 4)
        legs(p, t, 6, 8 * e)
        return p

    def retreat(t, v):
        p = Pose().move("Root", (v["x"], 0, 0.04 * v["jump"] + 0.012 * abs(wave(t, 6))))
        claws(p, t, v["panic"], v["panic"], 8)
        eyes(p, t, 18 * v["panic"], 4)
        legs(p, t, 6, 25)
        return p
    return [act(arm, "Idle", 40, idle, True), act(arm, "Walk", 16, walk, True), act(arm, "Attack", 30, attack, False),
            phases(arm, "Retreat", 30, [(0.0, dict(x=0, jump=0, panic=0)), (0.15, dict(x=0.02, jump=1, panic=1)),
                                        (1.0, dict(x=0.8, jump=0, panic=0.8))], retreat)]


# ---------------------------------------------------------------------------
# Murciélago: cuerpo redondo, orejas grandes, alas con festón
# ---------------------------------------------------------------------------
def wing_mesh(name, s, mats):
    outline = [(0.06, 0.07), (0.15, 0.12), (0.26, 0.135), (0.37, 0.13), (0.47, 0.09), (0.42, -0.04), (0.37, 0.02),
               (0.31, -0.09), (0.24, -0.01), (0.16, -0.09), (0.09, -0.02), (0.05, -0.04)]
    bm = bmesh.new()
    front, back = [], []
    for x, z in outline:
        front.append(bm.verts.new((x * s, -0.005, z + 0.05)))
        back.append(bm.verts.new((x * s, 0.005, z + 0.05)))
    cf = bm.verts.new((0.2 * s, -0.005, 0.1))
    cb = bm.verts.new((0.2 * s, 0.005, 0.1))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((cf, front[i], front[j]))
        bm.faces.new((cb, back[j], back[i]))
        bm.faces.new((front[j], front[i], back[i], back[j]))
    return gs._obj_from_bm(name, bm, mats, None, smooth=False)


def build_bat():
    gs.reset_scene()
    fur, belly, memb = gs.mat("#5B3C88"), gs.mat("#A585D6"), gs.mat("#3A2458")
    pink, tooth = gs.mat("#F29BA6"), gs.mat("#FFF8E6", rough=0.3)
    bones = [("Root", (0, 0, 0.0), (0, 0, 0.12), None), ("Head", (0, 0, 0.12), (0, 0, 0.25), "Root", True)]
    for sd, s in SIDES:
        bones.append((f"Wing{sd}1", X((0.06, 0, 0.1), s), X((0.25, 0, 0.18), s), "Root"))
        bones.append((f"Wing{sd}2", X((0.25, 0, 0.18), s), X((0.47, 0, 0.14), s), f"Wing{sd}1", True))
    arm = gs.build_generic_armature("Bat", bones)
    P = []
    b = gs.ellipsoid("Body", (0, 0, 0.04), (0.08, 0.07, 0.1), [fur, belly], seg=10, rings=6)
    gs.paint_faces(b, lambda c: c.y < -0.035 and c.z < 0.08, 1)
    P.append(b)
    P.append(gs.ellipsoid("HeadM", (0, -0.01, 0.16), (0.08, 0.07, 0.068), fur, bone="Head", seg=10, rings=6))
    P.append(gs.ellipsoid("Snout", (0, -0.07, 0.14), (0.035, 0.025, 0.025), pink, bone="Head", seg=6, rings=4))
    for s in (1, -1):
        P.append(gs.ellipsoid(f"Ear{s}", (0.055 * s, 0.0, 0.245), (0.035, 0.014, 0.075), fur, bone="Head", seg=6,
                              rings=4, rot=("Y", -22 * s)))
        P.append(gs.ellipsoid(f"EarIn{s}", (0.057 * s, -0.012, 0.24), (0.022, 0.006, 0.05), pink, bone="Head",
                              seg=6, rings=3, rot=("Y", -22 * s)))
        P.append(gs.ellipsoid(f"EyeW{s}", (0.032 * s, -0.06, 0.18), (0.026, 0.016, 0.03), gs.mat("eye_white"),
                              bone="Head", seg=8, rings=4))
        P.append(gs.ellipsoid(f"EyeP{s}", (0.034 * s, -0.074, 0.178), (0.013, 0.006, 0.017), gs.mat("#E2394B"),
                              bone="Head", seg=6, rings=3))
        P.append(gs.cone(f"Fang{s}", (0.015 * s, -0.08, 0.125), (0.015 * s, -0.082, 0.1), 0.007, 0.001, tooth,
                         seg=4, bone="Head", round_tip=False))
        P.append(gs.ellipsoid(f"Foot{s}", (0.03 * s, 0.0, -0.065), (0.018, 0.022, 0.02), memb, seg=6, rings=3))
        w = wing_mesh(f"Wing{s}", s, memb)
        w["bone"] = "auto"
        P.append(w)
        sd = "L" if s == 1 else "R"
        P.append(gs.capsule(f"Bone{s}1", X((0.06, 0, 0.1), s), X((0.25, 0, 0.18), s), 0.013, fur,
                            bone=f"Wing{sd}1", seg=5, cap=1, sub=1))
        P.append(gs.capsule(f"Bone{s}2", X((0.25, 0, 0.18), s), X((0.47, 0, 0.14), s), 0.009, fur,
                            bone=f"Wing{sd}2", seg=5, cap=1, sub=1))
        P.append(gs.cone(f"Thumb{s}", X((0.25, -0.005, 0.18), s), X((0.27, -0.01, 0.22), s), 0.01, 0.001, tooth,
                         seg=4, bone=f"Wing{sd}1", round_tip=False))
    body = gs.join_and_skin("Bat", P, arm)
    return arm, body


def bat_wings(p, t, f=1.0, a=40.0, fold=0.0):
    for sd, s in SIDES:
        srot(p, f"Wing{sd}1", s, ("Y", a * wave(t, f) - 10 + 30 * fold), ("Z", 50 * fold))
        srot(p, f"Wing{sd}2", s, ("Y", 0.7 * a * wave(t, f, -0.15) + 60 * fold))


def bat_actions(arm):
    def idle(t):
        p = Pose().move("Root", (0, 0, -0.03 * wave(t, 1, 0.1)))
        p.rot("Head", ("Z", 8 * wave(t, 0.5)))
        bat_wings(p, t, 1, 45)
        return p

    def attack(t, v):
        p = Pose().move("Root", (0, v["dy"], v["dz"] - 0.02 * wave(t, 4)))
        p.rot("Root", ("X", v["pitch"]))
        p.rot("Head", ("X", -v["pitch"] * 0.5))
        bat_wings(p, t, 4, 30 * (1 - v["fold"]), v["fold"])
        return p

    def retreat(t):
        p = Pose().move("Root", (0, 0.5 * t, 0.9 * t - 0.03 * wave(t, 6)))
        p.rot("Root", ("X", -20 * min(1.0, t * 4)))
        p.rot("Head", ("Z", 15 * wave(t, 3)))
        bat_wings(p, t, 6, 55)
        return p
    return [act(arm, "Idle", 16, idle, True),
            phases(arm, "Attack", 30, [(0.0, dict(dy=0, dz=0, pitch=0, fold=0)),
                                       (0.25, dict(dy=0.1, dz=0.15, pitch=-15, fold=0)),
                                       (0.55, dict(dy=-0.7, dz=-0.45, pitch=45, fold=1)),
                                       (0.7, dict(dy=-0.8, dz=-0.4, pitch=10, fold=0.3)),
                                       (1.0, dict(dy=0, dz=0, pitch=0, fold=0))], attack),
            act(arm, "Retreat", 24, retreat, False)]


# ---------------------------------------------------------------------------
# Tentáculo gigante (jefe W2-5)
# ---------------------------------------------------------------------------
TENT_N = 8


def build_tentacle():
    gs.reset_scene()
    purple, pink, deep = gs.mat("#A23B8F"), gs.mat("#F6B7D6"), gs.mat("#6E2465")
    zs = [0, 0.7, 1.4, 2.1, 2.75, 3.35, 3.9, 4.3, 4.6]
    ys = [0, 0, 0.03, 0.06, 0.06, 0.02, -0.1, -0.3, -0.55]
    pts = [Vector((0, y, z)) for y, z in zip(ys, zs)]
    rads = [0.55, 0.5, 0.44, 0.37, 0.3, 0.24, 0.17, 0.11, 0.05]
    bones = [("Root", (0, 0, -0.4), (0, 0, 0.0), None)]
    par = "Root"
    for i in range(TENT_N):
        bones.append((f"T{i + 1}", pts[i], pts[i + 1], par, i > 0))
        par = f"T{i + 1}"
    arm = gs.build_generic_armature("Tentacle", bones)
    body = gs.tube("TentM", [pts[0] - Vector((0, 0, 0.6))] + pts[1:], [0.58] + rads[1:], [purple, pink], seg=14,
                   sub=3)
    # cara interior (-Y, hacia la balsa) rosada
    bm = bmesh.new()
    bm.from_mesh(body.data)
    for f in bm.faces:
        c = f.calc_center_median()
        k = min(range(len(pts)), key=lambda i: (pts[i] - c).length)
        if (c - pts[k]).y < -0.45 * rads[k] and c.z > 0.1:
            f.material_index = 1
    bm.to_mesh(body.data)
    bm.free()
    P = [body]
    for i in range(1, 15):
        u = i / 15
        k = u * (len(pts) - 1)
        j = min(int(k), len(pts) - 2)
        c = pts[j].lerp(pts[j + 1], k - j)
        r = rads[j] + (rads[j + 1] - rads[j]) * (k - j)
        tang = (pts[j + 1] - pts[j]).normalized()
        n = Vector((0, -1, 0))
        n = (n - tang * n.dot(tang)).normalized()
        for s in ((-1, 1) if r > 0.2 else (0,)):
            side = tang.cross(n).normalized()
            pos = c + n * r * 0.92 + side * r * 0.42 * s
            rot = (n * 1.0).to_track_quat("Z", "Y").to_matrix().to_4x4()
            sz = r * 0.32
            P.append(gs.ellipsoid(f"Sucker{i}{s}", pos, (sz, sz, sz * 0.35), pink, seg=8, rings=4, rot=rot))
            P.append(gs.ellipsoid(f"SuckerIn{i}{s}", pos + n * sz * 0.25, (sz * 0.5, sz * 0.5, sz * 0.15), deep,
                                  seg=6, rings=3, rot=rot))
    rnd = random.Random(3)
    P += gs.stickers(body, 10, 0.12, deep, rnd, keep_fn=lambda c: c.y > 0.1 and 0.4 < c.z < 3.6, seg=6, rings=3)
    body = gs.join_and_skin("Tentacle", P, arm)
    return arm, body


def tentacle_actions(arm):
    def bend(p, x, z=0.0, t=0.0, wob=0.0):
        for i in range(TENT_N):
            p.rot(f"T{i + 1}", ("X", x * (0.5 + i / TENT_N) + wob * 5 * wave(t, 1, -0.1 * i)),
                  ("Z", z + 6 * wob * wave(t, 0.5, -0.12 * i)))

    def idle(t):
        p = Pose().move("Root", (0, 0, 0.05 * wave(t, 1)))
        bend(p, 2 * wave(t, 0.5), t=t, wob=1)
        return p

    def attack(t, v):
        p = Pose().move("Root", (0, 0, v["rise"]))
        bend(p, v["bend"], t=t, wob=v["wob"])
        return p

    def retreat(t, v):
        p = Pose().move("Root", (0, 0, v["z"]))
        bend(p, v["curl"], t=t, wob=1)
        return p
    return [act(arm, "Idle", 60, idle, True),
            phases(arm, "Attack", 45, [(0.0, dict(rise=0, bend=0, wob=1)), (0.35, dict(rise=0.6, bend=-9, wob=0.3)),
                                       (0.5, dict(rise=0.3, bend=13, wob=0)), (0.62, dict(rise=0.0, bend=12, wob=0)),
                                       (0.7, dict(rise=0.05, bend=11, wob=0)), (1.0, dict(rise=0, bend=0, wob=1))],
                   attack),
            phases(arm, "Retreat", 40, [(0.0, dict(z=0, curl=0)), (0.3, dict(z=0.2, curl=-6)),
                                        (1.0, dict(z=-5.2, curl=-14))], retreat)]


# ---------------------------------------------------------------------------
FAUNA = {
    "Fossa": (build_fossa, fossa_actions, 5000, (0.8, -0.7, 0.35)),
    "Croc": (lambda: build_croc("Croc", 1.0), lambda a: croc_actions(a, 1.0), 6000, (0.9, -0.6, 0.45)),
    "GiantCroc": (lambda: build_croc("GiantCroc", 3.0, True), lambda a: croc_actions(a, 3.0, True), 12000,
                  (0.9, -0.6, 0.45)),
    "Crab": (build_crab, crab_actions, 1500, (0.6, -0.9, 0.6)),
    "Bat": (build_bat, bat_actions, 1000, (0.4, -0.9, 0.3)),
    "Tentacle": (build_tentacle, tentacle_actions, 3000, (0.9, -0.5, 0.2)),
}
SHOT_T = {"Sleep": 0.3, "Wake": 0.55, "Idle": 0.25, "Walk": 0.25, "Swim": 0.25, "Bite": 0.35, "Attack": 0.45,
          "Retreat": 0.2}


def run(names):
    res, cols = 300, 7
    os.makedirs(gs.PREVIEW_DIR, exist_ok=True)
    blank = os.path.join(gs.PREVIEW_DIR, "_tmp_blank.png")
    tiles, paths = [], []
    for name in names:
        build, actions, budget, view = FAUNA[name]
        arm, body = build()
        tris = gs.tri_count(body)
        print(f"[gs] {name}: {tris} triángulos (máx. {budget})")
        assert tris <= budget, f"{name} excede {budget} ({tris})"
        acts = actions(arm)
        path = os.path.join(OUT, name + ".fbx")
        gs.export_fbx([arm, body], path)
        paths.append(path)
        sc, cam = gs._setup_render(res)
        row = []
        p = os.path.join(gs.PREVIEW_DIR, f"_tmp_fauna_{name}_rest.png")
        gs._render_view(p, [body], view, cam, margin=1.1)
        row.append(p)
        for a in acts:
            arm.animation_data.action = a
            sc.frame_set(int(round(a.frame_range[1] * SHOT_T.get(a.name, 0.4))))
            p = os.path.join(gs.PREVIEW_DIR, f"_tmp_fauna_{name}_{a.name}.png")
            gs._render_view(p, [body], view, cam, margin=1.2)
            row.append(p)
        arm.animation_data.action = None
        tiles += row + [blank] * (cols - len(row))
        print(f"[gs] {name}: clips {[a.name for a in acts]}")
    blank_tile(blank, res)
    sheet = os.path.join(gs.PREVIEW_DIR, "Fauna_sheet.png" if len(names) > 1 else f"Fauna_{names[0]}.png")
    gs.stitch(tiles, sheet, cols=cols)
    for p in set(tiles):
        os.remove(p)
    print(f"[gs] preview {sheet}")
    if "--no-verify" not in sys.argv:
        for path in paths:
            gs.verify_fbx(path)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    run([a for a in argv if not a.startswith("--")] or list(FAUNA))
