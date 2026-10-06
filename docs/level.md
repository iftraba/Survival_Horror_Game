# Level

Escena `Assets/Scenes/Comisaria.unity` (índice 1 de compilación). Es la **fuente de verdad** del nivel, editable a mano. Prefabs reutilizables en `Assets/_Project/Prefabs/{Doors, Interactables, Lighting, Characters}`.

## Estructura de la escena
`LEVEL` (geometría), `Items` (pickups), `Zombies`, jugador (instancia de `Player.prefab`), cámara, `GameFlow`, `RuntimeNavMesh`, luces y objetos de gestión.

## Zonas
- Planta baja de la comisaría: hall principal, despachos (capitán, oficina oeste), vestuario con taquillas (a oscuras), almacén, sala segura 1.
- **Zona del jefe** (`BossWing`): pasillo corto con dos puertas — izquierda sala segura 2 (terminal + baúl) y frente la sala final iluminada con el jefe y la salida (puerta cerrada hasta soltar la llave).

- **Planta superior** (`UpperFloor`): escalera recta de 20 peldaños (0.2 m de alto, 0.28 de huella, 1.4 m de ancho) pegada al muro este del hall, con espacio libre al pie; el forjado tiene un hueco sobre ella. Arriba, un pasillo central (z −7…−4) y seis salas: archivo (luz que parpadea), interrogatorio, descanso (catres), **despacho del jefe (con la llave de la sala)**, **sala segura 3 (teléfono + baúl)** y sala de reuniones. Suelo a y = 4, techo a y = 7.
- Para que el NavMesh cruce la escalera hay una **rampa invisible** bajo los peldaños (los peldaños de 20 cm solos no se conectan); el jugador sigue pisando los peldaños.

## Reparto de objetos (resumen)
Pistola al empezar; **la llave de la sala está en la planta superior**; escopeta en el escritorio del capitán; cartuchos y balas repartidos (almacén, escritorios, sala segura, taquilla de la arena); sprays; la llave de salida **la suelta el jefe**.

## Navegación
`RuntimeNavMesh` en `Start`, `useGeometry = PhysicsColliders`; los muebles no caminables llevan `NavMeshModifier`. Las puertas llevan `NavMeshObstacle` con carve.

## Luz
Lámparas de techo (`CeilingLamp`) con interruptores (`LightSwitch`): cada interruptor controla un grupo. La sala del jefe y las salas seguras tienen luz propia.

## Pendiente
Sonido de pasos distinto en la escalera, ventanas y azotea.
