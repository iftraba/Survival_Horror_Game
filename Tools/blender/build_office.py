"""Comisaria v2, fase 3 (2026-10-08): mobiliario de la primera planta (oficinas, despacho del comisario, sala de conferencias).

    blender --background --python build_office.py -- <carpeta Assets/_Project/Art/Props/Office>

Origen en el centro de la base; el frente (lo que mira al usuario: la pantalla, el asiento, el frente de la mesa) a -Y en
Blender (= +Z en Unity). Los objetos de sobremesa (ordenador, portatil, papeles, lampara) tienen la base en z = 0 y se ponen
encima de la mesa (Desk.fbx mide 0,76 de alto). Los nombres de material deciden el material horneado (texture_items.py).
"""
import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())


def make_desk_computer():
    """Monitor plano con su pie, teclado, raton con alfombrilla. Ancho 0,6 m."""
    m = Mesher("deskpc", {"plastic": (0.10, 0.10, 0.11), "screen": (0.02, 0.03, 0.04), "keys": (0.75, 0.74, 0.70), "cardboard": (0.15, 0.15, 0.17)})
    m.box((0, 0.12, 0.01), (0.22, 0.16, 0.02), "plastic", bevel=0.006)                         # pie
    m.box((0, 0.13, 0.12), (0.05, 0.035, 0.2), "plastic")
    m.box((0, 0.1, 0.3), (0.56, 0.05, 0.34), "plastic", bevel=0.012)                           # monitor
    m.box((0, 0.074, 0.305), (0.52, 0.004, 0.3), "screen")
    m.box((0, -0.16, 0.012), (0.44, 0.15, 0.024), "plastic", bevel=0.006)                      # teclado
    for r in range(4):
        for c in range(14):
            m.box((-0.195 + c * 0.03, -0.205 + r * 0.03, 0.026), (0.024, 0.024, 0.008), "keys")
    m.box((0.32, -0.15, 0.002), (0.2, 0.17, 0.004), "cardboard")                               # alfombrilla
    m.box((0.32, -0.15, 0.016), (0.06, 0.1, 0.03), "plastic", bevel=0.012)                     # raton
    return m.finish("DeskComputer")


def make_laptop():
    m = Mesher("laptop", {"plastic": (0.18, 0.18, 0.19), "screen": (0.02, 0.03, 0.05), "keys": (0.06, 0.06, 0.06)})
    m.box((0, -0.03, 0.01), (0.34, 0.24, 0.02), "plastic", bevel=0.006)
    m.box((0, -0.05, 0.021), (0.28, 0.1, 0.004), "keys")
    m.box((0, 0.1, 0.12), (0.34, 0.012, 0.22), "plastic", rot=(-15, 0, 0), bevel=0.004)       # pantalla abierta
    m.box((0, 0.093, 0.12), (0.3, 0.003, 0.19), "screen", rot=(-15, 0, 0))
    return m.finish("Laptop")


def make_paper_stack():
    m = Mesher("papers", {"paper": (0.88, 0.87, 0.82), "cardboard": (0.72, 0.58, 0.32), "paper2": (0.85, 0.82, 0.6)})
    for i in range(7):
        m.box((0.01 * math.sin(i), 0.008 * math.cos(i * 1.7), 0.004 + i * 0.006), (0.21, 0.297, 0.005), "paper" if i % 3 else "paper2", rot=(0, 0, (i * 37) % 9 - 4))
    m.box((0.27, 0.02, 0.012), (0.24, 0.32, 0.024), "cardboard", rot=(0, 0, 8), bevel=0.003)   # carpeta
    return m.finish("PaperStack")


def make_desk_lamp():
    m = Mesher("desklamp", {"metal": (0.15, 0.25, 0.2), "steel": (0.6, 0.6, 0.62)})
    m.loft_z([(0, 0, 0, 0.08, 0.08), (0, 0, 0.025, 0.08, 0.08)], "metal", 16)
    m.box((0, 0.02, 0.18), (0.02, 0.02, 0.33), "steel", rot=(-15, 0, 0))
    m.box((0, -0.08, 0.36), (0.02, 0.24, 0.02), "steel", rot=(-20, 0, 0))
    m.loft_z([(0, -0.19, 0.27, 0.07, 0.07), (0, -0.19, 0.34, 0.03, 0.03)], "metal", 14)
    return m.finish("DeskLamp")


