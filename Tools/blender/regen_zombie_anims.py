"""Regenera Zombie.fbx (malla base + esqueleto + todas las animaciones del zombi) en Blender sin interfaz.
blender -b --python regen_zombie_anims.py -- <ruta_Zombie.fbx>
"""
import os
import sys

out = sys.argv[sys.argv.index("--") + 1]
src = os.path.join(os.path.dirname(os.path.abspath(__file__)), "build_characters.py")
exec(compile(open(src, encoding="utf-8").read(), src, "exec"))
print("ACCIONES", build("zombie", export_path=out), flush=True)
print("PROCESO_TERMINADO", flush=True)
