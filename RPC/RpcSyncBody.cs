using Crewmeleon.Components;
using Crewmeleon.Roles;
using FungleAPI.Base.Rpc;
using FungleAPI.Role;
using FungleAPI.Utilities;
using Hazel;
using Il2CppInterop.Generator.Extensions;
using Il2CppSystem;
using Sentry.Unity.NativeUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Crewmeleon.RPC
{
    internal class RpcSyncBody : SimpleRpc<PlayerControl>
    {
        public override void Write(MessageWriter messageWriter)
        {
            IEnumerable<KeyValuePair<Vector2Int, PaintStroke>> batch = CanvaPaintBehaviour.Instance.Canva.Strokes.Take(80);

            messageWriter.Write((byte)batch.Count());
            foreach (KeyValuePair<Vector2Int, PaintStroke> pair in batch)
            {
                CanvaPaintBehaviour.Instance.Canva.Strokes.Remove(pair.Key);

                ushort index = (ushort)(pair.Key.y * 165 + pair.Key.x);
                messageWriter.Write(index);
                messageWriter.Write(pair.Value.Radius);
                messageWriter.Write(pair.Value.Color.r);
                messageWriter.Write(pair.Value.Color.g);
                messageWriter.Write(pair.Value.Color.b);
            }
        }
        public override void Handle(PlayerControl innerNetObject, MessageReader messageReader)
        {
            if (innerNetObject.Data.RoleType != CustomRoleManager.GetRoleType<ChameleonRole>()) return;

            CanvaBehaviour canvaBehaviour = innerNetObject.GetComponent<CanvaBehaviour>();

            Texture2D texture2D = canvaBehaviour.Canva.sprite.texture;

            byte pixelCount = messageReader.ReadByte();
            bool painted = false;

            for (int i = 0; i < pixelCount; i++)
            {
                ushort index = messageReader.ReadUInt16();

                int x = index % 165;
                int y = index / 165;
                byte size = messageReader.ReadByte();
                byte r = messageReader.ReadByte();
                byte g = messageReader.ReadByte();
                byte b = messageReader.ReadByte();

                if (canvaBehaviour.PaintBrush(new Vector2Int(x, y), new Color32(r, g, b, byte.MaxValue), size))
                {
                    painted = true;
                }
            }

            if (painted)
            {
                texture2D.SetPixels32(canvaBehaviour.TextureBuffer);
                texture2D.Apply(false);
            }
        }
    }
}