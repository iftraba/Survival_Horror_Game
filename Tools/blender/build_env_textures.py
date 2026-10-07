"""Texturas del escenario v2 (suelo, paredes, techo, madera de puertas, metal, columnas de hormigon), tileables, con numpy.

    blender --background --python build_env_textures.py -- <carpeta Assets/_Project/Art/Textures>

Sustituye a las de build_textures.py (que se queda para las manchas de sangre). Cada textura saca:
  tex_X.png     color (sRGB)
  tex_X_n.png   normales (OpenGL, la de Unity)
  tex_X_ms.png  metal en RGB y suavidad en el alfa (mapa _MetallicGlossMap de URP Lit)
  tex_X_ao.png  oclusion
Se mantiene la escala de las de antes (el suelo cubre 4 m con 8x8 baldosas, el techo 2,4 m con 4x4 placas, la pared y la
columna 3 m, la madera 8 tablas), asi que no hay que tocar las UV del escenario.
"""
import math
import os
import sys

import bpy
import numpy as np

rng = np.random.default_rng(7177)


# ------------------------------------------------------------------ utilidades
def spectral(n, beta=2.0, ay=1.0, ax=1.0):
    """Ruido tileable 1/f^beta normalizado. ay grande => varia poco a lo largo de y (vetas verticales)."""
    F = np.fft.fft2(rng.standard_normal((n, n)))
    fy = np.fft.fftfreq(n)[:, None] * ay
    fx = np.fft.fftfreq(n)[None, :] * ax
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    F *= 1.0 / f ** (beta / 2.0)
    F[0, 0] = 0
    out = np.real(np.fft.ifft2(F))
    return (out - out.mean()) / (out.std() + 1e-9)


def gblur(a, s):
    """Desenfoque gaussiano tileable (sigma en pixeles)."""
    n0, n1 = a.shape
    fy = np.fft.fftfreq(n0)[:, None]
    fx = np.fft.fftfreq(n1)[None, :]
    g = np.exp(-2.0 * (math.pi * s) ** 2 * (fx * fx + fy * fy))
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g))


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    if np.ndim(t) == 2:
        t = t[..., None]
    return a + (b - a) * t


def rgb(n, c):
    return np.zeros((n, n, 3)) + np.array(c)[None, None, :]


def normal_map(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny, nz = -dx * strength, dy * strength, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / ln * 0.5 + 0.5, ny / ln * 0.5 + 0.5, nz / ln * 0.5 + 0.5], axis=-1)


