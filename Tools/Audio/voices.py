"""Voces sin palabras por personaje (docs/08 §4: effort 3, fall 2, laugh 2, cheer 2 → el doble para elegir).
Base: voz de Windows (System.Speech) con sílabas cortas; luego tono/velocidad tipo caricatura por personaje.
Si se instala una voz en español (p. ej. "Microsoft Sabina Desktop"), pasarla con --voz-es "<nombre>".
python voices.py  →  Tools/Export/Audio/Voices/<pj>/voice_<pj>_<tipo>_<nn>.wav"""
import json
import os
import subprocess
import sys
import tempfile
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_audio as A

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(A.OUT, "Voices")
MALE, FEMALE = "Microsoft David Desktop", "Microsoft Zira Desktop"

# Sílabas sin palabras (escritas para que la voz las pronuncie como sonidos).
LINES = {
    "effort": ["Uhhh!", "Oof!", "Hup!", "Humf!", "Ugh!", "Hyah!"],
    "fall": ["Waaaaah!", "Aaaaay!", "Whoa oh oh oh!", "Yaaaaah!"],
    "laugh": ["Ha ha ha ha!", "Hee hee hee hee!", "Ho ho ho!", "Ha, haha, ha!"],
    "cheer": ["Yoo hoo!", "Woo hoo!", "Yippee!", "Yeah yeah!"],
}

# voz base, rate de la TTS, factor de tono/velocidad, temblor (Melman), brillo
CHARS = {
    "alex": dict(voice=MALE, rate=-1, factor=0.9, wobble=0.0, hp=90),
    "marty": dict(voice=MALE, rate=3, factor=1.18, wobble=0.0, hp=120),
    "melman": dict(voice=MALE, rate=0, factor=1.32, wobble=0.035, hp=150),
    "gloria": dict(voice=FEMALE, rate=-1, factor=0.84, wobble=0.0, hp=80),
    "julien": dict(voice=MALE, rate=2, factor=1.62, wobble=0.0, hp=200),
}


def wobble(x, depth, rate=6.5):
    """Vibrato por retardo variable: voz temblorosa."""
    n = len(x)
    t = A.t_axis(n)
    d = depth * A.SR / (2 * np.pi * rate) * np.sin(2 * np.pi * rate * t)
    return np.interp(np.arange(n) - d, np.arange(n), x)


def main():
    voz_es = sys.argv[sys.argv.index("--voz-es") + 1] if "--voz-es" in sys.argv else None
    tmp = tempfile.mkdtemp(prefix="gs_tts_")
    jobs = []
    for pj, c in CHARS.items():
        for kind, lines in LINES.items():
            for i, text in enumerate(lines):
                jobs.append(dict(voice=voz_es or c["voice"], rate=c["rate"], text=text,
                                 out=os.path.join(tmp, f"{pj}_{kind}_{i}.wav"), pj=pj, kind=kind, i=i))
    jp = os.path.join(tmp, "jobs.json")
    json.dump(jobs, open(jp, "w", encoding="utf-8"))
    r = subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
                        os.path.join(HERE, "tts.ps1"), jp], capture_output=True, text=True)
    print(r.stdout.strip(), r.stderr.strip()[:300])
    rng = np.random.default_rng(5)
    for j in jobs:
        c = CHARS[j["pj"]]
        x = A.load(j["out"])
        f = c["factor"] * rng.uniform(0.97, 1.04)
        if j["kind"] in ("fall", "cheer"):
            f *= 1.06
        y = A.pitch_speed(x, f)
        if c["wobble"]:
            y = wobble(y, c["wobble"])
        y = A.highpass(y, c["hp"], 2)
        A.save(os.path.join(OUT, j["pj"], f"voice_{j['pj']}_{j['kind']}_{j['i'] + 1:02d}.wav"), y)
    print("voces:", len(jobs))


if __name__ == "__main__":
    main()
