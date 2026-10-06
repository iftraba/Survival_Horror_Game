"""Analiza los sonidos gratuitos descargados (duracion, volumen, centroide espectral) para elegir cada uno por su papel.
Uso: blender --background --python analyze_audio.py -- <carpeta> [patron]
"""
import fnmatch
import os
import sys

import aud
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
root = args[0]
pat = args[1] if len(args) > 1 else "*"
SR = 44100


def load(path):
    snd = aud.Sound(path).resample(SR, False).rechannel(1)
    data = np.asarray(snd.data(), dtype=np.float64).reshape(-1)
    return data


rows = []
for dp, _, fs in os.walk(root):
    for f in sorted(fs):
        if not f.lower().endswith((".wav", ".ogg", ".mp3")) or not fnmatch.fnmatch(f, pat):
            continue
        p = os.path.join(dp, f)
        try:
            x = load(p)
        except Exception as e:
            print("ERR", f, e)
            continue
        if len(x) < 64:
            continue
        pk = np.max(np.abs(x))
        rms = np.sqrt(np.mean(x ** 2))
        X = np.abs(np.fft.rfft(x * np.hanning(len(x))))
        fr = np.fft.rfftfreq(len(x), 1.0 / SR)
        cen = float((X * fr).sum() / max(X.sum(), 1e-9))
        low = float((X[fr < 300] ** 2).sum() / max((X ** 2).sum(), 1e-9))
        # tiempo hasta el pico y duracion util (envolvente > 5% del pico)
        env = np.abs(x)
        idx = np.where(env > 0.05 * pk)[0]
        useful = (idx[-1] - idx[0]) / SR if len(idx) else 0
        rows.append((os.path.relpath(p, root).replace("\\", "/"), len(x) / SR, useful, 20 * np.log10(pk + 1e-9), cen, low, np.argmax(env) / SR))
print("archivo | dur | util | pico dB | centroide Hz | %<300Hz | t_pico")
for r in rows:
    print("%s | %.2f | %.2f | %.1f | %.0f | %.2f | %.2f" % r)
print("ANALISIS_TERMINADO")
