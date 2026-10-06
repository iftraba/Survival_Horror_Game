using UnityEngine;

namespace Horror
{
    /// <summary>Lista de slots de guardado para el menu principal, la pausa y el telefono (IMGUI).</summary>
    public static class SaveSlotsGUI
    {
        public const float RowHeight = 54f, RowGap = 8f;

        public static float Height => SaveSystem.SlotCount * (RowHeight + RowGap);

        /// <summary>
        /// Dibuja los slots uno bajo otro. saving=true: se puede elegir cualquiera (vacio o no); false: solo los ocupados.
        /// selected resalta un slot (teclado). Devuelve el slot pulsado (1..N) o 0.
        /// </summary>
        public static int Draw(float x, float y, float w, bool saving, int selected = 0)
        {
            var left = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 16, richText = true };
            int clicked = 0;
            var prevColor = GUI.color;
            for (int i = 1; i <= SaveSystem.SlotCount; i++)
            {
                var info = SaveSystem.Peek(i);
                string last = i == SaveSystem.CurrentSlot && info.exists ? "   <i>(ultimo)</i>" : "";
                string text = info.exists
                    ? $"  Slot {i}   {info.savedAt}{last}\n  <size=13>{Shorten(info.objective, 52)}</size>"
                    : $"  Slot {i}   <i>Vacio</i>";
                GUI.enabled = saving || info.exists;
                if (i == selected) GUI.color = new Color(1f, 0.85f, 0.4f);
                if (GUI.Button(new Rect(x, y, w, RowHeight), text, left)) clicked = i;
                GUI.color = prevColor;
                y += RowHeight + RowGap;
            }
            GUI.enabled = true;
            return clicked;
        }

        static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max - 1) + "...";
        }
    }
}
