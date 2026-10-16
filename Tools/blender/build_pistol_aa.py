"""Pistola de servicio AA (2026-10-16): sustituye a MeshyPistolLow (la de Meshy tenia dos empunaduras y la malla abollada) con las mismas medidas,
orientacion (boca del canon hacia -Y) y origen, y el mismo nombre de FBX para que W_Pistol e I_Pistol la sigan usando.

    blender --background --python build_pistol_aa.py -- <carpeta ABSOLUTA de salida>
    (en un Blender abierto: exec(open(ruta).read()); preview())

Modelado como en las armas reales: siluetas de perfil con esquinas redondeadas extruidas (armazon, guardamonte, gatillo, miras, mandos), corredera con
su seccion (costados rectos, hombros achaflanados) extruida a lo largo, y cortes booleanos de verdad (estrias, ventana de expulsion, nariz biselada,
ranuras del rail, hueco del guardamonte y grabado del costado). Punteado en relieve en la empunadura.
"""
import math
import os
import sys

HERE_P = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE_P, "aa_lib.py"), encoding="utf-8").read())


# ------------------------------------------------------------------ utilidades 2D / booleanas
def round_poly(pts, r, segs=4):
    """Redondea las esquinas de un poligono cerrado (lista de (u, v)) con curvas de Bezier cuadraticas de radio aproximado r."""
    out = []
    n = len(pts)
    for i in range(n):
        p0, p1, p2 = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[(i + 1) % n])
        a, c = p0 - p1, p2 - p1
        ra = min(r, a.length * 0.45)
        rc = min(r, c.length * 0.45)
        if ra < 1e-6 or rc < 1e-6:
            out.append(tuple(p1)); continue
        q0 = p1 + a.normalized() * ra
        q2 = p1 + c.normalized() * rc
        for k in range(segs + 1):
            t = k / segs
            q = q0 * (1 - t) ** 2 + p1 * 2 * t * (1 - t) + q2 * t * t
            out.append((q.x, q.y))
    return out


def prism_yz(outline, x0, x1):
    """Silueta en el plano YZ (lista de (y, z)) extruida de x0 a x1."""
    bm = bmesh.new()
    a = [bm.verts.new((x0, y, z)) for (y, z) in outline]
    c = [bm.verts.new((x1, y, z)) for (y, z) in outline]
    bm.faces.new(a)
    bm.faces.new(list(reversed(c)))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], c[j], c[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def prism_xz(outline, y0, y1):
    """Seccion en el plano XZ (lista de (x, z)) extruida de y0 a y1."""
    bm = bmesh.new()
    a = [bm.verts.new((x, y0, z)) for (x, z) in outline]
    c = [bm.verts.new((x, y1, z)) for (x, z) in outline]
    bm.faces.new(a)
    bm.faces.new(list(reversed(c)))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], c[j], c[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def prism_round(outline, half, radius, steps=7):
    """Silueta YZ extruida en X con los cantos redondeados de verdad (perfil acolchado): capas de la silueta encogida hacia dentro segun un cuarto de
    circulo de radio 'radius' a cada costado. Da piezas abombadas (empunadura, armazon) en vez de recortes planos."""
    pts = [Vector(p) for p in outline]
    n = len(pts)
    area = sum(pts[i].x * pts[(i + 1) % n].y - pts[(i + 1) % n].x * pts[i].y for i in range(n))
    sign = 1.0 if area > 0 else -1.0                                                      # orientacion: normal hacia fuera
    nrm = []
    for i in range(n):
        e0 = (pts[i] - pts[i - 1]).normalized()
        e1 = (pts[(i + 1) % n] - pts[i]).normalized()
        t = (e0 + e1).normalized() if (e0 + e1).length > 1e-6 else e1
        out = Vector((t.y, -t.x)) * sign
        nrm.append(out)
    rings = []
    bm = bmesh.new()
    levels = [(-half + radius - radius * math.cos(math.pi / 2 * k / steps), radius - radius * math.sin(math.pi / 2 * k / steps)) for k in range(0, steps + 1)]
    levels = levels + [(-x, r) for (x, r) in reversed(levels)]
    for (x, ins) in levels:
        rings.append([bm.verts.new((x, (pts[i] - nrm[i] * ins).x, (pts[i] - nrm[i] * ins).y)) for i in range(n)])
    for a_, c_ in zip(rings[:-1], rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            try:
                bm.faces.new((a_[i], a_[j], c_[j], c_[i]))
            except ValueError:
                pass
    bm.faces.new(rings[0])
    bm.faces.new(list(reversed(rings[-1])))
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-7)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def box_bm(c, s, rot=(0, 0, 0)):
    bm = bmesh.new()
    r = bmesh.ops.create_cube(bm, size=1.0)
    for v in r["verts"]:
        v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
    bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector(), matrix=Euler([math.radians(a) for a in rot]).to_matrix())
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
    return bm


