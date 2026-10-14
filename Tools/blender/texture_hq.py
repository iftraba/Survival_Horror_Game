"""Pipeline de alta calidad (2026-10-14): modelo de alta (biseles redondos) -> modelo de baja (chaflan) con normales horneadas, y texturas pintadas por capas.

    blender --background --python texture_hq.py -- <builder.py> <funcion> <Nombre> <carpeta texturas ABSOLUTA> <carpeta FBX ABSOLUTA> [tam=2048]
    (ejemplo: ... build_furniture_v2.py make_locker Locker ...)

Pasos:
  1. Se ejecuta la funcion del constructor dos veces: SEG alto (biseles de 4 segmentos, la «alta») y SEG=1 (un chaflan, la «baja»). Las dos comparten forma.
  2. UV de la baja (smart project).
  3. Mapas de identificador de la baja: color base, clase de material, metal y rugosidad (cada material es una clase).
  4. Normales (tangente) y oclusion horneadas de la alta a la baja (selected to active).
  5. Se compone en numpy: curvatura sacada de las normales (bordes convexos = desgaste y pintura saltada, concavos = suciedad), oclusion en las cavidades,
     chorretones verticales, rasguños, manchas de oxido y variacion de tono por clase de material (chapa pintada, metal, madera, tela, plastico, papel, cristal).
  6. Se escribe NOMBRE_BaseColor.png, NOMBRE_MetallicSmoothness.png (R metal, A suavidad), NOMBRE_Normal.png y NOMBRE.fbx con UN material «NOMBRE_baked».
El constructor debe usar Mesher.box con el bisel pedido: aqui se parchea el numero de segmentos con la variable global SEG.
"""
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
BUILDER, FUNC, NAME, TEXDIR, FBXDIR = args[:5]
SIZE = int(args[5]) if len(args) > 5 else 2048
os.makedirs(TEXDIR, exist_ok=True)
os.makedirs(FBXDIR, exist_ok=True)

HERE = os.path.dirname(os.path.abspath(__file__))
_NO_MAIN = True
SEG = 2
src = open(BUILDER, encoding="utf-8").read()
# el constructor trae build_items.py (Mesher) por exec: se parchea alli el numero de segmentos del bisel
_orig_open = open


def _patched_open(path, *a, **k):
    f = _orig_open(path, *a, **k)
    if str(path).endswith("build_items.py"):
        text = f.read().replace("segments=2, profile=0.6", "segments=SEG, profile=0.7")
        import io
        return io.StringIO(text)
    return f


builtins_open = open
import builtins
builtins.open = _patched_open
g = {"__name__": "builder", "__file__": BUILDER, "_NO_MAIN": True, "SEG": 2}
exec(compile(src, BUILDER, "exec"), g)
builtins.open = builtins_open
make = g[FUNC]
clear = g["_clear"]
export = g["_export"]


def build(seg):
    g["SEG"] = seg
    before = set(bpy.data.objects)
    obj = make()
    return obj


clear()
high = build(4)
high.name = "HIGH"
low = build(1)
low.name = "LOW"
bpy.context.view_layer.update()
print("alta", len(high.data.polygons), "baja", len(low.data.polygons), flush=True)

sc = bpy.context.scene
sc.render.engine = "CYCLES"
sc.cycles.device = "CPU"
sc.cycles.samples = 12
sc.render.bake.margin = 16
w = bpy.data.worlds.new("w")
w.use_nodes = True
sc.world = w                                                               # sin mundo el AO se hornea negro
w.light_settings.distance = 0.5

# ---------- UV de la baja
bpy.ops.object.select_all(action="DESELECT")
low.select_set(True)
bpy.context.view_layer.objects.active = low
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.006)
bpy.ops.object.mode_set(mode="OBJECT")

# ---------- clases de material
CLASSES = ["painted", "metal", "wood", "fabric", "plastic", "paper", "glass", "rubber"]
PARAM = {   # metal, rugosidad base
    "painted": (0.0, 0.5), "metal": (1.0, 0.38), "wood": (0.0, 0.6), "fabric": (0.0, 0.88), "plastic": (0.0, 0.5), "paper": (0.0, 0.8), "glass": (0.0, 0.1), "rubber": (0.0, 0.85)}


