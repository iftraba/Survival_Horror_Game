# Resumen de la sesión: rediseño de la comisaría (rama `rediseno-comisaria`)

Fecha de cierre: 2026-10-09. Último commit: `e98f123`. Todo subido a GitHub.

## Estado actual
- **Escena de trabajo:** `Assets/Scenes/Comisaria_v2.unity`. Su nivel se genera con `ComisariaGrande` (fase A) y los kits de las fases B a E. Hasta ahora se usaba la `Comisaria_v2` de las fases 1 a 6 (edificio pequeño); el de la comisaría grande la sustituye.
- **Escena original:** `Assets/Scenes/Comisaria.unity`, sin tocar, de referencia.
- **Builds en el escritorio:** dos carpetas, `Sector7 - Version 1 (comisaria original)` y `Sector7 - Version 2 (comisaria nueva)`. La **Version 2 se recompiló el 2026-10-09** con la comisaría grande y las sombras por cercanía (`docs/sombras.md`); la Version 1 sigue siendo anterior. Para recompilar la v2 hay que pasar las escenas `MainMenu` + `Comisaria_v2` a mano (Build Settings lleva `Comisaria`); el skill `/recompile` apunta a `Desktop/Juego`, que ya no existe.
- **Build antigua:** `Desktop/Juego` (commit `ad06e54`), sin tocar.

## Qué se ha hecho (en orden)
1. **Fases 1 a 6 de la comisaría v2** (edificio de 40 × 32 m): estructura, vestíbulo, exterior e intro, primera planta, archivo con jefe 1, sótano con jefe 2, recorrido con medallones, ascensor, portón final.
2. **Dos versiones del juego:** `GameSettings.GameScene` elige la escena según la build. Guardados de la v2 en archivos aparte (`savegame_v2_N.json`).
3. **Comisaría grande (rehecha):**
   - **A. Estructura:** 64 × 44 m, 73 espacios en 4 plantas, paredes y puertas generadas desde una lista de datos (`ComisariaGrande.Define`). Callejón con escalera de incendios y azotea.
   - **B. Exterior e intro:** patio, verja con zombis, calle, coches ardiendo, edificios, fachada con huecos. Escena de cámara recentrada.
   - **C. Mobiliario:** cada sala según su tipo (`ComisariaGrandeProps`), unos 1.200 muebles y 8 modelos nuevos.
   - **D. Puzles:** cizalla y candados, tarjetas (seguridad y jefe de seguridad), taquillas con código (4519, 0832, 7258), sala a oscuras, medallones, fusibles y puerta sin corriente, jefes y portón final. Componentes nuevos: `LockVisual`, `ProgressSeal`.
   - **E. Zombis y botín:** 74 zombis (12 en letargo), 23 objetos sueltos.

## Planes y pendiente
- **Fase F: pase de juego y equilibrio.** Jugarlo entero de principio a fin. Equilibrar munición y zombis (`ComisariaGrandeEnemies.Spawns`). Pase de luces: fugas de luz por fuera del archivo y salas algo oscuras.
- **Rendimiento:** medir con todos los zombis en una build. Si va justo: occlusion culling o menos zombis.
- **Recompilar las dos builds** del escritorio con la comisaría grande cuando se quiera probarla fuera del editor.
- **Documentación:** `docs/rediseno-comisaria.md` tiene las fases A a E. Falta el pase F. El README tiene las entradas del registro.
- **Menús viejos:** `Horror/Comisaria v2/…` son del diseño anterior. No ejecutarlos sobre `Comisaria_v2`: la estropean. Los nuevos son `Horror/Comisaria grande/1…5`.

## Reglas de trabajo del proyecto (recordatorio)
- Commit y push a `rediseno-comisaria` al cerrar cada tarea.
- No recompilar Unity mientras esté en Play. Comprobar `EditorApplication.isPlaying` antes.
- No ofrecer recompilar; el usuario usa `/recompile` cuando lo pide.
- Antes de enviar un prompt a una IA generativa (fal, Meshy), enseñarlo al usuario y esperar su OK.
- Actualizar `docs/<módulo>.md` y el registro del README con cada cambio.

## Notas útiles para retomar
- **Pruebas en Play:** con el editor parado, `EditorApplication.isPlaying = true` y avanzar fotogramas con `EditorApplication.Step` (si no, el tiempo no avanza).
- **Caminos de NavMesh:** `ComisariaGrande` usa voxel de 0,1 m. Con el de por defecto, las puertas de 1,6 m se cierran al hornear.
- **Materiales horneados:** `texture_items.py blend` por cada FBX nuevo. Los tipos de material se eligen por nombre (`stone`, `body`, `metal`…).
- **Modelos:** Blender headless con `build_*.py` en `Tools/blender/`. Las copias de respaldo en `Tools/raw_generated/backup_*`.
