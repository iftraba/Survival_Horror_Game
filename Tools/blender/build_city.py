"""Piezas para las fachadas y azoteas de los edificios de la ciudad (2026-10-14, fase 3 del lavado de cara del exterior).

    blender --background --python build_city.py -- <carpeta ABSOLUTA de salida> [Nombre ...]
    (en un Blender abierto: exec(open(ruta).read()); preview("RoofAC"); preview_all())

Modelos: Awning, AwningTorn, BoardsX, RoofAC, WaterTower, Chimney, Dish, Antenna, RoofAccess, FireEscape.
Convenciones: los toldos, tablones y la escalera de incendios cuelgan de una pared que queda a +Y de Blender (frente = -Y = +Z de Unity); el origen esta en el
punto de anclaje a la pared (toldo: borde alto, centro; tablones: centro de la ventana; escalera: base, centro de la pared). Azotea: origen en el centro de la base.
"""
import math
import os
import random
import sys

_NO_MAIN = True
HERE3 = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE3, "build_street.py"), encoding="utf-8").read())

CITY = dict(BASE, fabric_red=(0.45, 0.06, 0.05), fabric_cream=(0.72, 0.66, 0.5), fabric_green=(0.07, 0.25, 0.14), concrete_brick=(0.38, 0.15, 0.1), metal_rust=(0.3, 0.12, 0.06))


def make_awning(torn=False, seed=1):
    m = new("awning", CITY)
    rng = random.Random(seed)
    W, D, H = 1.6, 0.95, 0.55                                   # ancho, vuelo hacia delante, caida
    n = 8
    red, cream = ("fabric_red", "fabric_cream") if not torn else ("fabric_green", "fabric_cream")
    for i in range(n):
        x0, x1 = -W / 2 + W * i / n, -W / 2 + W * (i + 1) / n
        mat = red if i % 2 == 0 else cream
        droop = 0.0
        if torn and i in (3, 4):
            droop = 0.28                                          # franjas rotas que cuelgan
        a = (x0, 0.0, 0.0)
        b = (x1, 0.0, 0.0)
        c = (x1, -D, -H - droop * 0.2)
        d = (x0, -D, -H - droop * 0.2)
        if droop:
            c = (x1 + 0.02, -D * 0.7, -H - droop)
            d = (x0 - 0.02, -D * 0.7, -H - droop)
        vs = [m.bm.verts.new(p) for p in (a, d, c, b)]
        f = m.bm.faces.new(vs)
        f.material_index = m._idx(mat)
        f.smooth = False
        # el reverso del toldo (que se vea desde abajo)
        vs2 = [m.bm.verts.new(p) for p in (a, b, c, d)]
        f2 = m.bm.faces.new(vs2)
        f2.material_index = m._idx(mat)
        f2.smooth = False
        m._deco.append((f, Vector((0, -0.4, 1.0)).normalized()))
        m._deco.append((f2, Vector((0, 0.4, -1.0)).normalized()))
        # volante frontal
        if not (torn and i in (3, 4)):
            for (xa, xb) in ((x0, x1),):
                pts = [(xa, -D, -H), (xb, -D, -H), (xb, -D - 0.015, -H - 0.2), (xa, -D - 0.015, -H - 0.2)]
                vs3 = [m.bm.verts.new(p) for p in pts]
                f3 = m.bm.faces.new(vs3)
                f3.material_index = m._idx(mat)
                f3.smooth = False
                m._deco.append((f3, Vector((0, -1, 0))))
                vs4 = [m.bm.verts.new(p) for p in reversed(pts)]
                f4 = m.bm.faces.new(vs4)
                f4.material_index = m._idx(mat)
                f4.smooth = False
                m._deco.append((f4, Vector((0, 1, 0))))
    # estructura: brazos y barra frontal
    for sx in (-W / 2, 0.0, W / 2):
        tube(m, [(sx, 0.0, 0.0), (sx, -D, -H)], 0.012, "metal_dark", 6)
    tube(m, [(-W / 2, -D, -H), (W / 2, -D, -H)], 0.014, "metal_dark", 6)
    m.box((0, 0.02, 0.0), (W + 0.05, 0.04, 0.05), "metal_dark")
    return finish(m, "AwningTorn" if torn else "Awning")


def make_boards():
    m = new("boards", CITY)
    rng = random.Random(5)
    W, H = 1.05, 1.7
    for k, z in enumerate((-0.52, -0.17, 0.17, 0.52)):
        w = W + rng.uniform(-0.05, 0.12)
        m.box((rng.uniform(-0.05, 0.05), -0.03, z), (w, 0.04, 0.2), "wood", bevel=0.004, rot=(0, rng.uniform(-2, 2), 0))
    for sgn in (-1, 1):
        ang = math.degrees(math.atan2(H * 0.78, W)) * sgn
        m.box((0.0, -0.065, 0.0), (1.62, 0.035, 0.17), "wood", bevel=0.004, rot=(0, ang, 0))
    for x in (-0.45, 0.45):
        for z in (-0.52, 0.52):
            m.box((x, -0.088, z), (0.03, 0.02, 0.03), "metal_dark")
    return finish(m, "BoardsX")


