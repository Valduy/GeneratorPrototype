using System;
using System.Collections.Generic;
using System.IO;

namespace StoryGenerator
{

    //  class Hero
    //  {
    //int Strength;
    //int Influence;
    //int Oratory;

    //List<string> Actions = { "убить", "уговорить", "обмануть"};

    //Hero(int s, int i, int O)
    //      {

    //	Strength = s;
    //	Influence = i;
    //	Oratory = O;

    //}
    //  }

    struct Hero
    {
		public string strong { get; set; }
		public string weak { get; set; }
		public string secret { get; set; }
		public string target { get; set; }

		public Hero(string s, string w, string sec, string t)
        {
			strong = s;
			weak = w;
			secret = sec;
			target = t;
        }
    }

    struct Act
    {
		public string start { get; set; }
		public string middle { get; set; }
		public string end { get; set; }

		public Act(string s, string m, string e)
        {
			start = s;
			middle = m;
			end = e;
        }
    }

	class TarotCardTextGenerator
    {

		List<(string, string, bool)> cards = new List<(string, string, bool)>();

		Hero gg;

		List<Act> acts = new List<Act>();

		public TarotCardTextGenerator(string path)
        {
			var cards_lines = File.ReadLines(path);
			
			foreach (string s in cards_lines)
			{
				var ss = s.Split('|');

				cards.Add((ss[0], ss[1], false));
			}
		}

		public void GenStoryStructure(string Hero, string Antagonist)
        {
			Random rr = new Random();

			var card_pool = cards.Count();
			var rInt = rr.Next(0, card_pool);

			string s = rr.Next(0, 1) == 0 ? cards[rInt].Item1 : cards[rInt].Item2;
			cards.RemoveAt(rInt);
			card_pool -= 1;

			rInt = rr.Next(0, card_pool);
			string w = rr.Next(0, 1) == 0 ? cards[rInt].Item1 : cards[rInt].Item2;
			cards.RemoveAt(rInt);
			card_pool -= 1;

			rInt = rr.Next(0, card_pool);
			string sec = rr.Next(0, 1) == 0 ? cards[rInt].Item1 : cards[rInt].Item2;
			cards.RemoveAt(rInt);
			card_pool -= 1;

			rInt = rr.Next(0, card_pool);
			string t = rr.Next(0, 1) == 0 ? cards[rInt].Item1 : cards[rInt].Item2;
			cards.RemoveAt(rInt);
			card_pool -= 1;


			gg = new Hero(s, w, sec, t);

			for (int i = 0; i < 4; i++)
			{
				Random r = new Random();

				rInt = rr.Next(0, card_pool);
				string f = r.Next(0, 1) == 0 ? cards[rInt].Item1 : cards[rInt].Item2;
				cards.RemoveAt(rInt);
				card_pool -= 1;

				rInt = rr.Next(0, card_pool);
				string m = r.Next(0, 1) == 0 ? cards[rInt].Item1 : cards[rInt].Item2;
				cards.RemoveAt(rInt);
				card_pool -= 1;

				rInt = rr.Next(0, card_pool);
				string e = r.Next(0, 1) == 0 ? cards[rInt].Item1 : cards[rInt].Item2;
				cards.RemoveAt(rInt);
				card_pool -= 1;

				acts.Add(new Act(f,m,e));
            }
		}

