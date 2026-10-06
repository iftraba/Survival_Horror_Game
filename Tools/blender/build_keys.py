"""Objetos clave de la segunda mitad del juego: tarjeta de acceso, llave del garaje y llave maestra.

    blender -b --python build_keys.py -- <carpeta_salida_fbx> [<carpeta_vistas_previas>]

Mismas convenciones que la llave existente (build_items.make_key): metros, el objeto tumbado en el plano XY (grosor
en Z, se exporta con Forward -Z / Up Y), origen en el centro, y se reutiliza el Mesher de build_items.py.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())   # Mesher, _clear, _export

args = sys.argv[sys.argv.index("--") + 1:]
OUT = args[0]
PREVIEW = args[1] if len(args) > 1 else None


def make_keycard():
    """Tarjeta magnetica de acceso: 85.6 x 54 mm. Banda cian, banda magnetica, chip, foto y lineas de texto."""
    m = Mesher("keycard", {"plastic": (0.80, 0.84, 0.88), "band": (0.08, 0.60, 0.85), "mag": (0.03, 0.03, 0.035),
                           "chip": (0.85, 0.68, 0.20), "photo": (0.42, 0.47, 0.53), "ink": (0.13, 0.15, 0.18), "hole": (0.01, 0.01, 0.01)})
    t = 0.0022
    z = t / 2 + 0.0002
    m.box((0, 0, 0), (0.0856, 0.054, t), "plastic", bevel=0.0007)
    m.box((0, 0.0215, z), (0.0856, 0.011, 0.0005), "band")                       # banda de color superior
    m.box((0, -0.0185, z), (0.0856, 0.0095, 0.0005), "mag")                      # banda magnetica
    m.box((-0.027, 0.0015, z), (0.0125, 0.0095, 0.0006), "chip", bevel=0.0003)   # chip dorado
    for dx in (-0.0033, 0.0033):                                                 # surcos del chip
        m.box((-0.027 + dx, 0.0015, z + 0.0004), (0.0004, 0.0090, 0.0003), "ink")
    m.box((0.025, 0.001, z), (0.022, 0.026, 0.0005), "photo")                    # foto
    m.box((0.025, 0.007, z + 0.0004), (0.010, 0.009, 0.0003), "plastic")         # cabeza de la foto
    for i, w in enumerate((0.034, 0.026, 0.030)):                                # lineas de texto
        m.box((-0.012 - (0.034 - w) / 2, -0.0075 - i * 0.0036 + 0.0022, z), (w, 0.0017, 0.0004), "ink")
    m.box((0, 0.0238, z + 0.0003), (0.012, 0.0022, 0.0006), "hole")              # ranura del cordon
    return m.finish("KeyCard")


def make_garage_key():
    """Llave de acero (cerradura de portón) con un llavero de plastico amarillo."""
    m = Mesher("garagekey", {"steel": (0.62, 0.64, 0.68), "fob": (0.95, 0.78, 0.08), "ink": (0.10, 0.10, 0.11)})
    m.box((0, -0.018, 0), (0.0075, 0.084, 0.0042), "steel", bevel=0.0008)           # vastago
    for y, w in ((-0.050, 0.016), (-0.040, 0.011), (-0.030, 0.015), (-0.020, 0.009)):   # dientes
        m.box((0.0105, y, 0), (w, 0.0065, 0.0042), "steel")
    m.box((0, 0.030, 0), (0.021, 0.021, 0.0050), "steel", bevel=0.0012)             # cabeza
    m.box((0, 0.030, 0), (0.0095, 0.0095, 0.0060), "ink")                           # agujero
    m.box((0, 0.062, 0.0005), (0.030, 0.047, 0.0070), "fob", bevel=0.0025)          # llavero
    m.box((0, 0.046, 0.0005), (0.008, 0.010, 0.0074), "steel")                      # argolla que lo une
    m.box((0, 0.064, 0.0040), (0.019, 0.011, 0.0008), "ink")                        # etiqueta
    m.box((0, 0.064, 0.0045), (0.003, 0.007, 0.0004), "fob")
    m.box((0, 0.076, 0.0040), (0.019, 0.0025, 0.0008), "ink")
    return m.finish("KeyGarage")


def make_master_key():
    """Llave maestra: arandela dorada con una gema roja, vastago con collares y paletón de dientes."""
    m = Mesher("masterkey", {"gold": (0.86, 0.68, 0.22), "dark": (0.14, 0.13, 0.15), "red": (0.74, 0.07, 0.05)})
    cy = 0.080
    for k in range(18):                                                              # aro de la empuñadura
        a = 2 * math.pi * k / 18
        m.box((0.021 * math.cos(a), cy + 0.021 * math.sin(a), 0), (0.0100, 0.0120, 0.0070), "gold", rot=(0, 0, math.degrees(a) + 90), bevel=0.0006)
    m.box((0, cy, 0.0004), (0.0200, 0.0200, 0.0045), "red", rot=(0, 0, 45), bevel=0.0010)    # gema
    m.box((0, cy, 0.0030), (0.0070, 0.0070, 0.0010), "gold", rot=(0, 0, 45))                  # destello
    m.box((0, cy - 0.027, 0), (0.0160, 0.0100, 0.0080), "dark", bevel=0.0010)       # cuello
    m.box((0, 0.0115, 0), (0.0085, 0.1230, 0.0060), "gold", bevel=0.0009)           # vastago (de la punta al cuello)
    for dx, dy in ((0.0145, 0), (-0.0145, 0), (0, 0.0145), (0, -0.0145)):           # radios: la gema queda unida al aro
        m.box((dx, cy + dy, 0), (0.0050, 0.0050, 0.0050), "gold")
    m.box((0, 0.034, 0), (0.0150, 0.0050, 0.0078), "gold", bevel=0.0008)            # collares
    m.box((0, 0.012, 0), (0.0135, 0.0045, 0.0076), "gold", bevel=0.0008)
    m.box((0, -0.002, 0), (0.0110, 0.0035, 0.0072), "dark")
    teeth = ((-0.0135, 0.0170, 0.0075), (-0.0235, 0.0120, 0.0060), (-0.0330, 0.0180, 0.0070), (-0.0415, 0.0100, 0.0060))
    for sx, (y, w, h) in zip((1, -1, 1, -1), teeth):                                 # paleton: dientes a los dos lados, pegados al vastago
        m.box((sx * (0.0030 + w / 2), y, 0), (w, h, 0.0058), "gold")
    m.box((0, -0.0525, 0), (0.0065, 0.0050, 0.0058), "dark")                        # punta
    return m.finish("KeyMaster")


def render_previews(obj, folder, prefix):
    os.makedirs(folder, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "MATERIAL"
    sc.render.resolution_x, sc.render.resolution_y = 800, 560
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(cam)
    sc.camera = cam
    cam.data.type = "PERSP"
    cam.data.lens = 60
    bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
    mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
    mid = (mn + mx) / 2
    size = max(mx.x - mn.x, mx.y - mn.y)
    views = {"arriba": mid + Vector((0, -0.001, size * 2.6)), "3cuartos": mid + Vector((size * 0.9, -size * 1.4, size * 1.5))}
    for name, pos in views.items():
        cam.location = pos
        cam.rotation_euler = (mid - pos).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(folder, f"{prefix}_{name}.png")
        bpy.ops.render.render(write_still=True)


def dims(obj):
    bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    return tuple(round(max(getattr(v, a) for v in bb) - min(getattr(v, a) for v in bb), 4) for a in "xyz")


os.makedirs(OUT, exist_ok=True)
for name, fn in (("KeyCard", make_keycard), ("KeyGarage", make_garage_key), ("KeyMaster", make_master_key)):
    _clear()
    obj = fn()
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print("DIMENSIONES", name, dims(obj), "triangulos", tris, flush=True)
    if PREVIEW:
        render_previews(obj, PREVIEW, name)
    _export(obj, os.path.join(OUT, name + ".fbx"))
print("PROCESO_TERMINADO", flush=True)
