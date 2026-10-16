"""Quita las marcas registradas de la pistola de Meshy (VP70) y pone grabados propios con grafia parecida, sin perder resolucion (2026-10-16).

Entrada: Tools/raw_generated/pistola_vp70/ (MeshyPistolLow.fbx ya girado y escalado en Blender: boca hacia -Y, costados en +-X; PistolaVP_diff/_nor/_ms.png).
Salida: las mismas texturas (se guardan las originales como *_orig.png la primera vez).

  1. Se hornean a la textura (Cycles, EMIT, mismas UV) las zonas de las marcas, definidas en el espacio del objeto (banda de texto de cada costado de la
     corredera, logo de cada cacha), y los textos nuevos proyectados en plano sobre cada costado (renderizados antes a imagen con Workbench).
  2. Con numpy, dentro de las zonas, los trazos de las letras se detectan por contraste local (mas claros que la media de su entorno) y se rellenan con
     la media del metal o polimero de alrededor, en color, normales y metal-suavidad. El resto de la textura no se toca.
  3. Los textos nuevos se pintan en color y metal-suavidad, y su relieve grabado se hornea en las normales (Cycles, NORMAL, con un nodo Bump).

    blender --background --python debrand_pistol.py
"""
import math
import os
import shutil

import bpy
import numpy as np

RAW = r"C:\Users\iftra\My project\Tools\raw_generated\pistola_vp70" + "\\"
TMP = RAW + "debrand_tmp\\"

# zonas en el espacio del objeto (metros)
SLIDE = dict(y0=-0.172, y1=-0.023, z0=0.062, z1=0.106, xmin=0.011)
GRIP = dict(y0=0.016, y1=0.054, z0=-0.062, z1=-0.022, xmin=0.015)
ENGRAVE = (0.60, 0.60, 0.59)                       # color lineal de las letras grabadas (como el original)


