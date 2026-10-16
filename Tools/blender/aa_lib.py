"""Biblioteca comun de los modelos AA (2026-10-15, fase C): piezas con cantos biselados reales, molduras con inglete por anillos de perfil, tubos,
cilindros, cojines, y montaje final con una submalla por material, UV en metros (proyeccion en caja, con la veta o el cepillado a lo largo de cada pieza),
normales suaves por angulo y normales ponderadas. Los materiales se llaman AA_* y Unity les pone materiales reales (AAMaterialKit) en vez de hornear.

Uso: exec(open(ruta_de_aa_lib).read()) dentro del constructor.
Ejes de Blender: X ancho, Y fondo (frente = -Y, que es +Z en Unity), Z alto; origen en el centro de la base.
"""
import math
import os

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

AA_COLORS = {
    "AA_Madera": (0.2, 0.11, 0.06), "AA_Laton": (0.6, 0.45, 0.18), "AA_Acero": (0.6, 0.62, 0.64), "AA_Negro": (0.03, 0.03, 0.035),
    "AA_PinturaVerde": (0.3, 0.37, 0.34), "AA_PinturaGris": (0.36, 0.38, 0.36), "AA_PinturaCrema": (0.62, 0.6, 0.52), "AA_Papel": (0.82, 0.8, 0.7),
    "AA_Tela": (0.1, 0.11, 0.13), "AA_Plastico": (0.05, 0.05, 0.055), "AA_Goma": (0.02, 0.02, 0.022), "AA_Laminado": (0.32, 0.3, 0.26),
    "AA_Cromo": (0.8, 0.8, 0.82), "AA_Pantalla": (0.02, 0.025, 0.03),
}


class Part:
    def __init__(self, bm, mat, grain="x"):
        self.bm, self.mat, self.grain = bm, mat, grain


class Builder:
    def __init__(self):
        self.parts = []

    def box(self, c, s, mat, bevel=0.003, seg=3, grain="x", rot=None):
        bm = bmesh.new()
        r = bmesh.ops.create_cube(bm, size=1.0)
        for v in r["verts"]:
            v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
        if bevel > 0:
            bev = min(bevel, min(s) * 0.45)
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bev, offset_type="OFFSET", segments=seg, profile=0.5, affect="EDGES", clamp_overlap=True)
        if rot is not None:
            m = rot if isinstance(rot, Matrix) else Euler([math.radians(a) for a in rot]).to_matrix()
            bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector((0, 0, 0)), matrix=m)
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
        self.parts.append(Part(bm, mat, grain))
        return bm

    def cyl(self, c, r, depth, axis, mat, segs=24, bevel=0.0015, r_top=None):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r, radius2=r if r_top is None else r_top, depth=depth)
        if bevel > 0:
            caps = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-6 and abs(abs(e.verts[0].co.z) - depth / 2) < 1e-5]
            bmesh.ops.bevel(bm, geom=caps, offset=min(bevel, r * 0.4, depth * 0.4), offset_type="OFFSET", segments=2, profile=0.5, affect="EDGES", clamp_overlap=True)
        if axis == "x":
            bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(90), 3, "Y"))
        elif axis == "y":
            bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(90), 3, "X"))
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
        self.parts.append(Part(bm, mat))
        return bm

    def frame_profile(self, rect, profile, y0, face, mat, cap=False, grain="x"):
        """Moldura alrededor de un hueco (x0, x1, z0, z1) en una cara que mira a Y (face +1 / -1). profile [(u, h)]: u desde el borde, h hacia fuera."""
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
                bm.faces.new((a[k], a[k2], b[k2], b[k]) if face > 0 else (a[k], b[k], b[k2], a[k2]))
        if cap:
            last = rings[-1]
            bm.faces.new(last if face > 0 else list(reversed(last)))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        self.parts.append(Part(bm, mat, grain))
        return bm

    def sweep(self, pts, r, mat, segs=12, r_end=None):
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

    def cushion(self, c, s, mat, bulge=0.015, nx=10, ny=10, round_edge=0.02, grain="x"):
        """Cojin: caja con la cara de arriba abombada y los cantos redondeados (rejilla subdividida con el centro levantado)."""
        bm = bmesh.new()
        r = bmesh.ops.create_cube(bm, size=1.0)
        for v in r["verts"]:
            v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=min(round_edge, min(s) * 0.45), offset_type="OFFSET", segments=3, profile=0.5, affect="EDGES", clamp_overlap=True)
        bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if e.calc_length() > 0.05], cuts=4, use_grid_fill=True)
        for v in bm.verts:
            if v.co.z > s[2] * 0.2:
                u, w = v.co.x / (s[0] / 2), v.co.y / (s[1] / 2)
                k = max(0.0, 1.0 - max(abs(u), abs(w)) ** 4)
                v.co.z += bulge * k
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
        self.parts.append(Part(bm, mat, grain))
        return bm

    def finish(self, name, smooth_angle=40):
        mats = []
        for p in self.parts:
            if p.mat not in mats:
                mats.append(p.mat)
        bm = bmesh.new()
        for part in self.parts:
            mi = mats.index(part.mat)
            src = part.bm
            src_uv = src.loops.layers.uv.verify()
            src.normal_update()
            for f in src.faces:
                n = f.normal
                ax = max(range(3), key=lambda i: abs(n[i]))
                for loop in f.loops:
                    p = loop.vert.co
                    if ax == 1:
                        uvp = (p.x, p.z) if part.grain == "x" else (p.z, p.x)
                    elif ax == 0:
                        uvp = (p.z, p.y) if part.grain == "z" else (p.y, p.z)
                    else:
                        uvp = (p.x, p.y) if part.grain == "x" else (p.y, p.x)
                    loop[src_uv].uv = uvp
            tmp = bpy.data.meshes.new("tmp")
            src.to_mesh(tmp)
            nf = len(bm.faces)
            bm.from_mesh(tmp)
            bm.faces.ensure_lookup_table()
            for i in range(nf, len(bm.faces)):
                bm.faces[i].material_index = mi
            bpy.data.meshes.remove(tmp)
            src.free()
        mesh = bpy.data.meshes.new(name + "Mesh")
        for m in mats:
            mat = bpy.data.materials.get(m) or bpy.data.materials.new(m)
            mat.diffuse_color = (*AA_COLORS.get(m, (0.5, 0.5, 0.5)), 1.0)
            mesh.materials.append(mat)
        bm.to_mesh(mesh)
        bm.free()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(smooth_angle))
        wn = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
        wn.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier="wn")
        return obj


def aa_clear():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials):
        for it in list(coll):
            coll.remove(it)


def aa_export(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             mesh_smooth_type="FACE", path_mode="STRIP", bake_anim=False)


def aa_view(rx, rz, dist, loc):
    for area in bpy.context.window.screen.areas:
        if area.type == "VIEW_3D":
            area.spaces[0].shading.type = "MATERIAL"
            r3d = area.spaces[0].region_3d
            r3d.view_rotation = Euler((math.radians(rx), 0, math.radians(rz))).to_quaternion()
            r3d.view_distance = dist
            r3d.view_location = loc


def aa_preview_materials():
    for m in bpy.data.materials:
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        if b:
            b.inputs["Base Color"].default_value = (*m.diffuse_color[:3], 1)
            b.inputs["Metallic"].default_value = 0.9 if m.name in ("AA_Laton", "AA_Acero", "AA_Cromo") else 0.0
            b.inputs["Roughness"].default_value = 0.35
