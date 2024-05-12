namespace Demo.Scripts
{
    public static class ResourceKey
    {
        private const string PrefabFormat = "prefab_demo_{0}";
        private const string ColorRedScreenPrefabName = "screen_color_red";
        private const string ColorBlueScreenPrefabName = "screen_color_blue";
        private const string TopScreenPrefabName = "screen_top";
        private const string HomeScreenPrefabName = "screen_home";
        private const string HomeLoadingScreenPrefabName = "screen_home_loading";
        private const string ShopScreenPrefabName = "screen_shop";
        private const string SettingsModalPrefabName = "modal_settings";
        private const string CharacterModalPrefabName = "modal_character";
        private const string ShopItemGridSheetPrefabName = "sheet_shop_item_grid";
        private const string CharacterModalImageSheetPrefabName = "sheet_character_modal_image";
        private const string CharacterImageModalPrefabName = "modal_character_image";
        private const string LoadingActivityPrefabName = "activity_loading";
        private const string ShopItemPrefabName = "control_shop_item";

        private const string CharacterImageFormat = "tex_character_{0:D3}_{1}";
        private const string CharacterThumbnailFormat = "tex_character_thumb_{0:D3}_{1}";

        public static string ColorRedScreenPrefab()
        {
            return string.Format(PrefabFormat, ColorRedScreenPrefabName);
        }
        
        public static string ColorBlueScreenPrefab()
        {
            return string.Format(PrefabFormat, ColorBlueScreenPrefabName);
        }
        
        public static string TopScreenPrefab()
        {
            return string.Format(PrefabFormat, TopScreenPrefabName);
        }

        public static string HomeScreenPrefab()
        {
            return string.Format(PrefabFormat, HomeScreenPrefabName);
        }

        public static string HomeLoadingScreenPrefab()
        {
            return string.Format(PrefabFormat, HomeLoadingScreenPrefabName);
        }

        public static string ShopScreenPrefab()
        {
            return string.Format(PrefabFormat, ShopScreenPrefabName);
        }

        public static string SettingsModalPrefab()
        {
            return string.Format(PrefabFormat, SettingsModalPrefabName);
        }

        public static string CharacterModalPrefab()
        {
            return string.Format(PrefabFormat, CharacterModalPrefabName);
        }

        public static string ShopItemGridSheetPrefab()
        {
            return string.Format(PrefabFormat, ShopItemGridSheetPrefabName);
        }

        public static string CharacterModalImageSheetPrefab()
        {
            return string.Format(PrefabFormat, CharacterModalImageSheetPrefabName);
        }

        public static string CharacterImageModalPrefab()
        {
            return string.Format(PrefabFormat, CharacterImageModalPrefabName);
        }

        public static string CharacterSprite(int characterId, int rank)
        {
            return string.Format(CharacterImageFormat, characterId, rank);
        }

        public static string CharacterThumbnailSprite(int characterId, int rank)
        {
            return string.Format(CharacterThumbnailFormat, characterId, rank);
        }

        public static string LoadingActivity()
        {
            return string.Format(PrefabFormat, LoadingActivityPrefabName);
        }

        public static string ShopItemControlPrefab()
        {
            return string.Format(PrefabFormat, ShopItemPrefabName);
        }
    }
}