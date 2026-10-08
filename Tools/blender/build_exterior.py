"""Comisaria v2, fase 2 (2026-10-08): recepcion y exterior.

    blender --background --python build_exterior.py -- <carpeta Assets/_Project/Art/Props/Exterior>

Modelos (origen en el centro de la base, frente a -Y en Blender = +Z en Unity):
  ReceptionDesk  mostrador de recepcion en L con encimera, frente con paneles, monitor, teclado y papeles (4,2 x 2,2 m)
  WaitingBench   banco de espera de listones de madera y patas de hierro (2 m)
  PoliceCar      coche patrulla quemado: carroceria blanca y negra chamuscada, lunas rotas, ruedas, puente de luces
  FenceSegment   tramo de verja de barrotes con puntas, 3 m de ancho y 2,6 m de alto (se encadenan a lo largo de X)
  StreetLamp     farola de brazo
Los nombres de material deciden el material horneado de texture_items.py (body = chapa pintada, rust/iron = oxido,
wood = madera, metal = metal; el resto plastico).
"""
import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())


def make_reception_desk():
    m = Mesher("reception", {"wood": (0.32, 0.20, 0.11), "board": (0.42, 0.40, 0.36), "metal": (0.55, 0.56, 0.58), "plastic": (0.08, 0.08, 0.09),
                             "screen": (0.03, 0.04, 0.05), "paper": (0.88, 0.87, 0.82)})
    L, D, H = 4.2, 0.75, 1.1
    # brazo largo (frente a -Y) y brazo corto (a la derecha, hacia +Y)
    m.box((0, 0, H / 2), (L, 0.06, H), "wood", bevel=0.01)                                  # frente
    for i in range(6):                                                                      # paneles del frente
        x = -L / 2 + L * (i + 0.5) / 6
        m.box((x, -0.035, H * 0.5), (L / 6 - 0.08, 0.012, H * 0.62), "board", bevel=0.006)
    m.box((0, 0.3, H + 0.02), (L + 0.1, D, 0.05), "wood", bevel=0.012)                       # encimera alta
    m.box((0, 0.55, 0.76), (L - 0.1, 0.6, 0.04), "board", bevel=0.006)                       # mesa de trabajo (detras)
    m.box((L / 2 - 0.03, 1.1, H / 2), (0.06, 2.2, H), "wood", bevel=0.01)                    # lateral (brazo corto)
    m.box((L / 2 + 0.25, 1.1, H + 0.02), (0.6, 2.25, 0.05), "wood", bevel=0.012)
    m.box((-L / 2 + 0.03, 0.4, H / 2), (0.06, 0.8, H), "wood", bevel=0.01)                   # cierre izquierdo
    m.box((0, -0.04, 0.06), (L, 0.1, 0.12), "plastic")                                       # zocalo
    for x in (-1.2, 0.6):                                                                   # dos puestos: monitor, teclado, raton
        m.box((x, 0.75, 0.80), (0.22, 0.16, 0.02), "plastic")
        m.box((x, 0.76, 0.98), (0.05, 0.04, 0.34), "plastic")
        m.box((x, 0.74, 1.18), (0.56, 0.04, 0.34), "plastic", bevel=0.01)
        m.box((x, 0.715, 1.18), (0.52, 0.004, 0.30), "screen")
        m.box((x, 0.45, 0.79), (0.44, 0.15, 0.02), "plastic", bevel=0.004)
        m.box((x + 0.32, 0.45, 0.79), (0.06, 0.1, 0.025), "plastic", bevel=0.01)
    for (x, r) in ((-0.3, 8), (-0.2, -5), (1.4, 12)):
        m.box((x, 0.5, 0.785), (0.21, 0.297, 0.004), "paper", rot=(0, 0, r))
    m.box((1.8, 0.3, H + 0.08), (0.3, 0.22, 0.1), "plastic", bevel=0.01)                    # timbre / bandeja
    return m.finish("ReceptionDesk")


def make_bench():
    m = Mesher("bench", {"wood": (0.38, 0.24, 0.12), "iron": (0.12, 0.12, 0.12)})
    L = 2.0
    for i in range(4):                                                                       # asiento
        m.box((0, -0.18 + i * 0.12, 0.45), (L, 0.1, 0.035), "wood", bevel=0.006)
    for i in range(3):                                                                       # respaldo inclinado
        m.box((0, 0.26 + i * 0.02, 0.6 + i * 0.13), (L, 0.035, 0.1), "wood", rot=(15, 0, 0), bevel=0.006)
    for x in (-L / 2 + 0.12, L / 2 - 0.12):
        m.box((x, -0.18, 0.22), (0.05, 0.05, 0.44), "iron")
        m.box((x, 0.2, 0.42), (0.05, 0.05, 0.84), "iron")
        m.box((x, 0.01, 0.43), (0.05, 0.44, 0.04), "iron")
        m.box((x, 0.0, 0.66), (0.05, 0.4, 0.04), "iron")                                     # reposabrazos
    return m.finish("WaitingBench")


