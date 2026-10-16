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


# ------------------------------------------------------------------ escalera de caracol del vestibulo
# Misma helice que ComisariaGrande.SpiralStair (la rampa de colision se queda alli): 22 peldanos, una vuelta, radio 2,2, subida 4 m.
# Angulo en grados de Unity (x, z) = (cos a, sin a), a(k) = 180 - 360 k. Al importar en Unity, la X de Blender sale como -X y la Y como -Z
# (comprobado con rayos desde Unity: con (x, -z) los peldanos salian en espejo en X y con (-x, z) en espejo en Z).
SPIRAL_R, SPIRAL_IN, SPIRAL_RISE, SPIRAL_STEPS = 2.2, 0.3, 4.0, 22


def _sp(k, r, z):
    a = math.radians(180.0 - 360.0 * k)
    return (-r * math.cos(a), -r * math.sin(a), z)


def _tread(b, k0, k1, r0, r1, z_top, th, mat):
    """Peldano en abanico (sector de corona) con cantos biselados."""
    bm = bmesh.new()
    n = 8
    pts = [_sp(k0 + (k1 - k0) * i / n, r1, z_top) for i in range(n + 1)] + [_sp(k1 + (k0 - k1) * i / 2, r0, z_top) for i in range(3)]
    vs = [bm.verts.new(p) for p in pts]
    f = bm.faces.new(vs)
    bmesh.ops.recalc_face_normals(bm, faces=[f])
    if f.normal.z < 0:
        f.normal_flip()
    ext = bmesh.ops.extrude_face_region(bm, geom=[f])
    nv = [e for e in ext["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=nv, vec=Vector((0, 0, -th)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.004, offset_type="OFFSET", segments=2, profile=0.5, affect="EDGES", clamp_overlap=True)
    b.parts.append(Part(bm, mat, "x"))


def make_spiral_stair():
    b = Builder()
    R, RIN, RISE, N = SPIRAL_R, SPIRAL_IN, SPIRAL_RISE, SPIRAL_STEPS
    iron, wood = "AA_Hierro", "AA_Madera"
    # columna central con basa, anillos por peldano y remate
    b.cyl((0, 0, 2.45), 0.085, 4.9, "z", iron, 24, 0.0)
    b.cyl((0, 0, 0.03), 0.2, 0.06, "z", iron, 28, 0.01)
    b.cyl((0, 0, 0.1), 0.13, 0.08, "z", iron, 28, 0.012, r_top=0.095)
    b.cyl((0, 0, 4.93), 0.11, 0.06, "z", iron, 24, 0.012)
    b.cyl((0, 0, 5.0), 0.05, 0.1, "z", iron, 16, 0.01, r_top=0.02)
    for i in range(N):
        k0, k1 = i / N, (i + 1) / N
        z = RISE * (i + 1) / N - 0.005                                           # cara de pisada como la del peldano anterior
        _tread(b, k0 - 0.004, k1 + 0.004, RIN - 0.2, R, z, 0.035, iron)         # chapa de pisada (llega hasta la columna)
        km = (k0 + k1) / 2
        for kk in (k0 + 0.25 / N, km, k1 - 0.25 / N):                           # nervios antideslizantes en la pisada
            p0, p1 = _sp(kk, 0.35, z + 0.004), _sp(kk, R - 0.1, z + 0.004)
            ang = math.degrees(math.atan2(p1[1] - p0[1], p1[0] - p0[0]))
            b.box(((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2, z + 0.003), (R - 0.45, 0.012, 0.006), iron, 0.002, seg=1, rot=(0, 0, ang))
        p0, p1 = _sp(k0 + 0.03 / N, 0.12, z - 0.035), _sp(k0 + 0.03 / N, R - 0.08, z - 0.035)           # nervio de carga bajo el canto delantero
        ang = math.degrees(math.atan2(p1[1] - p0[1], p1[0] - p0[0]))
        b.box(((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2, z - 0.07), (R - 0.2, 0.025, 0.08), iron, 0.006, rot=(0, 0, ang))
        b.cyl((0, 0, z - 0.06), 0.12, 0.13, "z", iron, 24, 0.012)               # casquillo del peldano en la columna
        # balaustres: uno grueso con bolas en el arranque de cada peldano y uno fino en medio
        for j, kk in enumerate((k0 + 0.15 / N, k0 + 0.6 / N)):
            x, y, _ = _sp(kk, R - 0.07, 0)
            zb, zt = z, RISE * kk + 0.95 + (RISE / N) * 0.5
            rr = 0.016 if j == 0 else 0.011
            b.cyl((x, y, (zb + zt) / 2), rr, zt - zb, "z", iron, 10, 0.0)
            b.cyl((x, y, zb + 0.02), rr + 0.012, 0.04, "z", iron, 12, 0.006)
            if j == 0:
                for t in (0.3, 0.62):
                    b.cyl((x, y, zb + (zt - zb) * t), 0.026, 0.035, "z", iron, 12, 0.012)
    # pasamanos helicoidal de madera sobre una pletina de hierro, y pletina baja que une los balaustres
    pts_h, pts_p, pts_b = [], [], []
    M = 160
    for i in range(M + 1):
        k = i / M
        zz = RISE * k + 0.95 + (RISE / N) * 0.5
        pts_h.append(_sp(k, R - 0.07, zz + 0.03))
        pts_p.append(_sp(k, R - 0.07, zz - 0.005))
        pts_b.append(_sp(k, R - 0.07, RISE * k + (RISE / N) * 0.5 + 0.11))
    b.sweep(pts_h, 0.03, wood, 14)
    b.sweep(pts_p, 0.012, iron, 8)
    b.sweep(pts_b, 0.01, iron, 8)
    # voluta de arranque del pasamanos
    x0, y0, z0 = pts_h[0]
    vol = [(x0 + 0.06 * math.cos(t), y0 + 0.06 * math.sin(t) + 0.06, z0 - 0.02 * t / 6) for t in [i * 0.4 for i in range(16)]]
    b.sweep([pts_h[2], pts_h[1], pts_h[0]] + vol[1:], 0.028, wood, 12, r_end=0.02)
    b.cyl((x0, y0, z0 / 2), 0.03, z0, "z", iron, 14, 0.0)                         # poste de arranque
    b.cyl((x0, y0, 0.04), 0.06, 0.08, "z", iron, 16, 0.012)
    return b.finish("SpiralStairAA", smooth_angle=35)


# ------------------------------------------------------------------ luminaria de techo (sustituye a la caja "Housing" y al "Marco")
def make_ceiling_lamp():
    """Pantalla fluorescente de superficie con rejilla de celdas. Origen en el centro de la caja antigua (1,30 x 0,42 x 0,07; el techo esta en z = +0,035).
    El panel emisivo 'Bulb' de la escena (1,18 x 0,30, de z -0,0475 a -0,0225) se queda: la rejilla va delante y el marco alrededor. 252 instancias:
    poca geometria (cajas sin bisel donde no se ve)."""
    b = Builder()
    L, W = 1.30, 0.42
    paint = "AA_PinturaCrema"
    b.box((0, 0, 0.0), (L, W, 0.07), paint, 0.012, seg=2)                                     # carcasa de chapa con cantos redondeados
    b.box((0, 0, 0.03), (L + 0.02, W + 0.02, 0.01), paint, 0.003, seg=1)                     # pestana de apoyo contra el techo
    # marco de aluminio alrededor del difusor (abertura 1,20 x 0,32), algo por debajo del panel
    fz, fw = -0.044, 0.035
    for sy in (-1, 1):
        b.box((0, sy * (0.16 + fw / 2), fz), (1.20 + 2 * fw, fw, 0.014), "AA_Acero", 0.003, seg=1)
    for sx in (-1, 1):
        b.box((sx * (0.60 + fw / 2), 0, fz), (fw, 0.32, 0.014), "AA_Acero", 0.003, seg=1)
    # rejilla de celdas de chapa blanca (la de las oficinas de los 80) delante del panel: 1 lama larga y 7 transversales, bajas para que mande el panel
    lz = -0.0555
    b.box((0, 0, lz), (1.19, 0.003, 0.014), "AA_PinturaCrema", 0.0)
    for i in range(7):
        x = -0.45 + i * 0.15
        b.box((x, 0, lz), (0.003, 0.31, 0.014), "AA_PinturaCrema", 0.0)
    for sy in (-1, 1):
        b.box((0, sy * 0.155, lz), (1.19, 0.004, 0.014), "AA_PinturaCrema", 0.0)
    # tapas de los extremos, pestillos del marco y tornillos
    for sx in (-1, 1):
        b.box((sx * (L / 2 + 0.006), 0, -0.004), (0.012, W - 0.03, 0.062), "AA_Plastico", 0.004, seg=1)
        for sy in (-1, 1):
            b.cyl((sx * 0.62, sy * 0.178, fz - 0.0075), 0.0055, 0.003, "z", "AA_Acero", 8, 0.0)
    for sy in (-1, 1):
        b.box((0, sy * (0.16 + fw + 0.004), fz + 0.004), (0.05, 0.008, 0.012), "AA_Acero", 0.002, seg=1)
    b.box((L / 2 - 0.14, -W / 2 - 0.0005, 0.0), (0.07, 0.001, 0.03), "AA_Papel", 0.0)     # etiqueta del fabricante
    return b.finish("CeilingLampAA", smooth_angle=30)


# ------------------------------------------------------------------ tanda de oficina: ordenador, flexo, papelera y silla de visitas
def _crumple(b, c, r, seed, mat="AA_Papel"):
    """Bola de papel arrugado: icosfera con los vertices desplazados al azar."""
    import random
    rnd = random.Random(seed)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=r)
    for v in bm.verts:
        v.co *= 0.75 + rnd.random() * 0.45
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
    b.parts.append(Part(bm, mat, "x"))


def _shell(b, c, r_bot, r_top, h, segs, mat_out, mat_in, t=0.0025):
    """Pantalla abierta (cono sin tapas) con cara exterior e interior, para que se vea hueca."""
    for (rb, rt, inner) in ((r_bot, r_top, False), (r_bot - t, r_top - t, True)):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=False, cap_tris=False, segments=segs, radius1=rb, radius2=rt, depth=h)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        if inner:
            for f in bm.faces:
                f.normal_flip()
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
        b.parts.append(Part(bm, mat_in if inner else mat_out, "z"))


def make_desk_computer():
    """Puesto de ordenador de sobremesa: monitor plano hacia -Y (donde se sienta el usuario), teclado con las teclas modeladas, raton sobre alfombrilla y cables."""
    b = Builder()
    f = -1.0                                                                                     # la pantalla mira a -Y
    x, y, z = 0.0, 0.1, 0.0
    b.box((x, y, z + 0.008), (0.24, 0.18, 0.016), "AA_Plastico", 0.006)
    b.box((x, y + 0.03, z + 0.11), (0.06, 0.03, 0.2), "AA_Plastico", 0.008, rot=(-8, 0, 0))
    b.box((x, y, z + 0.30), (0.56, 0.055, 0.35), "AA_Plastico", 0.012)
    b.box((x, y + 0.045, z + 0.30), (0.3, 0.04, 0.22), "AA_Plastico", 0.015)                    # joroba trasera
    b.box((x, y + f * 0.0285, z + 0.315), (0.51, 0.004, 0.29), "AA_Pantalla", 0.0)
    b.box((x + 0.23, y + f * 0.029, z + 0.142), (0.012, 0.004, 0.006), "AA_Cromo", 0.0)
    for i in range(3):
        b.box((x - 0.2 + i * 0.03, y + f * 0.0285, z + 0.142), (0.016, 0.003, 0.006), "AA_Negro", 0.0)
    # teclado (las filas suben hacia el monitor)
    kx, ky = 0.0, -0.16
    b.box((kx, ky, 0.013), (0.45, 0.165, 0.026), "AA_Plastico", 0.006, rot=(3, 0, 0))
    for r in range(5):
        n = 14 if r < 4 else 7
        for k in range(n):
            w = 0.022
            kxx = kx - 0.19 + k * 0.0275
            if r == 4:
                if k == 3:
                    w, kxx = 0.13, kx - 0.19 + 3 * 0.0275 + 0.054
                elif k > 3:
                    kxx += 0.11
            b.box((kxx, ky - 0.06 + r * 0.0285, 0.03 + (4 - r) * 0.0014), (w, 0.022, 0.009), "AA_PinturaCrema", 0.0)
    b.box((0.33, -0.15, 0.002), (0.2, 0.17, 0.004), "AA_Tela", 0.001)                              # alfombrilla
    b.box((0.33, -0.15, 0.016), (0.055, 0.095, 0.024), "AA_Plastico", 0.011, seg=3)             # raton
    b.sweep([(0.33, -0.1, 0.012), (0.3, -0.02, 0.005), (0.18, 0.08, 0.005), (0.08, 0.14, 0.005)], 0.0025, "AA_Negro", 5)
    b.sweep([(kx + 0.1, ky + 0.083, 0.012), (0.12, -0.02, 0.005), (0.04, 0.12, 0.005)], 0.0025, "AA_Negro", 5)
    return b.finish("DeskComputer")


def make_desk_lamp():
    """Flexo articulado de los 70: base pesada con interruptor, dos brazos de varillas paralelas con muelles, rotulas y pantalla conica esmaltada hacia -Y."""
    b = Builder()
    enamel, steel = "AA_PinturaVerde", "AA_Cromo"
    b.cyl((0, 0, 0.012), 0.078, 0.024, "z", enamel, 28, 0.008)                                  # base
    b.cyl((0, 0, 0.03), 0.05, 0.016, "z", enamel, 24, 0.006, r_top=0.035)
    b.box((0.045, -0.035, 0.03), (0.018, 0.012, 0.01), "AA_Negro", 0.003)                        # interruptor
    b.cyl((0, 0.02, 0.05), 0.016, 0.03, "z", steel, 14, 0.004)                                  # rotula baja
    # brazo inferior: sube hacia +Y; brazo superior: vuelve hacia -Y
    p0, p1, p2 = (0.0, 0.02, 0.06), (0.0, 0.06, 0.33), (0.0, -0.2, 0.37)
    for dx in (-0.011, 0.011):
        b.sweep([(p0[0] + dx, p0[1], p0[2]), (p1[0] + dx, p1[1], p1[2])], 0.0045, steel, 8)
        b.sweep([(p1[0] + dx, p1[1], p1[2]), (p2[0] + dx, p2[1], p2[2])], 0.0045, steel, 8)
    for (a, c) in ((p0, p1), (p1, p2)):                                                          # muelles en espiral junto a cada brazo
        pts = []
        for i in range(61):
            t = 0.15 + 0.5 * i / 60
            px, py, pz = a[0] + (c[0] - a[0]) * t, a[1] + (c[1] - a[1]) * t, a[2] + (c[2] - a[2]) * t
            ang = i * 0.9
            pts.append((px + 0.026 + 0.006 * math.cos(ang), py + 0.006 * math.sin(ang), pz))
        b.sweep(pts, 0.0012, steel, 4)
    for p in (p1, p2):
        b.cyl(p, 0.014, 0.034, "x", steel, 14, 0.003)                                            # rotulas con pomo
        b.cyl((p[0] + 0.022, p[1], p[2]), 0.009, 0.012, "x", "AA_Negro", 12, 0.002)
    # pantalla conica abierta hacia abajo con portalamparas y bombilla
    hx, hy, hz = 0.0, -0.2, 0.30
    b.cyl((hx, hy, hz + 0.065), 0.022, 0.03, "z", enamel, 16, 0.004)
    _shell(b, (hx, hy, hz + 0.02), 0.075, 0.028, 0.07, 28, enamel, "AA_PinturaCrema")
    b.cyl((hx, hy, hz - 0.016), 0.077, 0.006, "z", enamel, 28, 0.002, r_top=0.077)               # borde enrollado
    b.cyl((hx, hy, hz + 0.03), 0.016, 0.02, "z", "AA_Negro", 14, 0.002)
    b.cyl((hx, hy, hz + 0.008), 0.024, 0.03, "z", "AA_Papel", 16, 0.01, r_top=0.014)            # bombilla
    b.sweep([(0.0, 0.03, 0.03), (0.0, 0.07, 0.01), (0.03, 0.075, 0.003)], 0.0025, "AA_Negro", 5)  # cable
    return b.finish("DeskLamp")


def make_trash_bin():
    """Papelera de chapa troncoconica con borde enrollado, aros embutidos, fondo y papeles arrugados."""
    b = Builder()
    paint = "AA_PinturaGris"
    _shell(b, (0, 0, 0.185), 0.12, 0.148, 0.35, 32, paint, paint, t=0.003)
    b.cyl((0, 0, 0.006), 0.122, 0.012, "z", paint, 32, 0.004)                                    # fondo
    b.cyl((0, 0, 0.003), 0.11, 0.006, "z", "AA_Negro", 32, 0.002)                                # zocalo de goma
    rim = [(0.153 * math.cos(2 * math.pi * k / 48), 0.153 * math.sin(2 * math.pi * k / 48), 0.36) for k in range(49)]
    b.sweep(rim, 0.006, paint, 10)                                                               # borde enrollado
    for z in (0.11, 0.24):                                                                       # aros embutidos
        r = 0.12 + (z - 0.01) / 0.35 * 0.028 + 0.002
        ring = [(r * math.cos(2 * math.pi * k / 48), r * math.sin(2 * math.pi * k / 48), z) for k in range(49)]
        b.sweep(ring, 0.004, paint, 6)
    for i, (x, y, z, r) in enumerate(((0.04, 0.02, 0.33, 0.055), (-0.05, 0.03, 0.34, 0.05), (0.0, -0.06, 0.35, 0.048), (0.06, -0.04, 0.37, 0.045))):
        _crumple(b, (x, y, z), r, i + 3)
    b.box((0.05, -0.02, 0.405), (0.15, 0.1, 0.002), "AA_Papel", 0.0, rot=(0, 28, 20))           # hoja asomando
    return b.finish("TrashBin")


def make_visitor_chair():
    """Silla de visitas: tubo cromado doblado con curvas suaves, asiento y respaldo tapizados, carcasas de plastico y tacos."""
    b = Builder()
    tube_r = 0.0125

    def bend(pts, n=4):
        """Redondea las esquinas de una polilinea con arcos cortos."""
        out = [pts[0]]
        for a, c, d in zip(pts[:-2], pts[1:-1], pts[2:]):
            va = Vector(c) - Vector(a); vd = Vector(d) - Vector(c)
            ra = min(0.05, va.length * 0.4); rd = min(0.05, vd.length * 0.4)
            pa = Vector(c) - va.normalized() * ra; pd = Vector(c) + vd.normalized() * rd
            for i in range(n + 1):
                t = i / n
                q = pa * (1 - t) ** 2 + Vector(c) * 2 * t * (1 - t) + pd * t * t
                out.append(tuple(q))
        out.append(pts[-1])
        return out

    for sx in (-1, 1):
        x = 0.185 * sx
        b.sweep(bend([(x, -0.17, 0.02), (x, -0.17, 0.42), (x, 0.2, 0.42), (x, 0.2, 0.02)]), tube_r, "AA_Cromo", 10)
        b.sweep(bend([(x, 0.16, 0.42), (x, 0.22, 0.62), (x, 0.235, 1.0)]), tube_r, "AA_Cromo", 10)
        for y in (-0.17, 0.2):
            b.cyl((x, y, 0.01), 0.017, 0.02, "z", "AA_Goma", 12, 0.004)                         # tacos
    for y, z in ((-0.17, 0.12), (0.2, 0.12)):
        b.sweep([(-0.185, y, z), (0.185, y, z)], 0.0095, "AA_Cromo", 8)                           # travesanos
    b.sweep(bend([(-0.185, 0.235, 0.9), (-0.185, 0.235, 1.0), (0.185, 0.235, 1.0), (0.185, 0.235, 0.9)]), tube_r, "AA_Cromo", 10)
    b.box((0, 0.0, 0.425), (0.40, 0.38, 0.02), "AA_Plastico", 0.008)                              # carcasa del asiento
    b.cushion((0, 0.0, 0.46), (0.44, 0.42, 0.05), "AA_Tela", 0.014, round_edge=0.02)
    b.box((0, 0.232, 0.78), (0.40, 0.02, 0.40), "AA_Plastico", 0.008, rot=(-4, 0, 0))             # carcasa del respaldo
    b.cushion((0, 0.212, 0.78), (0.38, 0.04, 0.38), "AA_Tela", 0.01, round_edge=0.018)
    for sx in (-1, 1):
        b.box((sx * 0.185, 0.225, 0.62), (0.03, 0.03, 0.05), "AA_Plastico", 0.008)                 # abrazaderas del respaldo
    return b.finish("Chair")


# ------------------------------------------------------------------ tanda 2 del punto 5: estanteria de libros, butaca del auditorio, banco de espera
BOOK_MATS = ("AA_LibroRojo", "AA_LibroAzul", "AA_LibroVerde", "AA_LibroMarron", "AA_LibroCrema")


def make_bookcase():
    """Libreria de madera de 1,2 m: costados con zocalo y cornisa moldurada, trasera de tablas, baldas con canto, y libros variados (tela con nervios dorados
    en el lomo, algunos inclinados, montones tumbados y cajas de archivo)."""
    import random
    b = Builder()
    W, D, H = 1.2, 0.36, 2.1
    wood = "AA_Madera"
    for sx in (-1, 1):
        b.box((sx * (W / 2 - 0.016), 0, H / 2 - 0.02), (0.032, D - 0.01, H - 0.12), wood, 0.004, grain="z")
    for i in range(6):                                                                            # trasera de tablas machihembradas
        b.box((-W / 2 + 0.03 + (W - 0.06) * (i + 0.5) / 6, D / 2 - 0.012, H / 2), ((W - 0.06) / 6 - 0.003, 0.012, H - 0.1), wood, 0.002, seg=1, grain="z")
    b.box((0, -0.005, 0.045), (W, D - 0.02, 0.09), wood, 0.006)                                    # zocalo
    b.box((0, -0.01, H - 0.045), (W + 0.04, D + 0.01, 0.05), wood, 0.008)                          # cornisa: cuerpo
    b.box((0, -0.02, H - 0.012), (W + 0.07, D + 0.04, 0.024), wood, 0.01, seg=3)                    # vuelo superior redondeado
    b.box((0, -D / 2 + 0.01, H - 0.08), (W - 0.04, 0.02, 0.03), wood, 0.008)                         # moldura bajo la cornisa
    shelves = [0.09, 0.5, 0.91, 1.32, 1.72]
    for z in shelves[1:]:
        b.box((0, 0, z), (W - 0.064, D - 0.03, 0.025), wood, 0.003)
        b.box((0, -D / 2 + 0.02, z - 0.004), (W - 0.064, 0.012, 0.035), wood, 0.004)                 # canto de la balda
    rnd = random.Random(7)
    tops = shelves[1:] + [H - 0.07]
    for s in range(5):
        z0 = shelves[s] + (0.0125 if s else 0.0)
        room = tops[s] - z0 - 0.02
        x = -W / 2 + 0.04
        while x < W / 2 - 0.07:
            r = rnd.random()
            if r < 0.07:                                                                          # hueco
                x += rnd.uniform(0.06, 0.14); continue
            if r < 0.13 and x < W / 2 - 0.34:                                                     # caja de archivo
                bw = 0.26
                b.box((x + bw / 2, 0.0, z0 + 0.13), (bw, 0.3, 0.26), "AA_Carton", 0.004, grain="x")
                b.box((x + bw / 2, -0.151, z0 + 0.17), (0.06, 0.002, 0.025), "AA_Negro", 0.0)           # asa
                b.box((x + bw / 2, -0.151, z0 + 0.09), (0.14, 0.002, 0.07), "AA_Papel", 0.0)            # etiqueta
                x += bw + 0.01; continue
            if r < 0.19 and x < W / 2 - 0.3:                                                      # montoncito tumbado
                for k in range(rnd.randint(2, 4)):
                    hh = rnd.uniform(0.028, 0.045)
                    b.box((x + 0.12 + rnd.uniform(-0.01, 0.01), rnd.uniform(-0.02, 0.01), z0 + 0.003 + k * 0.045 + hh / 2), (0.23, rnd.uniform(0.16, 0.2), hh), rnd.choice(BOOK_MATS), 0.003, seg=1)
                x += 0.26; continue
            w = rnd.uniform(0.022, 0.055); h = min(room, rnd.uniform(0.21, 0.33)); dd = rnd.uniform(0.17, 0.24)
            tilt = rnd.uniform(4, 10) if rnd.random() < 0.12 else 0.0
            cx = x + w / 2 + (math.sin(math.radians(tilt)) * h / 2 if tilt else 0.0)
            mat = rnd.choice(BOOK_MATS)
            yb = -D / 2 + 0.035 + dd / 2 + rnd.uniform(0, 0.02)
            b.box((cx, yb, z0 + h / 2), (w, dd, h), mat, 0.003, seg=1, rot=(0, tilt, 0))
            if rnd.random() < 0.5 and not tilt:                                                  # nervios dorados del lomo
                for zz in (z0 + h * 0.82, z0 + h * 0.18):
                    b.box((cx, yb - dd / 2 - 0.0008, zz), (w - 0.004, 0.0015, 0.006), "AA_Laton", 0.0)
            x += w + (0.012 + h * 0.4 if tilt else 0.003)
    return b.finish("Bookcase")


def make_auditorium_chair():
    """Butaca de sala de actos: laterales de fundicion con pie, reposabrazos de madera, asiento abatible y respaldo tapizados sobre carcasas de
    contrachapado y placa de numero de laton."""
    b = Builder()
    iron, fab = "AA_Hierro", "AA_TelaAzul"
    for sx in (-1, 1):
        x = 0.245 * sx
        # lateral: perfil con cintura (barrido grueso aplanado no; se hace con un tubo grueso y una placa)
        b.box((x, 0.02, 0.33), (0.035, 0.16, 0.5), iron, 0.012, grain="z")
        b.box((x, 0.03, 0.025), (0.07, 0.26, 0.05), iron, 0.012)                                    # pie
        b.cyl((x, -0.07, 0.006), 0.012, 0.012, "z", "AA_Acero", 10, 0.002)                          # pernos al suelo
        b.cyl((x, 0.13, 0.006), 0.012, 0.012, "z", "AA_Acero", 10, 0.002)
        b.sweep([(x, 0.08, 0.55), (x, 0.17, 0.68), (x, 0.22, 0.85), (x, 0.235, 0.98)], 0.016, iron, 10)  # montante del respaldo
        b.box((x, 0.01, 0.635), (0.06, 0.42, 0.035), "AA_Madera", 0.014, seg=3)                      # reposabrazos
        b.box((x, 0.06, 0.6), (0.03, 0.06, 0.04), iron, 0.008)
        b.cyl((x, 0.04, 0.45), 0.025, 0.04, "x", "AA_Acero", 16, 0.004)                              # eje del asiento abatible
    b.box((0.245 + 0.019, -0.03, 0.42), (0.004, 0.05, 0.03), "AA_Laton", 0.001)                    # placa de numero
    b.box((0, 0.02, 0.43), (0.43, 0.42, 0.02), "AA_Madera", 0.006, rot=(-4, 0, 0))                 # carcasa del asiento
    b.cushion((0, 0.0, 0.47), (0.44, 0.43, 0.065), fab, 0.022, round_edge=0.025)
    b.box((0, 0.27, 0.75), (0.45, 0.025, 0.5), "AA_Madera", 0.008, rot=(-8, 0, 0), grain="z")       # carcasa del respaldo
    b.cushion((0, 0.245, 0.76), (0.42, 0.06, 0.46), fab, 0.02, round_edge=0.022)
    for z in (0.66, 0.86):                                                                        # costuras horizontales
        b.box((0, 0.21, z), (0.36, 0.006, 0.006), "AA_Negro", 0.0)
    b.box((0, 0.03, 0.2), (0.46, 0.03, 0.03), iron, 0.008)                                          # travesano
    return b.finish("AuditoriumChair")


def make_waiting_bench():
    """Banco de espera de 2 m: tres bastidores de hierro fundido curvados, listones de madera atornillados en asiento y respaldo, reposabrazos."""
    b = Builder()
    L, iron, wood = 2.0, "AA_Hierro", "AA_Madera"
    for x in (-L / 2 + 0.12, 0.0, L / 2 - 0.12):
        b.sweep([(x, -0.18, 0.02), (x, -0.17, 0.25), (x, -0.15, 0.43)], 0.02, iron, 10)            # pata delantera
        b.sweep([(x, 0.22, 0.02), (x, 0.21, 0.3), (x, 0.24, 0.55), (x, 0.3, 0.88)], 0.02, iron, 10)  # pata trasera y respaldo
        b.box((x, 0.02, 0.425), (0.04, 0.42, 0.03), iron, 0.01)                                     # travesano del asiento
        for y in (-0.18, 0.22):
            b.cyl((x, y, 0.008), 0.03, 0.016, "z", iron, 14, 0.004)                                  # pies
    for x in (-L / 2 + 0.12, L / 2 - 0.12):                                                      # reposabrazos con voluta
        b.sweep([(x, 0.18, 0.66), (x, 0.0, 0.67), (x, -0.16, 0.65), (x, -0.2, 0.6), (x, -0.17, 0.55), (x, -0.155, 0.45)], 0.018, iron, 10)
        b.box((x, -0.0, 0.675), (0.06, 0.34, 0.03), wood, 0.012, seg=3)
    for i in range(5):                                                                            # listones del asiento
        y = -0.17 + i * 0.085
        b.box((0, y, 0.455), (L - 0.04, 0.07, 0.03), wood, 0.007, seg=2)
        for x in (-L / 2 + 0.12, 0.0, L / 2 - 0.12):
            b.cyl((x, y, 0.471), 0.006, 0.003, "z", "AA_Acero", 8, 0.0)                              # cabezas de tornillo
    for i in range(3):                                                                            # listones del respaldo inclinado
        t = 0.3 + i * 0.25
        y = 0.24 + (0.3 - 0.24) * t * 1.1
        z = 0.55 + (0.88 - 0.55) * t
        b.box((0, y - 0.03, z), (L - 0.04, 0.03, 0.085), wood, 0.007, seg=2, rot=(14, 0, 0))
    return b.finish("WaitingBench")


# ------------------------------------------------------------------ tanda 3 del punto 5: maquina expendedora y catre
def _coil(b, x, y, z, length, r=0.03, turns=6, mat="AA_Acero"):
    """Espiral de la maquina expendedora a lo largo de Y."""
    pts = []
    n = turns * 8
    for i in range(n + 1):
        t = i / n
        a = 2 * math.pi * turns * t
        pts.append((x + r * math.cos(a), y + length * t, z + r * math.sin(a)))
    b.sweep(pts, 0.0025, mat, 3)


def make_vending():
    """Maquina expendedora de los 80: chapa roja, vitrina de cristal transparente con baldas, espirales y productos, panel de monedas con teclado,
    pantalla y ranuras, trampilla de recogida, cartel superior y rejilla."""
    import random
    rnd = random.Random(11)
    b = Builder()
    W, D, H = 0.95, 0.8, 1.85
    red = "AA_PinturaRoja"
    fy = -D / 2
    # mueble: costados, techo, trasera y zocalo; frente abierto donde va la vitrina
    for sx in (-1, 1):
        b.box((sx * (W / 2 - 0.015), 0.0, H / 2), (0.03, D, H), red, 0.012, grain="z")
    b.box((0, 0.0, H - 0.015), (W, D, 0.03), red, 0.01)
    b.box((0, D / 2 - 0.01, H / 2), (W - 0.03, 0.02, H - 0.02), red, 0.004, grain="z")
    b.box((0, 0.01, 0.05), (W - 0.04, D - 0.04, 0.1), "AA_Negro", 0.006)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cyl((sx * 0.4, sy * 0.33, 0.012), 0.025, 0.024, "z", "AA_Negro", 12, 0.004)       # patas niveladoras
    # puerta: marco de chapa con la vitrina a la izquierda y el panel de pago a la derecha
    b.box((0, fy + 0.02, 0.27), (W - 0.01, 0.04, 0.34), red, 0.008)                                 # parte baja de la puerta
    b.box((0, fy + 0.02, 1.73), (W - 0.01, 0.04, 0.24), red, 0.008)                                 # cabecero
    b.box((-W / 2 + 0.035, fy + 0.02, 1.03), (0.07, 0.04, 1.18), red, 0.008, grain="z")
    b.box((0.21, fy + 0.02, 1.03), (0.04, 0.04, 1.18), red, 0.008, grain="z")
    b.box((0.355, fy + 0.02, 1.03), (0.25, 0.04, 1.18), red, 0.008, grain="z")                      # columna del panel
    # vitrina: marco negro, cristal transparente e interior
    gx0, gx1, gz0, gz1 = -W / 2 + 0.07, 0.19, 0.44, 1.62
    gcx, gw = (gx0 + gx1) / 2, gx1 - gx0
    b.frame_profile((gx0, gx1, gz0, gz1), [(0.0, 0.0), (0.0, 0.012), (0.018, 0.012), (0.018, 0.0)], fy - 0.0, -1, "AA_Negro")
    b.box((gcx, fy + 0.012, (gz0 + gz1) / 2), (gw, 0.004, gz1 - gz0), "AA_Cristal", 0.0)
    b.box((gcx, D / 2 - 0.025, (gz0 + gz1) / 2), (gw, 0.004, gz1 - gz0), "AA_PinturaCrema", 0.0)   # fondo claro del interior
    cols = 5
    for r in range(5):
        z = gz0 + 0.06 + r * 0.23
        b.box((gcx, 0.02, z - 0.012), (gw - 0.02, D - 0.12, 0.01), "AA_Acero", 0.002, seg=1)       # bandeja
        b.box((gcx, fy + 0.07, z + 0.01), (gw - 0.02, 0.012, 0.035), "AA_Negro", 0.002, seg=1)       # canto con precios
        for c in range(cols):
            x = gx0 + 0.04 + c * (gw - 0.08) / (cols - 1)
            b.box((x, fy + 0.0705, z + 0.02), (0.04, 0.002, 0.012), "AA_Papel", 0.0)               # etiqueta de precio
            _coil(b, x, fy + 0.09, z + 0.035, 0.42, 0.032, 5)
            kind = rnd.random()
            for k in range(3 if r % 2 == 0 else 4):
                py = fy + 0.13 + k * 0.1
                if r % 2 == 0:                                                                      # latas
                    b.cyl((x, py, z + 0.065), 0.031, 0.115, "z", rnd.choice(("AA_PlasticoRojo", "AA_PlasticoAzul", "AA_PlasticoAmarillo", "AA_Cromo")), 14, 0.004)
                else:                                                                               # bolsas y chocolatinas
                    mat = ("AA_PlasticoRojo", "AA_PlasticoAzul", "AA_PlasticoAmarillo", "AA_Carton")[int(kind * 4) % 4]
                    b.box((x, py, z + 0.07), (0.085, 0.03, 0.13), mat, 0.008, seg=2)
            if rnd.random() < 0.15:                                                                # una bolsa caida sobre la bandeja
                b.box((x + 0.01, fy + 0.11, z + 0.01), (0.085, 0.13, 0.025), "AA_PlasticoAmarillo", 0.008, seg=2, rot=(0, 0, 25))
    # panel de pago
    px = 0.355
    b.box((px, fy - 0.004, 1.3), (0.2, 0.012, 0.5), "AA_Negro", 0.004)
    b.box((px, fy - 0.011, 1.47), (0.14, 0.004, 0.05), "AA_Pantalla", 0.0)                         # pantalla de precio
    for r in range(4):
        for c in range(3):
            b.box((px - 0.045 + c * 0.045, fy - 0.014, 1.38 - r * 0.045), (0.032, 0.008, 0.032), "AA_Cromo", 0.004, seg=2)
    b.box((px, fy - 0.012, 1.15), (0.06, 0.006, 0.02), "AA_Cromo", 0.003)                           # ranura de monedas
    b.box((px, fy - 0.013, 1.15), (0.035, 0.004, 0.004), "AA_Negro", 0.0)
    b.cyl((px + 0.06, fy - 0.012, 1.15), 0.013, 0.01, "y", "AA_Cromo", 14, 0.002)                   # boton de devolucion
    b.box((px, fy - 0.01, 1.08), (0.08, 0.004, 0.035), "AA_Negro", 0.0)                             # ranura de billetes
    b.box((px, fy - 0.03, 0.82), (0.12, 0.05, 0.1), "AA_Negro", 0.012)                              # cajetin del cambio
    b.box((px, fy - 0.055, 0.81), (0.09, 0.004, 0.05), "AA_Cromo", 0.002)
    b.box((px, fy - 0.006, 1.6), (0.18, 0.006, 0.12), "AA_Papel", 0.0)                              # pegatina de instrucciones
    # trampilla de recogida y rejilla
    b.box((-0.12, fy - 0.025, 0.25), (0.55, 0.05, 0.18), "AA_Negro", 0.012)
    b.box((-0.12, fy - 0.052, 0.25), (0.47, 0.004, 0.11), "AA_Plastico", 0.0, rot=(-8, 0, 0))
    for i in range(8):
        b.box((-0.32 + i * 0.09, fy - 0.003, 0.08), (0.06, 0.006, 0.012), "AA_Negro", 0.0)          # rejilla de ventilacion
    # cartel superior con marco cromado
    b.box((0, fy - 0.006, 1.74), (W - 0.1, 0.012, 0.15), "AA_PinturaCrema", 0.004)
    b.frame_profile((-W / 2 + 0.05, W / 2 - 0.05, 1.665, 1.815), [(0.0, 0.0), (0.0, 0.014), (0.012, 0.014), (0.012, 0.0)], fy - 0.012, -1, "AA_Cromo")
    b.box((-0.18, fy - 0.0125, 1.74), (0.42, 0.002, 0.07), "AA_PinturaRoja", 0.0)                    # franja del rotulo
    b.box((0.3, fy - 0.0125, 1.74), (0.2, 0.002, 0.05), "AA_PlasticoAzul", 0.0)
    return b.finish("VendingMachine")


def make_cot():
    """Catre de calabozo: bastidor de angular de acero soldado, patas de tubo cuadrado con zapatas, somier de pletinas cruzadas, colchon fino,
    manta militar doblada y almohada."""
    b = Builder()
    steel = "AA_Hierro"
    W, L, zr = 0.88, 1.88, 0.4
    for sx in (-1, 1):                                                                               # largueros en L
        b.box((sx * (W / 2 - 0.02), 0, zr), (0.04, L, 0.005), steel, 0.0015, seg=1)
        b.box((sx * (W / 2 - 0.0025), 0, zr - 0.02), (0.005, L, 0.04), steel, 0.0015, seg=1)
    for sy in (-1, 1):                                                                               # cabezales en L
        b.box((0, sy * (L / 2 - 0.02), zr), (W, 0.04, 0.005), steel, 0.0015, seg=1)
        b.box((0, sy * (L / 2 - 0.0025), zr - 0.02), (W, 0.005, 0.04), steel, 0.0015, seg=1)
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (W / 2 - 0.02), sy * (L / 2 - 0.02)
            b.box((x, y, zr / 2), (0.035, 0.035, zr), steel, 0.004, grain="z")                      # pata
            b.box((x, y, 0.004), (0.06, 0.06, 0.008), steel, 0.002)                                  # zapata
            b.cyl((x, y, 0.009), 0.006, 0.004, "z", "AA_Acero", 8, 0.0)                              # tornillo al suelo
    for k in range(9):                                                                              # pletinas a lo largo
        x = -W / 2 + 0.07 + k * (W - 0.14) / 8
        b.box((x, 0, zr + 0.002), (0.022, L - 0.06, 0.003), steel, 0.0)
    for k in range(13):                                                                             # pletinas transversales
        y = -L / 2 + 0.09 + k * (L - 0.18) / 12
        b.box((0, y, zr + 0.005), (W - 0.06, 0.022, 0.003), steel, 0.0)
    b.box((0, -L / 2 + 0.05, zr - 0.08), (W - 0.06, 0.02, 0.02), steel, 0.004)            # travesano bajo
    b.box((0, L / 2 - 0.05, zr - 0.08), (W - 0.06, 0.02, 0.02), steel, 0.004)
    b.cushion((0, 0, zr + 0.05), (W - 0.04, L - 0.04, 0.085), "AA_Colchon", 0.015, round_edge=0.03)   # colchon
    b.cushion((0, 0.36, zr + 0.105), (W - 0.02, 1.08, 0.03), "AA_Manta", 0.012, round_edge=0.014)       # manta estirada
    b.cushion((0, -0.2, zr + 0.13), (W - 0.04, 0.12, 0.04), "AA_Manta", 0.01, round_edge=0.018)        # doblez
    b.cushion((0, -0.66, zr + 0.13), (0.52, 0.34, 0.08), "AA_Colchon", 0.03, round_edge=0.035)          # almohada
    return b.finish("Cot")


# ------------------------------------------------------------------ cartel de la fachada
def _text(b, text, size, matrix, mat, depth=0.03, spacing=1.2, bevel=0.0015):
    """Texto con grosor (fuente de Blender, Inter, licencia libre) como pieza del Builder. Se escribe en el plano XY y 'matrix' lo coloca."""
    cu = bpy.data.curves.new("txt", "FONT")
    cu.body = text
    cu.size = size
    cu.extrude = depth / 2
    cu.bevel_depth = bevel
    cu.bevel_resolution = 1
    cu.resolution_u = 3
    cu.align_x = "CENTER"
    cu.align_y = "CENTER"
    cu.space_character = spacing
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
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    b.parts.append(Part(bm, mat, "x"))


def _star(b, cx, y, cz, r_out, r_in, n, depth, mat):
    """Estrella de n puntas extruida hacia -Y (mirando al frente)."""
    bm = bmesh.new()
    vs = []
    for k in range(2 * n):
        a = math.radians(90 + 180 / n * k)
        rr = r_out if k % 2 == 0 else r_in
        vs.append(bm.verts.new((cx + rr * math.cos(a), y, cz + rr * math.sin(a))))
    f = bm.faces.new(vs)
    bmesh.ops.recalc_face_normals(bm, faces=[f])
    if f.normal.y > 0:
        f.normal_flip()
    ext = bmesh.ops.extrude_face_region(bm, geom=[f])
    nv = [e for e in ext["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=nv, vec=Vector((0, -depth, 0)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    b.parts.append(Part(bm, mat, "x"))


def make_station_sign():
    """Rotulo de la comisaria sobre la marquesina: faja de piedra con moldura, letras de laton sueltas sobre tacos ('SECTOR 7' y 'POLICE DEPARTMENT'),
    escudo de bronce con estrella esmaltada de siete puntas y dos focos de cuello de cisne. Todo queda delante del plano y = 0 (la fachada): la trasera
    toca ese plano y nada lo atraviesa (antes la placa se metia en el muro y parpadeaba). Frente hacia -Y, origen en el centro del borde inferior trasero."""
    b = Builder()
    W, H, D = 5.4, 0.82, 0.07
    stone = "AA_Piedra"
    b.box((0, -D / 2, H / 2), (W, D, H), stone, 0.012, seg=2)                                       # faja
    for z, hh in ((0.03, 0.06), (H - 0.03, 0.06)):                                                  # molduras arriba y abajo
        b.box((0, -D - 0.012, z), (W + 0.04, 0.03, hh), stone, 0.014, seg=3)
    for sx in (-1, 1):                                                                              # remates laterales
        b.box((sx * (W / 2 - 0.05), -D - 0.008, H / 2), (0.1, 0.022, H - 0.12), stone, 0.01, seg=2, grain="z")
    b.box((0, -D - 0.003, H / 2), (W - 0.24, 0.006, H - 0.16), stone, 0.002, seg=1)                 # campo rehundido (filete)
    # letras de laton sobre tacos
    rot = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))                         # plano XY -> XZ mirando a -Y
    fy = -D - 0.03
    _text(b, "SECTOR 7", 0.36, Matrix.Translation((0.42, fy - 0.015, 0.5)) @ rot, "AA_Laton", 0.03, 1.25)
    _text(b, "POLICE DEPARTMENT", 0.12, Matrix.Translation((0.42, fy - 0.01, 0.19)) @ rot, "AA_Laton", 0.02, 1.45)
    for x in (-0.55, 0.0, 0.55, 1.05, 1.4):                                                         # tacos (casi ocultos tras las letras)
        b.cyl((0.42 + x, -D - 0.015, 0.5), 0.006, 0.03, "y", "AA_Acero", 8, 0.0)
    # escudo: disco de bronce con aro, estrella esmaltada y una estrella de laton encima
    ex, ez = -2.05, H / 2
    b.cyl((ex, -D - 0.02, ez), 0.33, 0.04, "y", "AA_Laton", 48, 0.008)
    ring = [(ex + 0.3 * math.cos(2 * math.pi * k / 64), -D - 0.042, ez + 0.3 * math.sin(2 * math.pi * k / 64)) for k in range(65)]
    b.sweep(ring, 0.012, "AA_Laton", 8)
    b.cyl((ex, -D - 0.041, ez), 0.26, 0.004, "y", "AA_EsmalteAzul", 48, 0.0)
    _star(b, ex, -D - 0.043, ez, 0.21, 0.09, 7, 0.012, "AA_Laton")
    b.cyl((ex, -D - 0.057, ez), 0.055, 0.006, "y", "AA_EsmalteAzul", 24, 0.001)
    _text(b, "7", 0.08, Matrix.Translation((ex, -D - 0.062, ez)) @ rot, "AA_Laton", 0.006, 1.0, 0.0)
    for k in range(4):                                                                              # tornillos del escudo
        a = math.radians(45 + 90 * k)
        b.cyl((ex + 0.315 * math.cos(a), -D - 0.042, ez + 0.315 * math.sin(a)), 0.008, 0.004, "y", "AA_Acero", 8, 0.0)
    # focos de cuello de cisne que iluminan la faja desde arriba
    for sx in (-1.4, 1.6):
        b.cyl((sx, -0.008, H + 0.18), 0.04, 0.016, "y", "AA_Negro", 16, 0.003)                       # placa de anclaje
        b.sweep([(sx, -0.016, H + 0.18), (sx, -0.1, H + 0.3), (sx, -0.3, H + 0.36), (sx, -0.42, H + 0.3)], 0.014, "AA_Negro", 10)
        _shell(b, (sx, -0.46, H + 0.24), 0.08, 0.035, 0.09, 20, "AA_Negro", "AA_PinturaCrema")
        b.cyl((sx, -0.46, H + 0.25), 0.025, 0.04, "z", "AA_Papel", 12, 0.01)
    return b.finish("StationSign")


# La pistola esta en build_pistol_aa.py (siluetas extruidas y cortes booleanos).


# ------------------------------------------------------------------ archivo (sustituye a build_archive.py; mismas medidas, frente -Y y semillas)
BINDER_MATS = ("AA_PlasticoAzul", "AA_PlasticoRojo", "AA_Plastico", "AA_PinturaVerde")


def aa_archive_box(b, c, w, d, h, rng, lid=True, rot=0.0):
    """Caja de archivo de carton: cuerpo con cantos, tapa con faldon, asideros troquelados, etiqueta escrita y, si esta abierta, papeles asomando."""
    R = (0, 0, rot)
    ca, sa = math.cos(math.radians(rot)), math.sin(math.radians(rot))

    def at(lx, ly, lz):
        return (c[0] + lx * ca - ly * sa, c[1] + lx * sa + ly * ca, c[2] + lz)
    mat = "AA_Carton" if rng.random() < 0.7 else "AA_CartonOscuro"
    b.box(at(0, 0, h / 2), (w, d, h), mat, 0.004, seg=2, rot=R)
    if lid:
        b.box(at(0, 0, h - 0.018), (w + 0.008, d + 0.008, 0.036), mat, 0.004, seg=2, rot=R)
        b.box(at(0, -d / 2 - 0.0045, h - 0.018), (w * 0.98, 0.001, 0.03), "AA_CartonOscuro", 0.0, rot=R)   # borde de la tapa (sombra)
    else:
        for i in range(rng.randint(3, 6)):
            b.box(at(rng.uniform(-w * 0.3, w * 0.3), rng.uniform(-d * 0.3, d * 0.3), h + 0.015), (w * 0.85, 0.002, 0.07), "AA_Papel", 0.0,
                  rot=(rng.uniform(-12, 12), 0, rot + 90 + rng.uniform(-8, 8)))
    for sx in (-1, 1):                                                                    # asideros troquelados en los costados
        b.box(at(sx * (w / 2 + 0.0008), 0, h * 0.78), (0.0016, d * 0.32, 0.024), "AA_Negro", 0.0, rot=R)
    b.box(at(0, -d / 2 - 0.0008, h * 0.82), (w * 0.28, 0.0016, 0.022), "AA_Negro", 0.0, rot=R)            # asidero frontal
    b.box(at(0, -d / 2 - 0.001, h * 0.48), (w * 0.5, 0.0015, h * 0.26), "AA_Papel", 0.0, rot=R)           # etiqueta
    for k in range(3):                                                                    # lineas escritas a mano
        b.box(at(-w * 0.04 + rng.uniform(-0.01, 0.01), -d / 2 - 0.0019, h * (0.54 - k * 0.06)), (w * rng.uniform(0.25, 0.4), 0.0006, 0.004),
              "AA_Tinta", 0.0, rot=R)


def aa_binders(b, x0, z, d, hb, n, mat, rng, x_max):
    """Fila de archivadores AZ con anilla en el lomo y tarjeta; la ultima puede caer inclinada. Devuelve la x final."""
    x = x0
    for i in range(n):
        if x + 0.08 > x_max:
            break
        tilt = rng.uniform(10, 24) if (i == n - 1 and rng.random() < 0.55) else 0.0
        cx = x + 0.038 + (math.sin(math.radians(tilt)) * hb / 2 if tilt else 0.0)
        b.box((cx, -0.02, z + hb / 2), (0.074, d, hb), mat, 0.003, seg=2, rot=(0, tilt, 0))
        if not tilt:
            yf = -0.02 - d / 2
            b.box((cx, yf - 0.001, z + hb * 0.62), (0.042, 0.002, 0.075), "AA_Papel", 0.0007, seg=1)           # tarjeta del lomo
            b.cyl((cx, yf - 0.002, z + hb * 0.24), 0.011, 0.004, "y", "AA_Negro", 14, 0.001)                  # agujero de la anilla
            b.box((cx, yf - 0.0015, z + hb * 0.62 + 0.012), (0.03, 0.0006, 0.004), "AA_Tinta", 0.0)
        x += 0.078 + (0.03 if tilt else 0.0)
    return x


def aa_shelf_frame(b, L, D, H, levels, center_post=False):
    """Estanteria de angulo ranurado: montantes en L con ranuras, baldas de chapa con pestana doblada, escuadras atornilladas y cruces traseras."""
    st = "AA_PinturaGris"
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (L / 2 - 0.02), sy * (D / 2 - 0.02)
            b.box((x, y + sy * 0.0185, H / 2), (0.04, 0.003, H), st, 0.0012, seg=1, grain="z")          # ala que da al frente/fondo
            b.box((x + sx * 0.0185, y, H / 2), (0.003, 0.04, H), st, 0.0012, seg=1, grain="z")          # ala lateral
            for k in range(int(H / 0.05)):                                                            # ranuras (en las dos alas)
                zz = 0.03 + k * 0.05
                b.box((x, y + sy * 0.0202, zz), (0.008, 0.0008, 0.018), "AA_Negro", 0.0)
                b.box((x + sx * 0.0202, y, zz), (0.0008, 0.008, 0.018), "AA_Negro", 0.0)
            b.box((x, y, 0.004), (0.05, 0.05, 0.008), st, 0.002, seg=1)                               # pie
    if center_post:
        for sy in (-1, 1):
            b.box((0, sy * (D / 2 - 0.0035), H / 2), (0.04, 0.003, H), st, 0.0012, seg=1, grain="z")
    for z in levels:
        b.box((0, 0, z), (L - 0.012, D - 0.012, 0.0025), st, 0.0008, seg=1)                          # chapa de la balda
        for sy in (-1, 1):
            b.box((0, sy * (D / 2 - 0.008), z - 0.016), (L - 0.012, 0.0025, 0.035), st, 0.0008, seg=1)   # pestanas dobladas
        for sx in (-1, 1):
            b.box((sx * (L / 2 - 0.008), 0, z - 0.016), (0.0025, D - 0.012, 0.035), st, 0.0008, seg=1)
            for sy in (-1, 1):                                                                        # tornillos a los montantes
                b.cyl((sx * (L / 2 - 0.02), sy * (D / 2 - 0.0005) + sy * 0.0195, z - 0.016), 0.004, 0.003, "y", "AA_Acero", 8, 0.0)
    for s in (-1, 1):                                                                                # cruces de pletina por detras
        ang = math.degrees(math.atan2(L - 0.06, H - 0.1))
        b.box((0, D / 2 - 0.0025, H / 2), (0.022, 0.0025, math.hypot(L - 0.06, H - 0.1)), st, 0.0006, seg=1, rot=(0, s * ang, 0))
    b.cyl((0, D / 2 - 0.004, H / 2), 0.006, 0.006, "y", "AA_Acero", 10, 0.001)


def aa_fill_level(b, L, D, z, h_avail, rng, mess):
    x = -L / 2 + 0.06
    while x < L / 2 - 0.12:
        r = rng.random()
        if r < mess * 0.25:
            x += rng.uniform(0.15, 0.4)
            continue
        if r < 0.62:
            w = rng.uniform(0.3, 0.4)
            if x + w > L / 2 - 0.04:
                break
            h = min(h_avail - 0.04, rng.uniform(0.26, 0.32))
            rot = rng.uniform(-6, 6) * mess
            dy = rng.uniform(-0.04, 0.06) * (1 + mess)
            aa_archive_box(b, (x + w / 2, dy, z + 0.0015), w, D * 0.85, h, rng, lid=rng.random() > 0.15 * (1 + mess), rot=rot)
            x += w + rng.uniform(0.005, 0.03)
        else:
            x = aa_binders(b, x, z + 0.0015, D * 0.6, min(h_avail - 0.04, 0.31), rng.randint(3, 9), rng.choice(BINDER_MATS), rng, L / 2 - 0.04)
            x += rng.uniform(0.01, 0.05)


def aa_archive_shelf(name, seed, mess):
    import random
    rng = random.Random(seed)
    b = Builder()
    L, D, H = 1.8, 0.5, 2.3
    levels = [0.08, 0.53, 0.98, 1.43, 1.88, 2.28]
    aa_shelf_frame(b, L, D, H, levels)
    for i, z in enumerate(levels[:-1]):
        aa_fill_level(b, L, D, z, levels[i + 1] - z, rng, mess)
    for k in range(rng.randint(0, 2)):
        aa_archive_box(b, (rng.uniform(-0.6, 0.6), 0, levels[-1] + 0.0015), 0.36, 0.42, 0.28, rng, lid=True, rot=rng.uniform(-20, 20))
    return b.finish(name)


def make_archive_wall_shelf():
    import random
    rng = random.Random(21)
    b = Builder()
    L, D, H = 4.0, 0.45, 3.4
    levels = [0.08 + i * 0.47 for i in range(7)] + [3.37]
    aa_shelf_frame(b, L, D, H, levels, center_post=True)
    for i, z in enumerate(levels[:-1]):
        aa_fill_level(b, L, D, z, levels[i + 1] - z, rng, 0.3)
    return b.finish("ArchiveWallShelf")


def aa_box_stack(name, seed):
    import random
    rng = random.Random(seed)
    b = Builder()
    z = 0.0
    for lvl in range(rng.randint(3, 5)):
        for i in range(2 if lvl < 2 else 1):
            aa_archive_box(b, (rng.uniform(-0.05, 0.05) + (i - 0.5) * 0.42 * (1 if lvl < 2 else 0), rng.uniform(-0.04, 0.04), z), 0.4, 0.32, 0.27, rng,
                           lid=rng.random() > 0.3, rot=rng.uniform(-12, 12))
        z += 0.3
    aa_archive_box(b, (0.65, 0.1, 0), 0.4, 0.32, 0.27, rng, lid=False, rot=35)
    for i in range(6):
        _sheet(b, (rng.uniform(-0.6, 0.9), rng.uniform(-0.5, 0.5), 0.0015 + i * 0.0004), rng.uniform(0, 180), rng)
    return b.finish(name)


def _sheet(b, c, rot, rng, mat="AA_Papel", size=(0.21, 0.297), curl=0.012, lines=True):
    """Hoja con una esquina levantada (rejilla 4x5 curvada) y renglones de texto."""
    w, h = size
    bm = bmesh.new()
    nx, ny = 4, 5
    vs = [[bm.verts.new((-w / 2 + w * i / nx, -h / 2 + h * j / ny, 0)) for j in range(ny + 1)] for i in range(nx + 1)]
    for i in range(nx):
        for j in range(ny):
            bm.faces.new((vs[i][j], vs[i + 1][j], vs[i + 1][j + 1], vs[i][j + 1]))
    k = rng.random()
    for v in bm.verts:
        u, t = (v.co.x + w / 2) / w, (v.co.y + h / 2) / h
        v.co.z = curl * max(0.0, (u if k < 0.5 else 1 - u) * t - 0.45) * 2.0 + 0.002 * math.sin(u * 5 + t * 3)
    bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.radians(rot), 3, "Z"))
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector(c))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    b.parts.append(Part(bm, mat, "x"))
    if lines:
        ca, sa = math.cos(math.radians(rot)), math.sin(math.radians(rot))
        for r in range(7):
            ly = h * 0.36 - r * 0.028
            lw = w * rng.uniform(0.45, 0.75)
            lx = -w * 0.38 + lw / 2
            b.box((c[0] + lx * ca - ly * sa, c[1] + lx * sa + ly * ca, c[2] + 0.0008), (lw, 0.0025, 0.0004), "AA_Tinta", 0.0, rot=(0, 0, rot))


