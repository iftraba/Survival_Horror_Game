# Plan: lavado de cara del interior (calidad de los modelos 3D)

Fecha: 2026-10-14 · Estado: **pendiente de aprobación** · Se hace con Blender a la vista (puente MCP) y sin añadir objetos nuevos.

## Qué se ve hoy (auditoría con capturas de vestíbulo, pasillos y salas)
- Muebles hechos con cajas biseladas (taquilla 492 tris, archivador 756, escritorio 600, silla 264, silla giratoria 852). Formas rectas y sin detalles pequeños: tiradores, juntas, cerraduras, patas con perfil, cantos redondeados, roces.
- Puertas: hoja de madera clara con ventanilla cuadrada negra y marco liso; el color naranja no pega con el resto de la paleta gris verdosa.
- Paredes: textura de hormigón con manchas, pero planas (sin zócalo con relieve, sin cenefas, sin juntas de panel). Suelo de baldosa azul-gris repetida. Techo de placas con manchas.
- Luminarias: cajas planas con una placa emisiva.
- Mostrador del vestíbulo y escalera de caracol: lo mejor, pero aun así con perfiles rectos.

## Qué se cambia, por orden de impacto (nº de instancias × lo que se ve)
1. **Materiales de arquitectura** (paredes, suelo, techo, zócalo): texturas con mejor resolución y mapas de normales y rugosidad; tres variantes de tono por zona (vestíbulo, oficinas, sótano) para romper la repetición. Es lo que ocupa más pantalla.
2. **Puertas** (50 hojas + 100 marcos): cuarterones con perfil, ventanilla con junquillo y cristal con reflejo, manivela y placa de latón, marco con moldura, colores ajustados a la paleta, variantes (despacho, servicio, calabozo, metal).
3. **Taquillas (109), archivadores (94), escritorios (88)**: tiradores, rejillas de ventilación, juntas, etiquetas, cantos redondeados, patas, cajones con tiradores y cerraduras, pintura con desgaste.
4. **Sillas** (Chair 50, giratoria 52, auditorio 63): asientos acolchados con costuras, ruedas y pistón en las giratorias, respaldos con curvatura.
5. **Luminarias de techo**: difusor con bordes, tornillos y tubos visibles, variante rota y variante de emergencia.
6. **Muebles únicos y de zona** (mostrador, estanterías de archivo, calderas, máquinas del sótano, sofá, máquinas expendedoras, camas de calabozo): rehechos según se vean en capturas.
7. **Detalles de pared** (extintores, relojes, tablones, radiadores): ya existen y se revisan.

## Cómo se trabaja
- Cada pieza: se rehace su script en `Tools/blender/build_*.py`, se ve en tu Blender, se exporta, se hornea con `texture_items.py` y se sustituye en el FBX (las instancias de la escena se actualizan solas: son prefabs del mismo FBX). Sin tocar posiciones ni puzles.
- Mismas dimensiones que el modelo actual (colisiones y anclas de los menús 3 a 7 siguen valiendo). Si alguna cambia, se avisa.
- Presupuesto de triángulos: hasta ~3× el actual por pieza repetida (taquilla ~1.500, silla ~1.200) y ~10× por piezas únicas. Se medirá con `performance-audit` antes de dar por buena cada tanda.
- Una captura antes y otra después de cada pieza; tu OK al final de cada tanda.

## Tandas propuestas
- **T1**: materiales de arquitectura + puertas.
- **T2**: taquillas, archivadores, escritorios.
- **T3**: sillas y luminarias.
- **T4**: mostrador, escalera, sofás, máquinas, estanterías y piezas únicas; revisión de detalles de pared.
- Al terminar: rebakeo de oclusión (menú 8), auditoría de rendimiento y recompilar si lo pides.

## Fuera de alcance
- Más objetos o más mobiliario; cambios de planta, puzles, zombis o botín.
- Texturas con IA generativa (si se quisieran, se enseña el prompt antes).

## Riesgos
- Más triángulos en un nivel de 62 salas: se mide y se recorta si hace falta.
- Cambiar las texturas de pared y suelo cambia toda la luz percibida: se ajusta la exposición de las luces de techo después.
