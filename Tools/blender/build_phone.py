"""Telefono antiguo de disco (baquelita negra) para el punto de guardado.

Origen: centro de la base, en el suelo de su soporte. El frente (disco de marcar) mira a -Y (Blender), que en Unity
queda como +Z. Medidas reales: ~0.24 x 0.20 x 0.16 m.
Uso: blender --background --python build_phone.py -- <salida.fbx>
"""
import math
import sys

import bpy
import bmesh
from mathutils import Vector

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "phone.fbx"
bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, rgb, rough, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m


BAKE = mat("bakelite", (0.015, 0.014, 0.014), 0.22)
BRASS = mat("brass", (0.62, 0.45, 0.16), 0.35, 0.9)
CREAM = mat("dial_cream", (0.78, 0.74, 0.62), 0.6)
DARK = mat("dial_dark", (0.02, 0.02, 0.02), 0.8)
CORD = mat("cord", (0.03, 0.03, 0.03), 0.7)
parts = []


def finish(o, material, bevel=0.0):
    o.data.materials.append(material)
    if bevel > 0:
        bpy.context.view_layer.objects.active = o
        md = o.modifiers.new("bevel", "BEVEL")
        md.width = bevel
        md.segments = 3
        bpy.ops.object.modifier_apply(modifier="bevel")
    parts.append(o)
    return o


def box(name, c, s, material, bevel=0.004):
    bpy.ops.mesh.primitive_cube_add(location=c)
    o = bpy.context.object
    o.name = name
    o.scale = (s[0] / 2, s[1] / 2, s[2] / 2)
    bpy.ops.object.transform_apply(scale=True)
    return finish(o, material, bevel)


def cyl(name, c, r, h, material, rot=(0, 0, 0), seg=32, bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=seg, radius=r, depth=h, location=c, rotation=rot)
    o = bpy.context.object
    o.name = name
    return finish(o, material, bevel)


# ---- cuerpo: base ancha + parte alta mas estrecha con el frente inclinado donde va el disco
box("base", (0, 0, 0.025), (0.245, 0.205, 0.05), BAKE, 0.010)
body = box("cuerpo", (0, 0.004, 0.10), (0.205, 0.175, 0.10), BAKE, 0.014)
# frente inclinado: se eleva el borde trasero de la cara superior
bm = bmesh.new()
bm.from_mesh(body.data)
for v in bm.verts:
    if v.co.z > 0 and v.co.y > 0:
        v.co.z -= 0.010
    if v.co.z > 0 and v.co.y < 0:
        v.co.z -= 0.075
bm.to_mesh(body.data)
bm.free()

# ---- disco de marcar (en la parte inclinada, mirando hacia delante y arriba)
tilt = math.radians(20)
cx, cy, cz = 0.0, -0.040, 0.095
cyl("anillo_laton", (cx, cy, cz), 0.060, 0.008, BRASS, rot=(tilt, 0, 0), seg=48)
cyl("disco", (cx, cy - 0.002, cz + 0.002), 0.053, 0.008, CREAM, rot=(tilt, 0, 0), seg=48)
cyl("centro", (cx, cy - 0.004, cz + 0.004), 0.016, 0.006, BRASS, rot=(tilt, 0, 0), seg=24)
# agujeros: diez circulos oscuros sobre un arco de 270 grados (el hueco queda abajo a la derecha, donde esta el tope)
nrm = Vector((0, -math.sin(tilt), math.cos(tilt)))      # normal del disco: hacia delante y arriba
up = Vector((0, math.cos(tilt), math.sin(tilt)))        # direccion "arriba" sobre el plano del disco
base = Vector((cx, cy - 0.003, cz + 0.003))
for i in range(10):
    a = math.radians(10 + i * 30)
    p = base + Vector((math.cos(a) * 0.036, 0, 0)) + up * (math.sin(a) * 0.036) + nrm * 0.004
    cyl("agujero%d" % i, p, 0.0085, 0.003, DARK, rot=(tilt, 0, 0), seg=16)
