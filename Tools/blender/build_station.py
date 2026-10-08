"""Comisaria grande (2026-10-09): mobiliario que faltaba para amueblar todas las salas.

    blender --background --python build_station.py -- <carpeta Assets/_Project/Art/Props/Station>

Origen en el centro de la base; frente a -Y en Blender (= +Z en Unity). Nombres de material -> material horneado (texture_items.py).
  RestroomStall   cabina de aseo (1 x 1,5 m) con su inodoro y la puerta entreabierta
  SinkCounter     encimera con tres lavabos, grifos y espejo (2,4 m)
  CCTVDesk        mesa de vigilancia con un muro de 9 monitores, teclados y papeles (2,4 m)
  Couch           sofa de tres plazas gastado
  VendingMachine  maquina expendedora con frontal de botellas
  KitchenCounter  encimera de office con armarios, fregadero, microondas y cafetera (2,4 m)
  GunRack         armero de pared con fusiles y escopetas encadenados (1,6 m)
  TireStack       pila de neumaticos con una llanta suelta
"""
import math
import os
import random
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())


def make_stall():
    m = Mesher("stall", {"body": (0.55, 0.58, 0.55), "plastic_white": (0.9, 0.9, 0.88), "steel": (0.6, 0.6, 0.62), "plastic_black": (0.05, 0.05, 0.05)})
    W, D, H = 1.0, 1.5, 2.0
    for sx in (-1, 1):
        m.box((sx * W / 2, 0, H / 2 + 0.15), (0.03, D, H - 0.3), "body")                     # mamparas laterales
    m.box((0, D / 2, H / 2 + 0.15), (W, 0.03, H - 0.3), "body")                               # fondo
    m.box((-0.15, -D / 2 - 0.25, H / 2 + 0.15), (0.6, 0.03, H - 0.3), "body", rot=(0, 0, -35))   # puerta entreabierta
    m.box((0.12, -D / 2 - 0.42, 1.05), (0.03, 0.03, 0.12), "steel", rot=(0, 0, -35))
    for sx in (-1, 1):
        m.box((sx * W / 2, -D / 2, 0.08), (0.05, 0.05, 0.16), "steel")                        # pies
    # inodoro
    m.loft_z([(0, 0.35, 0, 0.13, 0.17), (0, 0.32, 0.25, 0.16, 0.22), (0, 0.3, 0.4, 0.18, 0.24)], "plastic_white", 16)
    m.loft_z([(0, 0.3, 0.4, 0.19, 0.25), (0, 0.3, 0.43, 0.19, 0.25)], "plastic_black", 16)
    m.box((0, 0.65, 0.65), (0.42, 0.18, 0.4), "plastic_white", bevel=0.02)
    m.box((0.3, 0.7, 0.85), (0.04, 0.04, 0.04), "steel")
    m.box((0.42, 0.3, 0.8), (0.12, 0.08, 0.12), "plastic_white")                               # portarrollos
    return m.finish("RestroomStall")


def make_sink():
    m = Mesher("sink", {"plastic_white": (0.9, 0.9, 0.88), "body": (0.35, 0.37, 0.38), "steel": (0.7, 0.7, 0.72), "plastic_glass": (0.55, 0.6, 0.62)})
    L, D = 2.4, 0.55
    m.box((0, 0.05, 0.84), (L, D, 0.06), "body", bevel=0.01)
    m.box((0, D / 2 - 0.02, 0.45), (L, 0.04, 0.8), "body")
    for x in (-0.75, 0, 0.75):
        m.loft_z([(x, 0.0, 0.66, 0.16, 0.12), (x, 0.0, 0.86, 0.22, 0.17)], "plastic_white", 16)
        m.loft_z([(x, 0.18, 0.86, 0.02, 0.02), (x, 0.18, 1.02, 0.02, 0.02)], "steel", 8)
        m.box((x, 0.1, 1.02), (0.03, 0.16, 0.03), "steel")
    m.box((0, D / 2 + 0.01, 1.55), (L - 0.1, 0.02, 0.8), "plastic_glass")                       # espejo
    m.box((0, D / 2 + 0.02, 1.55), (L, 0.01, 0.86), "steel")
    m.box((1.05, 0.15, 1.2), (0.18, 0.1, 0.25), "plastic_white")                                 # dispensador
    return m.finish("SinkCounter")


