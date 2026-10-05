"""Armas, objetos del suelo y atrezzo (muebles, cajas, barriles...) en Blender.

    exec(open(r"<ruta>/build_items.py").read())
    build_items(r"<carpeta Assets/_Project/Art/Weapons>")     # pistola, escopeta, municion, spray, llave
    build_props(r"<carpeta Assets/_Project/Art/Props>")       # cajas, barril, taquilla, escritorio...

Convenciones (las mismas que usa el resto del proyecto):
  - Armas: origen en el agarre, canon hacia -Y, arriba +Z.
  - Atrezzo: origen en el centro de la base (z = 0 es el suelo), frente hacia -Y.
"""
import math

import bmesh
import bpy
import mathutils
from mathutils import Vector


# ------------------------------------------------------------------ utilidades de modelado
class Mesher:
    def __init__(self, prefix, palette):
        self.prefix = prefix
        self.palette = palette
        self.names = list(palette.keys())
        self.bm = bmesh.new()

    def _idx(self, mat):
        return self.names.index(mat)

    def _loft_rings(self, rings, mat, smooth):
        bm = self.bm
        vr = [[bm.verts.new(Vector(p)) for p in ring] for ring in rings]
        n = len(vr[0])
        faces = []
        for i in range(len(vr) - 1):
            for j in range(n):
                j2 = (j + 1) % n
                faces.append(bm.faces.new((vr[i][j], vr[i][j2], vr[i + 1][j2], vr[i + 1][j])))
        faces.append(bm.faces.new(vr[0]))
        faces.append(bm.faces.new(list(reversed(vr[-1]))))
        for f in faces:
            f.material_index = self._idx(mat)
            f.smooth = smooth

    def loft_z(self, secs, mat, segs=12, smooth=True):
        """secs: [(cx, cy, cz, rx, ry)] ordenadas en Z."""
        rings = [[(cx + rx * math.cos(a), cy + ry * math.sin(a), cz)
                  for a in (2 * math.pi * k / segs for k in range(segs))] for cx, cy, cz, rx, ry in secs]
        self._loft_rings(rings, mat, smooth)

    def loft_y(self, secs, mat, segs=12, smooth=True):
        """secs: [(cx, cy, cz, rx, rz)] ordenadas en Y."""
        rings = [[(cx + rx * math.cos(a), cy, cz + rz * math.sin(a))
                  for a in (2 * math.pi * k / segs for k in range(segs))] for cx, cy, cz, rx, rz in secs]
        self._loft_rings(rings, mat, smooth)

    def ellipsoid(self, c, r, mat, segs=10, rings=6):
        secs = []
        for i in range(rings):
            t = math.pi * (i + 0.5) / rings
            secs.append((c[0], c[1], c[2] + r[2] * math.cos(t), r[0] * math.sin(t), r[1] * math.sin(t)))
        self.loft_z(secs, mat, segs)

    def box(self, c, s, mat, rot=(0, 0, 0), bevel=0.0):
        bm = self.bm
        res = bmesh.ops.create_cube(bm, size=1.0)
        verts = res["verts"]
        rm = mathutils.Euler([math.radians(a) for a in rot]).to_matrix()
        for v in verts:
            v.co = rm @ Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2])) + Vector(c)
        mi = self._idx(mat)
        vset = set(verts)
        if bevel > 0:
            edges = list({e for v in verts for e in v.link_edges})
            r2 = bmesh.ops.bevel(bm, geom=edges, offset=bevel, offset_type="OFFSET", segments=2, profile=0.6, affect="EDGES")
            vset |= set(r2["verts"])
        for f in bm.faces:
            if f.is_valid and all(v in vset for v in f.verts):
                f.material_index = mi
                f.smooth = False

    def finish(self, name):
        bm = self.bm
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
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
            mesh.materials.append(_mat(f"{self.prefix}_{self.names[old]}", self.palette[self.names[old]]))
        return obj


def _mat(name, rgb):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    try:
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        if b:
            b.inputs["Base Color"].default_value = (*rgb, 1.0)
            b.inputs["Roughness"].default_value = 0.6
    except Exception:
        pass
    return m


