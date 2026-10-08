"""Comisaria v2, fase 5 (2026-10-08): el sotano (instalaciones de la antigua zona 2) y la sala de calderas del segundo jefe.

    blender --background --python build_basement.py -- <carpeta Assets/_Project/Art/Props/Basement>

Origen en el centro de la base; frente a -Y en Blender (= +Z en Unity). Los nombres de material deciden el material horneado
(texture_items.py: "body" = chapa pintada, "rust" = oxido, "steel"/"metal"/"brass" = metal, "plastic", "wood", "cardboard").
  Boiler          caldera industrial horizontal (4,6 x 2,6 m, 3,4 m de alto) sobre cunas, con hogar, manometros y tubos al techo
  WaterPump       bomba de agua con motor sobre bancada y tuberias con bridas
  Generator       grupo electrogeno con rejillas, cuadro y escape
  ControlPanel    fila de armarios electricos (3 x 0,6 x 2,2 m) con puertas, relojes, interruptores y canaletas
  LabBench        mesa de laboratorio con armarios, balda alta con frascos, microscopio y probetas
  LabCabinet      vitrina de laboratorio con frascos
  MetalRack       estanteria industrial con cajas, bidones y garrafas
  Workbench       banco de trabajo con tornillo, herramientas y panel perforado
  PipeValves      colector de tuberias de pared con volantes y manometros
"""
import math
import os
import random
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())


def gauge(m, c, r, face_mat="plastic_white", rim="steel", depth=0.04):
    """Reloj redondo mirando a -Y: aro, esfera y aguja."""
    m.loft_y([(c[0], c[1], c[2], r, r), (c[0], c[1] - depth, c[2], r, r)], rim, 16, smooth=False)
    m.loft_y([(c[0], c[1] - depth, c[2], r * 0.85, r * 0.85), (c[0], c[1] - depth - 0.003, c[2], r * 0.85, r * 0.85)], face_mat, 16, smooth=False)
    m.box((c[0] + r * 0.25, c[1] - depth - 0.006, c[2] + r * 0.2), (r * 0.6, 0.003, r * 0.06), "plastic_black", rot=(0, -35, 0))


def wheel(m, c, r, mat="body_red"):
    """Volante de valvula mirando a -Y."""
    n = 14
    for k in range(n):
        a = 2 * math.pi * k / n
        m.box((c[0] + r * math.cos(a), c[1], c[2] + r * math.sin(a)), (2 * math.pi * r / n * 1.1, 0.025, 0.025), mat, rot=(0, -math.degrees(a) + 90, 0))
    for k in range(3):
        a = 2 * math.pi * k / 3
        m.box((c[0] + r / 2 * math.cos(a), c[1], c[2] + r / 2 * math.sin(a)), (r, 0.018, 0.018), mat, rot=(0, -math.degrees(a), 0))
    m.loft_y([(c[0], c[1] - 0.03, c[2], 0.03, 0.03), (c[0], c[1] + 0.03, c[2], 0.03, 0.03)], "steel", 8)


def pipe_z(m, x, y, z0, z1, r, mat="rust"):
    m.loft_z([(x, y, z0, r, r), (x, y, z1, r, r)], mat, 12)


def flange_z(m, x, y, z, r, mat="steel"):
    m.loft_z([(x, y, z - 0.02, r * 1.5, r * 1.5), (x, y, z + 0.02, r * 1.5, r * 1.5)], mat, 12)


def loft_x(m, secs, cy, cz, mat, segs=20):
    """Cilindro a lo largo de X: secs = [(x, radio)] ordenadas en X (tapas en los extremos)."""
    rings = [[(x, cy + r * math.cos(2 * math.pi * k / segs), cz + r * math.sin(2 * math.pi * k / segs)) for k in range(segs)] for x, r in secs]
    m._loft_rings(rings, mat, True)


