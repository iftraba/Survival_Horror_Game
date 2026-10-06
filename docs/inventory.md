# Inventory

Objetos, inventario del jugador y baúl global. Código en `Assets/_Project/Scripts/Inventory/`. Datos en `Assets/_Project/Data/`.

## Clases
| Clase | Qué hace |
|---|---|
| `ItemData` (ScriptableObject) | Objeto: nombre, descripción, `ItemType` (Weapon, Ammo, Healing, Key, Misc), icono, `maxStack`, prefab en el suelo (`worldPrefab`, `worldScale`, `mass`), `healAmount`, `weapon` (si es un arma), `ammoType`, `pickupObjective`. Assets: `I_Pistol`, `I_Shotgun`, `I_HandgunAmmo`, `I_ShotgunAmmo`, `I_Spray`, `I_KeyRoom`, `I_KeyExit`. |
| `ItemStack` | Pareja objeto + cantidad. |
| `Inventory` | Cuadrícula del jugador (`slotCount` 8). `TryAdd` apila y luego usa huecos libres, `ConsumeAmmo`, `Drop`, `Has`, evento `Changed`. **Riñoneras**: `AddBagSlots`/`SetBagSlots` amplían la cuadrícula (`BagSlots`, tope `maxBagSlots` = 6 → 14 casillas) conservando el contenido. |
| Objetos clave con modelo (Blender, `Tools/blender/build_keys.py`) | `I_KeyGarage` (llave de acero con llavero amarillo, `Art/Weapons/KeyGarage.fbx`), `I_KeyCard` (tarjeta magnética con banda, chip y foto, `KeyCard.fbx`) e `I_KeyFinal` (llave maestra dorada con gema roja, `KeyMaster.fbx`), cada uno con su icono (`ItemIcons`). Antes eran cubos sin icono. |
| `ItemType.Bag` / `ItemData.extraSlots` | Riñonera (`I_Bag`, +2 casillas). Al recogerla (`Pickup.Interact`) no entra en el inventario: suma casillas de forma permanente y desaparece. Se guarda en `SaveData.bagSlots` y se aplica antes de cargar las casillas. La rejilla del inventario sube para que quepan 3-4 filas. Está dentro de la **taquilla con código** de la sala de reuniones (planta alta, código 4719, pista en una nota del archivo); la monta `ArchiveSetup`. Modelo hecho en Blender (`Tools/blender/build_rinonera.py`, `Art/Props/Rinonera.fbx`, ~1.400 triángulos, 39 × 7 × 9 cm tumbada con las correas extendidas, nailon carbón con cremalleras y hebilla); miniatura `Art/Icons/I_Bag.png` con `ItemIcons`. Al recogerla se abre la pantalla de **objeto conseguido** (`ItemShowcase`, modelo 3D girando); la animación del personaje poniéndosela queda para más adelante. |
| `ItemStorage` (estática) | Baúl **global** compartido por todas las salas seguras (capacidad 48). `Put`, `RemoveAt`, `Set`, `Clear`. Se guarda con la partida. |

## Equipo inicial
`GameFlow`: la pistola ya equipada y el cargador con `startMagazine` balas, más munición de reserva en el inventario.

## Decisiones
- Los objetos se reconstruyen por `displayName` al cargar (`ItemDatabase`): **no cambies el nombre** de un `ItemData` sin pensar en los guardados.
