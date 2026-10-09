# Plan: fase F, parte 1 (reservas en las arenas y fugas de luz)

- **Fecha:** 2026-10-09
- **Estado:** Hecho (2026-10-10), aprobado por el usuario con la opción A. Falta jugarlo y medir fps en una build
- **Autor:** Claude (Sonnet), a partir de la revisión de nivel `/level-review` del mismo día
- **Rama:** rediseno-comisaria

## Objetivo
Tres cambios que salieron de la revisión de nivel:
1. Que el jugador no se quede sin salida en la arena de un jefe por falta de munición.
2. Quitar las fugas de luz por fuera del edificio (pendiente de la fase B).
3. Que `ShadowBudget` deje con sombra las lámparas que más importan, para que sus vecinas no atraviesen las paredes.

No se toca el equilibrio general (vida de zombis frente a munición): eso va después de que el usuario juegue el nivel entero.

## Estado actual (observado en la revisión, 2026-10-09)

**Arenas**
- Jefe 1: 1.410 de vida, en `(15, 7,6, 29)`, archivo. `BossRoomTrigger` es un volumen de 13,6 × 13,6 m centrado en el jefe; al entrar sella `Puerta_Archivo` (-4, 7,5, 36) hasta que muere. Dentro del archivo no hay munición suelta; la más cercana está en la antesala, delante de la puerta (8 cartuchos y 1 spray).
- Jefe 2: 1.800 de vida, en `(-27, -6,5, 41,5)`, foso de calderas. Su disparador es pequeño, en el rellano `(-22, -3, 30,3)`. Fuera, en el pasillo de calderas (`B_C2W`), hay 8 cartuchos y 12 balas.
- Pistola: 28 de daño. Escopeta: 14 × 8 perdigones (112 si aciertan todos). Jefe 1 = 51 tiros de pistola o 13 cartuchos con acierto perfecto; jefe 2 = 65 tiros o 17 cartuchos.
- Munición recogible antes del jefe 1: 92 balas y 36 cartuchos (más 10 balas iniciales); en el sótano: 34 balas y 20 cartuchos. Con acierto perfecto, esto cubre ~64 % y ~60 % de la vida de zombis y jefe del tramo.
- Con la puerta sellada no se puede salir ni recoger munición. Si el jugador entra sin balas solo le queda morir y cargar; si guardó así, solo le sirven las recogidas que aún no cogió.
- Botín: `Loot` en `Assets/_Project/Scripts/Editor/ComisariaGrandeEnemies.cs` (líneas 40-47) pone el objeto encima de un mueble en un punto **al azar de la sala**; el menú *Horror/Comisaria grande/5* borra y rehace `Enemigos` y `Botin` enteros.

**Luz**
- Lámparas: 129 focos `Spot` de 125° de ángulo y rango `min(14, alto + 6)` (9,5 m en salas de 3,5 m, 12,5 m en el archivo), intensidad 28 (22 en pasillos), con sombra blanda. Cada una lleva una luz `Fill` puntual a 1,9 m, rango 5, intensidad 8, **sin sombra** (se crean en `TestSceneBuilder.Lamp`, llamada desde `ComisariaGrande.EmitLamps`, línea 429).
- `ShadowBudget` (8 focos con sombra, por distancia al jugador) deja el resto sin sombra.
- Fuga exterior: en 15 de 20 puntos a 1 m fuera de los muros llega luz de lámparas interiores (método: `Physics.Linecast` de la lámpara al punto, dentro de su rango y cono). Se ve en una captura desde la azotea: dos manchas de luz en la cara exterior del muro sur del archivo.
- Fugas hacia otras salas (centro de 61 salas, estimación estática): 26 de focos sin sombra por el presupuesto y 56 de luces de relleno, que ya eran sin sombra antes de `ShadowBudget`. Los focos concentran las fugas en la primera planta norte (detectives, registro, sindicato, escalera norte) y en la planta de descanso.
- **No se sabe** cuánto de la mancha exterior del archivo viene de los focos sin sombra por `ShadowBudget` y cuánto de los rellenos (que ya existían). Es lo primero que hay que separar.

## Decisiones tomadas
- Sin cambiar la dificultad global ahora.
- Medir con el mismo método y los mismos escenarios que en `docs/sombras.md`.
- Nada de recompilar la build salvo `/recompile` cuando el usuario lo pida.

## Decisiones abiertas
1. **Cómo evitar el atasco blando en las arenas.**
   - **A (recomendada):** reservas de munición dentro de la arena, encima de cobertura. Ayudan, pero no garantizan nada: con 50 % de acierto, el jefe 1 pide ~26 cartuchos.
   - **B:** que la puerta se libere sola si el jefe sigue vivo y el jugador se queda sin munición. Garantiza la salida, pero rompe la tensión del encierro.
   - **C:** A y B.
   Recomendación: **A ahora** y decidir B tras jugar, si la gente se queda atascada.
