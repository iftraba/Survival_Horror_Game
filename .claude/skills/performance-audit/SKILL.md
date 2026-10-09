---
name: performance-audit
description: Audita el rendimiento de Sector 7 (comisaría grande, escena Comisaria_v2) - zombis, luces, mallas, animadores, NavMesh y oclusión - con mediciones reales del profiler y sin inventar cifras. Úsalo antes de recompilar una build para probar la comisaría grande, o si el usuario nota tirones. Solo mide e informa.
argument-hint: "[escena, planta o sistema]"
---

# Auditoría de rendimiento

Mide e informa; no cambies nada sin que el usuario lo apruebe. **No des cifras que no hayas medido** y no digas que algo "mejora" sin comparar antes y después en las mismas condiciones.

## 0. Datos que no se saben (pregúntalo si hace falta)
- Equipo objetivo, resolución y fotogramas por segundo que quiere el usuario: **DESCONOCIDOS** hasta que lo diga. Sin eso no hay presupuesto numérico, solo mediciones y comparaciones.
- Comprueba `EditorApplication.isPlaying` antes de compilar o cambiar nada.

## 1. Recuento (sin ejecutar el juego)
Con `execute_code`, en la escena abierta:
- Zombis, `NavMeshAgent` y animadores: total y por planta. Fíjate en los modelos pesados (el reptante tiene unos 50.000 triángulos; los demás 10.000 a 14.000).
- Luces: cuántas `CeilingLamp`, cuántas con sombras y de qué tipo, y las luces de fuego y emergencia.
- Mallas: renderers totales, cuántos son estáticos (`isStatic`) y cuántos materiales distintos hay.
- Colliders y `MeshCollider` (la rampa de la escalera de caracol es de malla).
- Oclusión: ¿hay datos de occlusion culling horneados? (las paredes de la comisaría grande no ocultan nada por defecto).

## 2. Medición real
- Usa el profiler por MCP (`manage_profiler`) en Play: CPU, GPU, memoria, llamadas de dibujo, triángulos, `SetPass`.
- Escenarios repetibles y comparables: (a) entrada y vestíbulo, (b) pasillo largo con varias salas a la vista, (c) archivo con el jefe 1, (d) sala de calderas con el jefe 2, (e) planta con más zombis despiertos.
- Distingue el **editor** de una **build**: el editor es más lento. Si el usuario nota tirones en la build, pide o mide ahí.
- Para avanzar fotogramas en Play desde la herramienta, sigue lo de `CLAUDE.md`.

## 3. Clasifica lo que encuentres
- **Medido:** cuello de botella con cifra y escenario.
- **Riesgo del código o de la escena:** probable, sin medir.
- **Hipótesis:** a comprobar con la prueba más pequeña posible.

## 4. Palancas posibles (solo si la medición lo justifica)
Occlusion culling o portales por sala, menos sombras de lámpara, `Animator` con culling de transformaciones y zombis lejanos desactivados, instanciado de materiales, `MeshCollider` por colisión simple, menos zombis por planta (`ComisariaGrandeEnemies.Spawns`), luces con rango menor.

## 5. Informe
1. Recuento (tabla).
2. Mediciones por escenario, con las condiciones.
3. Cuellos de botella medidos y riesgos sin medir.
4. Propuestas por orden de coste y beneficio, cada una con cómo se comprobaría.
5. Lo que no se pudo medir y por qué.
