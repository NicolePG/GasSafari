"""GAS SAFARI · iconos extra del kit de interfaz (M10-01 / pings): usa el mismo dibujo y la misma paleta que `Tools/Blender/ui_kit.py`
(se importa tal cual, sin modificarlo).

    python Tools/Export/UI/Kit/make_extra_icons.py            # genera los 4 PNG de abajo (necesita Pillow y numpy; sin Blender)

Salida (256x256, fondo transparente, contorno casi negro de ~9 px, sin textos):
    Pings/Ping_Now_Exclamation.png    exclamación naranja del ping "¡Ahora!" (docs/02 §11)
    Pings/Ping_Wait_Hand.png          mano abierta del ping "Espera" (blanca con contorno, se puede teñir desde la UI)
    Icons/Icon_Gas_Empty.png          lata vacía del aviso "Sin gas" (gas por debajo del 10 %)
    Icons/Icon_Chamber_Popped.png     cámara reventada rehecha: goma verde desinflada y rota, no una roca gris
OJO: `python Tools/Blender/ui_kit.py icons` vuelve a escribir la versión vieja de Icon_Chamber_Popped; si se regeneran los iconos del kit,
correr este script después.
"""
import math
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(HERE))), "Blender"))
import ui_kit as K  # noqa: E402
from ui_kit import Cv, add, alpha, hx, inter, mix, sub, NAVY, WHITE, SS  # noqa: E402


def stroke_pts(cv, pts, width):
    return cv.stroke(pts, width)


def smooth(pts, n=20):
    return K.ridge_pts(pts, n)


# ---------------------------------------------------------------------------
def ping_now():
    """Exclamación naranja gruesa con destellos de "ping" alrededor."""
    cv = Cv(256, 256)
    # destellos
    for a0, a1 in (((46, 60), (24, 36)), ((210, 60), (232, 36)), ((38, 112), (14, 112)), ((218, 112), (242, 112))):
        cv.shape(stroke_pts(cv, [a0, a1], 12), hx("#FFD36B"), ow=5)
    bar = cv.poly([(88, 24), (168, 24), (154, 164), (102, 164)], rnd=18, shrink=0.88)
    dot = cv.ell(128, 214, 25)
    m = add(bar, dot)
    cv.shape(m, cv.grad(24, 240, hx("#FFC857"), hx("#F2711C")), ow=9)
    cv.paint(sub(cv.shrink(m, 8), cv.shrink(m, 12)), alpha(WHITE, 170))
    cv.paint(cv.rr(110, 40, 124, 112, 7), alpha(WHITE, 210))          # brillo
    cv.paint(cv.ell(118, 204, 7), alpha(WHITE, 210))
    return cv.done()


def ping_wait():
    """Mano abierta de frente (alto): blanca, como las flechas de ping, para teñirla desde la UI."""
    cv = Cv(256, 256)
    palm = cv.rr(70, 120, 190, 222, 34)
    fingers = [
        cv.rr(70, 62, 98, 150, 14),       # meñique-lado izquierdo (índice, visto de frente)
        cv.rr(102, 36, 134, 150, 16),     # medio
        cv.rr(138, 46, 168, 150, 15),     # anular
        cv.rr(172, 78, 198, 160, 13),     # meñique
    ]
    thumb = cv.stroke([(80, 178), (44, 146), (30, 112)], 28)
    m = palm
    for f in fingers:
        m = add(m, f)
    m = add(m, thumb)
    cv.shape(m, cv.grad(36, 222, WHITE, hx("#C9D0EA")), ow=9)
    # separación de dedos
    for x in (100, 136, 170):
        cv.paint(cv.rr(x - 2, 112, x + 2, 152, 2), alpha(hx("#1C1A26"), 210))
    cv.paint(cv.rr(96, 158, 164, 168, 5), alpha(hx("#8A93B8"), 150))   # pliegue de la palma
    cv.paint(inter(cv.shrink(m, 6), cv.rr(0, 0, 256, 54, 0)), alpha(WHITE, 160))
    return cv.done()


def _dashed_ring(cv, m, width, dashes, cx, cy):
    ring = sub(cv.grow(m, width / 2), cv.shrink(m, width / 2))
    ys, xs = np.mgrid[0:cv.H, 0:cv.W]
    ang = (np.arctan2(ys - cy * SS, xs - cx * SS) / (2 * math.pi) + 0.5) * dashes
    dash = Image.fromarray((((ang % 1.0) < 0.55) * 255).astype(np.uint8), "L")
    return inter(ring, dash)


