"""Coches patrulla de la comisaria (2026-10-10): modelo mas detallado, en dos versiones.

    blender --background --python build_police_cars.py -- <carpeta Assets/_Project/Art/Props/Exterior> [PoliceCar PoliceCarClean]

  PoliceCar       quemado y abandonado: chapa chamuscada, lunas rotas, neumaticos desinflados, luces rotas (sustituye al de build_exterior.py)
  PoliceCarClean  intacto: puertas blancas, capo y maletero negros, lunas, barra de luces, matricula y faros (los coches del garaje)
Mismas medidas que el anterior (4,7 x 1,82 m, origen en el centro de la base, frente a -Y de Blender = +Z de Unity), asi que sustituye al
modelo antiguo sin tocar la escena. Los nombres de material deciden el material horneado de texture_items.py (body = chapa pintada,
rust = chamusco/oxido, metal = cromo; el resto plastico).
"""
import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())

L, W = 4.7, 1.82


def wheel(m, x, y, flat, side):
    r = 0.34 if flat else 0.37
    rings = [[(x + s * 0.12, y + r * math.cos(a), r + r * math.sin(a)) for a in (2 * math.pi * k / 20 for k in range(20))] for s in (-1, 1)]
    m._loft_rings(rings, "plastic", True)                                                                  # neumatico
    rr = 0.22
    rings = [[(x + s * 0.125 + side * 0.0, y + rr * math.cos(a), r + rr * math.sin(a)) for a in (2 * math.pi * k / 14 for k in range(14))] for s in (-1, 1)]
    m._loft_rings(rings, "metal_rim", True)                                                                # llanta
    for i in range(5):                                                                                     # radios
        a = i * 2 * math.pi / 5
        m.box((x + side * 0.135, y + 0.1 * math.cos(a), r + 0.1 * math.sin(a)), (0.02, 0.04, 0.16), "metal_dark", rot=(math.degrees(a), 0, 0))
    m.box((x + side * 0.14, y, r), (0.02, 0.07, 0.07), "metal")                                            # tapacubos


