"""Texturas de las banderas y del mapa de la sala de conferencias (comisaria v2, fase 3), con numpy.

    blender --background --python build_flag_map_textures.py -- <carpeta Assets/_Project/Art/Textures>

  tex_flag_police  azul marino con franja dorada y escudo de la policia del distrito (estrella de seis puntas)
  tex_flag_city    bandera de la ciudad inventada: franjas granate y blanca con torre y "7"
  tex_city_map     plano de la ciudad: manzanas, avenidas, rio, parque; la comisaria marcada en rojo y chinchetas
Banderas 1024 x 640 (proporcion 1,6). Mapa 2048 x 1280.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_env_textures.py"), encoding="utf-8").read().split('if __name__ == "__main__":')[0])


def save_rect(path, arr, srgb=True):
    h, w = arr.shape[:2]
    if arr.shape[2] == 3:
        arr = np.concatenate([arr, np.ones((h, w, 1))], axis=2)
    arr = np.clip(np.flipud(arr), 0, 1).astype(np.float32)
    img = bpy.data.images.new("tmp_rect", w, h, alpha=True)
    img.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
    img.pixels.foreach_set(arr.ravel())
    img.file_format = "PNG"; img.filepath_raw = path; img.save()
    bpy.data.images.remove(img)


def star(xx, yy, cx, cy, r_out, r_in, points):
    """Estrella de lados rectos: en cada sector, el borde es el segmento de la punta (r_out) al valle (r_in)."""
    a = np.arctan2(yy - cy, xx - cx) + math.pi / 2; d = np.hypot(xx - cx, yy - cy)
    sector = 2 * math.pi / points
    th = np.abs(((a % sector) + sector) % sector - sector / 2)        # 0 en el valle, sector/2 en la punta
    th = sector / 2 - th                                              # 0 en la punta
    px, py = r_out, 0.0
    qx, qy = r_in * math.cos(sector / 2), r_in * math.sin(sector / 2)
    ex, ey = qx - px, qy - py
    dx, dy = np.cos(th), np.sin(th)
    r = (px * ey - py * ex) / (dx * ey - dy * ex)
    return d < r


def cloth(h, w, rng):
    folds = np.sin(np.linspace(0, 7 * math.pi, w))[None, :] * 0.06 + rng.normal(0, 0.01, (h, w))
    return 1 + folds


def flag_police(h=640, w=1024):
    rng = np.random.default_rng(1)
    yy, xx = np.mgrid[0:h, 0:w]
    col = np.zeros((h, w, 3)) + np.array([0.04, 0.07, 0.22])
    band = np.abs(yy - h * 0.5) < h * 0.06
    col[band] = (0.75, 0.6, 0.2)
    cx, cy = w * 0.5, h * 0.5
    disc = np.hypot(xx - cx, yy - cy) < h * 0.3
    col[disc] = (0.04, 0.07, 0.22)
    ring = (np.hypot(xx - cx, yy - cy) < h * 0.3) & (np.hypot(xx - cx, yy - cy) > h * 0.26)
    col[ring] = (0.8, 0.65, 0.22)
    st = star(xx, yy, cx, cy, h * 0.22, h * 0.11, 6)
    col[st] = (0.85, 0.7, 0.25)
    col[np.hypot(xx - cx, yy - cy) < h * 0.06] = (0.04, 0.07, 0.22)
    col *= cloth(h, w, rng)[..., None]
    return col


def flag_city(h=640, w=1024):
    rng = np.random.default_rng(2)
    yy, xx = np.mgrid[0:h, 0:w]
    col = np.zeros((h, w, 3)) + np.array([0.45, 0.05, 0.08])
    col[(yy // (h // 5)) % 2 == 1] = (0.88, 0.86, 0.8)
    canton = (xx < w * 0.38) & (yy < h * 0.6)
    col[canton] = (0.45, 0.05, 0.08)
    # torre almenada en el canton
    tx, ty = w * 0.19, h * 0.32
    tower = (np.abs(xx - tx) < w * 0.06) & (np.abs(yy - ty) < h * 0.16)
    merl = (np.abs(xx - tx) < w * 0.075) & (yy > ty - h * 0.22) & (yy < ty - h * 0.15) & ((((xx - tx + w) // (w * 0.025)).astype(int)) % 2 == 0)
    col[tower | merl] = (0.85, 0.7, 0.25)
    door = (np.abs(xx - tx) < w * 0.018) & (yy > ty + h * 0.04) & (yy < ty + h * 0.16)
    col[door] = (0.45, 0.05, 0.08)
    col *= cloth(h, w, rng)[..., None]
    return col


def city_map(h=1280, w=2048):
    rng = np.random.default_rng(7)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    col = np.zeros((h, w, 3)) + np.array([0.9, 0.87, 0.78])                     # papel
    # rio en curva
    rx = w * 0.62 + np.sin(yy / h * math.pi * 1.6) * w * 0.08
    river = np.abs(xx - rx) < w * 0.025
    # manzanas: rejilla de calles con avenidas
    block = 64
    street = ((xx % block) < 7) | ((yy % block) < 7)
    avenue = (np.abs(xx - w * 0.3) < 10) | (np.abs(yy - h * 0.45) < 10) | (np.abs((xx - yy * 0.9) - w * 0.05) < 9)
    col[~street] = (0.82, 0.78, 0.68)
    col[street] = (0.96, 0.94, 0.88)
    col[avenue] = (0.95, 0.85, 0.55)
    park = (np.abs(xx - w * 0.2) < 120) & (np.abs(yy - h * 0.75) < 90)
    col[park] = (0.55, 0.7, 0.45)
    col[river] = (0.5, 0.65, 0.8)
    # limites de distrito (linea discontinua granate)
    for (dx, dy, dw, dh) in ((0.05, 0.05, 0.25, 0.35), (0.35, 0.1, 0.22, 0.3), (0.7, 0.55, 0.25, 0.4), (0.08, 0.55, 0.3, 0.38)):
        x0, y0, x1, y1 = w * dx, h * dy, w * (dx + dw), h * (dy + dh)
        frame = (((np.abs(xx - x0) < 4) | (np.abs(xx - x1) < 4)) & (yy > y0) & (yy < y1)) | (((np.abs(yy - y0) < 4) | (np.abs(yy - y1) < 4)) & (xx > x0) & (xx < x1))
        dash = ((xx + yy) // 18) % 2 == 0
        col[frame & dash] = (0.4, 0.15, 0.12)
    # la comisaria (distrito 7) marcada y chinchetas
    px, py = w * 0.42, h * 0.38
    col[(np.abs(xx - px) < 30) & (np.abs(yy - py) < 22)] = (0.75, 0.08, 0.06)
    circ = np.abs(np.hypot(xx - px, yy - py) - 70) < 5
    col[circ] = (0.75, 0.08, 0.06)
    for _ in range(14):
        cx, cy = rng.uniform(0.05, 0.95) * w, rng.uniform(0.05, 0.95) * h
        c = (0.85, 0.1, 0.08) if rng.random() < 0.6 else (0.1, 0.25, 0.7)
        col[np.hypot(xx - cx, yy - cy) < 11] = c
    # manchas de cafe y viejo
    stain = smoothstep(1.4, 2.4, spectral(1024, 3.0))
    stain = np.kron(stain, np.ones((2, 2)))[:h, :w]
    col *= (1 - stain * 0.25)[..., None]
    grid = ((xx % 256) < 2) | ((yy % 256) < 2)                                  # cuadricula del plano
    col[grid] *= 0.85
    return col


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    save_rect(os.path.join(out, "tex_flag_police.png"), flag_police())
    save_rect(os.path.join(out, "tex_flag_city.png"), flag_city())
    save_rect(os.path.join(out, "tex_city_map.png"), city_map())
    print("PROCESO_TERMINADO")
