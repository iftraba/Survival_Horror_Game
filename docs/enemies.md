# Enemies

Zombis y jefe. Código en `Assets/_Project/Scripts/Enemies/`. Prefabs en `Prefabs/Characters/`: `Zombie_Civil`, `Zombie_Girl`, `Zombie_Cop`, `Zombie_Yaku`, `Boss`.

## Clases
| Clase | Qué hace |
|---|---|
| `ZombieAI` | Máquina de estados sobre `NavMeshAgent`: vagar → alerta (grito, `alertTime`) → perseguir → atacar. Ve al jugador por distancia/ángulo/visión directa y oye disparos (`Noise`). Ataque con variantes (`AttackVariant`: `hitDelay`, `damageMultiplier`, `cooldown`) y daño retardado (`LandHit`) que se cancela si muere, se aleja (1.35× alcance) o es aturdido. Fuerza puertas no bloqueadas tras `zombieForceTime`. Estático `All` (vivos) y `ActiveBoss`. |
| `ZombieHitZones` | Cápsulas/esferas que siguen a los huesos (Humanoid o por nombre): cabeza, torso, brazos, piernas, escaladas por el tamaño del modelo. Multiplicador por zona (cabeza ×3 por defecto). |
| `ZombieAudio` | Gruñidos (más frecuentes en persecución), ataque, quejido y muerte en 3D; gruñido de alerta al detectar al jugador; el jefe tiene voz grave, rugido al despertar (`Alerted`) y pasos pesados. |
| `BossRoomTrigger` | Volumen tras la puerta del jefe: al entrar el jugador, `boss.Wake()`. |

## Jefe ("EL COLOSO")
- Modelo PumpkinHulk, escala 1.35, `dormant`: baila (Gangnam Style) hasta que el jugador entra en la sala; entonces ruge (`alertTime` 2.6 s) y combate.
- Ataque 55 de daño (100 de vida del jugador: muere con 2 golpes), sin aturdimiento.
- Vida **1410**: la fija `BossBalance.Apply(headShots=15, distance=5)` tras simular 6000 disparos (escopeta 8×14, dispersión ±4°). Cabeza = punto débil; torso ×0.5 y extremidades ×0.25 con zonas agrandadas.
- Al morir suelta `I_KeyExit` y fija el objetivo de recogerla y salir. Barra de vida del HUD mientras pelea.

## Reparto
(Reparto original; los civiles se retiraron y ahora hay zombis Pxl en sus puestos, y más enemigos en las zonas tras el jefe.) Planta baja: 2 civiles, 2 chicas, 2 policías; la planta superior añade civil ×2, chica, policía y el **Yaku**, que guarda la llave en el despacho del jefe. El jefe está en su sala. Los zombis suben y bajan por la escalera (rampa de NavMesh invisible).

## Tipos de zombi (definidos en `ZombieKit.Kinds`)
| Tipo | Rasgos |
|---|---|
| Civil | Tambaleo lento, 100 de vida. **Retirado de la escena (2026-10-06)**: el usuario vio que se bugeaba siempre; el prefab sigue en `Prefabs/Characters/` y los constructores (`BossWing.Zombies`, `UpperFloor.Zombies`) tienen sus cuatro puestos comentados (pasillo de la oficina, barricada, pasillo de arriba y reuniones). Para reponerlo hay que investigar antes qué falla. |
| Girl | Más pequeña (0.84), 80 de vida |
| Cop | Lento y resistente (140), escala 0.9 |
| Yaku | Corre, 130 de vida, 20 de daño |
| **Pxl1** (`Zombie_Pxl1`) | Del pack de la Asset Store *Zombie* de **Pxltiger** (ver `art-pipeline.md`). Equilibrado: 110 de vida, 15 de daño, persigue a 0,6 m/s. Sustituye a los Civil retirados. |
| **Pxl2** | Rápido y frágil: 90 de vida, 12 de daño, corre a 2,2 m/s (usa la animación de carrera). |
| **Pxl3** | Lento y resistente: 170 de vida, 22 de daño, 0,4 m/s. |

Los Pxl se construyen con `PxlZombieKit` (menú *Horror/Construir zombis Pxltiger*) sobre el mismo controlador base y prefab que el resto (`ZombieKit.BuildPrefab` con `Spec.fbxPath`). El pack solo trae **un ataque** y **ninguna reacción al golpe ni grito de alerta**: el ataque se repite en las tres variantes, el golpe y el grito reutilizan el idle (no se ve reacción) y la muerte es `Z_FallingBack`. Su clip de andar avanza a 0,27 m/s, así que la persecución se mantiene en 0,4-0,6 m/s (el modo "gait" acelera hasta 2,2× sin patinar).
Reparto de los Pxl: oficina (Pxl1) y barricada (Pxl3) abajo; pasillo (Pxl1) y reuniones (Pxl2) arriba; sala de control (Pxl1) y garaje (Pxl2); cuarto de bombas (Pxl2), sala de máquinas (Pxl3) y laboratorio (Pxl1) de la zona 2.

## Decisiones
- La velocidad de animación se ajusta a la velocidad real del agente ("gait mode", ver `animation.md`) para evitar patinar.
- Los zombis rodean muebles: `RuntimeNavMesh` + `NavMeshAgent.climb` 0.3 impiden subirse.
- Si el jugador carga partida, los zombis muertos se retiran y los vivos recuperan posición y vida.
