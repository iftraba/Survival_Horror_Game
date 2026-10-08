"""Atrezzo de pared (2026-10-08): detalles para que las paredes no sean planas.

    blender --background --python build_wall_props.py -- <carpeta Assets/_Project/Art/Props/Wall>

Convenciones: origen en el centro de la cara de atras (la que toca la pared), a ras de la base del objeto; el frente mira a -Y
(en Unity, +Z). Asi se coloca en la pared con la rotacion LookRotation(normal de la pared) y la altura que toque.
Los nombres de material deciden el material horneado de texture_items.py: body = chapa pintada, metal/steel = metal,
iron/rust = hierro oxidado, wood = madera, cardboard = corcho/carton, el resto = plastico.
"""
import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())


def make_radiator():
    """Radiador de hierro fundido pintado, de pie en el suelo: 0,9 x 0,62 x 0,13 m, 12 elementos, valvula y tubo."""
    m = Mesher("radiator", {"body": (0.78, 0.74, 0.62), "metal": (0.55, 0.55, 0.56), "iron": (0.30, 0.20, 0.14)})
    n, w = 12, 0.9
    step = w / n
    for i in range(n):
        x = -w / 2 + step * (i + 0.5)
        m.box((x, -0.065, 0.36), (step * 0.72, 0.105, 0.50), "body", bevel=0.012)            # elemento
        m.box((x, -0.065, 0.085), (step * 0.5, 0.05, 0.05), "body", bevel=0.008)             # pie
    for z in (0.13, 0.59):
        m.box((0, -0.065, z), (w, 0.06, 0.045), "body", bevel=0.01)                          # colectores arriba y abajo
    m.loft_z([(-w / 2 - 0.04, -0.065, 0.10, 0.018, 0.018), (-w / 2 - 0.04, -0.065, 0.62, 0.018, 0.018)], "iron", 10)   # tubo
    m.loft_y([(-w / 2 - 0.04, -0.065, 0.59, 0.03, 0.03), (-w / 2 - 0.04, -0.12, 0.59, 0.03, 0.03)], "metal", 10)      # valvula
    m.loft_z([(-w / 2 - 0.04, -0.13, 0.59, 0.012, 0.012), (-w / 2 - 0.04, -0.13, 0.66, 0.012, 0.012)], "metal", 8)
    m.loft_z([(-w / 2 - 0.04, -0.13, 0.66, 0.035, 0.035), (-w / 2 - 0.04, -0.13, 0.68, 0.035, 0.035)], "metal", 12)    # volante
    for x in (-0.3, 0.3):
        m.box((x, -0.012, 0.45), (0.04, 0.024, 0.03), "metal")                                # soportes a la pared
    return m.finish("WallRadiator")


def make_extinguisher():
    """Extintor rojo en su soporte con la placa de senalizacion encima. Origen: base del soporte (se cuelga a ~0,9 m)."""
    m = Mesher("extinguisher", {"body": (0.62, 0.05, 0.04), "metal": (0.6, 0.6, 0.62), "plastic": (0.05, 0.05, 0.05), "sign": (0.85, 0.85, 0.82), "mark": (0.70, 0.06, 0.05)})
    m.box((0, -0.01, 0.30), (0.12, 0.02, 0.50), "metal", bevel=0.004)                         # placa del soporte
    m.loft_z([(0, -0.10, 0.02, 0.07, 0.07), (0, -0.10, 0.05, 0.08, 0.08), (0, -0.10, 0.48, 0.08, 0.08), (0, -0.10, 0.54, 0.05, 0.05), (0, -0.10, 0.57, 0.025, 0.025)], "body", 16)
    m.box((0, -0.10, 0.61), (0.025, 0.06, 0.05), "metal", bevel=0.005)                        # valvula
    m.box((0, -0.14, 0.645), (0.02, 0.08, 0.012), "metal", rot=(10, 0, 0))                    # maneta
    m.loft_z([(0.06, -0.14, 0.18, 0.012, 0.012), (0.06, -0.14, 0.58, 0.012, 0.012)], "plastic", 8)   # manguera
    m.box((0.06, -0.14, 0.16), (0.03, 0.03, 0.05), "plastic")                                  # boquilla
    m.box((0, -0.06, 0.25), (0.17, 0.02, 0.03), "metal")                                       # abrazadera
    m.box((0, -0.006, 0.95), (0.22, 0.012, 0.22), "sign", bevel=0.003)                         # placa
    m.box((0, -0.013, 0.95), (0.14, 0.004, 0.14), "mark")
    return m.finish("WallExtinguisher")


