"""Extrae fotogramas de un video con el secuenciador de Blender.
blender -b --python extract_frames.py -- <video> <carpeta_salida> <inicio_s> <fin_s> <cada_n_s>
"""
import sys

import bpy

video, outdir, t0, t1, step = sys.argv[sys.argv.index("--") + 1:][:5]
t0, t1, step = float(t0), float(t1), float(step)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.sequence_editor_create()
strip = sc.sequence_editor.strips.new_movie("v", video, channel=1, frame_start=1)
fps = strip.elements[0] and (sc.render.fps / sc.render.fps_base)
w, h = strip.elements[0].orig_width, strip.elements[0].orig_height
print("INFO", w, h, "frames", strip.frame_final_duration, "fps", fps, flush=True)
sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = w, h, 50
sc.render.image_settings.file_format = "JPEG"
sc.frame_end = strip.frame_final_duration
t = t0
while t <= t1:
    sc.frame_set(int(t * fps) + 1)
    sc.render.filepath = f"{outdir}\\f_{int(t * 10):04d}.jpg"
    bpy.ops.render.render(write_still=True)
    t += step
print("PROCESO_TERMINADO", flush=True)
