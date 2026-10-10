# Interaction

**Apertura sin incrustarse (2026-10-08)**: `Door.SwingAwayFrom` ya no abre siempre 100°. Mide cuánto puede girar la hoja hacia cada lado (`FreeAngle`: prueba un volumen algo encogido de la hoja en pasos de 8° contra el nivel y los muebles, ignorando jugador, zombis, objetos sueltos y otras puertas), elige el lado preferido si se abre del todo o el que deje más hueco, y **nunca pasa de 90°**: a más, la hoja se inclina hacia el muro de su lado y con los muros gruesos se metía hasta 11 cm (Door_Exit, Door_GarageGate, Door_Main). Resultado: la mayoría abren 90° y las de muro grueso 72-80°.

**Puertas atrancables (2026-10-07)**: `Door.Seal()` cierra de golpe la puerta (si estaba abierta) y la deja atrancada (`Prompt` "Atrancada", mensaje al intentar abrirla, los zombis no la fuerzan); `Unseal()` la libera. `BossRoomTrigger.sealDoors` las atranca al empezar el combate con el jefe (no se puede huir) y las libera al morir el jefe.

Objetos con los que se interactúa con **E**. Código en `Assets/_Project/Scripts/Interaction/`.

## Clases
| Clase | Qué hace |
|---|---|
| `IInteractable` | Contrato: `Prompt` e `Interact(who)`. |
| `PlayerInteractor` | Elige el interactuable más cercano delante del jugador y muestra su `Prompt` en el HUD. |
| `Door` | Puerta abatible en la bisagra. Puede exigir llave (`requiredKey`, `consumeKey`), tener `partner` (doble hoja), mensaje de objetivo al abrir (`openedObjective`); **la llave se gasta al usarla** (`consumeKey`; en una puerta doble basta con que una de las dos hojas lo tenga, porque la hoja derecha de `Door_Main` no lo tenía y la llave de la sala se quedaba en el inventario) y deja pasar a los zombis (`zombiesCanForce`, `zombieForceTime`). Lleva un `NavMeshObstacle` que bloquea el paso cerrada. `ApplySaved` restaura el estado. |
| `ExitDoor` | Puerta de salida: con `I_KeyExit` termina la partida con victoria. |
| `Pickup` | Objeto del mundo (rigidbody). Se recoge con E, lleva `item` y `count`; puede fijar un objetivo al recogerlo. |
| `LockerDoor` | Taquilla que se abre con E. Con `code` rellenado está bloqueada: E abre el teclado numérico (`Keypad`) y al acertar se desbloquea y se abre. Las de código se ordenan al final en el guardado para no desalinear partidas antiguas. |
| `Keypad` | Estado del teclado numérico (entrada, error, taquilla objetivo); lo dibuja el `Hud` y acepta ratón, teclas 0-9 y teclado numérico. |
| `ReadableNote` | Nota en el mundo (papel sobre una mesa, `NoteData`). E la abre a pantalla completa y la añade al Archivo; no se recoge ni ocupa casilla. Pasar a leerla por primera vez puede fijar un objetivo. |
| `LightSwitch` | Interruptor que enciende o apaga un grupo de `CeilingLamp`. |
| `ItemBox` | Baúl de sala segura: abre la pantalla de intercambio con `ItemStorage` (global). |
| `SaveTerminal` | Punto de guardado de sala segura: abre el menú de guardado del HUD. Físicamente es un **teléfono antiguo de disco** sobre una mesita (prefab `Interactables/SavePhone`, modelo `Art/Props/Phone.fbx`, con una luz cálida tenue que guía al jugador). El script mantiene el nombre `SaveTerminal` por compatibilidad. Solo suena **al usarlo** (descolgar al abrir el menú, marcar al guardar): el timbre por el mapa está apagado (`ringsNearby` = false; si se activa, suena a `ringRange` m cada 30-55 s). |

## Puertas con animación (2026-10-08)
Al abrir una puerta, el jugador hace la animación de abrir (`PlayerActions.OpenDoor`) y la hoja gira cuando la mano llega al pomo. Las puertas de entrada a la sala de un jefe dormido (las de `BossRoomTrigger.sealDoors`, vistas desde fuera) lanzan la entrada animada (`PlayerActions.EnterBossRoom`). Una puerta atrancada espera a que acabe la animación para cerrarse.

## Zona del jefe
`Door_SafeRoom` (sala segura, los zombis no la fuerzan) y `Door_Boss` (reforzada, la fuerzan tras 2.5 s).

## Decisiones
- El guardado de puertas/taquillas/interruptores usa el orden espacial (ver `core.md`).
- Las salas seguras son las únicas con terminal y baúl; el baúl es compartido entre todas.

## Candados con corte y objetos clave con brillo (2026-10-10, fase F parte 2, etapa A)
- **`LockVisual` con `dropOnUnlock`:** al desbloquear una puerta con candado jugando, el candado ya no desaparece de golpe: suena un chasquido (provisional: `Sfx.LampZap` grave; no hay sonido de cizalla), se desengancha de la puerta, cae con `Rigidbody` y `BoxCollider` (`Pickup.FitBoxCollider`) sin chocar con la propia hoja, y se desvanece a los 5 s (`lingerSeconds`). Al cargar una partida con la puerta ya abierta se oculta directamente. En el lector de tarjetas, `beepOnUnlock` da un pitido (`Sfx.Switch` agudo) al pasar el piloto a verde.
- **Causa del «candado en el aire»:** `ComisariaGrandePuzzles.Padlock` colgaba el modelo de `Leaf`, la hoja, que tiene escala no uniforme (1,46 × 2,36 × 0,05); `SetParent` conserva la escala del mundo y el candado salía estirado ×29 y aplastado a 3 cm (un sliver de ~16 m). Ahora cuelga de la bisagra (escala 1): mide 6 × 56 × 56 cm, centrado a 1,10 m. Comprobado en Play: antes del corte (cadena en X sobre la puerta), tras el corte (suelto, con rigidbody, en el suelo) y a los ~7 s (destruido; los otros dos siguen en su sitio).
- **`PickupGlint` + `ItemData.highlight`:** los objetos clave (cizalla, tarjetas de seguridad y del jefe, 3 medallones, 3 fusibles, llaves del ascensor y maestra) llevan una luz puntual pequeña, sin sombra, que pulsa (0,5 Hz, intensidad 1,5-4,5, rango 2,4 m) y solo se enciende a menos de 10 m del jugador. `Pickup.Spawn` la añade sola si `item.highlight`. En la escena actual se añadió a los 9 que ya estaban. El efecto es **sutil** en un garaje oscuro (se ve un destello rojizo sobre el banco); si queda corto, se sube en `PickupGlint`.
- Pendiente de comprobar: el sonido del corte (chasquido provisional) y el pitido del lector no se han podido oír.

## OneWayDoor y LightSwitch.partner (2026-10-10, etapa E)
`OneWayDoor` (va junto a `Door`): puerta atrancada por un lado (`freePoint` marca el lado libre); mientras el jugador está en el lado malo la `Door` está sellada (`Seal`), en el bueno se libera, y una vez abierta desde ahí queda libre. Usada en `Puerta_Garaje_Bombas` y `Puerta_Galeria_HallNorte`. `LightSwitch.partner` sincroniza dos interruptores de la misma sala (sala de pruebas: uno por puerta).
