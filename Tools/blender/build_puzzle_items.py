"""Comisaria grande, fase D (2026-10-09): objetos y piezas de los puzles.

    blender --background --python build_puzzle_items.py -- <carpeta Assets/_Project/Art/Props/Puzzle>

  BoltCutter    cizalla de mangos largos (para romper candados); objeto recogible
  KeyCard       tarjeta magnetica con banda y foto; objeto recogible (el color lo pone el tinte del objeto)
  Fuse          fusible de ceramica con casquillos de laton; objeto recogible
  PadlockChain  cadena cruzada con candado que se pone en la hoja de una puerta (se quita al abrirla)
  CardReader    lector de tarjetas de pared con su piloto
  FuseBox       cuadro de fusibles de pared con tres huecos y una palanca
Origen en el centro de la base (los de pared: centro de la placa, frente a -Y = +Z en Unity).
"""
import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())


def make_cutter():
    m = Mesher("cutter", {"steel": (0.45, 0.46, 0.5), "plastic_red": (0.6, 0.06, 0.05), "steel_dark": (0.1, 0.1, 0.1)})
    for s in (-1, 1):
        m.box((s * 0.03, -0.2, 0.02), (0.02, 0.42, 0.02), "steel", rot=(0, 0, s * 4))              # brazos
        m.box((s * 0.045, -0.48, 0.02), (0.035, 0.24, 0.035), "plastic_red", rot=(0, 0, s * 6))     # mangos
    m.box((0, 0.06, 0.02), (0.07, 0.1, 0.03), "steel_dark")                                         # cabeza
    for s in (-1, 1):
        m.box((s * 0.012, 0.14, 0.02), (0.018, 0.08, 0.025), "steel", rot=(0, 0, -s * 8))           # mordazas
    return m.finish("BoltCutter")


def make_card():
    m = Mesher("card", {"plastic_white": (0.88, 0.88, 0.85), "plastic_black": (0.05, 0.05, 0.05), "plastic_photo": (0.45, 0.38, 0.32), "brass": (0.75, 0.6, 0.25)})
    m.box((0, 0, 0.001), (0.086, 0.054, 0.002), "plastic_white", bevel=0.001)
    m.box((0, 0.018, 0.0025), (0.086, 0.01, 0.001), "plastic_black")
    m.box((-0.025, -0.008, 0.0025), (0.022, 0.026, 0.001), "plastic_photo")
    m.box((0.02, -0.01, 0.0025), (0.012, 0.01, 0.001), "brass")
    return m.finish("KeyCard")


def make_fuse():
    m = Mesher("fuse", {"plastic_white": (0.85, 0.82, 0.75), "brass": (0.75, 0.6, 0.25), "plastic_red": (0.6, 0.06, 0.05)})
    m.loft_y([(0, -0.045, 0.018, 0.016, 0.016), (0, 0.045, 0.018, 0.016, 0.016)], "plastic_white", 14)
    for y in (-0.05, 0.05):
        m.loft_y([(0, y - 0.01, 0.018, 0.019, 0.019), (0, y + 0.01, 0.018, 0.019, 0.019)], "brass", 14)
    m.box((0, 0, 0.034), (0.012, 0.03, 0.002), "plastic_red")
    return m.finish("Fuse")


def make_padlock_chain():
    m = Mesher("chain", {"steel": (0.4, 0.4, 0.42), "brass": (0.7, 0.55, 0.22)})
    for s in (-1, 1):
        for i in range(10):
            t = i / 9.0
            m.box((s * (-0.25 + 0.5 * t), -0.04, 0.85 + 0.5 * t), (0.05, 0.015, 0.03), "steel", rot=(0, s * -45 + (i % 2) * 90, 0))
    m.box((0, -0.07, 0.95), (0.08, 0.035, 0.09), "brass", bevel=0.006)                              # candado
    m.loft_y([(0, -0.07, 1.02, 0.03, 0.03), (0, -0.06, 1.02, 0.03, 0.03)], "steel", 10)
    return m.finish("PadlockChain")


def make_reader():
    m = Mesher("reader", {"plastic_black": (0.06, 0.06, 0.07), "plastic_grey": (0.3, 0.32, 0.33), "plastic_led": (0.8, 0.1, 0.08)})
    m.box((0, 0, 0), (0.12, 0.04, 0.2), "plastic_grey", bevel=0.006)
    m.box((0, -0.022, 0.02), (0.09, 0.006, 0.1), "plastic_black")
    m.box((0, -0.024, 0.08), (0.03, 0.006, 0.012), "plastic_led")
    m.box((0, -0.03, -0.06), (0.1, 0.02, 0.01), "plastic_black")                                     # ranura
    return m.finish("CardReader")


def make_fusebox():
    m = Mesher("fusebox", {"body": (0.4, 0.43, 0.42), "steel": (0.55, 0.55, 0.57), "plastic_black": (0.05, 0.05, 0.05), "plastic_yellow": (0.75, 0.6, 0.05), "plastic_white": (0.85, 0.82, 0.75)})
    m.box((0, 0.05, 0), (0.7, 0.18, 0.9), "body", bevel=0.01)
    m.box((0, -0.045, 0.0), (0.6, 0.01, 0.8), "plastic_black")
    for x in (-0.18, 0, 0.18):
        m.box((x, -0.05, 0.12), (0.07, 0.02, 0.16), "steel")                                        # portafusibles vacios
        m.box((x, -0.06, 0.12), (0.04, 0.01, 0.11), "plastic_black")
    m.box((0, -0.05, 0.36), (0.5, 0.01, 0.06), "plastic_yellow")                                    # rotulo de peligro
    m.box((0.22, -0.08, -0.22), (0.04, 0.05, 0.2), "plastic_black", rot=(25, 0, 0))                 # palanca
    m.box((0.22, -0.06, -0.3), (0.08, 0.04, 0.06), "steel")
    for x in (-0.25, -0.1):
        m.box((x, -0.05, -0.25), (0.08, 0.01, 0.12), "plastic_white")                               # etiquetas
    return m.finish("FuseBox")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_cutter, make_card, make_fuse, make_padlock_chain, make_reader, make_fusebox):
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.3f x %.3f x %.3f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
