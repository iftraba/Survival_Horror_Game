"""Puerta AA (2026-10-15, calidad AA fase C, primera pieza): hoja de madera con largueros y travesanos de ensamble, plafones con relieve, moldura
de perfil de gola alrededor de cada hueco por las dos caras, mirilla de cristal con junquillo, y herrajes de verdad: tres pernios de cinco nudillos,
manilla de palanca curvada con roseta, cilindro de cerradura con escudo, placa de patada de acero con tornillos, cierrapuertas con brazo articulado
y placa con el numero. Marco (jamba y dintel) con moldura de chambrana y tapajuntas. Cantos biselados reales (no planos), normales suaves por angulo.

    blender --background --python build_door_aa.py -- <carpeta ABSOLUTA de salida>
    (en un Blender abierto: exec(open(ruta).read()); preview())

Materiales (cada uno su submalla; DoorModelKit les pone los materiales de Unity): Puerta_Madera (PBR escaneado dark_wood, UV en metros con la veta a lo largo
de cada pieza), Puerta_Laton, Puerta_Acero, Puerta_Negro. Mismas cajas que build_doors.py: la hoja va de x 0,02 a 1,48 y de z 0,02 a 2,38, 5 cm de grueso.
Ejes de Blender: X ancho, Z alto, Y grueso (+Y = cara delantera).
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

W0, W1, H0, H1, T = 0.02, 1.48, 0.02, 2.38, 0.05
MATS = ["Puerta_Madera", "Puerta_Laton", "Puerta_Acero", "Puerta_Negro"]
COLORS = {"Puerta_Madera": (0.2, 0.11, 0.06), "Puerta_Laton": (0.6, 0.45, 0.18), "Puerta_Acero": (0.6, 0.62, 0.64), "Puerta_Negro": (0.03, 0.03, 0.035)}


class Part:
    """Una pieza: su bmesh, su material y la direccion de la veta (para las UV de la madera)."""

    def __init__(self, bm, mat, grain="x"):
        self.bm, self.mat, self.grain = bm, mat, grain


class Builder:
    def __init__(self):
        self.parts = []

    # ---------- primitivas
    def box(self, c, s, mat, bevel=0.003, seg=3, grain="x", rot=None):
        bm = bmesh.new()
        r = bmesh.ops.create_cube(bm, size=1.0)
        for v in r["verts"]:
            v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
        if bevel > 0:
            bev = min(bevel, min(s) * 0.45)
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bev, offset_type="OFFSET", segments=seg, profile=0.5, affect="EDGES", clamp_overlap=True)
        if rot is not None:
            bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector((0, 0, 0)), matrix=rot)
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
        self.parts.append(Part(bm, mat, grain))
        return bm

    def cyl(self, c, r, depth, axis, mat, segs=24, bevel=0.0015):
        """Cilindro centrado en c con el eje 'x', 'y' o 'z'."""
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r, radius2=r, depth=depth)
        if bevel > 0:
            caps = [e for e in bm.edges if not all(abs(v.co.z) < 1e-6 for v in e.verts) and abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-6]
            bmesh.ops.bevel(bm, geom=caps, offset=min(bevel, r * 0.4, depth * 0.4), offset_type="OFFSET", segments=2, profile=0.5, affect="EDGES", clamp_overlap=True)
        if axis == "x":
            bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(90), 3, "Y"))
        elif axis == "y":
            bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(90), 3, "X"))
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
        self.parts.append(Part(bm, mat))
        return bm

    def frame_profile(self, rect, profile, y0, face, mat, cap=False, grain="x"):
        """Moldura alrededor de un hueco rectangular (x0, x1, z0, z1) en la cara 'face' (+1 / -1).
        profile: [(u, h)] de dentro afuera: u = cuanto se separa del borde del hueco, h = altura sobre y0 (hacia fuera de la cara).
        Cada punto del perfil es un anillo de 4 esquinas desplazado u: al unir anillos sale el inglete de las esquinas solo."""
        x0, x1, z0, z1 = rect
        bm = bmesh.new()
        rings = []
        for (u, h) in profile:
            y = y0 + face * h
            ring = [(x0 - u, y, z0 - u), (x1 + u, y, z0 - u), (x1 + u, y, z1 + u), (x0 - u, y, z1 + u)]
            rings.append([bm.verts.new(p) for p in ring])
        for a, b in zip(rings[:-1], rings[1:]):
            for k in range(4):
                k2 = (k + 1) % 4
                quad = (a[k], a[k2], b[k2], b[k]) if face > 0 else (a[k], b[k], b[k2], a[k2])
                bm.faces.new(quad)
        if cap:
            last = rings[-1]
            bm.faces.new(last if face > 0 else list(reversed(last)))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        self.parts.append(Part(bm, mat, grain))
        return bm

    def sweep(self, pts, r, mat, segs=12, r_end=None):
        """Tubo por una poligonal (manilla, brazo)."""
        bm = bmesh.new()
        pts = [Vector(p) for p in pts]
        rings = []
        for i, p in enumerate(pts):
            t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
            q = t.to_track_quat("Z", "Y").to_matrix()
            rr = r if r_end is None else r + (r_end - r) * i / (len(pts) - 1)
            rings.append([bm.verts.new(p + q @ Vector((rr * math.cos(2 * math.pi * k / segs), rr * math.sin(2 * math.pi * k / segs), 0))) for k in range(segs)])
        for a, b in zip(rings[:-1], rings[1:]):
            for k in range(segs):
                bm.faces.new((a[k], a[(k + 1) % segs], b[(k + 1) % segs], b[k]))
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        self.parts.append(Part(bm, mat))
        return bm

    # ---------- montaje
    def finish(self, name):
        bm = bmesh.new()                                 # la capa UV la trae cada pieza (si se crea otra aqui, Unity recibe las UV en el canal 2)
        for part in self.parts:
            mi = MATS.index(part.mat)
            src = part.bm
            src_uv = src.loops.layers.uv.verify()
            for f in src.faces:
                n = f.normal
                ax = max(range(3), key=lambda i: abs(n[i]))
                for loop in f.loops:
                    p = loop.vert.co
                    # proyeccion en caja, en metros; la veta va por U (asi viene la textura): U = eje de la veta
                    if ax == 1:      # caras delantera y trasera (X, Z)
                        uvp = (p.x, p.z) if part.grain == "x" else (p.z, p.x)     # comprobado en Unity: la veta de la textura va por U
                    elif ax == 0:    # cantos laterales (Y, Z)
                        uvp = (p.z, p.y) if part.grain == "z" else (p.y, p.z)
                    else:            # cantos de arriba y abajo (X, Y)
                        uvp = (p.x, p.y) if part.grain == "x" else (p.y, p.x)
                    loop[src_uv].uv = uvp
            for f in src.faces:
                f.material_index = mi
            tmp = bpy.data.meshes.new("tmp")
            src.to_mesh(tmp)
            nf = len(bm.faces)
            bm.from_mesh(tmp)
            bm.faces.ensure_lookup_table()
            for i in range(nf, len(bm.faces)):
                bm.faces[i].material_index = mi        # from_mesh pierde el indice (la malla temporal no tiene materiales)
            bpy.data.meshes.remove(tmp)
            src.free()
        mesh = bpy.data.meshes.new(name + "Mesh")
        for m in MATS:                                  # antes de to_mesh: sin huecos de material Blender recorta los indices a 0
            mat = bpy.data.materials.get(m) or bpy.data.materials.new(m)
            mat.diffuse_color = (*COLORS[m], 1.0)
            mesh.materials.append(mat)
        bm.to_mesh(mesh)
        bm.free()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))
        wn = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
        wn.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier="wn")
        # quita submallas vacias (los materiales sin caras) para que Unity no reciba huecos
        used = sorted({p.material_index for p in mesh.polygons})
        remap = {o: i for i, o in enumerate(used)}
        idx = [remap[p.material_index] for p in mesh.polygons]
        keep = [mesh.materials[i] for i in used]
        mesh.materials.clear()                          # vaciar los materiales pone los indices a 0: se vuelven a poner despues
        for m in keep:
            mesh.materials.append(m)
        for p, i in zip(mesh.polygons, idx):
            p.material_index = i
        return obj


# ------------------------------------------------------------------ hoja
OGEE = [(0.0, 0.0), (0.002, 0.004), (0.006, 0.008), (0.010, 0.010), (0.014, 0.0095), (0.017, 0.007), (0.020, 0.0055), (0.024, 0.006), (0.026, 0.008), (0.028, 0.008), (0.030, 0.0)]


def make_leaf():
    b = Builder()
    cx = (W0 + W1) / 2
    st, top_r, bot_r = 0.13, 0.13, 0.23
    lock_r = (0.95, 1.10)
    mid_r = (1.33, 1.47)
    win = (cx - 0.31, cx + 0.31, 1.47, 1.97)          # hueco del cristal (lo rellena el Window del prefab)
    ty = T / 2
    # largueros y travesanos (grueso completo) con cantos biselados
    b.box((W0 + st / 2, 0, (H0 + H1) / 2), (st, T, H1 - H0), "Puerta_Madera", 0.004, grain="z")
    b.box((W1 - st / 2, 0, (H0 + H1) / 2), (st, T, H1 - H0), "Puerta_Madera", 0.004, grain="z")
    inner = W1 - W0 - 2 * st
    for (z0, z1) in ((H0, H0 + bot_r), lock_r, mid_r, (H1 - top_r, H1)):
        b.box((cx, 0, (z0 + z1) / 2), (inner, T, z1 - z0), "Puerta_Madera", 0.003, grain="x")
    # montantes a los lados del cristal (entre el travesano medio y el de arriba)
    for (x0, x1) in ((W0 + st, win[0]), (win[1], W1 - st)):
        b.box(((x0 + x1) / 2, 0, (mid_r[1] + H1 - top_r) / 2), (x1 - x0, T, H1 - top_r - mid_r[1]), "Puerta_Madera", 0.003, grain="z")
    b.box((cx, 0, (win[3] + H1 - top_r) / 2), (win[1] - win[0], T, H1 - top_r - win[3]), "Puerta_Madera", 0.003, grain="x")
    # plafones: bajo (z bot..lock) y medio (lock..mid). Alma fina + campo en relieve por cada cara (anillos de perfil)
    for (z0, z1) in ((H0 + bot_r, lock_r[0]), (lock_r[1], mid_r[0])):
        rect = (W0 + st, W1 - st, z0, z1)
        b.box((cx, 0, (z0 + z1) / 2), (inner + 0.01, 0.012, z1 - z0 + 0.01), "Puerta_Madera", 0.0, grain="z")
        for face in (1, -1):
            # plafon con relieve: del canto (pegado al alma) sube en rampa al campo
            inset = 0.045
            prof = [(-0.0, 0.0), (-0.02, 0.004), (-inset, 0.009), (-inset - 0.004, 0.0105)]
            b.frame_profile((rect[0], rect[1], rect[2], rect[3]), prof, 0.006 * face, face, "Puerta_Madera", cap=True, grain="z")
            # moldura de gola alrededor del hueco del plafon, sobre largueros y travesanos
            b.frame_profile((rect[0], rect[1], rect[2], rect[3]), [(u, h) for (u, h) in OGEE], ty * face, face, "Puerta_Madera", grain="x")
    # junquillo del cristal por las dos caras
    for face in (1, -1):
        b.frame_profile(win, [(0.0, 0.0), (0.0, 0.006), (0.004, 0.009), (0.010, 0.010), (0.016, 0.006), (0.02, 0.0)], ty * face, face, "Puerta_Madera", grain="x")
    # ---------- herrajes
    hx = W1 - 0.065
    for face in (1, -1):
        y = face * ty
        # roseta, cuello y manilla de palanca curvada hacia la bisagra
        b.cyl((hx, y + face * 0.004, 1.02), 0.028, 0.008, "y", "Puerta_Laton", 32, 0.002)
        b.cyl((hx, y + face * 0.022, 1.02), 0.011, 0.03, "y", "Puerta_Laton", 20, 0.0)
        lever = []
        for k in range(9):
            t = k / 8
            lever.append((hx - 0.13 * t, y + face * (0.036 + 0.014 * math.sin(t * math.pi * 0.6)), 1.02 - 0.012 * t * t))
        b.sweep(lever, 0.0095, "Puerta_Laton", 14, 0.0085)
        b.box((hx - 0.13, y + face * 0.042, 1.0175), (0.022, 0.02, 0.02), "Puerta_Laton", 0.007)          # remate de la palanca
        # escudo del cilindro y cilindro
        b.box((hx, y + face * 0.003, 0.9), (0.034, 0.006, 0.07), "Puerta_Laton", 0.0025)
        b.cyl((hx, y + face * 0.008, 0.905), 0.0115, 0.006, "y", "Puerta_Laton", 20, 0.001)
        b.box((hx, y + face * 0.0115, 0.905), (0.0025, 0.002, 0.012), "Puerta_Negro", 0.0)              # ranura de la llave
        # placa de patada de acero con 8 tornillos
        b.box((cx, y + face * 0.0012, 0.12), (W1 - W0 - 0.05, 0.0024, 0.2), "Puerta_Acero", 0.0011)
        for i in range(4):
            for zz in (0.035, 0.205):
                b.cyl((W0 + 0.05 + i * (W1 - W0 - 0.1) / 3, y + face * 0.0028, zz), 0.004, 0.0016, "y", "Puerta_Acero", 10, 0.0)
    # cierrapuertas (cara -Y, arriba): cuerpo con tapas redondas, brazo y zapata en el marco
    yb = -ty - 0.03
    b.box((cx - 0.36, yb, H1 - 0.16), (0.26, 0.05, 0.062), "Puerta_Negro", 0.012)
    for sx in (-1, 1):
        b.cyl((cx - 0.36 + sx * 0.13, yb, H1 - 0.16), 0.031, 0.05, "y", "Puerta_Negro", 20, 0.004)
    b.cyl((cx - 0.26, yb - 0.028, H1 - 0.16), 0.012, 0.012, "y", "Puerta_Acero", 16, 0.0)
    b.sweep([(cx - 0.26, yb - 0.036, H1 - 0.16), (cx - 0.05, yb - 0.036, H1 - 0.12)], 0.007, "Puerta_Negro", 10)
    b.sweep([(cx - 0.05, yb - 0.036, H1 - 0.12), (cx + 0.16, yb - 0.036, H1 - 0.05)], 0.006, "Puerta_Negro", 10)
    b.cyl((cx - 0.05, yb - 0.04, H1 - 0.12), 0.01, 0.014, "y", "Puerta_Acero", 14, 0.0)
    # placa con el numero (cara +Y)
    b.box((cx, ty + 0.0015, 1.72), (0.16, 0.003, 0.06), "Puerta_Laton", 0.0012)
    for sx in (-1, 1):
        b.cyl((cx + sx * 0.068, ty + 0.0033, 1.72), 0.003, 0.0012, "y", "Puerta_Laton", 8, 0.0)
    # pernios: tres, de cinco nudillos, en el canto de la bisagra
    for z in (0.28, 1.2, 2.12):
        for k in range(5):
            zz = z - 0.05 + k * 0.025
            b.cyl((W0 - 0.006, 0, zz + 0.0125), 0.0075, 0.0235, "z", "Puerta_Laton", 16, 0.0008)
        b.cyl((W0 - 0.006, 0, z + 0.077), 0.004, 0.006, "z", "Puerta_Laton", 10, 0.0)                     # cabeza del pasador
        b.box((W0 + 0.012, 0, z + 0.0125), (0.03, T + 0.002, 0.125), "Puerta_Laton", 0.0006)               # ala
    return b.finish("DoorLeaf")


# ------------------------------------------------------------------ marco
CASING = [(0.0, 0.0), (0.0, 0.016), (0.008, 0.019), (0.022, 0.02), (0.034, 0.018), (0.046, 0.013), (0.058, 0.01), (0.07, 0.009), (0.075, 0.004), (0.078, 0.0)]


def casing_vertical(b, x_in, x_dir, z0, z1, mat):
    """Chambrana vertical: perfil CASING extruido de z0 a z1; x_in = canto del hueco, x_dir = hacia donde se aleja del hueco (+1/-1)."""
    bm = bmesh.new()
    for face in (1, -1):
        ring0 = [bm.verts.new((x_in + x_dir * u, face * (0.025 + h), z0)) for (u, h) in CASING]
        ring1 = [bm.verts.new((x_in + x_dir * u, face * (0.025 + h), z1)) for (u, h) in CASING]
        for k in range(len(CASING) - 1):
            q = (ring0[k], ring0[k + 1], ring1[k + 1], ring1[k])
            bm.faces.new(q)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for f in bm.faces:
        if f.normal.y * (1 if f.calc_center_median().y > 0 else -1) < 0:
            f.normal_flip()
    b.parts.append(Part(bm, mat, "z"))


def make_jamb():
    """Jamba (caja de Unity: x -0,09..0,01, z -0,025..2,425, 5 cm): galce con tope de puerta, chambrana a las dos caras y plinto."""
    b = Builder()
    x0, x1, z0, z1 = -0.09, 0.01, -0.025, 2.425
    b.box(((x0 + x1) / 2, 0, (z0 + z1) / 2), (x1 - x0, 0.05, z1 - z0), "Puerta_Madera", 0.003, grain="z")
    b.box((x1 - 0.004, 0.0, (z0 + z1) / 2), (0.012, 0.022, z1 - z0 - 0.02), "Puerta_Madera", 0.002, grain="z")          # tope de la puerta
    casing_vertical(b, x0 + 0.072, -1, z0 + 0.14, z1, "Puerta_Madera")
    for face in (1, -1):
        b.box((x0 + 0.03, face * 0.034, z0 + 0.075), (0.09, 0.022, 0.15), "Puerta_Madera", 0.003, grain="z")            # plinto
    return b.finish("DoorJamb")


def make_header():
    """Dintel (caja de Unity: x -0,09..1,59, z 2,40..2,50): galce y chambrana horizontal con cornisa."""
    b = Builder()
    x0, x1, z0, z1 = -0.09, 1.59, 2.40, 2.50
    b.box(((x0 + x1) / 2, 0, (z0 + z1) / 2), (x1 - x0, 0.05, z1 - z0), "Puerta_Madera", 0.003, grain="x")
    for face in (1, -1):
        b.box(((x0 + x1) / 2, face * 0.036, z0 + 0.05), (x1 - x0, 0.022, 0.09), "Puerta_Madera", 0.004, grain="x")
        b.box(((x0 + x1) / 2, face * 0.04, z1 - 0.012), (x1 - x0 + 0.03, 0.03, 0.024), "Puerta_Madera", 0.006, grain="x")   # cornisa
        b.box(((x0 + x1) / 2, face * 0.046, z0 + 0.012), (x1 - x0 - 0.02, 0.01, 0.01), "Puerta_Madera", 0.003, grain="x")    # junquillo
    return b.finish("DoorHeader")


def clear():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials):
        for it in list(coll):
            coll.remove(it)


def export(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             mesh_smooth_type="FACE", path_mode="STRIP", bake_anim=False)


def preview():
    clear()
    leaf = make_leaf()
    jamb = make_jamb()
    head = make_header()
    jamb.location.x -= 0.0
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
            r3d = area.spaces[0].region_3d
            from mathutils import Euler
            r3d.view_rotation = Euler((math.radians(85), 0, math.radians(20))).to_quaternion()
            r3d.view_distance = 3.2
            r3d.view_location = (0.75, 0, 1.2)
    for o in (leaf, jamb, head):
        print(o.name, "tris", sum(len(p.vertices) - 2 for p in o.data.polygons))


if __name__ == "__main__" and "--" in sys.argv:
    out = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(out, exist_ok=True)
    for fn in (make_leaf, make_jamb, make_header):
        clear()
        obj = fn()
        d = obj.dimensions
        export(obj, os.path.join(out, obj.name + ".fbx"))
        print("hecho", obj.name, "%.3f x %.3f x %.3f m" % (d.x, d.y, d.z), "tris", sum(len(p.vertices) - 2 for p in obj.data.polygons), "mats", [m.name for m in obj.data.materials], flush=True)
    print("PROCESO_TERMINADO")
