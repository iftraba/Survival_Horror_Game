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

## Fase 3, cambio: memorial en el recibidor y oficinas al fondo (2026-10-08)
A petición del usuario, la sala de ordenadores pasa del recibidor (donde sale la escalera) a la **sala del fondo** (x -20..20, z 16..32, tras la puerta norte):
- Mismo estilo de distribución: grupos de dos islas, en 6 columnas y 2 filas, con dos huecos para archivadores y para la zona de pizarras. Son 40 puestos.
- Hay un pasillo central libre desde la puerta y otro entre las dos filas.

En el **recibidor** va el **memorial de los agentes caídos** (modelos nuevos en `Tools/blender/build_memorial.py`, en `Art/Props/Memorial`; la piedra se hornea con el nuevo tipo «stone» de `texture_items.py`):
- **Monumento** de piedra escalonado en el lado oeste, mirando hacia donde sale la escalera.
  - Lleva la estrella de la policía en bronce, una placa y **tres huecos para medallones** (también existe el modelo del medallón).
  - El puzle de colocar los medallones para abrir el camino al archivo se monta en la fase 6.
- Cordón de terciopelo con postes de latón alrededor, abierto por delante. Delante, dos coronas de flores y velas derretidas; detrás, las banderas de la policía y de la ciudad.
- 15 placas con nombres en las paredes sur y oeste, sin tapar la puerta del despacho.
- Dos bancos mirando al monumento, una alfombra, plantas en maceta y la vitrina de trofeos.

**NavMesh:**
- El voxel pasa a 0,1 m (antes 0,167). Con el de antes, las puertas de 1,6 m quedaban cerradas en el horneado.
- Comprobado con una rejilla de rutas: toda la planta es alcanzable, incluido el paso al ascensor por el este del hueco de la escalera.
- Las puertas cerradas siguen cortando el NavMesh (`NavMeshObstacle`), como antes.

## Fase 4: archivo y jefe 1 (2026-10-08)
Kit `ComisariaV2Archive` (menú *Horror/Comisaria v2/4 Archivo y jefe 1*). Se puede repetir: rehace `Props/Archivo_Props`, `Archivo_Estanterias` y `Archivo_Jefe`.

**Modelos nuevos** (Blender, `Tools/blender/build_archive.py`, en `Art/Props/Archive`; registrados en `ItemTextureKit.ArchiveProps`):
- Estantería metálica exenta (1,8 × 0,5 × 2,3 m) en tres variantes con distinto desorden: cajas de archivo con etiqueta y asa, filas de carpetas AZ de colores, huecos, cajas sin tapa con papeles, carpetas inclinadas.
- Estantería de obra de pared (4 × 3,4 m).
- Pilas de cajas, papeles por el suelo, escalerilla con ruedas, fichero de madera con cajoncitos (alguno abierto) y carrito con carpetas.

**Nave del archivo** (34 × 32 m, 6,5 m de techo):
- Estanterías de obra en todas las paredes, fijas.
- Al oeste, tres pasillos largos de estanterías espalda con espalda, con dos cruces. Junto a la entrada, cuatro bloques más, que dejan libre el paso desde la puerta. Son 120 estanterías exentas:
  - sirven de cobertura;
  - el jefe las revienta al embestir o caer encima (`PropBreaker`: cuelgan de un grupo `_Props` y miden menos de 2,6 m).
- En el centro queda la arena del combate: pilas de cajas, papeles, carritos, ficheros, escalerillas, la mesa del archivero y una estantería volcada.

**Jefe 1** (`Boss.prefab`: salto, embestida, rompe muebles y suelta la llave):
- Está dormido en el centro, haciendo músculo, mirando a la entrada.
- Al pasar la puerta del vestíbulo del ascensor (`BossRoomTrigger`) despierta y la puerta se atranca hasta que muere.
- Comprobado en Play: anima bien (no sale en T), despierta, atranca la puerta y va a por el jugador.

**NavMesh:** toda la nave es alcanzable desde el vestíbulo del ascensor (comprobado con una rejilla de rutas). Al archivo se llega por el ascensor, que se pone en marcha en la fase 6.

## Fase 5: sótano y jefe 2 (2026-10-08)
Kit `ComisariaV2Basement` (menú *Horror/Comisaria v2/5 Sotano y jefe 2*).
- Rehace `Props/Sotano_Props` y `Sotano_Jefe`.
- El hundimiento de la sala de calderas (`Sotano_Calderas`) cambia la losa del sótano y solo se hace una vez. Si ya existe, el kit no hace nada; para repetirlo hay que reconstruir desde la fase 1.

**Modelos nuevos** (Blender, `Tools/blender/build_basement.py`, en `Art/Props/Basement`; registrados en `ItemTextureKit.BasementProps`):
- Caldera industrial horizontal con tapas abombadas, anillos, hogar, manómetros, nivel de agua, tubos al techo con bridas y volante, y escalerilla.
- Bomba de agua con motor de aletas, grupo electrógeno y fila de armarios eléctricos con relojes, pilotos y canaletas.
- Mesa de laboratorio con frascos, microscopio y gradilla, y vitrina con frascos.
- Estantería industrial con cajas, bidones y garrafas.
- Banco de trabajo con panel de herramientas y colector de tuberías con volantes.

**Salas** (las instalaciones de la antigua zona 2):
- **Cuarto de bombas** (suroeste): cuatro bombas, colectores de tuberías, banco, estantería, bidones.
- **Sala de máquinas** (sureste): dos grupos electrógenos, armarios eléctricos y banco.
- **Laboratorio** (norte): tres mesas de laboratorio, vitrinas, mesa con lámpara y archivadores.
- **Almacén** (noreste): hileras de estanterías industriales, cajas y bidones.

**Sala de calderas** (x -20..-6, z 12..32):
- **Hundida 2 m**: foso de 5,5 m de alto, como la arena antigua.
- Al cruzar la puerta se sale a un rellano con barandilla y se baja por una escalera de chapa. Lo que se pisa es una rampa invisible, para no tropezar y para que haya NavMesh.
- En el foso:
  - la caldera en el centro;
  - armarios eléctricos, grupos electrógenos y una bomba como cobertura, con pasillos de unos 2,5 m;
  - colectores en las paredes y bidones;
  - luz roja de emergencia (dos parpadean) y el resplandor del hogar.

**Jefe 2** (`Zombie_BossPxl`):
- Espera al fondo del foso y suelta la llave maestra (`I_KeyFinal`).
- Despierta al pisar el rellano (`BossRoomTrigger_2`) y la puerta de la sala se atranca hasta que muere.
- Comprobado en Play: despierta, atranca la puerta y va hacia la escalera.

**NavMesh:** todo el sótano es alcanzable desde el pasillo del ascensor, también el foso por la escalera (comprobado con una rejilla de rutas).