def gas_empty():
    """Lata abierta y vacía, con una nube de gas dibujada a rayas (no hay gas)."""
    cv = Cv(256, 256)
    # nube fantasma
    cloud = add(add(cv.ell(92, 52, 26), cv.ell(128, 40, 32)), add(cv.ell(166, 54, 26), cv.rr(78, 52, 180, 80, 14)))
    cv.paint(cloud, alpha(WHITE, 70))
    cv.paint(_dashed_ring(cv, cloud, 7, 26, 128, 60), alpha(NAVY, 235))
    # cuerpo de la lata
    body = cv.rr(60, 96, 196, 232, 22)
    cv.shape(body, cv.grad(96, 232, hx("#DCE1EE"), hx("#8991AB")), ow=9)
    band = inter(body, cv.rr(60, 138, 196, 196, 0))
    cv.paint(band, cv.grad(138, 196, hx("#FF7A6A"), hx("#D8333F")))
    cv.paint(sub(cv.grow(band, 0), cv.shrink(band, 4)), alpha(NAVY, 160))
    cv.paint(cv.ell(128, 167, 17), alpha(WHITE, 220))                   # etiqueta: círculo en blanco (sin dibujo)
    cv.paint(cv.rr(72, 104, 84, 226, 6), alpha(WHITE, 120))             # brillo lateral
    # boca abierta: aro de metal y hueco oscuro
    rim = cv.ell(128, 98, 68, 22)
    cv.shape(rim, cv.grad(76, 120, hx("#F4F6FC"), hx("#B6BDD4")), ow=8)
    hole = cv.ell(128, 100, 55, 14)
    cv.paint(hole, cv.grad(86, 114, hx("#3A3850"), hx("#15131E")))
    cv.paint(sub(cv.ell(128, 100, 55, 14), cv.ell(128, 104, 55, 14)), alpha(WHITE, 60))
    # tapa doblada colgando de un costado
    lid = cv.poly([(186, 84), (226, 70), (240, 96), (206, 112)], rnd=6, shrink=0.9)
    cv.shape(lid, cv.grad(70, 112, hx("#F4F6FC"), hx("#A9B0C9")), ow=6)
    return cv.done()


def chamber_popped():
    """Cámara de aire reventada: tubo verde pálido, flácido y arrugado, con un desgarro y aire que escapa."""
    cv = Cv(256, 256)
    # cuerpo flácido: un tubo aplastado que se hunde en el medio
    top = smooth([(22, 168), (56, 126), (104, 150), (150, 170), (194, 146), (234, 168)], 22)
    bot = smooth([(234, 190), (196, 218), (150, 208), (104, 214), (56, 212), (22, 190)], 22)
    body = cv.poly(top + bot, rnd=4, shrink=1.0)
    pale_top, pale_bot = hx("#C3DDA3"), hx("#7FA76A")
    cv.shape(body, cv.grad(126, 218, pale_top, pale_bot), ow=9)
    # anillos de la cámara, vencidos
    for x, tilt in ((76, 10), (132, -8), (186, 8)):
        cv.paint(inter(body, cv.stroke([(x - tilt, 120), (x + tilt, 224)], 12)), alpha(mix(pale_bot, NAVY, 0.45), 190))
    # arrugas de goma floja
    for pts in (((40, 176), (58, 184), (74, 176)), ((100, 186), (118, 196), (132, 190)), ((160, 180), (176, 190), (192, 182))):
        cv.paint(cv.stroke(list(pts), 5), alpha(mix(pale_bot, NAVY, 0.6), 200))
    cv.paint(inter(cv.shrink(body, 6), cv.rr(0, 118, 256, 142, 0)), alpha(WHITE, 130))   # brillo del lomo
    # válvula caída en el extremo izquierdo
    valve = cv.poly([(14, 150), (38, 140), (48, 160), (24, 172)], rnd=4, shrink=0.9)
    cv.shape(valve, cv.grad(140, 172, hx("#F7F4EC"), hx("#C6C9D6")), ow=6)
    cv.paint(cv.poly([(10, 146), (32, 138), (36, 148), (14, 156)], rnd=3, shrink=0.95), hx("#FFD60A"))
    # desgarro con bordes dentados
    tear = cv.poly([(108, 160), (124, 152), (128, 164), (146, 158), (142, 174), (156, 184), (132, 192), (120, 200), (116, 186), (98, 180), (112, 172)],
                   rnd=2, shrink=0.95)
    cv.shape(tear, hx("#1C1A26"), ow=5, outline=hx("#5E7F50"))
    # aire que se escapa: nubecitas y rayas de "pssst"
    for (x, y, r) in ((132, 120, 15), (160, 92, 20), (136, 62, 14), (112, 36, 9)):
        cv.shape(cv.ell(x, y, r), alpha(WHITE, 245), ow=5)
    for a, b in (((176, 128), (206, 108)), ((84, 124), (60, 104)), ((178, 70), (208, 56))):
        cv.paint(cv.stroke([a, b], 8), alpha(WHITE, 240))
    return cv.done()


def main():
    items = [
        ("Pings", "Ping_Now_Exclamation", ping_now()),
        ("Pings", "Ping_Wait_Hand", ping_wait()),
        ("Icons", "Icon_Gas_Empty", gas_empty()),
        ("Icons", "Icon_Chamber_Popped", chamber_popped()),
    ]
    for group, name, im in items:
        print(K.save(im, group, name))
    if "--sheet" in sys.argv:
        sheet = Image.new("RGBA", (256 * 4 + 50, 256 + 20), (60, 70, 100, 255))
        for i, (_, _, im) in enumerate(items):
            sheet.alpha_composite(im, (10 + i * 266, 10))
        out = sys.argv[sys.argv.index("--sheet") + 1]
        sheet.save(out)
        print(out)


if __name__ == "__main__":
    main()
