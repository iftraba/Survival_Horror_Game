# Interaction

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

## Zona del jefe
`Door_SafeRoom` (sala segura, los zombis no la fuerzan) y `Door_Boss` (reforzada, la fuerzan tras 2.5 s).

## Decisiones
- El guardado de puertas/taquillas/interruptores usa el orden espacial (ver `core.md`).
- Las salas seguras son las únicas con terminal y baúl; el baúl es compartido entre todas.
