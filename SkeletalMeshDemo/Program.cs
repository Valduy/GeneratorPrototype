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

        var position = new Vector3(0.0f, 3.0f, 4.0f);
        CreateSkeletalTube(engine, position, Vector3.Zero, 1.0f);

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

        var topDisirable = new Vector3(3.0f, 2.0f, 0.0f);
        var bottomDisirable = new Vector3(-3.0f, -2.0f, 0.0f);

        var top = skeleton["Top"];
        top.Position = topDisirable;

        var topHandRotation = 0;
        var topHandCoerce = Matrix4.CreateTranslation(Vector3.UnitZ);
        topHandCoerce *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(topHandRotation));

        var topHand = skeleton["TopHand"];
        topHand.Position = -topHandCoerce.ExtractTranslation();
        topHand.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(topHandRotation), 0, 0);

        var bottom = skeleton["Bottom"];
        bottom.Position = bottomDisirable;

        var bottomHandRotation = 0;
        var bottomHandCoerce = Matrix4.CreateTranslation(-Vector3.UnitZ);
        bottomHandCoerce *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(bottomHandRotation));

        var bottomHand = skeleton["BottomHand"];
        bottomHand.Position = -bottomHandCoerce.ExtractTranslation();
        bottomHand.Rotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(bottomHandRotation), 0, 0);

        engine.CreateCube(topDisirable + position, Quaternion.Identity, new Vector3(0.3f));
        engine.CreateCube(bottomDisirable + position, Quaternion.Identity, new Vector3(0.3f));
        return go;
    }
}