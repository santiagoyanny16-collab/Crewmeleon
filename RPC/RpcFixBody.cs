using FungleAPI.Base.Rpc;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewmeleon.RPC
{
    internal class RpcFixBody : SimpleRpc<PlayerControl>
    {
        public override void Write(PlayerControl innerNetObject, MessageWriter messageWriter)
        {
            messageWriter.Write(innerNetObject.transform.position.x);
            messageWriter.Write(innerNetObject.transform.position.y);
            messageWriter.Write(innerNetObject.cosmetics.FlipX);
        }
        public override void Handle(PlayerControl innerNetObject, MessageReader messageReader)
        {
            float x = messageReader.ReadSingle();
            float y = messageReader.ReadSingle();
            bool flip = messageReader.ReadBoolean();
            innerNetObject.NetTransform.SnapTo(new UnityEngine.Vector2(x, y));
            innerNetObject.cosmetics.SetFlipX(flip);
        }
    }
}
