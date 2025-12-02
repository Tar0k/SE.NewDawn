using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    public class TestDrawPanel
    {
        private readonly IMyTextSurface _textSurface;
        private readonly RectangleF _viewport;
        
        public TestDrawPanel(IMyTextSurface textPanel)
        {
            _textSurface = textPanel;
            _viewport = new RectangleF(
                (_textSurface.TextureSize - _textSurface.SurfaceSize) / 2f,
                _textSurface.SurfaceSize
            );
        }


        public void DrawSprites()
        {
            var frame = _textSurface.DrawFrame();
            var position = new Vector2(256, 20) + _viewport.Position;
            var sprite = MySprite.CreateClipRect(new Rectangle(0, 0, (int)position.X, (int)position.Y + 16));
            frame.Add(sprite);
            sprite = new MySprite
            {
                Type = SpriteType.TEXT,
                Data = "Col1 NAME",
                Position = position,
                Size = _viewport.Size,
                Color = Color.White.Alpha(0.66f),
                Alignment = TextAlignment.LEFT
            };
            frame.Add(sprite);
            
            
            frame.Dispose();
        }
        
    }
}