"""Cielo nocturno de la ciudad (2026-10-14): panorama equirectangular con degradado, resplandor de la ciudad en el horizonte, estrellas, luna con halo y nubes.

    blender --background --python build_night_sky.py -- <ruta absoluta del PNG de salida> [ancho=4096]

Convencion de Unity (Skybox/Panoramic, Latitude-Longitude): u = 0.5 - atan2(z, x) / (2 pi), v = 1 - acos(y) / pi (v = 1 es el cielo). La luna esta al este (azimut 90, elevacion 23); la luz direccional de la escena
se gira para que venga de ella (Euler(23, 270, 0)).
"""
import math
import sys

import bpy
import numpy as np

out = sys.argv[sys.argv.index("--") + 1]
W = int(sys.argv[sys.argv.index("--") + 2]) if len(sys.argv) > sys.argv.index("--") + 2 else 4096
H = W // 2
rng = np.random.default_rng(7)

# direccion de la luna: al este del patio (el lado del coche en llamas, a la derecha al empezar), a 23 grados sobre el horizonte (a 20 o mas queda libre
# por encima del Edificio_E). La luz direccional de la escena viene de ahi: rotacion Euler(23, 270, 0) (lo aplica NightSkyKit).
AZ, EL = math.radians(90.0), math.radians(23.0)               # azimut desde el norte (+Z) hacia el este (+X), elevacion
MOON_DIR = np.array([math.sin(AZ) * math.cos(EL), math.sin(EL), math.cos(AZ) * math.cos(EL)])

# direccion de cada pixel
uu = (np.arange(W) + 0.5) / W
vv = (np.arange(H) + 0.5) / H
lon = (0.5 - uu) * 2 * math.pi                         # atan2(z, x)
colat = (1.0 - vv) * math.pi                           # acos(y): v = 1 es el cielo
LON, COLAT = np.meshgrid(lon, colat)
Y = np.cos(COLAT)
R = np.sin(COLAT)
X = R * np.cos(LON)
Z = R * np.sin(LON)
elev = np.degrees(np.arcsin(np.clip(Y, -1, 1)))        # -90..90

img = np.zeros((H, W, 3), dtype=np.float32)


def lerp(a, b, t):
    return a + (b - a) * t[..., None]


# ---- degradado del cielo: cenit azul muy oscuro -> azul de noche -> resplandor sucio de la ciudad en el horizonte
zen = np.array([0.006, 0.012, 0.035])
mid = np.array([0.020, 0.036, 0.085])
hor = np.array([0.150, 0.115, 0.105])
below = np.array([0.020, 0.020, 0.025])
t_up = np.clip(elev / 90.0, 0, 1) ** 0.55
sky = lerp(np.broadcast_to(hor, (H, W, 3)), np.broadcast_to(mid, (H, W, 3)), np.clip(t_up * 2.0, 0, 1))
sky = lerp(sky, np.broadcast_to(zen, (H, W, 3)), np.clip(t_up * 2.0 - 1.0, 0, 1))
glow = np.exp(-np.clip(elev, 0, 90) / 6.0)                       # banda de resplandor pegada al horizonte
sky = sky + glow[..., None] * np.array([0.06, 0.035, 0.02])
ground = elev < 0
sky[ground] = below
img[:] = sky


# ---- ruido fractal sencillo para las nubes (valor + interpolacion bilineal, sin dependencias)
def value_noise(shape, cells, seed):
    r = np.random.default_rng(seed)
    gy, gx = cells
    g = r.random((gy + 1, gx + 1)).astype(np.float32)
    g[:, -1] = g[:, 0]                                            # que cierre en horizontal
    ys = np.linspace(0, gy, shape[0], endpoint=False)
    xs = np.linspace(0, gx, shape[1], endpoint=False)
    y0 = ys.astype(int)
    x0 = xs.astype(int)
    fy = (ys - y0)[:, None]
    fx = (xs - x0)[None, :]
    fy = fy * fy * (3 - 2 * fy)
    fx = fx * fx * (3 - 2 * fx)
    a = g[y0][:, x0]
    b = g[y0][:, x0 + 1]
    c = g[y0 + 1][:, x0]
    d = g[y0 + 1][:, x0 + 1]
    return a * (1 - fx) * (1 - fy) + b * fx * (1 - fy) + c * (1 - fx) * fy + d * fx * fy


