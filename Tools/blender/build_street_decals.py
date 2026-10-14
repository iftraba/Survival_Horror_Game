"""Capa de suelo de la calle (2026-10-14, fase 2 del lavado de cara del exterior): texturas con alfa que se pegan sobre el asfalto y las aceras.

    blender --background --python build_street_decals.py -- <carpeta ABSOLUTA de salida>

Salen (1024 salvo indicacion): street_crack_a/b/c, street_patch, street_oil, street_puddle, street_manhole (512), street_drain (512 x 256),
street_zebra (1024 x 2048: las barras corren a lo largo de X y se repiten a lo largo de Z; 8 barras en 8 m), street_litter (papeles y latas), street_skid (1024 x 256).
Colores ya en sRGB y alfa recta (sin premultiplicar). Se escriben con zlib a mano para que ningun ajuste de color de Blender los toque.
"""
import math
import os
import struct
import sys
import zlib

import numpy as np

out = sys.argv[sys.argv.index("--") + 1]
os.makedirs(out, exist_ok=True)
rng = np.random.default_rng(5)


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
    ys = np.linspace(0, gy, shape[0], endpoint=False)
    xs = np.linspace(0, gx, shape[1], endpoint=False)
    y0, x0 = ys.astype(int), xs.astype(int)
    fy = (ys - y0)[:, None]
    fx = (xs - x0)[None, :]
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    return g[y0][:, x0] * (1 - fx) * (1 - fy) + g[y0][:, x0 + 1] * fx * (1 - fy) + g[y0 + 1][:, x0] * (1 - fx) * fy + g[y0 + 1][:, x0 + 1] * fx * fy


def fbm(shape, base, octaves, seed):
    total, amp, norm = np.zeros(shape, np.float32), 1.0, 0.0
    for o in range(octaves):
        total += amp * value_noise(shape, (base[0] * 2 ** o, base[1] * 2 ** o), seed + o)
        norm += amp
        amp *= 0.55
    return total / norm


