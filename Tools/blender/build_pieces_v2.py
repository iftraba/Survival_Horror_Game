"""Piezas unicas y de zona v2 (2026-10-14, lavado de cara del interior, tanda 4): mostrador de recepcion, sofa, maquina expendedora, catre, papelera,
ordenador de escritorio y dispensador de agua con mas detalle.

    blender --background --python build_pieces_v2.py -- <carpeta ABSOLUTA de salida> [Nombre ...]
    (en un Blender abierto: exec(open(ruta).read()); preview_all())

Mismas medidas aproximadas, orientacion (frente -Y) y origen que los originales. Biseles <= la mitad del grosor de cada caja.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE7 = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
_NO_MAIN = True
exec(open(os.path.join(HERE7, "build_chairs_v2.py"), encoding="utf-8").read())   # tube, cushion, view, Mesher...
_NO_MAIN = False


def make_reception():
    m = Mesher("reception", {"wood": (0.26, 0.16, 0.09), "board": (0.36, 0.34, 0.31), "metal": (0.55, 0.56, 0.58), "plastic": (0.07, 0.07, 0.08),
                             "screen": (0.03, 0.04, 0.05), "paper": (0.88, 0.87, 0.82), "brass": (0.6, 0.46, 0.18), "laminate": (0.13, 0.15, 0.15)})
    L, D, H = 4.2, 0.75, 1.1
    m.box((0, 0, H / 2), (L, 0.06, H), "wood", bevel=0.01)                                                        # frente
    for i in range(6):                                                                                            # paneles del frente con marco y moldura
        x = -L / 2 + L * (i + 0.5) / 6
        m.box((x, -0.034, H * 0.5), (L / 6 - 0.06, 0.012, H * 0.7), "wood", bevel=0.004)                         # marco
        m.box((x, -0.0415, H * 0.5), (L / 6 - 0.16, 0.007, H * 0.58), "board", bevel=0.003)                      # panel hundido
        m.box((x, -0.0475, H * 0.5), (L / 6 - 0.22, 0.005, H * 0.5), "wood", bevel=0.002)                        # moldura interior
    m.box((0, 0.3, H + 0.02), (L + 0.1, D, 0.05), "laminate", bevel=0.012)                                       # encimera alta de laminado oscuro
    m.box((0, -0.075, H + 0.0), (L + 0.12, 0.05, 0.08), "wood", bevel=0.012)                                     # canto frontal de madera
    m.box((0, 0.55, 0.76), (L - 0.1, 0.6, 0.04), "board", bevel=0.006)                                           # mesa de trabajo
    m.box((L / 2 - 0.03, 1.1, H / 2), (0.06, 2.2, H), "wood", bevel=0.01)
    m.box((L / 2 + 0.25, 1.1, H + 0.02), (0.6, 2.25, 0.05), "laminate", bevel=0.012)
    m.box((-L / 2 + 0.03, 0.4, H / 2), (0.06, 0.8, H), "wood", bevel=0.01)
    m.box((0, -0.04, 0.06), (L, 0.1, 0.12), "plastic", bevel=0.01)                                               # zocalo
    # reposapies de latón y juntas verticales entre paneles
    tube(m, [(-L / 2 + 0.1, -0.08, 0.14), (L / 2 - 0.1, -0.08, 0.14)], 0.018, "brass", 8)
    for i in range(1, 6):
        m.box((-L / 2 + L * i / 6, -0.033, H * 0.5), (0.014, 0.014, H - 0.1), "plastic")
    for x in (-1.2, 0.6):                                                                                        # dos puestos con pantalla articulada
        m.box((x, 0.75, 0.80), (0.22, 0.16, 0.02), "plastic", bevel=0.008)
        tube(m, [(x, 0.76, 0.81), (x, 0.76, 0.95), (x, 0.74, 1.05)], 0.014, "plastic", 8)
        m.box((x, 0.74, 1.2), (0.56, 0.04, 0.34), "plastic", bevel=0.014)
        m.box((x, 0.715, 1.2), (0.52, 0.004, 0.30), "screen")
        m.box((x, 0.45, 0.79), (0.44, 0.15, 0.02), "plastic", bevel=0.008)
        m.box((x, 0.43, 0.803), (0.4, 0.1, 0.006), "metal")                                                                  # teclas (bloque)
        m.box((x + 0.32, 0.45, 0.79), (0.06, 0.1, 0.025), "plastic", bevel=0.012)
    for (x, r) in ((-0.3, 8), (-0.2, -5), (1.4, 12)):
        m.box((x, 0.5, 0.785), (0.21, 0.297, 0.004), "paper", rot=(0, 0, r))
    m.box((1.8, 0.3, H + 0.08), (0.3, 0.22, 0.1), "metal", bevel=0.01)                                           # timbre: base
    m.ellipsoid((1.8, 0.3, H + 0.15), (0.05, 0.05, 0.03), "brass", 12, 5)
    m.box((-1.6, 0.35, H + 0.08), (0.34, 0.24, 0.12), "plastic", bevel=0.012)                                   # telefono
    return m.finish("ReceptionDesk")


def make_couch():
    m = Mesher("couch", {"fabric_couch": (0.22, 0.16, 0.12), "wood": (0.15, 0.09, 0.05), "metal": (0.4, 0.4, 0.42)})
    L = 2.0
    cushion(m, (0, 0.05, 0.27), (L - 0.2, 0.86, 0.28), "fabric_couch", 0.012, 8, 6)                              # base
    for i in range(3):                                                                                           # tres cojines de asiento
        cushion(m, (-L / 3 + i * L / 3, -0.04, 0.47), (L / 3 - 0.08, 0.7, 0.16), "fabric_couch", 0.03, 6, 6)
    for i in range(3):                                                                                           # tres cojines de respaldo
        cushion(m, (-L / 3 + i * L / 3, 0.36, 0.7), (L / 3 - 0.08, 0.2, 0.46), "fabric_couch", 0.03, 6, 6)
    cushion(m, (0, 0.46, 0.66), (L - 0.2, 0.1, 0.58), "fabric_couch", 0.01, 8, 6)                                # estructura del respaldo
    for sx in (-1, 1):                                                                                           # brazos redondeados
        cushion(m, (sx * (L / 2 + 0.02), 0.05, 0.45), (0.2, 0.86, 0.44), "fabric_couch", 0.03, 4, 6)
        for sy in (-1, 1):
            tube(m, [(sx * (L / 2 - 0.08), sy * 0.34, 0.0), (sx * (L / 2 - 0.08), sy * 0.34, 0.1)], 0.028, "wood", 8, 0.018)    # patas torneadas
    cushion(m, (0.5, -0.05, 0.6), (0.45, 0.12, 0.35), "fabric_couch", 0.02, 4, 4)                                 # cojin caido
    return m.finish("Couch")


def make_vending():
    rng = random.Random(6)
    m = Mesher("vending", {"body_red": (0.5, 0.07, 0.05), "plastic_glass": (0.15, 0.2, 0.22), "plastic_black": (0.04, 0.04, 0.045), "steel": (0.6, 0.6, 0.62),
                           "plastic_blue": (0.12, 0.3, 0.6), "plastic_green": (0.15, 0.45, 0.2), "plastic_yellow": (0.7, 0.55, 0.05), "plastic_white": (0.9, 0.9, 0.88),
                           "lens_amber": (0.9, 0.5, 0.05), "paper": (0.88, 0.87, 0.8)})
    W, D, H = 0.95, 0.8, 1.85
    m.box((0, 0, H / 2), (W, D, H), "body_red", bevel=0.02)
    m.box((0, -D / 2 - 0.006, H / 2), (W - 0.02, 0.012, H - 0.02), "body_red", bevel=0.005)                                    # frente
    m.box((-0.12, -D / 2 - 0.016, 1.05), (0.66, 0.014, 1.34), "plastic_black", bevel=0.004)                                  # marco de la vitrina
    m.box((-0.12, -D / 2 - 0.0215, 1.05), (0.62, 0.004, 1.3), "plastic_glass")                                               # cristal
    for r in range(5):                                                                                                       # baldas y espirales
        z = 0.5 + r * 0.25
        m.box((-0.12, -D / 2 + 0.2, z - 0.03), (0.62, 0.3, 0.012), "steel")
        for c in range(5):
            x = -0.36 + c * 0.12
            col = rng.choice(("plastic_blue", "plastic_green", "plastic_yellow", "plastic_white"))
            m.loft_z([(x, -D / 2 + 0.1, z, 0.035, 0.035), (x, -D / 2 + 0.1, z + 0.17, 0.035, 0.035)], col, 8)
            m.box((x, -D / 2 + 0.1, z + 0.176), (0.05, 0.05, 0.012), col, bevel=0.003)                                       # tapa
            m.box((x, -D / 2 + 0.19, z - 0.02), (0.08, 0.012, 0.008), "steel")                                                   # espiral
    m.box((0.35, -D / 2 - 0.01, 1.2), (0.16, 0.02, 0.5), "plastic_black", bevel=0.006)                                       # panel de monedas
    m.box((0.35, -D / 2 - 0.02, 1.36), (0.1, 0.01, 0.05), "lens_amber")                                                       # pantalla
    for r in range(4):
        for c in range(3):
            m.box((0.31 + c * 0.04, -D / 2 - 0.02, 1.25 - r * 0.045), (0.028, 0.008, 0.028), "steel", bevel=0.002)           # teclas
    m.box((0.35, -D / 2 - 0.02, 1.0), (0.08, 0.008, 0.016), "plastic_black")                                                  # ranura de monedas
    m.box((0.35, -D / 2 - 0.02, 0.97), (0.04, 0.008, 0.04), "steel")
    m.box((-0.1, -D / 2 - 0.02, 0.22), (0.5, 0.05, 0.18), "plastic_black", bevel=0.01)                                       # trampilla de recogida
    m.box((-0.1, -D / 2 - 0.046, 0.22), (0.44, 0.003, 0.12), "plastic_glass")
    m.box((0, -D / 2 - 0.012, 1.8), (0.8, 0.014, 0.06), "plastic_white", bevel=0.003)                                         # cartel superior
    return m.finish("VendingMachine")


def make_cot():
    m = Mesher("cot", {"frame": (0.23, 0.24, 0.26), "mattress": (0.5, 0.5, 0.46), "blanket": (0.2, 0.26, 0.22), "pillow": (0.7, 0.7, 0.66)})
    for sx in (-1, 1):
        for sy in (-1, 1):
            tube(m, [(0.42 * sx, 0.92 * sy, 0.0), (0.42 * sx, 0.92 * sy, 0.42)], 0.02, "frame", 8)
        tube(m, [(0.42 * sx, -0.92, 0.40), (0.42 * sx, 0.92, 0.40)], 0.018, "frame", 8)
    for sy in (-1, 1):
        tube(m, [(-0.42, 0.92 * sy, 0.40), (0.42, 0.92 * sy, 0.40)], 0.018, "frame", 8)
    for k in range(7):                                                                                           # tirantes del somier
        tube(m, [(-0.42, -0.78 + k * 0.26, 0.385), (0.42, -0.78 + k * 0.26, 0.385)], 0.006, "frame", 4)
    cushion(m, (0, 0, 0.455), (0.84, 1.84, 0.1), "mattress", 0.02, 6, 10)                                        # colchon abombado
    cushion(m, (0, 0.34, 0.53), (0.86, 1.1, 0.04), "blanket", 0.018, 6, 8)                                        # manta
    m.box((0, -0.2, 0.552), (0.86, 0.04, 0.012), "blanket", bevel=0.004)                                          # doblez
    cushion(m, (0, -0.7, 0.545), (0.5, 0.34, 0.08), "pillow", 0.03, 6, 6)                                         # almohada
    return m.finish("Cot")


def make_trash_bin():
    m = Mesher("trashbin", {"metal": (0.3, 0.32, 0.33), "paper": (0.85, 0.84, 0.8), "plastic": (0.05, 0.05, 0.055)})
    m.loft_z([(0, 0, 0.0, 0.12, 0.12), (0, 0, 0.02, 0.125, 0.125), (0, 0, 0.35, 0.15, 0.15), (0, 0, 0.36, 0.158, 0.158)], "metal", 20)
    for z in (0.12, 0.24):                                                                                       # acanaladuras
        m.loft_z([(0, 0, z - 0.008, 0.139 + (z - 0.12) * 0.04, 0.139 + (z - 0.12) * 0.04), (0, 0, z + 0.008, 0.15 + (z - 0.12) * 0.04, 0.15 + (z - 0.12) * 0.04)], "metal", 20) if False else None
    m._loft_rings([[((0.136 + (z - 0.1) * 0.04) * math.cos(2 * math.pi * k / 20), (0.136 + (z - 0.1) * 0.04) * math.sin(2 * math.pi * k / 20), z + dz) for k in range(20)] for dz in (-0.007, 0.007)] for z in (0.1,)) if False else None
    for z in (0.12, 0.24):
        r = 0.14 + (z / 0.35) * 0.01
        m._loft_rings([[(r * math.cos(2 * math.pi * k / 20), r * math.sin(2 * math.pi * k / 20), z + dz) for k in range(20)] for dz in (-0.01, 0.01)], "metal", True)
    m.loft_z([(0, 0, 0.0, 0.11, 0.11), (0, 0, 0.012, 0.11, 0.11)], "plastic", 20)
    for i in range(4):
        m.ellipsoid((0.045 * math.cos(i * 1.6), 0.045 * math.sin(i * 1.6), 0.36), (0.055, 0.05, 0.045), "paper", 8, 5)
    m.box((0.06, -0.02, 0.4), (0.12, 0.09, 0.012), "paper", rot=(0, 25, 20))
    return m.finish("TrashBin")


def make_desk_computer():
    m = Mesher("deskpc", {"plastic": (0.09, 0.09, 0.10), "screen": (0.02, 0.03, 0.04), "keys": (0.72, 0.71, 0.67), "cardboard": (0.13, 0.13, 0.15), "metal": (0.5, 0.5, 0.53)})
    m.box((0, 0.12, 0.011), (0.22, 0.16, 0.022), "plastic", bevel=0.008)
    tube(m, [(0, 0.13, 0.02), (0, 0.13, 0.12), (0, 0.11, 0.19)], 0.016, "plastic", 8)                          # cuello articulado
    m.box((0, 0.1, 0.3), (0.58, 0.05, 0.36), "plastic", bevel=0.02)                                            # monitor con marco
    m.box((0, 0.074, 0.305), (0.52, 0.004, 0.3), "screen")
    m.box((0.2, 0.076, 0.14), (0.018, 0.004, 0.006), "metal")                                                  # indicador
    m.box((0, -0.16, 0.014), (0.44, 0.15, 0.028), "plastic", bevel=0.01)                                       # teclado con teclas separadas
    for r in range(4):
        for c in range(14):
            m.box((-0.195 + c * 0.03, -0.205 + r * 0.032, 0.03), (0.025, 0.026, 0.01), "keys")
    m.box((0, -0.235, 0.032), (0.18, 0.02, 0.01), "keys")                                         # barra espaciadora
    m.box((0.32, -0.15, 0.002), (0.2, 0.17, 0.004), "cardboard")
    m.ellipsoid((0.32, -0.15, 0.02), (0.032, 0.05, 0.02), "plastic", 10, 5)                                    # raton
    tube(m, [(0.32, -0.095, 0.014), (0.28, -0.0, 0.006), (0.1, 0.1, 0.006)], 0.003, "plastic", 4)             # cable
    return m.finish("DeskComputer")


def make_water_cooler():
    m = Mesher("watercooler", {"plastic": (0.82, 0.82, 0.79), "glass": (0.35, 0.55, 0.75), "keys": (0.1, 0.1, 0.1), "metal": (0.55, 0.56, 0.58), "lens_red": (0.8, 0.05, 0.04), "lens_blue": (0.1, 0.3, 0.8)})
    m.box((0, 0, 0.5), (0.32, 0.32, 1.0), "plastic", bevel=0.02)
    m.box((0, 0, 0.03), (0.30, 0.30, 0.06), "keys", bevel=0.01)                                                # zocalo
    m.box((0, -0.162, 0.66), (0.26, 0.012, 0.22), "keys", bevel=0.004)                                         # panel frontal
    for x, mat in ((-0.06, "lens_blue"), (0.06, "lens_red")):
        m.box((x, -0.172, 0.74), (0.045, 0.02, 0.04), mat, bevel=0.005)                                         # grifos
        tube(m, [(x, -0.172, 0.725), (x, -0.19, 0.715)], 0.008, "metal", 6)
    m.box((0, -0.14, 0.62), (0.2, 0.1, 0.014), "metal", bevel=0.003)                                            # rejilla de goteo
    m.box((0, -0.19, 0.62), (0.2, 0.004, 0.0), "metal") if False else None
    m.loft_z([(0, 0, 1.0, 0.115, 0.115), (0, 0, 1.38, 0.14, 0.14), (0, 0, 1.44, 0.06, 0.06), (0, 0, 1.47, 0.028, 0.028)], "glass", 20)       # garrafa
    m.loft_z([(0, 0, 1.0, 0.13, 0.13), (0, 0, 1.03, 0.13, 0.13)], "plastic", 20)
    return m.finish("WaterCooler")


BUILDERS = {"ReceptionDesk": make_reception, "Couch": make_couch, "VendingMachine": make_vending, "Cot": make_cot, "TrashBin": make_trash_bin,
            "DeskComputer": make_desk_computer, "WaterCooler": make_water_cooler}


def preview_all():
    _clear()
    x = 0.0
    for name, fn in BUILDERS.items():
        obj = fn()
        d = obj.dimensions
        obj.location = (x + d.x / 2 + 0.2, 0, 0)
        x += d.x + 0.5
        print(name, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    view(72, -22, x * 0.9, (x / 2, 0.3, 0.7))


if __name__ == "__main__" and "--" in sys.argv and not globals().get("_NO_MAIN"):
    out = sys.argv[sys.argv.index("--") + 1]
    only = sys.argv[sys.argv.index("--") + 2:]
    os.makedirs(out, exist_ok=True)
    for name, fn in BUILDERS.items():
        if only and name not in only:
            continue
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, name + ".fbx"))
        print("hecho", name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons), flush=True)
    print("PROCESO_TERMINADO")
