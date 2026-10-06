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
