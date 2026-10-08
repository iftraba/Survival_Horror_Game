"""Texturas de la ciudad (comisaria v2, fase 2): fachadas con ventanas, asfalto y acera. Tileables, con numpy.

    blender --background --python build_city_textures.py -- <carpeta Assets/_Project/Art/Textures>

Usa las utilidades de build_env_textures.py. Salen color, normales, metal/suavidad (_ms) y oclusion (_ao) como las del
escenario; la fachada ademas trae _emis (ventanas encendidas, pocas: la ciudad esta a oscuras y en caos).
Escala: fachada 6 m (dos pisos y tres ventanas por textura), asfalto y acera 4 m.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, "build_env_textures.py"), encoding="utf-8").read().split('if __name__ == "__main__":')[0]
exec(_src)


def tex_facade(n, seed):
    """Ladrillo con dos pisos de ventanas (3 por piso). Algunas encendidas, otras rotas o tapiadas."""
    local = np.random.default_rng(seed)
    s = n / 1024.0
    yy, xx = np.mgrid[0:n, 0:n]
    # ladrillo: hiladas de 7,5 cm en 6 m -> 80 hiladas, juntas
    rows = 80
    rh = n / rows
    row = (yy // rh).astype(int)
    off = (row % 2) * (n / 24 / 2)
    bw = n / 24
    bx = ((xx + off) % bw)
    joint = ((yy % rh) < max(1, rh * 0.18)) | (bx < max(1, bw * 0.06))
    tone = local.normal(0, 0.06, (rows, 26))[row % rows, (((xx + off) // bw).astype(int)) % 26]
    base = np.array([0.36, 0.17, 0.11]) if seed % 2 else np.array([0.32, 0.29, 0.26])
    col = rgb(n, base) * (1 + tone + spectral(n, 2.4) * 0.08)[..., None]
    col = np.where(joint[..., None], rgb(n, (0.22, 0.21, 0.2)), col)
    grime = smoothstep(0.0, 2.0, spectral(n, 2.6, ay=6.0, ax=1.0))                        # churretes verticales
    col = lerp(col, col * 0.45, grime * 0.6)
    h = np.where(joint, -0.4, 0.0) + spectral(n, 1.0) * 0.05
    emis = np.zeros((n, n, 3))
    smooth = np.full((n, n), 0.12)
    # ventanas: 3 x 2 por textura
    for fy in range(2):
        for fx in range(3):
            cx, cy = (fx + 0.5) * n / 3, (fy + 0.45) * n / 2
            w, hgt = n / 3 * 0.42, n / 2 * 0.5
            inside = (np.abs(xx - cx) < w / 2) & (np.abs(yy - cy) < hgt / 2)
            frame = inside & ~((np.abs(xx - cx) < w / 2 - 6 * s) & (np.abs(yy - cy) < hgt / 2 - 6 * s))
            mull = inside & ((np.abs(xx - cx) < 3 * s) | (np.abs(yy - cy) < 3 * s))
            sill = (np.abs(xx - cx) < w / 2 + 8 * s) & (yy > cy + hgt / 2) & (yy < cy + hgt / 2 + 10 * s)
            kind = local.random()
            if kind < 0.18:
                glass = rgb(n, (0.9, 0.7, 0.35))                                               # encendida (luz calida)
                emis = np.where(inside[..., None] & ~frame[..., None] & ~mull[..., None], np.array([1.4, 0.9, 0.45]), emis)
            elif kind < 0.3:
                glass = rgb(n, (0.25, 0.18, 0.12))                                             # tapiada con tablas
            else:
                glass = rgb(n, (0.04, 0.05, 0.06)) * (1 + spectral(n, 1.0) * 0.2)[..., None]  # oscura
            col = np.where(inside[..., None], glass, col)
            col = np.where((frame | mull)[..., None], rgb(n, (0.12, 0.12, 0.12)), col)
            col = np.where(sill[..., None], rgb(n, (0.45, 0.44, 0.42)), col)
            smooth = np.where(inside & ~frame & ~mull, 0.85, smooth)
            h = np.where(inside, -1.0, h) + np.where(frame | mull, 0.6, 0) + np.where(sill, 0.5, 0)
    metal = np.zeros((n, n))
    ao = np.clip(1 - cavity(h, 3 * s) * 1.5, 0.3, 1)
    return col, h, 2.0 * s, metal, smooth, ao, emis


def tex_asphalt(n):
    s = n / 1024.0
    col = rgb(n, (0.09, 0.09, 0.095)) * (1 + spectral(n, 2.0) * 0.12 + spectral(n, 0.3) * 0.08)[..., None]
    lf, df = flecks(n, 0.6 * s, 2.0)
    col = lerp(col, col * 1.8, lf * 0.6)
    cr = np.zeros((n, n))
    for _ in range(5):
        walk(cr, n, int(240 * s), 0.4, 0, None, 2.0)
    cr = np.clip(gblur(cr, 0.7 * s) * 1.5, 0, 1)
    col = lerp(col, col * 0.4, cr)
    wet = smoothstep(1.2, 2.0, spectral(n, 3.4))
    col = lerp(col, col * 0.6, wet)
    h = (lf - df) * 0.1 + spectral(n, 0.8) * 0.05 - cr * 0.6
    smooth = 0.25 + wet * 0.6
    return col, h, 2.5 * s, np.zeros((n, n)), smooth, np.clip(1 - cr * 0.4, 0.4, 1)


def tex_sidewalk(n):
    s = n / 1024.0
    tiles = 8
    ts = n // tiles
    yy, xx = np.mgrid[0:n, 0:n]
    joint = ((yy % ts) < 3 * s) | ((xx % ts) < 3 * s)
    tid = (yy // ts) * tiles + (xx // ts)
    col = rgb(n, (0.42, 0.41, 0.39)) * (1 + np.random.default_rng(3).normal(0, 0.04, tiles * tiles)[tid] + spectral(n, 2.0) * 0.06)[..., None]
    col = np.where(joint[..., None], col * 0.5, col)
    grime = smoothstep(0.3, 2.0, spectral(n, 2.4))
    col = lerp(col, col * 0.6, grime * 0.5)
    h = np.where(joint, -0.5, 0.0) + spectral(n, 1.0) * 0.04
    return col, h, 2.5 * s, np.zeros((n, n)), np.full((n, n), 0.15), np.clip(1 - joint * 0.4, 0.4, 1)


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    jobs = [("tex_facade_a", lambda n: tex_facade(n, 11)), ("tex_facade_b", lambda n: tex_facade(n, 24)),
            ("tex_asphalt", tex_asphalt), ("tex_sidewalk", tex_sidewalk)]
    for name, fn in jobs:
        res = fn(1024)
        col, h, strength, metal, smooth, ao = res[:6]
        save_png(os.path.join(out, name + ".png"), np.clip(col, 0, 1), True)
        save_png(os.path.join(out, name + "_n.png"), normal_map(h, strength), False)
        ms = np.concatenate([np.stack([metal] * 3, axis=-1), np.clip(smooth, 0, 1)[..., None]], axis=-1)
        save_png(os.path.join(out, name + "_ms.png"), ms, False)
        save_png(os.path.join(out, name + "_ao.png"), ao, False)
        if len(res) > 6:
            save_png(os.path.join(out, name + "_emis.png"), np.clip(res[6] / 1.5, 0, 1), True)
        print("hecha", name, flush=True)
    print("PROCESO_TERMINADO")
