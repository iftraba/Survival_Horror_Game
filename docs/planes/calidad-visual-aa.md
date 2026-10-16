# Plan: calidad visual de nivel AA

Fecha: 2026-10-14 · Estado: **pendiente de aprobación** · Encargo del usuario: «necesito una mejora visual digna de un juego doble A; no importa si se tarda más».

## Diagnóstico honesto
Lo hecho hasta ahora (modelos por código con cajas biseladas y texturas procedurales, aunque se hornee de alta a baja) **no llega a AA y no va a llegar** por más tandas que se hagan. Lo que separa una escena AA de la nuestra, por orden de peso:

1. **La luz (lo que más pesa y menos se ha tocado).** Medido en la escena: 0 lightmaps, 0 sondas de luz, 0 sondas de reflexión. Toda la luz indirecta es un color plano (ambiente Flat 0,2/0,21/0,27); por eso las salas se ven planas y negras en las esquinas, y por eso hicieron falta 88 luces de relleno. Un juego AA con este estilo (RE2 remake, Alan Wake) tiene **iluminación global horneada** (rebotes de luz, el color de la pared tiñe el suelo), **sondas de reflexión por sala** (el metal y el suelo mojado reflejan la sala, no un cielo genérico), **SSAO** (ya está), **niebla volumétrica** en los haces de luz y **etalonaje de color** (LUT).
2. **Los materiales.** Las texturas procedurales se notan: repiten, son limpias, no tienen la «suciedad real» de una foto. AA usa **materiales escaneados de fotografías** (PBR con color, normales, rugosidad y altura reales) a una densidad de unos 512 píxeles por metro, más **calcomanías** (manchas, grietas, sangre, carteles) y **hojas de molduras** (trim sheets) para los bordes.
3. **Los modelos.** Formas reales (no cajas): perfiles con curvatura, juntas, tornillería, desgaste modelado, piezas con historia (abolladuras, piezas que faltan). Se hacen con modelado de superficie dura en Blender (subdivisión con aristas de apoyo, booleanos, normales ponderadas) y, para lo orgánico, esculpido; las normales y la oclusión se hornean a una malla de juego.

## Qué propongo (por orden de impacto por hora)

### Fase A. Luz y render (1-2 sesiones; afecta a TODO lo que ya existe)
- Iluminación global horneada (Lightmapping progresivo en GPU) de la estructura estática: paredes, suelos, techos, muebles grandes. Lámparas en modo *Mixed* (luz directa en tiempo real y sombras, rebote horneado).
- Sondas de luz en rejilla por sala (los zombis, el jugador y los objetos móviles reciben la luz horneada).
- Una sonda de reflexión por sala (horneada, con proyección de caja) y otra en la calle.
- Volumen de postproceso por zona: etalonaje (tono frío-verdoso, contraste en sombras, *lift* azul), viñeta, grano de película leve, aberración cromática mínima, profundidad de campo solo en las secuencias. Niebla de altura y haces de luz (falso volumétrico con conos aditivos bajo las lámparas y las farolas).
- Se retiran las 88 luces de relleno (el rebote horneado las sustituye).
- Medición de rendimiento antes y después (los lightmaps cuestan memoria, no CPU).

### Fase B. Biblioteca de materiales escaneados (1-2 sesiones)
- Sustituir los materiales de arquitectura (suelo de baldosa, pared de yeso pintado, techo de placas, hormigón, asfalto, acera, ladrillo de fachada, madera, chapa pintada, metal) por **materiales PBR escaneados de Poly Haven** (licencia CC0, gratis y libres para uso comercial). **Necesita tu permiso para descargar** (te diré archivo, origen y tamaño de cada uno antes de bajarlo).
- Mezcla de capas en el shader (suciedad en la base de las paredes, humedad, desgaste) y **calcomanías URP** (Decal Projector): manchas de agua, moho, grietas, sangre, huellas, pósters.

### Fase C. Modelos AA, por orden de lo que más se ve
Cada pieza: referencia real → modelo de alta en Blender con superficie dura de verdad (no cajas) → malla de juego → horneado de normales/AO/curvatura → texturas con los materiales escaneados de la fase B como base + desgaste por capas → revisión con captura en el motor junto a la versión actual → tu OK.
1. Puertas y marcos (150 instancias). **Hecho 2026-10-15.**
2. Taquilla, archivador, escritorio, silla giratoria (≈350 instancias). **Hecho 2026-10-15** (con materiales escaneados por submalla en vez de horneado, ver `docs/art-pipeline.md`; GI rehorneada el 2026-10-16).
3. Coche de policía (pieza estrella del exterior). Ya se rehízo en la v3 de `build_police_cars.py` (lavado de cara del exterior); se deja para el final por si se quiere pasar a materiales por submalla.
4. Mostrador del vestíbulo (**hecho 2026-10-16**), escalera de caracol, luminarias.
5. Resto de muebles por zonas.
Calculo 1-2 piezas por sesión a esta calidad.

### Alternativa más rápida para la fase C
Modelos ya hechos de calidad AA con licencia libre (Poly Haven tiene muebles, puertas y objetos escaneados CC0; Fab/Quixel tiene más, de pago o gratis según el caso). Los integro, reescalo y adapto. Requiere tu permiso de descarga y revisar licencias.

## Orden recomendado
**A → B → C.** La fase A es la que más cambia la imagen con menos trabajo y mejora también los modelos actuales; sin ella, los modelos AA de la fase C se seguirían viendo planos.

## Riesgos
- El horneado de luz tarda (minutos u horas según la calidad) y hay que repetirlo cada vez que se cambia la planta o un mueble estático; los menús 1 a 15 tendrán que lanzarse antes.
- Los lightmaps ocupan memoria y disco (cientos de MB a la calidad buena).
- Las descargas dependen de tu permiso y de la conexión.

## Fuera de alcance
Personajes y zombis (otra tarea), audio, cambios de planta o de jugabilidad.
