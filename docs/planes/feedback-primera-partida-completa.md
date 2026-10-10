# Plan: cambios tras la primera partida completa (2026-10-10)

El usuario rejugó la comisaría entera tras las etapas A a G. Este plan recoge todo lo que dijo, la causa que ya he encontrado en el código (solo lectura) y el orden de trabajo. **Nada de esto está aplicado.** Pendiente de aprobación; las decisiones abiertas están al final.

## Lo que dijo el usuario y lo que he verificado

| # | Queja o petición | Causa o estado (leído en el código) |
|---|---|---|
| 1 | Las notas muestran la palabra clave en rojo y grande debajo del texto, pisando el papel. Prefiere la palabra en rojo dentro del propio texto, y mejor estilo de nota. | `HudArchive.cs` ~l. 166 dibuja `NoteData.highlight` aparte, grande y rotado. |
| 2 | Desde la sala de estanterías junto a la sala de máquinas expendedoras se activó el jefe (su barra de vida). | `ZombieAI.Noise` despierta a los dormidos del radio, y a través de paredes si están al 35 % del radio, **sin mirar la planta ni excluir a los jefes**. El jefe 1 (S_Arch, planta 2) está justo encima de la planta 1; un disparo abajo lo despierta y pone `ActiveBoss` (barra de vida). Debe despertarlo solo `BossRoomTrigger`. |
| 3 | La reja del archivo «sigue sin verse que se abre»; quiere una mini animación: ver cómo se coloca el 3.er medallón, la cámara va hacia la puerta (recorrido rápido) y se ve abrirse en directo. Lo mismo cada vez que algo abra otra cosa. | `ServiceGate` sube sola 2,2 s sin que la cámara esté allí. Hay que añadir una secuencia de cámara genérica («revelar efecto»). |
| 4 | Munición: al principio casi no hay y al abrir zonas hay demasiada; tuvo que dejar cosas. | Medido: hasta el jefe 1 hay 182 balas, 62 cartuchos y 8 sprays, repartidos de forma pareja por la ruta. Hay que mover munición hacia el principio y bajar el total de las zonas finales. |
| 5 | Faltan salas de guardado (un par más en el mapa de arriba). Aparece un terminal que no debería salir; el teléfono elegido no siempre sale. | `ComisariaGrandeProps`, caso `safe`: usa `SavePhone` solo en el sótano y `SaveTerminal` en las demás. |
| 6 | Muchos zombis se quedan quietos hasta que les disparas. | Son los dormidos (18): solo despiertan por un ruido (disparo) o un disparo; nunca por verte, acercarte o abrir la puerta. |
| 7 | Al disparar a algunos sale la animación de comer en el suelo. | `ZombieFeeding` sustituye el reposo **y la reacción al golpe** por el bucle de morder; al recibir el tiro la reacción se reproduce con ese clip. Hay que comprobar quién lo tiene activo (debería ser solo el carroñero) y que la reacción vuelva a la normal. |
| 8 | Sillas en pasillos que estorban: quiere poder empujarlas al caminar (las mesas no). | Todas las sillas son estáticas con collider de caja. |
| 9 | El jefe del archivo debía romper las estanterías y no lo hace: solo choca. | **Causa:** `PropBreaker.PropRoot` solo rompe lo que cuelga de un grupo llamado `Props` o `*_Props`; el mobiliario de la planta nueva cuelga de `Props/Mobiliario`, así que no se rompe nada. Además descarta lo que mida más de 3,2 m en planta o 2,6 m de alto (`MetalRack` mide 2,64). |
| 10 | Patrullas: mejorar el 3D y añadir una que no esté quemada. | Solo existe `PoliceCar.fbx` (quemada). |
| 11 | Objetos clave (tijeras, tarjetas...) llenan el inventario y no se van aunque ya estén usados. No debería poder tirarlos: solo guardarlos en el baúl; al estar completamente usados, un check rojo en la esquina de su casilla del baúl, y entonces se habilita «tirar» y desaparece del todo. | Hoy cualquier objeto se puede tirar (`Inventory.Drop` crea un `Pickup` en el suelo). |
| 12 | Todo lo que se tira debería desaparecer del escenario (desecharlo), no quedarse en el suelo. | Ver 11. |
| 13 | Falta un mapa; uno se pierde y las ayudas de la esquina superior izquierda no bastan. | No existe. |
| 14 | Las dos tarjetas son casi idénticas y no queda claro cuál abre qué al volver a un sitio. | Mismo modelo (`KeyCard.fbx`) con distinto tinte y casi el mismo nombre. |

## Fases propuestas (cada una cierra con commit, documentación y prueba)

