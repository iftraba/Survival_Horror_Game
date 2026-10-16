"""Muebles AA (2026-10-15, fase C): taquilla de chapa, archivador de cajones, escritorio de acero con tablero laminado y silla giratoria.

    blender --background --python build_furniture_aa.py -- <carpeta ABSOLUTA de salida> [Locker FilingCabinet Desk SwivelChair]
    (en un Blender abierto: exec(open(ruta).read()); preview("Locker"); preview_all())

Mismas medidas, orientacion (frente -Y) y origen (centro de la base) que los anteriores, asi sustituyen a los FBX sin tocar la escena. Una submalla por
material AA_* (aa_lib); Unity les pone materiales reales con AAMaterialKit.
"""
import math
import os
import sys

HERE_AA = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else r"C:\Users\iftra\My project\Tools\blender"
exec(open(os.path.join(HERE_AA, "aa_lib.py"), encoding="utf-8").read())


def louvre(b, x, y_face, z, w, face=-1):
    """Rejilla estampada: capucha inclinada que sobresale de la chapa y ranura oscura debajo."""
    b.box((x, y_face + face * 0.001, z - 0.004), (w, 0.002, 0.008), "AA_Negro", 0.0)
    b.box((x, y_face + face * 0.004, z + 0.002), (w + 0.004, 0.006, 0.012), "AA_PinturaVerde", 0.0025, rot=(-35 * face, 0, 0))


def screw_y(b, x, y, z, face=-1, mat="AA_Acero", r=0.0035):
    b.cyl((x, y + face * 0.001, z), r, 0.002, "y", mat, 10, 0.0006)


# ------------------------------------------------------------------ taquilla
def make_locker():
    b = Builder()
    W, D, H = 0.90, 0.48, 1.88
    zb = 0.10
    pin = "AA_PinturaVerde"
    # cuerpo: laterales, trasera y techo de chapa con canto plegado (cajas de 15 mm con bisel redondo)
    for sx in (-1, 1):
        b.box((sx * (W / 2 - 0.0075), 0, zb + (H - zb - 0.03) / 2), (0.015, D, H - zb - 0.03), pin, 0.005, grain="z")
    b.box((0, D / 2 - 0.0075, zb + (H - zb - 0.03) / 2), (W, 0.015, H - zb - 0.03), pin, 0.004, grain="z")
    b.box((0, 0.0, H - 0.015), (W + 0.012, D + 0.012, 0.03), pin, 0.008)                                       # techo con vuelo
    b.box((0, 0.0, zb + 0.008), (W - 0.02, D - 0.02, 0.016), pin, 0.003)                                       # suelo
    b.box((0, -0.01, zb / 2), (W - 0.06, D - 0.08, zb), "AA_Negro", 0.006)                                     # zocalo retranqueado
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cyl((sx * (W / 2 - 0.05), sy * (D / 2 - 0.06), 0.012), 0.018, 0.024, "z", "AA_Negro", 16, 0.003)  # patas niveladoras
    # marco frontal y montante central
    fy = -D / 2
    for (x, w) in ((-W / 2 + 0.017, 0.034), (W / 2 - 0.017, 0.034), (0, 0.02)):
        b.box((x, fy + 0.006, zb + (H - zb - 0.03) / 2), (w, 0.012, H - zb - 0.03), pin, 0.003, grain="z")
    for z in (zb + 0.03, H - 0.045):
        b.box((0, fy + 0.006, z), (W, 0.012, 0.03), pin, 0.003)
    # puertas: hoja con cantos plegados, refuerzo interior (se ve por las rejillas), rejillas, tarjetero, numero, maneta con candado y bisagras
    dz0, dz1 = zb + 0.05, H - 0.065
    for sx in (-1, 1):
        x0 = 0.012 if sx > 0 else -W / 2 + 0.036
        x1 = W / 2 - 0.036 if sx > 0 else -0.012
        cx, dw = (x0 + x1) / 2, x1 - x0
        b.box((cx, fy - 0.004, (dz0 + dz1) / 2), (dw, 0.016, dz1 - dz0), pin, 0.004, grain="z")
        for i in range(7):
            louvre(b, cx, fy - 0.012, dz1 - 0.07 - i * 0.032, dw - 0.11)
        for i in range(4):
            louvre(b, cx, fy - 0.012, dz0 + 0.12 + i * 0.032, dw - 0.11)
        # tarjetero de chapa con la tarjeta de papel y el numero
        b.box((cx, fy - 0.0135, dz1 - 0.33), (0.09, 0.003, 0.05), "AA_Acero", 0.0012)
        b.box((cx, fy - 0.0150, dz1 - 0.33), (0.078, 0.001, 0.038), "AA_Papel", 0.0)
        b.box((cx, fy - 0.0135, dz1 - 0.27), (0.04, 0.002, 0.03), "AA_Laton", 0.001)
        # maneta de levantar empotrada (cazoleta cromada) en el canto de cierre, con la anilla del candado
        hx = x1 - 0.045 if sx < 0 else x0 + 0.045
        b.box((hx, fy - 0.013, 1.02), (0.05, 0.004, 0.16), "AA_Cromo", 0.002)
        b.box((hx, fy - 0.012, 1.0), (0.028, 0.002, 0.08), "AA_Negro", 0.0)
        b.box((hx, fy - 0.022, 1.03), (0.018, 0.016, 0.06), "AA_Cromo", 0.005)
        b.sweep([(hx - 0.012, fy - 0.016, 1.11), (hx - 0.012, fy - 0.034, 1.11), (hx + 0.012, fy - 0.034, 1.11), (hx + 0.012, fy - 0.016, 1.11)], 0.003, "AA_Cromo", 8)
        # bisagras de tres nudillos en el canto exterior
        ex = x0 - 0.004 if sx < 0 else x1 + 0.004
        for z in (dz0 + 0.12, (dz0 + dz1) / 2, dz1 - 0.12):
            b.cyl((ex, fy - 0.006, z), 0.006, 0.07, "z", "AA_Acero", 12, 0.0008)
        for z in (dz0 + 0.03, dz1 - 0.03):
            screw_y(b, cx - dw / 2 + 0.02, fy - 0.012, z)
            screw_y(b, cx + dw / 2 - 0.02, fy - 0.012, z)
    return b.finish("Locker")


