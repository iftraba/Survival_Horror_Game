"""Coches patrulla de la comisaria (v2, 2026-10-14): modelo de nivel AA, en dos versiones.

    blender --background --python build_police_cars.py -- <carpeta Assets/_Project/Art/Props/Exterior> [PoliceCar PoliceCarClean]
    (en un Blender abierto: exec(open(ruta).read()); preview("clean") o preview("burnt"))

  PoliceCar       quemado y abandonado: chapa chamuscada y oxidada, neumaticos desinflados, luces apagadas
  PoliceCarClean  intacto: bicolor negro y blanco, barra de luces, rotulos POLICE y placa, faros
Mismas medidas que el modelo anterior (4,7 x 1,82 m, origen en el centro de la base, frente a -Y de Blender = +Z de Unity), asi que sustituye al
modelo antiguo sin tocar la escena. Los nombres de material deciden el material horneado de texture_items.py (body* = chapa pintada,
rust = chamusco/oxido, metal* = cromo; el resto plastico).

Carroceria: una sola malla por secciones (cuerpo + habitaculo) suavizada con subdivision y con los pasos de rueda recortados con booleanos; encima,
piezas sueltas (parachoques, faros, rejilla, retrovisores, barra de luces, ruedas con neumatico, llanta, disco y pinza) y rotulos de texto en malla.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())

L, W = 4.7, 1.82
WHEEL_Y = (-1.42, 1.42)
WHEEL_X = 0.785
WHEEL_R = 0.36

# (y, base, cintura, techo, semi-ancho en cintura, semi-ancho del techo)
STATIONS = [
    (-2.35, 0.36, 0.60, 0.64, 0.72, 0.58),
    (-2.32, 0.27, 0.66, 0.70, 0.83, 0.68),
    (-2.22, 0.23, 0.76, 0.80, 0.89, 0.74),
    (-1.90, 0.22, 0.86, 0.90, 0.91, 0.78),
    (-1.40, 0.22, 0.90, 0.94, 0.91, 0.78),
    (-0.98, 0.22, 0.94, 0.98, 0.91, 0.77),
    (-0.78, 0.22, 0.95, 1.14, 0.91, 0.72),
    (-0.58, 0.22, 0.95, 1.253, 0.91, 0.70),
    (-0.48, 0.22, 0.95, 1.31, 0.91, 0.69),
    (-0.14, 0.22, 0.95, 1.42, 0.91, 0.67),
    (0.40, 0.22, 0.95, 1.46, 0.91, 0.66),
    (1.00, 0.22, 0.95, 1.45, 0.91, 0.66),
    (1.28, 0.22, 0.95, 1.413, 0.91, 0.664),
    (1.38, 0.22, 0.95, 1.40, 0.91, 0.67),
    (1.68, 0.22, 0.95, 1.20, 0.91, 0.70),
    (1.94, 0.22, 0.95, 1.02, 0.91, 0.76),
    (2.12, 0.22, 0.94, 0.98, 0.91, 0.78),
    (2.27, 0.23, 0.89, 0.93, 0.89, 0.77),
    (2.33, 0.27, 0.82, 0.86, 0.83, 0.70),
    (2.35, 0.36, 0.70, 0.74, 0.72, 0.58),
]


def palette(burnt):
    pal = {"body": (0.88, 0.88, 0.86), "bodyblack": (0.035, 0.035, 0.04), "plastic": (0.035, 0.035, 0.038), "metal": (0.72, 0.74, 0.77), "metal_dark": (0.1, 0.1, 0.11),
           "metal_rim": (0.46, 0.47, 0.5), "glass": (0.04, 0.06, 0.08), "red": (0.8, 0.03, 0.02), "blue": (0.04, 0.12, 0.85), "lights": (0.95, 0.93, 0.82),
           "amber": (0.9, 0.45, 0.04), "plastic_white": (0.9, 0.9, 0.88), "plastic_interior": (0.03, 0.03, 0.035), "rubber": (0.02, 0.02, 0.022),
           "decal_k": (0.02, 0.02, 0.03), "decal_w": (0.92, 0.92, 0.9), "decal_blue": (0.05, 0.13, 0.55), "decal_gold": (0.78, 0.6, 0.18), "rust": (0.16, 0.08, 0.04)}
    if burnt:
        pal.update({"body": (0.44, 0.41, 0.37), "bodyblack": (0.03, 0.025, 0.024), "glass": (0.025, 0.028, 0.03), "lights": (0.2, 0.18, 0.15), "red": (0.22, 0.03, 0.02),
                    "blue": (0.04, 0.06, 0.2), "amber": (0.25, 0.14, 0.04), "decal_w": (0.5, 0.48, 0.44), "decal_blue": (0.04, 0.06, 0.18), "decal_gold": (0.3, 0.24, 0.1)})
    return pal


# ------------------------------------------------------------------ carroceria
def half_profile(st):
    """Perfil de medio corte (x >= 0) de una estacion, de la base al techo."""
    y, zb, zbelt, zroof, hw, hwr = st
    zs = zb + 0.46 * (zbelt - zb)
    z6 = max(zbelt + 0.006, zroof - 0.07)
    z7 = max(z6 + 0.006, zroof)
    return [(0.0, zb), (hw - 0.06, zb), (hw - 0.006, zb + 0.1), (hw, zs), (hw, zbelt - 0.05), (hw - 0.03, zbelt),
            (hwr + 0.03, z6), (hwr - 0.05, z7), (0.0, z7 + 0.012)]


def build_shell(m, pal, burnt=False):
    """Malla (sin suavizar) del cuerpo por secciones; el material de cada cara ya es de paleta."""
    sb = bmesh.new()
    rings = []
    for st in STATIONS:
        hp = half_profile(st)
        ring = [Vector((x, st[0], z)) for (x, z) in hp] + [Vector((-x, st[0], z)) for (x, z) in reversed(hp[1:-1])]
        rings.append([sb.verts.new(p) for p in ring])
    n = len(rings[0])                                  # 16 puntos: 0..8 a la derecha (base -> centro del techo), 9..15 a la izquierda
    for i in range(len(rings) - 1):
        s0, s1 = STATIONS[i], STATIONS[i + 1]
        cab = ((s0[3] - s0[2]) + (s1[3] - s1[2])) * 0.5        # altura del habitaculo sobre la cintura
        slope = (s1[3] - s0[3]) / (s1[0] - s0[0])
        for j in range(n):
            j2 = (j + 1) % n
            f = sb.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
            f.smooth = True
            seg = j if j <= 7 else 15 - j                      # 0 base ... 5 cristal lateral, 6 borde del techo, 7 techo
            mat = "bodyblack"
            in_doors = s0[0] >= -0.581 and s1[0] <= 1.281
            if in_doors and seg in (2, 3, 4):
                mat = "body"                                   # puertas blancas
            if seg == 7 and -0.15 <= s0[0] and s1[0] <= 1.39 and cab > 0.2:
                mat = "body"                                   # techo blanco
            if burnt and cab <= 0.2 and seg in (4, 5, 6, 7):
                mat = "rust"                                   # capo y maletero calcinados
            if burnt and seg == 7 and cab > 0.2 and mat == "body":
                mat = "rust"                                   # techo quemado
            if cab > 0.2:
                if seg == 5:
                    mat = "glass"
                elif seg == 6:
                    mat = "bodyblack"
                elif seg == 7 and abs(slope) > 0.33:
                    mat = "glass"                                # parabrisas y luna trasera
            f.material_index = m._idx(mat)
    for ring in (rings[0], list(reversed(rings[-1]))):
        f = sb.faces.new(ring)
        f.material_index = m._idx("bodyblack")
    cl = sb.edges.layers.float.get("crease_edge") or sb.edges.layers.float.new("crease_edge")
    sharp = {2: 0.55, 4: 0.5, 5: 0.9, 6: 0.9, 7: 0.5}                                       # puntos del perfil que quedan marcados (zocalo, hombro, cintura, borde del techo)
    for ring in rings:
        pass
    for i in range(len(rings) - 1):
        for j in range(n):
            pj = j if j <= 8 else n - j                                                         # indice del punto en el perfil de la derecha
            if pj in sharp:
                e = sb.edges.get((rings[i][j], rings[i + 1][j]))
                if e is not None:
                    e[cl] = sharp[pj]
    for ring in (rings[0], rings[1], rings[-2], rings[-1]):
        for j in range(n):
            e = sb.edges.get((ring[j], ring[(j + 1) % n]))
            if e is not None:
                e[cl] = max(e[cl], 0.4)
    mesh = bpy.data.meshes.new("shell")
    sb.normal_update()
    sb.to_mesh(mesh)
    sb.free()
    return mesh


def shell_object(m, pal, burnt):
    mesh = build_shell(m, pal, burnt)
    # los indices de material del shell son indices de paleta: hace falta un hueco por cada nombre para que los modificadores no los recorten
    for nme in m.names:
        mesh.materials.append(bpy.data.materials.get("tmp_" + nme) or bpy.data.materials.new("tmp_" + nme))
    obj = bpy.data.objects.new("shell", mesh)
    bpy.context.scene.collection.objects.link(obj)
    sub = obj.modifiers.new("sub", "SUBSURF")
    sub.levels = 2
    sub.render_levels = 2
    sub.subdivision_type = "CATMULL_CLARK"
    # pasos de rueda: cilindros que atraviesan el lateral
    cutters = []
    for y in WHEEL_Y:
        for sx in (-1, 1):
            bpy.ops.mesh.primitive_cylinder_add(vertices=40, radius=WHEEL_R + 0.08, depth=0.5, location=(sx * (WHEEL_X + 0.13), y, WHEEL_R), rotation=(0, math.radians(90), 0))
            c = bpy.context.active_object
            c.name = "cut"
            c.data.materials.append(bpy.data.materials["tmp_plastic_interior"])
            cutters.append(c)
            b = obj.modifiers.new("arch", "BOOLEAN")
            b.operation = "DIFFERENCE"
            b.object = c
            b.solver = "EXACT"
            b.material_mode = "TRANSFER"
    dg = bpy.context.evaluated_depsgraph_get()
    ev = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    for c in cutters:
        bpy.data.objects.remove(c, do_unlink=True)
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(mesh)
    return ev


def insert_shell(m, ev, burnt):
    """Pasa la malla evaluada (con los colores ya repartidos por las caras gruesas) a m.bm."""
    for p in ev.polygons:
        p.use_smooth = True
    m.bm.from_mesh(ev)
    bpy.data.meshes.remove(ev)


# ------------------------------------------------------------------ piezas
def tube(m, pts, r, mat, segs=8):
    """Tubo por una poligonal (barra de choque, antenas)."""
    for a, b in zip(pts[:-1], pts[1:]):
        a, b = Vector(a), Vector(b)
        d = b - a
        length = d.length
        mid = (a + b) / 2
        rot = d.to_track_quat("Z", "Y").to_euler()
        rings = []
        for s in (-0.5, 0.5):
            ring = []
            for k in range(segs):
                ang = 2 * math.pi * k / segs
                ring.append(Vector((r * math.cos(ang), r * math.sin(ang), s * length)))
            rings.append(ring)
        mat3 = rot.to_matrix()
        rr = [[mid + mat3 @ p for p in ring] for ring in rings]
        m._loft_rings(rr, mat, True)


def wheel(m, x, y, flat, side):
    """Rueda: neumatico con flancos y dibujo, llanta de chapa con tapacubos y tornillos."""
    r = WHEEL_R - (0.03 if flat else 0.0)
    half = [(0.0, 1.0), (0.05, 0.998), (0.085, 0.99), (0.108, 0.94), (0.118, 0.84), (0.116, 0.72), (0.10, 0.62)]
    prof = [(-xo, f) for (xo, f) in reversed(half[1:])] + half
    rings = []
    for xo, f in prof:
        rr = r * f
        rings.append([(x + xo, y + rr * math.cos(a), r + rr * math.sin(a) + (-0.035 if (flat and f > 0.9 and math.sin(a) < -0.5) else 0.0)) for a in (2 * math.pi * k / 32 for k in range(32))])
    m._loft_rings(rings, "rubber", True)
    # llanta de chapa casi plana, con el borde elevado, y tapacubos pequeno
    rim = [(0.112, 0.62), (0.108, 0.58), (0.096, 0.54), (0.09, 0.46), (0.09, 0.22)]
    rings = [[(x + side * xo, y + r * f * math.cos(a), r + r * f * math.sin(a)) for a in (2 * math.pi * k / 28 for k in range(28))] for xo, f in rim]
    m._loft_rings(rings, "metal_rim", True)
    cap = [(0.09, 0.2), (0.1, 0.19), (0.108, 0.15), (0.113, 0.08), (0.114, 0.01)]
    rings = [[(x + side * xo, y + r * f * math.cos(a), r + r * f * math.sin(a)) for a in (2 * math.pi * k / 20 for k in range(20))] for xo, f in cap]
    m._loft_rings(rings, "metal", True)
    for i in range(5):                                                                         # tornillos
        a = i * 2 * math.pi / 5
        m.box((x + side * 0.095, y + 0.29 * r * math.cos(a), r + 0.29 * r * math.sin(a)), (0.016, 0.026, 0.026), "metal", rot=(math.degrees(a), 0, 0))


def rounded(m, c, s, mat, r=0.02, rot=(0, 0, 0)):
    m.box(c, s, mat, rot=rot, bevel=r)


def lightbar(m, burnt):
    z0 = 1.465 + 0.0
    cy = 0.12
    # base y soporte
    m.box((0, cy, z0 + 0.02), (1.18, 0.28, 0.04), "plastic", bevel=0.012)
    for sx in (-0.4, 0.4):
        m.box((sx, cy, z0 + 0.005), (0.06, 0.2, 0.03), "plastic")                           # patas
    # cuerpo de las lentes: cinco modulos de cupula
    mods = [(-0.5, "red"), (-0.25, "red"), (0.0, "plastic_white"), (0.25, "blue"), (0.5, "blue")]
    for x, mat in mods:
        w = 0.22
        if burnt and mat in ("red", "blue", "plastic_white"):
            mat = "plastic"
        secs = []
        for k in range(7):
            t = math.pi * (k + 0.5) / 7
            secs.append((x, cy, z0 + 0.04 + 0.075 * math.cos(t) * 0.0, 0, 0))
        # cupula achatada: elipsoide
        m.ellipsoid((x, cy, z0 + 0.045), (w / 2, 0.135, 0.07), mat, 12, 7)
    # remates de los extremos y rejillas
    for sx in (-1, 1):
        m.box((sx * 0.63, cy, z0 + 0.06), (0.05, 0.26, 0.1), "plastic", bevel=0.015)
    m.box((0, cy - 0.145, z0 + 0.065), (1.2, 0.012, 0.06), "plastic")
    m.box((0, cy + 0.145, z0 + 0.065), (1.2, 0.012, 0.06), "plastic")


def build(burnt):
    pal = palette(burnt)
    m = Mesher("policecar", pal)
    # --- carroceria
    ev = shell_object(m, pal, burnt)
    insert_shell(m, ev, burnt)
    for sx in (-1, 1):
        wx = sx * (W / 2)
        # juntas de puertas, zocalo y tiradores
        for y in (-0.58, 0.36, 1.28):
            m.box((sx * (0.915), y, 0.66), (0.012, 0.014, 0.58), "bodyblack")
        m.box((sx * 0.915, 0.35, 0.385), (0.012, 1.86, 0.014), "bodyblack")
        for y in (-0.22, 0.74):
            rounded(m, (sx * 0.93, y, 0.8), (0.03, 0.16, 0.03), "metal", 0.008)
        # retrovisor: brazo y carcasa
        tube(m, [(sx * 0.82, -0.7, 1.0), (sx * 1.0, -0.82, 1.06)], 0.014, "plastic")
        m.ellipsoid((sx * 1.04, -0.84, 1.1), (0.045, 0.07, 0.08), "bodyblack", 10, 6)
        # luz de posicion y embellecedor de la aleta
        m.box((sx * 0.92, -2.0, 0.66), (0.01, 0.2, 0.05), "amber", bevel=0.005)
    # --- frontal: parachoques, rejilla, faros
    m.box((0, -2.33, 0.42), (1.7, 0.14, 0.2), "plastic", bevel=0.045)                                 # parachoques
    m.box((0, -2.405, 0.32), (1.0, 0.05, 0.1), "plastic_interior", bevel=0.01)                           # toma de aire inferior
    for i in range(5):
        m.box((0, -2.435, 0.28 + i * 0.03), (0.92, 0.012, 0.012), "metal_dark")
    m.box((0, -2.39, 0.6), (1.0, 0.03, 0.16), "plastic_interior", bevel=0.01)                            # rejilla superior
    for i in range(4):
        m.box((0, -2.41, 0.55 + i * 0.035), (0.96, 0.012, 0.012), "metal")
    for sx in (-1, 1):
        m.box((sx * 0.64, -2.36, 0.68), (0.4, 0.06, 0.13), "plastic_interior", bevel=0.02)             # carcasa del faro
        m.box((sx * 0.64, -2.385, 0.68), (0.36, 0.02, 0.1), "lights", bevel=0.015)                       # lente
        m.box((sx * 0.64, -2.395, 0.68), (0.14, 0.012, 0.07), "metal", bevel=0.005)                      # reflector
        m.box((sx * 0.75, -2.37, 0.5), (0.1, 0.04, 0.06), "amber", bevel=0.01)                           # intermitente
    # defensa de empuje (negra)
    tube(m, [(-0.5, -2.46, 0.3), (-0.5, -2.52, 0.34), (-0.5, -2.52, 0.74), (-0.5, -2.46, 0.78)], 0.026, "plastic", 8)
    tube(m, [(0.5, -2.46, 0.3), (0.5, -2.52, 0.34), (0.5, -2.52, 0.74), (0.5, -2.46, 0.78)], 0.026, "plastic", 8)
    tube(m, [(-0.5, -2.52, 0.74), (0.5, -2.52, 0.74)], 0.026, "plastic", 8)
    tube(m, [(-0.5, -2.52, 0.34), (0.5, -2.52, 0.34)], 0.026, "plastic", 8)
    tube(m, [(-0.5, -2.52, 0.54), (0.5, -2.52, 0.54)], 0.02, "plastic", 8)
    # --- trasera
    m.box((0, 2.33, 0.42), (1.7, 0.14, 0.2), "plastic", bevel=0.045)
    for sx in (-1, 1):
        m.box((sx * 0.66, 2.36, 0.78), (0.44, 0.05, 0.14), "red", bevel=0.02)                            # piloto
        m.box((sx * 0.66, 2.38, 0.78), (0.2, 0.02, 0.1), "plastic_white" if not burnt else "plastic", bevel=0.01)
    m.box((0, 2.37, 0.62), (0.42, 0.02, 0.2), "bodyblack", bevel=0.01)                                  # hueco de la matricula
    m.box((0, 2.385, 0.62), (0.34, 0.012, 0.17), "plastic_white" if not burnt else "plastic", bevel=0.004)  # matricula
    m.box((0, 2.37, 0.38), (1.2, 0.03, 0.04), "metal_dark")                                             # difusor
    tube(m, [(0.55, 2.3, 0.26), (0.55, 2.47, 0.26)], 0.03, "metal_dark", 10)                           # escape
    # --- barra de luces y accesorios del techo
    lightbar(m, burnt)
    m.box((0.62, -0.66, 1.1), (0.04, 0.06, 0.06), "metal_dark", bevel=0.012)                            # foco lateral
    tube(m, [(-0.62, 1.2, 1.45), (-0.62, 1.2, 1.78)], 0.005, "plastic", 5)                              # antena
    # --- ruedas
    for y in WHEEL_Y:
        for sx in (-1, 1):
            wheel(m, sx * WHEEL_X, y, burnt, sx)
    # --- rotulos y escudo (solo la patrulla intacta lleva los colores vivos; la quemada los lleva apagados)
    decals(m, burnt)
    return m.finish("PoliceCar" if burnt else "PoliceCarClean")


# ------------------------------------------------------------------ rotulos
def text_mesh(text, size, extrude=0.004, font=None, align="CENTER"):
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = extrude
    cu.align_x = align
    cu.align_y = "CENTER"
    obj = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(obj)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.curves.remove(cu)
    return me


def put_mesh(m, me, mat, matrix):
    bm = m.bm
    vmap = {}
    for v in me.vertices:
        vmap[v.index] = bm.verts.new(matrix @ v.co)
    mi = m._idx(mat)
    for p in me.polygons:
        try:
            f = bm.faces.new([vmap[i] for i in p.vertices])
            f.material_index = mi
        except ValueError:
            pass
    bpy.data.meshes.remove(me)


def decals(m, burnt):
    """Rotulos: POLICE en la puerta trasera (negro sobre blanco) y en el maletero (blanco), franja azul y dorada y escudo en la puerta delantera."""
    for sx in (-1, 1):
        me = text_mesh("POLICE", 0.14, 0.004)
        # el texto se crea en el plano XY (lee hacia +X, arriba +Y, relieve +Z): se lleva al lateral para que se lea desde fuera
        rows = [[0, 0, sx, 0], [sx if sx > 0 else -1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 0, 1]]
        if sx > 0:
            rows = [[0, 0, 1, 0.922], [1, 0, 0, -0.12], [0, 1, 0, 0.66], [0, 0, 0, 1]]
        else:
            rows = [[0, 0, -1, -0.922], [-1, 0, 0, -0.12], [0, 1, 0, 0.66], [0, 0, 0, 1]]
        put_mesh(m, me, "decal_k", Matrix(rows))
        m.box((sx * 0.921, 0.36, 0.5), (0.008, 1.66, 0.028), "decal_blue")
        m.box((sx * 0.921, 0.36, 0.465), (0.008, 1.66, 0.012), "decal_gold")
        shield(m, sx, 0.78, 0.8)
    me = text_mesh("POLICE", 0.2, 0.004)
    put_mesh(m, me, "decal_w", Matrix([[-1, 0, 0, 0], [0, 0, 1, 2.372], [0, 1, 0, 0.9], [0, 0, 0, 1]]))


def shield(m, sx, y, z):
    """Escudo: pentagono con una estrella dorada, sobre la puerta."""
    bm = m.bm
    pts = [(-0.07, 0.07), (0.07, 0.07), (0.075, -0.02), (0.0, -0.085), (-0.075, -0.02)]
    verts = [bm.verts.new(Vector((sx * 0.921, y + (p[0] * (-sx)), z + p[1]))) for p in pts]
    f = bm.faces.new(verts)
    f.material_index = m._idx("decal_blue")
    m.box((sx * 0.923, y, z), (0.006, 0.07, 0.07), "decal_gold", rot=(45, 0, 0))
    m.box((sx * 0.923, y, z), (0.006, 0.07, 0.07), "decal_gold")


# ------------------------------------------------------------------ vista previa y exportacion
def view(rx, rz, dist, loc):
    """Coloca el visor 3D de Blender (solo para mirar el modelo mientras se trabaja)."""
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            r3d = area.spaces[0].region_3d
            r3d.view_rotation = Euler((math.radians(rx), 0, math.radians(rz))).to_quaternion()
            r3d.view_distance = dist
            r3d.view_location = loc


def preview(kind="clean"):
    _clear()
    obj = build(kind == "burnt")
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
            r3d = area.spaces[0].region_3d
            r3d.view_rotation = Euler((math.radians(72), 0, math.radians(35))).to_quaternion()
            r3d.view_distance = 8.5
            r3d.view_location = (0, 0, 0.7)
    d = obj.dimensions
    print("modelo %.2f x %.2f x %.2f m, %d caras, %d tris" % (d.x, d.y, d.z, len(obj.data.polygons), sum(len(p.vertices) - 2 for p in obj.data.polygons)))
    return obj


if __name__ == "__main__" and "--" in sys.argv:
    out = sys.argv[sys.argv.index("--") + 1]
    only = [a for a in sys.argv[sys.argv.index("--") + 2:]]
    os.makedirs(out, exist_ok=True)
    for name, burnt in (("PoliceCar", True), ("PoliceCarClean", False)):
        if only and name not in only:
            continue
        _clear()
        obj = build(burnt)
        d = obj.dimensions
        _export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), len(obj.data.polygons), "caras", flush=True)
    print("PROCESO_TERMINADO")
