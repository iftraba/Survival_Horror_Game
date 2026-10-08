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
