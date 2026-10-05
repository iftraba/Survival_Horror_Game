"""Genera personajes low-poly (jugador y zombi) con esqueleto y animaciones en Blender.

Uso (desde el puente MCP o Blender):
    exec(open(r"<ruta>/build_characters.py").read())
    build("player", export_path=None)   # solo construir
    build("zombie", export_path=r"...Zombie.fbx")

Convenciones: Blender Z arriba, el personaje mira hacia -Y. Todos los huesos verticales
tienen ejes locales X=mundo X, Y=mundo Z, Z=mundo -Y, de modo que una rotacion +X
balancea la punta del hueso hacia delante (-Y).
"""
import math
import bpy
import bmesh
import mathutils
from mathutils import Vector

FPS = 30

# ---------------------------------------------------------------- esqueleto
def bone_defs():
    d = [
        ("Hips", None, (0, 0, 0.95), (0, 0, 1.05)),
        ("Spine", "Hips", (0, 0, 1.05), (0, 0, 1.35)),
        ("Neck", "Spine", (0, 0, 1.35), (0, 0, 1.50)),
        ("Head", "Neck", (0, 0, 1.50), (0, 0, 1.78)),
    ]
    for side, sx in (("L", 1), ("R", -1)):
        d += [
            ("UpperArm." + side, "Spine", (0.22 * sx, 0, 1.32), (0.22 * sx, 0, 1.05)),
            ("LowerArm." + side, "UpperArm." + side, (0.22 * sx, 0, 1.05), (0.22 * sx, 0, 0.80)),
            ("Hand." + side, "LowerArm." + side, (0.22 * sx, 0, 0.80), (0.22 * sx, 0, 0.66)),
            ("UpperLeg." + side, "Hips", (0.10 * sx, 0, 0.95), (0.10 * sx, 0, 0.52)),
            ("LowerLeg." + side, "UpperLeg." + side, (0.10 * sx, 0, 0.52), (0.10 * sx, 0, 0.10)),
            ("Foot." + side, "LowerLeg." + side, (0.10 * sx, 0, 0.10), (0.10 * sx, -0.22, 0.05)),
        ]
    return d


# (nombre, centro, tamano, hueso, material)  material: skin / shirt / pants / boots
def part_defs():
    p = [
        ("Pelvis", (0, 0, 1.0), (0.34, 0.20, 0.20), "Hips", "pants"),
        ("Torso", (0, 0, 1.20), (0.38, 0.22, 0.34), "Spine", "shirt"),
        ("Neck", (0, 0, 1.43), (0.09, 0.09, 0.12), "Neck", "skin"),
        ("Head", (0, 0, 1.64), (0.22, 0.24, 0.26), "Head", "skin"),
    ]
    for side, sx in (("L", 1), ("R", -1)):
        p += [
            ("UpperArm." + side, (0.22 * sx, 0, 1.18), (0.10, 0.10, 0.27), "UpperArm." + side, "shirt"),
            ("LowerArm." + side, (0.22 * sx, 0, 0.93), (0.085, 0.085, 0.25), "LowerArm." + side, "skin"),
            ("Hand." + side, (0.22 * sx, 0, 0.73), (0.09, 0.09, 0.12), "Hand." + side, "skin"),
            ("UpperLeg." + side, (0.10 * sx, 0, 0.74), (0.15, 0.15, 0.43), "UpperLeg." + side, "pants"),
            ("LowerLeg." + side, (0.10 * sx, 0, 0.31), (0.13, 0.13, 0.42), "LowerLeg." + side, "pants"),
            ("Foot." + side, (0.10 * sx, -0.05, 0.05), (0.13, 0.28, 0.10), "Foot." + side, "boots"),
        ]
    return p


PALETTES = {
    "player": {"skin": (0.80, 0.60, 0.48), "shirt": (0.12, 0.22, 0.45), "pants": (0.15, 0.15, 0.17), "boots": (0.06, 0.05, 0.05)},
    "zombie": {"skin": (0.38, 0.52, 0.34), "shirt": (0.22, 0.18, 0.16), "pants": (0.14, 0.13, 0.12), "boots": (0.05, 0.04, 0.04)},
}


# ---------------------------------------------------------------- escena
def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.actions, bpy.data.materials):
        for item in list(coll):
            coll.remove(item)


def make_material(name, rgb):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    try:
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
            bsdf.inputs["Roughness"].default_value = 0.85
    except Exception:
        pass
    return m


def make_armature():
    data = bpy.data.armatures.new("Armature")
    arm = bpy.data.objects.new("Armature", data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for name, parent, head, tail in bone_defs():
        eb = data.edit_bones.new(name)
        eb.head = Vector(head)
        eb.tail = Vector(tail)
        if parent:
            eb.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def make_body(kind, arm):
    pal = PALETTES[kind]
    mat_names = ["skin", "shirt", "pants", "boots"]
    mesh = bpy.data.meshes.new(kind.capitalize() + "Mesh")
    obj = bpy.data.objects.new(kind.capitalize(), mesh)
    bpy.context.scene.collection.objects.link(obj)
    for n in mat_names:
        mesh.materials.append(make_material(f"{kind}_{n}", pal[n]))
    group_idx = {}
    for name, *_ in bone_defs():
        group_idx[name] = obj.vertex_groups.new(name=name).index

    bm = bmesh.new()
    dl = bm.verts.layers.deform.verify()
    for _name, c, s, bone, mat in part_defs():
        res = bmesh.ops.create_cube(bm, size=1.0)
        verts = res["verts"]
        vset = set(verts)
        for v in verts:
            v.co = Vector((v.co.x * s[0] + c[0], v.co.y * s[1] + c[1], v.co.z * s[2] + c[2]))
            v[dl][group_idx[bone]] = 1.0
        for f in bm.faces:
            if all(v in vset for v in f.verts):
                f.material_index = mat_names.index(mat)
    bm.to_mesh(mesh)
    bm.free()

    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm
    return obj


# ---------------------------------------------------------------- malla detallada
# Cuerpos con secciones elipticas (lofts) y elipsoides en vez de cajas. Los pesos se reparten entre
# huesos vecinos en las articulaciones para que codos, rodillas y cintura se doblen suavemente.
# Mismo esqueleto que la version de bloques: las animaciones y el soporte del arma siguen valiendo.

DETAIL_COLORS = {  # color aproximado en Blender; en Unity se sobrescribe con materiales propios
    "skin": (0.80, 0.60, 0.48), "shirt": (0.14, 0.24, 0.42), "vest": (0.10, 0.12, 0.16),
    "pants": (0.17, 0.17, 0.19), "boots": (0.07, 0.06, 0.06), "belt": (0.06, 0.05, 0.04),
    "gold": (0.70, 0.55, 0.20), "hair": (0.14, 0.09, 0.06), "eye": (0.90, 0.90, 0.90),
    "pupil": (0.03, 0.03, 0.04), "mouth": (0.35, 0.12, 0.12), "teeth": (0.75, 0.70, 0.55),
    "blood": (0.35, 0.02, 0.02), "bone": (0.80, 0.76, 0.65),
}
DETAIL_MATS = list(DETAIL_COLORS.keys())


def _mat_index(name):
    return DETAIL_MATS.index(name)


def _loft(ctx, sections, mat, segs=10, smooth=True):
    """sections: [((x, y, z), rx, ry, {hueso: peso})] ordenadas en Z."""
    bm, dl, gi = ctx
    rings = []
    for center, rx, ry, w in sections:
        ring = []
        for j in range(segs):
            a = 2.0 * math.pi * j / segs
            v = bm.verts.new(Vector((center[0] + rx * math.cos(a), center[1] + ry * math.sin(a), center[2])))
            for bone, wt in w.items():
                v[dl][gi[bone]] = wt
            ring.append(v)
        rings.append(ring)
    faces = []
    for i in range(len(rings) - 1):
        for j in range(segs):
            j2 = (j + 1) % segs
            faces.append(bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j])))
    faces.append(bm.faces.new(rings[0]))
    faces.append(bm.faces.new(list(reversed(rings[-1]))))
    for f in faces:
        f.material_index = _mat_index(mat)
        f.smooth = smooth


def _ellipsoid(ctx, center, radii, mat, w, segs=10, rings=6):
    secs = []
    for i in range(rings):
        t = math.pi * (i + 0.5) / rings
        secs.append(((center[0], center[1], center[2] + radii[2] * math.cos(t)),
                     radii[0] * math.sin(t), radii[1] * math.sin(t), w))
    _loft(ctx, secs, mat, segs)