# ------------------------------------------------------------------ archivador
def make_filing_cabinet():
    b = Builder()
    W, D, H = 0.46, 0.62, 1.32
    pin = "AA_PinturaGris"
    for sx in (-1, 1):
        b.box((sx * (W / 2 - 0.0075), 0, 0.04 + (H - 0.06) / 2), (0.015, D, H - 0.06), pin, 0.005, grain="z")
        for k in range(3):                                                                                   # nervios de refuerzo en los laterales
            b.box((sx * (W / 2 + 0.001), 0.02, 0.25 + k * 0.4), (0.004, D - 0.12, 0.02), pin, 0.0018)
    b.box((0, D / 2 - 0.0075, 0.04 + (H - 0.06) / 2), (W, 0.015, H - 0.06), pin, 0.004, grain="z")
    b.box((0, 0.004, H - 0.012), (W + 0.012, D + 0.008, 0.024), pin, 0.007)                                 # tapa
    b.box((0, -0.01, 0.02), (W - 0.03, D - 0.06, 0.04), "AA_Negro", 0.006)                                  # zocalo
    fy = -D / 2
    for i in range(3):
        z0, z1 = 0.05 + i * 0.415, 0.05 + i * 0.415 + 0.4
        cz = (z0 + z1) / 2
        b.box((0, fy - 0.003, cz), (W - 0.012, 0.018, z1 - z0 - 0.008), pin, 0.005, grain="z")              # frente del cajon (canto plegado)
        b.frame_profile((-W / 2 + 0.05, W / 2 - 0.05, z0 + 0.05, z1 - 0.05), [(0.0, 0.0), (0.006, 0.003), (0.012, 0.0)], fy - 0.012, -1, pin)   # embuticion decorativa
        # tirador de cazoleta cromado con tarjetero encima
        b.box((0, fy - 0.014, cz + 0.07), (0.17, 0.006, 0.05), "AA_Cromo", 0.0025)
        b.box((0, fy - 0.017, cz + 0.06), (0.15, 0.006, 0.012), "AA_Negro", 0.0)
        b.box((0, fy - 0.022, cz + 0.083), (0.16, 0.012, 0.01), "AA_Cromo", 0.004)
        b.box((0, fy - 0.0135, cz + 0.14), (0.11, 0.003, 0.045), "AA_Cromo", 0.0012)
        b.box((0, fy - 0.0152, cz + 0.14), (0.095, 0.001, 0.032), "AA_Papel", 0.0)
        b.box((0, fy - 0.004, z1 + 0.004), (W - 0.02, 0.006, 0.008), "AA_Negro", 0.0)                       # sombra entre cajones
    b.cyl((W / 2 - 0.05, fy - 0.012, H - 0.06), 0.012, 0.016, "y", "AA_Cromo", 20, 0.002)                   # cerradura de pulsador
    b.box((W / 2 - 0.05, fy - 0.021, H - 0.06), (0.003, 0.002, 0.012), "AA_Negro", 0.0)
    return b.finish("FilingCabinet")


