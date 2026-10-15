"""Prepara texturas PBR de Poly Haven para URP Lit (2026-10-15): copia color, normales (GL) y oclusion con el nombre corto y empaqueta metal y
suavidad en <nombre>_ms.png (RGB metal, A = 1 - rugosidad), que es lo que lee _MetallicGlossMap.

    blender --background --python pack_pbr.py -- <carpeta descargas> <carpeta destino> <nombre>[:metal] ...
    <nombre>:0:neutro escribe ademas <nombre>_neutral_diff.jpg, el color dividido por su media, para tenirlo en Unity con otro color
    (metal 0..1 constante, por defecto 0; los archivos de origen son <nombre>_diff_2k.jpg, _nor_gl_2k.jpg, _rough_2k.jpg, _ao_2k.jpg)
"""
import os
import shutil
import sys

import bpy
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
src, dst = args[0], args[1]
os.makedirs(dst, exist_ok=True)
for spec in args[2:]:
    name, metal, neutral = (spec.split(":") + ["0", ""])[:3]
    for a, b in (("diff", "diff"), ("nor_gl", "nor"), ("ao", "ao")):
        p = os.path.join(src, "%s_%s_2k.jpg" % (name, a))
        if os.path.exists(p):
            shutil.copyfile(p, os.path.join(dst, "%s_%s.jpg" % (name, b)))
    if neutral == "neutro":
        # version neutra del color: cada pixel dividido por el color medio (la pintura queda gris clara y el oxido o los arañazos conservan su tono)
        dif = bpy.data.images.load(os.path.join(src, name + "_diff_2k.jpg"))
        dw, dh = dif.size
        c = np.empty(dw * dh * 4, dtype=np.float32)
        dif.pixels.foreach_get(c)
        c = c.reshape(-1, 4)
        mean = c[:, 0:3].mean(axis=0)
        c[:, 0:3] = np.clip(c[:, 0:3] / mean * 0.72, 0.0, 1.0)
        neu = bpy.data.images.new(name + "_neutral_diff", dw, dh, alpha=False)
        neu.pixels.foreach_set(c.ravel())
        neu.filepath_raw = os.path.join(dst, name + "_neutral_diff.jpg")
        neu.file_format = "JPEG"
        bpy.context.scene.render.image_settings.quality = 92
        neu.save()
        print("color neutro", name, "media", mean, flush=True)
    rough = bpy.data.images.load(os.path.join(src, name + "_rough_2k.jpg"))
    rough.colorspace_settings.name = "Non-Color"
    w, h = rough.size
    px = np.empty(w * h * 4, dtype=np.float32)
    rough.pixels.foreach_get(px)
    px = px.reshape(-1, 4)
    out = np.empty_like(px)
    out[:, 0:3] = float(metal)
    out[:, 3] = 1.0 - px[:, 0]
    img = bpy.data.images.new(name + "_ms", w, h, alpha=True, float_buffer=False)
    img.colorspace_settings.name = "Non-Color"
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set(out.ravel())
    img.filepath_raw = os.path.join(dst, name + "_ms.png")
    img.file_format = "PNG"
    img.save()
    print("empaquetado", name, w, h, "metal", metal, flush=True)
print("PROCESO_TERMINADO")
