using UnityEngine;
using UnityEngine.InputSystem;

namespace Horror
{
    /// <summary>
    /// Inventario (estilo RE2): la escena se ve de fondo oscurecida; el objeto seleccionado se muestra en 3D SIN fondo a la izquierda,
    /// la rejilla a la derecha y debajo el nombre, el tipo y la descripcion. Al hacer clic en un objeto sale un menu con sus acciones
    /// (Equipar/Usar, Examinar, Tirar, Salir). Examinar muestra solo el objeto, centrado y grande, que se gira arrastrando con el
    /// raton (rueda: acercar) para buscar pistas. Arrastrar un objeto a otra casilla lo reordena (Inventory.Move).
    /// </summary>
    public partial class Hud
    {
        // menu contextual y examen
        bool invMenu, examining;
        int invMenuIdx, invMenuSel;
        float examYaw = 25f, examPitch = 10f, examZoom = 1.35f;

        static string TypeLabel(ItemData it)
        {
            switch (it.type)
            {
                case ItemType.Weapon: return "Arma";
                case ItemType.Ammo: return "Munición";
                case ItemType.Healing: return "Curación";
                case ItemType.Key: return "Llave";
                case ItemType.Bag: return "Riñonera";
                default: return "Objeto";
            }
        }

        void CloseInventoryPanels()
        {
            invMenu = false; examining = false; dragFrom = -1; dragging = false;
            var pv = ItemPreview.Get(); pv.Manual = false;
        }

        /// <summary>Opciones del menu de un objeto (el texto es la propia accion).</summary>
        string[] MenuOptions(ItemData item)
        {
            string act = ActionFor(item);
            bool canDrop = !item.IsKey || KeyUsage.IsSpent(item);           // un objeto clave solo se tira cuando ya esta usado por completo
            if (act != null) return canDrop ? new[] { act, "Examinar", "Tirar", "Salir" } : new[] { act, "Examinar", "Salir" };
            return canDrop ? new[] { "Examinar", "Tirar", "Salir" } : new[] { "Examinar", "Salir" };
        }

        void RunMenuOption(int idx, string option)
        {
            invMenu = false;
            switch (option)
            {
                case "Examinar": examining = true; examYaw = 25f; examPitch = 10f; examZoom = 1.35f; break;
                case "Tirar": inventory.Drop(idx); break;
                case "Salir": break;
                default: DoAction(idx); break;            // Equipar / Desequipar / Usar
            }
        }

