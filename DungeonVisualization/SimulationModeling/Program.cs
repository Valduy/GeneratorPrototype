
using System.Text.RegularExpressions;

namespace SimulationModeling
{

    enum EmotionsType
    {
        Emotional,
        Rational
    }

    enum SkillType
    {
        Phisical,
        Mental
    }

    class Skill
    {
        public string EmotinalAction { get; set; }
        //public string Neutral { get; set; }
        public string RationalAction { get; set; }

        public Skill(string emotinalAction, string rationalAction)
        {
            EmotinalAction = emotinalAction;
            RationalAction = rationalAction;
        }
    }

    class Goals
    {
        public string goal;

        public Goals(string goal)
        {
            this.goal = goal;
        }
    }

    class UtilitySystem
    {
        //public Hero hero { get; set; }

        Dictionary<double, Skill> phisical_actions;
        Dictionary<double, Skill> mental_actions;

        public UtilitySystem()
        {
            Skill LightWeapon = new Skill("{0} быстро стреляет несколько раз из легкого оружия по {1}", "{0} прицельно стреляет из легкого оружия по {1}");
            Skill HeavyWeapon = new Skill("{0} быстро стреляет несколько раз из тяжелого оружия по {1}", "{0} прицельно стреляет из тяжелого оружия по {1}");
            Skill EnergyWeapon = new Skill("{0} быстро стреляет несколько раз из энергитического оружия по {1}", "{0} прицельно стреляет из энергетического оружия по {1}");
            Skill WithoutWeapon = new Skill("{0} наносит несколько тяжелых ударов руками {1}", "{0} бьет одним точным ударом рукой в голову {1}");
            Skill SteelArms = new Skill("{0} хаотично наносит удары холодным оружием по {1}", "{0} бьет холодным оружием по {1} в область шеи");
            Skill Throwing = new Skill("{0} испульсивно кидает несколько предметов в {1}", "{0} прицельно кидает предмет в {1}");

            Skill Theft = new Skill("{0} хочет обокрасть {1}", "{0} аккратно хочет обокрасть {1}");
            Skill Trap = new Skill("{0} решает устроить хитрую ловушку для {1}", "{0} решает аккурвтно устроить ловушку для {1}");
            Skill Repair = new Skill("{0} в порыве чувств хочет помочь {1} с починкой", "Подумав {0} решил помочь {1} с починкой");
            Skill Eloquence = new Skill("{0} пытается льстиво говороить с {1}", "{0} решает поделиться фактами с {1} в разговоре");
            Skill Barter = new Skill("{0} эмоционально предлагает обменяться с {1}", "Подумав {0} решает обменяться с {1}");
            Skill Gambling = new Skill("{0} импульсивно предлагает сыграть {1} в азартную игру", "{0} аккуратно предлагает сыграть {1} в азартную игру");
            //Skill Naturalist = new Skill("");

            phisical_actions = new Dictionary<double, Skill>() { { 0.0, LightWeapon },
            { 0.3, HeavyWeapon }, { 0.4, EnergyWeapon }, { 0.5, WithoutWeapon },
            { 0.6, SteelArms }, { 0.7, Throwing }};

            mental_actions = new Dictionary<double, Skill>() { { 0.0, Theft },
            { 0.3, Trap }, { 0.4, Repair }, { 0.5, Eloquence },
            { 0.6, Barter }, { 0.7, Gambling }};
        }

        public string GetAction(EmotionsType emotionsType, SkillType skillType, double emotinalRate, double skillRate)
        {
            switch(skillType)
            {
                case SkillType.Phisical:
                    {
                        Skill s = phisical_actions.Where(kvp => kvp.Key <= skillRate).OrderByDescending(kvp => kvp.Key).First().Value;

                        return emotionsType == EmotionsType.Emotional ? s.EmotinalAction : s.RationalAction;

                        //break;
                    }
                case SkillType.Mental:
                    {
                        Skill s = mental_actions.Where(kvp => kvp.Key <= skillRate).OrderByDescending(kvp => kvp.Key).First().Value;

                        return emotionsType == EmotionsType.Emotional ? s.EmotinalAction : s.RationalAction;

                        //break;
                    }
                default:
                    throw new Exception();
            }

            //return "";
        }

    }

