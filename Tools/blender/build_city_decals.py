"""Decals de las fachadas de la ciudad (2026-10-14, fase 3 del lavado de cara del exterior): pintadas, carteles, chorretones y hollin, con alfa.

    blender --background --python build_city_decals.py -- <carpeta ABSOLUTA de salida>

Salen: city_graffiti_a/b/c (512), city_poster (256 x 384), city_streak (256 x 1024), city_soot (512 x 256). Mismo escritor de PNG que build_street_decals.py.
"""
import math
import os
import struct
import sys
import zlib

import numpy as np

out = sys.argv[sys.argv.index("--") + 1]
os.makedirs(out, exist_ok=True)


def write_png(path, rgba):
    h, w, _ = rgba.shape
    data = (np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8)
    raw = b"".join(b"\x00" + data[y].tobytes() for y in range(h))

    def chunk(tag, payload):
        c = struct.pack(">I", len(payload)) + tag + payload
        return c + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)
    print("hecho", os.path.basename(path), w, h, flush=True)


def value_noise(shape, cells, seed):
    r = np.random.default_rng(seed)
    gy, gx = cells
    g = r.random((gy + 1, gx + 1)).astype(np.float32)
    ys, xs = np.linspace(0, gy, shape[0], endpoint=False), np.linspace(0, gx, shape[1], endpoint=False)
    y0, x0 = ys.astype(int), xs.astype(int)
    fy, fx = (ys - y0)[:, None], (xs - x0)[None, :]
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    return g[y0][:, x0] * (1 - fx) * (1 - fy) + g[y0][:, x0 + 1] * fx * (1 - fy) + g[y0 + 1][:, x0] * (1 - fx) * fy + g[y0 + 1][:, x0 + 1] * fx * fy


def fbm(shape, base, octaves, seed):
    total, amp, norm = np.zeros(shape, np.float32), 1.0, 0.0
    for o in range(octaves):
        total += amp * value_noise(shape, (base[0] * 2 ** o, base[1] * 2 ** o), seed + o)
        norm += amp
        amp *= 0.55
    return total / norm


