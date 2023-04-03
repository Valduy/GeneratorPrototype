using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static System.Collections.Specialized.BitVector32;

namespace StoryGenerator
{
    enum BasicNeeds
    {
        Hunger,
        Thirst,
        Environmental_protection,
        Breathable_air,
        Dream,
        Health
    }

    class Card
    {
        public float weight { get; set; }
        public float priority { get; set; }

        public Func<StoryManager, bool> prediction { get; set;}
        public StoryEvent scenario_effect { get; set;}
        public int tensity_change { get; set; }
        public Dictionary<Agent, string> reaction { get; set; }

        public Card()
        {

        }

        public Card(float w, float p, Func<StoryManager, bool>pred, StoryEvent e, int tc, Dictionary<Agent, string> r)
        {
            weight = w;
            priority = p;
            prediction = pred;
            scenario_effect = e;
            tensity_change = tc;
            reaction = r;
        }

    }

    class StoryEvent
    {
        public int id { get; set; }
        public string description { get; set; }
        public bool value { get; set; }

        public Dictionary<Agent, Dictionary<BasicNeeds, int>> agent_change;

        public StoryEvent(int i, string d, bool v, Dictionary<Agent, Dictionary<BasicNeeds, int>> a) 
        {
            id = i;
            description = d;
            value = v;
            agent_change = a;
        }

        public void ApplyAgentChanges()
        {
            foreach(var a in agent_change)
            {
                foreach(var s in a.Value)
                {
                    a.Key.stats.ModifyStat(s.Key, s.Value);
                }
            }
        }
    }

    class Stats
    {
        //public int hunger { get; set; }
        //public int thirst { get; set; }
        //public int environmental_protection { get; set; }
        //public int breathable_air { get; set; }
        //public int dream { get; set; }
        //public int health { get; set; }

        public Dictionary<BasicNeeds, int> stat { get; set; }


