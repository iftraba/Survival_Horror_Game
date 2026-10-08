"""Comisaria v2 (2026-10-08): memorial de los agentes caidos, en el recibidor de la primera planta.

    blender --background --python build_memorial.py -- <carpeta Assets/_Project/Art/Props/Memorial>

Origen en el centro de la base; frente a -Y en Blender (= +Z en Unity). Los nombres de material deciden el material horneado
(texture_items.py; "stone" es piedra con vetas).
  MemorialMonument  monumento de piedra escalonado con estela, estrella de la policia en bronce y tres huecos para medallones
  MemorialMedallion medallon de bronce que encaja en un hueco (para el puzle; se coloca en Unity)
  MemorialPlaque    placa de pared con los nombres grabados sobre un tablero de madera
  Stanchion         poste de laton para cordon;  StanchionRope: cordon de terciopelo de 1,5 m que cuelga entre dos postes
  PottedPlant       planta en maceta (ficus), algo mustia
  FlowerWreath      corona de flores en su tripode, con cinta
  Candles           grupo de velas de varias alturas, derretidas, con su cera en el suelo
"""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())


def star_prism(m, c, r_out, r_in, depth, mat, points=6, rot0=90.0):
    """Estrella de lados rectos en el plano XZ, con grosor hacia -Y (sale de la cara frontal)."""
    bm = m.bm
    pts = []
    for k in range(points * 2):
        a = math.radians(rot0) + math.pi * k / points
        r = r_out if k % 2 == 0 else r_in
        pts.append((c[0] + r * math.cos(a), c[2] + r * math.sin(a)))
    back = [bm.verts.new(Vector((x, c[1], z))) for x, z in pts]
    front = [bm.verts.new(Vector((x, c[1] - depth, z))) for x, z in pts]
    faces = [bm.faces.new(front), bm.faces.new(list(reversed(back)))]
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((back[i], back[j], front[j], front[i])))
    for f in faces:
        f.material_index = m._idx(mat)
        f.smooth = False


def disc_y(m, c, r, depth, mat, segs=20):
    """Disco con su eje en Y (cara hacia -Y)."""
    m.loft_y([(c[0], c[1], c[2], r, r), (c[0], c[1] - depth, c[2], r, r)], mat, segs, smooth=False)


def make_monument():
    m = Mesher("monument", {"stone": (0.62, 0.6, 0.56), "stone_dark": (0.2, 0.2, 0.21), "brass": (0.72, 0.55, 0.22)})
    # tres escalones
    for i, (w, h) in enumerate(((2.4, 0.18), (2.0, 0.18), (1.6, 0.2))):
        z0 = sum(x[1] for x in ((2.4, 0.18), (2.0, 0.18), (1.6, 0.2))[:i])
        m.box((0, 0, z0 + h / 2), (w, w, h), "stone", bevel=0.015)
    zb = 0.56
    # estela con zocalo y cornisa
    m.box((0, 0, zb + 0.15), (1.1, 0.5, 0.3), "stone", bevel=0.02)
    m.box((0, 0, zb + 0.3 + 1.0), (0.9, 0.36, 2.0), "stone", bevel=0.012)
    m.box((0, 0, zb + 2.36), (1.04, 0.48, 0.12), "stone", bevel=0.02)
    m.box((0, 0, zb + 2.47), (0.9, 0.38, 0.1), "stone", bevel=0.02)
    # remate: frontón bajo
    m.box((0, 0, zb + 2.57), (0.7, 0.3, 0.1), "stone", bevel=0.02)
    fy = -0.18
    # estrella de la policia en bronce, con su aro
    disc_y(m, (0, fy + 0.002, zb + 1.85), 0.31, 0.012, "brass", 32)                  # aro (asoma 2 mm alrededor del fondo)
    disc_y(m, (0, fy - 0.009, zb + 1.85), 0.29, 0.004, "stone_dark", 32)               # fondo oscuro
    star_prism(m, (0, fy - 0.012, zb + 1.85), 0.26, 0.13, 0.03, "brass")
    disc_y(m, (0, fy - 0.04, zb + 1.85), 0.055, 0.01, "stone_dark", 14)
    # tres huecos para los medallones (oscuros, hundidos) con su borde de bronce
    for x in (-0.27, 0, 0.27):
        disc_y(m, (x, fy + 0.002, zb + 1.2), 0.118, 0.012, "brass", 24)               # borde de bronce
        disc_y(m, (x, fy - 0.009, zb + 1.2), 0.1, 0.004, "stone_dark", 24)            # hueco oscuro delante
    # placa de bronce con lineas grabadas
    m.box((0, fy - 0.005, zb + 0.75), (0.62, 0.012, 0.32), "brass", bevel=0.004)
    for i in range(5):
        w = 0.5 if i == 0 else 0.38 - 0.04 * (i % 2)
        m.box((0, fy - 0.012, zb + 0.86 - i * 0.055), (w, 0.004, 0.018 if i == 0 else 0.012), "stone_dark")
    # dos pebeteros de bronce en el escalon de arriba
    for x in (-0.62, 0.62):
        m.loft_z([(x, -0.3, 0.36, 0.08, 0.08), (x, -0.3, 0.42, 0.05, 0.05), (x, -0.3, 0.6, 0.04, 0.04), (x, -0.3, 0.66, 0.13, 0.13), (x, -0.3, 0.7, 0.14, 0.14)], "brass", 16)
        m.loft_z([(x, -0.3, 0.69, 0.12, 0.12), (x, -0.3, 0.7, 0.12, 0.12)], "stone_dark", 16)
    return m.finish("MemorialMonument")


