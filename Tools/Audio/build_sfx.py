"""Arma los efectos de docs/08 §4 (habilidades, balsa, agua, obstáculos, power-ups, eventos y UI)
a partir de packs CC0 descargados + síntesis propia para lo que falta.
uso: python -I build_sfx.py <carpeta_packs>
La carpeta de packs contiene: kenney_interfaceSounds/, kenney_impact/, jingleSounds_Kenney/, oga_sfx/<slug>/.
Salida: Tools/Export/Audio/SFX/<Grupo>/<id>_<nn>.wav y Tools/Export/Audio/sources.json (origen de cada archivo)."""
import glob
import json
import os
import sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_audio as A
import fart_synth as F

PACKS = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "_packs")
SFX = os.path.join(A.OUT, "SFX")
SOURCES = {}
rng = np.random.default_rng(2026)

# ---------------------------------------------------------------------------
# Fuentes de los packs
# ---------------------------------------------------------------------------
PACK_INFO = {
    "kenney_interfaceSounds": ("Interface Sounds", "Kenney", "https://kenney.nl/assets/interface-sounds"),
    "kenney_impact": ("Impact Sounds", "Kenney", "https://kenney.nl/assets/impact-sounds"),
    "jingleSounds_Kenney": ("Music Jingles", "Kenney", "https://kenney.nl/assets/music-jingles"),
}


def find(pattern):
    hits = sorted(glob.glob(os.path.join(PACKS, "**", pattern), recursive=True))
    hits = [h for h in hits if not os.path.basename(h).startswith("._")]
    if not hits:
        raise FileNotFoundError(pattern)
    # preferir .wav/.ogg sobre .mp3 si hay duplicados
    return hits[0]


def src_label(path):
    rel = os.path.relpath(path, PACKS).replace("\\", "/")
    top = rel.split("/")[0]
    if top == "oga_sfx":
        slug = rel.split("/")[1]
        return f"OGA:{slug}/{os.path.basename(path)}"
    return f"{PACK_INFO.get(top, (top,))[0]} (Kenney)/{os.path.basename(path)}"


def clip(pattern, pitch=1.0, maxdur=None, gain=1.0, start=0.0):
    p = find(pattern)
    x = A.load(p)
    if start:
        x = x[A.n_of(start):]
    if pitch != 1.0:
        x = A.pitch_speed(x, pitch)
    x = A.trim(x, -50)
    if maxdur and len(x) > A.n_of(maxdur):
        x = x[:A.n_of(maxdur)]
        k = A.n_of(min(0.15, maxdur / 3))
        x[-k:] *= np.linspace(1, 0, k)
    return x * gain, src_label(p)


def out(group, sid, i, x, srcs, loop=False):
    path = os.path.join(SFX, group, f"{sid}_{i:02d}.wav")
    A.save(path, x, loop=loop)
    SOURCES[os.path.relpath(path, A.OUT).replace("\\", "/")] = srcs
    return path


def from_pack(group, sid, patterns, **kw):
    for i, pat in enumerate(patterns, 1):
        x, s = clip(pat, **kw)
        out(group, sid, i, x, [s])


# ---------------------------------------------------------------------------
# Síntesis
# ---------------------------------------------------------------------------
SYN = "síntesis propia (Tools/Audio/build_sfx.py)"


def osc(freq, n, wave="sine", phase=0.0):
    f = np.broadcast_to(np.asarray(freq, dtype=float), (n,))
    ph = np.cumsum(2 * np.pi * f / A.SR) + phase
    if wave == "sine":
        return np.sin(ph)
    if wave == "square":
        return np.sign(np.sin(ph)) * 0.6
    if wave == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    if wave == "saw":
        return ((ph / (2 * np.pi)) % 1.0) * 2 - 1
    raise ValueError(wave)


def tone(f, dur, wave="sine", attack=0.005, release=0.05, decay=None):
    n = A.n_of(dur)
    x = osc(f, n, wave) * A.env(n, attack, release)
    if decay:
        x *= A.exp_decay(n, decay)
    return x


def bell(f, dur, bright=1.0):
    n = A.n_of(dur)
    x = np.zeros(n)
    for ratio, amp, tau in ((1, 1, dur / 2.5), (2.0, 0.5 * bright, dur / 4), (2.76, 0.35 * bright, dur / 6),
                            (5.4, 0.2 * bright, dur / 10)):
        x += amp * osc(f * ratio, n) * A.exp_decay(n, tau)
    return x * A.env(n, 0.002, 0.02)


