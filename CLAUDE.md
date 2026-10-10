# Sector 7: Grimheim (Unity 6 URP, survival horror)

Juego de terror en tercera persona en Unity 6000.6.4f1 (URP), con modelos hechos en Blender (headless). El usuario escribe en español: responde en español.

## Dónde está todo
- **Estado y traspaso:** `docs/resumen-sesion-comisaria.md` (rediseño actual) y `docs/estado-actual.md`. Léelos al empezar una sesión.
- **Documentación por módulo:** `docs/<modulo>.md`. Plan y fases del rediseño: `docs/rediseno-comisaria.md`.
- **Planes aprobados:** `docs/planes/` (plantilla en `docs/planes/PLANTILLA.md`).
- **Registro de cambios:** `README.md`, sección «Registro de cambios» (más reciente primero).
- Rama de trabajo actual: `rediseno-comisaria`. `main` es la estable.

## Reglas de trabajo (del usuario, permanentes)
1. **Plan antes de trabajar.** En tareas no triviales, enseña un plan corto y espera el OK. Dentro de un plan aprobado se trabaja de seguido. Para cosas pequeñas y reversibles, adelante.
2. **Nunca compiles ni edites scripts con Unity en Play.** Comprueba `EditorApplication.isPlaying` antes; si es `True`, espera a que el usuario salga. Recompilar jugando le borra vida, arma e inventario.
3. **No ofrezcas recompilar la build.** Solo con `/recompile` cuando el usuario lo pida.
4. **Prompts de IA generativa** (fal, Meshy…): enseña el modelo y el prompt exacto y espera el OK antes de enviarlos.
5. **Documenta cada cambio:** actualiza `docs/<modulo>.md` y el registro del README.
6. **Commit y push** a la rama actual al cerrar cada tarea. Los mensajes van en español y con la línea `Co-Authored-By` que indique el sistema.
7. No archivos sueltos en la raíz: los temporales van al directorio scratchpad de la sesión.

## Repartir el trabajo por modelo
- **Opus (agente `disenador`):** define diseño, flujo de nivel, puzles, equilibrio y arquitectura. Solo lee y escribe planes en `docs/planes/`.
- **Sonnet (agente `aplicador`):** aplica un plan ya aprobado (kits de editor, scripts, Blender, escena) y lo prueba.
- **Haiku (agente `rutinas`):** tareas cíclicas: docs y registro del README, commits y push, comprobaciones de rutas, capturas, listados.
- Revisiones de solo lectura: skills `level-review` (nivel, recorrido, equilibrio, luz) y `performance-audit` (mediciones del profiler).
- Un agente nuevo empieza sin contexto: el relevo siempre es un documento de `docs/planes/`.
- **Un solo escritor sobre la escena a la vez.** Solo hay un Unity abierto. Haiku puede ir en paralelo porque solo toca documentos.

## Cómo se trabaja con Unity y Blender aquí
- **Unity por MCP** (`mcp__unity-mcp__*`): `execute_code` ejecuta C# en el editor. `refresh_unity` con `compile: request` compila.
- **Kits de editor** (`Assets/_Project/Scripts/Editor/`): cada uno es un menú repetible que reconstruye su parte de la escena. Los de la comisaría grande son `ComisariaGrande*` (menú *Horror/Comisaria grande/1…5*).
- **Estado a medias (2026-10-10):** la planta se está rehaciendo (`docs/planes/fase-f-parte-2-rediseno-planta.md`). Ya están adaptados los menús 1 (estructura v3), 2 (exterior), 3 (mobiliario), 4 (puzles), 5 (zombis y botín) y 6 (reservas de arenas); **no ejecutes el 7 (luces)** hasta la etapa F, y recuerda que el menú 1 borra mobiliario, puzles, zombis y botín (orden 1→2→3→4→5→6).
- **Trampas de `Comisaria_v2`:** los menús viejos *Horror/Comisaria v2/…* son del diseño anterior y estropean la escena: no los ejecutes. El NavMesh se hornea con voxel de 0,1 m (con el de por defecto las puertas de 1,6 m se cierran).
- **Probar en Play:** en el editor, `EditorApplication.isPlaying = true` y avanzar fotogramas con `EditorApplication.Step()` desde un callback (si no, el tiempo no avanza). Salta la intro por reflexión (campo `skip` de `IntroCutscene`).
- **Blender:** `Tools/blender/build_*.py`, headless con `blender --background --python … -- <salida>`. Cada FBX nuevo se hornea con `Tools/blender/texture_items.py blend` y se registra en `ItemTextureKit`. Skill `modelo-blender`.
- **Todo MonoBehaviour va en un archivo con su mismo nombre**; si no, Unity no lo serializa.

## Informar con honradez
No des por probado lo que no se ha ejecutado. Di qué se comprobó (y cómo) y qué no. Si algo falla, cuéntalo con la salida real.