def _box(ctx, center, size, mat, w, rot=(0, 0, 0)):
    bm, dl, gi = ctx
    res = bmesh.ops.create_cube(bm, size=1.0)
    verts = res["verts"]
    vset = set(verts)
    rm = mathutils.Euler([math.radians(a) for a in rot]).to_matrix()
    for v in verts:
        co = rm @ Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
        v.co = co + Vector(center)
        for bone, wt in w.items():
            v[dl][gi[bone]] = wt
    for f in bm.faces:
        if all(v in vset for v in f.verts):
            f.material_index = _mat_index(mat)
            f.smooth = False


def _build_figure(ctx, zombie):
    P = (lambda **k: k)  # azucar para escribir pesos
    for side, sx in (("L", 1), ("R", -1)):
        UL, LL, FT = "UpperLeg." + side, "LowerLeg." + side, "Foot." + side
        UA, LA, HD = "UpperArm." + side, "LowerArm." + side, "Hand." + side
        x = 0.10 * sx
        ax = 0.22 * sx

        # --- pierna: pantalon, bota y pie
        _loft(ctx, [
            ((x, 0, 0.98), 0.092, 0.095, {"Hips": 0.5, UL: 0.5}),
            ((x, 0, 0.78), 0.080, 0.085, {UL: 1.0}),
            ((x, 0, 0.55), 0.063, 0.067, {UL: 0.5, LL: 0.5}),
            ((x, 0, 0.32), 0.066, 0.074, {LL: 1.0}),
            ((x, 0, 0.17), 0.050, 0.054, {LL: 1.0}),
        ], "pants")
        _loft(ctx, [
            ((x, 0, 0.22), 0.060, 0.064, {LL: 1.0}),
            ((x, 0, 0.10), 0.056, 0.060, {LL: 0.6, FT: 0.4}),
        ], "boots")
        _ellipsoid(ctx, (x, 0.015, 0.065), (0.053, 0.065, 0.058), "boots", {FT: 1.0})
        _ellipsoid(ctx, (x, -0.095, 0.045), (0.050, 0.110, 0.042), "boots", {FT: 1.0})
        _box(ctx, (x, -0.05, 0.008), (0.104, 0.27, 0.018), "belt", {FT: 1.0})

        # --- brazo: manga, antebrazo y mano
        left_sleeve_torn = zombie and side == "L"
        _loft(ctx, [
            ((ax, 0, 1.33), 0.064, 0.064, {"Spine": 0.6, UA: 0.4}),
            ((ax, 0, 1.20), 0.054, 0.054, {UA: 1.0}),
            ((ax, 0, 1.07), 0.049, 0.049, {UA: 0.5, LA: 0.5}),
        ], "skin" if left_sleeve_torn else "shirt")
        _loft(ctx, [
            ((ax, 0, 1.07), 0.043, 0.043, {UA: 0.5, LA: 0.5}),
            ((ax, 0, 0.95), 0.043, 0.043, {LA: 1.0}),
            ((ax, 0, 0.84), 0.034, 0.034, {LA: 0.6, HD: 0.4}),
        ], "skin")
        _ellipsoid(ctx, (ax, 0, 0.735), (0.042, 0.030, 0.070), "skin", {HD: 1.0})
        _ellipsoid(ctx, (ax - 0.036 * sx, -0.02, 0.755), (0.014, 0.014, 0.030), "skin", {HD: 1.0})  # pulgar

    # --- cadera, cinturon y torso
    _loft(ctx, [((0, 0, 0.93), 0.172, 0.108, {"Hips": 1.0}), ((0, 0, 1.04), 0.166, 0.106, {"Hips": 1.0})], "pants")
    _loft(ctx, [((0, 0, 1.03), 0.172, 0.110, {"Hips": 0.5, "Spine": 0.5}), ((0, 0, 1.075), 0.170, 0.109, {"Hips": 0.5, "Spine": 0.5})], "belt")
    _box(ctx, (0, -0.113, 1.052), (0.04, 0.014, 0.034), "gold", {"Hips": 0.5, "Spine": 0.5})

    torso = [
        (1.07, 0.160, 0.103, {"Hips": 0.5, "Spine": 0.5}),
        (1.16, 0.150, 0.096, {"Spine": 1.0}),
        (1.28, 0.190, 0.114, {"Spine": 1.0}),
        (1.37, 0.208, 0.108, {"Spine": 0.85, "Neck": 0.15}),
        (1.41, 0.130, 0.082, {"Neck": 0.7, "Spine": 0.3}),
        (1.44, 0.060, 0.060, {"Neck": 1.0}),
    ]
    secs = lambda lo, hi: [((0, 0, z), rx, ry, w) for z, rx, ry, w in torso if lo <= z <= hi]
    if zombie:
        _loft(ctx, secs(1.07, 1.16), "skin")          # camisa rota: barriga al aire
        _loft(ctx, secs(1.16, 1.45), "shirt")
        # jirones colgando del bajo de la camisa
        for i, (a, ln) in enumerate([(-0.13, 0.10), (-0.07, 0.15), (0.0, 0.08), (0.06, 0.17), (0.12, 0.11), (0.17, 0.07)]):
            _box(ctx, (a, -0.096 if i % 2 else 0.094, 1.16 - ln / 2), (0.04, 0.012, ln), "shirt", {"Spine": 1.0}, rot=(0, 0, (i - 2) * 4))
        # costillas a la vista y herida abierta en el pecho
        _ellipsoid(ctx, (-0.075, -0.108, 1.27), (0.065, 0.014, 0.06), "blood", {"Spine": 1.0}, segs=8, rings=5)
        for k, z in enumerate((1.235, 1.265, 1.295)):
            _ellipsoid(ctx, (-0.075, -0.117, z), (0.052, 0.008, 0.007), "bone", {"Spine": 1.0}, segs=8, rings=4)
        _ellipsoid(ctx, (0.09, -0.112, 1.20), (0.045, 0.012, 0.04), "blood", {"Spine": 1.0}, segs=8, rings=5)
        _ellipsoid(ctx, (-0.11, -0.095, 1.12), (0.05, 0.012, 0.035), "blood", {"Spine": 1.0}, segs=8, rings=5)
    else:
        _loft(ctx, secs(1.07, 1.45), "shirt")
        # chaleco tactico, collarin, pistolera y cartucheras
        _loft(ctx, [((0, 0, 1.17), 0.162, 0.108, {"Spine": 1.0}), ((0, 0, 1.28), 0.202, 0.128, {"Spine": 1.0}),
                    ((0, 0, 1.37), 0.220, 0.121, {"Spine": 0.85, "Neck": 0.15}), ((0, 0, 1.40), 0.170, 0.100, {"Neck": 0.6, "Spine": 0.4})], "vest")
        _loft(ctx, [((0, 0, 1.41), 0.085, 0.080, {"Neck": 1.0}), ((0, 0, 1.46), 0.062, 0.062, {"Neck": 1.0})], "vest")
        _box(ctx, (0, -0.126, 1.24), (0.14, 0.03, 0.08), "vest", {"Spine": 1.0})          # bolsillo frontal
        _box(ctx, (-0.18, 0.0, 0.84), (0.045, 0.09, 0.14), "belt", {"UpperLeg.R": 1.0})   # pistolera
        _box(ctx, (0.185, 0.0, 1.00), (0.05, 0.08, 0.08), "belt", {"Hips": 1.0})          # cartuchera

    # --- cuello y cabeza
    _loft(ctx, [((0, 0, 1.42), 0.064, 0.064, {"Neck": 1.0}), ((0, 0, 1.53), 0.058, 0.060, {"Neck": 0.5, "Head": 0.5})], "skin")
    _ellipsoid(ctx, (0, 0, 1.645), (0.095, 0.108, 0.125), "skin", {"Head": 1.0}, segs=12, rings=8)
    for sx in (1, -1):
        _ellipsoid(ctx, (0.098 * sx, 0.0, 1.635), (0.014, 0.025, 0.035), "skin", {"Head": 1.0}, segs=8, rings=5)  # orejas
    _ellipsoid(ctx, (0, -0.108, 1.628), (0.016, 0.020, 0.025), "skin", {"Head": 1.0}, segs=8, rings=5)           # nariz

    if zombie:
        for sx in (1, -1):
            big = 0.024 if sx > 0 else 0.019
            _ellipsoid(ctx, (0.038 * sx, -0.092, 1.665), (big, 0.012, big * 0.85), "eye", {"Head": 1.0}, segs=8, rings=5)
            _ellipsoid(ctx, (0.038 * sx, -0.102, 1.665), (0.008, 0.005, 0.008), "blood", {"Head": 1.0}, segs=6, rings=4)
        _ellipsoid(ctx, (0, -0.098, 1.588), (0.044, 0.016, 0.034), "mouth", {"Head": 1.0}, segs=10, rings=6)    # boca abierta
        for k in range(5):                                                                                          # dientes
            _box(ctx, (-0.032 + k * 0.016, -0.108, 1.612), (0.009, 0.008, 0.016), "teeth", {"Head": 1.0})
        for k in range(4):
            _box(ctx, (-0.024 + k * 0.016, -0.108, 1.566), (0.009, 0.008, 0.013), "teeth", {"Head": 1.0})
        _ellipsoid(ctx, (0.0, -0.104, 1.545), (0.05, 0.014, 0.036), "blood", {"Head": 1.0}, segs=8, rings=5)    # sangre en la barbilla
        _ellipsoid(ctx, (0.05, -0.098, 1.70), (0.032, 0.012, 0.042), "blood", {"Head": 1.0}, segs=8, rings=5)    # herida en la frente
        for c, r in (((0.045, 0.045, 1.745), (0.045, 0.05, 0.028)), ((-0.05, 0.03, 1.735), (0.04, 0.05, 0.026)), ((0.0, 0.07, 1.70), (0.05, 0.04, 0.05))):
            _ellipsoid(ctx, c, r, "hair", {"Head": 1.0}, segs=8, rings=5)                                          # pelo ralo
        # manos y antebrazos manchados, rodilla rota
        for ax in (0.22, -0.22):
            _ellipsoid(ctx, (ax, -0.018, 0.90), (0.046, 0.022, 0.06), "blood", {"LowerArm." + ("L" if ax > 0 else "R"): 1.0}, segs=8, rings=5)
        _ellipsoid(ctx, (0.10, -0.062, 0.54), (0.042, 0.022, 0.05), "skin", {"UpperLeg.L": 0.5, "LowerLeg.L": 0.5}, segs=8, rings=5)
        _ellipsoid(ctx, (0.10, -0.068, 0.54), (0.026, 0.012, 0.03), "blood", {"UpperLeg.L": 0.5, "LowerLeg.L": 0.5}, segs=8, rings=4)
    else:
        for sx in (1, -1):
            _ellipsoid(ctx, (0.038 * sx, -0.093, 1.665), (0.020, 0.012, 0.016), "eye", {"Head": 1.0}, segs=8, rings=5)
            _ellipsoid(ctx, (0.038 * sx, -0.103, 1.665), (0.009, 0.006, 0.009), "pupil", {"Head": 1.0}, segs=6, rings=4)
            _box(ctx, (0.040 * sx, -0.098, 1.693), (0.046, 0.010, 0.008), "hair", {"Head": 1.0}, rot=(0, 0, -6 * sx))  # cejas
        _box(ctx, (0, -0.101, 1.592), (0.05, 0.008, 0.008), "mouth", {"Head": 1.0})
        _ellipsoid(ctx, (0, 0.014, 1.688), (0.100, 0.112, 0.098), "hair", {"Head": 1.0}, segs=12, rings=7)         # pelo
        _ellipsoid(ctx, (0, -0.066, 1.742), (0.082, 0.040, 0.030), "hair", {"Head": 1.0}, segs=10, rings=5)         # flequillo