def _clear():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials):
        for it in list(coll):
            coll.remove(it)


def _export(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        mesh_smooth_type="FACE", path_mode="COPY", bake_anim=False)


# ------------------------------------------------------------------ armas
def make_pistol():
    m = Mesher("pistol", {"steel": (0.55, 0.56, 0.60), "poly": (0.07, 0.07, 0.08), "dark": (0.03, 0.03, 0.035)})
    m.box((0, -0.095, 0.047), (0.030, 0.205, 0.036), "steel", bevel=0.004)                  # corredera
    m.loft_y([(0, -0.2155, 0.050, 0.0085, 0.0085), (0, -0.196, 0.050, 0.0085, 0.0085)], "dark", 10)   # boca del canon
    m.box((0.0150, -0.075, 0.056), (0.004, 0.045, 0.016), "dark")                           # ventana de expulsion
    for i in range(5):                                                                       # estrias traseras
        for sx in (1, -1):
            m.box((0.0155 * sx, 0.001 - i * 0.007, 0.047), (0.003, 0.0025, 0.030), "dark")
    m.box((0, -0.070, 0.016), (0.028, 0.150, 0.026), "poly", bevel=0.003)                    # armazon
    m.box((0, 0.012, -0.040), (0.031, 0.052, 0.108), "poly", rot=(12, 0, 0), bevel=0.004)    # empunadura
    m.box((0, 0.024, -0.095), (0.033, 0.056, 0.010), "dark", rot=(12, 0, 0), bevel=0.002)    # base del cargador
    m.box((0, -0.058, 0.0), (0.007, 0.007, 0.026), "poly")                                   # guardamonte: frente
    m.box((0, -0.040, -0.012), (0.007, 0.050, 0.007), "poly")                                # guardamonte: base
    m.box((0, -0.034, 0.0), (0.006, 0.008, 0.020), "dark", rot=(-15, 0, 0))                  # gatillo
    m.box((0, -0.200, 0.069), (0.004, 0.008, 0.009), "dark")                                 # punto de mira
    for sx in (1, -1):
        m.box((0.007 * sx, -0.002, 0.068), (0.004, 0.008, 0.010), "dark")                    # alza
    m.box((0.0165, -0.045, 0.020), (0.003, 0.020, 0.005), "dark")                            # palanca de cierre
    return m.finish("Pistol")


def make_shotgun():
    m = Mesher("shotgun", {"steel": (0.25, 0.26, 0.30), "wood": (0.40, 0.22, 0.10), "dark": (0.05, 0.05, 0.05), "brass": (0.75, 0.60, 0.20)})
    m.loft_y([(0, -0.755, 0.040, 0.0168, 0.0168), (0, -0.740, 0.040, 0.0150, 0.0150), (0, -0.12, 0.040, 0.0150, 0.0150)], "steel", 12)  # canon
    m.loft_y([(0, -0.58, 0.012, 0.0125, 0.0125), (0, -0.14, 0.012, 0.0125, 0.0125)], "steel", 10)                                      # tubo del cargador
    m.loft_y([(0, -0.585, 0.012, 0.0150, 0.0150), (0, -0.565, 0.012, 0.0150, 0.0150)], "dark", 10)                                     # tapon
    m.ellipsoid((0, -0.740, 0.0585), (0.0045, 0.0045, 0.0045), "brass", 8, 5)                                                          # punto de mira
    m.box((0, 0.010, 0.030), (0.040, 0.250, 0.062), "steel", bevel=0.006)                                                              # caja de mecanismos
    m.box((0.0205, 0.000, 0.040), (0.003, 0.070, 0.022), "dark")                                                                       # ventana de expulsion
    m.box((0, -0.310, 0.008), (0.050, 0.190, 0.046), "wood", bevel=0.008)                                                              # bomba
    for i in range(6):
        m.box((0, -0.385 + i * 0.030, 0.008), (0.052, 0.004, 0.040), "dark")                                                           # estrias de la bomba
    m.loft_y([(0, 0.14, 0.020, 0.021, 0.034), (0, 0.30, 0.008, 0.022, 0.040), (0, 0.46, -0.012, 0.024, 0.056), (0, 0.60, -0.030, 0.024, 0.070)], "wood", 12)  # culata
    m.box((0, 0.620, -0.030), (0.030, 0.018, 0.140), "dark", bevel=0.004)                                                              # tapa de goma
    m.box((0, 0.185, -0.030), (0.032, 0.060, 0.075), "wood", rot=(8, 0, 0), bevel=0.005)                                               # empunadura
    m.box((0, 0.115, -0.025), (0.008, 0.075, 0.007), "steel")                                                                          # guardamonte: base
    m.box((0, 0.080, -0.012), (0.008, 0.008, 0.028), "steel")                                                                          # guardamonte: frente
    m.box((0, 0.100, -0.008), (0.006, 0.008, 0.022), "dark")                                                                           # gatillo
    m.ellipsoid((0, -0.50, 0.003), (0.008, 0.008, 0.008), "brass", 8, 5)                                                               # anilla de correa
    m.ellipsoid((0, 0.50, -0.060), (0.008, 0.008, 0.008), "brass", 8, 5)
    return m.finish("Shotgun")


