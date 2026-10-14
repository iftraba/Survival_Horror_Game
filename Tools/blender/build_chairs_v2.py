"""Sillas v2 (2026-10-14, lavado de cara del interior, tanda 3): silla de oficina, silla giratoria y silla de auditorio con mas detalle.

    blender --background --python build_chairs_v2.py -- <carpeta ABSOLUTA de salida> [Chair SwivelChair AuditoriumChair]
    (en un Blender abierto: exec(open(ruta).read()); preview_all())

Mismas medidas, orientacion (frente hacia -Y, respaldo en +Y) y origen que las de build_items.py y build_office.py. Biseles <= la mitad del grosor de la caja:
si no, la caja se auto-intersecta y su AO sale negro/oxidado al hornear.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE6 = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE6, "build_items.py"), encoding="utf-8").read())


def tube(m, pts, r, mat, segs=8, r1=None):
    pts = [Vector(p) for p in pts]
    rings = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        q = t.to_track_quat("Z", "Y").to_matrix()
        rr = r if r1 is None else r + (r1 - r) * i / max(1, len(pts) - 1)
        rings.append([p + q @ Vector((rr * math.cos(2 * math.pi * k / segs), rr * math.sin(2 * math.pi * k / segs), 0.0)) for k in range(segs)])
    m._loft_rings(rings, mat, True)


def cushion(m, c, size, mat, bulge=0.012, nx=6, ny=6):
    """Cojin con la cara de arriba abombada: rejilla de nx x ny cuyo centro sube 'bulge'. Cierra con una base plana."""
    cx, cy, cz = c
    sx, sy, sz = size
    bm = m.bm
    top = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            u, v = i / nx - 0.5, j / ny - 0.5
            edge = 1.0 - (abs(u) * 2) ** 3 * 0.5 - (abs(v) * 2) ** 3 * 0.5
            row.append(bm.verts.new((cx + u * sx, cy + v * sy, cz + sz / 2 + bulge * max(0.0, edge))))
        top.append(row)
    bot = [[bm.verts.new((cx + (i / nx - 0.5) * sx * 0.96, cy + (j / ny - 0.5) * sy * 0.96, cz - sz / 2)) for i in range(nx + 1)] for j in range(ny + 1)]
    mi = m._idx(mat)
    def face(vs):
        f = bm.faces.new(vs)
        f.material_index = mi
        f.smooth = True
    for j in range(ny):
        for i in range(nx):
            face((top[j][i], top[j][i + 1], top[j + 1][i + 1], top[j + 1][i]))
            face((bot[j][i], bot[j + 1][i], bot[j + 1][i + 1], bot[j][i + 1]))
    for i in range(nx):
        face((top[0][i], bot[0][i], bot[0][i + 1], top[0][i + 1]))
        face((top[ny][i + 1], bot[ny][i + 1], bot[ny][i], top[ny][i]))
    for j in range(ny):
        face((top[j][0], top[j + 1][0], bot[j + 1][0], bot[j][0]))
        face((top[j + 1][nx], top[j][nx], bot[j][nx], bot[j + 1][nx]))


def make_chair():
    """Silla de oficina basica (visitas): asiento y respaldo de tapizado con costura, estructura tubular cromada, patas con tacos."""
    m = Mesher("chair", {"seat": (0.14, 0.12, 0.10), "metal": (0.5, 0.5, 0.54), "plastic": (0.03, 0.03, 0.035), "fabric": (0.12, 0.13, 0.1)})
    cushion(m, (0, 0.0, 0.455), (0.44, 0.42, 0.05), "seat", 0.014)
    m.box((0, 0.0, 0.42), (0.40, 0.38, 0.02), "plastic", bevel=0.008)                                      # base del asiento
    # respaldo: dos paneles con curvatura y una costura central
    cushion(m, (0, 0.215, 0.78), (0.40, 0.05, 0.40), "seat", 0.012, 6, 6)
    m.box((0, 0.22, 0.545), (0.30, 0.03, 0.06), "plastic", bevel=0.01)
    # estructura tubular: patas delanteras y traseras unidas por travesanos, con respaldo continuo
    for sx in (-1, 1):
        x = 0.185 * sx
        tube(m, [(x, -0.17, 0.02), (x, -0.17, 0.42), (x, -0.12, 0.42)], 0.0125, "metal", 8)                 # pata delantera
        tube(m, [(x, 0.2, 0.02), (x, 0.2, 0.42), (x, 0.22, 0.62), (x, 0.235, 1.0)], 0.0125, "metal", 8)      # pata trasera y montante del respaldo
        m.ellipsoid((x, -0.17, 0.012), (0.022, 0.022, 0.014), "plastic", 8, 4)                                # tacos
        m.ellipsoid((x, 0.2, 0.012), (0.022, 0.022, 0.014), "plastic", 8, 4)
    for y, z in ((-0.17, 0.12), (0.2, 0.12), (-0.12, 0.42), (0.2, 0.42)):
        tube(m, [(-0.185, y, z), (0.185, y, z)], 0.01, "metal", 6)                                           # travesanos
    tube(m, [(-0.185, 0.235, 1.0), (0.185, 0.235, 1.0)], 0.0125, "metal", 8)                                 # barra superior
    return m.finish("Chair")


def make_swivel_chair():
    """Silla giratoria ergonomica: base de 5 brazos con ruedas dobles, pistón de gas con funda, mecanismo, asiento y respaldo con lumbar, reposabrazos."""
    m = Mesher("swivelchair", {"fabric": (0.11, 0.12, 0.15), "plastic": (0.05, 0.05, 0.055), "steel": (0.5, 0.5, 0.53), "rubber": (0.02, 0.02, 0.022)})
    for k in range(5):
        a = 2 * math.pi * k / 5
        ca, sa = math.cos(a), math.sin(a)
        tube(m, [(0.04 * ca, 0.04 * sa, 0.095), (0.17 * ca, 0.17 * sa, 0.075), (0.31 * ca, 0.31 * sa, 0.055)], 0.022, "plastic", 8, 0.014)   # brazo ahusado
        wx, wy = 0.31 * ca, 0.31 * sa
        for off in (-0.012, 0.012):                                                                                                         # rueda doble
            px, py = wx - sa * off * 2.0, wy + ca * off * 2.0
            m._loft_rings([[(px + (-0.004 if dd < 0 else 0.004) * ca + 0.0, py + (-0.004 if dd < 0 else 0.004) * sa, 0.03 + 0.03 * math.sin(2 * math.pi * jj / 10))
                            for jj in range(10)] for dd in (-1, 1)], "rubber", True) if False else None
        m.ellipsoid((wx, wy, 0.03), (0.026, 0.026, 0.03), "rubber", 10, 5)
        m.box((wx, wy, 0.052), (0.03, 0.03, 0.02), "plastic", bevel=0.005)
    tube(m, [(0, 0, 0.09), (0, 0, 0.33)], 0.03, "steel", 12)                                                                                  # pistón
    tube(m, [(0, 0, 0.09), (0, 0, 0.22)], 0.04, "plastic", 12)                                                                                # funda
    m.box((0, 0.0, 0.405), (0.32, 0.30, 0.04), "plastic", bevel=0.015)                                                                        # mecanismo bajo el asiento
    cushion(m, (0, -0.01, 0.46), (0.5, 0.48, 0.07), "fabric", 0.02, 7, 7)                                                                    # asiento
    m.box((0, 0.17, 0.43), (0.12, 0.05, 0.02), "plastic", bevel=0.006)                                                                       # palanca de altura
    tube(m, [(0.16, 0.0, 0.42), (0.23, 0.0, 0.42), (0.26, -0.02, 0.4)], 0.008, "plastic", 6)
    # respaldo: soporte curvo de plastico, panel superior y lumbar
    tube(m, [(0, 0.15, 0.42), (0, 0.24, 0.55), (0, 0.255, 0.7)], 0.022, "plastic", 8)
    cushion(m, (0, 0.265, 0.84), (0.44, 0.06, 0.42), "fabric", 0.02, 7, 7)                                                                   # parte alta
    cushion(m, (0, 0.245, 0.58), (0.38, 0.06, 0.16), "fabric", 0.014, 6, 4)                                                                  # lumbar
    # reposabrazos con almohadillas
    for sx in (-1, 1):
        x = 0.27 * sx
        tube(m, [(x, 0.02, 0.45), (x, 0.02, 0.58)], 0.012, "plastic", 6)
        m.box((x, -0.02, 0.605), (0.06, 0.28, 0.03), "plastic", bevel=0.012)
    return m.finish("SwivelChair")


def make_auditorium_chair():
    """Butaca de sala de actos: asiento abatible tapizado, respaldo alto con costuras, laterales de acero con reposabrazos y base de pie."""
    m = Mesher("audchair", {"fabric": (0.10, 0.12, 0.26), "steel": (0.34, 0.35, 0.38), "plastic": (0.03, 0.03, 0.035), "wood": (0.2, 0.12, 0.07)})
    for sx in (-1, 1):
        x = 0.24 * sx
        m.box((x, -0.02, 0.23), (0.03, 0.08, 0.46), "steel", bevel=0.008)                                      # montante lateral (pie)
        m.box((x, 0.1, 0.45), (0.035, 0.46, 0.04), "steel", bevel=0.01)                                         # larguero del asiento
        tube(m, [(x, 0.16, 0.45), (x, 0.22, 0.7), (x, 0.24, 0.9)], 0.015, "steel", 8)                           # montante del respaldo
        m.box((x, -0.02, 0.025), (0.06, 0.2, 0.05), "steel", bevel=0.01)                                        # pie
        m.box((x, 0.02, 0.62), (0.05, 0.4, 0.035), "wood", bevel=0.012)                                         # reposabrazos de madera
        tube(m, [(x, 0.18, 0.455), (x, 0.18, 0.62)], 0.012, "steel", 6)
    cushion(m, (0, 0.0, 0.485), (0.46, 0.44, 0.06), "fabric", 0.02, 7, 7)                                       # asiento acolchado
    m.box((0, 0.0, 0.445), (0.44, 0.40, 0.025), "plastic", bevel=0.008)                                        # base del asiento
    cushion(m, (0, 0.235, 0.78), (0.44, 0.07, 0.46), "fabric", 0.025, 7, 8)                                     # respaldo
    m.box((0, 0.26, 0.85), (0.34, 0.012, 0.012), "plastic")                                                   # costuras
    m.box((0, 0.26, 0.74), (0.34, 0.012, 0.012), "plastic")
    m.box((0, 0.0, 0.2), (0.48, 0.03, 0.03), "steel", bevel=0.008)                                             # travesano entre los pies
    return m.finish("AuditoriumChair")


BUILDERS = {"Chair": make_chair, "SwivelChair": make_swivel_chair, "AuditoriumChair": make_auditorium_chair}


def view(rx, rz, dist, loc):
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            r3d = area.spaces[0].region_3d
            r3d.view_rotation = Euler((math.radians(rx), 0, math.radians(rz))).to_quaternion()
            r3d.view_distance = dist
            r3d.view_location = loc


def preview_all():
    _clear()
    x = 0.0
    for name, fn in BUILDERS.items():
        obj = fn()
        d = obj.dimensions
        obj.location = (x + d.x / 2 + 0.1, 0, 0)
        x += d.x + 0.4
        print(name, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    view(72, -30, x * 1.6, (x / 2, 0, 0.5))


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
