# Animation

Conexión de los personajes con sus `Animator`. Código en `Assets/_Project/Scripts/Animation/`. Controladores en `Assets/_Project/Animation/`. Todos los personajes son **Humanoid** (Mixamo).

## Protagonista (`PlayerAnimation`, controlador `PlayerHumanoid.controller`)
- Capa 0 (cuerpo): **Free** (árbol por `Speed`: Idle / Pistol Walk / Run Forward) y **Aim** (árbol 2D `MoveX`/`MoveY` con pasos laterales, diagonales y hacia atrás). Muerte desde cualquier estado.
- Capa 1 (torso, máscara `UpperBodyHumanoid.mask`): Empty, AimRifle/AimPistol (según `LongGun`), ShootRifle/ShootPistol, ReloadPistol/ReloadRifle (`Reload`) y Hit (`Hit`). El peso de la capa pasa a 0 al morir.
- Parámetros: `Speed`, `MoveX`, `MoveY`, `Aiming`, `Armed`, `LongGun`, `Shoot`, `Reload`, `Hit`, `Dead`. Solo se envían los que el controlador tiene.
- **Alineación del arma** (`LateUpdate`): mientras apunta, gira el hueso `Spine` para que el eje del arma (el agarre) coincida con la dirección de la cámara en horizontal y vertical, filtrado (`Slerp`, ~14/s) para que no haya tirones. Incluye **retroceso por código** (4° pistola, 7° arma larga).
- Disparar no cambia de clip: los estados de disparo reutilizan el clip de apuntar. El clip *Firing Rifle* giraba el torso unos 34° y se veía un tirón.
- La duración de la recarga se ajusta al `reloadTime` del arma.

## Zombis (`ZombieAnimation`, controlador base `ZombieHumanoid.controller` + un override por tipo)
- Modo "gait": `Speed` 0 reposo / 1 andar / 2 correr, y la velocidad de reproducción se adapta a la velocidad real del agente (`walkClipSpeed`, `runClipSpeed`, `maxWalkPlayback`) para que no patine.
- Parámetros: `Speed`, `AttackVariant` (int, antes del trigger), `Attack`, `Alert`, `Hit`, `Dead`.
- Fase aleatoria al empezar para que no caminen sincronizados.

## Jefe (`Boss.controller`)
Baile (Gangnam Style) por defecto → Alert → Locomotion; tres ataques con tiempos de salida propios; muerte con la animación de muerte de zombi.

## Estructura de clips
`Assets/_Project/Art/Mixamo/{PlayerAnims, ZombieAnims, GenericAnims, BossAnims}`. Prefijos `P_`, `Z_`, `G_`, `B_`. Importación en `MixamoImport` (ver `editor-tools.md`).
