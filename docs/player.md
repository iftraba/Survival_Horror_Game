# Player

Control, cámara y sonido del protagonista. Código en `Assets/_Project/Scripts/Player/`. Prefab: `Prefabs/Characters/Player.prefab`.

## Clases
| Clase | Qué hace |
|---|---|
| `PlayerController` | Movimiento con `CharacterController`: andar (`walkSpeed` 2.4), correr (`runSpeed` 4.4, Mayús, no mientras apunta) y apuntar (`aimSpeed` 1.7, la velocidad de los pasos laterales de Mixamo). Al apuntar, el cuerpo se orienta a la cámara. Empuja objetos con `pushPower`. `IsAiming` y `IsRunning` son de solo lectura. |
| `PlayerController` (giro rápido) | **Q** gira al personaje y a la cámara 180° en `quickTurnTime` (0,22 s) mediante `ThirdPersonCamera.AddYaw`; durante el giro no se reorienta. |
| `ThirdPersonCamera` | Cámara al hombro estilo RE2. Se acerca y desplaza al apuntar (apuntando: distancia 1,2 m, hombro 0,6, FOV 44; antes 1,7 / 0,65 / 48). La sensibilidad se multiplica por `GameSettings.Sensitivity`. Expone `Yaw`/`SetYaw` para guardar y cargar. |
| `Flashlight` | Linterna (F) con parpadeo leve. |
| `PlayerFootsteps` | Pasos por distancia recorrida: más rápidos y fuertes al correr, sigilosos al apuntar. |
| `PlayerAudio` | Quejidos al recibir daño, muerte y latido con poca vida. |

## Acciones con animación (`PlayerActions`, 2026-10-08)
Componente que añade `PlayerController` si falta. Mientras dura una acción, `PlayerActions.Locked` quita el control (moverse, apuntar, disparar, interactuar).
- **Abrir puerta** (`Door.Interact` al abrir): se gira hacia la puerta, guarda el arma, alarga la mano y la hoja gira cuando la mano llega (0,75 s); control a los 1,4 s.
- **Entrar en la sala del jefe**: si al otro lado de la puerta hay un `BossRoomTrigger` con el jefe dormido, se coloca delante, abre y cruza hasta pasado el disparador (el recorrido sigue la cadera del clip). No se puede asomar y volver; la puerta se atranca al terminar.
- **Giro corriendo**: Q mientras corres sin apuntar: derrapa, gira 180° a la izquierda y la cámara gira con él. Quieto o andando, Q sigue siendo el giro rápido.
- **Voltereta**: al caer 1,6 m o más (`PlayerController.rollFallHeight`).
- **Escaleras**: corriendo y subiendo, animación de subir escaleras.
- **Agarre** de un zombi: ver `enemies.md`.

## Movimiento ampliado (Pro Rifle Pack y Action Adventure Pack, 2026-10-08)
- **Escopeta**: `PlayerAnimation.longGunController` es el override `PlayerHumanoid_Long` (reposo, andar, correr, laterales y salto del pack de rifle). Al cambiar de arma, la capa del torso hace *Rifle Pull Out* / *Put Back Rifle*.
- **Agacharse (C)**: `PlayerController.IsCrouching`, 1,2 m/s, mapa de 8 direcciones del pack de rifle; la cámara baja 45 cm. Correr o pulsar espacio lo levanta. Agachado y a más del 55 % de su distancia de detección, un zombi no te ve (`ZombieAI.CanSeePlayer`).
- **Saltar (espacio)**: 0,6 m (`jumpSpeed` 4,8); estados `JumpUp` → `Airborne` → `Land`. Cualquier caída de más de 0,35 s pasa a `Airborne`. Al aterrizar de 1,6 m o más, voltereta; de 3,5 m o más, aterrizaje duro (`PlayerActions.HardLand`).
- **Cobertura (V)**: `PlayerActions.TryCover` busca una pared delante (rayo a la altura de las rodillas; de pie si también la hay a la altura del pecho, agachado si es baja o vas agachado). Se pega de espaldas a la pared; A/D (según la cámara) lo desliza mientras haya pared (en el borde se para). Sale con V o espacio, empujando hacia fuera de la pared o, directamente, apuntando (clic derecho). Los clips de cobertura llevan la raíz sin girar: el código orienta al personaje.
- **Gestos en reposo**: con la pistola, tras 12-24 s quieto, uno de cuatro (`A_Idle2..5`).
- **Muerte** según de dónde llega el último golpe: de frente, por la espalda, por la derecha (clips del pack de rifle) o la de siempre.

## Composición del prefab
Raíz con `PlayerController`, `CharacterController`, `Health`, `Inventory`, `WeaponController`, `PlayerAnimation`,
`PlayerInteractor`. Hijo `Model` = Soldier de Mixamo (Humanoid) con los *holders* de arma en la mano derecha.
Lo regenera el menú **Horror/Construir protagonista** (ver `editor-tools.md`).

## Decisiones
- El movimiento lo da el `CharacterController`; el `Animator` no usa root motion (`applyRootMotion = false`).
- La velocidad al apuntar coincide con los clips de pasos laterales para que los pies no patinen.
