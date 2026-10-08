"""Comisaria v2, fase 4 (2026-10-08): el archivo de la segunda planta, donde esta el primer jefe.

    blender --background --python build_archive.py -- <carpeta Assets/_Project/Art/Props/Archive>

Origen en el centro de la base; frente a -Y en Blender (= +Z en Unity). Los nombres de material deciden el material horneado
(texture_items.py: "body" = chapa pintada, "cardboard", "wood", "brass", "plastic").
  ArchiveShelfA/B/C  estanteria metalica exenta (1,8 x 0,5 x 2,3 m), llena de cajas de archivo y carpetas; cada variante con
                     otro reparto (huecos, cajas caidas, carpetas tumbadas). Se rompe si la embiste el jefe.
  ArchiveWallShelf   estanteria de obra contra la pared (4 x 0,45 x 3,4 m), fija.
  ArchiveBoxStackA/B pila de cajas de archivo, alguna sin tapa con papeles
  PaperScatter       papeles tirados por el suelo (2 x 2 m)
  RollingLadder      escalerilla de archivo con ruedas
  CardCatalog        fichero de madera con cajoncitos (alguno abierto)
  ArchiveCart        carrito con carpetas
"""
import math
import os
import random
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())

PAL_SHELF = {"body": (0.32, 0.35, 0.36), "cardboard": (0.62, 0.5, 0.32), "cardboard_dark": (0.45, 0.36, 0.22),
             "paper": (0.88, 0.86, 0.8), "plastic_blue": (0.1, 0.18, 0.4), "plastic_red": (0.45, 0.08, 0.07),
             "plastic_black": (0.06, 0.06, 0.07), "plastic_green": (0.1, 0.28, 0.14), "steel_dark": (0.08, 0.08, 0.08)}
BINDERS = ("plastic_blue", "plastic_red", "plastic_black", "plastic_green")


def archive_box(m, c, w, d, h, rng, lid=True, rot=0.0):
    """Caja de archivo: cuerpo, tapa algo mayor, etiqueta y agujero de asa."""
    m.box((c[0], c[1], c[2] + h / 2), (w, d, h), "cardboard" if rng.random() < 0.7 else "cardboard_dark", rot=(0, 0, rot))
    if lid:
        m.box((c[0], c[1], c[2] + h - 0.012), (w + 0.01, d + 0.01, 0.03), "cardboard", rot=(0, 0, rot))
    else:
        for i in range(rng.randint(2, 5)):                         # papeles asomando
            m.box((c[0] + rng.uniform(-0.03, 0.03), c[1] + rng.uniform(-0.03, 0.03), c[2] + h + 0.02), (w * 0.8, 0.003, 0.08), "paper",
                  rot=(rng.uniform(-15, 15), 0, rot + 90 + rng.uniform(-10, 10)))
    ca, sa = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    fx, fy = -sa * (d / 2 + 0.002), -ca * (d / 2 + 0.002)
    m.box((c[0] + fx, c[1] + fy, c[2] + h * 0.55), (w * 0.45, 0.003, h * 0.22), "paper", rot=(0, 0, rot))           # etiqueta
    m.box((c[0] + fx, c[1] + fy, c[2] + h * 0.85), (w * 0.3, 0.004, 0.025), "steel_dark", rot=(0, 0, rot))         # asa


