"""Genera armas low-poly (pistola y escopeta) para sujetar en la mano.

El origen esta en el agarre y el cañon apunta a -Y (Blender), que en Unity queda como +Z.
Uso: exec(open(ruta).read()); build_weapons(r"carpeta/de/salida")
"""
import bpy
import bmesh
from mathutils import Vector

METAL = (0.07, 0.07, 0.08)
WOOD = (0.28, 0.16, 0.07)

# (centro, tamano, material)
PISTOL = [
    ((0, -0.085, 0.045), (0.032, 0.20, 0.040), "metal"),   # corredera
    ((0, -0.060, 0.015), (0.030, 0.14, 0.026), "metal"),   # armazon
    ((0, 0.008, -0.040), (0.030, 0.050, 0.100), "metal"),  # empuñadura
    ((0, -0.030, -0.012), (0.012, 0.045, 0.022), "metal"),  # guardamonte
    ((0, -0.190, 0.040), (0.014, 0.025, 0.016), "metal"),  # boca del cañon
]
SHOTGUN = [
    ((0, 0.300, 0.000), (0.040, 0.320, 0.075), "wood"),    # culata
    ((0, 0.060, 0.020), (0.045, 0.240, 0.065), "metal"),   # caja
    ((0, -0.060, -0.040), (0.030, 0.050, 0.100), "wood"),  # empuñadura
    ((0, -0.420, 0.035), (0.026, 0.620, 0.026), "metal"),  # cañon
    ((0, -0.250, 0.002), (0.048, 0.170, 0.048), "wood"),   # bomba
]


def _mat(name, rgb):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    try:
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        if b:
            b.inputs["Base Color"].default_value = (*rgb, 1.0)
            b.inputs["Roughness"].default_value = 0.5
    except Exception:
        pass
    return m


def _clear():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.armatures, bpy.data.actions):
        for it in list(coll):
            coll.remove(it)


def _make(name, parts):
    mesh = bpy.data.meshes.new(name + "Mesh")
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    names = ["metal", "wood"]
    mesh.materials.append(_mat(name + "_metal", METAL))
    mesh.materials.append(_mat(name + "_wood", WOOD))
    bm = bmesh.new()
    for c, s, m in parts:
        res = bmesh.ops.create_cube(bm, size=1.0)
        vs = set(res["verts"])
        for v in res["verts"]:
            v.co = Vector((v.co.x * s[0] + c[0], v.co.y * s[1] + c[1], v.co.z * s[2] + c[2]))
        for f in bm.faces:
            if all(v in vs for v in f.verts):
                f.material_index = names.index(m)
    bm.to_mesh(mesh)
    bm.free()
    return obj


def _export(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        mesh_smooth_type="FACE", path_mode="COPY", bake_anim=False)


def build_weapons(out_dir):
    _clear()
    _export(_make("Pistol", PISTOL), out_dir + r"\Pistol.fbx")
    _clear()
    _export(_make("Shotgun", SHOTGUN), out_dir + r"\Shotgun.fbx")
    return "ok"
