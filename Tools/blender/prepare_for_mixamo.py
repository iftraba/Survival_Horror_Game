"""Deja un FBX de Meshy (con el nuevo flujo: 40k poligonos, malla limpia, mapas PBR) listo para subir a Mixamo:
una sola malla, material simple con SOLO la textura de color incrustada, pies en el suelo, 1.8 m de alto,
mismos ejes que los FBX que Mixamo ya acepto (Y arriba). No toca la pose ni deforma nada.

    blender -b --python prepare_for_mixamo.py -- <fbx_meshy> <fbx_salida> [altura] [tpose]

Con 'tpose' los brazos se enderezan antes a una T con codos rectos (straighten_arms.py).
"""
import sys

import bpy
from mathutils import Matrix, Vector

args = sys.argv[sys.argv.index("--") + 1:]
IN_FBX, OUT_FBX = args[:2]
HEIGHT = float(args[2]) if len(args) > 2 else 1.8
TPOSE = "tpose" in args[3:]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=IN_FBX)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if not meshes:
    raise SystemExit("sin mallas")
# una sola malla
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
mesh = bpy.context.view_layer.objects.active
mesh.name = "Character"
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

if TPOSE:
    import os
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import straighten_arms
    for side, msg in straighten_arms.straighten(mesh, 20.0):
        print("BRAZO", side, msg, flush=True)

# textura de color: la imagen enlazada a "Base Color" del material
color_img = None
for slot in mesh.material_slots:
    mat = slot.material
    if mat is None or not mat.use_nodes:
        continue
    bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        continue
    link = bsdf.inputs["Base Color"].links
    node = link[0].from_node if link else None
    while node is not None and node.type != "TEX_IMAGE":      # puede haber nodos intermedios (mezcla, color)
        ins = [i for i in node.inputs if i.links]
        node = ins[0].links[0].from_node if ins else None
    if node is not None:
        color_img = node.image
        break
if color_img is None:
    color_img = next((i for i in bpy.data.images if i.name == "Image_0"), None)
print("TEXTURA", color_img.name if color_img else None, tuple(color_img.size) if color_img else None, flush=True)

mat = bpy.data.materials.new("Character_mat")
mat.use_nodes = True
tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = color_img
mat.node_tree.links.new(tex.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
mesh.data.materials.clear()
mesh.data.materials.append(mat)

# pies al suelo, centrado, altura final (el eje vertical es Z tras importar)
pts = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
zmin = min(p.z for p in pts)
k = HEIGHT / (max(p.z for p in pts) - zmin)
cx = (min(p.x for p in pts) + max(p.x for p in pts)) / 2
cy = (min(p.y for p in pts) + max(p.y for p in pts)) / 2
mesh.data.transform(Matrix.Scale(k, 4) @ Matrix.Translation((-cx, -cy, -zmin)))
mesh.data.update()
bb = [mesh.matrix_world @ Vector(c) for c in mesh.bound_box]
print("CARAS", len(mesh.data.polygons), "MEDIDAS", tuple(round(max(v[i] for v in bb) - min(v[i] for v in bb), 3) for i in range(3)), flush=True)

bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={"MESH"},
                         apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=True,
                         mesh_smooth_type="FACE", bake_anim=False)
print("PROCESO_TERMINADO", flush=True)
