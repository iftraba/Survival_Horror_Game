"""Endereza la pose base de un personaje generado con IA (encorvado, en zancada) para que encaje con el esqueleto
y las animaciones del proyecto, que parten de una pose recta: columna, brazos y piernas verticales.

Se crea un esqueleto temporal con las articulaciones reales del modelo, se calculan pesos automaticos, se lleva cada
hueso a su posicion recta (pose) y se aplica el modificador a la malla. Despues: pies al suelo y altura final fija.

    blender -b --python straighten_character.py -- <fbx_entrada> <fbx_salida> <altura_m>
"""
import math
import sys

import bpy
from mathutils import Matrix, Vector

IN_FBX, OUT_FBX, HEIGHT = sys.argv[sys.argv.index("--") + 1:][:3]
HEIGHT = float(HEIGHT)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=IN_FBX)
mesh = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
verts = [mesh.matrix_world @ v.co for v in mesh.data.vertices]


def centroid(zc, half, side=0, xmin=0.0, xmax=9.0):
    pts = [v for v in verts if abs(v.z - zc) <= half and xmin <= abs(v.x) <= xmax and (side == 0 or v.x * side > 0)]
    if not pts:
        return (0.0, 0.0)
    return (sum(p.x for p in pts) / len(pts), sum(p.y for p in pts) / len(pts))


def torso_y(z):
    return centroid(z, 0.05, 0, 0.0, 0.12)[1]


Z_HIP, Z_SPINE, Z_NECK, Z_HEAD, Z_TOP = 0.95, 1.05, 1.40, 1.52, 1.80
Z_SH, Z_EL, Z_WR, Z_FI, Z_KN, Z_AN = 1.46, 1.17, 0.92, 0.73, 0.50, 0.10
yh = torso_y(1.0)

# ---- esqueleto temporal con las articulaciones REALES (direcciones inclinadas)
real = {}   # nombre -> (padre, head, tail)
real["Hips"] = (None, Vector((0, yh, Z_HIP)), Vector((0, torso_y(Z_SPINE), Z_SPINE)))
real["Spine"] = ("Hips", real["Hips"][2], Vector((0, torso_y(Z_NECK), Z_NECK)))
real["Neck"] = ("Spine", real["Spine"][2], Vector((0, torso_y(Z_HEAD), Z_HEAD)))
real["Head"] = ("Neck", real["Neck"][2], Vector((0, centroid(1.68, 0.06, 0, 0.0, 0.2)[1], Z_TOP)))
for side, sx in (("L", 1), ("R", -1)):
    sh = Vector((0.19 * sx, torso_y(Z_SH), Z_SH))
    ex, ey = centroid(Z_EL, 0.04, sx, 0.2, 0.45)
    wx, wy = centroid(Z_WR, 0.04, sx, 0.2, 0.45)
    el = Vector((ex if abs(ex) > 0.2 else 0.24 * sx, ey, Z_EL))
    wr = Vector((wx if abs(wx) > 0.2 else 0.26 * sx, wy, Z_WR))
    fi = Vector((wr.x, wr.y, Z_FI))
    real["UpperArm." + side] = ("Spine", sh, el)
    real["LowerArm." + side] = ("UpperArm." + side, el, wr)
    real["Hand." + side] = ("LowerArm." + side, wr, fi)
    hx, hy = centroid(0.85, 0.05, sx, 0.0, 0.3)
    kx, ky = centroid(Z_KN, 0.05, sx, 0.0, 0.3)
    ax, ay = centroid(0.12, 0.06, sx, 0.0, 0.3)
    hip, kn, an = Vector((hx, hy, Z_HIP)), Vector((kx, ky, Z_KN)), Vector((ax, ay, Z_AN))
    real["UpperLeg." + side] = ("Hips", hip, kn)
    real["LowerLeg." + side] = ("UpperLeg." + side, kn, an)
    real["Foot." + side] = ("LowerLeg." + side, an, Vector((ax, ay - 0.22, 0.05)))

