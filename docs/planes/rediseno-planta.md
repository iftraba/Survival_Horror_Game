# Plan: esquema de la nueva planta de la comisaría (etapa B)

- **Fecha:** 2026-10-10
- **Estado:** Borrador revisado (2026-10-10, 2.ª versión: garaje y calabozos al sótano), **pendiente de que el usuario apruebe el esquema** antes de construir nada
- **Autor:** Claude (Sonnet), con un agente de diseño; parte de `docs/planes/fase-f-parte-2-rediseno-planta.md`
- **Rama:** rediseno-comisaria

## Objetivo
Sustituir los 70 espacios iguales unidos por pasillos de 2,5 m por unos 45 con propósito, halls anchos, circuitos y salas con relato, conservando la cadena de puzles y la huella de 64×44 m.

## Concepto
Noche de las 03:40: el relevo no llegó y cada grupo se atrincheró en un sitio; cada sala cuenta qué defensa falló. Huella, caracol, escalera norte, escalera del archivo, ascensor, callejón y fachada **no cambian** (el exterior y la intro dependen de ellos).

- **Eje central (x −8..8):** vestíbulo (caracol) → **atrio de doble altura** (ascensor) → hall de ingreso (escalera norte). Los pasillos desaparecen: son atrio, halls y paredes entre salas.
- **Ala oeste:** público y mantenimiento (espera, segura, garaje, pruebas, vestuarios, taller). **Ala este:** policial (agentes, armería, seguridad, descanso, calabozos).
- **Primera planta:** sur = mando (comisario, conferencias, memorial) y detectives; centro = biblioteca, **galería con balaustrada alrededor de un hueco de 6×10 m sobre el atrio** y comedor; norte = interrogatorios, escalera del archivo, registro, sindicato.
- **Segunda planta y sótano:** casi igual. Sótano con galerías de 4 m y bombas y depósitos fundidos.
- Espacios: baja 16, primera 13, segunda 3, sótano 13 = **45** (antes ~70).

## Cambio de la 2.ª versión (feedback del usuario, 2026-10-10)
«El garaje y los calabozos no tienen sentido en esa planta: mejor una planta inferior. El garaje debe tener salida al exterior (puerta enrollable vertical, cerrada de momento) y ser amplio, con varios vehículos. Los calabozos, unas 20 celdas.» Decisiones del usuario sobre ello:
- **Rampa por el callejón oeste:** ~4,5 m de ancho y ~32 m de largo (≈14 % de pendiente), de z 44 a z 12 entre x −42 y −37, dentro de la verja, hasta una explanada plana a −4,5 m (x −42..−32, z 4..12) y una **puerta enrollable cerrada** en el muro oeste del garaje. Hay que excavar el suelo del callejón (`G_Alley`), levantar muros de contención, dar a la rampa un collider/NavMesh (como la rampa del caracol) y **reubicar el pie de la escalera de incendios** (balcón z 17..26 y tramo a la azotea z 6..11, hoy en x −36..−32). Si la verja estorba, se desplaza ~4 m al oeste en `ComisariaGrandeExterior`.
- **Dos escaleras y zona separada:** escalera abierta desde la sala de espera (ala oeste, x −14..−11,6) al garaje (la cizalla está ahí y se necesita al empezar); escalera desde el hall de ingreso (tarjeta de seguridad) hasta la custodia y los calabozos (tarjeta del jefe). La zona garaje + calabozos queda separada del sótano industrial por pared; se une solo con una **puerta de un solo sentido** (`OneWayDoor`) en el muro este del garaje (−8, 5), que se abre desde el lado de las bombas: sin ella se llegaría a fusibles y calderas sin pasar por el jefe 1; con ella, tras el jefe 1, hay atajo al garaje.
- **Planta baja:** donde estaban garaje y calabozos van **sala de radio y despachos** (oeste, −32,12,−18,29, con ventanas al callejón) y **sala de interrogatorios con cristal unidireccional y sala de observación** (noreste, 10,29,32,44). En la primera planta, `F_Inter` pasa a **oficinas administrativas y archivo auxiliar**.
- **Vehículos:** 4–5 coches patrulla con el modelo actual (variando color, desgaste y puertas abiertas) y una **furgoneta de detenidos nueva en Blender** (~2 h); garaje de unos 24×25 m. Rincón del taller con el banco y el tablero donde cuelga la cizalla.
- **Calabozos:** 20 celdas de ~2,2×3 m en dos filas de 10 contra los muros norte y sur del bloque (22×15 m), con pasillo central de ~9 m, mostrador del carcelero, banco de detenidos y cámara; medallón I en el catre de la 2.ª celda.