def make_paper_scatter():
    import random
    rng = random.Random(5)
    b = Builder()
    for i in range(45):
        r = rng.random() ** 0.7 * 1.0
        a = rng.uniform(0, 2 * math.pi)
        _sheet(b, (r * math.cos(a), r * math.sin(a), 0.001 + i * 0.0004), rng.uniform(0, 180), rng, "AA_Papel" if rng.random() < 0.75 else "AA_PapelAmarillo",
               lines=rng.random() < 0.5)
    for i in range(3):                                                                    # carpetas de carton abiertas
        a = rng.uniform(0, 2 * math.pi)
        _sheet(b, (0.6 * math.cos(a), 0.6 * math.sin(a), 0.003), rng.uniform(0, 180), rng, "AA_CartonClaro", size=(0.46, 0.32), curl=0.03, lines=False)
    return b.finish("PaperScatter")


def make_rolling_ladder():
    """Escalera de archivo con ruedas: largueros de tubo rojo, peldanos de chapa estriada, plataforma con barandilla y ruedas con freno."""
    b = Builder()
    red, steel = "AA_PinturaRoja", "AA_Acero"
    for sx in (-1, 1):
        x = sx * 0.25
        b.sweep([(x, 0.07, 0.07), (x, 0.3, 1.2)], 0.016, red, 12)                               # larguero delantero
        b.sweep([(x, 0.52, 0.07), (x, 0.42, 1.2)], 0.015, red, 12)                              # trasero
        b.sweep([(x, 0.3, 1.2), (x, 0.3, 1.71)], 0.013, red, 10)                                # montante de la barandilla
        for y in (0.07, 0.52):                                                                 # ruedas
            b.cyl((x, y, 0.035), 0.033, 0.024, "x", "AA_Goma", 20, 0.006)
            b.cyl((x, y, 0.035), 0.012, 0.03, "x", steel, 12, 0.002)
            b.box((x, y, 0.062), (0.035, 0.02, 0.02), steel, 0.004)
    b.sweep([(-0.27, 0.3, 1.71), (0.27, 0.3, 1.71)], 0.013, red, 10)                             # barandilla superior
    b.sweep([(-0.25, 0.3, 1.45), (0.25, 0.3, 1.45)], 0.01, red, 8)
    for k in range(4):                                                                          # peldanos estriados
        z = 0.3 + k * 0.28
        y = 0.07 + (z - 0.07) / 1.13 * 0.23
        b.box((0, y + 0.05, z), (0.5, 0.16, 0.012), steel, 0.003, seg=1)
        for i in range(6):
            b.box((0, y + 0.0 + i * 0.022, z + 0.0075), (0.46, 0.006, 0.003), steel, 0.001, seg=1)
    b.box((0, 0.36, 1.2), (0.54, 0.3, 0.014), steel, 0.003, seg=1)                              # plataforma
    for i in range(10):
        b.box((0, 0.24 + i * 0.026, 1.2085), (0.5, 0.006, 0.003), steel, 0.001, seg=1)
    return b.finish("RollingLadder")


