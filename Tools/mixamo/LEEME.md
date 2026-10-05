# Mixamo: rig y animaciones

Mixamo (https://www.mixamo.com, cuenta de Adobe gratuita) pone un esqueleto humanoide a cada personaje y da
animaciones de captura de movimiento. En Unity se usan como **Humanoid**: cualquier animación sirve para cualquier
personaje, así que las animaciones se descargan una sola vez.

## 1. Personajes (uno por uno)

Para cada archivo de `upload/`:

1. En Mixamo: **Upload Character** → arrastra el `.fbx`.
2. Coloca los marcadores (barbilla, muñecas, codos, rodillas, ingle) y pulsa **Next**. Esqueleto: **Standard (65)**.
3. Cuando aparezca el personaje, pulsa **Download** con:
   - Format: **FBX for Unity (.fbx)**
   - Pose: **T-pose**
4. Guárdalo en `download/personajes/` con el mismo nombre (por ejemplo `Zombie_Civil.fbx`).

El protagonista aparecerá en `upload/` cuando termine de generarse.

## 2. Animaciones de zombi

Con cualquier zombi cargado en Mixamo, busca cada una y pulsa **Download** con:
- Format: **FBX for Unity (.fbx)**, Skin: **Without Skin**, Frames per second: **30**, Keyframe Reduction: **none**
- Si aparece la casilla **In Place**, márcala.

Guárdalas en `download/animaciones_zombi/`. No hace falta renombrarlas.

| Buscar en Mixamo | Para qué |
|---|---|
| Zombie Idle | reposo |
| Zombie Scratch Idle | reposo alternativo |
| Zombie Walk (2 o 3 variantes distintas) | andar (una por tipo de zombi) |
| Zombie Running | persecución rápida |
| Zombie Crawl | zombi arrastrándose |
| Zombie Attack | ataque |
| Zombie Neck Bite | agarre y mordisco |
| Zombie Punching | golpe |
| Zombie Reaction Hit | recibe un disparo |
| Zombie Dying | muerte |
| Zombie Death | muerte alternativa |
| Zombie Stand Up | levantarse del suelo |
| Zombie Scream | grito al detectarte |

## 3. Animaciones del protagonista

Mismas opciones de descarga (**Without Skin**, 30 fps, **In Place** cuando exista). Se pueden bajar con
cualquier personaje, incluso con el **Y Bot** que Mixamo trae por defecto. Guárdalas en
`download/animaciones_jugador/`.

| Buscar en Mixamo | Para qué |
|---|---|
| Breathing Idle | reposo sin arma |
| Walking | andar |
| Running | correr |
| Walking Backwards | andar hacia atrás |
| Left Strafe Walking / Right Strafe Walking | andar de lado |
| Left Turn 90 / Right Turn 90 | girar en el sitio |
| Pistol Idle | reposo con pistola |
| Pistol Walk | andar con pistola |
| Pistol Run | correr con pistola |
| Pistol Walk Backward | atrás con pistola |
| Pistol Strafe (izquierda y derecha) | de lado con pistola |
| Pistol Aim (o "Pistol Idle" apuntando) | apuntar con pistola |
| Shooting (pistola) | disparar pistola |
| Reloading (pistola) | recargar pistola |
| Rifle Idle | reposo con escopeta |
| Rifle Aiming Idle | apuntar con escopeta |
| Rifle Walk | andar con escopeta |
| Rifle Run | correr con escopeta |
| Firing Rifle | disparar escopeta |
| Reloading (rifle) | recargar escopeta |
| Hit Reaction | recibe daño |
| Dying | muerte |

Si alguna no existe con ese nombre exacto, coge la más parecida: las identifico al importarlas.

## 4. Avísame

Cuando estén los archivos en `download/`, los importo en Unity como Humanoid, monto los controladores de
animación (mezclas de andar en 8 direcciones, apuntar por capas, IK de manos y pies) y sustituyo las
animaciones actuales.
