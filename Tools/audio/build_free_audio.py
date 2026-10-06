"""Monta los sonidos del juego a partir de packs gratuitos CC0 (Tools/audio/download) mezclados con capas sintetizadas.

Fuentes (todas CC0):
  zombies.zip           opengameart.org/content/zombies-sound-pack            (artisticdude)
  sndfx.zip             opengameart.org/content/war-on-water-sndfx            (yd, origen Freesound)
  100cc0.zip            opengameart.org/content/100-cc0-sfx                   (rubberduck)
  kenney_impact.zip     kenney.nl/assets/impact-sounds                        (Kenney)

Uso: blender --background --python build_free_audio.py -- <carpeta Assets/_Project/Audio>
Sobrescribe los WAV existentes con el mismo nombre (se conservan los .meta y las referencias) y crea los nuevos.
"""
import math
import os
import sys
import wave

import aud
import numpy as np

SR = 44100
OUT = sys.argv[sys.argv.index("--") + 1]
DL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "download")
rng = np.random.default_rng(20261006)
_cache = {}


# ------------------------------------------------------------------ utilidades
def load(path):
    if path not in _cache:
        snd = aud.Sound(path).resample(SR, False).rechannel(1)
        _cache[path] = np.asarray(snd.data(), dtype=np.float64).reshape(-1).copy()
    return _cache[path].copy()


def src(*parts):
    return load(os.path.join(DL, *parts))


def old(name):
    return load(os.path.join(OUT, name + ".wav"))


def trim(x, thr=0.02, pre=0.004, post=0.03):
    pk = np.max(np.abs(x)) + 1e-9
    idx = np.where(np.abs(x) > thr * pk)[0]
    if len(idx) == 0:
        return x
    a = max(0, idx[0] - int(pre * SR))
    b = min(len(x), idx[-1] + int(post * SR))
    return x[a:b]


def norm(x, peak=0.9):
    return x * (peak / (np.max(np.abs(x)) + 1e-9))


def fade(x, a=0.002, b=0.04):
    x = x.copy()
    na, nb = min(len(x), int(a * SR)), min(len(x), int(b * SR))
    if na > 1:
        x[:na] *= np.linspace(0, 1, na)
    if nb > 1:
        x[-nb:] *= np.linspace(1, 0, nb)
    return x


def shape(x, fn):
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1.0 / SR)
    return np.fft.irfft(X * fn(f), len(x))


def lp(x, fc, order=2):
    return shape(x, lambda f: 1.0 / np.sqrt(1.0 + (f / fc) ** (2 * order)))


def hp(x, fc, order=2):
    return shape(x, lambda f: (f / fc) ** order / np.sqrt(1.0 + (f / fc) ** (2 * order)))


def pitch(x, r):
    """Cambia tono y duracion a la vez (r < 1 = mas grave y largo)."""
    n = int(len(x) / r)
    return np.interp(np.arange(n) * r, np.arange(len(x)), x)


def mix(*layers):
    """layers: (array, ganancia, retardo_s)"""
    n = max(int(d * SR) + len(a) for a, g, d in layers)
    out = np.zeros(n)
    for a, g, d in layers:
        s = int(d * SR)
        out[s:s + len(a)] += a * g
    return out


def reverb(x, rt=0.5, wet=0.3, pre=0.012):
    n = int(rt * SR)
    t = np.arange(n) / SR
    ir = rng.standard_normal(n) * np.exp(-6.9 * t / rt)
    ir = lp(ir, 3500)
    ir[: int(pre * SR)] = 0
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    size = len(x) + n
    y = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)
    out = np.zeros(size)
    out[: len(x)] += x
    out += y * wet * (np.max(np.abs(x)) / (np.max(np.abs(y)) + 1e-9))
    return out


def save(name, x):
    x = np.clip(x, -1.0, 1.0)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype("<i2").tobytes())
    print("%-18s %5.2fs peak %.2f" % (name, len(x) / SR, np.max(np.abs(x))))


def noise_burst(d, fc_lo, fc_hi, decay):
    n = int(d * SR)
    t = np.arange(n) / SR
    return hp(lp(rng.standard_normal(n), fc_hi), fc_lo) * np.exp(-t / decay)


