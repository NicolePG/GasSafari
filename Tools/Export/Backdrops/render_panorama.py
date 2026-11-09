"""Vista panorámica de prueba de los fondos de un mundo: capa media + capa lejana + suelo de relleno con niebla.
blender --background --python render_panorama.py -- W1 [W2 ...]
Coloca las capas como indica el README (cauce a lo largo de +Y, cámara en la balsa), renderiza izquierda/frente/derecha
(84° cada una) con el sombreado toon de gs_render y mezcla la niebla con el pase Mist.
Salida: Tools/Blender/previews/Backdrops_<Mundo>_panorama.png y Tools/Export/Backdrops/<Mundo>/<Mundo>_Panorama.jpg."""
import math
import os
import random
import sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import backdrop_lib as bd
import gs_common as gs
import gs_render as r
import bpy
from mathutils import Vector

# cielo, niebla, luz y color del agua por mundo
CFG = {
    "W1": dict(top="#070C26", hor="#3B2C74", fog="#2E2A66", fog_d=(60, 1000), amb=0.30, amb_col="#8FA0FF", stars=0.5,
               sun=("#9FB4FF", 0.9, (48, 0, 215)), water="#1E4F9A", bank="#6F7FA6"),
    "W2": dict(top="#58676F", hor="#A9BCC3", fog="#93A8B0", fog_d=(60, 900), amb=0.55, amb_col="#E6F2F4", stars=0,
               sun=("#FFFFFF", 1.6, (55, 0, 200)), water="#2EC4B6", bank="#B7C4C8"),
    "W3": dict(top="#4C9FE0", hor="#FFE7B0", fog="#C4DFB4", fog_d=(70, 1100), amb=0.45, amb_col="#FFF6DC", stars=0,
               sun=("#FFE2A8", 2.2, (50, 0, 215)), water="#2E9C9A", bank="#C9A56A"),
    "W4": dict(top="#0D0616", hor="#2C113C", fog="#22103A", fog_d=(40, 800), amb=0.30, amb_col="#B58CFF", stars=0,
               sun=("#7FD8FF", 0.7, (50, 0, 200)), water="#2C5F8E", bank="#4A2A60"),
    "W5": dict(top="#5A4A8C", hor="#FFB36B", fog="#E79A68", fog_d=(70, 1200), amb=0.40, amb_col="#FFD7A8", stars=0,
               sun=("#FF9A4A", 2.4, (68, 0, 235)), water="#2E8FA0", bank="#9C3D1C"),
    "W6": dict(top="#14072A", hor="#8A1F78", fog="#4C1B58", fog_d=(60, 1000), amb=0.35, amb_col="#E9A6FF", stars=0.5,
               sun=("#FF8AD8", 1.0, (50, 0, 210)), water="#4F6FD6", bank="#B5E48C"),
}


def layout(world, rnd):
    """Lista de (archivo, x, y, z, yaw) con el reparto recomendado en el README."""
    d = os.path.join(HERE, world)
    files = sorted(os.listdir(d))
    mids = [f for f in files if "_Mid_" in f and f.endswith(".fbx")]
    fars = [f for f in files if "_Far_" in f and f.endswith(".fbx")]
    out = []
    # capa media: ambos lados del cauce, pivote a 75-150 m del eje
    bag = []
    for side in (-1, 1):
        y = -90.0
        while y < 520:
            if not bag:
                bag = mids[:]
                rnd.shuffle(bag)
            nm = bag.pop()
            out.append((f"{world}/{nm}", side * rnd.uniform(78, 150), y + rnd.uniform(-10, 10), 55.0 if "Stalactites" in nm else 0.0,
                        rnd.choice([0.0, 0.0, math.radians(180)]) + rnd.uniform(-0.15, 0.15)))
            y += rnd.uniform(70, 105)
    # capa lejana: anillo de piezas mirando al centro, 280-560 m
    n = 14
    bag = []
    for i in range(n):
        a = i / n * 2 * math.pi + 0.2
        rad = 330 + 170 * (i % 2) + rnd.uniform(-30, 30)
        if not bag:
            bag = fars[:]
            rnd.shuffle(bag)
        nm = bag.pop()
        out.append((f"{world}/{nm}", rad * math.cos(a), rad * math.sin(a), 170.0 if "Stalactites" in nm else 0.0,
                    math.atan2(-math.cos(a), math.sin(a))))
    return out


