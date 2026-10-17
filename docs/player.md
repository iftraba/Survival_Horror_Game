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
- **Abrir puerta** (`Door.Interact` al abrir): primero camina hasta quedar delante del pomo (a 0,6 m de la hoja, en su lado) y se gira hacia ella; luego guarda el arma, alarga la mano y la hoja gira cuando la mano llega (0,75 s); control a los 1,4 s.
- **Entrar en la sala del jefe**: si al otro lado de la puerta hay un `BossRoomTrigger` con el jefe dormido, se coloca delante, abre y cruza hasta pasado el disparador (el recorrido sigue la cadera del clip). No se puede asomar y volver; la puerta se atranca al terminar.
- **Giro corriendo**: quitado a petición del usuario; Q siempre es el giro rápido de 180°.
- **Voltereta**: al caer 1,6 m o más (`PlayerController.rollFallHeight`).
- **Escaleras**: corriendo y subiendo, animación de subir escaleras.
- **Agarre** de un zombi: ver `enemies.md`.

## Movimiento ampliado (Pro Rifle Pack y Action Adventure Pack, 2026-10-08)
- **Escopeta**: `PlayerAnimation.longGunController` es el override `PlayerHumanoid_Long` (reposo, andar, correr, laterales y salto del pack de rifle). Al cambiar de arma, la capa del torso hace *Rifle Pull Out* / *Put Back Rifle*.
- **Agacharse (C)**: `PlayerController.IsCrouching`, 1,2 m/s, mapa de 8 direcciones del pack de rifle; la cámara baja 45 cm. Correr o pulsar espacio lo levanta. Agachado y a más del 55 % de su distancia de detección, un zombi no te ve (`ZombieAI.CanSeePlayer`).
- **Saltar (espacio)**: 0,6 m (`jumpSpeed` 4,8); estados `JumpUp` → `Airborne` → `Land`. Cualquier caída de más de 0,35 s pasa a `Airborne`. Al aterrizar de 1,6 m o más, voltereta; de 3,5 m o más, aterrizaje duro (`PlayerActions.HardLand`).
- **Cobertura (V)**: `PlayerActions.TryCover` busca una pared delante (rayo a la altura de las rodillas; de pie si también la hay a la altura del pecho, agachado si es baja o vas agachado). Se pega de espaldas a la pared; A/D (según la cámara) lo desliza mientras haya pared (en el borde se para). Sale con V o espacio, empujando hacia fuera de la pared o, directamente, apuntando (clic derecho). Los clips de cobertura llevan la raíz sin girar y con la orientación basada en el cuerpo (`keepOriginalOrientation` falso): así todos miran hacia delante y el código orienta al personaje (antes, al deslizarse quedaba de cara a la pared).
- **Gestos en reposo**: con la pistola, tras 12-24 s quieto, uno de cuatro (`A_Idle2..5`).
- **Muerte** según de dónde llega el último golpe: de frente, por la espalda, por la derecha (clips del pack de rifle) o la de siempre.

## Pistola (2026-10-17)
Clips de Mixamo descargados con la sesión del usuario (lote 1, en `PlayerAnims/`):
- **Apuntar:** `P_PistolAimIdle` («Idle With Aimed Pistol»), a dos manos.
- **Pasos laterales apuntando:** `P_PistolStrafeLeft` y `P_PistolStrafeRight`. En el override de arma larga se cambian por los del rifle.
- **Recarga:** `P_PistolReload` («Gun Reload While Walking»). Mixamo no tiene recarga de pistola de pie; como solo cuenta el torso, vale también parado.
- **Desenfundar:** `P_PistolDraw` («Drawing Gun»), al equipar la pistola (estado `DrawPistol` de la capa del torso; no se anima al cargar ni en el primer segundo). Si el torso está en una transición, espera a que acabe (unos 0,25 s).
- **Disparo:** la pose de apuntar más un retroceso por código en `PlayerAnimation.LateUpdate`: los brazos suben 9° con el arma y vuelven en 0,2 s, y el torso 4°.
  - «Firing A Gun» (`P_PistolFire2`) inclina la cadera; en la capa del torso, con la cadera recta, echaba el torso atrás y levantaba el brazo.
  - «Shooting Handgun» (`P_PistolFire`) dispara hacia arriba.
  - Los dos se quedan importados, sin usar, igual que «Pistol Aim To Holster Idle» (`P_PistolHolster`), que es bajar el arma y quedarse en reposo (7 s), no enfundar.
- **Agarre:** `WeaponHolder` (pistola) se calcula con la pose de apuntar con pistola, recto al frente y sin ladear. `LongWeaponHolder` sigue con la del rifle. Antes los dos usaban la del rifle y la pistola salía torcida con los clips nuevos.
- **Comprobado en Play** (capturas): apuntar con el botón derecho simulado, disparo real (baja una bala, fogonazo y casquillo), recarga, paso lateral y desenfundar (lanzado a mano).

## Composición del prefab
Raíz con `PlayerController`, `CharacterController`, `Health`, `Inventory`, `WeaponController`, `PlayerAnimation`,
`PlayerInteractor`. Hijo `Model` = Soldier de Mixamo (Humanoid) con los *holders* de arma en la mano derecha.
Lo regenera el menú **Horror/Construir protagonista** (ver `editor-tools.md`).

## Personajes jugables (menú oculto, 2026-10-17)
- **Cómo se abre:** en el menú principal, F3 siete veces seguidas (menos de 1,5 s entre pulsaciones) abre «Selección de personaje». No hay otra forma de abrirlo. Elegir uno empieza una partida nueva con él; «Nueva partida» normal siempre usa el soldado.
- **Personajes:** 0 = Soldado (el `Model` del prefab) y 1 = Chica (biped rigeado de Meshy que descargó el usuario).
- **Cómo funciona:** `GameSettings.Character` guarda el elegido; `PlayerModelSwap` (en el prefab Player, orden −100) instancia `Prefabs/Characters/PlayerModel_Chica.prefab` en lugar de `Model` antes de que lo lean `PlayerAnimation` y `WeaponController` (les pasa su Animator y sus `WeaponHolder`/`LongWeaponHolder`). Va en la partida guardada (`SaveData.character`) y se restaura al cargar, antes de recargar la escena.
- **Modelo:** `Art/Mixamo/Characters/Chica/` (FBX, material URP con color, normal y metal-suavidad 4K). El FBX de Meshy traía el esqueleto tumbado y la cadera a ~100 m (unidades mezcladas) y la malla explotaba al animar; se arregla con `Tools/blender/fix_meshy_rig.py` antes de copiarlo. Lo monta el menú **Horror/Personajes/Construir chica (menú oculto)** (`PlayerCharacterKit`): Humanoid sin animaciones propias (usa las del soldado), altura al 95 % del soldado (escala ×1,06) y agarres del arma calculados con la pose de apuntar, como en `PlayerKit`.
- **Pendiente:** se ve algún corte en la piel de los muslos al animar (pesos del rig automático de Meshy). Probado en Play solo en reposo con la pistola; apuntar, disparar, la escopeta y las acciones (puertas, cobertura, agarre) no se han mirado con ella.

## Decisiones
- El movimiento lo da el `CharacterController`; el `Animator` no usa root motion (`applyRootMotion = false`).
- La velocidad al apuntar coincide con los clips de pasos laterales para que los pies no patinen.