# ------------------------------------------------------------------ escritorio
def make_desk():
    """Escritorio de acero de oficina de los 70 con tablero laminado y canto de aluminio: cajonera de tres cajones a la derecha, panel a la izquierda."""
    b = Builder()
    W, D, H = 1.50, 0.75, 0.77
    pin = "AA_PinturaGris"
    b.box((0, 0, H - 0.016), (W, D, 0.032), "AA_Laminado", 0.002, grain="x")                                   # tablero laminado
    b.box((0, 0, H - 0.036), (W - 0.01, D - 0.01, 0.012), pin, 0.002)
    for sy in (-1, 1):
        b.box((0, sy * (D / 2 + 0.002), H - 0.016), (W + 0.004, 0.006, 0.034), "AA_Cromo", 0.002)            # canto de aluminio
    for sx in (-1, 1):
        b.box((sx * (W / 2 + 0.002), 0, H - 0.016), (0.006, D + 0.004, 0.034), "AA_Cromo", 0.002)
    # pedestal derecho
    px, pw = W / 2 - 0.22, 0.42
    for sx in (-1, 1):
        b.box((px + sx * (pw / 2 - 0.0075), 0, (H - 0.04) / 2 + 0.03), (0.015, D - 0.04, H - 0.07), pin, 0.004, grain="z")
    b.box((px, D / 2 - 0.03, (H - 0.04) / 2 + 0.03), (pw, 0.015, H - 0.07), pin, 0.004, grain="z")
    b.box((px, -0.01, 0.025), (pw - 0.02, D - 0.08, 0.05), "AA_Negro", 0.006)                                  # zocalo
    fy = -D / 2 + 0.02
    rows = [(0.06, 0.32), (0.33, 0.52), (0.53, 0.70)]
    for (z0, z1) in rows:
        cz = (z0 + z1) / 2
        b.box((px, fy - 0.004, cz), (pw - 0.012, 0.018, z1 - z0 - 0.008), pin, 0.004, grain="x")
        b.box((px, fy - 0.0135, cz + (z1 - z0) * 0.22), (0.2, 0.006, 0.03), "AA_Cromo", 0.0025)              # tirador de barra empotrado
        b.box((px, fy - 0.016, cz + (z1 - z0) * 0.22 - 0.008), (0.17, 0.004, 0.008), "AA_Negro", 0.0)
        if z1 - z0 > 0.2:
            b.box((px, fy - 0.0135, cz - 0.04), (0.09, 0.003, 0.04), "AA_Cromo", 0.0012)                       # tarjetero del cajon de archivo
            b.box((px, fy - 0.015, cz - 0.04), (0.078, 0.001, 0.028), "AA_Papel", 0.0)
    b.cyl((px + pw / 2 - 0.04, fy - 0.012, H - 0.07), 0.01, 0.014, "y", "AA_Cromo", 18, 0.0015)               # cerradura
    # pata panel izquierda y faldon
    lx = -W / 2 + 0.05
    b.box((lx, 0, (H - 0.04) / 2 + 0.01), (0.03, D - 0.06, H - 0.05), pin, 0.006, grain="z")
    b.box((lx, 0, 0.02), (0.06, D - 0.04, 0.04), "AA_Negro", 0.006)
    b.box(((lx + px - pw / 2) / 2, D / 2 - 0.05, H - 0.2), (px - pw / 2 - lx, 0.012, 0.3), pin, 0.003)        # faldon de modestia
    b.box(((lx + px - pw / 2) / 2, fy + 0.02, H - 0.085), (px - pw / 2 - lx - 0.02, 0.015, 0.07), pin, 0.003)  # cajon central plano
    b.box(((lx + px - pw / 2) / 2, fy + 0.008, H - 0.09), (0.18, 0.006, 0.022), "AA_Cromo", 0.002)
    return b.finish("Desk")


