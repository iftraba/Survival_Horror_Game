# Core

Servicios transversales del juego. Código en `Assets/_Project/Scripts/Core/`. Espacio de nombres `Horror`.

## Responsabilidades
Estado global, vida, guardado/carga, objetivos, ajustes del jugador, audio ambiente, luces del techo y navegación.

## Clases
| Clase | Qué hace |
|---|---|
| `GameState` (estática) | Banderas globales: inventario, baúl, menú de guardado, pausa, muerte y victoria. `InputBlocked` las agrupa; `Apply()` fija `Time.timeScale` (0 con menús) y el cursor. `ResetAll()` limpia al reiniciar. |
| `GameFlow` | Arranque de la escena de juego (`Start`): `GameState.ResetAll()`, restaura un guardado pendiente o prepara partida nueva (baúl vacío, objetivo inicial, kit inicial: pistola equipada con `startMagazine` balas). |
| `GameSettings` (estática) | Volumen y sensibilidad del ratón en `PlayerPrefs`. Nombres de escena: `Comisaria` (juego) y `MainMenu`. Aplica `AudioListener.volume` antes de cargar la escena. |
| `Health` / `IDamageable` | Vida con eventos `Changed`, `Damaged(punto)`, `Died`. `SetCurrent` fija vida sin disparar daño (carga de partida). |
| `SaveSystem` (estática) | Guarda/carga un JSON en `persistentDataPath/savegame.json`: jugador, inventario, arma y cargadores, pickups, zombis vivos, puertas, taquillas, baúl, interruptores y objetivo. Cargar = `LoadAndRestart()` (recarga la escena de juego) y `GameFlow` aplica `Pending`. |
| `Objectives` | Objetivo actual (se muestra en el HUD y se guarda). |
| `ItemDatabase` | Lista de todos los `ItemData` para reconstruir objetos por nombre al cargar. |
| `GameAudio` | Música ambiente, capa de tensión cuando hay persecución, sustos lejanos y utilidades `Play`/`PlayClip`. Sonidos nuevos: `StairStep`, `PhoneRing/Dial/Pickup`, `BossRoar/Step`. Los clips salen de packs CC0 y síntesis (`Tools/audio/build_free_audio.py`). |
| `CeilingLamp` / `FillLightRating` | Lámpara de techo con parpadeo, apagado desde interruptor (`SetPowered`) e intensidad nominal guardada aparte. |
| `RuntimeNavMesh` | Hornea el NavMesh en `Start` (usa colliders físicos y respeta `NavMeshModifier` no caminable); excluye las hojas de las puertas durante el horneado. |

## Decisiones
- El guardado identifica puertas, taquillas e interruptores por **orden espacial** (z, luego x), no por ID: si se mueve o añade uno, los guardados antiguos pueden desalinearse.
- El NavMesh se hornea al arrancar en vez de guardarse en la escena, para que los obstáculos de la escena siempre cuenten.
- Cargar una partida desde el menú principal funciona porque `LoadAndRestart` carga la escena por nombre.

## Límites conocidos
- El guardado no incluye el estado del jefe (si estaba despierto) ni de las puertas forzadas por zombis más allá de abierta/cerrada.
