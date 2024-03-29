﻿using Edgar.Geometry;
using Edgar.GraphBasedGenerator.Grid2D.Drawing;
using Edgar.GraphBasedGenerator.Grid2D;
using Edgar.Graphs;
using Edgar.Legacy.Core.MapLayouts;
using System.Xml.Linq;

namespace TextGeneratorBasedOnTemporalGraphModel
{
    interface IEdge<T, TTimeStamp> where T : ICard
    {
        T From { get; }

        T To { get; }
    }

    class Edge<T, TTimeStamp> : IEdge<T, TTimeStamp>, IEquatable<Edge<T, TTimeStamp>> where T : ICard
    {
        public T From { get; }

        public T To { get; }

        public Edge(T from, T to)
        {
            From = from;
            To = to;
        }

        public bool Equals(Edge<T, TTimeStamp> other)
        {
            if (other == null)
            {
                return false;
            }

            if (this == other)
            {
                return true;
            }

            if (EqualityComparer<T>.Default.Equals(From, other.From))
            {
                return EqualityComparer<T>.Default.Equals(To, other.To);
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this == obj)
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Edge<T, TTimeStamp>)obj);
        }

        public override int GetHashCode()
        {
            return (EqualityComparer<T>.Default.GetHashCode(From) * 397) ^ EqualityComparer<T>.Default.GetHashCode(To);
        }
    }

    interface IGraph<T, TTimeStamp> where T : ICard
    {
        bool IsDirected { get; }

        IEnumerable<T> Vertices { get; }

        IEnumerable<IEdge<T, TTimeStamp>> Edges { get; }

        int VerticesCount { get; }

        void AddVertex(T vertex);

        void AddEdge(T from, T to, TTimeStamp time);

        T GetNeighbours(T vertex, TTimeStamp time);

        bool HasEdge(T from, T to, TTimeStamp time);
    }

    class TemporalGraph<T, TTimeStamp> : IGraph<T, TTimeStamp> where T : ICard
    {
        private readonly Dictionary<T, Dictionary<TTimeStamp, T>> adjacencyLists = new Dictionary<T, Dictionary<TTimeStamp, T>>();

        private readonly Dictionary<string, EventPattern> locations_meta = new Dictionary<string, EventPattern>();
        private readonly UndirectedAdjacencyListGraph<string> locations = new UndirectedAdjacencyListGraph<string>();

        public bool IsDirected { get; }

        public IEnumerable<T> Vertices => adjacencyLists.Keys;

        public IEnumerable<IEdge<T, TTimeStamp>> Edges => GetEdges();

        public int VerticesCount => adjacencyLists.Count;

        public void AddVertex(T vertex)
        {
            if (adjacencyLists.ContainsKey(vertex))
            {
                throw new ArgumentException("Vertex already exists");
            }
            
            if(vertex.location != "")
            {
                try
                {
                    locations.AddVertex(vertex.location);
                    locations_meta[vertex.location] = vertex.eventPattern;
                }
                catch
                {
                    Console.WriteLine("Error add location vertex.");
                }

                foreach (var loc in vertex.adjacent_locations)
                {
                    try
                    {
                        locations.AddVertex(loc);
                    }
                    catch
                    {
                        Console.WriteLine("Can add adjacent locations.");
                    }
                }

                foreach (var loc in vertex.adjacent_locations)
                {
                    try
                    {
                        locations.AddEdge(vertex.location, loc);
                    }
                    catch
                    {
                        Console.WriteLine("Can add adjacent locations.");
                    }
                }

                adjacencyLists[vertex] = new Dictionary<TTimeStamp, T>();
            }
        }

        public void AddEdge(T from, T to, TTimeStamp time)
        {
            if(from.location != "" && to.location != "")
            {
                if (!adjacencyLists.TryGetValue(from, out var value) || !adjacencyLists.TryGetValue(to, out var value2))
                {
                    throw new ArgumentException("One of the vertices does not exist");
                }

                if (value.ContainsKey(time))
                {
                    throw new ArgumentException("The edge was already added");
                }

                try
                {
                    locations.AddEdge(from.location, to.location);
                }
                catch
                {
                    Console.WriteLine("Error add location edge.");
                }

                value[time] = to;
            }
        }

        public UndirectedAdjacencyListGraph<string> GetLocationSubGraph()
        {

            return locations;
        }

        public Dictionary<string, EventPattern> GetMetaSubGraph()
        {

            return locations_meta;
        }

        //public void AddEdges()
        //{

        //}

        public T GetNeighbours(T vertex, TTimeStamp time)
        {
            if (!adjacencyLists.TryGetValue(vertex, out var value))
            {
                throw new ArgumentException("The vertex does not exist");
            }

            return value[time];
        }

        public bool HasEdge(T from, T to, TTimeStamp time)
        {
            if (GetNeighbours(from, time).Equals(to))
            {
                return true;
            }
            return false;
        }

        private IEnumerable<IEdge<T, TTimeStamp>> GetEdges()
        {
            HashSet<Tuple<T, T>> hashSet = new HashSet<Tuple<T, T>>();
            List<IEdge<T, TTimeStamp>> list = new List<IEdge<T, TTimeStamp>>();
            foreach (var adjacencyList in adjacencyLists)
            {
                T key = adjacencyList.Key;
                foreach (var item in adjacencyList.Value)
                {
                    if (!hashSet.Contains(Tuple.Create(key, item.Value)))
                    {
                        //list.Add(new Edge<T, TTimeStamp, TRelationShip>(key, item, )); - доделать
                        hashSet.Add(Tuple.Create(key, item.Value));
                    }
                }
            }

            return list;
        }
    }

    interface IHero
    {
        string name { get; set; }
    }
    class Hero : IHero
    {
        public string name { get; set; }

        public Hero(string n)
        {
            name = n;
        }

    }

    interface IStoryEvent
    {
        string description { get; set; }
    }
    class StoryEvent : IStoryEvent
    {
        public string description { get; set; }
        public bool value { get; set; }

        public StoryEvent(string d)
        {
            description = d;
            value = false;
        }
    }

    interface ICard
    {
        public int time { get; set; }
        public bool value { get; set; }
        public float weight { get; set; }
        public int tensity_change { get; set; }
        public float priority { get; set; }
        public string location { get; set; }
        public EventPattern eventPattern { get; set; }
        Func<bool> prediction { get; set; }

        public List<string> adjacent_locations { get; set; }
    }
    class StoryCard<THero, TStoryEvent> : ICard where THero : IHero where TStoryEvent : IStoryEvent
    {
        public int time { get; set; }
        public string description { get; set; }
        public float weight { get; set; }
        public float priority { get; set; }

        public bool value { get; set; }
        public int tensity_change { get; set; }
        public string location { get; set; }
        public List<THero> heroes { get; set; }
        public EventPattern eventPattern { get; set; }
        public Func<bool> prediction { get; set; }
        public Dictionary<THero, TStoryEvent> reaction { get; set; }

        public List<string> adjacent_locations { get; set; }

        public StoryCard(string title)
        {
            description = title;

            value = false;
            heroes = new List<THero>();
            reaction = new Dictionary<THero, TStoryEvent>();
            adjacent_locations = new List<string>();
            location = "";
            eventPattern = EventPattern.Move;
        }
        public static StoryCard<THero, TStoryEvent> Begin(string title) => new StoryCard<THero, TStoryEvent>(title);

        public StoryCard<THero, TStoryEvent> Heroes(params THero[] hero)
        {
            foreach (var h in hero)
            {

                heroes.Add(h);
            }
            return this;
        }

        public StoryCard<THero, TStoryEvent> Time(int time)
        {
            this.time = time;

            return this;
        }

        public StoryCard<THero, TStoryEvent> Location(string location)
        {
            this.location = location;

            return this;
        }

        public StoryCard<THero, TStoryEvent> Bit(float weight, float priority)
        {
            this.weight = weight;
            this.priority = priority;

            return this;
        }

        public StoryCard<THero, TStoryEvent> Reaction(THero hero, TStoryEvent se)
        {
            reaction.Add(hero, se);

            return this;
        }

        public StoryCard<THero, TStoryEvent> Reaction(THero hero, TStoryEvent se, string location)
        {
            reaction.Add(hero, se);
            adjacent_locations.Add(location);
            return this;
        }

        public override string ToString()
        {
            string ret = $"///{description}\\\\\\ \n";

            foreach (var r in reaction)
            {
                ret += $"{r.Key.name}: {r.Value.description}\n";
            }

            //Aret += "\n";

            return ret;
        }

        public StoryCard<THero, TStoryEvent> GenReaction()
        {


            return this;
        }

        public StoryCard<THero, TStoryEvent> Rule(Func<bool> p)
        {
            prediction = p;

            return this;
        }

        public StoryCard<THero, TStoryEvent> Change_Tensity(int tc)
        {
            tensity_change = tc;
            return this;
        }

        public StoryCard<THero, TStoryEvent> EventType(EventPattern ep)
        {
            eventPattern = ep;
            return this;
        }

        //public StoryGenerator Rule(Func<TContext, TContext, bool> rule);
        //public StoryGenerator Rule(Func<TContext, bool> rule);
        //public StoryGenerator Rule(THero h0, Func<TContext, THero, bool> rule);
        //public StoryGenerator Rule(THero h0, THero h1, Func<TContext, THero, THero, bool> rule);
    }

    class StoryManager<THero, TCard> where THero : IHero where TCard : ICard
    {
        public List<TCard> cards { get; set; }
        public List<float> story_curve { get; set; }
        public List<THero> heroes { get; set; }

        public TemporalGraph<TCard, int> graph { get; set; }

        public StoryManager()
        {
            graph = new TemporalGraph<TCard, int>();
            cards = new List<TCard>();
            heroes = new List<THero>();
            story_curve = new List<float>();
        }

        public static StoryManager<THero, TCard> Begin() => new StoryManager<THero, TCard>();

        public StoryManager<THero, TCard> Cards(params TCard[] card)
        {
            foreach (var c in card)
            {
                cards.Add(c);
            }

            return this;
        }

        public StoryManager<THero, TCard> ChangeStoryCurve(params float[] curve)
        {
            foreach (var c in curve)
            {
                story_curve.Add(c);
            }

            return this;
        }

        public StoryManager<THero, TCard> Heroes(params THero[] hero)
        {
            foreach (var h in hero)
            {
                heroes.Add(h);
            }

            return this;
        }

        static private Dictionary<string, RoomTemplateGrid2D> GetRoomTemplates()
        {
            return new List<RoomTemplateGrid2D>()
            {
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetRectangle(15, 19),
                    new SimpleDoorModeGrid2D(1, 2),
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Normal 1"
                ),
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetRectangle(13, 15),
                    new SimpleDoorModeGrid2D(1, 2),
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Normal 2"
                ),
                new RoomTemplateGrid2D(
                    new PolygonGrid2DBuilder()
                        .AddPoint(-11, 6).AddPoint(-5, 6).AddPoint(-5, 5).AddPoint(-3, 5)
                        .AddPoint(-3, 6).AddPoint(2, 6).AddPoint(2, 5).AddPoint(4, 5)
                        .AddPoint(4, 6).AddPoint(10, 6).AddPoint(10, -1).AddPoint(4, -1)
                        .AddPoint(4, 0).AddPoint(2, 0).AddPoint(2, -1).AddPoint(-3, -1)
                        .AddPoint(-3, 0).AddPoint(-5, 0).AddPoint(-5, -1).AddPoint(-11, -1)
                        .Build(),
                    new SimpleDoorModeGrid2D(1, 2),
                    // repeatMode: RoomTemplateRepeatMode.NoRepeat,
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Normal 3"
                ),
                #region hidden:Other room templates
                new RoomTemplateGrid2D(
                    new PolygonGrid2DBuilder()
                        .AddPoint(-39, 1).AddPoint(-37, 1).AddPoint(-37, 10).AddPoint(-39, 10)
                        .AddPoint(-39, 15).AddPoint(-26, 15).AddPoint(-26, 10).AddPoint(-28, 10)
                        .AddPoint(-28, 1).AddPoint(-26, 1).AddPoint(-26, -4).AddPoint(-39, -4)
                        .Build(),
                    new SimpleDoorModeGrid2D(1, 2),
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Normal 4"
                ),
                new RoomTemplateGrid2D(
                    new PolygonGrid2DBuilder()
                        .AddPoint(-14, 3).AddPoint(0, 3).AddPoint(0, 5).AddPoint(-14, 5)
                        .AddPoint(-14, 12).AddPoint(8, 12).AddPoint(8, -4).AddPoint(-6, -4)
                        .AddPoint(-6, -6).AddPoint(8, -6).AddPoint(8, -13).AddPoint(-14, -13)
                        .Build(),
                    new SimpleDoorModeGrid2D(1, 2),
                    // repeatMode: RoomTemplateRepeatMode.NoRepeat,
                    name: "Normal 5"
                ),
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetSquare(13),
                    new SimpleDoorModeGrid2D(1, 2),
                    name: "Normal 6"
                ),
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetSquare(11),
                    new SimpleDoorModeGrid2D(1, 2),
                    name: "Spawn"
                ),
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetRectangle(26, 26),
                    new SimpleDoorModeGrid2D(1, 4),
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Boss"
                ),
                new RoomTemplateGrid2D(PolygonGrid2D.GetRectangle(20, 26),
                    new SimpleDoorModeGrid2D(1, 4),
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Hub 1"
                ),
                new RoomTemplateGrid2D(
                    new PolygonGrid2DBuilder()
                        .AddPoint(-8, 7).AddPoint(-7, 7).AddPoint(-7, 8).AddPoint(3, 8)
                        .AddPoint(3, 7).AddPoint(4, 7).AddPoint(4, -3).AddPoint(3, -3)
                        .AddPoint(3, -4).AddPoint(-7, -4).AddPoint(-7, -3).AddPoint(-8, -3)
                        .Build(),
                    new SimpleDoorModeGrid2D(1, 2),
                    name: "Reward"
                ),
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetRectangle(12, 17),
                    new SimpleDoorModeGrid2D(1, 3),
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Normal 7"
                ),
                new RoomTemplateGrid2D(
                    new PolygonGrid2DBuilder()
                        .AddPoint(-3, 4).AddPoint(4, 4).AddPoint(4, -1).AddPoint(-3, -1)
                        .Build(),
                    new ManualDoorModeGrid2D(new List<DoorGrid2D>()
                        {
                        new DoorGrid2D(new Vector2Int(4, 2), new Vector2Int(4, 1)),
                        new DoorGrid2D(new Vector2Int(-3, 2), new Vector2Int(-3, 1)),
                        new DoorGrid2D(new Vector2Int(0, 4), new Vector2Int(1, 4)),
                        new DoorGrid2D(new Vector2Int(0, -1), new Vector2Int(1, -1)),
                        }
                    ),
                    name: "Exit"
                ),
                new RoomTemplateGrid2D(
                    new PolygonGrid2DBuilder()
                        .AddPoint(-8, 7).AddPoint(-7, 7).AddPoint(-7, 8).AddPoint(3, 8)
                        .AddPoint(3, 7).AddPoint(4, 7).AddPoint(4, -3).AddPoint(3, -3)
                        .AddPoint(3, -4).AddPoint(-7, -4).AddPoint(-7, -3).AddPoint(-8, -3)
                        .Build(),
                    new SimpleDoorModeGrid2D(1, 2),
                    name: "Shop"
                ),
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetSquare(9),
                    new SimpleDoorModeGrid2D(1, 2),
                    name: "Secret"
                )
                #endregion
            }.ToDictionary(x => x.Name, x => x);
        }

        public static RoomDescriptionGrid2D GetBasicRoomDescription()
        {
            var doors = new SimpleDoorModeGrid2D(doorLength: 1, cornerDistance: 1);
            var transformations = new List<TransformationGrid2D>()
            {
                TransformationGrid2D.Identity,
                //TransformationGrid2D.Rotate90
            };

            var squareRoom1 = new RoomTemplateGrid2D(
                PolygonGrid2D.GetSquare(20),
                doors,
                allowedTransformations: transformations,
                name: "Boss"
            );
            var squareRoom2 = new RoomTemplateGrid2D(
                PolygonGrid2D.GetSquare(25),
                doors,
                allowedTransformations: transformations,
                name: "Exit"
            );
            var rectangleRoom = new RoomTemplateGrid2D(
                PolygonGrid2D.GetRectangle(16, 24),
                doors,
                allowedTransformations: transformations,
                name: "Room"
            );

            return new RoomDescriptionGrid2D
            (
                isCorridor: false,
                roomTemplates: new List<RoomTemplateGrid2D>() {
                    squareRoom1,
                    squareRoom2,
                    rectangleRoom
                }
            );
        }

        public static RoomDescriptionGrid2D GetParameterizedRoomDescription(string name)
        {
            var room_templates = new List<RoomTemplateGrid2D>()
            {
                new RoomTemplateGrid2D(
                    PolygonGrid2D.GetRectangle(12, 48),
                    new SimpleDoorModeGrid2D(1, 2),
                    allowedTransformations: TransformationGrid2DHelper.GetRotations(),
                    name: "Коридор"
                ),
            }.ToDictionary(x => x.Name, x => x);

            if (room_templates.ContainsKey(name))
            {
                return new RoomDescriptionGrid2D
                (
                isCorridor: false,
                roomTemplates: new List<RoomTemplateGrid2D>() { room_templates[name] }
                );
            }
            else
            {
                return GetBasicRoomDescription();
            }
        }

            public static void Visualize(UndirectedAdjacencyListGraph<string> graph, Dictionary<string, EventPattern> meta)
        {

            var corridorOutline = PolygonGrid2D.GetRectangle(2, 1);

            var corridorDoors = new ManualDoorModeGrid2D(new List<DoorGrid2D>()
                {
                    new DoorGrid2D(new Vector2Int(0, 0), new Vector2Int(0, 1)),
                    new DoorGrid2D(new Vector2Int(2, 0), new Vector2Int(2, 1))
                }
            );

            var corridorRoomTemplate = new RoomTemplateGrid2D(
                corridorOutline,
                corridorDoors,
                allowedTransformations: new List<TransformationGrid2D>()
                {
                    TransformationGrid2D.Identity,
                    TransformationGrid2D.Rotate90
                }
            );

            var corridorRoomTemplateLonger = new RoomTemplateGrid2D(
                PolygonGrid2D.GetRectangle(4, 1),
                new ManualDoorModeGrid2D(new List<DoorGrid2D>()
                    {
                        new DoorGrid2D(new Vector2Int(0, 0), new Vector2Int(0, 1)),
                        new DoorGrid2D(new Vector2Int(4, 0), new Vector2Int(4, 1))
                    }
                ),
                allowedTransformations: new List<TransformationGrid2D>()
                {
                    TransformationGrid2D.Identity,
                    TransformationGrid2D.Rotate90
                }
            );
            var corridorRoomDescription = new RoomDescriptionGrid2D
            (
                isCorridor: true,
                roomTemplates: new List<RoomTemplateGrid2D>() { corridorRoomTemplate, corridorRoomTemplateLonger }
            );

            var basicRoomDescription = GetBasicRoomDescription();
            var roomTemplates = GetRoomTemplates();

            var levelDescription = new LevelDescriptionGrid2D<string>();
            levelDescription.MinimumRoomDistance = 1;

            foreach (var room in graph.Vertices)
            {
                //if (room.Contains("Тупик") && !room.Contains("Начало") && !room.Contains("Конец"))
                //{
                //    levelDescription.AddRoom(room, new RoomDescriptionGrid2D(isCorridor: false, new List<RoomTemplateGrid2D>() { roomTemplates["Exit"] }));
                //}
                //else if (room.Contains("Горло"))
                //{
                //    levelDescription.AddRoom(room, new RoomDescriptionGrid2D(isCorridor: false, new List<RoomTemplateGrid2D>() { roomTemplates["Boss"] }));
                //}
                //else
                //{
                //    levelDescription.AddRoom(room, basicRoomDescription);
                //}

                levelDescription.AddRoom(room, GetParameterizedRoomDescription(room)); // + "\n" + meta[room]
            }

            var corridorCounter = graph.VerticesCount;

            foreach (var connection in graph.Edges)
            {
                if (connection.From != connection.To)
                {
                    // We manually insert a new room between each pair of neighboring rooms in the graph
                    levelDescription.AddRoom(corridorCounter.ToString(), corridorRoomDescription);

                    // And instead of connecting the rooms directly, we connect them to the corridor room
                    levelDescription.AddConnection(connection.From, corridorCounter.ToString()); // + "\n" + meta[connection.From]
                    levelDescription.AddConnection(connection.To, corridorCounter.ToString()); // + "\n" + meta[connection.To]
                    corridorCounter++;
                }
            }

            var generator = new GraphBasedGeneratorGrid2D<string>(levelDescription);
            var layout = generator.GenerateLayout();

            var drawer = new DungeonDrawer<string>();
            drawer.DrawLayoutAndSave(layout, "layout.png", new DungeonDrawerOptions()
            {
                Width = 4000,
                Height = 4000,
            });

        }
        public StoryManager<THero, TCard> Do()
        {
            float t = 0;
            int i = 0;
            int num = 1;

            List<TCard> winners = new List<TCard>();

            while (cards.Count != 0)
            {
                List<TCard> new_cards = new List<TCard>();
                new_cards.AddRange(cards);

                List<TCard> satisfied = new List<TCard>() { };

                foreach (var card in new_cards)
                {
                    if (card.prediction())
                    {
                        double scope = (card.weight * (1 / Math.Exp((story_curve[i] - t) / num - card.tensity_change))) * card.priority;

                        satisfied.Add(card);
                    }
                }

                if (satisfied.Count == 0)
                {
                    break;
                }

                var max_priority = satisfied.Max(comparer => comparer.priority);
                Dictionary<TCard, double> highest_priority = new Dictionary<TCard, double>() { };

                foreach (var satisfy in satisfied)
                {
                    if (satisfy.priority >= max_priority - 0.1)
                    {
                        double scope = (satisfy.weight * (1 / Math.Exp((story_curve[i] - t) / num - satisfy.tensity_change))) * satisfy.priority;

                        highest_priority.Add(satisfy, scope);

                    }
                }

                var winner = highest_priority.OrderByDescending(kvp => kvp.Value).First();

                winner.Key.value = true;

                t += winner.Key.tensity_change;

                Console.Write(winner.Key.ToString());

                winners.Add(winner.Key);
                //

                if (winner.Key.tensity_change != 0)
                {
                    i++;
                }

                cards.Remove(winner.Key);
            }

            //Добавить сортировку по времени
            graph.AddVertex(winners[0]);
            var start = winners[0];

            //if(start.adjacent_locations.Count > 0)
            //{

            //}
            //else
            //{

            //}

            for (int j = 0; j <= winners.Count - 2; j++)
            {
                if(winners[j].location != "") //Переосмыслить
                {
                    if (start.location == winners[j + 1].location)
                    {
                        winners[j + 1].location = winners[j + 1].location + "_" + winners[j + 1].time;
                        graph.AddVertex(winners[j + 1]);
                        graph.AddEdge(start, winners[j + 1], winners[j + 1].time);
                    }
                    else
                    {
                        graph.AddVertex(winners[j + 1]);
                        graph.AddEdge(start, winners[j + 1], winners[j + 1].time);
                        start = winners[j + 1];
                    }
                }
            }

            Visualize(graph.GetLocationSubGraph(), graph.GetMetaSubGraph());

            return this;
        }
    }
}
