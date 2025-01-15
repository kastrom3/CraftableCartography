using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CraftableCartography
{
    public class SavedPositions
    {
        public BlockPos pos;
        public float zoomLevel;

        public SavedPositions(ICoreAPI api)
        {
            pos = api.World.DefaultSpawnPosition.AsBlockPos;
            zoomLevel = 1f;
        }
    }
}
