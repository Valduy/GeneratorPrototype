using System;
using System.Collections.Generic;
using Edgar.Geometry;
using Edgar.GraphBasedGenerator.Grid2D;
using Edgar.GraphBasedGenerator.Grid2D.Drawing;
using Edgar.Graphs;

using System.IO;
using System.Linq;

namespace DungeonViz
{
    class Program
    {
		public class Generator
		{
			public Generator(int room_count)
			{
				signature = new List<int> { 0, 1, 2, 3, 2, 4, 6, 8, 7, 8, 9 };
				rooms = room_count;

				g = new UndirectedAdjacencyListGraph<string>();

				Gen();
			}


			public UndirectedAdjacencyListGraph<string> g;

			public List<int> signature = new List<int>();
			public int rooms;

			public List<string> pred = new List<string>();
			public List<string> current = new List<string>();
			public List<string> events = new List<string>();

			public List<int> room_counts = new List<int>();

			string room_events = "";


			public void Reno(int border)
			{
				int exp = 2;
				//int lin = 1;

				int complexity_lost = 1;

				room_counts.Add(1);

				int full_complexity = signature.Sum();

				//Отнимаем начало и конец
				int rooms_remain = rooms - 2;

				for (int i = 1; i < signature.Count; i++)
				{
					if (rooms_remain <= 0)
					{
						room_counts.Add(0);

						int lost = (int)MathF.Ceiling(full_complexity / (rooms - 2));

						float coef = (float)rooms / signature.Count;

						int new_signature = (int)MathF.Ceiling((signature[complexity_lost] + lost) * coef);
						signature[complexity_lost] = new_signature;

						//for (int j = complexity_lost; j < i; j++)
						//{
							//int lost = (int)MathF.Ceiling(signature[i] / (signature.Count - i));
							//int new_signature = (signature[j] + lost) / 2 + 1;

							
						//}

						complexity_lost += 1;
					}
					else
					{
						if (i < border && room_counts[room_counts.Count - 1] >= 4)
						{
							if (rooms_remain > room_counts[room_counts.Count - 1] * exp)
							{
								room_counts.Add(room_counts[room_counts.Count - 1] * exp);
								rooms_remain -= room_counts[room_counts.Count - 1];
							}
							else
							{
								room_counts.Add(1);
								rooms_remain -= room_counts[room_counts.Count - 1];
							}
						}
						else
						{
							if (rooms_remain > room_counts[room_counts.Count - 1] + 1)
							{
								room_counts.Add(room_counts[room_counts.Count - 1] + 1);
								rooms_remain -= room_counts[room_counts.Count - 1];
							}
							else
							{
								room_counts.Add(1);
								rooms_remain -= room_counts[room_counts.Count - 1];
							}
						}

						full_complexity -= signature[i];
					}
				}
			}

			public void Add_Event(string name)
			{
				int c = 0;

				while (g.Vertices.Contains(name + c))
				{
					c++;
				}

				events.Add(name + c);
			}

			public void Add_StringEvent(string name)
			{
				room_events += ("\n" + name);
			}

			public void High_Event(ref int dd)
			{
				while (dd - 8 >= 0)
				{
					Add_StringEvent("Босс: +8");

					dd -= 8;
				}

				while (dd - 6 >= 0)
				{
					Add_StringEvent("Артефакт: +6");

					dd -= 6;
				}
			}

			public void Mid_Event(ref int dd)
			{
				while (dd - 4 >= 0)
				{
					Add_StringEvent("Противник: +4");

					dd -= 4;
				}

				while (dd - 3 >= 0)
				{
					Add_StringEvent("Диалог: +3");

					dd -= 3;
				}


			}

			public void Low_Event(ref int dd)
			{
				while (dd - 2 >= 0)
				{
					Add_StringEvent("Предмет: +2");

					dd -= 2;
				}

				while (dd - 1 >= 0)
				{
					var r = new Random();
					int v = r.Next(1, 4);


					switch (v)
					{
						case 1:
							Add_StringEvent("Секрет: +1");
							break;
						case 2:
							Add_StringEvent("Кнопка: +1");
							break;
						case 3:
							Add_StringEvent("КатСцена: +1");
							break;
						default:
							break;
					}

					dd -= 1;
				}
			}

