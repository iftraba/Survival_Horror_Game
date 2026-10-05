"""Pone al personaje generado con IA el MISMO esqueleto que los zombis del proyecto (build_characters.bone_defs),
con las articulaciones colocadas donde estan en la malla, para reutilizar sus animaciones en Unity.

Entrada: FBX ya limpio (process_generated_weapon.py con origen en los pies, mirando a -Y).
Los huesos verticales conservan la orientacion de ejes del esqueleto original (X=mundo X, Y=mundo Z, Z=mundo -Y);
solo cambian las posiciones de las cabezas. Pesos: automaticos (calor) con reserva por hueso mas cercano.

    blender -b --python rig_generated_character.py -- <fbx_entrada> <textura_color> <fbx_salida>
"""
import sys

import bpy
from mathutils import Vector

IN_FBX, IN_TEX, OUT_FBX = sys.argv[sys.argv.index("--") + 1:][:3]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=IN_FBX)
mesh = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
mesh.name = "ZombieGenerated"
verts = [mesh.matrix_world @ v.co for v in mesh.data.vertices]


def centroid(zc, half, side=0, xmin=0.0, xmax=9.0, ymax=9.0):
    """Centro (x, y) de los vertices en una franja de altura; side = +1/-1 filtra por lado, 0 usa |x|."""
    pts = [v for v in verts if abs(v.z - zc) <= half and xmin <= abs(v.x) <= xmax and (side == 0 or v.x * side > 0) and abs(v.y) <= ymax]
    if not pts:
        return (0.0, 0.0)
    return (sum(p.x for p in pts) / len(pts), sum(p.y for p in pts) / len(pts))


def torso_y(z):
    return centroid(z, 0.05, 0, 0.0, 0.12)[1]


# alturas de articulaciones (m), medidas sobre el modelo: ver render frontal/lateral
Z_HIP, Z_SPINE, Z_NECK, Z_HEAD, Z_TOP = 0.95, 1.05, 1.40, 1.52, 1.80
Z_SHOULDER, Z_ELBOW, Z_WRIST, Z_FINGERS = 1.46, 1.17, 0.92, 0.73
Z_KNEE, Z_ANKLE = 0.50, 0.10
SH_X = 0.19

bones = []   # (nombre, padre, head, tail)
yh = torso_y(1.0)
bones.append(("Hips", None, (0, yh, Z_HIP), (0, yh, Z_SPINE)))
bones.append(("Spine", "Hips", (0, torso_y(Z_SPINE), Z_SPINE), (0, torso_y(Z_SPINE), Z_NECK)))
bones.append(("Neck", "Spine", (0, torso_y(Z_NECK), Z_NECK), (0, torso_y(Z_NECK), Z_HEAD)))
bones.append(("Head", "Neck", (0, torso_y(Z_NECK) - 0.03, Z_HEAD), (0, torso_y(Z_NECK) - 0.03, Z_TOP)))
for side, sx in (("L", 1), ("R", -1)):
    ex, ey = centroid(Z_ELBOW, 0.04, sx, 0.2, 0.45)       # el codo queda fuera del torso
    wx, wy = centroid(Z_WRIST, 0.04, sx, 0.2, 0.45)
    ex = ex if abs(ex) > 0.2 else 0.24 * sx
    wx = wx if abs(wx) > 0.2 else 0.26 * sx
    sy = torso_y(Z_SHOULDER)
    bones += [
        ("UpperArm." + side, "Spine", (SH_X * sx, sy, Z_SHOULDER), (SH_X * sx, sy, Z_ELBOW)),
        ("LowerArm." + side, "UpperArm." + side, (ex, ey, Z_ELBOW), (ex, ey, Z_WRIST)),
        ("Hand." + side, "LowerArm." + side, (wx, wy, Z_WRIST), (wx, wy, Z_FINGERS)),
    ]
    hx, hy = centroid(0.85, 0.05, sx, 0.0, 0.3)
    kx, ky = centroid(Z_KNEE, 0.05, sx, 0.0, 0.3)
    ax, ay = centroid(0.12, 0.06, sx, 0.0, 0.3)
    bones += [
        ("UpperLeg." + side, "Hips", (hx, hy, Z_HIP), (hx, hy, Z_KNEE)),
        ("LowerLeg." + side, "UpperLeg." + side, (kx, ky, Z_KNEE), (kx, ky, Z_ANKLE)),
        ("Foot." + side, "LowerLeg." + side, (ax, ay, Z_ANKLE), (ax, ay - 0.22, 0.05)),
    ]
for b in bones:
    print("BONE", b[0], tuple(round(c, 3) for c in b[2]), flush=True)

data = bpy.data.armatures.new("Armature")
arm = bpy.data.objects.new("Armature", data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
for name, parent, head, tail in bones:
    eb = data.edit_bones.new(name)
    eb.head = Vector(head)
    eb.tail = Vector(tail)
    if parent:
        eb.parent = data.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")

# pesos automaticos
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_AUTO")

# vertices sin peso: se asignan al hueso mas cercano
names = [b[0] for b in bones]
segs = {b[0]: (Vector(b[2]), Vector(b[3])) for b in bones}


def dist_to_bone(p, name):
    a, b = segs[name]
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(1e-9, ab.length_squared)))
    return (p - (a + ab * t)).length


