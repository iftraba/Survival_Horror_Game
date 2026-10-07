# UI

Interfaz. Código en `Assets/_Project/Scripts/UI/`. Todo es **IMGUI** provisional (`OnGUI`).

## Clases
| Clase | Qué hace |
|---|---|
| `Hud` | Salud, munición con miniatura del arma, **mira de cruz** (cuatro trazos de 7,7 px con hueco y punto central, borde oscuro, escala con la resolución; el hueco se abre 0,25 s al disparar: `Hud.CrosshairKick`), indicaciones (`Prompt`), mensajes (`Hud.Message`), objetivo, barra de vida del jefe, inventario (con examen 3D del objeto), pantalla del baúl, menú de guardado (estilo teléfono, tonos cálidos; lista de 5 slots con fecha y objetivo, flechas + Enter o clic, pide confirmación al sobrescribir), pausa (Reanudar, Cargar → lista de slots, Reiniciar, **Menú principal**, Salir), muerte y victoria. Atajos: Tab inventario, Esc pausa, R reiniciar al morir. |
| `ItemPreview` | Estudio fotográfico oculto (cámara, luces, modelo) para el "examinar objeto" del inventario. |
| `Hud` (parcial `HudArchive`) | **Archivo**: pestaña de Tab (Q o clic cambia entre Objetos y Archivo) con dos categorías, Historia y Pistas; lista de notas leídas y la hoja a la derecha. **Lectura de nota**: hoja de papel 2D a pantalla completa (título, texto a mano, línea destacada en rojo, dibujo opcional), E/Esc la cierra. **Teclado de taquilla**: panel con pantalla de dígitos y botones 3x4. **Objeto conseguido** (`ItemShowcase`): al coger una riñonera se pausa y se muestra el modelo 3D girando (visor de `ItemPreview`), su nombre, "+N casillas de inventario (permanente)" y la descripción; E/Esc continúa. |
| `MainMenu` | Menú principal (escena `MainMenu`, título **SECTOR 7: GRIMHEIM** a 54 pt): Nueva partida, Cargar partida (lista de 5 slots con fecha y objetivo; `SaveSlotsGUI` es la lista compartida con la pausa y el teléfono), Opciones (volumen y sensibilidad) y Salir. Escala la interfaz con la altura de pantalla. |

## Escenas
`Assets/Scenes/MainMenu.unity` (índice 0 de compilación; Soldier y zombis en penumbra) y `Assets/Scenes/Comisaria.unity` (índice 1).

## Límites conocidos
- IMGUI no se escala bien en pantallas muy distintas; está previsto migrar a UI Toolkit o uGUI.
