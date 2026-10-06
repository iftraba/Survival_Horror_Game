# Interaction

Objetos con los que se interactúa con **E**. Código en `Assets/_Project/Scripts/Interaction/`.

## Clases
| Clase | Qué hace |
|---|---|
| `IInteractable` | Contrato: `Prompt` e `Interact(who)`. |
| `PlayerInteractor` | Elige el interactuable más cercano delante del jugador y muestra su `Prompt` en el HUD. |
| `Door` | Puerta abatible en la bisagra. Puede exigir llave (`requiredKey`, `consumeKey`), tener `partner` (doble hoja), mensaje de objetivo al abrir (`openedObjective`) y deja pasar a los zombis (`zombiesCanForce`, `zombieForceTime`). Lleva un `NavMeshObstacle` que bloquea el paso cerrada. `ApplySaved` restaura el estado. |
| `ExitDoor` | Puerta de salida: con `I_KeyExit` termina la partida con victoria. |
| `Pickup` | Objeto del mundo (rigidbody). Se recoge con E, lleva `item` y `count`; puede fijar un objetivo al recogerlo. |
| `LockerDoor` | Taquilla que se abre con E. |
| `LightSwitch` | Interruptor que enciende o apaga un grupo de `CeilingLamp`. |
| `ItemBox` | Baúl de sala segura: abre la pantalla de intercambio con `ItemStorage` (global). |
| `SaveTerminal` | Punto de guardado de sala segura: abre el menú de guardado del HUD. Físicamente es un **teléfono antiguo de disco** sobre una mesita (prefab `Interactables/SavePhone`, modelo `Art/Props/Phone.fbx`, con una luz cálida tenue que guía al jugador). El script mantiene el nombre `SaveTerminal` por compatibilidad. |

## Zona del jefe
`Door_SafeRoom` (sala segura, los zombis no la fuerzan) y `Door_Boss` (reforzada, la fuerzan tras 2.5 s).

## Decisiones
- El guardado de puertas/taquillas/interruptores usa el orden espacial (ver `core.md`).
- Las salas seguras son las únicas con terminal y baúl; el baúl es compartido entre todas.
