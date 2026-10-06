using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Horror
{
    [Serializable] public class SlotSave { public string item; public int count; }
    [Serializable] public class MagSave { public string weapon; public int ammo; }
    [Serializable] public class PickupSave { public string item; public int count; public Vector3 pos; public Quaternion rot; }
    [Serializable] public class ZombieSave { public string name; public Vector3 pos; public Quaternion rot; public float hp; }

    [Serializable]
    public class SaveData
    {
        public string savedAt;
        public Vector3 playerPos;
        public float playerYaw;
        public float cameraYaw;
        public float health;
        public List<SlotSave> slots = new List<SlotSave>();
        public string equipped;
        public List<MagSave> mags = new List<MagSave>();
        public List<PickupSave> pickups = new List<PickupSave>();
        public List<ZombieSave> zombies = new List<ZombieSave>();
        public bool doorUnlocked;       // formato antiguo (una sola puerta)
        public bool doorOpen;
        public List<bool> doorsUnlocked = new List<bool>();
        public List<bool> doorsOpen = new List<bool>();
        public List<bool> lockers = new List<bool>();
        public List<SlotSave> box = new List<SlotSave>();   // baul de objetos (comun a todas las salas seguras)
        public List<bool> switches = new List<bool>();
        public string objective;
    }

    /// <summary>
    /// Guardado y carga de partida en un archivo JSON. Guarda: jugador (posicion, vida), inventario, arma equipada
    /// y cargadores, objetos sueltos, zombis vivos, puerta, interruptores y objetivo.
    /// Cargar = recargar la escena y aplicar los datos en GameFlow.Start.
    /// </summary>
    public static class SaveSystem
    {
        public static SaveData Pending { get; private set; }

        static string FilePath => Path.Combine(Application.persistentDataPath, "savegame.json");
        public static bool HasSave => File.Exists(FilePath);

        public static void ClearPending() => Pending = null;

        static IEnumerable<Door> OrderedDoors() =>
            UnityEngine.Object.FindObjectsByType<Door>(FindObjectsSortMode.None)
                .OrderBy(x => x.transform.position.z).ThenBy(x => x.transform.position.x);

        static IEnumerable<LockerDoor> OrderedLockers() =>
            UnityEngine.Object.FindObjectsByType<LockerDoor>(FindObjectsSortMode.None)
                .OrderBy(x => x.transform.position.z).ThenBy(x => x.transform.position.x);

        static IEnumerable<LightSwitch> OrderedSwitches() =>
            UnityEngine.Object.FindObjectsByType<LightSwitch>(FindObjectsSortMode.None)
                .OrderBy(s => s.transform.position.z).ThenBy(s => s.transform.position.x);

        public static bool Save()
        {
            var pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (pc == null) return false;
            var health = pc.GetComponent<Health>();
            var inv = pc.GetComponent<Inventory>();
            var weapons = pc.GetComponent<WeaponController>();
            var cam = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();

            var d = new SaveData
            {
                savedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                playerPos = pc.transform.position,
                playerYaw = pc.transform.eulerAngles.y,
                cameraYaw = cam != null ? cam.Yaw : pc.transform.eulerAngles.y,
                health = health != null ? health.Current : 100f,
                objective = Objectives.Current,
            };

            if (inv != null && inv.slots != null)
                foreach (var s in inv.slots)
                    d.slots.Add(s.IsEmpty ? new SlotSave { item = "", count = 0 } : new SlotSave { item = s.item.displayName, count = s.count });
            if (weapons != null)
            {
                d.equipped = weapons.Equipped != null ? weapons.Equipped.displayName : "";
                foreach (var kv in weapons.Magazines) d.mags.Add(new MagSave { weapon = kv.Key.displayName, ammo = kv.Value });
            }

            foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                if (p.item != null) d.pickups.Add(new PickupSave { item = p.item.displayName, count = p.count, pos = p.transform.position, rot = p.transform.rotation });

            foreach (var z in ZombieAI.All)   // solo los vivos
            {
                var zh = z.GetComponent<Health>();
                d.zombies.Add(new ZombieSave { name = z.name, pos = z.transform.position, rot = z.transform.rotation, hp = zh != null ? zh.Current : 100f });
            }

            foreach (var door in OrderedDoors()) { d.doorsUnlocked.Add(door.IsUnlocked); d.doorsOpen.Add(door.IsOpen); }
            foreach (var l in OrderedLockers()) d.lockers.Add(l.IsOpen);
            foreach (var b in ItemStorage.Slots) d.box.Add(new SlotSave { item = b.item.displayName, count = b.count });
            foreach (var s in OrderedSwitches()) d.switches.Add(s.IsOn);

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(d, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Horror] No se pudo guardar la partida: " + e.Message);
                return false;
            }
        }

        public static string SavedAt()
        {
            if (!HasSave) return "";
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)).savedAt; }
            catch { return ""; }
        }

        /// <summary>Lee el archivo y recarga la escena; GameFlow aplica los datos al arrancar.</summary>
        public static bool LoadAndRestart()
        {
            if (!HasSave) return false;
            try { Pending = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
            catch (Exception e)
            {
                Debug.LogWarning("[Horror] Partida guardada ilegible: " + e.Message);
                return false;
            }
            GameState.ResetAll();
            SceneManager.LoadScene(GameSettings.GameScene);
            return true;
        }

        /// <summary>Aplica los datos cargados a la escena recien creada. Devuelve true si habia algo que aplicar.</summary>
        public static bool ApplyPending()
        {
            var d = Pending;
            if (d == null) return false;
            Pending = null;

            var pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                var cc = pc.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;           // el CharacterController no deja teletransportar mientras esta activo
                pc.transform.SetPositionAndRotation(d.playerPos, Quaternion.Euler(0f, d.playerYaw, 0f));
                if (cc != null) cc.enabled = true;
                var cam = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCamera>();
                if (cam != null) cam.SetYaw(d.cameraYaw);

                var health = pc.GetComponent<Health>();
                if (health != null) health.SetCurrent(d.health);

                var inv = pc.GetComponent<Inventory>();
                if (inv != null)
                {
                    var stacks = new List<ItemStack>();
                    foreach (var s in d.slots)
                    {
                        var item = string.IsNullOrEmpty(s.item) ? null : ItemDatabase.Find(s.item);
                        stacks.Add(item == null ? default : new ItemStack { item = item, count = s.count });
                    }
                    inv.SetSlots(stacks);
                }

                var weapons = pc.GetComponent<WeaponController>();
                if (weapons != null)
                {
                    WeaponData equipped = null;
                    foreach (var m in d.mags)
                    {
                        var w = FindWeapon(m.weapon);
                        if (w != null) weapons.SetMagazine(w, m.ammo);
                        if (m.weapon == d.equipped) equipped = w;
                    }
                    if (equipped == null && !string.IsNullOrEmpty(d.equipped)) equipped = FindWeapon(d.equipped);
                    weapons.Equip(equipped);
                }
            }

            // Objetos sueltos: se retiran los de la escena y se recrean tal como estaban
            foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(p.gameObject);
            var itemsRoot = GameObject.Find("Items");
            foreach (var ps in d.pickups)
            {
                var item = ItemDatabase.Find(ps.item);
                if (item == null) continue;
                var p = Pickup.Spawn(item, ps.count, ps.pos);
                p.transform.rotation = ps.rot;
                if (itemsRoot != null) p.transform.SetParent(itemsRoot.transform);
            }

            // Zombis: los que no estaban vivos al guardar se retiran; los demas vuelven a su sitio y vida
            var alive = d.zombies.ToDictionary(z => z.name, z => z);
            foreach (var z in UnityEngine.Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None))
            {
                if (!alive.TryGetValue(z.name, out var zs)) { UnityEngine.Object.Destroy(z.gameObject); continue; }
                var agent = z.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(zs.pos);
                else z.transform.position = zs.pos;
                z.transform.rotation = zs.rot;
                var zh = z.GetComponent<Health>();
                if (zh != null) zh.SetCurrent(zs.hp);
            }

            int di = 0;
            foreach (var door in OrderedDoors())
            {
                if (di < d.doorsOpen.Count) door.ApplySaved(d.doorsUnlocked[di], d.doorsOpen[di]);
                di++;
            }
            var boxStacks = new List<ItemStack>();
            foreach (var b in d.box)
            {
                var it = ItemDatabase.Find(b.item);
                if (it != null) boxStacks.Add(new ItemStack { item = it, count = b.count });
            }
            ItemStorage.Set(boxStacks);

            int li = 0;
            foreach (var l in OrderedLockers()) { if (li < d.lockers.Count) l.SetOpen(d.lockers[li]); li++; }

            int i = 0;
            foreach (var s in OrderedSwitches()) { if (i < d.switches.Count) s.SetOn(d.switches[i]); i++; }

            Objectives.Set(d.objective, false);
            Hud.Message("Partida cargada");
            return true;
        }

        static WeaponData FindWeapon(string displayName)
        {
            var db = ItemDatabase.Instance;
            if (db == null || db.items == null) return null;
            foreach (var i in db.items)
                if (i != null && i.weapon != null && i.weapon.displayName == displayName) return i.weapon;
            return null;
        }
    }
}
