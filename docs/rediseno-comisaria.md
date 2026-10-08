# Rediseño de la comisaría (rama `rediseno-comisaria`, 2026-10-08)

Lo que pide el usuario. Se guarda todo lo que hay (modelos, enemigos, jefes, sistemas); cambia la distribución del nivel.

## Inicio
- Escena corta de cámara al empezar: gira alrededor de la comisaría en una ciudad en caos (un par de edificios alrededor, fuego, humo).
  - Un coche de policía en llamas en la entrada.
  - Una valla rodeando la comisaría con varios zombis detrás, empujando.
- Termina con el jugador entrando por la puerta principal al vestíbulo.

## Plantas (de abajo arriba)
| Planta | Contenido |
|---|---|
| **Sótano** | Lo relacionado con el segundo jefe: lo que ahora es la zona 2 (bombas, máquinas, laboratorio, almacén) y la sala de calderas del jefe 2. |
| **Planta baja (principal)** | **Recepción / vestíbulo principal**: mostrador, puertas a izquierda y derecha y **escalera de caracol** para subir. |
| **Primera planta** | **Sala de oficinas** con las mesas y sillas que ya hay, más portátiles o monitor, teclado y ratón. A un lado, el **despacho del comisario**; al otro, la **sala de conferencias**: filas de sillas, un estrado al fondo con 4 banderas (dos a cada lado), un mapa en el centro y el atril con micrófono delante. |
| **Segunda planta** | **Archivo** (estanterías de expedientes): aquí está el **primer jefe**. |

## Otros cambios pedidos
- **Salto desactivado** (`PlayerController.jumpEnabled`); la animación se queda en el controlador.
- **Abrir puertas más rápido**: estado a 2,7× (antes 1,8×), la hoja gira a 0,5 s y el control vuelve a 0,9 s.
- **Más variedad de objetos**: hay muchas estanterías, sillas, etc. idénticas. Se harán variantes en Blender (tamaños, colores, desgaste, objetos encima) y se colocarán al azar con giros y desplazamientos pequeños.

## Modelos nuevos que harán falta
- **Ciudad:** edificios de fondo, coche de policía (en llamas), valla metálica, farolas, escombros, fuego y humo.
- **Vestíbulo:** mostrador de recepción, escalera de caracol (metálica, con barandilla), bancos y señalización.
- **Oficinas:** portátil, monitor con teclado y ratón, archivadores, papeleras, lámparas de mesa.
- **Sala de conferencias:** sillas de auditorio en filas, estrado, atril con micro, 4 banderas con mástil y un mapa grande.
- **Despacho del comisario:** mesa grande, sillón, estanterías con libros, vitrina con trofeos.
- **Archivo:** estanterías de expedientes, cajas de archivo y carpetas.

## Decisiones (2026-10-08)
- **Escena nueva** `Comisaria_v2`, construida desde cero con las 4 plantas. La actual se queda de referencia hasta que la nueva funcione entera. Se reaprovechan prefabs, zombis, jefes, objetos y sistemas.
- **Comunicación entre plantas:**
  - La **escalera de caracol** del vestíbulo une la planta baja con la primera.
  - Un **ascensor** que hay que poner en marcha lleva al sótano y al archivo.
- **Orden del recorrido:** vestíbulo → oficinas (primera planta) → archivo (segunda planta, **jefe 1**, da la llave) → sótano (**jefe 2**) → salida.

## Fases
1. **Estructura:** las 4 plantas con paredes, suelos, techos y puertas; escalera de caracol, hueco del ascensor y NavMesh.
2. **Vestíbulo y exterior:** recepción, fachada, valla, coche en llamas, edificios y la escena de cámara del inicio.
3. **Primera planta:** oficinas con ordenadores, despacho del comisario y sala de conferencias.
4. **Archivo y jefe 1:** estanterías de expedientes y la arena del jefe.
5. **Sótano y jefe 2:** pasar ahí la zona 2 y la sala de calderas.
6. **Recorrido:** llaves, ascensor, salas seguras, munición, zombis y salida.
7. **Variedad de objetos** en todas las salas.
