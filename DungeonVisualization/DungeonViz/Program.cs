using System;
using System.Collections.Generic;
using Edgar.Geometry;
using Edgar.GraphBasedGenerator.Grid2D;
using Edgar.GraphBasedGenerator.Grid2D.Drawing;
using Edgar.Graphs;

using System.IO;
using System.Linq;
using System.Diagnostics;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace DungeonViz
{
    class Program
    {
		public enum RoomType
		{
			Normal, Hub, Spawn, Boss, Corridor, Exit, Reward, Shop,
		}

		public class PlayerState
        {
			public int hp;
			public int damage;
			public int attack_speed;
			public string Event = "No Event";

			public int AidKits;

			public int time_to_kill = 0;
			public (int, int) damage_chance;

			public (int, int) lore;

			public bool IsKilled = false;

			public PlayerState()
			{
				hp = 100;
				damage = 10;
				attack_speed = 2;
				damage_chance = (3, 6);

				
			}

			public PlayerState(int h, int d, int s, int a, (int, int) dc, string e, (int, int) l)
			{
				hp = h;
				damage = d;
				attack_speed = s;

				AidKits = a;

				damage_chance = dc;
				Event = e;

				lore = l;
			}

			public PlayerState(PlayerState player)
			{
				hp = player.hp;
				damage = player.damage;
				attack_speed = player.attack_speed;
				damage_chance = player.damage_chance;

				AidKits = player.AidKits;

				Event = player.Event;
				time_to_kill = player.time_to_kill;
				lore = player.lore;
			}

			public bool Fight(MonsterState monster)
            {
				time_to_kill = 0;

				while (monster.hp >= 0)
                {
					for(int i = 0; i < attack_speed; i++)
                    {
						if(RollTheDice())
                        {
							monster.hp -= damage;

							time_to_kill++;
						}
                    }

					for (int i = 0; i < monster.attack_speed; i++)
					{
						if(monster.RollTheDice())
						{
							hp -= monster.damage;
						}

						if(hp <= 0)
						{
							IsKilled = true;
							return false;
						}

						if(hp <= 75 && AidKits > 0)
                        {
							hp += 25;

							AidKits--;
						}
					}
				}

				return true;
            }

			bool RollTheDice()
            {
				var r = new Random();
				int chance = r.Next(damage_chance.Item1, damage_chance.Item2);

				if (chance <= damage_chance.Item1)
                {
					return true;
				}

				return false;
			}
		}

		public class MonsterState
		{
			public int hp;
			public int damage;
			public int attack_speed;

			public (int, int) damage_chance;

			public MonsterState()
			{
				hp = 100;
				damage = 10;
				attack_speed = 1;
				damage_chance = (5, 6);
			}

			public MonsterState(int h, int d, int s, (int, int) dc)
			{
				hp = h;
				damage = d;
				attack_speed = s;
				damage_chance = dc;
			}

			public MonsterState(MonsterState monster)
			{
				hp = monster.hp;
				damage = monster.damage;
				attack_speed = monster.attack_speed;
				damage_chance = monster.damage_chance;
			}

			public bool RollTheDice()
			{
				var r = new Random();
				int chance = r.Next(damage_chance.Item1, damage_chance.Item2);

				if (chance <= damage_chance.Item1)
				{
					return true;
				}

				return false;
			}
		}

		public class Generator
		{
			public Generator(int room_count, (int, int, int, int, int, int) settings)
			{
				signature = new List<int> { 0, 1, 2, 3, 2, 4, 6, 8, 7, 8, 9 };

				rooms = room_count;
				gen_settings = settings; 

				g = new UndirectedAdjacencyListGraph<string>();

				Gen();
			}

			(int, int, int, int, int, int) gen_settings;

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
				int exp = gen_settings.Item2;
				//int lin = 1;

				int complexity_lost = 1;

				room_counts.Add(1);
				room_counts.Add(1);

				int full_complexity = signature.Sum();

				//Отнимаем начало и конец
				int rooms_remain = rooms - 2;

				for (int i = 2; i < signature.Count; i++)
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
						if (i < gen_settings.Item1 && room_counts[room_counts.Count - 1] <= gen_settings.Item3)
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
							if (rooms_remain > room_counts[room_counts.Count - 1] + 1 && room_counts[room_counts.Count - 1] <= gen_settings.Item3)
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

				var config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					NewLine = Environment.NewLine,
					HasHeaderRecord = false, // Don't write the header again.
				};

				var records = new List<Dufficult>
				{
					new Dufficult {difficult = room_counts},
				};

				using (var stream = File.Open("reno.csv", FileMode.Append))
				using (var writer = new StreamWriter(stream))
				using (var csv = new CsvWriter(writer, config))
				{
					csv.WriteRecords(records);
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

						//Генератор связей между комнатами на одном уровне
						for (int v = 0; v < current.Count / 2; v++)
						{
							var r = new Random();

							int ii = r.Next(0, gen_settings.Item4);

							//std::cout << ii << std::endl;

							if (ii == 0)
							{
								if (!g.HasEdge(current[v], current[v + 1]))
								{
									g.AddEdge(current[v], current[v + 1]);
								}
							}

						}

						pred.Clear();

						//Гарантия сущетвования хотя бы одной связи на след уровне
						pred.Add(current[0]);

						//Генератор тупиков
						for (int q = 1; q < current.Count; q++)
						{
							var rr = new Random();
							int ii = rr.Next(0, gen_settings.Item5);

							if (ii == 0)
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

			public void OldGen()
			{
				Reno(3);
				//int rooms_on_layr = std::ceil( rooms / (signature.size() - 2));

				pred.Add("Начало");
				g.AddVertex("Начало");

				for (int i = 1; i < signature.Count - 1; i++)
				{
					for (int j = 0; j < room_counts[i]; j++)
					{

						int dd = signature[i];
						int d = signature[i];

						High_Event(ref d);
						Mid_Event(ref d);
						Low_Event(ref d);

						//for ( auto ev : events)
						//{
						//	g.AddEdge(name, { ev });
						//}

						//events.Clear();

						string name = "Room_" + i + "_" + j + room_events;
						//string name = "+" + signature[i].ToString();
						current.Add(name);

						room_events = "";

						difficulty.Add(name, dd - d);
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

							if (ii != 0 && i < current.Count())
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

			//public void DFS(Dictionary<string, bool> vertexes, List<string> Path, string vertex, string output)
   //         {
   //             if (!vertexes.ContainsKey(vertex))
   //             {
   //                 vertexes.Add(vertex, true);

   //                 foreach (var neighbor in g.GetNeighbours(vertex))
   //                 {
			//			Path.Add(neighbor);
   //                     DFS(vertexes, Path, neighbor, output + "->" + neighbor);
   //                 }

			//		string s = "";
			//		foreach (var v in Path)
			//		{
			//			s += (v + "->");
			//		}

			//		Console.WriteLine(s);
   //             }
   //         }

			public UndirectedAdjacencyListGraph<string> gg = new UndirectedAdjacencyListGraph<string>();

			public void new_DFS(string vertex, string end, Stack<string> path, HashSet<string> visited)
			{
				if(vertex == end)
                {
					PrintPath(path);
					return;
                }

				foreach (var neighbor in gg.GetNeighbours(vertex))
				{
					if(!visited.Contains(neighbor))
                    {
						path.Push(neighbor);
						visited.Add(neighbor);

						new_DFS(neighbor, end, path, visited);

						path.Pop();
						visited.Remove(neighbor);

					}
					else if(gg.GetNeighbours(vertex).Count() == 1)
                    {
						path.Push(neighbor);
						//visited.Add(neighbor);

						new_DFS(neighbor, end, path, visited);

						path.Pop();
						//visited.Remove(neighbor);
					}
                }
			}

			public PlayerState ProcessRoom(PlayerState player, string vertex)
            {
				if(vertex.Contains("Босс"))
                {
					var p = new PlayerState(player.hp, player.damage, player.attack_speed, player.AidKits, player.damage_chance, "Бой с Боссом", (player.lore.Item1 + 1, player.lore.Item2));
					var m = new MonsterState(200, 15, 2, (3,6));

					p.Fight(m);

					return p;
                }
				else if(vertex.Contains("Артефакт"))
                {

					return new PlayerState(player.hp, player.damage + 10, player.attack_speed, player.AidKits, player.damage_chance, "Получен артефакт", (player.lore.Item1, player.lore.Item2 + 1));
				}
				else if (vertex.Contains("Оружие"))
				{

					return new PlayerState(player.hp, player.damage + 20, player.attack_speed + 1, player.AidKits, player.damage_chance, "Получено оружие", (player.lore.Item1, player.lore.Item2));
				}
				else if (vertex.Contains("Секрет"))
				{

					return new PlayerState(player.hp + 5, player.damage + 5, player.attack_speed, player.AidKits, player.damage_chance, "Найден секрет", (player.lore.Item1, player.lore.Item2));
				}
				else if (vertex.Contains("Предмет"))
				{

					return new PlayerState(player.hp + 10, player.damage, player.attack_speed, player.AidKits, player.damage_chance, "Найден предмет", (player.lore.Item1, player.lore.Item2));
				}
				else if (vertex.Contains("Аптечка"))
				{

					return new PlayerState(player.hp, player.damage, player.attack_speed, player.AidKits += 1, player.damage_chance, "Найдена аптечка", (player.lore.Item1, player.lore.Item2));
				}
				else if (vertex.Contains("Противник"))
				{

					var p = new PlayerState(player.hp, player.damage, player.attack_speed, player.AidKits, player.damage_chance, "Бой с Противником", (player.lore.Item1, player.lore.Item2));
					var m = new MonsterState();

					p.Fight(m);

					return p;
				}
				else
				{
					return new PlayerState(player.hp, player.damage, player.attack_speed, player.AidKits, player.damage_chance, "Что-то пошло не так", (player.lore.Item1, player.lore.Item2));
				}
			}

			public void SpeedRunSimulation(string vertex, string end, PlayerState player, Stack<PlayerState> path, HashSet<string> visited)
			{
				if (vertex == end)
				{
					PrintPlayerStats(path);
					return;
				}

				foreach (var neighbor in gg.GetNeighbours(vertex))
				{
					if (!visited.Contains(neighbor))
					{

						PlayerState p = ProcessRoom(player, neighbor);

						if(p.IsKilled)
                        {
							return;
                        }

						path.Push(p);
						visited.Add(neighbor);

                        //BFS_Plus()

                        SpeedRunSimulation(neighbor, end, p, path, visited);

						path.Pop();
						visited.Remove(neighbor);

					}
				}
			}

			public void old_DFS(string vertex, string end, Stack<string> path, HashSet<string> visited)
			{
				if (vertex == end)
				{
					PrintPathOld(path);
					return;
				}

				foreach (var neighbor in g.GetNeighbours(vertex))
				{
					if (!visited.Contains(neighbor))
					{
						path.Push(neighbor);
						visited.Add(neighbor);

						new_DFS(neighbor, end, path, visited);

						path.Pop();
						visited.Remove(neighbor);

					}
					else if (g.GetNeighbours(vertex).Count() == 1)
					{
						path.Push(neighbor);

						new_DFS(neighbor, end, path, visited);

						path.Pop();
					}
				}
			}

			public string Scenario(int index)
            {
				if(index == 1)
                {
					return "_Откр.Сцена";
                }
				else if(index == g.Vertices.Count())
                {
					return "_Перех.";
				}
				else if(index < (rooms / 4 * 1))
                {
					return "_Ф.Темы";
				}
				else if(index < (rooms / 4 * 2))
                {
					return "_Уст.";
				}
				else if (index < (rooms / 4 * 3)) 
                {
					return "_Кат.";
				}
				else //if (index < (rooms - 1))
				{
					return "_Разм.";
				}

            }

			Dictionary<string, int> difficulty;

			public void BFS_Plus(string vertex, int difficult)
			{
				int dequeue_counter = 0;
				difficulty = new Dictionary<string, int>();

				Dictionary<string, (int, int, bool)> visited =new Dictionary<string, (int, int, bool)>();

				Queue<string> q = new Queue<string>();

				Dictionary<string, string> RenameMap = new Dictionary<string, string>();
				
				q.Enqueue(vertex);

				visited.Add(vertex, (0, 1, false));

				

				while (q.Count != 0)
                {
					string s = q.Dequeue();
					dequeue_counter++;

					int d = (int)MathF.Round(visited[s].Item1 / visited[s].Item2);
					
					vertex = s;

					if (g.GetNeighbours(vertex).Count() == 1)
                    {
						string ss = "";

						if(visited[s].Item3 == true)
                        {
							d += 0;
							ss += "_Ключ";

							g.AddVertex(s + "_Шорткат");

							g.AddEdge(s, s + "_Шорткат");

							g.AddEdge(s + "_Шорткат", g.GetNeighbours(vertex).First());
						}
						else
                        {

							if (s != "Конец")
							{
								ss = AddSecreetDynamic_Event(ref d);
							}
							else
							{
								ss = AddNormalDynamic_Event(ref d);
							}
						}

						//var scen = Scenario(dequeue_counter);

						//RenameMap.Add(s, s + " - Тупик" + ss + "\nОстаток: " + d + scen);
						RenameMap.Add(s, s + " - Тупик" + ss + "\nОстаток: " + d );

						s += " - Тупик";
						s += ss;
						s += ("\nОстаток: " + d);
						//s += scen;
						gg.AddVertex(s);

						difficulty.Add(s, d);
					}
					else if (q.Count == 0)
					{
						string ss = "";

						if (s != "Начало")
                        {
							ss = AddHardDynamic_Event(ref d);
						}
						else
                        {
							ss = AddNormalDynamic_Event(ref d);
						}

						//var scen = Scenario(dequeue_counter);

						//RenameMap.Add(s, s + " - Горло" + ss + "\nОстаток: " + d + scen);
						RenameMap.Add(s, s + " - Горло" + ss + "\nОстаток: " + d);

						s += " - Горло";
						s += ss;
						s += ("\nОстаток: " + d);
						//s += scen;
						gg.AddVertex(s);

						difficulty.Add(s, d);
					}
					else
                    {
						bool toggle = false;
						string ss = "";

						if (g.GetNeighbours(vertex).Count() >= 3)
						{
							foreach (var v in g.GetNeighbours(vertex))
							{

								if (g.GetNeighbours(v).Count() == 1)
								{
									var rr = new Random();
									int ii = rr.Next(0, gen_settings.Item6);

									if (ii == 0 && !visited.ContainsKey(v))
									{
										d += 0;

										visited.Add(v, (d, 0, true));
										q.Enqueue(v);

										ss += "_Закрытая_дверь";

										toggle = true;
									}

									//foreach (var vv in g.GetNeighbours(vertex))
									//{
										

										//if (visited.ContainsKey(vv))
                                          //{
											

										//	//g.AddEdge(v, vv);
          //                              }
									//}
								}
							}
						}

						if(!toggle)
                        {
							ss += AddNormalDynamic_Event(ref d);
							
						}

						//var scen = Scenario(dequeue_counter);

						//RenameMap.Add(s, s + " - Норма" + ss + "\nОстаток: " + d + scen);
						RenameMap.Add(s, s + " - Норма" + ss + "\nОстаток: " + d);

						s += " - Норма";
						s += ss;
						s += ("\nОстаток: " + d);
						//s += scen;
						gg.AddVertex(s);

						difficulty.Add(s, d);
					}

					foreach (var v in g.GetNeighbours(vertex))
					{
						if (!visited.ContainsKey(v))
						{
							visited.Add(v, (d, 1, false));
							q.Enqueue(v);
						}
						else
                        {
							visited[v] = (visited[v].Item1 + d, visited[v].Item2 + 1, visited[v].Item3);
						}
					}
                }

				foreach(var v in g.Edges)
                {
					gg.AddEdge(RenameMap[v.From], RenameMap[v.To]);
                }
			}

			public string AddHardDynamic_Event(ref int dd)
			{
				if(dd > 6)
                {
					dd -= 8;
					return "\nБосс: -8";
				}
                else if (dd < 0)
                {
					dd += 6;
					return "\nАртефакт: +6";
				}
				else
                {
					dd += 4;
					return "\nОружие: +4";
				}
			}

			public string AddSecreetDynamic_Event(ref int dd)
			{
				dd += 2;
				return "\nСекрет: +2";
			}

			public string AddNormalDynamic_Event(ref int dd)
			{
				if(dd + 2 >= 0 && dd <= 0)
				{
					dd += 2;
					return "\nПредмет: +2";
				}

				if (dd + 3 >= 0 && dd <= 0)
				{
					dd += 3;
					return "\nАптечка: +3";
				}

				if (dd <= 0)
				{
					dd += 6;
					return "\nАртефакт: +6";
				}

				if (dd - 6 >= 0)
				{
					dd -= 6;
					return "\nПротивник: -6";
				}

				var r = new Random();
				int v = r.Next(1, 3);

				switch (v)
				{
					case 1:
						dd += 2;
						return "\nПредмет: +2";
					case 2:
						dd += 3;
						return "\nАптечка: +3";
					default:
						return "\nЧто-то пошло не так :(";
				}
			}

			public void PrintPath(Stack<string> path)
			{

				var new_path = path.Reverse();
				string s = "";

				var d = new List<int>();

				foreach (var v in new_path)
				{
					s += (v + "->");
					d.Add(difficulty[v]);
				}

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    NewLine = Environment.NewLine,
                    HasHeaderRecord = false, // Don't write the header again.
				};

                var records = new List<Dufficult>
                {
                    new Dufficult {difficult = d},
                };

                using (var stream = File.Open("difficult_2.csv", FileMode.Append))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(records);
                }

                //s += ("->" + vertex);

                //Console.WriteLine(s);
				//Console.WriteLine();
			}

			public void PrintPlayerStats(Stack<PlayerState> path)
			{
				Console.WriteLine("_______________________________");
				var new_path = path.Reverse();

				var hp = new List<int>();
				var dmg = new List<int>();
				var spd = new List<int>();
				var ttk = new List<int>();
				

				foreach (var v in new_path)
				{
					Console.WriteLine("HP: " + v.hp);
					hp.Add(v.hp);
					Console.WriteLine("DMG: " + v.damage);
					dmg.Add(v.damage);
					Console.WriteLine("A_SPD: " + v.attack_speed);
					spd.Add(v.attack_speed);
					Console.WriteLine("TTK: " + v.time_to_kill);
					ttk.Add(v.time_to_kill);

					Console.WriteLine("Aids: " + v.AidKits);
					ttk.Add(v.time_to_kill);

					Console.WriteLine("DMG_CH: " + v.damage_chance);
					Console.WriteLine("Event: " + v.Event);

					Console.WriteLine();
				}

				//var l = new_path.Last();

				//if (l.lore.Item1 + l.lore.Item2 >= 2)
				//{
				//	Console.WriteLine("В этом подземелье обьтает могущечтвенное чудовище, которое охраняет могущестенный артефакт. Я обязан его заполучить.");
				//}
				//else if(l.lore.Item1 + l.lore.Item2 >= 1)
    //            {
				//	Console.WriteLine("В этом подземелье есть могущетсвенный артефакт. Я должен его заполучить. Но подземелье кишит монстрами.");
				//}
				//else
    //            {
				//	Console.WriteLine("Жители деревни, попросили меня помочь им с монстрами, что живут в этом подземелье. Тут может бть много полезных вещей.");
				//}

				Console.WriteLine();

				var config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					NewLine = Environment.NewLine,
					HasHeaderRecord = false, // Don't write the header again.
				};

				var records_hp = new List<Dufficult>
				{
					new Dufficult {difficult = hp},
				};

				var records_dmg = new List<Dufficult>
				{
					new Dufficult {difficult = dmg},
				};

				var records_spd = new List<Dufficult>
				{
					new Dufficult {difficult = spd},
				};

				var records_ttk = new List<Dufficult>
				{
					new Dufficult {difficult = ttk},
				};

				using (var stream_1 = File.Open("hp.csv", FileMode.Append))
				using (var writer_1 = new StreamWriter(stream_1))
				using (var csv_1 = new CsvWriter(writer_1, config))
				{
					csv_1.WriteRecords(records_hp);
				}

				using (var stream_2 = File.Open("dmg.csv", FileMode.Append))
				using (var writer_2 = new StreamWriter(stream_2))
				using (var csv_2 = new CsvWriter(writer_2, config))
				{
					csv_2.WriteRecords(records_dmg);
				}

				using (var stream_3 = File.Open("spd.csv", FileMode.Append))
				using (var writer_3 = new StreamWriter(stream_3))
				using (var csv_3 = new CsvWriter(writer_3, config))
				{
					csv_3.WriteRecords(records_spd);
				}

				using (var stream_4 = File.Open("ttk.csv", FileMode.Append))
				using (var writer_4 = new StreamWriter(stream_4))
				using (var csv_4 = new CsvWriter(writer_4, config))
				{
					csv_4.WriteRecords(records_ttk);
				}

			}

			public void PrintPathOld(Stack<string> path)
			{

				var new_path = path.Reverse();
				string s = "";

				var d = new List<int>();

				foreach (var v in new_path)
				{
					s += (v + "->");
					d.Add(difficulty[v]);
				}

				var config = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					NewLine = Environment.NewLine,
					HasHeaderRecord = false, // Don't write the header again.
				};

				var records = new List<Dufficult>
				{
					new Dufficult {difficult = d},
				};

				using (var stream = File.Open("difficult_1.csv", FileMode.Append))
				using (var writer = new StreamWriter(stream))
				using (var csv = new CsvWriter(writer, config))
				{
					csv.WriteRecords(records);
				}

				//s += ("->" + vertex);

				//Console.WriteLine(s);
				//Console.WriteLine();
			}
		}

		public class Room
		{
			public string Name { get; }
			public RoomType Type { get; }
			public Room(string name, RoomType type)
			{
				Type = type;
				Name = name;
			}
			public override string ToString()
			{
				return Name;
			}
		}

		private List<RoomTemplateGrid2D> GetRoomTemplatesForRoom(Room room, Dictionary<string, RoomTemplateGrid2D> roomTemplates)
		{
			switch (room.Type)
			{
				case RoomType.Spawn:
					return new List<RoomTemplateGrid2D>()
					{
						roomTemplates["Spawn"],
					};
				case RoomType.Normal:
					return new List<RoomTemplateGrid2D>()
					{
						roomTemplates["Normal 1"],
						roomTemplates["Normal 2"],
						roomTemplates["Normal 3"],
						roomTemplates["Normal 4"],
						roomTemplates["Normal 5"],
						roomTemplates["Normal 6"],
						roomTemplates["Normal 7"],
					};
				case RoomType.Boss:
					return new List<RoomTemplateGrid2D>()
					{
						roomTemplates["Boss"],
					};
				case RoomType.Exit:
					return new List<RoomTemplateGrid2D>()
					{
						roomTemplates["Exit"],
					};
				case RoomType.Reward:
					return new List<RoomTemplateGrid2D>()
					{
						roomTemplates["Reward"],
					};
				case RoomType.Shop:
					return new List<RoomTemplateGrid2D>()
					{
						roomTemplates["Shop"],
					};
				case RoomType.Hub:
					return new List<RoomTemplateGrid2D>()
					{
						roomTemplates["Hub 1"],
					};
				default:
					throw new ArgumentOutOfRangeException(nameof(room.Type), room.Type, null);
			}
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

		public static void MakeGraph(int rooms, (int, int, int, int, int, int) setting)
		{

			//var levelDescription = new LevelDescriptionGrid2D<Room>
			//{
			//    MinimumRoomDistance = 2,
			//};
			//var graph = GetGraph();
			//var roomTemplates = GetRoomTemplates();
			//foreach (var room in graph.Vertices)
			//{
			//    levelDescription.AddRoom(room, new RoomDescriptionGrid2D
			//    (
			//        isCorridor: false,
			//        roomTemplates: GetRoomTemplatesForRoom(room, roomTemplates)
			//    ));
			//}
			//var corridorRoomDescription = new RoomDescriptionGrid2D
			//(
			//    isCorridor: true,
			//    roomTemplates: GetCorridorRoomTemplates()
			//);
			//foreach (var edge in graph.Edges)
			//{
			//    var corridorRoom = new Room("Corridor", RoomType.Corridor);
			//    levelDescription.AddRoom(corridorRoom, corridorRoomDescription);
			//    levelDescription.AddConnection(edge.From, corridorRoom);
			//    levelDescription.AddConnection(edge.To, corridorRoom);
			//}
			//return levelDescription;

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

			////
			var gen = new Generator(rooms, setting);

			////
			//gen.new_DFS("Начало", "Конец", new Stack<string>(), new HashSet<string>());
			gen.BFS_Plus("Начало", 0);

			////
			var graph = gen.gg;

			var st = new Stack<string>();

			var stp = new Stack<PlayerState>();

			st.Push(gen.gg.Vertices.First());
			//stp.Push(new PlayerState());

			var h = new HashSet<string>();
			h.Add(gen.gg.Vertices.First());

			gen.new_DFS(gen.gg.Vertices.First(), gen.gg.Vertices.Last(),  st, new HashSet<string>());

			for(int i = 0; i < 1; i++)
            {
				gen.SpeedRunSimulation(gen.gg.Vertices.First(), gen.gg.Vertices.Last(), new PlayerState(), stp, new HashSet<string>());
			}

			//int k = 0;
			foreach (var room in graph.Vertices)
            {
				if(room.Contains("Тупик") && !room.Contains("Начало") && !room.Contains("Конец"))
                {
					levelDescription.AddRoom(room, new RoomDescriptionGrid2D(isCorridor: false, new List<RoomTemplateGrid2D>() { roomTemplates["Exit"] }));
				}
				else if(room.Contains("Горло"))
                {
					levelDescription.AddRoom(room, new RoomDescriptionGrid2D(isCorridor: false, new List<RoomTemplateGrid2D>(){roomTemplates["Boss"]}));
                }
				else
                {
					levelDescription.AddRoom(room, basicRoomDescription);
				}
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

			Process photoViewer = new Process();
			photoViewer.StartInfo.FileName = @"C:\Programs\PaintNet\paintdotnet.exe";
			photoViewer.StartInfo.Arguments = @"D:\Sperasoft_Research_Works\GeneratorPrototype\DungeonVisualization\DungeonViz\bin\Debug\net5.0\layout.png";
			photoViewer.Start();

			//var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			//{
			//	// Don't write the header again.
			//	NewLine = Environment.NewLine,
			//	HasHeaderRecord = false,
			//};

			//var records = new List<Dufficult>
			//{
			//	new Dufficult {difficult = new List<int> { 1, 2, 3, 4, 5, 6} },
			//};

			//using (var stream = File.Open("file.csv", FileMode.Append))
			//using (var writer = new StreamWriter(stream))
			//using (var csv = new CsvWriter(writer, config))
			//{
			//	csv.WriteRecords(records);
			//}

			//records = new List<Dufficult>
			//{
			//	new Dufficult {difficult = new List<int> { 6, 7, 8, 9} },
			//};

			//using (var stream = File.Open("file.csv", FileMode.Append))
			//using (var writer = new StreamWriter(stream))
			//using (var csv = new CsvWriter(writer, config))
			//{
			//	csv.WriteRecords(records);
			//}
		}

		public static void MakeOldGraph(int rooms)
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
			var gen = new Generator(rooms, (3, 2, 1, 2, 2, 1));

			////
			//gen.new_DFS("Начало", "Конец", new Stack<string>(), new HashSet<string>());
			//gen.BFS_Plus("Начало", 0);

			////
			var graph = gen.g;

			var st = new Stack<string>();
			st.Push(gen.g.Vertices.First());

			//var h = new HashSet<string>();
			//h.Add(gen.gg.Vertices.First());

			gen.old_DFS(gen.g.Vertices.First(), gen.g.Vertices.Last(), st, new HashSet<string>());

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

			Process photoViewer = new Process();
			photoViewer.StartInfo.FileName = @"C:\Programs\PaintNet\paintdotnet.exe";
			photoViewer.StartInfo.Arguments = @"D:\Sperasoft_Research_Works\GeneratorPrototype\DungeonVisualization\DungeonViz\bin\Debug\net5.0\layout.png";
			photoViewer.Start();
		}

		public class Dufficult
		{
			public List<int> difficult { get; set; }
		}

		static void Main(string[] args)
        {

			//ReadGraph(@"D:\DungeonViz\DungeonViz\my_graph.txt");

			//var gen = new Generator(8);
			//gen.DFS(new Dictionary<string, bool>(), new List<string>(), "Начало", "Начало");

			//Settings:
			//1 - Ограничение экспоненциального роста
			//2 - Скорость экпоненциального роста
			//3 - Ограничение по комнатам на уровне / Ограничитель линейного роста
			//4 - Связность графа (больше - хуже)
			//5 - Вероятность того, что вершина графа окажется тупиковой (больше - хуже)
			//6 - Вероятность того, что тупик будет использован для квеста (больше - хуже)

			//MakeGraph(12, (3, 1, 2, 10, 10, 1));

			MakeGraph(12, (3, 1, 4, 4, 2, 1));

			//MakeOldGraph(12);

			Console.WriteLine("Hello World!");
        }
    }
}