# ------------------------------------------------------------------ textos nuevos (render Workbench a imagen)
def render_layout(path, w_m, h_m, items, tris=(), res=4000):
    """items: (texto, altura de mayuscula m, x m desde la izquierda, y m del centro desde abajo, inclinacion, ancho m o None, grosor).
    tris: lista de triangulos [(x,y)x3] dibujados como contorno de 1,1 mm. Fondo negro, trazos blancos."""
    sc = bpy.data.scenes.new("Rotulos")
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "FLAT"
    sc.display.shading.color_type = "SINGLE"
    sc.display.shading.single_color = (1, 1, 1)
    sc.display.shading.background_type = "VIEWPORT"
    sc.display.shading.background_color = (0, 0, 0)
    sc.view_settings.view_transform = "Standard"
    sc.render.film_transparent = False
    rx = res
    ry = max(16, int(round(res * h_m / w_m)))
    sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = rx, ry, 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "BW"
    sc.render.filepath = path
    world = bpy.data.worlds.new("negro")
    world.color = (0, 0, 0)
    sc.world = world
    cam_d = bpy.data.cameras.new("cam")
    cam_d.type = "ORTHO"
    cam_d.ortho_scale = max(w_m, h_m)
    cam = bpy.data.objects.new("cam", cam_d)
    cam.location = (w_m / 2, h_m / 2, 1.0)
    sc.collection.objects.link(cam)
    sc.camera = cam
    for (text, cap, x, y, shear, width, bold) in items:
        cu = bpy.data.curves.new("t", "FONT")
        cu.body = text
        cu.size = cap / 0.727
        cu.shear = shear
        cu.offset = bold
        cu.align_x, cu.align_y = "LEFT", "CENTER"
        cu.resolution_u = 6
        ob = bpy.data.objects.new("t", cu)
        sc.collection.objects.link(ob)
        ob.location = (x, y, 0)
        if width:                                                       # se mide en la escena activa (la de render no se evalua aqui)
            bpy.context.scene.collection.objects.link(ob)
            bpy.context.view_layer.update()
            dg = bpy.context.evaluated_depsgraph_get()
            me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
            xs = [v.co.x for v in me.vertices]
            bpy.data.meshes.remove(me)
            bpy.context.scene.collection.objects.unlink(ob)
            ob.scale.x = width / max(1e-6, (max(xs) - min(xs)))
    for tri in tris:
        import bmesh
        bm = bmesh.new()
        c = np.mean(np.array(tri), axis=0)
        outer = [bm.verts.new((p[0], p[1], 0)) for p in tri]
        inner = [bm.verts.new((c[0] + (p[0] - c[0]) * 0.86, c[1] + (p[1] - c[1]) * 0.86, 0)) for p in tri]
        for i in range(3):
            j = (i + 1) % 3
            bm.faces.new((outer[i], outer[j], inner[j], inner[i]))
        me = bpy.data.meshes.new("tri")
        bm.to_mesh(me)
        bm.free()
        ob = bpy.data.objects.new("tri", me)
        sc.collection.objects.link(ob)
    # la escena de render no es la activa: se renderiza con una ventana de contexto
    win = bpy.context.window_manager.windows[0] if bpy.context.window_manager.windows else None
    if win:
        old = win.scene
        win.scene = sc
        bpy.ops.render.render(write_still=True, scene=sc.name)
        win.scene = old
    else:
        bpy.ops.render.render(write_still=True, scene=sc.name)
    for ob in list(sc.collection.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.scenes.remove(sc)
    return path


def make_text_images():
    os.makedirs(TMP, exist_ok=True)
    W = SLIDE["y1"] - SLIDE["y0"]
    Hh = SLIDE["z1"] - SLIDE["z0"]
    zc_big, zc_small = 0.0906 - SLIDE["z0"], 0.0812 - SLIDE["z0"]
    # costado +X: se lee con la boca a la izquierda (u crece hacia atras)
    def blocks(u_mod, u_brand, dz_brand=0.0):
        return [("Mod.", 0.0034, u_mod, zc_big - 0.0009, 0.38, None, 0.0),
                ("M'19Z", 0.0067, u_mod + 0.0112, zc_big, 0.0, 0.032, 0.00012),
                ("9 MM x 19", 0.0029, u_mod + 0.0155, zc_small - 0.0012, 0.0, 0.019, 0.00006),
                ("GRIMHEIM ARMS GMBH", 0.0043, u_brand, zc_big - 0.0005 + dz_brand, 0.0, 0.066, 0.00008),
                ("GRIMHEIM/S7      MADE IN GERMANY", 0.0031, u_brand, zc_small - 0.0006 + dz_brand, 0.0, 0.066, 0.00005)]
    # +X: Mod. hacia la boca (u 0,013) y la marca delante de las estrias (u 0,067)
    render_layout(TMP + "slide_px.png", W, Hh, blocks(0.013, 0.067))
    # -X: se lee al reves (u crece hacia la boca): marca a la izquierda (atras) y Mod. a la derecha (delante)
    render_layout(TMP + "slide_mx.png", W, Hh, blocks(0.092, 0.013, -0.0045))       # bajo la ventana de expulsion
    Wg = GRIP["y1"] - GRIP["y0"]
    Hg = GRIP["z1"] - GRIP["z0"]
    render_layout(TMP + "grip_px.png", Wg, Hg, [("S7", 0.0062, 0.0085, 0.0115, 0.0, None, 0.00025)],
                  tris=[[(0.004, 0.004), (0.034, 0.004), (0.004, 0.034)]], res=1600)
    render_layout(TMP + "grip_mx.png", Wg, Hg, [("S7", 0.0062, 0.0185, 0.0115, 0.0, None, 0.00025)],
                  tris=[[(0.004, 0.004), (0.034, 0.004), (0.034, 0.034)]], res=1600)


# ------------------------------------------------------------------ nodos
def node_box_mask(nt, xyz, r, side):
    """Mascara 0/1 de una caja en el espacio del objeto, en el costado 'side' (+1 / -1)."""
    def cmp(a, val, op):
        n = nt.nodes.new("ShaderNodeMath")
        n.operation = op
        nt.links.new(a, n.inputs[0])
        n.inputs[1].default_value = val
        return n.outputs[0]

    def mul(a, b):
        n = nt.nodes.new("ShaderNodeMath")
        n.operation = "MULTIPLY"
        nt.links.new(a, n.inputs[0])
        nt.links.new(b, n.inputs[1])
        return n.outputs[0]
    x, y, z = xyz.outputs[0], xyz.outputs[1], xyz.outputs[2]
    sx = nt.nodes.new("ShaderNodeMath")
    sx.operation = "MULTIPLY"
    nt.links.new(x, sx.inputs[0])
    sx.inputs[1].default_value = side
    m = cmp(sx.outputs[0], r["xmin"], "GREATER_THAN")
    m = mul(m, cmp(y, r["y0"], "GREATER_THAN"))
    m = mul(m, cmp(y, r["y1"], "LESS_THAN"))
    m = mul(m, cmp(z, r["z0"], "GREATER_THAN"))
    m = mul(m, cmp(z, r["z1"], "LESS_THAN"))
    return m


def node_projected(nt, xyz, r, side, img):
    """Imagen proyectada en plano sobre la zona r del costado 'side' (u a lo largo de Y, al reves en -X; v a lo largo de Z)."""
    def lin(a, sub, div, flip):
        n1 = nt.nodes.new("ShaderNodeMath")
        n1.operation = "SUBTRACT"
        nt.links.new(a, n1.inputs[0])
        n1.inputs[1].default_value = sub
        n2 = nt.nodes.new("ShaderNodeMath")
        n2.operation = "DIVIDE"
        nt.links.new(n1.outputs[0], n2.inputs[0])
        n2.inputs[1].default_value = div
        if not flip:
            return n2.outputs[0]
        n3 = nt.nodes.new("ShaderNodeMath")
        n3.operation = "SUBTRACT"
        n3.inputs[0].default_value = 1.0
        nt.links.new(n2.outputs[0], n3.inputs[1])
        return n3.outputs[0]
    u = lin(xyz.outputs[1], r["y0"], r["y1"] - r["y0"], side < 0)
    v = lin(xyz.outputs[2], r["z0"], r["z1"] - r["z0"], False)
    comb = nt.nodes.new("ShaderNodeCombineXYZ")
    nt.links.new(u, comb.inputs[0])
    nt.links.new(v, comb.inputs[1])
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.image.colorspace_settings.name = "Non-Color"
    tex.extension = "CLIP"
    tex.interpolation = "Cubic"
    nt.links.new(comb.outputs[0], tex.inputs[0])
    m = node_box_mask(nt, xyz, r, side)
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    nt.links.new(tex.outputs[0], mul.inputs[0])
    nt.links.new(m, mul.inputs[1])
    return mul.outputs[0]


def add_max(nt, outs):
    cur = outs[0]
    for o2 in outs[1:]:
        n = nt.nodes.new("ShaderNodeMath")
        n.operation = "MAXIMUM"
        nt.links.new(cur, n.inputs[0])
        nt.links.new(o2, n.inputs[1])
        cur = n.outputs[0]
    return cur


def text_height(nt, xyz, imgs):
    return add_max(nt, [node_projected(nt, xyz, SLIDE, 1, imgs["slide_px"]), node_projected(nt, xyz, SLIDE, -1, imgs["slide_mx"]),
                        node_projected(nt, xyz, GRIP, 1, imgs["grip_px"]), node_projected(nt, xyz, GRIP, -1, imgs["grip_mx"])])


def bake(o, mat, img, kind):
    o.data.materials.clear()
    o.data.materials.append(mat)
    nt = mat.node_tree
    tgt = nt.nodes.new("ShaderNodeTexImage")
    tgt.image = img
    for n in nt.nodes:
        n.select = False
    tgt.select = True
    nt.nodes.active = tgt
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 1
    sc.cycles.use_denoising = False
    sc.render.bake.margin = 12
    if kind == "NORMAL":
        sc.render.bake.normal_space = "TANGENT"
        bpy.ops.object.bake(type="NORMAL", margin=12)
    else:
        bpy.ops.object.bake(type="EMIT", margin=12)


def img_np(img):
    w, h = img.size
    a = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(h, w, 4)


def np_img(name, arr, path, noncolor):
    h, w = arr.shape[:2]
    im = bpy.data.images.new(name, w, h, alpha=True, float_buffer=False)
    if noncolor:
        im.colorspace_settings.name = "Non-Color"
    im.alpha_mode = "STRAIGHT"
    im.pixels.foreach_set(arr.astype(np.float32).ravel())
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    return im


def box_blur(a, r):
    """Media en una ventana (2r+1)^2 por sumas acumuladas (a: HxW o HxWxC)."""
    pad = [(r + 1, r), (r + 1, r)] + [(0, 0)] * (a.ndim - 2)
    p = np.pad(a, pad, mode="edge")
    c = p.cumsum(0).cumsum(1)
    k = 2 * r + 1
    s = c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]
    return s / float(k * k)