def make_trash_bin():
    m = Mesher("trashbin", {"metal": (0.3, 0.32, 0.33), "paper": (0.85, 0.84, 0.8)})
    m.loft_z([(0, 0, 0, 0.13, 0.13), (0, 0, 0.36, 0.15, 0.15)], "metal", 16)
    for i in range(3):
        m.ellipsoid((0.04 * math.cos(i * 2), 0.04 * math.sin(i * 2), 0.36), (0.05, 0.05, 0.04), "paper", 8, 4)
    return m.finish("TrashBin")


def make_water_cooler():
    m = Mesher("watercooler", {"plastic": (0.85, 0.85, 0.82), "glass": (0.35, 0.55, 0.75), "keys": (0.1, 0.1, 0.1)})
    m.box((0, 0, 0.5), (0.32, 0.32, 1.0), "plastic", bevel=0.02)
    m.loft_z([(0, 0, 1.0, 0.13, 0.13), (0, 0, 1.38, 0.14, 0.14), (0, 0, 1.44, 0.06, 0.06)], "glass", 16)
    for x in (-0.06, 0.06):
        m.box((x, -0.17, 0.75), (0.03, 0.04, 0.05), "keys")
    m.box((0, -0.13, 0.62), (0.2, 0.08, 0.02), "keys")
    return m.finish("WaterCooler")


def make_whiteboard():
    m = Mesher("whiteboard", {"plastic": (0.92, 0.92, 0.9), "steel": (0.6, 0.6, 0.62), "marker": (0.1, 0.2, 0.6)})
    m.box((0, 0, 1.35), (1.6, 0.03, 1.0), "plastic", bevel=0.005)
    for (x, z, sx, sz) in ((0, 1.86, 1.64, 0.03), (0, 0.84, 1.64, 0.03), (-0.81, 1.35, 0.03, 1.04), (0.81, 1.35, 0.03, 1.04)):
        m.box((x, 0, z), (sx, 0.04, sz), "steel")
    m.box((0, -0.05, 0.82), (1.2, 0.08, 0.02), "steel")                                         # bandeja
    m.box((-0.2, -0.05, 0.84), (0.12, 0.02, 0.02), "marker")
    for x in (-0.7, 0.7):
        m.box((x, 0, 0.42), (0.04, 0.04, 0.84), "steel")
        m.box((x, 0, 0.02), (0.06, 0.5, 0.04), "steel")
    for i in range(5):                                                                          # garabatos de la reunion
        m.box((-0.5 + i * 0.22, -0.016, 1.5 - (i % 2) * 0.25), (0.16, 0.002, 0.012), "marker", rot=(0, (i * 23) % 30 - 15, 0))
    return m.finish("Whiteboard")


def make_swivel_chair():
    m = Mesher("swivelchair", {"fabric": (0.12, 0.13, 0.16), "plastic": (0.06, 0.06, 0.06), "steel": (0.5, 0.5, 0.52)})
    for k in range(5):                                                                          # base de estrella con ruedas
        a = 2 * math.pi * k / 5
        m.box((0.16 * math.cos(a), 0.16 * math.sin(a), 0.06), (0.3, 0.04, 0.03), "plastic", rot=(0, 0, math.degrees(a)))
        m.ellipsoid((0.3 * math.cos(a), 0.3 * math.sin(a), 0.03), (0.03, 0.03, 0.03), "plastic", 8, 4)
    m.loft_z([(0, 0, 0.07, 0.03, 0.03), (0, 0, 0.42, 0.025, 0.025)], "steel", 10)
    m.box((0, 0, 0.47), (0.5, 0.48, 0.08), "fabric", bevel=0.03)
    m.box((0, 0.24, 0.8), (0.46, 0.07, 0.56), "fabric", rot=(8, 0, 0), bevel=0.03)
    for x in (-0.26, 0.26):
        m.box((x, 0.0, 0.62), (0.04, 0.3, 0.03), "plastic", bevel=0.01)
        m.box((x, 0.06, 0.55), (0.03, 0.03, 0.14), "plastic")
    return m.finish("SwivelChair")