# ---------------------------------------------------------------- variantes de zombi
# Cada variante cambia la ropa (malla), el peinado y la paleta. Mismo esqueleto => mismas animaciones.
ZOMBIE_OUTFITS = ["tshirt", "cop", "gown", "suit", "overalls", "bare"]
DETAIL_COLORS.update({"gown": (0.70, 0.80, 0.82), "jacket": (0.10, 0.10, 0.12), "tie": (0.50, 0.05, 0.05),
                      "overall": (0.80, 0.40, 0.10), "hat": (0.15, 0.20, 0.40)})
DETAIL_MATS[:] = list(DETAIL_COLORS.keys())


def _blood(ctx, c, r, bone="Spine", w=None):
    _ellipsoid(ctx, c, r, "blood", w or {bone: 1.0}, segs=8, rings=5)


def _legs(ctx, pant="pants", shoes="boots", barefoot=False, knee_tear=False):
    for side, sx in (("L", 1), ("R", -1)):
        UL, LL, FT = "UpperLeg." + side, "LowerLeg." + side, "Foot." + side
        x = 0.10 * sx
        _loft(ctx, [
            ((x, 0, 0.98), 0.092, 0.095, {"Hips": 0.5, UL: 0.5}),
            ((x, 0, 0.78), 0.080, 0.085, {UL: 1.0}),
            ((x, 0, 0.55), 0.063, 0.067, {UL: 0.5, LL: 0.5}),
            ((x, 0, 0.32), 0.066, 0.074, {LL: 1.0}),
            ((x, 0, 0.17), 0.050, 0.054, {LL: 1.0}),
        ], pant)
        if barefoot:
            _loft(ctx, [((x, 0, 0.18), 0.048, 0.052, {LL: 1.0}), ((x, 0, 0.10), 0.046, 0.050, {LL: 0.6, FT: 0.4})], "skin")
            _ellipsoid(ctx, (x, 0.015, 0.060), (0.047, 0.060, 0.052), "skin", {FT: 1.0})
            _ellipsoid(ctx, (x, -0.085, 0.032), (0.046, 0.100, 0.030), "skin", {FT: 1.0})
        else:
            _loft(ctx, [((x, 0, 0.22), 0.060, 0.064, {LL: 1.0}), ((x, 0, 0.10), 0.056, 0.060, {LL: 0.6, FT: 0.4})], shoes)
            _ellipsoid(ctx, (x, 0.015, 0.065), (0.053, 0.065, 0.058), shoes, {FT: 1.0})
            _ellipsoid(ctx, (x, -0.095, 0.045), (0.050, 0.110, 0.042), shoes, {FT: 1.0})
            _box(ctx, (x, -0.05, 0.008), (0.104, 0.27, 0.018), "belt", {FT: 1.0})
        if knee_tear and side == "L":
            _ellipsoid(ctx, (0.10, -0.062, 0.54), (0.042, 0.022, 0.05), "skin", {UL: 0.5, LL: 0.5}, segs=8, rings=5)
            _blood(ctx, (0.10, -0.068, 0.54), (0.026, 0.012, 0.03), w={UL: 0.5, LL: 0.5})


def _arms(ctx, upper, lower, torn_left=False, cuff=None):
    for side, sx in (("L", 1), ("R", -1)):
        UA, LA, HD = "UpperArm." + side, "LowerArm." + side, "Hand." + side
        ax = 0.22 * sx
        _loft(ctx, [
            ((ax, 0, 1.33), 0.064, 0.064, {"Spine": 0.6, UA: 0.4}),
            ((ax, 0, 1.20), 0.054, 0.054, {UA: 1.0}),
            ((ax, 0, 1.07), 0.049, 0.049, {UA: 0.5, LA: 0.5}),
        ], "skin" if (torn_left and side == "L") else upper)
        _loft(ctx, [
            ((ax, 0, 1.07), 0.044 if lower != "skin" else 0.043, 0.044 if lower != "skin" else 0.043, {UA: 0.5, LA: 0.5}),
            ((ax, 0, 0.95), 0.043, 0.043, {LA: 1.0}),
            ((ax, 0, 0.84), 0.034, 0.034, {LA: 0.6, HD: 0.4}),
        ], "skin" if (torn_left and side == "L") else lower)
        if cuff:
            _loft(ctx, [((ax, 0, 0.88), 0.040, 0.040, {LA: 1.0}), ((ax, 0, 0.82), 0.038, 0.038, {LA: 0.7, HD: 0.3})], cuff)
        _ellipsoid(ctx, (ax, 0, 0.735), (0.042, 0.030, 0.070), "skin", {HD: 1.0})
        _ellipsoid(ctx, (ax - 0.036 * sx, -0.02, 0.755), (0.014, 0.014, 0.030), "skin", {HD: 1.0})
        _blood(ctx, (ax, -0.018, 0.90), (0.046, 0.022, 0.06), w={LA: 1.0})            # antebrazo manchado


