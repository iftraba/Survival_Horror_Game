# Level

Escena `Assets/Scenes/Comisaria.unity` (índice 1 de compilación). Es la **fuente de verdad** del nivel, editable a mano. Prefabs reutilizables en `Assets/_Project/Prefabs/{Doors, Interactables, Lighting, Characters}`.

## Estructura de la escena
`LEVEL` (geometría), `Items` (pickups), `Zombies`, jugador (instancia de `Player.prefab`), cámara, `GameFlow`, `RuntimeNavMesh`, luces y objetos de gestión.

## Zonas
- Planta baja de la comisaría: hall principal, despachos (capitán, oficina oeste), vestuario con taquillas (a oscuras), almacén, sala segura 1.
- **Zona del jefe** (`BossWing`): pasillo corto con dos puertas — izquierda sala segura 2 (terminal + baúl) y frente la sala final iluminada con el jefe y la salida (puerta cerrada hasta soltar la llave).

- **Planta superior** (`UpperFloor`): escalera recta de 20 peldaños (0.2 m de alto, 0.28 de huella, 1.4 m de ancho) pegada al muro este del hall, con espacio libre al pie; el forjado tiene un hueco sobre ella. Arriba, un pasillo central (z −7…−4) y seis salas: archivo (luz que parpadea), interrogatorio, descanso (catres), **despacho del jefe (con la llave de la sala)**, **sala segura 3 (teléfono + baúl)** y sala de reuniones. Suelo a y = 4, techo a y = 7.
- **Tramo final tras el jefe** (`PostBossWing`, z 20 → 39): la puerta del norte de la arena (`Door_Exit`, antes la salida) ahora es una puerta con la llave de salida y da a un pasillo (z 20.5-25) y un vestíbulo (z 25-29). Al norte, tres salas: **sala segura 4** (oeste: teléfono, baúl, mesa con la *nota del operador* — pista del armario — y el *comunicado interno*), **sala de control** (centro: llave del garaje en la mesa central, taquilla con código **0316** con la **2ª riñonera**, 2 zombis, lámpara que parpadea) y **garaje** (este: el portón `GarageGate`, un `ExitDoor` que pide la *llave del garaje* y termina la partida; 1 zombi). 4 zombis `PB_*` en total (vestíbulo, control ×2, garaje). Las puertas de las salas son las normales (la de la sala segura no la fuerzan los zombis); las nuevas quedan al final del orden espacial de puertas, así que los guardados antiguos siguen valiendo.
- Para que el NavMesh cruce la escalera hay una **rampa invisible** bajo los peldaños (los peldaños de 20 cm solos no se conectan); el jugador sigue pisando los peldaños.

## Reparto de objetos (resumen)
Pistola al empezar; **la llave de la sala está en la planta superior**; escopeta en el escritorio del capitán; cartuchos y balas repartidos (almacén, escritorios, sala segura, taquilla de la arena); sprays; la llave de salida **la suelta el jefe**; la **llave del garaje** está en la sala de control (final); riñoneras: una en la taquilla de reuniones (arriba, código 4719) y otra en la sala de control (código 0316, pista en la sala segura 4).

## Navegación
`RuntimeNavMesh` en `Start`, `useGeometry = PhysicsColliders`; los muebles no caminables llevan `NavMeshModifier`. Las puertas llevan `NavMeshObstacle` con carve.

## Luz
Lámparas de techo (`CeilingLamp`) con interruptores (`LightSwitch`): cada interruptor controla un grupo. La sala del jefe y las salas seguras tienen luz propia. Una lámpara por posición: `UpperFloor.Build` retira las lámparas de la planta alta anteriores antes de crear las suyas (antes se apilaban 5 copias por sala y el centro salía quemado).

**Cómo está calibrada la luz (octubre 2026)**: los materiales `Env_*` (suelo, pared, techo, madera, metal) sacan la suavidad del canal alfa de la textura multiplicada por `_Smoothness`; con el deslizador a 1 el suelo era un espejo y cada lámpara dejaba un fogonazo. Ahora: suelo 0,4, pared 0,22, techo 0,2, madera 0,3, metal 0,55. El Bloom está en intensidad 0,3 (umbral 1). Cada lámpara tiene un foco con sombra (intensidad ~17-32) y un relleno puntual sin sombras **a 2,55 m** (cerca del techo, para no quemar el suelo bajo la lámpara) cuyo alcance se midió contra las paredes de su sala (1,4 × la distancia, entre 5 y 9 m; si es menor que la distancia a la pared, la pared queda a oscuras). Las lámparas de la arena del jefe se dejaron como estaban (referencia). Al añadir salas nuevas: foco ~25, relleno a 2,55 m de altura y alcance ≥ 1,4 × la distancia a la pared más lejana.

## Pendiente
Sonido de pasos distinto en la escalera, ventanas y azotea.
