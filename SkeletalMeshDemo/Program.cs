using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using OpenTK.Mathematics;

class Program
{
    public static void Main(string[] args)
    {
        using var engine = new Engine();

        var operatorGo = engine.CreateGameObject();
        operatorGo.Add<Operator3DComponent>();
        operatorGo.Add<LightComponent>();
        operatorGo.Position = new Vector3(0, 1, 1);

        CreateSkeletalTube(engine, Vector3.Zero, Vector3.Zero, 1.0f);

        var gird = engine.Grid(20);
        var axis = engine.Axis(2);
        axis.Position = new Vector3(-11.0f, 0.0f, -11.0f);
        engine.Run();
    }

    public static GameObject CreateSkeletalTube(Engine engine, Vector3 position, Vector3 rotation, float scale)
    {
        var go = engine.CreateGameObject();
        go.Position = position;
        go.Euler = rotation;
        go.Scale = new Vector3(scale);

        var render = go.Add<SkeletalMeshRenderComponent>();
        render.Model = Model.Load("Content/PipeSegment.fbx");

        var skeleton = render.Model.Skeleton!;

        var root = skeleton["Root"];

        var disirable = new Vector3(0.0f, -5.0f, 5.0f);
      
        var top = skeleton["Top"];
        top.Position = disirable;

        var handRotation = 90;

        var handCoerce = Matrix4.CreateTranslation(Vector3.UnitZ);
        handCoerce *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(handRotation));

        var topHand = skeleton["TopHand"];
        topHand.Position = -handCoerce.ExtractTranslation();
        topHand.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(handRotation), 0, 0);

        var bottom = skeleton["Bottom"];
        bottom.Position = new Vector3(0.0f, 0.0f, 0.0f);

        var bottomHand = skeleton["BottomHand"];
        bottomHand.Position = new Vector3(0.0f, 0.0f, 0.0f);

        engine.CreateCube(position, Quaternion.Identity, new Vector3(0.3f));
        engine.CreateCube(disirable, Quaternion.Identity, new Vector3(0.3f));
        return go;
    }
}