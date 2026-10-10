#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande (menu Horror/Comisaria grande/9 Mapa): copia el plano (salas y puertas con su cota) de ComisariaGrande.Define() a un objeto
    /// "Mapa" de la escena (MapData + MapTracker), que es lo que dibuja la pestana MAPA del menu del inventario. Lo llama tambien el menu 1,
    /// asi que solo hace falta ejecutarlo a mano si se cambia Define() sin rehacer el nivel.
    /// </summary>
    public static class ComisariaGrandeMapKit
    {
        [MenuItem("Horror/Comisaria grande/9 Mapa")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            ComisariaGrande.Define();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("--- COMISARIA V2 ---"); if (root == null) return "falta la fase A";
            string r = Fill(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return r;
        }

        public static string Fill(Transform level)
        {
            var old = level.Find("Mapa"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject("Mapa"); go.transform.SetParent(level);
            var md = go.AddComponent<MapData>(); go.AddComponent<MapTracker>();
            var rooms = new System.Collections.Generic.List<MapRoom>();
            foreach (var r in ComisariaGrande.Rooms)
            {
                if (r.kind == ComisariaGrande.Kind.Shaft) continue;
                rooms.Add(new MapRoom { id = r.id, label = r.label, type = r.type, group = r.group, x0 = r.r.xMin, z0 = r.r.yMin, x1 = r.r.xMax, z1 = r.r.yMax, floorY = r.floor, indoor = r.Indoor, safe = r.type == "safe" });
            }
            var doors = new System.Collections.Generic.List<MapDoor>();
            foreach (var d in ComisariaGrande.Doors)
            {
                if (d.kind == ComisariaGrande.DoorKind.Fixed) continue;
                int k = d.kind == ComisariaGrande.DoorKind.Padlock ? 1 : d.kind == ComisariaGrande.DoorKind.Card ? 2 : d.kind == ComisariaGrande.DoorKind.ChiefCard ? 3 : d.kind == ComisariaGrande.DoorKind.Power ? 4 : d.kind == ComisariaGrande.DoorKind.Roll ? 6 : 0;
                doors.Add(new MapDoor { name = d.name, x = d.p.x, z = d.p.y, floorY = d.floor, width = d.width, kind = k });
            }
            md.rooms = rooms.ToArray(); md.doors = doors.ToArray();
            EditorUtility.SetDirty(md);
            return "mapa: " + rooms.Count + " salas, " + doors.Count + " puertas";
        }
    }
}
#endif
