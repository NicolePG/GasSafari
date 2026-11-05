"""GAS SAFARI · utilidades de audio: filtros, envolventes, loops sin cortes y exportación
(WAV 44.1 kHz 16 bits, pico a -1 dB, fades de 5 ms, silencios recortados; docs/08 §7)."""
import os
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfilt, resample_poly

SR = 44100
TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(TOOLS, "Export", "Audio")
PEAK = 10 ** (-1 / 20)


def n_of(dur):
    return max(1, int(round(dur * SR)))


def t_axis(n):
    return np.arange(n) / SR


def smooth_noise(n, rate_hz, rng):
    """Ruido suave (interpolado) con ~rate_hz cambios por segundo, desviación ~1."""
    k = max(3, int(n * rate_hz / SR) + 3)
    pts = rng.standard_normal(k)
    return np.interp(np.linspace(0, k - 1, n), np.arange(k), pts)


def periodic_noise(n, cycles, rng, harmonics=4):
    """Ruido suave periódico (para loops): suma de senos con número entero de ciclos en n muestras."""
    t = np.arange(n) / n
    x = np.zeros(n)
    for h in range(1, harmonics + 1):
        x += rng.standard_normal() / h * np.sin(2 * np.pi * cycles * h * t + rng.uniform(0, 2 * np.pi))
    return x / (np.std(x) + 1e-9)


def _sos(kind, f, order=2):
    nyq = SR / 2 * 0.98
    if isinstance(f, (list, tuple)):
        f = [max(10, min(v, nyq)) for v in f]
    else:
        f = max(10, min(f, nyq))
    return butter(order, f, kind, fs=SR, output="sos")


def lowpass(x, f, order=2):
    return sosfilt(_sos("lowpass", f, order), x)


def highpass(x, f, order=2):
    return sosfilt(_sos("highpass", f, order), x)


def bandpass(x, lo, hi, order=2):
    return sosfilt(_sos("bandpass", [lo, hi], order), x)


def sweep_lowpass(x, f_start, f_end, blocks=64):
    """Pasa-bajos con corte variable por bloques (whooshes, llamas)."""
    out = np.zeros_like(x)
    edges = np.linspace(0, len(x), blocks + 1).astype(int)
    fs = np.geomspace(f_start, f_end, blocks)
    zi = None
    for i in range(blocks):
        a, b = edges[i], edges[i + 1]
        sos = _sos("lowpass", fs[i], 2)
        seg = x[max(0, a - 512):b]
        out[a:b] = sosfilt(sos, seg)[-(b - a):] if b > a else []
    return out


def env(n, attack=0.01, release=0.05, curve=1.0):
    e = np.ones(n)
    a, r = min(n, n_of(attack)), min(n, n_of(release))
    if a:
        e[:a] = np.linspace(0, 1, a) ** curve
    if r:
        e[-r:] *= np.linspace(1, 0, r) ** curve
    return e


def exp_decay(n, tau):
    return np.exp(-t_axis(n) / tau)


def fade(x, ms=5.0):
    k = min(len(x) // 2, n_of(ms / 1000))
    if k > 0:
        x = x.copy()
        x[:k] *= np.linspace(0, 1, k)
        x[-k:] *= np.linspace(1, 0, k)
    return x


def trim(x, thresh_db=-48, pad_ms=4):
    a = np.abs(x)
    th = a.max() * 10 ** (thresh_db / 20) if a.max() > 0 else 0
    idx = np.where(a > th)[0]
    if len(idx) == 0:
        return x
    p = n_of(pad_ms / 1000)
    return x[max(0, idx[0] - p):min(len(x), idx[-1] + p)]


def normalize(x, peak=PEAK):
    m = np.max(np.abs(x))
    return x * (peak / m) if m > 0 else x


def remove_dc(x):
    return highpass(x, 25, 2)


def loopify(x, xfade):
    """Loop sin cortes: mezcla el final sobre el principio (potencia constante) y devuelve len(x)-xfade muestras."""
    k = min(n_of(xfade), len(x) // 3)
    body = x[:-k].copy()
    w = np.linspace(0, np.pi / 2, k)
    body[:k] = x[:k] * np.sin(w) + x[-k:] * np.cos(w)
    return body


def loop_seam(x):
    """Diferencia en la unión del loop (último → primero) relativa al paso típico entre muestras."""
    d = np.abs(np.diff(x))
    return abs(x[0] - x[-1]) / (np.percentile(d, 95) + 1e-9)


def save(path, x, loop=False, do_trim=True):
    """Guarda WAV mono 44.1 kHz 16 bits normalizado a -1 dB. Los loops no se recortan ni llevan fade."""
    x = np.asarray(x, dtype=np.float64)
    if not loop:
        x = remove_dc(x)
        if do_trim:
            x = trim(x)
        x = fade(x, 5)
    x = normalize(x)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sf.write(path, x.astype(np.float32), SR, subtype="PCM_16")
    return path


def load(path, mono=True):
    x, sr = sf.read(path, always_2d=True)
    if mono:
        x = x.mean(axis=1)
    if sr != SR:
        from math import gcd
        g = gcd(int(sr), SR)
        x = resample_poly(x, SR // g, int(sr) // g, axis=0)
    return x


def pitch_speed(x, factor):
    """Cambia tono y velocidad a la vez (factor > 1 = más agudo y rápido), estilo cinta."""
    from fractions import Fraction
    fr = Fraction(factor).limit_denominator(200)
    return resample_poly(x, fr.denominator, fr.numerator)


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def place(dst, src, at):
    i = n_of(at)
    j = min(len(dst), i + len(src))
    dst[i:j] += src[:j - i]
    return dst