def make_electric_panel():
    """Cuadro electrico de chapa gris con puerta, cerradura, etiqueta y dos tubos que suben 1,2 m. Origen: base de la caja."""
    m = Mesher("elecpanel", {"body": (0.45, 0.47, 0.46), "metal": (0.6, 0.6, 0.62), "plastic": (0.08, 0.08, 0.08), "label": (0.85, 0.70, 0.08)})
    m.box((0, -0.075, 0.35), (0.50, 0.15, 0.70), "body", bevel=0.01)
    m.box((0, -0.152, 0.35), (0.46, 0.006, 0.66), "body", bevel=0.004)                         # puerta
    m.box((0.19, -0.16, 0.36), (0.025, 0.012, 0.06), "plastic")                                 # cerradura
    for z in (0.12, 0.58):
        m.box((-0.235, -0.155, z), (0.015, 0.012, 0.06), "metal")                               # bisagras
    m.box((0, -0.157, 0.56), (0.14, 0.003, 0.09), "label")                                      # etiqueta de peligro
    for x in (-0.12, 0.12):
        m.loft_z([(x, -0.06, 0.70, 0.022, 0.022), (x, -0.06, 1.90, 0.022, 0.022)], "metal", 10)  # tubos hacia el techo
        for z in (1.0, 1.5):
            m.box((x, -0.035, z), (0.06, 0.03, 0.025), "metal")                                 # grapas
    return m.finish("WallElectricPanel")


def make_notice_board():
    """Tablon de anuncios de corcho con marco de madera y papeles clavados. 1,0 x 0,7 m. Origen: base del marco."""
    m = Mesher("noticeboard", {"wood": (0.35, 0.22, 0.12), "cardboard": (0.58, 0.42, 0.26), "paper": (0.88, 0.86, 0.80), "paper2": (0.85, 0.80, 0.55), "pin": (0.70, 0.08, 0.06)})
    w, h = 1.0, 0.7
    m.box((0, -0.012, h / 2), (w, 0.024, h), "cardboard")
    for (cx, cz, sx, sz) in ((0, 0.02, w + 0.06, 0.04), (0, h - 0.02, w + 0.06, 0.04), (-w / 2, h / 2, 0.04, h), (w / 2, h / 2, 0.04, h)):
        m.box((cx, -0.02, cz), (sx, 0.04, sz), "wood", bevel=0.006)
    papers = [(-0.3, 0.45, 0.21, 0.29, 4, "paper"), (-0.05, 0.40, 0.21, 0.29, -6, "paper2"), (0.25, 0.47, 0.18, 0.25, 8, "paper"),
              (-0.32, 0.17, 0.15, 0.10, -3, "paper2"), (0.05, 0.15, 0.21, 0.15, 2, "paper"), (0.33, 0.18, 0.12, 0.17, -9, "paper")]
    for i, (x, z, sx, sz, r, mat) in enumerate(papers):
        m.box((x, -0.026 - i * 0.0008, z), (sx, 0.002, sz), mat, rot=(0, r, 0))
        m.ellipsoid((x, -0.032, z + sz * 0.42), (0.008, 0.008, 0.008), "pin", 8, 4)
    return m.finish("WallNoticeBoard")


