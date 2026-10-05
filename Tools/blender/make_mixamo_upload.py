"""Prepara un personaje para el auto-rig de Mixamo: pose en A (brazos separados ~40 grados del cuerpo),
una sola malla sin esqueleto y la textura de color incrustada en el FBX.

Entrada: el FBX ya rigeado con el esqueleto del proyecto (rig_generated_character.py), que se usa solo para
abrir los brazos; la malla resultante se exporta sin esqueleto (Mixamo pone el suyo).

    blender -b --python make_mixamo_upload.py -- <fbx_rigeado> <textura_color> <fbx_salida> [angulo]
"""
import math
import sys

import bpy
from mathutils import Matrix, Vector

args = sys.argv[sys.argv.index("--") + 1:]
IN_FBX, IN_TEX, OUT_FBX = args[:3]
ANGLE = float(args[3]) if len(args) > 3 else 40.0

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=IN_FBX)
arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
bpy.context.view_layer.update()

# Abrir los brazos: cada UpperArm gira alrededor del eje delante-detras que pasa por el hombro.
# Personaje mirando a -Y, izquierda en +X: girar -angulo en Y lleva la punta del brazo izquierdo hacia +X.
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
for side, sign in (("L", -1.0), ("R", 1.0)):
    pb = arm.pose.bones.get(f"UpperArm.{side}")
    if pb is None:
        continue
    head = arm.matrix_world @ pb.head
    rot = Matrix.Rotation(math.radians(ANGLE * sign), 4, "Y")
    world = arm.matrix_world @ pb.matrix
    new_world = Matrix.Translation(head) @ rot @ Matrix.Translation(-head) @ world
    pb.matrix = arm.matrix_world.inverted() @ new_world
    bpy.context.view_layer.update()
bpy.ops.object.mode_set(mode="OBJECT")

# Aplicar la pose a la malla y quitar el esqueleto
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.context.view_layer.objects.active = mesh
for m in list(mesh.modifiers):
    if m.type == "ARMATURE":
        bpy.ops.object.modifier_apply(modifier=m.name)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.data.objects.remove(arm, do_unlink=True)
mesh.vertex_groups.clear()

# Material con la textura de color (se incrusta en el FBX)
img = bpy.data.images.load(IN_TEX)
mat = bpy.data.materials.new(mesh.name + "_mat")
mat.use_nodes = True
tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = img
mat.node_tree.links.new(tex.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
mesh.data.materials.clear()
mesh.data.materials.append(mat)

bb = [mesh.matrix_world @ Vector(c) for c in mesh.bound_box]
print("MEDIDAS", tuple(round(max(v[i] for v in bb) - min(v[i] for v in bb), 3) for i in range(3)), flush=True)

bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={"MESH"},
                         apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=True,
                         mesh_smooth_type="FACE", bake_anim=False)
print("PROCESO_TERMINADO", flush=True)