def make_card_catalog():
    """Fichero de biblioteca: mueble de roble con zocalo y cornisa molduradas, 40 cajones con tirador de anilla y tarjetero de laton, algunos abiertos con fichas."""
    import random
    rng = random.Random(9)
    b = Builder()
    W, D, H = 1.0, 0.5, 1.3
    wood = "AA_Madera"
    b.box((0, 0, 0.06), (W - 0.04, D - 0.04, 0.12), wood, 0.01, seg=3)
    b.box((0, 0.0, 0.13 + (H - 0.13) / 2), (W, D, H - 0.13), wood, 0.006, seg=2, grain="z")
    b.box((0, -0.005, H + 0.012), (W + 0.04, D + 0.03, 0.024), wood, 0.009, seg=3)
    b.box((0, -0.01, H - 0.012), (W + 0.02, D + 0.02, 0.02), wood, 0.006, seg=2)
    cols, rows = 5, 8
    cw = (W - 0.08) / cols
    rh = (H - 0.22) / rows
    for c in range(cols):
        for r in range(rows):
            x = -W / 2 + 0.04 + cw * (c + 0.5)
            z = 0.17 + rh * (r + 0.5)
            out = rng.uniform(0.08, 0.25) if rng.random() < 0.12 else 0.0
            if out:
                b.box((x, -D / 2 - out / 2 + 0.02, z - 0.01), (cw - 0.03, out + 0.04, rh - 0.03), wood, 0.002, seg=1)
                for k in range(int(out / 0.012)):
                    b.box((x, -D / 2 - out + 0.03 + k * 0.012, z), (cw - 0.05, 0.0015, rh - 0.05), "AA_Papel", 0.0)
            fy = -D / 2 - out
            b.box((x, fy - 0.006, z), (cw - 0.012, 0.014, rh - 0.012), wood, 0.003, seg=2)
            b.box((x, fy - 0.0135, z + rh * 0.2), (0.05, 0.002, 0.022), "AA_Laton", 0.0008, seg=1)            # tarjetero
            b.box((x, fy - 0.0145, z + rh * 0.2), (0.042, 0.001, 0.016), "AA_Papel", 0.0)
            b.sweep([(x - 0.012, fy - 0.013, z - rh * 0.12), (x - 0.01, fy - 0.022, z - rh * 0.2), (x + 0.01, fy - 0.022, z - rh * 0.2),
                     (x + 0.012, fy - 0.013, z - rh * 0.12)], 0.0022, "AA_Laton", 6)                           # tirador de anilla
            b.cyl((x, fy - 0.0135, z - rh * 0.12), 0.007, 0.002, "y", "AA_Laton", 12, 0.0)
    return b.finish("CardCatalog")


