"""Música (docs/08 §5): pistas CC0 de OpenGameArt convertidas a OGG + capa de intensidad por mundo.
Capa de intensidad: percusión sintetizada al tempo detectado de la pista (base + intensidad, mismo largo).
mus_w3_boss: pista rítmica propia con rampa de 110 a 128 BPM.
stinger_victory / stinger_fail: candidatos de Kenney Music Jingles elegidos por la dirección de la melodía.
uso: python -I build_music.py <carpeta_packs>"""
import glob
import json
import os
import sys
import numpy as np
import soundfile as sf
from scipy.signal import stft
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_audio as A
import build_sfx as S

PACKS = sys.argv[1]
MUSIC = os.path.join(A.OUT, "Music")
MAXLEN = 200.0  # s
rng = np.random.default_rng(11)

# id → (slug de OGA, archivo, estilo de la capa de intensidad o None, bpm conocido)
TRACKS = {
    "mus_title": ("enchanted-tiki-86", "enchanted tiki 86_0.mp3", None, None),
    "mus_select": ("flowerbed-fields-loop", "flowerbed_fields.ogg", None, None),
    "mus_map": ("bossa-nova", "8bit Bossa.mp3", None, None),
    "mus_tutorial": ("balance-0", "Komiku - It's time for adventure vol 2 - 01 Balance_0.mp3", None, None),
    "mus_w1": ("funked-up", "Funked Up.mp3", "funk", None),
    "mus_w1_boss": ("action-track", "actionTrack_0.mp3", "funk", None),
    "mus_w2": ("beach-stage", "beach_stage_bpm128_0.ogg", "marine", 128),
    "mus_w2_boss": ("battle-theme-a", "battleThemeA.mp3", "marine", None),
    "mus_w3": ("tropical-fantasy", "tropicalfantasy_2.ogg", "tropical", None),
    "mus_w4": ("8-bit-unknown-planet", "8bit_unknown_planet_0.mp3", "drums", None),
    "mus_w4_boss": ("castle-stage", "castle_stage_bpm180_0.ogg", "drums", 180),
    "mus_w5": ("green-hills", "Green Hills.mp3", "epic", None),
    "mus_w5_boss": ("boss-battle-music", "Juhani Junkala - Epic Boss Battle [Seamlessly Looping].wav", "epic", None),
    "mus_w6": ("let-the-games-begin-0", "S31-Let the Games Begin.ogg", "electro", None),
    "mus_w6_boss": ("8bit-action-final-boss-battle", "8bit_action_final_boss_battle_bpm185_0.ogg", "electro", 185),
    "mus_party": ("danza-d-bots-ii", "DanzaDeBots_2.mp3", None, None),
    "mus_results": ("apple-cider", "apple_cider.ogg", None, None),
}
SOURCES = {}


def save_ogg(path, x):
    x = A.normalize(np.asarray(x, dtype=np.float64))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    x = x.astype(np.float32)
    if x.ndim == 1:
        x = x[:, None]
    # por bloques: libsndfile se cae al escribir Vorbis largo de una vez
    with sf.SoundFile(path, "w", A.SR, x.shape[1], format="OGG", subtype="VORBIS") as f:
        for i in range(0, len(x), A.SR):
            f.write(x[i:i + A.SR])
    return path


