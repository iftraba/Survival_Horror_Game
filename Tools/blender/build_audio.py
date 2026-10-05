"""Sintetiza el audio del juego (efectos y musica) con numpy y lo guarda como WAV.

Se ejecuta con el Python de Blender (trae numpy):
    exec(open(r"<ruta>/build_audio.py").read())
    build_sfx(r"<carpeta de salida>")
    build_music(r"<carpeta de salida>")

Todo es sintesis procedural: sin muestras externas, sin licencias.
"""
import math
import os
import wave

import numpy as np

rng = np.random.default_rng(20261004)
TAU = 2.0 * math.pi


# ------------------------------------------------------------------ utilidades
def tgrid(d, sr):
    return np.arange(int(d * sr), dtype=np.float64) / sr


def noise(n):
    return rng.standard_normal(n)


def shape_filter(x, sr, gain_fn):
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1.0 / sr)
    return np.fft.irfft(X * gain_fn(f), len(x))


def lowpass(x, sr, fc, order=2):
    return shape_filter(x, sr, lambda f: 1.0 / np.sqrt(1.0 + (f / fc) ** (2 * order)))


def highpass(x, sr, fc, order=2):
    return shape_filter(x, sr, lambda f: (f / fc) ** order / np.sqrt(1.0 + (f / fc) ** (2 * order)))


def bandpass(x, sr, lo, hi):
    return highpass(lowpass(x, sr, hi), sr, lo)


def decay(n, sr, tau, attack=0.002):
    t = np.arange(n) / sr
    return np.exp(-t / tau) * np.minimum(1.0, t / max(attack, 1e-5))


def place(buf, sig, at, sr, gain=1.0):
    """Suma 'sig' dentro de 'buf' a partir del segundo 'at' (se recorta si se sale)."""
    i = int(at * sr)
    if i >= len(buf):
        return
    j = min(len(buf), i + len(sig))
    buf[i:j] += sig[: j - i] * gain


def reverb(x, sr, rt60, wet=0.3, circular=False, damp=4500):
    n = len(x)
    L = int(rt60 * sr)
    ir = noise(L) * np.exp(-6.9 * np.arange(L) / (rt60 * sr))
    ir = lowpass(ir, sr, damp)
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    if circular:   # sin cola: la reverberacion se envuelve, ideal para bucles
        irp = np.zeros(n)
        m = min(L, n)
        irp[:m] = ir[:m]
        y = np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(irp), n)
        return (1.0 - wet) * x + wet * y
    total = n + L
    size = 1 << (total - 1).bit_length()
    y = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)[:total]
    return (1.0 - wet) * np.pad(x, (0, L)) + wet * y


