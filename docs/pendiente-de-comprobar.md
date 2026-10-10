# Pendiente de probar y comprobar (comisaría grande, rediseño de la planta)

Lista viva de lo que **no se ha comprobado** o solo se vio a medias. Se consulta con la skill `pendiente-comprobar`. Al comprobar algo, se tacha aquí con fecha y cómo se hizo; si falla, se anota la salida real. Última actualización: 2026-10-10 (tras la etapa G).

Leyenda: **[PC]** se puede comprobar con herramientas (Play + MCP) · **[JUGAR]** necesita al usuario jugando · **[ETAPA]** depende de una etapa por hacer.

## 1. Recorrido y progresión
- [x] [PC] (2026-10-10, etapa G) Alcance de los interactuables: 96 de 96 (objetos clave, notas, botín, interruptores, terminales, baúles, cuadro, ascensor, monumento, reja, portón) tienen un sitio donde el jugador (cápsula libre de 0,33 m) los tiene a ≤2,2 m de la esfera del `PlayerInteractor` y a la vista desde el pecho. La prueba encontró 3 notas (turno, archivero, mantenimiento) que quedaban tapadas por la caja del escritorio; se corrigió (caja recortada a la malla, collider del papel 3 cm más alto, y las anclas exigen un sitio desde el que se alcance). **Falta andar de verdad con el jugador y pulsar E.**
- [ ] [PC] Los 12 objetos que van dentro de taquillas (3 con código): abrir cada taquilla y coger lo de dentro.
- [x] [PC] (2026-10-10) Las 8 notas se alcanzan y se ven (ver arriba) y, en el grafo de progresión, cada código tiene su nota en una sala alcanzable antes de su taquilla (4519 en pruebas, 0832 en vestuarios, 7258 en la sala segura del sótano). Leer el texto en el HUD sigue sin probarse.
- [ ] [PC] Grafo de progresión incluyendo la sala de calderas y el portón final (el script de la etapa E no cubre `B_Boiler`, cota −6,5). Repetir en Play con puertas reales, no solo el grafo de `Define()`.
- [ ] [PC] (2026-10-10: comprobado por datos que cada puerta con llave, el ascensor, el portón, el monumento y el cuadro de fusibles piden un objeto que existe en la escena o lo suelta un jefe; falta abrir cada una de verdad) Las puertas con llave abren de verdad con su objeto: 3 candados (cizalla), 3 lectores de tarjeta recolocados (atrio→ingreso, ingreso→escalera norte, custodia→calabozos: ver que el lector está visible y de cara a quien llega), puerta sin corriente, reja del archivo, ascensor.
- [ ] [PC] `OneWayDoor` desde los dos lados con el jugador de verdad (solo se probó teletransportando): garaje↔bombas y galería↔hall norte; que los zombis no la abran por el lado malo; que tras abrirla queda abierta; y **tras guardar y cargar partida**.
- [ ] [PC] Escaleras recorridas andando con el jugador (caracol, norte con sus dos tramos, archivo, garaje, custodia): hasta ahora solo NavMesh y capturas.
- [ ] [PC] Rampa del garaje y persiana enrollable (cerrada): que el jugador no puede atravesarla; salida por el callejón.
- [ ] [PC] Ascensor: llave del ascensor, viaje G↔sótano, llegada en el sitio correcto (nudo B_HubB).
- [ ] [PC] Guardado: medallones, fusibles, reja, ascensor, interruptores de la sala de pruebas (los dos sincronizados tras cargar) y `OneWayDoor`.
- [ ] [PC] Sala segura de la antesala del archivo: terminal y baúl accesibles, no tapados por el banco ni por la escalera, a unos 12 m del jefe 1.
- [ ] [PC] Softlock: ¿puede quedarse el jugador sin salida o sin munición dentro de las arenas? (puertas atrancadas con el jefe; reservas dentro del disparador).
- [ ] [PC] Camino de retorno: tras el jefe 1, ¿se vuelve sin pasar por el paso de un solo sentido atrancado?

