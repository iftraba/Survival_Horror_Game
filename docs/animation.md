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

## Animaciones nuevas (2026-10-08, menú *Horror/Animaciones nuevas (puertas, derribos, muertes)*, `AnimPackKit`)
14 clips de Mixamo (humanoides, valen para todos los modelos). Los que giran o avanzan dejan la raíz fuera del clip (`MixamoImport.ConfigureAnimation`, `bakeRotation`) y el giro/avance lo aplica el código, así el cuerpo no gira dos veces.
- **Jugador** (`PlayerKit.BuildController`, estados de la capa base que lanza `PlayerActions` con `PlayerAnimation.Trigger`): `OpenDoor` (*Opening*, solo la primera de sus dos aperturas, a 1,8×; se corta si echa a andar), `EnterDoor` (*Opening Door Inwards*, 1,25×), `RunTurn` (*Running To Turn*, el giro de 180° lo hace el código), `Roll` (*Falling To Roll*, empieza en el 23 %: al tocar el suelo) y `RunStairs` (*Running Up Stairs*, con el bool `Stairs`). Durante estas acciones la capa del torso se apaga.
- **Zombis** (`ZombieKit.AddReactions` sobre `ZombieHumanoid.controller`): `HeadHit` (*Head Hit*), `Stun` (*Kick To The Groin (1)*, se dobla y se recupera), `KnockFall` → `Situp` (la caída de espaldas del pack Pxltiger acaba boca arriba como empieza *Situp To Idle*), `Downed` (muere en el suelo: se queda tumbado), `Grab` (*Zombie Neck Bite*, bool) y tres muertes más por `DeathVariant`: 1 *Kick To The Groin* (último disparo al torso/vientre), 2 *Dying*, 3 *Zombie Stumbling*; 0 = la de cada tipo. `ZombieAnimation.PickDeath` elige: torso → 1; si no, al azar entre 0, 2 y 3; los reptantes siempre 0.
- **Pxltiger**: segundo ataque con *Zombie Punching* (variante 1). **Reptantes** (`Zombie_OficialReptante`, `Zombie_Carronero_Reptante`): andan con *Crawling*.
- **Reptantes que se hundían**: los clips de arrastrarse dejan el cuerpo hasta 30 cm bajo la raíz (la cabeza se metía en el suelo y no se le podía dar). `ZombieAnimation.LateUpdate` mide los huesos tras animar y eleva el modelo lo justo (comprobado: cabeza a 45 cm del suelo). El `heightOffset` del importador no sirve con clips humanoides.

## Packs del jugador (2026-10-08)
90 clips en `PlayerAnims` con prefijo: `R_` (Pro Rifle Pack), `A_` (Action Adventure Pack), `L_` (sueltos de rifle y coberturas). `PlayerKit.BuildMovementPack` añade a la capa base `Crouch` (árbol 2D), `JumpUp`/`Airborne`/`Land`/`HardLand`, coberturas (`CoverEnter1/2`, `CoverStand`/`CoverCrouch` con el parámetro `CoverMove`, salidas), `Fidget0-3` y `Death1-4` (`DeathVariant`); a la capa del torso, `DrawLong`/`HolsterLong`. `PlayerKit.LongGunOverride` crea `PlayerHumanoid_Long`. No se usan (de momento): esprintar, giros de 90°, carreras laterales y diagonales del pack de rifle, *Run To Stop*, *Firing Rifle* (el retroceso sigue siendo por código), cambiar de cobertura y girar en ella.

## Estructura de clips
`Assets/_Project/Art/Mixamo/{PlayerAnims, ZombieAnims, GenericAnims, BossAnims}`. Prefijos `P_`, `Z_`, `G_`, `B_`. Importación en `MixamoImport` (ver `editor-tools.md`).