def classify(name):
    n = name.lower()
    if "glass" in n or "lens" in n:
        return "glass"
    if "rubber" in n:
        return "rubber"
    if "paper" in n or "label" in n:
        return "paper"
    if any(k in n for k in ("handle", "steel", "metal", "brass", "chrome", "rim")):
        return "metal"
    if "wood" in n or "board" in n or "laminate" in n:
        return "wood"
    if any(k in n for k in ("fabric", "seat", "mattress", "blanket", "pillow", "cushion", "couch")):
        return "fabric"
    if "body" in n or "carpaint" in n or "paint" in n:
        return "painted"
    return "plastic"


slot_class = []
for slot in low.material_slots:
    slot_class.append(classify(slot.material.name))
print("clases", slot_class, flush=True)


def new_img(suffix, nc=True):
    img = bpy.data.images.new(NAME + "_" + suffix, SIZE, SIZE, alpha=False, float_buffer=True)
    img.colorspace_settings.name = "Non-Color" if nc else "sRGB"
    return img


def emission_tree(mat, color):
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*color, 1.0)
    em.inputs["Strength"].default_value = 1.0
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return nt


def attach_target(mat, img):
    nt = mat.node_tree
    nd = nt.nodes.new("ShaderNodeTexImage")
    nd.name = "BakeTarget"
    nd.image = img
    for o in nt.nodes:
        o.select = False
    nd.select = True
    nt.nodes.active = nd


def bake_id(suffix, colors, nc=True):
    img = new_img(suffix, nc)
    for slot, col in zip(low.material_slots, colors):
        emission_tree(slot.material, col)
        attach_target(slot.material, img)
    bpy.ops.object.select_all(action="DESELECT")
    low.select_set(True)
    bpy.context.view_layer.objects.active = low
    bpy.ops.object.bake(type="EMIT", use_clear=True, margin=16)
    return img


orig_colors = []
for slot in low.material_slots:
    m = slot.material
    c = tuple(m.diffuse_color[:3])
    orig_colors.append(c)
col_img = bake_id("ID_Color", orig_colors, nc=True)
cls_img = bake_id("ID_Class", [(CLASSES.index(c) / 10.0,) * 3 for c in slot_class])
met_img = bake_id("ID_Metal", [(PARAM[c][0],) * 3 for c in slot_class])
rou_img = bake_id("ID_Rough", [(PARAM[c][1],) * 3 for c in slot_class])


def arr(img):
    a = np.array(img.pixels[:], dtype=np.float32).reshape(SIZE, SIZE, 4)
    return a[..., :3]


# ---------- una sola material para las bakes de normal y oclusion
bpy.ops.object.select_all(action="DESELECT")
low.select_set(True)
bpy.context.view_layer.objects.active = low
single = bpy.data.materials.new(NAME + "_bake")
single.use_nodes = True
low.data.materials.clear()
low.data.materials.append(single)
for p in low.data.polygons:
    p.material_index = 0
nrm_img = new_img("Normal")
attach_target(single, nrm_img)

bpy.ops.object.select_all(action="DESELECT")
high.select_set(True)
low.select_set(True)
bpy.context.view_layer.objects.active = low
dims = max(low.dimensions)
bpy.ops.object.bake(type="NORMAL", use_selected_to_active=True, cage_extrusion=0.02 * max(1.0, dims), max_ray_distance=0.0, normal_space="TANGENT", use_clear=True, margin=16)
ao_img = new_img("AO")
attach_target(single, ao_img)
bpy.ops.object.select_all(action="DESELECT")
high.select_set(True)
low.select_set(True)
bpy.context.view_layer.objects.active = low
sc.cycles.samples = 48
bpy.ops.object.bake(type="AO", use_selected_to_active=True, cage_extrusion=0.02 * max(1.0, dims), max_ray_distance=0.0, use_clear=True, margin=16)
print("bakes hechas", flush=True)

