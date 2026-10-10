#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande, fase E (menu Horror/Comisaria grande/5 Zombis y botin): la dificultad. Planta v3 (docs/planes/rediseno-planta.md).
    ///  - 43 zombis: planta baja 13, primera 13, segunda 3, sotano 14 (garaje 3, calabozos 2, zona industrial 9). Entre ellos 5 reptantes y
    ///    3 carroneros comiendose un cadaver (solo despiertan si les disparan); una parte en letargo (no reaccionan hasta oir un ruido o
    ///    recibir un tiro). Ninguno en el vestibulo, las salas seguras, los aseos, la escalera norte, el ingreso, la custodia ni la antesala.
    ///    Dos encuentros a mano: un Cop dormido en la silla de la tarjeta de seguridad y un reptante dormido junto a un coche del garaje.
    ///  - Botin por anclas (encima de un mueble de la sala que corresponde: guantera del coche, mesa del agente, armero...) con los totales
    ///    del plan: hasta el jefe 1 ~180 balas y ~66 cartuchos; hasta el jefe 2 ~96 y ~34; sprays 8 + 4. Lo que hay dentro de las taquillas
    ///    (y las reservas de las arenas) lo ponen los menus 4 y 6. Provisional: se ajusta jugando.
    /// Repetible: rehace "Enemigos" y "Botin".
    /// </summary>
    public static class ComisariaGrandeEnemies
    {
        // Solo los aprobados por el usuario (docs/enemies.md, 2026-10-08): los de Mixamo con el juego de animaciones unificado y los de Pxltiger
        // (Pxl3 lleva la piel del jefe 2). Los ZombieGen* (Ejecutivo, Infectado, Mecanico, Paciente, Policia) estan descartados: no usarlos.
        public static readonly string[] Normal = { "Zombie_Civil", "Zombie_Cop", "Zombie_Girl", "Zombie_Oficial", "Zombie_Pxl1", "Zombie_Pxl2" };

        // sala -> (zombis, probabilidad de letargo, extras: 'R' reptante, 'C' carronero)
        static readonly (string room, int n, float dormant, string extra)[] Spawns =
        {
            // planta baja (13; con el Cop de seguridad a mano)
            ("G_Wait", 1, 0.5f, ""), ("G_Radio", 2, 0.3f, ""), ("G_Dark", 2, 0.8f, ""), ("G_Brief", 1, 0.2f, ""), ("G_Armory", 1, 0f, ""),
            ("G_Break", 0, 0f, "C"), ("G_Lock", 2, 0.4f, ""), ("G_Work", 1, 0.2f, ""), ("G_Alley", 0, 0f, "R"),
            // primera planta (13)
            ("F_Comm", 1, 0.5f, ""), ("F_Conf", 1, 0.3f, ""), ("F_Det", 2, 0.2f, ""), ("F_Lib", 1, 0.5f, ""), ("F_GalA", 1, 0.3f, ""),
            ("F_Canteen", 2, 0.3f, ""), ("F_Admin", 1, 0.2f, ""), ("F_NHall", 1, 0f, ""), ("F_Rec", 1, 0.4f, "R"), ("F_Lounge", 0, 0f, "C"),
            // segunda planta (3)
            ("S_RoofA", 1, 0f, ""), ("S_Arch", 2, 0.6f, ""),
            // sotano (14; el reptante del garaje va a mano)
            ("B_Garage", 2, 0.3f, ""), ("B_Cells", 1, 0.3f, "R"),
            ("B_Pump", 2, 0.3f, ""), ("B_Mach", 2, 0.2f, ""), ("B_Store", 1, 0.4f, ""), ("B_Lab", 1, 0.3f, "C"), ("B_Control", 1, 0.2f, ""), ("B_Gal", 0, 0f, "R"),
        };

        // botin: sala, clases de mueble donde puede ir (la primera que exista), objeto, cantidad, que mueble de esa clase (0 = el mas cercano al centro...)
        static readonly (string room, string[] on, string item, int n, int index)[] Loot =
        {
            // hasta el jefe 1: balas (las taquillas aportan 35 mas) -> ~182
            ("G_Wait", new[] { "WaitingBench" }, "I_HandgunAmmo", 12, 0), ("B_Garage", new[] { "PoliceCar" }, "I_HandgunAmmo", 10, 1),
            ("G_Lobby", new[] { "ReceptionDesk" }, "I_HandgunAmmo", 10, 0), ("G_Radio", new[] { "Desk" }, "I_HandgunAmmo", 10, 0),
            ("G_Brief", new[] { "Desk" }, "I_HandgunAmmo", 10, 1), ("G_Sec", new[] { "Desk" }, "I_HandgunAmmo", 8, 1),
            ("G_Work", new[] { "Workbench" }, "I_HandgunAmmo", 12, 0), ("G_Armory", new[] { "GunRack" }, "I_HandgunAmmo", 15, 0),
            ("G_Intake", new[] { "ReceptionDesk" }, "I_HandgunAmmo", 10, 0), ("F_Conf", new[] { "Lectern" }, "I_HandgunAmmo", 10, 0),
            ("F_Comm", new[] { "ExecutiveDesk" }, "I_HandgunAmmo", 10, 0), ("F_Det", new[] { "Desk" }, "I_HandgunAmmo", 12, 0),
            ("F_Canteen", new[] { "KitchenCounter" }, "I_HandgunAmmo", 8, 0), ("S_Ante", new[] { "Desk" }, "I_HandgunAmmo", 10, 0),
            // hasta el jefe 1: cartuchos (las taquillas aportan 10 mas) -> ~68
            ("G_Armory", new[] { "GunRack" }, "I_ShotgunAmmo", 6, 1), ("G_Dark", new[] { "MetalRack" }, "I_ShotgunAmmo", 8, 0),
            ("F_Det", new[] { "Desk" }, "I_ShotgunAmmo", 6, 1), ("F_Lib", new[] { "Bookcase" }, "I_ShotgunAmmo", 6, 0),
            ("F_Lounge", new[] { "Couch", "Desk" }, "I_ShotgunAmmo", 6, 0), ("F_Rec", new[] { "ArchiveCart", "ArchiveShelfA", "ArchiveShelfB" }, "I_ShotgunAmmo", 6, 0),
            ("S_Ante", new[] { "Desk" }, "I_ShotgunAmmo", 8, 1), ("B_Cells", new[] { "Desk" }, "I_ShotgunAmmo", 6, 0),
            // hasta el jefe 1: sprays (+1 en la taquilla de seguridad) -> 8
            ("G_Radio", new[] { "Desk" }, "I_Spray", 1, 1), ("G_Safe", new[] { "Desk" }, "I_Spray", 1, 0), ("F_Canteen", new[] { "KitchenCounter" }, "I_Spray", 1, 1),
            ("F_Det", new[] { "Desk" }, "I_Spray", 1, 2), ("F_Lib", new[] { "Bookcase" }, "I_Spray", 1, 1), ("S_Ante", new[] { "Desk" }, "I_Spray", 1, 2),
            ("B_Cells", new[] { "Desk" }, "I_Spray", 1, 1),
            // despues del jefe 1 (sotano industrial): ~96 balas, ~34 cartuchos (6 en la taquilla del almacen), 4 sprays
            ("B_Pump", new[] { "Barrel", "WaterPump", "Crate" }, "I_HandgunAmmo", 12, 0), ("B_Mach", new[] { "Workbench" }, "I_HandgunAmmo", 12, 0),
            ("B_Store", new[] { "MetalRack", "Shelf" }, "I_HandgunAmmo", 12, 0), ("B_Lab", new[] { "LabBench" }, "I_HandgunAmmo", 10, 0),
            ("B_Fuse", new[] { "Workbench" }, "I_HandgunAmmo", 12, 0), ("B_Control", new[] { "Desk" }, "I_HandgunAmmo", 12, 0),
            ("B_Safe", new[] { "Desk" }, "I_HandgunAmmo", 14, 1), ("B_Gal", new[] { "WaitingBench" }, "I_HandgunAmmo", 12, 0),
            ("B_Mach", new[] { "Generator", "Workbench" }, "I_ShotgunAmmo", 6, 1), ("B_Lab", new[] { "LabCabinet" }, "I_ShotgunAmmo", 6, 0),
            ("B_Control", new[] { "Desk" }, "I_ShotgunAmmo", 6, 1), ("B_Safe", new[] { "Shelf" }, "I_ShotgunAmmo", 8, 0),
            ("B_Lab", new[] { "LabBench" }, "I_Spray", 1, 1), ("B_Control", new[] { "Desk" }, "I_Spray", 1, 2), ("B_Safe", new[] { "Desk" }, "I_Spray", 1, 2),
            ("B_Mach", new[] { "Workbench" }, "I_Spray", 1, 1),
        };

        static System.Random rng;

        static Vector3? NavPoint(ComisariaGrande.Room r, List<Vector3> taken, float minDist)
        {
            for (int i = 0; i < 60; i++)
            {
                var p = new Vector3((float)(r.r.xMin + 1f + rng.NextDouble() * (r.r.width - 2f)), r.floor + 0.2f, (float)(r.r.yMin + 1f + rng.NextDouble() * (r.r.height - 2f)));
                if (!NavMesh.SamplePosition(p, out var h, 0.5f, NavMesh.AllAreas)) continue;
                if (Mathf.Abs(h.position.y - r.floor) > 0.4f || !r.r.Contains(new Vector2(h.position.x, h.position.z))) continue;
                if (taken.Any(t => Vector3.Distance(t, h.position) < minDist)) continue;
                return h.position;
            }
            return null;
        }

        [MenuItem("Horror/Comisaria grande/5 Zombis y botin")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            ComisariaGrande.Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var level = GameObject.Find("--- COMISARIA V2 ---").transform;
            foreach (var n in new[] { "Enemigos", "Botin" }) { var o = level.Find(n); if (o != null) Object.DestroyImmediate(o.gameObject); }
            var zr = new GameObject("Enemigos").transform; zr.SetParent(level);
            var lr = new GameObject("Botin").transform; lr.SetParent(level);
            rng = new System.Random(66);
            var taken = new List<Vector3>();
            var log = new List<string>();
            int total = 0, dorm = 0, reptantes = 0, carroneros = 0;
            GameObject Spawn(string prefab, Vector3 p, string name, bool dormant)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/" + prefab + ".prefab");
                if (pf == null) { log.Add("falta " + prefab); return null; }
                var z = (GameObject)PrefabUtility.InstantiatePrefab(pf, zr);
                z.name = name; z.transform.SetPositionAndRotation(p + Vector3.up * 0.05f, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0));
                total++;
                if (dormant) { var ai = z.GetComponent<ZombieAI>(); ai.dormant = true; EditorUtility.SetDirty(ai); PrefabUtility.RecordPrefabInstancePropertyModifications(ai); dorm++; }
                return z;
            }
            foreach (var (roomId, n, dp, extra) in Spawns)
            {
                var r = ComisariaGrande.Rooms.FirstOrDefault(x => x.id == roomId); if (r == null) { log.Add("falta la sala " + roomId); continue; }
                for (int i = 0; i < n; i++)
                {
                    var p = NavPoint(r, taken, 2.5f); if (p == null) { log.Add("sin sitio en " + roomId); continue; }
                    taken.Add(p.Value);
                    Spawn(Normal[rng.Next(Normal.Length)], p.Value, "Z_" + roomId + "_" + i, rng.NextDouble() < System.Math.Min(1.0, dp * 1.1));
                }
                foreach (var c in extra)
                {
                    var p = NavPoint(r, taken, 2.5f); if (p == null) { log.Add("sin sitio para el extra en " + roomId); continue; }
                    taken.Add(p.Value);
                    if (c == 'R') { Spawn("Zombie_OficialReptante", p.Value, "Z_Reptante_" + roomId, false); reptantes++; }
                    else if (c == 'C')
                    {
                        // carronero = un zombi normal de pie con ZombieFeeding.feeding marcado: come junto a su cadaver y, al dispararle, se levanta con su juego
                        // de animaciones de siempre (el prefab Zombie_Carronero usaba el bucle de morder como animacion de movimiento y de golpe)
                        var feeder = Spawn(new[] { "Zombie_Civil", "Zombie_Cop", "Zombie_Girl", "Zombie_Oficial" }[rng.Next(4)], p.Value, "Z_Carronero_" + roomId, false);
                        var fd = feeder != null ? feeder.GetComponent<ZombieFeeding>() : null;
                        if (fd != null) { fd.feeding = true; fd.spawnCorpse = true; EditorUtility.SetDirty(fd); PrefabUtility.RecordPrefabInstancePropertyModifications(fd); carroneros++; }
                        else log.Add("el carronero de " + roomId + " no tiene ZombieFeeding");
                    }
                }
            }
            Physics.SyncTransforms();
            ComisariaGrandeAnchors.Init(level);

            // encuentros a mano: el Cop dormido junto a la mesa donde esta la tarjeta de seguridad y el reptante dormido junto a un coche del garaje
            var card = level.GetComponentsInChildren<Pickup>(true).FirstOrDefault(pk => pk.item != null && pk.item.name == "I_CardSecurity");
            if (card != null && NavMesh.SamplePosition(card.transform.position, out var ch, 2.5f, NavMesh.AllAreas))
            { Spawn("Zombie_Cop", ch.position, "Z_G_Sec_Cop", true); taken.Add(ch.position); }
            else log.Add("sin tarjeta de seguridad o sin NavMesh junto a ella: falta el Cop dormido");
            var cars = ComisariaGrandeAnchors.Furniture("B_Garage", "PoliceCar").OrderBy(t => t.position.x).ToList();
            if (cars.Count > 1 && NavMesh.SamplePosition(cars[1].GetComponent<BoxCollider>().bounds.center, out var rh, 3.5f, NavMesh.AllAreas))
            { Spawn("Zombie_OficialReptante", rh.position, "Z_Reptante_B_Garage", true); reptantes++; }
            else log.Add("sin coche o sin NavMesh junto a el: falta el reptante del garaje");

            // botin por anclas
            int items = 0, bullets1 = 0, shells1 = 0, sprays1 = 0;
            foreach (var (roomId, on, itemName, n, index) in Loot)
            {
                var it = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/" + itemName + ".asset");
                if (it == null) { log.Add("falta " + itemName); continue; }
                var p = ComisariaGrandeAnchors.Spot(roomId, on, index, null, itemName + " x" + n);
                var pk = Pickup.Spawn(it, n, p + Vector3.up * 0.12f); pk.transform.SetParent(lr); items++;
                bool early = !roomId.StartsWith("B_") || roomId == "B_Garage" || roomId == "B_Cells";
                if (early) { if (itemName == "I_HandgunAmmo") bullets1 += n; else if (itemName == "I_ShotgunAmmo") shells1 += n; else sprays1 += n; }
            }
            foreach (var m in ComisariaGrandeAnchors.Log) log.Add(m);
            var oldMode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script; Physics.SyncTransforms();
            for (int i = 0; i < 150; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "fase E: " + total + " zombis (" + dorm + " en letargo, " + reptantes + " reptantes, " + carroneros + " carroneros), " + items + " objetos sueltos (tabla hasta el jefe 1: "
                + bullets1 + " balas, " + shells1 + " cartuchos, " + sprays1 + " sprays; las taquillas suman mas) | " + string.Join(" | ", log);
        }
    }
}
#endif