# ------------------------------------------------------------------ silla giratoria
def make_swivel_chair():
    b = Builder()
    for k in range(5):
        a = 2 * math.pi * k / 5 + math.pi / 2
        ca, sa = math.cos(a), math.sin(a)
        b.sweep([(0.04 * ca, 0.04 * sa, 0.105), (0.16 * ca, 0.16 * sa, 0.09), (0.28 * ca, 0.28 * sa, 0.075), (0.3 * ca, 0.3 * sa, 0.07)], 0.024, "AA_Plastico", 10, 0.015)
        wx, wy = 0.3 * ca, 0.3 * sa
        b.cyl((wx, wy, 0.07), 0.012, 0.03, "z", "AA_Cromo", 12, 0.001)                                          # vastago
        for off in (-0.013, 0.013):                                                                          # rueda doble
            b.cyl((wx - sa * off, wy + ca * off, 0.031), 0.031, 0.013, "x", "AA_Goma", 20, 0.003)
        b.box((wx, wy, 0.055), (0.035, 0.03, 0.028), "AA_Plastico", 0.01)
    b.cyl((0, 0, 0.11), 0.05, 0.03, "z", "AA_Plastico", 24, 0.006)                                           # buje de la estrella
    b.cyl((0, 0, 0.25), 0.026, 0.26, "z", "AA_Cromo", 20, 0.0)                                              # piston
    for k in range(7):                                                                                       # fuelle
        b.cyl((0, 0, 0.13 + k * 0.016), 0.04 if k % 2 == 0 else 0.034, 0.016, "z", "AA_Plastico", 20, 0.0)
    b.box((0, 0.02, 0.405), (0.3, 0.26, 0.05), "AA_Plastico", 0.012)                                         # mecanismo
    b.box((0.17, 0.0, 0.4), (0.09, 0.016, 0.016), "AA_Plastico", 0.006, rot=(0, 0, 10))                       # palanca
    b.box((0, 0.0, 0.44), (0.48, 0.46, 0.02), "AA_Plastico", 0.008)                                          # base del asiento
    b.cushion((0, -0.01, 0.48), (0.5, 0.48, 0.07), "AA_Tela", 0.025, round_edge=0.025)                       # asiento
    # respaldo: soporte en J, carcasa y cojin con lumbar
    b.sweep([(0, 0.17, 0.42), (0, 0.25, 0.5), (0, 0.27, 0.62)], 0.022, "AA_Plastico", 12)
    b.box((0, 0.29, 0.83), (0.44, 0.03, 0.46), "AA_Plastico", 0.012, rot=(-6, 0, 0))
    b.cushion((0, 0.265, 0.84), (0.42, 0.05, 0.44), "AA_Tela", 0.02, round_edge=0.02)
    b.cushion((0, 0.25, 0.6), (0.38, 0.05, 0.12), "AA_Tela", 0.012, round_edge=0.015)
    for sx in (-1, 1):                                                                                       # reposabrazos en T
        x = sx * 0.27
        b.sweep([(x * 0.7, 0.04, 0.42), (x, 0.04, 0.44), (x, 0.03, 0.6)], 0.014, "AA_Plastico", 10)
        b.box((x, -0.01, 0.62), (0.07, 0.26, 0.032), "AA_Plastico", 0.014)
    return b.finish("SwivelChair")