# tope de dedo
cyl("tope", base + Vector((0.050, 0, 0)) + up * -0.040 + nrm * 0.006, 0.006, 0.012, BRASS, rot=(tilt, 0, 0), seg=12)

# ---- horquilla y auricular
for sx in (-0.07, 0.07):
    box("horquilla", (sx, 0.045, 0.136), (0.018, 0.045, 0.030), BAKE, 0.004)
hz = 0.164
box("mango", (0, 0.045, hz), (0.200, 0.030, 0.026), BAKE, 0.010)
cyl("auricular", (-0.108, 0.045, hz - 0.002), 0.034, 0.032, BAKE, rot=(math.pi / 2, 0, 0), seg=32, bevel=0.004)
cyl("microfono", (0.108, 0.045, hz - 0.002), 0.034, 0.032, BAKE, rot=(math.pi / 2, 0, 0), seg=32, bevel=0.004)
cyl("tapa_aur", (-0.108, 0.045 - 0.017, hz - 0.002), 0.024, 0.004, DARK, rot=(math.pi / 2, 0, 0), seg=24)
cyl("tapa_mic", (0.108, 0.045 - 0.017, hz - 0.002), 0.024, 0.004, DARK, rot=(math.pi / 2, 0, 0), seg=24)

# ---- cable en espiral aproximado: tramos que bajan del auricular por el costado trasero
pts = [(0.132, 0.045, 0.150), (0.150, 0.06, 0.10), (0.140, 0.10, 0.045), (0.128, 0.098, 0.012)]
for a, b in zip(pts[:-1], pts[1:]):
    va, vb = Vector(a), Vector(b)
    mid = (va + vb) / 2
    d = vb - va
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.004, depth=d.length, location=mid)
    o = bpy.context.object
    o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = d.to_track_quat("Z", "Y")
    o.name = "cable"
    finish(o, CORD)

# ---- patitas de goma
for sx in (-0.095, 0.095):
    for sy in (-0.075, 0.075):
        cyl("pata", (sx, sy, 0.002), 0.012, 0.004, DARK, seg=12)

# ---- unir y exportar
bpy.ops.object.select_all(action="DESELECT")
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
ph = bpy.context.object
ph.name = "Phone"
bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type="ORIGIN_CURSOR")   # origen en el suelo del telefono
bpy.ops.object.shade_smooth()
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
dims = ph.dimensions
print("DIMENSIONES %.3f %.3f %.3f" % (dims.x, dims.y, dims.z))
bpy.ops.export_scene.fbx(filepath=OUT, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                         use_selection=False, object_types={"MESH"}, bake_space_transform=True, mesh_smooth_type="FACE", path_mode="COPY", bake_anim=False)

# vista previa opcional: segundo argumento = prefijo de los PNG (frontal 3/4, lateral, superior)
args = sys.argv[sys.argv.index("--") + 1:]
if len(args) > 1:
    for m in bpy.data.materials:
        b = m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value
        m.diffuse_color = (b[0] ** 0.45, b[1] ** 0.45, b[2] ** 0.45, 1)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "MATERIAL"
    sc.render.resolution_x, sc.render.resolution_y = 700, 500
    sc.world = bpy.data.worlds.new("w"); sc.world.color = (0.35, 0.37, 0.4)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = "ORTHO"; cam.data.ortho_scale = 0.42
    for name, loc, rot in (("3q", (-0.35, -0.45, 0.35), (math.radians(65), 0, math.radians(-38))),
                           ("lado", (0.6, 0.05, 0.08), (math.radians(90), 0, math.radians(90))),
                           ("arriba", (0, 0.0, 0.7), (0, 0, 0))):
        cam.location = loc; cam.rotation_euler = rot
        sc.render.filepath = args[1] + "_" + name + ".png"
        bpy.ops.render.render(write_still=True)
print("PROCESO_TERMINADO")
