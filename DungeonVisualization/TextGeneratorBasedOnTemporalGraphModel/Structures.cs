
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
        Change,
        StartMission,
        MissionInProgress,
        FinishMission
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
}
