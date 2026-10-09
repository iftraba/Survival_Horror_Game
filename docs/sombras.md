# Sombras por cercanía y contador de fps

Plan: `docs/planes/sombras-por-cercania.md`. Fecha: 2026-10-09.

## Por qué
La auditoría de rendimiento (editor, 640×480) dio la GPU como límite en la comisaría grande. Con las sombras blandas de los 129 focos de techo la GPU tardaba 14,9 a 19,1 ms (más de los 16,7 ms de 60 fps en dos de los tres sitios medidos). Sin esas sombras bajaba a 6,1 a 11,4 ms.

## `ShadowBudget` (`Assets/_Project/Scripts/Core/ShadowBudget.cs`)
- Se crea solo al cargar cada escena (`RuntimeInitializeOnLoadMethod` y `sceneLoaded`). No hay nada que poner en la escena.
- Recoge las luces no direccionales con sombra y guarda su modo original. Cada 0,2 s deja con sombra solo **N luces** (las encendidas). El resto queda en `LightShadows.None`.
- **Reparto (desde 2026-10-10, fase F):** en vez de «las N más cercanas», las plazas se reparten en dos grupos, cada uno por distancia al jugador:
  - **A (4 plazas, `visibleSlots`):** focos que alcanzan al jugador con línea de visión (dan las sombras de los muebles de su sala).
  - **B (el resto, hasta N):** focos que lo alcanzan con una pared o un suelo en medio. Sin sombra, su luz atravesaría la pared.
  - Las plazas que sobran pasan al otro grupo y luego a los focos que no lo alcanzan.
  - «Alcanza» se mide con dos puntos, el suelo bajo el jugador (`probeOffset` −0,8) y la altura de la cabeza (`headOffset` +0,5), y cuenta si llega a cualquiera de los dos (rango y cono del foco). La visión se comprueba con un rayo (`Physics.RaycastNonAlloc`) que ignora al propio jugador y a cualquier cosa con `Health`.
- Histéresis de 1 m: una luz con sombra no la cede hasta que otra le gana por más de ese margen.
- Solo toca `Light.shadows`. No afecta a intensidad, parpadeo ni interruptores de `CeilingLamp`.
- Campos públicos: `maxShadowed` (**8**), `visibleSlots` (4), `hysteresis` (1 m), `interval` (0,2 s), `probeOffset`, `headOffset`.
- Al destruirse restaura las sombras originales.

## `FpsCounter` (`Assets/_Project/Scripts/Core/FpsCounter.cs`)
- **F3** lo muestra u oculta (apagado por defecto). Enseña fps medios, el peor fotograma de los últimos 0,5 s y cuántas luces tienen sombra. Se pone naranja por debajo de 60 fps.

## Mediciones (editor, GPU en ms, 640×480, unas 78 muestras por caso)
| Escenario | Antes (129 con sombra) | N = 4 | N = 6 | N = 8 |
|---|---|---|---|---|
| Calderas | 19,1 (repetido 18,3) | 9,0 | 9,2 | 9,0 |
| Entrada | 17,2 (repetido 15,2) | 7,2 | 7,3 | 8,0 |
| Sala de espera | 14,9 | 9,7 | 8,3 | 8,3 |

- Con el tope puesto, el máximo de luces con sombra contadas fue exactamente N en todos los casos.
- Entre N = 4, 6 y 8 la diferencia entra en el ruido de la medición, por eso se eligió 8 (más sombras, mismo coste medido).
- Son cifras del editor a baja resolución. **La medición real en build a 1920×1080 está pendiente** (con F3).

## Fugas de luz a través de paredes (fase F, 2026-10-10)
Con la regla de «las N más cercanas» había focos sin sombra cuya luz cruzaba paredes y suelos. Medido en Play con el jugador en el centro de cada una de las 61 salas y pasillos, contando focos sin sombra que alcanzan el punto con una pared o suelo en medio (misma medición, mismas posiciones, los 8 más cercanos frente al reparto nuevo):

| Punto de medida | Regla antigua (8 más cercanos) | Regla nueva (4 + 4) |
|---|---|---|
| Suelo bajo el jugador | 25 | 0 |
| Altura de la cabeza | 42 | 0 |
| 2,25 m (sobre la cabeza) | 62 | 23 |

- Nunca hubo más de 8 luces con sombra a la vez.
- Prueba A/B de coste en la misma sesión (presupuesto activo y desactivado, sala de espera): unos **0,5 ms más de CPU principal** con el presupuesto activo; la deriva entre pasadas fue mayor que esa diferencia.
- **Sin explicar:** las medidas de GPU y CPU del editor de ese día salieron más altas que las del 2026-10-09 (por ejemplo CPU principal 16–23 ms frente a 4,4–4,8 ms) incluso con el presupuesto desactivado. No se encontró la causa, así que **esas cifras no se pueden comparar con la tabla anterior** y no se ha vuelto a medir fps en la build.
- Las fugas exteriores (luces de relleno) se tratan aparte, en `ComisariaGrandeLightFix`: ver `docs/rediseno-comisaria.md`, fase F.

## Prueba en la build
- 2026-10-09: el usuario jugó la Version 2 a 1920×1080 en su equipo (Core Ultra 7 255HX, RTX 5070 Ti Laptop) y confirmó que los fps van bien. **No se anotaron cifras**, así que no consta el valor exacto ni por escenario. Esa build lleva la regla antigua de `ShadowBudget` (los 8 más cercanos); el reparto 4 + 4 de la fase F **solo llegará a una build con `/recompile`** y nadie ha probado sus fps fuera del editor.

## Qué no se ha comprobado
- Cifras exactas de fps en la build (solo la valoración del usuario).
- Si las salas lejanas se ven peor o si hay saltos visibles de sombra al caminar (lo valora quien juegue).
- Cuánto sube el coste a 1920×1080.

## Si hay que ajustarlo
Bajar `maxShadowed`, subir `interval`, o recurrir a occlusion culling por sala (no hay datos horneados).