def make_vent():
    """Rejilla de ventilacion metalica con lamas. 0,45 x 0,25 m. Origen: centro de la base."""
    m = Mesher("vent", {"metal": (0.55, 0.56, 0.57), "dark": (0.05, 0.05, 0.05)})
    w, h = 0.45, 0.25
    m.box((0, -0.003, h / 2), (w - 0.03, 0.006, h - 0.03), "dark")                              # hueco
    for (cx, cz, sx, sz) in ((0, 0.012, w, 0.024), (0, h - 0.012, w, 0.024), (-w / 2 + 0.012, h / 2, 0.024, h), (w / 2 - 0.012, h / 2, 0.024, h)):
        m.box((cx, -0.012, cz), (sx, 0.024, sz), "metal", bevel=0.003)
    for i in range(7):
        m.box((0, -0.014, 0.04 + i * 0.028), (w - 0.05, 0.004, 0.024), "metal", rot=(40, 0, 0))
    for x in (-w / 2 + 0.02, w / 2 - 0.02):
        for z in (0.02, h - 0.02):
            m.ellipsoid((x, -0.026, z), (0.005, 0.004, 0.005), "metal", 6, 3)                  # tornillos
    return m.finish("WallVent")


def make_clock():
    """Reloj de pared de oficina, parado. Diametro 0,32 m. Origen: centro de la base."""
    m = Mesher("clock", {"plastic": (0.07, 0.07, 0.07), "face": (0.90, 0.89, 0.84), "hands": (0.04, 0.04, 0.04), "red": (0.7, 0.05, 0.05)})
    r, cz = 0.16, 0.16
    m.loft_y([(0, 0, cz, r, r), (0, -0.045, cz, r, r)], "plastic", 32)
    m.loft_y([(0, -0.046, cz, r - 0.018, r - 0.018), (0, -0.047, cz, r - 0.018, r - 0.018)], "face", 32)
    for k in range(12):
        a = 2 * math.pi * k / 12
        m.box((math.sin(a) * (r - 0.035), -0.048, cz + math.cos(a) * (r - 0.035)), (0.006, 0.002, 0.018 if k % 3 else 0.028), "hands", rot=(0, -math.degrees(a), 0))
    m.box((0.035 * math.sin(math.radians(130)), -0.050, cz + 0.035 * math.cos(math.radians(130))), (0.008, 0.002, 0.075), "hands", rot=(0, -130, 0))   # horaria (las 4 y algo)
    m.box((0.05 * math.sin(math.radians(250)), -0.051, cz + 0.05 * math.cos(math.radians(250))), (0.005, 0.002, 0.11), "hands", rot=(0, -250, 0))     # minutero
    m.box((0, -0.052, cz), (0.003, 0.002, 0.12), "red", rot=(0, -20, 0))                       # segundero
    return m.finish("WallClock")


def make_pipe_run():
    """Tramo de tuberia de 2 m (eje X) con bridas y abrazaderas a la pared. Origen: centro del tramo, a la altura del tubo."""
    m = Mesher("pipe", {"iron": (0.32, 0.30, 0.28), "metal": (0.5, 0.5, 0.52)})
    r, y = 0.045, -0.09
    segs = 14                                                                    # anillos a lo largo de X
    for (x0, x1, rad, mat) in ((-1.0, 1.0, r, "iron"), (-1.0, -0.96, r + 0.012, "metal"), (0.96, 1.0, r + 0.012, "metal"), (-0.02, 0.02, r + 0.012, "metal")):
        rings = [[(x, y + rad * math.cos(a), rad * math.sin(a)) for a in (2 * math.pi * k / segs for k in range(segs))] for x in (x0, x1)]
        m._loft_rings(rings, mat, True)
    for x in (-0.5, 0.5):
        m.box((x, y / 2, 0), (0.03, abs(y), 0.02), "metal")                                     # brazo
        rings = [[(x + 0.015 * s, y + (r + 0.006) * math.cos(a), (r + 0.006) * math.sin(a)) for a in (2 * math.pi * k / segs for k in range(segs))] for s in (-1, 1)]
        m._loft_rings(rings, "metal", True)                                                       # abrazadera
    return m.finish("WallPipeRun")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_radiator, make_extinguisher, make_electric_panel, make_notice_board, make_vent, make_clock, make_pipe_run):
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
