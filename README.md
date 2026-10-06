# Comisaría — survival horror (Unity 6)

Survival horror de terror medio, estilo RE2, en Unity 6000.6.4f1 (URP). Cámara al hombro, zombis con IA, jefe final,
salas seguras con guardado y baúl compartido, inventario por huecos.

## Cómo se juega
WASD mover · Mayús correr · clic derecho apuntar · clic izquierdo disparar · E interactuar · R recargar · F linterna ·
Tab inventario · Esc pausa.

## Documentación por módulos
| Módulo | Doc | Contenido |
|---|---|---|
| Core | [docs/core.md](docs/core.md) | Estado global, vida, guardado, objetivos, ajustes, audio, luces, NavMesh |
| Player | [docs/player.md](docs/player.md) | Control, cámara, linterna, pasos |
| Weapons | [docs/weapons.md](docs/weapons.md) | Armas, disparo, recarga, dispersión |
| Enemies | [docs/enemies.md](docs/enemies.md) | Zombis, zonas de impacto, jefe |
| Interaction | [docs/interaction.md](docs/interaction.md) | Puertas, taquillas, interruptores, terminales, pickups |
| Inventory | [docs/inventory.md](docs/inventory.md) | Objetos, inventario, baúl global |
| UI | [docs/ui.md](docs/ui.md) | HUD, menús, menú principal |
| Animation | [docs/animation.md](docs/animation.md) | Animators, alineación de arma, retroceso |
| Level | [docs/level.md](docs/level.md) | Escena, zonas, loot, navegación |
| Editor tools | [docs/editor-tools.md](docs/editor-tools.md) | Constructores, importadores, balance |
| Art pipeline | [docs/art-pipeline.md](docs/art-pipeline.md) | Mixamo, Meshy, Blender, Git LFS, exportar |

## Estructura
```
Assets/
  Scenes/                 MainMenu, Comisaria
  _Project/
    Scripts/              Core, Player, Weapons, Enemies, Interaction, Inventory, UI, Animation, Editor
    Data/                 ItemData y WeaponData (ScriptableObjects)
    Prefabs/              Characters, Doors, Interactables, Lighting
    Animation/            Animators, overrides, máscaras
    Art/Mixamo/           Personajes y clips importados
    Audio/ Materials/
Tools/                    Blender, Mixamo, adaptador de Meshy
docs/                     Documentación por módulos
.claude/skills/           Skills del proyecto (pedir-modelo-ia, modelo-blender)
```

## Normas de trabajo
- Al cambiar un módulo: actualizar `docs/<módulo>.md` y añadir una línea al registro de abajo, en el mismo commit.
- Git LFS para binarios. No recompilar Unity mientras corre una generación de modelo.
- La versión estable está en la etiqueta `estable`.

## Registro de cambios
Más reciente primero. Formato: fecha · módulo · cambio.