def make_boiler():
    m = Mesher("boiler", {"body": (0.28, 0.3, 0.3), "rust": (0.35, 0.2, 0.12), "steel": (0.55, 0.55, 0.57), "brass": (0.7, 0.54, 0.22),
                          "plastic_white": (0.85, 0.84, 0.78), "plastic_black": (0.05, 0.05, 0.05), "body_red": (0.5, 0.08, 0.06), "steel_dark": (0.1, 0.09, 0.09)})
    L, R0, zc = 4.6, 1.15, 1.55
    # cuerpo horizontal a lo largo de X con tapas abombadas y anillos de refuerzo
    loft_x(m, [(-L / 2 - 0.25, 0.35), (-L / 2 - 0.12, 0.95), (-L / 2, R0), (L / 2, R0), (L / 2 + 0.12, 0.95), (L / 2 + 0.25, 0.35)], 0, zc, "body", 28)
    for x in (-1.5, -0.5, 0.5, 1.5):
        loft_x(m, [(x - 0.05, R0 + 0.035), (x + 0.05, R0 + 0.035)], 0, zc, "steel_dark", 28)
    # frente (-Y es el frente del modelo; la puerta del hogar va en la tapa oeste -X para verla de lado... se pone en el lateral -Y)
    m.box((-0.6, -R0 - 0.02, zc - 0.25), (1.0, 0.12, 0.8), "steel_dark", bevel=0.01)                 # marco del hogar
    m.box((-0.6, -R0 - 0.09, zc - 0.25), (0.8, 0.04, 0.6), "rust", bevel=0.008)                      # puerta del hogar
    for dx in (-0.25, 0.25):
        m.box((-0.6 + dx, -R0 - 0.12, zc - 0.25), (0.06, 0.03, 0.62), "steel")                       # bisagras
    m.box((-0.3, -R0 - 0.14, zc - 0.2), (0.18, 0.03, 0.04), "steel")                                # pestillo
    for i, x in enumerate((0.5, 0.85, 1.2)):
        gauge(m, (x, -R0 - 0.02, zc + 0.35), 0.11)
    m.box((1.6, -R0 - 0.05, zc - 0.1), (0.05, 0.05, 1.0), "plastic_white")                           # nivel de agua (tubo de cristal)
    # cunas y bancada
    for x in (-1.5, 0, 1.5):
        m.box((x, 0, 0.25), (0.35, 2.0, 0.5), "steel_dark", bevel=0.01)
    m.box((0, 0, 0.05), (L + 0.4, 2.4, 0.1), "rust")
    # tubos al techo con valvulas
    for x, r in ((-1.4, 0.16), (0.2, 0.12), (1.5, 0.2)):
        pipe_z(m, x, 0.3, zc + R0 - 0.1, 5.4, r)
        flange_z(m, x, 0.3, zc + R0 + 0.3, r)
        flange_z(m, x, 0.3, 4.2, r)
    wheel(m, (0.2, 0.3 - 0.2, zc + R0 + 0.6), 0.16)
    # tuberia lateral hasta el suelo
    pipe_z(m, 2.1, -0.6, 0.1, zc, 0.1)
    m.box((1.9, -0.6, zc + 0.05), (0.5, 0.2, 0.2), "rust")
    # escalerilla de mantenimiento
    for sx in (-1, 1):
        m.box((-2.0 + sx * 0.22, -R0 - 0.35, 1.4), (0.04, 0.04, 2.8), "steel")
    for k in range(8):
        m.box((-2.0, -R0 - 0.35, 0.3 + k * 0.32), (0.44, 0.03, 0.03), "steel")
    return m.finish("Boiler")