    class Hero
    {
        public string archetype { get; set; }
        public string psychotype { get; set; }

        public Dictionary<string, string> MBTI_types { get; set; }

        public UtilitySystem US;

        //TODO: system of needs

        public Dictionary<string, int> Needs { get; set; }
        public Dictionary<string, (string, string, string, string)> NeedsDescriptions { get; set; }

        public int thirst = 100;

        public int hunger = 100;

        public int addiction = 100;

        public int sex = 100;

        public string name { get; set; }

        //Шкала E—I — ориентация сознания:
        //Е(Еxtraversion, экстраверсия) — ориентация сознания наружу, на объекты,
        //I (Introversion, интроверсия) — ориентация сознания внутрь, на субъекта;
        public double EI { get; set; } //

        //Шкала S—N — способ ориентировки в ситуации:
        //S(Sensing, ощущение) — ориентировка на материальную информацию,
        //N (iNtuition, интуиция) — ориентировка на интуитивную информацию;
        public double SN { get; set; } //Поиск-Удача

        //Шкала T—F — основа принятия решений:
        //T(Thinking, мышление) — логическое взвешивание альтернатив;
        //F(Feeling, чувство) — принятие решений на эмоциональной основе этики;
        public double TF { get; set; } //Интелект-Харизма

        //Шкала J—P — способ подготовки решений:
        //J(Judging, суждение) — рациональное предпочтение планировать и заранее упорядочивать информацию,
        //P(Perception, восприятие) — иррациональное предпочтение действовать без детальной предварительной подготовки, больше ориентируясь по обстоятельствам.
        public double JR { get; set; } //Планирование-Восприятие

        public Location locations { get; set; }

        //public Hero()
        //{

        //}

        public Hero(string n, double ei, double sn, double tf, double jr, int t, int h, int a, int s)
        {
            name = n;
            EI = ei;
            SN = sn;
            TF = tf;
            JR = jr;

            thirst = t;
            hunger = h;
            addiction = a;
            sex = s;

            Needs = new Dictionary<string, int>();

            Needs.Add("thirst", thirst);
            Needs.Add("hunger", hunger);
            Needs.Add("addiction", addiction);
            Needs.Add("sex", sex);

            NeedsDescriptions = new Dictionary<string, (string, string, string, string)>();

            NeedsDescriptions.Add("thirst", ("Персонаж хочет есть.", "Но персонаж все еще хочет есть.", "Персонаж поел.", "Персонаж не смог поесть."));
            NeedsDescriptions.Add("hunger", ("Персонаж хочет пить.", "Но персонаж все еще хочет пить.", "Персонаж попил.", "Персонаж не смог попить."));
            NeedsDescriptions.Add("addiction", ("Персонажу нужна доза.", "Но персонажу все еще нужна доза.", "Персонаж принял наркотики.", "Персонаж не смог принять наркотики."));
            NeedsDescriptions.Add("sex", ("Персонаж решает изнасилось другого персонажа.", "Но персонаж все еще хочет секса.", "Персонаж самоудовлетворился.", "персонаж не смог самоудовлетвориться."));

            MBTI_types = new Dictionary<string, string>()
            {
            { "INTJ", "Стратег"}, { "INTP", "Ученый" }, { "ENTJ", "Командир" }, { "ENTP", "Полемист" },
            { "INFJ", "Активист"},{ "INFP", "Посредник"},{ "ENFJ", "Тренер"},{ "ENFP", "Борец"},
            { "ISTJ", "Администратор"},{ "ISFJ", "Защитник"},{ "ESTJ", "Менеджер"},{ "ESFJ", "Консул"},
            { "ISTP", "Виртуоз"},{ "ISFP", "Артист"},{ "ESTP", "Делец"},{ "ESFP", "Развлекатель"}
            };

            US = new UtilitySystem();
        }