def save(out_dir, name, x, sr, peak=0.9, fade=0.004):
    x = np.asarray(x, dtype=np.float64)
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    if fade > 0:
        f = max(1, int(fade * sr))
        x[:f] *= np.linspace(0, 1, f)
        x[-f:] *= np.linspace(1, 0, f)
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    os.makedirs(out_dir, exist_ok=True)
    with wave.open(os.path.join(out_dir, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data.tobytes())
    return len(x) / sr


def voice(d, sr, f0_fn, formants, rasp=2.5, breath=0.25, vib_rate=5.0, vib_depth=0.05, attack=0.08, release=0.25):
    """Voz gutural: diente de sierra distorsionada filtrada por formantes + aliento."""
    n = int(d * sr)
    t = np.arange(n) / sr
    f0 = f0_fn(t / d) * (1.0 + vib_depth * np.sin(TAU * vib_rate * t))
    ph = TAU * np.cumsum(f0) / sr
    src = 2.0 * ((ph / TAU) % 1.0) - 1.0 + 0.5 * np.sin(2 * ph)
    src = np.tanh(rasp * src)
    v = shape_filter(src, sr, lambda f: 0.02 + sum(a * np.exp(-0.5 * ((f - fc) / bw) ** 2) for fc, a, bw in formants))
    br = bandpass(noise(n), sr, 500, 3500) * breath
    irregular = 0.75 + 0.25 * lowpass(noise(n), sr, 9) / (np.std(lowpass(noise(n), sr, 9)) + 1e-9) * 0.3
    env = np.minimum(1.0, t / attack) * np.clip((d - t) / release, 0, 1)
    return (v + br * 0.6) * env * np.clip(irregular, 0.3, 1.3)


def thump(d, sr, f_start, f_end, tau):
    t = tgrid(d, sr)
    f = f_end + (f_start - f_end) * np.exp(-t / (tau * 0.6))
    return np.sin(TAU * np.cumsum(f) / sr) * decay(len(t), sr, tau)


def click(sr, tone=1500.0, tau=0.006, hp=2500.0, d=0.05):
    n = int(d * sr)
    t = np.arange(n) / sr
    return highpass(noise(n), sr, hp) * decay(n, sr, tau) + 0.5 * np.sin(TAU * tone * t) * decay(n, sr, tau * 1.5)


# ------------------------------------------------------------------ efectos
def build_sfx(out):
    SR = 44100
    made = {}

    # Pasos sobre hormigon (3 variantes)
    for i in range(3):
        n = int(0.18 * SR)
        t = np.arange(n) / SR
        body = lowpass(noise(n), SR, 650 + i * 170) * decay(n, SR, 0.035 + 0.01 * i)
        low = np.sin(TAU * (68 + 9 * i) * t) * decay(n, SR, 0.05) * 0.9
        tick = highpass(noise(n), SR, 2800) * decay(n, SR, 0.006) * 0.12
        made[f"step{i + 1}"] = save(out, f"step{i + 1}", body + low + tick, SR, 0.8)

    # Pistola
    n = int(0.6 * SR)
    crack = highpass(noise(n), SR, 1200) * decay(n, SR, 0.011)
    body = lowpass(noise(n), SR, 1600) * decay(n, SR, 0.06)
    th = thump(0.6, SR, 220, 55, 0.09)
    x = np.tanh(1.5 * (0.9 * crack + 0.8 * body + 1.2 * th))
    made["pistol_shot"] = save(out, "pistol_shot", reverb(x, SR, 0.9, 0.35), SR, 0.95)

    # Escopeta: disparo + accion de bomba
    n = int(1.9 * SR)
    shot = np.zeros(n)
    place(shot, highpass(noise(int(0.4 * SR)), SR, 900) * decay(int(0.4 * SR), SR, 0.02), 0, SR, 1.0)
    place(shot, lowpass(noise(int(0.8 * SR)), SR, 900) * decay(int(0.8 * SR), SR, 0.14), 0, SR, 1.1)
    place(shot, thump(0.9, SR, 140, 34, 0.2), 0, SR, 1.5)
    pump = bandpass(noise(int(0.12 * SR)), SR, 700, 4200) * decay(int(0.12 * SR), SR, 0.025)
    place(shot, pump, 0.85, SR, 0.5)
    place(shot, thump(0.2, SR, 120, 80, 0.04), 0.86, SR, 0.5)
    place(shot, pump, 1.12, SR, 0.55)
    place(shot, click(SR, 900, 0.01), 1.14, SR, 0.5)
    made["shotgun_shot"] = save(out, "shotgun_shot", reverb(np.tanh(1.4 * shot), SR, 1.5, 0.4), SR, 0.95)

    # Percutor sin balas
    made["dry_fire"] = save(out, "dry_fire", click(SR, 1700, 0.005, 2200, 0.1), SR, 0.7)

    # Recarga: cargador fuera, cargador dentro, corredera
    buf = np.zeros(int(1.7 * SR))
    place(buf, click(SR, 400, 0.008, 1500, 0.08), 0.10, SR, 0.8)
    place(buf, bandpass(noise(int(0.16 * SR)), SR, 800, 3000) * decay(int(0.16 * SR), SR, 0.06, 0.02), 0.50, SR, 0.5)
    place(buf, click(SR, 250, 0.012, 1200, 0.1), 0.95, SR, 1.0)
    place(buf, thump(0.1, SR, 160, 90, 0.03), 0.95, SR, 0.5)
    place(buf, bandpass(noise(int(0.18 * SR)), SR, 600, 3800) * decay(int(0.18 * SR), SR, 0.07, 0.03), 1.25, SR, 0.6)
    place(buf, click(SR, 1100, 0.01, 1800, 0.08), 1.42, SR, 0.9)
    made["reload"] = save(out, "reload", buf, SR, 0.85)

    # Voces de zombi (aun sin efecto de sala)
    vs = 32000
    for i in range(4):
        d = 1.4 + 0.3 * i
        a, b = 78 + 14 * i, 52 + 6 * i
        f0 = lambda u, a=a, b=b: a * (1 - u) + b * u + 6 * np.sin(u * 9)
        fm = ((420 + 60 * i, 1.0, 140), (1050 + 80 * i, 0.7, 200), (2300, 0.25, 350))
        v = voice(d, vs, f0, fm, rasp=3.0 + i * 0.4, breath=0.35, attack=0.25, release=0.5)
        made[f"zgroan{i + 1}"] = save(out, f"zgroan{i + 1}", reverb(v, vs, 0.7, 0.2), vs, 0.85)

    v = voice(0.8, vs, lambda u: 95 + 55 * np.sin(np.pi * np.clip(u * 1.2, 0, 1)) - 25 * u,
              ((520, 1.0, 160), (1250, 0.8, 220), (2500, 0.3, 400)), rasp=4.5, breath=0.55, attack=0.05, release=0.2)
    made["zattack"] = save(out, "zattack", reverb(v, vs, 0.5, 0.15), vs, 0.9)

    for i in range(2):
        v = voice(0.5, vs, lambda u, i=i: 170 - 80 * u - 15 * i,
                  ((600, 1.0, 180), (1400, 0.6, 250)), rasp=3.5, breath=0.4, attack=0.03, release=0.18)
        made[f"zhurt{i + 1}"] = save(out, f"zhurt{i + 1}", v, vs, 0.85)

    v = voice(2.3, vs, lambda u: 115 * (1 - u) ** 0.8 + 36, ((400, 1.0, 130), (900, 0.7, 200)),
              rasp=3.0, breath=0.3, attack=0.1, release=0.7)
    buf = np.zeros(len(v) + int(0.3 * vs))
    place(buf, v, 0, vs)
    place(buf, thump(0.4, vs, 90, 40, 0.1) * 1.4, 1.95, vs)
    place(buf, lowpass(noise(int(0.3 * vs)), vs, 400) * decay(int(0.3 * vs), vs, 0.07), 1.95, vs, 0.7)
    made["zdeath"] = save(out, "zdeath", reverb(buf, vs, 0.8, 0.25), vs, 0.9)

    # Jugador
    v = voice(0.35, vs, lambda u: 190 - 50 * u, ((700, 1.0, 200), (1200, 0.6, 260)),
              rasp=1.6, breath=0.25, vib_depth=0.02, attack=0.02, release=0.12)
    made["player_hurt"] = save(out, "player_hurt", v, vs, 0.85)
    v = voice(1.4, vs, lambda u: 175 * (1 - u) ** 0.7 + 62, ((650, 1.0, 190), (1150, 0.6, 250)),
              rasp=1.8, breath=0.3, vib_depth=0.04, attack=0.04, release=0.5)
    buf = np.zeros(len(v) + int(0.4 * vs))
    place(buf, v, 0, vs)
    place(buf, thump(0.4, vs, 100, 45, 0.08) * 1.3, 1.2, vs)
    place(buf, lowpass(noise(int(0.3 * vs)), vs, 600) * decay(int(0.3 * vs), vs, 0.06), 1.2, vs, 0.8)
    made["player_death"] = save(out, "player_death", reverb(buf, vs, 0.6, 0.2), vs, 0.9)

    # Puertas: chirrido, cierre, cerrada con llave, desbloqueo
    def creak(d, lo=110, spread=120, rate=11.0):
        sr = 32000
        n = int(d * sr)
        t = np.arange(n) / sr
        wob = lowpass(noise(n), sr, 3.0)
        wob = wob / (np.max(np.abs(wob)) + 1e-9)
        f = lo + spread * (0.5 + 0.5 * wob) + 40 * t / d
        ph = TAU * np.cumsum(f) / sr
        saw = 2.0 * ((ph / TAU) % 1.0) - 1.0
        trem = 0.55 + 0.45 * np.sin(TAU * rate * t + 3 * wob)
        v = shape_filter(saw * trem, sr, lambda fr: 0.05 + 1.0 * np.exp(-0.5 * ((fr - 850) / 450) ** 2))
        fr_ = bandpass(noise(n), sr, 400, 2500) * 0.12 * trem
        env = np.minimum(1, t / 0.15) * np.clip((d - t) / 0.3, 0, 1) * (0.5 + 0.5 * np.sin(np.pi * t / d))
        return (v + fr_) * env

    c = creak(1.9)
    made["door_open"] = save(out, "door_open", reverb(c, 32000, 0.6, 0.18), 32000, 0.85)
    c = creak(0.9, 130, 90, 14)
    buf = np.zeros(len(c) + int(0.3 * 32000))
    place(buf, c, 0, 32000)
    place(buf, thump(0.3, 32000, 110, 50, 0.07) * 1.4, 0.85, 32000)
    place(buf, lowpass(noise(int(0.2 * 32000)), 32000, 500) * decay(int(0.2 * 32000), 32000, 0.04), 0.85, 32000, 0.8)
    made["door_close"] = save(out, "door_close", reverb(buf, 32000, 0.5, 0.2), 32000, 0.9)

    buf = np.zeros(int(0.9 * SR))
    for at, g in ((0.0, 1.0), (0.13, 0.9), (0.36, 0.7)):
        place(buf, lowpass(noise(int(0.15 * SR)), SR, 1300) * decay(int(0.15 * SR), SR, 0.03), at, SR, g)
        for fq in (880, 1450):
            tt_ = tgrid(0.15, SR)
            place(buf, np.sin(TAU * fq * tt_) * decay(len(tt_), SR, 0.05) * 0.18, at, SR, g)
    place(buf, bandpass(noise(int(0.3 * SR)), SR, 2500, 6000) * decay(int(0.3 * SR), SR, 0.1) * 0.15, 0.4, SR)
    made["door_locked"] = save(out, "door_locked", buf, SR, 0.85)

    buf = np.zeros(int(0.7 * SR))
    place(buf, click(SR, 1300, 0.007, 2000, 0.06), 0.0, SR, 0.8)
    place(buf, click(SR, 1100, 0.007, 2000, 0.06), 0.11, SR, 0.8)
    place(buf, thump(0.2, SR, 180, 70, 0.05), 0.30, SR, 0.9)
    place(buf, click(SR, 700, 0.012, 1500, 0.08), 0.30, SR, 1.0)
    made["door_unlock"] = save(out, "door_unlock", buf, SR, 0.85)

    # Objetos, linterna, interruptor
    n = int(0.4 * SR)
    r = bandpass(noise(n), SR, 1500, 7000)
    gate = np.clip(lowpass(noise(n), SR, 35) * 6, 0, 1)
    buf = r * gate * decay(n, SR, 0.15, 0.01) * 0.6
    tt_ = tgrid(0.25, SR)
    place(buf, np.sin(TAU * 1900 * tt_) * decay(len(tt_), SR, 0.07) * 0.18, 0.05, SR)
    made["pickup"] = save(out, "pickup", buf, SR, 0.7)

    buf = np.zeros(int(0.14 * SR))
    place(buf, click(SR, 1200, 0.004, 2500, 0.05), 0.0, SR, 1.0)
    place(buf, click(SR, 1000, 0.004, 2500, 0.05), 0.045, SR, 0.7)
    made["flashlight"] = save(out, "flashlight", buf, SR, 0.75)

    buf = np.zeros(int(0.25 * SR))
    n = int(0.12 * SR)
    place(buf, lowpass(noise(n), SR, 2600) * decay(n, SR, 0.012), 0.0, SR, 1.0)
    place(buf, np.sin(TAU * 600 * tgrid(0.12, SR)) * decay(n, SR, 0.02) * 0.8, 0.0, SR)
    place(buf, thump(0.1, SR, 220, 110, 0.03), 0.0, SR, 0.5)
    place(buf, click(SR, 1500, 0.006, 2200, 0.06), 0.06, SR, 0.55)
    made["switch"] = save(out, "switch", buf, SR, 0.9)

    # Lamparas: zumbido (bucle) y chispazo
    sr2 = 22050
    t = tgrid(2.0, sr2)   # 100 Hz * 2 s = 200 ciclos exactos => bucle sin saltos
    hum = (np.sin(TAU * 100 * t) + 0.5 * np.sin(TAU * 200 * t) + 0.3 * np.sin(TAU * 300 * t) + 0.12 * np.sin(TAU * 500 * t))
    hum += 0.05 * bandpass(noise(len(t)), sr2, 2000, 5000)
    made["lamp_hum"] = save(out, "lamp_hum", hum, sr2, 0.5, fade=0)

    n = int(0.35 * SR)
    t = np.arange(n) / SR
    bursts = (noise(n) * (lowpass(noise(n), SR, 60) > 0.15)) * decay(n, SR, 0.12)
    buzz = (2 * ((TAU * 120 * t / TAU) % 1.0) - 1) * decay(n, SR, 0.1) * 0.5
    made["lamp_zap"] = save(out, "lamp_zap", highpass(bursts, SR, 900) * 0.7 + buzz, SR, 0.7)

    # Latido (bucle de 0.9 s)
    sr3 = 22050
    buf = np.zeros(int(0.9 * sr3))
    place(buf, thump(0.4, sr3, 70, 45, 0.09), 0.0, sr3, 1.0)
    place(buf, thump(0.4, sr3, 62, 42, 0.08), 0.30, sr3, 0.75)
    made["heartbeat"] = save(out, "heartbeat", lowpass(buf, sr3, 220), sr3, 0.8, fade=0)

    # Sonidos lejanos de ambiente (para sustos aleatorios)
    n = int(1.2 * SR)
    b = np.sin(TAU * 42 * tgrid(1.2, SR)) * decay(n, SR, 0.3) + lowpass(noise(n), SR, 220) * decay(n, SR, 0.15) * 0.8
    made["sting_bang"] = save(out, "sting_bang", lowpass(reverb(b, SR, 3.2, 0.7), SR, 900), SR, 0.8)

    c = creak(2.4, 70, 60, 7)
    made["sting_creak"] = save(out, "sting_creak", lowpass(reverb(c, 32000, 2.5, 0.6), 32000, 1200), 32000, 0.6)

    n = int(2.6 * SR)
    t = np.arange(n) / SR
    sweep = bandpass(noise(n), SR, 1200, 3800)
    amp = np.abs(lowpass(noise(n), SR, 7)) * 6
    amp = np.clip(amp, 0, 1) * np.sin(np.pi * t / 2.6) ** 1.5
    made["sting_scrape"] = save(out, "sting_scrape", lowpass(reverb(sweep * amp, SR, 2.5, 0.5), SR, 4000), SR, 0.55)
    return made


# ------------------------------------------------------------------ musica
def _circular_events(n, sr, events, wet_rt60, wet):
    """events: [(segundo, señal, ganancia)]. Se colocan con recorrido circular y reverb envolvente."""
    ev = np.zeros(n)
    for at, sig, g in events:
        tmp = np.zeros(n)
        m = min(n, len(sig))
        tmp[:m] = sig[:m] * g
        ev += np.roll(tmp, int(at * sr))
    return reverb(ev, sr, wet_rt60, wet, circular=True)


def build_music(out):
    made = {}
    sr = 22050

    # ---- Ambiente: 40 s, bucle perfecto (todas las frecuencias dan ciclos enteros en 40 s)
    D = 40.0
    t = tgrid(D, sr)
    n = len(t)
    x = np.zeros(n)
    for f, a, per, ph in ((41.25, 0.45, 40.0, 0.0), (55.0, 0.50, 20.0, 1.0), (82.5, 0.34, 40.0 / 3, 2.0), (110.0, 0.20, 8.0, 0.5)):
        lfo = 0.6 + 0.4 * np.sin(TAU * t / per + ph)
        x += a * lfo * (np.sin(TAU * f * t) + 0.3 * np.sin(TAU * 2 * f * t) + 0.12 * np.sin(TAU * 3 * f * t))
    for f, per, ph in ((220.0, 40.0 / 3, 0.0), (232.5, 10.0, 1.3), (329.5, 8.0, 2.4), (349.0, 20.0, 0.6)):
        swell = (0.5 + 0.5 * np.sin(TAU * t / per + ph)) ** 3
        x += 0.07 * swell * np.sin(TAU * f * t + 0.5 * np.sin(TAU * 0.15 * t))
    wind = lowpass(noise(n), sr, 420) * (0.5 + 0.5 * np.sin(TAU * t / 20.0 + 0.7)) ** 2
    x += 0.16 * wind / (np.std(wind) + 1e-9) * 0.3

    events = []
    for at in (4.0, 11.5, 19.0, 27.0, 34.5):                         # golpes sordos lejanos
        th = thump(1.5, sr, 70, 38, 0.25) + lowpass(noise(int(1.5 * sr)), sr, 180) * decay(int(1.5 * sr), sr, 0.1) * 0.5
        events.append((at, th, 0.8))
    for at in (7.0, 22.0, 31.0):                                      # metal que rasca
        m = int(2.2 * sr)
        sc = bandpass(noise(m), sr, 900, 3200) * np.abs(lowpass(noise(m), sr, 6)) * 5 * np.sin(np.pi * np.arange(m) / m)
        events.append((at, sc, 0.35))
    x += 1.2 * _circular_events(n, sr, events, 3.5, 0.75)
    made["music_ambient"] = save(out, "music_ambient", x, sr, 0.7, fade=0)

    # ---- Tension: 24 s, pulsos graves + cuerdas disonantes + ruido creciente
    D = 24.0
    t = tgrid(D, sr)
    n = len(t)
    x = np.zeros(n)
    pulse = 0.55 + 0.45 * np.sin(TAU * 1.25 * t - 1.2)             # 1.25 Hz = 30 ciclos en 24 s
    x += 0.55 * pulse * (np.sin(TAU * 49.0 * t) + 0.35 * np.sin(TAU * 98.0 * t))
    for f, per in ((880.0, 12.0), (933.0, 8.0), (1318.5, 24.0)):
        trem = 0.5 + 0.5 * np.sin(TAU * t / per)
        x += 0.05 * trem * np.sin(TAU * f * t + 2.0 * np.sin(TAU * 5 * t))
    p = (t / 12.0) % 1.0
    riser = bandpass(noise(n), sr, 2200, 7000) * (np.sin(np.pi * p) ** 2) * p
    x += 0.35 * riser / (np.std(riser) + 1e-9) * 0.2
    beats = []
    for k in range(36):                                               # latidos graves a 1.5 Hz
        beats.append((k / 1.5, thump(0.5, sr, 85, 48, 0.1), 0.8 if k % 2 == 0 else 0.55))
    x += _circular_events(n, sr, beats, 1.2, 0.4)
    made["music_tension"] = save(out, "music_tension", x, sr, 0.75, fade=0)
    return made