def make_roof_ac():
    m = new("roofac", CITY)
    m.box((0, 0, 0.12), (1.6, 1.15, 0.24), "concrete", bevel=0.01)                                   # peana
    m.box((0, 0, 0.62), (1.4, 1.0, 0.78), "carpaint_white", bevel=0.03)                              # caja
    for i in range(7):
        m.box((0, -0.505, 0.35 + i * 0.07), (1.2, 0.012, 0.03), "metal_dark")                          # rejilla lateral
    cyl(m, (0.0, 0.0, 1.0), 0.4, 0.04, "metal_dark", 20)                                                # aro del ventilador
    cyl(m, (0.0, 0.0, 1.0), 0.03, 0.1, "metal", 8)
    for k in range(5):                                                                                  # aspas y rejilla
        a = k * 2 * math.pi / 5
        m.box((0.18 * math.cos(a), 0.18 * math.sin(a), 1.07), (0.34, 0.07, 0.01), "metal", rot=(0, 0, math.degrees(a)))
    for r in (0.15, 0.3, 0.4):
        tube(m, [(r * math.cos(2 * math.pi * k / 20), r * math.sin(2 * math.pi * k / 20), 1.1) for k in range(21)], 0.006, "metal_dark", 4)
    tube(m, [(0.7, 0.2, 0.5), (0.95, 0.2, 0.5), (0.95, 0.2, 0.05), (1.4, 0.2, 0.05)], 0.05, "metal", 8)  # tuberia
    m.box((-0.5, -0.52, 0.8), (0.18, 0.05, 0.14), "metal_dark", bevel=0.01)                            # caja electrica
    return finish(m, "RoofAC")


def make_water_tower():
    m = new("watertower", CITY)
    R, H0, H = 1.25, 3.0, 2.4
    for k in range(6):                                                                                 # patas de acero con cruces
        a = k * math.pi / 3
        x, y = (R - 0.1) * math.cos(a), (R - 0.1) * math.sin(a)
        tube(m, [(x, y, 0), (x, y, H0)], 0.05, "metal_rust", 6)
    for z in (0.9, 1.9):
        tube(m, [((R - 0.1) * math.cos(k * math.pi / 3), (R - 0.1) * math.sin(k * math.pi / 3), z) for k in range(7)], 0.025, "metal_rust", 5)
    for k in range(6):
        a1, a2 = k * math.pi / 3, (k + 1) * math.pi / 3
        tube(m, [((R - 0.1) * math.cos(a1), (R - 0.1) * math.sin(a1), 0.9), ((R - 0.1) * math.cos(a2), (R - 0.1) * math.sin(a2), 1.9)], 0.018, "metal_rust", 5)
    m.box((0, 0, H0 + 0.05), (2 * R, 2 * R, 0.1), "wood", bevel=0.01) if False else cyl(m, (0, 0, H0), R, 0.1, "wood", 20)
    for k in range(24):                                                                                # duelas
        a1, a2 = 2 * math.pi * k / 24, 2 * math.pi * (k + 1) / 24
        m._loft_rings([[(R * math.cos(a1), R * math.sin(a1), H0 + 0.1), (R * math.cos(a2), R * math.sin(a2), H0 + 0.1)], [(R * math.cos(a1) * 1.0, R * math.sin(a1), H0 + 0.1 + H), (R * math.cos(a2), R * math.sin(a2), H0 + 0.1 + H)]], "wood", False) if False else None
    cyl(m, (0, 0, H0 + 0.1), R, H, "wood", 24)
    for z in (H0 + 0.5, H0 + 1.3, H0 + 2.0):                                                           # aros de hierro
        m._loft_rings([[((R + 0.012 + 0.0) * math.cos(2 * math.pi * k / 24), (R + 0.012) * math.sin(2 * math.pi * k / 24), z + dz) for k in range(24)] for dz in (-0.03, 0.03)], "metal_rust", True)
    cyl(m, (0, 0, H0 + 0.1 + H), R + 0.05, 0.05, "metal_rust", 24, R + 0.02)
    cyl(m, (0, 0, H0 + 0.15 + H), R + 0.02, 0.9, "wood", 24, 0.12)                                      # techo conico
    tube(m, [(R + 0.1, 0.0, H0 + 2.4), (R + 0.1, 0.0, 0.2)], 0.025, "metal_rust", 6)                   # escalera lateral
    for z in range(0, int((H0 + 2.4) / 0.35)):
        m.box((R + 0.1, 0.0, 0.3 + z * 0.35), (0.04, 0.34, 0.03), "metal_rust")
    tube(m, [(0.4, 0.0, H0 + 0.1), (0.4, 0.0, 1.6), (1.2, 0.0, 1.6), (1.3, 0.0, 0.0)], 0.04, "metal_rust", 6)         # tuberia de bajada
    return finish(m, "WaterTower")


