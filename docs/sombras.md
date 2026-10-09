# Sombras por cercanía y contador de fps

Plan: `docs/planes/sombras-por-cercania.md`. Fecha: 2026-10-09.

## Por qué
La auditoría de rendimiento (editor, 640×480) dio la GPU como límite en la comisaría grande. Con las sombras blandas de los 129 focos de techo la GPU tardaba 14,9 a 19,1 ms (más de los 16,7 ms de 60 fps en dos de los tres sitios medidos). Sin esas sombras bajaba a 6,1 a 11,4 ms.

## `ShadowBudget` (`Assets/_Project/Scripts/Core/ShadowBudget.cs`)
- Se crea solo al cargar cada escena (`RuntimeInitializeOnLoadMethod` y `sceneLoaded`). No hay nada que poner en la escena.
- Recoge las luces no direccionales con sombra y guarda su modo original. Cada 0,2 s deja con sombra solo las **N más cercanas al jugador** que estén encendidas. El resto queda en `LightShadows.None`.
- Histéresis de 1 m: una luz con sombra no la cede hasta que otra le gana por más de ese margen.
- Solo toca `Light.shadows`. No afecta a intensidad, parpadeo ni interruptores de `CeilingLamp`.
- Campos públicos: `maxShadowed` (**8**), `hysteresis` (1 m), `interval` (0,2 s).
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

## Prueba en la build
- 2026-10-09: el usuario jugó la Version 2 a 1920×1080 en su equipo (Core Ultra 7 255HX, RTX 5070 Ti Laptop) y confirmó que los fps van bien. **No se anotaron cifras**, así que no consta el valor exacto ni por escenario.

## Qué no se ha comprobado
- Cifras exactas de fps en la build (solo la valoración del usuario).
- Si las salas lejanas se ven peor o si hay saltos visibles de sombra al caminar (lo valora quien juegue).
- Cuánto sube el coste a 1920×1080.

## Si hay que ajustarlo
Bajar `maxShadowed`, subir `interval`, o recurrir a occlusion culling por sala (no hay datos horneados).
