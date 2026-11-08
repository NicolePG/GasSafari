"""GAS SAFARI · kit de interfaz 2D (menús y HUD) en PNG, estilo caricatura: bordes redondeados gruesos, contorno casi
negro, colores planos con degradado suave y brillo blanco arriba, igual que los iconos de `ui_icons.py` y el logo.

    python Tools/Blender/ui_kit.py                 # todo (necesita Pillow y numpy; NO usa Blender)
    python Tools/Blender/ui_kit.py panels buttons  # solo esos grupos: panels buttons meter ring icons frames pings events

Salida: Tools/Export/UI/Kit/<Grupo>/<Nombre>.png (los bordes 9-slice de cada archivo están en Tools/Export/UI/README.md)
y hojas de revisión Tools/Blender/previews/UI_Kit_<grupo>_sheet.png (no se suben a Git).
Todo se dibuja a 4x y se reduce, así los bordes quedan suaves. Sin textos: los pone la UI (TextMeshPro)."""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
TOOLS = os.path.dirname(HERE)
OUTDIR = os.path.join(TOOLS, "Export", "UI", "Kit")
PREVIEW = os.path.join(HERE, "previews")
SS = 4  # supermuestreo

# ---------------------------------------------------------------------------
# Paleta (la del juego: docs/03 colores de jugador, docs/07 §5 daltonismo, y la del zoológico/gas)
# ---------------------------------------------------------------------------
def hx(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


NAVY = hx("#1C1A26")      # contorno (el de los demás iconos)
INK = hx("#1E2547")
CREAM = hx("#F7F4EC")
WHITE = (255, 255, 255, 255)
PLAYER = [hx("#F28C28"), hx("#3A86FF"), hx("#2DC653"), hx("#9B5DE5")]        # J1..J4 (docs/03)
PLAYER_CB = [hx("#E69F00"), hx("#56B4E9"), hx("#F0E442"), hx("#CC79A7")]     # daltonismo (docs/07 §5)
SHAPES = ["Circle", "Triangle", "Square", "Diamond"]                          # círculo, triángulo, cuadrado, rombo


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(4))


def light(c, t=0.3):
    return mix(c, WHITE, t)


def dark(c, t=0.3):
    return mix(c, (20, 18, 40, 255), t)


def alpha(c, a):
    return (c[0], c[1], c[2], int(a))