def make_chimney():
    m = new("chimney", CITY)
    m.box((0, 0, 1.4), (0.8, 0.8, 2.8), "concrete_brick", bevel=0.01)
    for z in (0.0, 1.2, 2.4):
        pass
    m.box((0, 0, 2.86), (0.98, 0.98, 0.14), "concrete", bevel=0.02)
    m.box((0, 0, 2.73), (0.9, 0.9, 0.06), "concrete_brick")
    cyl(m, (0, 0, 2.86), 0.31, 0.02, "metal_dark", 12)
    return finish(m, "Chimney")


def make_dish():
    m = new("dish", CITY)
    tube(m, [(0, 0, 0), (0, 0, 0.9)], 0.03, "metal", 8)
    m.box((0, 0, 0.02), (0.36, 0.36, 0.04), "metal_dark", bevel=0.005)
    rings = []
    for k in range(6):
        t = k / 5
        r = 0.5 * t
        z = 0.95 + 0.15 * (1 - t * t) * -1 + 0.0
        rings.append([(r * math.cos(2 * math.pi * j / 18), -0.12 - 0.0 + r * math.sin(2 * math.pi * j / 18) * 0.0, 0.0) for j in range(18)])
    # plato inclinado hacia arriba y hacia -Y
    pts = []
    for ring_i in range(6):
        t = ring_i / 5
        r = 0.55 * t
        pts.append([(r * math.cos(2 * math.pi * j / 20), r * math.sin(2 * math.pi * j / 20), 0.12 * (1 - t * t) * 0 + 0.12 * t * t) for j in range(20)])
    # rotar el plato 55 grados sobre X y colocarlo en lo alto del poste
    rot = Matrix.Rotation(math.radians(-55), 3, "X")
    out = [[tuple(rot @ Vector(p) + Vector((0, 0, 1.0))) for p in ring] for ring in pts]
    m._loft_rings(out, "carpaint_white", True)
    tube(m, [tuple(rot @ Vector((0, 0, 0.0)) + Vector((0, 0, 1.0))), tuple(rot @ Vector((0, 0.0, -0.45)) + Vector((0, 0, 1.0)))], 0.012, "metal", 6)
    m.box(tuple(rot @ Vector((0, 0.0, -0.45)) + Vector((0, 0, 1.0))), (0.05, 0.05, 0.08), "plastic", bevel=0.01)
    return finish(m, "Dish")


def make_antenna():
    m = new("antenna", CITY)
    tube(m, [(0, 0, 0), (0, 0, 2.6)], 0.025, "metal", 8)
    m.box((0, 0, 0.02), (0.3, 0.3, 0.04), "metal_dark")
    for i, z in enumerate((1.2, 1.55, 1.9, 2.25)):
        w = 1.0 - i * 0.18
        m.box((0, 0, z), (0.01, w * 1.1, 0.012), "metal")
    for k in range(3):                                                                                  # tirantes
        a = k * 2 * math.pi / 3
        tube(m, [(0, 0, 1.8), (0.9 * math.cos(a), 0.9 * math.sin(a), 0.0)], 0.005, "metal_dark", 4)
    return finish(m, "Antenna")


def make_roof_access():
    m = new("roofaccess", CITY)
    m.box((0, 0, 1.15), (2.2, 2.6, 2.3), "concrete_brick", bevel=0.015)
    m.box((0, 0, 2.34), (2.4, 2.8, 0.1), "concrete", bevel=0.02)
    m.box((0, -1.305, 0.95), (0.95, 0.04, 1.9), "metal_rust", bevel=0.01)                              # puerta metalica
    m.box((0.3, -1.33, 0.95), (0.06, 0.04, 0.12), "metal_dark")
    m.box((0, -1.35, 2.0), (1.1, 0.12, 0.1), "concrete", bevel=0.01)                                    # dintel
    for y in (-0.6, 0.6):
        m.box((1.12, y, 1.4), (0.04, 0.5, 0.35), "metal_dark", bevel=0.005)                              # rejillas
    tube(m, [(-0.6, 0.8, 2.34), (-0.6, 0.8, 3.1)], 0.07, "metal", 8)                                    # conducto de ventilacion
    cyl(m, (-0.6, 0.8, 3.1), 0.14, 0.07, "metal", 10, 0.2)
    return finish(m, "RoofAccess")


