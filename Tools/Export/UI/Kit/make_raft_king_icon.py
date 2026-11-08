"""GAS SAFARI · icono del logro 26 "Rey de la Balsa" (secreto, terminar W6-5; docs/04 W6-5).
Mismo marco, luz, contorno y paleta que las otras 25 medallas: usa `Tools/Blender/ui_icons.py` y `ui_glyphs.py` tal cual
(se importan sin modificarlos) y solo añade el dibujo nuevo (corona dorada sobre una balsa naranja, con una nube de gas).

    blender --background --python Tools/Export/UI/Kit/make_raft_king_icon.py

Salida: Tools/Export/UI/Icons/Achievements/Icon_Ach_26_ReyDeLaBalsa.png (256x256, fondo transparente, sin textos).
OJO: si se vuelve a correr `ui_icons.py achievements` esta medalla no se toca (su lista ACH llega hasta la 25)."""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
BLENDER_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(HERE))), "Blender")
sys.path.insert(0, BLENDER_DIR)
import gs_common as gs  # noqa: E402
import gs_render as R  # noqa: E402
import ui_glyphs as G  # noqa: E402
import ui_icons as I  # noqa: E402
from gs_common import Vector  # noqa: E402
from ui_glyphs import tm  # noqa: E402

INNER = "#0FA3B1"   # disco turquesa: no se repite con ningún otro logro


def g_raft_king():
    # balsa naranja inclinada (como la de Capitán Titanic) con la corona dorada encima
    P = [gs.torus("Raft", (0.0, 0, -0.26), 0.3, 0.09, tm("#FF7A1A"), axis=(0.1, -0.35, 0.93), seg=18, mseg=8)]
    P += G.puff((-0.42, -0.3), 0.2, n=3, seed=5)
    P += G.crown("Crown", (0, -0.12), 0.66, 0.44, tm("#F5C431"), tm("#F72585", 0.4), th=0.16)
    P += G.spark4("S1", (0.4, 0.34), 0.1, tm("#FFFFFF", 0.6)) + G.spark4("S2", (-0.38, 0.38), 0.08, tm("#FFD60A", 0.6))
    return P


def main():
    os.makedirs(os.path.join(I.ICONS, "Achievements"), exist_ok=True)
    R.ensure_cache()
    path = I.out("Achievements", "Icon_Ach_26_ReyDeLaBalsa.png")
    I.put_medal(INNER, g_raft_king, 1.0, name=path)
    print("[gs] medalla lista:", path)


if __name__ == "__main__":
    main()