# ---------- composicion en numpy
def vnoise(shape, cells, seed):
    r = np.random.default_rng(seed)
    gy, gx = cells
    gr = r.random((gy + 1, gx + 1)).astype(np.float32)
    gr[:, -1] = gr[:, 0]
    gr[-1, :] = gr[0, :]
    ys, xs = np.linspace(0, gy, shape[0], endpoint=False), np.linspace(0, gx, shape[1], endpoint=False)
    y0, x0 = ys.astype(int), xs.astype(int)
    fy, fx = (ys - y0)[:, None], (xs - x0)[None, :]
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    return gr[y0][:, x0] * (1 - fx) * (1 - fy) + gr[y0][:, x0 + 1] * fx * (1 - fy) + gr[y0 + 1][:, x0] * (1 - fx) * fy + gr[y0 + 1][:, x0 + 1] * fx * fy


def fbm(cells, octaves, seed, aspect=(1, 1)):
    tot, amp, norm = np.zeros((SIZE, SIZE), np.float32), 1.0, 0.0
    for o in range(octaves):
        tot += amp * vnoise((SIZE, SIZE), (cells[0] * 2 ** o * aspect[0], cells[1] * 2 ** o * aspect[1]), seed + o)
        norm += amp
        amp *= 0.55
    return tot / norm


def blur(a, k=1):
    for _ in range(k):
        a = (a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 5.0
    return a


def lin2srgb(c):
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(np.clip(c, 0, None), 1 / 2.4) - 0.055)


base = np.clip(arr(col_img), 0, 1)
cls = np.rint(arr(cls_img)[..., 0] * 10.0).astype(int)
metal0 = arr(met_img)[..., 0]
rough0 = arr(rou_img)[..., 0]
nm = arr(nrm_img) * 2.0 - 1.0                                              # normales tangentes
ao_raw = np.clip(arr(ao_img)[..., 0], 0, 1)
vm = arr(col_img).sum(axis=2) > 0.0005
print('ao media', float(ao_raw[vm].mean()), 'max', float(ao_raw.max()), 'validos', float(vm.mean()), 'nm', float(arr(nrm_img).mean()), flush=True)
ao = np.clip(ao_raw * 1.35, 0, 1)                                           # el AO de superficies abiertas sale ~0,7: se reescala para que 1 = abierto
cav = np.clip((0.78 - ao) * 3.0, 0, 1)                                      # solo las cavidades de verdad
valid = (arr(col_img).sum(axis=2) > 0.0005)                                # fuera de las islas el id de color es negro

# curvatura a partir de las normales: divergencia del campo (xy)
dx = np.roll(nm[..., 0], -1, 1) - np.roll(nm[..., 0], 1, 1)
dy = np.roll(nm[..., 1], -1, 0) - np.roll(nm[..., 1], 1, 0)
curv = (dx + dy) * 0.5
convex = np.clip(curv * 3.0, 0, 1)                                          # bordes que salen
concave = np.clip(-curv * 3.0, 0, 1)                                        # rincones
convex = blur(convex, 1)
concave = blur(concave, 2)
edge = np.clip(convex * 1.6, 0, 1)

n1 = fbm((6, 6), 5, 11)                                                     # variacion grande de tono
n2 = fbm((40, 40), 4, 21)                                                   # moteado fino
n3 = fbm((3, 28), 5, 31)                                                    # chorretones verticales (alargados en V)
grain = fbm((6, 90), 3, 41)                                                # veta de madera (alargada en U)
# rasguños: lineas finas con direccion aleatoria
scr = np.zeros((SIZE, SIZE), np.float32)
rg = np.random.default_rng(7)
for _ in range(220):
    x, y = rg.integers(0, SIZE, 2)
    ang = rg.uniform(0, math.pi)
    ln = int(rg.uniform(30, 160))
    for t in range(ln):
        xx = int(x + math.cos(ang) * t) % SIZE
        yy = int(y + math.sin(ang) * t) % SIZE
        scr[yy, xx] = max(scr[yy, xx], rg.uniform(0.4, 1.0))
