# Plan: rehacer el diseño de la comisaría (fase F, parte 2)

## Contexto

El usuario probó la comisaría grande y la rechaza en lo que importa para jugarla: pasillos estrechos y sin carácter, salas iguales conectadas solo por puertas, objetos colocados «al tuntún», objetos de puzle que no se distinguen, munición insuficiente, zombis descartados otra vez en la escena, candado que no se corta, y salas oscuras. Los puzles en sí están bien y la única parte que le gusta es lo que él diseñó al subir la escalera (memorial, conferencias, despacho del comisario).

Yo comprobé que la cadena de puzles se podía completar y que no había bloqueos, pero eso no mide si el edificio parece real, se recorre con sentido, se ve, ni si la luz funciona. Tampoco medí a qué se parecía lo que se construyó: las salas son cajas de 12×12 con 2–4 muebles, y los pasillos miden 2,5 m. Este plan arregla eso.

**Decisiones del usuario (ya tomadas):**
- **Planta:** rehacerla (menos salas, más grandes y con carácter; pasillos anchos o vestíbulos; circuitos y atajos). Se conserva la cadena de puzles.
- **Zombis:** solo Civil, Cop, Girl, Oficial, OficialReptante, Carronero y Pxl1/Pxl2 (fuera Ejecutivo, Infectado, Mecánico, Paciente, Policía y Pxl3).
- **Equilibrio:** menos zombis (~40–45) y munición para matar a casi todos.
- **Luz:** mixta (unas salas encendidas, otras a oscuras con interruptor según su historia).

## Hallazgos verificados (solo lectura, 2026-10-10)

| Tema | Dato |
|---|---|
| Zombis en la escena | 74: **37 son de los 5 modelos generados descartados** (Mecánico 11, Policía 11, Ejecutivo 7, Infectado 4, Paciente 4) y 4 de los 10 de la verja. Causa: `Normal` en `ComisariaGrandeEnemies.cs:22` y la lista de `ComisariaGrandeExterior.cs:157`. `Zombie_Policia` además está roto (clips humanoides sobre un rig genérico). Los aprobados (Civil, Cop, Girl, Oficial, Carronero, Pxl) usan el juego de animaciones unificado (`docs/enemies.md`, 2026-10-08). |
| Pasillos | Todos de 2,5 m; casi todo son salas de 12×12 con una puerta al pasillo; topología casi de árbol (`ComisariaGrande.cs` Define 62–210). |
| Sala de 144 m² | Una sola lámpara (spot 28, rango ~9,2) en 32 salas; la comisaría original tenía el doble de intensidad y varias por sala. Solo hay **1** `LightSwitch` en todo el nivel. Ambiente Flat (0,2; 0,21; 0,27). Render **Forward+** (comprobado): no hay tope de 8 luces por objeto. |
| Candado | `LockVisual` apaga el candado al desbloquear (en el código está bien) pero **no hay animación ni sonido**; los 3 candados cuelgan de `Leaf` con Y=0 (la hoja está a 1,2 m). Causa de «sigue puesto en el aire»: no reproducida, hay que verla en Play. |
| Objetos clave | Se ponen en coordenadas fijas (`PutOn`) encima del primer collider; el botín cae sobre un mueble al azar; `Pickup` no tiene brillo ni contorno. |
| Munición | 126 balas + 56 cartuchos recogibles + 24 balas y 12 cartuchos de reservas; con acierto del 60 % la cobertura real es ~24 % (59 % con acierto perfecto). |
| Inventario | 8 casillas (+ hasta 6 de riñoneras); pila de 30 balas, 12 cartuchos, **1 spray**. Esto limita cuánta munición se puede repartir. |
| Lo que le gusta | `ComisariaV2Office.cs`: `Memorial()` 114–150, `Commissioner()` 153–187, `Conference()` 230–267, `Flag()` 191 (estrado, mapa, 4 banderas, mesa de reuniones, alfombra). En la grande se perdieron; son `private static`. |
| Sin usar | 7 modelos `Wall*` (reloj, extintor, tablón, tuberías, radiador, rejilla, panel eléctrico); `WallDressingKit.cs` busca la raíz `--- LEVEL ---` en vez de `--- COMISARIA V2 ---`. No existen carteles, cuadros ni planos. |

## Enfoque: etapas con puertas de aprobación

