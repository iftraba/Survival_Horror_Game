# Estado actual y traspaso (2026-10-06)

Resumen para retomar el trabajo en una conversación nueva sin perder contexto. Se actualiza al cerrar cada tanda de cambios.

## Qué hay hecho
- Juego jugable en Unity 6000.6.4f1 (URP): planta baja, **planta superior con escalera**, zona del jefe, protagonista Soldier, 4 zombis + jefe con modelos del usuario, menú principal, guardado con **teléfono antiguo** en tres salas seguras, inventario y baúl global.
- Versión estable en la etiqueta `estable` (anterior a menú, planta superior y teléfonos; moverla cuando el usuario lo pida).
- Exportación Windows hecha una vez en `Desktop/Juego/Comisaria.exe` (volver a compilar para incluir lo nuevo).
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

## Entorno
- Unity necesita el módulo Windows Mono sano y **Smart App Control desactivado** (lo bloqueaba). Se restauró la DLL original `Unity.AspNetCore.NamedPipeSupport.dll` (existe `.bak` en la carpeta del runner).
- El servidor MCP de Unity se arranca desde **Window → MCP for Unity → Start Server**; si las herramientas `mcp__unity-mcp__*` no aparecen en la sesión, se puede hablar con él por HTTP en `http://127.0.0.1:8080/mcp` (initialize + tools/call).
- `claude.exe` se copió a `C:\Users\iftra\.local\bin\claude.exe` para poder usar `claude` desde cualquier terminal.
- No hacer pruebas en Play si el usuario está jugando; sin foco, Unity no avanza (usar pausa + `EditorApplication.Step()`).

## Pendiente
1. Comprobar los sonidos en juego y ajustar.
2. Probar a fondo: jefe (baile → puerta → combate → llave → salida), zombi Yaku, planta superior con zombis.
3. Volver a exportar el `.exe` con todo lo nuevo.
4. Más zonas, ventanas/azotea, mejoras de postproceso (algunos efectos no entran en la build).
5. Animación de disparo de pistola (descargar "Pistol Fire"/"Shooting" de Mixamo con el Soldier).
6. Mover la etiqueta `estable` al último commit estable cuando el usuario lo pida.
