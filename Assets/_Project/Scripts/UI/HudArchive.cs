using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>Parte del Hud: Archivo (pestana de Tab), lectura de notas a pantalla completa y teclado de taquilla.</summary>
    public partial class Hud
    {
        bool archiveTab;
        NoteCategory archiveCat = NoteCategory.Story;
        int archiveSel;

        static readonly Color PaperInk = new Color(0.17f, 0.12f, 0.08f);
        static readonly Color PenRed = new Color(0.58f, 0.08f, 0.06f);

        // ------------------------------------------------------------------ teclas

        /// <summary>Notas, teclado y pestana del Archivo. Devuelve true si la tecla se ha consumido y no hay que seguir.</summary>
        bool ArchiveAndKeypadKeys(Keyboard kb, bool fresh)
        {
            Keypad.Tick();
            if (GameState.ShowcaseOpen)
            {
                if (!fresh && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame
                               || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                    ItemShowcase.Close();
                return true;
            }
            if (GameState.NoteOpen)
            {
                if (!fresh && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame
                               || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                    NoteArchive.Close();
                return true;
            }
            if (GameState.KeypadOpen)
            {
                if (fresh) return true;
                if (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) { Keypad.Close(); return true; }
                if (kb.backspaceKey.wasPressedThisFrame) Keypad.Backspace();
                for (int d = 0; d <= 9; d++)
                    if (kb[DigitKeys[d]].wasPressedThisFrame || kb[NumpadKeys[d]].wasPressedThisFrame) Keypad.Press(d);
                return true;
            }
            return false;
        }

        static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        /// <summary>Dentro del inventario: Q cambia entre Objetos y Archivo; en el Archivo, flechas/WASD eligen categoria y nota.</summary>
        void ArchiveKeys(Keyboard kb)
        {
            var list = NoteArchive.Of(archiveCat);
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            {
                archiveCat = archiveCat == NoteCategory.Story ? NoteCategory.Puzzle : NoteCategory.Story;
                archiveSel = 0;
            }
            if (list.Count > 0)
            {
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) archiveSel = (archiveSel + 1) % list.Count;
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) archiveSel = (archiveSel - 1 + list.Count) % list.Count;
            }
        }

        // ------------------------------------------------------------------ pestanas e Archivo

        void DrawInvTabs()
        {
            float w = 190f, h = 34f, x = Screen.width / 2f - w - 6f, y = 14f;
            if (TabButton(new Rect(x, y, w, h), "OBJETOS", !archiveTab)) { archiveTab = false; }
            if (TabButton(new Rect(x + w + 12f, y, w, h), "ARCHIVO  (" + NoteArchive.Read.Count + ")", archiveTab)) { archiveTab = true; }
            GUI.color = new Color(1f, 1f, 1f, 0.45f);
            GUI.Label(new Rect(0, y + h + 2f, Screen.width, 20f), "Q: cambiar de pestaña", new GUIStyle(center) { fontSize = 12 });
            GUI.color = Color.white;
        }

        static bool TabButton(Rect r, string text, bool active)
        {
            Fill(r, active ? new Color(0.28f, 0.24f, 0.12f, 0.98f) : new Color(0.1f, 0.11f, 0.12f, 0.95f));
            Frame(r, active ? new Color(1f, 0.82f, 0.3f) : new Color(1f, 1f, 1f, 0.15f), active ? 2f : 1f);
            var st = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = active ? new Color(1f, 0.88f, 0.5f) : new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(r, text, st);
            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { e.Use(); return true; }
            return false;
        }

        void DrawArchive()
        {
            float totalW = 340f + 30f + 560f;
            float x0 = Screen.width / 2f - totalW / 2f, y0 = 100f;
            float pageH = Mathf.Min(Screen.height - y0 - 50f, 640f);

            // categorias
            var st = NoteCategory.Story; var pz = NoteCategory.Puzzle;
            if (TabButton(new Rect(x0, y0, 165f, 32f), "HISTORIA  " + NoteArchive.Of(st).Count, archiveCat == st)) { archiveCat = st; archiveSel = 0; }
            if (TabButton(new Rect(x0 + 175f, y0, 165f, 32f), "PISTAS  " + NoteArchive.Of(pz).Count, archiveCat == pz)) { archiveCat = pz; archiveSel = 0; }

            var list = NoteArchive.Of(archiveCat);
            archiveSel = list.Count == 0 ? 0 : Mathf.Clamp(archiveSel, 0, list.Count - 1);
            float ly = y0 + 44f;
            for (int i = 0; i < list.Count; i++)
            {
                var r = new Rect(x0, ly + i * 50f, 340f, 44f);
                bool sel = i == archiveSel;
                Fill(r, sel ? new Color(0.22f, 0.2f, 0.12f, 0.95f) : new Color(0.1f, 0.11f, 0.12f, 0.95f));
                Frame(r, sel ? new Color(1f, 0.82f, 0.3f) : new Color(1f, 1f, 1f, 0.12f), sel ? 2f : 1f);
                var ts = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
                ts.normal.textColor = sel ? new Color(1f, 0.9f, 0.6f) : new Color(1f, 1f, 1f, 0.8f);
                GUI.Label(new Rect(r.x + 12f, r.y, r.width - 20f, r.height), list[i].title, ts);
                var e = Event.current;
                if (e.type == EventType.MouseDown && r.Contains(e.mousePosition)) { archiveSel = i; e.Use(); }
            }
            if (list.Count == 0)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(new Rect(x0, ly + 6f, 340f, 80f),
                    archiveCat == NoteCategory.Story ? "Aún no has encontrado\nninguna nota de historia." : "Aún no has encontrado\nninguna pista.",
                    new GUIStyle(label) { fontSize = 15 });
                GUI.color = Color.white;
            }

            // pagina
            var page = new Rect(x0 + 370f, y0, 560f, pageH);
            if (list.Count > 0) DrawNotePage(page, list[archiveSel], 0.85f);
            else
            {
                Fill(page, new Color(0.06f, 0.07f, 0.08f, 0.95f));
                Frame(page, new Color(1f, 1f, 1f, 0.1f), 1f);
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.Label(page, "Las notas que leas quedan guardadas aquí", center);
                GUI.color = Color.white;
            }
        }

        // ------------------------------------------------------------------ hoja de papel (2D)

        /// <summary>Dibuja una nota como una hoja de papel: titulo, texto a mano, linea destacada y dibujo opcional. s escala las letras.</summary>
        void DrawNotePage(Rect r, NoteData n, float s)
        {
            Fill(r, new Color(0.88f, 0.82f, 0.67f));
            Fill(new Rect(r.x + r.width * 0.60f, r.y + r.height * 0.06f, r.width * 0.28f, r.height * 0.13f), new Color(0.62f, 0.45f, 0.22f, 0.10f));   // mancha
            Fill(new Rect(r.x + r.width * 0.05f, r.y + r.height * 0.72f, r.width * 0.18f, r.height * 0.1f), new Color(0.5f, 0.38f, 0.2f, 0.08f));
            Fill(new Rect(r.x, r.yMax - r.height * 0.16f, r.width, r.height * 0.16f), new Color(0.45f, 0.34f, 0.18f, 0.08f));                       // borde sucio
            Fill(new Rect(r.x, r.y + r.height * 0.5f, r.width, 1f), new Color(0.4f, 0.32f, 0.2f, 0.25f));                                              // doblez
            Frame(r, new Color(0.33f, 0.26f, 0.16f, 0.9f), 2f);

            float pad = 30f * s, x = r.x + pad, w = r.width - pad * 2f, y = r.y + pad;
            var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(28f * s), fontStyle = FontStyle.Bold, wordWrap = true };
            title.normal.textColor = PaperInk;
            float th = title.CalcHeight(new GUIContent(n.title), w);
            GUI.Label(new Rect(x, y, w, th), n.title, title);
            y += th + 6f * s;
            Fill(new Rect(x, y, w, 2f), new Color(PaperInk.r, PaperInk.g, PaperInk.b, 0.45f));
            y += 14f * s;

            var body = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(21f * s), wordWrap = true, fontStyle = FontStyle.Italic };
            body.normal.textColor = PaperInk;
            float bh = body.CalcHeight(new GUIContent(n.body), w);
            GUI.Label(new Rect(x, y, w, bh), n.body, body);
            y += bh + 16f * s;

            if (!string.IsNullOrEmpty(n.highlight))
            {
                var hs = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(56f * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                hs.normal.textColor = PenRed;
                float hh = 76f * s;
                GUI.Label(new Rect(x, y, w, hh), n.highlight, hs);
                Fill(new Rect(x + w * 0.22f, y + hh - 4f, w * 0.56f, 3f), new Color(PenRed.r, PenRed.g, PenRed.b, 0.7f));
                y += hh + 14f * s;
            }

            if (n.image != null && n.image.texture != null)
            {
                float ih = Mathf.Max(0f, r.yMax - pad - y);
                if (ih > 40f) GUI.DrawTexture(new Rect(x, y, w, ih), n.image.texture, ScaleMode.ScaleToFit, true);
            }
        }

        void DrawNoteReader()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.82f));
            var n = NoteArchive.Reading;
            if (n == null) return;
            float w = Mathf.Min(660f, Screen.width - 60f), h = Mathf.Min(Screen.height - 120f, 760f);
            var r = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f - 10f, w, h);
            // sombra y ligero giro: el papel esta puesto a mano sobre la mesa
            Fill(new Rect(r.x + 10f, r.y + 12f, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(-1.4f, r.center);
            DrawNotePage(r, n, 1f);
            GUI.matrix = m;
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(0, r.yMax + 24f, Screen.width, 24f), "E / Esc: cerrar    -    queda guardada en el Archivo (Tab)", new GUIStyle(center) { fontSize = 14 });
            GUI.color = Color.white;
        }

        // ------------------------------------------------------------------ objeto conseguido (riñonera...)

        void DrawShowcase()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.01f, 0.012f, 0.015f, 0.9f));
            var item = ItemShowcase.Item;
            if (item == null) return;
            var preview = ItemPreview.Get();
            preview.Show(item);

            float size = Mathf.Min(420f, Screen.height - 330f);
            float cx = Screen.width / 2f, top = Mathf.Max(30f, Screen.height / 2f - size / 2f - 110f);
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(0, top, Screen.width, 26f), "OBJETO CONSEGUIDO", new GUIStyle(center) { fontSize = 14, fontStyle = FontStyle.Bold });
            GUI.color = new Color(0.95f, 0.82f, 0.45f);
            GUI.Label(new Rect(0, top + 24f, Screen.width, 46f), item.displayName, new GUIStyle(center) { fontSize = 34, fontStyle = FontStyle.Bold });
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - size / 2f, top + 76f, size, size), preview.Texture, ScaleMode.ScaleToFit, true);
            float ty = top + 76f + size + 8f;
            if (!string.IsNullOrEmpty(ItemShowcase.Detail))
            {
                GUI.color = new Color(0.55f, 1f, 0.6f);
                GUI.Label(new Rect(0, ty, Screen.width, 30f), ItemShowcase.Detail, new GUIStyle(center) { fontSize = 20, fontStyle = FontStyle.Bold });
                ty += 32f;
            }
            GUI.color = new Color(1f, 1f, 1f, 0.75f);
            float dw = Mathf.Min(640f, Screen.width - 60f);
            GUI.Label(new Rect(cx - dw / 2f, ty, dw, 60f), item.description, new GUIStyle(center) { fontSize = 16, wordWrap = true });
            GUI.color = new Color(1f, 1f, 1f, 0.45f);
            GUI.Label(new Rect(0, Screen.height - 50f, Screen.width, 24f), "E / Esc: continuar", new GUIStyle(center) { fontSize = 14 });
            GUI.color = Color.white;
        }

        // ------------------------------------------------------------------ teclado numerico de la taquilla

        void DrawKeypad()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.8f));
            var locker = Keypad.Target;
            if (locker == null) return;
            var panel = new Rect(Screen.width / 2f - 190f, Screen.height / 2f - 270f, 380f, 540f);
            Fill(panel, new Color(0.08f, 0.09f, 0.1f, 0.98f));
            Frame(panel, new Color(0.6f, 0.65f, 0.7f, 0.5f), 2f);

            var title = new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = new Color(0.85f, 0.88f, 0.92f);
            GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 30f), "TAQUILLA CERRADA", title);
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(panel.x, panel.y + 44f, panel.width, 22f), "Introduce el código", new GUIStyle(center) { fontSize = 14 });
            GUI.color = Color.white;

            // pantallita con los digitos
            int len = locker.code.Length;
            float cw = 56f, gap = 10f, total = len * cw + (len - 1) * gap, dx = panel.center.x - total / 2f, dy = panel.y + 80f;
            bool wrong = Keypad.ShowingWrong;
            for (int i = 0; i < len; i++)
            {
                var c = new Rect(dx + i * (cw + gap), dy, cw, 70f);
                Fill(c, new Color(0.02f, 0.05f, 0.03f, 1f));
                Frame(c, wrong ? new Color(0.9f, 0.2f, 0.15f) : new Color(0.3f, 0.8f, 0.4f, 0.7f), 2f);
                var ds = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                ds.normal.textColor = wrong ? new Color(0.95f, 0.3f, 0.25f) : new Color(0.45f, 1f, 0.55f);
                GUI.Label(c, i < Keypad.Entry.Length ? Keypad.Entry[i].ToString() : "_", ds);
            }
            if (wrong)
            {
                GUI.color = new Color(0.95f, 0.35f, 0.3f);
                GUI.Label(new Rect(panel.x, dy + 78f, panel.width, 24f), "Código incorrecto", new GUIStyle(center) { fontSize = 15 });
                GUI.color = Color.white;
            }

            // botones 3x4
            float bw = 92f, bh = 64f, bg = 12f, bx = panel.center.x - (3 * bw + 2 * bg) / 2f, by = panel.y + 200f;
            string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Borrar", "0", "Salir" };
            for (int i = 0; i < 12; i++)
            {
                var r = new Rect(bx + (i % 3) * (bw + bg), by + (i / 3) * (bh + bg), bw, bh);
                if (!GUI.Button(r, labels[i], new GUIStyle(GUI.skin.button) { fontSize = i == 9 || i == 11 ? 16 : 26, fontStyle = FontStyle.Bold })) continue;
                if (i == 9) Keypad.Backspace();
                else if (i == 11) Keypad.Close();
                else Keypad.Press(i == 10 ? 0 : i + 1);
            }
            GUI.color = new Color(1f, 1f, 1f, 0.45f);
            GUI.Label(new Rect(panel.x, panel.yMax - 30f, panel.width, 22f), "Teclado numérico o ratón  -  Esc: salir", new GUIStyle(center) { fontSize = 12 });
            GUI.color = Color.white;
        }
    }
}
