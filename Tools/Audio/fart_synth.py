"""Pedos sintetizados por personaje (docs/08 §4, el doble de variantes para elegir).
Por personaje: 12 toques, 4 ráfagas en loop, 2 cargas en loop, 4 megapedos, 4 toses.
Globales: 6 fart_fire (capa de locoto) y 6 fart_bubble (capa de gaseosa).
python fart_synth.py  →  Tools/Export/Audio/SFX/Farts/   (--mega-only: solo los megapedos)

Modelo: tren de pulsos (vibración de "labios") con f0 variable, jitter, temblor (flaps),
ruido de aire, resonancia del cuerpo, saturación suave y envolvente."""
import os
import sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gs_audio as A

OUT = os.path.join(A.OUT, "SFX", "Farts")

# Timbre por personaje: Gloria muy grave, Alex grave, Marty rápido y medio,
# Melman agudo y tembloroso, Julien muy agudo.
CHARS = {
    "gloria": dict(f0=52, sharp=2.6, flut=9, flut_d=0.35, wet=0.38, bright=1100, res_f=170, res_g=1.6, drive=2.6,
                   vib=0.03, vib_rate=3.5, jit=0.10, shim=0.35, tempo=1.25),
    "alex": dict(f0=82, sharp=3.2, flut=14, flut_d=0.3, wet=0.26, bright=1700, res_f=280, res_g=1.3, drive=2.2,
                 vib=0.03, vib_rate=4.0, jit=0.08, shim=0.3, tempo=1.0),
    "marty": dict(f0=122, sharp=4.0, flut=27, flut_d=0.5, wet=0.2, bright=2400, res_f=430, res_g=1.1, drive=2.0,
                  vib=0.04, vib_rate=6.0, jit=0.09, shim=0.3, tempo=0.72),
    "melman": dict(f0=200, sharp=5.0, flut=17, flut_d=0.35, wet=0.15, bright=3200, res_f=620, res_g=1.0, drive=1.8,
                   vib=0.085, vib_rate=7.5, jit=0.07, shim=0.45, tempo=1.05, tremolo=0.45),
    "julien": dict(f0=330, sharp=6.0, flut=22, flut_d=0.3, wet=0.1, bright=4800, res_f=950, res_g=0.9, drive=1.7,
                   vib=0.05, vib_rate=9.0, jit=0.06, shim=0.3, tempo=0.85),
}


def core(n, f0, p, rng, periodic=None):
    """Señal base del pedo. f0: array (Hz) de largo n. periodic=ciclos para que las modulaciones cierren el loop."""
    t = A.t_axis(n)
    noise_fn = (lambda rate: A.periodic_noise(n, max(1, int(rate * n / A.SR / 4)), rng)) if periodic \
        else (lambda rate: A.smooth_noise(n, rate, rng))
    vib_rate = round(p["vib_rate"] * n / A.SR) * A.SR / n if periodic else p["vib_rate"]
    f = f0 * (1 + p["vib"] * np.sin(2 * np.pi * vib_rate * t + rng.uniform(0, 6.28)))
    f = f * np.exp(p["jit"] * noise_fn(40))
    phase = np.cumsum(2 * np.pi * f / A.SR) + rng.uniform(0, 6.28)
    pulse = ((1 + np.sin(phase)) / 2) ** p["sharp"]
    src = pulse * (1 + p["shim"] * noise_fn(p["f0"] * 0.6))
    flut_rate = round(p["flut"] * n / A.SR) * A.SR / n if periodic else p["flut"]
    fl = 0.5 + 0.5 * np.sin(2 * np.pi * flut_rate * t + 0.8 * noise_fn(3))
    src = src * (1 - p["flut_d"] * fl)
    if p.get("tremolo"):
        tr_rate = round(5.5 * n / A.SR) * A.SR / n if periodic else 5.5
        src = src * (1 - p["tremolo"] * (0.5 + 0.5 * np.sin(2 * np.pi * tr_rate * t)))
    src = src - A.lowpass(src, 20)
    air = A.bandpass(rng.standard_normal(n), 250, 3500) * (0.35 + 0.65 * pulse)
    x = src + p["wet"] * air * np.std(src) / (np.std(air) + 1e-9) * 0.55
    x = A.lowpass(x, p["bright"], 2)
    x = x + p["res_g"] * A.bandpass(x, p["res_f"] * 0.75, p["res_f"] * 1.3)
    x = x / (np.max(np.abs(x)) + 1e-9)
    x = np.tanh(p["drive"] * x) / np.tanh(p["drive"])
    return A.lowpass(x, p["bright"] * 1.6, 2)


def air_tail(dur, p, rng, level=0.25):
    n = A.n_of(dur)
    h = A.bandpass(rng.standard_normal(n), 400, min(6000, p["bright"] * 2.5)) * A.exp_decay(n, dur / 3)
    return level * h / (np.max(np.abs(h)) + 1e-9)