def noise(dur, lo=None, hi=None):
    x = rng.standard_normal(A.n_of(dur))
    if lo and hi:
        return A.bandpass(x, lo, hi)
    if hi:
        return A.lowpass(x, hi)
    if lo:
        return A.highpass(x, lo)
    return x


def nrm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def whoosh(dur, f0=300, f1=3000, down=False):
    n = A.n_of(dur)
    u = np.linspace(0, 1, n)
    x = A.sweep_lowpass(rng.standard_normal(n), f0 if not down else f1, f1 if not down else f0)
    return nrm(x) * np.sin(np.pi * u) ** 1.5


def boing(f0, dur, depth=0.35, rate=14, rise=0.6):
    n = A.n_of(dur)
    t = A.t_axis(n)
    f = f0 * (1 + rise * t / dur) * (1 + depth * np.sin(2 * np.pi * rate * t) * np.exp(-t * 3))
    return osc(f, n, "tri") * A.exp_decay(n, dur / 2.5) * A.env(n, 0.003, 0.03)


def thump(dur=0.35, f0=110, f1=40):
    n = A.n_of(dur)
    f = np.geomspace(f0, f1, n)
    return osc(f, n) * A.exp_decay(n, dur / 4) * A.env(n, 0.002, 0.03)


def kick():
    return A.mix(thump(0.3, 140, 45), 0.1 * nrm(noise(0.01, 1000, 5000)))


def snare():
    n = A.n_of(0.2)
    return 0.6 * nrm(noise(0.2, 1500, 8000)) * A.exp_decay(n, 0.05) + 0.5 * tone(190, 0.2, decay=0.04)


def hat(dur=0.06):
    n = A.n_of(dur)
    return 0.4 * nrm(noise(dur, 7000, None)) * A.exp_decay(n, dur / 4)


def conga(f=220):
    n = A.n_of(0.25)
    return osc(np.geomspace(f * 1.3, f, n), n) * A.exp_decay(n, 0.07)


def woodblock(f=900):
    n = A.n_of(0.12)
    return (osc(f, n) + 0.4 * osc(f * 2.7, n)) * A.exp_decay(n, 0.02)


def shaker():
    n = A.n_of(0.08)
    return 0.25 * nrm(noise(0.08, 5000, 12000)) * np.sin(np.pi * np.linspace(0, 1, n)) ** 2


def rumble(dur, f=90):
    n = A.n_of(dur)
    return nrm(A.lowpass(rng.standard_normal(n), f, 4))


def hiss(dur, lo=2000, hi=9000):
    return nrm(noise(dur, lo, hi))


def bubbles(dur, count, fmin=300, fmax=1200):
    x = np.zeros(A.n_of(dur + 0.1))
    for _ in range(count):
        bd = rng.uniform(0.03, 0.08)
        bn = A.n_of(bd)
        bt = A.t_axis(bn)
        f = rng.uniform(fmin, fmax) * (1 + rng.uniform(2, 6) * bt)
        A.place(x, osc(f, bn) * A.exp_decay(bn, bd / 3) * rng.uniform(0.4, 1), rng.uniform(0, dur))
    return x


def seq(events, dur):
    """events: lista de (tiempo, señal)."""
    x = np.zeros(A.n_of(dur))
    for t, s in events:
        A.place(x, s, t)
    return x


def arpeggio(freqs, step, dur_each, wave="square", bellish=False):
    ev = []
    for i, f in enumerate(freqs):
        ev.append((i * step, bell(f, dur_each) if bellish else tone(f, dur_each, wave, release=dur_each * 0.6)))
    return seq(ev, step * len(freqs) + dur_each)


NOTE = lambda m: 440.0 * 2 ** ((m - 69) / 12)  # noqa: E731


