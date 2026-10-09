using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>
    /// Contador de fotogramas por segundo, apagado por defecto. F3 lo muestra u oculta.
    /// Se crea solo al cargar la escena. Muestra fps medios y el peor fotograma de la ultima ventana de 0,5 s.
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        bool visible;
        float windowStart;
        int frames;
        float worst;
        float fps;
        float worstShown;
        GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindAnyObjectByType<FpsCounter>() != null) return;
            DontDestroyOnLoad(new GameObject("FpsCounter", typeof(FpsCounter)));
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) visible = !visible;

            float dt = Time.unscaledDeltaTime;
            frames++;
            if (dt > worst) worst = dt;
            float now = Time.unscaledTime;
            if (now - windowStart >= 0.5f)
            {
                fps = frames / (now - windowStart);
                worstShown = worst;
                frames = 0; worst = 0f; windowStart = now;
            }
        }

        void OnGUI()
        {
            if (!visible) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                style.normal.textColor = Color.white;
            }
            var sb = ShadowBudget.Instance;
            string shadows = sb != null ? "  sombras " + sb.ShadowedCount : "";
            string text = fps.ToString("F0") + " fps  peor " + (worstShown * 1000f).ToString("F1") + " ms" + shadows;
            var r = new Rect(10, 10, 420, 26);
            GUI.color = Color.black; GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
            GUI.color = fps >= 60f ? Color.white : new Color(1f, 0.6f, 0.3f);
            GUI.Label(r, text, style);
        }
    }
}