scr = np.clip(blur(scr, 1) * 3.0, 0, 1)

dirt_c = np.array([0.07, 0.055, 0.04], np.float32)
rust_c = np.array([0.30, 0.13, 0.05], np.float32)
bare_c = np.array([0.50, 0.50, 0.52], np.float32)

col = base.copy()
rough = rough0.copy()
metal = metal0.copy()

m_pt = cls == CLASSES.index("painted")
m_me = cls == CLASSES.index("metal")
m_wo = cls == CLASSES.index("wood")
m_fa = cls == CLASSES.index("fabric")
m_pl = cls == CLASSES.index("plastic")
m_pa = cls == CLASSES.index("paper")
m_ru = cls == CLASSES.index("rubber")
m_gl = cls == CLASSES.index("glass")

# tono: cada clase tiene su variacion
tone = (0.88 + 0.24 * n1 + 0.08 * (n2 - 0.5))[..., None]
col = col * tone
# pintura: desconchones en los bordes, oxido en rincones bajos, chorretones, manchas
wear = np.clip(edge * 1.2 + (n2 > 0.66) * edge * 0.8 - 0.1, 0, 1)
chip = (wear > 0.55)
rustmask = np.clip(concave * 1.3 + cav * 1.1 + np.clip(n3 - 0.72, 0, 1) * 0.8, 0, 1) * (n2 > 0.5)
streak = np.clip(n3 * 1.6 - 0.55, 0, 1) * (0.4 + 0.6 * (1 - ao))
for m in (m_pt,):
    c = col.copy()
    c = c * (1 - 0.30 * streak[..., None]) + np.array([0.01, 0.006, 0.0]) * streak[..., None]                                                        # chorretones oscuros
    c = c * (1 - 0.55 * concave[..., None])
    c = c * (0.55 + 0.45 * ao[..., None])
    c = c * (1 - 0.0) + 0
    c = np.where(((rustmask > 0.6) & (n2 > 0.55))[..., None], c * 0.45 + rust_c * 0.55, c)         # oxido
    c = np.where(chip[..., None], bare_c * (0.75 + 0.25 * n2[..., None]), c)                     # metal desnudo en los bordes
    col = np.where(m[..., None], c, col)
    rough = np.where(m, 0.46 + 0.25 * streak + 0.3 * (rustmask > 0.6) + 0.1 * n2 - 0.15 * chip, rough)
    metal = np.where(m, chip * 0.9 + 0.0, metal)