def make_fire_escape():
    m = new("fireescape", CITY)
    W, D, FH = 3.0, 1.1, 3.0                                                                           # ancho, vuelo, altura de planta
    floors = (FH, 2 * FH, 3 * FH)
    for z in floors:
        m.box((0, -D / 2, z), (W, D, 0.06), "metal_rust", bevel=0.005)                                   # plataforma
        for k in range(10):
            m.box((-W / 2 + (k + 0.5) * W / 10, -D / 2, z + 0.035), (W / 10 * 0.5, D * 0.94, 0.012), "metal_dark")        # rejilla del suelo
        for sx in (-W / 2, W / 2):
            tube(m, [(sx, 0.0, z), (sx, -D, z)], 0.02, "metal_rust", 6)
        tube(m, [(-W / 2, -D, z + 0.95), (W / 2, -D, z + 0.95)], 0.022, "metal_rust", 6)                  # barandilla
        tube(m, [(-W / 2, -D, z + 0.5), (W / 2, -D, z + 0.5)], 0.015, "metal_rust", 6)
        for sx in (-W / 2, W / 2):
            tube(m, [(sx, 0.0, z + 0.95), (sx, -D, z + 0.95), (sx, -D, z)], 0.022, "metal_rust", 6)
        for k in range(1, 8):
            tube(m, [(-W / 2 + k * W / 8, -D, z), (-W / 2 + k * W / 8, -D, z + 0.95)], 0.01, "metal_rust", 4)
        # soportes diagonales a la pared
        for sx in (-W / 2 + 0.1, W / 2 - 0.1):
            tube(m, [(sx, -D, z - 0.03), (sx, -0.05, z - 0.9)], 0.02, "metal_rust", 5)
    # escaleras entre plantas (zigzag) y escalera de gato al suelo
    for z0, flip in ((FH, 1), (2 * FH, -1)):
        x_top, x_bot = -W / 2 + 0.45 if flip > 0 else W / 2 - 0.45, W / 2 - 0.45 if flip > 0 else -W / 2 + 0.45
        for sx in (-0.2, 0.2):
            tube(m, [(x_top + sx * 0.0 + 0.0, -0.1 - 0.0, z0 + FH - 0.0 - 0.03), (x_bot, -D + 0.1, z0 - 0.0)], 0.0, "metal_rust", 3) if False else None
        steps = 10
        for s in range(steps):
            t = s / steps
            m.box((x_top + (x_bot - x_top) * t, -D / 2 + 0.0 - 0.0 + (-0.0), (z0 + FH) - (FH) * t - 0.0 - 0.05), (0.48, 0.25, 0.03), "metal_dark") if False else None
    for fl, flip in ((FH, 1), (2 * FH, -1), (3 * FH, 1)):
        pass
    # escalera diagonal de tramo entre cada par de plataformas, pegada al lateral
    for z_low, side in ((0.0, 1), (FH, -1), (2 * FH, 1)):
        x = side * (W / 2 - 0.32)
        for s in range(12):
            t = s / 12
            m.box((x, -0.18 - t * 0.7, z_low + t * FH + 0.03), (0.5, 0.26, 0.03), "metal_dark")
        for sx in (x - 0.26, x + 0.26):
            tube(m, [(sx, -0.12, z_low), (sx, -0.95, z_low + FH)], 0.02, "metal_rust", 5)
    # escalera plegable del suelo
    for k in range(9):
        m.box((W / 2 - 0.8, -0.95, 0.15 + k * 0.3), (0.45, 0.025, 0.025), "metal_rust") if False else None
    return finish(m, "FireEscape")


BUILDERS = {"Awning": lambda: make_awning(False, 1), "AwningTorn": lambda: make_awning(True, 2), "BoardsX": make_boards, "RoofAC": make_roof_ac, "WaterTower": make_water_tower,
            "Chimney": make_chimney, "Dish": make_dish, "Antenna": make_antenna, "RoofAccess": make_roof_access, "FireEscape": make_fire_escape}


def preview(name, rz=35):
    _clear()
    obj = BUILDERS[name]()
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    d = obj.dimensions
    view(75, rz, max(d.x, d.y, d.z) * 2.0 + 1.0, (0, 0, d.z * 0.5))
    print(name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    return obj


def preview_all():
    _clear()
    x = 0.0
    for name, fn in BUILDERS.items():
        obj = fn()
        d = obj.dimensions
        obj.location = (x + d.x / 2 + 0.2, 0, 0)
        x += d.x + 1.2
        print(name, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    view(78, 20, x * 0.9, (x / 2, 0, 1.5))


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