## Fase 6: recorrido (2026-10-08)
Kit `ComisariaV2Progress` (menú *Horror/Comisaria v2/6 Recorrido*).
- Rehace `Recorrido` y `Props/Recorrido_Props`.
- La escalera de servicio, que agujerea las losas, se hace una sola vez (`Escalera_Servicio`).

**Recorrido completo:**
1. **Exterior → vestíbulo:** la puerta se atranca y el objetivo pasa a ser «Explora la comisaría y busca una salida».
2. **Puzle del memorial** (primera planta): hay que reunir tres medallones de bronce.
   - **Dónde están:** en la mesa del comisario, en el estrado de la sala de conferencias y en el catre de la celda del medio de los calabozos (planta baja).
   - **La pista:** una nota del sargento, en un banco del memorial, dice dónde están.
   - **Al colocarlos** en el monumento (`MedallionMonument`) se ven puestos y sube la reja (`ServiceGate`) de la escalera de servicio de la sala de ordenadores.
3. **Escalera de servicio** (x 10→17 junto a la pared norte, de la primera planta al archivo):
   - Lleva peldaños de chapa, una rampa invisible y barandillas.
   - Arriba hay un cuarto con puerta. Al salir de él despierta el **jefe 1** y la puerta se atranca.
   - El jefe 1 suelta la **llave del ascensor** (`I_KeyElevator`). La nota del archivero lo explica.
4. **Ascensor de carga** (`ElevatorPanel`):
   - Hay botoneras en la planta baja (ala este) y en el sótano. Con la llave se pone en marcha.
   - Después, al usarlo, funde a negro y el jugador aparece en la otra planta.
5. **Jefe 2** en la sala de calderas: suelta la **llave maestra**.
6. **Fin:** el **portón del túnel de servicio** (pared oeste del foso, con luz verde) se abre con la llave maestra y gana la partida (`ExitDoor`).

**Planta baja amueblada:**
- Sala de espera: bancos, plantas, fuente de agua.
- Sala segura: terminal, baúl, catre, mesa.
- Oficina este: mesas, ordenador, archivadores.
- Vestíbulo del ascensor: banco y planta.
- Vestuarios: 26 taquillas y bancos.
- Calabozos: tres celdas con rejas y catres, y mesa de interrogatorio.

**Salas seguras:** planta baja (oeste), sala de ordenadores (junto a la reja) y sótano (junto al ascensor, con teléfono).

**Botín:**
- La escopeta y cartuchos en el despacho del comisario.
- 28 objetos en total: munición, sprays, la riñonera en los vestuarios y los medallones.

**Zombis:** 16 repartidos (5 en la planta baja, 6 en la primera, 5 en el sótano), sin los modelos del jefe 2.

**Guardado:** las marcas de progreso (`Progress`: medallones, reja, ascensor) van en la partida (`SaveData.flags`). Al cargar, la reja y los medallones aparecen como estaban.

**Comprobado en Play (avanzando fotogramas a mano):**
- El monumento con 2 medallones no abre; con 3 sí, y la reja sube.
- El ascensor sin llave no va; con llave, se pone en marcha y lleva al sótano. El jugador queda en el suelo y recupera el control.
- El portón sin llave maestra no abre; con ella, victoria.
- NavMesh: rutas completas de la planta baja al archivo por la escalera de servicio y a todas las salas.

---
# Ampliación: comisaría grande (2026-10-09)
Al probarla, el usuario vio que la dificultad era nula: un zombi por sala, salas enormes y vacías, y el jefe 1 al alcance enseguida. Pidió:
- un edificio más grande y de pasillos, con muchas más salas;
- más puzles de puertas (candados que romper, tarjeta de seguridad, tarjeta del jefe de seguridad para los calabozos, puerta sin corriente antes del jefe 2 con fusibles);
- taquillas normales y con código;
- una escalera exterior que lleve a una planta con una llave o pieza.

Las fases 1 a 6 anteriores se rehacen sobre la misma escena `Comisaria_v2`. Los kits viejos (`ComisariaV2*`) quedan de referencia.

## Plan
| Bloqueo | Se abre con | Dónde está |
|---|---|---|
| Candados (puerta este del vestíbulo, pasillo sur en x 8, puerta del callejón) | Cizalla | Garaje (planta baja) |
| Puerta de seguridad (pasillo este → pasillo norte) | Tarjeta de seguridad | Oficina de seguridad (planta baja, tras el candado) |
| Calabozos | Tarjeta del jefe de seguridad | Su despacho (primera planta), solo por la escalera de incendios |
| Taquillas con código | Códigos en notas y pizarras | Repartidas |
| Memorial: 3 medallones | Calabozos, taquilla con código de la biblioteca, caseta de la azotea | Abren la escalera del archivo (jefe 1) |
| Ascensor al sótano | Llave del ascensor | La suelta el jefe 1 |
| Puerta sin corriente del pasillo de calderas | 3 fusibles en el cuadro eléctrico | Almacén, laboratorio (taquilla con código), sala de máquinas |
| Portón del túnel | Llave maestra | La suelta el jefe 2 |

Fases: **A** estructura → **B** exterior e intro → **C** mobiliario por salas → **D** puzles, llaves y taquillas → **E** botín y zombis (2-4 por zona, emboscadas) → **F** pruebas y equilibrio.

## Fase A hecha: estructura (`ComisariaGrande`, menú *Horror/Comisaria grande/1 Estructura*)
La planta se describe **con datos** en `ComisariaGrande.Define()`:
- **Salas:** rectángulo, cota de suelo y techo, tipo y grupo (los espacios del mismo grupo no llevan pared entre sí).
- **Puertas:** posición, tipo (madera, doble, candado, tarjeta, tarjeta del jefe, sin corriente, chapa, fija) y ancho.
- **Huecos de escalera** en las losas.

El constructor saca solo lo demás:
- **Paredes:** en cada borde de la rejilla de 0,5 m entre dos espacios distintos. Son exteriores de 0,35 m e interiores de 0,2 m, y llevan rodapié por el lado de las salas.
- **Puertas:** su hueco con dintel y la puerta.
- **Suelos y techos:** por sala.
- **Lámparas:** según el tamaño de la sala (129).

**Edificio:** 64 × 44 m (x -32..32, z 0..44). Cada planta tiene una fila de salas al sur, un pasillo, otra fila, otro pasillo y otra fila al norte.
- **Planta baja** (14 espacios): sala segura, sala de espera, vestíbulo con la caracol, oficina de recepción, armería, garaje, sala de pruebas, aseos, vestíbulo del ascensor, oficina de seguridad, sala de descanso, pasillo de seguridad, vestuarios, calabozos, escalera norte, almacén y taller.
  - Fuera, al oeste, el **callejón** con la **escalera de incendios**: tramo al balcón de la primera planta (puerta del despacho del jefe de seguridad) y tramo a la azotea.