- 2026-10-06 · Core/Enemies/Player · Sonidos reales: packs CC0 + síntesis (zombis, puertas, pasos, armas, teléfono, jefe); pasos de escalera; el teléfono suena cerca; rugido y pasos del jefe; alerta de zombis; recargas por arma. Traspaso en `docs/estado-actual.md`.
- 2026-10-06 · Level/Notes/Enemies · **Tramo final tras el jefe** (`PostBossWing`): la puerta norte de la arena pasa a ser una puerta con la llave de salida y detrás hay pasillo, vestíbulo, sala segura 4 (teléfono, baúl, 2 notas), sala de control (llave del garaje, taquilla con código 0316 con la 2ª riñonera, 2 zombis) y garaje (el portón que ahora termina la partida, pide la llave del garaje). 4 zombis nuevos, NavMesh comprobado (la arena conecta con las 3 salas), textos de las notas provisionales. Sin probar en Play.
- 2026-10-06 · Audio · Pasos del jugador (`stepGain` 0,4) y daño que recibe el personaje (`playerHurtGain` 0,5) bajados con dos controles más en `GameAudio`; se ajustan en el inspector.
- 2026-10-06 · Audio · Mezcla tras probar la música: música a 0,7 (antes 0,45) y dos controles nuevos en `GameAudio` (`doorGain` 0,4 y `zombieGain` 0,5) que bajan las puertas y, otra vez, los zombis. Los valores se ajustan en el inspector sin tocar código.
- 2026-10-06 · Audio · **Música de tensión de fondo** nueva con fal (ElevenLabs Music v2.5, ~1,65 USD): pista de fondo de 85 s y capa de persecución de 70 s, instrumentales, con bucle sin corte, asignadas en `GameAudio`. Sin oír.
- 2026-10-06 · Interaction/Inventory/Enemies · Teléfono de guardado: ya no suena por el mapa, solo al usarlo (`SaveTerminal.ringsNearby` = false). Modelos propios (Blender, `build_keys.py`) para la llave del garaje, la tarjeta de acceso y la llave maestra, con iconos. Zombis Pxl corregidos tras verlos en Play (parada de alerta 0,35 s, animación a ~1,5×, corredor a 2,8 m/s).
- 2026-10-06 · Enemies/Art · Importado el pack de la Asset Store **Zombie (Pxltiger)**: 3 zombis humanoides nuevos (`Zombie_Pxl1/2/3`: equilibrado, rápido y lento-resistente) con material URP y 10 animaciones, construidos con `PxlZombieKit`. Ocupan los puestos de los Civil retirados y varían las zonas tras el jefe (9 zombis Pxl en total, 23 zombis en la escena). Documentado el procedimiento de importación y la licencia en `docs/art-pipeline.md`. Sin probar en Play.
- 2026-10-06 · Level/Enemies · **Zona 2 con segundo jefe** (`Zone2Wing`): el portón del garaje pasa a ser una puerta con llave hacia sala segura 5, cuarto de bombas, sala de máquinas, laboratorio (tarjeta de acceso + pista del código 7258), almacén (taquilla con la 3ª riñonera) y la arena del segundo jefe (`Boss_2` "COLOSSUS II", mismo prefab, provisional; suelta la llave maestra). El portón final con la llave maestra termina ahora la partida. 6 zombis nuevos, 2 notas, 3 llaves/objetos nuevos; NavMesh comprobado del garaje a la arena. Sin probar en Play.
- 2026-10-06 · Weapons/Audio · Volumen de disparo por arma (`WeaponData.fireVolume`) en lugar de uno global: pistola vuelve a su clip original a 1,0 (sonaba mejor) y la escopeta baja a 0,5 (sonaba demasiado alta). Se retira `WeaponController.fireVolume`.
- 2026-10-06 · Core/UI · El juego pasa a llamarse **Sector 7: Grimheim** (producto de Unity, título del menú principal, ejecutable `Sector7_Grimheim.exe`); los guardados de la carpeta `Comisaria` se copian solos a la nueva. El nombre de la escena `Comisaria` no cambia (es el nivel).
- 2026-10-06 · Interaction/Audio · Teléfono de guardado más fuerte: clip normalizado (pico 0,66 → 0,97), volumen 0,55 → 1,0, `ringRange` 14 m y alcance sonoro 30 m (`GameAudio.Play` admite `range`).
- 2026-10-06 · Core/Audio · Chirrido constante de fondo corregido: el zumbido de lámpara de fal era un pitido de ~8,5 kHz sonando en las 29 lámparas; vuelve el `lamp_hum.wav` grave y cada zumbido baja a 0,08 de volumen y 7 m de alcance.
- 2026-10-06 · Enemies/Level · Zombi Civil (verde) retirado de la escena y de los constructores (se bugeaba siempre): quedan 8 zombis + jefe. Prefab intacto para reponerlo cuando se arregle.
- 2026-10-06 · Weapons/Audio · Disparos más fuertes (`WeaponController.fireVolume` = 2, segunda fuente de audio cuando el volumen pasa de 1); escopeta regenerada en fal y recortada (cola larga), pistola normalizada (estaba a 0,61 de pico).
- 2026-10-06 · Core/Enemies/Audio · Mezcla tras las pruebas: curva de atenuación propia que sí se apaga (22 m), amortiguación entre plantas (los zombis de arriba ya no se oyen abajo), zombis a 0,5-0,65 de volumen para que los disparos destaquen; el primer disparo de escopeta de fal (sonaba a recarga) se descartó.
- 2026-10-06 · UI/Inventory · Pantalla de **objeto conseguido** al recoger una riñonera (`ItemShowcase`, `GameState.ShowcaseOpen`): pausa, modelo 3D girando con su nombre y "+2 casillas", E/Esc continúa. Sin probar en Play.
- 2026-10-06 · Art/Inventory · Modelo de la riñonera hecho en Blender (`Tools/blender/build_rinonera.py` → `Art/Props/Rinonera.fbx`, ~1.400 triángulos) con miniatura de inventario, asignado a `I_Bag`. Dos intentos con Meshy (texto→3D) habían devuelto una persona y se descartaron.
- 2026-10-06 · Level/Lighting · Iluminación recalibrada (el centro de las salas salía quemado): suavidad de `Env_*` de 1,0 a 0,2-0,55 (el suelo era un espejo), Bloom a 0,3, focos a la mitad, relleno subido a 2,55 m (antes a 1,9 m, otro pico sobre la lámpara) y con alcance medido contra las paredes de cada sala. Arena del jefe sin tocar. Detalle en `docs/level.md`.
- 2026-10-06 · Notes/UI/Level · **Archivo** (pestaña de Tab, categorías Historia y Pistas, guardado en la partida), notas leíbles en hoja 2D (`NoteData`, `ReadableNote`), teclado numérico y taquilla con código (`LockerDoor.code`, `Keypad`). Puzzle de la riñonera: nota con el código 4719 en la mesa del archivo → taquilla de la sala de reuniones (planta alta) con la riñonera dentro; más una nota de historia (parte de guardia) en el interrogatorio. Montaje: `ArchiveSetup`. Textos de las notas provisionales. La riñonera ya no está en la arena del jefe.
- 2026-10-06 · Core/UI · 5 slots de guardado (`savegame_1..5.json`): lista de slots (fecha + objetivo) en el teléfono (con confirmación al sobrescribir), en la pausa y en el menú principal; el guardado antiguo pasa al slot 1. Archivos: `SaveSystem`, `SaveSlotsGUI`, `Hud`, `MainMenu`.
- 2026-10-06 · Inventory/Core · Concepto de riñonera (como la hip pouch de RE2): objeto `I_Bag` (`ItemType.Bag`) que al recogerlo suma 2 casillas permanentes sin ocupar una (tope 6 extra → 14), guardado en la partida; una colocada en el escritorio de la arena del jefe. Pendiente: modelo propio, vista previa/animación al recogerla, más riñoneras en salas nuevas (puzzle de taquilla con código o dos piezas) y salas tras el jefe.
- 2026-10-06 · Level/Lighting · Planta superior: las 11 lámparas estaban repetidas 5 veces (55 focos con sombra apilados, centro quemado); quedan 29 lámparas en total, una por posición. `UpperFloor.Build` ya no las duplica al reconstruir.
- 2026-10-06 · Weapons/Audio · Disparos de pistola y escopeta generados con fal.ai (`fal-ai/elevenlabs/sound-effects/v2`, `Audio/Generated/*_fal.mp3`) asignados en `W_Pistol`/`W_Shotgun` (`fireSound`); `TestSceneBuilder` los prefiere a los sintetizados. Pendiente que el usuario los oiga.
- 2026-10-06 · Core/Audio · Los 39 clips de fal.ai (`Audio/Generated/*_fal.mp3`: zombis, jefe, jugador, puertas, pasos, escalera, teléfono, objetos, lámparas, latido, sustos, recargas) cableados en `GameAudio`, `W_Pistol`/`W_Shotgun` y la escena `Comisaria`. `TestSceneBuilder` los prefiere a los `.wav`. Sin hueco: `shotgun_pump`. Música sigue con los clips antiguos. Pendiente de oír en juego.
- 2026-10-06 · Core/Art pipeline · Plan de sonidos reales con fal.ai (`Tools/audio/sfx_prompts.md`), pendiente de la clave en el editor.
- 2026-10-06 · Level/Enemies · Planta superior con escalera: pasillo y seis salas (archivo, interrogatorio, descanso, despacho del jefe, sala segura 3 con teléfono, reuniones); la llave de la sala pasa al despacho del jefe; 5 zombis nuevos arriba; cubículo del pie de la escalera retirado.
- 2026-10-06 · Interaction/UI · Punto de guardado físico: teléfono antiguo de disco (prefab `SavePhone`, modelo hecho en Blender) en lugar del terminal; menú de guardado en tonos cálidos.
- 2026-10-06 · Weapons · Cargador de la escopeta bajado a 3 cartuchos.
- 2026-10-06 · Docs · Documentación por módulos en `docs/` y este registro.
- 2026-10-06 · Art pipeline · Skills `pedir-modelo-ia` y `modelo-blender` (`.claude/skills/`).
- 2026-10-06 · Editor tools · Ajustes de producto: nombre "Comisaria", empresa "iftraba", ventana 1920×1080; primera compilación Windows a `Desktop/Juego`.
- 2026-10-06 · UI · Menú principal (escena `MainMenu`), opciones de volumen y sensibilidad, botón "Menú principal" en la pausa. Core: `GameSettings`; `SaveSystem` carga por nombre de escena.
- 2026-10-06 · Animation · Disparo sin cambio de clip + retroceso por código; la corrección del torso se filtra para evitar tirones.
- 2026-10-06 · Animation · Alineación del arma con la dirección de disparo (yaw y pitch) en `PlayerAnimation.LateUpdate`.
- 2026-10-06 · Animation · Animaciones reales del protagonista: reposo, andar con pistola, apuntar con pistola, recarga (pistola y arma larga) y golpe recibido. `MixamoImport` descarta el clip vacío "Take 001".
- 2026-10-05 · Player · Protagonista Soldier de Mixamo (Humanoid), `aimSpeed` 1.7.
- 2026-10-05 · Enemies/Level · Jefe "EL COLOSO" y zona del jefe (pasillo, sala segura 2, sala final iluminada), zombis Civil/Girl/Cop/Yaku con modelos propios.