# ------------------------------------------------------------------ mostrador del vestibulo
def _fc(face, p, u, n, z):
    """Punto en una cara: 'y-' (frente, plano y=p), 'x+' / 'x-' (laterales, plano x=p). u corre a lo largo de la cara, n sale de ella."""
    if face == "y-":
        return (u, p - n, z)
    return (p + n, u, z) if face == "x+" else (p - n, u, z)


def _sz(face, su, sn, sz):
    return (su, sn, sz) if face == "y-" else (sn, su, sz)


def _bay(b, face, p, u0, u1, z0, z1):
    """Plafon de madera en relieve con junquillo alrededor (el bastidor va aparte)."""
    w, h, uc, zc = u1 - u0, z1 - z0, (u0 + u1) / 2, (z0 + z1) / 2
    gz = "z"
    b.box(_fc(face, p, uc, 0.004, zc), _sz(face, w, 0.008, h), "AA_Madera", 0.002, seg=2, grain=gz)
    b.box(_fc(face, p, uc, 0.010, zc), _sz(face, w - 0.08, 0.008, h - 0.08), "AA_Madera", 0.0035, seg=2, grain=gz)
    b.box(_fc(face, p, uc, 0.0135, zc), _sz(face, w - 0.13, 0.006, h - 0.13), "AA_Madera", 0.0027, seg=2, grain=gz)
    t = 0.022
    for z in (z0 + t / 2 - 0.004, z1 - t / 2 + 0.004):
        b.box(_fc(face, p, uc, 0.012, z), _sz(face, w + 0.008, 0.016, t), "AA_Madera", 0.0065, seg=2, grain="x" if face == "y-" else "z")
    for u in (u0 + t / 2 - 0.004, u1 - t / 2 + 0.004):
        b.box(_fc(face, p, u, 0.012, zc), _sz(face, t, 0.016, h - 0.03), "AA_Madera", 0.0065, seg=2, grain=gz)


def _panelled(b, face, p, u0, u1, bays, z_rail0=0.12, z_open0=0.26, z_open1=0.90, z_rail1=1.06, stile=0.08):
    """Bastidor (largueros y travesanos de 20 mm) con 'bays' plafones."""
    length = u1 - u0
    for (za, zb) in ((z_rail0, z_open0), (z_open1, z_rail1)):
        b.box(_fc(face, p, (u0 + u1) / 2, 0.01, (za + zb) / 2), _sz(face, length, 0.02, zb - za), "AA_Madera", 0.003, seg=2)
    step = length / bays
    for i in range(bays + 1):
        u = u0 + i * step
        w = stile * (0.75 if i in (0, bays) else 1.0)
        uc = u + (w / 2 if i == 0 else (-w / 2 if i == bays else 0))
        b.box(_fc(face, p, uc, 0.01, (z_open0 + z_open1) / 2), _sz(face, w, 0.02, z_open1 - z_open0), "AA_Madera", 0.003, seg=2, grain="z")
    for i in range(bays):
        a = u0 + i * step + (stile * 0.75 if i == 0 else stile / 2)
        c = u0 + (i + 1) * step - (stile * 0.75 if i == bays - 1 else stile / 2)
        _bay(b, face, p, a, c, z_open0, z_open1)


def _monitor(b, x, y, z):
    """Monitor plano de los 2000 mirando a +Y (al funcionario)."""
    b.box((x, y, z + 0.008), (0.24, 0.18, 0.016), "AA_Plastico", 0.006)
    b.box((x, y - 0.03, z + 0.11), (0.06, 0.03, 0.2), "AA_Plastico", 0.008, rot=(8, 0, 0))
    b.box((x, y, z + 0.33), (0.52, 0.055, 0.35), "AA_Plastico", 0.012)
    b.box((x, y - 0.045, z + 0.33), (0.3, 0.04, 0.22), "AA_Plastico", 0.015)                                 # joroba trasera
    b.box((x, y + 0.0285, z + 0.345), (0.47, 0.004, 0.29), "AA_Pantalla", 0.0)
    b.box((x + 0.21, y + 0.029, z + 0.172), (0.012, 0.004, 0.006), "AA_Cromo", 0.0)                         # piloto
    for i in range(3):
        b.box((x - 0.18 + i * 0.03, y + 0.0285, z + 0.172), (0.016, 0.003, 0.006), "AA_Negro", 0.0)          # botones