def make_archive_cart():
    """Carro de archivo: bastidor de tubo, tres bandejas con borde, ruedas giratorias, asa, archivadores y una caja abierta."""
    import random
    rng = random.Random(13)
    b = Builder()
    L, D = 0.9, 0.45
    st = "AA_PinturaGris"
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (L / 2 - 0.02), sy * (D / 2 - 0.02)
            b.cyl((x, y, 0.5), 0.0125, 0.86, "z", st, 14, 0.0)
            b.box((x, y, 0.072), (0.04, 0.04, 0.006), "AA_Acero", 0.002, seg=1)
            b.cyl((x, y - 0.012, 0.035), 0.033, 0.022, "x", "AA_Goma", 20, 0.006)
            b.box((x, y - 0.006, 0.058), (0.03, 0.03, 0.02), "AA_Acero", 0.004)
    for z in (0.15, 0.5, 0.92):
        b.box((0, 0, z), (L, D, 0.004), st, 0.001, seg=1)
        for sy in (-1, 1):
            b.box((0, sy * (D / 2 - 0.002), z + 0.02), (L, 0.004, 0.04), st, 0.0012, seg=1)
        for sx in (-1, 1):
            b.box((sx * (L / 2 - 0.002), 0, z + 0.02), (0.004, D, 0.04), st, 0.0012, seg=1)
    for z in (0.152, 0.502):
        x = -L / 2 + 0.06
        x = aa_binders(b, x, z, 0.28, 0.3, 10, rng.choice(BINDER_MATS), rng, L / 2 - 0.05)
    aa_archive_box(b, (0.1, 0, 0.922), 0.4, 0.32, 0.27, rng, lid=False, rot=8)
    b.sweep([(0.42, -0.2, 0.92), (0.47, -0.25, 1.0), (0.47, -0.25, 1.0)], 0.011, st, 10)
    b.sweep([(0.47, -0.25, 1.0), (0.47, 0.2, 1.0)], 0.012, "AA_Goma", 10)
    b.sweep([(0.42, 0.2, 0.92), (0.47, 0.2, 1.0)], 0.011, st, 10)
    return b.finish("ArchiveCart")