## Esquemas
Los esquemas (planta baja, primera y **sótano**) se enseñaron al usuario como dibujos en la conversación; las cotas están en las tablas. **En la planta baja de abajo, `G_Garage` pasa a «Radio y despachos» y `G_Cells` a «Interrogatorios y observación».**

### Planta baja (y 0, techo 3,5)
| id | Espacio | Rect (x0,z0,x1,z1) |
|---|---|---|
| G_Safe | Sala segura | −32,0,−25,12 |
| G_Wait | Espera y atención | −25,0,−8,12 |
| G_Lobby | Vestíbulo (caracol, mostrador en L) | −8,0,8,12 |
| G_Brief | Agentes | 8,0,21,12 |
| G_Armory | Armería | 21,0,32,12 |
| G_Radio | Radio y despachos (antes garaje) | −32,12,−18,29 |
| G_Dark | Pruebas (a oscuras) | −18,12,−8,29 |
| G_WC | Aseos | −8,12,−2,19 |
| G_Atrio | Atrio central (ascensor en x 5..8, z 23..26,5) | −8,12,8,29 |
| G_Sec | Seguridad | 8,12,20,29 |
| G_Break | Descanso | 20,12,32,29 |
| G_Lock | Vestuarios | −32,29,−20,44 |
| G_Work | Taller y almacén | −20,29,−6,44 |
| G_NHall | Hall de ingreso | −6,29,2,44 |
| G_Stair2 | Escalera norte | 2,29,10,44 |
| G_Interr | Interrogatorios y observación (antes calabozos) | 10,29,32,44 |

Puertas con cerradura (nombres que se conservan): `Puerta_Vestibulo_E` (candado, 8,6), `Puerta_Pasillo_Candado` (candado, 8,17), `Puerta_Tarjeta` (tarjeta, −2,29), `Puerta_Escalera_Norte` (tarjeta, 2,35), `Puerta_Callejon` (candado, −32,40). `Puerta_Calabozos` (tarjeta del jefe) **pasa al sótano**, entre la custodia y el bloque de celdas (10,36 abajo). Los vestuarios llegan al callejón por el taller.
- Nuevas en la planta baja: hueco y escalera al garaje en la sala de espera (x −14..−11,6, z 3..9,5); la escalera norte (`G_Stair2`) gana un segundo tramo que **baja** a la custodia (x 7..9,4, z 31,6..40).
- Al subir el garaje a un sótano, la **cizalla** (y `Puerta_Garaje`) ya no está en la planta baja: la escalera de la sala de espera está abierta desde el principio.

### Primera planta (y 4, techo 7,0)
Comisario (−32,0,−20,12), Conferencias (−20,0,−8,12), Memorial (−8,0,8,12), Detectives (8,0,32,12), Jefe de seguridad (−32,12,−20,29, solo por el balcón), Biblioteca (−20,12,−8,29), Galería (−8,12,8,29, hueco en −5,14,5..1,24,5), Comedor (8,12,32,29), Interrogatorios (−32,29,−14,44), Escalera del archivo (−14,29,−4,44, reja en z≈31,3), Hall norte (−4,29,10,44), Registro (10,29,22,44), Sindicato (22,29,32,44).

### Segunda planta
Antesala del archivo (pasa a **sala segura**), archivo, caseta y azotea sin cambios.

### Sótano (y −4,5, techo −1) — rehecho
| id | Espacio | Rect (x0,z0,x1,z1) | Notas |
|---|---|---|---|
| B_Garage | **Garaje** (5 patrullas, furgoneta, taller) | −32,0,−8,25 | Puerta enrollable en el muro oeste (−32, z 6..10); escalera a la sala de espera; cizalla en el tablero |
| B_Custody | **Custodia** | 2,29,10,44 | Escalera desde el hall de ingreso (tarjeta de seguridad) |
| B_Cells | **Calabozos, 20 celdas** | 10,29,32,44 | `Puerta_Calabozos` con tarjeta del jefe, a oscuras con interruptor junto a la puerta |
| B_Pump | Bombas y depósitos | −8,0,10,10 | Lado este del paso de un solo sentido al garaje |
| B_Mach | Máquinas y taller | 10,0,24,10 | |
| B_Store | Almacén | 24,0,32,10 | Fusible A |
| B_Gal | Galería de servicio (tuberías vistas) | −8,10,32,14 | Antes sala de «tuberías»: aquí cae el fusible C |
| B_Safe | Segura | −8,14,0,26 | Terminal y baúl |
| B_Hub | Nudo del ascensor | 0,14,8,26.5 | Hueco x 5..8, z 23..26.5 |
| B_Lab | Laboratorio | 8,14,20,26 | Fusible B en la taquilla 7258 |
| B_Fuse | Cuadro eléctrico | 20,14,32,26 | |
| B_CN | Galería norte | −32,26,32,29 | `Puerta_Sin_Corriente` en (−12, 27,5) |
| B_Control | Control | −12,29,2,44 | |
| B_Boiler | Calderas (hundida 2 m) | −32,29,−12,44 | **Sin cambios** (`Puerta_Calderas` en −22,29; escalera de calderas; jefe 2; portón) |
| Fuera | Rampa y explanada | −42..−37 × 12..44 y −42..−32 × 4..12 | Excavación en el callejón |

