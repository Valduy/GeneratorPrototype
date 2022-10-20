using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PatternDemo
{
    public class DebugerComponent : Component
    {
        private bool _textureFlag;

        public Texture DetailedTexture { get; set; }
        public Texture LogicalTexture { get; set; }
        public GameObject Model { get; set; }
        public MeshTopology.MeshTopology MeshTopology { get; set; }

        public override void Start()
        {
            base.Start();
            _textureFlag = false;
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (Inputs!.IsKeyPressed(Keys.R))
            {
                Model.Get<MaterialRenderComponent>()!.Texture = _textureFlag ? DetailedTexture : LogicalTexture;
                _textureFlag = !_textureFlag;
            }

            if (Inputs!.IsKeyPressed(Keys.T))
            {
                var position = Camera!.Position;
                var smallestDistance = float.PositiveInfinity;
                int index = 0;

                for (int i = 0; i < MeshTopology.Count; i++)
                {
                    var centroid = MeshTopology[i].Face.Centroid();
                    var distanceToFace = Vector3.Distance(position, centroid);

                    if (Vector3.Distance(position, centroid) < smallestDistance)
                    {
                        smallestDistance = distanceToFace;
                        index = i;
                    }
                }

                Console.WriteLine(index);
                Engine!.CreateCube(MeshTopology[index].Face.Centroid(), new Vector3(0.2f));
            }
        }
    }
}