# ------------------------------------------------------------------ sotano (sustituye a build_basement.py / build_basement_v2.py; mismas medidas, frente -Y)
def aa_gauge(b, c, r, face=-1, depth=0.035, rng=None):
    """Manometro mirando a -Y: caja con aro biselado, esfera con marcas, zona roja, aguja con eje y cristal."""
    y = c[1]
    b.cyl((c[0], y + face * depth / 2, c[2]), r, depth, "y", "AA_Acero", 32, 0.004)
    b.cyl((c[0], y + face * depth, c[2]), r * 0.88, 0.003, "y", "AA_Esfera", 32, 0.0)
    for k in range(13):                                                                         # marcas de 270 grados
        a = math.radians(225 - k * 22.5)
        L = r * (0.16 if k % 3 == 0 else 0.09)
        rr = r * 0.78
        b.box((c[0] + math.cos(a) * (rr - L / 2), y + face * (depth + 0.0018), c[2] + math.sin(a) * (rr - L / 2)), (L, 0.0006, r * 0.025), "AA_Negro", 0.0,
              rot=(0, -math.degrees(a), 0))
    for k in range(3):                                                                          # sector rojo
        a = math.radians(-20 - k * 10)
        b.box((c[0] + math.cos(a) * r * 0.7, y + face * (depth + 0.0016), c[2] + math.sin(a) * r * 0.7), (r * 0.08, 0.0006, r * 0.12), "AA_PlasticoRojo", 0.0,
              rot=(0, -math.degrees(a), 0))
    a = math.radians((rng.uniform(40, 160)) if rng else 100)
    b.box((c[0] + math.cos(a) * r * 0.3, y + face * (depth + 0.003), c[2] + math.sin(a) * r * 0.3), (r * 0.7, 0.001, r * 0.035), "AA_Negro", 0.0,
          rot=(0, -math.degrees(a), 0))
    b.cyl((c[0], y + face * (depth + 0.003), c[2]), r * 0.07, 0.003, "y", "AA_Negro", 12, 0.0)
    ring = [(c[0] + r * 0.95 * math.cos(2 * math.pi * k / 32), y + face * (depth + 0.001), c[2] + r * 0.95 * math.sin(2 * math.pi * k / 32)) for k in range(33)]
    b.sweep(ring, r * 0.06, "AA_Acero", 8)
    b.cyl((c[0], y - face * 0.01, c[2] - r - 0.015), r * 0.15, 0.04, "z", "AA_Laton", 12, 0.002)   # racor


def aa_wheel(b, c, r, mat="AA_PinturaRoja", face=-1):
    """Volante de valvula en el plano XZ: llanta toroidal ondulada para el agarre, tres radios curvos, cubo y tuerca."""
    n = 48
    ring = [(c[0] + (r + 0.004 * math.sin(k * 6 * 2 * math.pi / n)) * math.cos(2 * math.pi * k / n), c[1],
             c[2] + (r + 0.004 * math.sin(k * 6 * 2 * math.pi / n)) * math.sin(2 * math.pi * k / n)) for k in range(n + 1)]
    b.sweep(ring, r * 0.09, mat, 10)
    for k in range(3):
        a = 2 * math.pi * k / 3 + 0.3
        b.sweep([(c[0] + 0.03 * math.cos(a), c[1] - face * 0.01, c[2] + 0.03 * math.sin(a)),
                 (c[0] + r * 0.55 * math.cos(a + 0.2), c[1], c[2] + r * 0.55 * math.sin(a + 0.2)),
                 (c[0] + r * 0.95 * math.cos(a + 0.12), c[1], c[2] + r * 0.95 * math.sin(a + 0.12))], r * 0.06, mat, 8)
    b.cyl((c[0], c[1], c[2]), 0.035, 0.045, "y", mat, 18, 0.006)
    b.cyl((c[0], c[1] + face * 0.03, c[2]), 0.016, 0.022, "y", "AA_Acero", 6, 0.002)               # tuerca hexagonal
    b.cyl((c[0], c[1] - face * 0.05, c[2]), 0.012, 0.08, "y", "AA_Acero", 12, 0.0)                 # vastago


def aa_flange(b, c, r, axis="z", bolts=8, mat="AA_Hierro"):
    """Brida con pernos y tuercas alrededor de una tuberia."""
    b.cyl(c, r * 1.6, 0.03, axis, mat, 32, 0.004)
    for k in range(bolts):
        a = 2 * math.pi * (k + 0.5) / bolts
        ox, oy = r * 1.35 * math.cos(a), r * 1.35 * math.sin(a)
        p = {"z": (c[0] + ox, c[1] + oy, c[2]), "x": (c[0], c[1] + ox, c[2] + oy), "y": (c[0] + ox, c[1], c[2] + oy)}[axis]
        b.cyl(p, r * 0.13 + 0.004, 0.05, axis, "AA_Acero", 6, 0.001)


def aa_pipe(b, pts, r, mat="AA_Oxido"):
    b.sweep(pts, r, mat, 20)


def aa_rivets(b, pts, r=0.008, face="y", mat="AA_Hierro"):
    for p in pts:
        b.cyl(p, r, r * 0.8, face, mat, 8, r * 0.35)


