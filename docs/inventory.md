# Inventory

Objetos, inventario del jugador y baúl global. Código en `Assets/_Project/Scripts/Inventory/`. Datos en `Assets/_Project/Data/`.

## Clases
| Clase | Qué hace |
|---|---|
| `ItemData` (ScriptableObject) | Objeto: nombre, descripción, `ItemType` (Weapon, Ammo, Healing, Key, Misc), icono, `maxStack`, prefab en el suelo (`worldPrefab`, `worldScale`, `mass`), `healAmount`, `weapon` (si es un arma), `ammoType`, `pickupObjective`. Assets: `I_Pistol`, `I_Shotgun`, `I_HandgunAmmo`, `I_ShotgunAmmo`, `I_Spray`, `I_KeyRoom`, `I_KeyExit`. |
| `ItemStack` | Pareja objeto + cantidad. |
| `Inventory` | Cuadrícula del jugador (`slotCount` 8). `TryAdd` apila y luego usa huecos libres, `ConsumeAmmo`, `Drop`, `Has`, evento `Changed`. |
| `ItemStorage` (estática) | Baúl **global** compartido por todas las salas seguras (capacidad 48). `Put`, `RemoveAt`, `Set`, `Clear`. Se guarda con la partida. |

## Equipo inicial
`GameFlow`: la pistola ya equipada y el cargador con `startMagazine` balas, más munición de reserva en el inventario.

## Decisiones
- Los objetos se reconstruyen por `displayName` al cargar (`ItemDatabase`): **no cambies el nombre** de un `ItemData` sin pensar en los guardados.