def fbm(shape, base, octaves, seed):
    total = np.zeros(shape, dtype=np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        total += amp * value_noise(shape, (base[0] * 2 ** o, base[1] * 2 ** o), seed + o)
        norm += amp
        amp *= 0.55
    return total / norm


# ---- luna: disco con mares y bordes suaves, halo ancho
cosang = np.clip(X * MOON_DIR[0] + Y * MOON_DIR[1] + Z * MOON_DIR[2], -1, 1)
ang = np.degrees(np.arccos(cosang))
moon_r = 2.9                                                      # radio angular exagerado (la luna real mide 0,25 grados)
disk = np.clip((moon_r - ang) / 0.25, 0, 1)
tex = fbm((H, W), (8, 16), 5, 11)
fine_tex = fbm((H, W), (60, 120), 4, 33)
maria = np.clip((tex - 0.5) * 5.0, -0.6, 0.6) - np.clip((fine_tex - 0.62) * 4.0, 0, 0.5) * 0.7
moon_col = np.array([0.95, 0.93, 0.85])
limb = np.clip(1.0 - (ang / moon_r) ** 6 * 0.35, 0.6, 1.0)                      # oscurecimiento del borde
moon_light = (0.80 + 0.55 * maria)[..., None] * limb[..., None] * moon_col
halo = np.exp(-ang / 7.0) * 0.20 + np.exp(-ang / 30.0) * 0.05
img += halo[..., None] * np.array([0.40, 0.46, 0.62])
img = img * (1 - disk[..., None]) + moon_light * disk[..., None] * 1.15

# ---- nubes: bandas bajas y alargadas, iluminadas por la luna (mas claras cerca de ella) y teñidas por el resplandor abajo
cl = fbm((H, W), (6, 12), 6, 100)
cl2 = fbm((H, W), (14, 28), 4, 200)
band = np.exp(-((elev - 24.0) / 22.0) ** 2)
cover = np.clip((cl * 0.75 + cl2 * 0.25 - 0.52) * 3.2, 0, 1) * band * (elev > -2)
lit = np.exp(-ang / 38.0)
cloud_col = np.array([0.045, 0.055, 0.085])[None, None, :] + lit[..., None] * np.array([0.22, 0.24, 0.30]) + glow[..., None] * np.array([0.12, 0.07, 0.04])
img = img * (1 - 0.8 * cover[..., None]) + cloud_col * 0.8 * cover[..., None]

# ---- estrellas: mas densas hacia arriba, tapadas por las nubes y por el brillo de la luna
n = 3400
sv = rng.normal(size=(n, 3))
sv /= np.linalg.norm(sv, axis=1)[:, None]
sv = sv[sv[:, 1] > 0.02]
mag = rng.random(len(sv)) ** 5.0                                 # pocas brillantes
lat = np.degrees(np.arcsin(sv[:, 1]))
tint = rng.random(len(sv))[:, None]
scol = np.array([0.8, 0.9, 1.0]) * (1 - tint) + np.array([1.0, 0.88, 0.7]) * tint
stars = np.zeros((H, W, 3), dtype=np.float32)
for i in range(len(sv)):
    x, y, z = sv[i]
    l = math.atan2(z, x)
    c = math.acos(y)
    px = (0.5 - l / (2 * math.pi)) * W
    py = (1.0 - c / math.pi) * H                                     # fila de la matriz (la primera es la de abajo)
    size = 0.6 + 2.0 * mag[i]
    sx = size / max(math.sin(c), 0.12)                              # los polos del panorama estiran horizontalmente
    r = int(math.ceil(max(sx, size) * 2.5)) + 1
    y0, y1 = int(py) - r, int(py) + r + 1
    if y0 < 0 or y1 >= H:
        continue
    xs = (np.arange(int(px) - r, int(px) + r + 1)) % W
    ys = np.arange(y0, y1)
    dx = (np.arange(int(px) - r, int(px) + r + 1) + 0.5 - px) / sx
    dy = (ys + 0.5 - py) / size
    g = np.exp(-(dx[None, :] ** 2 + dy[:, None] ** 2) * 1.6)
    bright = 0.3 + 3.4 * mag[i]
    stars[y0:y1][:, xs] += (g * bright)[..., None] * scol[i]
fade = np.clip(1.0 - cover * 1.3, 0, 1) * np.clip(1.0 - np.exp(-np.clip(elev, 0, 90) / 14.0) * 1.0, 0, 1) * np.clip((ang - moon_r * 3) / 25.0, 0, 1)
img += stars * fade[..., None] * 0.9

img = np.clip(img, 0, 1.2)
# a sRGB para el PNG (Unity lo importa como color sRGB)
srgb = np.where(img <= 0.0031308, img * 12.92, 1.055 * np.power(np.clip(img, 0, None), 1 / 2.4) - 0.055)
srgb = srgb + (rng.random(srgb.shape).astype(np.float32) - 0.5) * (2.2 / 255.0)       # ruido de dither: sin bandas en el degradado oscuro
srgb = np.clip(srgb, 0, 1)
rgba = np.ones((H, W, 4), dtype=np.float32)
rgba[..., :3] = srgb                                              # la primera fila de Blender es la de abajo (v = 0): ya va en ese orden
im = bpy.data.images.new("NightSky", W, H, alpha=False, float_buffer=False)
im.colorspace_settings.name = "Non-Color"                           # los valores ya son sRGB: que Blender no los toque al guardar
im.pixels.foreach_set(rgba.ravel())
im.filepath_raw = out
im.file_format = "PNG"
im.save()
print("CIELO", out, W, H, "luna en", np.round(MOON_DIR, 3).tolist(), flush=True)
