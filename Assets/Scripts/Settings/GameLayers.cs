using UnityEngine;

namespace Settings {
    public static class GameLayers {

        #region Layer Names

        public const string UILayerName = "UI";
        public const string MapLayerName = "Map";
        public const string ObstaclesLayerName = "Obstacles";
        public const string GroundLayerName = "Ground";
        public const string PlayerLayerName = "Player";
        public const string EnemiesLayerName = "Enemies";
        public const string NpcLayerName = "Npc";
        public const string InteractableObjectLayerName = "InteractableObject";
        public const string PortalLayerName = "Portal";
        public const string AlliesLayerName = "Allies";
        public const string PetLayerName = "Pet";

        #endregion

        #region Layer indices & Masks

        public static readonly int UILayer;
        public static readonly int MapLayer;
        public static readonly int ObstaclesLayer;
        public static readonly int GroundLayer;
        public static readonly int PlayerLayer;
        public static readonly int EnemiesLayer;
        public static readonly int NpcLayer;
        public static readonly int InteractableObjectLayer;
        public static readonly int PortalLayer;
        public static readonly int AlliesLayer;
        public static readonly int PetLayer;
        
        public static readonly int ObstaclesAndMapMask; // 조합된 마스크
        public static readonly int AllCharactersMask;

        #endregion

        #region Sorting Layer IDs

        public static readonly int DefaultSortingID;
        public static readonly int Layer1SortingID;
        public static readonly int Layer2SortingID;
        public static readonly int Layer3SortingID;
        public static readonly int UISortingID;

        #endregion

        static GameLayers() {
            UILayer = LayerMask.NameToLayer(UILayerName);
            MapLayer = LayerMask.NameToLayer(MapLayerName);
            ObstaclesLayer = LayerMask.NameToLayer(ObstaclesLayerName);
            GroundLayer = LayerMask.NameToLayer(GroundLayerName);
            PlayerLayer = LayerMask.NameToLayer(PlayerLayerName);
            EnemiesLayer = LayerMask.NameToLayer(EnemiesLayerName);
            NpcLayer = LayerMask.NameToLayer(NpcLayerName);
            InteractableObjectLayer = LayerMask.NameToLayer(InteractableObjectLayerName);
            PortalLayer = LayerMask.NameToLayer(PortalLayerName);
            AlliesLayer = LayerMask.NameToLayer(AlliesLayerName);
            PetLayer = LayerMask.NameToLayer(PetLayerName);

            ObstaclesAndMapMask = (1 << ObstaclesLayer) | (1 << MapLayer);
            AllCharactersMask = (1 << PlayerLayer) | (1 << EnemiesLayer) | (1 << NpcLayer) | (1 << AlliesLayer) | (1 << PetLayer);
            
            // TODO
        }
    }
}