"""Genera ZombieStyles.fbx: esqueleto del zombi + animaciones propias de cada tipo (ver ZOMBIE_STYLES).
blender -b --python regen_zombie_styles.py -- <ruta_ZombieStyles.fbx>
"""
import os
import sys

out = sys.argv[sys.argv.index("--") + 1]
src = os.path.join(os.path.dirname(os.path.abspath(__file__)), "build_characters.py")
exec(compile(open(src, encoding="utf-8").read(), src, "exec"))
info = build_zombie_styles(out)
for k, (walk, run) in info.items():
    print(f"ESTILO {k}: paso {walk} fotogramas, carrera {run}", flush=True)
print("PROCESO_TERMINADO", flush=True)
