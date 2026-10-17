# Plan: animaciones más fluidas, jefes y guardado de zombis y puertas

- **Fecha:** 2026-10-17
- **Estado:** Aprobado por el usuario (2026-10-17); fase 1 hecha
- **Autor:** Opus (sesión principal)
- **Rama:** modelos_3d

## Objetivo
El usuario ve las animaciones escasas y poco fluidas. Pide: una animación real para la pistola, más animaciones y más fluidas para los zombis, arreglar la de abrir puerta (en la sala del jefe del archivo el personaje se teletransporta), más animaciones y más ataques para los jefes (la embestida del jefe del archivo «parece una piedra moviéndose»), que ese jefe rompa todas las estanterías y no solo los objetos pequeños, y que al cargar partida no revivan los zombis ni se cierren las puertas de tarjeta. El modelo de la chica era solo una prueba: no se trabaja más en él.

## Estado actual (observado en el código, sin Unity: el MCP de Unity no conecta ahora mismo)
- **Guardado (causa encontrada):** `SaveSystem.ApplyPending` busca los zombis por nombre con `d.zombies.ToDictionary(z => z.name)`. En la verja hay nombres repetidos (`Verja_Zombie_Cop`, `_Civil`, `_Girl` y `_Oficial`, dos de cada uno; `ComisariaGrandeExterior.cs:180`), y también pueden repetirse `Z_Reptante_<sala>` y `Z_Carronero_<sala>` (`ComisariaGrandeEnemies.cs:131,136`). Con un nombre repetido, `ToDictionary` lanza una excepción y la carga se corta justo antes de quitar los zombis muertos y de aplicar las puertas. Por eso aparecen todos vivos y con las puertas de tarjeta cerradas. Además, las taquillas, el baúl y los interruptores van detrás y tampoco se aplicarían. No se ha reproducido en Play: es la causa que explica exactamente lo que pasa.
- **Pistola:** `PlayerKit.BuildController` apunta con `P_PistolIdle`, pero **no hay clip de disparo de pistola** (`P_PistolFire`/`P_Shooting` no existen): al disparar se mantiene la pose y el retroceso es por código. La recarga usa `P_Reloading` (de rifle o genérica). No hay desenfundar ni enfundar pistola.
- **Zombis:** un solo andar (`Z_ZombieWalk`), una carrera y un reposo para los cuatro tipos normales (`ZombieKit.Kinds`). Hay 5 ataques, golpe, golpe en la cabeza, aturdido, levantarse, agarre y 4 muertes. Sin giros en el sitio ni variantes de andar por zombi.
- **Jefe del archivo:** `BossRush` (embestida) desactiva el NavMeshAgent y mueve el transform; la animación depende de `ZombieAnimation.speedOverride = 7,5`. Sin Unity no se puede ver por qué queda rígido (hipótesis: el árbol de locomoción del jefe no llega a esa velocidad o el clip `B_MutantRun` no reproduce bien). Ataques: los del controlador del jefe más `BossLeap`.
- **Estanterías:** `PropBreaker.PropRoot` solo rompe lo que cuelga de `Props`/`Mobiliario`/`*_Props` y mide menos de 3,2 × 2,9 m; las estanterías de obra y las altas del archivo quedan fuera a propósito (eran cobertura y paredes de la arena).
- **Abrir puerta del jefe:** `PlayerActions` (entrar en la sala del jefe) hace avanzar al personaje según la cadera del clip; el salto se ve al terminar la acción. No reproducido.

## Decisiones tomadas
- La chica se queda como está (menú oculto) y no se trabaja más en ella.

## Decisiones abiertas (con mi recomendación)
1. **¿Quitar la chica y el menú oculto del juego?** Recomiendo dejarlos: no molestan y no se ven sin el truco de F3.
2. **Animaciones nuevas de Mixamo:** las descargas tú (o yo, con tu sesión de Mixamo abierta en el navegador y tu OK por lote). Lista propuesta abajo.
3. **Arena del jefe del archivo sin estanterías:** si rompe todas, la sala se queda diáfana a media pelea. Recomiendo que las rompa y que deje restos que sigan tapando un poco (cobertura baja).

## Diseño y pasos

### Fase 1: guardado (rápido, lo primero)
1. `SaveSystem`: identificar los zombis por un id único y estable (nombre + posición inicial redondeada, o un `SaveId` en un componente que los kits rellenan), y no usar `ToDictionary` con nombres que pueden repetirse.
2. Los kits de zombis (`ComisariaGrandeEnemies`, `ComisariaGrandeExterior`) dan nombres únicos (`Verja_Zombie_Cop_5`…).
3. Cada bloque de `ApplyPending` va protegido, para que un fallo en uno no deje el resto sin aplicar, y el error sale en la consola.
4. Compatibilidad: los guardados viejos siguen cargando (por nombre si no hay id).
5. **Prueba:** en Play, matar zombis (incluidos dos de la verja con el mismo tipo), abrir una puerta de tarjeta, guardar, cargar, y comprobar por código y con captura que siguen muertos y la puerta abierta.