- **Primera:** despacho del comisario, conferencias, memorial (llega la caracol), dos oficinas, despacho del jefe de seguridad (sin puerta interior), biblioteca, descanso, detectives, aseos, interrogatorios, escalera del archivo, depósito de pruebas, escalera norte, registro y sala del sindicato.
- **Segunda:** la antesala (llega la escalera del archivo) y el **archivo** (36 × 29,5 m, techo a 14). El resto es **azotea** con pretil, y en ella hay una caseta.
- **Sótano:** bombas, depósitos, máquinas, taller, almacén, sala segura, vestíbulo del ascensor, laboratorio, cuadro eléctrico, sala de control, galería de tuberías y la sala de calderas hundida 2 m (rellano y escalera). Un pasillo de calderas queda separado por la puerta sin corriente.
- **Ascensor:** hueco en x 5-8, z 23-26,5, del sótano a la primera planta.

**Comprobado:**
- Con las puertas abiertas, NavMesh completo a todas las salas de las cuatro plantas, al callejón, al balcón, a la azotea y a la caseta.
- En el sótano, desde el vestíbulo del ascensor, también al foso de calderas.

## Fase B hecha: exterior e intro (`ComisariaGrandeExterior`, menú *Horror/Comisaria grande/2 Exterior e intro*)
- Reutiliza las piezas de `ComisariaV2Exterior` (ahora `internal`): modelos, fuego, humo y materiales de ciudad.
- **Patio, calle y verja:** patio delantero de 8 m y franjas alrededor dentro de la verja (x -42..36,5, z -8..48,5). Delante, la acera, la calzada y la acera de enfrente.
- **Fachada:** de hormigón con ventanas hasta 8,6 m, con huecos para la puerta principal, la del callejón y la del balcón de la primera planta. Marquesina y rótulo.
- **Coches y luces:** dos coches patrulla (uno ardiendo en la entrada), un coche quemado en la calle y 13 farolas. En el callejón hay dos luces que parpadean.
- **Alrededor:** edificios en las cuatro manzanas, fuegos, columnas de humo, escombros y 10 zombis agolpados tras la verja.
- **Escena de cámara:** radio de 52 m y altura de 34, centrada en el edificio (comprobada en Play con capturas). La puerta principal se atranca al entrar. Objetivo: «Explora la comisaría y busca una salida».
- **Pendiente:** algunas lámparas del archivo iluminan sus muros por fuera (luces sin sombra). Se corrige en el pase de luces.

## Fase C hecha: mobiliario (`ComisariaGrandeProps`, menú *Horror/Comisaria grande/3 Mobiliario*)
**8 modelos nuevos** (`Tools/blender/build_station.py`, en `Art/Props/Station`): cabina de aseo con inodoro, encimera con lavabos y espejo, mesa de vigilancia con 9 monitores, sofá, máquina expendedora, encimera de office (fregadero, microondas, cafetera), armero con armas encadenadas y pila de neumáticos.

**Amueblado por tipo de sala** (una función por tipo):
- Para colocar cosas contra la pared usa `AlongWall`, que se salta las puertas. Para muebles sueltos usa `At`, que comprueba que caben.
- Nunca se tapa la zona delante de cada puerta, las escaleras ni el ascensor.

| Sala | Contenido |
|---|---|
| Oficinas | Islas de mesas |
| Vestuarios | Filas de taquillas y bancos |
| Calabozos | 4 celdas con rejas y catres |
| Biblioteca | Estanterías de libros, mesas de lectura y fichero |
| Aseos | Cabinas y lavabos |
| Seguridad | Mesa de monitores |
| Armería | Armeros |
| Garaje | Coche, neumáticos y banco |
| Descanso | Office, máquinas y sofá |
| Archivo | Laberinto de estanterías de obra alrededor de una sala central (arena del jefe 1), con estanterías rompibles en los pasillos |
| Sótano | Bombas, depósitos, generadores, laboratorio, cuadros eléctricos, sala de control, galería de tuberías y calderas |
| Pasillos | Muebles sueltos contra una pared, sin estrechar el paso por debajo de 1,9 m |

- 1.227 muebles y 47 piezas fijas.
- **Comprobado:** con las puertas abiertas se llega a las 70 salas (ruta de NavMesh a cada una) y las capturas de 9 salas se ven bien.

## Fase D hecha: puzles (`ComisariaGrandePuzzles`, menú *Horror/Comisaria grande/4 Puzles*)
**Modelos nuevos** (`Tools/blender/build_puzzle_items.py`, en `Art/Props/Puzzle`): cizalla, tarjeta magnética, fusible, cadena con candado, lector de tarjetas y cuadro de fusibles.

**Componentes nuevos:**
- `LockVisual`: muestra el candado o el piloto rojo mientras la puerta está cerrada con llave, y el piloto verde al abrirla.
- `ProgressSeal`: deja una puerta atrancada hasta una marca de progreso.
- `Door.sealedPrompt` y `Door.sealedMessage`: textos propios de una puerta atrancada («Sin corriente»).
- `MedallionMonument`: sus textos son configurables, así que también sirve de cuadro de fusibles.

**Cadena del recorrido:**
1. **Cizalla** (garaje, en el pasillo sur al oeste; lo dice la nota de la recepción). Corta los tres candados y no se gasta: puerta este del vestíbulo, puerta del pasillo sur en x 8 y puerta trasera de los vestuarios.
2. **Ala este:**
   - **Tarjeta de seguridad** en la oficina de seguridad. Abre el pasillo de seguridad y la escalera norte de la planta baja (así no se cuela nadie bajando desde la primera).
   - **Escopeta** en la taquilla con teclado de la armería: código **4519**, en la nota de la sala de pruebas.
3. **Sala de pruebas a oscuras:** el interruptor está junto a la puerta.
4. **Pasillo norte:**
   - Vestuarios: riñonera en una taquilla y la nota con el código de la biblioteca (**0832**).
   - Puerta trasera con candado → callejón → **escalera de incendios** → despacho del jefe de seguridad (**tarjeta del jefe**) y azotea (caseta: **medallón III**).
5. **Calabozos** (tarjeta del jefe): **medallón I** en el catre de la segunda celda.
6. **Biblioteca** (primera planta): **medallón II** en la taquilla con teclado.
7. **Memorial:**
   - Con los tres medallones sube la reja de la escalera del archivo (pasillo norte de la primera planta).
   - Arriba, en la arena central del archivo, espera el **jefe 1**. La puerta del archivo se atranca hasta que muere, y suelta la **llave del ascensor**.