def build(world):
    c = CFG[world]
    rnd = random.Random(11)
    sc = r.new_scene((800, 400), samples=16)
    r.world_sky(c["top"], c["hor"], ambient=c["amb"], ambient_col=c["amb_col"], stars=c["stars"])
    sun = c["sun"]
    r.sun(rot=sun[2], strength=sun[1], color=sun[0], angle=2.0)
    for rel, x, y, z, yaw in layout(world, rnd):
        r.asset(f"Backdrops/{rel}", loc=(x, y, z), rot=math.degrees(yaw))
    # suelo: losas de 200 m, falda y anillo del horizonte, 0.4 m bajo la línea de agua
    gz = -0.4
    for i in range(-2, 3):
        for j in range(-2, 3):
            r.asset(f"Backdrops/{world}/{world}_Ground_Tile{'AB'[(i + j) % 2]}.fbx", loc=(i * 200, j * 200 + 100, gz))
    r.asset(f"Backdrops/{world}/{world}_Ground_Skirt.fbx", loc=(0, 0, gz))
    r.asset(f"Backdrops/{world}/{world}_Ground_HorizonRing.fbx", loc=(0, 0, gz))
    # cauce de prueba: agua de 24 m y dos orillas de 1 m
    wm = r.toon_mat(c["water"])
    bm = r.toon_mat(c["bank"])
    for (cx, sx, mt, z0, h) in ((0, 12.0, wm, -0.3, 0.3), (-13.5, 1.5, bm, -0.5, 1.5), (13.5, 1.5, bm, -0.5, 1.5)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, 250, z0 + h / 2))
        o = bpy.context.active_object
        o.scale = (sx * 2 if sx == 12.0 else sx * 2, 1200, h)
        o.data.materials.append(mt)
        if mt is wm:
            o.scale = (24, 1200, h)
    return c


def render_world(world):
    c = build(world)
    sc = bpy.context.scene
    r.toonify_all()
    # niebla con el pase Mist
    vl = sc.view_layers[0]
    vl.use_pass_mist = True
    w = sc.world
    w.mist_settings.start, w.mist_settings.depth = c["fog_d"]
    w.mist_settings.falloff = "QUADRATIC"
    sc.use_nodes = True
    nt = sc.node_tree
    nt.nodes.clear()
    rl = nt.nodes.new("CompositorNodeRLayers")
    inv = nt.nodes.new("CompositorNodeInvert")
    mixn = nt.nodes.new("CompositorNodeMixRGB")
    comp = nt.nodes.new("CompositorNodeComposite")
    lc = r.lin(c["fog"])
    mixn.inputs[2].default_value = (*lc, 1)
    mixn.blend_type = "MIX"
    nt.links.new(rl.outputs["Mist"], mixn.inputs[0])
    nt.links.new(rl.outputs["Image"], mixn.inputs[1])
    nt.links.new(mixn.outputs["Image"], comp.inputs["Image"])
    sc.render.resolution_x, sc.render.resolution_y = 800, 400
    paths = []
    cam = r.camera((0, 0, 2.4), (0, 100, 2.4), lens=21.0, clip=(0.1, 6000))
    for name, tgt in (("L", (-100, 0, 8)), ("F", (0, 100, 8)), ("R", (100, 0, 8))):
        d = Vector(tgt) - Vector((0, 0, 2.4))
        cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(gs.PREVIEW_DIR, f"_tmp_pano_{world}_{name}.png")
        sc.render.filepath = p
        bpy.ops.render.render(write_still=True)
        paths.append(p)
    out = os.path.join(gs.PREVIEW_DIR, f"Backdrops_{world}_panorama.png")
    gs.stitch(paths, out, cols=3)
    for p in paths:
        os.remove(p)
    img = bpy.data.images.load(out)
    _ = img.pixels[0]  # fuerza la lectura de los datos
    sc.render.image_settings.file_format = "JPEG"
    sc.render.image_settings.quality = 84
    jpg = os.path.join(HERE, world, f"{world}_Panorama.jpg")
    img.save_render(jpg, scene=sc)
    bpy.data.images.remove(img)
    print(f"[bd] panorama {out} -> {jpg}")


if __name__ == "__main__":
    ws = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(CFG)
    for wn in ws:
        render_world(wn)
