"""Sotano v2 (2026-10-14, lavado de cara del interior, tanda 5): caldera, bomba, generador y cuadro electrico con remaches, tornillos, escotillas y placas.

    blender --background --python build_basement_v2.py -- <carpeta ABSOLUTA de salida> [Boiler WaterPump Generator ControlPanel]

No rehace los modelos de build_basement.py (ya tenian forma y detalle): se reutilizan sus funciones y, justo antes de cerrar la malla de cada pieza, un gancho
anade el detalle fino (remaches en los aros y tapas, escotilla de registro con 12 tornillos, tornillos en las bridas, placas de datos, bisagras, franjas de aviso).
Misma geometria base: mismas medidas, orientacion y origen.
"""
import math
import os
import sys

import bpy

HERE8 = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
_src = open(os.path.join(HERE8, "build_basement.py"), encoding="utf-8").read().replace('if __name__ == "__main__":', "if False:")
exec(_src)


def rivet_ring_x(m, x, cy, cz, r, n, size=0.03, mat="steel"):
    for k in range(n):
        a = 2 * math.pi * k / n
        m.box((x, cy + r * math.cos(a), cz + r * math.sin(a)), (size, size, size * 0.6), mat, rot=(math.degrees(a) - 90, 0, 0))


def bolts_flange_z(m, x, y, z, r, n=6, mat="steel"):
    for k in range(n):
        a = 2 * math.pi * k / n
        m.box((x + r * 1.25 * math.cos(a), y + r * 1.25 * math.sin(a), z + 0.03), (0.028, 0.028, 0.03), mat)


def extras_boiler(m):
    L, R0, zc = 4.6, 1.15, 1.55
    for x in (-1.5, -0.5, 0.5, 1.5):
        rivet_ring_x(m, x - 0.052, 0, zc, R0 + 0.04, 30, 0.028)
        rivet_ring_x(m, x + 0.052, 0, zc, R0 + 0.04, 30, 0.028)
    for sgn in (-1, 1):
        rivet_ring_x(m, sgn * (L / 2 + 0.03), 0, zc, R0 - 0.03, 36, 0.034)
    m.loft_z([(0.9, 0.0, zc + R0 - 0.03, 0.26, 0.26), (0.9, 0.0, zc + R0 + 0.07, 0.24, 0.24)], "steel_dark", 24)
    m.loft_z([(0.9, 0.0, zc + R0 + 0.07, 0.2, 0.2), (0.9, 0.0, zc + R0 + 0.1, 0.2, 0.2)], "steel", 24)
    for k in range(12):
        a = 2 * math.pi * k / 12
        m.box((0.9 + 0.22 * math.cos(a), 0.22 * math.sin(a), zc + R0 + 0.075), (0.035, 0.035, 0.03), "steel")
    m.box((-2.0, -R0 - 0.01, zc + 0.45), (0.3, 0.012, 0.16), "brass", bevel=0.003)
    m.box((-2.0, -R0 - 0.02, zc + 0.45), (0.22, 0.004, 0.05), "plastic_black")
    m.box((-0.6, -R0 - 0.17, zc - 0.25), (0.05, 0.05, 0.3), "steel", bevel=0.01)
    m.box((-0.6, -R0 - 0.16, zc + 0.03), (0.5, 0.012, 0.012), "steel")


def extras_pump(m):
    for (x, y, z, r) in ((0.5, 0, 0.75, 0.09), (0.5, 0, 1.3, 0.09)):
        bolts_flange_z(m, x, y, z, r, 8)
    for sx in (-0.78, 0.78):
        for sy in (-0.28, 0.28):
            m.box((sx, sy, 0.17), (0.05, 0.05, 0.03), "steel")
    m.box((-0.1, -0.265, 0.5), (0.16, 0.01, 0.08), "steel")
    m.box((-0.1, -0.272, 0.5), (0.12, 0.004, 0.04), "plastic_black")
    m.box((0.85, 0.0, 0.5), (0.05, 0.05, 0.05), "steel")
    m.loft_z([(0.95, 0.0, 0.5, 0.012, 0.012), (0.95, 0.0, 0.6, 0.012, 0.012)], "steel", 8)