### Fase 1: arreglos pequeños y seguros (causas ya localizadas)
1. **Jefe**: `Noise` ignora a los que tienen `bossName` y a los que están a más de ~2,5 m de altura sin línea directa (nadie despierta por un disparo en otra planta). `PropRoot` acepta también grupos `Mobiliario`; se prueba que el jefe 1 rompe estanterías (`MetalRack`, `ArchiveShelf*`) y que el NavMesh se reabre.
2. **Guardado**: siempre `SavePhone`, en las 3 salas actuales; **2 salas nuevas** en la planta baja y la primera (propuesta abajo).
3. **Tirar = desechar**: `Inventory.Drop` destruye el objeto (sin `Pickup`).
4. **Notas**: palabra clave en rojo dentro del texto (`<color>` con negrita) y un estilo nuevo (papel envejecido, márgenes, tipografías por categoría, clip o cinta, sin el sello rojo flotante).
5. **Animación de comer**: localizar qué zombis la usan al ser disparados y arreglar la reacción.

### Fase 2: progreso y claridad
6. **Objetos clave**: `ItemData.keyItem`, sin opción de tirar, solo guardar en el baúl. Cada uno declara cuándo está «completamente usado» (cizalla: sus 3 candados cortados; tarjeta de seguridad: sus 2 puertas abiertas; tarjeta del jefe: calabozos; medallones y fusibles ya se consumen al usarse). Check rojo en la esquina de la casilla del baúl; con el check, se habilita «tirar» (desaparece).
7. **Tarjetas distintas**: dos modelos y nombres claros (por ejemplo la de seguridad azul con foto, la del jefe dorada de acceso total); el lector de cada puerta del mismo color y un rótulo («Requiere: ...») en la puerta, y la descripción del objeto dice a qué puertas sirve.
8. **Secuencias de revelado**: componente `ProgressCutscene` (cámara + bloqueo de control): coloca el medallón/fusible, vuelve la cámara hacia la reja o la puerta con un recorrido rápido, se ve abrirse en directo y se devuelve el control. Se usa en reja del archivo y puerta de las calderas; el ascensor y el portón, si se quiere.
9. **Munición**: mover ~35 % de las balas de las zonas centrales al principio (espera, garaje, vestíbulo, radio, sala de agentes) y bajar ~20 % el total de las zonas finales; mantener el presupuesto de los jefes. Se recuenta en escena y se anota.
10. **Zombis dormidos**: según la decisión de abajo.

### Fase 3: funciones nuevas más grandes
11. **Mapa** (tecla M): según la decisión de abajo; se genera por script desde `ComisariaGrande.Rooms` y `Doors` (una imagen por planta, con las puertas con llave marcadas), con marcador del jugador.
12. **Sillas empujables**: según la decisión; `Rigidbody` con masa baja, no estáticas, que el personaje empuja al andar; fuera del bake del NavMesh y del occlusion como occluder.
13. **Patrullas**: según la decisión.

## Salas de guardado nuevas (propuesta mía, se puede cambiar)
- **Planta baja, ala este:** sala de descanso (`G_Break`, 204 m², tiene un carroñero que se quita o se mueve) o sala de agentes (`G_Brief`).
- **Primera planta:** despacho del jefe de seguridad (`F_Chief`, silencioso, solo se llega por la escalera de incendios) o sala del sindicato (`F_Lounge`, ala este, ya tiene sofás).
Con las ya existentes quedan 5 en el mapa principal.

## Riesgos
- Las fases 2 y 3 tocan scripts de runtime y UI (`Inventory`, `HudInventory`, `ItemStorage`, `ItemData`, `ServiceGate`, `MedallionMonument`, `PlayerController`) y compatibilidad con partidas guardadas.
- El mapa y las sillas empujables pueden pesar en rendimiento; se mide como en `docs/rendimiento.md`.
- Los modelos nuevos (tarjetas, patrullas) se hacen con Blender (`modelo-blender`); si se usa IA generativa, se enseñan modelo y prompt exacto antes.

## Decisiones tomadas (2026-10-10, respondidas por el usuario)
1. **Zombis dormidos:** despiertan al acercarte (a ~3,5 m con línea directa; agachado, la mitad) o por ruido (disparos); quedan de pie, sin animación nueva. Baja el número de dormidos de ~18 a ~12.
2. **Sillas empujables:** todas menos las butacas del auditorio de la sala de conferencias.
3. **Patrullas:** rehacerlas con Blender (modelo más detallado, variante intacta y variante quemada), sin IA.
4. **Mapa:** pantalla completa (tecla M), un plano por planta con niebla: solo se dibujan las salas ya visitadas, con las puertas con llave marcadas y la posición del jugador.
5. **Salas de guardado nuevas** (mi propuesta, no vetada): `G_Intake` (ingreso, planta baja) y `F_Mem` (memorial, primera planta, junto al caracol). Teléfono siempre, nunca terminal.

## Estado
- Fase 1: en curso (ver el registro del README y `docs/rediseno-comisaria.md`).
