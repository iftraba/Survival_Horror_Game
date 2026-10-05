"""Lleva los brazos de un personaje sin esqueleto a pose en A (para el auto-rig de Mixamo).

Cada brazo se gira alrededor de su hombro desde su direccion actual hasta una direccion lateral inclinada
'angulo' grados por debajo de la horizontal. Los vertices se giran con un peso que crece a lo largo del brazo
(0 en el hombro, 1 a partir de unos centimetros), asi el hombro se dobla suave en vez de partirse.
Despues: pies al suelo, altura final y textura incrustada en el FBX.

    blender -b --python arms_to_apose.py -- <fbx_entrada> <textura_color> <fbx_salida> [angulo] [altura]
"""
import math
import sys

import bpy
from mathutils import Matrix, Quaternion, Vector

args = sys.argv[sys.argv.index("--") + 1:]
IN_FBX, IN_TEX, OUT_FBX = args[:3]
ANGLE = float(args[3]) if len(args) > 3 else 40.0
HEIGHT = float(args[4]) if len(args) > 4 else 1.8

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=IN_FBX)
mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
mw = mesh.matrix_world
verts = mesh.data.vertices
co = [mw @ v.co for v in verts]
zmin = min(p.z for p in co)
zmax = max(p.z for p in co)
H = zmax - zmin

for side in (1, -1):
    # el brazo: lo que queda fuera del torso por ese lado y por encima del pecho
    arm_pts = [p for p in co if p.x * side > 0.24 * H / 1.8 and p.z > zmin + 0.62 * H]
    if not arm_pts:
        continue
    # punta del brazo = el punto mas alejado del eje del cuerpo
    tip = max(arm_pts, key=lambda p: Vector((p.x, p.y)).length + 0.2 * (p.z - zmin))
    # hombro: a la altura de la base del cuello, a ~0.17 m del centro por ese lado
    shoulder_z = zmin + 0.815 * H
    torso_y = sum(p.y for p in co if abs(p.x) < 0.1 and abs(p.z - shoulder_z) < 0.05) / max(1, sum(1 for p in co if abs(p.x) < 0.1 and abs(p.z - shoulder_z) < 0.05))
    S = Vector((side * 0.17 * H / 1.8, torso_y, shoulder_z))
    d = (tip - S).normalized()
    t = Vector((side * math.cos(math.radians(ANGLE)), 0.0, -math.sin(math.radians(ANGLE)))).normalized()
    q = d.rotation_difference(t)
    print(f"LADO {side}: hombro {tuple(round(c, 2) for c in S)} punta {tuple(round(c, 2) for c in tip)}", flush=True)
    for i, v in enumerate(verts):
        p = co[i]
        rel = p - S
        along = rel.dot(d)
        lateral = p.x * side
        if lateral < 0.1 * H / 1.8 or along < -0.02:
            continue
        # peso: 0 en el hombro, 1 a 12 cm a lo largo del brazo; los puntos pegados al torso no se mueven
        w = max(0.0, min(1.0, (along + 0.02) / 0.14))
        w *= max(0.0, min(1.0, (lateral - 0.1 * H / 1.8) / 0.08))
        if w <= 0.0:
            continue
        qw = Quaternion().slerp(q, w)
        co[i] = S + qw @ rel

inv = mw.inverted()
for i, v in enumerate(verts):
    v.co = inv @ co[i]
mesh.data.update()

# pies al suelo, centrado, altura final
bpy.context.view_layer.update()
pts = [mesh.matrix_world @ v.co for v in verts]
zmin = min(p.z for p in pts)
k = HEIGHT / (max(p.z for p in pts) - zmin)
cx = (min(p.x for p in pts) + max(p.x for p in pts)) / 2
cy = (min(p.y for p in pts) + max(p.y for p in pts)) / 2
mesh.data.transform(mw.inverted() @ Matrix.Scale(k, 4) @ Matrix.Translation((-cx, -cy, -zmin)) @ mw)
mesh.data.update()

img = bpy.data.images.load(IN_TEX)
mat = bpy.data.materials.new(mesh.name + "_mat")
mat.use_nodes = True
tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = img
mat.node_tree.links.new(tex.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
mesh.data.materials.clear()
mesh.data.materials.append(mat)

bpy.context.view_layer.update()
bb = [mesh.matrix_world @ Vector(c) for c in mesh.bound_box]
print("MEDIDAS", tuple(round(max(v[i] for v in bb) - min(v[i] for v in bb), 3) for i in range(3)), flush=True)
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={"MESH"},
                         apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=True,
                         mesh_smooth_type="FACE", bake_anim=False)
print("PROCESO_TERMINADO", flush=True)