## 2. Salas y mobiliario (etapa D)
- [ ] [PC] Orientación de las patrullas del garaje (larga en X a yaw 0, se vio bien a ojo, sin medir) y que dejan carril libre.
- [ ] [PC] Colisiones de la balaustrada de la galería (el jugador no cae al hueco, no la atraviesa).
- [ ] [PC] Camastros y rejas de las celdas de cerca; la primera celda de la fila sur queda libre (entrada). Son 19 celdas, no 20.
- [ ] [PC] Orientación del mapa enmarcado de la conferencia (quad a yaw 180) y de las banderas.
- [x] [PC] (2026-10-10, etapa G) Hojas de contacto de las 45 salas de más de 20 m² (vista desde una esquina, luces como en el juego): todas amuebladas y con luz. Quedan sin ver de cerca la caseta (16 m²), el vestíbulo con el mostrador nuevo y los detalles del comedor; el archivo sale muy oscuro.
- [x] [PC] (2026-10-10, etapa G, tras menús 3-7) Prueba de rutas y puertas con los obstáculos apagados: 62 de 62 salas alcanzables desde el vestíbulo, 54 de 54 puertas con paso libre a 1,1 m de cada lado, 55 enemigos (43 + 10 de la verja + 2 jefes) sobre el NavMesh. Repetir tras cualquier cambio de los menús 3-6.
- [ ] [PC] Cajas de collider recortadas por las anclas: que ningún mueble ha quedado atravesable o con huecos (zombis, balas).
- [ ] [ETAPA] Modelos nuevos pendientes: placas de puerta, señal de salida, botiquín, lámpara colgante, marco de ventana, cuadros/retratos (IA solo con prompt aprobado), furgoneta de detenidos, tablero de herramientas, bolsas de pruebas, mesa de comedor, llavero. Y `WallDressingKit` (apuntado a la raíz nueva, no ejecutado).

## 3. Zombis, botín y equilibrio
- [ ] [PC] Zombis: que cada uno despierta, persigue y puede cruzar sus puertas; reptantes y carroñeros se comportan (el carroñero solo despierta si le disparan); los 18 en letargo despiertan con ruido o tiro. Están de pie (no hay animación de dormido): decidir si se acepta.
- [ ] [PC] Los dos encuentros a mano: Cop junto a la mesa de la tarjeta y reptante junto al coche del garaje (hoy no hay disparador «sale al coger la cizalla»).
- [ ] [PC] Los dos primeros encuentros (espera y garaje) son suaves y están cerca; ningún zombi despierto pegado a una sala segura o a un guardado.
- [x] [PC] (2026-10-10, etapa G) Recuento real en la escena (suelo, muebles y taquillas): hasta el jefe 1 182 balas, 62 cartuchos y 8 sprays; sótano industrial 96, 32 y 4; arena 1 18+12; arena 2 24+12. Coincide con el presupuesto. Pendiente: cuántas casillas hay que llevar con pilas 45/18/3 y que el `maxStack` nuevo no rompa el guardado ni el HUD.
- [ ] [PC] Objeto del cuadro eléctrico que cae al suelo (no hay banco en esa sala) y los que el log marque «al suelo».
- [ ] [JUGAR] ¿Alcanza la munición? ¿El ritmo de zombis es justo? ¿Los sprays están bien repartidos? (cifras provisionales, calculadas con 60 % de acierto).
- [ ] [JUGAR] ¿Se encuentran los objetos clave sin pistas externas? ¿El brillo y la lámpara de mesa bastan?
- [ ] [JUGAR] El jefe 1 en la arena nueva (36×29 m, antes 13,6 m): ¿se puede huir dando vueltas?, ¿rompe los muebles y no las estanterías de obra?, ¿alcanza la reserva?
- [ ] [JUGAR] El jefe 2 y la reserva de la caldera; el portón final.

