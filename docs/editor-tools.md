# Editor tools

Scripts de editor (envueltos en `#if UNITY_EDITOR`) en `Assets/_Project/Scripts/Editor/`. Se ejecutan desde el menú **Horror** o por MCP (`execute_code`).

| Herramienta | Menú | Qué hace |
|---|---|---|
| `MixamoImport` | — | Configura personajes y animaciones como Humanoid. Cada clip tiene su avatar; el desplazamiento XZ de la raíz se **extrae** (no se hornea), Y y rotación sí; la velocidad de zancada sale de `AnimationClip.averageSpeed`. Descarta el clip vacío "Take 001" que trae Mixamo. |
| `PlayerKit` | Horror/Construir protagonista | Construye `PlayerHumanoid.controller` y `UpperBodyHumanoid.mask` con los clips `P_*` que haya y reemplaza el modelo del prefab `Player` por el Soldier, con los agarres de arma. Se repite al llegar clips nuevos. |
| `ZombieKit` | Horror/Construir zombis y jefe | Genera `ZombieHumanoid.controller`, un override por tipo, `Boss.controller` y los prefabs de zombis y jefe. Tiempos de impacto de ataque detectados automáticamente (primer pico de velocidad de mano/pie). |
| `BossBalance` | — | Simula disparos para fijar la vida del jefe (`Apply`). |
| `BossWing` | Horror/Reformar zona del jefe | Reforma la zona final: pasillo, sala segura 2, puertas, sala del jefe, loot y zombis. |
| `UpperFloor` | Horror/Construir planta superior | Construye escalera, hueco del forjado, muros, salas, luces, mobiliario, botín y zombis de la planta superior. Idempotente (rehace `UpperFloor_*` y `CeilingSlab`; deja el `Ceiling` original desactivado). |
| `PxlZombieKit` | Horror/Construir zombis Pxltiger | Crea los dos `AnimatorOverrideController` (`Zombie_Pxl1`, `Zombie_Pxl2`) sobre `ZombieHumanoid.controller` y los prefabs `Zombie_Pxl1/2/3` a partir del pack de Pxltiger (`Assets/Zombie`). Reutiliza `ZombieKit.BuildPrefab` (campo nuevo `Spec.fbxPath`). Repetible. |
| `PostBossWing` | Horror/Tramo final (despues del jefe) | Construye el tramo final al norte de la arena (pasillo, vestíbulo, sala segura 4, sala de control, garaje), cambia la salida por la puerta `Door_Exit` con llave, crea `I_KeyGarage`, dos notas (`note_comunicado_sector7`, `note_armario_mando`), la taquilla con código 0316 (con la 2ª riñonera) y 4 zombis `PB_*`. Idempotente (rehace `PostBoss_*`, zombis `PB_*` y botín con z > 20,5). **Si se vuelve a ejecutar `BossWing`, hay que ejecutar este después**: `BossWing.Loot/Zombies` borran el botín (z > 9) y los zombis. Usa `ArchiveSetup.MakeCodeLocker` y `ArchiveSetup.Note`. |
| `Zone2Wing` | Horror/Zona 2 (segundo jefe) | Construye la zona 2 al norte del garaje (z 39-85): sala segura 5, cuarto de bombas, sala de máquinas, laboratorio, pasillo, almacén y la arena del segundo jefe con el portón final; crea `I_KeyCard` e `I_KeyFinal`, dos notas (`note_registro_mantenimiento`, `note_hoja_pruebas`), la taquilla 7258 con la 3ª riñonera, 6 zombis `Z2_*` y `Boss_2` (prefab `Boss` con `bossName` "COLOSSUS II" y `dropOnDeath` = llave maestra, más su `BossRoomTrigger_2`). Idempotente (rehace `Zone2_*`, zombis `Z2_*`/`Boss_2` y botín con z > 39,5). **Orden: `BossWing` → `PostBossWing` → `Zone2Wing`** (cada uno borra lo suyo: reejecutar uno no toca al siguiente, pero `BossWing` borra zombis y botín de más al norte). |
| `ArchiveSetup` | Horror/Notas y taquilla con codigo | Crea las notas (`Data/Notes/*.asset`), las registra en `ItemDatabase.notes`, coloca los papeles (pista del código en la mesa del archivo, parte de guardia en el interrogatorio), monta la taquilla con código (4719) en la sala de reuniones y deja dentro la riñonera. Idempotente (rehace `UpperFloor_Extras` y retira cualquier riñonera suelta). |
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