2. **Cuánta munición poner en cada arena** (provisional, a ajustar jugando): archivo, 6 cartuchos y 12 balas; foso de calderas, 6 cartuchos y 12 balas. Es un 1.008 de daño ideal por arena (~71 % y ~56 % de la vida del jefe).
3. **Reparto de las 8 sombras (cambio 3).** Recomendación: 4 plazas para las lámparas de la sala (con línea de visión al jugador, para que los muebles tengan sombra) y 4 para las lámparas que **sí alcanzan al jugador pero tienen una pared o un suelo en medio** (las que fugarían), ordenadas por distancia en cada grupo. Hipótesis: hay que medir las fugas y los fps después.
4. **Cómo cortar la fuga exterior (cambio 2).** Recomendación: acortar el rango de los focos y rellenos que estén a menos de 6 m de un muro exterior, hasta ~0,3 m más allá de ese muro (lo justo para iluminar su cara interior). Alternativas, más caras: capa de «piel» exterior en los muros con *rendering layers*, o sombra forzada en las lámparas del perímetro. Se decide tras el paso 1.

## Diseño

**Cambio 1: kit nuevo `ComisariaGrandeArenaLoot`** (menú *Horror/Comisaria grande/6 Reservas de arenas*)
- Un kit propio, para **no** rehacer los zombis ni el botín con el menú 5 (el azar cambiaría).
- Rehace solo un grupo `Botin_Arenas` bajo `--- COMISARIA V2 ---`. Repetible.
- Coloca los objetos con `Pickup.Spawn`, en posiciones elegidas con raycast **a 4-8 m del jefe**, encima de cobertura o en el suelo, **accesibles** (comprobar ruta de NavMesh) y fuera de las estanterías rompibles de `_Props`.
- Archivo: dentro del volumen de 13,6 m del jefe 1 o justo en su borde. Foso: abajo, a pie de escalera y en el lado opuesto a la caldera.
- Se comprueba con el recuento por reflexión (pickups por sala).

**Cambio 2: kit nuevo `ComisariaGrandeLightFix`** (menú *Horror/Comisaria grande/7 Pase de luces*)
- Idempotente: calcula el rango base de cada `Lamp_<sala>` con la misma fórmula que `EmitLamps` y lo recorta solo si hay un muro exterior cerca. No acumula recortes al repetirlo.
- Aplica lo mismo a la luz `Fill` hija.
- Nota: si se rehace el nivel con el menú 1, hay que volver a ejecutar los menús 6 y 7 (y el 5 si se rehace el botín).

**Cambio 3: `ShadowBudget`** (`Assets/_Project/Scripts/Core/ShadowBudget.cs`)
- Mantiene `maxShadowed` (8) y la histéresis.
- Cada intervalo clasifica las luces encendidas en tres grupos: (1) alcanzan al jugador con línea de visión, (2) alcanzan al jugador pero con una pared o suelo en medio, (3) no lo alcanzan. Reparte las plazas según la decisión 3 y rellena con el siguiente grupo si sobran.
- Coste: un `Linecast` por luz que alcance al jugador (unas pocas decenas cada 0,2 s). Se mide.

## Pasos de implementación (para el aplicador)
1. Comprobar `EditorApplication.isPlaying`; si es `True`, esperar. Leer `CLAUDE.md` y `docs/sombras.md`.
2. **Diagnóstico de la fuga (sin tocar nada, en Play):** hacer la misma captura de la azotea con (a) el estado actual, (b) `ShadowBudget` destruido (sombras originales) y (c) rellenos apagados. Anotar cuál elimina las manchas. Con ello, confirmar la decisión 4.
3. **Cambio 3:** reescribir la selección en `ShadowBudget`. Compilar (`refresh_unity`, `compile: request`) y revisar la consola.
4. Repetir el estimador de fugas de salas (26 y 56 de la revisión) y el de fugas exteriores (15 de 20). Repetir la medición de GPU en entrada, calderas y sala de espera (7,2-9,7 ms en la última medición).
5. **Cambio 2:** crear `ComisariaGrandeLightFix` y ejecutarlo. Repetir los estimadores y la captura de la azotea. Capturas de 3 salas interiores junto a un muro exterior para ver que no se oscurecen demasiado.
6. **Cambio 1:** crear `ComisariaGrandeArenaLoot` y ejecutarlo. Comprobar las posiciones y que cada reserva tiene ruta de NavMesh (con los obstáculos de puerta apagados dentro de Play, como en la revisión).
7. Documentar: `docs/sombras.md`, un apartado nuevo en `docs/rediseno-comisaria.md` (fase F, parte 1) y entradas en el registro del README. Actualizar el Resultado de este plan.
8. Commit y push a `rediseno-comisaria`. La build, solo con `/recompile` si el usuario lo pide (la versión 2 necesita `MainMenu` + `Comisaria_v2` a mano, ver `docs/estado-actual.md`).

