# UI

Interfaz. Código en `Assets/_Project/Scripts/UI/`. Todo es **IMGUI** provisional (`OnGUI`).

## Clases
| Clase | Qué hace |
|---|---|
| `Hud` | Salud, munición con miniatura del arma, mira, indicaciones (`Prompt`), mensajes (`Hud.Message`), objetivo, barra de vida del jefe, inventario (con examen 3D del objeto), pantalla del baúl, menú de guardado (estilo teléfono, tonos cálidos), pausa (Reanudar, Cargar, Reiniciar, **Menú principal**, Salir), muerte y victoria. Atajos: Tab inventario, Esc pausa, R reiniciar al morir. |
| `ItemPreview` | Estudio fotográfico oculto (cámara, luces, modelo) para el "examinar objeto" del inventario. |
| `MainMenu` | Menú principal (escena `MainMenu`): Nueva partida, Continuar (con fecha del guardado), Opciones (volumen y sensibilidad) y Salir. Escala la interfaz con la altura de pantalla. |

## Escenas
`Assets/Scenes/MainMenu.unity` (índice 0 de compilación; Soldier y zombis en penumbra) y `Assets/Scenes/Comisaria.unity` (índice 1).

## Límites conocidos
- IMGUI no se escala bien en pantallas muy distintas; está previsto migrar a UI Toolkit o uGUI.