        public Stats(int hunger, int thirst, int environmental_protection, int breathable_air, int dream, int health)
        {
            //this.hunger = hunger;
            //this.thirst = thirst;
            //this.environmental_protection = environmental_protection;
            //this.breathable_air = breathable_air;
            //this.dream = dream;
            //this.health = health;

            stat = new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, hunger }, { BasicNeeds.Thirst, thirst }, { BasicNeeds.Environmental_protection, environmental_protection },
                { BasicNeeds.Breathable_air, breathable_air }, { BasicNeeds.Dream, dream }, { BasicNeeds.Health, health }};
        }

        public BasicNeeds GetMin()
        {
            //stat.OrderByDescending(kvp => kvp.Value).First();
            var mi = stat.OrderBy(kvp => kvp.Value).First();
            return mi.Key;
        }

        public int GetStat(BasicNeeds name)
        {
            return stat[name];
        }

        public void ModifyStat(BasicNeeds name, int inc)
        {
            var v = stat[name] + inc;
            //Math.Clamp(v, 0, 100f);
            stat[name] = (int)Math.Clamp(v, 0, 100f);
        }
    }

    class Agent
    {
        public string Name { get; set; }

        public Stats stats { get; set; }

        public Agent(string n, Stats stats)
        {
            Name = n;
            this.stats = stats;
        }

        public Card GenCard(StoryManager manager)
        {
            Dictionary<Agent, string> r = new Dictionary<Agent, string>();
            
            var m = stats.GetMin();
            var v = stats.GetStat(m);

            var rand = new Random();
            var change = rand.Next(16);

            change = 100;

            switch (m)
            {
                case BasicNeeds.Hunger:
                    r = new Dictionary<Agent, string>() { { this, Name + " хочет есть и пока есть время решает перекусить." } };
                    break; 
                case BasicNeeds.Thirst:
                    r = new Dictionary<Agent, string>() { { this, Name + " промок и пока есть немного времени решает согреться." } };
                    break; 
                case BasicNeeds.Environmental_protection:
                    r = new Dictionary<Agent, string>() { { this, Name + " чувствует себя некомфортно и решает починить свой скафандр." } };
                    break; 
                case BasicNeeds.Health:
                    r = new Dictionary<Agent, string>() { { this, Name + " поранился и решает себя подлечить." } };
                    break; 
                case BasicNeeds.Breathable_air:
                    r = new Dictionary<Agent, string>() { { this, Name + " задыхается и решает срочно сменить кислородный балон." } };
                    break; 
                case BasicNeeds.Dream:
                    r = new Dictionary<Agent, string>() { { this, Name + " устал и решает поспать." } };
                    break;
            }

            StoryEvent se = new StoryEvent(101, "Какой то случайный ивент", true, 
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { this, new Dictionary<BasicNeeds, int> { { m, change } } } });

            if(v == 100)
            {
                Console.WriteLine("");
            }

            float pp = (1f / v);
            float p = 2f * (Math.Abs(v - 100f) / 100f);
            p = Math.Clamp(p, 0f, 1.0f);

            return new Card( 1, p, manager => true, se, 0, r);
        }

    }

    class StoryManager
    {
        public Dictionary<StoryEvent, bool> events { get; set; }
        public List<float> story_curve { get; set; }
        public List<Agent> agents { get; set; }

        public StoryManager()
        {
            
        }

        public StoryManager(Dictionary<StoryEvent, bool> e, List<float> sc, List<Agent> a)
        {
            events = e;
            story_curve = sc;
            agents = a;
        }
    }

    //class Algorith
    //{
    //    public StoryManager manager { get; set; }
    //    public float current_tension { get; set; }

    //    public Algorith()
    //    {
    //        current_tension = 0;
    //    }
    //}

    class Program
    {
        static void Main(string[] args)
        {
            Card current_bit = new Card();
            List<Card> previous_bits = new List<Card>() { };

            var rand = new Random();
            
            //rand.Next(50, 101);

            Agent h1 = new Agent("Джим Рейнор", new Stats(rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101)));
            Agent h2 = new Agent("Тайкус Финдли", new Stats(rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101)));
            Agent h3 = new Agent("Сара Керриган", new Stats(rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101)));
            Agent h4 = new Agent("Зератул", new Stats(rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101)));
            Agent h5 = new Agent("Арктур Менгск", new Stats(rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101)));
            Agent h6 = new Agent("Валериан Менгск", new Stats(rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101), rand.Next(50, 101)));

            List<Agent> agents = new List<Agent>() {h1, h2, h3, h4, h5, h6 };

            StoryEvent se1 = new StoryEvent(0, "Джим Рейнор встретил Тайкус Финдли", false, 
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { h1, new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, -15} } }, { h2, new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, -15 } } } });
            
            StoryEvent se2 = new StoryEvent(1, "Сара Керриган приказывает зергам начать наступление на Доминион.", false,
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { h3, new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, -20 } } } });
            
            StoryEvent se3 = new StoryEvent(2, "Джим Рейнор и Тайкус Финдли находят артефакты", false,
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { h1, new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, -25 }, { BasicNeeds.Dream, -25 } } }, 
                    { h2, new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, -15 }, { BasicNeeds.Dream, -25 } } } });
            
            StoryEvent se4 = new StoryEvent(3, "Джим Рейнор встретил Валериан Менгск", false,
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { h1, new Dictionary<BasicNeeds, int> { { BasicNeeds.Dream, -10 } } }, 
                    { h6, new Dictionary<BasicNeeds, int> { { BasicNeeds.Dream, -10 } } } });
            
            StoryEvent se5 = new StoryEvent(4, "Зератул встретил Джим Рейнор", false,
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { h4, new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, -10 }, { BasicNeeds.Dream, -5 } } }, 
                    { h1, new Dictionary<BasicNeeds, int> { { BasicNeeds.Hunger, -10 }, { BasicNeeds.Dream, -5 } } } });
            
            StoryEvent se6 = new StoryEvent(5, "Зератул рассказал Джим Рейнор и Тайкус Финдли о Падшем", false,
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { h4, new Dictionary<BasicNeeds, int> { { BasicNeeds.Dream, -20 } } }, 
                    { h1, new Dictionary<BasicNeeds, int> { { BasicNeeds.Dream, -20 } } }, 
                    { h2, new Dictionary<BasicNeeds, int> { { BasicNeeds.Dream, -20 } } } });
            
            StoryEvent se7 = new StoryEvent(6, "Тайкус Финдли придает Джим Рейнор", false,
                new Dictionary<Agent, Dictionary<BasicNeeds, int>>() { { h1, new Dictionary<BasicNeeds, int> { { BasicNeeds.Health, -50 } } }, 
                    { h2, new Dictionary<BasicNeeds, int> { { BasicNeeds.Health, -100 } } } });

            Dictionary<StoryEvent, bool> stories = new Dictionary<StoryEvent, bool>() { { se1, false}, { se2, false }, 
                { se3, false }, { se4, false }, { se5, false }, { se6, false }, { se7, false }};

            var r1 = new Dictionary<Agent, string>() { { h1, "Тайкус рассказывает, что Доминион и Фонд ищут артефакты пришельцев, и предлагает Рейнору работать на Фонд, который выдает вознаграждение людям, нашедшим их." } };
            var r2 = new Dictionary<Agent, string>() { { h3, "Сара Керриган приказывает зергам начать наступление на Доминион." } };
            var r3 = new Dictionary<Agent, string>() { { h1, "Джим Рейнор на Мар Саре перехватывает у Доминиона первый артефакт" }, { h2, "Тайкус Финдли на Мар Саре перехватывает у Доминиона первый артефакт" } };
            var r4 = new Dictionary<Agent, string>() { { h6, "Валериан сказал, что он может дать Рейнору шанс спасти Сару Керриган" } };
            var r5 = new Dictionary<Agent, string>() { { h4, "Зератул попросил Джима Рейнора встретиться с  Валериан Менгск." } };
            var r6 = new Dictionary<Agent, string>() { { h4, "Зератул рассказал, что ему  открылась страшная правда — загадочный «Падший» хочет уничтожить все живое во Вселенной, после чего мир погрузится в кромешную тьму. Зератул рассказал, что первым такое будущее увидел Сверхразум зергов и нашёл единственное спасение в лице Королевы Клинков." } };
            var r7 = new Dictionary<Agent, string>() { { h1, "Джим спасает Сару Керриган и убивает предателя Тайкуса." }, { h2, "Тайкус попытался убить Джима Рейнора, но у него ничего не вышло." } };

            List<float> story_curve = new List<float>() { 0, 1, 2, 3, 3, 2, 1, 2, 3, 5, 3, 2, 1};

            StoryManager manager = new StoryManager(stories, story_curve, agents);

            Card bit1 = new Card(1, (float)rand.Next(80, 100) / 100f, manager => true, se1, 1, r1);
            
            Card bit2 = new Card(1, (float)rand.Next(80, 100) / 100f, manager => true, se2, 1, r2);
            
            Card bit3 = new Card(1, (float)rand.Next(80, 100) / 100f, manager => manager.events[se1] && !(manager.events[se2]), se3, 1, r3);
            
            Card bit4 = new Card(1, (float)rand.Next(80, 100) / 100f, manager => manager.events[se3] || (manager.events[se5]), se4, 1, r4);
            
            Card bit5 = new Card(1, (float)rand.Next(80, 100) / 100f, manager => manager.events[se1] && !(manager.events[se3]), se5, 1, r5);
            
            Card bit6 = new Card(1, (float)rand.Next(80, 100) / 100f, manager => manager.events[se4], se6, 1, r6);
            
            Card bit7 = new Card(1, (float)rand.Next(80, 100) / 100f, manager => manager.events[se6], se7, 1, r7);

            List<Card> cards = new List<Card>() {bit1, bit2, bit3, bit4, bit5, bit6, bit7};

            float t = 0;

            int i = 0;
            int ii = 0;

            int num = 1;

            while(cards.Count != 0)
            {
                //i++;

                if (ii == 13)
                {
                    break;
                }

                List <Card> new_cards = new List<Card>();
                new_cards.AddRange(cards.ToArray());

                foreach (Agent a in agents)
                {
                    new_cards.Add(a.GenCard(manager));
                }

                List<Card> satisfied = new List<Card>() { };

                foreach (Card card in new_cards)
                {
                    if(card.prediction(manager))
                    {
                        double scope = (card.weight * (1 / Math.Exp((story_curve[i] - t) / num - card.tensity_change))) * card.priority;

                        satisfied.Add(card);
                    }
                }

                var max_priority = satisfied.Max(comparer => comparer.priority);
                Dictionary<Card, double> highest_priority = new Dictionary<Card, double>() { };
                //Console.WriteLine(max_priority);

                foreach (Card satisfy in satisfied)
                {
                    //if (true)
                    if (satisfy.priority >= max_priority - 0.1)
                    {
                        double scope = (satisfy.weight * (1 / Math.Exp((story_curve[i] - t) / num - satisfy.tensity_change))) * satisfy.priority;

                        highest_priority.Add(satisfy, scope);

                    }
                }

                var winner = highest_priority.OrderByDescending(kvp => kvp.Value).First();

                manager.events[winner.Key.scenario_effect] = true;

                t += winner.Key.tensity_change;

                foreach(var reaction in winner.Key.reaction)
                {
                    Console.WriteLine(reaction.Key.Name + " : " + reaction.Value);
                }

                winner.Key.scenario_effect.ApplyAgentChanges();

                if (winner.Key.tensity_change != 0)
                {
                    i++;
                }

                ii++;

                cards.Remove(winner.Key);
            }
        }
    }
}