8. **Ascensor** (planta baja, junto a los aseos) → sótano.
9. **Fusibles** para el **cuadro eléctrico** del ala este del sótano:
   - almacén;
   - taquilla del laboratorio, con el código **7258** de la nota de la sala de máquinas;
   - galería de tuberías.

   Con los tres, la puerta del pasillo de las calderas recupera la corriente.
10. **Sala de calderas:**
    - Al pisar el rellano despierta el **jefe 2** y la puerta se atranca.
    - Suelta la **llave maestra**, que abre el **portón del túnel** (pared oeste del foso) y da la victoria.

**Taquillas y notas:**
- 10 taquillas: 3 con código y 7 normales con munición, curas o riñonera.
- 8 notas con las pistas.
- Las marcas de progreso (medallones, fusibles, corriente, reja, ascensor) se guardan con la partida.

**Comprobado en Play:**
- La puerta de calderas empieza «Sin corriente» y se libera con los tres fusibles.
- El candado no abre sin cizalla y sí con ella: desaparece la cadena y la cizalla se conserva.
- La tarjeta del jefe abre los calabozos y el piloto pasa de rojo a verde.

## Fase E hecha: zombis y botín (`ComisariaGrandeEnemies`, menú *Horror/Comisaria grande/5 Zombis y botin*)
**74 zombis** repartidos por zonas: unos 30 en la planta baja, 22 en la primera y la azotea, 3 en el archivo además del jefe, y 21 en el sótano.
- Van de 1 a 3 por sala o pasillo. No hay zombis en las salas seguras, el vestíbulo, los aseos ni el vestíbulo del ascensor.
- **Mezcla:** 9 modelos normales (ninguno con la piel del jefe 2), 5 reptantes (garaje, calabozos, callejón, galería de tuberías y otro) y 3 carroñeros comiéndose un cadáver (descanso, sindicato y laboratorio), que solo despiertan si les disparan.
- **Letargo:** 12 están dormidos; no reaccionan hasta oír un ruido o recibir un tiro. Están sobre todo en la sala de pruebas (a oscuras), almacenes, biblioteca y archivo.
- Fuera siguen los 10 de la verja y los dos jefes.

**Botín justo:**
- 23 objetos sueltos encima de los muebles: unas 140 balas de pistola, 40 cartuchos y 7 sprays.
- Además, lo que hay en las taquillas de la fase D: escopeta y 6 cartuchos en la armería, 2 riñoneras y algo de munición.

**Comprobado:** 400 fotogramas en Play sin errores. Falta probar el rendimiento con todos los zombis en una build. Si va justo: occlusion culling (las paredes no ocultan nada ahora) o menos zombis en `Spawns`.

**Pendiente (fase F):** jugarlo entero, equilibrar munición y zombis, y el pase de luces (fugas de luz por fuera del archivo; salas algo oscuras). Los menús viejos *Horror/Comisaria v2/…* son del diseño anterior: no hay que usarlos sobre esta escena.

## Fase F, parte 1: reservas en las arenas y fugas de luz (2026-10-10)
Sale de la revisión de nivel (`/level-review`) y de `docs/planes/fase-f-arenas-y-luces.md`. **No cambia el equilibrio general** (vida de zombis frente a munición): eso va después de jugar el nivel entero.

**Cambio 1: reservas de munición en las arenas** (`ComisariaGrandeArenaLoot`, menú *Horror/Comisaria grande/6 Reservas de arenas*)
- Por qué: con la puerta sellada no se puede salir ni recoger munición, y dentro de las arenas no había ninguna. Quien entra sin balas solo puede morir y cargar.
- Cada arena lleva 6 cartuchos y 12 balas (provisional, a ajustar jugando), en el suelo y a 4,5-8,5 m del jefe. En el archivo, además, **dentro** del volumen del disparador del jefe 1, para no poder cogerlas antes de que empiece el combate (el disparador del jefe 2 es solo el rellano, no la arena).
- Colocadas: archivo `(20,9; 7,5; 27,2)` y `(11,0; 7,5; 31,7)`; foso de calderas `(-23,4; -6,5; 38,1)` y `(-26,6; -6,5; 36,5)`. Todas con ruta de NavMesh desde el jefe o el rellano, en suelo desnudo y fuera de `_Props` (rompible).
- Repetible: rehace solo el grupo `Botin_Arenas`. Va aparte del menú 5, que rehace zombis y botín al azar. Total de recogidas: 45 (antes 41).
- Es una ayuda, no una garantía: con un 50 % de acierto, el jefe 1 pide ~26 cartuchos. **Decisión abierta:** liberar la puerta si el jugador se queda sin munición (opción B del plan); se decide tras jugar.

**Cambio 2: fugas de luz exteriores** (`ComisariaGrandeLightFix`, menú *Horror/Comisaria grande/7 Pase de luces*)
- Diagnóstico (Play, capturas de la azotea): las manchas en la cara exterior del muro sur del archivo desaparecen al anular las luces de relleno `Fill` (puntuales, sin sombra, rango 5). **No las causan ni los focos ni `ShadowBudget`**: con las 131 sombras originales seguían igual.
- Dos causas: relleno a menos de su alcance de un muro exterior, y relleno de la primera planta bajo una azotea cuya luz sube por el forjado y alumbra la base de los muros del archivo (pasillo `F_C1`).
- El kit recorta el rango del relleno (mínimo 2,5 m, base 5) hasta la distancia al muro exterior más cercano y, bajo una azotea, hasta la distancia 3D a la base del muro que se levanta sobre ella. 59 de 129 rellenos recortados. Idempotente: parte siempre del rango 5.
- Resultado medido en 20 puntos a 1 m fuera de los muros (solo rellenos): 5 puntos con fuga e irradiancia relativa 1,43 → **2 puntos y 0,14** (−90 %). En la captura de la azotea, las manchas grandes quedan en dos destellos pequeños y tenues en la base del muro (mínimo de rango 2,5 m).
- El interior no se oscurece de forma visible: capturas A/B del pasillo `F_C1` con rango 5 y con el recorte, prácticamente iguales.
- Si se rehace el nivel con el menú 1, hay que volver a ejecutar los menús 6 y 7 (y el 5 si se rehace el botín).

**Cambio 3: reparto de sombras de `ShadowBudget`** (4 plazas para focos con visión, 4 para focos con pared en medio). Detalle, tablas y mediciones en `docs/sombras.md`: las fugas de focos a través de paredes bajan de 25/42/62 a 0/0/23 (suelo/cabeza/sobre la cabeza).

**Sin comprobar:** fps reales en una build (la build del escritorio no lleva estos cambios hasta `/recompile`), qué tal se siente el ambiente jugando y si las reservas bastan.

