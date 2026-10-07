---
name: recompile
description: Recompila la build de Windows de Sector 7: Grimheim (Desktop/Juego/Sector7_Grimheim.exe) con el estado actual del proyecto. Solo cuando el usuario lo pida (/recompile o "recompila la build"); nunca la ofrezcas por tu cuenta.
---

# Recompilar la build de Windows

Se usa **únicamente cuando el usuario lo pide**. No ofrezcas recompilar al terminar otras tareas.

## Pasos
1. Comprueba que Unity no está en Play (`EditorApplication.isPlaying` por `execute_code`); si lo está, avisa y espera (recompilar con el usuario jugando le borra vida/arma/inventario).
2. Si hay scripts sin compilar, fuerza la compilación (`refresh_unity` con `compile: request`, `wait_for_ready`) y comprueba que no hay `error CS` en la consola.
3. Guarda la escena abierta (`EditorSceneManager.SaveOpenScenes()`) para que la build lleve lo último.
4. Lanza la build con `manage_build`:
   `action: build`, `target: StandaloneWindows64`, `output_path: C:/Users/iftra/Desktop/Juego/Sector7_Grimheim.exe`
   (sin pasar `scenes`: usa las de Build Settings). Tarda ~30-90 s; espera y consulta con `manage_build action: status` hasta `result: succeeded` (revisa `errors: 0`).
5. Actualiza en `docs/estado-actual.md` (línea de "Exportación Windows") la fecha y el último commit incluido (`git log --oneline -1`), según `docs/art-pipeline.md`. Di al usuario que ese cambio de docs queda sin commit.
6. Responde con el resultado en una o dos líneas: éxito o fallo, tamaño y ruta del `.exe`. No ofrezcas nada más.

## Si el servidor MCP de Unity no responde
Usa el cliente HTTP (`mcp_call.py`) contra `http://127.0.0.1:8080/mcp`; si el servidor está caído, el usuario debe abrir Unity y `Tools/run_mcp_server.bat`.