def _zombie_head(ctx, hair="sparse"):
    _loft(ctx, [((0, 0, 1.42), 0.064, 0.064, {"Neck": 1.0}), ((0, 0, 1.53), 0.058, 0.060, {"Neck": 0.5, "Head": 0.5})], "skin")
    _ellipsoid(ctx, (0, 0, 1.645), (0.095, 0.108, 0.125), "skin", {"Head": 1.0}, segs=12, rings=8)
    for sx in (1, -1):
        _ellipsoid(ctx, (0.098 * sx, 0.0, 1.635), (0.014, 0.025, 0.035), "skin", {"Head": 1.0}, segs=8, rings=5)
        big = 0.024 if sx > 0 else 0.019
        _ellipsoid(ctx, (0.038 * sx, -0.092, 1.665), (big, 0.012, big * 0.85), "eye", {"Head": 1.0}, segs=8, rings=5)
        _ellipsoid(ctx, (0.038 * sx, -0.102, 1.665), (0.008, 0.005, 0.008), "blood", {"Head": 1.0}, segs=6, rings=4)
    _ellipsoid(ctx, (0, -0.108, 1.628), (0.016, 0.020, 0.025), "skin", {"Head": 1.0}, segs=8, rings=5)
    _ellipsoid(ctx, (0, -0.098, 1.588), (0.044, 0.016, 0.034), "mouth", {"Head": 1.0}, segs=10, rings=6)
    for k in range(5):
        _box(ctx, (-0.032 + k * 0.016, -0.108, 1.612), (0.009, 0.008, 0.016), "teeth", {"Head": 1.0})
    for k in range(4):
        _box(ctx, (-0.024 + k * 0.016, -0.108, 1.566), (0.009, 0.008, 0.013), "teeth", {"Head": 1.0})
    _blood(ctx, (0.0, -0.104, 1.545), (0.05, 0.014, 0.036), "Head")
    _blood(ctx, (0.05, -0.098, 1.70), (0.032, 0.012, 0.042), "Head")

    if hair == "sparse":
        for c, r in (((0.045, 0.045, 1.745), (0.045, 0.05, 0.028)), ((-0.05, 0.03, 1.735), (0.04, 0.05, 0.026)), ((0.0, 0.07, 1.70), (0.05, 0.04, 0.05))):
            _ellipsoid(ctx, c, r, "hair", {"Head": 1.0}, segs=8, rings=5)
    elif hair == "short":
        _ellipsoid(ctx, (0, 0.014, 1.688), (0.100, 0.112, 0.098), "hair", {"Head": 1.0}, segs=12, rings=7)
        _ellipsoid(ctx, (0, -0.066, 1.742), (0.082, 0.040, 0.030), "hair", {"Head": 1.0}, segs=10, rings=5)
    elif hair == "long":
        _ellipsoid(ctx, (0, 0.016, 1.69), (0.102, 0.114, 0.100), "hair", {"Head": 1.0}, segs=12, rings=7)
        _ellipsoid(ctx, (0, 0.075, 1.56), (0.100, 0.060, 0.185), "hair", {"Head": 0.75, "Neck": 0.25}, segs=10, rings=7)
        for sx in (1, -1):   # mechones a los lados de la cara, enredados
            _ellipsoid(ctx, (0.085 * sx, -0.03, 1.58), (0.022, 0.03, 0.12), "hair", {"Head": 1.0}, segs=8, rings=5)
    elif hair == "comb":   # calvo con unos mechones pegados al lado
        _ellipsoid(ctx, (0.06, 0.02, 1.715), (0.04, 0.07, 0.05), "hair", {"Head": 1.0}, segs=8, rings=5)
        _ellipsoid(ctx, (-0.09, 0.02, 1.64), (0.015, 0.06, 0.06), "hair", {"Head": 1.0}, segs=8, rings=5)
    elif hair == "cap":    # gorra
        _ellipsoid(ctx, (0, 0.01, 1.715), (0.108, 0.118, 0.075), "hat", {"Head": 1.0}, segs=12, rings=6)
        _box(ctx, (0, -0.125, 1.705), (0.15, 0.09, 0.012), "hat", {"Head": 1.0}, rot=(-8, 0, 0))
        _ellipsoid(ctx, (-0.06, 0.07, 1.63), (0.03, 0.04, 0.05), "hair", {"Head": 1.0}, segs=8, rings=5)
    # "bald": sin pelo


def _chest_wound(ctx, x=-0.075):
    _blood(ctx, (x, -0.108, 1.27), (0.065, 0.014, 0.06))
    for z in (1.235, 1.265, 1.295):
        _ellipsoid(ctx, (x, -0.117, z), (0.052, 0.008, 0.007), "bone", {"Spine": 1.0}, segs=8, rings=4)


def _build_variant(ctx, outfit):
    shirt_sections = [
        (1.07, 0.160, 0.103, {"Hips": 0.5, "Spine": 0.5}), (1.16, 0.150, 0.096, {"Spine": 1.0}),
        (1.28, 0.190, 0.114, {"Spine": 1.0}), (1.37, 0.208, 0.108, {"Spine": 0.85, "Neck": 0.15}),
        (1.41, 0.130, 0.082, {"Neck": 0.7, "Spine": 0.3}), (1.44, 0.060, 0.060, {"Neck": 1.0})]
    S = lambda lo, hi: [((0, 0, z), rx, ry, w) for z, rx, ry, w in shirt_sections if lo <= z <= hi]
    pelvis = lambda m: _loft(ctx, [((0, 0, 0.93), 0.172, 0.108, {"Hips": 1.0}), ((0, 0, 1.04), 0.166, 0.106, {"Hips": 1.0})], m)

    if outfit == "cop":
        _legs(ctx, "pants", "boots", knee_tear=True)
        _arms(ctx, "shirt", "skin", torn_left=False)
        pelvis("pants")
        _loft(ctx, [((0, 0, 1.03), 0.172, 0.110, {"Hips": 0.5, "Spine": 0.5}), ((0, 0, 1.075), 0.170, 0.109, {"Hips": 0.5, "Spine": 0.5})], "belt")
        _box(ctx, (0, -0.113, 1.052), (0.04, 0.014, 0.034), "gold", {"Hips": 0.5, "Spine": 0.5})
        _loft(ctx, S(1.07, 1.45), "shirt")
        _loft(ctx, [((0, 0, 1.17), 0.162, 0.108, {"Spine": 1.0}), ((0, 0, 1.28), 0.202, 0.128, {"Spine": 1.0}),
                    ((0, 0, 1.37), 0.220, 0.121, {"Spine": 0.85, "Neck": 0.15}), ((0, 0, 1.40), 0.170, 0.100, {"Neck": 0.6, "Spine": 0.4})], "vest")
        _box(ctx, (0, -0.126, 1.24), (0.14, 0.03, 0.08), "vest", {"Spine": 1.0})
        _box(ctx, (-0.18, 0.0, 0.84), (0.045, 0.09, 0.14), "belt", {"UpperLeg.R": 1.0})
        _box(ctx, (0.185, 0.0, 1.00), (0.05, 0.08, 0.08), "belt", {"Hips": 1.0})
        _blood(ctx, (0.10, -0.130, 1.33), (0.05, 0.012, 0.05))
        _blood(ctx, (-0.09, -0.130, 1.20), (0.06, 0.012, 0.04))
        _zombie_head(ctx, "short")

    elif outfit == "gown":
        _legs(ctx, "skin", barefoot=True)
        _arms(ctx, "skin", "skin")
        # bata de hospital: tubo abierto desde los hombros hasta por encima de la rodilla
        _loft(ctx, [
            ((0, 0, 1.40), 0.200, 0.105, {"Spine": 0.8, "Neck": 0.2}), ((0, 0, 1.28), 0.188, 0.118, {"Spine": 1.0}),
            ((0, 0, 1.15), 0.165, 0.112, {"Spine": 0.6, "Hips": 0.4}), ((0, 0, 1.00), 0.185, 0.140, {"Hips": 1.0}),
            ((0, 0, 0.85), 0.205, 0.165, {"Hips": 1.0}), ((0, 0, 0.70), 0.215, 0.172, {"Hips": 1.0})], "gown")
        _loft(ctx, [((0, 0, 1.41), 0.085, 0.075, {"Neck": 1.0}), ((0, 0, 1.46), 0.060, 0.060, {"Neck": 1.0})], "skin")
        for sx in (1, -1):   # tirantes
            _box(ctx, (0.14 * sx, 0.0, 1.40), (0.05, 0.12, 0.03), "gown", {"Spine": 1.0})
        _blood(ctx, (0.04, -0.125, 1.18), (0.07, 0.012, 0.09))
        _blood(ctx, (-0.10, -0.150, 0.82), (0.05, 0.012, 0.10), w={"Hips": 1.0})
        _blood(ctx, (0.10, -0.165, 0.74), (0.04, 0.012, 0.05), w={"Hips": 1.0})
        _zombie_head(ctx, "long")

    elif outfit == "suit":
        _legs(ctx, "pants", "boots", knee_tear=False)
        _arms(ctx, "jacket", "jacket", cuff="shirt")
        pelvis("pants")
        _loft(ctx, [((0, 0, 1.03), 0.172, 0.110, {"Hips": 0.5, "Spine": 0.5}), ((0, 0, 1.075), 0.170, 0.109, {"Hips": 0.5, "Spine": 0.5})], "belt")
        _loft(ctx, S(1.07, 1.45), "shirt")
        _loft(ctx, [  # chaqueta, mas larga que la camisa
            ((0, 0, 0.88), 0.176, 0.114, {"Hips": 1.0}), ((0, 0, 1.02), 0.178, 0.113, {"Hips": 0.6, "Spine": 0.4}),
            ((0, 0, 1.16), 0.158, 0.103, {"Spine": 1.0}), ((0, 0, 1.28), 0.198, 0.122, {"Spine": 1.0}),
            ((0, 0, 1.37), 0.214, 0.114, {"Spine": 0.85, "Neck": 0.15}), ((0, 0, 1.41), 0.140, 0.090, {"Neck": 0.7, "Spine": 0.3})], "jacket")
        _box(ctx, (0, -0.118, 1.30), (0.075, 0.012, 0.20), "shirt", {"Spine": 1.0})           # camisa visible en V
        _box(ctx, (0, -0.126, 1.25), (0.03, 0.012, 0.18), "tie", {"Spine": 1.0})              # corbata
        _box(ctx, (0, -0.126, 1.355), (0.04, 0.014, 0.03), "tie", {"Spine": 1.0})             # nudo
        for sx in (1, -1):   # solapas
            _box(ctx, (0.055 * sx, -0.122, 1.30), (0.03, 0.012, 0.19), "jacket", {"Spine": 1.0}, rot=(0, 0, -10 * sx))
        _blood(ctx, (0.08, -0.128, 1.12), (0.07, 0.012, 0.07))
        _blood(ctx, (-0.12, -0.12, 1.30), (0.05, 0.012, 0.07))
        _zombie_head(ctx, "comb")

    elif outfit == "overalls":
        _legs(ctx, "overall", "boots", knee_tear=True)
        _arms(ctx, "overall", "skin", torn_left=True)
        pelvis("overall")
        _loft(ctx, S(1.07, 1.45)[:5], "overall")
        _loft(ctx, [((0, 0, 1.40), 0.150, 0.100, {"Spine": 1.0}), ((0, 0, 1.44), 0.062, 0.062, {"Neck": 1.0})], "shirt")
        _box(ctx, (0, -0.126, 1.28), (0.12, 0.014, 0.10), "overall", {"Spine": 1.0})          # peto
        _box(ctx, (0.075, -0.134, 1.30), (0.045, 0.012, 0.045), "belt", {"Spine": 1.0})       # bolsillo
        _loft(ctx, [((0, 0, 1.03), 0.172, 0.110, {"Hips": 0.5, "Spine": 0.5}), ((0, 0, 1.075), 0.170, 0.109, {"Hips": 0.5, "Spine": 0.5})], "belt")
        for sx in (1, -1):
            _box(ctx, (0.085 * sx, -0.10, 1.36), (0.03, 0.02, 0.18), "overall", {"Spine": 1.0})   # tirantes
        _blood(ctx, (-0.07, -0.130, 1.18), (0.06, 0.012, 0.06))
        _ellipsoid(ctx, (0.12, -0.106, 0.88), (0.05, 0.012, 0.05), "belt", {"Hips": 0.5, "UpperLeg.L": 0.5}, segs=8, rings=5)  # grasa
        _zombie_head(ctx, "cap")

    elif outfit == "bare":
        _legs(ctx, "pants", "boots", knee_tear=True)
        _arms(ctx, "skin", "skin")
        pelvis("pants")
        _loft(ctx, [((0, 0, 1.03), 0.172, 0.110, {"Hips": 0.5, "Spine": 0.5}), ((0, 0, 1.075), 0.170, 0.109, {"Hips": 0.5, "Spine": 0.5})], "belt")
        _loft(ctx, S(1.07, 1.45), "skin")
        _chest_wound(ctx, -0.075)
        _blood(ctx, (0.09, -0.112, 1.18), (0.05, 0.012, 0.05))
        _blood(ctx, (-0.10, -0.098, 1.10), (0.06, 0.012, 0.04))
        _blood(ctx, (0.16, -0.04, 1.30), (0.03, 0.06, 0.05), w={"Spine": 1.0})                # mordisco en el hombro
        _zombie_head(ctx, "bald")


