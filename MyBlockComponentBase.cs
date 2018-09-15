using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Cheetah.LaserTools
{
    public abstract class MyEntityComponentBase<EntityType> : MyGameLogicComponent where EntityType: IMyEntity
    {
        public EntityType MyEntity;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyEntity = (EntityType)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            AfterInit();
        }

        protected abstract void AfterInit();
    }

    public abstract class MyTerminalBlockComponentBase<BlockType> : MyEntityComponentBase<IMyCubeBlock> where BlockType: IMyTerminalBlock
    {
        public BlockType MyBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyEntity = (BlockType)Entity;
            MyBlock = (BlockType)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            AfterInit();
        }
    }

    public abstract class MyTerminalSavingBlockBase<BlockType> : MyTerminalBlockComponentBase<BlockType> where BlockType : IMyTerminalBlock
    {

    }
}
