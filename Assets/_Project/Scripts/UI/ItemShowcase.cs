namespace Horror
{
    /// <summary>
    /// Pantalla de "objeto conseguido": el modelo 3D girando con su nombre y lo que da, como al recoger una mejora.
    /// La dibuja el Hud (parcial HudArchive) reutilizando el visor del inventario (ItemPreview).
    /// </summary>
    public static class ItemShowcase
    {
        public static ItemData Item { get; private set; }
        public static string Detail { get; private set; } = "";

        public static void Open(ItemData item, string detail)
        {
            if (item == null) return;
            Item = item; Detail = detail ?? "";
            GameState.SetShowcaseOpen(true);
        }

        public static void Close()
        {
            Item = null; Detail = "";
            ItemPreview.Get().Show(null);   // apaga la camara del visor
            GameState.SetShowcaseOpen(false);
        }
    }
}
