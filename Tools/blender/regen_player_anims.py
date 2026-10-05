"""Regenera Player.fbx (esqueleto + animaciones del protagonista; la malla de bloques no se usa en juego).
blender -b --python regen_player_anims.py -- <ruta_Player.fbx>
"""
import os
import sys

out = sys.argv[sys.argv.index("--") + 1]
src = os.path.join(os.path.dirname(os.path.abspath(__file__)), "build_characters.py")
exec(compile(open(src, encoding="utf-8").read(), src, "exec"))
print("ACCIONES", build("player", export_path=out), flush=True)
print("PROCESO_TERMINADO", flush=True)