# ------------------------------------------------------------------ entradas (se leen antes de sobrescribir)
Z = lambda i: src("zombies", "zombies", "zombie-%d.wav" % i)
G = lambda n: src("sndfx", "Guns", n + ".ogg")
C = lambda n: src("100cc0", n + ".ogg")
K = lambda n: src("kenney_impact", "Audio", n + ".ogg")
old_pistol = old("pistol_shot")
old_shotgun = old("shotgun_shot")
old_step = [old("step1"), old("step2"), old("step3")]

# ------------------------------------------------------------------ armas
crack = norm(trim(G("gun1Light")), 0.9)
body = norm(lp(old_pistol, 700), 0.8)
save("pistol_shot", norm(fade(reverb(mix((crack, 0.85, 0), (body, 0.75, 0.0), (pitch(crack, 0.6), 0.5, 0.0)), 0.45, 0.35)[: int(1.3 * SR)], 0.0005, 0.35), 0.95))

crack = norm(trim(G("gun2Heavy")), 0.9)
body = norm(lp(old_shotgun, 500), 0.85)
save("shotgun_shot", norm(fade(reverb(mix((crack, 0.8, 0), (pitch(crack, 0.55), 0.9, 0.0), (body, 0.8, 0.0)), 0.9, 0.45)[: int(2.2 * SR)], 0.0005, 0.7), 0.97))

save("reload_pistol", norm(fade(trim(G("gun1LightLoad")), 0.002, 0.05), 0.8))
save("reload_shotgun", norm(fade(mix((trim(G("gun2HeavyLoad")), 1.0, 0), (trim(src("sndfx", "Interface", "cartriges.ogg")), 0.5, 0.35)), 0.002, 0.08), 0.85))
save("reload", norm(fade(trim(G("gun1LightLoad")), 0.002, 0.05), 0.8))     # compatibilidad con referencias antiguas
save("dry_fire", norm(trim(G("gunempty")), 0.7))
save("shotgun_pump", norm(fade(trim(G("gunloadmain")), 0.002, 0.06), 0.85))

# ------------------------------------------------------------------ puertas, interruptores, objetos
creak = trim(C("door_02"), 0.03)[: int(2.0 * SR)]
save("door_open", norm(fade(lp(creak, 6000), 0.02, 0.35), 0.8))
save("door_close", norm(fade(mix((trim(C("door_close_03")), 1.0, 0), (trim(C("door_close_04")), 0.35, 0.01)), 0.002, 0.06), 0.9))
save("door_unlock", norm(fade(mix((trim(C("key_open_01")), 1.0, 0), (lp(trim(C("switch_01")), 1500), 0.5, 0.12)), 0.002, 0.05), 0.85))
rattle = trim(C("key_open_02"))
save("door_locked", norm(fade(mix(*[(pitch(rattle, 0.75 + 0.05 * i), 0.9 - 0.12 * i, 0.13 * i) for i in range(5)]), 0.002, 0.06), 0.8))
save("switch", norm(fade(trim(C("switch_01")), 0.001, 0.03), 0.85))
save("flashlight", norm(fade(pitch(trim(C("switch_02")), 1.25), 0.001, 0.02), 0.6))
save("pickup", norm(fade(trim(C("paper_02")), 0.005, 0.08)[: int(0.7 * SR)], 0.7))

# ------------------------------------------------------------------ pasos (baldosa y escalera)
thump = [trim(K("footstep_wood_00%d" % i))[: int(0.14 * SR)] for i in (0, 1, 2)]
click = [trim(K("footstep_concrete_00%d" % i)) for i in (0, 3, 4)]
for i in range(3):
    s = mix((norm(lp(thump[i], 600), 0.9), 1.0, 0.0), (norm(lp(click[i], 4500), 0.8), 0.6, 0.0))
    save("step%d" % (i + 1), norm(fade(reverb(s, 0.3, 0.35), 0.001, 0.1), 0.85))
for i, (wi, ci) in enumerate(((1, 1), (3, 2))):
    w = trim(K("footstep_wood_00%d" % wi))
    c = trim(K("footstep_concrete_00%d" % ci))
    save("step_stairs%d" % (i + 1), norm(fade(reverb(mix((norm(lp(w, 900), 0.9), 1.0, 0.0), (norm(lp(c, 5000), 0.8), 0.7, 0.0)), 0.35, 0.35), 0.001, 0.12), 0.9))