def make_ammo_box():
    m = Mesher("ammobox", {"cardboard": (0.60, 0.42, 0.18), "label": (0.85, 0.70, 0.10), "white": (0.90, 0.90, 0.85)})
    m.box((0, 0, 0), (0.105, 0.070, 0.045), "cardboard", bevel=0.002)
    m.box((0, 0, 0), (0.040, 0.072, 0.047), "label")                      # cinta
    m.box((0, 0, 0.0232), (0.060, 0.040, 0.002), "white")                 # etiqueta
    return m.finish("AmmoBox")


def make_shell_box():
    m = Mesher("shellbox", {"box": (0.55, 0.08, 0.06), "label": (0.90, 0.75, 0.15), "brass": (0.75, 0.60, 0.20), "white": (0.90, 0.90, 0.85)})
    m.box((0, 0, 0), (0.120, 0.075, 0.050), "box", bevel=0.002)
    m.box((0, 0, 0), (0.030, 0.077, 0.052), "label")
    m.box((0, 0, 0.0257), (0.070, 0.045, 0.002), "white")
    for sx in (-1, 1):                                                    # cartuchos asomando por la tapa
        m.loft_y([(0.028 * sx, -0.020, 0.0285, 0.009, 0.009), (0.028 * sx, 0.020, 0.0285, 0.009, 0.009)], "box", 8)
        m.loft_y([(0.028 * sx, 0.020, 0.0285, 0.0095, 0.0095), (0.028 * sx, 0.030, 0.0285, 0.0095, 0.0095)], "brass", 8)
    return m.finish("ShellBox")


def make_spray():
    m = Mesher("spray", {"can": (0.25, 0.60, 0.30), "cap": (0.90, 0.90, 0.88), "label": (0.85, 0.85, 0.80), "cross": (0.75, 0.10, 0.10), "nozzle": (0.15, 0.15, 0.15)})
    m.loft_z([(0, 0, -0.075, 0.030, 0.030), (0, 0, -0.068, 0.0315, 0.0315), (0, 0, 0.040, 0.0315, 0.0315), (0, 0, 0.058, 0.024, 0.024), (0, 0, 0.068, 0.013, 0.013)], "can", 14)
    m.loft_z([(0, 0, -0.030, 0.0322, 0.0322), (0, 0, 0.030, 0.0322, 0.0322)], "label", 14)
    m.loft_z([(0, 0, 0.066, 0.017, 0.017), (0, 0, 0.100, 0.017, 0.017)], "cap", 12)
    m.box((0, -0.012, 0.098), (0.010, 0.016, 0.010), "nozzle")
    m.box((0, -0.0328, 0.0), (0.024, 0.002, 0.007), "cross")
    m.box((0, -0.0328, 0.0), (0.007, 0.002, 0.024), "cross")
    return m.finish("Spray")