def make_cctv():
    rng = random.Random(4)
    m = Mesher("cctv", {"wood": (0.35, 0.3, 0.25), "plastic": (0.1, 0.1, 0.11), "screen": (0.08, 0.12, 0.12), "screen_lit": (0.25, 0.32, 0.3), "keys": (0.75, 0.74, 0.7), "paper": (0.88, 0.86, 0.8), "body": (0.3, 0.32, 0.33)})
    L, D = 2.4, 0.9
    m.box((0, 0, 0.74), (L, D, 0.05), "wood", bevel=0.01)
    for sx in (-1, 1):
        m.box((sx * (L / 2 - 0.25), 0, 0.37), (0.45, D - 0.1, 0.72), "body")
    m.box((0, 0.38, 1.35), (L, 0.12, 1.2), "body")                                                   # bastidor
    for r in range(3):
        for c in range(3):
            x, z = -0.75 + c * 0.75, 0.95 + r * 0.4
            m.box((x, 0.3, z), (0.7, 0.08, 0.38), "plastic", bevel=0.01)
            m.box((x, 0.255, z), (0.64, 0.01, 0.32), rng.choice(("screen", "screen_lit", "screen_lit")))
    for x in (-0.6, 0.6):
        m.box((x, -0.2, 0.78), (0.44, 0.15, 0.025), "plastic", bevel=0.004)
        m.box((x, -0.2, 0.795), (0.4, 0.12, 0.008), "keys")
    m.box((0, -0.15, 0.775), (0.3, 0.25, 0.01), "paper", rot=(0, 0, 10))
    m.box((-1.0, -0.15, 0.8), (0.08, 0.08, 0.1), "plastic", rot=(0, 0, 20))                         # taza
    return m.finish("CCTVDesk")


def make_couch():
    m = Mesher("couch", {"fabric_couch": (0.25, 0.18, 0.13), "wood": (0.2, 0.12, 0.07)})
    L = 2.0
    m.box((0, 0.05, 0.25), (L, 0.85, 0.3), "fabric_couch", bevel=0.05)
    for i in range(3):
        m.box((-L / 3 + i * L / 3, -0.02, 0.45), (L / 3 - 0.04, 0.7, 0.16), "fabric_couch", bevel=0.05, rot=(0, 0, (i - 1) * 2))
    m.box((0, 0.38, 0.65), (L, 0.2, 0.6), "fabric_couch", bevel=0.06, rot=(-8, 0, 0))
    for sx in (-1, 1):
        m.box((sx * (L / 2 + 0.05), 0.05, 0.45), (0.18, 0.85, 0.42), "fabric_couch", bevel=0.05)
        for sy in (-1, 1):
            m.box((sx * (L / 2 - 0.05), sy * 0.35, 0.05), (0.06, 0.06, 0.1), "wood")
    m.box((0.5, -0.05, 0.56), (0.45, 0.12, 0.35), "fabric_couch", bevel=0.05, rot=(70, 0, 25))        # cojin caido
    return m.finish("Couch")


def make_vending():
    rng = random.Random(6)
    m = Mesher("vending", {"body_red": (0.55, 0.08, 0.06), "plastic_glass": (0.3, 0.35, 0.38), "plastic_black": (0.05, 0.05, 0.05), "steel": (0.6, 0.6, 0.62),
                           "plastic_blue": (0.12, 0.3, 0.6), "plastic_green": (0.15, 0.45, 0.2), "plastic_yellow": (0.7, 0.55, 0.05), "plastic_white": (0.9, 0.9, 0.88)})
    W, D, H = 0.95, 0.8, 1.85
    m.box((0, 0, H / 2), (W, D, H), "body_red", bevel=0.02)
    m.box((-0.12, -D / 2 - 0.005, 1.05), (0.62, 0.01, 1.3), "plastic_glass")
    for r in range(5):
        for c in range(5):
            m.loft_z([(-0.36 + c * 0.12, -D / 2 + 0.1, 0.5 + r * 0.25, 0.035, 0.035), (-0.36 + c * 0.12, -D / 2 + 0.1, 0.5 + r * 0.25 + 0.17, 0.035, 0.035)],
                     rng.choice(("plastic_blue", "plastic_green", "plastic_yellow", "plastic_white")), 8)
    m.box((0.35, -D / 2 - 0.01, 1.2), (0.16, 0.02, 0.5), "plastic_black")
    m.box((0.35, -D / 2 - 0.02, 1.3), (0.08, 0.01, 0.05), "steel")
    m.box((-0.1, -D / 2 - 0.02, 0.22), (0.5, 0.04, 0.16), "plastic_black")
    return m.finish("VendingMachine")