vg = mesh.vertex_groups
fixed = 0
for v in mesh.data.vertices:
    total = sum(g.weight for g in v.groups if vg[g.group].name in names)
    if total < 1e-4:
        p = mesh.matrix_world @ v.co
        best = min(names, key=lambda n: dist_to_bone(p, n))
        (vg.get(best) or vg.new(name=best)).add([v.index], 1.0, "REPLACE")
        fixed += 1
print(f"vertices reasignados al hueso mas cercano: {fixed} de {len(mesh.data.vertices)}", flush=True)

# Limpieza de brazos: con los brazos pegados al cuerpo, los pesos automaticos dejan partes del torso (chaleco,
# costados) atadas a los huesos del brazo y se estiran como una tela al apuntar. Un vertice que esta mas cerca del
# eje del torso que de la cadena del brazo pierde la influencia del brazo, que pasa a la columna o la cadera.
ARM_FIX = len(sys.argv[sys.argv.index("--") + 1:]) > 3 and sys.argv[sys.argv.index("--") + 1:][3] == "armfix"
if ARM_FIX:
    torso_c = Vector((0.0, torso_y(1.2)))
    arm_names = {s: [f"UpperArm.{s}", f"LowerArm.{s}", f"Hand.{s}"] for s in ("L", "R")}
    moved = 0
    for v in mesh.data.vertices:
        p = mesh.matrix_world @ v.co
        if p.z < 0.6 or p.z > 1.55:
            continue
        side = "L" if p.x > 0 else "R"
        arm_w = [(g, vg[g.group].name) for g in v.groups if vg[g.group].name in arm_names["L"] + arm_names["R"]]
        if not arm_w:
            continue
        d_torso = (Vector((p.x, p.y)) - torso_c).length - 0.15
        d_arm = min(dist_to_bone(p, n) for n in arm_names[side]) - 0.055
        if d_torso >= d_arm:
            continue
        total = sum(g.weight for g, _ in arm_w)
        target = "Spine" if p.z > 1.0 else "Hips"
        for g, n in arm_w:
            vg[n].remove([v.index])
        (vg.get(target) or vg.new(name=target)).add([v.index], total, "ADD")
        moved += 1
    print(f"limpieza de brazos: {moved} vertices pasan del brazo al torso", flush=True)

    # Malla fusionada: si la mano del modelo original tocaba la cadera, hay triangulos que unen antebrazo/mano con
    # cadera/piernas/columna y al levantar el brazo forman una "tela". Esos huesos nunca son vecinos: se cortan.
    import bmesh
    names_by_index = {g.index: g.name for g in vg}

    def dominant(v):
        best = max(v.groups, key=lambda g: g.weight, default=None)
        return names_by_index.get(best.group) if best else None

    dom = [dominant(v) for v in mesh.data.vertices]
    forearm = {"LowerArm.L", "LowerArm.R", "Hand.L", "Hand.R"}
    body = {"Hips", "Spine", "UpperLeg.L", "UpperLeg.R", "LowerLeg.L", "LowerLeg.R"}
    # vertices del cuerpo con algo de peso de antebrazo/mano: ese peso pasa al hueso dominante (si no, se estiran)
    cleaned = 0
    for v in mesh.data.vertices:
        if dom[v.index] not in body:
            continue
        fw = [(g.group, g.weight) for g in v.groups if names_by_index.get(g.group) in forearm]
        if not fw:
            continue
        for gi, w in fw:
            vg[names_by_index[gi]].remove([v.index])
            vg[dom[v.index]].add([v.index], w, "ADD")
        cleaned += 1
    print(f"vertices del cuerpo sin influencia de antebrazo/mano: {cleaned}", flush=True)
    bm = bmesh.new()
    bm.from_mesh(mesh.data)
    bm.verts.ensure_lookup_table()
    upper = {"UpperArm.L", "UpperArm.R"}
    trunk = {"Spine", "Hips"}
    mw = mesh.matrix_world

    def armpit(f):
        # union brazo-torso por debajo del hombro: es la axila, que se estira como una tela al levantar el brazo
        if not (any(dom[v.index] in upper for v in f.verts) and any(dom[v.index] in trunk for v in f.verts)):
            return False
        return all((mw @ v.co).z < Z_SHOULDER - 0.1 for v in f.verts)

    cut = [f for f in bm.faces
           if (any(dom[v.index] in forearm for v in f.verts) and any(dom[v.index] in body for v in f.verts)) or armpit(f)]
    bmesh.ops.delete(bm, geom=cut, context="FACES")
    bm.to_mesh(mesh.data)
    bm.free()
    print(f"triangulos puente brazo-cuerpo eliminados: {len(cut)}", flush=True)

# material con la textura (en Unity se sustituye por uno propio)
mat = bpy.data.materials.new("ZombieGenerated_mat")
mat.use_nodes = True
mesh.data.materials.clear()
mesh.data.materials.append(mat)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=OUT_FBX, use_selection=True, object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=False,
    apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
    path_mode="COPY", embed_textures=False)
print("PROCESO_TERMINADO", flush=True)