# metal: pulido en bordes, oscurecido en rincones, rasguños claros
c = col * (0.7 + 0.3 * ao[..., None]) * (1 - 0.4 * concave[..., None])
c = np.clip(c + edge[..., None] * 0.12 + scr[..., None] * 0.14 + (n2 - 0.5)[..., None] * 0.08, 0, 1)
col = np.where(m_me[..., None], c, col)
rough = np.where(m_me, np.clip(0.3 + 0.35 * (1 - ao) + 0.2 * n2 - 0.15 * scr - 0.1 * edge, 0.1, 1), rough)
# madera: veta, bordes mas claros y sucios en las juntas, barniz gastado
v = (0.82 + 0.35 * grain)[..., None]
c = col * v * (0.6 + 0.4 * ao[..., None])
c = c * (1 - 0.5 * concave[..., None]) + edge[..., None] * 0.07
c = np.where((edge > 0.45)[..., None], c * 0.7 + np.array([0.35, 0.25, 0.15]) * 0.3, c)
col = np.where(m_wo[..., None], c, col)
rough = np.where(m_wo, np.clip(0.45 + 0.3 * (edge) + 0.2 * (1 - ao) + 0.1 * n2, 0.1, 1), rough)
# tela: trama fina, mugre de contacto en los pliegues, brillo gastado en lo que sobresale
weave = (np.sin(np.arange(SIZE)[None, :] * 0.9) * np.sin(np.arange(SIZE)[:, None] * 0.9)).astype(np.float32)
c = col * (0.88 + 0.12 * weave[..., None]) * (0.55 + 0.45 * ao[..., None])
c = c * (1 - 0.55 * concave[..., None])
c = c + (n2 - 0.5)[..., None] * 0.08 + edge[..., None] * 0.05
col = np.where(m_fa[..., None], c, col)
rough = np.where(m_fa, np.clip(0.8 + 0.15 * (n2 - 0.5) - 0.25 * edge, 0.3, 1), rough)
# plastico, goma, papel, cristal
c = col * (0.6 + 0.4 * ao[..., None]) * (1 - 0.45 * concave[..., None])
c = c + scr[..., None] * 0.07 + edge[..., None] * 0.08
col = np.where(m_pl[..., None] | m_ru[..., None], c, col)
rough = np.where(m_pl, np.clip(0.42 + 0.25 * n2 + 0.2 * (1 - ao) - 0.2 * scr, 0.15, 1), rough)
rough = np.where(m_ru, np.clip(0.8 + 0.1 * n2, 0, 1), rough)
c = col * (0.8 + 0.2 * ao[..., None]) * (0.95 + 0.1 * n1[..., None])
col = np.where(m_pa[..., None], c, col)
rough = np.where(m_pa, 0.8, rough)
c = col * (0.9 + 0.1 * n2[..., None])
col = np.where(m_gl[..., None], c, col)
rough = np.where(m_gl, np.clip(0.08 + 0.4 * (1 - ao) + 0.1 * scr, 0, 1), rough)
# manchas de suciedad generales en las zonas bajas del objeto: pocas, grandes, suaves
dirt = np.clip((1 - ao) * 1.4 + concave, 0, 1) * np.clip(n1 * 1.8 - 0.5, 0, 1)
col = col * (1 - 0.5 * dirt[..., None]) + dirt_c * 0.5 * dirt[..., None] * 0.35

col = np.clip(col, 0, 1)
rgb = np.clip(lin2srgb(col), 0, 1)
# fuera de las islas: relleno por dilatacion no hace falta (el sangrado de la bake ya cubre el margen); se deja el color
rgba = np.ones((SIZE, SIZE, 4), np.float32)
rgba[..., :3] = rgb


def save_array(path, a, alpha=None):
    img = bpy.data.images.new("out_" + os.path.basename(path), SIZE, SIZE, alpha=True, float_buffer=False)
    img.colorspace_settings.name = "Non-Color"
    px = np.ones((SIZE, SIZE, 4), np.float32)
    px[..., :a.shape[2]] = a
    if alpha is not None:
        px[..., 3] = alpha
    img.pixels.foreach_set(px.ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()


save_array(os.path.join(TEXDIR, NAME + "_BaseColor.png"), rgb)
ms = np.zeros((SIZE, SIZE, 3), np.float32)
ms[..., 0] = np.clip(metal, 0, 1)
save_array(os.path.join(TEXDIR, NAME + "_MetallicSmoothness.png"), ms, alpha=np.clip(1.0 - rough, 0, 1))
save_array(os.path.join(TEXDIR, NAME + "_Normal.png"), np.clip(nm * 0.5 + 0.5, 0, 1))
print("texturas escritas", flush=True)

# ---------- FBX de la baja con UN material
bpy.data.objects.remove(high, do_unlink=True)
baked = bpy.data.materials.new(NAME + "_baked")
baked.use_nodes = True
tex = baked.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = bpy.data.images.load(os.path.join(TEXDIR, NAME + "_BaseColor.png"))
baked.node_tree.links.new(tex.outputs["Color"], baked.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
low.data.materials.clear()
low.data.materials.append(baked)
for p in low.data.polygons:
    p.material_index = 0
low.name = NAME
export(low, os.path.join(FBXDIR, NAME + ".fbx"))
print("MEDIDAS", tuple(round(d, 3) for d in low.dimensions), "tris", sum(len(p.vertices) - 2 for p in low.data.polygons), flush=True)
print("PROCESO_TERMINADO", NAME, flush=True)