# ------------------------------------------------------------------ zombis
for i, zi in enumerate((16, 17, 18, 20), start=1):
    save("zgroan%d" % i, norm(fade(reverb(lp(trim(Z(zi)), 7000), 0.35, 0.2), 0.01, 0.12), 0.9))
save("zattack", norm(fade(mix((trim(Z(13)), 1.0, 0), (trim(Z(12)), 0.55, 0.05)), 0.003, 0.08), 0.95))
for i, zi in enumerate((10, 11, 22), start=1):
    save("zhurt%d" % i, norm(fade(trim(Z(zi)), 0.003, 0.06), 0.85))
save("zdeath", norm(fade(reverb(mix((trim(Z(21)), 1.0, 0), (pitch(trim(Z(19)), 0.8), 0.7, 0.15)), 0.5, 0.25), 0.003, 0.2), 0.95))

# ------------------------------------------------------------------ jefe
roar = mix((pitch(trim(Z(17)), 0.5), 1.0, 0), (pitch(trim(Z(16)), 0.42), 0.8, 0.12), (pitch(trim(Z(18)), 0.6), 0.6, 0.3))
roar = lp(roar, 3500) + 0.35 * lp(noise_burst(len(roar) / SR, 80, 900, 0.9), 900)[: len(roar)]
save("boss_roar", norm(fade(reverb(roar, 1.0, 0.4), 0.02, 0.4), 0.97))
n = int(0.5 * SR)
t = np.arange(n) / SR
step = np.sin(2 * math.pi * np.cumsum(90 * np.exp(-9 * t) + 32) / SR) * np.exp(-7 * t)
step += 0.6 * lp(noise_burst(0.5, 30, 400, 0.08), 400)[:n]
save("boss_step", norm(fade(reverb(step, 0.5, 0.25), 0.002, 0.15), 0.95))

# ------------------------------------------------------------------ jugador
save("player_death", norm(fade(reverb(lp(pitch(trim(src("sndfx", "Pirates", "scream1.ogg")), 0.82), 5500), 0.4, 0.2), 0.005, 0.2), 0.9))

# ------------------------------------------------------------------ telefono (sintetizado)
def bell_strike(freqs, d=0.35):
    n = int(d * SR)
    t = np.arange(n) / SR
    y = sum(a * np.sin(2 * math.pi * f * t) * np.exp(-t / dec) for f, a, dec in freqs)
    return y


partials = [(1010, 1.0, 0.12), (1590, 0.7, 0.09), (2330, 0.35, 0.06), (3150, 0.2, 0.04)]
ring = np.zeros(int(4.2 * SR))
for start, length in ((0.0, 1.4), (2.0, 1.4)):
    k = 0
    while k * (1 / 17.0) < length:                    # martillo a ~17 golpes por segundo
        s = int((start + k / 17.0) * SR)
        alt = 1.0 if k % 2 == 0 else 0.55            # el martillo golpea dos campanas
        b = bell_strike(partials, 0.25) * alt
        ring[s:s + len(b)] += b[: len(ring) - s]
        k += 1
save("phone_ring", norm(fade(reverb(ring, 0.5, 0.2), 0.005, 0.2), 0.8))

dial = np.zeros(int(1.6 * SR))
t_click = 0.05
for i in range(9):                                    # el disco vuelve: chasquidos a ritmo constante
    s = int(t_click * SR)
    c = noise_burst(0.02, 1500, 5000, 0.004) * 1.0 + 0.5 * np.sin(2 * math.pi * 900 * np.arange(int(0.02 * SR)) / SR) * np.exp(-np.arange(int(0.02 * SR)) / SR / 0.004)
    dial[s:s + len(c)] += c
    t_click += 0.1
dial += 0.35 * lp(noise_burst(1.6, 200, 1200, 0.5), 1200)[: len(dial)] * np.linspace(1, 0, len(dial))   # roce del muelle
save("phone_dial", norm(fade(reverb(dial, 0.3, 0.15), 0.002, 0.1), 0.75))
save("phone_pickup", norm(fade(mix((lp(pitch(trim(C("wooden_01")), 0.8), 2500), 1.0, 0), (lp(trim(C("slam_04")), 1500), 0.45, 0.02)), 0.002, 0.06), 0.8))

print("MONTAJE_TERMINADO")
