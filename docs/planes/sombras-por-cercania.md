# Plan: sombras de focos solo por cercanía

- **Fecha:** 2026-10-09
- **Estado:** Aprobado por el usuario (2026-10-09). Implementado; falta medir fps en la build
- **Autor:** Claude (Sonnet), a partir de la auditoría de rendimiento
- **Rama:** rediseno-comisaria

## Objetivo
Que la comisaría grande llegue como mínimo a **60 fps** (16,7 ms por fotograma) en el equipo del usuario. La auditoría mostró que las sombras de los focos de techo son el cuello de botella en la GPU.

## Estado actual (observado)
- Equipo del usuario: Intel Core Ultra 7 255HX, 32 GB de RAM, NVIDIA GeForce RTX 5070 Ti Laptop (12 GB). Resolución y pantalla: **DESCONOCIDAS** (el usuario no las ha dicho).
- Escena `Comisaria_v2`: 289 luces. 129 son focos (`Spot`) con sombra blanda; casi todos cuelgan de un `CeilingLamp` (`Assets/_Project/Scripts/Core/CeilingLamp.cs`). Hay además 2 puntuales exteriores con sombra blanda. No hay datos de occlusion culling horneados.
- Medición en el editor (GPU, 640×480, con `FrameTimingManager`, 76 muestras de media por caso):

| Escenario | Con sombras | Sin sombras de los 129 focos |
|---|---|---|
| Calderas | 19,1 ms (repetido 18,3) | 11,4 ms |
| Entrada | 17,2 ms (repetido 15,2) | 6,1 ms |
| Sala de espera | 14,9 ms | 7,3 ms |

- Es medición del editor, no de una build. En una build, y a la resolución real, los números cambiarán.
- `CeilingLamp.Apply` enciende y apaga la luz (parpadeo, apagones, interruptor) pero **no toca `Light.shadows`**, así que un script externo puede gestionarlas sin chocar.

## Decisiones tomadas
- Se hace el script de sombras por cercanía (lo pidió el usuario).
- El mínimo es 60 fps.

## Decisiones abiertas
1. **Número de focos con sombra a la vez (N).** Provisional: **6**. Se ajusta midiendo, empezando por 4, 6 y 8. Recomendación: el N más alto que siga cumpliendo 16,7 ms.
2. **Qué cuenta como «cercano».** Recomendación: distancia 3D al jugador, con histéresis para que no parpadeen las sombras en el límite.
3. **Contador de fps en la build.** Para medir fuera del editor hace falta uno. Recomendación: uno mínimo con una tecla (F3), apagado por defecto. Si el juego ya tiene uno, se usa ese. Esto es código nuevo, por eso se pregunta.
4. **Resolución y pantalla del usuario.** Hace falta para que la medición sea comparable.

## Diseño
Un `MonoBehaviour` nuevo, `ShadowBudget`, en `Assets/_Project/Scripts/Core/ShadowBudget.cs` (mismo nombre que el archivo).

- Se crea solo al cargar la escena (`RuntimeInitializeOnLoadMethod`) y no hace falta tocar la escena ni los kits de editor.
- Al arrancar recoge todas las `Light` con `shadows != None` que no sean direccionales y guarda su modo original.
- Cada 0,2 s (no en cada fotograma) ordena esas luces por distancia al jugador. Solo las **N más cercanas y que estén encendidas** conservan su modo original; el resto queda en `LightShadows.None`.
- Histéresis: una luz que ya tiene sombra no la pierde hasta que otra le gane por más de un margen (provisional 1 m). Así no hay parpadeo de sombras al caminar.
- No cambia intensidad ni color, solo `Light.shadows`. Las luces exteriores con sombra entran en el mismo reparto.

## Pasos de implementación (para el aplicador)
1. Comprobar `EditorApplication.isPlaying`; si es `True`, esperar a que el usuario salga.
2. Crear `ShadowBudget.cs` con N y el margen como campos públicos.
3. Compilar con `refresh_unity` (`compile: request`) y revisar la consola.
4. En Play, avanzando fotogramas con `Step`: comprobar que nunca hay más de N luces con sombra y que se reparten al mover al jugador.
5. Repetir las mediciones de la auditoría en entrada, calderas y sala de espera para N = 4, 6 y 8, en las mismas condiciones.
6. Si hace falta, añadir el contador de fps (decisión abierta 3).
7. Documentar: crear `docs/sombras.md` y una entrada en el registro del README.
8. Recompilar la build con `/recompile` (el usuario ya lo pidió) **cuando los pasos anteriores estén hechos**, para que la build lleve el cambio. Medir fps en la build en los mismos sitios.
9. Commit y push a `rediseno-comisaria`.

## Criterios de aceptación
- En el editor, con N elegido, las tres mediciones quedan por debajo de 16,7 ms de GPU. Esto es un requisito mínimo, no una garantía de que la build llegue a 60 fps.
- En la build, 60 fps o más en los cinco escenarios de la auditoría, en el equipo del usuario y a su resolución. Si no se llega, se reevalúa (menos N, menor calidad de sombra, o occlusion culling).
- Ningún foco cambia de sombra de forma visible al caminar (sin parpadeo).
- El ambiente de las salas lejanas no empeora de forma notoria. Esto lo valora el usuario jugando.

## Qué se puede probar solo y qué necesita al usuario jugando
- Solo (editor, Play avanzando fotogramas): el reparto de sombras, el tope N, los tiempos de GPU en el editor.
- Con el usuario jugando: si el ambiente se siente peor, si se ven sombras que aparecen y desaparecen, y los fps reales en la build.

## Riesgos y cosas que no tocar
- No ejecutar los menús viejos `Horror/Comisaria v2/…`.
- No compilar con Unity en Play.
- Las salas con pocos focos cercanos tendrán menos sombras de las que tenían: es lo que se acepta.
- Una luz que entra y sale del grupo puede notarse; por eso la histéresis.
- Las medidas del editor no son las de una build: la decisión final de N se toma con la build.
- Las builds del escritorio actuales son anteriores a la comisaría grande.

## Resultado (lo rellena quien ejecuta)
- Decisiones abiertas resueltas por el usuario: contador F3 sí; pantalla 1920×1080; N elegido **8** (entre 4, 6 y 8 no hubo diferencia medible).
- Hecho: `ShadowBudget.cs` y `FpsCounter.cs` en `Assets/_Project/Scripts/Core/`, compilados sin errores propios. Detalle y tabla de mediciones en `docs/sombras.md`.
- Probado de verdad (editor, Play avanzando con `Step`): el máximo de luces con sombra coincide con N en los nueve casos, y la GPU queda en 7,2 a 9,7 ms (antes 14,9 a 19,1 ms), a 640×480.
- Build: el skill `/recompile` apunta a `Desktop/Juego` y Build Settings solo lleva `Comisaria`, así que se construyó la **versión 2** a mano con `MainMenu` + `Comisaria_v2` en `Desktop/Sector7 - Version 2 (comisaria nueva)`. La versión 1 no se tocó.
- **No comprobado:** fps reales en la build a 1920×1080, ni si las sombras saltan de forma visible al caminar. Eso necesita al usuario jugando con F3.
