"""Renderiza vistas frontal, lateral y trasera de un FBX (Workbench) y escribe PNG. Uso:
blender -b --python preview_model.py -- <fbx> <textura_color> <carpeta_salida> <prefijo>
"""
import math
import sys

import bpy
from mathutils import Vector

fbx, tex, outdir, prefix = sys.argv[sys.argv.index("--") + 1:][:4]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
img = bpy.data.images.load(tex)
for o in objs:
    m = bpy.data.materials.new("p")
    m.use_nodes = True
    t = m.node_tree.nodes.new("ShaderNodeTexImage")
    t.image = img
    m.node_tree.links.new(t.outputs["Color"], m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    o.data.materials.clear()
    o.data.materials.append(m)

bb = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
mn = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
mx = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
print("BBOX", tuple(round(c, 3) for c in mn), tuple(round(c, 3) for c in mx), flush=True)
mid = (mn + mx) / 2

sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.display.shading.light = "STUDIO"
sc.display.shading.color_type = "TEXTURE"
sc.render.resolution_x, sc.render.resolution_y = 600, 900
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
cam.data.type = "ORTHO"
cam.data.ortho_scale = 2.1
sc.collection.objects.link(cam)
sc.camera = cam
for name, direction in (("front", Vector((0, -1, 0))), ("side", Vector((1, 0, 0))), ("back", Vector((0, 1, 0)))):
    cam.location = mid + direction * 5
    cam.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = f"{outdir}\\{prefix}_{name}.png"
    bpy.ops.render.render(write_still=True)
print("PROCESO_TERMINADO", flush=True)