def make_body_detailed(kind, arm, outfit=None):
    variant = kind == "zombie" and outfit not in (None, "tshirt")
    # La variante base conserva los nombres de siempre (zombie_*); las demas usan z<traje>_*
    prefix = ("z" + outfit) if variant else kind
    mesh = bpy.data.meshes.new(kind.capitalize() + "Mesh")
    obj = bpy.data.objects.new(kind.capitalize() + (outfit.capitalize() if variant else ""), mesh)
    bpy.context.scene.collection.objects.link(obj)
    gi = {}
    for name, *_ in bone_defs():
        gi[name] = obj.vertex_groups.new(name=name).index

    bm = bmesh.new()
    dl = bm.verts.layers.deform.verify()
    if variant:
        _build_variant((bm, dl, gi), outfit)
    else:
        _build_figure((bm, dl, gi), zombie=(kind == "zombie"))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

    # Solo se crean los materiales que realmente se usan
    used = sorted({f.material_index for f in bm.faces})
    remap = {old: new for new, old in enumerate(used)}
    for f in bm.faces:
        f.material_index = remap[f.material_index]
    bm.to_mesh(mesh)
    bm.free()
    colors = dict(DETAIL_COLORS)
    if kind == "zombie":
        colors.update({"skin": (0.46, 0.52, 0.42), "shirt": (0.30, 0.27, 0.22), "pants": (0.14, 0.13, 0.14),
                       "hair": (0.12, 0.11, 0.10), "eye": (0.85, 0.80, 0.50), "mouth": (0.12, 0.02, 0.02)})
    for old in used:
        n = DETAIL_MATS[old]
        mesh.materials.append(make_material(f"{prefix}_{n}", colors[n]))

    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm
    return obj


# ---------------------------------------------------------------- animacion
def rot_loc_to_local(w):
    """Desplazamiento en mundo -> espacio local del hueso Hips (X, Z, -Y)."""
    return (w[0], w[2], -w[1])


def make_action(arm, name, keys, loop=True):
    """keys: lista de (frame, {hueso: (rx, ry, rz) grados, 'loc': (x, y, z) mundo para Hips})."""
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    for pb in arm.pose.bones:
        pb.rotation_mode = "XYZ"
    # Los huesos que cuelgan hacia abajo giran al reves: con +X su punta va hacia atras.
    # Las poses se escriben pensando en "+X = hacia delante", asi que se invierte aqui.
    hanging = ("UpperArm", "LowerArm", "UpperLeg", "LowerLeg")
    for frame, pose in keys:
        for pb in arm.pose.bones:
            r = pose.get(pb.name, (0, 0, 0))
            if pb.name.startswith(hanging):
                r = (-r[0], r[1], r[2])
            pb.rotation_euler = [math.radians(a) for a in r]
            pb.keyframe_insert("rotation_euler", frame=frame)
            if pb.name == "Hips":
                pb.location = rot_loc_to_local(pose.get("loc", (0, 0, 0)))
                pb.keyframe_insert("location", frame=frame)
    act.use_frame_range = True
    act.frame_start = keys[0][0]
    act.frame_end = keys[-1][0]
    act["loop"] = loop
    return act


def cyc(n, fn):
    """Genera n+1 claves de un ciclo (la ultima repite la primera)."""
    step = 1
    return [(i * step, fn(2 * math.pi * i / n)) for i in range(n + 1)]


def locomotion(leg, knee, arm_s, bob, lean, arm_fwd=0.0, arm_out=0.0, head=0.0, twist=0.0, roll=0.0, bend=10.0):
    def pose(ph):
        s, c = math.sin(ph), math.cos(ph)
        return {
            "Hips": (0, twist * s, roll * s),
            "loc": (0, 0, bob * abs(c) * -1.0),
            "Spine": (lean, -twist * s * 0.6, 0),
            "Head": (head - lean * 0.5, 0, 0),
            "UpperLeg.L": (leg * s, 0, 0),
            "UpperLeg.R": (-leg * s, 0, 0),
            "LowerLeg.L": (-knee * max(0.0, c), 0, 0),
            "LowerLeg.R": (-knee * max(0.0, -c), 0, 0),
            "UpperArm.L": (arm_fwd - arm_s * s, 0, -arm_out),
            "UpperArm.R": (arm_fwd + arm_s * s, 0, arm_out),
            "LowerArm.L": (bend, 0, 0),
            "LowerArm.R": (bend, 0, 0),
        }
    return pose


