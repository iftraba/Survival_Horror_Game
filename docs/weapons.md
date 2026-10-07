# Weapons

Armas, disparo y munición cargada. Código en `Assets/_Project/Scripts/Weapons/`.

## Clases
| Clase | Qué hace |
|---|---|
| `WeaponData` (ScriptableObject) | Datos de un arma: modelo en mano (`heldPrefab`, `heldScale`), sonidos (`fireSound`, `reloadSound` y `cycleSound` con `cycleDelay`/`cycleVolume`: el bombeo de la escopeta, que `WeaponController.CycleRoutine` reproduce tras cada disparo si sigues con el arma, no recargas y queda munición), **casquillos** (`casingPrefab`, `ejectAtCycle`, `ejectOffset` en ejes de la cámara, `ejectSpeed`: la pistola expulsa su casquillo al disparar y la escopeta su cartucho al bombear; `WeaponController.EjectCasing` lo lanza con física y sin chocar con el jugador; `EjectedCasing` los rebota, los encoge a los 9 s y limita a 40 a la vez; `clinkSound` queda sin sonido), tipo de munición, `damage`, `range`, `fireRate`, `magazineSize`, `reloadTime`, `pellets`, `spread`, `automatic`, `twoHanded`. Assets: `Data/W_Pistol`, `W_Shotgun`. |
| `WeaponController` | Equipa el arma (instancia el modelo en `handSocket` o `longSocket` según `twoHanded`), gestiona cargadores (`MagAmmo`, `ReserveAmmo` desde el inventario), recarga (R o automática al disparar con el cargador vacío) y disparo con el clic izquierdo mientras se apunta. |

## Atajos de arma (2026-10-07)
`WeaponHotkeys`: las teclas **1-4** equipan el arma asignada (o la guardan si ya está en la mano). Se asignan en el inventario: arma seleccionada + tecla 1-4 (la misma tecla otra vez la libera) y se ve una insignia con el número en la casilla. Se guardan en `PlayerPrefs` (`hotkey_1..4`, por nombre de objeto) y solo valen si el arma está en el inventario; las armas nuevas toman solas la primera tecla libre (`EnsureDefaults`). `WeaponController.Update` los lee antes de comprobar si hay arma equipada.

## Flujo de disparo
1. `Fire()` resta una bala, emite `Fired`, reproduce sonido y llama a `ZombieAI.Noise` (radio 14 m) para atraer enemigos.
2. Por cada perdigón (`pellets`) se lanza un rayo **desde la cámara de apuntado** con dispersión aleatoria (`spread`).
3. `RaycastAll` ordenado por distancia; el primer impacto válido aplica `damage` a un `IDamageable`. Las zonas de `ZombieHitZones` aportan el multiplicador (cabeza ×3 por defecto) y emiten el evento de impacto en cabeza.
4. Eventos: `Fired`, `ReloadStarted` (los usa `PlayerAnimation`).

## Equilibrio
- Pistola y escopeta: ver los assets `W_*`. La escopeta dispara 8 perdigones de 14 y su **cargador es de 3** cartuchos (recarga 2.4 s).
- La vida del jefe (1410) se calcula con `BossBalance` (ver `enemies.md`) para morir con ~15 escopetazos a la cabeza a 5 m.

## Decisiones
- La dirección de disparo es la de la cámara; el torso del jugador se alinea con ella (`PlayerAnimation.LateUpdate`, ver `animation.md`).
- El retroceso visual es por código (no hay clip de disparo específico de pistola).
