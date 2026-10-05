"""Vuelca informacion de un FBX: mallas (caras, medidas, textura), esqueleto (huesos), animacion (fotogramas) y
opcionalmente una vista previa.   blender -b --python dump_fbx_info.py -- <fbx> [carpeta_png prefijo]
"""
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=args[0])
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
print("INFO mallas=%d (%s) caras=%d armaduras=%d" % (len(meshes), ",".join(o.name for o in meshes), sum(len(o.data.polygons) for o in meshes), len(arms)), flush=True)
if meshes:
    bb = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    size = [max(v[i] for v in bb) - min(v[i] for v in bb) for i in range(3)]
    print("INFO medidas", tuple(round(c, 2) for c in size), flush=True)
for a in arms:
    names = [b.name for b in a.data.bones]
    print("INFO huesos=%d primeros=%s" % (len(names), names[:6]), flush=True)
    if a.animation_data and a.animation_data.action:
        act = a.animation_data.action
        print("INFO accion=%s fotogramas=%s" % (act.name, tuple(round(x) for x in act.frame_range)), flush=True)
        # desplazamiento de la cadera en el clip (raiz)
        hips = next((b for b in names if b.endswith("Hips")), None)
        if hips:
            sc = bpy.context.scene
            pb = a.pose.bones[hips]
            sc.frame_set(int(act.frame_range[0])); p0 = (a.matrix_world @ pb.matrix @ Vector((0, 0, 0)))
            sc.frame_set(int(act.frame_range[1])); p1 = (a.matrix_world @ pb.matrix @ Vector((0, 0, 0)))
            print("INFO cadera desplazamiento inicio->fin=%s" % (tuple(round(c, 2) for c in (p1 - p0)),), flush=True)
for i in bpy.data.images:
    print("INFO imagen", i.name, tuple(i.size), flush=True)
if len(args) >= 3 and meshes:
    mid = Vector((sum(v.x for v in bb) / len(bb), sum(v.y for v in bb) / len(bb), sum(v.z for v in bb) / len(bb)))
    sc = bpy.context.scene
    sc.frame_set(int(arms[0].animation_data.action.frame_range[0]) if arms and arms[0].animation_data and arms[0].animation_data.action else 1)
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x, sc.render.resolution_y = 600, 900
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = max(size) * 1.15
    sc.collection.objects.link(cam)
    sc.camera = cam
    up = 2
    for name, d in (("front", Vector((0, -1, 0))), ("side", Vector((1, 0, 0)))):
        cam.location = mid + d * 8
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = "%s\\%s_%s.png" % (args[1], args[2], name)
        bpy.ops.render.render(write_still=True)
print("PROCESO_TERMINADO", flush=True)