def to_obj(bm, name):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


def booleans(target_bm, cutters, op="DIFFERENCE", bevel=None):
    """Aplica cortes booleanos (solver exacto) y opcionalmente un bisel por angulo; devuelve un bmesh nuevo."""
    o = to_obj(target_bm, "tmp_target")
    cobjs = []
    for i, c in enumerate(cutters):
        co = c if isinstance(c, bpy.types.Object) else to_obj(c, "tmp_cut%d" % i)
        co.hide_render = True
        cobjs.append(co)
        m = o.modifiers.new("b%d" % i, "BOOLEAN")
        m.operation = op
        m.object = co
        m.solver = "EXACT"
    if bevel:
        bv = o.modifiers.new("bev", "BEVEL")
        bv.width, bv.segments = bevel[0], bevel[1]
        bv.limit_method = "ANGLE"
        bv.angle_limit = math.radians(bevel[2] if len(bevel) > 2 else 40)
        bv.harden_normals = False
    dg = bpy.context.evaluated_depsgraph_get()
    ev = o.evaluated_get(dg)
    me = ev.to_mesh()
    out = bmesh.new()
    out.from_mesh(me)
    ev.to_mesh_clear()
    old = o.data
    bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.meshes.remove(old)
    for co in cobjs:
        m2 = co.data
        bpy.data.objects.remove(co, do_unlink=True)
        if m2.users == 0:
            bpy.data.meshes.remove(m2)
    return out


def bevel_only(bm, width, segs, angle=40):
    return booleans(bm, [], bevel=(width, segs, angle))