			public void Gen()
			{
				Reno(3);
				//int rooms_on_layr = std::ceil( rooms / (signature.size() - 2));

				pred.Add("Начало");
				g.AddVertex("Начало");

				for (int i = 1; i < signature.Count - 1; i++)
				{
					for (int j = 0; j < room_counts[i]; j++)
					{


						int d = signature[i];

						//High_Event(ref d);
						//Mid_Event(ref d);
						//Low_Event(ref d);

						//for ( auto ev : events)
						//{
						//	g.AddEdge(name, { ev });
						//}

						//events.Clear();

						string name = "Room_" + i + "_" + j + room_events;
						//string name = "+" + signature[i].ToString();
						current.Add(name);

						room_events = "";
					}

					foreach (var item in current)
					{
						if (!g.Vertices.Contains(item))
						{
							g.AddVertex(item);

						}
					}

					if (current.Count != 0)
					{
						if (pred.Count <= current.Count)
						{
							for (int l = 0; l < pred.Count; l++)
							{
								//if(!g.Vertices.Contains(current[l]))
								//{
								//	g.AddVertex(current[l]);
								//}

								g.AddEdge(pred[l], current[l]);
							}

							for (int k = pred.Count; k < current.Count; k++)
							{
								var r = new Random();

								int ii = r.Next(0, pred.Count);
								g.AddEdge(pred[ii], current[k]);
							}

						}
						else
						{
							for (int m = 0; m < current.Count; m++)
							{
								//if (!g.Vertices.Contains(current[m]))
								//{
								//	g.AddVertex(current[m]);
								//}

								g.AddEdge(pred[m], current[m]);
							}

							for (int z = current.Count; z < pred.Count; z++)
							{
								var r = new Random();

								int ii = r.Next(0, current.Count);
								g.AddEdge(pred[z], current[ii]);
							}
						}

						for (int v = 0; v < current.Count / 2; v++)
						{
							var r = new Random();

							int ii = r.Next(0, 2);

							//std::cout << ii << std::endl;

							if (ii != 0)
							{
								if (!g.HasEdge(current[i - 1], current[i]))
								{
									g.AddEdge(current[i - 1], current[i]);
								}
							}

						}

						pred.Clear();

						pred.Add(current[0]);

						for (int q = 1; q < current.Count; q++)
						{
							var rr = new Random();
							int ii = rr.Next(0, 2);

							if (ii != 0)
							{
								pred.Add(current[q]);
							}
						}

						//pred = new List<string>(current);
						current.Clear();
					}
				}

				g.AddVertex("Конец");
				foreach (var p in pred)
				{
					g.AddEdge(p, "Конец");
				}
			}

			public void Print(string path)
			{
				StreamWriter sw = new StreamWriter(path);

				string res = "";

				foreach (var vertex1 in g.Vertices)
				{
					foreach (var vertex2 in g.Vertices)
					{
						if (g.HasEdge(vertex1, vertex2))
						{
							res += 1;
							res += " ";
						}
						else
						{
							res += 0;
							res += " ";
						}
					}
					res = res.Trim();
					sw.WriteLine(res);
					res = "";
				}
				sw.Close();
			}

			public void DFS(Dictionary<string, bool> vertexes, List<string> Path, string vertex, string output)
            {
                if (!vertexes.ContainsKey(vertex))
                {
                    vertexes.Add(vertex, true);

                    foreach (var neighbor in g.GetNeighbours(vertex))
                    {
						Path.Add(neighbor);
                        DFS(vertexes, Path, neighbor, output + "->" + neighbor);
                    }

					string s = "";
					foreach (var v in Path)
					{
						s += (v + "->");
					}

					Console.WriteLine(s);
                }

    //            else if (vertexes[vertex] == false)
    //            {
    //                vertexes[vertex] = true;

    //                foreach (var neighbor in g.GetNeighbours(vertex))
    //                {
				//		Path.Add(neighbor);
				//		DFS(vertexes, Path, neighbor, output + "->" + neighbor);
    //                }

				//	string s = "";
				//	foreach(var v in Path)
    //                {
				//		s += (v + "->");
				//	}

				//	Console.WriteLine(s);
				//}

                //if (!vertexes.ContainsKey(vertex))
                //{
                //	vertexes.Add(vertex, true);

                //	foreach (var neighbor in g.GetNeighbours(vertex))
                //	{
                //		DFS(vertexes, neighbor, output + "->" + neighbor);
                //	}

                //	Console.WriteLine(output);

                //}
            }

			public void Circules(Dictionary<string, bool> vertexes, List<string> Path, string vertex, string output)
			{
				if (true)
				{
					//vertexes.Add(vertex, true);

					foreach (var neighbor in g.GetNeighbours(vertex))
					{
						if(!Path.Contains(neighbor))
                        {
							Path.Add(vertex);
							Circules(vertexes, Path, neighbor, output + "->" + neighbor);
						}
						else
                        {
							//Path.Add(vertex);
							string s = "";
							bool toogle = false;

							foreach (var v in Path)
							{
								if (v == vertex)
								{
									toogle = true;

								}

								if (toogle == true)
								{
									s += (v + "->");
								}
							}

							s += ("->" + vertex);

							Console.WriteLine(s);
						}
					}
				}
                else
                {
					
				}
			}

