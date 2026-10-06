# Estado actual y traspaso (2026-10-06)

Resumen para retomar el trabajo en una conversación nueva sin perder contexto. Se actualiza al cerrar cada tanda de cambios.

## Qué hay hecho
- Juego jugable en Unity 6000.6.4f1 (URP): planta baja, **planta superior con escalera**, zona del jefe, protagonista Soldier, 4 zombis + jefe con modelos del usuario, menú principal, guardado con **teléfono antiguo** en tres salas seguras, inventario y baúl global.
- Versión estable en la etiqueta `estable` (anterior a menú, planta superior y teléfonos; moverla cuando el usuario lo pida).
- Exportación Windows en `Desktop/Juego/Sector7_Grimheim.exe`: última build el 2026-10-06 (commit `111be06`: slots, Archivo, taquilla con código, riñonera, luces recalibradas, 39 sonidos, mezcla de audio corregida). Volver a compilar al añadir cosas (ver `docs/art-pipeline.md`).
- Documentación por módulos en `docs/` y registro de cambios en el README (regla: actualizar en cada cambio).
- Skills del proyecto en `.claude/skills/`: `pedir-modelo-ia`, `modelo-blender`.

## Sonidos
- Montados con packs CC0 gratuitos + síntesis: `Tools/audio/build_free_audio.py` (fuentes en `Tools/audio/download/`, fuera de git; copia de los sintetizados antiguos en `Tools/audio/backup_synth/`). Lista de prompts de IA: `Tools/audio/sfx_prompts.md`.
- Código nuevo: teléfono que suena cerca, pasos de escalera, rugido y pasos del jefe, alerta de zombis, recargas por arma.
- Clips ya **asignados** en `GameAudio` (escalera, teléfono, jefe, quejidos x3) y en `W_Pistol`/`W_Shotgun` (recargas) y la escena guardada.
- No se puede oír el audio al montarlo: el usuario debe probarlo y decir qué sonidos no encajan para cambiarlos por otro del pack.

## IA generativa de audio (fal.ai)
- El plugin de Unity falla con `Model schema expansion failed` (ningún modelo funciona; no es la clave ni la red).
- El **MCP oficial de fal** está añadido a la configuración de usuario y autenticado (`claude mcp list` → fal conectado). Hace falta una **sesión nueva** para que cargue sus herramientas.
- **Mostrar siempre el modelo y el prompt exacto al usuario antes de enviar nada** (regla permanente). Prompts: `Tools/audio/sfx_prompts.md`.
- Modelo en uso: `fal-ai/elevenlabs/sound-effects/v2` (~0,002 USD/s; `cassetteai` ya no está en el catálogo). Se llama con las herramientas MCP `mcp__fal__*` y se descarga con curl a `Assets/_Project/Audio/Generated/`.
- Hechos y **cableados** (39 clips `*_fal.mp3` en `Audio/Generated/`): disparos y recargas en `W_Pistol`/`W_Shotgun` (WeaponData); el resto en `GameAudio` de la escena `Comisaria`. `TestSceneBuilder` prefiere los `_fal` a los `.wav`. Sin hueco en el código: `shotgun_pump`. Falta: música (`stable-audio`, pedir OK antes), recortar/comprobar los bucles `lamp_hum` y `heartbeat`, y que el usuario los oiga en juego (cambiar los que no encajen).
- Incidencia: una petición dio 403 `account_locked` al recuperar el resultado aunque la cuenta figuraba lista; reenviar el mismo prompt funcionó.

