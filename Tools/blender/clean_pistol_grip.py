"""Quita el logo de marca (letras en relieve dentro de la placa triangular de las cachas) de la pistola VP70 de Meshy (2026-10-17).

Se hornea la posicion y la normal de cada texel (Cycles, EMIT, mismas UV) para trabajar en la pieza y no en el atlas (troceado en islas). En la caja
de las letras de cada cacha, la normal pasa a plana y el color y el metal-suavidad toman la mediana de la placa alrededor. La placa triangular se queda.

    blender --background --python clean_pistol_grip.py -- analizar      (imprime un mapa de donde hay relieve en cada cacha)
    blender --background --python clean_pistol_grip.py                  (limpia; guarda las texturas originales como *_orig.png la primera vez)
"""
import os
import shutil
import sys

import bpy
import numpy as np

RAW = r"C:\Users\iftra\My project\Tools\raw_generated\pistola_vp70" + "\\"
ZONE = dict(y0=0.014, y1=0.060, z0=-0.068, z1=-0.026)
# caja de las letras de cada cacha (y0, y1, z0, z1), medida con 'analizar'
LETTERS = {1: (0.017, 0.052, -0.0590, -0.0235), -1: (0.017, 0.052, -0.0590, -0.0235)}   # toda la placa triangular con las letras


def img_np(img):
    w, h = img.size
    a = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(h, w, 4)


def bake(o, W, H, which):
    mat = bpy.data.materials.new("bk")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission")
    if which == "pos":
        nt.links.new(nt.nodes.new("ShaderNodeNewGeometry").outputs["Position"], em.inputs[0])   # mundo (el FBX importado lleva rotacion de ejes)
    else:
        nt.links.new(nt.nodes.new("ShaderNodeNewGeometry").outputs["Normal"], em.inputs[0])
    nt.links.new(em.outputs[0], nt.nodes.new("ShaderNodeOutputMaterial").inputs[0])
    img = bpy.data.images.new("bk_" + which, W, H, alpha=False, float_buffer=True)
    img.colorspace_settings.name = "Non-Color"
    t = nt.nodes.new("ShaderNodeTexImage")
    t.image = img
    nt.nodes.active = t
    o.data.materials.clear()
    o.data.materials.append(mat)
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 1
    bpy.ops.object.bake(type="EMIT", margin=4)
    return img_np(img)[..., :3]


def save(arr, path, noncolor):
    h, w = arr.shape[:2]
    im = bpy.data.images.new("out", w, h, alpha=True)
    if noncolor:
        im.colorspace_settings.name = "Non-Color"
    im.alpha_mode = "STRAIGHT"
    im.pixels.foreach_set(np.clip(arr, 0, 1).astype(np.float32).ravel())
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()