## Fase F, parte 2: rehacer el diseño (2026-10-10), etapa A hecha
Tras probar la build, el usuario rechazó el diseño: pasillos estrechos y sin carácter, salas iguales conectadas solo por puertas, objetos «al tuntún», munición insuficiente, zombis descartados, candado sin animación y salas oscuras. Plan aprobado en `docs/planes/fase-f-parte-2-rediseno-planta.md`; decisiones del usuario: **rehacer la planta**, **solo zombis aprobados**, **menos zombis y munición para casi todos**, **luz mixta**.
- **Etapa A (hecha, ver `docs/interaction.md` y `docs/enemies.md`):** zombis descartados fuera, candado corregido y con animación de corte, brillo en los objetos clave, `WallDressingKit` apuntando a la raíz de la comisaría grande (sin ejecutarlo todavía).
- **Etapa B (siguiente):** esquema de la nueva planta para que el usuario lo apruebe **antes de construir**. Etapas C a G: estructura, salas con carácter, puzles/zombis/botín por anclas, luz y verificación.
- Observado de paso: el pasillo sur de la planta baja y el garaje se ven muy oscuros en Play (una sola lámpara de 28 por sala de 12×12), lo que confirma la queja de la luz.

## Fase F, parte 2, etapa C.1: estructura de la planta v3 (2026-10-10)
`ComisariaGrande.Define()` rehecho con el esquema aprobado (`docs/planes/rediseno-planta.md`, versión 3): 62 espacios con propósito (antes ~70), atrio de doble altura, galería con balaustrada en la primera planta, **garaje (24×26,5 m) y calabozos (22×15 m) en el sótano**, custodia, radio y despachos, interrogatorios y observación. Construido con el menú 1 y el exterior con el menú 2.

**Nuevo en `ComisariaGrande.cs`:**
- `DoorKind.Roll`: persiana enrollable cerrada y fija (lamas y cajón) en `Puerta_Garaje_Rampa` (−32, 2, ancho 4).
- `GarageRamp()`: rampa de coches por el callejón oeste, dentro de la verja (x −41,5..−37,2): calzada inclinada de z 44 (y 0) a z 5,5 (y −4,5), ≈11,7 % de pendiente, muros de contención de 0,9 m a ambos lados, y una explanada plana a −4,5 m (x −41,5..−32, z 0..5,5) junto a la persiana. El callejón (`G_Alley`) pasa de 8 a 5 m (x −37..−32).
- `StraightStairX`: escalera recta a lo largo de X. La usa `EscaleraGaraje` (sala de espera → garaje, x −14,5..−23, pegada a la pared sur, baja hacia el oeste, con barandilla a ras de suelo).
- `EscaleraCustodia`: segundo tramo de la escalera norte, que baja de z 32 a z 40 (x 7..9) a la custodia.
- `GalleryRails()`: balaustrada de la galería alrededor del hueco `GalleryHole`.
- Huecos de losa nuevos (`GalleryHole`, `GarageStairHole`, `CustodyStairHole`) y dos retoques de las losas: los techos del sótano se abren bajo un hueco de la planta baja (tolerancia 0,9 → 1,05 m) y las paredes del sótano llegan a −0,25 m para tocar las de la planta baja.
- Exterior: el patio oeste ya no cubre la explanada ni la rampa, y cajas y barriles del callejón se reubican.

**Desviaciones del esquema dibujado (hechas a propósito):**
- El hueco de la galería está en x −1..5 (se dibujó en −5..1): el dibujado habría abierto el techo de los aseos.
- La puerta de interrogatorios está en (10, 42) y no en (10, 35): el tramo que baja a la custodia deja solo ~0,9 m junto a la pared este, menos de lo que cabe un agente de NavMesh.
- Galería norte y pasillo de calderas miden 2,5 m (z 26,5..29), como el diseño anterior, no 4 m.
- El nudo del ascensor se parte en dos piezas del mismo grupo para dejar sitio al hueco del ascensor.

**Comprobado (Play, NavMesh con los obstáculos de puerta apagados; capturas):**
- Con todas las puertas abiertas se llega desde el vestíbulo a las 62 salas (garaje, custodia, calabozos, callejón, azotea, caseta).
- Con el paso de un sentido (`Puerta_Garaje_Bombas`) cerrado, las únicas 13 salas inalcanzables son las industriales (bombas, máquinas, almacén, galería, segura, nudo, laboratorio, cuadro, galerías norte, pasillo de calderas, calderas, control). Desde el nudo del ascensor se llega a esas 13 y a nada más.
- Rampa: la superficie coincide con la teórica en 8 puntos (±0,01 m); hay camino de NavMesh de 41 m desde la entrada de la rampa hasta la explanada; la persiana corta el paso en x −32,06.
- Capturas en Play: rampa, persiana desde la explanada, atrio con la balaustrada de la galería arriba y la escalera al garaje.

**El nivel está a medias (a propósito, hasta las etapas D-F):**
- **No hay mobiliario, puzles, objetos, zombis ni botín**: el menú 1 rehace todo el nivel y borra lo que colgaba de él.
- **No ejecutar los menús 3 a 7**: usan coordenadas y ids de salas antiguos (`G_Garage`, `G_Cells`, `G_C1W`…, puertas con coordenadas fijas) y darían resultados incoherentes hasta que se adapten (etapas D y E).
- Sin comprobar: recorrer las escaleras andando con el jugador (solo NavMesh y capturas); la puerta de un sentido sigue siendo una puerta normal (`OneWayDoor` va en la etapa E).
- Pendiente de exterior: iluminación de la rampa y la explanada (oscuras), y la puerta de coches de la verja norte por donde entrarían los vehículos.
- Las lámparas (110) siguen con la regla antigua: etapa F.

## Fase F, parte 2, etapa D: mobiliario con carácter (2026-10-10)

**Qué hace.** `ComisariaGrandeProps` (menú 3) se adaptó a la planta v3 y amuebla las salas con composición y función:
- **Conferencias** (tu diseño, girado 90° para que el estrado no choque con la puerta): estrado con escalones, atril, mapa enmarcado, 4 banderas y sillas con pasillo central. Memorial con banderas y alfombra; despacho del comisario con banderas y mesa de reuniones (`ComisariaV2Office.Flag/TexMat` pasan a `internal`, con `FlagAt`, `PoliceFlagMat`, `CityFlagMat`, `CityMapMat`).
- **Garaje**: 5 patrullas en dos filas con carril central; taller al norte. **Calabozos**: 19 celdas en dos filas de 10 contra los muros norte y sur (3 m de fondo, rejas con hueco de puerta, camastro), pasillo central con mesa del carcelero y bancos; la primera de la fila sur queda libre porque ahí desemboca la puerta.
- Radio y despachos, interrogatorios (3 mesas + consola de observación), atrio por piezas, galería, hall, ingreso y custodia. Zonas de exclusión (`KeepOut`) para los huecos y cajas de escalera.
- `Puerta_Calabozos` y `Puerta_Admin_Archivo` se mueven a z=42: caían junto a un tramo de escalera sólido y quedaban tapadas.

