---
name: pendiente-comprobar
description: Muestra y gestiona la lista de lo que queda por probar y comprobar del rediseño de la comisaría grande (docs/pendiente-de-comprobar.md). Úsalo cuando el usuario pida qué falta por probar o comprobar, el pendiente de verificación, o quiera ejecutar y tachar esas comprobaciones.
argument-hint: "[sección, 'todo' o 'ejecutar <apartado>']"
---

# Pendiente de comprobar

Fuente única: `docs/pendiente-de-comprobar.md` (lista con casillas `[ ]`/`[x]`, etiquetas **[PC]** comprobable con herramientas, **[JUGAR]** necesita al usuario, **[ETAPA]** depende de una etapa por hacer).

## Pasos
1. Lee `docs/pendiente-de-comprobar.md` entero (y `CLAUDE.md` si no lo has leído en la sesión).
2. Responde en español con un resumen corto: cuántas casillas hay abiertas por sección y por etiqueta, y lo más importante primero (progresión y bloqueos, después salas, zombis/botín, luz). Si el usuario dio un argumento (una sección, «todo» o un apartado), limita o amplía el listado a eso.
3. No marques nada como hecho sin haberlo comprobado de verdad. Si se ejecuta una comprobación [PC] (sigue las reglas de `CLAUDE.md`: nunca compilar ni editar scripts con Unity en Play; plan antes de trabajar si no es trivial), tacha la casilla en el md con fecha y cómo se comprobó; si falla, anota la salida real y deja la casilla abierta.
4. Lo nuevo que se descubra sin comprobar se añade al md en su sección. Al cerrar, actualiza «Última actualización» y haz commit y push a la rama actual (solo los archivos propios).
5. No ofrezcas recompilar la build.
