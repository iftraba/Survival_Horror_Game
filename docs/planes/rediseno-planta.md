# Plan: esquema de la nueva planta de la comisaría (etapa B)

- **Fecha:** 2026-10-10
- **Estado:** Borrador, **pendiente de que el usuario apruebe el esquema** antes de construir nada
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

## Esquemas
Los dos esquemas (planta baja y primera) se enseñaron al usuario como dibujos en la conversación; las cotas están en las tablas.

### Planta baja (y 0, techo 3,5)
| id | Espacio | Rect (x0,z0,x1,z1) |
|---|---|---|
| G_Safe | Sala segura | −32,0,−25,12 |
| G_Wait | Espera y atención | −25,0,−8,12 |
| G_Lobby | Vestíbulo (caracol, mostrador en L) | −8,0,8,12 |
| G_Brief | Agentes | 8,0,21,12 |
| G_Armory | Armería | 21,0,32,12 |
| G_Garage | Garaje | −32,12,−18,29 |
| G_Dark | Pruebas (a oscuras) | −18,12,−8,29 |
| G_WC | Aseos | −8,12,−2,19 |
| G_Atrio | Atrio central (ascensor en x 5..8, z 23..26,5) | −8,12,8,29 |
| G_Sec | Seguridad | 8,12,20,29 |
| G_Break | Descanso | 20,12,32,29 |
| G_Lock | Vestuarios | −32,29,−20,44 |
| G_Work | Taller y almacén | −20,29,−6,44 |
| G_NHall | Hall de ingreso | −6,29,2,44 |
| G_Stair2 | Escalera norte | 2,29,10,44 |
| G_Cells | Calabozos | 10,29,32,44 |

Puertas con cerradura (nombres que se conservan): `Puerta_Vestibulo_E` (candado, 8,6), `Puerta_Pasillo_Candado` (candado, 8,17), `Puerta_Tarjeta` (tarjeta, −2,29), `Puerta_Escalera_Norte` (tarjeta, 2,35), `Puerta_Calabozos` (tarjeta del jefe, 10,35), `Puerta_Callejon` (candado, −32,40). Los vestuarios llegan al callejón por el taller, no por los calabozos (sin bloqueo cruzado con la tarjeta del jefe).

### Primera planta (y 4, techo 7,0)
Comisario (−32,0,−20,12), Conferencias (−20,0,−8,12), Memorial (−8,0,8,12), Detectives (8,0,32,12), Jefe de seguridad (−32,12,−20,29, solo por el balcón), Biblioteca (−20,12,−8,29), Galería (−8,12,8,29, hueco en −5,14,5..1,24,5), Comedor (8,12,32,29), Interrogatorios (−32,29,−14,44), Escalera del archivo (−14,29,−4,44, reja en z≈31,3), Hall norte (−4,29,10,44), Registro (10,29,22,44), Sindicato (22,29,32,44).

### Segunda planta y sótano
Segunda: antesala del archivo (pasa a **sala segura**), archivo, caseta y azotea sin cambios. Sótano: galería de servicio y galería norte de 4 m; `B_Boiler` (−32,29,−12,44), `Puerta_Calderas` y la escalera de calderas no se mueven; se fusionan bombas y depósitos; se conservan los nombres de puerta del sótano.

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
Geometría con `modelo-blender`; retratos y texturas con IA solo con prompt aprobado.

## Objetos de puzle y botín
Cizalla en el tablero del garaje (1,2 m despejados y lámpara de mesa), tarjeta de seguridad sobre la mesa del jefe de turno (Cop dormido en la silla), medallón I en el catre de la 2.ª celda, medallón II en la taquilla de la biblioteca, medallón III en la mesa de la caseta, fusible A en la caja de repuestos del almacén, B en la taquilla 7258, C «caído» junto a una rejilla de las tuberías. Las coordenadas absolutas actuales pasan a **anclas por sala**.

## Zombis (43) y munición (provisional)
- Planta baja 16, primera 13, segunda 3, sótano 11; unos 18 dormidos, 5 reptantes, 3 carroñeros; 0 en vestíbulo, segura, aseos y escalera norte.
- Cálculo (acierto 60 %: bala ≈ 19,5 PV, cartucho ≈ 56 PV, zombi 220 PV, objetivo ≥ 80 % de la vida de los zombis de cada tramo + jefe): hasta el jefe 1 ~180 balas y ~66 cartuchos; hasta el jefe 2 ~96 y ~34; arenas 18+12 y 24+12; sprays 8+4.
- **Límite de inventario:** 8 casillas (+ hasta 6 de riñoneras) con pilas de 30/12/1. Se reparte por la ruta para llevar ≤ ~5 casillas de munición y el resto al baúl; se valora `maxStack` balas 45, cartuchos 18, spray 3.

## Luz (mixta)
Una lámpara por ~40 m² (2–8 por sala); spot 20 (halls 16, servicio 12), rango ~7,5 m; relleno 6/4 en una de cada dos lámparas de salas de más de 100 m²: ~176 lámparas (hoy 129). 25–30 % de las del atrio, comedor y galerías arrancan apagadas. **A oscuras con interruptor y piloto naranja (6):** pruebas, calabozos, biblioteca, interrogatorios, almacén y laboratorio del sótano. Color: vestíbulo ámbar, garaje sodio, calabozos frío verdoso, servicio azulado, sótano rojo de emergencia. Render Forward+ comprobado (sin tope de 8 luces por objeto); `ShadowBudget` sigue limitando las sombras.

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
Cambio de planta completo: hay que repetir los menús 1 a 7 y volver a comprobar NavMesh, puertas y progresión. Los nombres de puerta usados por otros kits se conservan. Cantidades de munición y luz provisionales hasta jugar. Las estimaciones de esfuerzo en Blender (prioridad 1: 8–10 h; prioridad 2: 6–8 h) son del agente de diseño y no están contrastadas.

## Resultado
Pendiente de aprobación.