def blur(a, k=2):
    for _ in range(k):
        a = (a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 5.0
    return a


def stamp(mask, x, y, r, v=1.0):
    h, w = mask.shape
    ri = int(math.ceil(r * 2.0)) + 1
    y0, y1, x0, x1 = max(0, int(y) - ri), min(h, int(y) + ri + 1), max(0, int(x) - ri), min(w, int(x) + ri + 1)
    if y0 >= y1 or x0 >= x1:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    g = np.exp(-(((xx - x) ** 2 + (yy - y) ** 2) / (r * r)) * 1.6) * v
    mask[y0:y1, x0:x1] = np.maximum(mask[y0:y1, x0:x1], g)


class Layer:
    """Capa de pintura en spray: mascara de cada color, se compone al final."""

    def __init__(self, h, w):
        self.h, self.w = h, w
        self.cols = []

    def stroke(self, pts, r, col):
        mask = np.zeros((self.h, self.w), np.float32)
        for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
            n = int(max(abs(x1 - x0), abs(y1 - y0)) / max(1.0, r * 0.35)) + 1
            for i in range(n + 1):
                t = i / n
                stamp(mask, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, r)
        self.cols.append((mask, np.array(col, np.float32)))
        return mask

    def drips(self, mask, col, rng, count=7, length=(30, 110)):
        m2 = np.zeros_like(mask)
        ys, xs = np.where(mask > 0.8)
        if len(xs) == 0:
            return
        for _ in range(count):
            i = rng.integers(0, len(xs))
            x, y = xs[i], ys[i]
            for d in range(int(rng.uniform(*length))):
                stamp(m2, x + math.sin(d * 0.05) * 0.6, y + d, 1.7, 1.0 - d / 140.0)
        self.cols.append((m2, np.array(col, np.float32)))

    def compose(self):
        img = np.zeros((self.h, self.w, 4), np.float32)
        for mask, col in self.cols:
            a = np.clip(mask * 1.3, 0, 1)
            img[..., :3] = img[..., :3] * (1 - a[..., None]) + col * a[..., None]
            img[..., 3] = np.maximum(img[..., 3], a)
        n = fbm((self.h, self.w), (24, 24), 4, 5)
        img[..., 3] *= np.clip(0.8 + 0.4 * n, 0, 1)                                       # pintura desgastada
        return img


def graffiti(seed, kind):
    rng = np.random.default_rng(seed)
    L = Layer(512, 512)
    if kind == 0:                                                                          # aspa dentro de un circulo, rojo, con chorreones
        ring = [(256 + 190 * math.cos(a), 256 + 190 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 60)]
        m1 = L.stroke(ring, 13, (0.65, 0.04, 0.03))
        m2 = L.stroke([(130, 130), (385, 385)], 15, (0.65, 0.04, 0.03))
        m3 = L.stroke([(385, 130), (130, 385)], 15, (0.65, 0.04, 0.03))
        L.drips(np.maximum(np.maximum(m1, m2), m3), (0.65, 0.04, 0.03), rng, 9)
    elif kind == 1:                                                                        # espiral y rayas, blanco y negro
        sp = [(256 + t * 6.5 * math.cos(t * 0.9), 256 + t * 6.5 * math.sin(t * 0.9)) for t in np.linspace(1, 38, 120)]
        L.stroke(sp, 9, (0.02, 0.02, 0.025))
        m = L.stroke([(60, 460), (450, 430)], 12, (0.9, 0.9, 0.88))
        L.stroke([(60, 60), (200, 100), (140, 170)], 10, (0.9, 0.9, 0.88))
        L.drips(m, (0.9, 0.9, 0.88), rng, 6)
    else:                                                                                  # trazos en zigzag verdes y subrayado azul
        zz = [(40 + i * 50, 150 + (60 if i % 2 else -60) + rng.uniform(-20, 20)) for i in range(9)]
        m = L.stroke(zz, 17, (0.05, 0.5, 0.15))
        L.stroke([(40, 330), (470, 310)], 12, (0.05, 0.2, 0.75))
        L.stroke([(100, 400), (410, 410)], 9, (0.9, 0.78, 0.1))
        L.drips(m, (0.05, 0.5, 0.15), rng, 8)
    return L.compose()


def poster(w=256, h=384):
    img = np.zeros((h, w, 4), np.float32)
    n = fbm((h, w), (10, 7), 5, 9)
    inside = np.ones((h, w), np.float32)
    inside[:6] = inside[-6:] = 0
    inside[:, :6] = inside[:, -6:] = 0
    inside = blur(inside, 3)
    inside *= np.clip(n * 3.0 - 0.1, 0.0, 1.0) * 0.5 + 0.55                                  # bordes rotos
    paper = np.array([0.78, 0.74, 0.62]) * (0.8 + 0.3 * n[..., None])
    img[..., :3] = paper
    yy, xx = np.mgrid[0:h, 0:w]
    img[..., :3][(yy > 28) & (yy < 70) & (xx > 20) & (xx < w - 20)] = np.array([0.55, 0.05, 0.04])       # banda roja de titulo (sin letras)
    img[..., :3][(yy > 86) & (yy < 206) & (xx > 52) & (xx < w - 52)] = np.array([0.13, 0.12, 0.12]) * (0.7 + 0.5 * n[(yy > 86) & (yy < 206) & (xx > 52) & (xx < w - 52)][:, None])     # foto
    for i, y0 in enumerate(range(226, 350, 14)):                                              # lineas de texto
        ln = int((w - 56) * (0.9 if i % 3 else 0.6))
        img[..., :3][(yy > y0) & (yy < y0 + 5) & (xx > 28) & (xx < 28 + ln)] = np.array([0.12, 0.12, 0.12])
    img[..., :3] *= (1 - blur(np.clip(fbm((h, w), (5, 4), 4, 31) - 0.55, 0, 1), 3)[..., None] * 1.2)    # manchas de humedad
    img[..., 3] = np.clip(inside, 0, 1) * 0.96
    return img


def streak(w=256, h=1024):
    img = np.zeros((h, w, 4), np.float32)
    vec = fbm((h, w), (2, 14), 5, 41)
    thin = fbm((h, w), (3, 40), 4, 51)
    v = np.clip((vec - 0.45) * 3.0, 0, 1) * np.clip(thin * 1.6 - 0.2, 0, 1)
    fade = np.clip(1.0 - np.arange(h)[:, None] / h * 0.6, 0.3, 1.0)                          # mas oscuro arriba (de donde cae el agua)
    img[..., :3] = np.array([0.02, 0.018, 0.016])
    img[..., 3] = np.clip(v * fade * 0.75, 0, 0.75)
    return img


def soot(w=512, h=256):
    img = np.zeros((h, w, 4), np.float32)
    yy, xx = np.mgrid[0:h, 0:w]
    n = fbm((h, w), (6, 10), 5, 61)
    lick = np.clip(1.0 - yy / (h * (0.35 + 0.65 * n)), 0, 1) * np.clip(1.0 - np.abs(xx - w / 2) / (w * 0.45), 0, 1)
    img[..., :3] = np.array([0.008, 0.008, 0.01])
    img[..., 3] = np.clip(lick ** 1.3 * 0.9, 0, 0.9)
    return img


write_png(os.path.join(out, "city_graffiti_a.png"), graffiti(1, 0))
write_png(os.path.join(out, "city_graffiti_b.png"), graffiti(2, 1))
write_png(os.path.join(out, "city_graffiti_c.png"), graffiti(3, 2))
write_png(os.path.join(out, "city_poster.png"), poster())
write_png(os.path.join(out, "city_streak.png"), streak())
write_png(os.path.join(out, "city_soot.png"), soot())
print("PROCESO_TERMINADO")
