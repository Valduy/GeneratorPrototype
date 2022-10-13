using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edgar.Graphs;

namespace GeneratorDungeonGraph
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

			public void Reno(int border)
			{
				int exp = 2;
				//int lin = 1;

				int complexity_lost = 1;

				room_counts.Add(1);

				//Отнимаем начало и конец
				int rooms_remain = rooms - 2;

				for (int i = 1; i < signature.Count; i++)
				{
					if (rooms_remain <= 0)
					{
						room_counts.Add(0);

						for (int j = complexity_lost; j < i; j++)
						{
							int lost = (int)MathF.Ceiling(signature[i] / (signature.Count - i));
							int new_signature = (signature[j] + lost) / 2 + 1;
							signature[j] = new_signature;
						}

						complexity_lost += 1;
					}
					else
					{
						if (i < border)
						{
							if(rooms_remain > room_counts[room_counts.Count - 1] * exp)
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
							if(rooms_remain > room_counts[room_counts.Count - 1] + 1)
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

			public void High_Event(ref int dd)
			{
				while (dd - 8 >= 0)
				{
					Add_Event("Босс_");

					dd -= 8;
				}

				while (dd - 6 >= 0)
				{
					Add_Event("Артефакт_");

					dd -= 6;
				}
			}

			public void Mid_Event(ref int dd)
			{
				while (dd - 4 >= 0)
				{
					Add_Event("Противник_");

					dd -= 4;
				}

				while (dd - 3 >= 0)
				{
					Add_Event("Диалог_");

					dd -= 3;
				}


			}

			public void Low_Event(ref int dd)
			{
				while (dd - 2 >= 0)
				{
					Add_Event("Предмет_");

					dd -= 2;
				}

				while (dd - 1 >= 0)
				{
					var r = new Random();
					int v = r.Next(1, 4);


					switch (v)
					{
						case 1:
							Add_Event("Секрет_");
							break;
						case 2:
							Add_Event("Кнопка_");
							break;
						case 3:
							Add_Event("КатСцена_");
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
						string name = "Room_" + i + " " + j;
						current.Add(name);

						int d = signature[i];

						High_Event(ref d);
						Mid_Event(ref d);
						Low_Event(ref d);

						//for ( auto ev : events)
						//{
						//	g.AddEdge(name, { ev });
						//}

						//events.Clear();
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
								if(!g.HasEdge(current[i - 1], current[i]))
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
						if(g.HasEdge(vertex1, vertex2))
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
		}


		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			var gen = new Generator(10);
			gen.Print(@"D:\DungeonViz\DungeonViz\my_graph.txt");

		}
	}
}