def main():
    analyse = "analizar" in sys.argv
    for f in ("PistolaVP_diff.png", "PistolaVP_nor.png", "PistolaVP_ms.png"):
        if not os.path.exists(RAW + f.replace(".png", "_orig.png")):
            shutil.copyfile(RAW + f, RAW + f.replace(".png", "_orig.png"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=RAW + "MeshyPistolLow.fbx")
    o = [x for x in bpy.data.objects if x.type == "MESH"][0]
    N = img_np(bpy.data.images.load(RAW + "PistolaVP_nor_orig.png"))
    H, W = N.shape[:2]
    pos = bake(o, W, H, "pos")
    nor = bake(o, W, H, "nor")
    print("rango posicion", pos.reshape(-1, 3).min(0), pos.reshape(-1, 3).max(0), flush=True)
    dev = np.linalg.norm(N[..., :2] - 0.5, axis=-1)
    if analyse:
        Da = img_np(bpy.data.images.load(RAW + "PistolaVP_diff_orig.png"))
        st = Da.shape[0] // H
        Da = Da[::st, ::st] if st >= 1 else Da
        lumA = Da[..., 0] * 0.3 + Da[..., 1] * 0.59 + Da[..., 2] * 0.11
    THR = 0.02
    for sx in (1, -1):
        side = (nor[..., 0] * sx > 0.5) & (pos[..., 1] > ZONE["y0"]) & (pos[..., 1] < ZONE["y1"]) & (pos[..., 2] > ZONE["z0"]) & (pos[..., 2] < ZONE["z1"])
        if analyse:
            print("dev pct", np.round(np.percentile(dev[side], [50, 90, 99, 99.9]), 3), "texels", int(side.sum()), flush=True)
            ny, nz = 23, 21
            grid = np.zeros((nz, ny))
            cnt = np.zeros((nz, ny))
            yi = ((pos[..., 1][side] - ZONE["y0"]) / (ZONE["y1"] - ZONE["y0"]) * ny).astype(int).clip(0, ny - 1)
            zi = ((pos[..., 2][side] - ZONE["z0"]) / (ZONE["z1"] - ZONE["z0"]) * nz).astype(int).clip(0, nz - 1)
            med = np.median(lumA[side])
            print("lum mediana", round(float(med), 3), "pct", np.round(np.percentile(lumA[side], [5, 50, 95, 99]), 3), flush=True)
            np.add.at(grid, (zi, yi), np.abs(lumA[side] - med) > 0.04)
            np.add.at(cnt, (zi, yi), 1)
            frac = grid / np.maximum(cnt, 1)
            print("cacha", sx, "(filas de z alta a baja; columnas y de", ZONE["y0"], "a", ZONE["y1"], "cada 2 mm)", flush=True)
            for r in range(nz - 1, -1, -1):
                z = ZONE["z0"] + (r + 0.5) * (ZONE["z1"] - ZONE["z0"]) / nz
                print("%.4f " % z + "".join(" .:+#"[min(4, int(f * 40))] for f in frac[r]), flush=True)
    if analyse:
        return
    D = img_np(bpy.data.images.load(RAW + "PistolaVP_diff_orig.png"))
    S = img_np(bpy.data.images.load(RAW + "PistolaVP_ms_orig.png"))
    Dh, Sh = D.shape[0], S.shape[0]
    for sx, (y0, y1, z0, z1) in LETTERS.items():
        side = nor[..., 0] * sx > 0.5
        box = side & (pos[..., 1] > y0) & (pos[..., 1] < y1) & (pos[..., 2] > z0) & (pos[..., 2] < z1)
        ring = side & (pos[..., 1] > y0 - 0.004) & (pos[..., 1] < y1 + 0.004) & (pos[..., 2] > z0 - 0.0) & (pos[..., 2] < z1 + 0.012) & ~box
        N[box, :3] = (0.5, 0.5, 1.0)
        bD = box if Dh == H else box[:: H // Dh, :: H // Dh] if Dh < H else np.repeat(np.repeat(box, Dh // H, 0), Dh // H, 1)
        rD = ring if Dh == H else ring[:: H // Dh, :: H // Dh] if Dh < H else np.repeat(np.repeat(ring, Dh // H, 0), Dh // H, 1)
        rng = np.random.default_rng(7)
        src = D[rD, :3]
        D[bD, :3] = src[rng.integers(0, len(src), int(bD.sum()))]          # grano del polimero de alrededor, cogido al azar
        bS = box if Sh == H else box[:: H // Sh, :: H // Sh] if Sh < H else np.repeat(np.repeat(box, Sh // H, 0), Sh // H, 1)
        rS = ring if Sh == H else ring[:: H // Sh, :: H // Sh] if Sh < H else np.repeat(np.repeat(ring, Sh // H, 0), Sh // H, 1)
        S[bS] = np.median(S[rS], axis=0)
        print("cacha", sx, "texels limpiados", int(box.sum()), flush=True)
    save(N, RAW + "PistolaVP_nor.png", True)
    save(D, RAW + "PistolaVP_diff.png", False)
    save(S, RAW + "PistolaVP_ms.png", True)
    print("PROCESO_TERMINADO", flush=True)


main()