def _keyboard(b, x, y, z):
    b.box((x, y, z + 0.013), (0.45, 0.165, 0.026), "AA_Plastico", 0.006, rot=(-3, 0, 0))
    for r in range(5):
        n = 14 if r < 4 else 7
        for k in range(n):
            w = 0.022 if r < 4 else (0.13 if k == 3 else 0.022)
            kx = x - 0.19 + k * 0.0275 + (0.0 if r < 4 else (0.05 if k > 3 else 0.0))
            if r == 4 and k == 3:
                kx = x - 0.19 + 3 * 0.0275 + 0.054
            b.box((kx, y + 0.06 - r * 0.0285, z + 0.03 + r * 0.0014), (w, 0.022, 0.009), "AA_Negro", 0.0)
    b.box((x + 0.32, y + 0.01, z + 0.012), (0.06, 0.1, 0.024), "AA_Plastico", 0.01)                         # raton


def _rotary_phone(b, x, y, z, yaw=0.0):
    rot = (0, 0, yaw)
    b.box((x, y, z + 0.035), (0.21, 0.24, 0.07), "AA_Negro", 0.025, seg=4, rot=rot)
    b.box((x, y + 0.01, z + 0.075), (0.15, 0.13, 0.03), "AA_Negro", 0.012, seg=3, rot=rot)
    b.cyl((x, y - 0.06, z + 0.072), 0.05, 0.012, "z", "AA_Cromo", 28, 0.002)                                # dial
    b.cyl((x, y - 0.06, z + 0.08), 0.018, 0.006, "z", "AA_Papel", 18, 0.001)
    hy = y + 0.03
    b.sweep([(x - 0.11, hy, z + 0.105), (x - 0.06, hy, z + 0.118), (x + 0.06, hy, z + 0.118), (x + 0.11, hy, z + 0.105)], 0.016, "AA_Negro", 12)
    for sx in (-1, 1):
        b.cyl((x + sx * 0.11, hy, z + 0.098), 0.03, 0.035, "z", "AA_Negro", 20, 0.008)
    b.sweep([(x + 0.105, y + 0.12, z + 0.02), (x + 0.16, y + 0.2, z + 0.006), (x + 0.12, y + 0.3, z + 0.004), (x + 0.0, y + 0.34, z + 0.004)], 0.004, "AA_Negro", 6)