def player_locomotion(leg, knee, arm_s, bob, lean, bend_lo, bend_hi, twist, roll, foot, head=0.0, arm_fwd=0.0):
    """Andar/correr del protagonista, mas natural que la locomocion basica:
    - la rodilla de apoyo cede un poco al pisar y la de la pierna que avanza se dobla al pasar
    - el pie rueda: talon al pisar, punta al despegar
    - la cadera gira y cae hacia la pierna libre; los hombros contrarrotan; la cabeza compensa
    - los brazos se balancean opuestos a las piernas, con el codo mas doblado al ir hacia delante"""
    def pose(ph):
        s, c = math.sin(ph), math.cos(ph)
        s2 = math.sin(2 * ph)
        # rodillas: flexion de vuelo (pierna que avanza) + amortiguacion breve al apoyar
        swing_l = max(0.0, c)
        swing_r = max(0.0, -c)
        knee_l = knee * swing_l ** 1.5 + 8 * max(0.0, -s) * (1 - swing_l)
        knee_r = knee * swing_r ** 1.5 + 8 * max(0.0, s) * (1 - swing_r)
        arm_l = -arm_s * s
        arm_r = arm_s * s
        return {
            "Hips": (0, twist * s, roll * s2 * 0.5),
            "loc": (0, 0, -bob * (1 - abs(s2)) * 0.5 - bob * 0.5),
            "Spine": (lean, -twist * s * 1.3, -roll * s2 * 0.4),
            "Neck": (0, twist * s * 0.4, 0),
            "Head": (head - lean * 0.6 + 1.5 * s2, twist * s * 0.3, 0),
            "UpperLeg.L": (leg * s + 4, 0, 0),
            "UpperLeg.R": (-leg * s + 4, 0, 0),
            "LowerLeg.L": (-knee_l, 0, 0),
            "LowerLeg.R": (-knee_r, 0, 0),
            # pie: punta arriba al apoyar el talon (pierna delante), punta abajo al despegar (pierna detras)
            "Foot.L": (-foot * s, 0, 0),
            "Foot.R": (foot * s, 0, 0),
            "UpperArm.L": (arm_fwd + arm_l, 0, -6),
            "UpperArm.R": (arm_fwd + arm_r, 0, 6),
            "LowerArm.L": (bend_lo + (bend_hi - bend_lo) * max(0.0, -s), 0, 0),
            "LowerArm.R": (bend_lo + (bend_hi - bend_lo) * max(0.0, s), 0, 0),
        }
    return pose


# Postura de tirador (de lado): la cadera y los pies giran a la derecha (pie izquierdo adelantado), la columna,
# el cuello y la cabeza compensan para que la mirada y el arma apunten al frente.
STANCE_PISTOL = {"hips": -32, "spine": 18, "neck": 6, "head": 8}
STANCE_LONG = {"hips": -45, "spine": 22, "neck": 8, "head": 10}


def stance_legs(st, walk_ph=None, amp=0.0):
    """Piernas de la postura: izquierda adelantada, derecha atras, rodillas algo flexionadas."""
    s = math.sin(walk_ph) if walk_ph is not None else 0.0
    c = math.cos(walk_ph) if walk_ph is not None else 0.0
    return {
        "Hips": (0, st["hips"], 0),
        "loc": (0, 0, -0.03),
        "UpperLeg.L": (14 + amp * s, 0, -4),
        "UpperLeg.R": (-10 - amp * s, 0, 5),
        "LowerLeg.L": (-12 - amp * 0.9 * max(0.0, c), 0, 0),
        "LowerLeg.R": (-6 - amp * 0.9 * max(0.0, -c), 0, 0),
        "Foot.L": (-6, 0, 0),
    }


def stance_upper(st):
    return {"Spine": (4, st["spine"], 0), "Neck": (0, st["neck"], 0), "Head": (0, st["head"], 0)}


def pistol_aim_pose(st):
    p = stance_upper(st)
    # brazo derecho extendido; el izquierdo cruza por delante para sujetar desde abajo (Weaver)
    p.update({"UpperArm.R": (86, -30, 2), "LowerArm.R": (4, 0, 0), "Hand.R": (0, 0, 0),
              "UpperArm.L": (82, 40, -6), "LowerArm.L": (26, 0, 0)})
    return p


def long_aim_pose(st):
    p = stance_upper(st)
    # culata al hombro derecho (codo alto y abierto), mano izquierda adelantada bajo el guardamanos,
    # cabeza inclinada sobre la culata
    p.update({"Head": (10, st["head"], -10),
              "UpperArm.R": (78, -14, 40), "LowerArm.R": (118, 0, 0), "Hand.R": (0, 0, 0),
              "UpperArm.L": (98, 12, -2), "LowerArm.L": (6, 0, 0)})
    return p


def player_actions(arm):
    make_action(arm, "Idle", cyc(80, lambda ph: {
        "loc": (0, 0, -0.004 * (1 + math.sin(ph))),
        "Spine": (1.5 + 1.2 * math.sin(ph), 0.8 * math.sin(ph * 0.5), 0), "Head": (-1.0 * math.sin(ph), 2 * math.sin(ph * 0.5), 0),
        "UpperArm.L": (3 * math.sin(ph), 0, -4), "UpperArm.R": (3 * math.sin(ph), 0, 4),
        "LowerArm.L": (10, 0, 0), "LowerArm.R": (10, 0, 0),
        "UpperLeg.L": (2, 0, -2), "UpperLeg.R": (-1, 0, 3), "LowerLeg.L": (-4, 0, 0)}))
    make_action(arm, "Walk", cyc(30, player_locomotion(30, 55, 20, 0.035, 3, 10, 28, 7, 3, 14)))
    make_action(arm, "Run", cyc(20, player_locomotion(46, 95, 48, 0.07, 12, 70, 95, 11, 4, 22, head=4)))

    # ---- pistola
    for name, st in (("", STANCE_PISTOL), ("_Long", STANCE_LONG)):
        legs = stance_legs(st)
        make_action(arm, "AimIdle" + name, cyc(60, lambda ph, legs=legs: dict(legs, loc=(0, 0, -0.03 - 0.004 * math.sin(ph)))))
        make_action(arm, "AimWalk" + name, cyc(28, lambda ph, st=st: stance_legs(st, ph, 16)))

    aim = pistol_aim_pose(STANCE_PISTOL)
    aim_legs = stance_legs(STANCE_PISTOL)
    make_action(arm, "Aim", cyc(60, lambda ph: dict(aim_legs, **dict(aim, Spine=(4 + 0.6 * math.sin(ph), STANCE_PISTOL["spine"], 0)))))
    ready = {"Spine": (4, 0, 0), "Head": (2, 0, 0),
             "UpperArm.R": (32, -10, 6), "UpperArm.L": (36, 24, -4),
             "LowerArm.R": (58, 0, 0), "LowerArm.L": (66, 0, 0)}
    make_action(arm, "LowReady", cyc(60, lambda ph: dict(ready, Spine=(4 + 0.8 * math.sin(ph), 0, 0))))
    recoil = dict(aim_legs, **dict(aim, **{"UpperArm.R": (104, -30, 2), "UpperArm.L": (98, 40, -6), "LowerArm.R": (14, 0, 0),
                                            "Spine": (-3, STANCE_PISTOL["spine"], 0), "Head": (-3, STANCE_PISTOL["head"], 0)}))
    aimf = dict(aim_legs, **aim)
    make_action(arm, "Shoot", [(0, aimf), (2, recoil), (5, dict(aimf, **{"UpperArm.R": (92, -30, 2)})), (10, aimf)], loop=False)
    low = dict(aim_legs, **{"Spine": (8, STANCE_PISTOL["spine"], 0), "Head": (24, STANCE_PISTOL["head"], 0),
                            "UpperArm.R": (50, -20, 8), "LowerArm.R": (60, 0, 0), "UpperArm.L": (42, 28, -6), "LowerArm.L": (70, 0, 0)})
    eject = dict(low, **{"Hand.R": (0, 0, 25)})
    mag = dict(low, **{"UpperArm.L": (20, 0, -18), "LowerArm.L": (100, 0, 0)})
    insert = dict(low, **{"UpperArm.L": (46, 30, -6), "LowerArm.L": (80, 0, 0)})
    rack = dict(insert, **{"UpperArm.L": (62, 34, -4), "LowerArm.L": (50, 0, 0)})
    make_action(arm, "Reload", [(0, aimf), (8, low), (12, eject), (20, mag), (30, insert), (36, rack), (44, aimf)], loop=False)

    # ---- arma larga (escopeta) a dos manos
    laim = dict(stance_legs(STANCE_LONG), **long_aim_pose(STANCE_LONG))
    make_action(arm, "Aim_Long", cyc(60, lambda ph: dict(laim, Spine=(4 + 0.5 * math.sin(ph), STANCE_LONG["spine"], 0))))
    lready = {"Spine": (6, 14, 0), "Head": (4, -6, 0),
              "UpperArm.R": (26, 0, 18), "LowerArm.R": (70, 0, 0),
              "UpperArm.L": (54, 30, -4), "LowerArm.L": (52, 0, 0)}
    make_action(arm, "LowReady_Long", cyc(60, lambda ph: dict(lready, Spine=(6 + 0.8 * math.sin(ph), 14, 0))))
    lrecoil = dict(laim, **{"Spine": (-6, STANCE_LONG["spine"] - 4, 0), "UpperArm.R": (86, -14, 40), "UpperArm.L": (105, 12, -2), "Head": (4, STANCE_LONG["head"], -10)})
    pump_back = dict(laim, **{"UpperArm.L": (90, 10, -2), "LowerArm.L": (30, 0, 0)})
    make_action(arm, "Shoot_Long", [(0, laim), (2, lrecoil), (8, laim), (13, pump_back), (19, laim), (24, laim)], loop=False)
    # recarga: arma baja, la mano izquierda va al cinturon y mete cartuchos por abajo (dos veces)
    lload = dict(laim, **{"Spine": (14, STANCE_LONG["spine"], 0), "Head": (22, STANCE_LONG["head"], 0),
                          "UpperArm.R": (48, -14, 34), "LowerArm.R": (104, 0, 0)})
    belt = dict(lload, **{"UpperArm.L": (8, 0, -16), "LowerArm.L": (70, 0, 0)})
    push = dict(lload, **{"UpperArm.L": (50, 30, -4), "LowerArm.L": (72, 0, 0)})
    make_action(arm, "Reload_Long", [(0, laim), (8, lload), (14, belt), (22, push), (28, belt), (36, push), (42, lload), (52, laim)], loop=False)

    make_action(arm, "Hit", [
        (0, {}), (3, {"Spine": (-16, 0, 0), "Head": (-22, 0, 0), "loc": (0, 0.08, 0), "UpperArm.L": (20, 0, -10), "UpperArm.R": (20, 0, 10)}),
        (12, {})], loop=False)
    death_actions(arm, "Death")


