using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Scripts.KSWH
{
    class EnemyShip
    {
        public Sandbox.ModAPI.IMyCubeGrid Ship;
        private IMyControllableEntity m_cockpit;
        public IMyEntity Target;
        public int Range = 500;

        public EnemyShip(IMyCubeGrid ship, int agroRange)
        {
            Ship = ship;
            Range = agroRange;
            var lst = new List<IMySlimBlock>();
            ship.GetBlocks(lst, (x) => x.FatBlock is IMyControllableEntity);
            m_cockpit = lst.FirstOrDefault().FatBlock as IMyControllableEntity;
        }

        public void Update(int count)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
            if (Target == null)
            {
                if (count % 100 == 0)
                    FindTarget();
                return;
            }
            var dir = Target.GetPosition() - Ship.GetPosition();
            if (dir.Length() < 10)
                return;
            var dirNorm = Vector3.Normalize(dir);
            var x = -(m_cockpit as IMyEntity).WorldMatrix.Up.Dot(dirNorm);
            var y = -(m_cockpit as IMyEntity).WorldMatrix.Left.Dot(dirNorm);
            var forw = (m_cockpit as IMyEntity).WorldMatrix.Forward.Dot(dirNorm);

            if (forw < 0)
                y = 0;
            if (Math.Abs(x) < 0.2f)
                x = 0;
            if (Math.Abs(y) < 0.2f)
                y = 0;
            if (dir.Length() < 100)
                dir = Vector3.Zero;
            else
                dir = Vector3.TransformNormal(dir, (m_cockpit as IMyEntity).WorldMatrixNormalizedInv);

            var rot = new Vector2(x, y) * 10f;
            if (dir.LengthSquared() > 0 || rot.LengthSquared() > 0)
                m_cockpit.MoveAndRotate(dir, rot, 0);
            else
                m_cockpit.MoveAndRotateStopped();
        }

        private void FindTarget()
        {
            var bs = new BoundingSphere(Ship.GetPosition(), Range);
            Target = MissionComponent.GetPlayerInSphere(ref bs);
            if (Target != null)
            {
                if (Ship.DisplayName.Contains("Drone"))
                    MyAPIGateway.Utilities.ShowMessage("", "Enemy detected, guard drone activated");
                DisableOverride();
            }
        }

        private void DisableOverride()
        {
            MissionComponent.Logger.WriteLine(string.Format("Disabling override {0}", Ship.DisplayName));
            var lst = new List<IMySlimBlock>();
            Ship.GetBlocks(lst, (x) => x.FatBlock != null && x.FatBlock.BlockDefinition.SubtypeId.Contains("Gyro"));
            var gyro = lst.FirstOrDefault();
            var actions = new List<ITerminalAction>();
            MyAPIGateway.TerminalActionsHelper.GetActions(gyro.FatBlock.GetType(), actions, (x) => x.Id.Contains("Override"));
            var action = actions.FirstOrDefault();
            foreach (var block in lst)
                action.Apply(block.FatBlock);
        }
    }
}