def make_pump():
    m = Mesher("pump", {"body_blue": (0.1, 0.22, 0.38), "rust": (0.35, 0.2, 0.12), "steel": (0.55, 0.55, 0.57), "steel_dark": (0.1, 0.09, 0.09), "body_red": (0.5, 0.08, 0.06),
                        "plastic_white": (0.85, 0.84, 0.78), "plastic_black": (0.05, 0.05, 0.05)})
    m.box((0, 0, 0.08), (1.7, 0.7, 0.16), "steel_dark", bevel=0.01)                                  # bancada
    # motor a lo largo de X con aletas
    loft_x(m, [(-0.78, 0.2), (-0.75, 0.26), (0.05, 0.26), (0.08, 0.2)], 0, 0.45, "body_blue", 20)
    for k in range(12):
        a = 2 * math.pi * k / 12
        m.box((-0.35, 0.28 * math.cos(a), 0.45 + 0.28 * math.sin(a)), (0.7, 0.008, 0.035), "body_blue", rot=(math.degrees(a) + 90, 0, 0))
    m.box((-0.35, 0, 0.75), (0.25, 0.2, 0.12), "body_blue")                                          # caja de bornas
    m.box((-0.35, 0, 0.18), (0.6, 0.45, 0.06), "steel_dark")
    m.box((0.15, 0, 0.45), (0.2, 0.1, 0.1), "steel")                                                 # acoplamiento
    # voluta de la bomba
    m.ellipsoid((0.5, 0, 0.45), (0.25, 0.22, 0.3), "body_red", 16, 8)
    pipe_z(m, 0.5, 0, 0.7, 1.5, 0.09)                                                                 # impulsion hacia arriba
    flange_z(m, 0.5, 0, 0.75, 0.09); flange_z(m, 0.5, 0, 1.3, 0.09)
    wheel(m, (0.5, -0.14, 1.05), 0.13)
    gauge(m, (0.5, -0.1, 1.38), 0.07)
    m.loft_y([(0.75, 0, 0.4, 0.1, 0.1), (0.75, -0.0, 0.4, 0.1, 0.1)], "rust", 12) if False else None
    for k in range(10):                                                                              # aspiracion hacia el suelo, de lado
        pass
    m.box((0.85, 0, 0.42), (0.4, 0.18, 0.18), "rust")
    m.box((1.05, 0, 0.22), (0.18, 0.18, 0.4), "rust")
    return m.finish("WaterPump")


def make_generator():
    m = Mesher("generator", {"body_yellow": (0.6, 0.45, 0.08), "steel_dark": (0.1, 0.09, 0.09), "steel": (0.55, 0.55, 0.57), "rust": (0.35, 0.2, 0.12),
                             "plastic_black": (0.05, 0.05, 0.05), "plastic_white": (0.85, 0.84, 0.78), "plastic_red": (0.6, 0.06, 0.05), "plastic_green": (0.1, 0.5, 0.15)})
    m.box((0, 0, 0.08), (2.7, 1.15, 0.16), "steel_dark")
    m.box((0, 0, 0.88), (2.5, 1.05, 1.44), "body_yellow", bevel=0.02)
    for x in (-1.0, -0.6, 0.6, 1.0):                                                                # rejillas
        for k in range(9):
            m.box((x, -0.53, 0.5 + k * 0.08), (0.32, 0.02, 0.03), "steel_dark")
    m.box((0, -0.535, 0.95), (0.6, 0.02, 0.5), "steel_dark")                                         # cuadro
    for i, x in enumerate((-0.17, 0, 0.17)):
        gauge(m, (x, -0.545, 1.08), 0.06)
    for x, mat in ((-0.15, "plastic_red"), (0.0, "plastic_green"), (0.15, "plastic_black")):
        m.box((x, -0.56, 0.8), (0.05, 0.03, 0.05), mat)
    m.loft_z([(0.9, 0.2, 1.6, 0.08, 0.08), (0.9, 0.2, 2.2, 0.08, 0.08)], "rust", 12)              # escape
    m.box((0.9, 0.2, 2.25), (0.22, 0.22, 0.06), "rust")
    m.box((-0.9, 0.3, 1.66), (0.4, 0.3, 0.12), "plastic_black")                                       # tapon del deposito
    for sx in (-1, 1):
        m.box((sx * 1.1, -0.0, 1.62), (0.2, 0.9, 0.04), "steel_dark")                                 # cancamos
    return m.finish("Generator")


