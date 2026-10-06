# Level

Escena `Assets/Scenes/Comisaria.unity` (índice 1 de compilación). Es la **fuente de verdad** del nivel, editable a mano. Prefabs reutilizables en `Assets/_Project/Prefabs/{Doors, Interactables, Lighting, Characters}`.

## Estructura de la escena
`LEVEL` (geometría), `Items` (pickups), `Zombies`, jugador (instancia de `Player.prefab`), cámara, `GameFlow`, `RuntimeNavMesh`, luces y objetos de gestión.

## Zonas
- Planta baja de la comisaría: hall principal, despachos (capitán, oficina oeste), vestuario con taquillas (a oscuras), almacén, sala segura 1.
- **Zona del jefe** (`BossWing`): pasillo corto con dos puertas — izquierda sala segura 2 (terminal + baúl) y frente la sala final iluminada con el jefe y la salida (puerta cerrada hasta soltar la llave).

## Reparto de objetos (resumen)
Pistola al empezar; escopeta en el escritorio del capitán; cartuchos y balas repartidos (almacén, escritorios, sala segura, taquilla de la arena); sprays; la llave de salida **la suelta el jefe**.

## Navegación
`RuntimeNavMesh` en `Start`, `useGeometry = PhysicsColliders`; los muebles no caminables llevan `NavMeshModifier`. Las puertas llevan `NavMeshObstacle` con carve.

## Luz
Lámparas de techo (`CeilingLamp`) con interruptores (`LightSwitch`): cada interruptor controla un grupo. La sala del jefe y las salas seguras tienen luz propia.

## Pendiente
Segunda planta con escaleras (planificada).