def make_medallion():
    m = Mesher("medallion", {"brass": (0.75, 0.58, 0.22), "steel": (0.35, 0.3, 0.22)})
    disc_y(m, (0, 0.005, 0.1), 0.095, 0.014, "brass", 24)
    star_prism(m, (0, -0.009, 0.1), 0.055, 0.028, 0.006, "steel")
    return m.finish("MemorialMedallion")


def make_plaque():
    m = Mesher("plaque", {"wood": (0.25, 0.13, 0.06), "brass": (0.7, 0.54, 0.22), "steel": (0.18, 0.15, 0.1)})
    m.box((0, 0, 0), (0.62, 0.04, 0.84), "wood", bevel=0.01)
    m.box((0, -0.025, 0.0), (0.5, 0.012, 0.72), "brass", bevel=0.004)
    star_prism(m, (0, -0.031, 0.27), 0.07, 0.035, 0.008, "steel")
    rng = random.Random(4)
    for i in range(9):
        w = rng.uniform(0.22, 0.36)
        m.box((0, -0.0325, 0.14 - i * 0.05), (w, 0.003, 0.014), "steel")
    for x in (-0.22, 0.22):                         # tornillos
        for z in (-0.33, 0.33):
            m.loft_y([(x, -0.031, z, 0.01, 0.01), (x, -0.036, z, 0.008, 0.008)], "brass", 8)
    obj = m.finish("MemorialPlaque")
    return obj


def make_stanchion():
    m = Mesher("stanchion", {"brass": (0.78, 0.6, 0.25)})
    m.loft_z([(0, 0, 0, 0.16, 0.16), (0, 0, 0.03, 0.15, 0.15), (0, 0, 0.06, 0.05, 0.05)], "brass", 20)
    m.loft_z([(0, 0, 0.06, 0.025, 0.025), (0, 0, 0.9, 0.025, 0.025)], "brass", 12)
    m.ellipsoid((0, 0, 0.93), (0.04, 0.04, 0.04), "brass", 12, 6)
    return m.finish("Stanchion")


def make_rope():
    """Cordon de 1,5 m a lo largo de Y (de -0,75 a 0,75), colgando de 0,88 m a 0,7 m en el centro, con sus ganchos."""
    m = Mesher("rope", {"fabric_velvet": (0.42, 0.04, 0.05), "brass": (0.78, 0.6, 0.25)})
    secs = []
    n = 18
    for i in range(n + 1):
        y = -0.71 + 1.42 * i / n
        sag = 0.18 * (1 - (y / 0.71) ** 2)
        secs.append((0, y, 0.88 - sag, 0.018, 0.018))
    m.loft_y(secs, "fabric_velvet", 10)
    for y in (-0.72, 0.72):
        m.loft_y([(0, y - 0.03, 0.88, 0.022, 0.022), (0, y + 0.03, 0.88, 0.022, 0.022)], "brass", 10)
    return m.finish("StanchionRope")


def make_plant():
    rng = random.Random(7)
    m = Mesher("plant", {"plastic_pot": (0.45, 0.22, 0.12), "cardboard": (0.12, 0.08, 0.05), "wood": (0.3, 0.2, 0.1), "plastic_leaf": (0.16, 0.3, 0.1), "plastic_dry": (0.42, 0.36, 0.14)})
    m.loft_z([(0, 0, 0, 0.17, 0.17), (0, 0, 0.42, 0.23, 0.23), (0, 0, 0.46, 0.25, 0.25)], "plastic_pot", 20)
    m.loft_z([(0, 0, 0.4, 0.22, 0.22), (0, 0, 0.42, 0.22, 0.22)], "cardboard", 20)   # tierra
    for t in range(3):                                    # troncos
        a = t * 2.1
        m.loft_z([(0.02 * math.cos(a), 0.02 * math.sin(a), 0.42, 0.02, 0.02), (0.12 * math.cos(a), 0.12 * math.sin(a), 1.5, 0.012, 0.012)], "wood", 6)
    for i in range(200):                                  # hojas, agrupadas alrededor de los troncos
        t = rng.randrange(3); ta = t * 2.1
        h = rng.uniform(0.8, 1.6)
        k = (h - 0.42) / (1.5 - 0.42)
        tx, ty = (0.02 + 0.1 * k) * math.cos(ta), (0.02 + 0.1 * k) * math.sin(ta)
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.03, 0.26) * (0.6 + 0.4 * k)
        c = (tx + r * math.cos(a), ty + r * math.sin(a), h)
        dry = rng.random() < 0.18
        m.box(c, (0.08, 0.15, 0.006), "plastic_dry" if dry else "plastic_leaf", rot=(rng.uniform(-50, 10) - (35 if dry else 0), rng.uniform(-20, 20), math.degrees(a) + 90 + rng.uniform(-30, 30)))
    for i in range(5):                                    # hojas secas caidas
        a = rng.uniform(0, 2 * math.pi)
        m.box((0.32 * math.cos(a), 0.32 * math.sin(a), 0.003), (0.07, 0.12, 0.004), "plastic_dry", rot=(0, 0, rng.uniform(0, 180)))
    return m.finish("PottedPlant")