def make_control_panel():
    rng = random.Random(3)
    m = Mesher("panel", {"body": (0.42, 0.45, 0.42), "steel_dark": (0.1, 0.09, 0.09), "steel": (0.55, 0.55, 0.57), "plastic_black": (0.05, 0.05, 0.05),
                         "plastic_white": (0.85, 0.84, 0.78), "plastic_red": (0.6, 0.06, 0.05), "plastic_green": (0.1, 0.5, 0.15), "plastic_yellow": (0.7, 0.55, 0.05)})
    L, D, H = 3.0, 0.6, 2.2
    m.box((0, 0, 0.05), (L, D, 0.1), "steel_dark")
    m.box((0, 0, H / 2 + 0.05), (L, D, H - 0.1), "body", bevel=0.01)
    m.box((0, 0, H + 0.03), (L + 0.04, D + 0.04, 0.06), "body")
    for i in range(4):                                                                                # cuatro puertas
        x = -L / 2 + 0.375 + i * 0.75
        m.box((x, -D / 2 - 0.01, 1.1), (0.7, 0.02, 1.9), "body", bevel=0.004)
        m.box((x + 0.28, -D / 2 - 0.03, 1.1), (0.03, 0.03, 0.18), "steel")                         # maneta
        for j in range(2):
            gauge(m, (x - 0.12 + j * 0.24, -D / 2 - 0.02, 1.7), 0.07, depth=0.03)
        for j in range(4):
            col = rng.choice(("plastic_red", "plastic_green", "plastic_yellow", "plastic_black"))
            m.box((x - 0.18 + j * 0.12, -D / 2 - 0.03, 1.35), (0.05, 0.03, 0.05), col)
        m.box((x, -D / 2 - 0.022, 0.9), (0.4, 0.003, 0.2), "plastic_white")                         # rotulo
        for k in range(5):
            m.box((x, -D / 2 - 0.022, 0.35 + k * 0.05), (0.5, 0.004, 0.02), "steel_dark")            # rejilla baja
    for x in (-1.2, -0.3, 0.8):                                                                       # canaletas al techo
        m.box((x, 0.1, H + 0.6), (0.12, 0.12, 1.2), "steel")
    return m.finish("ControlPanel")


def make_lab_bench():
    rng = random.Random(5)
    m = Mesher("labbench", {"body": (0.82, 0.82, 0.78), "plastic_black": (0.05, 0.05, 0.05), "steel": (0.6, 0.6, 0.62), "plastic_glass": (0.55, 0.7, 0.7),
                            "plastic_amber": (0.5, 0.28, 0.06), "plastic_green": (0.15, 0.45, 0.2), "plastic_white": (0.88, 0.88, 0.85), "wood": (0.4, 0.28, 0.15)})
    L, D = 2.4, 0.8
    m.box((0, 0, 0.45), (L - 0.1, D - 0.08, 0.86), "body")                                            # armarios
    for i in range(4):
        x = -L / 2 + 0.33 + i * 0.58
        m.box((x, -D / 2 + 0.03, 0.47), (0.54, 0.02, 0.76), "body", bevel=0.004)
        m.box((x + 0.2, -D / 2 + 0.01, 0.75), (0.1, 0.02, 0.02), "steel")
    m.box((0, 0, 0.9), (L, D, 0.04), "plastic_black")                                                 # encimera
    for sx in (-1, 1):
        m.box((sx * (L / 2 - 0.05), 0.3, 1.35), (0.04, 0.04, 0.9), "steel")
    m.box((0, 0.3, 1.6), (L, 0.25, 0.025), "steel")                                                   # balda alta
    x = -L / 2 + 0.1
    while x < L / 2 - 0.1:
        col = rng.choice(("plastic_glass", "plastic_amber", "plastic_green", "plastic_white"))
        h = rng.uniform(0.12, 0.26)
        m.loft_z([(x, 0.3, 1.613, 0.045, 0.045), (x, 0.3, 1.613 + h * 0.75, 0.045, 0.045), (x, 0.3, 1.613 + h, 0.018, 0.018)], col, 10)
        x += rng.uniform(0.1, 0.18)
    # microscopio
    m.box((-0.6, -0.05, 0.94), (0.16, 0.22, 0.04), "plastic_white")
    m.box((-0.6, 0.04, 1.08), (0.06, 0.06, 0.28), "plastic_white")
    m.box((-0.6, -0.02, 1.25), (0.06, 0.18, 0.06), "plastic_white", rot=(30, 0, 0))
    m.loft_z([(-0.6, -0.1, 1.28, 0.02, 0.02), (-0.6, -0.1, 1.34, 0.02, 0.02)], "plastic_black", 8)
    # probetas en gradilla y matraces
    m.box((0.3, -0.1, 0.96), (0.3, 0.08, 0.06), "wood")
    for k in range(5):
        m.loft_z([(0.18 + k * 0.06, -0.1, 0.94, 0.012, 0.012), (0.18 + k * 0.06, -0.1, 1.08, 0.012, 0.012)], rng.choice(("plastic_glass", "plastic_amber", "plastic_green")), 8)
    for (x, y) in ((0.75, 0.05), (0.95, -0.12)):
        m.loft_z([(x, y, 0.92, 0.07, 0.07), (x, y, 1.0, 0.07, 0.07), (x, y, 1.08, 0.02, 0.02), (x, y, 1.16, 0.02, 0.02)], "plastic_glass", 12)
    m.box((-0.1, 0.15, 0.93), (0.3, 0.22, 0.02), "plastic_white", rot=(0, 0, 12))                     # papeles
    return m.finish("LabBench")


