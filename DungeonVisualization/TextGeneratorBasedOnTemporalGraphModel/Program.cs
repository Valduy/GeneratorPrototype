﻿using Edgar.Geometry;
using Edgar.GraphBasedGenerator.Grid2D.Drawing;
using Edgar.GraphBasedGenerator.Grid2D;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;

//using DungeonViz;

using Edgar.Graphs;
//using GraphPlanarityTesting.Graphs.DataStructures;

namespace TextGeneratorBasedOnTemporalGraphModel
{
    //На будущее
    enum EventType
    {
        State, 
        DirectImpact,
        IndirectImpact,
        FullEvent
    }

    //На будущее
    enum EventPattern
    {
        Move,
        See,
        Attack,
        Talk,
        Change
    }

    //На будущее
    class Event
    {
        public int time { get; set; } //Нормальзированный момент времени
        public int id { get; set; } // Уникальный индентификатор
        public List<int> ch_id { get; set; } //Причинно следственные связи
        public bool vm { get; set; } //Было совершено или нет
        public int weight { get; set; } // Вес события
        public int priority { get; set; } // Приоритет события
        public int delta_m { get; set; } // Изменение напряжения
        public int verb { get; set; } // Глагол (описывает событие)
        public string initiator { get; set; } // 
        public string target { get; set; } //
        public string sub_targets { get; set; } // Косвенные связи
        public string pretext { get; set; } // Предлог
        public string meta { get; set; } // Красивое описание

    }

    interface IEdge<T, TTimeStamp, TRelationShip> where T : ICard
    {
        T From { get; }

        T To { get; }
    }

    class Edge<T, TTimeStamp, TRelationShip> : IEdge<T, TTimeStamp, TRelationShip>, IEquatable<Edge<T, TTimeStamp, TRelationShip>> where T : ICard
    {
        public T From { get; }

        public T To { get; }

        public Edge(T from, T to)
        {
            From = from;
            To = to;
        }

        public bool Equals(Edge<T, TTimeStamp, TRelationShip> other)
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

            return Equals((Edge<T, TTimeStamp, TRelationShip>)obj);
        }

