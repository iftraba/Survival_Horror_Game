"""Texturas procedurales (sin imagenes externas) con numpy; se guardan como PNG con el motor de imagenes de Blender.

    exec(open(r"<ruta>/build_textures.py").read())
    build_textures(r"<carpeta Assets/_Project/Art/Textures>")

Todas son 'tileables' (el ruido se genera en el dominio de frecuencia, que es periodico por construccion).
Cada textura de albedo trae su mapa de normales (_n).
"""
import math

import bpy
import numpy as np

rng = np.random.default_rng(424242)


# ------------------------------------------------------------------ ruido
def spectral_noise(n, beta=2.0, ay=1.0, ax=1.0):
    """Ruido tileable 1/f^beta, normalizado. Eje 0 = filas (y), eje 1 = columnas (x).
    ay grande => el ruido varia poco a lo largo de y (vetas verticales)."""
    r = rng.standard_normal((n, n))
    F = np.fft.fft2(r)
    fy = np.fft.fftfreq(n)[:, None] * ay
    fx = np.fft.fftfreq(n)[None, :] * ax
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    F *= 1.0 / f ** (beta / 2.0)
    F[0, 0] = 0
    out = np.real(np.fft.ifft2(F))
    return (out - out.mean()) / (out.std() + 1e-9)


def blur(a, k=1):
    """Suavizado tileable (media con vecinos, k veces)."""
    for _ in range(k):
        a = (a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 5.0
    return a


def normal_map(h, strength=2.0):
    """Mapa de normales (convencion OpenGL, la de Unity) a partir de un mapa de alturas tileable."""
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny, nz = -dx * strength, dy * strength, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / ln * 0.5 + 0.5, ny / ln * 0.5 + 0.5, nz / ln * 0.5 + 0.5], axis=-1)


def crack(mask, n, steps=140, width=1, seed_pos=None):
    """Dibuja una grieta (paseo aleatorio) sobre 'mask' con valor 1. Tileable por modulo."""
    y, x = seed_pos if seed_pos else (rng.integers(0, n), rng.integers(0, n))
    ang = rng.uniform(0, 2 * math.pi)
    for _ in range(steps):
        ang += rng.normal(0, 0.45)
        y = int(y + math.sin(ang) * 2) % n
        x = int(x + math.cos(ang) * 2) % n
        for dy in range(-width, width + 1):
            for dx in range(-width, width + 1):
                mask[(y + dy) % n, (x + dx) % n] = 1.0


def save_png(path, arr, srgb=True):
    h, w = arr.shape[:2]
    if arr.shape[2] == 3:
        arr = np.concatenate([arr, np.ones((h, w, 1))], axis=2)
    arr = np.clip(np.flipud(arr), 0, 1).astype(np.float32)   # Blender: origen abajo-izquierda
    img = bpy.data.images.new("tmp_tex", w, h, alpha=True)
    img.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
    img.pixels.foreach_set(arr.ravel())
    img.file_format = "PNG"
    img.filepath_raw = path
    img.save()
    bpy.data.images.remove(img)


# ------------------------------------------------------------------ texturas
def _pack(col, h, strength, smooth, ao):
    """Devuelve (albedo RGBA con el brillo en el canal alfa, mapa de normales, mapa de oclusion)."""
    albedo = np.concatenate([np.clip(col, 0, 1), np.clip(smooth, 0, 1)[..., None]], axis=-1)
    return albedo, normal_map(h, strength), np.clip(ao, 0, 1)


