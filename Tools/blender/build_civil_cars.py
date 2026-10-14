"""Coches civiles abandonados de la calle (2026-10-14, fase 2 del lavado de cara del exterior): la misma carroceria de la patrulla sin rotulos,
barra de luces ni defensa, en tres versiones. Mide lo mismo que la patrulla (2,07 x 5,02 x 1,78 m con retrovisores).

    blender --background --python build_civil_cars.py -- <carpeta ABSOLUTA de salida> [CivilCarA CivilCarB CivilCarC]
    (en un Blender abierto: exec(open(ruta).read()); preview_civil("CivilCarA"))

  CivilCarA  berlina verde azulado, intacta (sucia)
  CivilCarB  familiar rojo oscuro, intacto
  CivilCarC  berlina beige quemada (capo, techo y maletero calcinados, neumaticos aplastados)
"""
import os
import sys

_NO_MAIN = True
HERE2 = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE2, "build_police_cars.py"), encoding="utf-8").read())

WAGON = [
    (-2.35, 0.36, 0.60, 0.64, 0.73, 0.58), (-2.32, 0.27, 0.66, 0.70, 0.84, 0.68), (-2.22, 0.23, 0.77, 0.81, 0.90, 0.74), (-1.90, 0.22, 0.87, 0.91, 0.925, 0.78),
    (-1.40, 0.22, 0.915, 0.955, 0.93, 0.79), (-0.98, 0.22, 0.94, 0.98, 0.925, 0.79), (-0.78, 0.22, 0.95, 1.14, 0.915, 0.75), (-0.58, 0.22, 0.95, 1.253, 0.91, 0.73),
    (-0.48, 0.22, 0.95, 1.31, 0.91, 0.72), (-0.14, 0.22, 0.95, 1.40, 0.91, 0.70), (0.40, 0.22, 0.95, 1.43, 0.91, 0.70), (1.00, 0.22, 0.95, 1.43, 0.91, 0.70),
    (1.28, 0.22, 0.95, 1.43, 0.91, 0.70), (1.38, 0.22, 0.95, 1.43, 0.915, 0.71), (1.80, 0.22, 0.95, 1.40, 0.92, 0.72), (2.12, 0.22, 0.95, 1.30, 0.925, 0.74),
    (2.27, 0.23, 0.92, 1.12, 0.915, 0.76), (2.33, 0.27, 0.86, 0.95, 0.85, 0.70), (2.35, 0.36, 0.72, 0.80, 0.74, 0.58),
]
SEDAN = list(STATIONS)
VARIANTS = {
    "CivilCarA": dict(name="CivilCarA", color=(0.07, 0.24, 0.26), burnt=False, stations=SEDAN),
    "CivilCarB": dict(name="CivilCarB", color=(0.36, 0.035, 0.04), burnt=False, stations=WAGON),
    "CivilCarC": dict(name="CivilCarC", color=(0.55, 0.5, 0.36), burnt=True, stations=SEDAN),
}


def build_variant(key):
    v = VARIANTS[key]
    global STATIONS
    STATIONS = v["stations"]
    try:
        return build(v["burnt"], civil=v)
    finally:
        STATIONS = SEDAN


def preview_civil(key="CivilCarA"):
    _clear()
    obj = build_variant(key)
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    view(72, 40, 7.5, (0, 0, 0.7))
    d = obj.dimensions
    print(key, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), sum(len(p.vertices) - 2 for p in obj.data.polygons), "tris")
    return obj


if __name__ == "__main__" and "--" in sys.argv:
    out = sys.argv[sys.argv.index("--") + 1]
    only = sys.argv[sys.argv.index("--") + 2:]
    os.makedirs(out, exist_ok=True)
    for key in VARIANTS:
        if only and key not in only:
            continue
        _clear()
        obj = build_variant(key)
        d = obj.dimensions
        _export(obj, os.path.join(out, key + ".fbx"))
        print("hecho", key, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), sum(len(p.vertices) - 2 for p in obj.data.polygons), "tris", flush=True)
    print("PROCESO_TERMINADO")
