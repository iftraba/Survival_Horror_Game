"""Reduce un FBX ya preparado para Mixamo (una malla, textura de color incrustada) a un numero objetivo de caras
con el modificador Decimate (colapso, conserva UV y bordes) y lo vuelve a exportar con la textura incrustada.
Los modelos de Meshy salen con ~600.000 caras: demasiado para un juego con decenas de zombis.

    blender -b --python decimate_for_mixamo.py -- <fbx_entrada> <fbx_salida> [caras_objetivo=50000]
"""
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:]
IN_FBX, OUT_FBX = args[:2]
TARGET = int(args[2]) if len(args) > 2 else 50000

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=IN_FBX)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
mesh = meshes[0]
bpy.context.view_layer.objects.active = mesh
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)

# triangular primero para contar caras reales
tri = mesh.modifiers.new("tri", "TRIANGULATE")
bpy.ops.object.modifier_apply(modifier=tri.name)
before = len(mesh.data.polygons)
dec = mesh.modifiers.new("dec", "DECIMATE")
dec.decimate_type = "COLLAPSE"
dec.ratio = min(1.0, TARGET / max(1, before))
dec.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=dec.name)
after = len(mesh.data.polygons)
bpy.ops.object.shade_smooth()
print("CARAS", before, "->", after, flush=True)

bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={"MESH"},
                         apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=True,
                         mesh_smooth_type="FACE", bake_anim=False)
print("PROCESO_TERMINADO", flush=True)