def make_boiler():
    """Caldera industrial horizontal: cuerpo con virolas remachadas y anillos, tapas abombadas, hogar con puerta, mirilla y bisagras, manometros, nivel de
    agua con cristal, valvula de seguridad, tubos al techo con bridas, cunas y bancada, escalerilla y placa del fabricante."""
    import random
    rng = random.Random(2)
    b = Builder()
    L, R0, zc = 4.6, 1.15, 1.55
    body = "AA_PinturaGris"
    segs = 48
    secs = [(-L / 2 - 0.25, 0.35), (-L / 2 - 0.2, 0.7), (-L / 2 - 0.12, 0.95), (-L / 2 - 0.04, 1.1), (-L / 2, R0), (L / 2, R0), (L / 2 + 0.04, 1.1),
            (L / 2 + 0.12, 0.95), (L / 2 + 0.2, 0.7), (L / 2 + 0.25, 0.35)]
    bm = bmesh.new()
    rings = [[bm.verts.new((x, r * math.cos(2 * math.pi * k / segs), zc + r * math.sin(2 * math.pi * k / segs))) for k in range(segs)] for x, r in secs]
    for a_, c_ in zip(rings[:-1], rings[1:]):
        for k in range(segs):
            bm.faces.new((a_[k], a_[(k + 1) % segs], c_[(k + 1) % segs], c_[k]))
    bm.faces.new(rings[0])
    bm.faces.new(list(reversed(rings[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    b.parts.append(Part(bm, body, "x"))
    for x in (-1.5, -0.5, 0.5, 1.5):                                                            # juntas de virola con remaches
        ring = [(x, (R0 + 0.012) * math.cos(2 * math.pi * k / 64), zc + (R0 + 0.012) * math.sin(2 * math.pi * k / 64)) for k in range(65)]
        b.sweep(ring, 0.022, "AA_Hierro", 8)
        for k in range(40):
            a = 2 * math.pi * k / 40
            b.cyl((x + 0.045, (R0 + 0.006) * math.cos(a), zc + (R0 + 0.006) * math.sin(a)), 0.011, 0.012, "x", "AA_Hierro", 8, 0.004)
    # hogar en el frente (-Y)
    fy = -R0
    b.box((-0.6, fy - 0.02, zc - 0.25), (1.0, 0.14, 0.82), "AA_Hierro", 0.02, seg=3)
    b.box((-0.6, fy - 0.1, zc - 0.25), (0.82, 0.045, 0.62), "AA_Oxido", 0.012, seg=3)
    b.cyl((-0.6, fy - 0.13, zc - 0.1), 0.07, 0.02, "y", "AA_Hierro", 24, 0.004)                     # mirilla
    b.cyl((-0.6, fy - 0.14, zc - 0.1), 0.05, 0.005, "y", "AA_LuzFuego", 24, 0.0)
    for dz in (-0.2, 0.15):
        b.cyl((-1.02, fy - 0.11, zc - 0.25 + dz), 0.03, 0.12, "z", "AA_Hierro", 14, 0.004)            # bisagras
        b.box((-0.92, fy - 0.11, zc - 0.25 + dz), (0.2, 0.025, 0.05), "AA_Hierro", 0.005)
    b.box((-0.2, fy - 0.14, zc - 0.22), (0.2, 0.03, 0.05), "AA_Acero", 0.008)                     # pestillo
    b.sweep([(-0.12, fy - 0.15, zc - 0.22), (-0.06, fy - 0.2, zc - 0.22), (0.05, fy - 0.2, zc - 0.22)], 0.012, "AA_Acero", 10)
    for i, x in enumerate((0.5, 0.85, 1.2)):
        aa_pipe(b, [(x, fy + 0.05, zc + 0.1), (x, fy - 0.05, zc + 0.12), (x, fy - 0.06, zc + 0.22)], 0.012, "AA_Laton")
        aa_gauge(b, (x, fy - 0.07, zc + 0.35), 0.11, rng=rng)
    b.cyl((1.6, fy - 0.06, zc - 0.1), 0.025, 1.0, "z", "AA_Cristal", 16, 0.0)                     # nivel de agua
    for z in (zc - 0.62, zc + 0.42):
        b.box((1.6, fy - 0.04, z), (0.1, 0.12, 0.08), "AA_Laton", 0.01)
    b.box((1.6, fy - 0.025, zc - 0.25), (0.06, 0.01, 0.22), "AA_Agua", 0.0)
    b.box((0.85, fy - 0.03, zc - 0.55), (0.42, 0.012, 0.22), "AA_Laton", 0.004)                   # placa del fabricante
    for k in range(4):
        b.box((0.85, fy - 0.037, zc - 0.5 - k * 0.035), (0.34 - k * 0.04, 0.002, 0.012), "AA_Negro", 0.0)
    # cunas y bancada
    for x in (-1.5, 0, 1.5):
        b.box((x, 0, 0.32), (0.3, 1.9, 0.5), "AA_Hierro", 0.012)
        b.box((x, 0, 0.05), (0.4, 2.1, 0.06), "AA_Hierro", 0.008)
        for sy in (-1, 1):
            b.cyl((x + 0.15, sy * 0.85, 0.09), 0.025, 0.04, "z", "AA_Acero", 6, 0.003)
    b.box((0, 0, 0.02), (L + 0.4, 2.4, 0.04), "AA_Hormigon", 0.01)
    # tubos al techo con bridas y una valvula
    for x, r in ((-1.4, 0.16), (0.2, 0.12), (1.5, 0.2)):
        aa_pipe(b, [(x, 0.3, zc + R0 - 0.1), (x, 0.3, 5.4)], r)
        aa_flange(b, (x, 0.3, zc + R0 + 0.3), r)
        aa_flange(b, (x, 0.3, 4.2), r)
    aa_wheel(b, (0.2, 0.3 - 0.22, zc + R0 + 0.65), 0.17)
    b.box((0.2, 0.3, zc + R0 + 0.65), (0.3, 0.3, 0.3), "AA_Hierro", 0.03)
    b.cyl((1.0, 0.0, zc + R0 + 0.15), 0.06, 0.3, "z", "AA_Laton", 18, 0.01)                         # valvula de seguridad
    b.cyl((1.0, 0.0, zc + R0 + 0.33), 0.035, 0.08, "z", "AA_Laton", 14, 0.006)
    b.sweep([(1.0, 0.0, zc + R0 + 0.36), (1.0, 0.0, zc + R0 + 0.42), (1.18, 0.0, zc + R0 + 0.42)], 0.008, "AA_Acero", 8)
    # tuberia lateral hasta el suelo con codo
    aa_pipe(b, [(2.1, -0.6, 0.1), (2.1, -0.6, zc - 0.1), (2.1, -0.6, zc), (2.0, -0.6, zc + 0.05), (1.9, -0.6, zc + 0.05)], 0.1)
    aa_flange(b, (2.1, -0.6, 0.6), 0.1)
    # escalerilla
    for sx in (-1, 1):
        b.cyl((-2.0 + sx * 0.22, -R0 - 0.35, 1.4), 0.02, 2.8, "z", "AA_Acero", 14, 0.0)
    for k in range(8):
        b.cyl((-2.0, -R0 - 0.35, 0.3 + k * 0.32), 0.014, 0.44, "x", "AA_Acero", 12, 0.0)
    for z in (0.6, 2.4):
        b.box((-2.0, -R0 - 0.2, z), (0.04, 0.3, 0.03), "AA_Acero", 0.005)
    return b.finish("Boiler")


def make_water_pump():
    """Bomba centrifuga: bancada con pernos, motor electrico con aletas, caja de bornas y placa, acoplamiento con proteccion, voluta, tuberias con bridas,
    valvula y manometro."""
    import random
    rng = random.Random(4)
    b = Builder()
    blue = "AA_PinturaAzul"
    b.box((0, 0, 0.08), (1.7, 0.7, 0.16), "AA_Hierro", 0.012, seg=2)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.cyl((sx * 0.78, sy * 0.3, 0.17), 0.018, 0.03, "z", "AA_Acero", 6, 0.003)
    b.cyl((-0.35, 0, 0.45), 0.25, 0.8, "x", blue, 40, 0.02)                                      # motor
    for k in range(20):                                                                        # aletas
        a = 2 * math.pi * k / 20
        if math.sin(a) < -0.6:
            continue
        b.box((-0.35, 0.272 * math.cos(a), 0.45 + 0.272 * math.sin(a)), (0.66, 0.008, 0.045), blue, 0.002, seg=1, rot=(math.degrees(a) + 90, 0, 0))
    b.cyl((-0.78, 0, 0.45), 0.2, 0.06, "x", blue, 32, 0.015)                                     # tapa del ventilador
    for k in range(10):
        b.box((-0.812, 0, 0.3 + k * 0.03), (0.004, 0.3, 0.012), "AA_Negro", 0.0)
    b.box((-0.35, 0, 0.75), (0.25, 0.2, 0.12), blue, 0.015)
    b.box((-0.35, -0.101, 0.73), (0.12, 0.002, 0.06), "AA_Acero", 0.0)                          # placa
    b.sweep([(-0.25, 0.0, 0.8), (-0.1, 0.05, 0.9), (0.1, 0.25, 0.9)], 0.012, "AA_Negro", 8)     # cable
    for sx in (-1, 1):
        b.box((-0.35 + sx * 0.25, 0, 0.21), (0.08, 0.5, 0.04), blue, 0.008)                    # patas del motor
    b.cyl((0.16, 0, 0.45), 0.1, 0.14, "x", "AA_PinturaAmarilla", 24, 0.01)                         # proteccion del acoplamiento
    b.ellipsoid((0.5, 0, 0.45), (0.26, 0.22, 0.3), "AA_PinturaRoja", 24, 12) if hasattr(b, "ellipsoid") else b.cyl((0.5, 0, 0.45), 0.28, 0.22, "y", "AA_PinturaRoja", 32, 0.04)
    b.box((0.5, 0, 0.2), (0.3, 0.3, 0.1), "AA_Hierro", 0.01)
    aa_pipe(b, [(0.5, 0, 0.7), (0.5, 0, 1.5)], 0.09)
    aa_flange(b, (0.5, 0, 0.76), 0.09)
    aa_flange(b, (0.5, 0, 1.3), 0.09)
    b.box((0.5, 0, 1.05), (0.24, 0.24, 0.22), "AA_Hierro", 0.02)
    aa_wheel(b, (0.5, -0.17, 1.05), 0.13)
    aa_gauge(b, (0.5, -0.1, 1.4), 0.07, rng=rng)
    aa_pipe(b, [(0.72, 0, 0.42), (0.95, 0, 0.42), (1.05, 0, 0.36), (1.06, 0, 0.0)], 0.09)        # aspiracion con codo al suelo
    aa_flange(b, (0.8, 0, 0.42), 0.09, "x")
    return b.finish("WaterPump")


def make_generator():
    """Grupo electrogeno insonorizado: bancada con antivibratorios, carcasa amarilla con puertas, bisagras y cerraduras, rejillas de lamas, cuadro con
    instrumentos y pulsadores, escape con silenciador, tapon de deposito, cancamos y franja de aviso."""
    import random
    rng = random.Random(6)
    b = Builder()
    yel = "AA_PinturaAmarilla"
    b.box((0, 0, 0.08), (2.7, 1.15, 0.16), "AA_Hierro", 0.012, seg=2)
    for sx in (-1, 1):
        for x in (0.4, 1.1):
            b.cyl((sx * x, 0, 0.19), 0.06, 0.06, "z", "AA_Goma", 16, 0.01)
    b.box((0, 0, 0.94), (2.5, 1.05, 1.44), yel, 0.025, seg=3)
    for x0 in (-0.85, 0.85):                                                                   # puertas laterales con lamas
        b.box((x0, -0.53, 0.94), (0.72, 0.012, 1.2), yel, 0.006, seg=2)
        for k in range(14):
            b.box((x0, -0.543, 0.5 + k * 0.065), (0.6, 0.02, 0.012), yel, 0.003, seg=1, rot=(-30, 0, 0))
            b.box((x0, -0.537, 0.5 + k * 0.065 - 0.012), (0.6, 0.002, 0.018), "AA_Negro", 0.0)
        for z in (0.5, 1.38):
            b.cyl((x0 - 0.37, -0.54, z), 0.012, 0.08, "z", "AA_Acero", 10, 0.002)
        b.cyl((x0 + 0.3, -0.54, 0.94), 0.018, 0.01, "y", "AA_Acero", 14, 0.002)
    b.box((0, -0.536, 0.95), (0.62, 0.02, 0.56), "AA_Negro", 0.006)                            # cuadro
    for i, x in enumerate((-0.18, 0, 0.18)):
        aa_gauge(b, (x, -0.546, 1.1), 0.065, depth=0.02, rng=rng)
    for x, mat in ((-0.18, "AA_PlasticoRojo"), (-0.06, "AA_PinturaVerde"), (0.06, "AA_Plastico"), (0.18, "AA_PlasticoAmarillo")):
        b.cyl((x, -0.556, 0.84), 0.022, 0.02, "y", mat, 16, 0.004)
    b.box((0, -0.548, 0.73), (0.4, 0.002, 0.05), "AA_Papel", 0.0)
    for k in range(8):                                                                         # franja de aviso
        b.box((-1.2 + k * 0.1 + 0.05, -0.531, 0.25), (0.05, 0.002, 0.08), "AA_Negro", 0.0, rot=(0, 30, 0))
    b.box((0, -0.53, 0.25), (2.4, 0.001, 0.08), "AA_PlasticoAmarillo", 0.0)
    b.cyl((0.9, 0.2, 1.9), 0.08, 0.6, "z", "AA_Oxido", 20, 0.0)                                  # escape y silenciador
    b.cyl((0.9, 0.2, 1.75), 0.14, 0.3, "z", "AA_Oxido", 24, 0.02)
    b.sweep([(0.9, 0.2, 2.2), (0.9, 0.2, 2.25), (0.97, 0.2, 2.28)], 0.08, "AA_Oxido", 16)
    b.cyl((-0.9, 0.3, 1.69), 0.08, 0.05, "z", "AA_Plastico", 20, 0.01)                             # tapon del deposito
    for sx in (-1, 1):
        b.sweep([(sx * 1.1 - 0.06, 0, 1.66), (sx * 1.1 - 0.06, 0, 1.73), (sx * 1.1 + 0.06, 0, 1.73), (sx * 1.1 + 0.06, 0, 1.66)], 0.012, "AA_Acero", 8)
    return b.finish("Generator")


def make_control_panel():
    """Fila de armarios electricos: zocalo, cuatro puertas con junta y bisagras, maneta de cuarto de vuelta, voltimetros, pilotos, pulsadores, selector,
    rotulos, rejilla de ventilacion, triangulo de riesgo electrico y bandejas de cable al techo con cables."""
    import random
    rng = random.Random(3)
    b = Builder()
    L, D, H = 3.0, 0.6, 2.2
    gray = "AA_PinturaGris"
    b.box((0, 0, 0.05), (L, D, 0.1), "AA_Negro", 0.008)
    b.box((0, 0, H / 2 + 0.05), (L, D, H - 0.1), gray, 0.012, seg=2)
    b.box((0, 0, H + 0.03), (L + 0.04, D + 0.04, 0.06), gray, 0.01, seg=2)
    for i in range(4):
        x = -L / 2 + 0.375 + i * 0.75
        fy = -D / 2
        b.box((x, fy - 0.012, 1.1), (0.7, 0.022, 1.9), gray, 0.008, seg=2)
        b.frame_profile((x - 0.34, x + 0.34, 0.16, 2.04), [(0.0, 0.0), (0.0, 0.004), (0.004, 0.004)], fy - 0.0, -1, "AA_Negro")    # junta
        for z in (0.4, 1.8):
            b.cyl((x - 0.36, fy - 0.02, z), 0.012, 0.09, "z", "AA_Acero", 12, 0.002)
        b.cyl((x + 0.28, fy - 0.03, 1.1), 0.022, 0.02, "y", "AA_Negro", 16, 0.004)                   # maneta
        b.box((x + 0.28, fy - 0.045, 1.1), (0.012, 0.012, 0.05), "AA_Negro", 0.003)
        for j in range(2):
            aa_gauge(b, (x - 0.12 + j * 0.24, fy - 0.022, 1.72), 0.07, depth=0.025, rng=rng)
        for j in range(4):
            col = rng.choice(("AA_PlasticoRojo", "AA_PinturaVerde", "AA_PlasticoAmarillo", "AA_Plastico"))
            b.cyl((x - 0.18 + j * 0.12, fy - 0.03, 1.42), 0.022, 0.02, "y", "AA_Acero", 16, 0.003)
            b.cyl((x - 0.18 + j * 0.12, fy - 0.042, 1.42), 0.017, 0.008, "y", col, 16, 0.003)
            b.box((x - 0.18 + j * 0.12, fy - 0.0235, 1.36), (0.06, 0.001, 0.014), "AA_Papel", 0.0)
        b.cyl((x, fy - 0.03, 1.22), 0.03, 0.02, "y", "AA_Negro", 16, 0.004)                         # selector
        b.box((x, fy - 0.045, 1.22), (0.01, 0.01, 0.05), "AA_Negro", 0.003, rot=(0, 30, 0))
        b.box((x, fy - 0.024, 0.95), (0.4, 0.002, 0.12), "AA_Papel", 0.0)                           # rotulo
        for k in range(2):
            b.box((x - 0.05, fy - 0.026, 0.97 - k * 0.04), (0.25, 0.0006, 0.012), "AA_Tinta", 0.0)
        tri = [(x, fy - 0.025, 1.98), (x - 0.06, fy - 0.025, 1.88), (x + 0.06, fy - 0.025, 1.88)]
        b.box((x, fy - 0.024, 1.91), (0.08, 0.001, 0.07), "AA_PlasticoAmarillo", 0.0)
        for k in range(8):
            b.box((x, fy - 0.024, 0.32 + k * 0.035), (0.5, 0.018, 0.012), gray, 0.003, seg=1, rot=(-30, 0, 0))
            b.box((x, fy - 0.021, 0.32 + k * 0.035 - 0.01), (0.5, 0.002, 0.016), "AA_Negro", 0.0)
    for x in (-1.2, -0.3, 0.8):                                                                 # bandejas de cable al techo
        b.box((x, 0.1, H + 0.6), (0.15, 0.12, 1.2), "AA_Acero", 0.004, seg=1, grain="z")
        for k in range(4):
            b.sweep([(x - 0.045 + k * 0.03, 0.06, H + 0.06), (x - 0.045 + k * 0.03, 0.06, H + 1.2)], 0.008, rng.choice(("AA_Negro", "AA_PlasticoRojo", "AA_Plastico")), 6)
    return b.finish("ControlPanel")


def _bottle(b, x, y, z, h, r, mat, rng):
    """Frasco de laboratorio con cuello, tapon y etiqueta."""
    bm = bmesh.new()
    prof = [(r, 0), (r, h * 0.7), (r * 0.6, h * 0.8), (r * 0.4, h * 0.86), (r * 0.4, h)]
    n = 16
    rings = [[bm.verts.new((x + rr * math.cos(2 * math.pi * k / n), y + rr * math.sin(2 * math.pi * k / n), z + zz)) for k in range(n)] for rr, zz in prof]
    for a_, c_ in zip(rings[:-1], rings[1:]):
        for k in range(n):
            bm.faces.new((a_[k], a_[(k + 1) % n], c_[(k + 1) % n], c_[k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    b.parts.append(Part(bm, mat, "z"))
    b.cyl((x, y, z + h + 0.008), r * 0.45, 0.018, "z", rng.choice(("AA_Plastico", "AA_PlasticoRojo", "AA_PlasticoAzul")), 12, 0.003)
    if rng.random() < 0.7:
        b.box((x, y - r - 0.0008, z + h * 0.38), (r * 1.1, 0.0015, h * 0.32), "AA_Papel", 0.0)


def make_lab_bench():
    """Mesa de laboratorio: armarios con puertas y tiradores, encimera de resina negra con canto, balda alta sobre montantes con frascos, microscopio,
    gradilla con tubos de ensayo, matraces, mechero y papeles."""
    import random
    rng = random.Random(5)
    b = Builder()
    L, D = 2.4, 0.8
    white = "AA_PinturaCrema"
    b.box((0, 0.0, 0.45), (L - 0.1, D - 0.08, 0.86), white, 0.01)
    b.box((0, -0.0, 0.04), (L - 0.14, D - 0.14, 0.08), "AA_Negro", 0.006)
    for i in range(4):
        x = -L / 2 + 0.33 + i * 0.58
        b.box((x, -D / 2 + 0.03, 0.48), (0.54, 0.02, 0.74), white, 0.005, seg=2)
        b.sweep([(x + 0.2, -D / 2 + 0.02, 0.7), (x + 0.2, -D / 2 - 0.0, 0.7), (x + 0.2, -D / 2 - 0.0, 0.82), (x + 0.2, -D / 2 + 0.02, 0.82)], 0.006, "AA_Acero", 8)
    b.box((0, 0, 0.9), (L, D, 0.035), "AA_Negro", 0.008, seg=3)
    for sx in (-1, 1):
        b.box((sx * (L / 2 - 0.05), 0.3, 1.35), (0.035, 0.035, 0.9), "AA_Acero", 0.006, grain="z")
    b.box((0, 0.3, 1.6), (L, 0.25, 0.02), "AA_Cristal", 0.003)
    b.box((0, 0.3, 1.6), (L, 0.02, 0.03), "AA_Acero", 0.004)
    x = -L / 2 + 0.1
    while x < L / 2 - 0.1:
        _bottle(b, x, 0.3, 1.61, rng.uniform(0.12, 0.24), rng.uniform(0.035, 0.05), rng.choice(("AA_Cristal", "AA_VidrioAmbar", "AA_VidrioAmbar", "AA_PinturaCrema")), rng)
        x += rng.uniform(0.1, 0.16)
    # microscopio
    b.box((-0.6, -0.05, 0.935), (0.16, 0.22, 0.03), "AA_PinturaCrema", 0.008)
    b.sweep([(-0.6, 0.05, 0.94), (-0.6, 0.06, 1.12), (-0.6, 0.0, 1.22)], 0.025, "AA_PinturaCrema", 12)
    b.box((-0.6, -0.04, 1.04), (0.12, 0.1, 0.012), "AA_Negro", 0.003)
    b.cyl((-0.6, -0.03, 1.2), 0.022, 0.16, "z", "AA_Negro", 16, 0.004)
    b.cyl((-0.6, 0.0, 1.31), 0.014, 0.1, "y", "AA_Negro", 14, 0.003)
    b.cyl((-0.57, 0.06, 1.1), 0.03, 0.02, "x", "AA_Cromo", 16, 0.004)
    # gradilla con tubos
    b.box((0.3, -0.1, 0.97), (0.32, 0.08, 0.06), "AA_Madera", 0.004)
    for k in range(6):
        b.cyl((0.17 + k * 0.052, -0.1, 1.0), 0.009, 0.15, "z", rng.choice(("AA_Cristal", "AA_VidrioAmbar")), 12, 0.0)
    for (x, y) in ((0.75, 0.05), (0.95, -0.12)):                                               # matraces
        bm_r = [(0.07, 0), (0.07, 0.02), (0.05, 0.08), (0.02, 0.12), (0.02, 0.2)]
        _bottle(b, x, y, 0.92, 0.2, 0.065, "AA_Cristal", rng)
    b.cyl((1.05, 0.15, 0.95), 0.03, 0.04, "z", "AA_Acero", 16, 0.004)                           # mechero
    b.cyl((1.05, 0.15, 1.02), 0.008, 0.1, "z", "AA_Laton", 10, 0.0)
    b.sweep([(1.05, 0.18, 0.93), (1.1, 0.35, 0.92), (1.0, 0.39, 0.92)], 0.005, "AA_Goma", 6)
    _sheet(b, (-0.1, 0.15, 0.919), 12, rng)
    _sheet(b, (0.0, 0.2, 0.92), -20, rng)
    return b.finish("LabBench")


def make_lab_cabinet():
    """Vitrina de laboratorio: cuerpo con zocalo, baldas de cristal, puertas de cristal con marco, tiradores y cerradura, armario bajo con puertas, frascos."""
    import random
    rng = random.Random(8)
    b = Builder()
    W, D, H = 1.0, 0.45, 1.9
    white = "AA_PinturaCrema"
    b.box((0, 0, 0.36), (W, D, 0.72), white, 0.008)
    b.box((0, 0.01, 0.035), (W - 0.04, D - 0.06, 0.07), "AA_Negro", 0.005)
    for sx in (-1, 1):
        b.box((sx * (W / 2 - 0.02), 0, 1.3), (0.04, D, 1.2), white, 0.006, grain="z")
    b.box((0, D / 2 - 0.01, 1.3), (W, 0.02, 1.2), white, 0.004)
    b.box((0, 0, H - 0.02), (W + 0.02, D + 0.02, 0.04), white, 0.008)
    for z in (1.0, 1.4):
        b.box((0, 0, z), (W - 0.06, D - 0.04, 0.008), "AA_Cristal", 0.002)
    for z in (0.725, 1.006, 1.406):
        x = -W / 2 + 0.1
        while x < W / 2 - 0.08:
            _bottle(b, x, rng.uniform(-0.06, 0.06), z, rng.uniform(0.1, 0.22), rng.uniform(0.03, 0.045),
                    rng.choice(("AA_Cristal", "AA_VidrioAmbar", "AA_PinturaCrema", "AA_VidrioAmbar")), rng)
            x += rng.uniform(0.09, 0.14)
    for sx in (-1, 1):
        cx = sx * W / 4
        b.box((cx, -D / 2 - 0.004, 1.3), (W / 2 - 0.03, 0.006, 1.16), "AA_Cristal", 0.0)
        b.frame_profile((cx - W / 4 + 0.03, cx + W / 4 - 0.03, 0.73, 1.87), [(0.0, 0.0), (0.0, 0.012), (0.02, 0.012), (0.02, 0.0)], -D / 2 - 0.0, -1, white)
        b.sweep([(sx * 0.04, -D / 2 - 0.01, 1.22), (sx * 0.04, -D / 2 - 0.025, 1.22), (sx * 0.04, -D / 2 - 0.025, 1.38), (sx * 0.04, -D / 2 - 0.01, 1.38)], 0.006, "AA_Acero", 8)
        b.box((cx, -D / 2 - 0.01, 0.37), (W / 2 - 0.03, 0.02, 0.62), white, 0.005, seg=2)
        b.cyl((sx * 0.06, -D / 2 - 0.025, 0.6), 0.012, 0.025, "y", "AA_Acero", 14, 0.003)
    b.cyl((0.0, -D / 2 - 0.012, 1.5), 0.01, 0.01, "y", "AA_Laton", 12, 0.002)
    return b.finish("LabCabinet")


def make_metal_rack():
    """Estanteria de carga: puntales azules perforados con placas base, largueros naranjas con conectores, entablado de madera, cajas, bidones y garrafas."""
    import random
    rng = random.Random(11)
    b = Builder()
    L, D, H = 2.4, 0.8, 2.4
    blue, orange = "AA_PinturaAzul", "AA_PinturaNaranja"
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (L / 2 - 0.04), sy * (D / 2 - 0.04)
            b.box((x, y, H / 2), (0.08, 0.06, H), blue, 0.006, grain="z")
            for k in range(int(H / 0.05)):
                b.box((x, y - sy * 0.0302, 0.03 + k * 0.05), (0.012, 0.0008, 0.03), "AA_Negro", 0.0)
            b.box((x, y, 0.005), (0.14, 0.12, 0.01), blue, 0.003)
            for k in range(2):
                b.cyl((x + (k - 0.5) * 0.08, y, 0.012), 0.008, 0.008, "z", "AA_Acero", 6, 0.002)
        for k in range(6):                                                                    # diagonales del bastidor lateral
            z0 = 0.2 + k * 0.38
            ang = math.degrees(math.atan2(D - 0.1, 0.38)) * (1 if k % 2 else -1)
            b.box((sx * (L / 2 - 0.04), 0, z0 + 0.19), (0.03, 0.02, math.hypot(D - 0.1, 0.38)), blue, 0.004, rot=(ang, 0, 0))
    for z in (0.15, 1.05, 1.95):
        for sy in (-1, 1):
            b.box((0, sy * (D / 2 - 0.04), z), (L - 0.16, 0.05, 0.1), orange, 0.006)
            for sx in (-1, 1):
                b.box((sx * (L / 2 - 0.11), sy * (D / 2 - 0.04), z), (0.06, 0.055, 0.12), orange, 0.006)
        for k in range(6):
            b.box((-L / 2 + 0.2 + k * (L - 0.4) / 5, 0, z + 0.065), ((L - 0.4) / 5 - 0.01, D - 0.12, 0.022), "AA_Madera", 0.003, seg=1)
    for z in (0.226, 1.126, 2.026):
        x = -L / 2 + 0.15
        while x < L / 2 - 0.3:
            r = rng.random()
            if r < 0.45:
                w = rng.uniform(0.35, 0.55)
                h = rng.uniform(0.25, 0.5)
                bb = b.box((x + w / 2, rng.uniform(-0.05, 0.05), z + h / 2), (w, 0.55, h), rng.choice(("AA_Carton", "AA_CartonOscuro")), 0.006, seg=2,
                           rot=(0, 0, rng.uniform(-6, 6)))
                b.box((x + w / 2, -0.277, z + h * 0.9), (w * 0.95, 0.002, 0.04), "AA_CintaAdhesiva", 0.0)
                b.box((x + w / 2, -0.277, z + h * 0.5), (w * 0.4, 0.002, h * 0.2), "AA_Papel", 0.0)
                x += w + 0.04
            elif r < 0.7:
                drum(b, (x + 0.2, 0, z), 0.19, 0.62, rng.choice(("AA_Oxido", "AA_PinturaAzul", "AA_PinturaRoja")))
                x += 0.43
            elif r < 0.9:
                for k in range(2):
                    jerry(b, (x + 0.1 + k * 0.18, 0, z), rng.choice(("AA_PinturaCrema", "AA_PlasticoAzul")), rng.uniform(-8, 8))
                x += 0.4
            else:
                x += 0.35
    return b.finish("MetalRack")


def drum(b, c, r, h, mat):
    """Bidon de 200 l: aros de rodadura, bordes enrollados, tapones."""
    x, y, z = c
    b.cyl((x, y, z + h / 2), r, h - 0.02, "z", mat, 32, 0.0)
    for zz in (0.012, h - 0.012):
        ring = [(x + (r - 0.004) * math.cos(2 * math.pi * k / 40), y + (r - 0.004) * math.sin(2 * math.pi * k / 40), z + zz) for k in range(41)]
        b.sweep(ring, 0.012, mat, 8)
    for zz in (h * 0.33, h * 0.66):
        ring = [(x + (r + 0.004) * math.cos(2 * math.pi * k / 40), y + (r + 0.004) * math.sin(2 * math.pi * k / 40), z + zz) for k in range(41)]
        b.sweep(ring, 0.009, mat, 6)
    b.cyl((x + r * 0.5, y, z + h - 0.008), r * 0.15, 0.012, "z", "AA_Acero", 12, 0.003)
    b.cyl((x - r * 0.55, y, z + h - 0.008), r * 0.08, 0.012, "z", "AA_Acero", 10, 0.002)


def jerry(b, c, mat, rot):
    """Garrafa de plastico con asa y tapon."""
    x, y, z = c
    b.box((x, y, z + 0.17), (0.15, 0.25, 0.32), mat, 0.03, seg=3, rot=(0, 0, rot))
    b.box((x, y + 0.03, z + 0.35), (0.04, 0.12, 0.04), mat, 0.015, seg=2, rot=(0, 0, rot))
    b.cyl((x, y - 0.08, z + 0.35), 0.025, 0.04, "z", "AA_PlasticoRojo", 14, 0.005)


def make_workbench():
    """Banco de trabajo: patas de angular con travesanos, tablero de madera gruesa con canto gastado, balda baja, panel perforado con herramientas (llaves,
    martillo, alicates, sierra, destornilladores), tornillo de banco, caja de herramientas, piezas sueltas y lampara."""
    import random
    rng = random.Random(13)
    b = Builder()
    L, D = 2.0, 0.8
    st = "AA_PinturaGris"
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box((sx * (L / 2 - 0.05), sy * (D / 2 - 0.05), 0.45), (0.06, 0.06, 0.9), st, 0.004, grain="z")
        b.box((sx * (L / 2 - 0.05), 0, 0.2), (0.04, D - 0.1, 0.04), st, 0.004)
    b.box((0, 0, 0.92), (L, D, 0.06), "AA_MaderaCaja", 0.008, seg=3)
    b.box((0, 0, 0.2), (L - 0.1, D - 0.1, 0.025), "AA_MaderaCaja", 0.004)
    b.box((0, D / 2 - 0.02, 1.55), (L, 0.02, 1.2), "AA_CartonClaro", 0.003)                      # panel perforado
    for i in range(24):
        for j in range(12):
            b.cyl((-L / 2 + 0.05 + i * 0.0826, D / 2 - 0.031, 1.0 + j * 0.095), 0.004, 0.002, "y", "AA_Negro", 6, 0.0)
    # herramientas colgadas
    for k in range(5):                                                                           # llaves fijas de mayor a menor
        x = -0.85 + k * 0.06
        h = 0.22 - k * 0.025
        b.box((x, D / 2 - 0.045, 1.6 - h / 2), (0.016, 0.006, h), "AA_Cromo", 0.002, seg=1)
        b.cyl((x, D / 2 - 0.045, 1.6 - h), 0.016, 0.006, "y", "AA_Cromo", 12, 0.002)
    b.box((-0.4, D / 2 - 0.05, 1.5), (0.025, 0.025, 0.3), "AA_Madera", 0.008, seg=2, grain="z")    # martillo
    b.box((-0.4, D / 2 - 0.05, 1.66), (0.12, 0.03, 0.035), "AA_Hierro", 0.006)
    b.sweep([(-0.15, D / 2 - 0.05, 1.7), (-0.16, D / 2 - 0.05, 1.5)], 0.01, "AA_PlasticoRojo", 8)   # alicates
    b.sweep([(-0.11, D / 2 - 0.05, 1.7), (-0.1, D / 2 - 0.05, 1.5)], 0.01, "AA_PlasticoRojo", 8)
    b.box((-0.13, D / 2 - 0.05, 1.75), (0.04, 0.012, 0.08), "AA_Hierro", 0.004)
    b.box((0.25, D / 2 - 0.045, 1.55), (0.45, 0.004, 0.11), "AA_Acero", 0.001, rot=(0, 8, 0))       # sierra
    b.box((0.03, D / 2 - 0.05, 1.56), (0.08, 0.03, 0.1), "AA_Madera", 0.012, seg=2)
    for k in range(4):                                                                           # destornilladores
        x = 0.62 + k * 0.05
        b.cyl((x, D / 2 - 0.05, 1.68), 0.014, 0.1, "z", rng.choice(("AA_PlasticoAmarillo", "AA_PlasticoRojo")), 12, 0.004)
        b.cyl((x, D / 2 - 0.05, 1.55), 0.004, 0.16, "z", "AA_Cromo", 8, 0.0)
    # tornillo de banco
    b.box((-0.75, -0.35, 1.0), (0.24, 0.16, 0.1), "AA_PinturaAzul", 0.012, seg=2)
    b.box((-0.75, -0.42, 1.07), (0.2, 0.04, 0.1), "AA_PinturaAzul", 0.008)
    b.box((-0.75, -0.32, 1.07), (0.2, 0.04, 0.1), "AA_PinturaAzul", 0.008)
    b.cyl((-0.75, -0.5, 1.06), 0.012, 0.16, "y", "AA_Cromo", 12, 0.0)
    b.cyl((-0.75, -0.58, 1.06), 0.008, 0.16, "x", "AA_Cromo", 10, 0.0)
    # caja de herramientas
    b.box((0.5, 0.05, 1.03), (0.45, 0.22, 0.16), "AA_PlasticoRojo", 0.012, seg=2)
    b.box((0.5, 0.05, 1.12), (0.46, 0.23, 0.02), "AA_PlasticoRojo", 0.006)
    b.sweep([(0.38, 0.05, 1.13), (0.4, 0.05, 1.18), (0.6, 0.05, 1.18), (0.62, 0.05, 1.13)], 0.009, "AA_Negro", 8)
    for sx in (-1, 1):
        b.box((0.5 + sx * 0.18, -0.065, 1.08), (0.04, 0.01, 0.025), "AA_Cromo", 0.003)
    for k in range(7):                                                                           # piezas sueltas (tuercas, tornillos, llaves)
        p = (rng.uniform(-0.3, 0.2), rng.uniform(-0.3, 0.1), 0.955)
        if k % 2:
            b.cyl(p, 0.012, 0.01, "z", "AA_Acero", 6, 0.002)
        else:
            b.box((p[0], p[1], 0.954), (rng.uniform(0.08, 0.15), 0.014, 0.008), "AA_Cromo", 0.002, rot=(0, 0, rng.uniform(0, 180)))
    _sheet(b, (0.05, -0.2, 0.952), 25, rng, "AA_PapelAmarillo")
    return b.finish("Workbench")


def make_pipe_valves():
    """Colector de pared: cuatro tuberias verticales con bridas y valvulas de compuerta con volante, colector horizontal con te, manometros, abrazaderas
    a la pared con tacos, etiquetas de fluido y flechas."""
    import random
    rng = random.Random(7)
    b = Builder()
    for x, r in ((-0.7, 0.09), (-0.2, 0.07), (0.3, 0.1), (0.75, 0.06)):
        aa_pipe(b, [(x, 0.1, 0.0), (x, 0.1, 2.8)], r, rng.choice(("AA_Oxido", "AA_PinturaVerde", "AA_PinturaGris")))
        aa_flange(b, (x, 0.1, 0.4), r)
        aa_flange(b, (x, 0.1, 2.2), r)
        b.box((x, 0.1, 1.3), (r * 2.8, r * 2.4, r * 3.0), "AA_Hierro", r * 0.3, seg=3)            # cuerpo de la valvula
        aa_flange(b, (x, 0.1, 1.3 - r * 1.6), r)
        aa_flange(b, (x, 0.1, 1.3 + r * 1.6), r)
        b.cyl((x, 0.1 - r * 1.2 - 0.05, 1.3), 0.02, 0.1, "y", "AA_Acero", 12, 0.0)
        aa_wheel(b, (x, 0.1 - r * 1.2 - 0.11, 1.3), 0.12 + r * 0.3, "AA_PinturaRoja" if x < 0 else "AA_PinturaAmarilla")
        b.box((x, 0.1 - r - 0.002, 0.8), (r * 1.6, 0.003, 0.05), "AA_PlasticoAmarillo", 0.0)        # etiqueta de fluido
        b.box((x, 0.1 - r - 0.004, 0.8), (r * 1.0, 0.002, 0.012), "AA_Negro", 0.0)
        for z in (0.15, 2.6):                                                                     # abrazaderas a la pared
            ring = [(x + (r + 0.006) * math.cos(math.pi * k / 16), 0.1 - (r + 0.006) * math.sin(math.pi * k / 16), z) for k in range(17)]
            b.sweep(ring, 0.006, "AA_Acero", 6)
            b.box((x, 0.21, z), (r * 2.6, 0.02, 0.03), "AA_Acero", 0.004)
    aa_pipe(b, [(-0.95, 0.15, 1.95), (0.95, 0.15, 1.95)], 0.06)
    for x in (-0.45, 0.55):
        aa_pipe(b, [(x, 0.15, 1.95), (x, 0.05, 1.95)], 0.015, "AA_Laton")
        aa_gauge(b, (x, 0.04, 1.95), 0.08, rng=rng)
    for x in (-0.9, 0.9):
        b.box((x, 0.2, 2.5), (0.08, 0.06, 0.08), "AA_Acero", 0.008)
    return b.finish("PipeValves")


def make_barrel():
    """Bidon de 200 l oxidado con aros, bordes enrollados y tapones."""
    b = Builder()
    drum(b, (0, 0, 0), 0.30, 0.925, "AA_Oxido")
    return b.finish("Barrel")


def make_crate():
    """Caja de madera de transporte: tablas con juntas, listones de refuerzo en diagonal, clavos, esquineras metalicas y estarcido."""
    import random
    rng = random.Random(3)
    b = Builder()
    S, H = 1.0, 0.9
    wood = "AA_MaderaCaja"
    for face in range(4):
        rot = face * 90
        ca, sa = math.cos(math.radians(rot)), math.sin(math.radians(rot))

        def at(lx, ly, lz):
            return (lx * ca - ly * sa, lx * sa + ly * ca, lz)
        for k in range(5):                                                                      # tablas
            z = 0.06 + k * (H - 0.12) / 5 + (H - 0.12) / 10
            b.box(at(0, -S / 2 + 0.012, z), _rs((S - 0.06, 0.024, (H - 0.12) / 5 - 0.006), rot), wood, 0.004, seg=2, rot=(0, 0, rot))
        for sx in (-1, 1):                                                                      # listones verticales y diagonal
            b.box(at(sx * (S / 2 - 0.06), -S / 2 - 0.004, H / 2), _rs((0.09, 0.024, H - 0.02), rot), wood, 0.005, seg=2, rot=(0, 0, rot), grain="z")
        for z in (0.05, H - 0.05):
            b.box(at(0, -S / 2 - 0.004, z), _rs((S - 0.21, 0.024, 0.09), rot), wood, 0.005, seg=2, rot=(0, 0, rot))
        diag = math.hypot(S - 0.21, H - 0.2)
        ang = math.degrees(math.atan2(H - 0.2, S - 0.21)) * (1 if face % 2 else -1)
        b.box(at(0, -S / 2 - 0.006, H / 2), (diag, 0.024, 0.08), wood, 0.005, seg=2, rot=(0, ang, rot))
        for sx in (-1, 1):
            for z in (0.05, H - 0.05):
                b.cyl(at(sx * (S / 2 - 0.06), -S / 2 - 0.0165, z), 0.004, 0.002, "y", "AA_Hierro", 6, 0.0) if face in (0, 2) else None
        if face == 0:                                                                            # estarcido
            for k in range(3):
                b.box(at(-0.08, -S / 2 - 0.0005, 0.62 - k * 0.06), (0.3 - k * 0.07, 0.001, 0.028), "AA_Tinta", 0.0, rot=(0, 0, rot))
    b.box((0, 0, H - 0.012), (S, S, 0.024), wood, 0.004, seg=2)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box((sx * (S / 2 - 0.02), sy * (S / 2 - 0.02), H - 0.04), (0.07, 0.07, 0.08), "AA_Hierro", 0.004)
            b.box((sx * (S / 2 - 0.02), sy * (S / 2 - 0.02), 0.04), (0.07, 0.07, 0.08), "AA_Hierro", 0.004)
    return b.finish("Crate")


def _rs(s, rot):
    return s


BUILDERS = {"Locker": make_locker, "FilingCabinet": make_filing_cabinet, "Desk": make_desk, "SwivelChair": make_swivel_chair,
            "ReceptionDesk": make_reception_desk, "SpiralStairAA": make_spiral_stair,
            "CeilingLampAA": make_ceiling_lamp, "DeskComputer": make_desk_computer, "DeskLamp": make_desk_lamp,
            "TrashBin": make_trash_bin, "Chair": make_visitor_chair, "Bookcase": make_bookcase,
            "AuditoriumChair": make_auditorium_chair, "WaitingBench": make_waiting_bench, "VendingMachine": make_vending, "Cot": make_cot,
            "StationSign": make_station_sign,
            "ArchiveShelfA": lambda: aa_archive_shelf("ArchiveShelfA", 1, 0.2), "ArchiveShelfB": lambda: aa_archive_shelf("ArchiveShelfB", 2, 0.5),
            "ArchiveShelfC": lambda: aa_archive_shelf("ArchiveShelfC", 3, 1.0), "ArchiveWallShelf": make_archive_wall_shelf,
            "ArchiveBoxStackA": lambda: aa_box_stack("ArchiveBoxStackA", 4), "ArchiveBoxStackB": lambda: aa_box_stack("ArchiveBoxStackB", 8),
            "PaperScatter": make_paper_scatter, "RollingLadder": make_rolling_ladder, "CardCatalog": make_card_catalog, "ArchiveCart": make_archive_cart,
            "Boiler": make_boiler, "WaterPump": make_water_pump, "Generator": make_generator, "ControlPanel": make_control_panel, "LabBench": make_lab_bench,
            "LabCabinet": make_lab_cabinet, "MetalRack": make_metal_rack, "Workbench": make_workbench, "PipeValves": make_pipe_valves, "Barrel": make_barrel, "Crate": make_crate}


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