def make_key():
    m = Mesher("key", {"brass": (0.80, 0.62, 0.22), "tag": (0.10, 0.70, 0.80)})
    m.box((0, -0.010, 0), (0.008, 0.085, 0.005), "brass", bevel=0.001)         # vastago
    for k in range(10):                                                         # anilla
        a = 2 * math.pi * k / 10
        m.box((0.016 * math.cos(a), 0.052 + 0.016 * math.sin(a), 0), (0.007, 0.0095, 0.006), "brass", rot=(0, 0, math.degrees(a) + 90))
    for i, (y, w) in enumerate(((-0.044, 0.016), (-0.034, 0.011), (-0.024, 0.014))):   # dientes
        m.box((0.0105, y, 0), (w, 0.007, 0.005), "brass")
    m.box((0, 0.088, 0), (0.030, 0.022, 0.002), "tag")                          # etiqueta
    return m.finish("Key")


def build_items(out_dir):
    done = []
    for name, fn in (("Pistol", make_pistol), ("Shotgun", make_shotgun), ("AmmoBox", make_ammo_box),
                     ("ShellBox", make_shell_box), ("Spray", make_spray), ("Key", make_key)):
        _clear()
        obj = fn()
        _export(obj, f"{out_dir}\\{name}.fbx")
        done.append((name, len(obj.data.vertices), [mm.name for mm in obj.data.materials]))
    return done


# ------------------------------------------------------------------ atrezzo (origen = centro de la base)
def make_crate():
    m = Mesher("crate", {"wood": (0.45, 0.32, 0.18), "dark": (0.25, 0.17, 0.09), "iron": (0.20, 0.20, 0.22)})
    m.box((0, 0, 0.45), (0.90, 0.90, 0.84), "dark")                                  # nucleo
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((0.46 * sx, 0.46 * sy, 0.46), (0.09, 0.09, 0.92), "wood", bevel=0.006)   # montantes
    for z in (0.12, 0.46, 0.80):                                                     # travesanos
        for sy in (-1, 1):
            m.box((0, 0.47 * sy, z), (0.92, 0.035, 0.12), "wood", bevel=0.004)
        for sx in (-1, 1):
            m.box((0.47 * sx, 0, z), (0.035, 0.92, 0.12), "wood", bevel=0.004)
    for sy in (-1, 1):                                                               # diagonales
        m.box((0, 0.48 * sy, 0.46), (0.95, 0.03, 0.09), "wood", rot=(0, 38, 0))
    for sx in (-1, 1):
        m.box((0.48 * sx, 0, 0.46), (0.03, 0.95, 0.09), "wood", rot=(38, 0, 0))
    for sx in (-1, 1):                                                               # esquineros
        for sy in (-1, 1):
            m.box((0.47 * sx, 0.47 * sy, 0.92), (0.10, 0.10, 0.02), "iron")
    return m.finish("Crate")


def make_barrel():
    m = Mesher("barrel", {"body": (0.18, 0.30, 0.38), "rust": (0.38, 0.20, 0.10), "dark": (0.06, 0.06, 0.07)})
    prof = [(0.00, 0.26), (0.04, 0.285), (0.45, 0.31), (0.86, 0.285), (0.90, 0.26)]
    m.loft_z([(0, 0, z, r, r) for z, r in prof], "body", 16)
    for z in (0.20, 0.45, 0.70):                                                      # aros
        m.loft_z([(0, 0, z - 0.02, 0.318 if z == 0.45 else 0.300, 0.318 if z == 0.45 else 0.300),
                  (0, 0, z + 0.02, 0.318 if z == 0.45 else 0.300, 0.318 if z == 0.45 else 0.300)], "rust", 16)
    m.loft_z([(0, 0, 0.895, 0.245, 0.245), (0, 0, 0.905, 0.245, 0.245)], "dark", 16)   # tapa
    m.loft_z([(0.12, 0.05, 0.905, 0.035, 0.035), (0.12, 0.05, 0.925, 0.035, 0.035)], "rust", 10)  # tapon
    return m.finish("Barrel")