def party_loop(bpm=124, variant=0):
    """Fiesta Real: loop de 4 s aprox. (2 compases) con bombo, palmas, hi-hat, bajo y acordes."""
    beat = 60 / bpm
    bars = 2
    L = beat * 4 * bars
    n = A.n_of(L)
    x = np.zeros(n + A.n_of(0.5))
    roots = [57, 53] if variant == 0 else [60, 55]
    for b in range(bars * 4):
        t = b * beat
        A.place(x, kick(), t)
        A.place(x, hat(), t + beat / 2)
        if b % 2 == 1:
            A.place(x, snare() * 0.8, t)
        for k in range(4):
            A.place(x, shaker() * 0.6, t + k * beat / 4)
        root = roots[(b // 4) % 2]
        A.place(x, tone(NOTE(root - 24), beat * 0.45, "saw", release=0.05) * 0.35, t + beat / 2)
        if b % 2 == 0:
            for m in (root, root + 4 if variant else root + 3, root + 7):
                A.place(x, tone(NOTE(m), beat * 0.3, "square", release=0.1) * 0.12, t + beat * 0.75)
    x = A.lowpass(x, 9000)
    # cola de reverberación del final sobre el principio para que el loop cierre
    tail = x[n:].copy()
    x = x[:n]
    x[:len(tail)] += tail
    return x


def balloon_pop(i):
    pop = nrm(noise(0.03, 300, 9000)) * A.exp_decay(A.n_of(0.03), 0.006)
    dd = 0.5 + 0.2 * i
    n = A.n_of(dd)
    t = A.t_axis(n)
    f = (500 + 300 * i) * np.exp(-t * 3) * (1 + 0.15 * np.sin(2 * np.pi * 22 * t))
    deflate = (0.5 * osc(f, n, "saw") + 0.5 * nrm(noise(dd, 800, 5000))) * A.env(n, 0.01, dd * 0.6)
    return seq([(0, pop), (0, thump(0.2, 160, 60) * 0.7), (0.02, A.lowpass(deflate, 4000) * 0.45)], dd + 0.1)


def inflate(dur=1.0, pumps=3):
    ev = []
    for k in range(pumps):
        d = dur / pumps
        n = A.n_of(d * 0.8)
        h = hiss(d * 0.8, 1500 + 400 * k, 7000) * np.sin(np.pi * np.linspace(0, 1, n)) * 0.6
        ev.append((k * d, h))
    ev.append((dur - 0.05, boing(500, 0.25, 0.2, 18, 0.8) * 0.5))
    return seq(ev, dur + 0.3)


def water_loop(dur=6.0, lo=200, hi=2500, burble=30, variant=0):
    n = A.n_of(dur + 1.0)
    base = A.bandpass(rng.standard_normal(n), lo, hi)
    mod = 0.7 + 0.3 * A.smooth_noise(n, 2, rng)
    x = nrm(base * mod)
    x += 0.5 * bubbles(dur + 0.9, burble, 250, 900)[:n]
    return A.loopify(x, 1.0)


def wind(dur, i):
    n = A.n_of(dur)
    u = np.linspace(0, 1, n)
    x = rng.standard_normal(n)
    out_ = np.zeros(n)
    centers = 300 + 900 * (np.sin(np.pi * u) ** 2) * (1 + 0.3 * A.smooth_noise(n, 1.5, rng))
    edges = np.linspace(0, n, 40).astype(int)
    for a, b in zip(edges[:-1], edges[1:]):
        c = float(np.mean(centers[a:b]))
        out_[a:b] = A.bandpass(x[max(0, a - 800):b], c * 0.6, c * 1.6)[-(b - a):]
    return nrm(out_) * np.sin(np.pi * u) ** (1.2 + 0.3 * i)


def chirps(dur, count, fmin, fmax):
    ev = []
    for _ in range(count):
        d = rng.uniform(0.02, 0.06)
        n = A.n_of(d)
        f = np.geomspace(rng.uniform(fmax * 0.8, fmax), rng.uniform(fmin, fmin * 1.3), n)
        ev.append((rng.uniform(0, dur), osc(f, n) * np.sin(np.pi * np.linspace(0, 1, n))))
    return seq(ev, dur + 0.1)


# ---------------------------------------------------------------------------
# Construcción por grupo
# ---------------------------------------------------------------------------
def abilities():
    g = "Abilities"
    for i, (pat, p) in enumerate((("roar_01.*", 1.12), ("roar_02.*", 1.18), ("roar_03.*", 1.1)), 1):
        x, s = clip(pat, pitch=p)
        out(g, "ability_roar", i, A.mix(x, 0.3 * rumble(len(x) / A.SR, 120)), [s, SYN])
    for i in (1, 2):
        hits = [clip(f"impactWood_light_00{k}.ogg", pitch=1.3 + 0.1 * i)[0] for k in range(3)]
        ev = []
        for bar in range(3):
            t0 = bar * 0.28
            for k, dt in enumerate((0, 0.07, 0.14)):
                ev.append((t0 + dt, hits[k] * (0.7 + 0.3 * (k == 2))))
        w, s = clip(f"swish-{3 + i}.*")
        out(g, "ability_gallop", i, seq(ev + [(0.3, w * 0.9)], 1.4), ["Impact Sounds (Kenney)/impactWood_light",
                                                                       s, SYN])
    for i, pat in enumerate(("spring_01.*", "spring_04.*"), 1):
        x, s = clip(pat, pitch=0.9)
        out(g, "ability_neck", i, x, [s])
    out(g, "ability_neck", 3, boing(220, 0.7, 0.3, 11, 1.5), [SYN])
    for i in (1, 2):
        sp, s1 = clip(f"splash_{[12, 14][i - 1]:02d}.*")
        hit, s2 = clip(f"impactSoft_heavy_00{i}.ogg", pitch=0.8)
        x = seq([(0, hit), (0, thump(0.4, 90, 35)), (0.03, sp * 0.9)], max(len(sp) / A.SR + 0.1, 0.8))
        out(g, "ability_bellyflop", i, x, [s1, s2, SYN])
    for i in (0, 1):
        out(g, "ability_party", i + 1, party_loop(120 + 4 * i, i), [SYN], loop=True)
    for i in (1, 2):
        pa, s = clip(f"paper_0{i}.*", maxdur=0.4)
        out(g, "ability_repair", i, seq([(0, pa), (0.3, inflate(0.9, 2 + i))], 1.6), [s, SYN])


def raft():
    g = "Raft"
    from_pack(g, "raft_hit_soft", ["impactSoft_medium_000.ogg", "impactSoft_medium_002.ogg",
                                   "impactSoft_medium_004.ogg"])
    for i, k in enumerate((0, 2, 4), 1):
        a, s1 = clip(f"impactSoft_heavy_00{k}.ogg")
        b, s2 = clip(f"impactPunch_heavy_00{k}.ogg", gain=0.6)
        out(g, "raft_hit_hard", i, A.mix(a, b, 0.4 * thump(0.3, 100, 40)), [s1, s2, SYN])
    for i in range(3):
        out(g, "raft_chamber_pop", i + 1, balloon_pop(i), [SYN])
    for i in range(2):
        out(g, "raft_chamber_repair", i + 1, inflate(0.8 + 0.3 * i, 3 + i), [SYN])
    for i in range(2):
        d = 1.6
        n = A.n_of(d)
        t = A.t_axis(n)
        p = dict(F.CHARS["gloria"], f0=70 + 25 * i, flut_d=0.6)
        rasp = F.core(n, p["f0"] * np.exp(-t * 0.8), p, rng) * A.env(n, 0.02, 0.5)
        x = 0.6 * rasp + 0.6 * bubbles(d, 25, 150, 500)[:n] + 0.4 * rumble(d, 150) * np.linspace(1, 0.2, n)
        out(g, "raft_sink", i + 1, x, [SYN])
    for i, k in enumerate((11, 13, 15), 1):
        sp, s1 = clip(f"splash_{k:02d}.*")
        hit, s2 = clip(f"impactSoft_heavy_00{i}.ogg", gain=0.7)
        out(g, "raft_land_water", i, A.mix(sp, hit), [s1, s2])
    for i in range(3):
        hit, s = clip(f"impactSoft_medium_00{i + 1}.ogg", gain=0.6)
        out(g, "raft_bounce", i + 1, seq([(0, hit), (0, boing(110 + 30 * i, 0.45, 0.4, 9, 0.3) * 0.8)], 0.6),
            [s, SYN])
    for i in range(2):
        L = 4.0
        x = np.zeros(A.n_of(L + 0.5))
        for _ in range(5 + 3 * i):
            d = rng.uniform(0.15, 0.4)
            n = A.n_of(d)
            f = rng.uniform(250, 600) * (1 + 0.3 * np.sin(np.pi * np.linspace(0, 1, n)))
            sq = osc(f, n, "saw") * A.env(n, 0.03, d * 0.5) * (0.4 + 0.6 * np.abs(A.smooth_noise(n, 40, rng)))
            A.place(x, A.bandpass(sq, 300, 2500) * rng.uniform(0.3, 0.8), rng.uniform(0, L))
        out(g, "raft_creak_loop", i + 1, A.loopify(x, 0.5), [SYN], loop=True)


def water():
    g = "Water"
    durs = []
    for k in range(1, 16):
        x, _ = clip(f"splash_{k:02d}.*")
        durs.append((len(x), k))
    durs.sort()
    from_pack(g, "water_splash_small", [f"splash_{k:02d}.*" for _, k in durs[:3]])
    from_pack(g, "water_splash_big", [f"splash_{k:02d}.*" for _, k in durs[-3:]])
    for i, pat in enumerate(("loop_water_01.*", "water_flowing.*"), 1):
        x, s = clip(pat)
        x = x[:A.n_of(min(len(x) / A.SR, 12))]
        out(g, "water_flow_loop", i, A.loopify(x, 1.0), [s, "loop sin cortes por fundido (build_sfx.py)"],
            loop=True)
    out(g, "water_flow_loop", 3, water_loop(6, 200, 2500, 30), [SYN], loop=True)
    for i, pat in enumerate(("park_ambience_river.*", "loop_water_03.*"), 1):
        x, s = clip(pat, maxdur=None)
        x = x[:A.n_of(min(len(x) / A.SR, 10))]
        roar = A.bandpass(rng.standard_normal(len(x)), 150, 4000) * 0.5 * np.std(x) / 0.2
        out(g, "water_rapids_loop", i, A.loopify(x + roar, 1.0), [s, SYN], loop=True)
    for i in range(2):
        n = A.n_of(7)
        x = nrm(A.lowpass(rng.standard_normal(n), 2500 + 1500 * i, 2)) + 0.6 * rumble(7, 80)
        out(g, "water_waterfall_loop", i + 1, A.loopify(x * (0.9 + 0.1 * A.smooth_noise(n, 1, rng)), 1.0),
            [SYN], loop=True)
    for i in range(3):
        sp, s = clip(f"splash_{durs[i][1]:02d}.*", pitch=1.25 + 0.1 * i, gain=0.8)
        out(g, "swim_stroke", i + 1, seq([(0, whoosh(0.15, 400, 2500) * 0.3), (0.05, sp)], 0.6), [s, SYN])
    for i in (1, 2):
        st, s1 = clip(f"sfx100v2_footstep_wet_0{i}.*")
        hit, s2 = clip(f"impactSoft_medium_00{i}.ogg", gain=0.7)
        out(g, "climb_aboard", i, seq([(0, hit), (0.12, st), (0.2, boing(300, 0.2, 0.2, 20, 0.2) * 0.25)], 0.8),
            [s1, s2, SYN])


def hazards():
    g = "Hazards"
    from_pack(g, "gate_open", ["sfx100v2_door_01.*", "door_open.*"])
    from_pack(g, "gate_close", ["door_close_01.*", "slam_02.*"])
    for i in range(2):
        d = 1.8
        n = A.n_of(d)
        x = hiss(d, 2500, 9000) * (0.6 + 0.4 * np.abs(A.smooth_noise(n, 30, rng)))
        crack = np.zeros(n)
        for _ in range(40 + 20 * i):
            k = int(rng.uniform(0, n - 200))
            crack[k:k + 120] += rng.standard_normal(120) * np.exp(-np.arange(120) / 20)
        out(g, "barrel_fuse", i + 1, 0.6 * x + 0.5 * nrm(A.highpass(crack, 2000)), [SYN])
    for i in range(2):
        ex, s = clip("explosion.*", pitch=1.0 - 0.1 * i)
        out(g, "barrel_explode", i + 1, A.mix(ex, thump(0.6, 80, 30), 0.4 * F.fire(rng, 0)), [s, SYN])
    from_pack(g, "crate_break", ["bfh1_wood_breaking_01.*", "bfh1_wood_breaking_02.*", "bfh1_wood_breaking_03.*"])
    for i in range(3):
        out(g, "wind_gust", i + 1, wind(1.4 + 0.4 * i, i), [SYN])
    for i in range(2):
        L = 5.0
        n = A.n_of(L + 1)
        t = A.t_axis(n)
        x = rng.standard_normal(n)
        swirl = np.zeros(n)
        edges = np.linspace(0, n, 120).astype(int)
        for a, b in zip(edges[:-1], edges[1:]):
            c = 500 + 350 * np.sin(2 * np.pi * (1.2 + 0.4 * i) * t[a])
            swirl[a:b] = A.bandpass(x[max(0, a - 600):b], c * 0.7, c * 1.4)[-(b - a):]
        y = nrm(swirl) + 0.5 * bubbles(L + 0.9, 40, 200, 700)[:n] + 0.4 * rumble(L + 1, 100)
        out(g, "whirlpool_loop", i + 1, A.loopify(y, 1.0), [SYN], loop=True)
    for i, pat in enumerate(("impactMetal_light_001.ogg", "bfh1_metal_hit_02.*"), 1):
        x, s = clip(pat)
        out(g, "spikes_hit", i, A.mix(x, 0.5 * balloon_pop(0)), [s, SYN])
    for i in (1, 2):
        st, s = clip(f"footstep_snow_00{i}.ogg", pitch=0.75)
        grind = nrm(noise(0.6, 300, 2500)) * A.env(A.n_of(0.6), 0.05, 0.3) * 0.5
        out(g, "sand_stuck", i, A.mix(st, grind), [s, SYN])
    for i in range(2):
        x = F.fire(rng, i)
        out(g, "vine_burn", i + 1, np.concatenate([x, x[::-1] * 0.6]), [SYN])
    for i, pat in enumerate(("rolling.*", "machine_03.*"), 1):
        x, s = clip(pat)
        x = x[:A.n_of(min(len(x) / A.SR, 6))]
        out(g, "log_spin_loop", i, A.loopify(A.lowpass(x, 2500), 0.6), [s], loop=True)
    for i in range(2):
        ev = [(rng.uniform(0, 1.0), nrm(noise(0.12, 1500, 7000)) * A.env(A.n_of(0.12), 0.01, 0.08)
               * rng.uniform(0.3, 1)) for _ in range(9 + 4 * i)]
        out(g, "fossa_rustle", i + 1, seq(ev, 1.3), [SYN])
    for i, pat in enumerate(("monster_02.*", "grunt_03.*"), 1):
        x, s = clip(pat, pitch=1.1)
        w, s2 = clip(f"swish-{i}.*")
        out(g, "fossa_attack", i, seq([(0, w), (0.08, x)], len(x) / A.SR + 0.2), [s, s2])
    for i, pat in enumerate(("snore.*", "monster_05.*"), 1):
        x, s = clip(pat, pitch=0.8, maxdur=1.5)
        sp, s2 = clip("splash_03.*", gain=0.5)
        out(g, "croc_wake", i, A.mix(x, np.pad(sp, (A.n_of(0.4), 0))), [s, s2])
    for i, pat in enumerate(("eat_01.*", "eat_03.*"), 1):
        x, s = clip(pat, pitch=0.85)
        hit, s2 = clip("impactPunch_heavy_001.ogg")
        snap = woodblock(1400) * 0.8
        out(g, "croc_bite", i, seq([(0, snap), (0.01, hit), (0.03, x)], 0.8), [s, s2, SYN])
    out(g, "beat_click", 1, woodblock(950), [SYN])
    out(g, "beat_click", 2, A.mix(woodblock(1400), 0.3 * hat(0.03)), [SYN])
    for i in range(2):
        d = 1.5
        n = A.n_of(d)
        x = rumble(d, 70 + 30 * i) * np.linspace(0.2, 1, n) + 0.6 * bubbles(d, 20 + 10 * i, 120, 500)[:n] * np.linspace(0, 1, n)
        out(g, "geyser_warn", i + 1, x, [SYN])
        burst = seq([(0, thump(0.4, 120, 40)), (0.0, hiss(1.2, 800, 9000) * A.env(A.n_of(1.2), 0.01, 0.8)),
                     (0.1, clip(f"splash_0{5 + i}.*")[0] * 0.8)], 1.5)
        out(g, "geyser_burst", i + 1, burst, [SYN, "OGA:40-cc0-water-splash-slime-sfx"])
    for i in (1, 2):
        rf, s = clip(f"bfh1_rock_falling_0{i}.*", gain=0.6)
        out(g, "rock_warn", i, A.mix(rumble(1.2, 90) * A.env(A.n_of(1.2), 0.4, 0.3), rf), [s, SYN])
        rb, s2 = clip(f"bfh1_rock_breaking_0{i}.*")
        out(g, "rock_impact", i, A.mix(rb, thump(0.5, 90, 35)), [s2, SYN])
    for i in range(2):
        out(g, "bats_screech", i + 1, A.mix(chirps(1.0, 14 + 6 * i, 2500, 7000), 0.3 * whoosh(1.0, 500, 2500)), [SYN])
    for i in range(2):
        n = A.n_of(0.04)
        click = (osc(3200 + 400 * i, n) + nrm(noise(0.04, 3000, 9000))) * A.exp_decay(n, 0.006)
        out(g, "crab_snip", i + 1, seq([(0, click), (0.09 + 0.03 * i, click * 0.9)], 0.25), [SYN])
    for i in range(2):
        d = 1.0 + 0.3 * i
        n = A.n_of(d)
        f = np.geomspace(2200, 700, n)
        out(g, "coconut_whistle", i + 1, osc(f, n) * A.env(n, 0.05, 0.1) * (1 + 0.1 * osc(6, n)), [SYN])
    for i, pat in enumerate(("alarm_01.*", "alarm_02.*"), 1):
        x, s = clip(pat, maxdur=1.6)
        out(g, "crane_warn", i, x, [s])
    for i, pat in enumerate(("impactMetal_heavy_001.ogg", "slam_05.*"), 1):
        x, s = clip(pat)
        out(g, "crane_slam", i, A.mix(x, thump(0.5, 70, 30)), [s, SYN])
    for i, pat in enumerate(("slime_03.*", "slime_08.*"), 1):
        x, s = clip(pat, pitch=0.8)
        out(g, "tentacle_warn", i, A.mix(x, 0.6 * bubbles(1.0, 15, 150, 450)), [s, SYN])
        y, s2 = clip(f"slime_1{i}.*", pitch=0.7)
        sp, s3 = clip("splash_15.*", gain=0.7)
        out(g, "tentacle_slam", i, A.mix(y, thump(0.5, 80, 30), sp), [s2, s3, SYN])
    for i, pat in enumerate(("roar_02.*", "monster_07.*"), 1):
        x, s = clip(pat, pitch=0.68)
        out(g, "giant_croc_roar", i, A.mix(x, 0.4 * rumble(len(x) / A.SR, 80)), [s, SYN])
    for i in range(2):
        ev = [(rng.uniform(0, 0.7), nrm(noise(0.05, 800, 4000)) * A.exp_decay(A.n_of(0.05), 0.012))
              for _ in range(18)]
        out(g, "net_tangle", i + 1, seq(ev + [(0, whoosh(0.4, 300, 2000) * 0.5)], 0.9), [SYN])


def powerups_events():
    g = "PowerUps"
    C5 = 72
    # cada pickup con su carácter
    out(g, "pickup_beans", 1, seq([(0, boing(300, 0.25, 0.3, 20, 1.0)), (0.12, arpeggio([NOTE(C5), NOTE(C5 + 7)], 0.06, 0.15))], 0.5), [SYN])
    out(g, "pickup_beans", 2, seq([(0, woodblock(700)), (0.05, arpeggio([NOTE(C5 + 4), NOTE(C5 + 12)], 0.07, 0.15))], 0.4), [SYN])
    for i in range(2):
        out(g, "pickup_locoto", i + 1, seq([(0, F.fire(rng, i)[:A.n_of(0.5)] * 0.6),
                                            (0, arpeggio([NOTE(C5 + k) for k in (0, 3, 7, 12)], 0.05, 0.12, "saw"))], 0.6), [SYN])
    for i in range(2):
        out(g, "pickup_mango", i + 1, arpeggio([NOTE(C5 + k + 2 * i) for k in (0, 4, 7, 12)], 0.055, 0.25, bellish=True), [SYN])
    for i in range(2):
        out(g, "pickup_coconut", i + 1, seq([(0, woodblock(500 + 200 * i)), (0.08, bell(NOTE(C5 + 12), 0.6) * 0.6),
                                             (0.08, hiss(0.5, 5000, 12000) * A.exp_decay(A.n_of(0.5), 0.15) * 0.2)], 0.8), [SYN])
    for i in range(2):
        out(g, "pickup_soda", i + 1, seq([(0, nrm(noise(0.04, 1000, 8000)) * A.exp_decay(A.n_of(0.04), 0.01)),
                                          (0.02, bubbles(0.5, 12, 600, 1600) * 0.8),
                                          (0.02, hiss(0.5, 6000, 14000) * A.env(A.n_of(0.5), 0.01, 0.4) * 0.3)], 0.7), [SYN])
    for i in range(2):
        n = A.n_of(0.5)
        bw = osc(np.geomspace(260 - 40 * i, 110, n), n, "saw") * A.env(n, 0.01, 0.2)
        out(g, "pickup_onion", i + 1, A.lowpass(bw, 1500) + 0.3 * whoosh(0.5, 200, 1200), [SYN])
    for i in range(2):
        notes = [NOTE(C5 + 12 + k) for k in ((0, 4, 7, 12, 16) if i == 0 else (0, 5, 9, 12, 17))]
        x = arpeggio(notes, 0.05, 0.6, bellish=True)
        out(g, "pickup_golden_mango", i + 1, A.mix(x, 0.2 * chirps(0.8, 10, 4000, 9000)), [SYN])
    g = "Events"
    for k, cnt in ((2, 2), (3, 3), (4, 4)):
        notes = [NOTE(60 + s) for s in (0, 4, 7, 12)[:cnt]]
        for i, wave in enumerate(("square", "tri"), 1):
            x = seq([(j * 0.045, tone(f, 0.35, wave, release=0.25) * (0.8 + 0.1 * j)) for j, f in enumerate(notes)]
                    + [(0, thump(0.2, 150, 60) * 0.5)], 0.6)
            out(g, f"sync_{k}", i, x, [SYN])
    for i in range(2):
        n = A.n_of(1.0)
        sh = osc(np.geomspace(300, 1200 + 400 * i, n), n, "saw") * np.linspace(0.1, 1, n) * 0.4
        out(g, "superfart_ready", i + 1, seq([(0, A.lowpass(sh, 4000)), (0.9, bell(NOTE(84), 0.6))], 1.6), [SYN])
        p = dict(F.CHARS["alex"], drive=4)
        m = F.mega(p, rng, i)
        out(g, "superfart_launch", i + 1, A.mix(m, thump(0.8, 70, 25), whoosh(1.2, 300, 6000) * 0.6), [SYN])
    from_pack(g, "checkpoint", ["confirmation_002.ogg", "confirmation_004.ogg"])
    out(g, "checkpoint", 3, arpeggio([NOTE(n) for n in (72, 76, 79, 84)], 0.07, 0.4, bellish=True), [SYN])
    out(g, "countdown_beep", 1, tone(NOTE(69), 0.18, "square", release=0.04), [SYN])
    out(g, "countdown_beep", 2, tone(NOTE(72), 0.18, "tri", release=0.04), [SYN])
    out(g, "countdown_go", 1, tone(NOTE(81), 0.5, "square", release=0.2), [SYN])
    out(g, "countdown_go", 2, A.mix(tone(NOTE(84), 0.5, "tri", release=0.2), tone(NOTE(76), 0.5, "tri", release=0.2)), [SYN])
    from_pack(g, "finish_fanfare", ["jingles_SAX02.ogg", "jingles_STEEL00.ogg", "jingles_NES00.ogg"])
    for i, m in enumerate((72, 76, 79), 1):
        out(g, "star_earn", i, A.mix(bell(NOTE(m + 12), 0.7), 0.2 * chirps(0.4, 4, 5000, 9000)), [SYN])
    out(g, "ping_now", 1, seq([(0, tone(NOTE(88), 0.07, "square")), (0.1, tone(NOTE(88), 0.07, "square"))], 0.25), [SYN])
    out(g, "ping_wait", 1, tone(NOTE(64), 0.45, "tri", attack=0.03, release=0.2), [SYN])
    n = A.n_of(0.35)
    out(g, "ping_turn", 1, osc(np.geomspace(NOTE(72), NOTE(79), n), n, "tri") * A.env(n, 0.01, 0.1), [SYN])
    n = A.n_of(0.6)
    out(g, "ping_nogas", 1, seq([(0, tone(NOTE(67), 0.25, "saw", release=0.1) * 0.6),
                                 (0.28, osc(np.geomspace(NOTE(63), NOTE(58), A.n_of(0.3)), A.n_of(0.3), "saw")
                                  * A.env(A.n_of(0.3), 0.01, 0.15) * 0.6)], 0.65), [SYN])


def ui():
    g = "UI"
    from_pack(g, "ui_move", ["tick_001.ogg", "scroll_002.ogg", "click_001.ogg"])
    from_pack(g, "ui_select", ["select_002.ogg", "confirmation_001.ogg", "select_005.ogg"])
    from_pack(g, "ui_back", ["back_001.ogg", "back_003.ogg"])
    from_pack(g, "ui_locked", ["error_004.ogg", "error_006.ogg"])
    coin = seq([(0, tone(NOTE(83), 0.08, "square")), (0.07, tone(NOTE(88), 0.3, "square", release=0.2))], 0.4)
    out(g, "ui_buy", 1, coin, [SYN])
    x, s = clip("confirmation_003.ogg")
    out(g, "ui_buy", 2, x, [s])
    out(g, "ui_achievement", 1, arpeggio([NOTE(n) for n in (72, 76, 79, 84, 88)], 0.06, 0.5, bellish=True), [SYN])
    x, s = clip("jingles_NES03.ogg")
    out(g, "ui_achievement", 2, x, [s])
    from_pack(g, "ui_join", ["maximize_003.ogg", "open_002.ogg"])
    from_pack(g, "ui_ready", ["toggle_002.ogg", "switch_003.ogg", "confirmation_001.ogg"])


def julien_monkey():
    """Variantes extra de voz para Julien con el pack CC0 "Monkey Sounds" (antumdeluge)."""
    for k, (kind, pitch) in enumerate((("laugh", 1.15), ("cheer", 1.25), ("fall", 1.1)), 1):
        x, s = clip(f"monkey-{k}.wav", pitch=pitch)
        path = os.path.join(A.OUT, "Voices", "julien", f"voice_julien_{kind}_05.wav")
        A.save(path, A.highpass(x, 200))
        SOURCES[os.path.relpath(path, A.OUT).replace("\\", "/")] = [s]


if __name__ == "__main__":
    for fn in (abilities, raft, water, hazards, powerups_events, ui, julien_monkey):
        fn()
        print("ok", fn.__name__)
    sp = os.path.join(A.OUT, "sources.json")
    old = json.load(open(sp, encoding="utf-8")) if os.path.exists(sp) else {}
    old.update(SOURCES)
    json.dump(old, open(sp, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print("efectos:", len(SOURCES))