def make_coat_rack():
    m = Mesher("coatrack", {"wood": (0.3, 0.18, 0.09), "fabric": (0.2, 0.18, 0.15)})
    m.loft_z([(0, 0, 0, 0.03, 0.03), (0, 0, 1.75, 0.025, 0.025)], "wood", 10)
    for k in range(4):
        a = 2 * math.pi * k / 4
        m.box((0.18 * math.cos(a), 0.18 * math.sin(a), 0.04), (0.38, 0.04, 0.04), "wood", rot=(0, 0, math.degrees(a)))
        m.box((0.09 * math.cos(a), 0.09 * math.sin(a), 1.65), (0.2, 0.025, 0.025), "wood", rot=(0, -30, math.degrees(a)))
    m.box((0.12, 0.0, 1.25), (0.08, 0.32, 0.7), "fabric", bevel=0.04)                          # abrigo colgado
    return m.finish("CoatRack")


def make_executive_desk():
    m = Mesher("execdesk", {"wood": (0.26, 0.13, 0.06), "board": (0.2, 0.1, 0.05), "fabric": (0.12, 0.22, 0.14), "brass": (0.7, 0.55, 0.25)})
    W, D, H = 2.0, 0.95, 0.78
    m.box((0, 0, H - 0.025), (W, D, 0.05), "wood", bevel=0.015)
    m.box((0, 0.02, H - 0.048), (W - 0.6, D - 0.3, 0.005), "fabric")                           # tapete
    for x in (-W / 2 + 0.25, W / 2 - 0.25):                                                    # cajoneras
        m.box((x, 0, (H - 0.05) / 2), (0.46, D - 0.08, H - 0.05), "wood", bevel=0.01)
        for z in (0.16, 0.38, 0.6):
            m.box((x, -(D - 0.08) / 2 - 0.006, z), (0.4, 0.012, 0.18), "board", bevel=0.005)
            m.box((x, -(D - 0.08) / 2 - 0.02, z), (0.08, 0.015, 0.015), "brass")
    m.box((0, D / 2 - 0.08, (H - 0.05) / 2 + 0.05), (W - 1.0, 0.03, H - 0.25), "board", bevel=0.006)   # faldon (lado del visitante)
    return m.finish("ExecutiveDesk")


def make_executive_chair():
    m = Mesher("execchair", {"fabric": (0.08, 0.05, 0.04), "plastic": (0.05, 0.05, 0.05), "steel": (0.5, 0.5, 0.52)})
    for k in range(5):
        a = 2 * math.pi * k / 5
        m.box((0.17 * math.cos(a), 0.17 * math.sin(a), 0.06), (0.34, 0.05, 0.035), "steel", rot=(0, 0, math.degrees(a)))
        m.ellipsoid((0.33 * math.cos(a), 0.33 * math.sin(a), 0.03), (0.035, 0.035, 0.03), "plastic", 8, 4)
    m.loft_z([(0, 0, 0.07, 0.035, 0.035), (0, 0, 0.42, 0.03, 0.03)], "steel", 10)
    m.box((0, 0, 0.5), (0.6, 0.56, 0.12), "fabric", bevel=0.05)
    m.box((0, 0.28, 0.98), (0.58, 0.12, 0.9), "fabric", rot=(10, 0, 0), bevel=0.06)            # respaldo alto de piel
    for x in (-0.31, 0.31):
        m.box((x, 0.02, 0.68), (0.07, 0.46, 0.08), "fabric", bevel=0.03)
    return m.finish("ExecutiveChair")