def make_locker():
    m = Mesher("locker", {"body": (0.32, 0.38, 0.36), "dark": (0.05, 0.05, 0.06), "handle": (0.55, 0.55, 0.58)})
    w, d, h = 0.90, 0.48, 1.85
    m.box((0, 0, 0.10 + (h - 0.10) / 2), (w, d, h - 0.10), "body", bevel=0.008)
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((0.4 * sx, 0.20 * sy, 0.05), (0.06, 0.06, 0.10), "dark")           # patas
    m.box((0, -d / 2 - 0.002, 0.95), (0.006, 0.006, 1.70), "dark")                    # union de las puertas
    for sx in (-1, 1):
        for i in range(4):                                                            # rejillas de ventilacion
            m.box((0.22 * sx, -d / 2 - 0.002, 1.62 - i * 0.045), (0.26, 0.006, 0.014), "dark")
        m.box((0.05 * sx, -d / 2 - 0.012, 1.0), (0.016, 0.022, 0.12), "handle", bevel=0.003)   # tirador
    m.box((0, -d / 2 - 0.002, 0.35), (0.80, 0.004, 0.006), "dark")
    return m.finish("Locker")


def make_filing_cabinet():
    m = Mesher("cabinet", {"body": (0.38, 0.40, 0.36), "dark": (0.05, 0.05, 0.06), "handle": (0.60, 0.60, 0.62), "paper": (0.80, 0.78, 0.68)})
    w, d, h = 0.46, 0.62, 1.32
    m.box((0, 0, h / 2), (w, d, h), "body", bevel=0.008)
    for i in range(3):
        z = 0.22 + i * 0.42
        m.box((0, -d / 2 - 0.004, z), (w - 0.04, 0.008, 0.36), "body", bevel=0.004)
        m.box((0, -d / 2 - 0.012, z), (0.20, 0.014, 0.03), "handle", bevel=0.003)
        m.box((0, -d / 2 - 0.009, z + 0.10), (0.12, 0.004, 0.05), "paper")             # portaetiquetas
        m.box((0, -d / 2 - 0.0005, z - 0.19), (w - 0.02, 0.003, 0.006), "dark")
    return m.finish("FilingCabinet")


def make_desk():
    m = Mesher("desk", {"wood": (0.28, 0.18, 0.10), "dark": (0.12, 0.08, 0.05), "handle": (0.55, 0.55, 0.55)})
    m.box((0, 0, 0.74), (1.50, 0.75, 0.045), "wood", bevel=0.006)                      # tablero
    m.box((-0.66, 0, 0.37), (0.04, 0.70, 0.70), "wood")                                # lateral izquierdo
    m.box((0.45, 0, 0.37), (0.58, 0.68, 0.70), "wood", bevel=0.004)                    # cajonera
    m.box((-0.0, 0.33, 0.45), (1.20, 0.03, 0.55), "dark")                              # panel trasero
    for i in range(3):
        z = 0.13 + i * 0.22
        m.box((0.45, -0.346, z + 0.045), (0.52, 0.012, 0.19), "dark", bevel=0.003)
        m.box((0.45, -0.360, z + 0.045), (0.12, 0.012, 0.02), "handle")
    return m.finish("Desk")


def make_chair():
    m = Mesher("chair", {"seat": (0.12, 0.10, 0.09), "metal": (0.22, 0.22, 0.24)})
    m.box((0, 0, 0.46), (0.44, 0.42, 0.06), "seat", bevel=0.01)
    m.box((0, 0.20, 0.78), (0.42, 0.05, 0.56), "seat", rot=(-6, 0, 0), bevel=0.01)
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((0.19 * sx, 0.18 * sy, 0.22), (0.035, 0.035, 0.44), "metal")
    return m.finish("Chair")