def tex_floor(n=512):
    # A 1024 px el suelo cubre 4 m con baldosas de 0.5 m: la suciedad se repite cada 4 m en vez de cada 2 m
    tiles, grout = (8, 6) if n >= 1024 else (4, 5)
    ts = n // tiles
    yy, xx = np.mgrid[0:n, 0:n]
    gy = (yy % ts) < grout
    gx = (xx % ts) < grout
    is_grout = gy | gx
    tile_id = (yy // ts) * tiles + (xx // ts)
    tone = rng.normal(0, 0.035, tiles * tiles)[tile_id]
    base = np.array([0.20, 0.22, 0.22])
    col = np.zeros((n, n, 3)) + base
    col += tone[..., None]
    grime = spectral_noise(n, 2.6) * 0.045 + spectral_noise(n, 1.2) * 0.02
    col += grime[..., None]
    col += (spectral_noise(n, 0.2) * 0.012)[..., None]                       # grano fino
    dirt = np.clip(spectral_noise(n, 3.0) - 0.8, 0, 1) * 0.10                # manchas oscuras (menos y mas tenues)
    col -= dirt[..., None]
    col[is_grout] = np.array([0.07, 0.075, 0.07]) + grime[is_grout][:, None] * 0.3
    scr = np.zeros((n, n))
    for _ in range(10):
        crack(scr, n, steps=60, width=0)
    col -= (scr * 0.10)[..., None]
    h = np.where(is_grout, 0.0, 1.0)
    h = blur(h, 2) + spectral_noise(n, 1.8) * 0.03 - scr * 0.25
    # Brillo (alfa): baldosa limpia semibrillante, juntas y suciedad mates. Oclusion: juntas y grietas oscuras
    base_h = np.where(is_grout, 0.0, 1.0)
    smooth = np.where(is_grout, 0.10, 0.52 + spectral_noise(n, 2.0) * 0.07 - dirt * 2.5 - scr * 0.3)
    occ = np.clip(blur(base_h, 3) - base_h, 0, 1)
    ao = np.clip(1.0 - np.where(is_grout, 0.55, 0.0) - occ * 0.9 - scr * 0.3 - dirt * 0.30, 0.25, 1.0)
    return _pack(col, h, 3.0, smooth, ao)


def tex_wall(n=512):
    base = np.array([0.46, 0.44, 0.39])
    col = np.zeros((n, n, 3)) + base
    col += (spectral_noise(n, 3.0) * 0.045)[..., None]                       # moteado grande
    streaks = spectral_noise(n, 2.2, ay=14.0, ax=1.0)                        # vetas de humedad verticales
    col -= (np.clip(streaks, 0, 3) * 0.035)[..., None]
    stain = np.clip(spectral_noise(n, 3.2) - 1.0, 0, 2)                      # manchas marrones
    col -= stain[..., None] * np.array([0.05, 0.08, 0.12])[None, None, :]
    col += (spectral_noise(n, 0.3) * 0.02)[..., None]
    cr = np.zeros((n, n))
    for _ in range(4):
        crack(cr, n, steps=170, width=0)
    col -= (blur(cr, 1) * 0.30)[..., None]
    h = spectral_noise(n, 2.4) * 0.5 + spectral_noise(n, 0.4) * 0.15 - cr * 1.0
    wet = np.clip(streaks - 0.8, 0, 2)                                       # las vetas de humedad brillan un poco
    smooth = np.clip(0.10 + wet * 0.12 + spectral_noise(n, 2.5) * 0.03 - stain * 0.05, 0.04, 0.5)
    ao = np.clip(1.0 - blur(cr, 2) * 2.5 - np.clip(stain, 0, 1) * 0.15, 0.3, 1.0)
    return _pack(col, h, 1.6, smooth, ao)


def tex_wood(n=512):
    planks = 8
    ph = n // planks
    yy, xx = np.mgrid[0:n, 0:n]
    pid = yy // ph
    tone = rng.normal(0, 0.03, planks)[pid]
    grain = spectral_noise(n, 2.0, ay=1.0, ax=22.0)                          # veta horizontal (varia poco en x)
    fine = spectral_noise(n, 1.0, ay=1.0, ax=10.0)
    base = np.array([0.26, 0.15, 0.08])
    col = np.zeros((n, n, 3)) + base + tone[..., None]
    col += (grain * 0.045 + fine * 0.02)[..., None] * np.array([1.0, 0.7, 0.4])[None, None, :]
    seam = (yy % ph) < 3
    col[seam] = np.array([0.05, 0.03, 0.02])
    for _ in range(5):                                                       # nudos
        cy, cx = rng.integers(0, n), rng.integers(0, n)
        r2 = ((yy - cy + n // 2) % n - n // 2) ** 2 / 36.0 + ((xx - cx + n // 2) % n - n // 2) ** 2 / 160.0
        col -= (np.exp(-r2) * 0.10)[..., None]
    h = grain * 0.3 + fine * 0.1 - seam * 1.0
    smooth = np.clip(0.28 + grain * 0.04 + fine * 0.02 - seam * 0.2, 0.05, 0.6)
    ao = np.clip(1.0 - seam * 0.8, 0.3, 1.0)
    return _pack(col, h, 1.8, smooth, ao)


def tex_ceiling(n=512):
    tiles, grout = 4, 4
    ts = n // tiles
    yy, xx = np.mgrid[0:n, 0:n]
    is_grout = ((yy % ts) < grout) | ((xx % ts) < grout)
    col = np.zeros((n, n, 3)) + np.array([0.40, 0.40, 0.37])
    col += (spectral_noise(n, 2.5) * 0.03)[..., None]
    holes = (spectral_noise(n, 0.0) > 1.7).astype(float)                     # puntitos de placa acustica
    col -= (holes * 0.10)[..., None]
    stain = np.clip(spectral_noise(n, 3.2) - 0.9, 0, 2)
    col -= stain[..., None] * np.array([0.06, 0.09, 0.13])[None, None, :]
    col[is_grout] = np.array([0.12, 0.12, 0.11])
    h = np.where(is_grout, 0.0, 1.0)
    h = blur(h, 1) - holes * 0.2
    smooth = np.where(is_grout, 0.04, np.clip(0.07 + stain * 0.03, 0.03, 0.3))
    ao = np.clip(1.0 - holes * 0.25 - np.where(is_grout, 0.5, 0.0), 0.3, 1.0)
    return _pack(col, h, 2.0, smooth, ao)


def tex_metal(n=512):
    col = np.zeros((n, n, 3)) + np.array([0.30, 0.31, 0.33])
    brush = spectral_noise(n, 1.5, ay=1.0, ax=20.0)
    col += (brush * 0.03)[..., None]
    rust = np.clip(spectral_noise(n, 3.0) - 0.7, 0, 2)
    col = col * (1 - np.clip(rust, 0, 1)[..., None] * 0.7) + (np.clip(rust, 0, 1)[..., None] * 0.7) * np.array([0.38, 0.19, 0.08])[None, None, :]
    sc = np.zeros((n, n))
    for _ in range(14):
        crack(sc, n, steps=50, width=0)
    col += (sc * 0.06)[..., None]
    h = brush * 0.15 + rust * 0.3
    smooth = np.clip(0.55 + brush * 0.04 - np.clip(rust, 0, 1) * 0.42, 0.08, 0.8)   # metal limpio brilla, el oxido no
    ao = np.clip(1.0 - np.clip(rust, 0, 1) * 0.3, 0.4, 1.0)
    return _pack(col, h, 1.4, smooth, ao)


def decal_blood(n, seed):
    local = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    acc = np.zeros((n, n))
    for _ in range(local.integers(4, 7)):                                    # manchas principales
        cy, cx = local.uniform(0.3, 0.7, 2) * n
        r = local.uniform(0.06, 0.16) * n
        acc += np.exp(-(((yy - cy) ** 2 + (xx - cx) ** 2) / (2 * r * r))) * local.uniform(0.7, 1.2)
    for _ in range(local.integers(25, 45)):                                  # gotas
        ang = local.uniform(0, 2 * math.pi)
        dist = local.uniform(0.15, 0.46) * n
        cy, cx = n / 2 + math.sin(ang) * dist, n / 2 + math.cos(ang) * dist
        r = local.uniform(0.006, 0.026) * n
        acc += np.exp(-(((yy - cy) ** 2 + (xx - cx) ** 2) / (2 * r * r))) * 1.4
    noise_edge = spectral_noise(n, 2.0) * 0.12
    alpha = np.clip((acc + noise_edge - 0.55) * 6.0, 0, 1)
    shade = np.clip(acc * 0.35, 0, 1)
    rgb = np.stack([0.22 + 0.18 * (1 - shade), 0.012 + 0.02 * (1 - shade), 0.012 + 0.015 * (1 - shade)], axis=-1)
    return np.concatenate([rgb, alpha[..., None]], axis=-1)


def build_textures(out):
    import os
    os.makedirs(out, exist_ok=True)
    done = []
    for name, fn in (("tex_floor", tex_floor), ("tex_wall", tex_wall), ("tex_wood", tex_wood),
                     ("tex_ceiling", tex_ceiling), ("tex_metal", tex_metal)):
        albedo, nrm, ao = fn(1024 if name == "tex_floor" else 512)
        save_png(os.path.join(out, name + ".png"), albedo, srgb=True)
        save_png(os.path.join(out, name + "_n.png"), nrm, srgb=False)
        save_png(os.path.join(out, name + "_ao.png"), np.stack([ao, ao, ao], axis=-1), srgb=False)
        done.append(name)
    for i in range(3):
        save_png(os.path.join(out, f"decal_blood{i + 1}.png"), decal_blood(256, 100 + i * 7), srgb=True)
        done.append(f"decal_blood{i + 1}")
    return done
