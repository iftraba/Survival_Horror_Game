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

## Fase 1 hecha: estructura (`ComisariaV2Builder`, menú *Horror/Comisaria v2/1 Estructura*)
Crea `Assets/Scenes/Comisaria_v2.unity` copiando la escena actual (se quedan jugador, HUD, audio, `GameFlow`, cámara e iluminación global) y levanta el nivel bajo `--- COMISARIA V2 ---`. Repetible. Edificio de 40 × 32 m (x -20..20, z 0..32; la fachada con la puerta principal doble mira al sur, z = 0).

| Planta | Suelo / techo | Distribución |
|---|---|---|
| Sótano | -4,5 / -1 | Pasillo central (z 12-16) desde el ascensor hacia el oeste, dos salas al sur y dos al norte, y la sala de calderas al noroeste (x -20..-6, z 16-32). |
| Baja | 0 / 3,5 | Vestíbulo (x -8..8, z 0-16) con puertas oeste, este y al fondo. Ala oeste: sala de espera y sala segura. Ala este: oficina y vestíbulo del ascensor. Al fondo: dos salas grandes (vestuarios y calabozos o interrogatorios). |
| Primera | 4 / 7 | Oficinas (x -8..8, z 0-16) con el hueco de la escalera. Despacho del comisario al oeste, sala de conferencias al este (z 0-12), vestíbulo del ascensor (z 12-16). Al fondo, una sala grande (por definir). |
| Archivo | 7,5 / 14 | Nave de 34 × 32 m con 6,5 m de techo (jefe 1) y un vestíbulo del ascensor cerrado (x 14-20, z 10-18). |

- **Escalera de caracol** (centro x 5, z 12; radio 2,2 m; 400° de giro; 24 peldaños) del vestíbulo a las oficinas.
  - Los peldaños, la columna y la barandilla son decorado. La colisión es una rampa helicoidal invisible (`Art/Props/RampaCaracol.asset`), así el jugador no tropieza y los zombis tienen NavMesh para subir.
  - Arriba llega a un rellano hacia el oeste, con barandilla alrededor del hueco.
  - Comprobado: hay camino de NavMesh completo de la planta baja a la primera, y el jugador la sube entera andando.
- **Hueco del ascensor:** x 17-20, z 12-15, de -4,5 a 14 m, con una abertura al oeste en cada planta, tapada de momento por una puerta de chapa fija. El ascensor en sí es la fase 6.
- **Puertas y luz:** 19 puertas de madera (las de modelo nuevo) y 79 lámparas.
- **Fuera:** de momento solo una calle plana delante; el exterior de verdad es la fase 2.

## Fase 2 hecha: vestíbulo, exterior e intro (`ComisariaV2Exterior`, menú *Horror/Comisaria v2/2 Vestibulo y exterior*)
Repetible: rehace los grupos `Exterior`, `Vestibulo_Props` e `Intro` y vuelve a hornear el NavMesh.

**Modelos nuevos** (`Tools/blender/build_exterior.py`, horneados con `texture_items.py`; copias sin hornear en `Tools/raw_generated/backup_exterior/`):
- Mostrador de recepción en L con dos puestos (monitor, teclado, ratón, papeles).
- Banco de espera.
- Coche patrulla quemado.
- Tramo de verja de barrotes de 3 m.
- Farola.

**Texturas de la ciudad** (`Tools/blender/build_city_textures.py`):
- Dos fachadas de ladrillo u hormigón con ventanas, algunas encendidas (con emisión) y otras tapiadas.
- Asfalto con grietas y charcos, y acera de losas.

**Vestíbulo:** el mostrador al fondo a la izquierda (la escalera queda a la derecha), con sillas y archivadores detrás. Hay bancos de espera en las paredes de los lados, sin tapar las puertas, y unas cajas y un bidón.

**Exterior:**
- La comisaría tiene fachada con ventanas, marquesina con pilares, rótulo con luz y un patio de acera.
- La rodea una **verja** (x -24..24, z -12..38) con colisión. Detrás de la verja de delante hay **8 zombis** de varios modelos que la sacuden y la golpean (`FenceRattler`: en letargo, solo despiertan si les disparan, y la verja no les deja pasar).
- El **coche patrulla arde** delante de la entrada (llamas, humo, chispas y una luz que parpadea, `FireLight`). Hay otro coche quemado en la calle.
- Hay farolas (una de cada tres fundida) y **edificios** alrededor: siete enfrente, dos a cada lado y uno detrás, de 12 a 30 m.
- Más fuegos en la calle, tres columnas de humo a lo lejos, escombros, cajas y bidones.
- Es de noche: luna fría y niebla.

