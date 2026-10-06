# Player

Control, cámara y sonido del protagonista. Código en `Assets/_Project/Scripts/Player/`. Prefab: `Prefabs/Characters/Player.prefab`.

## Clases
| Clase | Qué hace |
|---|---|
| `PlayerController` | Movimiento con `CharacterController`: andar (`walkSpeed` 2.4), correr (`runSpeed` 4.4, Mayús, no mientras apunta) y apuntar (`aimSpeed` 1.7, la velocidad de los pasos laterales de Mixamo). Al apuntar, el cuerpo se orienta a la cámara. Empuja objetos con `pushPower`. `IsAiming` y `IsRunning` son de solo lectura. |
| `ThirdPersonCamera` | Cámara al hombro estilo RE2. Se acerca y desplaza al apuntar. La sensibilidad se multiplica por `GameSettings.Sensitivity`. Expone `Yaw`/`SetYaw` para guardar y cargar. |
| `Flashlight` | Linterna (F) con parpadeo leve. |
| `PlayerFootsteps` | Pasos por distancia recorrida: más rápidos y fuertes al correr, sigilosos al apuntar. |
| `PlayerAudio` | Quejidos al recibir daño, muerte y latido con poca vida. |

## Composición del prefab
Raíz con `PlayerController`, `CharacterController`, `Health`, `Inventory`, `WeaponController`, `PlayerAnimation`,
`PlayerInteractor`. Hijo `Model` = Soldier de Mixamo (Humanoid) con los *holders* de arma en la mano derecha.
Lo regenera el menú **Horror/Construir protagonista** (ver `editor-tools.md`).

## Decisiones
- El movimiento lo da el `CharacterController`; el `Animator` no usa root motion (`applyRootMotion = false`).
- La velocidad al apuntar coincide con los clips de pasos laterales para que los pies no patinen.
