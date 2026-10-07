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

**ABOMINACIÓN** (`Zombie_BossPxl`, segundo jefe, sala de calderas): el modelo del zombi 3 del pack a escala 1,65 (≈ 3 m) con material propio `Zombie_URP_Boss` (tinte verde y un brillo muy tenue: el escenario es oscuro y un brillo mayor tapa la textura). 1800 de vida (zonas: cabeza ×3, torso ×0,65, extremidades ×0,3: unas 99 balas de pistola o ~25 cartuchos al torso, parecido al jefe 1), 60 de daño, persigue a 1,4 m/s efectivos (`chase` 1,17 × 1,2 global), alcance 2,6 m, recarga 2 s, no se aturde, empieza dormido y suelta la llave maestra. **Ataques especiales** (`BossAttacks`, suspende la IA normal con `ZombieAI.Suspended`): *embestida* (se para, ruge y el cuerpo brilla en rojo ~1 s; corre en línea recta a 8 m/s hacia donde estabas, 45 de daño si te toca; si choca con una columna o la caldera se aturde 3 s y recibe ×1,8 de daño, vía `Health.damageTakenMultiplier`), *escupitajo de ácido* (proyectil `AcidSpit` a 12 m/s, 12 de daño, deja un charco donde cae; 3 a la vez en la fase 3) y **fases** al 66 % y 33 % de vida (ruge, +12 % de velocidad, golpes más seguidos, rastro más denso, avisos más cortos). Elige ataque cada 3,5-6 s si hay línea de visión y no estás pegado a él (a cuerpo a cuerpo manda el golpe normal). Durante la embestida `ZombieAnimation.speedOverride` fuerza la animación de carrera, porque `agent.Move` no actualiza `agent.velocity`. Sonidos propios (`chargeSound`, `crashSound`, `spitSound`; `ToxicTrail.sizzleSound` al pisar un charco), asignados por `PxlZombieKit.BuildBoss` desde `Audio/Generated/`. Probado en Play: fases, escupitajo y embestida contra la caldera.

**Rastro de ácido** (`ToxicTrail`): mientras anda deja un charco verde cada 1,4 m que dura 9 s (se encoge al final) y hace 6 de daño cada 0,5 s a quien lo pise; sin colisión, así no estorba al NavMesh ni a las balas. Los charcos son un quad con textura procedural (borde irregular, burbujas, borde lima; `ToxicPuddle.mat`, transparente) girado al azar, y la bola del escupitajo una esfera con textura veteada y emisiva (`ToxicGlob.mat`) con luz verde; ambas texturas las genera `ToxicTextures` (menú *Horror/Texturas de acido*). Se construye en `PxlZombieKit.BuildBoss`, con un override de `Boss.controller` que mapea las animaciones del jefe a las del pack (sin grito ni baile propios: reutiliza reposo y ataque).

Los Pxl se construyen con `PxlZombieKit` (menú *Horror/Construir zombis Pxltiger*) sobre el mismo controlador base y prefab que el resto (`ZombieKit.BuildPrefab` con `Spec.fbxPath`). El pack solo trae **un ataque** y **ninguna reacción al golpe ni grito de alerta**: el ataque se repite en las tres variantes, el golpe y el grito reutilizan el idle (no se ve reacción) y la muerte es `Z_FallingBack`. Su clip de andar avanza a 0,27 m/s, así que la persecución es de 0,34-0,42 m/s (animación a ~1,3-1,5×) y el corredor Pxl2 va a 2,8 m/s (carrera a ~0,76×). Se ajustó tras verlos en Play: a 0,6 m/s la animación iba a 2× y se veía nerviosa, a 2,2 m/s el corredor iba a cámara lenta (0,6×), y con `alertTime` 1,6 se quedaban congelados al detectarte porque no hay animación de grito: ahora 0,35 s.
Reparto de los Pxl: oficina (Pxl1) y barricada (Pxl3) abajo; pasillo (Pxl1) y reuniones (Pxl2) arriba; sala de control (Pxl1) y garaje (Pxl2); cuarto de bombas (Pxl2), sala de máquinas (Pxl3) y laboratorio (Pxl1) de la zona 2.

## Dificultad: velocidad de los enemigos
`GameFlow.enemySpeedMultiplier` (1,2 desde el 2026-10-06, a petición del usuario: +20 %) multiplica la velocidad de persecución de **todos** los enemigos, jefes incluidos (`ZombieAI.SpeedMultiplier`, que `GameFlow.Start` fija al empezar). Los valores de cada tipo (`chaseSpeed`) no cambian: p. ej. Cop 0,5 → 0,6 m/s, Yaku 2,2 → 2,64, jefes 1,9 → 2,28, Pxl1 0,42 → 0,50. La animación se adapta sola (el modo "gait" lee la velocidad real del agente). **Suelo de velocidad** (`GameFlow.minEnemySpeed` = 1,0 m/s, `ZombieAI.MinSpeed`): ningún enemigo persigue por debajo de ese valor (el usuario seguía viendo zombis "muy lentos" con solo el +20 %), así que Cop, Girl, Pxl1 y Pxl3 pasan a 1,0 m/s (los tipos lentos quedan igualados; Yaku 2,64, Pxl2 3,36 y los jefes no cambian). `ZombieAnimation` amplía el tope de reproducción del andar a 3× (antes 2,2×) para que los pies no patinen tanto. Para volver a la velocidad original, poner el multiplicador a 1 y el suelo a 0; los jefes con el tope de reproducción de carrera (1,6×) pueden patinar un poco.

## Decisiones
- La velocidad de animación se ajusta a la velocidad real del agente ("gait mode", ver `animation.md`) para evitar patinar.
- Los zombis rodean muebles: `RuntimeNavMesh` + `NavMeshAgent.climb` 0.3 impiden subirse.
- Si el jugador carga partida, los zombis muertos se retiran y los vivos recuperan posición y vida.