        /// <summary>Navegacion por teclado dentro del inventario (flechas/WASD, Enter/E abre el menu, X tirar, 1-4 atajos de arma).</summary>
        void InventoryKeys(Keyboard kb)
        {
            if (inventory == null) return;
            int n = inventory.slots.Length;
            if (selected < 0) selected = 0;
            if (examining) return;                                           // al examinar solo se gira con el raton (Esc/clic derecho salen)

            if (invMenu)
            {
                var item = invMenuIdx >= 0 && invMenuIdx < n && !inventory.slots[invMenuIdx].IsEmpty ? inventory.slots[invMenuIdx].item : null;
                if (item == null) { invMenu = false; return; }
                var opts = MenuOptions(item);
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) invMenuSel = (invMenuSel + 1) % opts.Length;
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) invMenuSel = (invMenuSel - 1 + opts.Length) % opts.Length;
                if (kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) RunMenuOption(invMenuIdx, opts[invMenuSel]);
                return;
            }

            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) selected = (selected + 1) % n;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) selected = (selected - 1 + n) % n;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) selected = (selected + InvCols) % n;
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) selected = (selected - InvCols + n) % n;
            if ((kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) && !inventory.slots[selected].IsEmpty) { invMenu = true; invMenuIdx = selected; invMenuSel = 0; }
            if (kb.xKey.wasPressedThisFrame) inventory.Drop(selected);
            // 1-4: asigna el arma seleccionada a esa tecla de atajo (la misma tecla otra vez la libera)
            var digits = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key };
            for (int d = 0; d < digits.Length; d++)
            {
                if (!digits[d].wasPressedThisFrame) continue;
                if (selected < 0 || selected >= n || inventory.slots[selected].IsEmpty || inventory.slots[selected].item.type != ItemType.Weapon)
                { Message("Selecciona un arma para asignarle una tecla"); continue; }
                var wi = inventory.slots[selected].item;
                Message(WeaponHotkeys.Assign(d, wi) ? wi.displayName + " asignada a la tecla " + (d + 1) : "Tecla " + (d + 1) + " liberada");
            }
        }

        void DrawInventory()
        {
            float u = Mathf.Max(0.7f, Screen.height / 1080f);                    // escala con la resolucion
            // La escena sigue visible detras: velo oscuro, mas denso a la derecha donde esta la rejilla
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.01f, 0.015f, 0.02f, 0.5f));
            Fill(new Rect(Screen.width * 0.5f, 0, Screen.width * 0.5f, Screen.height), new Color(0.01f, 0.015f, 0.02f, 0.45f));
            if (!examining) DrawInvTabs();
            if (mapTab) { CloseInventoryPanels(); DrawMap(); ItemPreview.Get().Show(null); return; }
            if (archiveTab) { CloseInventoryPanels(); DrawArchive(); ItemPreview.Get().Show(null); return; }
            if (inventory == null) return;

            int n = inventory.slots.Length;
            ItemData selItem = selected >= 0 && selected < n && !inventory.slots[selected].IsEmpty ? inventory.slots[selected].item : null;
            var preview = ItemPreview.Get();
            preview.Show(selItem);

            // ---------------------------------------------------------------- examinar: solo el objeto, centrado y girable
            if (examining)
            {
                if (selItem == null) { examining = false; preview.Manual = false; }
                else { DrawExamine(selItem, preview, u); return; }
            }
            preview.Manual = false;

            // ---------------------------------------------------------------- rejilla (derecha)
            float size = 84f * u, gap = 7f * u;
            float gridW = InvCols * size + (InvCols - 1) * gap;
            int rows = Mathf.Max(3, (n + InvCols - 1) / InvCols);
            float gx = Screen.width * 0.58f, gy = Mathf.Max(100f * u, Screen.height * 0.17f);

            // objeto 3D a la izquierda, sin fondo ni marco
            if (selItem != null)
            {
                float ps = Mathf.Min(Screen.height * 0.78f, Screen.width * 0.5f);
                GUI.DrawTexture(new Rect(Screen.width * 0.09f, Screen.height * 0.5f - ps * 0.55f, ps, ps), preview.Texture, ScaleMode.ScaleToFit, true);
            }

            var title = new GUIStyle(label) { fontSize = Mathf.RoundToInt(20 * u), fontStyle = FontStyle.Bold };
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Label(new Rect(gx, gy - 38f * u, 400, 30f * u), "INVENTARIO", title);
            GUI.color = Color.white;

            Rect menuRect = default;
            string[] opts = null;
            if (invMenu && invMenuIdx >= 0 && invMenuIdx < n && !inventory.slots[invMenuIdx].IsEmpty)
            {
                opts = MenuOptions(inventory.slots[invMenuIdx].item);
                float mw = 168f * u, mh = opts.Length * 38f * u + 6f;
                var sr = new Rect(gx + (invMenuIdx % InvCols) * (size + gap), gy + (invMenuIdx / InvCols) * (size + gap), size, size);
                float mx = sr.xMax + 8f * u; if (mx + mw > Screen.width - 8f) mx = sr.x - mw - 8f * u;
                menuRect = new Rect(mx, Mathf.Min(sr.y, Screen.height - mh - 8f), mw, mh);
            }
            else invMenu = false;

            var countStyle = new GUIStyle(label) { fontSize = Mathf.RoundToInt(15 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight };
            var nameStyle = new GUIStyle(label) { fontSize = Mathf.RoundToInt(11 * u), alignment = TextAnchor.UpperLeft, wordWrap = true };
            int hover = -1;
            Vector2 mouse = Event.current.mousePosition;
            bool overMenu = invMenu && menuRect.Contains(mouse);
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(gx + (i % InvCols) * (size + gap), gy + (i / InvCols) * (size + gap), size, size);
                var s = inventory.slots[i];
                bool sel = i == selected;
                if (!overMenu && r.Contains(mouse)) hover = i;
                bool beingDragged = dragging && i == dragFrom;
                Fill(r, sel ? new Color(0.16f, 0.17f, 0.18f, 0.92f) : new Color(0.07f, 0.08f, 0.09f, 0.82f));
                if (!s.IsEmpty)
                {
                    if (beingDragged) GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    if (s.item.icon != null)
                        GUI.DrawTexture(new Rect(r.x + 5 * u, r.y + 5 * u, size - 10 * u, size - 10 * u), s.item.icon.texture, ScaleMode.ScaleToFit, true);
                    else
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.8f);
                        GUI.Label(new Rect(r.x + 5, r.y + 5, size - 10, 40), s.item.displayName, nameStyle);
                    }
                    GUI.color = Color.white;
                    if (s.item.maxStack > 1) GUI.Label(new Rect(r.x, r.y, size - 6, size - 2), s.count.ToString(), countStyle);
                    if (s.item.type == ItemType.Weapon && weapons != null && weapons.Equipped == s.item.weapon)
                    {
                        var eq = new Rect(r.x + 3, r.y + 3, 20 * u, 18 * u);
                        Fill(eq, new Color(0.82f, 0.82f, 0.8f));
                        GUI.color = new Color(0.1f, 0.1f, 0.1f);
                        GUI.Label(eq, "E", new GUIStyle(label) { fontSize = Mathf.RoundToInt(12 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
                        GUI.color = Color.white;
                    }
                    if (s.item.IsKey && KeyUsage.IsSpent(s.item)) DrawRedCheck(new Rect(r.xMax - 31f * u, r.y + 4f * u, 27f * u, 27f * u));       // usado por completo: ya se puede tirar
                    int hk = s.item.type == ItemType.Weapon ? WeaponHotkeys.SlotOf(s.item) : -1;
                    if (hk >= 0)                                                                    // insignia con la tecla de atajo
                    {
                        var kr = new Rect(r.xMax - 22 * u, r.y + 3, 19 * u, 19 * u);
                        Fill(kr, new Color(0.15f, 0.45f, 0.2f, 0.95f));
                        GUI.Label(kr, (hk + 1).ToString(), new GUIStyle(label) { fontSize = Mathf.RoundToInt(13 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
                    }
                }
                Frame(r, sel ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.16f), sel ? 2f : 1f);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !overMenu && r.Contains(mouse))
                {
                    if (invMenu && invMenuIdx != i) invMenu = false;
                    selected = i;
                    if (!s.IsEmpty) { dragFrom = i; dragging = false; dragStart = mouse; }   // clic: abre el menu al soltar; arrastre: reordena
                    else invMenu = false;
                    Event.current.Use();
                }
            }
            // linea fina encima de la rejilla (como el original)
            Fill(new Rect(gx, gy - 6f * u, gridW, 1f), new Color(1f, 1f, 1f, 0.35f));

            // ---------------------------------------------------------------- arrastrar / soltar / clic
            if (dragFrom >= 0)
            {
                var ev = Event.current;
                bool pressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
                if (!dragging && Vector2.Distance(ev.mousePosition, dragStart) > 6f * u && pressed) dragging = true;
                bool released = ev.rawType == EventType.MouseUp || (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame);
                if (released)
                {
                    if (dragging)
                    {
                        if (hover >= 0 && hover != dragFrom && inventory.Move(dragFrom, hover)) selected = hover;
                    }
                    else if (hover == dragFrom)                                                      // clic sin arrastre: menu del objeto
                    {
                        if (invMenu && invMenuIdx == dragFrom) invMenu = false;
                        else { invMenu = true; invMenuIdx = dragFrom; invMenuSel = 0; }
                    }
                    dragFrom = -1; dragging = false;
                }
                else if (dragging)
                {
                    if (hover >= 0 && hover != dragFrom)
                    {
                        var tr = new Rect(gx + (hover % InvCols) * (size + gap), gy + (hover / InvCols) * (size + gap), size, size);
                        Frame(tr, new Color(0.4f, 1f, 0.5f), 3f);
                    }
                    var ds = inventory.slots[dragFrom];
                    if (!ds.IsEmpty && ds.item.icon != null && ev.type == EventType.Repaint)
                    {
                        var dr = new Rect(ev.mousePosition.x - size * 0.4f, ev.mousePosition.y - size * 0.4f, size * 0.8f, size * 0.8f);
                        GUI.color = new Color(1f, 1f, 1f, 0.9f);
                        GUI.DrawTexture(dr, ds.item.icon.texture, ScaleMode.ScaleToFit, true);
                        GUI.color = Color.white;
                    }
                }
            }

            // ---------------------------------------------------------------- nombre, tipo y descripcion bajo la rejilla
            float iy = Mathf.Max(gy + rows * (size + gap) + 26f * u, Screen.height * 0.6f);
            float iw = Mathf.Max(gridW + 120f * u, 520f * u);
            Fill(new Rect(gx, iy - 6f * u, iw, 1f), new Color(1f, 1f, 1f, 0.3f));
            if (selItem != null)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(gx, iy, iw, 36f * u), selItem.displayName, new GUIStyle(label) { fontSize = Mathf.RoundToInt(28 * u), fontStyle = FontStyle.Normal });
                string sub = TypeLabel(selItem);
                if (selItem.type == ItemType.Weapon && selItem.weapon != null && weapons != null)
                {
                    int mag = 0;
                    foreach (var kv in weapons.Magazines) if (kv.Key == selItem.weapon) mag = kv.Value;
                    sub += "  ·  Cargador " + mag + "/" + selItem.weapon.magazineSize;
                }
                else if (selItem.maxStack > 1) sub += "  ·  x" + inventory.slots[selected].count;
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                GUI.Label(new Rect(gx, iy + 36f * u, iw, 24f * u), sub, new GUIStyle(label) { fontSize = Mathf.RoundToInt(16 * u), fontStyle = FontStyle.Italic });
                Fill(new Rect(gx, iy + 64f * u, iw, 1f), new Color(1f, 1f, 1f, 0.25f));
                GUI.color = new Color(1f, 1f, 1f, 0.92f);
                GUI.Label(new Rect(gx, iy + 74f * u, iw - 20f, 150f * u), selItem.description + (selItem.IsKey ? "\n\n" + KeyUsage.Note(selItem) : ""), new GUIStyle(label) { fontSize = Mathf.RoundToInt(19 * u), wordWrap = true });
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.Label(new Rect(gx, iy, iw, 36f * u), "Casilla vacía", new GUIStyle(label) { fontSize = Mathf.RoundToInt(24 * u) });
                GUI.color = Color.white;
            }
            // ayuda de teclas (abajo a la derecha)
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(gx - 60f * u, Screen.height - 46f * u, iw + 60f * u, 24f * u), "Clic: opciones   ·   Arrastrar: reordenar   ·   1-4: atajo de arma   ·   X: tirar   ·   Tab: cerrar", new GUIStyle(label) { fontSize = Mathf.RoundToInt(13 * u) });
            GUI.color = Color.white;

            // ---------------------------------------------------------------- menu del objeto
            if (invMenu && opts != null)
            {
                Fill(menuRect, new Color(0.05f, 0.055f, 0.06f, 0.97f));
                Frame(menuRect, new Color(1f, 1f, 1f, 0.45f), 1f);
                for (int k = 0; k < opts.Length; k++)
                {
                    var br = new Rect(menuRect.x + 3f, menuRect.y + 3f + k * 38f * u, menuRect.width - 6f, 36f * u);
                    bool hl = k == invMenuSel || br.Contains(mouse);
                    if (hl) { Fill(br, new Color(0.24f, 0.26f, 0.28f, 0.98f)); Frame(br, new Color(1f, 1f, 1f, 0.7f), 1f); }
                    var ms = new GUIStyle(label) { fontSize = Mathf.RoundToInt(18 * u), alignment = TextAnchor.MiddleLeft };
                    ms.normal.textColor = hl ? Color.white : new Color(1f, 1f, 1f, 0.75f);
                    GUI.Label(new Rect(br.x + 12f, br.y, br.width - 12f, br.height), opts[k], ms);
                    if (br.Contains(mouse)) invMenuSel = k;
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && br.Contains(mouse))
                    {
                        Event.current.Use();
                        RunMenuOption(invMenuIdx, opts[k]);
                        break;
                    }
                }
                // clic fuera del menu y fuera de las casillas: lo cierra
                if (Event.current.type == EventType.MouseDown && !menuRect.Contains(mouse) && hover < 0) invMenu = false;
            }
        }

        /// <summary>Examinar: solo el objeto 3D, centrado y grande; se gira arrastrando con el raton y se acerca con la rueda.</summary>
        void DrawExamine(ItemData item, ItemPreview preview, float u)
        {
            preview.Manual = true;
            var ev = Event.current;
            if (ev.type == EventType.MouseDrag && ev.button == 0)
            {
                examYaw -= ev.delta.x * 0.6f;
                examPitch = Mathf.Clamp(examPitch + ev.delta.y * 0.6f, -85f, 85f);
                ev.Use();
            }
            if (ev.type == EventType.ScrollWheel) { examZoom = Mathf.Clamp(examZoom - ev.delta.y * 0.06f, 0.8f, 2.6f); ev.Use(); }
            if (ev.type == EventType.MouseDown && ev.button == 1) { examining = false; preview.Manual = false; ev.Use(); return; }
            preview.Yaw = examYaw; preview.Pitch = examPitch; preview.Zoom = examZoom;

            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.01f, 0.015f, 0.02f, 0.45f));
            float ps = Mathf.Min(Screen.height * 0.9f, Screen.width * 0.8f);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - ps * 0.5f, Screen.height * 0.5f - ps * 0.5f, ps, ps), preview.Texture, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.Label(new Rect(0, 24f * u, Screen.width, 40f * u), item.displayName, new GUIStyle(center) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(26 * u) });
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0, Screen.height - 54f * u, Screen.width, 28f * u), "Arrastra con el ratón para girar el objeto   ·   Rueda: acercar   ·   Esc o clic derecho: salir", new GUIStyle(center) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(15 * u) });
            GUI.color = Color.white;
        }
    }
}
