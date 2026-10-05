"""Extrae las texturas incrustadas de un FBX de Mixamo y las guarda como PNG con nombre segun su uso:
<material>_basecolor.png, <material>_normal.png, <material>_specular.png / _roughness.png / _emission.png.
Escribe tambien un resumen "TEX material tipo archivo" para montar el material en Unity.
    blender -b --python extract_fbx_textures.py -- <fbx> <carpeta_salida> <prefijo>
"""
import os
import sys

import bpy

fbx, outdir, prefix = sys.argv[sys.argv.index("--") + 1:][:3]
os.makedirs(outdir, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)


def find_image(socket, depth=0):
    """Imagen que alimenta un socket, atravesando nodos intermedios (normal map, mezcla, separar color...)."""
    if depth > 6 or not socket.links:
        return None
    node = socket.links[0].from_node
    if node.type == "TEX_IMAGE":
        return node.image
    for inp in node.inputs:
        if inp.links:
            img = find_image(inp, depth + 1)
            if img is not None:
                return img
    return None


saved = {}
for mat in bpy.data.materials:
    if not mat.use_nodes:
        continue
    bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        continue
    uses = {
        "basecolor": bsdf.inputs["Base Color"],
        "normal": bsdf.inputs["Normal"],
        "specular": bsdf.inputs.get("Specular IOR Level") or bsdf.inputs.get("Specular"),
        "roughness": bsdf.inputs["Roughness"],
        "metallic": bsdf.inputs["Metallic"],
        "emission": bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission"),
        "alpha": bsdf.inputs["Alpha"],
    }
    for kind, sock in uses.items():
        if sock is None:
            continue
        img = find_image(sock)
        if img is None:
            continue
        safe = "".join(c if c.isalnum() or c in "_-" else "_" for c in mat.name)
        path = os.path.join(outdir, f"{prefix}_{safe}_{kind}.png")
        if img.name not in saved:
            img.filepath_raw = path
            img.file_format = "PNG"
            img.save()
            saved[img.name] = path
            print("TEX", mat.name, kind, img.name, tuple(img.size), os.path.basename(path), flush=True)
        else:
            # misma imagen para dos usos: se copia
            import shutil
            shutil.copy(saved[img.name], path)
            print("TEX", mat.name, kind, img.name, "(copia)", os.path.basename(path), flush=True)
print("PROCESO_TERMINADO", flush=True)