        public (string, string) GetCause()
        {
            //Skill s = phisical_actions.Where(kvp => kvp.Key <= skillRate).OrderByDescending(kvp => kvp.Key).First().Value;

            //return emotionsType == EmotionsType.Emotional ? s.EmotinalAction : s.RationalAction;

            var s = Needs.OrderBy(kvp => kvp.Value).First().Key;

            return (s, NeedsDescriptions[s].Item1);
        }

        public string GetNewProblem()
        {
            var s = Needs.OrderBy(kvp => kvp.Value).First().Key;

            return NeedsDescriptions[s].Item2;
        }

        public string GetCauseResult(string s, bool result)
        {


            return result ? NeedsDescriptions[s].Item3 : NeedsDescriptions[s].Item4;
        }

        public void Print()
        {
            var s1 = (this.EI < 0.5) ? "E" : "I";
            var s2 = (this.SN < 0.5) ? "S" : "N";
            var s3 = (this.TF < 0.5) ? "T" : "F";
            var s4 = (this.JR < 0.5) ? "J" : "P";

            var s = s1 + s2 + s3 + s4;

            Console.WriteLine(name + ": " + MBTI_types[s]);
        }

        public string GetAction(EmotionsType emotionsType, SkillType skillType, double emotinalRate, double skillRate)
        {
            return US.GetAction(emotionsType, skillType, emotinalRate, skillRate);
        }
    }

    class Location
    {
        public string Name;

        public Location(string name)
        {
            Name = name;
        }
    }

    class ScenarioСard
    {
        public Location interior;
        public Location exterior;

        public Hero active;
        public Hero reactive;

        public string emotional_change;
        public string impact;
        public string literature_impact;
        public string conflict;

        public ScenarioСard(Location interior, Location exterior, Hero active, Hero reactive)
        {
            int inflict_will_modifier = 1;

            this.interior = interior;
            this.exterior = exterior;
            this.active = active;
            this.reactive = reactive;

            var a_ei = active.EI - reactive.EI;
            var a_sn = active.SN - reactive.SN;
            var a_tf = active.TF - reactive.TF;
            var a_jr = active.JR - reactive.JR;

            var r_ei = reactive.EI - active.EI;
            var r_sn = reactive.SN - active.SN;
            var r_tf = reactive.TF - active.TF;
            var r_jr = reactive.JR - active.JR;

            EmotionsType emotionsType;
            SkillType skillType;

            double emotional_rate = 0.0;
            double skill_rate = 0.0;

            Console.WriteLine("Сцена в локации: " + interior.Name + ". " + exterior.Name + ".");
            Console.WriteLine();

            active.Print();
            reactive.Print();
            Console.WriteLine();

            bool res;

            emotional_change = "Тип воздействия: ";
            impact = "Результат: ";
            //literature_impact = "Вывод: ";

            emotional_rate = (active.SN + active.TF) / 2;

            if (active.SN < 0.5 && active.TF > 0.5)
            {
                emotional_change += "Эмоциональное";

                if (a_sn < 0 && a_tf > 0)
                {
                    impact += "Положительно для активного";
                    literature_impact += "{0} получил все что хотел от {1}.";
                    active.SN += (a_sn / inflict_will_modifier);
                    active.TF += (a_tf / inflict_will_modifier);

                    res = true;
                }
                else
                {
                    impact += "Отрицательно для активного";
                    literature_impact += "{0} ничего не смог добиться от {1}.";
                    active.SN -= (a_sn / inflict_will_modifier);
                    active.TF -= (a_tf / inflict_will_modifier);

                    res = false;
                }

                emotionsType = EmotionsType.Emotional;
            }
            else
            {
                emotional_change += "Рациональное";

                if (a_sn > 0 && a_tf < 0)
                {
                    impact += "Положительно для активного";
                    literature_impact += "{0} получил все что хотел от {1}.";
                    active.SN += (a_sn / inflict_will_modifier);
                    active.TF += (a_tf / inflict_will_modifier);

                    res = true;
                }
                else
                {
                    impact += "Отрицательно для активного";
                    literature_impact += "{0} ничего не смог добиться от {1}.";
                    active.SN -= (a_sn / inflict_will_modifier);
                    active.TF -= (a_tf / inflict_will_modifier);

                    res = false;
                }

                emotionsType = EmotionsType.Rational;
            }

            skill_rate = (active.EI + active.JR) / 2;

            if (active.EI > 0.5 && active.JR < 0.5)
            {
                emotional_change += " Ментальное";

                if (r_ei > 0 && r_jr < 0)
                {
                    impact += " Положительно для реактивного";
                    literature_impact += " {1} принимает отчку зрения {0}.";
                    reactive.EI += r_ei;
                    reactive.JR += r_jr;
                }
                else
                {
                    impact += " Отрицательно для реактивного";
                    literature_impact += " {1} отричает позицию {0} и остается при своем мнении.";
                    reactive.EI -= r_ei;
                    reactive.JR -= r_jr;
                }

                skillType = SkillType.Mental;
            }
            else
            {
                emotional_change += " Физическое";

                if (r_ei < 0 && r_jr > 0)
                {
                    impact += " Положительно для реактивного";
                    literature_impact += " {1} принимает отчку зрения {0}.";
                    reactive.EI += r_ei;
                    reactive.JR += r_jr;
                }
                else
                {
                    impact += " Отрицательно для реактивного";
                    literature_impact += " {1} отричает позицию {0} и остается при своем мнении.";
                    reactive.EI -= r_ei;
                    reactive.JR -= r_jr;
                }

                skillType = SkillType.Phisical;
            }

            var cause = active.GetCause();

            if(res)
            {
                active.Needs[cause.Item1] = 100;
            }
            

            Console.WriteLine(emotional_change);
            Console.WriteLine();

            Console.WriteLine(impact);
            Console.WriteLine();

            Console.WriteLine("Действие: " + cause.Item2 + " "+ String.Format(active.GetAction(emotionsType, skillType, emotional_rate, skill_rate), active.name, reactive.name));
            Console.WriteLine();

            Console.WriteLine("Последствие: " + String.Format(literature_impact, active.name, reactive.name) + " " + active.GetCauseResult(cause.Item1, res) + " " + active.GetNewProblem());
            Console.WriteLine();
        }