def make_bookcase():
    """Libreria de 1,2 m con libros de colores, algunos tumbados, y alguna caja."""
    palette = {"wood": (0.25, 0.14, 0.07)}
    cols = [(0.45, 0.08, 0.06), (0.08, 0.15, 0.35), (0.1, 0.3, 0.12), (0.5, 0.42, 0.25), (0.15, 0.12, 0.1), (0.55, 0.5, 0.42)]
    for i, c in enumerate(cols):
        palette["book%d" % i] = c
    m = Mesher("bookcase", palette)
    W, D, H = 1.2, 0.36, 2.1
    m.box((-W / 2 + 0.015, 0, H / 2), (0.03, D, H), "wood", bevel=0.005)
    m.box((W / 2 - 0.015, 0, H / 2), (0.03, D, H), "wood", bevel=0.005)
    m.box((0, D / 2 - 0.01, H / 2), (W, 0.02, H), "wood")
    shelves = [0.06, 0.48, 0.9, 1.32, 1.74, 2.08]
    for z in shelves:
        m.box((0, 0, z), (W, D, 0.03), "wood", bevel=0.004)
    import random
    rnd = random.Random(5)
    for s in range(5):
        z0 = shelves[s] + 0.015
        x = -W / 2 + 0.05
        while x < W / 2 - 0.08:
            if rnd.random() < 0.08:                                                             # hueco
                x += 0.12; continue
            w = rnd.uniform(0.025, 0.06); h = rnd.uniform(0.22, 0.34)
            mat = "book%d" % rnd.randrange(len(cols))
            if rnd.random() < 0.07 and x < W / 2 - 0.3:                                          # montoncito tumbado
                for k in range(3):
                    m.box((x + 0.12, 0.0, z0 + 0.02 + k * 0.04), (0.22, 0.26, 0.038), "book%d" % rnd.randrange(len(cols)), bevel=0.003)
                x += 0.26; continue
            tilt = rnd.uniform(-6, 6) if rnd.random() < 0.2 else 0
            m.box((x + w / 2, rnd.uniform(-0.02, 0.02), z0 + h / 2), (w, 0.24, h), mat, rot=(0, tilt, 0), bevel=0.003)
            x += w + 0.004
    return m.finish("Bookcase")


def make_trophy_cabinet():
    m = Mesher("trophycab", {"wood": (0.25, 0.13, 0.06), "glass": (0.6, 0.7, 0.75), "brass": (0.75, 0.58, 0.2), "gold": (0.8, 0.62, 0.2), "fabric": (0.3, 0.05, 0.05)})
    W, D, H = 1.0, 0.42, 1.9
    m.box((0, 0, 0.2), (W, D, 0.4), "wood", bevel=0.01)
    m.box((0, 0, H - 0.04), (W + 0.04, D + 0.04, 0.08), "wood", bevel=0.01)
    for x in (-W / 2 + 0.02, W / 2 - 0.02):
        m.box((x, 0, H / 2), (0.04, D, H), "wood")
    m.box((0, D / 2 - 0.01, H / 2), (W, 0.02, H), "fabric")
    for z in (0.95, 1.4):
        m.box((0, 0, z), (W - 0.06, D - 0.04, 0.012), "glass")
    for (x, z, k) in ((-0.25, 0.42, 0), (0.05, 0.42, 1), (0.28, 0.42, 2), (-0.2, 0.96, 1), (0.2, 0.96, 0), (0, 1.41, 2)):
        m.box((x, 0, z + 0.03), (0.1, 0.1, 0.06), "wood")
        if k == 0:
            m.loft_z([(x, 0, z + 0.06, 0.02, 0.02), (x, 0, z + 0.16, 0.015, 0.015), (x, 0, z + 0.2, 0.06, 0.06), (x, 0, z + 0.3, 0.07, 0.07)], "gold", 14)
        elif k == 1:
            m.box((x, 0, z + 0.2), (0.12, 0.02, 0.28), "brass", bevel=0.01)                      # placa
        else:
            m.ellipsoid((x, 0, z + 0.16), (0.06, 0.06, 0.1), "gold", 12, 6)
    m.box((0, -D / 2 + 0.005, 1.15), (W - 0.06, 0.006, 1.42), "glass")                         # puerta de cristal
    return m.finish("TrophyCabinet")


