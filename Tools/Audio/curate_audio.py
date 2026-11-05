"""GAS SAFARI · M2-02: control de calidad y selección automática de los sonidos (sin escucharlos).
python -I curate_audio.py --report   → mide todo y escribe Tools/Export/Audio/_curate_report.json (no toca archivos)
python -I curate_audio.py --apply    → recorta, normaliza por loudness, aplica crossfade a loops y borra lo descartado

Reglas (pedido del usuario, 2026-11-04):
  QC: sin clipping, silencios > 20 ms recortados, duración lógica por tipo, loudness parejo por ID.
  Variantes casi idénticas (similitud espectral muy alta): se queda una.
  Pedos: centroide espectral Gloria < Alex < Marty < Melman < Julien; se descartan las que se cruzan.
  Loops: el final debe empalmar con el inicio (crossfade 30-50 ms); si sigue el salto, se descarta.
  Música: _base siempre; _intensity solo si pasa la prueba de sincronía; mus_w3_boss siempre.
  Stingers: victory = contorno ascendente y final más brillante; fail = contorno descendente."""
import glob
import json
import os
import re
import sys
import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfilt, stft, correlate, lfilter

TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROOT = os.path.join(TOOLS, "Export", "Audio")
REPORT = os.path.join(ROOT, "_curate_report.json")
PEAK_DB = -1.0
CHAR_ORDER = ["gloria", "alex", "marty", "melman", "julien"]  # tono ascendente (docs/08)
SIL_THRESH = 10 ** (-50 / 20)   # -50 dBFS
SIL_MAX = 0.020                 # se recortan silencios mayores a 20 ms
SIL_KEEP = 0.004                # lo que se deja de margen
FADE = 0.005
XFADE = 0.040
DUP_SIM = 0.997                 # "casi idéntica": cae en el 1 % más parecido de los 745 pares de variantes medidos (mediana 0.926)


# ---------------------------------------------------------------- medidas
def load(path):
    x, sr = sf.read(path, dtype="float64", always_2d=True)
    return x.mean(axis=1), sr


def k_weight(x, sr):
    """Filtro K de ITU-R BS.1770 (shelving + pasa-altos), coeficientes a 48 kHz recalculados por bilineal."""
    def shelf(f0, G, Q, kind):
        K = np.tan(np.pi * f0 / sr)
        Vh = 10 ** (G / 20)
        Vb = Vh ** 0.4996667741545416
        a0 = 1 + K / Q + K * K
        if kind == "high":
            b = [(Vh + Vb * K / Q + K * K) / a0, 2 * (K * K - Vh) / a0, (Vh - Vb * K / Q + K * K) / a0]
        a = [1, 2 * (K * K - 1) / a0, (1 - K / Q + K * K) / a0]
        return b, a
    b1, a1 = shelf(1681.974450955533, 3.999843853973347, 0.7071752369554196, "high")
    f0, Q = 38.13547087602444, 0.5003270373238773
    K = np.tan(np.pi * f0 / sr)
    a2 = [1, 2 * (K * K - 1) / (1 + K / Q + K * K), (1 - K / Q + K * K) / (1 + K / Q + K * K)]
    return lfilter([1, -2, 1], a2, lfilter(b1, a1, x))


def lufs(x, sr):
    """LUFS integrado (BS.1770-4) con compuerta; en clips < 0.4 s, media sin compuerta."""
    y = k_weight(x, sr)
    blk, hop = int(0.4 * sr), int(0.1 * sr)
    if len(y) < blk:
        ms = np.mean(y ** 2)
        return -0.691 + 10 * np.log10(ms + 1e-12)
    ms = np.array([np.mean(y[i:i + blk] ** 2) for i in range(0, len(y) - blk + 1, hop)])
    l = -0.691 + 10 * np.log10(ms + 1e-12)
    ms1 = ms[l > -70]
    if len(ms1) == 0:
        return -70.0
    rel = -0.691 + 10 * np.log10(ms1.mean()) - 10
    ms2 = ms1[(-0.691 + 10 * np.log10(ms1 + 1e-12)) > rel]
    return -0.691 + 10 * np.log10(ms2.mean() + 1e-12)