        public void Action()
        {

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            
            var l1 = new Location("Дом");
            var l2 = new Location("Парадная");

            Random random = new Random();

            bool ForGooglePlay = true;
            string EngineConfigPath = @"C:\Programs\Epic\UE_Projects\Cardboard\dev1\for1channel\Config\DefaultEngine.ini";
            if (ForGooglePlay)
            {
                File.WriteAllText(EngineConfigPath, Regex.Replace(File.ReadAllText(EngineConfigPath), @"PackageName=(.*)", "StoreVersion=com.ivl.Fantastika"));
            }

            {
                //for (int i = 0; i < 10; i++)
                //{
                //    Console.WriteLine("Номер карточки: " + i);
                //    Console.WriteLine();

                //    var h1 = new Hero("Бомж", random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble(), random.Next(0, 101), random.Next(0, 101), random.Next(0, 101), random.Next(0, 101));
                //    var h2 = new Hero("Прохожий", random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble(), random.Next(0, 101), random.Next(0, 101), random.Next(0, 101), random.Next(0, 101));
                //    var sc = new ScenarioСard(l1, l2, h1, h2);

                //    Console.WriteLine("_____________________________________________________________________________________________");
                //    Console.WriteLine();
                //}
            }

            //            {
            //                var h1 = new Hero("Герой 1", random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble());
            //                var h2 = new Hero("Герой 2", random.NextDouble(), random.NextDouble(), random.NextDouble(), random.NextDouble());

            //                for (int i = 0; i < 10; i++)
            //                {
            //                    Console.WriteLine("Номер карточки: " + i);
            //                    Console.WriteLine();

            //;
            //                    var sc = new ScenarioСard(l1, l2, i % 2 == 0 ? h1 : h2, i % 2 == 0 ? h2 : h1);

            //                    Console.WriteLine("_____________________________________________________________________________________________");
            //                    Console.WriteLine();
            //                }
            //            }

        }

    }
}


