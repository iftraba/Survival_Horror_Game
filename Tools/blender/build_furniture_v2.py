"""Muebles de oficina v2 (2026-10-14, lavado de cara del interior, tanda 2): taquilla, archivador y escritorio con mas detalle.

    blender --background --python build_furniture_v2.py -- <carpeta ABSOLUTA de salida> [Locker FilingCabinet Desk]
    (en un Blender abierto: exec(open(ruta).read()); preview("Locker"); preview_all())

Mismas medidas y orientacion que los de build_items.py (frente hacia -Y, origen en el centro de la base), asi que sustituyen a los antiguos sin tocar la escena.
Nombres de material: body* = chapa pintada, handle* = metal, wood* = madera, dark = plastico, paper, brass.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE5 = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE5, "build_items.py"), encoding="utf-8").read())


def tube(m, pts, r, mat, segs=8):
    pts = [Vector(p) for p in pts]
    rings = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        q = t.to_track_quat("Z", "Y").to_matrix()
        rings.append([p + q @ Vector((r * math.cos(2 * math.pi * k / segs), r * math.sin(2 * math.pi * k / segs), 0.0)) for k in range(segs)])
    m._loft_rings(rings, mat, True)


def make_locker():
    m = Mesher("locker", {"body": (0.30, 0.37, 0.35), "dark": (0.04, 0.04, 0.05), "handle": (0.55, 0.55, 0.58), "brass": (0.62, 0.48, 0.2), "paper": (0.78, 0.76, 0.66)})
    w, d, h = 0.90, 0.48, 1.85
    zb = 0.10
    m.box((0, 0, zb + (h - zb) / 2 - 0.02), (w, d, h - zb - 0.04), "body", bevel=0.012)                       # cuerpo
    m.box((0, 0.0, h - 0.025), (w + 0.03, d + 0.03, 0.05), "body", bevel=0.012)                                 # tapa que sobresale
    m.box((0, -0.03, h + 0.003), (w - 0.06, d - 0.12, 0.012), "body", rot=(-6, 0, 0))                            # remate inclinado
    m.box((0, 0, zb / 2), (w - 0.04, d - 0.04, zb), "dark", bevel=0.006)                                        # zocalo
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((0.41 * sx, 0.20 * sy, 0.012), (0.06, 0.06, 0.024), "dark")                                    # patas niveladoras
    hw = w / 2
    for sx in (-1, 1):                                                                                           # dos puertas hundidas
        cx = sx * hw / 2
        m.box((cx, -d / 2 - 0.006, 0.95), (hw - 0.03, 0.012, 1.69), "body", bevel=0.004)                          # hoja
        m.box((cx, -d / 2 - 0.0155, 0.93), (hw - 0.12, 0.007, 1.52), "body", bevel=0.003)                          # panel rehundido en relieve
        for i in range(9):                                                                                       # rejilla de ventilacion de lamas inclinadas arriba
            m.box((cx, -d / 2 - 0.016, 1.68 - i * 0.034), (hw - 0.17, 0.012, 0.012), "dark", rot=(-28, 0, 0))
        for i in range(4):                                                                                       # y abajo
            m.box((cx, -d / 2 - 0.014, 0.28 + i * 0.034), (hw - 0.17, 0.012, 0.012), "dark", rot=(-28, 0, 0))
        m.box((cx, -d / 2 - 0.0175, 1.38), (0.075, 0.004, 0.034), "paper", bevel=0.001)                          # etiqueta con numero (sin texto)
        m.box((cx, -d / 2 - 0.0165, 1.42), (0.027, 0.004, 0.01), "brass")                                         # placa de latón
    # manilla de palanca con cerradura (en la cara de cierre de cada puerta)
    for sx in (-1, 1):
        hx = sx * 0.04
        m.box((hx, -d / 2 - 0.021, 1.0), (0.046, 0.012, 0.16), "handle", bevel=0.004)                              # placa
        m.box((hx, -d / 2 - 0.036, 1.0), (0.016, 0.022, 0.1), "handle", bevel=0.004)                               # agarre
        m.loft_y([(hx, -d / 2 - 0.027, 0.92, 0.009, 0.009), (hx, -d / 2 - 0.033, 0.92, 0.009, 0.009)], "dark", 10)  # cerradura
    for z in (0.22, 1.55, 1.76):                                                                                 # bisagras vistas
        for sx in (-1, 1):
            m.box((sx * (w / 2 - 0.004), -d / 2 - 0.002, z), (0.012, 0.012, 0.06), "handle")
    m.box((0, -d / 2 - 0.0095, 0.95), (0.008, 0.012, 1.69), "dark")                                              # union entre puertas
    return m.finish("Locker")


def make_filing_cabinet():
    m = Mesher("cabinet", {"body": (0.34, 0.37, 0.33), "dark": (0.04, 0.04, 0.05), "handle": (0.58, 0.58, 0.6), "paper": (0.80, 0.78, 0.66), "brass": (0.62, 0.48, 0.2)})
    w, d, h = 0.46, 0.62, 1.32
    m.box((0, 0, h / 2 + 0.03), (w, d, h - 0.06), "body", bevel=0.012)                                         # cuerpo
    m.box((0, 0, 0.03), (w - 0.03, d - 0.04, 0.06), "dark", bevel=0.006)                                        # zocalo
    m.box((0, 0, h - 0.015), (w + 0.02, d + 0.02, 0.03), "body", bevel=0.01)                                    # tapa
    # cuatro cajones (el modelo viejo traia tres: se mantienen tres de mayor altura y una cerradura central superior)
    for i in range(3):
        z = 0.22 + i * 0.42
        m.box((0, -d / 2 - 0.007, z), (w - 0.04, 0.014, 0.385), "body", bevel=0.005)                              # frente
        m.box((0, -d / 2 - 0.0175, z), (w - 0.10, 0.007, 0.31), "body", bevel=0.003)                              # panel rehundido
        tube(m, [(-0.09, -d / 2 - 0.045, z + 0.1), (-0.09, -d / 2 - 0.03, z + 0.1), (0.09, -d / 2 - 0.03, z + 0.1), (0.09, -d / 2 - 0.045, z + 0.1)], 0.007, "handle", 6)   # tirador de barra
        m.box((0, -d / 2 - 0.0185, z + 0.12), (0.115, 0.005, 0.05), "handle", bevel=0.001)                       # marco del portaetiquetas
        m.box((0, -d / 2 - 0.021, z + 0.12), (0.1, 0.004, 0.036), "paper")                                       # etiqueta
        m.box((0, -d / 2 - 0.002, z - 0.205), (w - 0.02, 0.006, 0.008), "dark")                                  # junta entre cajones
    m.loft_y([(0.0, -d / 2 - 0.018, h - 0.15, 0.011, 0.011), (0.0, -d / 2 - 0.026, h - 0.15, 0.011, 0.011)], "brass", 12)   # cerradura de todo el mueble
    for sx in (-1, 1):
        m.box((sx * (w / 2 + 0.002), -0.05, h / 2), (0.006, d - 0.2, h - 0.2), "body")                           # nervio lateral
    return m.finish("FilingCabinet")


def make_desk():
    m = Mesher("desk", {"wood": (0.25, 0.16, 0.09), "dark": (0.10, 0.07, 0.05), "handle": (0.55, 0.55, 0.57), "metal": (0.2, 0.2, 0.22), "plastic": (0.03, 0.03, 0.035)})
    # tablero con canto y vuelo
    m.box((0, 0, 0.745), (1.50, 0.75, 0.04), "wood", bevel=0.008)
    m.box((0, -0.372, 0.742), (1.50, 0.012, 0.046), "dark", bevel=0.003)                                       # canto delantero oscuro
    m.box((0, 0.372, 0.742), (1.50, 0.012, 0.046), "dark", bevel=0.003)
    for sx in (-1, 1):
        m.box((sx * 0.745, 0, 0.742), (0.012, 0.75, 0.046), "dark", bevel=0.003)
    # lateral izquierdo: pata tipo panel con zocalo
    m.box((-0.66, 0, 0.37), (0.04, 0.70, 0.70), "wood", bevel=0.004)
    m.box((-0.66, 0.0, 0.04), (0.05, 0.62, 0.08), "dark", bevel=0.004)
    # cajonera derecha con tres cajones y zocalo
    m.box((0.45, 0.0, 0.40), (0.58, 0.68, 0.64), "wood", bevel=0.006)
    m.box((0.45, 0.0, 0.04), (0.54, 0.62, 0.08), "dark", bevel=0.004)
    for i in range(3):
        z = 0.14 + i * 0.20
        m.box((0.45, -0.346, z + 0.045), (0.53, 0.014, 0.18), "wood", bevel=0.003)                              # frente
        m.box((0.45, -0.3535, z + 0.045), (0.46, 0.006, 0.12), "wood", bevel=0.002)                             # panel
        tube(m, [(0.37, -0.375, z + 0.065), (0.37, -0.365, z + 0.065), (0.53, -0.365, z + 0.065), (0.53, -0.375, z + 0.065)], 0.006, "handle", 6)   # tirador de barra
        m.box((0.45, -0.3415, z - 0.052), (0.55, 0.004, 0.006), "dark")                                         # sombra entre cajones
    m.loft_y([(0.45, -0.352, 0.63, 0.01, 0.01), (0.45, -0.360, 0.63, 0.01, 0.01)], "metal", 12)               # cerradura
    # panel trasero a media altura (privacidad)
    m.box((-0.1, 0.33, 0.50), (1.15, 0.02, 0.45), "wood", bevel=0.004)
    # travesaño bajo el tablero y pasacables
    m.box((-0.1, 0.30, 0.69), (1.15, 0.04, 0.05), "dark", bevel=0.003)
    m.loft_z([(-0.45, 0.28, 0.756, 0.035, 0.028), (-0.45, 0.28, 0.77, 0.035, 0.028)], "plastic", 14)           # pasacables
    # bandeja de lápices deslizante bajo el tablero
    m.box((-0.15, -0.22, 0.71), (0.5, 0.2, 0.012), "dark")
    return m.finish("Desk")


BUILDERS = {"Locker": make_locker, "FilingCabinet": make_filing_cabinet, "Desk": make_desk}


def view(rx, rz, dist, loc):
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            r3d = area.spaces[0].region_3d
            r3d.view_rotation = Euler((math.radians(rx), 0, math.radians(rz))).to_quaternion()
            r3d.view_distance = dist
            r3d.view_location = loc


def preview(name, rz=-30):
    _clear()
    obj = BUILDERS[name]()
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    d = obj.dimensions
    view(80, rz, max(d.x, d.y, d.z) * 2.2 + 0.5, (0, 0, d.z * 0.5))
    print(name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    return obj


def preview_all():
    _clear()
    x = 0.0
    for name, fn in BUILDERS.items():
        obj = fn()
        d = obj.dimensions
        obj.location = (x + d.x / 2 + 0.2, 0, 0)
        x += d.x + 0.6
        print(name, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    view(78, -22, x * 0.9, (x / 2, 0, 0.8))


if __name__ == "__main__" and "--" in sys.argv:
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