def death_actions(arm, name):
    make_action(arm, name, [
        (0, {}),
        (10, {"Hips": (-12, 0, 0), "Spine": (-6, 0, 0), "UpperArm.L": (25, 0, -20), "UpperArm.R": (25, 0, 20), "loc": (0, 0.1, -0.1)}),
        (32, {"Hips": (-92, 0, 0), "UpperArm.L": (10, 0, -35), "UpperArm.R": (10, 0, 35), "UpperLeg.L": (0, 0, -6), "UpperLeg.R": (0, 0, 6), "loc": (0, 0.55, -0.80)}),
        (44, {"Hips": (-90, 0, 0), "UpperArm.L": (6, 0, -35), "UpperArm.R": (6, 0, 35), "loc": (0, 0.55, -0.82)}),
    ], loop=False)


def zombie_actions(arm):
    make_action(arm, "ZIdle", cyc(72, lambda ph: {
        "Hips": (0, 0, 2 * math.sin(ph)), "Spine": (10 + 1.5 * math.sin(ph), 0, 0), "Head": (14 + 3 * math.sin(ph * 2), 0, 3 * math.sin(ph)),
        "UpperArm.L": (22 + 4 * math.sin(ph), 0, -4), "UpperArm.R": (30 + 4 * math.sin(ph + 1), 0, 4),
        "LowerArm.L": (30, 0, 0), "LowerArm.R": (40, 0, 0)}))
    # twist/roll suaves: con valores altos el cuerpo se mece de lado a lado en vez de avanzar
    make_action(arm, "ZWalk", cyc(40, locomotion(22, 18, 8, 0.015, 12, arm_fwd=78, arm_out=5, head=14, twist=2.5, roll=1.2, bend=20)))
    make_action(arm, "ZRun", cyc(20, locomotion(40, 40, 10, 0.04, 20, arm_fwd=72, arm_out=6, head=16, twist=4, roll=2, bend=25)))

    fwd = {"Spine": (10, 0, 0), "Head": (12, 0, 0), "UpperArm.L": (80, 0, -5), "UpperArm.R": (80, 0, 5), "LowerArm.L": (20, 0, 0), "LowerArm.R": (20, 0, 0)}
    up = {"Spine": (-12, 0, 0), "Head": (-10, 0, 0), "UpperArm.L": (140, 0, -15), "UpperArm.R": (140, 0, 15), "LowerArm.L": (10, 0, 0), "LowerArm.R": (10, 0, 0), "loc": (0, 0.08, 0)}
    slam = {"Spine": (30, 0, 0), "Head": (20, 0, 0), "UpperArm.L": (50, 0, -8), "UpperArm.R": (50, 0, 8), "LowerArm.L": (35, 0, 0), "LowerArm.R": (35, 0, 0), "loc": (0, -0.18, -0.08)}
    make_action(arm, "ZAttack", [(0, fwd), (10, up), (16, slam), (22, slam), (32, fwd)], loop=False)

    make_action(arm, "ZHit", [
        (0, fwd), (3, dict(fwd, Spine=(-8, 0, 0), Head=(-18, 0, 12), loc=(0, 0.1, 0))), (14, fwd)], loop=False)
    death_actions(arm, "ZDeath")


# ---------------------------------------------------------------- estilos de zombi
# Cada tipo de zombi tiene su propia forma de moverse: brazos asimetricos, cojera, cabeza ladeada, postura...
# La cadencia (fotogramas por ciclo) se calcula para que la zancada avance a la velocidad de persecucion del tipo:
# asi los pies no patinan. Velocidad de la zancada ~ 2 pasos * (2 * 0.9 m * sin(angulo de pierna)) / duracion.
ZOMBIE_STYLES = {
    # speed: m/s de persecucion (igual que en TestSceneBuilder). arms: (adelante izq, adelante der, apertura, vaiven)
    "civil": dict(speed=1.4, leg=24, knee=20, lean=12, head=14, head_roll=6, roll_head_amp=2, arms=(68, 86, 5, 7),
                  bend=(14, 30), twist=2.5, roll=1.2, limp=1.0, stance=0, bob=0.015, attack="grab"),
    "cop": dict(speed=1.4, leg=26, knee=22, lean=8, head=10, head_roll=-16, roll_head_amp=3, arms=(10, 84, 4, 12),
                bend=(4, 22), twist=3, roll=2.5, limp=0.55, stance=0, bob=0.02, attack="one_arm"),
    "gown": dict(speed=1.8, leg=28, knee=30, lean=26, head=26, head_roll=0, roll_head_amp=12, arms=(32, 44, 3, 22),
                 bend=(30, 18), twist=6, roll=2, limp=1.0, stance=0, bob=0.03, attack="lunge"),
    "suit": dict(speed=1.3, leg=20, knee=12, lean=3, head=6, head_roll=24, roll_head_amp=2, arms=(96, 72, 12, 4),
                 bend=(6, 20), twist=1, roll=0.8, limp=1.0, stance=0, bob=0.01, attack="choke"),
    "overalls": dict(speed=1.0, leg=18, knee=16, lean=15, head=8, head_roll=4, roll_head_amp=3, arms=(24, 30, 14, 14),
                     bend=(20, 24), twist=3, roll=6, limp=1.0, stance=7, bob=0.035, attack="smash"),
    "bare": dict(speed=1.6, leg=30, knee=34, lean=22, head=4, head_roll=-8, roll_head_amp=6, arms=(104, 88, 18, 10),
                 bend=(42, 30), twist=5, roll=2, limp=1.0, stance=0, bob=0.03, attack="lunge"),
}


def style_cycle_frames(st, leg_scale=1.0, speed_scale=1.0):
    eff = math.sin(math.radians(st["leg"] * leg_scale)) * (1.0 + st["limp"]) / 2.0
    secs = 2 * 2 * 0.9 * eff / (st["speed"] * speed_scale)
    return max(14, int(round(secs * FPS)))


def zlocomotion(st, leg_scale=1.0, extra_lean=0.0):
    fl, fr, out, swing = st["arms"]
    bl, br = st["bend"]
    leg, knee = st["leg"] * leg_scale, st["knee"] * leg_scale
    lean = st["lean"] + extra_lean

    def pose(ph):
        s, c = math.sin(ph), math.cos(ph)
        lim = st["limp"]
        return {
            # el balanceo lateral de la cadera acompana a la pierna de apoyo (en la cojera se nota mas)
            "Hips": (0, st["twist"] * s, st["roll"] * s + (1 - lim) * 6 * max(0.0, -s)),
            "loc": (0, 0, -st["bob"] * abs(c)),
            "Spine": (lean, -st["twist"] * s * 0.6, -st["roll"] * s * 0.5),
            "Neck": (0, 0, 0),
            "Head": (st["head"] - lean * 0.5, st["roll_head_amp"] * 0.4 * s, st["head_roll"] + st["roll_head_amp"] * math.sin(ph * 0.5)),
            "UpperLeg.L": (leg * s, 0, -st["stance"]),
            "UpperLeg.R": (-leg * s * lim, 0, st["stance"]),
            "LowerLeg.L": (-knee * max(0.0, c), 0, 0),
            "LowerLeg.R": (-knee * lim * max(0.0, -c), 0, 0),
            "Foot.R": (12 * (1 - lim), 0, 0),          # pie que se arrastra: punta hacia abajo
            "UpperArm.L": (fl - swing * s, 0, -out),
            "UpperArm.R": (fr + swing * s * 0.8, 0, out),
            "LowerArm.L": (bl + 4 * c, 0, 0),
            "LowerArm.R": (br - 4 * c, 0, 0),
        }
    return pose


