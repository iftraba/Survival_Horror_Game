"""Lleva los brazos de un personaje (aunque esten doblados, levantados o apuntando al frente) a pose en T con los
codos rectos, sin esqueleto: el rig automatico de Mixamo necesita brazos rectos y separados del cuerpo.

Por cada lado:
 1. Hombro estimado a partir de la altura del personaje.
 2. Linea central del brazo: centroides de los puntos del brazo agrupados por distancia al hombro.
 3. Codo = el punto de esa linea mas alejado de la recta hombro-mano (si la desviacion es pequena, el brazo ya es recto).
 4. Se gira el brazo entero hasta la direccion objetivo (T inclinada 'angulo' grados) y el antebrazo hasta quedar alineado,
    con mezcla suave en hombro y codo para que la ropa no se parta.

Se puede importar (straighten(mesh, ...)) o ejecutar suelto:
    blender -b --python straighten_arms.py -- <fbx_entrada> <fbx_salida> [angulo_bajo_horizontal]
"""
import math
import sys

import bpy
from mathutils import Quaternion, Vector


def _smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def _seg_dist(p, a, d, length, lo=-0.02, hi=None):
    """Distancia de p al segmento a + d*s (s en [lo, hi]) y su parametro s sin recortar."""
    s = (p - a).dot(d)
    sc = max(lo, min(length if hi is None else hi, s))
    return (p - (a + d * sc)).length, s


def straighten(mesh, angle=20.0):
    mw = mesh.matrix_world
    verts = mesh.data.vertices
    co = [mw @ v.co for v in verts]
    zmin = min(p.z for p in co)
    H = max(p.z for p in co) - zmin
    k = H / 1.8
    shoulder_z = zmin + 0.815 * H
    near = [p.y for p in co if abs(p.x) < 0.1 * k and abs(p.z - shoulder_z) < 0.05 * k]
    torso_y = sum(near) / len(near) if near else 0.0

    report = []
    for side in (1, -1):
        S = Vector((side * 0.17 * k, torso_y, shoulder_z))
        pts = [p for p in co if p.x * side > 0.23 * k and zmin + 0.60 * H < p.z < zmin + 1.05 * H]
        if len(pts) < 200:
            report.append((side, "sin brazo"))
            continue
        tip = max(pts, key=lambda p: (p - S).length)
        # linea central: centroides por distancia al hombro
        bins = {}
        for p in pts:
            bins.setdefault(int((p - S).length / (0.06 * k)), []).append(p)
        cl = []
        for b in sorted(bins):
            if len(bins[b]) >= 12:
                c = sum(bins[b], Vector()) / len(bins[b])
                cl.append(c)
        if len(cl) < 3:
            report.append((side, "linea central insuficiente"))
            continue
        line = (tip - S).normalized()
        dev, E = 0.0, None
        for c in cl[:-1]:
            r = c - S
            d = (r - line * r.dot(line)).length
            if d > dev:
                dev, E = d, c
        straight = dev < 0.035 * k or E is None
        T = Vector((side * math.cos(math.radians(angle)), 0.0, -math.sin(math.radians(angle)))).normalized()
        if straight:
            d1 = line
            L1 = (tip - S).length
            E = tip
            d2, L2 = d1, 0.0
        else:
            d1 = (E - S).normalized()
            L1 = (E - S).length
            d2 = (tip - E).normalized()
            L2 = (tip - E).length
        Q1 = d1.rotation_difference(T)
        Epos = S + Q1 @ (E - S)
        Q2 = (Q1 @ d2).rotation_difference(T) if not straight else Quaternion()
        R = 0.19 * k
        moved = 0
        for i, p in enumerate(co):
            dist1, s1 = _seg_dist(p, S, d1, L1)
            if straight:
                dist, q = dist1, s1
            else:
                dist2, s2 = _seg_dist(p, E, d2, L2, lo=0.0, hi=L2 + 0.03)
                if dist1 <= dist2:
                    dist, q = dist1, s1
                else:
                    dist, q = dist2, L1 + s2
            if dist > R or q < -0.02:
                continue
            w1 = _smooth(q / (0.10 * k))
            w2 = 0.0 if straight else _smooth((q - (L1 - 0.05 * k)) / (0.10 * k))
            if w1 <= 0.0:
                continue
            np_ = S + Quaternion().slerp(Q1, w1) @ (p - S)
            if w2 > 0.0:
                np_ = Epos + Quaternion().slerp(Q2, w2) @ (np_ - Epos)
            co[i] = np_
            moved += 1
        report.append((side, f"codo {'recto' if straight else 'doblado ' + str(round(dev / k, 2)) + ' m'}, {moved} vertices"))

    inv = mw.inverted()
    for i, v in enumerate(verts):
        v.co = inv @ co[i]
    mesh.data.update()
    return report


if __name__ == "__main__" and "--" in sys.argv:
    args = sys.argv[sys.argv.index("--") + 1:]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=args[0])
    m = next(o for o in bpy.context.scene.objects if o.type == "MESH")
    for side, msg in straighten(m, float(args[2]) if len(args) > 2 else 20.0):
        print("BRAZO", side, msg, flush=True)
    bpy.ops.object.select_all(action="DESELECT")
    m.select_set(True)
    bpy.ops.export_scene.fbx(filepath=args[1], use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                             path_mode="COPY", embed_textures=True, bake_anim=False)
    print("PROCESO_TERMINADO", flush=True)
