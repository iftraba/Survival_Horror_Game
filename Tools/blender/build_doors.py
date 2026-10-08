"""Puertas de madera con detalle (2026-10-08): hoja con largueros y travesanos, panel inferior biselado, hueco para el cristal,
placa de chapa, manivelas con roseta y bisagras; jamba y dintel con moldura.

    blender --background --python build_doors.py -- <carpeta Assets/_Project/Art/Props/Door>

Medidas reales de la puerta de 1,50 (la hoja va de x = 0,02 a 1,48 desde la bisagra, de 0,02 a 2,38 de alto y 5 cm de grueso).
Unity (DoorModelKit) normaliza cada malla a la caja de la pieza que sustituye (Leaf, Jamb, Header) y la estira a cada puerta:
asi el prefab conserva sus colisiones y su logica. Ejes de Blender: X ancho, Z alto, Y grueso.
"""
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())

W0, W1, H0, H1, T = 0.02, 1.48, 0.02, 2.38, 0.05


def make_leaf():
    m = Mesher("doorleaf", {"wood": (0.36, 0.20, 0.10), "board": (0.30, 0.17, 0.08), "steel": (0.62, 0.62, 0.64), "brass": (0.70, 0.55, 0.25)})
    cx, w = (W0 + W1) / 2, W1 - W0
    st, rl = 0.13, 0.13                                      # ancho de largueros y travesanos
    win = (1.47, 1.97, cx - 0.31, cx + 0.31)                 # hueco del cristal (z0, z1, x0, x1): el de Window del prefab
    core = 0.032                                             # alma (los marcos sobresalen hasta T/2)
    # alma de la hoja, partida alrededor del hueco del cristal
    m.box((cx, 0, (H0 + win[0]) / 2), (w, core, win[0] - H0), "board")
    m.box((cx, 0, (win[1] + H1) / 2), (w, core, H1 - win[1]), "board")
    m.box(((W0 + win[2]) / 2, 0, (win[0] + win[1]) / 2), (win[2] - W0, core, win[1] - win[0]), "board")
    m.box(((win[3] + W1) / 2, 0, (win[0] + win[1]) / 2), (W1 - win[3], core, win[1] - win[0]), "board")
    for s in (-1, 1):                                        # las dos caras
        y = s * (T / 2 - 0.006)
        # largueros, travesanos (arriba, centro, bajo) y montantes del cristal
        m.box((W0 + st / 2, y, (H0 + H1) / 2), (st, 0.012, H1 - H0), "wood", bevel=0.004)
        m.box((W1 - st / 2, y, (H0 + H1) / 2), (st, 0.012, H1 - H0), "wood", bevel=0.004)
        m.box((cx, y, H1 - rl / 2), (w - 2 * st, 0.012, rl), "wood", bevel=0.004)
        m.box((cx, y, H0 + 0.11), (w - 2 * st, 0.012, 0.22), "wood", bevel=0.004)
        m.box((cx, y, 1.05), (w - 2 * st, 0.012, 0.14), "wood", bevel=0.004)
        m.box((cx, y, (1.12 + win[0]) / 2), (w - 2 * st, 0.012, win[0] - 1.12), "wood", bevel=0.004)
        m.box((cx, y, (win[1] + H1 - rl) / 2), (w - 2 * st, 0.012, H1 - rl - win[1]), "wood", bevel=0.004)
        # panel bajo: plano rehundido con bisel alrededor
        m.box((cx, y * 0.75, 0.62), (w - 2 * st - 0.02, 0.008, 0.62), "board", bevel=0.012)
        m.box((cx, y * 0.95, 0.62), (w - 2 * st - 0.16, 0.01, 0.48), "wood", bevel=0.02)
        # junquillo del cristal
        for (bx, bz, sx, sz) in ((cx, win[0] + 0.012, win[3] - win[2] + 0.04, 0.024), (cx, win[1] - 0.012, win[3] - win[2] + 0.04, 0.024),
                                 (win[2] + 0.012, (win[0] + win[1]) / 2, 0.024, win[1] - win[0]), (win[3] - 0.012, (win[0] + win[1]) / 2, 0.024, win[1] - win[0])):
            m.box((bx, s * (T / 2 + 0.002), bz), (sx, 0.014, sz), "wood", bevel=0.004)
        # placa de chapa abajo y manivela con roseta
        m.box((cx, s * (T / 2 + 0.001), 0.12), (w - 0.06, 0.003, 0.2), "steel")
        hx = W1 - 0.09
        m.loft_y([(hx, s * (T / 2), 1.0, 0.03, 0.03), (hx, s * (T / 2 + 0.01), 1.0, 0.03, 0.03)], "steel", 16)        # roseta
        m.loft_y([(hx, s * (T / 2 + 0.01), 1.0, 0.009, 0.009), (hx, s * (T / 2 + 0.05), 1.0, 0.009, 0.009)], "steel", 10)   # cuello
        m.box((hx - 0.055, s * (T / 2 + 0.055), 1.0), (0.13, 0.016, 0.018), "steel", bevel=0.006)                       # manivela
        m.loft_y([(hx, s * (T / 2), 0.9, 0.012, 0.012), (hx, s * (T / 2 + 0.006), 0.9, 0.012, 0.012)], "brass", 10)    # bocallave
    for z in (0.3, 1.2, 2.1):                                # bisagras
        m.loft_z([(W0 - 0.004, 0, z - 0.06, 0.01, 0.01), (W0 - 0.004, 0, z + 0.06, 0.01, 0.01)], "brass", 10)
        m.box((W0 + 0.01, 0, z), (0.03, T + 0.004, 0.11), "brass")
    return m.finish("DoorLeaf")


def make_jamb():
    """Jamba con moldura (la pieza Jamb del prefab: 0,1 x 2,45 x 0,05, de y = -0,025 a 2,425) con plinto abajo."""
    m = Mesher("doorjamb", {"wood": (0.34, 0.19, 0.09), "board": (0.28, 0.16, 0.08)})
    x0, x1, z0, z1 = -0.09, 0.01, -0.025, 2.425
    cx = (x0 + x1) / 2
    m.box((cx, 0, (z0 + z1) / 2), (x1 - x0, 0.03, z1 - z0), "board")
    m.box((cx, -0.006, (z0 + z1) / 2), (0.05, 0.034, z1 - z0 - 0.02), "wood", bevel=0.008)    # junquillo central
    m.box((x0 + 0.012, -0.004, (z0 + z1) / 2), (0.024, 0.04, z1 - z0), "wood", bevel=0.006)   # canto exterior
    m.box((cx, -0.006, z0 + 0.13), (x1 - x0 + 0.006, 0.05, 0.26), "board", bevel=0.006)      # plinto
    return m.finish("DoorJamb")


def make_header():
    """Dintel con cornisa (la pieza Header de la puerta de 1,50: 1,68 x 0,1 x 0,05)."""
    m = Mesher("doorheader", {"wood": (0.34, 0.19, 0.09), "board": (0.28, 0.16, 0.08)})
    x0, x1, z0, z1 = -0.09, 1.59, 2.40, 2.50
    cx = (x0 + x1) / 2
    m.box((cx, 0, (z0 + z1) / 2), (x1 - x0, 0.03, z1 - z0), "board")
    m.box((cx, -0.006, (z0 + z1) / 2 - 0.01), (x1 - x0, 0.034, 0.05), "wood", bevel=0.008)
    m.box((cx, -0.01, z1 - 0.012), (x1 - x0 + 0.02, 0.05, 0.024), "wood", bevel=0.006)      # cornisa
    return m.finish("DoorHeader")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_leaf, make_jamb, make_header):
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
