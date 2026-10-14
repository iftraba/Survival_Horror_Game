"""Fachada de la comisaria y patio delantero (2026-10-14, fase 4 del lavado de cara del exterior).

    blender --background --python build_station_front.py -- <carpeta ABSOLUTA de salida> [Nombre ...]
    (en un Blender abierto: exec(open(ruta).read()); preview("StationSign"); preview_all())

Modelos: StationSign (placa con letras SECTOR 7 / POLICE DEPARTMENT y escudo), FlagPole (mastil con soporte; la tela va aparte), FlagCloth (tela con UV, sin
hornear: la textura es la de la bandera), Planter, SandbagWall, Floodlight, WallLamp, RazorWire, SignHazard.
La pared queda a +Y de Blender (frente = -Y = +Z de Unity) en lo que se cuelga de la fachada; lo que se pone en el patio tiene el origen en el centro de la base.
"""
import math
import os
import random
import sys

_NO_MAIN = True
HERE4 = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE4, "build_city.py"), encoding="utf-8").read())

FRONT = dict(CITY, lens_head=(0.93, 0.92, 0.84), brass=(0.7, 0.52, 0.18), carpaint_navy=(0.03, 0.05, 0.16), fabric_sand=(0.5, 0.42, 0.27), concrete_dirt=(0.1, 0.07, 0.05), carpaint_yellow=(0.85, 0.65, 0.04))