def make_lab_cabinet():
    rng = random.Random(8)
    m = Mesher("labcab", {"body": (0.82, 0.82, 0.78), "plastic_glass": (0.55, 0.7, 0.72), "steel": (0.6, 0.6, 0.62), "plastic_amber": (0.5, 0.28, 0.06),
                          "plastic_green": (0.15, 0.45, 0.2), "plastic_white": (0.88, 0.88, 0.85), "plastic_red": (0.5, 0.08, 0.06)})
    W, D, H = 1.0, 0.45, 1.9
    m.box((0, 0, 0.35), (W, D, 0.7), "body")
    for sx in (-1, 1):
        m.box((sx * (W / 2 - 0.02), 0, 1.3), (0.04, D, 1.2), "body")
    m.box((0, D / 2 - 0.01, 1.3), (W, 0.02, 1.2), "body")
    m.box((0, 0, H - 0.02), (W, D, 0.04), "body")
    for z in (1.0, 1.4):
        m.box((0, 0, z), (W - 0.06, D - 0.04, 0.02), "plastic_glass")
    for z in (0.71, 1.01, 1.41):
        x = -W / 2 + 0.1
        while x < W / 2 - 0.08:
            col = rng.choice(("plastic_amber", "plastic_green", "plastic_white", "plastic_red"))
            h = rng.uniform(0.1, 0.24)
            m.loft_z([(x, rng.uniform(-0.08, 0.08), z, 0.04, 0.04), (x, 0, z + h, 0.04, 0.04)], col, 10)
            x += rng.uniform(0.09, 0.15)
    for sx in (-1, 1):
        m.box((sx * W / 4, -D / 2 - 0.005, 1.3), (W / 2 - 0.03, 0.01, 1.18), "plastic_glass")         # puertas de cristal
        m.box((sx * 0.04, -D / 2 - 0.02, 1.3), (0.02, 0.02, 0.15), "steel")
    for sx in (-1, 1):
        m.box((sx * W / 4, -D / 2 - 0.01, 0.36), (W / 2 - 0.03, 0.02, 0.64), "body", bevel=0.004)
    return m.finish("LabCabinet")


def make_metal_rack():
    rng = random.Random(11)
    m = Mesher("rack", {"body_blue": (0.12, 0.25, 0.45), "body_orange": (0.65, 0.3, 0.05), "cardboard": (0.6, 0.48, 0.3), "cardboard_dark": (0.45, 0.36, 0.22),
                        "rust": (0.35, 0.2, 0.12), "plastic_white": (0.85, 0.84, 0.8), "plastic_blue": (0.12, 0.3, 0.6), "wood": (0.5, 0.38, 0.22)})
    L, D, H = 2.4, 0.8, 2.4
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((sx * (L / 2 - 0.04), sy * (D / 2 - 0.04), H / 2), (0.08, 0.08, H), "body_blue")
    for z in (0.15, 1.05, 1.95):
        for sy in (-1, 1):
            m.box((0, sy * (D / 2 - 0.04), z), (L, 0.06, 0.1), "body_orange")
        m.box((0, 0, z + 0.06), (L - 0.1, D - 0.1, 0.02), "wood")
    for z in (0.22, 1.12, 2.02):
        x = -L / 2 + 0.15
        while x < L / 2 - 0.3:
            r = rng.random()
            if r < 0.45:
                w = rng.uniform(0.35, 0.55); h = rng.uniform(0.25, 0.5)
                m.box((x + w / 2, rng.uniform(-0.05, 0.05), z + h / 2), (w, 0.55, h), rng.choice(("cardboard", "cardboard_dark")), rot=(0, 0, rng.uniform(-6, 6)))
                x += w + 0.04
            elif r < 0.7:
                m.loft_z([(x + 0.2, 0, z, 0.19, 0.19), (x + 0.2, 0, z + 0.62, 0.19, 0.19)], rng.choice(("rust", "body_blue")), 14)   # bidon
                x += 0.43
            elif r < 0.9:
                for k in range(2):
                    m.box((x + 0.1 + k * 0.18, 0, z + 0.17), (0.15, 0.25, 0.34), rng.choice(("plastic_white", "plastic_blue")), rot=(0, 0, rng.uniform(-8, 8)))
                x += 0.4
            else:
                x += 0.35
    return m.finish("MetalRack")