def make_wreath():
    rng = random.Random(11)
    m = Mesher("wreath", {"wood": (0.25, 0.15, 0.08), "plastic_leaf": (0.1, 0.22, 0.08), "fabric_white": (0.85, 0.84, 0.8), "fabric_red": (0.55, 0.05, 0.06), "fabric_ribbon": (0.08, 0.1, 0.25)})
    for a in (90, 210, 330):                              # tripode
        ar = math.radians(a)
        m.box((0.18 * math.cos(ar), 0.18 * math.sin(ar) + 0.05, 0.6), (0.025, 0.025, 1.25), "wood", rot=(math.degrees(math.atan2(0.18, 1.2)) * math.sin(ar), -math.degrees(math.atan2(0.18, 1.2)) * math.cos(ar), 0))
    cz, R = 1.05, 0.36
    for i in range(36):                                   # aro de hojas
        a = 2 * math.pi * i / 36
        c = (R * math.cos(a), -0.06, cz + R * math.sin(a))
        m.ellipsoid(c, (0.07, 0.05, 0.07), "plastic_leaf", 8, 4)
    for i in range(22):                                   # flores
        a = rng.uniform(0, 2 * math.pi)
        rr = R + rng.uniform(-0.06, 0.06)
        m.ellipsoid((rr * math.cos(a), -0.11, cz + rr * math.sin(a)), (0.04, 0.03, 0.04), "fabric_white" if rng.random() < 0.6 else "fabric_red", 8, 4)
    m.box((0, -0.13, cz - 0.3), (0.5, 0.008, 0.08), "fabric_ribbon", rot=(0, 25, 0))  # cinta
    m.box((0.12, -0.13, cz - 0.52), (0.08, 0.008, 0.4), "fabric_ribbon", rot=(0, -12, 0))
    m.box((-0.12, -0.13, cz - 0.52), (0.08, 0.008, 0.4), "fabric_ribbon", rot=(0, 12, 0))
    return m.finish("FlowerWreath")


def make_candles():
    rng = random.Random(3)
    m = Mesher("candles", {"plastic_wax": (0.86, 0.82, 0.7), "plastic_wick": (0.05, 0.05, 0.05), "plastic_glass": (0.55, 0.12, 0.08)})
    m.loft_z([(0, 0, 0, 0.3, 0.22), (0, 0, 0.006, 0.28, 0.2)], "plastic_wax", 20)   # cera derramada
    for i in range(9):
        a = rng.uniform(0, 2 * math.pi); r = rng.uniform(0.0, 0.2)
        x, y = r * math.cos(a), r * math.sin(a) * 0.7
        h = rng.uniform(0.06, 0.3); rad = rng.uniform(0.022, 0.04)
        if rng.random() < 0.3:                            # vela en vaso rojo
            m.loft_z([(x, y, 0, 0.04, 0.04), (x, y, 0.11, 0.045, 0.045)], "plastic_glass", 14)
            m.loft_z([(x, y, 0.02, 0.035, 0.035), (x, y, 0.06, 0.035, 0.035)], "plastic_wax", 12)
            continue
        m.loft_z([(x, y, 0, rad * 1.15, rad * 1.15), (x, y, 0.02, rad, rad), (x, y, h, rad, rad), (x, y, h + 0.008, rad * 0.8, rad * 0.8)], "plastic_wax", 12)
        m.loft_z([(x, y, h, 0.003, 0.003), (x, y, h + 0.02, 0.002, 0.002)], "plastic_wick", 4)
        for d in range(2):                                # chorretones
            ad = rng.uniform(0, 2 * math.pi)
            m.ellipsoid((x + rad * math.cos(ad), y + rad * math.sin(ad), h * rng.uniform(0.4, 0.9)), (0.008, 0.008, h * 0.25), "plastic_wax", 6, 4)
    return m.finish("Candles")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_monument, make_medallion, make_plaque, make_stanchion, make_rope, make_plant, make_wreath, make_candles):
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