## 4. Luz (etapa F, hecha el 2026-10-10)
- [x] Menú 7 adaptado, lámparas por m², color por zona, lámparas rotas, 6 salas a oscuras con interruptores (2026-10-10: luminancia medida en Play, mediana 5,5 % → 8,0 %; interruptores accionados por script en calabozos, laboratorio, biblioteca y pruebas, anillo sincronizado).
- [ ] [JUGAR] ¿8 % de mediana es la penumbra buscada? (valor provisional; ajustar con 7a → 4 → 7). Hoy las salas con interruptor apagado quedan a 1-2 %.
- [ ] [PC] Los interruptores se accionan con la tecla E desde el sitio donde está el jugador y se ven (piloto naranja): solo se vieron en captura y accionados por script.
- [ ] [PC] Guardado de los interruptores en anillo (SaveSystem los guarda por orden): cargar partida con la biblioteca encendida y ver que los tres siguen iguales.
- [ ] [PC] Fugas de luz por fuera del edificio (el archivo las tenía) y luz de la rampa, la explanada y el callejón (9 luces añadidas, sin ver).
- [ ] [PC] Salas pequeñas con una lámpara muy cerca (G_Atrio4 31 %, G_Atrio3 14 %, caseta 17 %) y el atrio, aún oscuro en la zona de entrada.
- [ ] [PC] `ShadowBudget` con 252 lámparas y las luces de mesa de los objetos clave (límite de 8 sombras).
- [ ] [PC] Lámparas rotas (5) y parpadeantes (5): que se ven bien y no molestan.

## 5. Rendimiento
- [x] [PC] (2026-10-10) Occlusion culling horneado (menú 8): llamadas y CPU/GPU medidos antes y después en seis escenarios (ver `docs/rendimiento.md`); CPU por debajo de 16,7 ms en los seis.
- [ ] [JUGAR] Con la oclusión horneada: ¿parpadean objetos al girar la cámara, sobre todo en los huecos de la galería y del caracol, o en la escalera del archivo (la peor diferencia, 0,42 % de la imagen)?
- [x] [PC] (2026-10-10) Dormir la IA de los zombis lejanos: descartado tras medir (con oclusión, quitar IA, agentes y animadores cambia la CPU dentro del ruido; ver `docs/rendimiento.md`).
- [ ] [PC] Idea sin hacer ni medir: puertas como `OcclusionPortal` y combinar las piezas de las puertas. Solo si en una build a 1080p no llegan los 60 fps.
- [ ] [PC] (2026-10-10, etapa G: el editor va a 10 fps con el MCP, el contador de frame timing da GPU 0 ms y no sirve; render a 1920×1080 con una cámara temporal y lectura de píxeles, en el editor: atrio 8-14 ms, garaje 8-20 ms, galería 8 ms, archivo 13 ms, espera 13 ms; con las sombras de los focos desactivadas el atrio baja de 14 a 8 ms; la medición es ruidosa (±5 ms) y no hay línea base de antes de la etapa F; 428 luces en la escena) Skill `performance-audit` en 1080p con las 252 lámparas: GPU en el atrio con el hueco (objetivo provisional ≤ 12 ms), F3 (`FpsCounter`), recuento de luces.
- [ ] [JUGAR] fps en una **build** (no se ha medido; la build del escritorio no lleva nada de la planta nueva hasta `/recompile`, que solo se hace si lo pide el usuario).

## 6. Exterior y resto
- [ ] [ETAPA] Puerta de coches de la verja norte (por donde entrarían los vehículos).
- [ ] [PC] Intro y puerta que se atranca con la planta nueva; la cinemática se salta bien.
- [ ] [PC] Los zombis de la verja (10) usan solo modelos aprobados.
- [ ] [PC] Escena guardada limpia (sin objetos temporales) y sin avisos de consola nuevos.
- [ ] [PC] Candado: caída y desvanecimiento en Play de los tres candados nuevos y brillo de los objetos clave a 6 m.