### Fase 2: pistola
- Clips de Mixamo (pack de pistola): *Pistol Idle*, *Shooting* (pistola, de pie), *Pistol Walk* / *Pistol Strafe* izquierda y derecha / *Pistol Walk Backward*, *Pistol Run*, *Reloading* (de pistola), *Draw Pistol* (desenfundar) y *Holster Pistol* (enfundar).
- `PlayerKit`: estado de disparo con el clip real en la capa del torso (mezcla corta, 0,05 s), recarga de pistola, desenfundar y enfundar al cambiar de arma, y locomoción apuntando con los pasos de pistola.

### Fase 3: fluidez general (jugador y zombis)
- Revisar las transiciones (duraciones, sin cortes), arranques y paradas (*Run To Stop*, ya hay clips), y giros en el sitio para jugador y zombis (*Left/Right Turn 90*).
- Zombis: 2 o 3 andares distintos repartidos por tipo y con desfase aleatorio, giros en el sitio, *Zombie Agonizing* / *Zombie Transition*, y más reacciones a golpes (*Zombie Reaction Hit* variantes). Clips de Mixamo: *Zombie Walk* (variantes), *Zombie Turn*, *Zombie Running* (otra), *Zombie Stand Up*, *Zombie Reaction Hit*, *Zombie Agonizing*.
- Ajustar la velocidad de los clips a la del NavMeshAgent para que no patinen.

### Fase 4: jefes
- **Embestida:** ver en Play por qué queda rígida y arreglarla: clip de carrera de mutante a la velocidad real, preparación (*Mutant Roaring* o *Standing Taunt*), golpe final o choque y aturdido con su animación.
- **Más ataques para el jefe del archivo:** zarpazo (*Mutant Swiping*), combo de puñetazos (*Punch To Elbow Combo*, ya descargado), golpe al suelo con onda (*Mutant Jump Attack* o *Ground Slam*) y agarre o lanzamiento de muebles si encaja. Según la distancia, la vida y el tiempo, con pausas para dispararle.
- **Romper todas las estanterías:** `PropBreaker` acepta las estanterías del archivo (grupo propio o marca) con la embestida y el salto; deja restos grandes que hacen de cobertura baja. Se recalcula el NavMesh, como ya hace.
- Segundo jefe (calderas): las mismas mejoras de transición y un ataque más si da tiempo.

### Fase 5: abrir puertas
- Reproducir en Play la puerta del jefe del archivo y una normal, y medir dónde salta el personaje.
- Arreglo previsto: mover el CharacterController de forma continua con el avance real del clip (en vez de colocarlo al final) y casar el sitio de partida con la posición del pomo. Mezcla de entrada y salida suave.

## Criterios de aceptación
- Guardar con zombis muertos y puertas abiertas, y cargar: siguen muertos y abiertas (comprobado en Play).
- La pistola dispara, recarga y se desenfunda con clips reales.
- Los zombis tienen al menos 2 andares distintos y giran sin deslizarse.
- La embestida del jefe se ve corriendo, y rompe todas las estanterías del archivo.
- Al entrar en la sala del jefe no hay salto visible (comprobado midiendo la posición fotograma a fotograma).

## Qué se puede probar solo y qué necesita al usuario jugando
- **Solo:** guardado y carga, mediciones de posición en las puertas, capturas de las animaciones, rotura de estanterías.
- **Contigo jugando:** si la fluidez y el ritmo de los jefes se sienten bien.

## Riesgos y cosas que no tocar
- No compilar con Unity en Play. **Ahora el MCP de Unity no conecta:** hace falta abrir Unity (y `Tools/run_mcp_server.bat` si no arranca solo) para empezar.
- Las descargas de Mixamo dependen de tu cuenta; los nombres de clip pueden variar, y se ajustan a lo que haya.
- Cambiar los ids de los zombis no debe romper los guardados que ya tienes (compatibilidad del paso 4).
- Más clips y transiciones: medir después el coste de los animadores (hay unos 45 zombis).

## Resultado (lo rellena quien ejecuta)
**Fase 1 (guardado), hecha el 2026-10-17.** `ZombieAI.SaveId` (nombre + posición de partida) y `ZombieSave.id`; la carga empareja por id (y por nombre, uno a uno, en los guardados antiguos) y cada parte va protegida con `Try`. No se cambiaron los nombres en los kits: el id ya los distingue.
- Comprobado en Play: partida nueva, maté 5 zombis (los dos `Verja_Zombie_Cop` y 3 de dentro) y abrí 3 puertas de tarjeta, guardé y cargué: 0 muertos de vuelta (50 en escena), las 3 puertas abiertas y desbloqueadas, consola sin errores.
- Los 4 guardados del usuario de la comisaría grande tenían los nombres de la verja repetidos (la causa confirmada). El hueco 1 (formato antiguo) carga ahora con 48 zombis, 35 puertas abiertas y el jugador en su sitio, igual que el archivo.
- Error en la prueba: se sobrescribió el hueco 5 de la comisaría grande (`savegame_v2_5.json`); la copia se hizo de la ruta de la comisaría vieja.