def make_rug():
    m = Mesher("rug", {"fabric": (0.35, 0.08, 0.06), "fabric2": (0.55, 0.45, 0.25)})
    m.box((0, 0, 0.005), (2.6, 1.8, 0.01), "fabric")
    m.box((0, 0, 0.0105), (2.3, 1.5, 0.002), "fabric2")
    m.box((0, 0, 0.012), (2.1, 1.3, 0.002), "fabric")
    return m.finish("Rug")


def make_auditorium_chair():
    """Silla plegable de sala de actos con asiento y respaldo acolchados."""
    m = Mesher("audchair", {"fabric": (0.1, 0.12, 0.25), "steel": (0.35, 0.36, 0.38)})
    for x in (-0.22, 0.22):
        m.box((x, -0.18, 0.23), (0.03, 0.03, 0.46), "steel")
        m.box((x, 0.2, 0.45), (0.03, 0.03, 0.9), "steel", rot=(6, 0, 0))
    m.box((0, 0, 0.47), (0.48, 0.44, 0.06), "fabric", bevel=0.02)
    m.box((0, 0.22, 0.76), (0.46, 0.05, 0.36), "fabric", rot=(8, 0, 0), bevel=0.02)
    m.box((0, 0, 0.15), (0.46, 0.03, 0.03), "steel")
    return m.finish("AuditoriumChair")


def make_lectern():
    m = Mesher("lectern", {"wood": (0.25, 0.13, 0.06), "board": (0.18, 0.09, 0.05), "steel": (0.5, 0.5, 0.52), "plastic": (0.05, 0.05, 0.05), "brass": (0.75, 0.58, 0.2)})
    m.box((0, 0, 0.04), (0.7, 0.55, 0.08), "board", bevel=0.01)
    m.box((0, 0.02, 0.55), (0.56, 0.42, 0.95), "wood", bevel=0.02)
    m.box((0, -0.03, 1.06), (0.66, 0.5, 0.05), "wood", rot=(-15, 0, 0), bevel=0.01)             # tablero inclinado
    m.box((0, -0.23, 0.6), (0.3, 0.01, 0.3), "brass", bevel=0.005)                             # escudo
    m.loft_z([(0.18, 0.05, 1.08, 0.012, 0.012), (0.18, 0.05, 1.3, 0.008, 0.008)], "steel", 8)   # microfono de cuello
    m.box((0.18, -0.02, 1.35), (0.02, 0.16, 0.02), "steel", rot=(-30, 0, 0))
    m.ellipsoid((0.18, -0.1, 1.38), (0.022, 0.035, 0.022), "plastic", 10, 6)
    return m.finish("Lectern")


def make_flag_pole():
    """Mastil de bandera con base y remate dorado (la tela la pone Unity, con su textura)."""
    m = Mesher("flagpole", {"wood": (0.2, 0.1, 0.05), "gold": (0.8, 0.62, 0.2)})
    m.loft_z([(0, 0, 0, 0.18, 0.18), (0, 0, 0.06, 0.16, 0.16), (0, 0, 0.1, 0.06, 0.06)], "gold", 16)
    m.loft_z([(0, 0, 0.1, 0.022, 0.022), (0, 0, 2.5, 0.018, 0.018)], "wood", 10)
    m.ellipsoid((0, 0, 2.55), (0.045, 0.045, 0.06), "gold", 12, 6)
    return m.finish("FlagPole")


if __name__ == "__main__":
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    fns = (make_desk_computer, make_laptop, make_paper_stack, make_desk_lamp, make_trash_bin, make_water_cooler, make_whiteboard,
           make_swivel_chair, make_coat_rack, make_executive_desk, make_executive_chair, make_bookcase, make_trophy_cabinet,
           make_rug, make_auditorium_chair, make_lectern, make_flag_pole)
    for fn in fns:
        _clear()
        obj = fn()
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