- **Cambios tras las primeras pruebas del usuario (2026-10-06):** zombis demasiado altos y los de arriba se oían desde abajo → curva de atenuación propia, amortiguación entre plantas y zombis a 0,5-0,65 (ver `docs/core.md`). El primer disparo de escopeta de fal sonaba a recarga (descartado y borrado). Se regeneró con el prompt "Single powerful 12 gauge shotgun gunshot, one huge explosive bang with a deep low boom and a short echoing tail in a large room…" (ElevenLabs SFX v2, 2,5 s) y como su cola era muy larga y fuerte (energía 0,40 entre 0,4 y 1 s frente a 0,08-0,11 de los otros) se **recortó con una caída exponencial** a 1,5 s: `Generated/shotgun_shot_fal.wav` (original en `shotgun_shot_v2_original.mp3`). Si sigue sin sonar a escopeta, el respaldo es `Audio/shotgun_shot.wav`. La pistola de fal estaba grabada baja (pico 0,61): se normalizó a 0,95 en `pistol_shot_fal.wav` (original `pistol_shot_original.mp3`). **Volumen de disparo por arma** (`WeaponData.fireVolume`; un valor > 1 suma una segunda fuente porque un `AudioSource` no pasa de 1). Primero se subieron todos a 2 y el usuario dijo que la pistola sonaba mejor antes y la escopeta demasiado alta: ahora la pistola vuelve a su clip original de fal (`pistol_shot_fal.mp3`, sin normalizar) a 1,0 y la escopeta queda a 0,5. Los análisis de volumen se hicieron leyendo las muestras (no se puede oír).

- **Chirrido constante (2026-10-06):** era el zumbido de las lámparas: `lamp_hum_fal.mp3` tenía su frecuencia dominante en ~8.500 Hz (pitido agudo) y suena en cada una de las 29 lámparas. Se volvió a `Audio/lamp_hum.wav` (~100 Hz, grave); el de fal quedó apartado como `Generated/lamp_hum_descartado_pitido.mp3` (se puede borrar). Además cada zumbido baja a volumen 0,08 y alcance 7 m (`CeilingLamp`). Los clips `*_fal` de bucle (`heartbeat_fal`) no se han oído: si molestan, mismo remedio (volver al `.wav`).

- **Interfaz de vida y balas "desaparecida" (2026-10-06): no era un fallo del HUD.** Claude recompiló scripts mientras el usuario tenía el editor en Play; la recarga de dominio dejó el juego con vida 0, sin arma equipada e inventario con 0 casillas (el HUD sí se dibujaba en una partida nueva). Regla: comprobar `EditorApplication.isPlaying` antes de tocar scripts (memoria `no-recompilar-en-play`). El aviso "Failed to create agent because there is no valid NavMesh" del `Player.log` ya salía antes (NavMesh horneado en `Start`).
- **Teléfono**: sonaba bajo (clip con la mitad de energía + volumen 0,55 + atenuación nueva); normalizado y subido (ver README).
- **Nombre del juego**: Sector 7: Grimheim. El ejecutable ahora es `Desktop/Juego/Sector7_Grimheim.exe`.
- Servidor MCP de Unity: si se cae, `Tools/run_mcp_server.bat` lo levanta (usar `cmd /c` con la ruta entrecomillada); sin las herramientas `mcp__unity-mcp__*` en la sesión se habla por HTTP con `http://127.0.0.1:8080/mcp`.

## Entorno
- Unity necesita el módulo Windows Mono sano y **Smart App Control desactivado** (lo bloqueaba). Se restauró la DLL original `Unity.AspNetCore.NamedPipeSupport.dll` (existe `.bak` en la carpeta del runner).
- El servidor MCP de Unity se arranca desde **Window → MCP for Unity → Start Server**; si las herramientas `mcp__unity-mcp__*` no aparecen en la sesión, se puede hablar con él por HTTP en `http://127.0.0.1:8080/mcp` (initialize + tools/call).
- `claude.exe` se copió a `C:\Users\iftra\.local\bin\claude.exe` para poder usar `claude` desde cualquier terminal.
- No hacer pruebas en Play si el usuario está jugando; sin foco, Unity no avanza (usar pausa + `EditorApplication.Step()`).