De ~13 espacios industriales a 11 más garaje, custodia y calabozos. Se conservan los nombres de puerta del sótano (`Puerta_Bombas`, `Puerta_Maquinas`, `Puerta_TallerS`, `Puerta_AlmacenS`, `Puerta_SeguraS`, `Puerta_Laboratorio`, `Puerta_Cuadro`, `Puerta_Control`) y la lógica de `Ascensor_Sotano`.

## Conexión
Salas contiguas se comunican (enfilada) y salen 8 bucles (oeste y este de la baja, núcleo, vertical por la escalera norte, primera planta, mando, callejón, sótano). Opcionales, por decidir:
- **Barricada de un solo sentido** en la galería (`Puerta_Pruebas_Primera`, 0,29): atranca desde la galería y se abre desde el hall norte; ahorra ~60 m al volver del archivo. Componente nuevo `OneWayDoor` (~40 líneas).
- **Parada del ascensor en la primera planta** con la misma llave.
- **Sala segura en la antesala del archivo**: el terminal pasa de ~150 m a ~12 m del jefe 1; terminal y baúl hay que anclarlos a mano (el generador los pondría dentro del hueco de la escalera).

## Salas con relato
Cada sala lleva 3–5 elementos con función. Ejemplos: vestíbulo (mostrador en L con monitor y pistola de servicio, banco volcado contra la puerta, tablón de buscados, rótulo); garaje (coche con el capó abierto, banco con tablero de herramientas donde cuelga la cizalla, persiana fija a la calle); pruebas (estanterías con bolsas, mesa con el libro de registro donde está el código 4519, jaula de armas); armería (jaula del armero, mesa de limpieza, taquilla con teclado); seguridad (puesto del jefe de turno con la tarjeta sobre la mesa, llavero de pared); calabozos (mostrador del carcelero con llaves, banco de detenidos). Se reponen del diseño del usuario: estrado con dos escalones, atril, mapa enmarcado, 4 banderas con pliegues, alfombra y mesa de reuniones del comisario (`ComisariaV2Office`: `Memorial()`, `Commissioner()`, `Conference()`, `Flag()` pasan a `internal`; conferencias girada 90° para que el estrado no choque con la puerta).

## Modelos nuevos (prioridad)
1. Placa de puerta con nombre de sala, señal de salida emisiva, botiquín de pared, lámpara colgante, marco de ventana con persiana (decorativo), plano de evacuación generado por script desde `Define()`.
2. Cuadros/retratos, tablero de herramientas, mostrador de ingreso con tabla de estatura, bolsas de pruebas, mesa de comedor, llavero de pared.
3. **Por el cambio de garaje y calabozos:** furgoneta de detenidos (~2 h), puerta enrollable con carril y cajón, barrotes y camas/catres para 20 celdas (se reutilizan los de `Cells()`, a replicar), mostrador del carcelero con llaveros, elevador de coche o gato hidráulico y banco de taller (reutilizar `Workbench`), muros de contención y barandilla de la rampa (geometría de `Box`, sin modelo). **Comprobado:** solo existe `PoliceCar.fbx` (coche patrulla quemado, carrocería chamuscada y lunas rotas, `build_exterior.py`); no hay versión sin quemar. Las patrullas del garaje serán esas, con puertas abiertas y desgaste variado: «los coches que no llegaron a salir». Si el usuario las quiere intactas, hay que hacer una variante en Blender (~2 h).
Geometría con `modelo-blender`; retratos y texturas con IA solo con prompt aprobado.

