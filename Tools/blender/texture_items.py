"""Mejora las texturas de los objetos recogibles horneando materiales procedurales en Blender (Cycles).

Dos modos:

  blender -b --python texture_items.py -- blend <fbx> <carpeta_texturas> <nombre> [tam=1024]
      Para los objetos hechos en Blender (colores planos sin textura): desenvuelve UV, crea por cada material un material
      procedural segun su nombre (metal arañado con suciedad en los huecos y bordes gastados, nailon tejido, plastico rozado...),
      hornea Color, Rugosidad, Metal y Normales a texturas y reexporta el FBX (mismos ejes y escala) con UN material
      "<nombre>_baked". Las texturas: <nombre>_BaseColor.png, <nombre>_MetallicSmoothness.png (R metal, A suavidad), <nombre>_Normal.png.

  blender -b --python texture_items.py -- maps <fbx> <carpeta_texturas> <nombre> <basecolor.png> <tipo> [madera]
      Para los modelos de Meshy (ya tienen color): con sus UV hornea Normales y Metal/Suavidad a partir de la textura de color
      segun el tipo (arma, carton, plastico, bote). Con 'madera' tambien recolorea las zonas naranjas como madera con veta
      (<nombre>_BaseColor.png). No toca la malla.
"""
import math
import os
import sys

import bpy
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
MODE, FBX, OUT, NAME = args[:4]
if MODE == "blend":
    SIZE = int(args[4]) if len(args) > 4 else 1024
else:
    BASECOLOR, KIND = args[4], args[5]
    WOOD = "madera" in args[6:]
    SIZE = 2048

os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX)
sc = bpy.context.scene
sc.render.engine = "CYCLES"
sc.cycles.device = "CPU"
sc.cycles.samples = 16
sc.render.bake.margin = 8
meshes = [o for o in sc.objects if o.type == "MESH"]