**Escena de cámara** (`IntroCutscene`, 14 s):
- La cámara gira alrededor de la comisaría a 40 m y 25 m de altura. Aparece el título «SECTOR 7: GRIMHEIM» y baja hasta la puerta principal.
- Funde a negro y el jugador queda en el vestíbulo con la **puerta principal atrancada** (ya no se puede salir).
- Durante la escena, el jugador no se mueve y el HUD no se ve. Se salta con Espacio, E, Esc o clic.
- Comprobado en Play con capturas.

## Ajustes tras la revisión del usuario (2026-10-08)
- **Escalera de caracol** (centro x 3,5, z 11,5; hueco x 1-6, z 9-14):
  - Ahora empieza en su lado oeste mirando al norte. Quien entra por la puerta principal y anda recto pisa el primer peldaño sin rodearla.
  - Da una vuelta entera en el sentido de las agujas del reloj y arriba sale otra vez hacia el norte, a un rellano que da a las oficinas.
  - Está a 2 m de las paredes: la barandilla ya no tapa la puerta del ascensor de la primera planta.
  - Comprobado andando: desde la entrada se sube recto y se sale a las oficinas.
- **Barandillas de verdad** (`Railing`): pies derechos cada 1,2 m, pasamanos de madera, barra intermedia y barrotes cada 12 cm, con una colisión fina invisible. La de la escalera lleva barrotes y pasamanos de madera siguiendo la hélice.
- **Inicio fuera:**
  - Tras la escena de cámara, el jugador está en el patio, entre la verja con los zombis y la comisaría, delante del coche ardiendo (objetivo: «Entra en la comisaría»).
  - Al cruzar la puerta principal se cierra y se atranca detrás (`SealOnEnter`, «La puerta se ha atrancado tras de ti»). Comprobado.
- **Más cerca:**
  - La verja está a 8 m de la fachada delante y a 2,5 m por los lados y por detrás.
  - La calle tiene 8 m, con los edificios de enfrente al otro lado y callejones de 3,5 m a los lados.
  - Edificios de 10 a 20 m. La cámara de la intro gira a 34 m y a 26 m de altura.

## Fase 3: primera planta (2026-10-08)
Kit `ComisariaV2Office` (menú *Horror/Comisaria v2/3 Primera planta*). Se puede repetir: rehace `Props/Primera_Props` y `Primera_Estrado` con la misma semilla.

**Modelos nuevos** (Blender, `Tools/blender/build_office.py`, en `Art/Props/Office`, texturas horneadas con `texture_items.py`; registrados en `ItemTextureKit.OfficeProps`):
- ordenador de sobremesa (monitor, teclado y ratón), portátil, papeles, lámpara de mesa, papelera, fuente de agua, pizarra, silla giratoria, perchero;
- mesa y sillón de despacho, librería, vitrina de trofeos, alfombra;
- silla de auditorio, atril con micrófono, mástil de bandera.

**Texturas** (`Tools/blender/build_flag_map_textures.py`): bandera de la policía (azul con franja dorada y estrella de seis puntas), bandera de la ciudad (franjas granate y blanca con torre) y plano de la ciudad (río, avenidas, parque, la comisaría marcada en rojo y chinchetas).

**Oficinas** (x -8..8, z 0..16):
- Islas de dos mesas enfrentadas. Cada puesto lleva al azar silla giratoria o normal, ordenador o portátil (o nada), papeles, lámpara, teléfono y papelera.
- Archivadores, estanterías, pizarra, fuente de agua, perchero y una caja.
- Pasillos anchos entre islas y libre el paso al este del hueco de la escalera.

**Despacho del comisario** (x -20..-8):
- Mesa grande con sillón de piel, dos sillas de visita, alfombra, ordenador, teléfono y lámpara.
- Mesa de reuniones con cuatro sillas, cuatro librerías, vitrina de trofeos, archivador, banco, pizarra, fuente de agua, taquilla y perchero.
- Bandera de la policía y de la ciudad.

**Sala de conferencias** (x 8..20, z 0..12):
- Estrado de madera al fondo con dos escalones, atril con micrófono en el centro y el plano de la ciudad enmarcado en la pared.
- **Cuatro banderas**, dos a cada lado del mapa. La tela es una malla generada que cuelga con pliegues.
- Seis filas de sillas mirando al estrado con un pasillo central; falta alguna y alguna está volcada.

**Variedad** (`PropVariant`): cada mueble lleva un pequeño giro y desplazamiento al azar y un tono propio (±15 %, aplicado con MaterialPropertyBlock, sin materiales nuevos). 162 objetos con tono.

**NavMesh:**
- Toda la planta es alcanzable desde la planta baja por la escalera: oficinas, despacho, conferencias, estrado y vestíbulo del ascensor (comprobado con una rejilla de rutas).
- El estrado va fuera de `Props` porque ese grupo es «no transitable» para el NavMesh.