def text_obj(text, size, loc, depth, spacing=1.15, face="+x"):
    """Texto como objeto malla (cortador para grabar). face '+x': legible desde +X; '-x': desde -X."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body, cu.size, cu.extrude = text, size, depth
    cu.align_x, cu.align_y, cu.space_character = "CENTER", "CENTER", spacing
    cu.resolution_u = 3
    o = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(o)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.curves.remove(cu)
    bm = bmesh.new()
    bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    # el texto esta en XY mirando a +Z: se pone de pie en el plano YZ mirando a +X (o -X)
    if face == "+x":
        m = Matrix(((0, 0, 1), (1, 0, 0), (0, 1, 0)))            # normal +Z -> +X, renglon -> +Y (derecha de quien mira desde +X)
    else:
        m = Matrix(((0, 0, -1), (-1, 0, 0), (0, 1, 0)))          # normal +Z -> -X, renglon -> -Y
    bmesh.ops.transform(bm, matrix=m.to_4x4(), verts=bm.verts[:])
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(loc))
    return to_obj(bm, "txtcut")


def add(b, bm, mat, grain="x"):
    b.parts.append(Part(bm, mat, grain))


# ------------------------------------------------------------------ pistola
def make_pistol():
    """Pistola de servicio de linea VP70 (referencia del usuario, 2026-10-16): corredera larga de cantos rectos con faldon delantero que tapa el canon,
    nariz inclinada, remate trasero en cuna y estrias anchas solo arriba atras; armazon de polimero abombado con guardamonte grande de esquinas
    redondeadas, empunadura gruesa que sale del lomo trasero (que asoma tras la corredera), gatillo largo, seguro cruzado redondo con aro rojo, reten
    del cargador en el talon y placa triangular texturada. Grabado propio (sin marcas reales)."""
    b = Builder()
    steel, poly = "AA_Pavonado", "AA_Polimero"

    # ---- corredera: silueta lateral extruida, cantos redondeados por bisel; cortes de estrias, ventana, canon y grabado
    sl = [(-0.1955, 0.1085), (0.026, 0.1085), (0.040, 0.094), (0.040, 0.062), (-0.2035, 0.062), (-0.2035, 0.069)]
    slide = prism_yz(round_poly(sl, 0.0025, 5), -0.0138, 0.0138)
    cuts = []
    for sx in (-1, 1):
        for i in range(9):                                                                  # estrias anchas arriba atras
            cuts.append(box_bm((sx * 0.0138, -0.018 + i * 0.0058, 0.0985), (0.0034, 0.0028, 0.024)))
    cuts.append(box_bm((-0.0105, -0.03, 0.104), (0.012, 0.034, 0.016)))                       # ventana de expulsion (lado -X)
    bore = bmesh.new()
    bmesh.ops.create_cone(bore, cap_ends=True, segments=32, radius1=0.0074, radius2=0.0074, depth=0.03)
    bmesh.ops.rotate(bore, verts=bore.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(90), 3, "X"))
    bmesh.ops.translate(bore, verts=bore.verts[:], vec=Vector((0, -0.206, 0.087)))
    cuts.append(bore)
    cuts.append(box_bm((0, -0.12, 0.111), (0.004, 0.15, 0.004)))                              # canal de punteria sobre la corredera
    cuts.append(text_obj("Mod. M19", 0.0072, (0.0138 + 0.0004, -0.15, 0.085), 0.0011, 1.05, "+x"))
    cuts.append(text_obj("9 MM x 19", 0.0042, (0.0138 + 0.0004, -0.15, 0.0765), 0.0011, 1.1, "+x"))
    cuts.append(text_obj("SECTOR 7 POLICE DEPT.", 0.0052, (0.0138 + 0.0004, -0.07, 0.087), 0.0011, 1.1, "+x"))
    cuts.append(text_obj("GRIMHEIM", 0.0042, (0.0138 + 0.0004, -0.07, 0.0795), 0.0011, 1.1, "+x"))
    add(b, booleans(slide, cuts, bevel=(0.0018, 4, 35)), steel)
    # canon dentro de la nariz (con corona y anima) y capuchon visible por la ventana
    tube = bmesh.new()
    bmesh.ops.create_cone(tube, cap_ends=True, segments=32, radius1=0.0072, radius2=0.0072, depth=0.02)
    bmesh.ops.rotate(tube, verts=tube.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(90), 3, "X"))
    bmesh.ops.translate(tube, verts=tube.verts[:], vec=Vector((0, -0.192, 0.087)))
    inner = bmesh.new()
    bmesh.ops.create_cone(inner, cap_ends=True, segments=24, radius1=0.0045, radius2=0.0045, depth=0.03)
    bmesh.ops.rotate(inner, verts=inner.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(90), 3, "X"))
    bmesh.ops.translate(inner, verts=inner.verts[:], vec=Vector((0, -0.2, 0.087)))
    add(b, booleans(tube, [inner], bevel=(0.0008, 3, 50)), "AA_Acero")
    b.cyl((0, -0.19, 0.087), 0.0046, 0.002, "y", "AA_Negro", 24, 0.0)
    add(b, booleans(box_bm((0, -0.03, 0.1005), (0.0158, 0.034, 0.012)), [], bevel=(0.001, 2)), "AA_Acero")
    # miras: punto de mira en el canal y alza con muesca al final
    add(b, bevel_only(prism_yz(round_poly([(-0.192, 0.1085), (-0.183, 0.1085), (-0.1845, 0.1135), (-0.1905, 0.1135)], 0.001, 3), -0.0018, 0.0018), 0.0004, 2), steel)
    rear = prism_yz(round_poly([(0.008, 0.1085), (0.022, 0.1085), (0.022, 0.1145), (0.011, 0.1145)], 0.0012, 3), -0.009, 0.009)
    add(b, booleans(rear, [box_bm((0, 0.015, 0.116), (0.0034, 0.03, 0.006))], bevel=(0.0005, 2)), steel)

    # ---- armazon de polimero: una silueta (guardamonte + empunadura + lomo), muy redondeada
    fr = [(0.036, 0.063), (-0.2, 0.063), (-0.2028, 0.052), (-0.1975, 0.0405), (-0.114, 0.0395), (-0.1065, 0.031), (-0.1055, 0.009), (-0.098, -0.0005), (-0.084, -0.003),
          (-0.024, -0.003), (-0.012, 0.002), (-0.004, -0.012), (0.006, -0.05), (0.0085, -0.0615), (0.0145, -0.0665),
          (0.055, -0.0665), (0.0615, -0.059), (0.0655, -0.03), (0.0672, 0.02), (0.0665, 0.052), (0.06, 0.071), (0.049, 0.0795), (0.04, 0.0805)]
    frame = prism_round(round_poly(fr, 0.007, 6), 0.0152, 0.0085, 7)
    hole = prism_yz(round_poly([(-0.097, 0.0368), (-0.0978, 0.016), (-0.093, 0.0075), (-0.084, 0.0052), (-0.03, 0.0052), (-0.019, 0.011), (-0.0152, 0.0368)], 0.009, 7), -0.03, 0.03)
    fcuts = [hole]
    fcuts.append(box_bm((0, 0.035, -0.0665), (0.024, 0.036, 0.008)))                              # boca del cargador
    add(b, booleans(frame, fcuts, bevel=(0.0012, 3, 40)), poly)
    # placa triangular texturada en los dos costados de la empunadura
    for sx in (-1, 1):
        tri = round_poly([(0.019, -0.0525), (0.0505, -0.0525), (0.0505, -0.021)], 0.005, 5)
        plate = prism_yz(tri, sx * 0.0148, sx * 0.0156)
        add(b, bevel_only(plate, 0.0003, 1), "AA_Goma")
        bm = bmesh.new()                                                                      # punteado fino dentro de la placa
        for r_ in range(10):
            zz = -0.0495 + r_ * 0.0026
            for k in range(14):
                y = 0.0235 + k * 0.0024 + (0.0012 if r_ % 2 else 0)
                if y > 0.0475 or (y - 0.023) < (zz + 0.0525) * 0.95:
                    continue
                base, tip = sx * 0.0156, sx * 0.0162
                v = [bm.verts.new((base, y - 0.0008, zz - 0.0008)), bm.verts.new((base, y + 0.0008, zz - 0.0008)),
                     bm.verts.new((base, y + 0.0008, zz + 0.0008)), bm.verts.new((base, y - 0.0008, zz + 0.0008))]
                a_ = bm.verts.new((tip, y, zz))
                for i in range(4):
                    bm.faces.new((v[i], v[(i + 1) % 4], a_))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        add(b, bm, "AA_Goma")
    # gatillo largo y fino con estrias en la cara
    trig = round_poly([(-0.0555, 0.036), (-0.0605, 0.026), (-0.0618, 0.016), (-0.0598, 0.009), (-0.0545, 0.0078), (-0.052, 0.011),
                       (-0.0535, 0.019), (-0.052, 0.028), (-0.0485, 0.036)], 0.0016, 4)
    tcuts = [box_bm((0, -0.0612, 0.029 - i * 0.003), (0.01, 0.0016, 0.0009)) for i in range(6)]
    add(b, booleans(prism_round(trig, 0.0038, 0.0016, 3), tcuts, bevel=(0.0004, 2)), poly)
    b.cyl((0, -0.0525, 0.038), 0.0019, 0.0272, "x", "AA_Pavonado", 14, 0.0004)                    # pasador del gatillo
    # seguro cruzado: boton con aros en +X y anillo rojo en -X
    sy, sz = -0.012, 0.0255
    b.cyl((0.0158, sy, sz), 0.0052, 0.003, "x", "AA_Negro", 32, 0.001)
    for rr in (0.0038, 0.0024, 0.001):
        ring = [(0.0175, sy + rr * math.cos(2 * math.pi * k / 32), sz + rr * math.sin(2 * math.pi * k / 32)) for k in range(33)]
        b.sweep(ring, 0.00035, "AA_Negro", 5)
    b.cyl((-0.0158, sy, sz), 0.0052, 0.003, "x", "AA_Negro", 32, 0.001)
    b.cyl((-0.0172, sy, sz), 0.0042, 0.0006, "x", "AA_PlasticoRojo", 32, 0.0)
    b.cyl((-0.0176, sy, sz), 0.003, 0.0006, "x", "AA_Negro", 32, 0.0)
    # reten del cargador en el talon y base del cargador con labio
    add(b, bevel_only(prism_yz(round_poly([(0.058, -0.061), (0.066, -0.061), (0.067, -0.068), (0.059, -0.0685)], 0.0012, 3), -0.006, 0.006), 0.0005, 2), poly)
    mag = prism_yz(round_poly([(0.009, -0.0645), (0.058, -0.0645), (0.059, -0.0725), (0.0115, -0.0725), (0.0065, -0.069)], 0.0018, 4), -0.0158, 0.0158)
    add(b, bevel_only(mag, 0.0009, 3), "AA_Negro")
    for (y, z) in ((-0.008, 0.047), (0.022, 0.05)):
        b.cyl((0, y, z), 0.0018, 0.0262, "x", "AA_Pavonado", 14, 0.0004)                           # pasadores (a ras del armazon abombado)
    return b.finish("MeshyPistolLow", smooth_angle=30)


def preview():
    aa_clear()
    o = make_pistol()
    aa_preview_materials()
    ws = [Vector(c) for c in o.bound_box]
    print("min", [round(min(v[i] for v in ws), 3) for i in range(3)], "max", [round(max(v[i] for v in ws), 3) for i in range(3)],
          "tris", sum(len(p.vertices) - 2 for p in o.data.polygons))
    return o


if __name__ == "__main__" and "--" in sys.argv:
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    aa_clear()
    o = make_pistol()
    aa_export(o, os.path.join(out, "MeshyPistolLow.fbx"))
    print("hecho MeshyPistolLow tris", sum(len(p.vertices) - 2 for p in o.data.polygons), [m.name for m in o.data.materials], flush=True)
    print("PROCESO_TERMINADO")
