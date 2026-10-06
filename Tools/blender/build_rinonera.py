"""Rinonera (fanny pack) pequena de nailon negro, tumbada sobre su espalda con las correas extendidas. Objeto I_Bag.

    blender -b --python build_rinonera.py -- <salida.fbx> [<carpeta_vistas_previas>]

Convenciones del proyecto: metros, origen en el centro de la base (z = 0 es el suelo), frente hacia -Y, exportado
con Forward -Z / Up Y. El bolsillo frontal mira hacia arriba (+Z). Se reutiliza el Mesher de build_items.py.
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector, noise

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())   # Mesher, _clear, _export

args = sys.argv[sys.argv.index("--") + 1:]
OUT = args[0]
PREVIEW = args[1] if len(args) > 1 else None


def make_rinonera():
    m = Mesher("rinonera", {
        "nylon": (0.085, 0.087, 0.092),     # nailon negro desteñido (carbon: el negro puro se pierde en la penumbra)
        "seam": (0.140, 0.140, 0.145),      # ribete / costuras
        "webbing": (0.115, 0.115, 0.108),   # cinta de la correa
        "metal": (0.50, 0.50, 0.53),        # cremalleras
        "plastic": (0.020, 0.020, 0.022),   # hebilla
        "dark": (0.006, 0.006, 0.007),      # huecos
    })
    W, D, T = 0.160, 0.085, 0.058           # ancho (X), alto del bolso (Y, tumbado) y grosor (Z)
    zc = T / 2

    # cuerpo + ribete perimetral
    m.box((0, 0, zc), (W, D, T), "nylon", bevel=0.012)
    m.box((0, 0, zc), (W + 0.004, D + 0.004, 0.004), "seam", bevel=0.0015)

    # cremallera principal: dientes en el borde +Y, cursor y tirador sobre la cara superior
    m.box((0, D / 2 + 0.0005, zc), (W - 0.030, 0.003, 0.022), "metal")
    m.box((-0.040, D / 2 + 0.002, zc), (0.016, 0.010, 0.020), "metal", bevel=0.001)
    m.box((-0.040, D / 2 - 0.009, T + 0.0005), (0.006, 0.016, 0.0016), "metal")

    # bolsillo frontal (cara superior) con su cremallera
    m.box((0, -0.006, T + 0.0025), (0.120, 0.048, 0.006), "nylon", bevel=0.002)
    m.box((0, 0.015, T + 0.0058), (0.105, 0.004, 0.0016), "metal")
    m.box((0.030, 0.015, T + 0.0068), (0.010, 0.008, 0.004), "metal", bevel=0.0008)
    m.box((0.030, 0.0085, T + 0.0068), (0.004, 0.012, 0.0014), "metal")

    # trabillas laterales y correas (tramo elevado + tramo apoyado en el suelo)
    sw, st = 0.036, 0.004
    for sx in (-1, 1):
        m.box((sx * (W / 2 + 0.002), 0, zc), (0.008, 0.040, 0.012), "webbing", bevel=0.0015)
        m.box((sx * 0.094, 0, 0.016), (0.033, sw, st), "webbing", rot=(0, sx * 42, 0))
        m.box((sx * 0.128, 0, 0.0022), (0.060, sw, st), "webbing")
        for k in range(4):   # cosido transversal
            m.box((sx * (0.108 + 0.011 * k), 0, 0.0046), (0.0016, sw - 0.004, 0.0008), "seam")

    # hebilla en dos piezas: macho (izquierda) y hembra (derecha)
    m.box((-0.171, 0, 0.0055), (0.026, 0.040, 0.010), "plastic", bevel=0.0018)
    for sy in (-1, 1):
        m.box((-0.191, sy * 0.012, 0.0045), (0.016, 0.007, 0.008), "plastic", bevel=0.001)
    m.box((0.172, 0, 0.0055), (0.030, 0.042, 0.010), "plastic", bevel=0.0018)
    m.box((0.176, 0, 0.0108), (0.016, 0.026, 0.0016), "dark")

    # suavizado del nailon, desgaste (ruido pequeno en los vertices) y origen en el centro de la base
    bm = m.bm
    nyl = m.names.index("nylon")
    for f in bm.faces:
        if f.material_index == nyl:
            f.smooth = True
    for v in bm.verts:
        v.co += Vector(noise.noise_vector(v.co * 45.0)) * 0.0011
    xs = [v.co.x for v in bm.verts]; ys = [v.co.y for v in bm.verts]; zs = [v.co.z for v in bm.verts]
    off = Vector(((max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2, min(zs)))
    for v in bm.verts:
        v.co -= off
    return m.finish("Rinonera")


def render_previews(obj, folder):
    os.makedirs(folder, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "MATERIAL"
    sc.render.resolution_x, sc.render.resolution_y = 900, 600
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.type = "PERSP"
    cam.data.lens = 70
    target = Vector((0, 0, 0.02))
    views = {
        "3cuartos": Vector((0.30, -0.42, 0.34)),
        "arriba": Vector((0.0, -0.02, 0.62)),
        "lado": Vector((0.0, -0.62, 0.10)),
    }
    for name, pos in views.items():
        cam.location = pos
        cam.rotation_euler = (target - pos).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(folder, f"rinonera_{name}.png")
        bpy.ops.render.render(write_still=True)


_clear()
bpy.ops.wm.read_factory_settings(use_empty=True)
obj = make_rinonera()
bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
size = Vector((max(v.x for v in bb) - min(v.x for v in bb), max(v.y for v in bb) - min(v.y for v in bb), max(v.z for v in bb) - min(v.z for v in bb)))
tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
print("DIMENSIONES", tuple(round(c, 3) for c in size), "triangulos", tris, flush=True)
if PREVIEW:
    render_previews(obj, PREVIEW)
os.makedirs(os.path.dirname(os.path.abspath(OUT)), exist_ok=True)
_export(obj, OUT)
print("PROCESO_TERMINADO", flush=True)