def shelf_frame(m, L, D, H, levels):
    """Bastidor: cuatro montantes en L con agujeros, baldas con borde y tirantes en cruz detras."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (L / 2 - 0.02), sy * (D / 2 - 0.02)
            m.box((x, y, H / 2), (0.04, 0.04, H), "body")
            for k in range(int(H / 0.1)):
                m.box((x, y - sy * 0.021, 0.05 + k * 0.1), (0.012, 0.002, 0.025), "steel_dark")
    for z in levels:
        m.box((0, 0, z), (L - 0.02, D - 0.02, 0.025), "body")
        m.box((0, -D / 2 + 0.01, z + 0.015), (L - 0.04, 0.015, 0.035), "body")              # borde de la balda
    for s in (-1, 1):                                                                       # tirantes en X por detras
        m.box((0, D / 2 - 0.01, H / 2), (0.02, 0.006, math.hypot(L, H) * 0.98), "body", rot=(0, s * math.degrees(math.atan2(L, H)), 0))


def fill_level(m, L, D, z, h_avail, rng, mess):
    """Llena una balda de cajas y carpetas, con huecos y algo de desorden."""
    x = -L / 2 + 0.06
    while x < L / 2 - 0.12:
        r = rng.random()
        if r < mess * 0.25:                                        # hueco
            x += rng.uniform(0.15, 0.4)
            continue
        if r < 0.62:                                               # caja de archivo
            w = rng.uniform(0.3, 0.4)
            if x + w > L / 2 - 0.04:
                break
            h = min(h_avail - 0.04, rng.uniform(0.26, 0.32))
            rot = rng.uniform(-6, 6) * mess
            dy = rng.uniform(-0.04, 0.06) * (1 + mess)
            archive_box(m, (x + w / 2, dy, z + 0.013), w, D * 0.85, h, rng, lid=rng.random() > 0.15 * (1 + mess), rot=rot)
            x += w + rng.uniform(0.005, 0.03)
        else:                                                      # fila de carpetas AZ
            n = rng.randint(3, 9)
            col = rng.choice(BINDERS)
            hb = min(h_avail - 0.04, 0.31)
            for i in range(n):
                if x + 0.08 > L / 2 - 0.04:
                    break
                tilt = 0.0
                if i == n - 1 and rng.random() < 0.5 + mess * 0.3:
                    tilt = rng.uniform(10, 25)                    # la ultima, inclinada
                m.box((x + 0.04, -0.02, z + 0.013 + hb / 2), (0.075, D * 0.6, hb), col, rot=(0, tilt, 0))
                m.box((x + 0.04, -0.02 - D * 0.3 - 0.002, z + 0.013 + hb * 0.6), (0.03, 0.003, 0.06), "paper")   # lomo con etiqueta
                x += 0.08
            x += rng.uniform(0.01, 0.05)


def make_shelf(name, seed, mess):
    rng = random.Random(seed)
    m = Mesher(name.lower(), PAL_SHELF)
    L, D, H = 1.8, 0.5, 2.3
    levels = [0.08, 0.53, 0.98, 1.43, 1.88, 2.28]
    shelf_frame(m, L, D, H, levels)
    for i, z in enumerate(levels[:-1]):
        fill_level(m, L, D, z, levels[i + 1] - z, rng, mess)
    # encima, alguna caja suelta
    for k in range(rng.randint(0, 2)):
        archive_box(m, (rng.uniform(-0.6, 0.6), 0, levels[-1] + 0.013), 0.36, 0.42, 0.28, rng, lid=True, rot=rng.uniform(-20, 20))
    return m.finish(name)


def make_wall_shelf():
    rng = random.Random(21)
    m = Mesher("wallshelf", PAL_SHELF)
    L, D, H = 4.0, 0.45, 3.4
    levels = [0.08 + i * 0.47 for i in range(7)] + [3.37]
    shelf_frame(m, L, D, H, levels)
    m.box((0, -0.0, H / 2), (0.04, D - 0.04, H), "body")             # montante central
    for i, z in enumerate(levels[:-1]):
        fill_level(m, L, D, z, levels[i + 1] - z, rng, 0.3)
    return m.finish("ArchiveWallShelf")


def make_box_stack(name, seed):
    rng = random.Random(seed)
    m = Mesher(name.lower(), PAL_SHELF)
    z = 0.0
    for lvl in range(rng.randint(3, 5)):
        for i in range(2 if lvl < 2 else 1):
            archive_box(m, (rng.uniform(-0.05, 0.05) + (i - 0.5) * 0.42 * (1 if lvl < 2 else 0), rng.uniform(-0.04, 0.04), z),
                        0.4, 0.32, 0.27, rng, lid=rng.random() > 0.3, rot=rng.uniform(-12, 12))
        z += 0.3
    archive_box(m, (0.65, 0.1, 0), 0.4, 0.32, 0.27, rng, lid=False, rot=35)        # una en el suelo, abierta
    for i in range(6):                                                               # papeles alrededor
        m.box((rng.uniform(-0.6, 0.9), rng.uniform(-0.5, 0.5), 0.002), (0.21, 0.297, 0.003), "paper", rot=(0, 0, rng.uniform(0, 180)))
    return m.finish(name)


def make_paper_scatter():
    rng = random.Random(5)
    m = Mesher("paperscatter", {"paper": (0.88, 0.86, 0.8), "paper2": (0.82, 0.8, 0.62), "cardboard": (0.6, 0.48, 0.3)})
    for i in range(45):
        r = rng.random() ** 0.7 * 1.0; a = rng.uniform(0, 2 * math.pi)
        m.box((r * math.cos(a), r * math.sin(a), 0.002 + i * 0.0004), (0.21, 0.297, 0.002), "paper" if rng.random() < 0.75 else "paper2",
              rot=(rng.uniform(-3, 3), rng.uniform(-3, 3), rng.uniform(0, 180)))
    for i in range(3):                                                               # carpetas abiertas
        a = rng.uniform(0, 2 * math.pi)
        m.box((0.6 * math.cos(a), 0.6 * math.sin(a), 0.004), (0.46, 0.32, 0.004), "cardboard", rot=(0, 0, rng.uniform(0, 180)))
    return m.finish("PaperScatter")


def make_ladder():
    m = Mesher("ladder", {"steel": (0.6, 0.6, 0.62), "plastic": (0.08, 0.08, 0.08), "body": (0.55, 0.12, 0.08)})
    for sx in (-1, 1):
        m.box((sx * 0.25, 0.15, 0.6), (0.035, 0.035, 1.25), "body", rot=(-12, 0, 0))           # largueros delanteros
        m.box((sx * 0.25, 0.42, 0.55), (0.03, 0.03, 1.15), "body", rot=(10, 0, 0))            # traseros
        for y in (0.02, 0.55):
            m.loft_y([(sx * 0.25, y - 0.02, 0.04, 0.035, 0.035), (sx * 0.25, y + 0.02, 0.04, 0.035, 0.035)], "plastic", 10)
    for k in range(4):                                                                          # peldaños
        z = 0.3 + k * 0.28
        m.box((0, 0.19 - k * 0.06 + 0.12, z), (0.5, 0.2, 0.025), "steel")
    m.box((0, 0.3, 1.2), (0.55, 0.32, 0.03), "steel")
    for sx in (-1, 1):
        m.box((sx * 0.27, 0.3, 1.45), (0.025, 0.025, 0.5), "body")
    m.box((0, 0.3, 1.7), (0.56, 0.025, 0.025), "body")
    return m.finish("RollingLadder")


def make_card_catalog():
    rng = random.Random(9)
    m = Mesher("catalog", {"wood": (0.32, 0.17, 0.08), "brass": (0.72, 0.56, 0.24), "paper": (0.88, 0.85, 0.75)})
    W, D, H = 1.0, 0.5, 1.3
    m.box((0, 0, 0.06), (W - 0.04, D - 0.04, 0.12), "wood")                                      # zocalo
    m.box((0, 0, 0.12 + (H - 0.12) / 2), (W, D, H - 0.12), "wood", bevel=0.006)
    m.box((0, 0, H + 0.015), (W + 0.04, D + 0.04, 0.03), "wood", bevel=0.006)
    cols, rows = 5, 8
    for c in range(cols):
        for r in range(rows):
            x = -W / 2 + 0.1 + c * (W - 0.2) / (cols - 1)
            z = 0.2 + r * (H - 0.3) / rows
            out = 0.0
            if rng.random() < 0.12:
                out = rng.uniform(0.08, 0.25)                                                     # cajon abierto
                m.box((x, -D / 2 - out / 2, z + 0.05), (0.15, out, 0.1), "wood")
                for k in range(5):
                    m.box((x, -D / 2 - out + 0.02 + k * out / 5, z + 0.09), (0.12, 0.003, 0.07), "paper")
            m.box((x, -D / 2 - 0.006 - out, z + 0.05), (0.17, 0.012, 0.11), "wood", bevel=0.003)
            m.box((x, -D / 2 - 0.015 - out, z + 0.075), (0.05, 0.006, 0.02), "brass")
            m.box((x, -D / 2 - 0.016 - out, z + 0.035), (0.03, 0.01, 0.012), "brass")
    return m.finish("CardCatalog")


def make_cart():
    rng = random.Random(13)
    m = Mesher("cart", {"body": (0.3, 0.32, 0.34), "plastic_black": (0.05, 0.05, 0.05), "plastic_blue": (0.1, 0.18, 0.4),
                        "plastic_red": (0.45, 0.08, 0.07), "cardboard": (0.6, 0.48, 0.3), "cardboard_dark": (0.45, 0.36, 0.22),
                        "paper": (0.88, 0.86, 0.8), "steel_dark": (0.08, 0.08, 0.08)})
    L, D = 0.9, 0.45
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((sx * (L / 2 - 0.02), sy * (D / 2 - 0.02), 0.5), (0.025, 0.025, 0.9), "body")
            m.loft_y([(sx * (L / 2 - 0.02), sy * (D / 2 - 0.02) - 0.015, 0.045, 0.045, 0.045), (sx * (L / 2 - 0.02), sy * (D / 2 - 0.02) + 0.015, 0.045, 0.045, 0.045)], "plastic_black", 10)
    for z in (0.15, 0.5, 0.92):
        m.box((0, 0, z), (L, D, 0.02), "body")
    for z in (0.16, 0.51):
        x = -L / 2 + 0.06
        while x < L / 2 - 0.1:
            col = rng.choice(("plastic_blue", "plastic_red", "plastic_black"))
            m.box((x, 0, z + 0.16), (0.07, 0.28, 0.3), col, rot=(0, rng.uniform(-8, 8), 0))
            x += 0.08
    archive_box(m, (0.1, 0, 0.93), 0.4, 0.32, 0.27, rng, lid=False, rot=8)
    m.box((0.3, -0.36, 0.95), (0.025, 0.25, 0.025), "body")                                          # asa
    return m.finish("ArchiveCart")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    fns = (lambda: make_shelf("ArchiveShelfA", 1, 0.2), lambda: make_shelf("ArchiveShelfB", 2, 0.5), lambda: make_shelf("ArchiveShelfC", 3, 1.0),
           make_wall_shelf, lambda: make_box_stack("ArchiveBoxStackA", 4), lambda: make_box_stack("ArchiveBoxStackB", 8),
           make_paper_scatter, make_ladder, make_card_catalog, make_cart)
    for fn in fns:
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
