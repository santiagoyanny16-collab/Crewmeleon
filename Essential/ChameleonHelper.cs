using FungleAPI.Assets.Late;
using FungleAPI.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Rewired.Demos.CustomPlatform.MyPlatformControllerExtension;

namespace Crewmeleon.Essential
{
    public static class ChameleonHelper
    {
        private static Texture2D texture;
        public static Color32 BrushColor = Color.white;
        public static PaintState PaintState;
        public static Color32 GetMousePixelColor()
        {
            if (texture == null)
            {
                texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            }

            Vector2 pos = Input.mousePosition;

            texture.ReadPixels(new Rect(pos.x, pos.y, 1, 1), 0, 0, false);

            Color32 color = texture.GetPixelData<Color32>(0)[0];
            color.a = byte.MaxValue;

            return color;
        }
    }
}