El trabajo es grande (varias sesiones). Para no rehacer nada, primero lo que no depende del trazado y después el trazado con un esquema que el usuario aprueba antes de construir.

### Etapa A: arreglos independientes del trazado (se pueden hacer ya)

**A1. Zombis.**
- `ComisariaGrandeEnemies.cs:22`: `Normal` = Civil, Cop, Girl, Oficial + Pxl1, Pxl2.
- `ComisariaGrandeExterior.cs:157`: misma lista para la verja.
- No tocar `ComisariaV2Progress.cs` (kit viejo, no se ejecuta).
- Los 5 prefabs descartados se quedan en el proyecto sin usar (borrarlos rompería referencias; se decide después).
- Aplicarlo a la escena: menú 5 (zombis y botín) y menú 2 (exterior, para la verja), luego 6 y 7. Cuidado: el menú 5 reparte el botín al azar de nuevo.

**A2. Candado.**
- Reproducir en Play (dar la cizalla y abrir) y capturar antes/después para ver qué pasa de verdad.
- `LockVisual.cs`: cuando pase de bloqueado a desbloqueado (y no al cargar partida) lanzar una corrutina: sonido de corte, soltar la cadena y el candado del `Leaf`, `Rigidbody` + `BoxCollider` con impulso, caída con física y desaparición a los 4–5 s (patrón de `EjectedCasing.cs`/`PropBreaker.cs`). Ajustar su altura a la de la hoja.
- `Door.cs` (~línea 158): la hoja gira unos 0,6 s después del corte.
- Sonido: no hay uno de cizalla/cadena. Mínimo: `Sfx.DoorUnlock` con tono grave + `Sfx.LampZap` como chasquido. Si el usuario quiere uno mejor, se genera con IA **enseñando antes el modelo y el prompt exacto** (regla del proyecto).
- Lector de tarjetas: pitido y parpadeo del LED al abrir.

**A3. Objetos clave visibles.**
- Componente `PickupGlint` (~40 líneas): emisión suave pulsante (0,5 Hz) con `MaterialPropertyBlock`, solo a menos de 10 m; campo `highlight` en `ItemData`, activado en cizalla, tarjetas, medallones, fusibles y llaves.
- Sin luz extra por objeto. El resto (peana despejada, lámpara de mesa) llega con la planta nueva (etapa D).

**A4. Arreglos pequeños.**
- `WallDressingKit.cs` línea 44: raíz `--- COMISARIA V2 ---` (ya están los modelos `Wall*`).
- Datos: `I_Spray.maxStack` 1→3 (decisión: se confirma en el esquema).

**Verificación A:** en el editor, recuento de zombis por prefab = solo aprobados y 0 de los 5 descartados (escena y verja); captura en Play de un candado cortado antes/después; captura de un objeto clave a 6 m; consola sin errores.

### Etapa B: esquema de la nueva planta (puerta de aprobación)

Escribir `docs/planes/rediseno-planta.md` con el diseño completo y enseñar al usuario el esquema ASCII de cada planta. **No se construye nada hasta que lo apruebe.** Resumen de la propuesta:

**Huella y estructura (64×44 m, sin cambios):** se mantienen la huella, el caracol, la escalera norte, la del archivo, el ascensor, el callejón y la fachada (el exterior y la intro dependen de ellos). Cambia lo de dentro:
- Eje central monumental (x −8..8): vestíbulo (caracol) → **atrio de doble altura** con el ascensor → **hall de ingreso** (escalera norte). Los pasillos de 2,5 m **desaparecen**: pasan a ser atrio y halls (16 m de ancho, pilares, lámparas colgantes) y paredes entre salas contiguas.
- Alas: oeste = público y mantenimiento (espera, garaje, pruebas, vestuarios, taller); este = policial (agentes, armería, seguridad, descanso, calabozos).
- Primera planta: sur = mando (comisario, conferencias, memorial) y detectives; centro = biblioteca, **galería de 3–4,5 m con balaustrada alrededor de un hueco sobre el atrio (6×10 m)** y comedor; norte = interrogatorios, escalera del archivo, registro y sindicato.
- Segunda planta y sótano: casi igual; sótano con galerías de 4 m, bombas y depósitos fundidos.
- **De ~70 espacios a unos 45** (~37 salas con propósito y ~8 halls/galerías), con salas de 130–400 m² en vez de 144 m² uniformes.