def load_stereo(path):
    x, sr = sf.read(path, always_2d=True)
    if x.shape[1] == 1:
        x = np.repeat(x, 2, axis=1)
    if sr != A.SR:
        from math import gcd
        from scipy.signal import resample_poly
        g = gcd(int(sr), A.SR)
        x = resample_poly(x, A.SR // g, int(sr) // g, axis=0)
    return x[:, :2]


def tempo_phase(mono, known=None):
    hop = 512
    _, _, Z = stft(mono[:A.n_of(90)], A.SR, nperseg=2048, noverlap=2048 - hop)
    mag = np.log1p(np.abs(Z) * 10)
    flux = np.maximum(0, np.diff(mag, axis=1)).sum(0)
    flux = flux - np.convolve(flux, np.ones(16) / 16, "same")
    flux = np.maximum(flux, 0)
    fps = A.SR / hop
    if known:
        bpm = known
    else:
        ac = np.correlate(flux, flux, "full")[len(flux) - 1:]
        best, bpm = -1, 120
        for b in np.arange(70, 190, 0.25):
            lag = 60 * fps / b
            i = int(lag)
            v = ac[i] + (ac[i + 1] - ac[i]) * (lag - i)
            v *= np.exp(-0.5 * (np.log2(b / 118) / 0.9) ** 2)
            if v > best:
                best, bpm = v, b
    period = 60 * fps / bpm
    offs = np.arange(int(period))
    scores = [flux[(o + np.arange(0, len(flux) - o, period)).astype(int)].sum() for o in offs]
    phase = offs[int(np.argmax(scores))] / fps
    return float(bpm), float(phase)


def drum_layer(n, bpm, phase, style):
    beat = 60 / bpm
    x = np.zeros(n + A.n_of(1))
    t = phase % beat
    k = 0
    while t < n / A.SR:
        b = k % 4
        if style == "funk":
            if b in (0, 2):
                A.place(x, S.kick(), t)
            if b in (1, 3):
                A.place(x, S.snare() * 0.8, t)
            if b == 1:
                A.place(x, S.kick() * 0.6, t + beat * 0.5)
            for j in range(4):
                A.place(x, S.hat(0.04) * (0.8 if j % 2 == 0 else 0.5), t + j * beat / 4)
        elif style == "marine":
            A.place(x, S.kick() * (1.0 if b == 0 else 0.6), t)
            if b in (1, 3):
                A.place(x, S.conga(180) * 0.7, t + beat * 0.5)
            for j in range(2):
                A.place(x, S.shaker(), t + j * beat / 2)
        elif style == "tropical":
            A.place(x, S.conga(220 if b % 2 == 0 else 300) * 0.8, t)
            A.place(x, S.conga(260) * 0.5, t + beat * 0.75)
            A.place(x, S.woodblock(900) * 0.35, t + beat * 0.5)
            for j in range(4):
                A.place(x, S.shaker() * 0.8, t + j * beat / 4)
            if b == 0:
                A.place(x, S.kick(), t)
        elif style == "drums":
            A.place(x, S.thump(0.5, 90, 35), t)
            A.place(x, S.thump(0.35, 130, 60) * 0.6, t + beat * 0.5)
            if b == 3:
                A.place(x, S.thump(0.3, 160, 80) * 0.7, t + beat * 0.75)
            A.place(x, S.hat(0.05) * 0.5, t + beat * 0.5)
        elif style == "epic":
            if b in (0, 2):
                A.place(x, S.kick(), t)
            if b in (1, 3):
                A.place(x, S.snare(), t)
            if (k // 4) % 4 == 3 and b == 3:
                for j in range(4):
                    A.place(x, S.snare() * (0.4 + 0.15 * j), t + j * beat / 4)
            A.place(x, S.hat() * 0.5, t + beat * 0.5)
        elif style == "electro":
            A.place(x, S.kick(), t)
            A.place(x, S.hat(0.12) * 0.9, t + beat * 0.5)
            if b in (1, 3):
                A.place(x, S.snare() * 0.7, t)
            for j in (1, 3):
                A.place(x, S.hat(0.03) * 0.4, t + j * beat / 4)
        t += beat
        k += 1
    return A.lowpass(x[:n], 11000)


def stereoize(m, width=0.15):
    return np.stack([m * (1 - width), m * (1 - width)], axis=1) + np.stack(
        [A.highpass(m, 3000) * width, -A.highpass(m, 3000) * width * 0.0], axis=1)


def w3_boss(dur=96.0):
    """Pista rítmica de W3-5: percusión tropical + marimba, de 110 a 128 BPM (rampa en los primeros 64 s)."""
    n = A.n_of(dur)
    base = np.zeros(n + A.n_of(1))
    inten = np.zeros(n + A.n_of(1))
    scale = [0, 2, 4, 7, 9, 12, 14, 16]
    pattern = [0, 2, 4, 2, 5, 4, 2, 1, 0, 3, 4, 6, 5, 4, 2, 4]
    roots = [57, 53, 55, 52]
    t, k = 0.0, 0
    beats = []
    while t < dur:
        bpm = 110 + 18 * min(1.0, t / 64.0)
        beat = 60 / bpm
        beats.append(t)
        bar = k // 4
        root = roots[bar % 4]
        b = k % 4
        A.place(base, S.conga(220 if b % 2 == 0 else 300) * 0.7, t)
        A.place(base, S.shaker() * 0.7, t + beat * 0.5)
        A.place(base, S.woodblock(1000) * (0.5 if b == 0 else 0.25), t)
        A.place(base, S.tone(S.NOTE(root - 24), beat * 0.8, "tri", release=0.1) * 0.6, t)
        for j in range(2):
            m = root + scale[pattern[(2 * k + j) % 16]]
            A.place(base, S.bell(S.NOTE(m + 12), beat * 0.9, bright=0.4) * 0.35, t + j * beat / 2)
        A.place(inten, S.kick(), t)
        if b in (1, 3):
            A.place(inten, S.snare() * 0.8, t)
        for j in range(4):
            A.place(inten, S.hat(0.04) * 0.6, t + j * beat / 4)
        if b == 3:
            A.place(inten, S.conga(400) * 0.6, t + beat * 0.75)
        t += beat
        k += 1
    return base[:n], inten[:n], beats


def jingle_direction(path):
    x = A.load(path)
    x = A.trim(x, -40)
    seg = A.n_of(0.25)

    def pitch(s):
        spec = np.abs(np.fft.rfft(s * np.hanning(len(s)), 1 << 15))
        f = np.fft.rfftfreq(1 << 15, 1 / A.SR)
        m = (f > 150) & (f < 1500)
        return f[m][np.argmax(spec[m])]

    return np.log2(pitch(x[-seg - A.n_of(0.1):-A.n_of(0.1)]) / pitch(x[:seg])), len(x) / A.SR


def main():
    man = json.load(open(os.path.join(PACKS, "oga_music", "manifest.json"), encoding="utf-8"))
    report = {}
    for sid, (slug, fname, style, bpm_known) in ([] if "--stingers" in sys.argv else TRACKS.items()):
        path = os.path.join(PACKS, "oga_music", slug, fname)
        x = load_stereo(path)
        if len(x) > A.n_of(MAXLEN):
            x = x[:A.n_of(MAXLEN)]
            k = A.n_of(3)
            x[-k:] *= np.linspace(1, 0, k)[:, None]
        meta = man[slug]
        src = f"OGA:{slug} · {meta['title']} · {meta['author']} · CC0 · {meta['url']}"
        if style:
            save_ogg(os.path.join(MUSIC, f"{sid}_base.ogg"), x)
            bpm, ph = tempo_phase(x.mean(axis=1), bpm_known)
            layer = drum_layer(len(x), bpm, ph, style)
            save_ogg(os.path.join(MUSIC, f"{sid}_intensity.ogg"), stereoize(layer))
            SOURCES[f"Music/{sid}_base.ogg"] = [src]
            SOURCES[f"Music/{sid}_intensity.ogg"] = [f"síntesis propia a {bpm:.1f} BPM (build_music.py)"]
            report[sid] = dict(bpm=round(bpm, 1), fase=round(ph, 3), estilo=style, seg=round(len(x) / A.SR, 1))
        else:
            save_ogg(os.path.join(MUSIC, f"{sid}.ogg"), x)
            SOURCES[f"Music/{sid}.ogg"] = [src]
            report[sid] = dict(seg=round(len(x) / A.SR, 1))
        print("ok", sid, report[sid])
    if "--stingers" in sys.argv:
        old = json.load(open(os.path.join(MUSIC, "_music_report.json"), encoding="utf-8"))
        old.update(report)
        report = old
    b, i, beats = w3_boss()
    save_ogg(os.path.join(MUSIC, "mus_w3_boss_base.ogg"), stereoize(b))
    save_ogg(os.path.join(MUSIC, "mus_w3_boss_intensity.ogg"), stereoize(i))
    json.dump([round(t, 4) for t in beats], open(os.path.join(MUSIC, "mus_w3_boss_beats.json"), "w"))
    for f in ("mus_w3_boss_base.ogg", "mus_w3_boss_intensity.ogg"):
        SOURCES[f"Music/{f}"] = ["síntesis propia 110→128 BPM (build_music.py)"]
    report["mus_w3_boss"] = dict(bpm="110→128", seg=96)
    # stingers: Kenney Music Jingles por dirección de la melodía
    jingles = sorted(glob.glob(os.path.join(PACKS, "jingleSounds_Kenney", "OGG", "*", "*.ogg")))
    dirs = []
    for j in jingles:
        d, L = jingle_direction(j)
        if 0.8 <= L <= 6.0:
            dirs.append((d, j))
    dirs.sort()
    fail = [j for d, j in dirs[:3]]
    win = [j for d, j in dirs[::-1] if j not in fail][:3]
    for sid, picks in (("stinger_victory", win), ("stinger_fail", fail)):
        for k, j in enumerate(picks, 1):
            x = load_stereo(j)
            save_ogg(os.path.join(MUSIC, f"{sid}_{k:02d}.ogg"), x)
            SOURCES[f"Music/{sid}_{k:02d}.ogg"] = [f"Music Jingles (Kenney)/{os.path.basename(j)}"]
        report[sid] = [os.path.basename(j) for j in picks]
    json.dump(report, open(os.path.join(MUSIC, "_music_report.json"), "w", encoding="utf-8"), indent=1,
              ensure_ascii=False)
    sp = os.path.join(A.OUT, "sources.json")
    old = json.load(open(sp, encoding="utf-8")) if os.path.exists(sp) else {}
    old.update(SOURCES)
    json.dump(old, open(sp, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print(json.dumps(report, indent=1))


if __name__ == "__main__":
    main()