def make_workbench():
    rng = random.Random(13)
    m = Mesher("workbench", {"wood": (0.42, 0.3, 0.16), "body": (0.3, 0.33, 0.33), "steel": (0.55, 0.55, 0.57), "steel_dark": (0.1, 0.09, 0.09),
                             "plastic_red": (0.6, 0.06, 0.05), "plastic_yellow": (0.7, 0.55, 0.05), "plastic_black": (0.05, 0.05, 0.05), "board": (0.6, 0.5, 0.35)})
    L, D = 2.0, 0.8
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((sx * (L / 2 - 0.05), sy * (D / 2 - 0.05), 0.45), (0.06, 0.06, 0.9), "body")
    m.box((0, 0, 0.92), (L, D, 0.06), "wood", bevel=0.005)
    m.box((0, 0, 0.2), (L - 0.1, D - 0.1, 0.03), "wood")
    m.box((0, D / 2 - 0.02, 1.55), (L, 0.03, 1.2), "board")                                           # panel perforado
    for i in range(12):
        for j in range(6):
            m.box((-L / 2 + 0.1 + i * 0.165, D / 2 - 0.04, 1.05 + j * 0.18), (0.012, 0.004, 0.012), "steel_dark")
    for k, (x, z, w, h) in enumerate(((-0.7, 1.7, 0.04, 0.3), (-0.5, 1.6, 0.25, 0.04), (-0.2, 1.75, 0.05, 0.35), (0.1, 1.5, 0.3, 0.05), (0.5, 1.7, 0.04, 0.28))):
        m.box((x, D / 2 - 0.06, z), (w, 0.03, h), rng.choice(("steel", "plastic_red", "plastic_yellow")))   # herramientas colgadas
    # tornillo de banco
    m.box((-0.75, -0.35, 1.02), (0.22, 0.14, 0.14), "steel_dark")
    m.box((-0.75, -0.47, 1.03), (0.04, 0.2, 0.04), "steel")
    # caja de herramientas y piezas
    m.box((0.5, 0.05, 1.03), (0.45, 0.22, 0.16), "plastic_red", bevel=0.01)
    m.box((0.5, 0.05, 1.13), (0.3, 0.03, 0.03), "plastic_black")
    for k in range(5):
        m.box((rng.uniform(-0.3, 0.2), rng.uniform(-0.3, 0.1), 0.96), (rng.uniform(0.05, 0.15), 0.04, 0.03), rng.choice(("steel", "steel_dark")), rot=(0, 0, rng.uniform(0, 180)))
    return m.finish("Workbench")


def make_pipe_valves():
    m = Mesher("valves", {"rust": (0.35, 0.2, 0.12), "steel": (0.55, 0.55, 0.57), "body_red": (0.5, 0.08, 0.06), "body_yellow": (0.6, 0.45, 0.08),
                          "plastic_white": (0.85, 0.84, 0.78), "plastic_black": (0.05, 0.05, 0.05), "steel_dark": (0.1, 0.09, 0.09)})
    for x, r in ((-0.7, 0.09), (-0.2, 0.07), (0.3, 0.1), (0.75, 0.06)):
        pipe_z(m, x, 0.1, 0.0, 2.8, r)
        flange_z(m, x, 0.1, 0.4, r); flange_z(m, x, 0.1, 2.2, r)
        wheel(m, (x, -0.05, 1.3), 0.12 + r * 0.3, "body_red" if x < 0 else "body_yellow")
        m.box((x, 0.0, 1.3), (r * 2.6, 0.12, r * 2.6), "steel_dark")
    m.box((0, 0.15, 1.95), (1.9, 0.12, 0.12), "rust")                                                # colector horizontal
    for x in (-0.45, 0.55):
        gauge(m, (x, 0.05, 1.95), 0.08)
    for x in (-0.9, 0.9):
        m.box((x, 0.18, 2.5), (0.08, 0.04, 0.08), "steel")                                           # abrazaderas
    return m.finish("PipeValves")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_boiler, make_pump, make_generator, make_control_panel, make_lab_bench, make_lab_cabinet, make_metal_rack, make_workbench, make_pipe_valves):
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