def make_kitchen():
    m = Mesher("kitchen", {"wood": (0.5, 0.42, 0.32), "body": (0.75, 0.74, 0.7), "steel": (0.65, 0.65, 0.67), "plastic_black": (0.05, 0.05, 0.05), "plastic_white": (0.88, 0.88, 0.85), "plastic_red": (0.5, 0.08, 0.06)})
    L, D = 2.4, 0.6
    m.box((0, 0, 0.45), (L, D, 0.88), "body")
    for i in range(4):
        x = -L / 2 + 0.3 + i * 0.6
        m.box((x, -D / 2 - 0.01, 0.46), (0.56, 0.02, 0.8), "body", bevel=0.004)
        m.box((x + 0.2, -D / 2 - 0.03, 0.75), (0.1, 0.02, 0.02), "steel")
    m.box((0, 0, 0.91), (L + 0.04, D + 0.04, 0.04), "wood")
    m.box((0.3, -0.02, 0.86), (0.5, 0.4, 0.1), "steel")                                                # fregadero
    m.box((0.3, 0.2, 1.0), (0.03, 0.03, 0.2), "steel")
    m.box((-0.75, 0.05, 1.08), (0.5, 0.38, 0.3), "plastic_black", bevel=0.01)                          # microondas
    m.box((-0.82, -0.15, 1.08), (0.3, 0.01, 0.2), "plastic_white")
    m.box((0.95, 0.1, 1.1), (0.22, 0.25, 0.34), "plastic_black", bevel=0.01)                          # cafetera
    m.loft_z([(0.95, -0.02, 0.94, 0.06, 0.06), (0.95, -0.02, 1.08, 0.06, 0.06)], "plastic_white", 10)
    m.box((0, D / 2 - 0.15, 1.9), (L, 0.35, 0.65), "body")                                              # armarios altos
    for i in range(4):
        m.box((-L / 2 + 0.3 + i * 0.6, D / 2 - 0.33, 1.9), (0.56, 0.02, 0.6), "body", bevel=0.004)
    m.loft_z([(-0.2, 0.1, 0.93, 0.045, 0.045), (-0.2, 0.1, 1.03, 0.045, 0.045)], "plastic_red", 10)     # taza
    return m.finish("KitchenCounter")


def make_gunrack():
    m = Mesher("gunrack", {"wood": (0.35, 0.22, 0.12), "steel": (0.25, 0.25, 0.27), "steel_dark": (0.08, 0.08, 0.08), "plastic_black": (0.05, 0.05, 0.05)})
    L, H = 1.6, 1.9
    m.box((0, 0.12, H / 2 + 0.1), (L, 0.06, H), "wood", bevel=0.01)
    m.box((0, -0.05, 0.25), (L, 0.3, 0.06), "wood")
    m.box((0, 0.0, 1.45), (L, 0.2, 0.05), "wood")
    for i in range(6):
        x = -0.65 + i * 0.26
        if i == 4:
            continue                                                                                      # hueco: falta uno
        long = i % 2 == 0
        m.box((x, -0.02, 0.28 + (0.6 if long else 0.5)), (0.05, 0.06, 1.2 if long else 1.0), "steel_dark")   # canon/cuerpo
        m.box((x, -0.02, 0.4), (0.07, 0.1, 0.3), "wood" if long else "plastic_black")                       # culata
    m.box((0, -0.12, 1.1), (L - 0.1, 0.02, 0.02), "steel")                                             # cadena
    m.box((0.75, -0.13, 1.1), (0.06, 0.04, 0.08), "steel")                                             # candado
    return m.finish("GunRack")


def make_tires():
    m = Mesher("tires", {"plastic_black": (0.04, 0.04, 0.04), "steel": (0.55, 0.55, 0.57)})
    for i in range(4):
        z = 0.12 + i * 0.24
        m.loft_z([(0.02 * i, 0, z - 0.12, 0.33, 0.33), (0.02 * i, 0, z - 0.1, 0.36, 0.36), (0.02 * i, 0, z + 0.1, 0.36, 0.36), (0.02 * i, 0, z + 0.12, 0.33, 0.33)], "plastic_black", 20)
    m.loft_y([(0.7, -0.12, 0.36, 0.36, 0.36), (0.7, 0.12, 0.36, 0.36, 0.36)], "plastic_black", 20)    # una de pie
    m.loft_y([(0.7, -0.13, 0.36, 0.2, 0.2), (0.7, 0.13, 0.36, 0.2, 0.2)], "steel", 16)
    return m.finish("TireStack")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_stall, make_sink, make_cctv, make_couch, make_vending, make_kitchen, make_gunrack, make_tires):
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
