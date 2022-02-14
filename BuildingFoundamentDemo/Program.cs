using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace DrawDifferentFiguresDemo
{
    public class FoundamentGenerator : Component
    {
        private readonly Renderer _renderer;
        private readonly KeyboardState _keyboardState;
        private readonly Random _seed;
        List<KeyValuePair<int, DNA>> _population;
        List<DNA> _first_population;
        GameObject bound;
        List<GameObject>? f_block;

        DNA first;

        int _count = 100;
        bool _iskeypressed = false;
        
        //Не хватает мутаций

        public FoundamentGenerator(Renderer renderer, KeyboardState keyboardState)
        {
            _renderer = renderer;
            _keyboardState = keyboardState;
            _seed = new Random();
            _population = new List<KeyValuePair<int, DNA>>();
            _first_population = new List<DNA>();
            first = new DNA();
        }

        private void Gen_F_Population(int nn)
        {
            for (int n = 0; n < nn; n++)
            {
                int x = _seed.Next(-300, 300);
                int y = _seed.Next(-300, 300);

                int xn = _seed.Next(2, 12);
                int yn = _seed.Next(2, 12);

                _first_population.Insert(_first_population.Count, new DNA(x, y, xn, yn));
            }
        }

        public override void Start()
        {
            bound = GameObject.Engine.CreateGameObject();
            bound.Add(() => new Render2DComponent(_renderer)
            {
                Color = Colors.Magenta,
                Shape = Shape2D.Square(600),
                Layer = -99
            });
            bound.Position = new Vector2(0, 0);

            Gen_F_Population(_count/2);
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (_keyboardState.IsKeyDown(Keys.Space) && !_iskeypressed)
            {
                if(f_block != null)
                {
                    foreach (var block in f_block)
                    {
                        GameObject.Engine.RemoveGameObject(block);
                    }

                    f_block.Clear();
                }

                Gen_F_Population(_count / 2);

                _iskeypressed = !_iskeypressed;
                var blocks = new List<GameObject>();

                for(int n = 0; n < _count; n++)
                {
                    int x = _first_population[n].x;
                    int y = _first_population[n].y;

                    int xn = _first_population[n].widht;
                    int yn = _first_population[n].height;

                    int f = 0; //Оценка глубины пересечения

                    //int cxn = xn;
                    //int cyn = yn;

                    for (int i = 0; i < xn; i++)
                    {
                        for (int j = 0; j < yn; j++)
                        {
                            var block = GameObject.Engine.CreateGameObject();
                            block.Add(() => new Render2DComponent(_renderer)
                            {
                                Color = Colors.Lime,
                                Shape = Shape2D.Square(100)
                            });
                            block.Position = new Vector2(x + i * 100, y - j * 100);

                            blocks.Insert(blocks.Count, block);

                            if(!Mathematics.IsPolygonInsideConvexPolygon(bound.Get<Render2DComponent>().Points, block.Get<Render2DComponent>().Points))
                            {
                                //cxn--;
                                //cxn--;
                                //cyn--;
                                //cyn--;
                                f++;
                            }
                        }
                    }

                    _population.Add(new KeyValuePair<int, DNA>(f, new DNA(x, y, xn, yn)));

                    //Any_L:
                    //if (_population.TryGetValue(f, out var current))
                    //{
                    //    int cur = current.widht * current.height;
                    //    int new_el = xn * yn;

                    //    if (new_el > cur)
                    //    {
                    //        _population[f] = new DNA(x, y, xn, yn);

                    //    }
                    //    else
                    //    {
                    //        f++;
                    //        goto Any_L;
                    //        //_population[f+1000] = new DNA(x, y, xn, yn);
                    //    }

                    //}
                    //else
                    //{
                    //    _population[f] = new DNA(x, y, xn, yn);
                    //}

                    foreach (var block in blocks)
                    {
                        GameObject.Engine.RemoveGameObject(block);
                    }

                    blocks.Clear();
                }

                _first_population.Clear();

                //var val = _population.OrderBy(k => k.Key).OrderByDescending(k => k.Value.widht * k.Value.height).Select(k => k.Value).ToArray();

                var val = new List<DNA>();
                var groups = _population.GroupBy(k => k.Key).OrderBy(k => k.Key).ToList();

                foreach(var g in groups)
                {
                    foreach(var el in g.OrderByDescending(k => k.Value.widht * k.Value.height))
                    {
                        val.Insert(val.Count, el.Value);
                    }
                }

                for(var i = 0; i < val.Count-1; i+=2)
                {
                    int x = (val[i].x + val[i + 1].x) / 2;
                    int y = (val[i].y + val[i + 1].y) / 2;
                    int xn = (val[i].widht + val[i + 1].widht) / 2 + 1;
                    int yn = (val[i].height + val[i + 1].height) / 2 + 1;

                    if (i == 0)
                    {
                        first = new DNA(x, y, xn, yn);
                    }

                    x += _seed.Next(0, 100) <= 4 ? _seed.Next(-50, 50) : 0;
                    y += _seed.Next(0, 100) <= 4 ? _seed.Next(-50, 50) : 0;
                    xn += _seed.Next(0, 100) <= 4 ? _seed.Next(2, 4) : 0;
                    yn += _seed.Next(0, 100) <= 4 ? _seed.Next(2, 4) : 0;

                    _first_population.Insert(_first_population.Count, new DNA(x, y, xn, yn));
                }

                _population.Clear();

                Console.WriteLine("Pressed");
            }

            if (_keyboardState.IsKeyReleased(Keys.Space) && _iskeypressed)
            {
                _iskeypressed = !_iskeypressed;

                int x = first.x;
                int y = first.y;

                int xn = first.widht;
                int yn = first.height;

                f_block = new List<GameObject>();

                for (int i = 0; i < xn; i++)
                {
                    for (int j = 0; j < yn; j++)
                    {
                        var ff_block = GameObject.Engine.CreateGameObject();
                        ff_block.Add(() => new Render2DComponent(_renderer)
                        {
                            Color = Colors.Lime,
                            Shape = Shape2D.Square(100)
                        });
                        ff_block.Position = new Vector2(x + 100 * i, y - 100 * j);

                        f_block.Insert(f_block.Count, ff_block);
                    }
                }

                Console.WriteLine("Release");
                Console.WriteLine(x);
                Console.WriteLine(y);
                Console.WriteLine(xn);
                Console.WriteLine(yn);
                Console.WriteLine(_first_population.Count);
            }
        }
    }

    struct DNA
    {
        public int x;
        public int y;
        public int widht;
        public int height;

        public DNA()
        {
            this.x = 0;
            this.y = 0;
            this.widht = 0;
            this.height = 0;
        }

        public DNA(int x, int y, int widht, int height)
        {
            this.x = x;
            this.y = y;
            this.widht = widht;
            this.height = height;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {

            Dictionary<int, DNA> el;

            using var game = new Engine();
            float leftTopCornerX = -Engine.WindowWidth / 2;
            float leftTopCornerY = Engine.WindowHeight / 2;

            var genetator = game.Engine.CreateGameObject();
            genetator.Add(() => new FoundamentGenerator(game.Window.Renderer, game.Window.KeyboardState)
            {
            });

            //var squareGo = engine.Engine.CreateGameObject();
            //squareGo.Add(() => new Render2DComponent(engine.Window.Renderer)
            //{
            //    Color = Colors.Lime,
            //    Shape = Shape.Square(100)
            //});
            //squareGo.Add(() => new BoundsComponent(new GameEngine.Bounds.RectangleBounds()
            //{
            //    Size = new Vector2(100)
            //}));
            //squareGo.Position = new Vector2(leftTopCornerX + 400, leftTopCornerY - 100);

            //var squareGo_1 = engine.Engine.CreateGameObject();
            //squareGo_1.Add(() => new Render2DComponent(engine.Window.Renderer)
            //{
            //    Color = Colors.Magenta,
            //    Shape = Shape.Square(100),
            //    Layer = -9
            //});
            //squareGo_1.Add(() => new BoundsComponent(new GameEngine.Bounds.RectangleBounds()
            //{
            //    Size = new Vector2(100)
            //}));
            //squareGo_1.Position = new Vector2(leftTopCornerX + 301, leftTopCornerY - 100);

            //Console.WriteLine(Mathematics.IsBoundingBoxesIntersects(squareGo.Get<BoundsComponent>().Position, squareGo.Get<BoundsComponent>().Width, squareGo.Get<BoundsComponent>().Height, squareGo_1.Get<BoundsComponent>().Position, squareGo_1.Get<BoundsComponent>().Width, squareGo_1.Get<BoundsComponent>().Height));

            //Console.WriteLine(Mathematics.IsPolygonInsideConvexPolygon(squareGo_1.Get<Render2DComponent>().Points, squareGo.Get<Render2DComponent>().Points));

            //Console.WriteLine(Mathematics.IsConvexPolygonsIntersects(squareGo_1.Get<Render2DComponent>().Points, squareGo.Get<Render2DComponent>().Points));



            //var blocks = new List<GameObject>();

            //for (int i = 0; i < 4; i++)
            //{
            //    for (int j = 0; j < 4; j++)
            //    {
            //        var block = engine.Engine.CreateGameObject();
            //        block.Add(() => new Render2DComponent(engine.Window.Renderer)
            //        {
            //            Color = Colors.Lime,
            //            Shape = Shape.Square(100)
            //        });
            //        block.Position = new Vector2(leftTopCornerX + i*100 + 100, leftTopCornerY - j*100 - 100);

            //        blocks.Insert(blocks.Count,block);
            //    }
            //}


            game.Run();
        }
    }
}   
