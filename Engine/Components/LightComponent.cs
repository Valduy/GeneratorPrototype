using GameEngine.Core;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class LightComponent : Component
    {
        public override void GameUpdate(FrameEventArgs args)
        {
            GameObject!.Engine.Light.Position = GameObject!.Engine.Camera.Position;
        }
    }
}
