"""Genera Tools/Export/Audio/ELEGIR.md a partir de lo exportado, de sources.json y de los manifiestos de OpenGameArt.
No genera créditos: el oficial es Assets/_Project/CREDITS.md y se edita a mano.
uso: python -I gen_docs.py <carpeta_packs>"""
import collections
import glob
import json
import os
import re
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_audio as A

PACKS = sys.argv[1]
OUT = A.OUT
PJS = ["alex", "marty", "melman", "gloria", "julien"]
DOC_COUNT = {"fart_puff": 6, "fart_blast": 2, "fart_charge": 1, "fart_mega": 2, "fart_cough": 2,
             "fart_fire": 3, "fart_bubble": 3, "effort": 3, "fall": 2, "laugh": 2, "cheer": 2}


def files(pattern):
    return sorted(os.path.relpath(p, OUT).replace("\\", "/") for p in glob.glob(os.path.join(OUT, pattern)))


def ids_in(folder):
    c = collections.OrderedDict()
    for f in files(f"{folder}/*.*"):
        sid = re.sub(r"_\d\d$", "", os.path.splitext(os.path.basename(f))[0])
        c.setdefault(sid, []).append(f)
    return c


def elegir():
    L = ["# ELEGIR · sonidos para escuchar (M2-02)", "",
         "No pude escuchar nada: todo se generó o eligió por nombre y análisis. Escucha y marca con [x] los que "
         "se quedan. Lo que no marques se borra antes de importar a `Assets/_Project/Audio/`.",
         "Rutas relativas a `Tools/Export/Audio/`. Todos los efectos son WAV 44.1 kHz 16 bits con pico a -1 dB; "
         "la música es OGG.", "",
         "## 1. Pedos por personaje (lo más importante)", "",
         "Generé el doble de lo que pide docs/08 §4. Timbre: Gloria muy grave, Alex grave, Marty rápido y medio, "
         "Melman agudo y tembloroso, Julien muy agudo. Los loops (`blast`, `charge`) deben sonar sin corte al "
         "repetirse: escúchalos en loop (por ejemplo en Audacity con Shift+Espacio).", ""]
    for pj in PJS:
        L.append(f"### {pj.capitalize()} · `SFX/Farts/{pj}/`")
        L.append("")
        L.append("| Tipo | Elegir | Variantes |")
        L.append("|---|---|---|")
        for kind in ("fart_puff", "fart_blast", "fart_charge", "fart_mega", "fart_cough"):
            fs = files(f"SFX/Farts/{pj}/{kind}_{pj}_*.wav")
            nums = " ".join(f"[ ] {re.search(r'_(\d\d)\.wav$', f).group(1)}" for f in fs)
            L.append(f"| `{kind}_{pj}` | {DOC_COUNT[kind]} de {len(fs)} | {nums} |")
        L.append("")
    L += ["### Capas globales · `SFX/Farts/`", "", "| Tipo | Elegir | Variantes |", "|---|---|---|"]
    for kind in ("fart_fire", "fart_bubble"):
        fs = files(f"SFX/Farts/{kind}_*.wav")
        L.append(f"| `{kind}` | {DOC_COUNT[kind]} de {len(fs)} | "
                 + " ".join(f"[ ] {f[-6:-4]}" for f in fs) + " |")
    L += ["", "## 2. Voces sin palabras · `Voices/<pj>/`", "",
          "Windows solo tiene voces en inglés (David y Zira); no hay voz en español instalada. Usé sílabas sin "
          "palabras (\"Oof\", \"Hup\", \"Yoo hoo\", \"Ha ha\", \"Aaay\", \"Waaah\") y les cambié tono y velocidad. "
          "Si instalas una voz en español (Configuración → Hora e idioma → Voz → Agregar voces, p. ej. Sabina "
          "o Raúl de es-MX), puedo regenerarlas con `python voices.py --voz-es \"<nombre>\"`.", "",
          "| Personaje | effort (3 de 6) | fall (2 de 4) | laugh (2 de 4) | cheer (2 de 4) |", "|---|---|---|---|---|"]
    for pj in PJS:
        row = [pj.capitalize()]
        for k in ("effort", "fall", "laugh", "cheer"):
            fs = files(f"Voices/{pj}/voice_{pj}_{k}_*.wav")
            row.append(" ".join(f"[ ] {f[-6:-4]}" for f in fs))
        L.append("| " + " | ".join(row) + " |")
    L += ["", "## 3. Habilidades y efectos con varias opciones", "",
          "Para cada ID: quédate con 1 o más (el juego alterna entre los clips que dejes). "
          "Origen de cada archivo en `sources.json`.", ""]
    for folder in ("SFX/Abilities", "SFX/Raft", "SFX/Water", "SFX/Hazards", "SFX/PowerUps", "SFX/Events", "SFX/UI"):
        L.append(f"### `{folder}/`")
        L.append("")
        for sid, fs in ids_in(folder).items():
            nums = " ".join(f"[ ] {os.path.splitext(f)[0][-2:]}" for f in fs)
            L.append(f"- `{sid}`: {nums}")
        L.append("")
    rep = json.load(open(os.path.join(OUT, "Music", "_music_report.json"), encoding="utf-8"))
    L += ["## 4. Música · `Music/`", "",
          "Cada pista de mundo tiene `_base` (la canción CC0) y `_intensity` (percusión que sinteticé al tempo "
          "detectado). Escúchalas juntas: si la capa de intensidad va fuera de tiempo, anótalo y la rehago con "
          "otro BPM o la descartamos. Sugerencia de mezcla: intensidad a -6 dB respecto de la base.", "",
          "| ID | Pista | Tempo detectado | ¿Va a tiempo? |", "|---|---|---|---|"]
    src = json.load(open(os.path.join(OUT, "sources.json"), encoding="utf-8"))
    for sid, r in rep.items():
        if sid.startswith("stinger"):
            continue
        key = f"Music/{sid}_base.ogg" if f"Music/{sid}_base.ogg" in src else f"Music/{sid}.ogg"
        name = src.get(key, ["?"])[0].split(" · ")[1] if " · " in src.get(key, ["?"])[0] else src.get(key, ["?"])[0]
        bpm = r.get("bpm", "—")
        L.append(f"| `{sid}` | {name} | {bpm} | {'[ ]' if 'bpm' in r else '—'} |")
    L += ["", "`mus_w3_boss` es propia: percusión tropical y marimba de 110 a 128 BPM; los tiempos de cada golpe "
          "están en `Music/mus_w3_boss_beats.json` (sirve para `beat_click` y el nivel rítmico).", "",
          "### Stingers (elige 1 de cada uno)", ""]
    for sid in ("stinger_victory", "stinger_fail"):
        fs = files(f"Music/{sid}_*.ogg")
        L.append(f"- `{sid}`: " + " ".join(f"[ ] {f[-6:-4]}" for f in fs)
                 + f" (Kenney Music Jingles: {', '.join(rep[sid])})")
    L += ["", "## 5. Pendiente de decidir", "",
          "- Voces: ¿se quedan las de la voz de Windows o grabamos voces propias (docs/08 §6)? Ver la nota de "
          "licencia en `Assets/_Project/CREDITS.md`.",
          "- `mus_tutorial`, `mus_party` y `mus_results` duran más de 200 s en el original: los corté a 200 s con "
          "fade (no loopean perfecto).", ""]
    open(os.path.join(OUT, "ELEGIR.md"), "w", encoding="utf-8").write("\n".join(L))


if __name__ == "__main__":
    elegir()
    print("ok")