# ---------------------------------------------------------------------------
# Lienzo y máscaras (todo en coordenadas 1x; se escala por SS)
# ---------------------------------------------------------------------------
class Cv:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.W, self.H = w * SS, h * SS
        self.img = Image.new("RGBA", (self.W, self.H), (0, 0, 0, 0))

    def mask(self):
        return Image.new("L", (self.W, self.H), 0)

    def rr(self, x0, y0, x1, y1, r):
        m = self.mask()
        ImageDraw.Draw(m).rounded_rectangle([x0 * SS, y0 * SS, x1 * SS - 1, y1 * SS - 1], radius=r * SS, fill=255)
        return m

    def ell(self, cx, cy, rx, ry=None):
        ry = rx if ry is None else ry
        m = self.mask()
        ImageDraw.Draw(m).ellipse([(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS - 1, (cy + ry) * SS - 1], fill=255)
        return m

    def poly(self, pts, rnd=0.0, shrink=None):
        """Polígono con esquinas redondeadas (rnd px): el polígono se encoge hacia su centro y se engorda con un trazo."""
        pts = [(float(x), float(y)) for x, y in pts]
        if rnd > 0:
            cx = sum(p[0] for p in pts) / len(pts)
            cy = sum(p[1] for p in pts) / len(pts)
            s = shrink if shrink is not None else 0.9
            pts = [(cx + (x - cx) * s, cy + (y - cy) * s) for x, y in pts]
        sp = [(x * SS, y * SS) for x, y in pts]
        m = self.mask()
        d = ImageDraw.Draw(m)
        d.polygon(sp, fill=255)
        if rnd > 0:
            w = int(2 * rnd * SS)
            d.line(sp + [sp[0], sp[1]], fill=255, width=w, joint="curve")
            for x, y in sp:
                d.ellipse([x - rnd * SS, y - rnd * SS, x + rnd * SS, y + rnd * SS], fill=255)
        return m

    def stroke(self, pts, width, cap=True, closed=False):
        """Trazo grueso: se estampan círculos a lo largo de la polilínea (sin artefactos en las curvas)."""
        sp = [(x * SS, y * SS) for x, y in pts]
        if closed:
            sp = sp + [sp[0]]
        m = self.mask()
        d = ImageDraw.Draw(m)
        r = width * SS / 2
        for (x0, y0), (x1, y1) in zip(sp[:-1], sp[1:]):
            n = max(1, int(math.hypot(x1 - x0, y1 - y0) / (SS * 0.5)))
            for i in range(n + 1):
                x, y = x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n
                d.ellipse([x - r, y - r, x + r, y + r], fill=255)
        return m

    def grow(self, m, px):
        """Dilata una máscara px píxeles (1x) con borde redondeado (desenfoque + umbral)."""
        if px <= 0:
            return m
        s = px * SS / 2.0
        return m.filter(ImageFilter.GaussianBlur(s)).point(lambda v: 255 if v > 5.8 else 0)

    def shrink(self, m, px):
        if px <= 0:
            return m
        inv = ImageChops.invert(m)
        return ImageChops.invert(self.grow(inv, px))

    def grad(self, y0, y1, c0, c1, x0=None, x1=None):
        """Degradado vertical (o horizontal si se dan x0/x1) como imagen del tamaño del lienzo."""
        a = np.zeros((self.H, self.W, 4), dtype=np.float32)
        if x0 is None:
            t = np.clip((np.arange(self.H) / SS - y0) / max(y1 - y0, 1e-6), 0, 1)[:, None, None]
        else:
            t = np.clip((np.arange(self.W) / SS - x0) / max(x1 - x0, 1e-6), 0, 1)[None, :, None]
        a[:] = np.array(c0, dtype=np.float32) * (1 - t) + np.array(c1, dtype=np.float32) * t
        return Image.fromarray(a.astype(np.uint8), "RGBA")

    def paint(self, m, fill):
        layer = Image.new("RGBA", (self.W, self.H), fill) if isinstance(fill, tuple) else fill.copy()
        layer.putalpha(ImageChops.multiply(layer.getchannel("A"), m))
        self.img.alpha_composite(layer)

    def shape(self, m, fill, ow=6, outline=NAVY):
        """Contorno exterior de ow px + relleno."""
        if ow > 0:
            self.paint(self.grow(m, ow), outline)
        self.paint(m, fill)

    def glow(self, m, color, radius, power=1.0):
        b = m.filter(ImageFilter.GaussianBlur(radius * SS))
        if power != 1.0:
            b = b.point(lambda v: min(255, int(v * power)))
        self.paint(b, color)

    def done(self):
        return self.img.resize((self.w, self.h), Image.LANCZOS)


def sub(a, b):
    return ImageChops.subtract(a, b)


def add(a, b):
    return ImageChops.lighter(a, b)


def inter(a, b):
    return ImageChops.multiply(a, b)


def save(img, group, name):
    d = os.path.join(OUTDIR, group)
    os.makedirs(d, exist_ok=True)
    p = os.path.join(d, name + ".png")
    img.save(p)
    return p


def ridge_pts(pts, n=24):
    """Curva suave (Catmull-Rom) por los puntos."""
    out = []
    P = [pts[0]] + list(pts) + [pts[-1]]
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for j in range(n):
            t = j / n
            out.append(tuple(0.5 * ((2 * p1[k]) + (-p0[k] + p2[k]) * t + (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * t * t
                                    + (-p0[k] + 3 * p1[k] - 3 * p2[k] + p3[k]) * t ** 3) for k in (0, 1)))
    out.append(tuple(pts[-1]))
    return out


# ---------------------------------------------------------------------------
# Paneles 9-slice
# ---------------------------------------------------------------------------
def panel(kind):
    w = h = 192
    cv = Cv(w, h)
    ow = 7
    if kind == "Light":
        body = cv.rr(ow, ow, w - ow, h - ow, 46)
        cv.paint(cv.grow(body, ow), NAVY)
        cv.paint(body, hx("#D9D0B6"))                                   # ledge
        face = cv.rr(ow, ow, w - ow, h - ow - 12, 46)
        cv.paint(face, cv.grad(ow, h - ow - 12, hx("#FFFDF4"), hx("#F2EDDC")))
        line = sub(cv.shrink(face, 9), cv.shrink(face, 12))
        cv.paint(line, alpha(hx("#CFC6AA"), 200))
        cv.paint(cv.rr(30, 15, w - 30, 24, 4), alpha(WHITE, 190))
    elif kind == "Dark":
        body = cv.rr(ow, ow, w - ow, h - ow, 46)
        cv.paint(cv.grow(body, ow), hx("#12111F"))
        cv.paint(body, hx("#1B2146"))
        face = cv.rr(ow, ow, w - ow, h - ow - 12, 46)
        cv.paint(face, cv.grad(ow, h - ow - 12, hx("#3C4880"), hx("#283060")))
        line = sub(cv.shrink(face, 9), cv.shrink(face, 12))
        cv.paint(line, alpha(hx("#6676BC"), 170))
        cv.paint(cv.rr(30, 15, w - 30, 23, 4), alpha(WHITE, 70))
    elif kind == "Inset":
        w = h = 160
        cv = Cv(w, h)
        ow = 6
        body = cv.rr(ow, ow, w - ow, h - ow, 36)
        cv.paint(cv.grow(body, ow), NAVY)
        cv.paint(body, cv.grad(ow, h - ow, hx("#C2D9E8"), hx("#E4F1F8")))
        cv.paint(inter(body, cv.rr(0, ow, w, ow + 14, 0)), alpha(hx("#7FA3BA"), 140))   # sombra interior arriba
        cv.paint(inter(body, cv.rr(0, h - ow - 5, w, h, 0)), alpha(WHITE, 200))
    return cv.done()


# ---------------------------------------------------------------------------
# Botones 9-slice: 192x112, estados Normal / Highlighted / Pressed / Disabled
# ---------------------------------------------------------------------------
BTN = {  # cara arriba, cara abajo, base (ledge)
    "Green": (hx("#B6F56A"), hx("#5FCB3A"), hx("#33902A")),
    "Blue": (hx("#7FE0FF"), hx("#2E9FE6"), hx("#1A62B0")),
    "Red": (hx("#FF9A86"), hx("#E2394B"), hx("#9C1D2E")),
}


def button(color, state):
    w, h = 192, 112
    cv = Cv(w, h)
    top, bot, ledge = BTN[color]
    ow = 6
    y0 = 3
    ledge_h = 13
    out = NAVY
    if state == "Highlighted":
        top, bot, ledge = light(top, 0.35), light(bot, 0.3), light(ledge, 0.15)
    elif state == "Pressed":
        top, bot, ledge = dark(top, 0.12), dark(bot, 0.15), dark(ledge, 0.15)
        y0 = 11
        ledge_h = 4
    elif state == "Disabled":
        top, bot, ledge = hx("#D5D7E2"), hx("#A9ACBE"), hx("#868AA0")
        out = hx("#4B4E66")
    x0, x1, y1 = 3, w - 3, h - 3
    shell = cv.rr(x0, y0, x1, y1, 42)
    cv.paint(shell, out)
    inner = cv.rr(x0 + ow, y0 + ow, x1 - ow, y1 - ow, 36)
    cv.paint(inner, ledge)
    face = cv.rr(x0 + ow, y0 + ow, x1 - ow, y1 - ow - ledge_h, 36)
    cv.paint(face, cv.grad(y0 + ow, y1 - ow - ledge_h, top, bot))
    if state != "Disabled":
        cv.paint(cv.rr(x0 + 26, y0 + ow + 7, x1 - 26, y0 + ow + 19, 6), alpha(WHITE, 125 if state != "Pressed" else 80))
        cv.paint(cv.ell(x0 + 17, y0 + ow + 15, 5), alpha(WHITE, 150))
    else:
        cv.paint(cv.rr(x0 + 26, y0 + ow + 7, x1 - 26, y0 + ow + 17, 5), alpha(WHITE, 70))
    if state == "Highlighted":
        ring = sub(cv.shrink(inner, 0), cv.shrink(inner, 4))
        cv.paint(ring, alpha(WHITE, 235))
    return cv.done()


# ---------------------------------------------------------------------------
# Medidor de equipo (barra) y anillo de gas
# ---------------------------------------------------------------------------
MW, MH = 512, 72


def meter_back():
    cv = Cv(MW, MH)
    ow = 6
    m = cv.rr(ow, ow, MW - ow, MH - ow, 30)
    cv.paint(cv.grow(m, ow), NAVY)
    cv.paint(m, cv.grad(ow, MH - ow, hx("#141A3A"), hx("#2C3668")))
    cv.paint(inter(m, cv.rr(0, ow, MW, ow + 9, 0)), alpha((0, 0, 0, 255), 90))        # sombra interior
    cv.paint(inter(m, cv.rr(0, MH - ow - 4, MW, MH, 0)), alpha(hx("#6676BC"), 120))
    return cv.done()


def meter_fill(ready=False):
    cv = Cv(MW, MH)
    m = cv.rr(10, 10, MW - 10, MH - 10, 26)
    if ready:
        top, bot, base = hx("#FFF58A"), hx("#FFC21A"), hx("#E8860E")
    else:
        top, bot, base = hx("#B6F56A"), hx("#5FCB3A"), hx("#33902A")
    cv.paint(m, base)
    face = cv.rr(10, 10, MW - 10, MH - 10 - 8, 26)
    cv.paint(face, cv.grad(10, MH - 18, top, bot))
    # rayas diagonales suaves (movimiento del gas)
    stripes = cv.mask()
    d = ImageDraw.Draw(stripes)
    for x in range(-80, MW + 80, 56):
        d.polygon([(x * SS, 0), ((x + 24) * SS, 0), ((x + 24 - 30) * SS, MH * SS), ((x - 30) * SS, MH * SS)], fill=255)
    cv.paint(inter(face, stripes), alpha(WHITE, 38))
    cv.paint(cv.rr(24, 16, MW - 24, 27, 5), alpha(WHITE, 190 if ready else 140))      # brillo
    cv.paint(cv.ell(24, 22, 5), alpha(WHITE, 210))
    return cv.done()


def meter_shine():
    w, h = 128, MH
    cv = Cv(w, h)
    m = cv.mask()
    d = ImageDraw.Draw(m)
    d.polygon([(50 * SS, 0), (92 * SS, 0), (62 * SS, h * SS), (20 * SS, h * SS)], fill=255)
    m = m.filter(ImageFilter.GaussianBlur(3 * SS))
    cv.paint(m, alpha(WHITE, 200))
    return cv.done()


def meter_glow():
    w, h = 640, 160
    cv = Cv(w, h)
    m = cv.rr(60, 44, w - 60, h - 44, 36)
    cv.glow(m, hx("#FFD60A"), 22, 1.6)
    cv.glow(m, alpha(hx("#FFF58A"), 160), 8, 1.2)
    return cv.done()


def ring_back():
    cv = Cv(256, 256)
    ring = sub(cv.ell(128, 128, 118), cv.ell(128, 128, 86))
    cv.paint(cv.grow(ring, 5), NAVY)
    cv.paint(ring, hx("#2A3262"))
    cv.paint(sub(cv.ell(128, 128, 112), cv.ell(128, 128, 92)), alpha(hx("#141A3A"), 150))
    return cv.done()


def ring_fill():
    cv = Cv(256, 256)
    ring = sub(cv.ell(128, 128, 112), cv.ell(128, 128, 92))
    cv.paint(ring, cv.grad(16, 240, WHITE, hx("#D5D8EA")))
    hi = sub(cv.ell(128, 128, 109), cv.ell(128, 128, 104))
    cv.paint(inter(hi, cv.rr(0, 0, 256, 128, 0)), alpha(WHITE, 255))
    return cv.done()


def ring_glow():
    cv = Cv(256, 256)
    ring = sub(cv.ell(128, 128, 112), cv.ell(128, 128, 92))
    cv.glow(ring, WHITE, 9, 1.8)
    return cv.done()


# ---------------------------------------------------------------------------
# Iconos 256x256
# ---------------------------------------------------------------------------
def gloss_dot(cv, x, y, r, a=200):
    cv.paint(cv.ell(x, y, r), alpha(WHITE, a))


def chamber(state):
    """Cámara de aire de la balsa: flotador alargado con anillos y válvula."""
    cv = Cv(256, 256)
    if state == "Popped":
        pts = [(34, 150), (60, 112), (100, 132), (140, 108), (186, 134), (222, 150), (206, 188), (150, 176), (110, 194),
               (62, 182)]
        m = cv.poly(pts, rnd=14, shrink=0.88)
        cv.shape(m, cv.grad(108, 196, hx("#B7BED0"), hx("#7C849C")), ow=8)
        for (a, b) in (((70, 132), (96, 156)), ((128, 130), (150, 158)), ((170, 140), (190, 164))):   # arrugas
            cv.paint(cv.stroke([a, b], 6), alpha(hx("#59607A"), 200))
        # estallido
        sp = []
        for i in range(16):
            r = 46 if i % 2 == 0 else 22
            a = math.pi / 8 * i - math.pi / 2
            sp.append((176 + r * math.cos(a), 78 + r * math.sin(a)))
        b = cv.poly(sp, rnd=3, shrink=0.95)
        cv.shape(b, cv.grad(32, 124, hx("#FFF07A"), hx("#FFB11A")), ow=6)
        cv.paint(cv.poly([(176 + 0.4 * (x - 176), 78 + 0.4 * (y - 78)) for x, y in sp], rnd=2, shrink=1.0), alpha(WHITE, 230))
        rip = [(96, 128), (116, 150), (104, 158), (132, 178), (126, 160), (146, 150), (126, 144), (134, 128)]
        cv.paint(cv.poly(rip, rnd=2, shrink=0.95), NAVY)
        for (a, b) in (((44, 204), (66, 190)), ((196, 206), (218, 196)), ((120, 214), (134, 202))):   # aire que se escapa
            cv.paint(cv.stroke([a, b], 8), alpha(WHITE, 230))
        return cv.done()
    green = state in ("Healthy", "Shield")
    top, bot = (hx("#A9F36C"), hx("#3DBB45")) if green else (hx("#B7BED0"), hx("#7C849C"))
    m = cv.rr(20, 86, 236, 190, 52)
    cv.shape(m, cv.grad(86, 190, top, bot), ow=8)
    for x in (88, 128, 168):  # anillos
        cv.paint(inter(m, cv.rr(x - 5, 86, x + 5, 190, 0)), alpha(dark(bot, 0.35), 230))
    cv.paint(cv.rr(40, 98, 216, 112, 7), alpha(WHITE, 140))
    gloss_dot(cv, 38, 106, 6)
    valve = cv.rr(176, 56, 204, 88, 7)
    cv.shape(valve, cv.grad(56, 88, hx("#F7F4EC"), hx("#C6C9D6")), ow=6)
    cv.paint(cv.rr(172, 52, 208, 66, 6), hx("#FFD60A"))
    cv.paint(sub(cv.grow(cv.rr(172, 52, 208, 66, 6), 0), cv.rr(172, 52, 208, 66, 6)), NAVY)
    return cv.done()


def chamber_shield():
    cv = Cv(256, 256)
    base = chamber("Healthy")
    cv.img.alpha_composite(base.resize((256 * SS, 256 * SS), Image.LANCZOS))
    # escudo azul delante con un coco
    pts = []
    for t in np.linspace(0, 1, 18):
        pts.append((128 + 82 * math.sin(t * math.pi / 2) * (1 - 0.12 * t), 70 + 120 * t ** 1.5 + 6 * t))
    right = pts
    left = [(256 - x, y) for x, y in reversed(pts)]
    path = [(46, 62), (128, 80)] + right[3:] + left[:-3] + [(210, 62)]
    sh = [(46, 66), (128, 82), (210, 66), (206, 134), (186, 188), (128, 236), (70, 188), (50, 134)]
    m = cv.poly(sh, rnd=20, shrink=0.9)
    cv.shape(m, cv.grad(66, 236, hx("#6FB0FF"), hx("#1B5FBF")), ow=9)
    cv.paint(sub(cv.shrink(m, 8), cv.shrink(m, 12)), alpha(WHITE, 220))
    # coco
    cv.shape(cv.ell(128, 142, 34), cv.grad(108, 176, hx("#A9733F"), hx("#5E3A1C")), ow=6)
    for (x, y) in ((118, 134), (140, 134), (129, 152)):
        cv.paint(cv.ell(x, y, 5), hx("#2A1A0E"))
    gloss_dot(cv, 112, 122, 7, 160)
    cv.paint(cv.rr(88, 84, 132, 92, 4), alpha(WHITE, 130))
    return cv.done()


def star(full):
    cv = Cv(256, 256)
    cx, cy = 128, 138
    pts = []
    for i in range(10):
        r = 112 if i % 2 == 0 else 60
        a = -math.pi / 2 + i * math.pi / 5
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    m = cv.poly(pts, rnd=11, shrink=0.88)
    if full:
        cv.shape(m, cv.grad(24, 240, hx("#FFF18A"), hx("#FFB21E")), ow=9)
        inner = cv.shrink(m, 14)
        cv.paint(sub(cv.shrink(m, 8), cv.shrink(m, 11)), alpha(hx("#FFFBD0"), 200))
        cv.paint(inter(inner, cv.rr(0, 130, 256, 256, 0)), alpha(hx("#F08A10"), 90))
        cv.paint(cv.ell(100, 92, 11, 7), alpha(WHITE, 230))
        cv.paint(cv.ell(86, 106, 4), alpha(WHITE, 200))
    else:
        cv.shape(m, cv.grad(24, 240, hx("#59648F"), hx("#3A4370")), ow=9)
        inner = cv.shrink(m, 12)
        cv.paint(inter(inner, cv.rr(0, 20, 256, 90, 0)), alpha((0, 0, 0, 255), 70))    # hueco hundido
        cv.paint(inter(sub(inner, cv.shrink(inner, 5)), cv.rr(0, 150, 256, 256, 0)), alpha(hx("#8794C8"), 120))
    return cv.done()


def mango_shape(cv, cx, cy, rx, ry, rot=-25):
    pts = []
    for i in range(72):
        t = i / 72 * 2 * math.pi
        x = rx * math.cos(t) * (1 + 0.14 * math.sin(t))
        y = ry * math.sin(t) * (1 - 0.12 * math.cos(t))
        c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
        pts.append((cx + x * c - y * s, cy + x * s + y * c))
    return cv.poly(pts), pts


def golden_mango(full):
    cv = Cv(256, 256)
    m, pts = mango_shape(cv, 126, 150, 76, 96)
    if full:
        cv.shape(m, cv.grad(54, 246, hx("#FFEE8A"), hx("#F0A81C")), ow=9)
        cv.paint(inter(cv.shrink(m, 8), cv.ell(180, 210, 70, 70)), alpha(hx("#E8800E"), 100))
        cv.paint(cv.ell(92, 120, 12, 24), alpha(WHITE, 210))
        gloss_dot(cv, 90, 90, 6, 200)
    else:
        cv.shape(m, cv.grad(54, 246, hx("#59648F"), hx("#3A4370")), ow=9)
        cv.paint(inter(cv.shrink(m, 12), cv.rr(0, 50, 256, 110, 0)), alpha((0, 0, 0, 255), 70))
    stem = cv.stroke([(142, 66), (150, 44)], 12)
    cv.shape(stem, hx("#8A5A2B") if full else hx("#4A5380"), ow=5)
    leaf = cv.poly([(150, 52), (186, 24), (226, 34), (206, 62), (166, 70)], rnd=8, shrink=0.8)
    cv.shape(leaf, cv.grad(24, 70, hx("#7AD64F"), hx("#2F9A3A")) if full else hx("#4A5380"), ow=6)
    if full:
        cv.paint(cv.stroke([(160, 58), (206, 42)], 4), alpha(hx("#2F7A2A"), 200))
    return cv.done()


def lock():
    cv = Cv(256, 256)
    arc = []
    for i in range(0, 37):
        a = math.pi + math.pi * i / 36
        arc.append((128 + 52 * math.cos(a), 104 + 56 * math.sin(a)))
    sh = [(76, 108)] + arc + [(180, 108)]
    sm = cv.stroke(sh, 26)
    cv.shape(sm, cv.grad(40, 112, hx("#F1F3FA"), hx("#9AA3BC")), ow=8)
    body = cv.rr(48, 104, 208, 222, 34)
    cv.shape(body, cv.grad(104, 222, hx("#FFE066"), hx("#F0A21A")), ow=9)
    cv.paint(cv.rr(64, 116, 192, 128, 6), alpha(WHITE, 150))
    cv.paint(sub(cv.shrink(body, 8), cv.shrink(body, 11)), alpha(hx("#FFF3B0"), 190))
    key = cv.ell(128, 156, 17)
    cv.paint(key, NAVY)
    cv.paint(cv.poly([(118, 160), (138, 160), (143, 194), (113, 194)], rnd=3, shrink=0.95), NAVY)
    return cv.done()


def bean_coin():
    cv = Cv(256, 256)
    coin = cv.ell(128, 128, 112)
    cv.shape(coin, cv.grad(16, 240, hx("#FFEA7A"), hx("#F2A91C")), ow=9)
    cv.paint(sub(cv.shrink(coin, 9), cv.shrink(coin, 14)), alpha(hx("#FFF8C8"), 220))
    cv.paint(sub(cv.shrink(coin, 14), cv.shrink(coin, 17)), alpha(hx("#C77D0E"), 170))
    # frijolito (riñón): elipse con una mella
    bean = sub(cv.ell(128, 130, 56, 36), cv.ell(110, 84, 40, 16))
    # rotar la máscara del frijol -25°
    bean = bean.rotate(25, resample=Image.BICUBIC, center=(128 * SS, 130 * SS))
    cv.shape(bean, cv.grad(92, 170, hx("#E8744A"), hx("#A63B22")), ow=6)
    hl = cv.ell(106, 118, 14, 5).rotate(25, resample=Image.BICUBIC, center=(128 * SS, 130 * SS))
    cv.paint(hl, alpha(WHITE, 210))
    cv.paint(cv.ell(60, 66, 6, 3), alpha(WHITE, 220))
    cv.paint(cv.ell(190, 188, 5, 3), alpha(hx("#C77D0E"), 200))
    return cv.done()


# ---------------------------------------------------------------------------
# Flechas de pings (256x256, apuntan arriba o giran): blancas con contorno, se tiñen desde la UI
# ---------------------------------------------------------------------------
def turn_arrow(direction):
    cv = Cv(256, 256)
    pts = [(160, 214), (160, 150)]
    cxa, cya, r = 112, 150, 48
    for i in range(1, 25):
        a = math.radians(i * 90 / 24)
        pts.append((cxa + r * math.cos(a), cya - r * math.sin(a)))
    pts.append((84, 102))
    body = cv.stroke(pts, 40)
    head = cv.poly([(76, 56), (76, 148), (14, 102)], rnd=8, shrink=0.8)
    shape = add(body, head)
    if direction == "Right":
        shape = shape.transpose(Image.FLIP_LEFT_RIGHT)
    cv.shape(shape, cv.grad(40, 230, WHITE, hx("#CFD4EA")), ow=9)
    return cv.done()


def straight_arrow(kind):
    cv = Cv(256, 256)
    if kind == "Hazard":
        top, bot = hx("#FF9A86"), hx("#E2394B")
        m = cv.poly([(128, 18), (232, 138), (172, 138), (172, 236), (84, 236), (84, 138), (24, 138)], rnd=14, shrink=0.88)
        cv.shape(m, cv.grad(18, 236, top, bot), ow=9)
        cv.paint(sub(cv.shrink(m, 8), cv.shrink(m, 12)), alpha(WHITE, 180))
        cv.paint(cv.rr(116, 120, 140, 172, 10), WHITE)
        cv.paint(cv.ell(128, 198, 13), WHITE)
        return cv.done()
    top, bot = hx("#8FE8FF"), hx("#2E9FE6")
    for i, dy in enumerate((0, 78)):
        m = cv.poly([(128, 16 + dy), (232, 98 + dy), (204, 130 + dy), (128, 70 + dy), (52, 130 + dy), (24, 98 + dy)], rnd=10,
                    shrink=0.9)
        cv.shape(m, cv.grad(16 + dy, 130 + dy, top, bot), ow=8)
        cv.paint(inter(cv.shrink(m, 6), cv.rr(0, 16 + dy, 256, 40 + dy, 0)), alpha(WHITE, 120))
    return cv.done()


# ---------------------------------------------------------------------------
# Marcos de retrato y marcas de forma (J1-J4 y daltonismo)
# ---------------------------------------------------------------------------
def shape_mask(cv, name, cx, cy, s):
    if name == "Circle":
        return cv.ell(cx, cy, s)
    if name == "Square":
        return cv.rr(cx - s * 0.86, cy - s * 0.86, cx + s * 0.86, cy + s * 0.86, s * 0.3)
    if name == "Diamond":
        return cv.poly([(cx, cy - s * 1.2), (cx + s * 1.2, cy), (cx, cy + s * 1.2), (cx - s * 1.2, cy)], rnd=s * 0.16, shrink=0.88)
    return cv.poly([(cx, cy - s * 1.1), (cx + s * 1.12, cy + s * 0.9), (cx - s * 1.12, cy + s * 0.9)], rnd=s * 0.2, shrink=0.82)


def badge(shape, color, size=128):
    cv = Cv(size, size)
    c = size / 2
    s = size * 0.34
    m = shape_mask(cv, shape, c, c + (size * 0.04 if shape == "Triangle" else 0), s)
    cv.shape(m, cv.grad(0, size, light(color, 0.28), color), ow=max(5, size // 20))
    cv.paint(sub(cv.shrink(m, size / 24), cv.shrink(m, size / 14)), alpha(WHITE, 210))
    cv.paint(inter(cv.shrink(m, size / 18), cv.rr(0, 0, size, size * 0.38, 0)), alpha(WHITE, 40))
    return cv.done()


def frame(color, shape=None):
    cv = Cv(512, 512)
    c = 256
    ring = sub(cv.ell(c, c, 247), cv.ell(c, c, 216))
    cv.paint(cv.grow(ring, 7), NAVY)
    cv.paint(ring, cv.grad(0, 512, light(color, 0.22), dark(color, 0.12)))
    cv.paint(sub(cv.ell(c, c, 247), cv.ell(c, c, 227)), cv.grad(0, 512, light(color, 0.22), dark(color, 0.12)))
    cv.paint(sub(cv.ell(c, c, 227), cv.ell(c, c, 221)), alpha(WHITE, 235))
    # brillo arriba a la izquierda
    cv.paint(inter(sub(cv.ell(c, c, 241), cv.ell(c, c, 232)), cv.rr(0, 0, 300, 220, 0)), alpha(WHITE, 90))
    img = cv.done()
    if shape:
        b = badge(shape, color, 170)
        img.alpha_composite(b, (512 - 170 - 6, 512 - 170 - 6))
    return img


# ---------------------------------------------------------------------------
# Fondos de texto de evento (9-slice horizontal) y estallido
# ---------------------------------------------------------------------------
EVT = {"Sync": (hx("#FFE45C"), hx("#FFA81E"), hx("#C8620C")),
       "Super": (hx("#FF7CBE"), hx("#F72585"), hx("#9C0E55")),
       "Info": (hx("#7FE0FF"), hx("#2E7FE6"), hx("#1A3F9C"))}


def event_banner(kind):
    w, h = 384, 128
    cv = Cv(w, h)
    top, bot, ledge = EVT[kind]
    ow = 8
    m = cv.rr(ow, ow, w - ow, h - ow, 54)
    cv.paint(cv.grow(m, ow), NAVY)
    cv.paint(m, ledge)
    face = cv.rr(ow, ow, w - ow, h - ow - 14, 54)
    cv.paint(face, cv.grad(ow, h - ow - 14, top, bot))
    cv.paint(sub(cv.shrink(face, 8), cv.shrink(face, 11)), alpha(WHITE, 130))
    cv.paint(cv.rr(40, 20, w - 40, 31, 5), alpha(WHITE, 140))
    return cv.done()


def event_burst():
    cv = Cv(512, 512)
    n = 14
    pts = []
    for i in range(2 * n):
        r = 246 if i % 2 == 0 else 186
        a = math.pi * i / n - math.pi / 2
        pts.append((256 + r * math.cos(a), 256 + r * math.sin(a)))
    m = cv.poly(pts, rnd=14, shrink=0.95)
    cv.shape(m, cv.grad(10, 502, hx("#FFEE7A"), hx("#FF9A1A")), ow=10)
    inner = []
    for i in range(2 * n):
        r = 204 if i % 2 == 0 else 160
        a = math.pi * i / n - math.pi / 2
        inner.append((256 + r * math.cos(a), 256 + r * math.sin(a)))
    cv.paint(cv.poly(inner, rnd=10, shrink=0.95), alpha(hx("#FFF8C0"), 120))
    cv.paint(inter(cv.shrink(m, 10), cv.rr(0, 0, 512, 90, 0)), alpha(WHITE, 40))
    return cv.done()


# ---------------------------------------------------------------------------
# Hojas de contacto y prueba de 9-slice
# ---------------------------------------------------------------------------
def checker(w, h, s=16):
    a = np.zeros((h, w, 3), dtype=np.uint8)
    yy, xx = np.mgrid[0:h, 0:w]
    a[:] = np.where(((xx // s + yy // s) % 2 == 0)[..., None], (118, 118, 126), (92, 92, 100)).astype(np.uint8)
    return Image.fromarray(a, "RGB").convert("RGBA")


def nine_slice(img, border, size):
    """border = (L, T, R, B) en px del archivo."""
    l, t, r, b = border
    w, h = img.size
    W, H = size
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    xs = [(0, l, 0, l), (l, w - r, l, W - r), (w - r, w, W - r, W)]
    ys = [(0, t, 0, t), (t, h - b, t, H - b), (h - b, h, H - b, H)]
    for (sx0, sx1, dx0, dx1) in xs:
        for (sy0, sy1, dy0, dy1) in ys:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            piece = img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.BICUBIC)
            out.alpha_composite(piece, (dx0, dy0))
    return out


def sheet(name, items, cols=6, cell=180, label=True, bg=None):
    """items = [(archivo, imagen)] -> hoja con fondo de ajedrez y nombre."""
    rows = (len(items) + cols - 1) // cols
    pad = 14
    W, H = cols * (cell + pad) + pad, rows * (cell + pad + 16) + pad
    sh = checker(W, H)
    d = ImageDraw.Draw(sh)
    for i, (nm, im) in enumerate(items):
        r, c = divmod(i, cols)
        x, y = pad + c * (cell + pad), pad + r * (cell + pad + 16)
        k = min(cell / im.width, cell / im.height, 1.0 if im.width <= cell and im.height <= cell else 9)
        t = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)
        sh.alpha_composite(t, (x + (cell - t.width) // 2, y + (cell - t.height) // 2))
        if label:
            d.text((x, y + cell + 2), nm, fill=(255, 255, 255, 255))
    os.makedirs(PREVIEW, exist_ok=True)
    p = os.path.join(PREVIEW, f"UI_Kit_{name}_sheet.png")
    sh.save(p)
    print("[ui] hoja", p)
    return p


# ---------------------------------------------------------------------------
# Grupos
# ---------------------------------------------------------------------------
BORDERS = {}  # nombre -> (L, T, R, B), para el README


def g_panels():
    items = []
    for kind, b in (("Light", 58), ("Dark", 58), ("Inset", 50)):
        im = panel(kind)
        nm = f"Panel_{kind}"
        save(im, "Panels", nm)
        BORDERS[nm] = (im.size, (b, b, b, b))
        items.append((nm, im))
    return items


def g_buttons():
    items = []
    for color in BTN:
        for state in ("Normal", "Highlighted", "Pressed", "Disabled"):
            im = button(color, state)
            nm = f"Button_{color}_{state}"
            save(im, "Buttons", nm)
            BORDERS[nm] = (im.size, (52, 52, 52, 52))
            items.append((nm, im))
    return items


def g_meter():
    items = []
    for nm, im, b in (("Meter_Back", meter_back(), (40, 8, 40, 8)), ("Meter_Fill", meter_fill(), (40, 8, 40, 8)),
                      ("Meter_Fill_Ready", meter_fill(True), (40, 8, 40, 8)), ("Meter_Shine", meter_shine(), None),
                      ("Meter_Glow", meter_glow(), (90, 60, 90, 60))):
        save(im, "Meter", nm)
        BORDERS[nm] = (im.size, b)
        items.append((nm, im))
    return items


def g_ring():
    items = []
    for nm, im in (("Ring_Gas_Back", ring_back()), ("Ring_Gas_Fill", ring_fill()), ("Ring_Gas_Glow", ring_glow())):
        save(im, "Ring", nm)
        BORDERS[nm] = (im.size, None)
        items.append((nm, im))
    return items


def g_icons():
    items = []
    for nm, im in (("Icon_Chamber_Healthy", chamber("Healthy")), ("Icon_Chamber_Popped", chamber("Popped")),
                   ("Icon_Chamber_Shield", chamber_shield()), ("Icon_Star_Empty", star(False)), ("Icon_Star_Full", star(True)),
                   ("Icon_GoldenMango_Empty", golden_mango(False)), ("Icon_GoldenMango_Full", golden_mango(True)),
                   ("Icon_Lock", lock()), ("Icon_Bean", bean_coin())):
        save(im, "Icons", nm)
        BORDERS[nm] = (im.size, None)
        items.append((nm, im))
    return items


def g_frames():
    items = []
    for i in range(4):
        for tag, pal, shp in (("", PLAYER, None), ("CB_", PLAYER_CB, SHAPES[i])):
            nm = f"Frame_{tag}J{i + 1}"
            im = frame(pal[i], shp)
            save(im, "Frames", nm)
            BORDERS[nm] = (im.size, None)
            items.append((nm, im))
    for i in range(4):
        for tag, pal in (("", PLAYER), ("CB_", PLAYER_CB)):
            nm = f"Badge_{tag}J{i + 1}_{SHAPES[i]}"
            im = badge(SHAPES[i], pal[i])
            save(im, "Frames", nm)
            BORDERS[nm] = (im.size, None)
            items.append((nm, im))
    return items


def g_pings():
    items = []
    for nm, im in (("Ping_Arrow_Left", turn_arrow("Left")), ("Ping_Arrow_Right", turn_arrow("Right")),
                   ("Arrow_Hazard", straight_arrow("Hazard")), ("Arrow_Current", straight_arrow("Current"))):
        save(im, "Pings", nm)
        BORDERS[nm] = (im.size, None)
        items.append((nm, im))
    return items


def g_events():
    items = []
    for kind in EVT:
        im = event_banner(kind)
        nm = f"Event_Banner_{kind}"
        save(im, "Events", nm)
        BORDERS[nm] = (im.size, (72, 40, 72, 40))
        items.append((nm, im))
    im = event_burst()
    save(im, "Events", "Event_Burst")
    BORDERS["Event_Burst"] = (im.size, None)
    items.append(("Event_Burst", im))
    return items


GROUPS = {"panels": (g_panels, 3, 200), "buttons": (g_buttons, 4, 200), "meter": (g_meter, 3, 330),
          "ring": (g_ring, 3, 256), "icons": (g_icons, 5, 200), "frames": (g_frames, 8, 150), "pings": (g_pings, 4, 220),
          "events": (g_events, 4, 260)}


def slice_test():
    """Prueba: cada panel y botón estirado a otros tamaños con sus bordes 9-slice."""
    items = []
    for nm, (size, b) in sorted(BORDERS.items()):
        if b is None or not (nm.startswith(("Panel", "Button", "Meter_Back", "Meter_Fill", "Event_Banner"))):
            continue
        if nm.startswith("Button") and "Normal" not in nm and "Pressed" not in nm and "Disabled" not in nm \
                and "Highlighted" not in nm:
            continue
        img = Image.open(os.path.join(OUTDIR, {"Pane": "Panels", "Butt": "Buttons", "Mete": "Meter", "Even": "Events"}[nm[:4]], nm + ".png"))
        W, H = (420, 300) if nm.startswith("Panel") else ((420, 110) if nm.startswith("Button") else
                                                            ((460, 72) if nm.startswith("Meter") else (520, 128)))
        items.append((nm + f" {W}x{H}", nine_slice(img, b, (W, H))))
    return sheet("9slice", items, cols=3, cell=440)


def main():
    todo = [a for a in sys.argv[1:] if a in GROUPS] or list(GROUPS)
    for g in todo:
        fn, cols, cell = GROUPS[g]
        items = fn()
        sheet(g, items, cols=cols, cell=cell)
        print(f"[ui] {g}: {len(items)} archivos")
    if {"panels", "buttons", "meter", "events"} <= set(todo):
        slice_test()


if __name__ == "__main__":
    main()