## 7. Documentación y repositorio
- [ ] Revisar que `docs/estado-actual.md` y `docs/resumen-sesion-comisaria.md` reflejan la planta v3 (hoy el detalle está en `docs/rediseno-comisaria.md`).
- [ ] Decidir qué hacer con los 5 prefabs de zombi descartados (siguen en el proyecto sin usar) y con `Assets/_Recovery` y `Tools/raw_generated` (sin commit, no son del rediseño).

## Cómo se prueba aquí (recordatorio)
- Nunca compilar ni editar scripts con Unity en Play; no ofrecer recompilar la build.
- Play desde `execute_code`: `EditorApplication.isPlaying = true`; avanzar con `EditorApplication.Step()` desde un callback; saltar la intro con el campo `skip` de `IntroCutscene`; para el NavMesh apagar los `NavMeshObstacle` tras ~15 fotogramas y medir a ~40; jugador a `suelo + 1,05`; cámara con `yaw`/`pitch` de `ThirdPersonCamera`.
- Orden de menús tras cambiar la planta: 1 → 2 → 3 → 4 → 5 → 6 (el 7 no hasta la etapa F).

## 8. Tras la primera partida completa (2026-10-10, fases 1 a 3 del plan `feedback-primera-partida-completa.md`)
- [ ] [JUGAR] Jefe 1: ¿rompe las estanterías al embestir y se va abriendo la arena? (`PropBreaker` ya acepta `Mobiliario`; solo probado con una esfera, no en combate). ¿Sigue sin despertar con disparos de la planta de abajo?
- [ ] [JUGAR] Zombis dormidos (9 al azar, el Cop y el reptante del garaje): ¿despiertan bien al acercarte? ¿Los carroñeros (3) comen junto a su cadáver y, al dispararles, se levantan normales?
- [ ] [JUGAR] Secuencia de la reja (vista en Play) y de la puerta de las calderas (sin ver): ¿ritmo?, ¿se puede saltar?, ¿no molesta con zombis cerca?
- [ ] [JUGAR] Munición al principio: ¿alcanza hasta abrir el garaje y el vestíbulo este? ¿Sobra menos en la segunda mitad?
- [ ] [JUGAR] Objetos clave: tirar bloqueado, check rojo en el inventario y el baúl, desaparecen al tirarlos una vez usados; las tarjetas se distinguen en el suelo y en los lectores.
- [ ] [JUGAR] Mapa (M): niebla, flechas, guardado y carga (las salas visitadas se guardan en `SaveData.mapRooms`; sin ejecutar).
- [ ] [JUGAR] Sillas empujables: ¿se mueven bien?, ¿se atascan en puertas o escaleras?, ¿pesan en rendimiento (104 cuerpos rígidos, sin medir)?
- [ ] [JUGAR] Nuevos puntos de guardado (ingreso y memorial) y la antesala del archivo: ¿se encuentran, sin muebles que los tapen?
- [ ] [PC] Patrullas nuevas de cerca, tarjetas 3D en el suelo y lectores con franja y rótulo desde varios ángulos.
- [ ] [PC] Estilo de las notas en las 8 notas (solo vista la del jefe de seguridad) y que ninguna se corta en la hoja.

## 9. Segunda tanda tras jugar (2026-10-10)
- [ ] [JUGAR] Zombis que deambulan: ¿velocidad y pausas bien?, ¿se salen de su sala o se atascan?, ¿atacan todos al entrar y no antes?
- [ ] [PC] Puertas reforzadas y lectores con marco luminoso: ver de cerca las tres (la azul dos veces y la dorada) y que los refuerzos se mueven con la hoja al abrir.
- [ ] [JUGAR] Parpadeo del suelo: ¿ha desaparecido en el sitio de la captura y en el resto? (121 paredes bajadas 6 cm; comprobado solo en datos, no a ojo).
- [ ] [JUGAR] Notas con el ratón encima: la tinta ya no cambia a blanco (corregido en el estilo, sin ver en pantalla).