**Conexión y circuitos:** salas contiguas se comunican (enfilada) y salen 8 bucles (oeste y este de la planta baja, núcleo, vertical por la escalera norte, primera planta, mando, callejón, sótano). Opcionales a decidir en el esquema: barricada de un solo sentido en la galería (atajo al volver del archivo), parada del ascensor en la primera planta, y **sala segura en la antesala del archivo** (hoy el terminal está a ~150 m del jefe 1).

**Cada sala con relato** (3–5 elementos con función, p. ej.: vestíbulo con mostrador en L, banco volcado contra la puerta, tablón de buscados y rótulo; armería con jaula del armero y mesa de limpieza; seguridad con el puesto del jefe de turno; calabozos con el mostrador del carcelero). Se reponen estrado, mapa, 4 banderas, alfombra y mesa de reuniones del usuario (haciendo `internal` las funciones de `ComisariaV2Office` y re-anclándolas; conferencias girada 90° para que el estrado no choque con la puerta).

**Modelos nuevos, por prioridad:** (1) placa de puerta con el nombre de sala, señal de salida emisiva, botiquín de pared, lámpara colgante, marco de ventana con persiana (decorativo), plano de evacuación generado por script desde `Define()`; (2) cuadros/retratos, tablero de herramientas, mostrador de ingreso, bolsas de pruebas, mesa de comedor, llavero de pared. Geometría con el skill `modelo-blender`; retratos y texturas con IA solo tras enseñar y aprobar el prompt.

**Colocación lógica de objetos** (ejemplos): cizalla colgada en el tablero de herramientas del garaje con 1,2 m despejados y lámpara de mesa encendida; tarjeta de seguridad sobre la mesa del jefe de turno (con un Cop dormido en la silla); medallón I en el catre de la 2.ª celda; fusible C «caído» junto a una rejilla; munición de pistola en cajón del mostrador, guantera del coche patrulla y taquillas de agentes; cartuchos en el armero y pruebas confiscadas; sprays en botiquines.

**Zombis (43, ~18 dormidos, 5 reptantes, 3 carroñeros):** planta baja 16, primera 13, segunda 3 (archivo 2 + azotea 1), sótano 11. Zonas de respiro (vestíbulo, segura, aseos, escalera norte). Encuentros diseñados: reptante bajo el coche del garaje que sale al coger la cizalla, dormidos en la silla de la tarjeta, etc. Ninguno en antesala.

**Munición (PROVISIONAL, cálculo con acierto 60 %: bala ≈19,5 PV, cartucho ≈56 PV, zombi 220 PV):** objetivo ≥ 80 % de la vida de los zombis de cada tramo + coste del jefe.
- Hasta el jefe 1: ~180 balas y ~66 cartuchos; hasta el jefe 2: ~96 y ~34; reservas de arenas: 18+12 y 24+12.
- Sprays: 8 + 4.
- Es mucho más que hoy. **Riesgo:** con 8 casillas (+ riñoneras) y pilas de 30/12 el jugador no puede llevarlo todo; se reparte a lo largo de la ruta para que lleve ≤ ~5 casillas de munición y el resto vaya al baúl, y se valora subir `maxStack` (p. ej. balas 45, cartuchos 18, spray 3). Se ajusta jugando.

**Luz (mixta):**
- Una lámpara por ~40 m² (mínimo 2, máximo 8), spot 20 (halls 16, pasillos de servicio 12), rango ~7,5 m, relleno puntual 6/4 en una de cada dos lámparas de las salas de más de 100 m²: unas 176 lámparas (hoy 129).
- 25–30 % de las del atrio, comedor y galerías arrancan apagadas (charcos de luz y oscuridad).
- **Salas a oscuras con interruptor (6):** depósito de pruebas, calabozos, biblioteca, interrogatorios, almacén y laboratorio del sótano, cada una con interruptor junto a la puerta y piloto naranja emisivo.
- Color por zona: vestíbulo ámbar, garaje sodio, calabozos frío verdoso, pasillos de servicio azulados, sótano rojo de emergencia.

**Decisiones que se le piden al usuario en el esquema:** atrio de doble altura y galería con hueco (recomendado sí); sala segura en la antesala del archivo (sí); barricada de un solo sentido y parada de ascensor en la primera planta (sí); `maxStack` de spray y munición; estilo del brillo de objetos clave (emisión pulsante + lámpara de mesa); porcentaje de zombis dormidos; modelos con Blender o IA; zombi del comisario sentado en su despacho.