def make_reception_desk():
    """Mostrador del vestibulo en L (frente de 4,2 m, retorno derecho de 2,2 m, encimera a 1,10 m) con el puesto del funcionario detras."""
    b = Builder()
    L, H = 4.2, 1.10
    # carcasas (tablero de 40 mm) y zocalos retranqueados
    b.box((0, 0.0, 0.12 + (H - 0.12) / 2), (L, 0.04, H - 0.12), "AA_Madera", 0.003, grain="z")
    b.box((0, 0.03, 0.06), (L - 0.02, 0.06, 0.12), "AA_Negro", 0.006)
    b.box((L / 2 - 0.02, 1.1, 0.12 + (H - 0.12) / 2), (0.04, 2.2, H - 0.12), "AA_Madera", 0.003, grain="z")
    b.box((L / 2 - 0.04, 1.1, 0.06), (0.06, 2.18, 0.12), "AA_Negro", 0.006)
    b.box((-L / 2 + 0.02, 0.4, 0.12 + (H - 0.12) / 2), (0.04, 0.8, H - 0.12), "AA_Madera", 0.003, grain="z")
    b.box((-L / 2 + 0.04, 0.4, 0.06), (0.06, 0.78, 0.12), "AA_Negro", 0.006)
    # frente y laterales con plafones en relieve
    _panelled(b, "y-", -0.02, -L / 2, L / 2, 6)
    _panelled(b, "x+", L / 2, 0.0, 2.2, 3)
    _panelled(b, "x-", -L / 2, 0.0, 0.8, 1)
    # encimeras: laminado con canto de madera en media cana (el del frente hace de reborde)
    b.box((0, 0.32, H + 0.0175), (L + 0.1, 0.70, 0.035), "AA_Laminado", 0.002, grain="x")
    b.box((0, -0.055, H + 0.005), (L + 0.14, 0.07, 0.075), "AA_Madera", 0.03, seg=5, grain="x")
    b.box((L / 2 + 0.23, 1.12, H + 0.0175), (0.56, 2.24, 0.035), "AA_Laminado", 0.002, grain="z")
    b.box((L / 2 + 0.515, 1.11, H + 0.005), (0.07, 2.25, 0.075), "AA_Madera", 0.03, seg=5, grain="z")
    b.box((-L / 2 - 0.0, 0.4, H + 0.0175), (0.1, 0.8, 0.035), "AA_Laminado", 0.002, grain="z")
    b.box((-L / 2 - 0.02, 0.4, H + 0.005), (0.07, 0.86, 0.075), "AA_Madera", 0.03, seg=5, grain="z")
    for y in (0.35, 1.1, 1.85):                                                                              # mensulas del vuelo del retorno
        b.box((L / 2 + 0.08, y, H - 0.09), (0.16, 0.05, 0.15), "AA_Madera", 0.012, grain="z")
        b.box((L / 2 + 0.2, y, H - 0.03), (0.12, 0.05, 0.05), "AA_Madera", 0.012)
    # reposapies de laton con brazos curvos y rosetas
    ry, rz = -0.14, 0.17
    b.sweep([(-L / 2 + 0.12, ry, rz), (L / 2 - 0.12, ry, rz)], 0.019, "AA_Laton", 16)
    for x in (-L / 2 + 0.12, L / 2 - 0.12):
        b.cyl((x, ry, rz), 0.022, 0.02, "x", "AA_Laton", 16, 0.006)
    for i in range(6):
        x = -L / 2 + 0.3 + i * (L - 0.6) / 5
        b.cyl((x, -0.027, 0.25), 0.032, 0.012, "y", "AA_Laton", 18, 0.003)
        b.sweep([(x, -0.03, 0.25), (x, -0.08, 0.245), (x, -0.115, 0.21), (x, ry, rz + 0.02)], 0.009, "AA_Laton", 10)
        b.cyl((x, ry, rz), 0.025, 0.035, "x", "AA_Laton", 18, 0.004)
    # puesto del funcionario: mesa de trabajo sobre dos cajoneras (los cajones miran a +Y)
    b.box((0, 0.65, 0.76), (L - 0.1, 0.7, 0.032), "AA_Laminado", 0.003, grain="x")
    for px in (-1.55, 0.25):
        b.box((px, 0.62, 0.37), (0.42, 0.6, 0.72), "AA_PinturaGris", 0.006, grain="z")
        for k, (z0, z1) in enumerate(((0.06, 0.34), (0.36, 0.53), (0.55, 0.72))):
            cz = (z0 + z1) / 2
            b.box((px, 0.93, cz), (0.4, 0.018, z1 - z0 - 0.008), "AA_PinturaGris", 0.004)
            b.box((px, 0.942, cz + (z1 - z0) * 0.25), (0.18, 0.006, 0.026), "AA_Cromo", 0.0025)
            b.box((px, 0.944, cz + (z1 - z0) * 0.25 - 0.007), (0.15, 0.004, 0.007), "AA_Negro", 0.0)
    for x in (-1.2, 0.6):
        _monitor(b, x, 0.74, 0.776)
        _keyboard(b, x, 0.9, 0.776)
    for k, x in enumerate([1.25 + i * 0.055 for i in range(9)]):                                              # archivadores de anillas
        mat = ("AA_PinturaVerde", "AA_Negro", "AA_Plastico", "AA_PinturaCrema")[k % 4]
        b.box((x, 0.8, 0.776 + 0.145), (0.05, 0.28, 0.29), mat, 0.004, grain="z")
        b.cyl((x, 0.659, 0.776 + 0.19), 0.012, 0.004, "y", "AA_Cromo", 12, 0.0)
    for x in (1.2, 1.76):
        b.box((x, 0.8, 0.776 + 0.12), (0.012, 0.22, 0.24), "AA_Acero", 0.003)                                # sujetalibros
    for k in range(2):                                                                                      # bandejas de papel apiladas
        z = 0.776 + 0.005 + k * 0.07
        b.box((-0.35, 0.55, z + 0.03), (0.27, 0.35, 0.004), "AA_Acero", 0.001)
        for sx in (-1, 1):
            b.box((-0.35 + sx * 0.135, 0.55, z + 0.03), (0.004, 0.35, 0.06), "AA_Acero", 0.001)
        b.box((-0.35, 0.55, z + 0.04 + k * 0.004), (0.21, 0.297, 0.012 + 0.008 * k), "AA_Papel", 0.001)
    for (x, y, r) in ((-0.75, 0.62, 8), (1.0, 0.7, -12), (-1.75, 0.66, 4)):
        b.box((x, y, 0.776 + 0.003), (0.21, 0.297, 0.003), "AA_Papel", 0.0, rot=(0, 0, r))
    # sobre el mostrador: telefono de disco, timbre, libro de registro con boligrafo de cadena
    _rotary_phone(b, -1.6, 0.3, H + 0.035, 0)
    b.cyl((1.8, 0.25, H + 0.045), 0.045, 0.02, "z", "AA_Negro", 24, 0.004)                                   # timbre
    b.cyl((1.8, 0.25, H + 0.07), 0.04, 0.03, "z", "AA_Cromo", 24, 0.004, r_top=0.014)
    b.cyl((1.8, 0.25, H + 0.09), 0.005, 0.014, "z", "AA_Cromo", 10, 0.0)
    b.cyl((1.8, 0.25, H + 0.098), 0.009, 0.005, "z", "AA_Cromo", 12, 0.001)
    b.box((0.3, 0.12, H + 0.04), (0.42, 0.3, 0.012), "AA_PinturaVerde", 0.003)                               # libro de registro abierto
    for sx in (-1, 1):
        b.box((0.3 + sx * 0.103, 0.12, H + 0.048), (0.195, 0.28, 0.006), "AA_Papel", 0.001)
    b.sweep([(0.6, 0.1, H + 0.04), (0.64, 0.08, H + 0.045), (0.66, 0.02, H + 0.042)], 0.004, "AA_Negro", 8)   # boligrafo
    b.cyl((0.6, 0.18, H + 0.04), 0.014, 0.01, "z", "AA_Cromo", 14, 0.002)
    b.sweep([(0.6, 0.18, H + 0.045), (0.62, 0.14, H + 0.04), (0.6, 0.1, H + 0.042)], 0.0012, "AA_Cromo", 5)  # cadena
    return b.finish("ReceptionDesk")