def resize_mask(m, shape):
    h, w = shape
    if m.shape == (h, w):
        return m
    fy, fx = m.shape[0] // h, m.shape[1] // w
    if fy >= 1 and fx >= 1 and m.shape[0] == h * fy:
        return m.reshape(h, fy, w, fx).max(axis=(1, 3))
    ys = (np.arange(h) * m.shape[0] / h).astype(int)
    xs = (np.arange(w) * m.shape[1] / w).astype(int)
    return m[ys][:, xs]


def inpaint(arr, hole, r=10):
    """Rellena 'hole' (bool) con la media de los pixeles que no son hueco alrededor (dos pasadas: radio r y 3r)."""
    keep = (~hole).astype(np.float32)
    out = arr.copy()
    for rr in (r, 3 * r):
        num = box_blur(arr * keep[..., None], rr)
        den = box_blur(keep, rr)[..., None]
        fill = num / np.maximum(den, 1e-4)
        sel = hole & (den[..., 0] > 0.02)
        out[sel] = fill[sel]
        hole = hole & ~sel
        if not hole.any():
            break
    return out


def flatten_grip_logo(o):
    """El logo de la cacha esta modelado en relieve (placa y letras): se alisan los vertices de la zona solo en X (lo que sale de la cacha), con el
    borde fijo, y se guarda el FBX resultante."""
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(o.data)
    g = GRIP
    zone = [v for v in bm.verts if abs(v.co.x) > 0.012 and g["y0"] - 0.004 < v.co.y < g["y1"] + 0.004 and g["z0"] - 0.004 < v.co.z < g["z1"] + 0.004]
    inner = [v for v in zone if all(e.other_vert(v) in set(zone) for e in v.link_edges)]
    for _ in range(250):
        bmesh.ops.smooth_vert(bm, verts=inner, factor=0.9, use_axis_x=True, use_axis_y=False, use_axis_z=False)
    bm.to_mesh(o.data)
    bm.free()
    o.data.update()
    print("vertices alisados en la cacha", len(inner), flush=True)
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(filepath=RAW + "MeshyPistolLow.fbx", use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", path_mode="STRIP", bake_anim=False)


def run():
    os.makedirs(TMP, exist_ok=True)
    for f in ("PistolaVP_diff.png", "PistolaVP_nor.png", "PistolaVP_ms.png"):
        orig = RAW + f.replace(".png", "_orig.png")
        if not os.path.exists(orig):
            shutil.copyfile(RAW + f, orig)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    make_text_images()
    if not os.path.exists(RAW + "MeshyPistolLow_orig.fbx"):
        shutil.copyfile(RAW + "MeshyPistolLow.fbx", RAW + "MeshyPistolLow_orig.fbx")
    bpy.ops.import_scene.fbx(filepath=RAW + "MeshyPistolLow_orig.fbx")
    o = [x for x in bpy.data.objects if x.type == "MESH"][0]
    flatten_grip_logo(o)
    diff = bpy.data.images.load(RAW + "PistolaVP_diff_orig.png")
    nor = bpy.data.images.load(RAW + "PistolaVP_nor_orig.png")
    nor.colorspace_settings.name = "Non-Color"
    ms = bpy.data.images.load(RAW + "PistolaVP_ms_orig.png")
    ms.colorspace_settings.name = "Non-Color"
    imgs = {k: bpy.data.images.load(TMP + k + ".png") for k in ("slide_px", "slide_mx", "grip_px", "grip_mx")}
    W, H = diff.size
    print("texturas", diff.size[:], nor.size[:], ms.size[:], flush=True)

    # 1) mascaras a la textura: R zona corredera, G zona cacha, B textos nuevos
    mat = bpy.data.materials.new("mascaras")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    tc = nt.nodes.new("ShaderNodeTexCoord")
    xyz = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Object"], xyz.inputs[0])
    rs = add_max(nt, [node_box_mask(nt, xyz, SLIDE, 1), node_box_mask(nt, xyz, SLIDE, -1)])
    rg = add_max(nt, [node_box_mask(nt, xyz, GRIP, 1), node_box_mask(nt, xyz, GRIP, -1)])
    ring = dict(y0=GRIP["y0"] - 0.012, y1=GRIP["y1"] + 0.004, z0=GRIP["z0"], z1=GRIP["z1"] + 0.02, xmin=GRIP["xmin"])
    rr = add_max(nt, [node_box_mask(nt, xyz, ring, 1), node_box_mask(nt, xyz, ring, -1)])
    rg2 = nt.nodes.new("ShaderNodeMath")                                 # G: 1 zona del logo, 0,5 franja de cacha alrededor (para el tono)
    rg2.operation = "MULTIPLY_ADD"
    nt.links.new(rr, rg2.inputs[0])
    rg2.inputs[1].default_value = 0.5
    nt.links.new(rg, rg2.inputs[2])
    rg_mix = nt.nodes.new("ShaderNodeMath")
    rg_mix.operation = "MINIMUM"
    nt.links.new(rg2.outputs[0], rg_mix.inputs[0])
    rg_mix.inputs[1].default_value = 1.0
    rg = rg_mix.outputs[0]
    tx = text_height(nt, xyz, imgs)
    comb = nt.nodes.new("ShaderNodeCombineColor")
    nt.links.new(rs, comb.inputs[0])
    nt.links.new(rg, comb.inputs[1])
    nt.links.new(tx, comb.inputs[2])
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(comb.outputs[0], em.inputs[0])
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs[0])
    mimg = bpy.data.images.new("mascaras", W, H, alpha=False, float_buffer=True)
    mimg.colorspace_settings.name = "Non-Color"
    bake(o, mat, mimg, "EMIT")
    M = img_np(mimg)
    zone_s, zone_g, text = M[..., 0] > 0.5, M[..., 1] > 0.9, np.clip(M[..., 2], 0, 1)
    ring_g = (M[..., 1] > 0.3) & (M[..., 1] < 0.9)
    print("pixeles zona corredera", int(zone_s.sum()), "cacha", int(zone_g.sum()), "texto", int((text > 0.5).sum()), flush=True)

    # 2) letras viejas por contraste local y relleno
    D = img_np(diff)
    lum = D[..., 0] * 0.3 + D[..., 1] * 0.59 + D[..., 2] * 0.11
    mean = box_blur(lum, 14)
    letters = ((lum - mean) > 0.06) & (zone_s | zone_g)
    # cacha: polimero liso. Se marca todo lo que se aparta del tono mediano del polimero (letras, contorno y placa oscura del logo)
    gmed_c = np.median(D[..., :3][ring_g], axis=0)                       # tono del polimero alrededor del logo
    print("tono del polimero", np.round(gmed_c, 3), flush=True)
    letters = letters | zone_g                                          # toda la zona del logo pasa a polimero liso (color, normal y metal)
    letters_d = box_blur(letters.astype(np.float32), 2) > 0.02           # algo dilatado para el halo
    print("pixeles de letra", int(letters.sum()), flush=True)
    Dc = D.copy()
    Dc[..., :3] = inpaint(D[..., :3], letters_d & ~zone_g, 10)
    zg_d = box_blur(zone_g.astype(np.float32), 2) > 0.02
    Dc[..., :3][zg_d] = gmed_c                                         # cacha: tono del polimero (sin mezclar con otras islas del atlas)
    # 3) textos nuevos en el color
    t = text[..., None]
    Dc[..., :3] = Dc[..., :3] * (1 - t) + np.array(ENGRAVE)[None, None, :] * t
    Dc[..., 3] = 1.0
    np_img("PistolaVP_diff", Dc, RAW + "PistolaVP_diff.png", False)

    # metal-suavidad
    S = img_np(ms)
    hole_s = resize_mask(letters_d, S.shape[:2])
    Sc = S.copy()
    zs_s = resize_mask(zone_s, S.shape[:2])
    zg_s = resize_mask(zg_d, S.shape[:2])
    ring_s = resize_mask(ring_g, S.shape[:2]) & ~zg_s
    for ch in (0, 3):                                                   # metal y suavidad: valor mediano de cada zona fuera de las letras
        Sc[..., ch][hole_s & zs_s & ~zg_s] = float(np.median(S[..., ch][zs_s & ~hole_s]))
        Sc[..., ch][zg_s] = float(np.median(S[..., ch][ring_s]))
    Sc[..., 1] = Sc[..., 0]
    Sc[..., 2] = Sc[..., 0]
    ts = resize_mask(text, S.shape[:2])
    Sc[..., 0:3] = Sc[..., 0:3] * (1 - ts[..., None]) + 0.15 * ts[..., None]
    Sc[..., 3] = Sc[..., 3] * (1 - ts) + 0.35 * ts
    np_img("PistolaVP_ms", Sc, RAW + "PistolaVP_ms.png", True)

    # normales: letras viejas rellenas y relieve de las nuevas horneado con Bump
    N = img_np(nor)
    hole_n = resize_mask(letters_d, N.shape[:2])
    Nc = N.copy()
    Nc[..., :3][hole_n] = (0.5, 0.5, 1.0)                               # normal plana de la superficie donde estaban las letras
    v = Nc[..., :3] * 2 - 1
    v /= np.maximum(np.linalg.norm(v, axis=2, keepdims=True), 1e-5)
    Nc[..., :3] = v * 0.5 + 0.5
    Nc[..., 3] = 1.0
    nclean = np_img("nor_limpia", Nc, TMP + "nor_limpia.png", True)
    mat2 = bpy.data.materials.new("relieve")
    mat2.use_nodes = True
    nt = mat2.node_tree
    nt.nodes.clear()
    tc = nt.nodes.new("ShaderNodeTexCoord")
    xyz = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Object"], xyz.inputs[0])
    tx = text_height(nt, xyz, imgs)
    nimg = nt.nodes.new("ShaderNodeTexImage")
    nimg.image = nclean
    nmap = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(nimg.outputs[0], nmap.inputs["Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.invert = True                                                  # grabado (hundido)
    bump.inputs["Strength"].default_value = 0.8
    bump.inputs["Distance"].default_value = 0.0004
    nt.links.new(tx, bump.inputs["Height"])
    nt.links.new(nmap.outputs[0], bump.inputs["Normal"])
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(bump.outputs[0], bsdf.inputs["Normal"])
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(bsdf.outputs[0], out.inputs[0])
    nw, nh = nor.size
    nout = bpy.data.images.new("PistolaVP_nor", nw, nh, alpha=False, float_buffer=False)
    nout.colorspace_settings.name = "Non-Color"
    bake(o, mat2, nout, "NORMAL")
    nout.filepath_raw = RAW + "PistolaVP_nor.png"
    nout.file_format = "PNG"
    nout.save()
    print("PROCESO_TERMINADO", flush=True)


if __name__ == "__main__":
    run()