		public void PrintStoruStructure()
        {
			Console.WriteLine("Герой:");
			Console.WriteLine("Сильная сторона:" + gg.strong);
			Console.WriteLine("Слабая сторона:" + gg.weak);
			Console.WriteLine("Тайна:" + gg.secret);
			Console.WriteLine("Цель:" + gg.target);

			Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
			Console.WriteLine();

			List<(string, string, string)> titles = new List<(string, string, string)>();

			titles.Add(("Тема: ", "Антагонист: ", "Зараженное ружье: "));
			titles.Add(("Завязка: ", "Препятствие: ", "Перепетия: "));
			titles.Add(("Ход антагониста: ", "Действие игрока: ", "От счастья к несчастью: "));
			titles.Add(("Последняя капля: ", "Решающий выбор: ", "Развезка: "));

			for (int i = 0; i < 4; i++)
            {
				Console.WriteLine("Акт: " + i + ":");
				Console.WriteLine(titles[i].Item1 + acts[i].start);
				Console.WriteLine(titles[i].Item2 + acts[i].middle);
				Console.WriteLine(titles[i].Item3 + acts[i].end);

				Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
				Console.WriteLine();
			}
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			var heroes = new Dictionary<string, (string, string, List<string>)>();
			var location = new Dictionary<string, string>();
			var relationship = new Dictionary<string, (string, string, string, string, string, string, bool)>();

			//heroes.Add("gg", ("Рама", "Дом", "start_g"));
			//heroes.Add("aa", ("Тама", "Многоквартирное здание", "start_a"));
			//heroes.Add("ss", ("Сержант", "Полицейский участок", "start_s"));
			//heroes.Add("ee", ("Энди", "Многоквартирное здание", "start_e"));

			//relationship.Add("start_g", ("хочет вернуть", " ", "gg", "ee", "Дом", "e1", false));
			//relationship.Add("start_a", ("хочет продавать наркотики", " с ", "aa", "ee", "Многоквартирное здание", "", false));
			//relationship.Add("start_s", ("хочет посадить в тюрьму", " ", "ss", "aa", "Полицейский участок", "", false));
			//relationship.Add("start_e", ("не хочет домой", " с ", "ee", "gg", "Многоквартирное здание", "", false));
			//relationship.Add("e1", ("совершают рейд", " c ", "gg", "ss", "Многоквартирное здание", "e2", false));
			//relationship.Add("e2", ("заходит в здание", " c ", "ss", "gg", "Многоквартирное здание", "e3", false));
			//relationship.Add("e3", ("видит по камерам", " ", "aa", "ss", "Многоквартирное здание", "e4", false));
			//relationship.Add("e4", ("говорит разобраться c гостями", " ", "aa", "ee", "Многоквартирное здание", "e5", false));
			//relationship.Add("e5", ("хочет защитить", " ", "ee", "gg", "Многоквартирное здание", "e6", false));
			//relationship.Add("e6", ("приказывает соседям убить", " ", "aa", "gg", "Многоквартирное здание", "e7", true));
			//relationship.Add("e6", ("", "", "", "", "", false));
			//relationship.Add("e7", ("", "", "", "", "", false));
			//relationship.Add("e8", ("", "", "", "", "", false));
			//relationship.Add("end", ("", "", "", "", "", true));

			heroes.Add("JR", ("Джим Рейнор", "Бар", new List<string>() { "Start_JR" }));
			heroes.Add("TF", ("Тайкус Финдли", "Тюрьма", new List<string>() { "Start_TF" }));
			heroes.Add("SK", ("Сара Кериган", "Контроль надмозга", new List<string>() { "Start_SK" }));
			heroes.Add("AM", ("Арктур Менгск", "Трон", new List<string>() { "Start_AM" }));
			heroes.Add("OM", ("Надмозг", "Неизвестная планета", new List<string>() { "Start_OM" }));

			relationship.Add("Start_JR", ("хочет спасти ", "", "JR", "SK", "", "", false));
			relationship.Add("Start_TF", ("выбраться из тюрьмы", "", "TF", "", "Тюрьма", "TF1", false));
			relationship.Add("Start_SK", ("служить ", "", "SK", "OM", "Неизвестная планета", "SK1", false));
			relationship.Add("Start_AM", ("хочет убить ", "", "AM", "SK", "", "", false));
			relationship.Add("Start_OM", ("хочет водрузить чарство роя", "", "OM", "", "", "", false));

			relationship.Add("TF1", ("договаривается", " с ", "TF", "AM", "Тюрьма", "TF2", false));
			relationship.Add("TF2", ("уговаривает найти артифакты ", "", "TF", "JR", "Бар", "TF3", false));
			relationship.Add("TF3", ("находят тайную лабораторию", " с ", "TF", "JR", "Мар Саре", "JR1", false));

			relationship.Add("SK1", ("нападает на терранов", "", "SK", "", "Мар Саре", "", false));

			relationship.Add("JR1", ("узнает о том, что пси-эммитеры были включены по приказу ", "", "JR", "AM", "Мар Саре", "JR2", false));
			relationship.Add("JR2", ("поднимает восстание против ", "", "JR", "AM", "Диминион", "", true));

			//relationship.Add("e1", ("выбраться из тюрьмы", "", "TF", "", "Тюрьма", "", true));

			//foreach (var item in heroes)
			//         {
			//	string s = String.Format("{0} находиться: {1}", item.Value.Item1, item.Value.Item2);
			//	Console.WriteLine(s);
			//}

			//var b = false;
			//while(!b)
			//         {
			//	foreach(var item in heroes)
			//             {
			//		if(item.Value.Item3.First() != "" && relationship.ContainsKey(item.Value.Item3.First()))

			//		{
			//			string s = String.Format("{2} {0}{1}{3}{4}.", relationship[item.Value.Item3.First()].Item1, relationship[item.Value.Item3.First()].Item2, 
			//				heroes[relationship[item.Value.Item3.First()].Item3].Item1, heroes.ContainsKey(relationship[item.Value.Item3.First()].Item4) ? heroes[relationship[item.Value.Item3.First()].Item4].Item1 : "", relationship[item.Value.Item3.First()].Item5 != "" ? " в локации " + relationship[item.Value.Item3.First()].Item5 : "");

			//			Console.WriteLine(s);

			//			var key = item.Value.Item3.First();

			//			if(relationship[item.Value.Item3.First()].Item5 != heroes[relationship[item.Value.Item3.First()].Item3].Item2 && relationship[item.Value.Item3.First()].Item5 != "")
			//                     {
			//				string ss = String.Format("{0} переходит из локации {1} в {2}.", heroes[relationship[item.Value.Item3.First()].Item3].Item1, heroes[relationship[item.Value.Item3.First()].Item3].Item2, relationship[item.Value.Item3.First()].Item5);
			//				Console.WriteLine(ss);
			//			}

			//			if (heroes.ContainsKey(relationship[item.Value.Item3.First()].Item4) && relationship[item.Value.Item3.First()].Item5 != "" && relationship[item.Value.Item3.First()].Item5 != heroes[relationship[item.Value.Item3.First()].Item4].Item2)
			//			{
			//				string ss = String.Format("{0} переходит из локации {1} в {2}.", heroes[relationship[item.Value.Item3.First()].Item4].Item1, heroes[relationship[item.Value.Item3.First()].Item4].Item2, relationship[item.Value.Item3.First()].Item5);
			//				Console.WriteLine(ss);
			//			}

			//			string sss = relationship[item.Value.Item3.First()].Item6;
			//			item.Value.Item3.Add(sss);

			//			heroes[relationship[item.Value.Item3.First()].Item3] = (heroes[relationship[item.Value.Item3.First()].Item3].Item1, relationship[item.Value.Item3.First()].Item5 != "" ? relationship[item.Value.Item3.First()].Item5 : heroes[relationship[item.Value.Item3.First()].Item3].Item2, item.Value.Item3);

			//			if (heroes.ContainsKey(relationship[item.Value.Item3.First()].Item4))
			//                     {
			//				heroes[relationship[item.Value.Item3.First()].Item4].Item3.Add(sss);

			//				heroes[relationship[item.Value.Item3.First()].Item4] = (heroes[relationship[item.Value.Item3.First()].Item4].Item1, 
			//					relationship[item.Value.Item3.First()].Item5 != "" ? relationship[item.Value.Item3.First()].Item5 : heroes[relationship[item.Value.Item3.First()].Item4].Item2, 
			//					heroes[relationship[item.Value.Item3.First()].Item4].Item3);
			//			}

			//			b = b || relationship[item.Value.Item3.First()].Item7;

			//			item.Value.Item3.Remove(item.Value.Item3.First());
			//			relationship.Remove(key);

			//		}

			//	}
			//}

			TarotCardTextGenerator gen = new TarotCardTextGenerator("Info.txt");
			gen.GenStoryStructure("", "");
			gen.PrintStoruStructure();

		}
	}
}