def walk(mask, n, steps, turn=0.45, width=0, start=None, step=2.0, value=1.0):
    """Paseo aleatorio (grietas). Tileable por modulo."""
    y, x = start if start else (rng.uniform(0, n), rng.uniform(0, n))
    ang = rng.uniform(0, 2 * math.pi)
    pts = []
    for _ in range(steps):
        ang += rng.normal(0, turn)
        y += math.sin(ang) * step
        x += math.cos(ang) * step
        pts.append((y, x))
        if rng.random() < 0.012:                                             # rama
            walk(mask, n, steps // 3, turn, max(0, width - 1), (y, x), step, value * 0.8)
    for (y, x) in pts:
        for dy in range(-width, width + 1):
            for dx in range(-width, width + 1):
                mask[int(y + dy) % n, int(x + dx) % n] = max(mask[int(y + dy) % n, int(x + dx) % n], value)


def scratches(n, count, length=(20, 160), straight=True, angle=None):
    """Aranazos finos casi rectos."""
    m = np.zeros((n, n))
    for _ in range(count):
        y0, x0 = rng.uniform(0, n, 2)
        a = angle + rng.normal(0, 0.25) if angle is not None else rng.uniform(0, 2 * math.pi)
        L = rng.uniform(*length)
        t = np.linspace(0, L, int(L * 2))
        bend = rng.normal(0, 0.002) * t * t if straight else 0
        ys = (y0 + np.sin(a) * t + np.cos(a) * bend).astype(int) % n
        xs = (x0 + np.cos(a) * t - np.sin(a) * bend).astype(int) % n
        fade = np.sin(np.linspace(0, math.pi, len(t))) * rng.uniform(0.4, 1.0)
        np.maximum.at(m, (ys, xs), fade)
    return m


def flecks(n, sigma, thr):
    """Motas: ruido blanco suavizado y umbral (positivo -> claro, negativo -> oscuro)."""
    w = gblur(rng.standard_normal((n, n)), sigma)
    w /= w.std() + 1e-9
    return smoothstep(thr, thr + 0.4, w), smoothstep(thr, thr + 0.4, -w)


def cavity(h, s=4.0):
    """Cuanto mas hundido que su entorno esta un punto (para la oclusion y la suciedad)."""
    return np.clip(gblur(h, s) - h, 0, None)


def down(a, k):
    """Reduce la resolucion k veces (media por bloques)."""
    if k == 1:
        return a
    n = a.shape[0] // k
    if a.ndim == 2:
        return a.reshape(n, k, n, k).mean(axis=(1, 3))
    return a.reshape(n, k, n, k, a.shape[2]).mean(axis=(1, 3))


def save_png(path, arr, srgb=True):
    if arr.ndim == 2:
        arr = np.stack([arr, arr, arr], axis=-1)
    h, w = arr.shape[:2]
    if arr.shape[2] == 3:
        arr = np.concatenate([arr, np.ones((h, w, 1))], axis=2)
    arr = np.clip(np.flipud(arr), 0, 1).astype(np.float32)               # Blender: origen abajo-izquierda
    img = bpy.data.images.new("tmp_tex", w, h, alpha=True)
    img.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
    img.pixels.foreach_set(arr.ravel())
    img.file_format = "PNG"
    img.filepath_raw = path
    img.save()
    bpy.data.images.remove(img)


# ------------------------------------------------------------------ suelo: baldosa vinilica gastada (4 m, 8x8)
def tex_floor(n):
    tiles = 8
    ts = n // tiles
    g = max(4, n // 300)                                                     # junta ~1,3 cm
    yy, xx = np.mgrid[0:n, 0:n]
    ly, lx = yy % ts, xx % ts
    grout = (ly < g) | (lx < g)
    de = np.minimum(np.minimum(ly - g, ts - 1 - ly), np.minimum(lx - g, ts - 1 - lx)).astype(float)
    bevel = smoothstep(0, n / 512.0 * 5, de)                                 # canto redondeado de la baldosa
    tid = (yy // ts) * tiles + (xx // ts)
    checker = ((yy // ts) + (xx // ts)) % 2
    tone = rng.normal(0, 0.022, tiles * tiles)[tid]

    col = lerp(rgb(n, (0.25, 0.26, 0.245)), rgb(n, (0.19, 0.205, 0.205)), checker.astype(float))
    col *= (1 + tone)[..., None]
    col *= (1 + spectral(n, 2.6) * 0.05 + spectral(n, 1.0) * 0.015)[..., None]
    lf, df = flecks(n, n / 1400, 2.3)                                         # motas de la baldosa vinilica
    col = lerp(col, col * 1.45 + 0.03, lf * 0.8)
    col = lerp(col, col * 0.55, df * 0.8)

    # desgaste de paso: zonas mas claras y mates, con aranazos
    worn = smoothstep(0.3, 1.6, spectral(n, 3.2) + spectral(n, 1.6) * 0.3)
    col = lerp(col, col * 1.12 + 0.015, worn * 0.6)
    scr = np.clip(gblur(scratches(n, 900, (15, n / 10)), 0.5) * 1.6, 0, 1)
    col = lerp(col, col * 1.25 + 0.02, scr * 0.5)

    # baldosas rotas y esquinas desconchadas: se ve el mortero de debajo
    cr = np.zeros((n, n))
    for _ in range(5):
        t0 = rng.integers(0, tiles, 2)
        walk(cr, n, int(ts * 0.35), 0.16, 1 if n >= 2048 else 0, (t0[0] * ts + ts / 2, t0[1] * ts + ts / 2), 2.0)
    cr = np.clip(gblur(cr, 0.7) * 1.5, 0, 1)
    chip = np.zeros((n, n))
    for _ in range(14):
        ty, tx = rng.integers(0, tiles, 2)
        cy = ty * ts + (g if rng.random() < 0.5 else ts - 1)
        cx = tx * ts + (g if rng.random() < 0.5 else ts - 1)
        r = rng.uniform(0.04, 0.12) * ts
        dy = (yy - cy + n // 2) % n - n // 2
        dx = (xx - cx + n // 2) % n - n // 2
        chip = np.maximum(chip, (np.sqrt(dy * dy + dx * dx) < r * (1 + spectral(n, 1.5) * 0.25)).astype(float))
    chip *= (~grout)
    mortar = rgb(n, (0.16, 0.155, 0.14)) * (1 + spectral(n, 0.8) * 0.08)[..., None]
    col = lerp(col, mortar, chip)
    col = lerp(col, col * 0.35, cr)

    # suciedad: se acumula en las juntas y junto a los cantos; mas una capa general
    near = 1 - bevel
    dirt = np.clip(near * 0.7 + smoothstep(0.6, 2.0, spectral(n, 2.2)) * 0.6 + cr * 0.6, 0, 1)
    col = lerp(col, col * 0.55 * np.array([1.0, 0.95, 0.85]), dirt * 0.55)
    groutcol = rgb(n, (0.075, 0.072, 0.065)) * (1 + spectral(n, 1.2) * 0.12)[..., None]
    col = np.where(grout[..., None], groutcol, col)

    # charcos / zonas humedas: mas oscuras y muy brillantes
    wet = smoothstep(1.5, 2.1, spectral(n, 3.6) + spectral(n, 1.4) * 0.15)
    col = lerp(col, col * 0.7, wet * 0.8)

    h = np.where(grout, -0.2, bevel * 1.0) + spectral(n, 1.6) * 0.02 - scr * 0.08 - cr * 0.6 - chip * 0.5
    h += (lf - df) * 0.03
    smooth = 0.5 + spectral(n, 2.0) * 0.05 - worn * 0.18 - dirt * 0.2 - scr * 0.1
    smooth = np.where(grout, 0.08, smooth)
    smooth = smooth * (1 - chip) + chip * 0.12
    smooth = smooth * (1 - wet) + wet * 0.9
    metal = np.zeros((n, n))
    ao = np.clip(1 - cavity(h, n / 512.0 * 3) * 2.2 - grout * 0.35 - cr * 0.3, 0.2, 1)
    return col, h, 1.6 * n / 1024, metal, smooth, ao


# ------------------------------------------------------------------ pared: yeso pintado con humedades (3 m)
def tex_wall(n):
    s = n / 1024.0
    paint = rgb(n, (0.43, 0.425, 0.37))
    paint *= (1 + spectral(n, 3.0) * 0.05 + spectral(n, 1.8) * 0.02)[..., None]
    roller = spectral(n, 0.9) * 0.012                                        # textura de rodillo / piel de naranja
    paint += roller[..., None]
    plaster = rgb(n, (0.52, 0.51, 0.47)) * (1 + spectral(n, 1.0) * 0.06 + spectral(n, 2.4) * 0.05)[..., None]

    # pintura desconchada: deja ver el yeso, con el borde levantado
    pf = spectral(n, 2.8) + spectral(n, 1.3) * 0.35
    peel = smoothstep(1.85, 1.95, pf)
    rim = smoothstep(1.7, 1.85, pf) * (1 - peel)
    col = lerp(paint, plaster, peel)
    col = lerp(col, col * 1.08 + 0.02, rim * 0.6)

    # humedades: manchas con cerco marron
    ws = spectral(n, 3.4, ay=0.7, ax=1.0)
    wmask = smoothstep(1.5, 2.0, ws)
    tide = np.exp(-((ws - 1.55) / 0.06) ** 2)
    col = col * lerp(np.ones((n, n, 3)), np.array([0.84, 0.77, 0.63]) * np.ones((n, n, 3)), wmask * 0.7)
    col = col * lerp(np.ones((n, n, 3)), np.array([0.66, 0.57, 0.44]) * np.ones((n, n, 3)), tide * 0.5)
    mf, _ = flecks(n, 0.9 * s, 1.9)                                           # moho en las humedades
    col = lerp(col, rgb(n, (0.06, 0.07, 0.05)), mf * wmask * 0.85)

    # chorretones verticales
    drip = smoothstep(1.2, 2.4, spectral(n, 2.0, ay=28.0, ax=1.0)) * smoothstep(-0.5, 1.0, spectral(n, 2.5))
    col = lerp(col, col * np.array([0.7, 0.65, 0.55]), drip * 0.7)

    # grietas con ramas
    cr = np.zeros((n, n))
    for _ in range(3):
        walk(cr, n, int(300 * s), 0.17, 0, None, 2.5)
    cr = np.clip(gblur(cr, 0.6 * s) * 1.6, 0, 1)
    col = lerp(col, col * 0.3, cr)

    # golpes y rozaduras
    dents, _ = flecks(n, 3.0 * s, 2.6)
    scuff = np.clip(gblur(scratches(n, 120, (30 * s, 140 * s), True, 0.0), 0.8 * s) * 1.2, 0, 1)
    col = lerp(col, col * 0.75, scuff * 0.5)
    grime = smoothstep(0.2, 2.0, spectral(n, 2.2))
    col = lerp(col, col * 0.72, grime * 0.4)

    h = 0.15 * (1 - peel) + rim * 0.25 + roller * 3 + spectral(n, 2.2) * 0.12 - cr * 0.8 - dents * 0.3
    smooth = 0.3 + roller * 2 - grime * 0.08
    smooth = smooth * (1 - peel) + peel * 0.07
    smooth = smooth - wmask * 0.06 + drip * 0.15
    metal = np.zeros((n, n))
    ao = np.clip(1 - cavity(h, 3 * s) * 3 - cr * 0.4, 0.3, 1)
    return col, h, 2.4 * s, metal, smooth, ao


# ------------------------------------------------------------------ techo: placas acusticas con perfil en T (2,4 m, 4x4)
def tex_ceiling(n):
    tiles = 4
    ts = n // tiles
    g = max(6, int(n / 2.4 * 0.024))                                         # perfil de 2,4 cm
    yy, xx = np.mgrid[0:n, 0:n]
    ly, lx = (yy + g // 2) % ts, (xx + g // 2) % ts
    grid = (ly < g) | (lx < g)
    de = np.minimum(np.minimum(ly - g, ts - 1 - ly), np.minimum(lx - g, ts - 1 - lx)).astype(float)
    tid = ((yy + g // 2) // ts % tiles) * tiles + ((xx + g // 2) // ts % tiles)
    tone = rng.normal(0, 0.03, tiles * tiles)
    tone[rng.integers(0, tiles * tiles)] -= 0.08                             # una placa cambiada, mas oscura
    s = n / 1024.0

    col = rgb(n, (0.50, 0.49, 0.445)) * (1 + tone[tid])[..., None]
    col *= (1 + spectral(n, 2.6) * 0.035)[..., None]
    fis = np.clip(gblur((spectral(n, 0.3) > 1.9).astype(float), 0.8 * s) * 2.0, 0, 1)   # fisuras de la placa
    fis = np.maximum(fis, flecks(n, 0.7 * s, 2.4)[1])
    col = lerp(col, col * 0.55, fis * 0.6)

    # manchas de agua con cerco
    ws = spectral(n, 3.4)
    wmask = smoothstep(1.3, 1.8, ws)
    tide = np.exp(-((ws - 1.35) / 0.07) ** 2)
    col = col * lerp(np.ones((n, n, 3)), np.array([0.8, 0.7, 0.52]) * np.ones((n, n, 3)), wmask * 0.85)
    col = col * lerp(np.ones((n, n, 3)), np.array([0.55, 0.45, 0.3]) * np.ones((n, n, 3)), tide * 0.7)
    grime = smoothstep(0.3, 2.0, spectral(n, 2.0))
    col = lerp(col, col * 0.75, grime * 0.4)
    edge = 1 - smoothstep(0, 10 * s, de)                                    # sombra junto al perfil
    col = lerp(col, col * 0.7, edge * 0.5)

    # perfil metalico pintado, con oxido puntual
    rust = smoothstep(1.4, 2.2, spectral(n, 2.0))
    gcol = lerp(rgb(n, (0.56, 0.56, 0.53)), rgb(n, (0.33, 0.17, 0.07)), rust * 0.8)
    gcol *= (1 + spectral(n, 1.0) * 0.04)[..., None]
    col = np.where(grid[..., None], gcol, col)

    h = np.where(grid, 1.0, -0.3 + smoothstep(0, 6 * s, de) * 0.15) - fis * 0.25 + spectral(n, 1.2) * 0.03
    smooth = np.where(grid, 0.5 - rust * 0.35, 0.05 + wmask * 0.05)
    metal = np.zeros((n, n))
    ao = np.clip(1 - cavity(h, 4 * s) * 1.5 - fis * 0.2, 0.3, 1)
    return col, h, 2.5 * s, metal, smooth, ao


# ------------------------------------------------------------------ madera barnizada (puertas, marcos, zocalos), 8 tablas
def tex_wood(n):
    planks = 8
    ph = n // planks
    yy, xx = np.mgrid[0:n, 0:n]
    pid = yy // ph
    s = n / 1024.0
    warp = spectral(n, 3.0, ay=1.0, ax=25.0) * 10 * s + spectral(n, 2.0, ay=1.0, ax=40.0) * 1.5 * s
    phase = rng.uniform(0, 2 * math.pi, planks)[pid]
    freq = rng.uniform(1.6, 3.0, planks)[pid] / (ph / 6.0)                   # anillos por pixel, distinto en cada tabla
    m = rng.integers(1, 3, planks)[pid]                                      # arcos (veta de catedral), entero: tileable en x
    arch = np.cos(2 * math.pi * m * xx / n + rng.uniform(0, 6.3, planks)[pid]) * ph * rng.uniform(0.1, 0.35, planks)[pid]
    ring = 0.5 + 0.5 * np.sin(2 * math.pi * freq * (yy % ph + warp + arch) + phase)
    ring = ring ** 2                                                         # vetas finas oscuras sobre fondo claro
    pores = spectral(n, 0.6, ay=1.0, ax=50.0)
    streaks = spectral(n, 1.6, ay=1.0, ax=60.0)                              # bandas largas de color
    tone = rng.normal(0, 0.06, planks)[pid]

    light = np.array([0.29, 0.18, 0.10])
    dark = np.array([0.15, 0.08, 0.04])
    col = lerp(rgb(n, light), rgb(n, dark), np.clip(ring * 0.3 + pores * 0.08 + streaks * 0.12, 0, 1))
    col *= (1 + tone + spectral(n, 2.4, ay=1, ax=4) * 0.05)[..., None]
    # nudos
    for _ in range(6):
        cy, cx = rng.integers(0, n), rng.integers(0, n)
        dy = (yy - cy + n // 2) % n - n // 2
        dx = (xx - cx + n // 2) % n - n // 2
        r2 = dy * dy / (14 * s) ** 2 + dx * dx / (34 * s) ** 2
        col = lerp(col, rgb(n, (0.09, 0.05, 0.025)), np.exp(-r2) * 0.85)
    seam = (yy % ph) < max(2, int(2 * s))
    # barniz gastado (mas claro y mate), aranazos y suciedad
    worn = smoothstep(0.6, 1.9, spectral(n, 2.8))
    col = lerp(col, col * 1.35 + np.array([0.03, 0.02, 0.01]), worn * 0.55)
    scr = np.clip(gblur(scratches(n, 260, (10 * s, 90 * s)), 0.5 * s) * 1.5, 0, 1)
    col = lerp(col, col * 1.6 + 0.03, scr * 0.45)
    grime = smoothstep(0.3, 2.0, spectral(n, 2.0))
    col = lerp(col, col * 0.6, grime * 0.4)
    col = np.where(seam[..., None], rgb(n, (0.04, 0.025, 0.015)), col)

    h = -ring * 0.15 + pores * 0.05 - scr * 0.12 - seam * 1.0
    smooth = 0.5 - worn * 0.25 - ring * 0.05 - scr * 0.15 - grime * 0.12
    smooth = np.where(seam, 0.1, smooth)
    metal = np.zeros((n, n))
    ao = np.clip(1 - seam * 0.7 - cavity(h, 3 * s) * 2, 0.3, 1)
    return col, h, 2.0 * s, metal, smooth, ao


# ------------------------------------------------------------------ metal: chapa pintada con desconchones y oxido
def tex_metal(n):
    s = n / 1024.0
    paint = rgb(n, (0.20, 0.235, 0.225)) * (1 + spectral(n, 2.6) * 0.05 + spectral(n, 0.8) * 0.015)[..., None]
    brush = spectral(n, 1.2, ay=1.0, ax=60.0)
    steel = rgb(n, (0.56, 0.56, 0.57)) * (1 + brush * 0.06)[..., None]
    chipf = spectral(n, 2.2) + spectral(n, 0.9) * 0.4
    chip = smoothstep(1.75, 1.85, chipf)
    scr = np.clip(gblur(scratches(n, 400, (10 * s, 120 * s)), 0.5 * s) * 1.6, 0, 1)
    bare = np.clip(chip + scr * 0.8, 0, 1)
    rustf = spectral(n, 2.8) + spectral(n, 1.0) * 0.4
    rust = smoothstep(1.0, 1.8, rustf) * (0.4 + 0.6 * smoothstep(1.5, 1.9, chipf))
    rustcol = lerp(rgb(n, (0.20, 0.08, 0.03)), rgb(n, (0.42, 0.20, 0.07)), smoothstep(-1, 1, spectral(n, 1.0)))
    streak = smoothstep(1.0, 2.2, spectral(n, 2.0, ay=24.0, ax=1.0))
    col = lerp(paint, steel, bare)
    col = lerp(col, rustcol, np.clip(rust + streak * 0.35, 0, 1))
    grime = smoothstep(0.3, 2.0, spectral(n, 2.0))
    col = lerp(col, col * 0.6, grime * 0.4)

    h = (1 - chip) * 0.25 - scr * 0.1 + rust * spectral(n, 0.8) * 0.15 + brush * 0.01 * bare
    metal = np.clip(bare * (1 - rust), 0, 1)
    smooth = 0.42 - grime * 0.12
    smooth = smooth * (1 - bare) + bare * (0.6 + brush * 0.04)
    smooth = smooth * (1 - rust) + rust * 0.12
    ao = np.clip(1 - cavity(h, 3 * s) * 2.5, 0.4, 1)
    return col, h, 2.4 * s, metal, smooth, ao


# ------------------------------------------------------------------ columnas: hormigon visto (3 m)
def tex_concrete(n):
    s = n / 1024.0
    yy, xx = np.mgrid[0:n, 0:n]
    col = rgb(n, (0.40, 0.395, 0.375))
    col *= (1 + spectral(n, 2.8) * 0.06 + spectral(n, 1.6) * 0.03 + spectral(n, 0.5) * 0.02)[..., None]
    la, da = flecks(n, 0.8 * s, 2.0)                                         # arido
    col = lerp(col, col * 1.25, la * 0.5)
    col = lerp(col, col * 0.7, da * 0.5)
    # coqueras (burbujas de aire): poros oscuros y hundidos
    pits = flecks(n, 1.6 * s, 2.8)[0]
    col = lerp(col, col * 0.35, pits)
    # juntas del encofrado cada metro, con rebaba
    rowv = (yy % (n // 3)).astype(float)
    form = np.exp(-(rowv / (1.2 * s)) ** 2) + np.exp(-((n // 3 - rowv) / (1.2 * s)) ** 2)
    lip = np.exp(-((rowv - 3 * s) / (1.5 * s)) ** 2)
    col = lerp(col, col * 0.78, form * 0.5)
    # tablas del encofrado: bandas horizontales muy suaves
    boards = spectral(n, 2.0, ay=1.0, ax=30.0)
    col *= (1 + boards * 0.03)[..., None]
    # escorrentias, eflorescencias, desconchones en la esquina y suciedad
    drip = smoothstep(1.0, 2.3, spectral(n, 2.0, ay=26.0, ax=1.0))
    col = lerp(col, col * np.array([0.62, 0.6, 0.55]), drip * 0.6)
    efl = smoothstep(1.4, 2.2, spectral(n, 2.6))
    col = lerp(col, rgb(n, (0.6, 0.6, 0.57)), efl * 0.35)
    chipf = spectral(n, 2.4) + spectral(n, 1.0) * 0.3
    chip = smoothstep(1.95, 2.05, chipf)
    col = lerp(col, col * 0.8 + np.array([0.02, 0.02, 0.015]), chip)
    grime = smoothstep(0.3, 2.0, spectral(n, 2.0))
    col = lerp(col, col * 0.7, grime * 0.35)
    cr = np.zeros((n, n))
    for _ in range(3):
        walk(cr, n, int(260 * s), 0.17, 0, None, 2.5)
    cr = np.clip(gblur(cr, 0.6 * s) * 1.6, 0, 1)
    col = lerp(col, col * 0.35, cr)

    h = spectral(n, 1.8) * 0.06 + (la - da) * 0.03 - pits * 0.5 - form * 0.2 + lip * 0.08 - chip * 0.4 - cr * 0.6 + boards * 0.03
    smooth = 0.14 - grime * 0.05 + drip * 0.06 - pits * 0.08
    metal = np.zeros((n, n))
    ao = np.clip(1 - cavity(h, 3 * s) * 2.5, 0.3, 1)
    return col, h, 3.0 * s, metal, smooth, ao


# resolucion del color/normales y factor de reduccion del metal/oclusion
JOBS = (
    ("tex_floor", tex_floor, 2048, 2),
    ("tex_wall", tex_wall, 2048, 2),
    ("tex_concrete", tex_concrete, 2048, 2),
    ("tex_ceiling", tex_ceiling, 1024, 1),
    ("tex_wood", tex_wood, 1024, 1),
    ("tex_metal", tex_metal, 1024, 1),
)


def build(out, only=None):
    os.makedirs(out, exist_ok=True)
    for name, fn, n, k in JOBS:
        if only and name not in only:
            continue
        col, h, strength, metal, smooth, ao = fn(n)
        save_png(os.path.join(out, name + ".png"), np.clip(col, 0, 1), True)
        save_png(os.path.join(out, name + "_n.png"), normal_map(h, strength), False)
        ms = np.concatenate([np.stack([metal] * 3, axis=-1), np.clip(smooth, 0, 1)[..., None]], axis=-1)
        save_png(os.path.join(out, name + "_ms.png"), down(ms, k), False)
        save_png(os.path.join(out, name + "_ao.png"), down(ao, k), False)
        print("hecha", name, n, flush=True)


if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:]
    build(args[0], args[1:] or None)
    print("PROCESO_TERMINADO")