### Etapa C: construcción de la estructura

- Reescribir `ComisariaGrande.Define()` (salas, puertas, huecos de losa): añadir el hueco de la galería sobre el atrio como un `SlabHoles` más y su barandilla (`Rail()`), grupos comunes para fundir halls y atrio.
- Conservar los nombres de puerta que usan otros kits (`DoorNamed`): `Puerta_Vestibulo_E`, `Puerta_Pasillo_Candado`, `Puerta_Callejon`, `Puerta_Tarjeta`, `Puerta_Escalera_Norte`, `Puerta_Calabozos`, `Puerta_Sin_Corriente`, `Puerta_Calderas`, `Puerta_Archivo`, `Puerta_Principal`.
- Respetar posiciones fijas: caracol (1,5..6,5 × 4..9), escalera norte (4..6,4 × 31,6..40), escalera del archivo (−14..−11,4 × 31,6..39) con su reja (z≈31,3), ascensor (x 5..8, z 23..26,5), huecos de la fachada (z=40, z=20, x=0).
- Reutilizar tal cual: paredes, puertas, suelos, techos, escaleras, NavMesh con voxel 0,1 m, puzles, jefes, `ComisariaGrandeArenaLoot`, `ShadowBudget`.
- Orden de menús tras cambiar la planta: 1 → 2 → 3 → 4 → 5 → 6 → 7. **No ejecutar los menús viejos** `Horror/Comisaria v2/…`.

### Etapa D: salas con carácter

- Reescribir `ComisariaGrandeProps.Furnish` por tipo según el esquema aprobado (hoy son 2–4 muebles contra las paredes con una sola semilla): composición con foco, objetos con función, rótulos de puerta, tablones, extintores, relojes, retratos.
- Pasillos/galerías/halls: bancos en nichos, tablones, señalética, ventanas decorativas, luz distinta por tramo.
- Adaptar `WallDressingKit` al nuevo raíz y llamarlo desde el flujo.
- Generar los modelos nuevos de prioridad 1 (y 2 si hay tiempo), registrarlos en `ItemTextureKit` si llevan textura horneada.
- Reponer del diseño del usuario: estrado, mapa, banderas, alfombra y mesa de reuniones.

### Etapa E: puzles, zombis y botín por anclas

- `ComisariaGrandePuzzles`: pasar las coordenadas absolutas de lectores de tarjeta, taquillas, notas y objetos clave a **anclas por sala** (`Room.anchors`), de modo que sigan funcionando si cambia el trazado; zona despejada de 1,2 m alrededor de cada objeto clave antes del desorden.
- `ComisariaGrandeEnemies`: nuevos `Spawns` (43) y `Loot` con ancla de contenedor (cajón, guantera, taquilla, armero), según las tablas aprobadas.
- `ComisariaGrandeArenaLoot`: reservas actualizadas (18+12 y 24+12).
- Código pequeño nuevo: `OneWayDoor` (si se aprueba), botonera del ascensor en la primera planta (si se aprueba).

### Etapa F: luz

- `EmitLamps` v2: lámparas por superficie, intensidad/rango objetivo, lámparas «muertas», color por zona; nombres `Lamp_<sala>#n` (hay que adaptar `Puzzles` línea 236 y `ComisariaGrandeLightFix` línea 36, que usan igualdad exacta y `Substring(5)`).
- Interruptores en las 6 salas a oscuras con piloto emisivo (reutilizar `LightSwitch` y su guardado en `SaveSystem.OrderedSwitches`).
- Repetir el pase de luces (menú 7).

### Etapa G: verificación y juego

Ver sección siguiente. Después de verificar, el usuario juega; el equilibrio de munición, zombis y luz se ajusta con lo que vea.

## Archivos críticos

