#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Horror.EditorTools
{
    /// <summary>Crea la escena del menu principal (Assets/Scenes/MainMenu.unity) y la pone la primera en los ajustes de compilacion.</summary>
    public static class MainMenuBuilder
    {
        const string Path = "Assets/Scenes/MainMenu.unity";

        static GameObject Place(string fbx, string ctrl, Vector3 pos, float yaw, float scale, string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Mixamo/Characters/" + fbx);
            if (model == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            an.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrl);
            an.applyRootMotion = false;
            return go;
        }

        [MenuItem("Horror/Crear menu principal")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.01f, 0.01f, 0.015f); c.fieldOfView = 38f;
            cam.transform.position = new Vector3(0.2f, 1.45f, -4.2f);
            cam.transform.rotation = Quaternion.Euler(4f, -6f, 0f);
            var urp = cam.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urp.renderPostProcessing = false;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.025f, 0.03f, 0.04f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.015f, 0.02f); RenderSettings.fogDensity = 0.11f;

            // luz fria de lado sobre el protagonista y un contraluz rojo detras
            var key = new GameObject("Luz fria", typeof(Light)); var kl = key.GetComponent<Light>();
            kl.type = LightType.Spot; kl.color = new Color(0.65f, 0.78f, 1f); kl.intensity = 90f; kl.range = 14f; kl.spotAngle = 34f;
            key.transform.position = new Vector3(-1.2f, 3.4f, -3.0f); key.transform.LookAt(new Vector3(1.3f, 1.1f, 0f));
            var rim = new GameObject("Contraluz rojo", typeof(Light)); var rl = rim.GetComponent<Light>();
            rl.type = LightType.Spot; rl.color = new Color(1f, 0.1f, 0.05f); rl.intensity = 170f; rl.range = 16f; rl.spotAngle = 26f;
            rim.transform.position = new Vector3(3.0f, 2.6f, 2.4f); rim.transform.LookAt(new Vector3(1.3f, 1.5f, 0f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Suelo";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.012f, 0.012f, 0.014f) };
            mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, "Assets/_Project/Art/Mixamo/Materials/MenuFloor.mat");
            floor.GetComponent<Renderer>().sharedMaterial = mat;

            Place("Player_Soldier.fbx", "Assets/_Project/Animation/PlayerHumanoid.controller", new Vector3(1.3f, 0f, 0f), 160f, 1f, "Soldado");
            Place("Zombie_Civil_Mixamo.fbx", "Assets/_Project/Animation/Zombie_civil.overrideController", new Vector3(-0.2f, 0f, 5.6f), 190f, 1f, "Zombi 1");
            Place("Zombie_Cop.fbx", "Assets/_Project/Animation/Zombie_cop.overrideController", new Vector3(3.6f, 0f, 7.4f), 170f, 0.9f, "Zombi 2");
            Place("Boss.fbx", "Assets/_Project/Animation/Boss.controller", new Vector3(-2.2f, 0f, 10.5f), 200f, 1.35f, "Jefe");

            new GameObject("Menu", typeof(Horror.MainMenu));

            EditorSceneManager.SaveScene(scene, Path);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Path, true),
                new EditorBuildSettingsScene("Assets/Scenes/Comisaria.unity", true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[Horror] Menu principal creado y puesto el primero en la compilacion.");
        }
    }
}
#endif
