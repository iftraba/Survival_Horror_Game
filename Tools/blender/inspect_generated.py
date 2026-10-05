"""Inspecciona un FBX de Meshy: caras, medidas, texturas incrustadas y vistas previas (frente, lado, detalle de la cabeza).
blender -b --python inspect_generated.py -- <fbx> <carpeta_salida> <prefijo>
"""
import sys

import bpy
from mathutils import Vector

fbx, outdir, prefix = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
print("MALLAS", len(meshes), "CARAS", sum(len(o.data.polygons) for o in meshes), flush=True)
for img in bpy.data.images:
    print("IMAGEN", img.name, tuple(img.size), "empaquetada" if img.packed_file else img.filepath, flush=True)
for m in bpy.data.materials:
    if m.use_nodes:
        print("MATERIAL", m.name, [n.image.name for n in m.node_tree.nodes if n.type == "TEX_IMAGE" and n.image], flush=True)
bb = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
size = mx - mn
print("MEDIDAS", tuple(round(c, 3) for c in size), "min", tuple(round(c, 3) for c in mn), flush=True)
up = max(range(3), key=lambda i: size[i] if i != 0 else 0)   # eje vertical = el mayor entre Y y Z
mid = (mn + mx) / 2

sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.display.shading.light = "STUDIO"
sc.display.shading.color_type = "TEXTURE"
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
cam.data.type = "ORTHO"
sc.collection.objects.link(cam)
sc.camera = cam
height = size[up]
# Direcciones segun el eje vertical: Z-up (Blender) o Y-up (sin convertir)
front, side = (Vector((0, -1, 0)), Vector((1, 0, 0))) if up == 2 else (Vector((0, 0, 1)), Vector((1, 0, 0)))
upvec = Vector((0, 0, 1)) if up == 2 else Vector((0, 1, 0))


def shot(name, direction, center, scale, w, h):
    sc.render.resolution_x, sc.render.resolution_y = w, h
    cam.data.ortho_scale = scale
    cam.location = center + direction * 6
    cam.rotation_euler = (-direction).to_track_quat("-Z", "Y" if up == 2 else "Y").to_euler()
    sc.render.filepath = f"{outdir}\\{prefix}_{name}.png"
    bpy.ops.render.render(write_still=True)


shot("front", front, mid, height * 1.15, 600, 900)
shot("side", side, mid, height * 1.15, 600, 900)
head = mid + upvec * (height * 0.40)
shot("head", front, head, height * 0.30, 700, 700)
print("PROCESO_TERMINADO", flush=True)