- `Assets/_Project/Scripts/Editor/ComisariaGrande.cs` (Define, EmitLamps)
- `Assets/_Project/Scripts/Editor/ComisariaGrandeProps.cs` (Furnish por tipo, pasillos)
- `Assets/_Project/Scripts/Editor/ComisariaGrandePuzzles.cs` (anclas, candados, interruptor, lámparas)
- `Assets/_Project/Scripts/Editor/ComisariaGrandeEnemies.cs` (Normal, Spawns, Loot)
- `Assets/_Project/Scripts/Editor/ComisariaGrandeExterior.cs` (lista de la verja), `ComisariaGrandeArenaLoot.cs`, `ComisariaGrandeLightFix.cs`
- `Assets/_Project/Scripts/Editor/ComisariaV2Office.cs` (Memorial, Conference, Commissioner, Flag: hacerlas `internal`), `WallDressingKit.cs`
- `Assets/_Project/Scripts/Interaction/LockVisual.cs`, `Door.cs`, `Pickup.cs`; nuevo `PickupGlint.cs`; datos `ItemData` (`highlight`, `maxStack`)
- `Assets/_Project/Scripts/Core/ShadowBudget.cs` (revisar si cambian las lámparas), `docs/` y `README.md`

Se reutilizan sin reescribir: constructor de paredes y puertas, `Rail()`, escaleras y caracol, `Pickup.Spawn`, `LightSwitch`, `ComisariaGrande.Box`, `EjectedCasing`/`PropBreaker` (patrón de restos con física), `PropVariant`.

## Verificación (se hace con herramientas, no se da por hecho)

1. **Estructura:** ruta de NavMesh a cada sala con los obstáculos de puerta apagados en Play (como en `docs/rediseno-comisaria.md` L341), capturas de cada sala, ancho útil de pasillos/halls.
2. **Progresión:** simulación por grafo (puertas como aristas, objetos como llaves) sobre `Define()` + las llaves reales de la escena; debe cerrar toda la cadena y no dejar ningún objeto detrás de la puerta que él mismo abre. Se hará un menú de auditoría.
3. **Zombis:** recuento por prefab (solo aprobados), por sala y por planta, y letargos.
4. **Munición:** suma de balas, cartuchos y sprays por tramo frente al presupuesto; cuántas casillas haría falta llevar.
5. **Luz:** lámparas por sala frente a la regla; captura de cada tipo de sala; brillo medio de las capturas para detectar salas negras; comprobación de que las 6 salas a oscuras tienen interruptor visible a menos de 2 m de la puerta.
6. **Objetos clave:** rayo desde la puerta a cada objeto de puzle (visible, no tapado) y captura.
7. **Candado y animación:** captura en Play antes/después del corte y comprobar que no queda nada colgado.
8. **Rendimiento:** F3 y `performance-audit` en 1080p; objetivo provisional GPU ≤ 12 ms en el atrio con el hueco. (Las medidas del editor de hoy no son comparables con las de ayer y no se ha medido fps en la build.)
9. **Pruebas que necesitan al usuario jugando:** ambiente, ritmo de zombis, si la munición alcanza, si las salas «cuentan algo», si los objetos clave se encuentran.

## Reglas que se respetan

- No compilar ni editar scripts con Unity en Play; no ofrecer recompilar la build (solo con `/recompile` si lo pide).
- Prompts de IA generativa (retratos, texturas, sonido de cizalla): enseñar modelo y prompt exacto y esperar el OK.
- Documentar cada cambio en `docs/<módulo>.md` y en el registro del README; commit y push a `rediseno-comisaria` al cerrar cada etapa; el plan se guarda también en `docs/planes/`.
- No dejar archivos sueltos en la raíz; capturas temporales se borran.

## Riesgos y cosas que no se saben

- **Es un cambio de planta completo:** hay que repetir los 7 menús y volver a comprobar NavMesh, puertas y progresión. Las posiciones fijas actuales de los puzles se rompen (por eso las anclas).
- **El esquema es lo más importante** y es del usuario: puede pedir cambios antes de construir. Las cotas de la propuesta son de un primer boceto.
- **Cantidad de munición** y capacidad del inventario (arriba); el 80 % se calcula con acierto 60 % y daños por zona de impacto supuestos.
- **Luces:** más lámparas cuestan rendimiento; `ShadowBudget` limita las sombras a 8, pero con Forward+ y ~176 lámparas hay que medirlo.
- **Candado:** la causa de que «siga puesto» no está probada; se descubre al reproducirlo.
- **Sin comprobar aún:** el estado real de las animaciones de los zombis aprobados en Play (se leyó la configuración, no se vio), y si los 4 prefabs viejos de rig genérico se quedan sin uso sin romper nada.
