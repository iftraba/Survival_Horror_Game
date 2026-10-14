"""Mobiliario urbano de la calle (2026-10-14, fase 2 del lavado de cara del exterior).

    blender --background --python build_street.py -- <carpeta ABSOLUTA de salida> [Nombre ...]
    (en un Blender abierto: exec(open(ruta).read()); preview("Hydrant"); preview_all(); view(...))

Modelos: TrafficLight, SignStop, SignOneWay, SignNoParking, Hydrant, TrashCan, Dumpster, NewsBox, Mailbox, ParkingMeter, JerseyBarrier, TrafficCone,
Barricade, BusShelter, GarbageBags, DeadTree, Bollard. Origen en el centro de la base (z = 0 es el suelo), frente hacia -Y.
Los nombres de material deciden el material horneado de texture_items.py (carpaint* = pintura, decal* = rotulos, lens* = lentes, glass, rubber, concrete,
bark, metal*; el resto, plastico).
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())

# ------------------------------------------------------------------ utilidades
def tube(m, pts, r, mat, segs=8, r1=None):
    """Tubo continuo por una poligonal; r1 = radio final (cono)."""
    pts = [Vector(p) for p in pts]
    rings = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        q = t.to_track_quat("Z", "Y").to_matrix()
        rr = r if r1 is None else r + (r1 - r) * i / max(1, len(pts) - 1)
        rings.append([p + q @ Vector((rr * math.cos(2 * math.pi * k / segs), rr * math.sin(2 * math.pi * k / segs), 0.0)) for k in range(segs)])
    m._loft_rings(rings, mat, True)


def cyl(m, c, r, h, mat, segs=14, r_top=None):
    """Cilindro/cono vertical de base en c (x, y, z)."""
    x, y, z = c
    rt = r if r_top is None else r_top
    m._loft_rings([[(x + r * math.cos(2 * math.pi * k / segs), y + r * math.sin(2 * math.pi * k / segs), z) for k in range(segs)],
                   [(x + rt * math.cos(2 * math.pi * k / segs), y + rt * math.sin(2 * math.pi * k / segs), z + h) for k in range(segs)]], mat, True)


def text_mesh(text, size):
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = 0.0
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    cu.space_character = 1.1
    obj = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(obj)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.curves.remove(cu)
    return me


def put_text(m, text, size, matrix, mat):
    """Texto plano (rotulo) llevado a 'matrix'; las normales se orientan segun la columna Z de la matriz."""
    me = text_mesh(text, size)
    bm = bmesh.new()
    bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts[:])
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    out = (matrix.to_3x3() @ Vector((0, 0, 1))).normalized()
    vmap = {v: m.bm.verts.new(v.co) for v in bm.verts}
    for f in bm.faces:
        try:
            nf = m.bm.faces.new([vmap[v] for v in f.verts])
        except ValueError:
            continue
        nf.material_index = m._idx(mat)
        nf.smooth = False
        m._deco.append((nf, out))
    bm.free()


def finish(m, name):
    """Como Mesher.finish, pero los rotulos planos conservan la orientacion hacia fuera que se les dio."""
    bm = m.bm
    deco = {f for f, _ in m._deco if f.is_valid}
    bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces if f not in deco])
    for f, out in m._deco:
        if f.is_valid and f.normal.dot(out) < 0:
            f.normal_flip()
    used = sorted({f.material_index for f in bm.faces})
    remap = {old: new for new, old in enumerate(used)}
    for f in bm.faces:
        f.material_index = remap[f.material_index]
    mesh = bpy.data.meshes.new(name + "Mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for old in used:
        mesh.materials.append(_mat(f"{m.prefix}_{m.names[old]}", m.palette[m.names[old]]))
    return obj


def new(prefix, pal):
    m = Mesher(prefix, pal)
    m._deco = []
    return m


BASE = {"metal": (0.55, 0.57, 0.6), "metal_dark": (0.08, 0.08, 0.09), "plastic": (0.04, 0.04, 0.045), "rubber": (0.02, 0.02, 0.022), "glass": (0.05, 0.07, 0.09),
        "concrete": (0.42, 0.41, 0.39), "carpaint_red": (0.55, 0.05, 0.04), "carpaint_green": (0.07, 0.25, 0.12), "carpaint_blue": (0.04, 0.10, 0.45),
        "carpaint_yellow": (0.8, 0.62, 0.05), "carpaint_white": (0.88, 0.88, 0.85), "carpaint_black": (0.02, 0.02, 0.025), "carpaint_orange": (0.9, 0.3, 0.04),
        "lens_red": (0.8, 0.03, 0.02), "lens_amber": (0.9, 0.45, 0.03), "lens_green": (0.05, 0.6, 0.2), "decal_w": (0.93, 0.93, 0.9), "decal_k": (0.02, 0.02, 0.025),
        "decal_red": (0.7, 0.04, 0.03), "bark": (0.1, 0.075, 0.055), "wood": (0.3, 0.2, 0.1), "bag": (0.015, 0.015, 0.018)}


# ------------------------------------------------------------------ modelos
def make_hydrant():
    m = new("hydrant", BASE)
    cyl(m, (0, 0, 0), 0.115, 0.05, "carpaint_red", 14)
    cyl(m, (0, 0, 0.05), 0.09, 0.45, "carpaint_red", 14, 0.085)
    cyl(m, (0, 0, 0.5), 0.1, 0.05, "carpaint_red", 14, 0.1)
    m.ellipsoid((0, 0, 0.58), (0.098, 0.098, 0.1), "carpaint_red", 14, 7)
    cyl(m, (0, 0, 0.67), 0.03, 0.04, "carpaint_red", 8)
    m.box((0, 0, 0.715), (0.075, 0.075, 0.03), "metal", bevel=0.006)                               # tuerca superior
    for sx in (-1, 1):
        tube(m, [(sx * 0.07, 0, 0.42), (sx * 0.17, 0, 0.42)], 0.045, "carpaint_red", 12)
        cyl_x = [(sx * 0.17, 0, 0.42), (sx * 0.2, 0, 0.42)]
        tube(m, cyl_x, 0.052, "carpaint_red", 12)
        m.box((sx * 0.215, 0, 0.42), (0.02, 0.07, 0.07), "metal", bevel=0.004)                       # tapon
    tube(m, [(0, -0.07, 0.38), (0, -0.16, 0.38)], 0.06, "carpaint_red", 14)
    m.box((0, -0.175, 0.38), (0.04, 0.02, 0.04), "metal", bevel=0.004)
    for z in (0.22, 0.3):
        m.box((0, 0, z), (0.19, 0.19, 0.012), "carpaint_red", bevel=0.003)                          # anillos
    return finish(m, "Hydrant")


def make_trash_can():
    m = new("trashcan", BASE)
    cyl(m, (0, 0, 0.03), 0.26, 0.66, "metal", 18, 0.3)
    cyl(m, (0, 0, 0.02), 0.255, 0.012, "metal_dark", 18)
    for z in (0.18, 0.4, 0.62):
        m._loft_rings([[(0.3 * 0.0 + (0.262 + 0.03 * (z - 0.03) / 0.66) * math.cos(2 * math.pi * k / 24), (0.262 + 0.03 * (z - 0.03) / 0.66) * math.sin(2 * math.pi * k / 24), z + dz) for k in range(24)] for dz in (-0.012, 0.012)], "metal", True)
    cyl(m, (0, 0, 0.69), 0.32, 0.03, "metal_dark", 18)                                                # aro del borde
    for k in range(6):                                                                                  # agujeros de la lata: no se modelan; unas asas
        pass
    for sx in (-1, 1):
        tube(m, [(sx * 0.3, 0, 0.55), (sx * 0.36, 0, 0.55), (sx * 0.36, 0, 0.44), (sx * 0.3, 0, 0.44)], 0.012, "metal_dark", 6)
    # basura que asoma
    for (x, y, z, r) in ((0.05, 0.04, 0.72, 0.1), (-0.1, -0.05, 0.7, 0.08)):
        m.ellipsoid((x, y, z), (r, r, r * 0.8), "bag", 8, 5)
    m.box((0.12, -0.04, 0.73), (0.12, 0.08, 0.05), "carpaint_white", rot=(0, 0, 25))
    return finish(m, "TrashCan")


def make_dumpster():
    m = new("dumpster", BASE)
    # cuerpo (trapecio ancho arriba)
    w0, w1, d, h = 0.9, 1.0, 1.0, 1.15
    m._loft_rings([[(-w0 * 0.5, -d * 0.5, 0.22), (w0 * 0.5, -d * 0.5, 0.22), (w0 * 0.5, d * 0.5, 0.22), (-w0 * 0.5, d * 0.5, 0.22)],
                   [(-w1 * 0.5 - 0.05, -d * 0.5 - 0.03, 0.22 + h), (w1 * 0.5 + 0.05, -d * 0.5 - 0.03, 0.22 + h), (w1 * 0.5 + 0.05, d * 0.5 + 0.03, 0.22 + h), (-w1 * 0.5 - 0.05, d * 0.5 + 0.03, 0.22 + h)]], "carpaint_green", False)
    # nervios laterales
    for sx in (-1, 1):
        for y in (-0.3, 0, 0.3):
            m.box((sx * 0.5, y, 0.8), (0.04, 0.05, 0.9), "carpaint_green", bevel=0.01)
    m.box((0, 0, 0.19), (1.0, 1.08, 0.06), "metal_dark", bevel=0.01)                                  # patines
    for sx in (-1, 1):                                                                                  # ruedas
        for y in (-0.38, 0.38):
            m.box((sx * 0.42, y, 0.1), (0.07, 0.2, 0.2), "rubber", bevel=0.03)
    # tapas: una cerrada, otra abierta hacia atras
    m.box((0, -0.26, 1.4), (1.12, 0.5, 0.045), "plastic", bevel=0.01, rot=(-4, 0, 0))
    m.box((0, 0.33, 1.7), (1.12, 0.5, 0.045), "plastic", bevel=0.01, rot=(70, 0, 0))
    m.box((0, 0.57, 1.55), (1.14, 0.06, 0.06), "metal_dark")                                           # bisagra
    # bolsas desbordadas
    for (x, y, z, r) in ((-0.2, 0.1, 1.38, 0.2), (0.15, 0.2, 1.36, 0.17), (0.0, -0.1, 1.33, 0.16)):
        m.ellipsoid((x, y, z), (r, r, r * 0.8), "bag", 10, 6)
    put_text(m, "REFUSE", 0.12, Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, 1, 0, 0))) if False else Matrix(((1, 0, 0, 0.0), (0, 0, 1, -0.55), (0, -1, 0, 0.95), (0, 0, 0, 1))), "decal_w")
    return finish(m, "Dumpster")


def make_newsbox():
    m = new("newsbox", BASE)
    m.box((0, 0, 0.45), (0.42, 0.4, 0.9), "carpaint_blue", bevel=0.02)
    m.box((0, 0.2, 0.9), (0.42, 0.04, 0.04), "metal_dark")
    m.box((0, -0.205, 0.62), (0.32, 0.015, 0.34), "glass", bevel=0.005)                                # ventanilla
    m.box((0, -0.215, 0.38), (0.34, 0.03, 0.09), "metal", bevel=0.008)                                 # ranura de monedas
    m.box((0, -0.205, 0.25), (0.2, 0.02, 0.08), "metal_dark", bevel=0.005)
    m.box((0, 0.0, 0.93), (0.44, 0.42, 0.04), "carpaint_blue", bevel=0.015)
    m.box((0, 0, 0.04), (0.5, 0.45, 0.08), "metal_dark", bevel=0.01)
    m.box((0, -0.19, 0.6), (0.26, 0.012, 0.26), "decal_w", bevel=0.003)                                # periodico visto a traves del cristal
    put_text(m, "NEWS", 0.07, Matrix(((1, 0, 0, 0.0), (0, 0, -1, -0.212), (0, 1, 0, 0.8), (0, 0, 0, 1))) if False else Matrix(((1, 0, 0, 0.0), (0, 0, 1, -0.213), (0, -1, 0, 0.8), (0, 0, 0, 1))), "decal_w")
    return finish(m, "NewsBox")


def make_mailbox():
    m = new("mailbox", BASE)
    m.box((0, 0, 0.58), (0.52, 0.5, 0.22), "carpaint_blue", bevel=0.03)
    m._loft_rings([[(-0.26 + 0.52 * k / 12, -0.25, 0.69 + 0.2 * math.sin(math.pi * k / 12) * 0) for k in range(13)][::1] and [(-0.26, -0.25, 0.69), (0.26, -0.25, 0.69), (0.26, 0.25, 0.69), (-0.26, 0.25, 0.69)]], "carpaint_blue", False) if False else None
    cyl(m, (0, 0, 0.0), 0.0, 0.0, "metal", 3) if False else None
    m.box((0, 0, 0.3), (0.2, 0.2, 0.62), "carpaint_blue", bevel=0.02)
    m.box((0, 0, 0.76), (0.55, 0.52, 0.2), "carpaint_blue", bevel=0.05)
    m.ellipsoid((0, 0, 0.84), (0.275, 0.26, 0.12), "carpaint_blue", 14, 6)
    m.box((0, -0.265, 0.74), (0.34, 0.012, 0.045), "metal_dark")                                       # ranura
    m.box((0, -0.27, 0.62), (0.28, 0.02, 0.1), "metal", bevel=0.004)                                    # placa de recogida
    m.box((0, 0, 0.02), (0.38, 0.38, 0.04), "metal_dark", bevel=0.01)
    return finish(m, "Mailbox")


def make_parking_meter():
    m = new("meter", BASE)
    tube(m, [(0, 0, 0), (0, 0, 1.15)], 0.03, "metal", 10)
    m.box((0, 0, 0.02), (0.14, 0.14, 0.04), "metal_dark", bevel=0.01)
    m.box((0, 0, 1.28), (0.22, 0.14, 0.34), "metal", bevel=0.03)
    m.ellipsoid((0, 0, 1.46), (0.115, 0.075, 0.07), "metal", 12, 5)
    m.box((0, -0.075, 1.36), (0.14, 0.012, 0.12), "glass", bevel=0.004)                                # esfera
    m.box((0, -0.078, 1.2), (0.05, 0.016, 0.07), "metal_dark")                                         # ranura
    m.box((0, -0.075, 1.27), (0.06, 0.012, 0.012), "lens_red")                                         # piloto
    return finish(m, "ParkingMeter")


def make_jersey():
    m = new("jersey", BASE)
    prof = [(-0.3, 0.0), (0.3, 0.0), (0.3, 0.1), (0.22, 0.36), (0.12, 0.8), (-0.12, 0.8), (-0.22, 0.36), (-0.3, 0.1)]
    L = 2.4
    rings = [[(x, y, z) for (x, z) in prof] for y in (-L / 2, L / 2)]
    m._loft_rings(rings, "concrete", False)
    for sy in (-1, 1):                                                                                   # armaduras que asoman
        for k in (-0.1, 0.1):
            tube(m, [(k, sy * (L / 2 - 0.02), 0.7), (k, sy * (L / 2 + 0.22), 0.7)], 0.01, "metal_dark", 6)
    m.box((0, 0, 0.55), (0.26, 0.5, 0.02), "decal_red", bevel=0.002) if False else None
    for y in (-0.7, 0.0, 0.7):                                                                           # bandas reflectantes desgastadas
        m.box((0, y, 0.735), (0.22, 0.2, 0.012), "decal_w")
    return finish(m, "JerseyBarrier")


def make_cone():
    m = new("cone", BASE)
    m.box((0, 0, 0.02), (0.38, 0.38, 0.04), "rubber", bevel=0.01)
    cyl(m, (0, 0, 0.04), 0.14, 0.62, "carpaint_orange", 16, 0.035)
    cyl(m, (0, 0, 0.3), 0.093, 0.1, "decal_w", 16, 0.08) if False else None
    m._loft_rings([[(r * math.cos(2 * math.pi * k / 16), r * math.sin(2 * math.pi * k / 16), z) for k in range(16)] for (r, z) in ((0.083, 0.34), (0.0745, 0.42))], "decal_w", True)
    m._loft_rings([[(r * math.cos(2 * math.pi * k / 16), r * math.sin(2 * math.pi * k / 16), z) for k in range(16)] for (r, z) in ((0.108, 0.24), (0.1, 0.29))], "decal_w", True)
    cyl(m, (0, 0, 0.66), 0.036, 0.02, "carpaint_orange", 10)
    return finish(m, "TrafficCone")


def make_barricade():
    m = new("barricade", BASE)
    for sx in (-1, 1):
        tube(m, [(sx * 0.9, -0.3, 0.0), (sx * 0.9, -0.06, 1.0)], 0.03, "metal", 8)
        tube(m, [(sx * 0.9, 0.3, 0.0), (sx * 0.9, 0.06, 1.0)], 0.03, "metal", 8)
        m.box((sx * 0.9, 0.0, 0.5), (0.04, 0.5, 0.05), "metal_dark", bevel=0.005) if False else None
    for z in (0.45, 0.78):                                                                               # tablones a rayas
        n = 10
        for i in range(n):
            x0 = -1.0 + 2.0 * i / n
            m.box((x0 + 1.0 / n, 0.0, z), (2.0 / n, 0.04, 0.2), "carpaint_white" if i % 2 == 0 else "carpaint_orange", bevel=0.004)
    m.box((0, 0.0, 0.62), (1.9, 0.03, 0.03), "metal_dark")
    return finish(m, "Barricade")


def make_bollard():
    m = new("bollard", BASE)
    cyl(m, (0, 0, 0), 0.1, 0.9, "concrete", 12)
    m.ellipsoid((0, 0, 0.9), (0.1, 0.1, 0.07), "concrete", 12, 5)
    m._loft_rings([[(r * math.cos(2 * math.pi * k / 12), r * math.sin(2 * math.pi * k / 12), z) for k in range(12)] for (r, z) in ((0.102, 0.55), (0.102, 0.65))], "decal_w", True)
    cyl(m, (0, 0, 0), 0.14, 0.04, "concrete", 12)
    return finish(m, "Bollard")


def make_garbage():
    """Montón de bolsas de basura: negras, verdes y grises, aplastadas e irregulares, con el nudo del cuello torcido y una caja de carton debajo."""
    m = new("garbage", dict(BASE, bag_green=(0.04, 0.07, 0.04), bag_gray=(0.12, 0.12, 0.13), bag_blue=(0.05, 0.08, 0.2)))
    rng = random.Random(3)
    mats = ("bag", "bag_green", "bag_gray", "bag", "bag_blue")
    for i, (x, y, r) in enumerate(((0.0, 0.0, 0.3), (0.4, 0.1, 0.25), (-0.34, 0.14, 0.27), (0.1, -0.37, 0.23), (-0.1, 0.4, 0.26))):
        mat = mats[i % len(mats)]
        sq = rng.uniform(0.55, 0.8)                                                                  # aplastadas: la bolsa se asienta
        m.ellipsoid((x, y, r * sq), (r * rng.uniform(0.95, 1.2), r * rng.uniform(0.85, 1.1), r * sq), mat, 12, 7)
        m.ellipsoid((x + rng.uniform(-0.08, 0.08), y + rng.uniform(-0.08, 0.08), r * sq * 1.7), (r * 0.12, r * 0.12, r * 0.28), mat, 8, 5)       # nudo del cuello, ladeado
        m.ellipsoid((x + rng.uniform(-0.1, 0.1), y + rng.uniform(-0.1, 0.1), r * sq * 1.15), (r * 0.5, r * 0.4, r * 0.4), mat, 8, 5)             # bulto para romper la forma redonda
    m.box((0.2, -0.1, 0.05), (0.34, 0.24, 0.1), "carpaint_white", rot=(0, 0, 20), bevel=0.01)          # caja de carton aplastada
    m.box((-0.5, -0.25, 0.015), (0.3, 0.22, 0.03), "carpaint_white", rot=(0, 0, -35))                 # papel y carton sueltos
    return finish(m, "GarbageBags")


def make_sign(kind):
    m = new("sign" + kind, BASE)
    tube(m, [(0, 0, 0), (0, 0, 2.5)], 0.025, "metal", 8)
    m.box((0, 0, 0.01), (0.2, 0.2, 0.02), "metal_dark")
    zc = 2.2
    if kind == "Stop":
        r = 0.3
        pts = [(r * math.cos(math.radians(22.5 + 45 * k)), r * math.sin(math.radians(22.5 + 45 * k))) for k in range(8)]
        ring = lambda s, y: [(x * s, y, zc + z * s) for (x, z) in pts]
        m._loft_rings([ring(1.0, -0.035), ring(1.0, -0.02)], "metal", False)
        m._loft_rings([ring(1.0, -0.036), ring(0.93, -0.036)], "decal_w", False) if False else None
        m._loft_rings([ring(0.95, -0.038), ring(0.95, -0.036)], "decal_w", False)
        m._loft_rings([ring(0.88, -0.040), ring(0.88, -0.038)], "carpaint_red", False)
        put_text(m, "STOP", 0.17, Matrix(((1, 0, 0, 0), (0, 0, 1, -0.0415), (0, -1, 0, zc), (0, 0, 0, 1))), "decal_w")
    elif kind == "OneWay":
        m.box((0, -0.03, zc), (0.9, 0.025, 0.28), "decal_k", bevel=0.008)
        m.box((0, -0.045, zc), (0.8, 0.012, 0.19), "decal_w", bevel=0.004) if False else None
        put_text(m, "ONE WAY", 0.1, Matrix(((1, 0, 0, 0), (0, 0, 1, -0.0425), (0, -1, 0, zc + 0.045), (0, 0, 0, 1))), "decal_w")
        m.box((-0.2, -0.0435, zc - 0.07), (0.34, 0.004, 0.035), "decal_w")
        m._loft_rings([[(0.17, -0.0435, zc - 0.07 + 0.0), (0.17, -0.0435, zc - 0.07)]], "decal_w", False) if False else None
        tri = [(0.16, zc - 0.07 + 0.06), (0.3, zc - 0.07), (0.16, zc - 0.07 - 0.06)]
        vs = [m.bm.verts.new((x, -0.0435, z)) for (x, z) in tri]
        f = m.bm.faces.new(vs)
        f.material_index = m._idx("decal_w")
        f.smooth = False
        m._deco.append((f, Vector((0, -1, 0))))
    else:
        m.box((0, -0.03, zc), (0.55, 0.025, 0.7), "decal_w", bevel=0.01)
        m._loft_rings([[(0.21 * math.cos(2 * math.pi * k / 24), -0.0435, zc + 0.12 + 0.21 * math.sin(2 * math.pi * k / 24)) for k in range(24)], [(0.19 * math.cos(2 * math.pi * k / 24), -0.0435, zc + 0.12 + 0.19 * math.sin(2 * math.pi * k / 24)) for k in range(24)]], "decal_red", True) if False else None
        tube(m, [(0.2 * math.cos(math.radians(a)), -0.0445, zc + 0.12 + 0.2 * math.sin(math.radians(a))) for a in range(0, 361, 20)], 0.012, "decal_red", 6)
        tube(m, [(-0.14, -0.0445, zc + 0.26), (0.14, -0.0445, zc - 0.02)], 0.012, "decal_red", 6)
        put_text(m, "NO", 0.11, Matrix(((1, 0, 0, 0), (0, 0, 1, -0.0425), (0, -1, 0, zc - 0.15), (0, 0, 0, 1))), "decal_k")
        put_text(m, "PARKING", 0.075, Matrix(((1, 0, 0, 0), (0, 0, 1, -0.0425), (0, -1, 0, zc - 0.27), (0, 0, 0, 1))), "decal_k")
    return finish(m, "Sign" + kind)


def make_traffic_light():
    m = new("trafficlight", BASE)
    tube(m, [(0, 0, 0), (0, 0, 5.0)], 0.07, "metal", 10, 0.06)
    cyl(m, (0, 0, 0), 0.16, 0.12, "metal_dark", 10, 0.1)
    # brazo hacia +X (sobre la calzada) con curva suave
    pts = [(0, 0, 5.0), (0.05, 0, 5.4), (0.4, 0, 5.62), (1.2, 0, 5.62), (2.4, 0, 5.62), (3.2, 0, 5.62)]
    tube(m, pts, 0.045, "metal", 8, 0.03)
    def head(x, z, hang=0.0):
        m.box((x, 0, z), (0.34, 0.32, 1.0), "plastic", bevel=0.02)
        for k, mat in enumerate(("lens_red", "lens_amber", "lens_green")):
            zz = z + 0.33 - 0.33 * k
            m.ellipsoid((x, -0.16, zz), (0.1, 0.03, 0.1), mat, 12, 5)
            m.box((x, -0.2, zz + 0.04), (0.2, 0.12, 0.012), "plastic", bevel=0.004, rot=(-18, 0, 0))     # visera
            m.box((x, -0.2, zz + 0.1), (0.2, 0.02, 0.07), "plastic") if False else None
        m.box((x, 0.0, z + 0.54), (0.1, 0.1, 0.08), "metal_dark")
        m.box((x, 0.18, z), (0.28, 0.02, 0.92), "metal_dark") if False else None
    head(1.6, 4.62)
    head(2.9, 4.62)
    m.box((0.3, 0, 5.62 - 0.0), (0.12, 0.18, 0.1), "metal_dark")
    # semaforo de peatones en el poste
    m.box((0.0, -0.1, 2.5), (0.28, 0.2, 0.4), "plastic", bevel=0.02)
    m.box((0.0, -0.205, 2.58), (0.2, 0.012, 0.12), "lens_red", bevel=0.004)
    m.box((0.0, -0.205, 2.42), (0.2, 0.012, 0.12), "lens_green", bevel=0.004)
    m.box((0.1, 0.0, 1.5), (0.22, 0.14, 0.34), "metal", bevel=0.012)                                   # caja de control
    m.box((0.0, -0.075, 1.5), (0.16, 0.02, 0.28), "metal_dark")
    return finish(m, "TrafficLight")


def make_bus_shelter():
    m = new("busshelter", BASE)
    W, D, H = 3.0, 1.3, 2.5
    for sx in (-1, 1):
        for sy in (-1, 1):
            tube(m, [(sx * (W / 2 - 0.05), sy * (D / 2 - 0.05), 0), (sx * (W / 2 - 0.05), sy * (D / 2 - 0.05), H)], 0.04, "metal", 8)
    m.box((0, 0, H + 0.04), (W + 0.2, D + 0.2, 0.08), "metal", bevel=0.01)
    m.box((0, 0, H + 0.1), (W + 0.3, D + 0.3, 0.03), "metal_dark", bevel=0.005)
    # paneles de cristal: fondo y laterales; uno roto
    m.box((0, D / 2 - 0.04, 1.25), (W - 0.12, 0.012, 2.2), "glass", bevel=0.003)
    m.box((W / 2 - 0.045, 0, 1.25), (0.012, D - 0.12, 2.2), "glass", bevel=0.003)
    m.box((-W / 2 + 0.045, 0, 1.55), (0.012, D - 0.12, 1.6), "glass", bevel=0.003)                    # el lateral oeste, roto por abajo
    for sx in (-1, 1):
        m.box((sx * (W / 2 - 0.045), D / 2 - 0.04, 1.25), (0.05, 0.05, 2.2), "metal", bevel=0.008) if False else None
    m.box((0, D / 2 - 0.04, 0.14), (W - 0.1, 0.05, 0.28), "metal_dark", bevel=0.005)
    # banco con respaldo
    m.box((0, D / 2 - 0.3, 0.5), (1.9, 0.38, 0.05), "wood", bevel=0.008)
    for sx in (-0.85, 0.85):
        m.box((sx, D / 2 - 0.3, 0.25), (0.05, 0.36, 0.5), "metal_dark", bevel=0.005)
    m.box((0, D / 2 - 0.12, 0.8), (1.9, 0.03, 0.22), "wood", bevel=0.006)
    # panel publicitario lateral con cartel desgarrado
    m.box((W / 2 - 0.2, -D / 2 + 0.06, 1.3), (1.05, 0.08, 1.7), "metal_dark", bevel=0.01)
    m.box((W / 2 - 0.2, -D / 2 + 0.015, 1.3), (0.95, 0.012, 1.55), "carpaint_blue", bevel=0.004)
    put_text(m, "CITY BUS", 0.16, Matrix(((1, 0, 0, W / 2 - 0.2), (0, 0, 1, -D / 2 + 0.0), (0, 1, 0, 1.55), (0, 0, 0, 1))) if False else Matrix(((1, 0, 0, W / 2 - 0.2), (0, 0, 1, -D / 2 + 0.0085), (0, -1, 0, 1.55), (0, 0, 0, 1))), "decal_w")
    m.box((W / 2 - 0.2, -D / 2 + 0.0, 0.98), (0.8, 0.012, 0.3), "decal_w", bevel=0.003)
    # letrero de parada en un poste lateral
    tube(m, [(-W / 2 - 0.2, 0, 0), (-W / 2 - 0.2, 0, 2.9)], 0.025, "metal", 8)
    m.box((-W / 2 - 0.2, -0.02, 2.75), (0.4, 0.02, 0.4), "carpaint_blue", bevel=0.008)
    put_text(m, "BUS", 0.12, Matrix(((1, 0, 0, -W / 2 - 0.2), (0, 0, 1, -0.0305), (0, -1, 0, 2.75), (0, 0, 0, 1))), "decal_w")
    return finish(m, "BusShelter")


def make_dead_tree():
    m = new("deadtree", BASE)
    rng = random.Random(11)

    def branch(p, d, r, length, depth):
        steps = 4
        pts = [Vector(p)]
        cur = Vector(p)
        dd = d.copy()
        for i in range(steps):
            dd = (dd + Vector((rng.uniform(-0.18, 0.18), rng.uniform(-0.18, 0.18), rng.uniform(-0.05, 0.1)))).normalized()
            cur = cur + dd * (length / steps)
            pts.append(cur.copy())
        tube(m, [tuple(q) for q in pts], r, "bark", 6, r * 0.55)
        if depth > 0:
            n = 2 if depth > 1 else 3
            for _ in range(n):
                idx = rng.randint(2, steps)
                base = pts[idx]
                side = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(0.2, 0.9))).normalized()
                branch(tuple(base), (dd * 0.5 + side).normalized(), r * 0.55, length * 0.62, depth - 1)
    # tronco con raices
    tube(m, [(0, 0, 0), (0.02, 0.02, 1.0), (-0.03, 0.0, 2.0), (0.0, -0.03, 2.8)], 0.22, "bark", 10, 0.1)
    for k in range(5):
        a = 2 * math.pi * k / 5
        tube(m, [(0.25 * math.cos(a), 0.25 * math.sin(a), 0.0), (0.12 * math.cos(a), 0.12 * math.sin(a), 0.3)], 0.07, "bark", 6, 0.1)
    for (ang, tilt, ln) in ((10, 0.9, 2.2), (130, 1.0, 2.0), (250, 0.8, 2.4), (60, 0.6, 1.6), (200, 0.55, 1.7)):
        z0 = 2.2 + rng.uniform(-0.3, 0.5)
        a = math.radians(ang)
        branch((0.0, 0.0, z0), Vector((math.cos(a) * tilt, math.sin(a) * tilt, 0.7)).normalized(), 0.09, ln, 2)
    branch((0.0, -0.03, 2.8), Vector((0.1, 0.0, 1.0)).normalized(), 0.1, 1.8, 2)
    return finish(m, "DeadTree")


BUILDERS = {
    "Hydrant": make_hydrant, "TrashCan": make_trash_can, "Dumpster": make_dumpster, "NewsBox": make_newsbox, "Mailbox": make_mailbox, "ParkingMeter": make_parking_meter,
    "JerseyBarrier": make_jersey, "TrafficCone": make_cone, "Barricade": make_barricade, "Bollard": make_bollard, "GarbageBags": make_garbage,
    "SignStop": lambda: make_sign("Stop"), "SignOneWay": lambda: make_sign("OneWay"), "SignNoParking": lambda: make_sign("NoParking"),
    "TrafficLight": make_traffic_light, "BusShelter": make_bus_shelter, "DeadTree": make_dead_tree,
}


# ------------------------------------------------------------------ vista previa y exportacion
def view(rx, rz, dist, loc):
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            r3d = area.spaces[0].region_3d
            r3d.view_rotation = Euler((math.radians(rx), 0, math.radians(rz))).to_quaternion()
            r3d.view_distance = dist
            r3d.view_location = loc


def preview(name, rz=35):
    _clear()
    obj = BUILDERS[name]()
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    d = obj.dimensions
    view(75, rz, max(d.x, d.y, d.z) * 2.2 + 1.0, (0, 0, d.z * 0.5))
    print(name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    return obj


def preview_all():
    """Todos los modelos en fila para verlos de un vistazo."""
    _clear()
    x = 0.0
    for name, fn in BUILDERS.items():
        obj = fn()
        d = obj.dimensions
        obj.location = (x + d.x / 2 + 0.2, 0, 0)
        x += d.x + 1.2
        print(name, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    view(78, 20, x * 0.9, (x / 2, 0, 1.2))


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
