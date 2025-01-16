using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CraftableCartography
{
    public class SavedPositions
    {
        public BlockPos pos;
        public float zoomLevel;

        public SavedPositions() { }

        public SavedPositions(ICoreAPI api)
        {
            pos = api.World.DefaultSpawnPosition.AsBlockPos;
            zoomLevel = 1f;
        }

        public SavedPositions(BlockPos pos, float zoomLevel)
        {
            this.pos = pos;
            this.zoomLevel = zoomLevel;
        }
    }
}