def blur(a, k=3):
    for _ in range(k):
        a = (a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 5.0
    return a


def stamp(mask, x, y, r, v=1.0):
    """Punto suave (gaussiano) de radio r sobre la mascara."""
    h, w = mask.shape
    ri = int(math.ceil(r * 2.2)) + 1
    y0, y1, x0, x1 = max(0, int(y) - ri), min(h, int(y) + ri + 1), max(0, int(x) - ri), min(w, int(x) + ri + 1)
    if y0 >= y1 or x0 >= x1:
        return
    yy, xx = np.mgrid[y0:y1, x0:x1]
    g = np.exp(-(((xx - x) ** 2 + (yy - y) ** 2) / (r * r)) * 1.2) * v
    mask[y0:y1, x0:x1] = np.maximum(mask[y0:y1, x0:x1], g)


def crack(size, seed, branches=3, length=620):
    r = np.random.default_rng(seed)
    mask = np.zeros((size, size), np.float32)

    def walk(x, y, ang, n, width, depth):
        for i in range(n):
            ang += r.normal(0, 0.16)
            x += math.cos(ang) * 2.2
            y += math.sin(ang) * 2.2
            stamp(mask, x, y, max(0.6, width * (1 - i / n * 0.7)))
            if depth > 0 and r.random() < 0.012:
                walk(x, y, ang + r.choice([-1, 1]) * r.uniform(0.5, 1.2), int(n * r.uniform(0.25, 0.5)), width * 0.7, depth - 1)
    a0 = r.uniform(0, 2 * math.pi)
    walk(size * 0.1, size * 0.5, a0 * 0 + r.uniform(-0.4, 0.4), length // 2, 2.2, 2)
    walk(size * 0.5, size * 0.5, r.uniform(-0.6, 0.6), length // 2, 2.2, 2)
    mask = blur(mask, 1)
    img = np.zeros((size, size, 4), np.float32)
    img[..., :3] = np.array([0.012, 0.012, 0.014])
    edge = np.clip(blur(mask, 2) * 1.4 - mask * 0.4, 0, 1)
    img[..., :3] += edge[..., None] * 0.05
    img[..., 3] = np.clip(mask * 1.15, 0, 0.92)
    return img


def patch(size=1024):
    img = np.zeros((size, size, 4), np.float32)
    n = fbm((size, size), (8, 8), 5, 20)
    inside = np.zeros((size, size), np.float32)
    m = int(size * 0.07)
    inside[m:size - m, m:size - m] = 1.0
    inside = blur(inside, 2)
    inside = np.clip((inside - 0.5) * 6 + 0.5, 0, 1)
    img[..., :3] = (np.array([0.055, 0.055, 0.058]) + (n[..., None] - 0.5) * 0.04)
    edge = np.clip(1.0 - np.abs(inside - 0.5) * 5.0, 0, 1)
    img[..., :3] = img[..., :3] * (1 - edge[..., None] * 0.6)                                # costura oscura
    img[..., 3] = inside * 0.95
    return img


def blob(size, seed, scale, thresh, color, alpha_max, sharp=4.0):
    n = fbm((size, size), (scale, scale), 5, seed)
    yy, xx = np.mgrid[0:size, 0:size]
    rad = np.sqrt(((xx - size / 2) / (size / 2)) ** 2 + ((yy - size / 2) / (size / 2)) ** 2)
    v = (n - thresh) * sharp + (1.0 - rad) * 1.5 - 0.6
    a = np.clip(v, 0, 1)
    img = np.zeros((size, size, 4), np.float32)
    img[..., :3] = np.array(color)
    img[..., 3] = a * alpha_max
    return img


def manhole(size=512):
    img = np.zeros((size, size, 4), np.float32)
    yy, xx = np.mgrid[0:size, 0:size]
    cx = cy = size / 2
    rad = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / (size / 2)
    inside = (rad < 0.97).astype(np.float32)
    ring = ((rad > 0.84) & (rad < 0.97)).astype(np.float32)
    n = fbm((size, size), (10, 10), 5, 40)
    base = np.array([0.17, 0.16, 0.15]) + (n[..., None] - 0.5) * 0.06
    col = np.broadcast_to(base, (size, size, 3)).copy()
    col[rad > 0.96] = np.array([0.03, 0.03, 0.032])                                          # junta oscura con el asfalto
    # dibujo: aros concentricos y rejilla de rombos
    grooves = (np.abs((rad * 12) % 1.0 - 0.5) < 0.09) & (rad < 0.84)
    col[grooves] *= 0.55
    ang = np.arctan2(yy - cy, xx - cx)
    spokes = (np.abs(((ang * 12 / math.pi) % 1.0) - 0.5) < 0.07) & (rad < 0.8) & (rad > 0.12)
    col[spokes] *= 0.6
    col[(rad < 0.1)] *= 1.3
    col[ring.astype(bool)] *= 1.15
    rust = fbm((size, size), (6, 6), 4, 50)
    col = col * (1 - (rust[..., None] > 0.6) * 0.25) + (rust[..., None] > 0.6) * np.array([0.05, 0.02, 0.01]) * 0.5
    img[..., :3] = col
    edge = np.clip((0.99 - rad) * 40, 0, 1)
    img[..., 3] = edge * inside
    return img


def drain(w=512, h=256):
    img = np.zeros((h, w, 4), np.float32)
    n = fbm((h, w), (4, 8), 4, 60)
    img[..., :3] = np.array([0.15, 0.145, 0.14]) + (n[..., None] - 0.5) * 0.05
    img[..., 3] = 1.0
    yy, xx = np.mgrid[0:h, 0:w]
    frame = (xx < 18) | (xx > w - 19) | (yy < 14) | (yy > h - 15)
    slots = (~frame) & (np.abs(((xx - 18) / 36.0) % 1.0 - 0.5) < 0.28) & (yy > 30) & (yy < h - 31)
    img[..., :3][slots] = np.array([0.004, 0.004, 0.005])
    img[..., :3][frame] *= 1.4
    a = np.clip((np.minimum(xx, w - 1 - xx) * 0.5), 0, 1) * np.clip(np.minimum(yy, h - 1 - yy) * 0.5, 0, 1)
    img[..., 3] = a
    return img


def zebra(w=1024, h=2048):
    img = np.zeros((h, w, 4), np.float32)
    yy, xx = np.mgrid[0:h, 0:w]
    bar = ((yy // (h // 16)) % 2 == 0)                                                       # 16 tramos de 0,5 m: 8 barras blancas
    n = fbm((h, w), (24, 8), 6, 70)
    worn = fbm((h, w), (60, 20), 4, 80)
    a = bar.astype(np.float32)
    a = blur(a, 1)
    a = np.clip(a * (0.55 + 0.7 * worn) - np.clip(0.5 - n, 0, 1) * 0.9 - 0.05, 0, 1)
    ends = np.clip(np.minimum(xx, w - 1 - xx) / 6.0, 0, 1)
    a *= ends
    img[..., :3] = np.array([0.78, 0.78, 0.74]) * (0.8 + 0.2 * worn[..., None])
    img[..., 3] = a * 0.85
    return img


def litter(size=1024):
    img = np.zeros((size, size, 4), np.float32)
    yy, xx = np.mgrid[0:size, 0:size]
    for _ in range(70):
        cx, cy = rng.uniform(40, size - 40, 2)
        w, h = rng.uniform(14, 40), rng.uniform(10, 26)
        ang = rng.uniform(0, math.pi)
        c, s = math.cos(ang), math.sin(ang)
        u = (xx - cx) * c + (yy - cy) * s
        v = -(xx - cx) * s + (yy - cy) * c
        m = (np.abs(u) < w) & (np.abs(v) < h)
        kind = rng.random()
        if kind < 0.7:
            col = np.array([0.78, 0.76, 0.7]) * rng.uniform(0.6, 1.0)                           # papel
        elif kind < 0.88:
            col = np.array([0.7, 0.25, 0.1]) * rng.uniform(0.6, 1.0)                            # carton / lata roja
        else:
            col = np.array([0.4, 0.4, 0.42])
        shade = 0.85 + 0.15 * np.sin(u * 0.4)
        sel = m & (img[..., 3] < 0.5)
        img[..., :3][sel] = (col * shade[sel][:, None])
        img[..., 3][sel] = 1.0
    img[..., 3] = blur(img[..., 3], 1)
    img[..., 3] = np.clip(img[..., 3] * 1.1, 0, 1)
    return img


def skid(w=1024, h=256):
    img = np.zeros((h, w, 4), np.float32)
    yy, xx = np.mgrid[0:h, 0:w]
    n = fbm((h, w), (3, 12), 5, 90)
    for off in (-34, 34):
        center = h / 2 + off + np.sin(xx / 170.0) * 18
        a = np.exp(-((yy - center) / 9.0) ** 2) * np.clip(1.0 - xx / w * 0.6, 0.3, 1.0)
        a = a * np.clip(n * 1.6 - 0.3, 0, 1)
        img[..., 3] = np.maximum(img[..., 3], a)
    img[..., :3] = np.array([0.012, 0.012, 0.014])
    img[..., 3] = np.clip(img[..., 3] * 0.85, 0, 0.85)
    return img


write_png(os.path.join(out, "street_crack_a.png"), crack(1024, 1, 3, 700))
write_png(os.path.join(out, "street_crack_b.png"), crack(1024, 2, 2, 560))
write_png(os.path.join(out, "street_crack_c.png"), crack(1024, 3, 4, 800))
write_png(os.path.join(out, "street_patch.png"), patch())
write_png(os.path.join(out, "street_oil.png"), blob(1024, 11, 6, 0.46, (0.008, 0.008, 0.01), 0.9, 5.0))
write_png(os.path.join(out, "street_puddle.png"), blob(1024, 21, 4, 0.5, (0.02, 0.028, 0.04), 0.88, 6.0))
write_png(os.path.join(out, "street_manhole.png"), manhole())
write_png(os.path.join(out, "street_drain.png"), drain())
write_png(os.path.join(out, "street_zebra.png"), zebra())
write_png(os.path.join(out, "street_litter.png"), litter())
write_png(os.path.join(out, "street_skid.png"), skid())
print("PROCESO_TERMINADO")
