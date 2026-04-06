using System;

namespace UniBridge
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string   PlayerId;
        public string   PlayerName;
        public long     Score;
        public int      Rank;
        public bool     IsCurrentPlayer;
        public DateTime LastReportedDate;
    }
}
