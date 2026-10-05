#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Fase 0: convierte el nivel generado por codigo (TestSceneBuilder) en una escena editable a mano.
    /// Crea prefabs de los elementos repetidos y conecta todas sus copias de la escena a ellos (editar el prefab
    /// cambia todas), y renombra la escena a Comisaria.unity. A partir de aqui la ESCENA es la fuente de verdad.
    /// </summary>
    public static class SceneMigration
    {
        const string Root = "Assets/_Project/Prefabs";
        const string ScenePath = "Assets/Scenes/Comisaria.unity";

        [MenuItem("Horror/Migrar escena a prefabs (Fase 0)")]
        public static void MigrateMenu()
        {
            Debug.Log("[Horror] " + Migrate());
        }

        public static string Migrate()
        {
            var log = new List<string>();
            foreach (var f in new[] { "Characters", "Interactables", "Lighting", "Doors" })
                if (!AssetDatabase.IsValidFolder($"{Root}/{f}"))
                {
                    if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
                    AssetDatabase.CreateFolder(Root, f);
                }

            // Personajes: un prefab por objeto (jugador y cada tipo de zombi)
            var player = GameObject.Find("Player");
            if (player != null && !PrefabUtility.IsPartOfPrefabInstance(player))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(player, $"{Root}/Characters/Player.prefab", InteractionMode.AutomatedAction);
                log.Add("Player");
            }
            foreach (var z in Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None))
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(z.gameObject)) continue;
                PrefabUtility.SaveAsPrefabAssetAndConnect(z.gameObject, $"{Root}/Characters/{z.name}.prefab", InteractionMode.AutomatedAction);
                log.Add(z.name);
            }

            // Elementos repetidos: el primero se guarda como prefab y el resto se convierten en copias suyas
            // (los valores que difieren, como la intensidad de cada lampara, quedan como cambios de esa copia)
            log.Add(Group(Object.FindObjectsByType<CeilingLamp>(FindObjectsSortMode.None).Select(c => c.gameObject), $"{Root}/Lighting/CeilingLamp.prefab"));
            log.Add(Group(Object.FindObjectsByType<LightSwitch>(FindObjectsSortMode.None).Select(c => c.gameObject), $"{Root}/Lighting/LightSwitch.prefab"));
            log.Add(Group(Object.FindObjectsByType<LockerDoor>(FindObjectsSortMode.None).Select(c => c.gameObject), $"{Root}/Interactables/Locker.prefab"));
            log.Add(Group(Object.FindObjectsByType<ItemBox>(FindObjectsSortMode.None).Select(c => c.gameObject), $"{Root}/Interactables/ItemBox.prefab"));

            // Terminales de guardado: el pedestal y sus piezas sueltas se agrupan bajo un padre
            var terminals = new List<GameObject>();
            foreach (var t in Object.FindObjectsByType<SaveTerminal>(FindObjectsSortMode.None))
                terminals.Add(GroupTerminal(t));
            log.Add(Group(terminals, $"{Root}/Interactables/SaveTerminal.prefab"));

            // Puertas: un prefab por modelo (madera 1.5 m, madera 1.4 m, metalica de seguridad)
            var doorRoots = Object.FindObjectsByType<Door>(FindObjectsSortMode.None)
                .Select(d => d.transform.parent != null ? d.transform.parent.gameObject : d.gameObject).Distinct().ToList();
            foreach (var g in doorRoots.GroupBy(DoorModel))
                log.Add(Group(g, $"{Root}/Doors/{g.Key}.prefab"));

            // La escena pasa a llamarse Comisaria
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            if (scene.path != ScenePath && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                string err = AssetDatabase.MoveAsset(scene.path, ScenePath);
                if (!string.IsNullOrEmpty(err)) log.Add("No se pudo renombrar la escena: " + err);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                EditorSceneManager.OpenScene(ScenePath);
            }
            AssetDatabase.SaveAssets();
            return "Migracion completada: " + string.Join(" | ", log.Where(s => !string.IsNullOrEmpty(s)));
        }

        static string DoorModel(GameObject root)
        {
            var leaf = root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Leaf");
            bool security = root.GetComponentsInChildren<Transform>().Any(t => t.name == "PushBar");
            float w = leaf != null ? leaf.localScale.x + 0.04f : 0f;
            return security ? "Door_Security" : $"Door_Wood_{Mathf.RoundToInt(w * 100f)}";
        }

        static GameObject GroupTerminal(SaveTerminal t)
        {
            if (t.transform.parent != null && t.transform.parent.name == "SaveTerminal_Group") return t.transform.parent.gameObject;
            var group = new GameObject("SaveTerminal_Group");
            group.transform.SetParent(t.transform.parent, false);
            var basePos = t.transform.position;
            group.transform.position = new Vector3(basePos.x, 0f, basePos.z);
            // las piezas sueltas (pantalla, base, brillo) estan a menos de 1 m del pedestal
            var parts = new List<Transform> { t.transform };
            foreach (Transform sib in t.transform.parent)
                if ((sib.name == "Screen" || sib.name == "ScreenBase" || sib.name == "TerminalGlow") &&
                    Vector2.Distance(new Vector2(sib.position.x, sib.position.z), new Vector2(basePos.x, basePos.z)) < 1f)
                    parts.Add(sib);
            foreach (var p in parts) p.SetParent(group.transform, true);
            // la orientacion (que lado mira) la da la pantalla: rotar el grupo para que el prefab sea uno solo
            return group;
        }

        /// <summary>Guarda el primero como prefab y convierte el resto en copias. Devuelve un resumen.</summary>
        static string Group(IEnumerable<GameObject> objects, string path)
        {
            var list = objects.Where(o => o != null && !PrefabUtility.IsPartOfPrefabInstance(o)).ToList();
            if (list.Count == 0) return null;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            int start = 0;
            if (asset == null)
            {
                asset = PrefabUtility.SaveAsPrefabAssetAndConnect(list[0], path, InteractionMode.AutomatedAction);
                start = 1;
            }
            int converted = 0, failed = 0;
            for (int i = start; i < list.Count; i++)
            {
                try
                {
                    PrefabUtility.ConvertToPrefabInstance(list[i], asset, new ConvertToPrefabInstanceSettings
                    {
                        changeRootNameToAssetName = false,
                        componentsNotMatchedBecomesOverride = true,
                        gameObjectsNotMatchedBecomesOverride = true,
                        recordPropertyOverridesOfMatches = true,
                        objectMatchMode = ObjectMatchMode.ByHierarchy,
                    }, InteractionMode.AutomatedAction);
                    converted++;
                }
                catch (System.Exception e)
                {
                    failed++;
                    Debug.LogWarning($"[Horror] {list[i].name} no se pudo enlazar a {path}: {e.Message}");
                }
            }
            return $"{System.IO.Path.GetFileNameWithoutExtension(path)} x{converted + start}" + (failed > 0 ? $" ({failed} sin enlazar)" : "");
        }
    }
}
#endif