def make_shelf():
    m = Mesher("shelf", {"metal": (0.35, 0.36, 0.38), "board": (0.28, 0.20, 0.12), "cardboard": (0.55, 0.40, 0.20), "can": (0.5, 0.15, 0.1), "white": (0.8, 0.8, 0.75)})
    w, d, h = 1.20, 0.45, 1.90
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box(((w / 2 - 0.02) * sx, (d / 2 - 0.02) * sy, h / 2), (0.04, 0.04, h), "metal")
    for z in (0.12, 0.62, 1.10, 1.58, 1.88):
        m.box((0, 0, z), (w, d, 0.03), "board", bevel=0.003)
    for sx in (-1, 1):
        m.box(((w / 2 - 0.02) * sx, 0, 1.0), (0.02, d - 0.04, 0.02), "metal", rot=(0, 0, 0))
    m.box((-0.30, 0, 0.30), (0.40, 0.36, 0.26), "cardboard", bevel=0.004)                 # cajas y botes
    m.box((0.25, 0.02, 0.28), (0.34, 0.34, 0.22), "cardboard", rot=(0, 0, 8), bevel=0.004)
    m.box((-0.35, 0, 0.78), (0.30, 0.30, 0.30), "cardboard", bevel=0.004)
    for i in range(4):
        m.loft_z([(0.1 + i * 0.09, -0.05, 0.64, 0.035, 0.035), (0.1 + i * 0.09, -0.05, 0.80, 0.035, 0.035)], "can", 10)
    m.box((0.2, 0, 1.28), (0.5, 0.32, 0.30), "cardboard", bevel=0.004)
    m.box((-0.3, 0.02, 1.72), (0.40, 0.34, 0.26), "white", bevel=0.004)
    return m.finish("Shelf")


def make_cot():
    m = Mesher("cot", {"frame": (0.25, 0.26, 0.28), "mattress": (0.55, 0.55, 0.50), "blanket": (0.22, 0.28, 0.24), "pillow": (0.72, 0.72, 0.68)})
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.box((0.42 * sx, 0.92 * sy, 0.21), (0.04, 0.04, 0.42), "frame")
        m.box((0.42 * sx, 0, 0.40), (0.04, 1.90, 0.04), "frame")
    for sy in (-1, 1):
        m.box((0, 0.92 * sy, 0.40), (0.88, 0.04, 0.04), "frame")
    m.box((0, 0, 0.47), (0.84, 1.84, 0.12), "mattress", bevel=0.02)
    m.box((0, 0.35, 0.545), (0.86, 1.0, 0.04), "blanket", rot=(0, 0, 2), bevel=0.01)
    m.ellipsoid((0, -0.70, 0.56), (0.30, 0.18, 0.07), "pillow", 10, 6)
    return m.finish("Cot")


def make_rubble():
    m = Mesher("rubble", {"concrete": (0.40, 0.40, 0.38), "dark": (0.25, 0.25, 0.24), "rebar": (0.35, 0.18, 0.10)})
    for c, s, r in (((0, 0, 0.09), (0.45, 0.35, 0.18), (3, 9, 20)), ((0.35, 0.2, 0.07), (0.30, 0.25, 0.14), (-6, 4, 55)),
                    ((-0.3, 0.25, 0.06), (0.25, 0.22, 0.12), (8, -5, 100)), ((0.15, -0.3, 0.05), (0.22, 0.18, 0.10), (-4, 7, 140)),
                    ((-0.28, -0.22, 0.04), (0.16, 0.14, 0.08), (5, 3, 20)), ((0.55, -0.1, 0.04), (0.14, 0.12, 0.08), (2, -8, 70))):
        m.box(c, s, "concrete", rot=r, bevel=0.015)
    m.box((0.0, 0.0, 0.2), (0.015, 0.015, 0.30), "rebar", rot=(20, 10, 0))
    m.box((0.1, 0.05, 0.19), (0.012, 0.012, 0.26), "rebar", rot=(-25, 5, 0))
    return m.finish("Rubble")


def build_props(out_dir):
    done = []
    for name, fn in (("Crate", make_crate), ("Barrel", make_barrel), ("Locker", make_locker), ("FilingCabinet", make_filing_cabinet),
                     ("Desk", make_desk), ("Chair", make_chair), ("Shelf", make_shelf), ("Cot", make_cot), ("Rubble", make_rubble)):
        _clear()
        obj = fn()
        _export(obj, f"{out_dir}\\{name}.fbx")
        done.append((name, len(obj.data.vertices), [mm.name for mm in obj.data.materials]))
    return done