def make_police_car():
    m = Mesher("policecar", {"body": (0.85, 0.85, 0.83), "bodyblack": (0.05, 0.05, 0.06), "rust": (0.15, 0.08, 0.05), "glass": (0.05, 0.06, 0.07),
                             "plastic": (0.04, 0.04, 0.04), "metal": (0.55, 0.55, 0.57), "red": (0.7, 0.05, 0.04), "blue": (0.05, 0.15, 0.7), "lights": (0.9, 0.85, 0.7)})
    L, W = 4.7, 1.82
    # chasis bajo, puertas blancas, capo y maletero negros (y chamuscados)
    m.box((0, 0, 0.55), (W, L, 0.5), "bodyblack", bevel=0.05)
    m.box((0, 0.1, 0.62), (W + 0.01, 2.0, 0.44), "body", bevel=0.04)                         # puertas blancas
    m.box((0, -1.65, 0.83), (W - 0.04, 1.3, 0.08), "rust", bevel=0.03)                       # capo quemado
    m.box((0, 1.85, 0.83), (W - 0.06, 0.9, 0.08), "bodyblack", bevel=0.03)                   # maletero
    m.box((0, 0.25, 1.08), (W - 0.2, 2.0, 0.42), "rust", bevel=0.08)                         # habitaculo quemado
    m.box((0, -0.78, 1.06), (W - 0.26, 0.04, 0.34), "glass", rot=(-28, 0, 0))               # parabrisas (roto, oscuro)
    for sx in (-1, 1):
        m.box((sx * (W / 2 - 0.09), 0.25, 1.08), (0.02, 1.7, 0.3), "glass")                  # ventanillas
    for (x, y) in ((-0.83, -1.45), (0.83, -1.45), (-0.83, 1.5), (0.83, 1.5)):                # ruedas
        rings = [[(x + s * 0.12, y + 0.36 * math.cos(a), 0.36 + 0.36 * math.sin(a)) for a in (2 * math.pi * k / 18 for k in range(18))] for s in (-1, 1)]
        m._loft_rings(rings, "plastic", True)
        rings = [[(x + s * 0.125, y + 0.2 * math.cos(a), 0.36 + 0.2 * math.sin(a)) for a in (2 * math.pi * k / 12 for k in range(12))] for s in (-1, 1)]
        m._loft_rings(rings, "metal", True)
    m.box((0, 0.2, 1.33), (1.2, 0.28, 0.1), "plastic", bevel=0.02)                           # puente de luces
    m.box((-0.3, 0.2, 1.36), (0.55, 0.25, 0.08), "red", bevel=0.02)
    m.box((0.3, 0.2, 1.36), (0.55, 0.25, 0.08), "blue", bevel=0.02)
    for sx in (-1, 1):
        m.box((sx * 0.65, -L / 2 + 0.02, 0.62), (0.32, 0.04, 0.12), "lights")                 # faros
        m.box((sx * 0.7, L / 2 - 0.02, 0.66), (0.28, 0.04, 0.1), "red")                       # pilotos
    m.box((0, -L / 2 - 0.02, 0.42), (W - 0.1, 0.1, 0.16), "plastic", bevel=0.03)             # paragolpes
    m.box((0, L / 2 + 0.02, 0.42), (W - 0.1, 0.1, 0.16), "plastic", bevel=0.03)
    m.box((0, -L / 2 - 0.12, 0.55), (0.9, 0.08, 0.3), "metal")                               # defensa delantera (push bar)
    return m.finish("PoliceCar")


def make_fence():
    m = Mesher("fence", {"iron": (0.1, 0.1, 0.1), "metal": (0.25, 0.25, 0.27)})
    W, H = 3.0, 2.6
    for x in (-W / 2, W / 2):
        m.box((x, 0, H / 2 + 0.05), (0.1, 0.1, H + 0.1), "metal", bevel=0.01)                  # postes
        m.box((x, 0, H + 0.12), (0.14, 0.14, 0.06), "metal", bevel=0.01)
    for z in (0.15, H - 0.25, H * 0.5):
        m.box((0, 0, z), (W, 0.05, 0.05), "iron")                                             # travesanos
    n = 13
    for i in range(n):
        x = -W / 2 + W * (i + 0.5) / n
        m.box((x, 0, H / 2), (0.025, 0.025, H - 0.1), "iron")
        m.loft_z([(x, 0, H - 0.05, 0.022, 0.022), (x, 0, H + 0.08, 0.002, 0.002)], "iron", 4)  # punta
    return m.finish("FenceSegment")


def make_street_lamp():
    m = Mesher("streetlamp", {"metal": (0.18, 0.19, 0.2), "glass": (0.9, 0.85, 0.6)})
    m.loft_z([(0, 0, 0, 0.14, 0.14), (0, 0, 0.4, 0.11, 0.11), (0, 0, 0.45, 0.07, 0.07), (0, 0, 6.0, 0.06, 0.06)], "metal", 12)
    m.box((0, -0.7, 6.0), (0.06, 1.4, 0.06), "metal")
    m.box((0, -1.35, 5.92), (0.32, 0.55, 0.14), "metal", bevel=0.03)
    m.box((0, -1.35, 5.84), (0.26, 0.48, 0.02), "glass")
    return m.finish("StreetLamp")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_reception_desk, make_bench, make_police_car, make_fence, make_street_lamp):
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