data = bpy.data.armatures.new("TmpArm")
arm = bpy.data.objects.new("TmpArm", data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
for name, (parent, head, tail) in real.items():
    eb = data.edit_bones.new(name)
    eb.head, eb.tail = head, tail
    if parent:
        eb.parent = data.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")

bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_AUTO")

# ---- posiciones y direcciones objetivo (cuerpo recto)
UP, DOWN = Vector((0, 0, 1)), Vector((0, 0, -1))
tgt = {}   # nombre -> (head, direccion, longitud)


def length(n):
    return (real[n][2] - real[n][1]).length


tgt["Hips"] = (Vector((0, yh, Z_HIP)), UP)
tgt["Spine"] = (Vector((0, yh, Z_SPINE)), UP)
tgt["Neck"] = (Vector((0, yh, Z_NECK)), UP)
tilt = math.radians(10)
tgt["Head"] = (Vector((0, yh, Z_HEAD)), Vector((0, -math.sin(tilt), math.cos(tilt))))
chains = {"Arm": ("UpperArm", "LowerArm", "Hand"), "Leg": ("UpperLeg", "LowerLeg", "Foot")}
for side, sx in (("L", 1), ("R", -1)):
    # brazos: cuelgan casi verticales con la apertura lateral original, sin la inclinacion hacia delante/atras
    head = Vector((0.19 * sx, yh - 0.02, Z_SH))
    for part in chains["Arm"]:
        n = f"{part}.{side}"
        d = real[n][2] - real[n][1]
        d = Vector((d.x, 0, d.z)).normalized()
        tgt[n] = (head.copy(), d)
        head = head + d * length(n)
    # piernas: verticales bajo la cadera
    head = Vector((real["UpperLeg." + side][1].x, yh, Z_HIP))
    for part in ("UpperLeg", "LowerLeg"):
        n = f"{part}.{side}"
        tgt[n] = (head.copy(), DOWN)
        head = head + DOWN * length(n)
    tgt["Foot." + side] = (head.copy(), None)   # el pie conserva su orientacion (plano sobre el suelo)

for name in real:   # el orden de insercion ya va de padres a hijos
    pb = arm.pose.bones[name]
    b = arm.data.bones[name]
    new_head, new_dir = tgt[name]
    rest_dir = (b.tail_local - b.head_local).normalized()
    R = Matrix.Identity(3) if new_dir is None else rest_dir.rotation_difference(new_dir).to_matrix()
    pb.matrix = Matrix.Translation(new_head) @ (R @ b.matrix_local.to_3x3()).to_4x4()
    bpy.context.view_layer.update()

# aplicar la deformacion a la malla y quitar el esqueleto temporal
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.context.view_layer.objects.active = mesh
bpy.ops.object.modifier_apply(modifier=[m for m in mesh.modifiers if m.type == "ARMATURE"][0].name)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.data.objects.remove(arm, do_unlink=True)

# pies al suelo, centrado y altura final
vs = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
zmin = min(v.z for v in vs)
zmax = max(v.z for v in vs)
k = HEIGHT / (zmax - zmin)
mesh.data.transform(Matrix.Translation((0, 0, -zmin)))
mesh.data.transform(Matrix.Scale(k, 4))
bb = [mesh.matrix_world @ Vector(c) for c in mesh.bound_box]
cx = (min(v.x for v in bb) + max(v.x for v in bb)) / 2
mesh.data.transform(Matrix.Translation((-cx, 0, 0)))
print(f"ALTURA original {zmax - zmin:.3f} -> {HEIGHT:.3f} (escala {k:.3f})", flush=True)

bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.context.view_layer.objects.active = mesh
bpy.ops.export_scene.fbx(
    filepath=OUT_FBX, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", path_mode="COPY", bake_anim=False)
print("PROCESO_TERMINADO", flush=True)