def clipping(x):
    """Cuenta tramos de >= 4 muestras seguidas pegadas al máximo absoluto (pico plano) en archivos casi a 0 dBFS."""
    pk = np.max(np.abs(x))
    if pk < 0.985:
        return 0
    hit = np.abs(x) >= pk * 0.998
    runs = np.diff(np.concatenate([[0], hit.astype(int), [0]]))
    starts, ends = np.where(runs == 1)[0], np.where(runs == -1)[0]
    return int(np.sum((ends - starts) >= 4))


def trim_bounds(x, sr):
    """Índices [a, b) que recortan el silencio inicial/final si supera 20 ms (deja 4 ms de margen)."""
    loud = np.where(np.abs(x) > SIL_THRESH)[0]
    if len(loud) == 0:
        return 0, len(x)
    a, b = loud[0], loud[-1] + 1
    keep = int(SIL_KEEP * sr)
    a2 = max(0, a - keep) if a > SIL_MAX * sr else 0
    b2 = min(len(x), b + keep) if len(x) - b > SIL_MAX * sr else len(x)
    return a2, b2


def pstft(x, sr, nperseg=1024):
    """STFT con 75 % de solape; los clips más cortos que la ventana se rellenan con ceros."""
    if len(x) < nperseg:
        x = np.concatenate([x, np.zeros(nperseg - len(x))])
    return stft(x, sr, nperseg=nperseg, noverlap=nperseg * 3 // 4)


def centroid(x, sr):
    f, _, Z = pstft(x, sr)
    P = np.abs(Z) ** 2
    e = P.sum(axis=0)
    keep = e > e.max() * 1e-3
    if not keep.any():
        return 0.0
    c = (f[:, None] * P).sum(axis=0)[keep] / e[keep]
    return float(np.average(c, weights=e[keep]))


def fingerprint(x, sr):
    """Espectro medio en 48 bandas log (+ envolvente en 24 puntos), para comparar variantes."""
    f, _, Z = pstft(x, sr)
    P = np.abs(Z) ** 2
    edges = np.geomspace(40, 16000, 49)
    spec = np.array([P[(f >= edges[i]) & (f < edges[i + 1])].sum(axis=0).mean() if ((f >= edges[i]) & (f < edges[i + 1])).any() else 0 for i in range(48)])
    spec = np.log10(spec + 1e-10)
    spec -= spec.mean()
    e = P.sum(axis=0)
    env = np.interp(np.linspace(0, len(e) - 1, 24), np.arange(len(e)), np.log10(e + 1e-10))
    env -= env.mean()
    return spec, env


def similarity(fa, fb):
    sa, ea = fa
    sb, eb = fb
    cs = np.dot(sa, sb) / (np.linalg.norm(sa) * np.linalg.norm(sb) + 1e-12)
    ce = np.dot(ea, eb) / (np.linalg.norm(ea) * np.linalg.norm(eb) + 1e-12)
    return float(0.6 * cs + 0.4 * ce)


# ---------------------------------------------------------------- reglas por tipo
def is_loop(name):
    return name.endswith("_loop") or "_blast_" in name or "_charge_" in name or re.search(r"fart_(blast|charge)", name) is not None


DUR_RULES = [  # (regex del ID, min s, max s)
    (r"^fart_puff", 0.2, 0.6),
    (r"^fart_mega", 0.6, 1.5),
    (r"^fart_cough", 0.1, 1.0),
    (r"^fart_(blast|charge)", 0.5, 6.0),
    (r"^fart_(fire|bubble)", 0.3, 1.5),
    (r"^voice_.*_(effort|cheer)", 0.1, 1.0),
    (r"^voice_.*_(fall|laugh)", 0.2, 2.5),
    (r"^ui_", 0.01, 1.0),
    (r"^(beat_click|countdown_beep|ping_|crab_snip)", 0.05, 1.0),
    (r"_loop$", 0.25, 12.0),
    (r"^(ability_party)", 2.0, 6.0),
    (r"^(superfart_launch|finish_fanfare)", 0.5, 4.0),
    (r".*", 0.05, 3.0),
]


def dur_limits(name):
    for rx, lo, hi in DUR_RULES:
        if re.search(rx, name):
            return lo, hi


def id_of(path):
    return re.sub(r"_\d+$", "", os.path.splitext(os.path.basename(path))[0])


def char_of(path):
    m = re.search(r"fart_(?:puff|blast|charge|mega|cough)_(\w+?)_\d+\.wav$", path)
    return m.group(1) if m and m.group(1) in CHAR_ORDER else None


# ---------------------------------------------------------------- loops
def loop_seam(x, sr):
    """(salto de muestra / pendiente típica, diferencia de nivel RMS en dB entre 25 ms finales e iniciales)."""
    w = int(0.025 * sr)
    jump = abs(x[0] - x[-1]) / (np.percentile(np.abs(np.diff(x)), 95) + 1e-9)
    ra, rb = np.sqrt(np.mean(x[:w] ** 2)) + 1e-9, np.sqrt(np.mean(x[-w:] ** 2)) + 1e-9
    return float(jump), float(abs(20 * np.log10(ra / rb)))


def seam_ok(x, sr):
    j, d = loop_seam(x, sr)
    return j <= 3.0 and d <= 3.0


def crossfade_loop(x, sr, ms=XFADE):
    """Funde la cola con el inicio (potencia constante) y devuelve un loop de len(x)-N muestras."""
    n = int(ms * sr)
    n = min(n, len(x) // 4)
    t = np.linspace(0, np.pi / 2, n)
    head, tail = x[:n], x[-n:]
    out = x[:-n].copy()
    out[:n] = head * np.sin(t) + tail * np.cos(t)
    return out


# ---------------------------------------------------------------- música
def onset_env(x, sr, hop=441):
    f, t, Z = stft(x, sr, nperseg=1024, noverlap=1024 - hop)
    S = np.log1p(np.abs(Z) * 20)
    flux = np.maximum(0, np.diff(S, axis=1)).sum(axis=0)
    flux = np.concatenate([[0], flux])
    flux -= np.convolve(flux, np.ones(31) / 31, mode="same")
    return np.maximum(flux, 0), sr / hop


SYNC_LAG_MS, SYNC_MIN_CORR, SYNC_DRIFT_MS = 40, 0.10, 40


def sync_test(base, inten, sr, bpm, seg_s=12.0):
    """Correlación de onsets (flujo espectral, 100 fps) por tramos de 12 s. El desfase se busca solo hasta 0.45 pulsos
    (más allá, un tren periódico de golpes se confunde con el siguiente). Pasa si en TODOS los tramos
    |desfase| <= 40 ms y correlación >= 0.10, la deriva (máx-mín) <= 40 ms y ambas capas miden lo mismo (±20 ms)."""
    ob, fps = onset_env(base, sr)
    oi, _ = onset_env(inten, sr)
    n = min(len(ob), len(oi))
    ob, oi = ob[:n], oi[:n]
    seg = int(seg_s * fps)
    ml = int(min(0.25, 0.45 * 60.0 / bpm) * fps)
    lags, cors = [], []
    for a in range(0, n - seg + 1, seg):
        u, v = ob[a:a + seg], oi[a:a + seg]
        u = (u - u.mean()) / (u.std() + 1e-9)
        v = (v - v.mean()) / (v.std() + 1e-9)
        c = correlate(u, v, mode="full") / seg
        mid = len(c) // 2
        win = c[mid - ml:mid + ml + 1]
        k = int(np.argmax(win))
        lags.append((k - ml) / fps * 1000)
        cors.append(float(win[k]))
    if not lags:
        return dict(ok=False, motivo="pista demasiado corta")
    drift = max(lags) - min(lags)
    dd = abs(len(base) - len(inten)) / sr * 1000
    ok = max(abs(l) for l in lags) <= SYNC_LAG_MS and min(cors) >= SYNC_MIN_CORR and drift <= SYNC_DRIFT_MS and dd <= 20
    return dict(ok=bool(ok), bpm=bpm, tramos=len(lags), lag_max_ms=round(max(abs(l) for l in lags)), corr_min=round(min(cors), 2),
                corr_media=round(float(np.mean(cors)), 2), deriva_ms=round(drift), dif_duracion_ms=round(dd))


# ---------------------------------------------------------------- stingers
def contour(x, sr):
    """(pendiente de la frecuencia dominante en octavas/s, brillo del último 30 % menos el del resto en Hz)."""
    f, t, Z = stft(x, sr, nperseg=2048, noverlap=1536)
    P = np.abs(Z) ** 2
    band = (f > 150) & (f < 5000)
    e = P[band].sum(axis=0)
    ok = e > e.max() * 0.05
    dom = f[band][np.argmax(P[band], axis=0)]
    tt, dd = t[ok], np.log2(dom[ok])
    slope = float(np.polyfit(tt, dd, 1)[0]) if len(tt) > 3 else 0.0
    cen = (f[:, None] * P).sum(axis=0) / (P.sum(axis=0) + 1e-12)
    w = e
    k = int(len(t) * 0.7)
    late = np.average(cen[k:], weights=w[k:] + 1e-12)
    early = np.average(cen[:k], weights=w[:k] + 1e-12)
    return slope, float(late - early)


# ---------------------------------------------------------------- pipeline
def collect():
    files = sorted(glob.glob(os.path.join(ROOT, "SFX", "**", "*.wav"), recursive=True) +
                   glob.glob(os.path.join(ROOT, "Voices", "**", "*.wav"), recursive=True))
    info = {}
    for p in files:
        x, sr = load(p)
        a, b = trim_bounds(x, sr)
        y = x[a:b]
        name = id_of(p)
        lo, hi = dur_limits(name)
        d = len(y) / sr
        rec = dict(id=name, char=char_of(p), dur=round(d, 3), dur0=round(len(x) / sr, 3),
                   peak_db=round(20 * np.log10(np.max(np.abs(x)) + 1e-12), 2),
                   clip=clipping(x), trim_head_ms=round(a / sr * 1000), trim_tail_ms=round((len(x) - b) / sr * 1000),
                   lufs=round(lufs(y, sr), 2), centroid=round(centroid(y, sr)), loop=is_loop(name))
        fails = []
        if rec["clip"]:
            fails.append("clipping")
        if not (lo <= d <= hi):
            fails.append(f"duración {d:.2f}s fuera de {lo}-{hi}")
        if rec["lufs"] < -60:
            fails.append("silencio")
        rec["fails"] = fails
        rec["_fp"] = fingerprint(y, sr)
        info[p] = rec
    return info


def group_by(info, key):
    g = {}
    for p, r in info.items():
        g.setdefault(key(p, r), []).append(p)
    return g


MIN_CLIPS = {"puff": 6, "blast": 2, "charge": 1, "mega": 2, "cough": 2}  # mínimos de docs/08 §4


def tone_prune(info, drop):
    """Por tipo de pedo, deja los rangos [min, max] de centroide de los personajes disjuntos y en el orden
    Gloria < Alex < Marty < Melman < Julien: mientras dos vecinos se cruzan, cae la variante más alejada de la
    mediana de su personaje. (No se compara entre tipos: una tos es siempre más aguda que un toque.)"""
    for typ in MIN_CLIPS:
        by = {c: [p for p, r in info.items() if r["char"] == c and r["id"].startswith(f"fart_{typ}_") and p not in drop] for c in CHAR_ORDER}
        by = {c: v for c, v in by.items() if v}
        med = {c: float(np.median([info[p]["centroid"] for p in v])) for c, v in by.items()}
        order = [c for c in CHAR_ORDER if c in by]
        cen = lambda p: info[p]["centroid"]
        changed = True
        while changed:
            changed = False
            for lo_c, hi_c in zip(order, order[1:]):
                A, B = by[lo_c], by[hi_c]
                if not A or not B:
                    continue
                top, bot = max(A, key=cen), min(B, key=cen)
                if cen(top) < cen(bot):
                    continue
                dev_a = abs(np.log(cen(top) / med[lo_c]))
                dev_b = abs(np.log(cen(bot) / med[hi_c]))
                victim, group, c = (top, A, lo_c) if (dev_a >= dev_b and len(A) > 1) or len(B) == 1 else (bot, B, hi_c)
                if len(group) == 1:
                    continue  # no se vacía un personaje; el aviso sale en el informe
                group.remove(victim)
                other = hi_c if c == lo_c else lo_c
                drop[victim] = f"tono: centroide {cen(victim)} Hz cruza el rango de {other}"
                changed = True


def select(info):
    """Aplica descartes por QC → tono de pedos → casi duplicadas. Devuelve {path: motivo} de descartados."""
    drop = {}
    for p, r in info.items():
        if r["fails"]:
            drop[p] = "; ".join(r["fails"])
    tone_prune(info, drop)
    # duplicadas dentro del mismo ID
    for name, paths in group_by(info, lambda p, r: r["id"]).items():
        keep = []
        for p in paths:
            if p in drop:
                continue
            dup = next((q for q in keep if similarity(info[p]["_fp"], info[q]["_fp"]) >= DUP_SIM), None)
            if dup:
                drop[p] = f"casi idéntica a {os.path.basename(dup)} (sim {similarity(info[p]['_fp'], info[dup]['_fp']):.3f})"
            else:
                keep.append(p)
    return drop


def cmd_report():
    info = collect()
    drop = select(info)
    out = {os.path.relpath(p, ROOT).replace("\\", "/"): {k: v for k, v in r.items() if k != "_fp"} | {"drop": drop.get(p)} for p, r in info.items()}
    json.dump(out, open(REPORT, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print("archivos", len(info), "descartados", len(drop))
    by_id = {}
    for p, r in info.items():
        by_id.setdefault(r["id"], []).append(p in drop)
    for name, flags in sorted(by_id.items()):
        if any(flags):
            print(f"  {name}: {sum(flags)}/{len(flags)} descartadas")
    return info, drop


# ---------------------------------------------------------------- aplicar
def soft_limit(x, ceiling=10 ** (PEAK_DB / 20), knee=0.6):
    ax = np.abs(x)
    over = ax > knee
    y = x.copy()
    y[over] = np.sign(x[over]) * (knee + (ceiling - knee) * np.tanh((ax[over] - knee) / (ceiling - knee)))
    return y


def process(p):
    """Recorta, aplica fades (o crossfade en loops) y devuelve (audio, sr, nota, ok). ok solo cuenta en loops."""
    x, sr = load(p)
    a, b = trim_bounds(x, sr)
    y = x[a:b]
    name = id_of(p)
    if not is_loop(name):
        f = int(FADE * sr)
        y = y.copy()
        y[:f] *= np.linspace(0, 1, f)
        y[-f:] *= np.linspace(1, 0, f)
        return y, sr, None, None
    j0, d0 = loop_seam(y, sr)
    if seam_ok(y, sr):
        return y, sr, f"empalme ok (salto {j0:.1f}, nivel {d0:.1f} dB)", True
    z = crossfade_loop(y, sr)
    j1, d1 = loop_seam(z, sr)
    return z, sr, f"crossfade {int(XFADE * 1000)} ms (salto {j0:.1f}->{j1:.1f}, nivel {d0:.1f}->{d1:.1f} dB)", seam_ok(z, sr)


def rel(p):
    return os.path.relpath(p, ROOT).replace("\\", "/")


def cmd_apply():
    info = collect()
    drop = select(info)
    audio, notes = {}, {}
    for p in info:
        if p in drop:
            continue
        y, sr, note, ok = process(p)
        if ok is False:
            drop[p] = f"loop con salto tras el crossfade ({note})"
            continue
        audio[p] = (y, sr)
        if note:
            notes[p] = note
    # loudness por ID: todas las variantes al LUFS mediano del grupo
    final = {}
    for name, paths in group_by(info, lambda p, r: r["id"]).items():
        keep = [p for p in paths if p in audio]
        if not keep:
            continue
        ls = {p: lufs(*audio[p]) for p in keep}
        target = float(np.median(list(ls.values())))
        for p in keep:
            y, sr = audio[p]
            y = soft_limit(y * 10 ** ((target - ls[p]) / 20))
            audio[p] = (y, sr)
            final[p] = dict(lufs_antes=round(ls[p], 1), gain_db=round(target - ls[p], 1), lufs=round(lufs(y, sr), 1),
                            pico_db=round(20 * np.log10(np.max(np.abs(y))), 2))
    for p, (y, sr) in audio.items():
        sf.write(p, y, sr, subtype="PCM_16")
    for p in drop:
        os.remove(p)
    sources = os.path.join(ROOT, "sources.json")
    src = json.load(open(sources, encoding="utf-8"))
    for p in drop:
        src.pop(rel(p), None)
    json.dump(src, open(sources, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    music = music_pass()
    write_elegir(info, drop, notes, final, music)
    res = {rel(p): {k: v for k, v in r.items() if k != "_fp"} | {"drop": drop.get(p), "final": final.get(p), "loop": notes.get(p)} for p, r in info.items()}
    json.dump(dict(archivos=res, musica=music), open(REPORT, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print("conservados", len(audio), "descartados", len(drop))


def music_pass():
    M = os.path.join(ROOT, "Music")
    rep = json.load(open(os.path.join(M, "_music_report.json"), encoding="utf-8"))
    out = {"capas": {}, "stingers": {}}
    for base in sorted(glob.glob(os.path.join(M, "mus_*_base.ogg"))):
        key = os.path.basename(base)[:-9]
        inten = base.replace("_base", "_intensity")
        if not os.path.exists(inten):
            continue
        bpm = rep[key]["bpm"] if key in rep else 120.0
        if isinstance(bpm, str):  # mus_w3_boss: 110->128
            bpm = float(bpm.split("→")[-1])
        xb, sr = load(base)
        xi, _ = load(inten)
        r = sync_test(xb, xi, sr, bpm)
        r["siempre"] = key == "mus_w3_boss"
        r["queda"] = bool(r["ok"] or r["siempre"])
        if not r["queda"]:
            os.remove(inten)
        out["capas"][key] = r
    for kind in ("victory", "fail"):
        files = sorted(glob.glob(os.path.join(M, f"stinger_{kind}_*.ogg")))
        if len(files) <= 1:
            continue
        m = {}
        for f in files:
            x, sr = load(f)
            slope, bright = contour(x, sr)
            m[f] = dict(dur=len(x) / sr, slope=slope, bright=bright)
        sg = 1 if kind == "victory" else -1
        r_slope = {f: i for i, f in enumerate(sorted(files, key=lambda f: -sg * m[f]["slope"]))}
        r_bright = {f: i for i, f in enumerate(sorted(files, key=lambda f: -sg * m[f]["bright"]))}
        # victory: pendiente ascendente + final más brillante; fail: solo contorno descendente
        score = {f: r_slope[f] + (r_bright[f] if kind == "victory" else 0) for f in files}
        best = min(files, key=lambda f: (score[f], round(m[f]["dur"], 2)))  # empate: el más corto
        out["stingers"][kind] = dict(elegido=os.path.basename(best), candidatos={os.path.basename(f): dict(
            dur=round(m[f]["dur"], 2), pendiente_oct_s=round(m[f]["slope"], 2), brillo_final_hz=round(m[f]["bright"])) for f in files})
        for f in files:
            if f != best:
                os.remove(f)
    return out


def write_elegir(info, drop, notes, final, music):
    L = ["# ELEGIR · selección automática de sonidos (M2-02)", "",
         "Nadie escuchó estos archivos: la selección salió del análisis de `Tools/Audio/curate_audio.py` con las reglas del pedido del 2026-11-04. "
         "Lo que sigue en `Tools/Export/Audio/` es lo aprobado (ya recortado y normalizado); lo descartado se borró (queda en el historial de Git).", "",
         "## Reglas aplicadas", "",
         "- **Calidad**: sin clipping (tramos planos de pico); silencios de más de 20 ms al inicio/final recortados (margen de 4 ms); fades de 5 ms; WAV 44.1 kHz 16 bits.",
         "- **Duración por tipo**: toques 0.2-0.6 s, megapedos 0.6-1.5 s, toses 0.1-1.0 s, voces de esfuerzo/ánimo 0.1-1.0 s, caída/risa 0.2-2.5 s, UI 0.01-1.0 s, loops de pedo 0.5-6 s, loops de ambiente 0.25-12 s, resto 0.05-3 s.",
         "- **Volumen**: cada ID se lleva al LUFS (BS.1770, con compuerta) mediano de sus variantes; si el pico pasa de -1 dBFS, limitador suave.",
         f"- **Casi idénticas**: similitud coseno >= {DUP_SIM} (espectro medio en 48 bandas + envolvente); es el 1 % más parecido de los pares medidos (mediana 0.926). Se queda la de número menor.",
         "- **Tono de pedos**: por tipo (toque, ráfaga, carga, mega, tos) los rangos de centroide espectral quedan disjuntos y en el orden Gloria < Alex < Marty < Melman < Julien; mientras dos vecinos se cruzan cae la variante más alejada de la mediana de su personaje. No se compara entre tipos (una tos es más aguda que un toque).",
         "- **Loops** (ráfaga, carga, `*_loop`): salto de muestra <= 3 pendientes típicas y diferencia de nivel <= 3 dB entre 25 ms finales e iniciales; si no, crossfade de 40 ms; si aun así falla, se descarta.",
         "- **Música**: `_base` siempre; `_intensity` solo si pasa la prueba de sincronía (abajo); `mus_w3_boss` siempre.",
         "- **Voces**: las de Windows (proyecto no comercial); solo pasan el control de calidad.", "",
         "## Cambio de partida: megapedos", "",
         "Los 20 megapedos originales duraban 2.1-3.1 s y todos fallaban 0.6-1.5 s. Los regeneré con `python fart_synth.py --mega-only` (núcleo de 0.5-0.85 s + cola corta, generador propio) y pasaron el mismo control. Los demás archivos no cambiaron.", ""]
    by = group_by(info, lambda p, r: r["id"])
    cats = {}
    for name, paths in by.items():
        cat = rel(os.path.dirname(paths[0]))
        if cat.startswith("SFX/Farts/"):
            cat = "SFX/Farts (por personaje)"
        cats.setdefault(cat, []).append(name)
    num = lambda p: os.path.basename(p)[:-4].rsplit("_", 1)[1]
    for cat in sorted(cats, key=lambda c: (c.startswith("Voices"), c)):
        L += [f"## `{cat}`", ""]
        for name in sorted(cats[cat]):
            paths = sorted(by[name])
            kept = [p for p in paths if p not in drop]
            gone = [f"{num(p)} ({drop[p].split(':')[0].split(' (')[0].split(' fuera')[0]})" for p in paths if p in drop]
            txt = f"- `{name}`: quedan {' '.join(num(p) for p in kept) or 'ninguna'} de {len(paths)}"
            if gone:
                txt += "; descartadas " + ", ".join(gone)
            if kept and kept[0] in final:
                txt += f"; {final[kept[0]]['lufs']} LUFS"
            lp = [notes[p] for p in kept if p in notes]
            if lp:
                nx = sum("crossfade" in n for n in lp)
                txt += f"; loop: crossfade en {nx} de {len(lp)}" if nx else "; loop: empalme ok sin tocar"
            L.append(txt)
        L.append("")
    L += ["## Tono de pedos (centroide espectral en Hz, mín-máx de las variantes que quedan)", "",
          "| Tipo | Gloria | Alex | Marty | Melman | Julien |", "|---|---|---|---|---|---|"]
    for typ in MIN_CLIPS:
        row = [typ]
        for c in CHAR_ORDER:
            v = [r["centroid"] for p, r in info.items() if r["char"] == c and r["id"].startswith(f"fart_{typ}_") and p not in drop]
            row.append(f"{min(v)}-{max(v)} (n={len(v)})" if v else "—")
        L.append("| " + " | ".join(row) + " |")
    L += ["", "## Música · `Music/`", "",
          "Prueba de sincronía: onsets (flujo espectral a 100 fps) de la intensidad contra la base, por tramos de 12 s. Pasa si en todos los tramos |desfase| <= 40 ms y correlación >= 0.10, deriva <= 40 ms y misma duración (±20 ms).", "",
          "| Pista | Capa de intensidad | Desfase máx. | Correlación mín. | Deriva |", "|---|---|---|---|---|"]
    for key, r in music["capas"].items():
        estado = "queda" if r["ok"] else ("queda (siempre)" if r["siempre"] else "descartada")
        L.append(f"| `{key}` | {estado} | {r.get('lag_max_ms', '—')} ms | {r.get('corr_min', '—')} | {r.get('deriva_ms', '—')} ms |")
    L += ["", "Pistas sin capa de intensidad: usar solo la `_base` (`MusicDirector` debe tolerar la capa ausente). `mus_title`, `mus_select`, `mus_map`, `mus_tutorial`, `mus_party` y `mus_results` no tienen capas.", "",
          "### Stingers", ""]
    for kind, r in music["stingers"].items():
        L.append(f"- `stinger_{kind}`: queda `{r['elegido']}`. " + "; ".join(
            f"{k}: {v['dur']} s, pendiente {v['pendiente_oct_s']} oct/s, brillo final {v['brillo_final_hz']:+d} Hz" for k, v in r["candidatos"].items()))
    L += ["", "Nota: docs/08 §5 pide stingers de 2-4 s; los de Kenney duran 1-1.4 s. Quedan así hasta que se decida alargarlos.", ""]
    open(os.path.join(ROOT, "ELEGIR.md"), "w", encoding="utf-8").write("\n".join(L))


if __name__ == "__main__":
    if "--apply" in sys.argv:
        cmd_apply()
    elif "--report" in sys.argv:
        cmd_report()