**Comprobado (Play, NavMesh con obstáculos de puerta apagados):** 65 espacios, todos con ruta desde el vestíbulo salvo `B_Shaft` (hueco del ascensor, no andable); 54 puertas con paso libre a 1,1 m de cada lado (0 tapadas). Capturas en Play: conferencias, garaje, calabozos, atrio con galería y sala de radio.

**Sin comprobar:** orientación de las patrullas (en la captura se ven bien, pero no medida), colisiones de la balaustrada, camastros de cerca, resto de salas a la vista. La luz sigue siendo la antigua (etapa F): varias salas salen muy oscuras. Faltan los modelos nuevos (placas, señales, botiquín, lámpara colgante, cuadros, furgoneta) y `WallDressingKit`.

**Estado:** menús 1-3 válidos (1.195 muebles); **siguen sin adaptar los menús 4-7** (puzles, zombis, botín, luces).

## Fase F, parte 2, etapa E: puzles, zombis y botín por anclas (2026-10-10)

**Qué hace** (menús 3 a 6 adaptados a la planta v3; el 7, luces, sigue pendiente de la etapa F):
- **Anclas por sala** (`ComisariaGrandeAnchors`): los objetos clave, notas y botín se colocan «encima de un mueble de este tipo de esta sala», ya no en coordenadas absolutas. La superficie se saca de la malla del mueble (triángulos que miran arriba, la grande más cercana a 0,9 m), no de su collider (un banco con tablero mide 2,15 m de caja); la caja del mueble se recorta a esa altura para que el objeto se asiente. Evita lo ya colocado y los objetos pequeños de la mesa.
- **Puzles** (menú 4): cizalla en el banco del garaje, tarjeta de seguridad en la mesa de seguridad, tarjeta del jefe en su mesa, medallón I en el catre de la 2.ª celda, II en la taquilla de la biblioteca (0832), III en la mesa de la caseta, fusibles A (almacén), B (taquilla del laboratorio, 7258) y C (galería de servicio). Lámpara de mesa encendida junto a cizalla, tarjetas y medallón III. Lectores de tarjeta recolocados (atrio→ingreso, ingreso→escalera norte, custodia→calabozos). Cuadro de fusibles en la pared oeste del cuadro eléctrico.
- **`OneWayDoor`** (nuevo, `Interaction/`): `Puerta_Garaje_Bombas` y `Puerta_Galeria_HallNorte` están atrancadas por un lado y se abren desde el otro (y quedan abiertas). `LightSwitch.partner` mantiene sincronizados los **dos interruptores** de la sala de pruebas (uno junto a cada puerta, con piloto naranja).
- **Zombis** (menú 5): 43 (planta baja 13, primera 13, segunda 3, sótano 14), 5 reptantes, 3 carroñeros, 18 en letargo; solo los aprobados. A mano: un Cop junto a la mesa de la tarjeta de seguridad y un reptante dormido junto a un coche del garaje. Ninguno en vestíbulo, salas seguras, aseos, ingreso, custodia, escalera norte ni antesala.
- **Botín** (menú 5, más taquillas del 4 y reservas del 6): hasta el jefe 1 unas 182 balas, 62 cartuchos y 8 sprays; sótano industrial unas 96 balas, 30 cartuchos y 4 sprays; reservas de arena 18+12 y 24+12. `maxStack` sube a balas 45, cartuchos 18, spray 3.
- **Sala segura en la antesala del archivo**: terminal de guardado y baúl pegados a la pared este (menú 3).
- Además: mostrador del vestíbulo reubicado (no cabía) y mesa en la caseta; bancos de trabajo en las salas de máquinas y del cuadro.

**Comprobado:** compila sin errores; menús 3 a 6 se ejecutan sin avisos salvo 1 objeto del cuadro eléctrico que va al suelo (no hay banco en esa sala); en Play: 43 zombis + 10 de la verja + 2 jefes, todos sobre el NavMesh y solo modelos aprobados; los dos pasos de un solo sentido sellan por el lado malo y abren por el bueno; capturas del banco del garaje con la lámpara y del Cop de seguridad. Grafo de salas y puertas (script sobre `Define()` y las posiciones reales de los objetos): con las cerraduras en su sitio se obtienen las 9 piezas clave (cizalla, 2 tarjetas, 3 medallones, 3 fusibles) y se llega a todo salvo la sala de calderas, que el script no cubre (su cota -6,5 queda fuera de su filtro); eso ya se comprobó en la etapa C.1 por NavMesh.

**Sin comprobar:** que se coja cada objeto andando (alcance real en Play, solo se vio la cizalla), notas legibles desde el sitio donde quedan, luz de las salas (etapa F), equilibrio de munición y zombis (provisional: se ajusta jugando), el jefe 1 en la arena nueva de 36×29 m y la puerta de coches de la verja norte. Los zombis en letargo están de pie (no hay animación de dormido).

## Fase F, parte 2, etapa F: luz (2026-10-10)

**Qué hace**
- **Lámparas** (`ComisariaGrande.EmitLamps`, menú 1; nuevo menú **7a Rehacer lamparas** para ajustar solo la luz sin rehacer el nivel): una por ~40 m² (pasillos ~45), de 2 a 8 por sala, 252 en total (antes 110). Nombres `Lamp_<sala>#n`. Foco de **36** (halls, atrio y galerías 29, pasillos de servicio 22), rango 7,5, y relleno puntual (rango 6, intensidad 7) en una de cada dos lámparas de las salas de más de 100 m². Una lámpara que cae en un hueco de losa (escaleras, galería) se arrima a su borde.
- **Color por zona:** ámbar en vestíbulo, atrio, galería, hall y espera; sodio en el garaje; frío verdoso en calabozos y custodia; rojo suave de emergencia en el sótano industrial; azulado en los pasillos; las salas con color propio (seguras, comisario, calderas) lo conservan.
- **Lámparas rotas** (`CeilingLamp.dead`: nunca se encienden ni con interruptor): 5 en atrio, galerías y comedor. 5 lámparas de los pasillos del sótano parpadean.
- **Seis salas a oscuras con interruptor** (menú 4, `DarkRoomSwitches`): pruebas, calabozos, biblioteca, interrogatorios, almacén y laboratorio del sótano. Un interruptor con piloto naranja junto a cada puerta, por dentro y sin muebles delante; los de una sala van en anillo (`LightSwitch.partner`, ahora con propagación). 9 interruptores.
- **Pase de luces** (menú 7): sigue recortando el relleno junto a muros exteriores (base 6 m, nombres con `#`) y añade 9 luces exteriores de emergencia: 4 en la rampa, 2 en la explanada y 3 más tenues en el callejón.