BUILDERS = {"Locker": make_locker, "FilingCabinet": make_filing_cabinet, "Desk": make_desk, "SwivelChair": make_swivel_chair,
            "ReceptionDesk": make_reception_desk}


def preview(name):
    aa_clear()
    o = BUILDERS[name]()
    aa_preview_materials()
    d = o.dimensions
    aa_view(80, -25, max(d.x, d.y, d.z) * 1.9 + 0.3, (0, 0, d.z * 0.5))
    print(name, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "tris", sum(len(p.vertices) - 2 for p in o.data.polygons))


def preview_all():
    aa_clear()
    x = 0.0
    for n, fn in BUILDERS.items():
        o = fn()
        d = o.dimensions
        o.location.x = x + d.x / 2
        x += d.x + 0.4
        print(n, "%.2f x %.2f x %.2f" % (d.x, d.y, d.z), "tris", sum(len(p.vertices) - 2 for p in o.data.polygons))
    aa_preview_materials()
    aa_view(80, -20, x * 0.95, (x / 2, 0, 0.8))


if __name__ == "__main__" and "--" in sys.argv:
    out = sys.argv[sys.argv.index("--") + 1]
    only = sys.argv[sys.argv.index("--") + 2:]
    os.makedirs(out, exist_ok=True)
    for n, fn in BUILDERS.items():
        if only and n not in only:
            continue
        aa_clear()
        o = fn()
        d = o.dimensions
        aa_export(o, os.path.join(out, n + ".fbx"))
        print("hecho", n, "%.3f x %.3f x %.3f" % (d.x, d.y, d.z), "tris", sum(len(p.vertices) - 2 for p in o.data.polygons), [m.name for m in o.data.materials], flush=True)
    print("PROCESO_TERMINADO")
