using Craft.Net.Data.Blocks;

namespace Craft.Net.Data.Items
{
    
    public class NetherWartItem : Item
    {
        public override short Id
        {
            get
            {
                return 372;
            }
        }

        public override void OnItemUsedOnBlock(World world, Vector3 clickedBlock, Vector3 clickedSide, Vector3 cursorPosition, Entities.Entity usedBy)
        {
            if (world.GetBlock(clickedBlock) is SoulSandBlock && world.GetBlock(clickedBlock + clickedSide) == 0)
            {
                var seeds = new NetherWartBlock();
                world.SetBlock(clickedBlock + clickedSide, seeds);
                seeds.OnBlockPlaced(world, clickedBlock + clickedSide, clickedBlock, clickedSide, cursorPosition, usedBy);
            }
        }
    }
}
