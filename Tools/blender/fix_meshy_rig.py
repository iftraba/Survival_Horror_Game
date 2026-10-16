"""Limpia un personaje rigeado de Meshy (biped con huesos de Mixamo) para Unity (2026-10-17).

El FBX de Meshy no cuadra: el esqueleto va tumbado y con la cadera a ~100 m, y la malla de pie en metros. En reposo no se
nota, pero al animar la malla explota (en Unity sale a cientos de metros y no se ve). Aqui se coloca el esqueleto sobre la
malla, se aplican todas las transformaciones (escala 1, metros), se quita la animacion que trae y se exporta.

    blender --background --python fix_meshy_rig.py -- <entrada.fbx> <salida.fbx>
"""
import math
import sys

import bpy
from mathutils import Matrix, Vector

args = sys.argv[sys.argv.index("--") + 1:]
src, dst = args[0], args[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src, automatic_bone_orientation=False, ignore_leaf_bones=True)

for o in bpy.data.objects:
    if o.animation_data:
        o.animation_data_clear()
for a in list(bpy.data.actions):
    bpy.data.actions.remove(a)

arm = [o for o in bpy.data.objects if o.type == "ARMATURE"][0]
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
for pb in arm.pose.bones:                     # reposo
    pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
# En el mundo, el esqueleto de Meshy tiene bien las proporciones pero va tumbado (en Y) y con la cadera a ~100 m (su posicion
# en metros multiplicada por la escala 100 del armature), mientras la malla esta de pie en Z (0 a 1,7 m). Se gira +90 grados en X y
# se lleva la cadera a su sitio real (head_local, en metros); la malla conserva su posicion en el mundo.
keep = {m.name: m.matrix_world.copy() for m in meshes}
hips = arm.data.bones[0]
R = Matrix.Rotation(math.radians(90), 4, "X")
hw = R @ (arm.matrix_world @ hips.head_local)
arm.matrix_world = Matrix.Translation(Vector(hips.head_local) - hw) @ R @ arm.matrix_world
bpy.context.view_layer.update()
for m in meshes:
    m.matrix_world = keep[m.name]
for o in bpy.data.objects:
    o.select_set(False)
for group in ([arm], meshes):
    for o in group:
        o.select_set(True)
    bpy.context.view_layer.objects.active = group[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in group:
        o.select_set(False)
hips = arm.data.bones[0]
foot = arm.data.bones.get("mixamorig:LeftFoot")
print("tobillo a %.3f m" % (arm.matrix_world @ foot.head_local).z, flush=True)
print("hueso", hips.name, "a %.3f m" % (arm.matrix_world @ hips.head_local).z, flush=True)

zs = [(m.matrix_world @ v.co).z for m in meshes for v in m.data.vertices]
print("altura de la malla %.3f m (min %.3f max %.3f)" % (max(zs) - min(zs), min(zs), max(zs)), flush=True)
print("armature escala", tuple(arm.scale), "huesos", len(arm.data.bones), flush=True)

bpy.ops.export_scene.fbx(filepath=dst, use_selection=False, object_types={"ARMATURE", "MESH"}, apply_scale_options="FBX_SCALE_ALL",
                         add_leaf_bones=False, bake_anim=False, use_armature_deform_only=False, path_mode="STRIP",
                         armature_nodetype="NULL")
print("EXPORTADO", dst, flush=True)