def contour(n, kind, f0, rng):
    u = np.linspace(0, 1, n)
    if kind == "fall":
        return f0 * (1.25 - 0.45 * u)
    if kind == "rise":
        return f0 * (0.85 + 0.45 * u ** 1.5)
    if kind == "dip":
        return f0 * (1.1 - 0.35 * np.sin(np.pi * u))
    if kind == "squeak":
        return f0 * (1.6 + 0.5 * u)
    return f0 * (1.0 + 0.06 * rng.standard_normal() + 0.04 * np.sin(np.pi * u))


def puff(p, rng, i):
    kinds = ["fall", "flat", "rise", "dip", "fall", "squeak", "flat", "fall", "double", "dip", "rise", "double"]
    kind = kinds[i % len(kinds)]
    dur = rng.uniform(0.2, 0.36) * p["tempo"]
    f0 = p["f0"] * rng.uniform(0.85, 1.2)
    if kind == "double":
        a = puff_seg(dur * 0.45, f0 * 1.1, "fall", p, rng)
        b = puff_seg(dur * 0.55, f0 * 0.95, "fall", p, rng)
        x = np.concatenate([a, np.zeros(A.n_of(0.035 * p["tempo"])), b])
    else:
        x = puff_seg(dur, f0, kind, p, rng)
    return np.concatenate([x, np.zeros(A.n_of(0.08))]) + np.pad(air_tail(0.09, p, rng, 0.12),
                                                                 (len(x) - A.n_of(0.01), 0))[:len(x) + A.n_of(0.08)]


def puff_seg(dur, f0, kind, p, rng):
    n = A.n_of(dur)
    x = core(n, contour(n, kind, f0, rng), p, rng)
    e = A.env(n, attack=rng.uniform(0.004, 0.012), release=dur * 0.45, curve=1.6)
    e *= 1 - 0.35 * np.linspace(0, 1, n)
    return x * e


def blast_loop(p, rng, i):
    L, xf = 2.0, 0.25
    n = A.n_of(L + xf)
    t = A.t_axis(n)
    wob = [0.06, 0.1, 0.04, 0.14][i % 4]
    f0 = p["f0"] * rng.uniform(0.9, 1.1) * (1 + wob * np.sin(2 * np.pi * 1.0 / (L) * t + rng.uniform(0, 6.28)))
    q = dict(p)
    q["flut_d"] = min(0.7, p["flut_d"] * [1.0, 1.4, 0.7, 1.2][i % 4])
    q["wet"] = p["wet"] * [1.0, 0.8, 1.6, 1.1][i % 4]
    x = core(n, f0, q, rng)
    x *= 1 + 0.12 * np.sin(2 * np.pi * 2 / L * t)
    return A.loopify(x, xf)


def charge_loop(p, rng, i):
    """Aguantando: chillido apretado intermitente + rugido de panza."""
    L, xf = 1.6, 0.2
    n = A.n_of(L + xf)
    t = A.t_axis(n)
    q = dict(p, sharp=p["sharp"] * 2, flut_d=0.2, wet=0.05, bright=p["bright"] * 1.5, drive=1.3)
    sq = core(n, p["f0"] * (2.2 + 0.3 * i) * (1 + 0.05 * np.sin(2 * np.pi * 3 * t)), q, rng)
    gate = 0.5 + 0.5 * np.sin(2 * np.pi * (4 + 2 * i) * t + 0.6 * A.smooth_noise(n, 3, rng))
    sq *= 0.35 * gate ** 2
    rumble = A.lowpass(rng.standard_normal(n), 120 + 0.6 * p["f0"], 4)
    rumble = rumble / (np.max(np.abs(rumble)) + 1e-9) * (0.6 + 0.4 * np.sin(2 * np.pi * 1.5 * t))
    gurg = np.zeros(n)
    for _ in range(6 + 3 * i):
        at = rng.uniform(0, L)
        bn = A.n_of(0.05)
        bt = A.t_axis(bn)
        fb = rng.uniform(150, 400) * (p["f0"] / 100) ** 0.4
        b = np.sin(2 * np.pi * fb * bt * (1 + 3 * bt)) * A.exp_decay(bn, 0.015)
        A.place(gurg, 0.4 * b, at)
    return A.loopify(sq + 0.6 * rumble + gurg, xf)


def mega(p, rng, i):
    dur = rng.uniform(0.5, 0.85) * (0.85 + 0.15 * p["tempo"])  # total 0.8-1.5 s con la cola (M2-02: antes 2-3 s)
    n = A.n_of(dur)
    u = np.linspace(0, 1, n)
    f0 = p["f0"] * (1.35 - 0.7 * u ** 0.8) * rng.uniform(0.9, 1.1)
    q = dict(p, drive=p["drive"] * 1.5, flut_d=min(0.75, p["flut_d"] * 1.3), wet=p["wet"] * 1.3)
    q["flut"] = p["flut"] * 0.8
    x = core(n, f0, q, rng)
    e = A.env(n, attack=0.008, release=dur * 0.25) * (1.0 - 0.3 * u)
    e *= 1 + 0.25 * np.exp(-u * 12)
    x = x * e
    tail = []
    for k in range(1 + i % 3):
        sd = rng.uniform(0.05, 0.12)
        tail.append(np.zeros(A.n_of(rng.uniform(0.03, 0.08))))
        tail.append(puff_seg(sd, p["f0"] * rng.uniform(0.9, 1.4), "fall", p, rng) * (0.6 - 0.12 * k))
    return np.concatenate([x] + tail + [air_tail(0.15, p, rng, 0.2)])


