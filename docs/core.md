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
| `SaveSystem` (estática) | Guarda/carga **5 slots** (`persistentDataPath/savegame_1..5.json`; `Save(slot)`, `LoadAndRestart(slot)`, `Peek(slot)` con fecha y objetivo, `CurrentSlot` = último usado en PlayerPrefs). El `savegame.json` antiguo se migra solo al slot 1. Cada archivo guarda: ids de notas leídas (`NoteArchive`), casillas de riñonera, jugador, inventario, arma y cargadores, pickups, zombis vivos, puertas, taquillas, baúl, interruptores y objetivo. Cargar = `LoadAndRestart()` (recarga la escena de juego) y `GameFlow` aplica `Pending`. |
| `Objectives` | Objetivo actual (se muestra en el HUD y se guarda). |
| `ItemDatabase` | Lista de todos los `ItemData` para reconstruir objetos por nombre al cargar. |
| `GameAudio` | Música ambiente, capa de tensión cuando hay persecución, sustos lejanos y utilidades `Play`/`PlayClip`. Sonidos nuevos: `StairStep`, `PhoneRing/Dial/Pickup`, `BossRoar/Step`. **Música** (oct. 2026): `ambientLoop` y `tensionLoop` son pistas instrumentales de ElevenLabs Music v2.5 vía fal (`Audio/Generated/music_ambient_fal.wav` 85 s y `music_tension_fal.wav` 70 s; originales `*_original.mp3`), con el final mezclado con el principio 5 s para que el bucle no se corte, normalizadas a rms 0,10-0,12 y en *streaming* Vorbis. La de fondo suena siempre; la de tensión sube cuando un zombi te persigue (ver `Update`). Antes eran pads sintetizados. Los clips salen de packs CC0, síntesis (`Tools/audio/build_free_audio.py`) y fal.ai. **Atenuación** (oct. 2026): los efectos espaciales usan una curva propia que llega a cero en `DefaultRange` (22 m; los sustos lejanos, 50 m) en lugar de la logarítmica (que nunca se apagaba), y se multiplican por ~0,12 si la fuente está en otra planta (|Δy| de 1,5 a 3,5 m, el forjado). **Mezcla por familias** (campos de `GameAudio`, ajustables en el inspector, aplicados en `Play(Sfx…)`): `doorGain` 0,4 (puertas, taquillas y baúl) y `zombieGain` 0,5 (gruñidos, ataque, quejido y muerte; no el jefe), además de los volúmenes propios de `ZombieAudio` (0,5-0,65). `musicVolume` 0,7. Cada arma tiene su `WeaponData.fireVolume` (pistola 1,0; escopeta 0,5): `Emit` reparte un volumen > 1 entre varias fuentes (un `AudioSource` no pasa de 1). |
| `CeilingLamp` / `FillLightRating` | Lámpara de techo con parpadeo, apagado desde interruptor (`SetPowered`) e intensidad nominal guardada aparte. |
| `RuntimeNavMesh` | Hornea el NavMesh en `Start` (usa colliders físicos y respeta `NavMeshModifier` no caminable); excluye las hojas de las puertas durante el horneado. |

## Decisiones
- El guardado identifica puertas, taquillas e interruptores por **orden espacial** (z, luego x), no por ID: si se mueve o añade uno, los guardados antiguos pueden desalinearse.
- El NavMesh se hornea al arrancar en vez de guardarse en la escena, para que los obstáculos de la escena siempre cuenten.
- Cargar una partida desde el menú principal funciona porque `LoadAndRestart` carga la escena por nombre.

## Límites conocidos
- El guardado no incluye el estado del jefe (si estaba despierto) ni de las puertas forzadas por zombis más allá de abierta/cerrada.
