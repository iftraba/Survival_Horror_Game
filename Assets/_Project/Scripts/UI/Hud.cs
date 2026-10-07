using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Horror
{
    /// <summary>HUD e inventario provisionales con IMGUI. Se reemplazaran por UI Toolkit/uGUI mas adelante.</summary>
    public partial class Hud : MonoBehaviour
    {
        public Health playerHealth;
        public Inventory inventory;
        public WeaponController weapons;
        public PlayerController player;

        static string message;
        static float messageUntil;

        int selected = -1;
        GUIStyle label, big, center;
        int saveSel = 1, overwriteSlot;   // menu de guardado: slot elegido y slot pendiente de confirmar sobrescritura
        bool pauseLoadList, pauseOptions;

        public static void Message(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 2.5f;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (GameState.Victory || GameState.PlayerDead)
            {
                if (kb.rKey.wasPressedThisFrame) Restart();
                return;
            }

            bool fresh = Time.frameCount <= GameState.MenuOpenedFrame;   // la tecla que abrio el menu no cuenta dentro
            if (GameState.SaveMenuOpen)
            {
                if (fresh) { saveSel = SaveSystem.CurrentSlot; overwriteSlot = 0; }
                int n = SaveSystem.SlotCount;
                if (!fresh && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) { saveSel = saveSel % n + 1; overwriteSlot = 0; }
                if (!fresh && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) { saveSel = (saveSel + n - 2) % n + 1; overwriteSlot = 0; }
                if (!fresh && (kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) TrySave(saveSel);
                else if (!fresh && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) GameState.SetSaveMenuOpen(false);
                return;
            }
            if (ArchiveAndKeypadKeys(kb, fresh)) return;   // nota a pantalla completa o teclado de taquilla
            if (GameState.BoxOpen)
            {
                if (!fresh && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                    GameState.SetBoxOpen(false);
                return;
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                pauseLoadList = false;
                if (pauseOptions) { GameSettings.Save(); pauseOptions = false; }
                if (GameState.InventoryOpen)
                {
                    // Esc cierra primero el examen, luego el menu del objeto y por ultimo el inventario
                    if (examining) { examining = false; ItemPreview.Get().Manual = false; }
                    else if (invMenu) invMenu = false;
                    else { CloseInventoryPanels(); GameState.SetInventoryOpen(false); ItemPreview.Get().Show(null); }
                }
                else GameState.SetPaused(!GameState.Paused);
                return;
            }

            if (kb.tabKey.wasPressedThisFrame && !GameState.Paused)
            {
                GameState.SetInventoryOpen(!GameState.InventoryOpen);
                if (!GameState.InventoryOpen) { CloseInventoryPanels(); ItemPreview.Get().Show(null); }   // apaga la camara del visor
                return;
            }
            if (!GameState.InventoryOpen) archiveTab = false;
            if (GameState.InventoryOpen && !GameState.Paused)
            {
                if (kb.qKey.wasPressedThisFrame) archiveTab = !archiveTab;
                if (archiveTab) ArchiveKeys(kb); else InventoryKeys(kb);
            }
        }

        static void Restart()
        {
            SaveSystem.ClearPending();
            GameState.ResetAll();
            SceneManager.LoadScene(GameSettings.GameScene);
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnDestroy()
        {
            // Evita dejar el juego pausado al recargar
            if (GameState.InventoryOpen) GameState.SetInventoryOpen(false);
        }

        void Styles()
        {
            if (label != null)
            {
                // el color se reasigna en cada OnGUI: en el editor el skin lo devuelve a negro (tema claro) despues de crear los estilos
                label.normal.textColor = Color.white; big.normal.textColor = Color.white; center.normal.textColor = Color.white;
                return;
            }
            label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            big = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            center = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            // texto blanco explicito: sin esto hereda el color del skin (negro con el tema claro del editor)
            label.normal.textColor = Color.white; big.normal.textColor = Color.white; center.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            Styles();
            // al examinar un objeto solo se ve el objeto (sin vida, municion, objetivo ni pestanas)
            bool examineView = GameState.InventoryOpen && examining && !archiveTab;
            if (!examineView)
            {
                DrawHealth();
                DrawAmmo();
                DrawCrosshair();
                DrawPrompt();
                DrawMessage();
                DrawObjective();
                DrawBossBar();
            }
            if (GameState.InventoryOpen) DrawInventory();
            if (GameState.BoxOpen) DrawItemBox();
            if (GameState.SaveMenuOpen) DrawSaveMenu();
            if (GameState.NoteOpen) DrawNoteReader();
            if (GameState.KeypadOpen) DrawKeypad();
            if (GameState.ShowcaseOpen) DrawShowcase();
            if (GameState.Paused) DrawPause();
            if (GameState.PlayerDead) DrawDeath();
            if (GameState.Victory) DrawVictory();
        }

        void DrawObjective()
        {
            if (string.IsNullOrEmpty(Objectives.Current) || GameState.Victory) return;
            var small = new GUIStyle(label) { fontSize = 13, fontStyle = FontStyle.Bold };
            var text = new GUIStyle(label) { fontSize = 15, wordWrap = true };
            GUI.color = new Color(0.9f, 0.75f, 0.3f);
            GUI.Label(new Rect(20, 16, 420, 20), "OBJETIVO", small);
            GUI.color = Color.white;
            GUI.Label(new Rect(20, 36, 420, 60), Objectives.Current, text);
        }

        /// <summary>Barra de vida del jefe, abajo en el centro, mientras pelea.</summary>
        void DrawBossBar()
        {
            var boss = ZombieAI.ActiveBoss;
            if (boss == null || boss.Hp == null || boss.Hp.IsDead || GameState.Victory) return;
            float pct = Mathf.Clamp01(boss.Hp.Current / boss.Hp.maxHealth);
            float w = Mathf.Min(Screen.width * 0.5f, 640f), h = 16f;
            var r = new Rect(Screen.width / 2f - w / 2f, Screen.height - 70f, w, h);
            Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0f, 0f, 0f, 0.7f));
            Fill(r, new Color(0.12f, 0.02f, 0.02f, 1f));
            Fill(new Rect(r.x, r.y, r.width * pct, r.height), Color.Lerp(new Color(0.55f, 0.05f, 0.05f), new Color(0.85f, 0.15f, 0.1f), pct));
            Frame(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0.8f, 0.7f, 0.5f, 0.6f), 1f);
            var st = new GUIStyle(label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = new Color(0.95f, 0.85f, 0.6f);
            GUI.Label(new Rect(r.x, r.y - 26f, r.width, 22f), boss.bossName, st);
            GUI.color = Color.white;
        }

        void DrawPause()
        {
            GUI.color = new Color(0, 0, 0, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 320f, h = 44f, gap = 12f;
            float x = Screen.width / 2f - w / 2f, y = Screen.height / 2f - 150f;
            GUI.Label(new Rect(0, y - 70, Screen.width, 60), "PAUSA", big);

            if (pauseLoadList)
            {
                float lx = Screen.width / 2f - 220f, ly = Mathf.Max(90f, Screen.height / 2f - SaveSlotsGUI.Height / 2f - 20f);
                GUI.Label(new Rect(0, ly - 44, Screen.width, 34), "CARGAR PARTIDA", center);
                int slot = SaveSlotsGUI.Draw(lx, ly, 440f, false, SaveSystem.CurrentSlot);
                if (slot > 0) SaveSystem.LoadAndRestart(slot);
                if (GUI.Button(new Rect(x, ly + SaveSlotsGUI.Height + 4, w, h), "Volver")) pauseLoadList = false;
                return;
            }

            // Opciones (volumen y sensibilidad, las mismas que en el menu principal; se guardan al salir)
            if (pauseOptions)
            {
                GUI.Label(new Rect(x, y, w, 26), "Volumen  " + Mathf.RoundToInt(GameSettings.Volume * 100f) + "%", center);
                GameSettings.Volume = GUI.HorizontalSlider(new Rect(x, y + 32, w, 20), GameSettings.Volume, 0f, 1f);
                y += 76f;
                GUI.Label(new Rect(x, y, w, 26), "Sensibilidad del raton  " + GameSettings.Sensitivity.ToString("0.00") + "x", center);
                GameSettings.Sensitivity = GUI.HorizontalSlider(new Rect(x, y + 32, w, 20), GameSettings.Sensitivity, 0.3f, 2.5f);
                y += 84f;
                if (GUI.Button(new Rect(x, y, w, h), "Volver")) { GameSettings.Save(); pauseOptions = false; }
                return;
            }

            if (GUI.Button(new Rect(x, y, w, h), "Reanudar")) GameState.SetPaused(false);
            y += h + gap;

            GUI.enabled = SaveSystem.HasAnySave;
            if (GUI.Button(new Rect(x, y, w, h), SaveSystem.HasAnySave ? "Cargar partida" : "Cargar partida  (sin guardados)")) pauseLoadList = true;
            GUI.enabled = true;
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "Opciones")) pauseOptions = true;
            y += h + gap;

            if (GUI.Button(new Rect(x, y, w, h), "Reiniciar")) Restart();
            y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), "Menu principal")) { SaveSystem.ClearPending(); GameState.ResetAll(); SceneManager.LoadScene(GameSettings.MenuScene); }
            y += h + gap;
            if (GUI.Button(new Rect(x, y, w, h), "Salir")) QuitGame();

            GUI.Label(new Rect(0, y + h + 20, Screen.width, 30), "Guarda la partida en los telefonos de las salas seguras", center);
        }

        void DrawVictory()
        {
            GUI.color = new Color(0f, 0.12f, 0.05f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height / 2f - 90, Screen.width, 80), "HAS ESCAPADO", big);
            GUI.Label(new Rect(0, Screen.height / 2f, Screen.width, 40), "Gracias por jugar", center);
            GUI.Label(new Rect(0, Screen.height / 2f + 40, Screen.width, 40), "Pulsa R para volver a empezar", center);
        }

        void DrawHealth()
        {
            if (playerHealth == null) return;
            float pct = playerHealth.Current / playerHealth.maxHealth;
            var r = new Rect(20, Screen.height - 50, 240, 22);
            GUI.color = Color.black; GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(Color.red, Color.green, pct);
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * pct, r.height - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x, r.y - 24, 240, 24), $"Salud {Mathf.CeilToInt(playerHealth.Current)}", label);
        }

        void DrawAmmo()
        {
            if (weapons == null || weapons.Equipped == null) return;
            string txt = weapons.Reloading ? "Recargando..." : $"{weapons.MagAmmo} / {weapons.ReserveAmmo}";
            // miniatura del arma equipada junto a la municion
            var box = new Rect(Screen.width - 330, Screen.height - 86, 74, 74);
            Fill(box, new Color(0f, 0f, 0f, 0.45f));
            var icon = WeaponIcon(weapons.Equipped);
            if (icon != null) GUI.DrawTexture(new Rect(box.x + 4, box.y + 4, 66, 66), icon, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(Screen.width - 246, Screen.height - 74, 240, 30), weapons.Equipped.displayName, label);
            GUI.Label(new Rect(Screen.width - 246, Screen.height - 46, 240, 30), txt, new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold });
        }

        Texture WeaponIcon(WeaponData w)
        {
            if (inventory == null) return null;
            foreach (var s in inventory.slots)
                if (!s.IsEmpty && s.item.weapon == w && s.item.icon != null) return s.item.icon.texture;
            return null;
        }

        static float crosshairKick = -10f;
        /// <summary>El disparo abre un momento la mira (lo llama WeaponController).</summary>
        public static void CrosshairKick() { crosshairKick = Time.unscaledTime; }

        // Mira de cruz: cuatro trazos con hueco central y un punto, con borde oscuro para verse sobre cualquier fondo.
        // El hueco se abre al disparar y se cierra enseguida.
        void DrawCrosshair()
        {
            if (player == null || !player.IsAiming || GameState.InputBlocked) return;
            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            float s = Mathf.Max(1f, Screen.height / 1080f);                    // escala con la resolucion
            float kick = Mathf.Clamp01(1f - (Time.unscaledTime - crosshairKick) / 0.25f);
            float gap = (7f + 12f * kick) * s, len = 7.7f * s, th = 2f * s;   // trazos 30 % mas cortos que los 11 px iniciales
            var tex = Texture2D.whiteTexture;
            var old = GUI.color;
            for (int pass = 0; pass < 2; pass++)
            {
                float o = pass == 0 ? 1f * s : 0f;                              // pasada 0: borde oscuro, pasada 1: trazo claro
                GUI.color = pass == 0 ? new Color(0f, 0f, 0f, 0.75f) : new Color(1f, 1f, 1f, 0.95f);
                GUI.DrawTexture(new Rect(cx - th * 0.5f - o, cy - gap - len - o, th + 2f * o, len + 2f * o), tex);   // arriba
                GUI.DrawTexture(new Rect(cx - th * 0.5f - o, cy + gap - o, th + 2f * o, len + 2f * o), tex);          // abajo
                GUI.DrawTexture(new Rect(cx - gap - len - o, cy - th * 0.5f - o, len + 2f * o, th + 2f * o), tex);    // izquierda
                GUI.DrawTexture(new Rect(cx + gap - o, cy - th * 0.5f - o, len + 2f * o, th + 2f * o), tex);          // derecha
                GUI.DrawTexture(new Rect(cx - 1f * s - o, cy - 1f * s - o, 2f * s + 2f * o, 2f * s + 2f * o), tex);   // punto central
            }
            GUI.color = old;
        }

        void DrawPrompt()
        {
            var it = PlayerInteractor.Current;
            if (it == null || string.IsNullOrEmpty(it.Prompt)) return;
            GUI.Label(new Rect(0, Screen.height * 0.65f, Screen.width, 30), it.Prompt, center);
        }

        void DrawMessage()
        {
            if (Time.unscaledTime > messageUntil || string.IsNullOrEmpty(message)) return;
            GUI.Label(new Rect(0, 30, Screen.width, 30), message, center);
        }

        const int InvCols = 4;
        // arrastrar para reordenar el inventario
        int dragFrom = -1;
        bool dragging;
        Vector2 dragStart;

        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Frame(Rect r, Color c, float t = 2f)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        string ActionFor(ItemData item)
        {
            if (item.type == ItemType.Weapon) return weapons != null && weapons.Equipped == item.weapon ? "Desequipar" : "Equipar";
            if (item.type == ItemType.Healing) return "Usar";
            return null;
        }

        void DoAction(int index)
        {
            if (index < 0 || index >= inventory.slots.Length || inventory.slots[index].IsEmpty) return;
            var item = inventory.slots[index].item;
            if (item.type == ItemType.Weapon && weapons != null && weapons.Equipped == item.weapon)
            {
                weapons.Equip(null);
                Message($"{item.displayName} guardada");
            }
            else inventory.Use(index);
        }

        // InventoryKeys y DrawInventory (menu contextual, examinar, arrastrar) viven en HudInventory.cs

        // ------------------------------------------------------------------ sala segura: guardado y baul

        /// <summary>Guarda en el slot; si ya hay una partida, pide pulsar otra vez para sobrescribirla.</summary>
        void TrySave(int slot)
        {
            saveSel = slot;
            if (SaveSystem.HasSave(slot) && overwriteSlot != slot) { overwriteSlot = slot; return; }
            DoSave(slot);
        }

        void DoSave(int slot)
        {
            bool ok = SaveSystem.Save(slot);
            if (ok) GameAudio.Play(Sfx.PhoneDial, Vector3.zero, 0.8f, 1f, false);
            overwriteSlot = 0;
            GameState.SetSaveMenuOpen(false);
            Message(ok ? $"Partida guardada (slot {slot})" : "No se pudo guardar la partida");
        }

        void DrawSaveMenu()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.03f, 0.02f, 0f, 0.8f));
            float listH = SaveSlotsGUI.Height;
            var panel = new Rect(Screen.width / 2f - 260, Mathf.Max(20f, Screen.height / 2f - (listH + 170f) / 2f), 520, listH + 170f);
            Fill(panel, new Color(0.10f, 0.07f, 0.04f, 0.97f));
            Frame(panel, new Color(0.95f, 0.72f, 0.35f, 0.55f), 2f);
            var title = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = new Color(1f, 0.82f, 0.5f);
            GUI.Label(new Rect(panel.x, panel.y + 14, panel.width, 32), "TELEFONO - GUARDAR PARTIDA", title);
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            bool confirming = overwriteSlot != 0;
            GUI.Label(new Rect(panel.x, panel.y + 48, panel.width, 24),
                confirming ? $"El slot {overwriteSlot} ya tiene una partida: pulsa otra vez para sobrescribirla" : "Elige un slot  (flechas + Enter, o clic)",
                new GUIStyle(center) { alignment = TextAnchor.MiddleCenter, fontSize = 14 });
            GUI.color = Color.white;
            int clicked = SaveSlotsGUI.Draw(panel.x + 30, panel.y + 80, panel.width - 60, true, saveSel);
            if (clicked > 0) TrySave(clicked);
            if (GUI.Button(new Rect(panel.x + panel.width / 2f - 75, panel.yMax - 54, 150, 38), "Salir  (Esc)")) GameState.SetSaveMenuOpen(false);
        }

        int boxHover = -1;

        /// <summary>Baul de objetos: clic en tu inventario lo guarda; clic en el baul lo saca. Comun a todos los baules.</summary>
        void DrawItemBox()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.02f, 0.015f, 0.01f, 0.88f));
            if (inventory == null) return;
            const int size = 72, gap = 6, boxCols = 8;
            float invW = InvCols * size + (InvCols - 1) * gap;
            float boxW = boxCols * size + (boxCols - 1) * gap;
            float x0 = Screen.width / 2f - (invW + 60 + boxW) / 2f, y0 = Screen.height / 2f - 230;

            var title = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold };
            GUI.color = new Color(0.9f, 0.78f, 0.45f);
            GUI.Label(new Rect(x0, y0 - 60, 600, 32), "BAUL DE OBJETOS", title);
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(x0, y0 - 30, 900, 22), "Clic en un objeto para moverlo entre tu inventario y el baul.  Lo guardado aparece en todos los baules.   Esc/Tab: cerrar",
                new GUIStyle(label) { fontSize = 13 });
            GUI.color = Color.white;
            GUI.Label(new Rect(x0, y0, invW, 22), "INVENTARIO", new GUIStyle(label) { fontSize = 14, fontStyle = FontStyle.Bold });
            float bx = x0 + invW + 60;
            GUI.Label(new Rect(bx, y0, boxW, 22), $"BAUL  ({ItemStorage.Slots.Count}/{ItemStorage.Capacity})", new GUIStyle(label) { fontSize = 14, fontStyle = FontStyle.Bold });

            ItemData hoverItem = null;
            int hoverCount = 0;
            var e = Event.current;
            // inventario del jugador
            for (int i = 0; i < inventory.slots.Length; i++)
            {
                var r = new Rect(x0 + (i % InvCols) * (size + gap), y0 + 28 + (i / InvCols) * (size + gap), size, size);
                var s = inventory.slots[i];
                bool over = r.Contains(e.mousePosition);
                DrawSlot(r, s, over);
                if (over && !s.IsEmpty) { hoverItem = s.item; hoverCount = s.count; }
                if (over && e.type == EventType.MouseDown && !s.IsEmpty)
                {
                    int left = ItemStorage.Put(s.item, s.count);
                    int moved = s.count - left;
                    if (moved > 0)
                    {
                        if (s.item.type == ItemType.Weapon && weapons != null && weapons.Equipped == s.item.weapon) weapons.Equip(null);
                        inventory.RemoveAt(i, moved);
                        GameAudio.Play(Sfx.Pickup, player != null ? player.transform.position : Vector3.zero, 0.5f, 0.9f, false);
                    }
                    else Message("El baul esta lleno");
                    e.Use();
                }
            }
            // baul (almacen global)
            int rows = Mathf.CeilToInt(ItemStorage.Capacity / (float)boxCols);
            for (int i = 0; i < ItemStorage.Capacity; i++)
            {
                var r = new Rect(bx + (i % boxCols) * (size + gap), y0 + 28 + (i / boxCols) * (size + gap), size, size);
                var s = i < ItemStorage.Slots.Count ? ItemStorage.Slots[i] : default;
                bool over = r.Contains(e.mousePosition);
                DrawSlot(r, s, over);
                if (over && !s.IsEmpty) { hoverItem = s.item; hoverCount = s.count; }
                if (over && e.type == EventType.MouseDown && !s.IsEmpty)
                {
                    int left = inventory.TryAdd(s.item, s.count);
                    int moved = s.count - left;
                    if (moved > 0)
                    {
                        ItemStorage.RemoveAt(i, moved);
                        GameAudio.Play(Sfx.Pickup, player != null ? player.transform.position : Vector3.zero, 0.5f, 1.1f, false);
                    }
                    else Message("Inventario lleno");
                    e.Use();
                }
            }
            // nombre y descripcion del objeto bajo el raton
            float iy = y0 + 28 + 2 * (size + gap) + 20;
            if (hoverItem != null)
            {
                GUI.color = new Color(0.95f, 0.85f, 0.55f);
                GUI.Label(new Rect(x0, iy, invW + 20, 26), hoverItem.displayName + (hoverItem.maxStack > 1 ? $"  x{hoverCount}" : ""), new GUIStyle(label) { fontSize = 16, fontStyle = FontStyle.Bold });
                GUI.color = Color.white;
                GUI.Label(new Rect(x0, iy + 26, invW + 20, 80), hoverItem.description, new GUIStyle(label) { fontSize = 13, wordWrap = true });
            }
            if (GUI.Button(new Rect(x0, y0 + 28 + rows * (size + gap) - 40, 140, 34), "Cerrar  (Esc)")) GameState.SetBoxOpen(false);
        }

        void DrawSlot(Rect r, ItemStack s, bool over)
        {
            Fill(r, over ? new Color(0.22f, 0.2f, 0.12f, 0.95f) : new Color(0.1f, 0.11f, 0.12f, 0.95f));
            if (!s.IsEmpty)
            {
                if (s.item.icon != null) GUI.DrawTexture(new Rect(r.x + 5, r.y + 5, r.width - 10, r.height - 10), s.item.icon.texture, ScaleMode.ScaleToFit, true);
                else GUI.Label(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), s.item.displayName, new GUIStyle(label) { fontSize = 10, wordWrap = true });
                if (s.item.maxStack > 1)
                    GUI.Label(new Rect(r.x, r.y, r.width - 5, r.height - 2), s.count.ToString(), new GUIStyle(label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight });
                if (s.item.type == ItemType.Weapon && weapons != null && weapons.Equipped == s.item.weapon)
                {
                    var eq = new Rect(r.x + 3, r.y + 3, 18, 16);
                    Fill(eq, new Color(0.85f, 0.65f, 0.15f));
                    GUI.Label(eq, "E", new GUIStyle(label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
                }
            }
            Frame(r, over ? new Color(1f, 0.82f, 0.3f) : new Color(1f, 1f, 1f, 0.12f), over ? 2f : 1f);
        }

        void DrawDeath()
        {
            GUI.color = new Color(0.4f, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 80), "HAS MUERTO", big);
            GUI.Label(new Rect(0, Screen.height / 2f + 20, Screen.width, 40), "Pulsa R para reintentar", center);
        }
    }
}
