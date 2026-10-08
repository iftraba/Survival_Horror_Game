using UnityEngine;

namespace Horror
{
    /// <summary>Parte del Hud: aviso de pulsar repetidamente (agarre de un zombi) con barra de progreso.</summary>
    public partial class Hud
    {
        static string qteText;
        static float qteProgress;

        public static void SetQte(string text, float progress) { qteText = text; qteProgress = Mathf.Clamp01(progress); }
        public static void ClearQte() { qteText = null; }

        void DrawQte()
        {
            if (string.IsNullOrEmpty(qteText)) return;
            float w = Mathf.Min(520f, Screen.width - 40f), y = Screen.height * 0.7f;
            var old = GUI.color;
            // el texto parpadea un poco para llamar la atencion
            GUI.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 12f));
            GUI.Label(new Rect(0, y, Screen.width, 30), qteText, center);
            GUI.color = old;
            var bar = new Rect((Screen.width - w) / 2f, y + 36f, w, 14f);
            Fill(bar, new Color(0f, 0f, 0f, 0.7f));
            Fill(new Rect(bar.x + 2, bar.y + 2, (bar.width - 4) * qteProgress, bar.height - 4), new Color(0.85f, 0.15f, 0.1f, 0.95f));
            Frame(bar, new Color(1f, 1f, 1f, 0.6f), 1f);
        }
    }
}