        public override int GetHashCode()
        {
            return (EqualityComparer<T>.Default.GetHashCode(From) * 397) ^ EqualityComparer<T>.Default.GetHashCode(To);
        }
    }

    interface IGraph<T, TTimeStamp, TRelationShip> where T : ICard
    {
        bool IsDirected { get; }

        IEnumerable<T> Vertices { get; }

        IEnumerable<IEdge<T, TTimeStamp, TRelationShip>> Edges { get; }

        int VerticesCount { get; }

        void AddVertex(T vertex);

        void AddEdge(T from, T to, TTimeStamp time);

        T GetNeighbours(T vertex, TTimeStamp time);

        bool HasEdge(T from, T to, TTimeStamp time);
    }

    class TemporalGraph<T, TTimeStamp, TRelationShip> : IGraph<T, TTimeStamp, TRelationShip> where T : ICard
    {
        private readonly Dictionary<T, Dictionary<TTimeStamp, T>> adjacencyLists = new Dictionary<T, Dictionary<TTimeStamp, T>>();

        private readonly UndirectedAdjacencyListGraph<string> locations = new UndirectedAdjacencyListGraph<string>();

        public bool IsDirected { get; }

        public IEnumerable<T> Vertices => adjacencyLists.Keys;

        public IEnumerable<IEdge<T, TTimeStamp, TRelationShip>> Edges => GetEdges();

        public int VerticesCount => adjacencyLists.Count;

        public void AddVertex(T vertex)
        {
            if (adjacencyLists.ContainsKey(vertex))
            {
                throw new ArgumentException("Vertex already exists");
            }

            try
            {
                locations.AddVertex(vertex.location);
            }
            catch 
            {
                Console.WriteLine("Error add location vertex.");
            } 

            adjacencyLists[vertex] = new Dictionary<TTimeStamp, T>();
        }

        public void AddEdge(T from, T to, TTimeStamp time)
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

        public UndirectedAdjacencyListGraph<string> GetLocationSubGraph()
        {

            return locations;
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

        private IEnumerable<IEdge<T, TTimeStamp, TRelationShip>> GetEdges()
        {
            HashSet<Tuple<T, T>> hashSet = new HashSet<Tuple<T, T>>();
            List<IEdge<T, TTimeStamp, TRelationShip>> list = new List<IEdge<T, TTimeStamp, TRelationShip>>();
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
        Func<bool> prediction { get; set; }
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
        public Func<bool> prediction { get; set; }
        public Dictionary<THero, TStoryEvent> reaction { get; set; }

        public StoryCard(string title)
        {
            description = title;

            value = false;
            heroes = new List<THero>();
            reaction = new Dictionary<THero, TStoryEvent>();
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

        public TemporalGraph<TCard, int, int> graph { get; set; }

        public StoryManager()
        {
            graph = new TemporalGraph<TCard, int, int>();
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
                TransformationGrid2D.Rotate90
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

        public static void Visualize(UndirectedAdjacencyListGraph<string> graph)
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

                levelDescription.AddRoom(room, basicRoomDescription);
            }

            var corridorCounter = graph.VerticesCount;

            foreach (var connection in graph.Edges)
            {
                if(connection.From != connection.To)
                {
                    // We manually insert a new room between each pair of neighboring rooms in the graph
                    levelDescription.AddRoom(corridorCounter.ToString(), corridorRoomDescription);

                    // And instead of connecting the rooms directly, we connect them to the corridor room
                    levelDescription.AddConnection(connection.From, corridorCounter.ToString());
                    levelDescription.AddConnection(connection.To, corridorCounter.ToString());
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

            for (int j = 0; j <= winners.Count - 2; j++)
            {
                
                if(start.location == winners[j + 1].location)
                {
                    winners[j + 1].location = winners[j + 1].location + "_" + winners[j + 1].time;
                    graph.AddVertex(winners[j + 1]);
                    graph.AddEdge(start, winners[j + 1], winners[j + 1].time);
                }
                else
                {
                    graph.AddVertex(winners[j + 1]);
                    graph.AddEdge(start, winners[j + 1], winners[j + 1].time);
                    start = winners[j+1];
                }
            }

            Visualize(graph.GetLocationSubGraph());

            return this;
        }
    }

    class Program
    {
        static Hero h1 = new Hero("Джим Рейнор");
        static Hero h2 = new Hero("Тайкус Финдли");
        static Hero h3 = new Hero("Сара Керриган");
        static Hero h4 = new Hero("Зератул");
        static Hero h5 = new Hero("Арктур Менгск");
        static Hero h6 = new Hero("Валериан Менгск");

        static Hero h7 = new Hero("Представитель Зергов");
        static Hero h8 = new Hero("Местные Жители");
        static Hero h9 = new Hero("Представитель Протоссов");

        //// Story Events Start ////

        //Акт 1
        static StoryEvent se1 = new StoryEvent("Мы отправились на поиски артефакта, который может изменить ход войны " +
            "с Зергами. Моя команда и я уже преодолели множество препятствий и рисков, но мы не остановимся, " +
            "пока не достигнем своей цели. Мы готовы на все ради спасения человечества.");
        static StoryEvent se2 = new StoryEvent("Мы прибыли на планету, где, по слухам, находится артефакт. " +
            "Но мы не знаем точно, где его искать. Нам предстоит провести расследование и найти следы, " +
            "которые помогут нам достичь нашей цели.");

        //Акт 2
        static StoryEvent se3 = new StoryEvent("Мы начали расследование на планете, ища следы, которые могут привести нас к артефакту." +
            " Мы обнаружили несколько мест, которые могут быть связаны с ним, и теперь мы будем исследовать их более детально.");
        static StoryEvent se4 = new StoryEvent("Вы ищете артефакт? Я знаю, где он находится. Но я могу помочь вам только в " +
            "обмен на вашу помощь в защите от нападения Зергов.");
        static StoryEvent se5 = new StoryEvent("Мы поможем вам защититься от Зергов, а вы покажете нам, где находится артефакт.");
        static StoryEvent se6 = new StoryEvent("Зерги атакуют! Все на позиции! Мы должны помочь местным жителям защитить свои дома!");
        static StoryEvent se7 = new StoryEvent("Взгляните на это! Эти следы ведут прямо к месту, где должен находиться артефакт!");
        static StoryEvent se8 = new StoryEvent("Я знаю, что вы ищете артефакт. Но я тоже ищу его. Предлагаю объединить наши усилия.");
        static StoryEvent se9 = new StoryEvent("Никогда не доверяйте политикам. Но мы не можем отказаться от помощи в поиске артефакта.");

        //Акт 3
        static StoryEvent se13 = new StoryEvent("");
        static StoryEvent se14 = new StoryEvent("");
        static StoryEvent se15 = new StoryEvent("");
        static StoryEvent se16 = new StoryEvent("");
        static StoryEvent se17 = new StoryEvent("");
        static StoryEvent se18 = new StoryEvent("");
        static StoryEvent se19 = new StoryEvent("");
        static StoryEvent se20 = new StoryEvent("");
        static StoryEvent se21 = new StoryEvent("");
        static StoryEvent se22 = new StoryEvent("");
        static StoryEvent se23 = new StoryEvent("");
        static StoryEvent se24 = new StoryEvent("");
        static StoryEvent se25 = new StoryEvent("");
        static StoryEvent se26 = new StoryEvent("");
        static StoryEvent se27 = new StoryEvent("");
        static StoryEvent se28 = new StoryEvent("");
        static StoryEvent se29 = new StoryEvent("");
        static StoryEvent se30 = new StoryEvent("");
        static StoryEvent se31 = new StoryEvent("");
        static StoryEvent se32 = new StoryEvent("");

        //Акт1_1
        static StoryEvent se1_1 = new StoryEvent("Добро пожаловать, господа. Мы знаем, что вы опытные бойцы " +
            "и можем предложить вам работу.");
        static StoryEvent se2_1 = new StoryEvent("Что за работа?");
        static StoryEvent se3_1 = new StoryEvent("Мы столкнулись с новой угрозой для нашего мира. Мы нуждаемся " +
            "в вашей помощи, чтобы ее предотвратить.");
        static StoryEvent se4_1 = new StoryEvent("Мы обнаружили, что другая раса начала экспериментировать с " +
            "генетическими модификациями наших существ. Они планируют создать новый вид Зергов, который будет " +
            "еще опаснее и агрессивнее.");
        static StoryEvent se5_1 = new StoryEvent("Мы должны быть готовы ко всему. Это может быть очень опасно.");
        static StoryEvent se6_1 = new StoryEvent("Нам нужно уничтожить все образцы и материалы, которые они используют " +
            "для создания нового вида Зергов! Не дайте им остановить нас! Продолжайте двигаться вперед!");
        static StoryEvent se7_1 = new StoryEvent("Мы справились! Новый вид Зергов не будет создан!");
        static StoryEvent se8_1 = new StoryEvent("Вы сделали отличную работу. Мы будем помнить вашу помощь в будущем.");

        //Акт2_1
        static StoryEvent se9_1 = new StoryEvent("Добро пожаловать, господа. Мы знаем о вашей борьбе с Зергами и хотели бы предложить вам помощь.");
        static StoryEvent se10_1 = new StoryEvent("Что вы можете нам предложить?");
        static StoryEvent se11_1 = new StoryEvent("Мы можем предоставить вам нашу технологию и силу, чтобы помочь вам победить Зергов.");
        static StoryEvent se12_1 = new StoryEvent("Мы знаем, что Зерги планируют атаку на нашу базу. Нам нужна ваша помощь, чтобы защитить ее.");
        static StoryEvent se13_1 = new StoryEvent("Мы готовы помочь вам. Мы также знаем о другой угрозе для наших миров - Терранской империи.");
        static StoryEvent se14_1 = new StoryEvent("Мы должны быть готовы ко всему. Это может быть очень опасно.");
        static StoryEvent se15_1 = new StoryEvent("Не дайте им пройти! Мы должны защитить нашу базу!");
        static StoryEvent se16_1 = new StoryEvent("Мы не дадим им пройти! Сражайтесь до конца!");
        static StoryEvent se17_1 = new StoryEvent("Мы справились! Спасибо за вашу помощь!");
        static StoryEvent se18_1 = new StoryEvent("Мы будем помогать вам в борьбе против Зергов. И мы надеемся, " +
            "что вы поможете нам в борьбе против Терранской империи.");

        //Акт 3_1
        static StoryEvent se19_1 = new StoryEvent("");
        static StoryEvent se20_1 = new StoryEvent("");
        static StoryEvent se21_1 = new StoryEvent("");
        static StoryEvent se22_1 = new StoryEvent("");
        static StoryEvent se23_1 = new StoryEvent("");
        static StoryEvent se24_1 = new StoryEvent("");
        static StoryEvent se25_1 = new StoryEvent("");
        static StoryEvent se26_1 = new StoryEvent("");
        static StoryEvent se27_1 = new StoryEvent("");
        static StoryEvent se28_1 = new StoryEvent("");
        static StoryEvent se29_1 = new StoryEvent("");
        static StoryEvent se30_1 = new StoryEvent("");
        static StoryEvent se31_1 = new StoryEvent("");
        static StoryEvent se32_1 = new StoryEvent("");

        //// Story Cards Start ////

        //Акт 1

        static StoryCard<Hero, StoryEvent> card1 =
               StoryCard<Hero, StoryEvent>.Begin("Вступительный монолог Джима Рейнора")
                                          .Reaction(h1, se1)
                                          .Location("Терра (Начало игры)")
                                          .Time(0)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card2 =
               StoryCard<Hero, StoryEvent>.Begin("Джим и его команда прибывают на планету")
                                          .Reaction(h1, se2)
                                          .Location("Мар Сара")
                                          .Time(1)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card1.value);

        //Акт 2

        static StoryCard<Hero, StoryEvent> card3 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора начинает расследование на планете")
                                          .Reaction(h1, se3)
                                          .Location("Мар Сара")
                                          .Time(2)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card4 =
               StoryCard<Hero, StoryEvent>.Begin("Встреча с местными жителями")
                                          .Reaction(h8, se4)
                                          .Reaction(h1, se5)
                                          .Location("Деревня")
                                          .Time(3)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card3.value);

        static StoryCard<Hero, StoryEvent> card5 =
               StoryCard<Hero, StoryEvent>.Begin("Битва с Зергами")
                                          .Reaction(h1, se6)
                                          .Location("Деревня")
                                          .Time(4)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card4.value);

        static StoryCard<Hero, StoryEvent> card6 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора находит следы, которые могут привести их к артефакту")
                                          .Reaction(h2, se7)
                                          .Location("Древний храм")
                                          .Time(5)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card5.value);

        static StoryCard<Hero, StoryEvent> card7 =
               StoryCard<Hero, StoryEvent>.Begin("Встреча с Арктуром Менгском")
                                          .Reaction(h5, se8)
                                          .Reaction(h1, se9)
                                          .Location("Выход из храма")
                                          .Time(6)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card6.value);


        //Акт 3

        static StoryCard<Hero, StoryEvent> card8 =
               StoryCard<Hero, StoryEvent>.Begin("")
                                          .Reaction(h1, se1)
                                          .Location("")
                                          .Time(7)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        //Акт 1_1

        static StoryCard<Hero, StoryEvent> card1_1 =
               StoryCard<Hero, StoryEvent>.Begin("Встреча с представителями Зергов")
                                          .Reaction(h7, se1_1)
                                          .Location("Зерус")
                                          .Time(0)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card2_1 =
               StoryCard<Hero, StoryEvent>.Begin("Задание от представителями Зергов")
                                          .Reaction(h1, se2_1)
                                          .Reaction(h7, se3_1)
                                          .Location("Зерус")
                                          .Time(1)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card1_1.value);

        static StoryCard<Hero, StoryEvent> card3_1 =
               StoryCard<Hero, StoryEvent>.Begin("Представители Зергов делятся информацией")
                                          .Reaction(h7, se4_1)
                                          .Location("Зерус")
                                          .Time(2)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card2_1.value);

        static StoryCard<Hero, StoryEvent> card4_1 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора готовится к миссии")
                                          .Reaction(h1, se5_1)
                                          .Location("Вход в лабаратирию")
                                          .Time(3)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card3_1.value);

        static StoryCard<Hero, StoryEvent> card5_1 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора нападает на лабораторию")
                                          .Reaction(h1, se6_1)
                                          .Location("Лаборатория")
                                          .Time(4)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card4_1.value);

        static StoryCard<Hero, StoryEvent> card6_1 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора завершает миссию")
                                          .Reaction(h1, se7_1)
                                          .Reaction(h7, se8_1)
                                          .Location("Зерус")
                                          .Time(5)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => card5_1.value);

        //Акт 2_1

        static StoryCard<Hero, StoryEvent> card7_1 =
               StoryCard<Hero, StoryEvent>.Begin("Встреча с представителями Протоссов")
                                          .Reaction(h9, se9_1)
                                          .Location("Шакурас")
                                          .Time(6)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card8_1 =
               StoryCard<Hero, StoryEvent>.Begin("Задание от представителями Протоссов")
                                          .Reaction(h1, se10_1)
                                          .Reaction(h9, se11_1)
                                          .Location("Шакурас")
                                          .Time(7)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card9_1 =
               StoryCard<Hero, StoryEvent>.Begin("Представители Протоссов и команда Джима Рейнора обмениваются информацией")
                                          .Reaction(h9, se12_1)
                                          .Reaction(h1, se13_1)
                                          .Location("Храм Протосов")
                                          .Time(8)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card10_1 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора и представители Протоссов готовятся к атаке на Зергов")
                                          .Reaction(h1, se14_1)
                                          .Location("База Протосов")
                                          .Time(9)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card11_1 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора и представители Протоссов атакуют Зергов")
                                          .Reaction(h1, se15_1)
                                          .Reaction(h9, se16_1)
                                          .Location("База Протосов")
                                          .Time(10)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        static StoryCard<Hero, StoryEvent> card12_1 =
               StoryCard<Hero, StoryEvent>.Begin("Команда Джима Рейнора и представители Протоссов заключают союз")
                                          .Reaction(h1, se16_1)
                                          .Reaction(h9, se17_1)
                                          .Location("Шакурас")
                                          .Time(11)
                                          .Bit(1, 1)
                                          .Change_Tensity(1)
                                          .Rule(() => true);

        //Акт 3_1

        //// Story Cards End ////

        //// Story Events End ////

        static StoryManager<Hero, StoryCard<Hero, StoryEvent>> sm =
               StoryManager<Hero, StoryCard<Hero, StoryEvent>>.Begin()
                                                              .Heroes(h1, h2, h3, h4, h5, h6, h7, h8, h9)
                                                              .ChangeStoryCurve(0, 1, 2, 3, 3, 2, 1, 2, 3, 5, 3, 2, 1, 2, 3, 3, 2, 1, 2, 3, 5, 3, 2, 1)
                                                              .Cards(card1, card2, card3, card4, card5, card6, card7, card1_1, card2_1, card3_1, card4_1, card5_1, card6_1, card7_1, card8_1, card9_1, card10_1, card11_1, card12_1);

        //static StoryEvent se1 = new StoryEvent("Тайкус рассказывает, что Доминион и Фонд ищут артефакты пришельцев, " +
        //                                       "и предлагает Рейнору работать на Фонд, который выдает вознаграждение людям, нашедшим их");
        //static StoryEvent se2 = new StoryEvent("Сара Керриган приказывает зергам начать наступление на Доминион");
        //static StoryEvent se3 = new StoryEvent("Джим Рейнор на Мар Саре перехватывает у Доминиона первый артефакт");
        //static StoryEvent se4 = new StoryEvent("Тайкус Финдли на Мар Саре перехватывает у Доминиона первый артефакт");
        //static StoryEvent se5 = new StoryEvent("Валериан сказал, что он может дать Рейнору шанс спасти Сару Керриган");
        //static StoryEvent se6 = new StoryEvent("Зератул попросил Джима Рейнора встретиться с  Валериан Менгск");
        //static StoryEvent se7 = new StoryEvent("Зератул рассказал, что ему  открылась страшная правда — загадочный " +
        //                                       "«Падший» хочет уничтожить все живое во Вселенной, после чего мир погрузится " +
        //                                       "в кромешную тьму. Зератул рассказал, что первым такое будущее увидел Сверхразум " +
        //                                       "зергов и нашёл единственное спасение в лице Королевы Клинков");
        //static StoryEvent se8 = new StoryEvent("Джим спасает Сару Керриган и убивает предателя Тайкуса");
        //static StoryEvent se9 = new StoryEvent("Тайкус попытался убить Джима Рейнора, но у него ничего не вышло");

        //static StoryCard<Hero, StoryEvent> card1 = 
        //       StoryCard<Hero, StoryEvent>.Begin("Джим Рейнор встретил Тайкус Финдли")
        //                                  .Reaction(h1, se1)
        //                                  .Location("Мар-Сара")
        //                                  .Time(0)
        //                                  .Heroes(h1)
        //                                  .Bit(1, 2)
        //                                  .Change_Tensity(1)
        //                                  .Rule(() => true);

        //static StoryCard<Hero, StoryEvent> card2 =
        //       StoryCard<Hero, StoryEvent>.Begin("Сара Керриган приказывает зергам начать наступление на Доминион")
        //                                  .Reaction(h3, se2)
        //                                  .Location("Мар-Сара")
        //                                  .Time(0)
        //                                  .Heroes(h3)
        //                                  .Bit(1, 1)
        //                                  .Change_Tensity(1)
        //                                  .Rule(() => true);

        //static StoryCard<Hero, StoryEvent> card3 =
        //       StoryCard<Hero, StoryEvent>.Begin("Джим Рейнор и Тайкус Финдли находят артефакты")
        //                                  .Reaction(h1, se3)
        //                                  .Reaction(h2, se4)
        //                                  .Location("Мар-Сара")
        //                                  .Time(1)
        //                                  .Heroes(h1, h2)
        //                                  .Bit(4, 1)
        //                                  .Change_Tensity(0)
        //                                  .Rule(() => card1.value && !card2.value);

        //static StoryCard<Hero, StoryEvent> card4 =
        //       StoryCard<Hero, StoryEvent>.Begin("Джим Рейнор встретил Валериан Менгск")
        //                                  .Reaction(h6, se5)
        //                                  .Location("Гиперион")
        //                                  .Time(2)
        //                                  .Heroes(h6)
        //                                  .Bit(1, 1)
        //                                  .Change_Tensity(2)
        //                                  .Rule(() => card3.value || !card5.value);

        //static StoryCard<Hero, StoryEvent> card5 =
        //       StoryCard<Hero, StoryEvent>.Begin("Зератул встретил Джим Рейнор")
        //                                  .Reaction(h4, se6)
        //                                  .Location("Гиперион")
        //                                  .Time(1)
        //                                  .Heroes(h4)
        //                                  .Bit(1, 1)
        //                                  .Change_Tensity(1)
        //                                  .Rule(() => card1.value && !card3.value);

        //static StoryCard<Hero, StoryEvent> card6 =
        //       StoryCard<Hero, StoryEvent>.Begin("Зератул рассказал Джим Рейнор и Тайкус Финдли о Падшем")
        //                                  .Reaction(h4, se7)
        //                                  .Location("Чар")
        //                                  .Time(3)
        //                                  .Heroes(h4)
        //                                  .Bit(1, 1)
        //                                  .Change_Tensity(-2)
        //                                  .Rule(() => card4.value);

        //static StoryCard<Hero, StoryEvent> card7 =
        //       StoryCard<Hero, StoryEvent>.Begin("Тайкус Финдли придает Джим Рейнор")
        //                                  .Reaction(h1, se8)
        //                                  .Reaction(h2, se9)
        //                                  .Location("Чар")
        //                                  .Time(4)
        //                                  .Heroes(h1, h2)
        //                                  .Bit(1, 1)
        //                                  .Change_Tensity(1)
        //                                  .Rule(() => card5.value);

        //static StoryManager<Hero, StoryCard<Hero, StoryEvent>> sm = 
        //       StoryManager<Hero, StoryCard<Hero, StoryEvent>>.Begin()
        //                                                      .Heroes(h1, h2, h3, h4, h5, h6)
        //                                                      .ChangeStoryCurve(0, 1, 2, 3, 3, 2, 1, 2, 3, 5, 3, 2, 1)
        //                                                      .Cards(card1, card2, card3, card4, card5, card6, card7);

        static void Main(string[] args)
        {

            sm.Do();

        }

    }
}