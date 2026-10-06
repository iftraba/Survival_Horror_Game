# Editor tools

Scripts de editor (envueltos en `#if UNITY_EDITOR`) en `Assets/_Project/Scripts/Editor/`. Se ejecutan desde el menú **Horror** o por MCP (`execute_code`).

| Herramienta | Menú | Qué hace |
|---|---|---|
| `MixamoImport` | — | Configura personajes y animaciones como Humanoid. Cada clip tiene su avatar; el desplazamiento XZ de la raíz se **extrae** (no se hornea), Y y rotación sí; la velocidad de zancada sale de `AnimationClip.averageSpeed`. Descarta el clip vacío "Take 001" que trae Mixamo. |
| `PlayerKit` | Horror/Construir protagonista | Construye `PlayerHumanoid.controller` y `UpperBodyHumanoid.mask` con los clips `P_*` que haya y reemplaza el modelo del prefab `Player` por el Soldier, con los agarres de arma. Se repite al llegar clips nuevos. |
| `ZombieKit` | Horror/Construir zombis y jefe | Genera `ZombieHumanoid.controller`, un override por tipo, `Boss.controller` y los prefabs de zombis y jefe. Tiempos de impacto de ataque detectados automáticamente (primer pico de velocidad de mano/pie). |
| `BossBalance` | — | Simula disparos para fijar la vida del jefe (`Apply`). |
| `BossWing` | Horror/Reformar zona del jefe | Reforma la zona final: pasillo, sala segura 2, puertas, sala del jefe, loot y zombis. |
| `SavePhone` | Horror/Crear teléfono de guardado | Crea el prefab `SavePhone` (mesita + teléfono + interactuable + luz) y sustituye los terminales de la escena en su posición y orientación. El modelo sale de `Tools/blender/build_phone.py`. |
| `MainMenuBuilder` | Horror/Crear menú principal | Crea la escena `MainMenu` y deja las dos escenas en los ajustes de compilación. |
| `GraphicsSetup` | — | Ajustes de URP (antialiasing, sombras, SSAO, post-proceso). |
| `ItemIcons` | — | Genera las miniaturas del inventario fotografiando los modelos. |
| `SceneMigration` / `TestSceneBuilder` | — | **Legado**: el nivel se generaba por código hasta la Fase 0. La escena es ahora la fuente de verdad; `BossWing` sigue usando helpers privados de `TestSceneBuilder` por reflexión. |

## Flujo habitual
1. Importar clips de Mixamo → `MixamoImport.ConfigureAnimations`.
2. Reconstruir el protagonista o los zombis con su Kit.
3. Guardar la escena y comprobar en Play (pausa + `EditorApplication.Step()` si el editor no tiene foco).

## Avisos
- No recompilar Unity mientras un trabajo de generación (Meshy) esté en curso.
- No hacer pruebas en Play si el usuario está jugando.
