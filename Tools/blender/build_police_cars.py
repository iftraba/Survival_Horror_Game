"""Coches patrulla de la comisaria (v3, 2026-10-14): modelo de nivel AA, en dos versiones.

    blender --background --python build_police_cars.py -- <carpeta Assets/_Project/Art/Props/Exterior> [PoliceCar PoliceCarClean]
    (en un Blender abierto: exec(open(ruta).read()); preview("clean") o preview("burnt"); view(rx, rz, distancia, (x, y, z)))

  PoliceCar       quemado y abandonado: capo, maletero y techo calcinados y oxidados, neumaticos aplastados, luces apagadas
  PoliceCarClean  intacto: bicolor negro y blanco, barra de luces, rotulos POLICE y placa, faros
Mismas medidas base que el modelo anterior (4,7 x 1,82 m de carroceria), origen en el centro de la base, frente a -Y de Blender = +Z de Unity.
Los nombres de material deciden el material horneado de texture_items.py: carpaint* = pintura de coche, decal* = rotulos, glass = cristal,
lens* = lentes y luces, rubber = goma, rust = oxido/chamusco, metal* = cromo y acero; el resto, plastico.

Carroceria: una sola malla por secciones (cuerpo + habitaculo) suavizada con subdivision, con aristas marcadas y pasos de rueda recortados con
booleanos. Rotulos, franjas, escudo, juntas de puertas, capo y maletero son malla PLANA proyectada sobre la superficie real de la carroceria
(rayos contra la malla suavizada), asi que no flotan ni se hunden. El resto son piezas sueltas (parachoques, faros, rejilla, retrovisores,
barra de luces, ruedas).
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE, "build_items.py"), encoding="utf-8").read())

L, W = 4.7, 1.82
WHEEL_Y = (-1.42, 1.42)
WHEEL_X = 0.785
WHEEL_R = 0.36

# (y, base, cintura, techo, semi-ancho en cintura, semi-ancho del techo)
STATIONS = [
    (-2.35, 0.36, 0.60, 0.64, 0.73, 0.58),
    (-2.32, 0.27, 0.66, 0.70, 0.84, 0.68),
    (-2.22, 0.23, 0.77, 0.81, 0.90, 0.74),
    (-1.90, 0.22, 0.87, 0.91, 0.925, 0.78),
    (-1.40, 0.22, 0.915, 0.955, 0.93, 0.79),
    (-0.98, 0.22, 0.94, 0.98, 0.925, 0.79),
    (-0.78, 0.22, 0.95, 1.14, 0.915, 0.75),
    (-0.58, 0.22, 0.95, 1.253, 0.91, 0.73),
    (-0.48, 0.22, 0.95, 1.31, 0.91, 0.72),
    (-0.14, 0.22, 0.95, 1.40, 0.91, 0.70),
    (0.40, 0.22, 0.95, 1.43, 0.91, 0.70),
    (1.00, 0.22, 0.95, 1.42, 0.91, 0.70),
    (1.28, 0.22, 0.95, 1.40, 0.91, 0.70),
    (1.38, 0.22, 0.95, 1.385, 0.915, 0.71),
    (1.68, 0.22, 0.95, 1.20, 0.92, 0.74),
    (1.94, 0.22, 0.95, 1.02, 0.925, 0.79),
    (2.12, 0.22, 0.95, 0.99, 0.93, 0.80),
    (2.27, 0.23, 0.91, 0.945, 0.915, 0.79),
    (2.33, 0.27, 0.84, 0.875, 0.85, 0.72),
    (2.35, 0.36, 0.70, 0.74, 0.74, 0.58),
]


def palette(burnt):
    pal = {"carpaint_white": (0.9, 0.9, 0.88), "carpaint_black": (0.018, 0.018, 0.022), "plastic": (0.04, 0.04, 0.043), "metal": (0.74, 0.76, 0.79),
           "metal_dark": (0.1, 0.1, 0.11), "metal_rim": (0.2, 0.21, 0.23), "glass": (0.03, 0.045, 0.06), "lens_red": (0.85, 0.03, 0.02),
           "lens_blue": (0.03, 0.1, 0.9), "lens_head": (0.93, 0.92, 0.84), "lens_amber": (0.92, 0.45, 0.03), "lens_clear": (0.88, 0.9, 0.95),
           "plastic_white": (0.9, 0.9, 0.88), "plastic_interior": (0.025, 0.025, 0.03), "rubber": (0.022, 0.022, 0.025),
           "decal_k": (0.015, 0.015, 0.02), "decal_w": (0.93, 0.93, 0.9), "decal_blue": (0.04, 0.12, 0.6), "decal_gold": (0.8, 0.62, 0.18), "charred": (0.05, 0.043, 0.038)}
    if burnt:
        pal.update({"carpaint_white": (0.42, 0.4, 0.36), "carpaint_black": (0.03, 0.026, 0.025), "glass": (0.02, 0.022, 0.024), "lens_head": (0.22, 0.2, 0.17),
                    "lens_red": (0.25, 0.03, 0.02), "lens_blue": (0.03, 0.05, 0.22), "lens_amber": (0.28, 0.15, 0.04), "lens_clear": (0.3, 0.3, 0.3),
                    "decal_w": (0.5, 0.48, 0.44), "decal_blue": (0.04, 0.06, 0.2), "decal_gold": (0.32, 0.26, 0.1), "plastic_white": (0.35, 0.34, 0.32),
                    "charred": (0.05, 0.043, 0.038)})
    return pal


# ------------------------------------------------------------------ carroceria
def half_profile(st):
    """Perfil de medio corte (x >= 0) de una estacion, de la base al techo."""
    y, zb, zbelt, zroof, hw, hwr = st
    zs = zb + 0.46 * (zbelt - zb)
    z6 = max(zbelt + 0.006, zroof - 0.07)
    z7 = max(z6 + 0.006, zroof)
    return [(0.0, zb), (hw - 0.06, zb), (hw - 0.03, zb + 0.1), (hw, zs), (hw, zbelt - 0.05), (hw - 0.03, zbelt),
            (hwr + 0.03, z6), (hwr - 0.05, z7), (0.0, z7 + 0.012)]


def build_shell(m, burnt=False):
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
            mat = "carpaint_black"
            in_doors = s0[0] >= -0.581 and s1[0] <= 1.281
            if in_doors and seg in (2, 3, 4):
                mat = "carpaint_white"                         # puertas blancas
            if seg == 7 and -0.15 <= s0[0] and s1[0] <= 1.39 and cab > 0.2:
                mat = "carpaint_white"                         # techo blanco
            if burnt and cab <= 0.2 and seg in (4, 5, 6, 7):
                mat = "charred"                                # capo y maletero calcinados
            if burnt and seg == 7 and cab > 0.2 and mat == "carpaint_white":
                mat = "charred"                                # techo quemado
            if cab > 0.2:
                if seg == 5:
                    mat = "glass"
                elif seg == 6:
                    mat = "carpaint_black"
                elif seg == 7 and abs(slope) > 0.33:
                    mat = "glass"                              # parabrisas y luna trasera
            f.material_index = m._idx(mat)
    for ring in (rings[0], list(reversed(rings[-1]))):
        f = sb.faces.new(ring)
        f.material_index = m._idx("carpaint_black")
    # aristas marcadas: zocalo, hombro, cintura y borde del techo, y los extremos (parachoques)
    cl = sb.edges.layers.float.get("crease_edge") or sb.edges.layers.float.new("crease_edge")
    sharp = {2: 0.6, 4: 0.6, 5: 0.95, 6: 0.9, 7: 0.6}
    for i in range(len(rings) - 1):
        for j in range(n):
            pj = j if j <= 8 else n - j
            if pj in sharp:
                e = sb.edges.get((rings[i][j], rings[i + 1][j]))
                if e is not None:
                    e[cl] = sharp[pj]
    for ring in (rings[0], rings[1], rings[-2], rings[-1]):
        for j in range(n):
            e = sb.edges.get((ring[j], ring[(j + 1) % n]))
            if e is not None:
                e[cl] = max(e[cl], 0.75)
    mesh = bpy.data.meshes.new("shell")
    sb.normal_update()
    sb.to_mesh(mesh)
    sb.free()
    return mesh


def shell_object(m, burnt):
    mesh = build_shell(m, burnt)
    for nme in m.names:                    # un hueco por cada nombre: asi los modificadores no recortan los indices de material
        mesh.materials.append(bpy.data.materials.get("tmp_" + nme) or bpy.data.materials.new("tmp_" + nme))
    obj = bpy.data.objects.new("shell", mesh)
    bpy.context.scene.collection.objects.link(obj)
    sub = obj.modifiers.new("sub", "SUBSURF")
    sub.levels = 2
    sub.render_levels = 2
    cutters = []
    for y in WHEEL_Y:
        for sx in (-1, 1):
            bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=WHEEL_R + 0.08, depth=0.5, location=(sx * (WHEEL_X + 0.13), y, WHEEL_R), rotation=(0, math.radians(90), 0))
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
    for p in ev.polygons:
        p.use_smooth = True
    return ev


class Surface:
    """Superficie real de la carroceria (malla suavizada): da la X de la chapa en un punto del lateral y la Z en un punto del capo/techo."""

    def __init__(self, bm):
        bm.faces.ensure_lookup_table()
        self.tree = BVHTree.FromBMesh(bm)

    def side_x(self, y, z, s):
        hit = self.tree.ray_cast(Vector((s * 2.0, y, z)), Vector((-s, 0.0, 0.0)))
        return hit[0].x if hit[0] is not None else s * 0.9

    def top_z(self, x, y):
        hit = self.tree.ray_cast(Vector((x, y, 3.0)), Vector((0.0, 0.0, -1.0)))
        return hit[0].z if hit[0] is not None else 0.95

    def rear_y(self, x, z, s):
        """Y de la chapa en la parte delantera (s=-1) o trasera (s=+1) en un punto."""
        hit = self.tree.ray_cast(Vector((x, s * 4.0, z)), Vector((0.0, -s, 0.0)))
        return hit[0].y if hit[0] is not None else s * 2.3


def drape(surf, v, mode, s, off):
    if mode == "side":
        v.co.x = surf.side_x(v.co.y, v.co.z, s) + s * off
    elif mode == "top":
        v.co.z = surf.top_z(v.co.x, v.co.y) + off
    elif mode == "back":
        v.co.y = surf.rear_y(v.co.x, v.co.z, s) + s * off


def _emit(m, bm, surf, mat, mode, s, off):
    for v in bm.verts:
        drape(surf, v, mode, s, off)
    mi = m._idx(mat)
    vmap = {v: m.bm.verts.new(v.co) for v in bm.verts}
    for f in bm.faces:
        try:
            nf = m.bm.faces.new([vmap[v] for v in f.verts])
        except ValueError:
            continue
        nf.material_index = mi
        nf.smooth = False
        m._deco.append((nf, Vector((s, 0.0, 0.0)) if mode == "side" else Vector((0.0, 0.0, 1.0)) if mode == "top" else Vector((0.0, s, 0.0))))
    bm.free()


def finish(m, name):
    """Como Mesher.finish, pero las caras planas de los rotulos (abiertas) se orientan hacia fuera de la chapa en vez de dejar que la recalculo las gire al azar."""
    bm = m.bm
    deco = {f for f, _ in m._deco if f.is_valid}
    bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces if f not in deco])
    for f, out in m._deco:
        if f.is_valid and f.normal.dot(out) < 0:
            f.normal_flip()
    used = sorted({f.material_index for f in bm.faces})
    remap = {old: new for new, old in enumerate(used)}
    for f in bm.faces:
        f.material_index = remap[f.material_index]
    mesh = bpy.data.meshes.new(name + "Mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for old in used:
        mesh.materials.append(_mat(f"{m.prefix}_{m.names[old]}", m.palette[m.names[old]]))
    return obj


def flat_decal(m, surf, corners, mat, mode, s=1, off=0.0012, cuts=8):
    """Poligono plano (esquinas en coordenadas de mundo) subdividido y proyectado sobre la chapa. mode: 'side' (lateral, s = +1/-1), 'top', 'back'."""
    bm = bmesh.new()
    vs = [bm.verts.new(Vector(c)) for c in corners]
    try:
        bm.faces.new(vs)
    except ValueError:
        bm.free()
        return
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    _emit(m, bm, surf, mat, mode, s, off)


def strip(m, surf, p0, p1, half_w, mat, mode, s=1, off=0.0012, n=8):
    """Tira plana de n tramos entre p0 y p1 (mundo) con semiancho half_w en la direccion perpendicular a lo largo de la superficie."""
    p0, p1 = Vector(p0), Vector(p1)
    d = (p1 - p0).normalized()
    if mode == "side":
        w = Vector((0.0, -d.z, d.y)) if abs(d.x) < 0.5 else Vector((0, 0, 1))      # perpendicular dentro del plano YZ
        w = Vector((0.0, -d.z, d.y)).normalized()
    else:
        w = Vector((-d.y, d.x, 0.0)).normalized()                                  # perpendicular dentro del plano XY
    bm = bmesh.new()
    cols = []
    for k in range(n + 1):
        p = p0 + (p1 - p0) * (k / n)
        cols.append((bm.verts.new(p - w * half_w), bm.verts.new(p + w * half_w)))
    for k in range(n):
        bm.faces.new((cols[k][0], cols[k + 1][0], cols[k + 1][1], cols[k][1]))
    _emit(m, bm, surf, mat, mode, s, off)


def put_mesh(m, surf, me, matrix, mat, mode, s=1, off=0.0012, cuts=0):
    """Inserta una malla plana (rotulo) llevandola a 'matrix' y proyectandola sobre la chapa."""
    bm = bmesh.new()
    bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts[:])
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    if cuts:
        bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    _emit(m, bm, surf, mat, mode, s, off)


def text_mesh(text, size):
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = 0.0
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    cu.space_character = 1.12
    obj = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(obj)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.curves.remove(cu)
    return me


# ------------------------------------------------------------------ piezas
def tube(m, pts, r, mat, segs=8):
    """Tubo por una poligonal (defensa de empuje, antena, aro del paso de rueda)."""
    for a, b in zip(pts[:-1], pts[1:]):
        a, b = Vector(a), Vector(b)
        d = b - a
        length = d.length
        if length < 1e-5:
            continue
        mid = (a + b) / 2
        mat3 = d.to_track_quat("Z", "Y").to_matrix()
        rr = []
        for sgn in (-0.5, 0.5):
            rr.append([mid + mat3 @ Vector((r * math.cos(2 * math.pi * k / segs), r * math.sin(2 * math.pi * k / segs), sgn * length)) for k in range(segs)])
        m._loft_rings(rr, mat, True)


def tube_path(m, pts, r, mat, segs=6):
    """Tubo continuo por una poligonal (un solo anillo por punto, sin tapas intermedias)."""
    pts = [Vector(p) for p in pts]
    rings = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        q = t.to_track_quat("Z", "Y").to_matrix()
        rings.append([p + q @ Vector((r * math.cos(2 * math.pi * k / segs), r * math.sin(2 * math.pi * k / segs), 0.0)) for k in range(segs)])
    m._loft_rings(rings, mat, True)


def wheel(m, x, y, flat, side):
    """Rueda: neumatico con flancos curvos y bandas de rodadura, llanta de chapa oscura con tapacubos cromado y tornillos."""
    r = WHEEL_R - (0.03 if flat else 0.0)
    half = [(0.0, 0.992), (0.012, 1.0), (0.03, 0.992), (0.04, 1.0), (0.06, 0.992), (0.07, 1.0), (0.088, 0.99), (0.106, 0.945), (0.117, 0.86), (0.118, 0.76), (0.108, 0.66), (0.1, 0.6)]
    prof = [(-xo, f) for (xo, f) in reversed(half[1:])] + half
    rings = []
    for xo, f in prof:
        rr = r * f
        rings.append([(x + xo, y + rr * math.cos(a), r + rr * math.sin(a) + (-0.035 if (flat and f > 0.9 and math.sin(a) < -0.5) else 0.0)) for a in (2 * math.pi * k / 32 for k in range(32))])
    m._loft_rings(rings, "rubber", True)
    rim = [(0.112, 0.62), (0.108, 0.585), (0.1, 0.55), (0.094, 0.5), (0.09, 0.46), (0.092, 0.24), (0.09, 0.2)]
    rings = [[(x + side * xo, y + r * f * math.cos(a), r + r * f * math.sin(a)) for a in (2 * math.pi * k / 32 for k in range(32))] for xo, f in rim]
    m._loft_rings(rings, "metal_rim", True)
    cap = [(0.09, 0.2), (0.102, 0.19), (0.11, 0.15), (0.115, 0.09), (0.117, 0.01)]
    rings = [[(x + side * xo, y + r * f * math.cos(a), r + r * f * math.sin(a)) for a in (2 * math.pi * k / 24 for k in range(24))] for xo, f in cap]
    m._loft_rings(rings, "metal", True)
    for i in range(8):                                                                         # tornillos y orificios
        a = i * 2 * math.pi / 8
        m.box((x + side * 0.097, y + 0.34 * r * math.cos(a), r + 0.34 * r * math.sin(a)), (0.014, 0.024, 0.024), "metal", rot=(math.degrees(a), 0, 0))
        m.box((x + side * 0.094, y + 0.4 * r * math.cos(a + 0.4), r + 0.4 * r * math.sin(a + 0.4)), (0.008, 0.04, 0.016), "plastic_interior", rot=(math.degrees(a + 0.4) + 90, 0, 0))


def lightbar(m, surf, burnt):
    """Barra de luces de perfil bajo: base de aluminio con patas, siete modulos de lente (rojo, blanco, azul), divisores, filas de LED y remates."""
    cy = 0.14
    z0 = surf.top_z(0.0, cy) + 0.012
    half_len = 0.68
    m.box((0, cy, z0 + 0.012), (2 * half_len, 0.31, 0.026), "plastic", bevel=0.008)
    for sx in (-0.44, 0.44):
        m.box((sx, cy, z0 - 0.004), (0.14, 0.24, 0.02), "plastic", bevel=0.004)
    mods = [(-0.68, -0.46, "lens_red"), (-0.46, -0.24, "lens_red"), (-0.24, -0.08, "lens_clear"), (-0.08, 0.08, "lens_clear"), (0.08, 0.24, "lens_clear"),
            (0.24, 0.46, "lens_blue"), (0.46, 0.68, "lens_blue")]
    for (x0, x1, mat) in mods:
        use = "plastic_interior" if (burnt and mat != "lens_clear") else mat
        steps = 5
        rings = []
        for k in range(steps + 1):
            x = x0 + (x1 - x0) * k / steps
            taper = 1.0
            edge = min(abs(x + half_len), abs(half_len - x))
            if edge < 0.1:                                  # remates redondeados de los extremos de la barra
                taper = 0.55 + 0.45 * (edge / 0.1)
            ring = []
            for a_i in range(16):
                a = 2 * math.pi * a_i / 16
                dy = 0.15 * math.cos(a) * taper
                dz = (0.082 * math.sin(a) if math.sin(a) > 0 else 0.012 * math.sin(a)) * taper
                ring.append((x, cy + dy, z0 + 0.026 + dz))
            rings.append(ring)
        m._loft_rings(rings, use, True)
        xs = [x0 + (x1 - x0) * (i + 0.5) / 3 for i in range(3)]
        for fy in (-1, 1):                                  # filas de LED en cada cara
            for x in xs:
                m.box((x, cy + fy * 0.1, z0 + 0.062), (0.03, 0.012, 0.018), "lens_clear" if not burnt else "plastic_interior")
                m.box((x, cy + fy * 0.14, z0 + 0.046), (0.03, 0.012, 0.014), "lens_clear" if not burnt else "plastic_interior")
        m.box((x1, cy, z0 + 0.05), (0.012, 0.31, 0.1), "plastic", bevel=0.003)           # divisor negro
    m.box((-half_len, cy, z0 + 0.05), (0.014, 0.31, 0.1), "plastic", bevel=0.004)
    m.box((half_len, cy, z0 + 0.05), (0.014, 0.31, 0.1), "plastic", bevel=0.004)


def mirror(m, surf, s):
    x = surf.side_x(-0.78, 0.99, s)
    tube(m, [(x - s * 0.02, -0.74, 1.0), (x + s * 0.1, -0.83, 1.06)], 0.013, "plastic")
    m.ellipsoid((x + s * 0.145, -0.84, 1.1), (0.04, 0.075, 0.075), "carpaint_black", 12, 7)
    m.box((x + s * 0.145, -0.9, 1.1), (0.03, 0.012, 0.05), "metal_dark")


def shield(m, surf, s, y, z):
    """Escudo en la puerta trasera: pentagono azul con borde dorado y una estrella dorada."""
    def at(dy, dz):
        return (s * 0.9, y + dy * (-s), z + dz)
    out = [(-0.075, 0.075), (0.075, 0.075), (0.08, -0.02), (0.0, -0.09), (-0.08, -0.02)]
    flat_decal(m, surf, [at(*p) for p in out], "decal_gold", "side", s, 0.0014, 3)
    inn = [(-0.065, 0.065), (0.065, 0.065), (0.07, -0.015), (0.0, -0.078), (-0.07, -0.015)]
    flat_decal(m, surf, [at(*p) for p in inn], "decal_blue", "side", s, 0.0018, 3)
    star = []
    for k in range(10):
        a = math.radians(90 + 36 * k)
        rr = 0.045 if k % 2 == 0 else 0.019
        star.append(at(rr * math.cos(a), 0.005 + rr * math.sin(a)))
    flat_decal(m, surf, star, "decal_gold", "side", s, 0.0022, 2)


def seam_loop(m, surf, pts, width, mode, cuts):
    """Junta oscura (capo, maletero) que sigue un poligono cerrado de puntos (x, y), proyectada sobre la chapa por arriba."""
    for (ax, ay), (bx, by) in zip(pts, pts[1:] + pts[:1]):
        strip(m, surf, (ax, ay, 3.0), (bx, by, 3.0), width, "plastic_interior", mode, 1, 0.0012, cuts)


def build(burnt):
    pal = palette(burnt)
    m = Mesher("policecar", pal)
    m._deco = []
    ev = shell_object(m, burnt)
    m.bm.from_mesh(ev)
    bpy.data.meshes.remove(ev)
    surf = Surface(m.bm)
    for s in (-1, 1):
        # juntas de puertas y zocalo: lineas planas oscuras sobre la chapa
        for y in (-0.58, 0.36, 1.28):
            strip(m, surf, (s * 0.9, y, 0.35), (s * 0.9, y, 0.94), 0.004, "plastic_interior", "side", s, 0.001, 6)
        strip(m, surf, (s * 0.9, -0.58, 0.35), (s * 0.9, 1.28, 0.35), 0.004, "plastic_interior", "side", s, 0.001, 10)
        # franja azul y dorada, escudo
        strip(m, surf, (s * 0.9, -0.55, 0.515), (s * 0.9, 1.25, 0.515), 0.015, "decal_blue", "side", s, 0.0014, 12)
        strip(m, surf, (s * 0.9, -0.55, 0.4625), (s * 0.9, 1.25, 0.4625), 0.0075, "decal_gold", "side", s, 0.0014, 12)
        shield(m, surf, s, 0.78, 0.8)
        # texto POLICE en la puerta delantera: se crea en XY (lee hacia +X) y se lleva al lateral
        me = text_mesh("POLICE", 0.15)
        if s > 0:
            mat = Matrix([[0, 0, 1, 0.9], [1, 0, 0, -0.12], [0, 1, 0, 0.67], [0, 0, 0, 1]])
        else:
            mat = Matrix([[0, 0, -1, -0.9], [-1, 0, 0, -0.12], [0, 1, 0, 0.67], [0, 0, 0, 1]])
        put_mesh(m, surf, me, mat, "decal_k", "side", s, 0.0016)
        # tiradores, retrovisor, luz de posicion, aros de los pasos de rueda
        for y in (-0.24, 0.74):
            x = surf.side_x(y, 0.84, s)
            m.box((x + s * 0.004, y, 0.84), (0.018, 0.17, 0.03), "metal", bevel=0.008)
            m.box((x - s * 0.001, y, 0.84), (0.008, 0.2, 0.05), "plastic_interior")
        mirror(m, surf, s)
        x = surf.side_x(-2.0, 0.66, s)
        m.box((x, -2.0, 0.66), (0.012, 0.2, 0.045), "lens_amber", bevel=0.004)
        for wy in WHEEL_Y:
            pts = []
            for k in range(0, 21):
                th = math.radians(-8 + 196 * k / 20)
                yy, zz = wy + (WHEEL_R + 0.088) * math.cos(th), WHEEL_R + (WHEEL_R + 0.088) * math.sin(th)
                pts.append((surf.side_x(yy, zz, s) + s * 0.004, yy, zz))
            tube_path(m, pts, 0.012, "plastic", 6)
    # capo y maletero: juntas planas
    seam_loop(m, surf, [(-0.62, -2.18), (0.62, -2.18), (0.62, -0.99), (-0.62, -0.99)], 0.0035, "top", 16)
    seam_loop(m, surf, [(-0.62, 1.97), (0.62, 1.97), (0.62, 2.3), (-0.62, 2.3)], 0.0035, "top", 12)
    # frontal
    m.box((0, -2.335, 0.42), (1.7, 0.14, 0.2), "plastic", bevel=0.045)
    m.box((0, -2.41, 0.32), (1.0, 0.05, 0.1), "plastic_interior", bevel=0.01)
    for i in range(5):
        m.box((0, -2.44, 0.28 + i * 0.03), (0.92, 0.012, 0.012), "metal_dark")
    m.box((0, -2.395, 0.6), (1.0, 0.03, 0.16), "plastic_interior", bevel=0.01)
    for i in range(4):
        m.box((0, -2.415, 0.55 + i * 0.035), (0.96, 0.012, 0.012), "metal")
    for sx in (-1, 1):
        m.box((sx * 0.64, -2.36, 0.68), (0.42, 0.065, 0.14), "metal_dark", bevel=0.02)
        m.box((sx * 0.64, -2.39, 0.68), (0.37, 0.02, 0.105), "lens_head", bevel=0.015)
        for k in (-1, 1):
            m.ellipsoid((sx * 0.64 + k * 0.09, -2.402, 0.68), (0.05, 0.016, 0.05), "metal", 10, 6)
        m.box((sx * 0.76, -2.372, 0.5), (0.1, 0.04, 0.06), "lens_amber", bevel=0.01)
        m.box((sx * 0.34, -2.41, 0.6), (0.1, 0.012, 0.032), "lens_red" if sx < 0 else "lens_blue", bevel=0.004)
    for sx in (-0.5, 0.5):
        tube(m, [(sx, -2.46, 0.3), (sx, -2.52, 0.34), (sx, -2.52, 0.74), (sx, -2.46, 0.78)], 0.026, "plastic", 8)
    for z in (0.74, 0.54, 0.34):
        tube(m, [(-0.5, -2.52, z), (0.5, -2.52, z)], 0.026 if z != 0.54 else 0.02, "plastic", 8)
    # trasera
    m.box((0, 2.335, 0.42), (1.7, 0.14, 0.2), "plastic", bevel=0.045)
    for sx in (-1, 1):
        m.box((sx * 0.66, 2.36, 0.78), (0.46, 0.05, 0.14), "lens_red", bevel=0.02)
        m.box((sx * 0.5, 2.385, 0.78), (0.12, 0.012, 0.09), "plastic_white" if not burnt else "plastic", bevel=0.01)
        m.box((sx * 0.8, 2.372, 0.78), (0.08, 0.02, 0.1), "lens_amber", bevel=0.01)
    m.box((0, 2.372, 0.63), (0.44, 0.02, 0.21), "plastic_interior", bevel=0.01)
    m.box((0, 2.386, 0.63), (0.36, 0.012, 0.18), "plastic_white", bevel=0.004)
    m.box((0, 2.37, 0.38), (1.2, 0.03, 0.04), "metal_dark")
    tube(m, [(0.55, 2.3, 0.26), (0.55, 2.47, 0.26)], 0.03, "metal_dark", 10)
    me = text_mesh("POLICE", 0.105)
    put_mesh(m, surf, me, Matrix([[-1, 0, 0, 0], [0, 0, 1, 2.3], [0, 1, 0, 0.815], [0, 0, 0, 1]]), "decal_w", "back", 1, 0.0016, 2)
    # techo
    lightbar(m, surf, burnt)
    tube(m, [(-0.58, 1.22, 1.4), (-0.58, 1.22, 1.78)], 0.005, "plastic", 5)
    for y in WHEEL_Y:
        for sx in (-1, 1):
            wheel(m, sx * WHEEL_X, y, burnt, sx)
    return finish(m, "PoliceCar" if burnt else "PoliceCarClean")


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
    view(72, 40, 7.5, (0, 0, 0.7))
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