def build(burnt):
    pal = {"body": (0.86, 0.86, 0.84), "bodyblack": (0.04, 0.04, 0.05), "plastic": (0.04, 0.04, 0.04), "metal": (0.7, 0.72, 0.75), "metal_dark": (0.12, 0.12, 0.13),
           "metal_rim": (0.5, 0.5, 0.53), "glass": (0.06, 0.09, 0.11), "red": (0.75, 0.04, 0.03), "blue": (0.05, 0.14, 0.75), "lights": (0.92, 0.9, 0.78),
           "plastic_white": (0.9, 0.9, 0.88), "plastic_interior": (0.1, 0.1, 0.11)}
    if burnt:
        pal.update({"body": (0.45, 0.42, 0.38), "bodyblack": (0.03, 0.025, 0.025), "rust": (0.16, 0.08, 0.04), "glass": (0.03, 0.035, 0.04), "lights": (0.2, 0.18, 0.15),
                    "red": (0.25, 0.03, 0.02), "blue": (0.04, 0.06, 0.25)})
    m = Mesher("policecar", pal)
    chamusco = "rust" if burnt else "bodyblack"
    # --- carroceria: bajos negros, puertas blancas, capo y maletero negros
    m.box((0, 0, 0.5), (W, L, 0.42), "bodyblack", bevel=0.06)
    m.box((0, 0.25, 0.65), (W + 0.012, 2.15, 0.46), "body" if not burnt else "body", bevel=0.045)         # puertas
    for sx in (-1, 1):
        m.box((sx * (W / 2 + 0.002), 0.25, 0.4), (0.012, 2.15, 0.07), "bodyblack")                          # faldon
        m.box((sx * (W / 2 + 0.004), 1.17, 0.66), (0.014, 0.025, 0.44), "bodyblack")                         # junta trasera de puerta
        m.box((sx * (W / 2 + 0.004), 0.25, 0.66), (0.014, 0.025, 0.44), "bodyblack")                         # junta central
        m.box((sx * (W / 2 + 0.004), -0.62, 0.66), (0.014, 0.025, 0.44), "bodyblack")                        # junta delantera
        m.box((sx * (W / 2 + 0.02), -0.15, 0.8), (0.03, 0.16, 0.025), "metal")                               # tirador
        m.box((sx * (W / 2 + 0.02), 0.72, 0.8), (0.03, 0.16, 0.025), "metal")
        m.box((sx * (W / 2 + 0.03), -0.75, 0.5), (0.05, 0.05, 0.05), "metal_dark")                           # tapon
    # capo y aletas delanteros
    m.box((0, -1.62, 0.78), (W - 0.02, 1.38, 0.2), "bodyblack", bevel=0.05)
    m.box((0, -1.55, 0.9), (W - 0.12, 1.25, 0.05), chamusco if burnt else "bodyblack", bevel=0.03, rot=(-3, 0, 0))   # capo
    m.box((0, -1.25, 0.925), (0.5, 0.5, 0.012), "metal_dark")                                                # toma de aire
    # maletero
    m.box((0, 1.88, 0.78), (W - 0.02, 1.0, 0.2), "bodyblack", bevel=0.05)
    m.box((0, 1.9, 0.9), (W - 0.12, 0.9, 0.05), chamusco if burnt else "bodyblack", bevel=0.03, rot=(2, 0, 0))
    # --- habitaculo: techo blanco, montantes, lunas
    m.box((0, 0.3, 1.42), (W - 0.3, 1.7, 0.07), "body" if not burnt else "rust", bevel=0.04)                 # techo
    m.box((0, 0.3, 1.2), (W - 0.18, 1.9, 0.4), "body" if not burnt else "rust", bevel=0.06)                  # cuerpo del habitaculo
    for sx in (-1, 1):
        for y in (-0.62, 0.3, 1.2):
            m.box((sx * (W / 2 - 0.1), y, 1.2), (0.05, 0.07, 0.42), "bodyblack")                            # montantes
        m.box((sx * (W / 2 - 0.095), -0.2, 1.2), (0.012, 0.8, 0.32), "glass")                              # ventanillas
        m.box((sx * (W / 2 - 0.095), 0.78, 1.2), (0.012, 0.8, 0.32), "glass")
        m.box((sx * (W / 2 + 0.09), -0.88, 1.02), (0.06, 0.12, 0.08), "bodyblack", bevel=0.01)             # retrovisor
    m.box((0, -0.82, 1.18), (W - 0.28, 0.05, 0.42), "glass", rot=(-30, 0, 0))                                # parabrisas
    m.box((0, 1.48, 1.2), (W - 0.34, 0.05, 0.36), "glass", rot=(26, 0, 0))                                   # luna trasera
    # --- interior visible: salpicadero, volante, asientos y mampara
    m.box((0, -0.65, 1.0), (W - 0.3, 0.3, 0.12), "plastic_interior")
    m.box((-0.45, -0.5, 1.08), (0.02, 0.32, 0.32), "plastic", rot=(0, 25, 0))                                # volante
    for x in (-0.45, 0.45):
        m.box((x, 0.05, 0.98), (0.5, 0.5, 0.1), "plastic_interior"); m.box((x, 0.3, 1.18), (0.5, 0.1, 0.4), "plastic_interior")
    m.box((0, 0.55, 1.2), (W - 0.3, 0.03, 0.36), "metal_dark")                                               # mampara de rejilla
    # --- ruedas y pasos de rueda
    for (x, y) in ((-0.83, -1.45), (0.83, -1.45), (-0.83, 1.5), (0.83, 1.5)):
        wheel(m, x, y, burnt, 1 if x > 0 else -1)
        m.box((x, y, 0.72), (0.3, 0.86, 0.12), "bodyblack", bevel=0.02)                                      # ceja del paso de rueda
    # --- barra de luces, antena
    m.box((0, 0.3, 1.5), (1.2, 0.3, 0.07), "plastic", bevel=0.02)
    m.box((-0.3, 0.3, 1.56), (0.5, 0.26, 0.07), "red", bevel=0.02)
    m.box((0.3, 0.3, 1.56), (0.5, 0.26, 0.07), "blue", bevel=0.02)
    m.box((0, 0.3, 1.56), (0.14, 0.26, 0.07), "plastic_white" if not burnt else "plastic", bevel=0.02)
    m.loft_z([(0.6, 1.5, 1.42, 0.012, 0.012), (0.6, 1.5, 1.72, 0.006, 0.006)], "plastic", 6)                  # antena
    # --- frontal y trasera
    m.box((0, -L / 2 - 0.02, 0.4), (W - 0.1, 0.12, 0.18), "plastic", bevel=0.035)
    m.box((0, L / 2 + 0.02, 0.4), (W - 0.1, 0.12, 0.18), "plastic", bevel=0.035)
    m.box((0, -L / 2 - 0.13, 0.55), (1.0, 0.08, 0.34), "metal_dark")                                         # defensa delantera
    for sx in (-1, 1):
        m.box((sx * 0.62, -L / 2 + 0.01, 0.66), (0.34, 0.05, 0.13), "lights", bevel=0.01)                    # faros
        m.box((sx * 0.7, L / 2 - 0.01, 0.7), (0.3, 0.05, 0.11), "red", bevel=0.01)                           # pilotos
    for i in range(4):
        m.box((0, -L / 2 + 0.008, 0.6 + i * 0.03), (0.6, 0.02, 0.014), "metal_dark")                          # rejilla
    m.box((0, L / 2 + 0.03, 0.55), (0.34, 0.015, 0.17), "plastic_white")                                     # matricula
    m.box((0, L / 2 + 0.025, 0.55), (0.37, 0.012, 0.2), "bodyblack")
    if burnt:
        # chamuscado: manchas de oxido sobre las puertas y el techo, y una luna rota (cunas)
        for sx in (-1, 1):
            m.box((sx * (W / 2 + 0.008), 0.55, 0.78), (0.012, 1.0, 0.28), "rust", bevel=0.01)
            m.box((sx * (W / 2 + 0.008), -0.25, 0.55), (0.012, 0.5, 0.2), "rust", bevel=0.01)
        m.box((0.2, 0.2, 1.46), (0.7, 0.8, 0.012), "rust")
    return m.finish("PoliceCar" if burnt else "PoliceCarClean")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    only = [a for a in sys.argv[sys.argv.index("--") + 2:]]
    os.makedirs(out, exist_ok=True)
    for name, burnt in (("PoliceCar", True), ("PoliceCarClean", False)):
        if only and name not in only:
            continue
        _clear()
        obj = build(burnt)
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
