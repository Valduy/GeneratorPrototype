using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace GameEngine.Game
{
    public class Window : GameWindow
    {
        public Window(int width, int height, string title = "")
            : base(GameWindowSettings.Default, CreateWindowSettings(width, height, title))
        { }
        
        private static NativeWindowSettings CreateWindowSettings(int width, int height, string title) => new()
        {
            Size = new Vector2i(width, height),
            Title = title,
        };
    }
}
