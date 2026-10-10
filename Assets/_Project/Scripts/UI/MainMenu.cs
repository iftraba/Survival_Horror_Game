using UnityEngine;
using UnityEngine.SceneManagement;

namespace Horror
{
    /// <summary>Menu principal: partida nueva, continuar, opciones y salir.</summary>
    public class MainMenu : MonoBehaviour
    {
        public string title = "SECTOR 7: GRIMHEIM";
        public string subtitle = "una noche muy larga";

        GUIStyle titleStyle, subStyle, small;
        bool options, loadList;

        void Start()
        {
            GameState.ResetAll();
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Styles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 54, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };   // 54: "SECTOR 7: GRIMHEIM" cabe en el panel
            subStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleLeft };
            small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        }

        void OnGUI()
        {
            Styles();
            float scale = Mathf.Max(0.75f, Screen.height / 800f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale, sh = Screen.height / scale;

            // degradado oscuro a la izquierda para que se lea el texto sobre la escena
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, 560, sh), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(new Rect(560, 0, 160, sh), Texture2D.whiteTexture);
            GUI.color = new Color(0.85f, 0.12f, 0.1f);
            GUI.Label(new Rect(60, sh * 0.16f, 700, 90), title, titleStyle);
            GUI.color = new Color(0.85f, 0.8f, 0.7f);
            GUI.Label(new Rect(64, sh * 0.16f + 84, 600, 30), subtitle, subStyle);
            GUI.color = Color.white;

            float x = 64, y = sh * 0.45f, w = 340, h = 46, gap = 12;
            if (loadList)
            {
                GUI.Label(new Rect(x, y - 44, w, 30), "Cargar partida", small);
                int slot = SaveSlotsGUI.Draw(x, y, 440, false, SaveSystem.CurrentSlot);
                if (slot > 0) SaveSystem.LoadAndRestart(slot);
                if (GUI.Button(new Rect(x, y + SaveSlotsGUI.Height + 4, w, h), "Volver")) loadList = false;
            }
            else if (!options)
            {
                if (GUI.Button(new Rect(x, y, w, h), "Nueva partida")) NewGame();
                y += h + gap;
                GUI.enabled = SaveSystem.HasAnySave;
                string cont = SaveSystem.HasAnySave ? "Cargar partida" : "Cargar partida  (sin guardados)";
                if (GUI.Button(new Rect(x, y, w, h), cont)) loadList = true;
                GUI.enabled = true;
                y += h + gap;
                if (GUI.Button(new Rect(x, y, w, h), "Opciones")) options = true;
                y += h + gap;
                if (GUI.Button(new Rect(x, y, w, h), "Salir")) Quit();
                // controles: una linea por grupo y ancho dentro del panel (antes dos lineas largas se partian y se pisaban)
                string[] controls = { "WASD  mover        Mayus  correr", "Clic dcho  apuntar        Clic izq  disparar", "E  interactuar        R  recargar", "Q  girar        1-4  armas", "Tab  inventario        Esc  pausa" };
                float cy = sh - 24f - controls.Length * 26f;
                foreach (var line in controls) { GUI.Label(new Rect(x, cy, 480, 26), line, small); cy += 26f; }
            }
            else
            {
                GUI.Label(new Rect(x, y, w, 26), "Volumen  " + Mathf.RoundToInt(GameSettings.Volume * 100f) + "%", small);
                GameSettings.Volume = GUI.HorizontalSlider(new Rect(x, y + 30, w, 20), GameSettings.Volume, 0f, 1f);
                y += 70;
                GUI.Label(new Rect(x, y, w, 26), "Sensibilidad del raton  " + GameSettings.Sensitivity.ToString("0.00") + "x", small);
                GameSettings.Sensitivity = GUI.HorizontalSlider(new Rect(x, y + 30, w, 20), GameSettings.Sensitivity, 0.3f, 2.5f);
                y += 80;
                if (GUI.Button(new Rect(x, y, w, h), "Volver")) { GameSettings.Save(); options = false; }
            }
            GUI.matrix = old;
        }

        public static void NewGame()
        {
            SaveSystem.ClearPending();
            GameState.ResetAll();
            SceneManager.LoadScene(GameSettings.GameScene);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