def zidle(st):
    fl, fr, out, _ = st["arms"]
    bl, br = st["bend"]

    def pose(ph):
        s = math.sin(ph)
        return {
            "Hips": (0, 0, 2 * s + (1 - st["limp"]) * 5),
            "Spine": (st["lean"] + 1.5 * s, 0, 0),
            "Head": (st["head"] + 3 * math.sin(ph * 2), 0, st["head_roll"] + st["roll_head_amp"] * s),
            "UpperArm.L": (fl * 0.5 + 4 * s, 0, -out),
            "UpperArm.R": (fr * 0.5 + 4 * math.sin(ph + 1), 0, out),
            "LowerArm.L": (bl + 10, 0, 0), "LowerArm.R": (br + 10, 0, 0),
            "UpperLeg.L": (0, 0, -st["stance"]), "UpperLeg.R": (0, 0, st["stance"]),
        }
    return pose


def zattack_keys(st):
    lean = st["lean"]
    kind = st["attack"]
    if kind == "one_arm":       # zarpazo con el brazo bueno; el otro cuelga
        ready = {"Spine": (lean, 0, 0), "UpperArm.R": (80, 0, 6), "LowerArm.R": (20, 0, 0), "UpperArm.L": (10, 0, -4)}
        wind = dict(ready, **{"Spine": (lean - 8, -18, 0), "UpperArm.R": (120, 0, 30), "LowerArm.R": (60, 0, 0)})
        hit = dict(ready, **{"Spine": (lean + 18, 22, 0), "UpperArm.R": (70, 0, -20), "LowerArm.R": (10, 0, 0), "loc": (0, -0.12, 0)})
        return [(0, ready), (9, wind), (15, hit), (20, hit), (30, ready)]
    if kind == "lunge":         # embestida con mordisco: todo el cuerpo hacia delante
        ready = {"Spine": (lean, 0, 0), "Head": (10, 0, 0), "UpperArm.L": (60, 0, -10), "UpperArm.R": (60, 0, 10)}
        crouch = dict(ready, **{"Spine": (lean + 8, 0, 0), "UpperLeg.L": (20, 0, 0), "UpperLeg.R": (20, 0, 0),
                                "LowerLeg.L": (-40, 0, 0), "LowerLeg.R": (-40, 0, 0), "loc": (0, 0.05, -0.1)})
        lunge = dict(ready, **{"Spine": (lean + 22, 0, 0), "Head": (-15, 0, 0), "UpperArm.L": (100, 0, -20),
                               "UpperArm.R": (100, 0, 20), "LowerArm.L": (10, 0, 0), "LowerArm.R": (10, 0, 0), "loc": (0, -0.3, -0.05)})
        return [(0, ready), (8, crouch), (13, lunge), (20, lunge), (30, ready)]
    if kind == "choke":         # agarra a la altura del cuello y aprieta
        ready = {"Spine": (lean, 0, 0), "UpperArm.L": (90, 0, -12), "UpperArm.R": (90, 0, 12)}
        reach = dict(ready, **{"Spine": (lean + 10, 0, 0), "UpperArm.L": (105, 0, 10), "UpperArm.R": (105, 0, -10),
                               "LowerArm.L": (30, 0, 0), "LowerArm.R": (30, 0, 0), "loc": (0, -0.15, 0)})
        squeeze = dict(reach, **{"Spine": (lean + 4, 0, 0), "Head": (-10, 0, 20)})
        return [(0, ready), (10, reach), (16, squeeze), (24, reach), (34, ready)]
    if kind == "smash":         # golpe a dos manos desde arriba (pesado)
        fwd = {"Spine": (lean, 0, 0), "Head": (12, 0, 0), "UpperArm.L": (80, 0, -5), "UpperArm.R": (80, 0, 5)}
        up = {"Spine": (-14, 0, 0), "Head": (-10, 0, 0), "UpperArm.L": (150, 0, -15), "UpperArm.R": (150, 0, 15), "loc": (0, 0.06, 0)}
        slam = {"Spine": (38, 0, 0), "Head": (22, 0, 0), "UpperArm.L": (45, 0, -8), "UpperArm.R": (45, 0, 8),
                "LowerArm.L": (30, 0, 0), "LowerArm.R": (30, 0, 0), "loc": (0, -0.22, -0.12)}
        return [(0, fwd), (14, up), (20, slam), (28, slam), (40, fwd)]
    # "grab": los dos brazos, pero desacompasados
    fwd = {"Spine": (lean, 0, 0), "UpperArm.L": (70, 0, -5), "UpperArm.R": (86, 0, 5), "LowerArm.L": (20, 0, 0), "LowerArm.R": (30, 0, 0)}
    reach = dict(fwd, **{"Spine": (lean + 14, 8, 0), "UpperArm.L": (110, 0, -2), "UpperArm.R": (95, 0, 8), "LowerArm.L": (5, 0, 0), "loc": (0, -0.15, 0)})
    pull = dict(reach, **{"Spine": (lean - 4, -6, 0), "UpperArm.L": (75, 0, 10), "UpperArm.R": (80, 0, -10), "LowerArm.L": (60, 0, 0), "LowerArm.R": (60, 0, 0)})
    return [(0, fwd), (9, reach), (16, pull), (22, pull), (32, fwd)]


def zombie_style_actions(arm):
    """Por cada estilo: ZIdle_<s>, ZWalk_<s>, ZRun_<s>, ZAttack_<s>. Devuelve {estilo: fotogramas del paso}."""
    info = {}
    for name, st in ZOMBIE_STYLES.items():
        n = style_cycle_frames(st)
        make_action(arm, f"ZIdle_{name}", cyc(72, zidle(st)))
        make_action(arm, f"ZWalk_{name}", cyc(n, zlocomotion(st)))
        # carrera: mas zancada y mas inclinacion, al doble de velocidad
        nr = style_cycle_frames(st, leg_scale=1.6, speed_scale=2.0)
        make_action(arm, f"ZRun_{name}", cyc(nr, zlocomotion(st, leg_scale=1.6, extra_lean=8)))
        make_action(arm, f"ZAttack_{name}", zattack_keys(st), loop=False)
        info[name] = (n, nr)
    return info


def build_zombie_styles(export_path):
    """Exporta solo esqueleto + animaciones por estilo (las mallas de los zombis van en sus propios FBX)."""
    clear_scene()
    bpy.context.scene.render.fps = FPS
    arm = make_armature()
    # Malla de bloques minima: sin ninguna malla, Unity hace del esqueleto la raiz y las rutas de los clips
    # pierden el prefijo "Armature/" (no animarian a los modelos de los zombis)
    make_body("zombie", arm)
    info = zombie_style_actions(arm)
    bpy.context.scene.frame_set(0)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=export_path, use_selection=True, object_types={"ARMATURE", "MESH"}, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y")
    return info


# ---------------------------------------------------------------- principal
def export_fbx(path, animations=True):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=animations,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="FACE",
        path_mode="COPY",
        embed_textures=False,
    )


def build(kind, export_path=None, outfit=None, animations=True):
    """outfit: solo zombis ('tshirt', 'cop', 'gown', 'suit', 'overalls', 'bare').
    animations=False exporta solo malla + esqueleto (las variantes reutilizan las animaciones del zombi base)."""
    clear_scene()
    bpy.context.scene.render.fps = FPS
    arm = make_armature()
    make_body_detailed(kind, arm, outfit)   # make_body() = version antigua de bloques
    if animations:
        (player_actions if kind == "player" else zombie_actions)(arm)
        arm.animation_data.action = bpy.data.actions.get("Idle" if kind == "player" else "ZIdle")
    bpy.context.scene.frame_set(0)
    if export_path:
        export_fbx(export_path, animations)
    return [a.name for a in bpy.data.actions]


def build_zombie_variants(out_dir):
    """Exporta ZombieCop/Gown/Suit/Overalls/Bare.fbx (sin animaciones)."""
    report = {}
    for outfit in ZOMBIE_OUTFITS:
        if outfit == "tshirt":
            continue
        build("zombie", export_path=out_dir + "\\Zombie" + outfit.capitalize() + ".fbx", outfit=outfit, animations=False)
        o = bpy.data.objects["Zombie" + outfit.capitalize()]
        report[outfit] = (len(o.data.vertices), [m.name for m in o.data.materials])
    return report
