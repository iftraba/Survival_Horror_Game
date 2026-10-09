---
name: plan
description: Prepara un plan por escrito antes de trabajar en una fase nueva, un rediseño o un cambio que afecte a varios sistemas. Lo deja en docs/planes/ y espera la aprobación del usuario. Úsalo cuando el usuario diga /plan o cuando la tarea no sea trivial.
argument-hint: "<tema o fase>"
---

# Plan antes de trabajar

No implementes nada con este skill. El usuario quiere ver el plan y aprobarlo primero.

1. Lee `CLAUDE.md`, `docs/resumen-sesion-comisaria.md` y los `docs/` que toquen el tema. Mira `git status`.
2. Si hay decisiones de diseño grandes (flujo de nivel, puzles, equilibrio, arquitectura), delega el diseño en el agente `disenador` (Opus). Si es una tarea más técnica y acotada, escribe tú el plan.
3. Escribe el plan en `docs/planes/AAAA-MM-DD-tema.md` con la estructura de `docs/planes/PLANTILLA.md`.
4. Enseña al usuario un resumen corto: objetivo, pasos, qué se probará y qué se queda sin probar, y las **decisiones abiertas** con tu recomendación.
5. Espera su OK. No compiles ni toques la escena hasta entonces.

Una vez aprobado:
- Los cambios en Unity, scripts o Blender los hace el agente `aplicador` (Sonnet), uno a la vez.
- La documentación, el registro del README y los commits los puede hacer el agente `rutinas` (Haiku).