## Riñoneras y guardado (ideas del usuario)
- Hecho: concepto de riñonera (`I_Bag`, +2 casillas, tope 6), una dentro de la taquilla con código de la sala de reuniones. Es pequeña, tipo riñonera, NO mochila.
- Hecho: **5 slots de guardado** (`savegame_1..5.json`; selector en el teléfono, la pausa y el menú; migración automática del guardado antiguo al slot 1). Sin probar en Play: el usuario debe comprobar las pantallas.
- Hecho: **Archivo** (Historia/Pistas) y puzzle de la riñonera: nota con el código 4719 (mesa del archivo, planta alta) → taquilla con código en la sala de reuniones → riñonera dentro. Hay además una nota de historia en el interrogatorio. Textos provisionales. Sin probar en Play.
- Hecho: pantalla de objeto conseguido al recoger la riñonera (`ItemShowcase`, modelo 3D girando); sin probar en Play.
- Por hacer: animación del personaje al ponerse la riñonera (a futuro); **salas después del jefe** (un par) y más riñoneras: o colocadas, o con puzzle (taquilla bloqueada arriba que pide un código o dos piezas sueltas).
- Luces: corregidas las lámparas duplicadas de la planta alta y recalibrada la iluminación (suavidad de `Env_*`, bloom, relleno a 2,55 m con alcance medido; ver `docs/level.md`). Sin probar en Play.
- Modelo de la riñonera: **hecho en Blender** (`Tools/blender/build_rinonera.py`, 1.400 triángulos), asignado a `I_Bag` con su miniatura y colocado en la taquilla. Antecedente: **dos intentos con Meshy 6 en modo texto (`generate_model`) salieron como una persona de 25 cm** (soldado la primera vez, persona de negro con los pulgares arriba la segunda; 41 k triángulos, 8,7 MB cada uno), pese a que el segundo prompt decía "not a person, not a figurine". Se descartaron y se borraron los archivos. No repetir en modo texto para objetos sencillos: Blender (skill `modelo-blender`) o imagen → 3D. Cada intento gasta créditos de Meshy; el plugin no permite fijar el número de polígonos.

## Después de que el usuario y sus compañeros prueben la build (2026-10-06)
Lo que hay que comprobar en el `.exe` (todo se montó sin verlo jugando):
1. **Luces**: ¿alguna sala sigue quemada o demasiado oscura? (se recalibraron todas menos la arena del jefe). Anotar sala y qué se ve.
2. **Sonidos** (39 clips de fal.ai): ¿cuáles no encajan? En especial: disparos (pistola/escopeta), `lamp_hum` y `heartbeat` (bucles sin recortar), gruñidos, teléfono. Repetir los malos con otro prompt (enseñar el prompt antes de enviar). Falta el hueco para `shotgun_pump`.
3. **Puzzle de la riñonera**: nota (mesa del archivo, planta alta) → código 4719 → taquilla de reuniones → riñonera → pantalla de objeto conseguido → inventario de 10 casillas.
4. **Archivo** (pestaña de Tab, Q cambia; Historia/Pistas) y lectura de notas a pantalla completa.
5. **5 slots de guardado**: guardar en el teléfono, sobrescribir (pide confirmación), cargar desde pausa y menú; comprobar que las casillas de riñonera y las notas leídas vuelven al cargar.
6. **Jefe completo** (baile → puerta → combate → llave → salida), zombi Yaku y planta superior con zombis.
7. Rendimiento y cualquier cosa que se rompa solo en la build (algunos efectos de postproceso no entran).

## Pendiente (para seguir)
1. Arreglar lo que salga de las pruebas de arriba. **Zombi Civil (verde)**: quitado de la escena porque se bugeaba siempre (4 unidades); investigar la causa (animación/NavMesh/modelo `ZombieGenerated`) y reponerlo. Quedan 8 zombis + jefe.
2. **Salas después del jefe** (un par) y decidir cómo termina el juego; más riñoneras (colocadas o con puzzle) cuando haya salas.
3. **Textos reales** de las notas (los actuales los escribió Claude como provisionales) y más notas de historia/pistas, quizá con dibujos.
4. Animación del personaje al ponerse la riñonera (a futuro); animación de disparo de pistola (descargar "Pistol Fire"/"Shooting" de Mixamo con el Soldier).
5. Música (`music_ambient`, `music_tension`) con `stable-audio` de fal, pedir OK antes de enviar; recortar los bucles de las lámparas y el latido.
6. Más zonas, ventanas/azotea, mejoras de postproceso.
7. Mover la etiqueta `estable` al último commit estable cuando el usuario lo pida.