**Medido en Play** (luminancia media de cada sala con una cámara temporal que renderiza 4 direcciones desde el centro, a 1,5 m; con la misma cámara y postproceso): con foco 20/16/12 la **mediana de las 54 salas era 5,5 %**; con 36/29/22 sube a **8,0 %** (p10 3,6 %). Las salas a oscuras (calabozos 1,3, interrogatorios 1,7, biblioteca 1,8, pruebas 2,2) pasan a 7-10 % al accionar su interruptor (comprobado: calabozos, laboratorio, biblioteca y pruebas, con el anillo sincronizado). Capturas: sala de radio (antes casi negra) y biblioteca apagada/encendida.

**Sin comprobar:** rendimiento con 252 lámparas (el contador del editor no sirvió: GPU 0 ms; hay que pasar `performance-audit` y mirar F3), fugas de luz por fuera del edificio y luz real de la rampa y el callejón (solo se añadieron), si 8 % de mediana es la penumbra que quiere el usuario (valor provisional: se ajusta con el menú 7a → 4 → 7), y salas pequeñas con lámpara cerca de la cámara (G_Atrio4 sale a 31 %).

**Orden tras cambiar la planta:** 1 → 2 → 3 → 4 → 5 → 6 → 7. Para ajustar solo la luz: 7a → 4 → 7.

## Fase F, parte 2, etapa G: verificación (2026-10-10)

Qué se comprobó y cómo (el detalle de lo que sigue abierto está en `docs/pendiente-de-comprobar.md`):
- **Alcance de los 96 interactuables** (objetos clave, notas, botín suelto, interruptores, terminales, baúles, cuadro de fusibles, ascensor, monumento, reja, portón; sin los 12 objetos que van dentro de taquillas): para cada uno se busca un sitio con la cápsula del jugador libre, a ≤2,2 m de la esfera del `PlayerInteractor` y a la vista desde el pecho, con las mismas reglas que el interactor. Primera pasada: 66 fallos (la prueba usaba el NavMesh como proxy y era demasiado estricta); con la cápsula quedaron **3 notas inalcanzables** (turno, archivero y mantenimiento): estaban dentro de la caja de collider de su escritorio (la malla queda 2-4 cm por debajo de la caja) o en una mesa de 2,4 m de fondo contra una pared. Arreglo: `ComisariaGrandeAnchors` recorta la caja a la superficie en cuanto queda por debajo, exige un punto desde el que se alcance y se vea el objeto, y el collider del papel sube 3 cm. Resultado final: **96 de 96**.
- **Rutas y puertas** (Play, obstáculos apagados, tras los menús 3 a 7): 62 salas alcanzables desde el vestíbulo, 54 puertas con paso libre, 55 enemigos sobre el NavMesh.
- **Llaves**: cada puerta con llave, el ascensor, el portón, el monumento y el cuadro de fusibles piden un objeto que existe en la escena o que suelta un jefe.
- **Munición contada en la escena**: coincide con el presupuesto (hasta el jefe 1: 182 balas, 62 cartuchos, 8 sprays; sótano industrial 96, 32, 4; arenas 18+12 y 24+12).
- **Hojas de contacto de 45 salas** (cámara temporal desde una esquina, luces del juego): todas amuebladas y con luz razonable; calabozos, biblioteca e interrogatorios salen apagados por diseño; el archivo sale muy oscuro.
- **Rendimiento** (1920×1080, cámara temporal en el editor, con lectura de píxeles para sincronizar): 8-20 ms por fotograma según el sitio, con ±5 ms de ruido; las sombras de los focos son la mitad del coste en el atrio (14 → 8 ms sin ellas). No hay línea base anterior a la etapa F ni medida en build.

Lo que **no** se pudo comprobar aquí y queda para jugar: coger de verdad cada objeto con la tecla E, abrir cada puerta con su llave, las taquillas, guardar y cargar (revisado por lectura de `SaveSystem`, `Door.ApplySaved` y `LightSwitch.SetOn`, pero no ejecutado), el comportamiento de los zombis (despertar, perseguir, puertas), el jefe 1 en la arena nueva y el equilibrio de munición, zombis y luz.

## Cambios tras la primera partida completa: fase 1 (2026-10-10)

Plan completo y decisiones del usuario en `docs/planes/feedback-primera-partida-completa.md`. Lo hecho en esta fase:
- **Jefe 1 despertado desde otra planta:** `ZombieAI.Noise` ya no despierta a los jefes (solo su `BossRoomTrigger`) y los zombis que están a más de 2,5 m de altura sin línea directa no oyen el disparo. Medido en Play: un ruido justo debajo del jefe lo deja dormido y sin barra de vida.
- **El jefe no rompía estanterías:** `PropBreaker.PropRoot` solo aceptaba grupos `Props` o `*_Props`; el mobiliario nuevo cuelga de `Props/Mobiliario`. Ahora también acepta `Mobiliario` y el tope de altura sube a 2,9 m (las estanterías metálicas miden 2,64 m). Medido en Play: una esfera de 1 m sobre una `ArchiveShelfA` rompe 2 muebles. **Sin ver aún al jefe real romperlas.**
- **Zombis dormidos:** `ZombieAI.proximityWakeRange` (3,5 m, la mitad agachado; con línea directa y misma planta) los despierta; el carroñero (`wakeOnlyWhenShot`) y los jefes no. En el menú 5 el factor de letargo baja de 1,7 a 1,1: 9 dormidos al azar más el Cop y el reptante del garaje (antes 18). Medido en Play: el Cop de seguridad sigue dormido a 6 m y despierta a 2,8 m.
- **Animación de comer al disparar:** el prefab `Zombie_Carronero` usaba el bucle de morder (`Z_ZombieBiting2`) como movimiento y golpe, así que al dispararle seguía «comiendo». Los 3 carroñeros ahora son un zombi normal (Civil, Cop, Girl u Oficial) con `ZombieFeeding.feeding` activo: comen junto a su cadáver y, al dispararles, se levantan con sus animaciones de siempre. Medido en Play: reacción al golpe, reposo y puñetazo normales tras el tiro.
- **Guardado:** siempre `SavePhone` (también en la antesala del archivo, antes terminal), más dos rincones de guardado (teléfono y baúl) en el ingreso (`G_Intake`) y el memorial (`F_Mem`): 5 puntos en el edificio principal más el del sótano.
- **Tirar es desechar:** `Inventory.Drop` ya no crea un `Pickup` en el suelo.
- **Notas:** la frase clave sale en rojo y subrayada dentro del texto (acepta «4 5 1 9» para 4519; si no aparece, va en una línea aparte), con etiqueta de categoría, viñeta, renglones y cinta; la hoja se ajusta a su contenido.

**Comprobado:** compila; rutas (62 salas) y puertas (54) tras rehacer; 100 de 100 interactuables alcanzables y a la vista; oclusión horneada de nuevo. **Sin comprobar:** el estilo de las notas más allá de una captura, el comportamiento del jefe rompiendo estanterías en combate, y los dos rincones nuevos a ojo.

