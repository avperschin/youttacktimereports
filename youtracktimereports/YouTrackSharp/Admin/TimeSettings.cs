using System.Collections.Generic;

namespace YouTrackSharp.Admin
{
    public class TimeSettings
    {
        public int HoursADay { get; set; }
        public int DaysAWeek { get; set; }
        public List<workWeek> WorkWeek { get; set; }
    }
    public class workWeek
    {
        public WorkDay Value { get; set; }
    }
    public enum WorkDay
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }
}
