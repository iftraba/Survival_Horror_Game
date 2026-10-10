# Rendimiento de la comisaría grande

Mediciones en el **editor** (no en build) con una vista de juego de 1469×719; equipo del usuario: Core Ultra 7 255HX, RTX 5070 Ti Laptop, 32 GB; objetivo 60 fps a 1080p. Los tiempos son de `FrameTimingManager` (CPU y GPU por fotograma) y las llamadas de dibujo de `UnityStats`, avanzando fotogramas con `EditorApplication.Step()` (el editor limita el bucle a ~10 Hz con el MCP, así que no se mide fps reales).

## Auditoría del 2026-10-10 (tras las etapas F y G, 252 lámparas, 55 enemigos)
- **Recuento:** 5.948 renderers (4.149 estáticos, 1.799 no estáticos: 100 puertas × 8 piezas y 252 lámparas × carcasa y bombilla), 131 materiales, 423 luces (261 focos, 161 puntuales, 117 rellenos; 8 con sombra a la vez por `ShadowBudget`), 1.937 colliders (1 de malla), 55 `NavMeshAgent`/`Animator` (`CullUpdateTransforms`), 1,12 M de triángulos skinned en total (el reptante tiene 50.000). URP: SRP Batcher, máx. 8 luces adicionales, sombras adicionales a 4096, MSAA 4×.
- **Cuello de botella medido: CPU por llamadas de dibujo.** Sin oclusión horneada, el vestíbulo hacía ~7.500 llamadas y 21 ms de CPU incluso sin sombras ni zombis (6.700 / 17 ms). Quitar los 43 zombis bajaba 4-5 ms de CPU (agentes e IA), las sombras 3-4 ms y los rellenos casi nada.

## Occlusion culling (menú 8, `ComisariaGrandeOcclusion`)
Horneado con los ajustes por defecto (datos de ~195 KB en `Assets/Scenes/Comisaria_v2/`). Paredes, suelos y techos son occluders; **las estanterías, taquillas, archivadores y bibliotecas no** (son porosas: con ellas como occluders desaparecían cajas de dentro de las estanterías del archivo). Se marca en `ComisariaGrandeProps.Place`. Hay que repetir el horneado tras rehacer el nivel (menús 1 a 6).

| Escenario | Llamadas antes → después | CPU (ms) antes → después | GPU (ms) antes → después |
|---|---|---|---|
| a) vestíbulo | 7.544 → 3.014 | 21,1 → 13,1 | 13,9 → 6,6 |
| b) galería de servicio del sótano | 3.420 → 698 | 21,5 → 12,0 | 13,4 → 2,5 |
| c) archivo, jefe 1 despierto | 1.844 → 1.034 | 14,1 → 11,0 | 5,1 → 3,1 |
| d) calderas, jefe 2 despierto | 6.430 → 1.760 | 19,6 → 10,9 | 13,8 → 2,7 |
| e) planta baja, 23 enemigos despiertos | 3.795 → 1.714 | 15,2 → 11,0 | 5,7 → 2,7 |
| f) atrio y galería | 5.968 → 2.327 | 17,9 → 13,6 | 8,9 → 3,1 |

Mismas posiciones, orientación y condiciones antes y después (mediana de 60 fotogramas tras 40 de calentamiento). Con el techo de 16,7 ms para 60 fps, los seis quedan por debajo en CPU; antes tres pasaban de 19 ms.

**Corrección visual** (cámara temporal, 1.272 vistas = 6 posiciones × 4 direcciones por sala, con y sin oclusión, píxeles distintos >0,2 %): 3 vistas con diferencias, la peor 0,42 % de la imagen (escalera del archivo). Con las estanterías como occluders eran 4 vistas y hasta 1,21 % (cajas de las baldas que desaparecían). Queda **sin ver jugando** si hay parpadeos al girar la cámara en los huecos de la galería y del caracol.

## Pendiente
- Fps reales en una build a 1080p con F3 (hace falta `/recompile`, que solo lo pide el usuario).
- Dormir la IA de los zombis lejanos o dormidos (4-5 ms de CPU en el vestíbulo): sin hacer.
- Puertas como `OcclusionPortal` (una puerta cerrada hoy no tapa nada) y combinar las piezas de las puertas (800 dibujos no estáticos): sin hacer, sin medir.
- Sin línea base anterior a la etapa F (110 lámparas).