## Criterios de aceptación
- **Cambio 1:** cada arena tiene su reserva, a la que se llega por NavMesh desde la entrada, fuera de estanterías rompibles. Se confirma contando objetos, no jugando.
- **Cambio 2:** en los 20 puntos exteriores, las fugas bajan de 15 a **5 o menos**, y en la captura de la azotea no se ve mancha en el muro sur del archivo. Las salas interiores no quedan visiblemente más oscuras (lo valora el usuario).
- **Cambio 3:** las fugas entre salas estimadas bajan de 26 a la mitad o menos, con el mismo método, y la GPU en el editor sigue por debajo de 16,7 ms (referencia: 7,2-9,7 ms).
- Sin errores nuevos en la consola.
- **No comprobable aquí:** fps reales en la build a 1920×1080 y cómo se siente la luz. Los valida el usuario con F3 y jugando.

## Qué se puede probar solo y qué necesita al usuario jugando
- **Solo (editor y Play avanzando fotogramas):** estimadores de fugas, GPU, rutas de NavMesh a las reservas, capturas.
- **Con el usuario jugando:** si la munición de las arenas basta o sobra, si el ambiente de las salas es aceptable tras recortar los rangos y los fps en la build.

## Riesgos y cosas que no tocar
- No ejecutar el menú 1 ni el 5 sobre la escena (rehacen el nivel o reparten el botín y los zombis al azar); no ejecutar los menús viejos *Horror/Comisaria v2/…*.
- No compilar ni editar scripts con Unity en Play.
- Recortar el rango de las lámparas puede oscurecer las salas junto al muro exterior; por eso se mide y se mira con capturas.
- Las reservas suben la munición total: ahora son 126 balas y 56 cartuchos en recogidas; con las reservas, +24 balas y +12 cartuchos.
- Los guardados antiguos pueden tener recogidas ya hechas; las reservas nuevas solo aparecen al empezar partida o cargar con el nivel actualizado.
- `ShadowBudget` está en la build de la versión 2 de hoy; los cambios solo llegan a la build tras `/recompile`.

## Resultado (lo rellena quien ejecuta)
Hecho el 2026-10-10. Las decisiones abiertas quedaron así: 1 = **A** (reservas, sin liberar la puerta; B se decide tras jugar); 2 = 6 cartuchos y 12 balas por arena; 3 = 4 + 4 como se recomendó; 4 = recorte de rangos, **solo de las luces de relleno** (el diagnóstico mostró que los focos no fugan).

**Hecho y comprobado (editor, Play avanzando con `Step`, NavMesh y capturas):**
- Diagnóstico de la fuga (paso 2): las manchas desaparecen al anular los rellenos; no dependen de `ShadowBudget` ni de los focos.
- Cambio 3: `ShadowBudget` reescrito (`visibleSlots` 4, dos puntos de prueba: suelo y cabeza). En 61 salas y pasillos, fugas de focos a través de paredes: 25/42/62 → 0/0/23 (suelo/cabeza/sobre la cabeza); nunca más de 8 luces con sombra. Coste en CPU principal en una prueba A/B: unos 0,5 ms.
- Cambio 2: `ComisariaGrandeLightFix` (menú 7), idempotente (repetido da lo mismo). Fuga de rellenos en 20 puntos exteriores: 5 puntos y 1,43 → 2 y 0,14. Capturas: las manchas grandes quedan en dos destellos pequeños. Pasillo `F_C1` A/B sin diferencia visible.
- Cambio 1: `ComisariaGrandeArenaLoot` (menú 6): 4 objetos en el suelo dentro de las arenas, con ruta de NavMesh desde el jefe o el rellano; 45 recogidas (antes 41).
- Docs: `docs/sombras.md`, `docs/rediseno-comisaria.md` (fase F, parte 1) y registro del README.

**Corregido por el camino (errores míos):** el primer punto de prueba de `ShadowBudget` (los ojos) hacía que la regla nueva midiera 0 fugas por estar optimizada para ese mismo punto; el recorrido de salas teletransportó al jugador dentro del suelo (el pivote está ~1 m sobre los pies) y la primera medida no valió; la primera reserva del archivo cayó fuera del volumen que sella la puerta. Las tres se arreglaron y se repitió la medida.

**No comprobado / pendiente:**
- **Fps en una build.** La build del escritorio no lleva nada de esto hasta `/recompile`. Las medidas de GPU/CPU del editor de ese día salieron más altas que las del 2026-10-09 incluso con el presupuesto desactivado, sin causa conocida: no son comparables con la tabla de `docs/sombras.md`.
- Si 6 cartuchos y 12 balas bastan en las arenas, y si la puerta debe liberarse (opción B).
- El ambiente jugando, y los destellos que quedan en la base del muro sur del archivo (se quitarían bajando el rango mínimo de 2,5 m a 2 m en `ComisariaGrandeLightFix`).
- Las reservas solo aparecen al empezar partida o con el nivel actualizado; un guardado antiguo no las tiene.