## Objetos de puzle y botín
Cizalla en el tablero del garaje (1,2 m despejados y lámpara de mesa), tarjeta de seguridad sobre la mesa del jefe de turno (Cop dormido en la silla), medallón I en el catre de la 2.ª celda, medallón II en la taquilla de la biblioteca, medallón III en la mesa de la caseta, fusible A en la caja de repuestos del almacén, B en la taquilla 7258, C «caído» junto a una rejilla de las tuberías. Las coordenadas absolutas actuales pasan a **anclas por sala**.

## Zombis (43) y munición (provisional)
- Con el garaje y los calabozos en el sótano: planta baja 13, primera 13, segunda 3, sótano 14 (garaje 3 incl. reptante bajo un coche, calabozos 2 incl. reptante en una celda, resto industrial 9); unos 18 dormidos, 5 reptantes, 3 carroñeros; 0 en vestíbulo, segura, aseos, escalera norte y custodia. **Los dos primeros encuentros del juego (garaje y espera) quedan cerca**: el primero debe ser suave (el que está bajo el coche solo sale al coger la cizalla).
- Cálculo (acierto 60 %: bala ≈ 19,5 PV, cartucho ≈ 56 PV, zombi 220 PV, objetivo ≥ 80 % de la vida de los zombis de cada tramo + jefe): hasta el jefe 1 ~180 balas y ~66 cartuchos; hasta el jefe 2 ~96 y ~34; arenas 18+12 y 24+12; sprays 8+4.
- **Límite de inventario:** 8 casillas (+ hasta 6 de riñoneras) con pilas de 30/12/1. Se reparte por la ruta para llevar ≤ ~5 casillas de munición y el resto al baúl; se valora `maxStack` balas 45, cartuchos 18, spray 3.

## Luz (mixta)
Una lámpara por ~40 m² (2–8 por sala); spot 20 (halls 16, servicio 12), rango ~7,5 m; relleno 6/4 en una de cada dos lámparas de salas de más de 100 m²: ~176 lámparas (hoy 129). 25–30 % de las del atrio, comedor y galerías arrancan apagadas. **A oscuras con interruptor y piloto naranja (6):** pruebas, calabozos, biblioteca, interrogatorios, almacén y laboratorio del sótano. Color: vestíbulo ámbar, garaje sodio, calabozos frío verdoso, servicio azulado, sótano rojo de emergencia. Render Forward+ comprobado (sin tope de 8 luces por objeto); `ShadowBudget` sigue limitando las sombras.

## Decisiones ya tomadas por el usuario (2026-10-10)
Rampa por el callejón oeste; dos escaleras y zona garaje + calabozos separada del sótano industrial con paso de un solo sentido; radio y despachos arriba / interrogatorios y observación; patrulla reutilizada + furgoneta nueva; 20 celdas; solo zombis aprobados; menos zombis y munición para casi todos; luz mixta.

## Decisiones para el usuario
1. Atrio de doble altura con galería y hueco en la primera planta (recomendado: sí).
2. Sala segura en la antesala del archivo (sí).
3. Barricada de un solo sentido y parada de ascensor en la primera planta (sí a las dos).
4. `maxStack`: spray 3 y munición 45/18 (sí).
5. Brillo de objetos clave: ya hecho con luz pulsante; añadir lámpara de mesa encendida junto a los que están sobre mesas (sí).
6. ~18 zombis dormidos de 43 (42 %).
7. Modelos con Blender (geometría) e IA (retratos), con prompt aprobado antes de enviar.
8. Zombi del comisario sentado en su sillón (opcional).

## Riesgos
- **Rampa del garaje:** excavar el callejón y cortar `G_Alley`, muros de contención, collider y NavMesh de la rampa (como la del caracol, con voxel 0,1 m), reubicar la escalera de incendios y a lo mejor desplazar la verja; es lo más delicado de la etapa C y se hará primero y probado aparte.
- **El garaje es la primera zona jugable:** la cizalla (candado de la puerta este del vestíbulo) está allí; la escalera de la sala de espera no lleva puerta con llave. Las puertas de un solo sentido (`OneWayDoor`) hay que probarlas desde los dos lados.
- **Orden del puzle:** con la separación del sótano, no hay forma de llegar a fusibles ni calderas sin el ascensor (llave del jefe 1); hay que comprobarlo con la simulación por grafo.
- Cambio de planta completo: hay que repetir los menús 1 a 7 y volver a comprobar NavMesh, puertas y progresión. Los nombres de puerta usados por otros kits se conservan. Cantidades de munición y luz provisionales hasta jugar. Las estimaciones de esfuerzo en Blender (prioridad 1: 8–10 h; prioridad 2: 6–8 h) son del agente de diseño y no están contrastadas.

## Resultado
Pendiente de aprobación.
