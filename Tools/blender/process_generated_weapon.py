"""Convierte un modelo de IA (alta poligonizacion, UV fragmentada) en un asset de juego.

Pasos: importar -> orientar/escalar -> copia de baja poligonizacion (decimate) -> UV limpias (smart project) ->
hornear color y normales desde la malla original -> exportar FBX ligero + PNG.

Se ejecuta en un Blender aparte, sin interfaz:
    blender -b --python process_generated_weapon.py -- <fbx> <textura_color> <carpeta_salida> <nombre> <triangulos> <tam_textura> <largo_m>

Convenciones de salida (las mismas del proyecto para armas):
    origen en el agarre, canon hacia -Y, arriba +Z.
"""
import math
import sys
import time

import bpy
from mathutils import Euler, Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
IN_FBX, IN_TEX, OUT_DIR, NAME = argv[0], argv[1], argv[2], argv[3]
TARGET_TRIS = int(argv[4])
TEX_SIZE = int(argv[5])
LENGTH_M = float(argv[6])
ROT_Z = float(argv[7]) if len(argv) > 7 else 90.0      # giro para que el canon quede hacia -Y
GRIP_Y = float(argv[8]) if len(argv) > 8 else 0.76     # posicion del agarre a lo largo del arma (0 = boca, 1 = culata)
GRIP_Z = float(argv[9]) if len(argv) > 9 else 0.38     # altura del agarre (0 = base, 1 = parte superior)

t0 = time.time()


def log(msg):
    print(f"[{time.time() - t0:6.1f}s] {msg}", flush=True)


# ---------------------------------------------------------------- 1) importar y orientar
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=IN_FBX)
high = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
high.name = NAME + "_high"
bpy.context.view_layer.objects.active = high
high.select_set(True)
log(f"importado: {len(high.data.polygons)} caras")

high.rotation_euler = Euler((0, 0, math.radians(ROT_Z)), "XYZ")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
dims = high.dimensions
k = LENGTH_M / max(dims)
high.scale = (k, k, k)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# origen en el agarre (heuristica: fraccion de la caja envolvente)
bb = [Vector(c) for c in high.bound_box]
xs = [v.x for v in bb]; ys = [v.y for v in bb]; zs = [v.z for v in bb]
ox = (min(xs) + max(xs)) / 2
oy = min(ys) + GRIP_Y * (max(ys) - min(ys))
oz = min(zs) + GRIP_Z * (max(zs) - min(zs))
high.data.transform(Matrix.Translation((-ox, -oy, -oz)))
high.location = (0, 0, 0)
log(f"dimensiones finales: {tuple(round(d, 3) for d in high.dimensions)} m")

# material del original con su textura de color
img_color = bpy.data.images.load(IN_TEX)
mat_h = bpy.data.materials.new(NAME + "_high")
mat_h.use_nodes = True
b = mat_h.node_tree.nodes["Principled BSDF"]
tn = mat_h.node_tree.nodes.new("ShaderNodeTexImage")
tn.image = img_color
mat_h.node_tree.links.new(tn.outputs["Color"], b.inputs["Base Color"])
high.data.materials.clear()
high.data.materials.append(mat_h)

# ---------------------------------------------------------------- 2) baja poligonizacion
low = high.copy()
low.data = high.data.copy()
low.name = NAME
bpy.context.scene.collection.objects.link(low)
bpy.ops.object.select_all(action="DESELECT")
bpy.context.view_layer.objects.active = low
low.select_set(True)
mod = low.modifiers.new("Decimate", "DECIMATE")
mod.decimate_type = "COLLAPSE"
mod.ratio = min(1.0, TARGET_TRIS / max(1, len(high.data.polygons)))
bpy.ops.object.modifier_apply(modifier=mod.name)
log(f"malla ligera: {len(low.data.polygons)} caras")

# ---------------------------------------------------------------- 3) UV limpias
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.004)
bpy.ops.object.mode_set(mode="OBJECT")
log("UV generadas")

# ---------------------------------------------------------------- 4) hornear
sc = bpy.context.scene
sc.render.engine = "CYCLES"
sc.cycles.device = "CPU"
sc.cycles.samples = 4
sc.cycles.use_denoising = False
bk = sc.render.bake
bk.use_selected_to_active = True
bk.margin = 10
bk.cage_extrusion = 0.004
bk.max_ray_distance = 0.02
bk.use_pass_direct = False
bk.use_pass_indirect = False
bk.use_pass_color = True


def bake(kind, image_name, noncolor):
    img = bpy.data.images.new(image_name, TEX_SIZE, TEX_SIZE, alpha=False)
    img.colorspace_settings.name = "Non-Color" if noncolor else "sRGB"
    mat = bpy.data.materials.new(image_name)
    mat.use_nodes = True
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    node.image = img
    mat.node_tree.nodes.active = node            # el horneado escribe en el nodo de imagen activo
    low.data.materials.clear()
    low.data.materials.append(mat)
    bpy.ops.object.select_all(action="DESELECT")
    high.select_set(True)
    low.select_set(True)
    bpy.context.view_layer.objects.active = low
    if kind == "NORMAL":
        bk.normal_space = "TANGENT"
    bpy.ops.object.bake(type=kind)
    path = f"{OUT_DIR}\\{image_name}.png"
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    log(f"horneado {kind} -> {path}")
    return img


bake("DIFFUSE", NAME + "_basecolor", False)
bake("NORMAL", NAME + "_normal", True)

# ---------------------------------------------------------------- 5) exportar solo la malla ligera
final_mat = bpy.data.materials.new(NAME + "_mat")
final_mat.use_nodes = True
low.data.materials.clear()
low.data.materials.append(final_mat)
bpy.data.objects.remove(high, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
low.select_set(True)
bpy.context.view_layer.objects.active = low
bpy.ops.export_scene.fbx(
    filepath=f"{OUT_DIR}\\{NAME}.fbx", use_selection=True, object_types={"MESH"},
    apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
    mesh_smooth_type="FACE", path_mode="COPY", bake_anim=False)
log(f"FBX exportado: {len(low.data.polygons)} caras. FIN")
print("PROCESO_TERMINADO", flush=True)