def extras_generator(m):
    for sx in (-1.2, 1.2):
        for sz in (0.32, 1.52):
            m.box((sx, -0.535, sz), (0.04, 0.02, 0.04), "steel")
    for z in (0.35, 1.45):
        m.box((0, -0.532, z), (2.46, 0.01, 0.014), "steel_dark")
    for i in range(12):
        m.box((-1.2 + i * 0.2, -0.585, 0.08), (0.1, 0.012, 0.12), "plastic_black" if i % 2 == 0 else "body_yellow", rot=(0, 0, 20))
    m.box((0, -0.55, 1.35), (0.5, 0.012, 0.1), "plastic_white")
    m.box((-1.1, -0.545, 0.8), (0.04, 0.03, 0.22), "steel")
    m.box((1.1, -0.545, 0.8), (0.04, 0.03, 0.22), "steel")
    m.box((-1.28, 0.2, 0.32), (0.16, 0.14, 0.14), "steel_dark", bevel=0.01)
    m.loft_z([(-1.28, 0.2, 0.39, 0.02, 0.02), (-1.28, 0.2, 0.43, 0.02, 0.02)], "plastic_red", 8)
    m.loft_z([(-1.24, 0.2, 0.39, 0.02, 0.02), (-1.24, 0.2, 0.43, 0.02, 0.02)], "plastic_black", 8)


def extras_panel(m):
    L, D, H = 3.0, 0.6, 2.2
    for i in range(4):
        x = -L / 2 + 0.375 + i * 0.75
        for z in (0.35, 1.1, 1.85):
            m.box((x - 0.35, -D / 2 - 0.012, z), (0.03, 0.03, 0.12), "steel")
        for sx in (-0.3, 0.3):
            for sz in (0.2, 2.0):
                m.box((x + sx, -D / 2 - 0.016, sz), (0.025, 0.012, 0.025), "steel")
        m.box((x, -D / 2 - 0.011, 1.97), (0.5, 0.004, 0.04), "plastic_black")
        m.box((x + 0.22, -D / 2 - 0.025, 1.1), (0.012, 0.03, 0.06), "plastic_red")
    for x in (-1.2, -0.3, 0.8):
        for z in (H + 0.35, H + 0.9):
            m.box((x, 0.1, z), (0.17, 0.17, 0.03), "steel_dark")
    for k in range(6):
        m.box((-1.1 + k * 0.4, -D / 2 + 0.05, 0.02), (0.04, 0.04, 0.03), "plastic_black")


HOOKS = {"Boiler": extras_boiler, "WaterPump": extras_pump, "Generator": extras_generator, "ControlPanel": extras_panel}
_orig_finish = Mesher.finish
_orig_init = Mesher.__init__
EXTRA_MATS = {"steel": (0.55, 0.55, 0.57), "steel_dark": (0.1, 0.09, 0.09), "brass": (0.62, 0.46, 0.18), "plastic_black": (0.05, 0.05, 0.05),
              "plastic_white": (0.85, 0.84, 0.78), "plastic_red": (0.6, 0.06, 0.05), "body_yellow": (0.6, 0.45, 0.08)}


def _init(self, prefix, palette):
    pal = dict(palette)
    for k, v in EXTRA_MATS.items():
        pal.setdefault(k, v)
    _orig_init(self, prefix, pal)


def _finish(self, name):
    if name in HOOKS:
        HOOKS[name](self)
    return _orig_finish(self, name)


Mesher.__init__ = _init
Mesher.finish = _finish
BUILDERS = {"Boiler": make_boiler, "WaterPump": make_pump, "Generator": make_generator, "ControlPanel": make_control_panel}

if __name__ == "__main__" and "--" in sys.argv:
    out = sys.argv[sys.argv.index("--") + 1]
    only = sys.argv[sys.argv.index("--") + 2:]
    os.makedirs(out, exist_ok=True)
    for name, fn in BUILDERS.items():
        if only and name not in only:
            continue
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, name + ".fbx"))
        print("hecho", name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons), flush=True)
    print("PROCESO_TERMINADO")
