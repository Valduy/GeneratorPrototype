using System.Drawing;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace PipesDemo
{
    public class BuilderComponent : Component
    {
        public const int InWidth = 8;
        public const int InHeight = 8;
        public const int FloorWidth = 16;
        public const int FloorHeight = 16;

        public string MapPath { get; set; }

        public override void Start()
        {
            using var reader = new StreamReader(MapPath);
            var bmp = new Bitmap(reader.BaseStream);

            for (int x = 0; x < InWidth; x++)
            {
                for (int y = 0; y < InHeight; y++)
                {
                    CreateLevel(bmp, FloorWidth * x, FloorHeight * y);
                }
            }
        }

        private void CreateLevel(Bitmap bmp, int pivotX, int pivotY)
        {
            for (int x = 0; x < FloorWidth; x++)
            {
                for (int y = 0; y < FloorHeight; y++)
                {
                    if (bmp.GetPixel(pivotX + x, pivotY + y).A != 0)
                    {
                        var cellGo = GameObject!.Engine.CreateGameObject();
                        var render = cellGo.Add<Render3DComponent>();
                        render.Shape = new Mesh(Mesh.Cube);
                        GameObject!.AddChild(cellGo);
                        int height = InWidth * (pivotY / FloorHeight) + pivotX / FloorWidth; 
                        cellGo.Position = new Vector3(x, height, y);
                    }
                }
            }
        }
    }
}
