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
        //root.Position = new Vector3(0.0f, 0.0f, 0.0f);
        //root.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(30), MathHelper.DegreesToRadians(90), 0);
        //root.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(30), 0, 0);
        //root.Scale = new Vector3(2);

        var top = skeleton["Top"];
        top.Position = new Vector3(0.0f, 1.0f, 1.0f);
        //top.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(90), 0, 0);

        var topHand = skeleton["TopHand"];
        topHand.Position = new Vector3(0.0f, 0.0f, 0.0f);
        topHand.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(-90), 0, 0);

        var bottom = skeleton["Bottom"];
        bottom.Position = new Vector3(0.0f, 0.0f, 0.0f);
        //bottom.Rotation = Quaternion.FromEulerAngles(0, 0, MathHelper.DegreesToRadians(270));

        var bottomHand = skeleton["BottomHand"];
        bottomHand.Position = new Vector3(0.0f, 0.0f, 0.0f);

        //engine.CreateCube(position, Quaternion.Identity, new Vector3(0.3f));
        //engine.CreateCube(new Vector3(0, 2, 0), Quaternion.Identity, new Vector3(0.3f));
        return go;
    }
}