def solid_text(m, text, size, matrix, mat, depth=0.04, bevel=0.003):
    """Texto con grosor (letras sueltas de la placa): se evalua el objeto de texto y sus caras pasan a m.bm con la transformacion dada."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = depth / 2
    cu.bevel_depth = 0.0
    cu.resolution_u = 2
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    cu.space_character = 1.15
    obj = bpy.data.objects.new("txt", cu)
    bpy.context.scene.collection.objects.link(obj)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.curves.remove(cu)
    bm = bmesh.new()
    bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts[:])
    vmap = {v: m.bm.verts.new(v.co) for v in bm.verts}
    for f in bm.faces:
        try:
            nf = m.bm.faces.new([vmap[v] for v in f.verts])
        except ValueError:
            continue
        nf.material_index = m._idx(mat)
        nf.smooth = False
    bm.free()


def make_sign():
    m = new("stationsign", FRONT)
    W, H, D = 5.6, 1.05, 0.14
    m.box((0, D / 2, H / 2), (W, D, H), "carpaint_navy", bevel=0.02)                                  # placa
    for z in (0.04, H - 0.04):
        m.box((0, -0.002, z), (W - 0.12, 0.03, 0.05), "brass", bevel=0.005)                           # filetes dorados
    for x in (-W / 2 + 0.06, W / 2 - 0.06):
        m.box((x, -0.002, H / 2), (0.05, 0.03, H - 0.08), "brass", bevel=0.005)
    # letras: "SECTOR 7" grande y "POLICE DEPARTMENT" pequeno, vistas desde -Y (texto en XY de Blender: se levanta al plano XZ mirando a -Y)
    rot = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))                           # (x, y, z) -> (x, -z, y)
    solid_text(m, "SECTOR 7", 0.42, Matrix.Translation((0.55, -0.045, 0.66)) @ rot, "brass", 0.05)
    solid_text(m, "POLICE DEPARTMENT", 0.17, Matrix.Translation((0.55, -0.045, 0.27)) @ rot, "brass", 0.035)
    # escudo a la izquierda: pentagono azul con borde y estrella dorada
    pts = [(-0.3, 0.34), (0.3, 0.34), (0.33, -0.08), (0.0, -0.4), (-0.33, -0.08)]
    def shield(scale, z0, mat, off):
        vs = [m.bm.verts.new((-2.05 + p[0] * scale, -0.04 - off, z0 + p[1] * scale)) for p in pts]
        f = m.bm.faces.new(vs)
        f.material_index = m._idx(mat)
        f.smooth = False
        m._deco.append((f, Vector((0, -1, 0))))
    shield(1.0, 0.55, "brass", 0.0)
    shield(0.88, 0.55, "carpaint_navy", 0.004)
    star = []
    for k in range(10):
        a = math.radians(90 + 36 * k)
        rr = 0.17 if k % 2 == 0 else 0.07
        star.append((-2.05 + rr * math.cos(a), -0.049, 0.58 + rr * math.sin(a)))
    vs = [m.bm.verts.new(p) for p in star]
    f = m.bm.faces.new(vs)
    f.material_index = m._idx("brass")
    f.smooth = False
    m._deco.append((f, Vector((0, -1, 0))))
    # dos focos de cuello de cisne encima
    for sx in (-1.5, 1.5):
        tube(m, [(sx, 0.1, H + 0.02), (sx, -0.12, H + 0.3), (sx, -0.5, H + 0.34)], 0.018, "metal_dark", 6)
        m.box((sx, -0.55, H + 0.3), (0.22, 0.2, 0.1), "metal_dark", bevel=0.02, rot=(20, 0, 0))
        m.box((sx, -0.62, H + 0.26), (0.16, 0.1, 0.02), "lens_head")
    return finish(m, "StationSign")


def make_flag_pole():
    m = new("flagpole", FRONT)
    m.box((0, 0.04, 0), (0.22, 0.08, 0.22), "metal_dark", bevel=0.01)                                 # soporte a la pared
    tube(m, [(0, 0.0, 0.0), (0, -0.8, 1.25), (0, -1.45, 2.2)], 0.035, "metal", 8, 0.02)               # mastil inclinado
    cyl(m, (0, -1.45, 2.2), 0.04, 0.05, "brass", 8)
    m.ellipsoid((0, -1.46, 2.28), (0.045, 0.045, 0.045), "brass", 8, 5)
    return finish(m, "FlagPole")


def make_flag_cloth():
    """Tela de 2,2 x 1,4 m con ondas, UV de 0 a 1, doble cara. Origen en el borde izquierdo de arriba."""
    bm = bmesh.new()
    nx, nz = 16, 10
    W, H = 2.2, 1.4
    grid = []
    for j in range(nz + 1):
        row = []
        for i in range(nx + 1):
            u, v = i / nx, j / nz
            x = W * u
            drape = 0.18 * math.sin(u * math.pi * 2.3 + v * 1.4) * u                                 # ondas en el aire, mas hacia la punta
            y = -drape - 0.1 * v * u
            z = -H * v - 0.06 * u * u
            row.append(bm.verts.new((x, y, z)))
        grid.append(row)
    uv = bm.loops.layers.uv.new("UVMap")
    for j in range(nz):
        for i in range(nx):
            f = bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
            f.smooth = True
            for loop, (u, v) in zip(f.loops, ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1))):
                loop[uv].uv = (u / nx, 1.0 - v / nz)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    mesh = bpy.data.meshes.new("FlagClothMesh")
    bm.to_mesh(mesh)
    bm.free()
    mat = bpy.data.materials.new("flagcloth")
    mat.diffuse_color = (0.8, 0.8, 0.8, 1)
    mesh.materials.append(mat)
    obj = bpy.data.objects.new("FlagCloth", mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def make_planter():
    m = new("planter", FRONT)
    L, W2, H = 2.2, 0.85, 0.62
    m.box((0, 0, H / 2), (L, W2, H), "concrete", bevel=0.03)
    m.box((0, 0, H + 0.01), (L - 0.16, W2 - 0.16, 0.04), "concrete_dirt", bevel=0.01)                  # tierra reseca
    m.box((0, 0, 0.04), (L + 0.06, W2 + 0.06, 0.08), "concrete", bevel=0.01)                            # base
    rng = random.Random(8)
    for k in range(3):                                                                                    # arbustos secos
        x = -0.7 + 0.7 * k + rng.uniform(-0.1, 0.1)
        for b in range(7):
            a = rng.uniform(0, 2 * math.pi)
            ln = rng.uniform(0.5, 1.0)
            tube(m, [(x, 0, H), (x + 0.15 * math.cos(a), 0.15 * math.sin(a), H + ln * 0.5), (x + 0.4 * math.cos(a), 0.4 * math.sin(a), H + ln)], 0.012, "bark", 5, 0.004)
    m.box((0.5, -0.1, H + 0.04), (0.25, 0.18, 0.08), "concrete", rot=(0, 0, 30), bevel=0.02)            # piedra
    return finish(m, "Planter")


def make_sandbags():
    m = new("sandbags", FRONT)
    rng = random.Random(4)
    rows, per = 4, 6
    for r in range(rows):
        n = per - (1 if r % 2 else 0) - (r // 2)
        for i in range(n):
            x = (i - (n - 1) / 2) * 0.52 + (0.26 if r % 2 and False else 0)
            y = rng.uniform(-0.03, 0.03)
            z = 0.13 + r * 0.2
            m.ellipsoid((x, y, z), (0.27, 0.17, 0.11), "fabric_sand", 10, 6)
            m.box((x + rng.uniform(-0.02, 0.02), y, z + 0.0), (0.4, 0.2, 0.1), "fabric_sand", bevel=0.045)
    return finish(m, "SandbagWall")


def make_floodlight():
    m = new("floodlight", FRONT)
    for k in range(3):
        a = k * 2 * math.pi / 3 + 0.4
        tube(m, [(0.55 * math.cos(a), 0.55 * math.sin(a), 0.0), (0.0, 0.0, 1.55)], 0.022, "metal_dark", 6)
    tube(m, [(0, 0, 1.4), (0, 0, 3.1)], 0.035, "metal", 8, 0.03)
    m.box((0, 0, 3.1), (0.9, 0.08, 0.08), "metal_dark")
    for sx in (-0.38, 0.38):
        m.box((sx, -0.12, 3.12), (0.34, 0.24, 0.28), "metal_dark", bevel=0.02, rot=(-12, 0, 0))
        m.box((sx, -0.25, 3.1), (0.3, 0.02, 0.24), "lens_head", bevel=0.005, rot=(-12, 0, 0))
    tube(m, [(0.2, 0.05, 1.5), (0.4, 0.4, 0.6), (0.9, 0.8, 0.0)], 0.01, "plastic", 4)                   # cable
    m.box((0.95, 0.85, 0.05), (0.25, 0.2, 0.1), "metal_dark", bevel=0.01)
    return finish(m, "Floodlight")


def make_wall_lamp():
    m = new("walllamp", FRONT)
    m.box((0, 0.03, 0), (0.18, 0.06, 0.22), "metal_dark", bevel=0.01)
    tube(m, [(0, 0.0, 0.0), (0, -0.2, 0.0), (0, -0.34, 0.1)], 0.02, "metal_dark", 6)
    m.ellipsoid((0, -0.4, 0.0), (0.12, 0.12, 0.11), "lens_head", 10, 6)
    for k in range(6):                                                                                    # jaula
        a = k * math.pi / 3
        tube(m, [(0.13 * math.cos(a), -0.4 + 0.13 * math.sin(a) * 0.0, 0.13 * math.sin(a) * 0.0 + 0.0), (0.0, -0.4, 0.0)], 0.0, "metal_dark", 3) if False else None
    for r in (0.0, 0.07):
        m._loft_rings([[(0.135 * math.cos(2 * math.pi * k / 14), -0.4 + 0.135 * math.sin(2 * math.pi * k / 14) * 0 + 0.135 * math.sin(2 * math.pi * k / 14), 0.0 + 0.0) for k in range(14)]], "metal_dark", True) if False else None
    cyl(m, (0, -0.46, -0.12), 0.1, 0.03, "metal_dark", 10)
    cyl(m, (0, -0.46, 0.11), 0.07, 0.03, "metal_dark", 10)
    for k in range(6):
        a = 2 * math.pi * k / 6
        tube(m, [(0.1 * math.cos(a), -0.46 + 0.1 * math.sin(a) * 0.0 + 0.0, 0.11 + 0.0), (0.13 * math.cos(a), -0.46, 0.0), (0.1 * math.cos(a), -0.46, -0.12)], 0.007, "metal_dark", 4)
    return finish(m, "WallLamp")


def make_razor_wire():
    m = new("razorwire", FRONT)
    L, R, turns = 3.0, 0.19, 11
    pts = []
    for k in range(turns * 14 + 1):
        t = k / (turns * 14)
        a = t * turns * 2 * math.pi
        pts.append((-L / 2 + L * t, R * math.cos(a), R + R * math.sin(a)))
    tube(m, pts, 0.007, "metal", 4)
    for k in range(turns * 4):                                                                           # cuchillas
        t = k / (turns * 4)
        a = t * turns * 2 * math.pi
        x, y, z = -L / 2 + L * t, R * math.cos(a), R + R * math.sin(a)
        m.box((x, y * 1.06, z * 1.0 + 0.0), (0.012, 0.045, 0.02), "metal", rot=(math.degrees(a), 0, 0))
    for x in (-L / 2, L / 2):
        tube(m, [(x, 0, 0), (x, 0, 0.1)], 0.015, "metal_dark", 5)
    return finish(m, "RazorWire")


def make_hazard_sign():
    m = new("hazardsign", FRONT)
    tri = [(-0.3, -0.26), (0.3, -0.26), (0.0, 0.26)]
    def tri_plate(s, y, mat):
        vs = [m.bm.verts.new((p[0] * s, y, p[1] * s)) for p in tri]
        f = m.bm.faces.new(vs)
        f.material_index = m._idx(mat)
        f.smooth = False
        m._deco.append((f, Vector((0, -1, 0))))
    m.box((0, 0.01, 0.0), (0.64, 0.02, 0.58), "metal_dark", bevel=0.01) if False else None
    tri_plate(1.0, -0.004, "decal_k")
    tri_plate(0.86, -0.008, "carpaint_yellow")
    m.box((0, -0.014, -0.02), (0.05, 0.005, 0.16), "decal_k")                                              # signo de exclamacion
    m.box((0, -0.014, -0.15), (0.06, 0.005, 0.06), "decal_k")
    m.box((0, 0.012, 0.0), (0.7, 0.012, 0.5), "metal_dark", bevel=0.005) if False else None
    m.box((0, 0.02, 0.0), (0.04, 0.02, 0.04), "metal_dark")
    return finish(m, "SignHazard")


BUILDERS = {"StationSign": make_sign, "FlagPole": make_flag_pole, "Planter": make_planter, "SandbagWall": make_sandbags, "Floodlight": make_floodlight,
            "WallLamp": make_wall_lamp, "RazorWire": make_razor_wire, "SignHazard": make_hazard_sign, "FlagCloth": make_flag_cloth}


def preview(name, rz=25):
    _clear()
    obj = BUILDERS[name]()
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    d = obj.dimensions
    view(80, rz, max(d.x, d.y, d.z) * 1.8 + 1.0, (0, 0, d.z * 0.5))
    print(name, "%.2f x %.2f x %.2f m" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    return obj


def preview_all():
    _clear()
    x = 0.0
    for name, fn in BUILDERS.items():
        obj = fn()
        d = obj.dimensions
        obj.location = (x + d.x / 2 + 0.2, 0, 0)
        x += d.x + 1.0
        print(name, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "%d tris" % sum(len(p.vertices) - 2 for p in obj.data.polygons))
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
    view(80, 10, x * 0.8, (x / 2, 0, 1.0))


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