def cough(p, rng, i):
    """Sin gas: chisporroteos secos y aire."""
    parts = []
    for k in range(2 + i % 3):
        sd = rng.uniform(0.03, 0.07)
        q = dict(p, wet=p["wet"] * 2.5, flut_d=0.6)
        parts.append(puff_seg(sd, p["f0"] * rng.uniform(1.3, 2.0), "fall", q, rng) * rng.uniform(0.5, 1.0))
        parts.append(np.zeros(A.n_of(rng.uniform(0.04, 0.12) * p["tempo"])))
    parts.append(air_tail(rng.uniform(0.15, 0.3), p, rng, 0.35))
    return np.concatenate(parts)


def fire(rng, i):
    """Capa de fuego (locoto): llamarada con chasquidos."""
    dur = rng.uniform(0.7, 1.2)
    n = A.n_of(dur)
    u = np.linspace(0, 1, n)
    roar = A.sweep_lowpass(rng.standard_normal(n), 400, rng.uniform(2500, 5000)) * np.sin(np.pi * u ** 0.5)
    roar = roar / (np.max(np.abs(roar)) + 1e-9)
    rumble = A.lowpass(rng.standard_normal(n), 140, 4)
    rumble = rumble / (np.max(np.abs(rumble)) + 1e-9) * np.sin(np.pi * u)
    crack = np.zeros(n)
    for _ in range(int(rng.uniform(15, 35))):
        k = int(rng.uniform(0, n - 300))
        crack[k:k + 200] += rng.standard_normal(200) * np.exp(-np.arange(200) / 30) * rng.uniform(0.3, 1)
    crack = A.highpass(crack, 2000)
    whoosh = 0.8 * roar + 0.6 * rumble + 0.5 * crack / (np.max(np.abs(crack)) + 1e-9)
    return whoosh * A.env(n, 0.02, dur * 0.4)


def bubble(rng, i):
    """Capa de burbujas (gaseosa)."""
    dur = rng.uniform(0.5, 0.9)
    x = np.zeros(A.n_of(dur + 0.1))
    for _ in range(int(rng.uniform(7, 15))):
        bd = rng.uniform(0.03, 0.08)
        bn = A.n_of(bd)
        bt = A.t_axis(bn)
        f = rng.uniform(350, 1400) * (1 + rng.uniform(2, 6) * bt)
        b = np.sin(2 * np.pi * np.cumsum(f) / A.SR) * A.exp_decay(bn, bd / 3) * A.env(bn, 0.002, 0.005)
        A.place(x, b * rng.uniform(0.4, 1.0), rng.uniform(0, dur))
    fizz = A.highpass(rng.standard_normal(len(x)), 4000) * 0.04 * A.env(len(x), 0.05, 0.2)
    return x + fizz


MEGA_ONLY = "--mega-only" in sys.argv  # regenera solo los megapedos (el resto no cambia)


def main():
    report = []
    for ci, (pj, p) in enumerate(CHARS.items()):
        d = os.path.join(OUT, pj)
        rng = np.random.default_rng(1000 + ci)
        rng_m = np.random.default_rng(3000 + ci)  # los megapedos tienen su propio generador
        if MEGA_ONLY:
            for i in range(4):
                A.save(os.path.join(d, f"fart_mega_{pj}_{i + 1:02d}.wav"), mega(p, rng_m, i))
            continue
        for i in range(12):
            A.save(os.path.join(d, f"fart_puff_{pj}_{i + 1:02d}.wav"), puff(p, rng, i))
        for i in range(4):
            x = blast_loop(p, rng, i)
            report.append((f"fart_blast_{pj}_{i + 1:02d}", A.loop_seam(x)))
            A.save(os.path.join(d, f"fart_blast_{pj}_{i + 1:02d}.wav"), x, loop=True)
        for i in range(2):
            x = charge_loop(p, rng, i)
            report.append((f"fart_charge_{pj}_{i + 1:02d}", A.loop_seam(x)))
            A.save(os.path.join(d, f"fart_charge_{pj}_{i + 1:02d}.wav"), x, loop=True)
        for i in range(4):
            A.save(os.path.join(d, f"fart_mega_{pj}_{i + 1:02d}.wav"), mega(p, rng_m, i))
        for i in range(4):
            A.save(os.path.join(d, f"fart_cough_{pj}_{i + 1:02d}.wav"), cough(p, rng, i))
        print("ok", pj)
    rng = np.random.default_rng(77)
    for i in range(6):
        A.save(os.path.join(OUT, f"fart_fire_{i + 1:02d}.wav"), fire(rng, i))
        A.save(os.path.join(OUT, f"fart_bubble_{i + 1:02d}.wav"), bubble(rng, i))
    for name, s in report:
        if s > 1.5:
            print("AVISO costura de loop", name, round(s, 2))
    print("loops revisados:", len(report))


if __name__ == "__main__":
    main()
