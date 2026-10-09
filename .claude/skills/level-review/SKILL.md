---
name: level-review
description: Revisa un nivel de Sector 7 (ahora la comisaría grande, escena Comisaria_v2) con los criterios del juego - recorrido sin bloqueos, reparto de zombis y recursos, arenas de jefe, salas seguras, luz y puzles resolubles. Úsalo para el pase de equilibrio (fase F) o cuando se cambie la distribución de salas, puzles, zombis o botín. Solo informa; no cambia la escena.
argument-hint: "[escena, planta o sala]"
---

# Revisión de nivel

Es una revisión **de solo lectura**: no cambies la escena. Devuelve un informe y, si el usuario lo aprueba, el cambio lo hace el agente `aplicador`.

## 0. Antes de empezar
- Lee `CLAUDE.md`, `docs/resumen-sesion-comisaria.md` y `docs/rediseno-comisaria.md` (cadena de puzles y fases).
- Comprueba `EditorApplication.isPlaying`. Las pruebas por código se hacen con el editor parado, o en Play avanzando fotogramas a mano (ver `CLAUDE.md`).
- Separa lo **observado** (comprobado con una herramienta) de lo **opinión**. No digas que algo está equilibrado sin que el usuario lo haya jugado: los valores son provisionales.

## 1. Recorrido y bloqueos (lo más importante)
- **Alcanzabilidad:** con las puertas abiertas (desactiva los `NavMeshObstacle`), calcula una ruta de NavMesh desde la entrada a un punto dentro de cada sala (`ComisariaGrande.Rooms`). Toda sala debe ser alcanzable.
- **Sin atascos (softlock):** para cada puerta o paso cerrado, comprueba que la llave o pieza que lo abre está en una zona alcanzable ANTES de necesitarla. Orden esperado: cizalla → tarjeta de seguridad → escalera de incendios y tarjeta del jefe → tres medallones → reja del archivo → jefe 1 → llave del ascensor → fusibles → puerta de las calderas → jefe 2 → llave maestra → portón.
- **Pistas:** cada código (4519, 0832, 7258) debe tener su nota alcanzable antes de la taquilla.
- **Puertas atrancadas** durante un jefe: ¿se desatrancan al morir? ¿puede quedarse el jugador sin salida o sin munición dentro?
- **Guardado:** en una partida cargada, ¿se conservan medallones, fusibles, reja y ascensor (`Progress`)?

## 2. Encuentros y ritmo
- Cuenta los zombis por sala y planta y cuántos están en letargo. Busca tramos largos sin nada (aburrido) y tramos con demasiados seguidos (injusto).
- Comprueba que haya emboscadas con intención (zombi tras la puerta, reptante bajo una mesa) y no solo un zombi suelto por sala.
- Evita zombis ya despiertos pegados a una sala segura o a un punto de guardado.

## 3. Recursos
- Suma las balas de pistola y los cartuchos disponibles hasta cada jefe (sueltos y en taquillas) y compáralos con los zombis y con la vida de los jefes. Da el cálculo, marcado como provisional.
- Las curas (sprays) deben estar repartidas, con más antes de un jefe.
- La escopeta debe llegar antes del jefe 1.

## 4. Salas seguras y jefes
- Una sala segura (terminal o teléfono, baúl) antes de cada jefe, y su distancia.
- **Arena del jefe:** que no se pueda huir dando vueltas sin parar (hay islas o cobertura) y que no quede un hueco donde el jugador cabe y el jefe no. El jefe debe poder romper los muebles `_Props` y no las estanterías de obra.

## 5. Luz y legibilidad
- Estilo buscado: penumbra tipo Resident Evil 2, no negro total ni salas quemadas. Mira capturas de cada tipo de sala.
- Una sala oscura por diseño (sala de pruebas) debe tener su interruptor cerca de la puerta.
- Busca fugas de luz por fuera del edificio (el archivo las tenía) y pasillos sin luz.

## 6. Informe
Devuelve:
1. **Bloqueos y errores** (con prueba y sala).
2. **Problemas de ritmo y equilibrio**, con el cálculo.
3. **Luz.**
4. **Lo que está bien.**
5. **Lo que no se pudo comprobar** y necesita al usuario jugando.
6. **Cambios propuestos**, por orden de importancia y con el menú o archivo a tocar.