## Cambios tras la primera partida completa: fases 2 y 3 (2026-10-10)

Plan y decisiones del usuario en `docs/planes/feedback-primera-partida-completa.md`.

**Fase 2: progreso y claridad**
- **Objetos clave** (`ItemData.IsKey`, `KeyUsage`, `Door.originalKey`): no se pueden tirar mientras hagan falta (el menú del objeto y la tecla X lo impiden y avisan: «guárdalo en un baúl»). Está «usado por completo» cuando todas las puertas que abre (`Door.originalKey`) están desbloqueadas: la cizalla, tras sus 3 candados; la tarjeta azul, tras sus 2 lectores; la dorada, tras los calabozos. Entonces sale un **check rojo** en la esquina de su casilla (inventario y baúl), la descripción lo dice y «Tirar» lo hace desaparecer del todo. Medallones, fusibles y llaves del ascensor y final ya se gastan solos al usarse. **Tirar cualquier objeto lo desecha** (no se queda en el suelo).
- **Tarjetas distintas**: dos modelos de Blender (`KeyCard` blanca con banda azul, foto y código de barras; `KeyCardChief` negra con marco y estrella dorados y chip), nombres «Tarjeta azul de seguridad» y «Tarjeta dorada del jefe», descripciones que dicen qué puertas abren, y lectores con franja del color de la tarjeta y rótulo 3D («TARJETA AZUL DE SEGURIDAD» / «TARJETA DORADA DEL JEFE»).
- **Secuencias de revelado** (`ProgressCutscene`, `ThirdPersonCamera.SetCinematic`): al poner el tercer medallón o el tercer fusible, la cámara se queda un momento en lo colocado, recorre rápido el camino (sacado del NavMesh) hasta la reja del archivo o la puerta de las calderas, se queda viéndola abrirse y vuelve. El jugador no se mueve ni recibe daño; se salta con E, Espacio o Esc. `ServiceGate` y `ProgressSeal` esperan a que la cámara llegue (`DelayFor`). No se reproduce al cargar una partida. **Visto en Play con la reja** (cámara por el memorial, la galería y el hall norte hasta delante de la reja, que sube; vuelve y el jugador recupera el control con el daño a 1); la de la puerta de las calderas, no.
- **Munición**: más al principio (sala segura +15, espera 12→18, vestíbulo 10→14) y menos en el centro y el sótano industrial (−25 %). Hasta el jefe 1 unas 175 balas (antes 182) y en el sótano industrial unas 71 (antes 96).

**Fase 3**
- **Mapa** (`MapData`, `MapTracker`, `MapMemory`, `HudMap`, kit `ComisariaGrandeMapKit`, menú 9): pestaña MAPA del menú del inventario (tecla M, o Q para cambiar de pestaña). Plano por planta, norte arriba, **con niebla** (solo las salas ya visitadas, que se guardan con la partida), la posición y orientación del jugador, los puntos de guardado (G) y las puertas con cerradura (cerrada o abierta). Flechas o A/D cambian de planta. El plano viaja en la escena: lo rellena el menú 1 (y el 9).
- **Sillas empujables**: las 104 sillas (de oficina, giratorias, de comisario y de visita; no las butacas del auditorio) llevan un `Rigidbody` ligero y no son estáticas; el empuje al andar ya existía en `PlayerController`. Se asientan en el editor al crear el mobiliario para no moverse al empezar la partida. **Medido en Play**: una silla se desplaza ~5,6 m empujada por el jugador; 0 sillas y 0 objetos sueltos se mueven en los primeros 150 fotogramas.
- **Patrullas**: modelo nuevo de Blender (`Tools/blender/build_police_cars.py`: capó, puertas, montantes, lunas, ruedas con llanta, barra de luces, rejilla, matrícula, retrovisores) en dos versiones, `PoliceCar` quemada y `PoliceCarClean` intacta; el garaje lleva 4 intactas y 1 quemada.

**Comprobado:** compila; 62 salas con ruta y 55 enemigos sobre el NavMesh; 101 de 101 interactuables alcanzables y a la vista; oclusión rebakeada; capturas del mapa con niebla, del check rojo, de los lectores y del garaje. **Sin comprobar:** la secuencia de las calderas, el aspecto de las tarjetas 3D en el suelo, el modelo de patrulla visto de cerca, el rendimiento con 104 cuerpos rígidos, el mapa tras guardar y cargar, y el jefe 1 rompiendo estanterías en combate.

## Segunda tanda de arreglos tras jugar (2026-10-10)

- **Parpadeo del suelo (z-fighting):** 121 paredes (65 a la cota 4 y 56 a la 7,5) tenían el borde superior exactamente a la altura del suelo de la planta de arriba, así que su cara superior coincidía con la del suelo y se peleaban al dibujarse (las franjas que aparecían y desaparecían). `ComisariaGrande.Top()` da ahora 0,44 m sobre el techo en vez de 0,5 y la escena actual se parcheó bajando esas paredes 6 cm (0 quedan a ras). Rebakeada la oclusión.
- **Rótulos 3D de las puertas fuera** (se veían a través de las paredes). En su lugar, las puertas que piden tarjeta (`Puerta_Tarjeta`, `Puerta_Escalera_Norte`, `Puerta_Calabozos`) son **de acero reforzado**: sin ventanilla, con tres refuerzos oscuros por cara y una franja luminosa del color de su tarjeta (azul = seguridad, dorada = jefe); los lectores llevan un marco luminoso del mismo color.
- **Notas:** el texto negro se volvía blanco con el ratón encima (el estilo de etiqueta cambia en `hover`); ahora la tinta se fija en todos los estados (`HudArchive.Ink`).
- **Zombis sin dormidos:** ninguno queda dormido ni parado. Antes de detectarte **deambulan despacio por su sala** (`ZombieAI.wanderInRoom`, dentro del rectángulo de su sala y sobre el NavMesh) y, **si entras en su sala (o grupo de salas), se lanzan a por ti todos a la vez** (`ZombieAI.roomAggro`, `MapTracker.CurrentGroup`), aunque no te vean. La única excepción es el carroñero que come junto al cadáver (el par de 2): solo se levanta si le disparas. Medido en Play: 0 dormidos fuera de los 3 carroñeros, 29 de 43 caminando y 11 en una pausa entre destinos; al entrar en la sala de radio los 2 de esa sala te persiguen (1 de la sala contigua también, por vista); con el jugador dentro de la sala del carronero, este sigue comiendo.
- **Sin comprobar:** el aspecto de las puertas reforzadas (solo se generaron; falta verlas), la sensación de los zombis deambulando (velocidad y pausas) y su coste con las 43 IA moviéndose.
