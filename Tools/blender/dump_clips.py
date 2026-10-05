"""Resumen de todos los FBX de animacion de una carpeta: fotogramas, desplazamiento de la cadera (movimiento de raiz)
y si es ciclica (primer y ultimo fotograma parecidos).   blender -b --python dump_clips.py -- <carpeta>
"""
import glob
import os
import sys

import bpy
from mathutils import Vector

folder = sys.argv[sys.argv.index("--") + 1]
for path in sorted(glob.glob(os.path.join(folder, "*.fbx"))):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try:
        bpy.ops.import_scene.fbx(filepath=path)
    except Exception as e:
        print("CLIP", os.path.basename(path), "ERROR", e, flush=True)
        continue
    arm = next((o for o in bpy.context.scene.objects if o.type == "ARMATURE"), None)
    if arm is None or not arm.animation_data or not arm.animation_data.action:
        print("CLIP", os.path.basename(path), "sin animacion", flush=True)
        continue
    act = arm.animation_data.action
    f0, f1 = int(act.frame_range[0]), int(act.frame_range[1])
    hips = next(b for b in arm.pose.bones if b.name.endswith("Hips"))
    sc = bpy.context.scene

    def pos(f):
        sc.frame_set(f)
        return arm.matrix_world @ hips.matrix @ Vector((0, 0, 0))

    a, b = pos(f0), pos(f1)
    d = b - a
    # diferencia de pose entre primer y ultimo fotograma (ciclo cerrado si es pequena)
    sc.frame_set(f0)
    p0 = [(arm.matrix_world @ pb.matrix @ Vector((0, 0, 0))) - a for pb in arm.pose.bones]
    sc.frame_set(f1)
    p1 = [(arm.matrix_world @ pb.matrix @ Vector((0, 0, 0))) - b for pb in arm.pose.bones]
    diff = max((x - y).length for x, y in zip(p0, p1))
    fps = sc.render.fps
    print("CLIP %-34s fot=%4d (%.1fs) cadera_dxyz=(%.2f, %.2f, %.2f) horiz=%.2fm ciclo=%.2f huesos=%d" % (
        os.path.basename(path), f1 - f0 + 1, (f1 - f0 + 1) / fps, d.x, d.y, d.z, Vector((d.x, d.y)).length, diff, len(arm.data.bones)), flush=True)
print("PROCESO_TERMINADO", flush=True)