# ------------------------------------------------------------------ nodos
class G:
    """Ayudante para montar redes de nodos con poco codigo."""

    def __init__(self, mat):
        self.t = mat.node_tree
        self.n = self.t.nodes
        self.l = self.t.links
        self.x = -1600

    def node(self, kind, **props):
        nd = self.n.new(kind)
        nd.location = (self.x, 0)
        self.x += 40
        for k, v in props.items():
            setattr(nd, k, v)
        return nd

    def link(self, a, b):
        self.l.new(a, b)

    def val(self, v):
        nd = self.node("ShaderNodeValue")
        nd.outputs[0].default_value = v
        return nd.outputs[0]

    def math(self, op, a, b=None, clamp=False):
        nd = self.node("ShaderNodeMath", operation=op, use_clamp=clamp)
        for i, s in enumerate((a, b)):
            if s is None:
                continue
            if isinstance(s, (int, float)):
                nd.inputs[i].default_value = s
            else:
                self.link(s, nd.inputs[i])
        return nd.outputs[0]

    def vmath(self, op, a, b=None, scale=None):
        nd = self.node("ShaderNodeVectorMath", operation=op)
        for i, s in enumerate((a, b)):
            if s is None:
                continue
            if isinstance(s, (tuple, list)):
                nd.inputs[i].default_value = s
            else:
                self.link(s, nd.inputs[i])
        if scale is not None:
            if isinstance(scale, (int, float)):
                nd.inputs[3].default_value = scale
            else:
                self.link(scale, nd.inputs[3])
        return nd.outputs[0]

    def lerp(self, a, b, f):           # a + (b - a) * f   (colores como vectores)
        return self.vmath("ADD", a, self.vmath("SCALE", self.vmath("SUBTRACT", b, a), scale=f))

    def coords(self, scale=(1, 1, 1)):
        tc = self.node("ShaderNodeTexCoord")
        mp = self.node("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = scale
        self.link(tc.outputs["Object"], mp.inputs["Vector"])
        return mp.outputs["Vector"]

    def noise(self, scale, detail=4.0, rough=0.55, vec=None):
        nd = self.node("ShaderNodeTexNoise")
        nd.inputs["Scale"].default_value = scale
        nd.inputs["Detail"].default_value = detail
        nd.inputs["Roughness"].default_value = rough
        self.link(vec if vec is not None else self.coords(), nd.inputs["Vector"])
        return nd.outputs["Fac"]

    def wave(self, scale, direction, distortion=0.0, vec=None, kind="BANDS"):
        nd = self.node("ShaderNodeTexWave", wave_type=kind)
        if kind == "BANDS":
            nd.bands_direction = direction
        else:
            nd.rings_direction = direction
        nd.inputs["Scale"].default_value = scale
        nd.inputs["Distortion"].default_value = distortion
        self.link(vec if vec is not None else self.coords(), nd.inputs["Vector"])
        return nd.outputs["Fac"]

    def ao(self, dist):
        nd = self.node("ShaderNodeAmbientOcclusion", samples=16)
        nd.inputs["Distance"].default_value = dist
        return nd.outputs["AO"]

    def pointiness(self):
        return self.node("ShaderNodeNewGeometry").outputs["Pointiness"]

    def color_const(self, rgb):
        nd = self.node("ShaderNodeRGB")
        nd.outputs[0].default_value = (*rgb, 1.0)
        return nd.outputs[0]


def kind_of(name):
    n = name.lower()
    if any(k in n for k in ("stone", "marble")):        # memorial (2026-10-08): piedra con vetas
        return "stone"
    if any(k in n for k in ("wood", "board")):
        return "wood"
    if "cardboard" in n:
        return "cardboard"
    if any(k in n for k in ("rust", "iron")):
        return "rust"
    if "body" in n:                                   # taquilla, archivador, bidon: chapa pintada
        return "painted"
    if any(k in n for k in ("brass", "gold", "steel", "metal", "chip", "handle", "can", "frame")):
        return "metal"
    if any(k in n for k in ("nylon", "webbing", "seam", "mattress", "blanket", "pillow", "fabric")):
        return "fabric"
    return "plastic"


def build_material(mat, base_rgb, base_rough, name):
    """Sustituye la red del material por una procedural. Devuelve (principled, metal_socket, color_socket)."""
    mat.use_nodes = True
    g = G(mat)
    g.n.clear()
    out = g.node("ShaderNodeOutputMaterial")
    bsdf = g.node("ShaderNodeBsdfPrincipled")
    g.link(bsdf.outputs["BSDF"], out.inputs["Surface"])
    kind = kind_of(name)
    base = g.color_const(base_rgb)
    grime = g.math("SUBTRACT", 1.0, g.ao(0.012 if kind != "fabric" else 0.02), clamp=True)          # huecos
    edge = g.math("MULTIPLY", g.math("SUBTRACT", g.pointiness(), 0.52), 6.0, clamp=True)             # bordes vivos
    n1 = g.noise(55.0, 6.0, 0.6)                                                                       # manchas
    n2 = g.noise(9.0, 3.0, 0.5)                                                                        # variacion grande
    if kind == "wood":
        # veta: lineas a lo largo de la tabla. En las caras horizontales y laterales, bandas segun Y; en las caras cuya normal
        # es Y, segun Z (si no, esas caras saldrian lisas). Poca distorsion: con mucha sale un moteado tipo corcho.
        tc = g.node("ShaderNodeTexCoord")
        sepn = g.node("ShaderNodeSeparateXYZ")
        g.link(tc.outputs["Normal"], sepn.inputs["Vector"])
        ny = g.math("ABSOLUTE", sepn.outputs["Y"])
        gv = g.coords((0.35, 1.0, 1.0))
        gy = g.wave(14.0, "Y", 5.0, gv)
        gz = g.wave(14.0, "Z", 5.0, gv)
        grain = g.math("ADD", gy, g.math("MULTIPLY", g.math("SUBTRACT", gz, gy), ny))
        fy = g.wave(70.0, "Y", 1.2, gv)
        fz = g.wave(70.0, "Z", 1.2, gv)
        fine = g.math("ADD", fy, g.math("MULTIPLY", g.math("SUBTRACT", fz, fy), ny))
        col = g.lerp(g.vmath("SCALE", base, scale=0.66), g.vmath("SCALE", base, scale=1.1), grain)
        col = g.vmath("SCALE", col, scale=g.math("ADD", 0.9, g.math("MULTIPLY", fine, 0.14)))
        col = g.vmath("SCALE", col, scale=g.math("ADD", 0.92, g.math("MULTIPLY", n2, 0.16)))   # tablas algo distintas entre si
        streaks = g.noise(1.0, 4.0, 0.6, g.coords((1.2, 22.0, 22.0)))                          # vetas alargadas de tono distinto
        col = g.vmath("SCALE", col, scale=g.math("ADD", 0.78, g.math("MULTIPLY", streaks, 0.4)))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.38), g.math("MULTIPLY", grime, 0.65))
        col = g.lerp(col, g.vmath("ADD", g.vmath("SCALE", base, scale=0.9), (0.1, 0.08, 0.05)), g.math("MULTIPLY", edge, 0.55))   # barniz gastado
        rough = g.math("ADD", 0.45, g.math("MULTIPLY", n2, 0.15))
        rough = g.math("ADD", rough, g.math("MULTIPLY", edge, 0.3))
        rough = g.math("ADD", rough, g.math("MULTIPLY", grime, 0.2))
        metal = g.val(0.0)
        height = g.math("ADD", g.math("MULTIPLY", grain, 0.4), g.math("MULTIPLY", fine, 0.3))
        bump_strength = 0.18
    elif kind == "painted":
        # chapa pintada: desconchones en los bordes que dejan ver el metal, oxido en huecos y chorretones verticales
        chips_n = g.noise(26.0, 6.0, 0.7)
        chips = g.math("GREATER_THAN", g.math("ADD", g.math("MULTIPLY", edge, 0.55), g.math("MULTIPLY", chips_n, 0.75)), 0.78)
        streak_v = g.coords((14.0, 14.0, 1.2))                                                       # alargado en vertical
        streak = g.math("GREATER_THAN", g.noise(1.0, 3.0, 0.5, streak_v), 0.6)
        rust_mask = g.math("MINIMUM", g.math("ADD", g.math("MULTIPLY", grime, 0.9), g.math("MULTIPLY", streak, 0.45)), 1.0)
        paint = g.vmath("SCALE", base, scale=g.math("ADD", 0.86, g.math("MULTIPLY", n1, 0.22)))
        rust_col = g.lerp(g.color_const((0.25, 0.1, 0.04)), g.color_const((0.45, 0.2, 0.07)), n1)
        col = g.lerp(paint, rust_col, g.math("MULTIPLY", rust_mask, 0.75))
        col = g.lerp(col, g.color_const((0.5, 0.5, 0.52)), chips)                                   # metal desnudo
        rough = g.math("ADD", 0.48, g.math("MULTIPLY", n2, 0.15))
        rough = g.math("ADD", rough, g.math("MULTIPLY", rust_mask, 0.35))
        rough = g.math("SUBTRACT", rough, g.math("MULTIPLY", chips, 0.2), clamp=True)
        metal = g.math("MULTIPLY", chips, 0.9)
        height = g.math("ADD", g.math("MULTIPLY", chips, -0.5), g.math("MULTIPLY", n1, 0.2))
        bump_strength = 0.2
    elif kind == "rust":
        r1 = g.noise(18.0, 8.0, 0.65)
        col = g.lerp(g.color_const((0.18, 0.08, 0.04)), g.color_const((0.5, 0.24, 0.08)), r1)
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.8), g.math("MULTIPLY", n2, 0.4))          # restos del color original
        col = g.lerp(col, g.color_const((0.1, 0.06, 0.04)), g.math("MULTIPLY", grime, 0.6))
        rough = g.math("ADD", 0.78, g.math("MULTIPLY", r1, 0.15))
        metal = g.math("MULTIPLY", g.math("SUBTRACT", 1.0, r1), 0.35)
        height = g.math("MULTIPLY", g.noise(90.0, 6.0, 0.7), 0.7)
        bump_strength = 0.35
    elif kind == "cardboard":
        fib = g.noise(260.0, 3.0, 0.5)
        stain = g.math("GREATER_THAN", g.noise(4.0, 2.0, 0.5), 0.7)
        col = g.vmath("SCALE", base, scale=g.math("ADD", 0.93, g.math("MULTIPLY", fib, 0.1)))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.75), g.math("MULTIPLY", stain, 0.3))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.45), g.math("MULTIPLY", grime, 0.6))
        rough = g.math("ADD", 0.86, g.math("MULTIPLY", fib, 0.1))
        metal = g.val(0.0)
        height = g.math("MULTIPLY", fib, 0.4)
        bump_strength = 0.15
    elif kind == "metal":
        if any(k in name.lower() for k in ("brass", "gold", "chip")):
            base = g.vmath("MULTIPLY", base, base)                                                        # laton/oro: mas saturado y profundo
        scratch_v = g.coords((260.0, 260.0, 14.0))                                                     # arañazos alargados
        scratch = g.math("GREATER_THAN", g.noise(1.0, 2.0, 0.4, scratch_v), 0.62)
        tint = g.math("ADD", 0.78, g.math("MULTIPLY", n1, 0.38))
        col = g.vmath("SCALE", base, scale=tint)
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.35), g.math("MULTIPLY", grime, 0.75))           # mugre en los huecos
        col = g.lerp(col, g.vmath("ADD", base, (0.18, 0.18, 0.18)), g.math("MULTIPLY", edge, 0.6))      # bordes pulidos
        col = g.lerp(col, g.vmath("ADD", base, (0.12, 0.12, 0.12)), g.math("MULTIPLY", scratch, 0.5))
        rough = g.math("ADD", base_rough, g.math("MULTIPLY", n2, 0.25))
        rough = g.math("ADD", rough, g.math("MULTIPLY", grime, 0.35))
        rough = g.math("SUBTRACT", rough, g.math("MULTIPLY", scratch, 0.12), clamp=True)
        metal = g.val(1.0)
        height = g.math("ADD", g.math("MULTIPLY", n1, 0.4), g.math("MULTIPLY", scratch, -0.25))
        bump_strength = 0.18
    elif kind == "stone":
        # piedra pulida y gastada: vetas finas retorcidas, poros, mugre en las juntas y bordes algo mas claros
        vein = g.math("GREATER_THAN", g.wave(6.0, "X", 14.0, g.coords((1, 1, 1))), 0.93)
        pores = g.noise(320.0, 2.0, 0.5)
        col = g.vmath("SCALE", base, scale=g.math("ADD", 0.86, g.math("MULTIPLY", n2, 0.24)))
        col = g.vmath("SCALE", col, scale=g.math("ADD", 0.94, g.math("MULTIPLY", pores, 0.1)))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.62), g.math("MULTIPLY", vein, 0.55))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.4), g.math("MULTIPLY", grime, 0.7))
        col = g.lerp(col, g.vmath("ADD", base, (0.08, 0.08, 0.08)), g.math("MULTIPLY", edge, 0.5))
        rough = g.math("ADD", base_rough, g.math("MULTIPLY", n1, 0.18))
        rough = g.math("ADD", rough, g.math("MULTIPLY", grime, 0.2))
        metal = g.val(0.0)
        height = g.math("ADD", g.math("MULTIPLY", pores, 0.25), g.math("MULTIPLY", vein, -0.2))
        bump_strength = 0.12
    elif kind == "fabric":
        vec = g.coords((1, 1, 1))
        ws = 140.0 if any(k in name.lower() for k in ("blanket", "mattress", "pillow")) else 380.0
        weave = g.math("MULTIPLY", g.wave(ws, "X", 0.6, vec), g.wave(ws, "Y", 0.6, vec))
        col = g.vmath("SCALE", base, scale=g.math("ADD", 0.72, g.math("MULTIPLY", weave, 0.45)))
        col = g.vmath("SCALE", col, scale=g.math("ADD", 0.85, g.math("MULTIPLY", n2, 0.3)))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.4), g.math("MULTIPLY", grime, 0.6))
        dust = g.math("MULTIPLY", g.math("GREATER_THAN", g.noise(14.0, 4.0, 0.6), 0.58), 0.25)
        col = g.lerp(col, g.color_const((0.55, 0.52, 0.47)), dust)                                    # polvo
        col = g.lerp(col, g.vmath("ADD", base, (0.1, 0.1, 0.1)), g.math("MULTIPLY", edge, 0.5))        # roce en los bordes
        rough = g.math("ADD", 0.82, g.math("MULTIPLY", n1, 0.12))
        metal = g.val(0.0)
        height = weave
        bump_strength = 0.45
    else:
        scuff = g.math("GREATER_THAN", g.noise(160.0, 3.0, 0.5), 0.64)
        col = g.vmath("SCALE", base, scale=g.math("ADD", 0.9, g.math("MULTIPLY", n1, 0.18)))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.45), g.math("MULTIPLY", grime, 0.6))
        col = g.lerp(col, g.vmath("ADD", g.vmath("SCALE", base, scale=0.85), (0.12, 0.12, 0.12)), g.math("MULTIPLY", edge, 0.55))
        col = g.lerp(col, g.vmath("SCALE", base, scale=0.8), g.math("MULTIPLY", scuff, 0.35))
        if "plastic" in name.lower() and sum(base_rgb) > 2.4:                                          # tarjeta blanca: trama de seguridad
            guil = g.wave(220.0, "X", 4.0, g.coords((1, 1, 1)), kind="RINGS")
            col = g.lerp(col, g.vmath("SCALE", base, scale=0.86), g.math("MULTIPLY", g.math("GREATER_THAN", guil, 0.8), 0.6))
        rough = g.math("ADD", base_rough, g.math("MULTIPLY", scuff, 0.2))
        rough = g.math("ADD", rough, g.math("MULTIPLY", grime, 0.2))
        metal = g.val(0.0)
        height = g.math("ADD", g.math("MULTIPLY", n1, 0.3), g.math("MULTIPLY", scuff, -0.2))
        bump_strength = 0.1
    bump = g.node("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.0015
    g.link(height, bump.inputs["Height"])
    g.link(col, bsdf.inputs["Base Color"])
    g.link(rough, bsdf.inputs["Roughness"])
    g.link(metal, bsdf.inputs["Metallic"])
    g.link(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return bsdf, metal, col


def base_of(mat):
    rgb, rough = (0.5, 0.5, 0.5), 0.5
    if mat.use_nodes:
        for nd in mat.node_tree.nodes:
            if nd.type == "BSDF_PRINCIPLED":
                c = nd.inputs["Base Color"].default_value
                rgb, rough = (c[0], c[1], c[2]), nd.inputs["Roughness"].default_value
    else:
        rgb = tuple(mat.diffuse_color[:3])
    return rgb, rough


# ------------------------------------------------------------------ horneado
def new_image(suffix, alpha=False, non_color=False):
    img = bpy.data.images.new(NAME + "_" + suffix, SIZE, SIZE, alpha=alpha)
    if non_color:
        img.colorspace_settings.name = "Non-Color"
    return img


def set_target(obj, img):
    for slot in obj.material_slots:
        t = slot.material.node_tree
        nd = t.nodes.get("BakeTarget") or t.nodes.new("ShaderNodeTexImage")
        nd.name = "BakeTarget"
        nd.image = img
        for o in t.nodes:
            o.select = False
        nd.select = True
        t.nodes.active = nd


def bake(obj, kind, img, **kw):
    set_target(obj, img)
    bpy.ops.object.bake(type=kind, use_clear=True, margin=8, **kw)


def bake_emit(obj, img, metal_sockets):
    """Hornea un valor cualquiera (color base o metal) conectandolo a una emision temporal y horneando EMIT
    (el horneado DIFFUSE deja negro todo lo metalico: un metal no tiene color difuso)."""
    saved = []
    for slot, sock in zip(obj.material_slots, metal_sockets):
        t = slot.material.node_tree
        out = next(n for n in t.nodes if n.type == "OUTPUT_MATERIAL")
        prev = out.inputs["Surface"].links[0].from_socket
        em = t.nodes.new("ShaderNodeEmission")
        t.links.new(sock, em.inputs["Color"])
        t.links.new(em.outputs["Emission"], out.inputs["Surface"])
        saved.append((t, out, prev, em))
    bake(obj, "EMIT", img)
    for t, out, prev, em in saved:
        t.links.new(prev, out.inputs["Surface"])
        t.nodes.remove(em)


def save(img, path):
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()


def metallic_smoothness(metal_img, rough_img, path):
    m = np.array(metal_img.pixels[:]).reshape(SIZE, SIZE, 4)
    r = np.array(rough_img.pixels[:]).reshape(SIZE, SIZE, 4)
    o = np.zeros((SIZE, SIZE, 4), dtype=np.float32)
    o[..., 0] = m[..., 0]
    o[..., 3] = 1.0 - r[..., 0]
    img = bpy.data.images.new(NAME + "_MetallicSmoothness", SIZE, SIZE, alpha=True)
    img.colorspace_settings.name = "Non-Color"
    img.pixels[:] = o.ravel()
    save(img, path)


def finish(obj, extra_images):
    print("PROCESO_TERMINADO", NAME, flush=True)


if MODE == "blend":
    # una sola malla (por si el FBX trae varias) con UV propias
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    metal_sockets, color_sockets = [], []
    for slot in obj.material_slots:
        rgb, rough = base_of(slot.material)
        _, metal, colsock = build_material(slot.material, rgb, rough, slot.material.name)
        metal_sockets.append(metal)
        color_sockets.append(colsock)
    col_img, rough_img, nrm_img = new_image("BaseColor"), new_image("Roughness", non_color=True), new_image("Normal", non_color=True)
    metal_img = new_image("Metal", non_color=True)
    bake_emit(obj, col_img, color_sockets)
    bake(obj, "ROUGHNESS", rough_img)
    bake(obj, "NORMAL", nrm_img, normal_space="TANGENT")
    bake_emit(obj, metal_img, metal_sockets)
    save(col_img, os.path.join(OUT, NAME + "_BaseColor.png"))
    save(nrm_img, os.path.join(OUT, NAME + "_Normal.png"))
    metallic_smoothness(metal_img, rough_img, os.path.join(OUT, NAME + "_MetallicSmoothness.png"))
    # un unico material con las texturas horneadas
    baked = bpy.data.materials.new(NAME + "_baked")
    baked.use_nodes = True
    tex = baked.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = col_img
    baked.node_tree.links.new(tex.outputs["Color"], baked.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    obj.data.materials.clear()
    obj.data.materials.append(baked)
    for p in obj.data.polygons:
        p.material_index = 0
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", bake_anim=False, path_mode="STRIP")
    dims = obj.dimensions
    print("MEDIDAS", tuple(round(d, 4) for d in dims), flush=True)
    print("PROCESO_TERMINADO", NAME, flush=True)

else:
    obj = meshes[0]
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    img_base = bpy.data.images.load(BASECOLOR)
    mat = bpy.data.materials.new(NAME + "_maps")
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    mat.use_nodes = True
    g = G(mat)
    g.n.clear()
    out = g.node("ShaderNodeOutputMaterial")
    bsdf = g.node("ShaderNodeBsdfPrincipled")
    g.link(bsdf.outputs["BSDF"], out.inputs["Surface"])
    tex = g.node("ShaderNodeTexImage")
    tex.image = img_base
    col = tex.outputs["Color"]
    sep = g.node("ShaderNodeSeparateColor", mode="HSV")
    g.link(col, sep.inputs["Color"])
    hue, sat, valv = sep.outputs[0], sep.outputs[1], sep.outputs[2]
    grey = g.math("LESS_THAN", sat, 0.18)                                      # zonas grises/negras = metal o polimero
    n1 = g.noise(70.0, 5.0, 0.6)
    if KIND == "arma":
        metal = g.math("MULTIPLY", grey, g.math("ADD", 0.55, g.math("MULTIPLY", valv, 0.6)))      # pavonado: metal medio
        rough = g.math("ADD", 0.38, g.math("MULTIPLY", n1, 0.22))
        rough = g.math("ADD", rough, g.math("MULTIPLY", g.math("SUBTRACT", 1.0, grey), 0.25))    # madera/plastico mas mate
    elif KIND == "bote":
        metal = g.math("MULTIPLY", g.math("MULTIPLY", grey, g.math("GREATER_THAN", valv, 0.45)), 0.95)   # tapa y fondo plateados
        rough = g.math("ADD", 0.3, g.math("MULTIPLY", n1, 0.15))
    elif KIND == "carton":
        metal = g.val(0.0)
        rough = g.math("ADD", 0.82, g.math("MULTIPLY", n1, 0.12))
    else:
        metal = g.val(0.0)
        rough = g.math("ADD", 0.5, g.math("MULTIPLY", n1, 0.2))
    # relieve a partir de la luminancia de la propia textura + ruido fino
    height = g.math("ADD", g.math("MULTIPLY", valv, 0.7), g.math("MULTIPLY", n1, 0.3))
    bump = g.node("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35 if KIND != "carton" else 0.5
    bump.inputs["Distance"].default_value = 0.002
    g.link(height, bump.inputs["Height"])
    final_col = col
    if WOOD:
        # zonas naranjas (culata y guardamanos): madera oscura con veta
        orange = g.math("MULTIPLY", g.math("GREATER_THAN", sat, 0.35), g.math("LESS_THAN", hue, 0.13))
        grain_v = g.coords((6.0, 6.0, 90.0))
        grain = g.wave(1.0, "Z", 8.0, grain_v, kind="BANDS")
        wood_a, wood_b = g.color_const((0.23, 0.11, 0.05)), g.color_const((0.45, 0.24, 0.11))
        wood = g.lerp(wood_a, wood_b, grain)
        final_col = g.lerp(col, wood, g.math("MULTIPLY", orange, 0.85))
        rough = g.math("ADD", rough, g.math("MULTIPLY", orange, 0.1))
        height = g.math("ADD", height, g.math("MULTIPLY", g.math("MULTIPLY", grain, orange), 0.5))
        g.link(height, bump.inputs["Height"])
    g.link(final_col, bsdf.inputs["Base Color"])
    g.link(rough, bsdf.inputs["Roughness"])
    g.link(metal, bsdf.inputs["Metallic"])
    g.link(bump.outputs["Normal"], bsdf.inputs["Normal"])
    rough_img, nrm_img, metal_img = new_image("Roughness", non_color=True), new_image("Normal", non_color=True), new_image("Metal", non_color=True)
    bake(obj, "ROUGHNESS", rough_img)
    bake(obj, "NORMAL", nrm_img, normal_space="TANGENT")
    bake_emit(obj, metal_img, [metal])
    save(nrm_img, os.path.join(OUT, NAME + "_Normal.png"))
    metallic_smoothness(metal_img, rough_img, os.path.join(OUT, NAME + "_MetallicSmoothness.png"))
    if WOOD:
        col_img = new_image("BaseColor")
        bake_emit(obj, col_img, [final_col])
        save(col_img, os.path.join(OUT, NAME + "_BaseColor.png"))
    print("PROCESO_TERMINADO", NAME, flush=True)