			public void new_DFS(string vertex, string end, Stack<string> path, HashSet<string> visited)
			{
				if(vertex == end)
                {
					PrintPath(path);
					return;
                }

				foreach (var neighbor in g.GetNeighbours(vertex))
				{
					if(!visited.Contains(neighbor))
                    {
						path.Push(neighbor);
						visited.Add(neighbor);

						new_DFS(neighbor, end, path, visited);

						path.Pop();
						visited.Remove(neighbor);

					}
                }
			}

			public UndirectedAdjacencyListGraph<string> gg = new UndirectedAdjacencyListGraph<string>();

			public void BFS_Plus(string vertex, int difficult)
			{
				Dictionary<string, bool> visited = new Dictionary<string, bool>();

				Queue<string> q = new Queue<string>();
				Queue<int> d = new Queue<int>();

				q.Enqueue(vertex);
				d.Enqueue(difficult);

				visited.Add(vertex, true);

				while (q.Count != 0)
                {
					string s = q.Dequeue();
					int c = d.Dequeue();
					
					vertex = s;
					difficult = c;

					if (g.GetNeighbours(vertex).Count() == 1)
                    {
						s += " - Тупик";
                    }
					else if (q.Count == 0)
					{
						s += " - Горло";
					}
					else
                    {
						s += " - Норма";
					}

					Console.WriteLine(s);

					foreach (var v in g.GetNeighbours(vertex))
					{
						if (!visited.ContainsKey(v))
						{
							visited.Add(v, true);
							q.Enqueue(v);
						}
					}
                }
			}

			public void PrintPath(Stack<string> path)
			{
				string s = "";

				foreach (var v in path)
				{
					s += (v + "->");
				}

				//s += ("->" + vertex);

				Console.WriteLine(s);
			}
		}

		public class Room
		{
			public string Name { get; }
			public Room(string name)
			{
				Name = name;
			}
			public override string ToString()
			{
				return Name;
			}
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

        public static void ReadGraph(string path)
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
            var levelDescription = new LevelDescriptionGrid2D<int>();
            var graph = new UndirectedAdjacencyListGraph<int>();

            var matrix = System.IO.File.ReadAllLines(path);

            graph.AddVerticesRange(0, matrix.Length);

            levelDescription.MinimumRoomDistance = 3;

            for (int i = 0; i < matrix.Length; i++)
            {
                var str = matrix[i].Split(" ");

                for (int j = 0; j < str.Length; j++)
                {
                    if (str[j] == "1")
                    {
                        if (!graph.HasEdge(i, j))
                        {
                            graph.AddEdge(i, j);
                        }

                    }
                }
            }

            foreach (var room in graph.Vertices)
            {
                levelDescription.AddRoom(room, basicRoomDescription);
            }

            var corridorCounter = graph.VerticesCount;

            foreach (var connection in graph.Edges)
            {
                // We manually insert a new room between each pair of neighboring rooms in the graph
                levelDescription.AddRoom(corridorCounter, corridorRoomDescription);
                // And instead of connecting the rooms directly, we connect them to the corridor room
                levelDescription.AddConnection(connection.From, corridorCounter);
                levelDescription.AddConnection(connection.To, corridorCounter);
                corridorCounter++;
            }


            var generator = new GraphBasedGeneratorGrid2D<int>(levelDescription);
            var layout = generator.GenerateLayout();

            var drawer = new DungeonDrawer<int>();
            drawer.DrawLayoutAndSave(layout, "layout.png", new DungeonDrawerOptions()
            {
                Width = 1000,
                Height = 1000,
            });

        }

		public static void MakeGraph(int rooms)
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
			var levelDescription = new LevelDescriptionGrid2D<string>();
			levelDescription.MinimumRoomDistance = 1;

			////
			var gen = new Generator(rooms);
			////
			var graph = gen.g;
			////
			//gen.new_DFS("Начало", "Конец", new Stack<string>(), new HashSet<string>());
			gen.BFS_Plus("Начало", 0);

            foreach (var room in graph.Vertices)
            {
                levelDescription.AddRoom(room, basicRoomDescription);
            }

            var corridorCounter = graph.VerticesCount;

            foreach (var connection in graph.Edges)
            {
                // We manually insert a new room between each pair of neighboring rooms in the graph
                levelDescription.AddRoom(corridorCounter.ToString(), corridorRoomDescription);

                // And instead of connecting the rooms directly, we connect them to the corridor room
                levelDescription.AddConnection(connection.From, corridorCounter.ToString());
                levelDescription.AddConnection(connection.To, corridorCounter.ToString());
                corridorCounter++;
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

		static void Main(string[] args)
        {

			//ReadGraph(@"D:\DungeonViz\DungeonViz\my_graph.txt");

			//var gen = new Generator(8);
			//gen.DFS(new Dictionary<string, bool>(), new List<string>(), "Начало", "Начало");

			MakeGraph(8);

			Console.WriteLine("Hello World!");
        }
    }
}
