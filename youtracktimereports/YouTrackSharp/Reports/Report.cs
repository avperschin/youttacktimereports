using System;
using System.Collections.Generic;
using System.Linq;

namespace YouTrackSharp.Reports
{
    public class Report
    {
        public string id { get; set; }
        public string name { get; set; }
        public string ownerLogin { get; set; }
        public string type { get; set; }
        public bool own { get; set; }
        public VisibleTo visibleTo { get; set; }
        public int invalidationInterval { get; set; }
        public string state { get; set; }
        public string lastCalculated { get; set; }
        public int progress { get; set; }
        public ReportParameters parameters { get; set; }
        public ReportData reportData { get; set; }
        public string oldData { get; set; }
        public DateTime? reportDate { get; set; }
        public bool reportFill { get; set; }
    }
    public class ReportParameters
    {
        public List<Projects> projects { get; set; }
        public string query { get; set; }
        public string queryUrl { get; set; }
        public Range range { get; set; }
        public string groupBy { get; set; }
        public string groupById { get; set; }
        public bool perUserAvailable { get; set; }
        public bool showTypesAvailable { get; set; }
        public string issuesQuery { get; set; }
    }
    public class ReportData
    {
        public bool perUser { get; set; }
        public string groupBy { get; set; }
        public string estimation { get; set; }
        public double estimationNumber
        {
            get
            {
                double Estimation = 0;
                if (!string.IsNullOrEmpty(estimation) && !string.IsNullOrWhiteSpace(estimation))
                {
                    string[] se = estimation.Split(' ');
                    double t = 0;
                    for (int j = 0; j < se.Count(); j++)
                    {
                        if (se[j].Contains("h"))
                        {
                            t += Convert.ToInt32(se[j].Replace("h", "")) * 60;
                        }
                        if (se[j].Contains("m"))
                        {
                            t += Convert.ToInt32(se[j].Replace("m", ""));
                        }
                    }
                    Estimation = t / 60;
                }
                return Estimation;
            }
        }
        public string duration { get; set; }
        public double durationNumber
        {
            get
            {
                double Duration = 0;
                if (!string.IsNullOrEmpty(duration) && !string.IsNullOrWhiteSpace(duration))
                {
                    string[] sd = duration.Split(' ');
                    double t = 0;
                    for (int j = 0; j < sd.Count(); j++)
                    {
                        if (sd[j].Contains("h"))
                        {
                            t += Convert.ToInt32(sd[j].Replace("h", "")) * 60;
                        }
                        if (sd[j].Contains("m"))
                        {
                            t += Convert.ToInt32(sd[j].Replace("m", ""));
                        }
                    }
                    Duration = t / 60;
                }
                return Duration;
            }
        }
        public List<Groups> groups { get; set; }
    }
    public class Groups
    {
        public string name { get; set; }
        public string duration { get; set; }
        public double durationNumber
        {
            get
            {
                double Duration = 0;
                if (!string.IsNullOrEmpty(duration) && !string.IsNullOrWhiteSpace(duration))
                {
                    string[] sd = duration.Split(' ');
                    double t = 0;
                    for (int j = 0; j < sd.Count(); j++)
                    {
                        if (sd[j].Contains("h"))
                        {
                            t += Convert.ToInt32(sd[j].Replace("h", "")) * 60;
                        }
                        if (sd[j].Contains("m"))
                        {
                            t += Convert.ToInt32(sd[j].Replace("m", ""));
                        }
                    }
                    Duration = t / 60;
                }
                return Duration;
            }
        }
        public string estimation { get; set; }
        public double estimationNumber
        {
            get
            {
                double Estimation = 0;
                if (!string.IsNullOrEmpty(estimation) && !string.IsNullOrWhiteSpace(estimation))
                {
                    string[] se = estimation.Split(' ');
                    double t = 0;
                    for (int j = 0; j < se.Count(); j++)
                    {
                        if (se[j].Contains("h"))
                        {
                            t += Convert.ToInt32(se[j].Replace("h", "")) * 60;
                        }
                        if (se[j].Contains("m"))
                        {
                            t += Convert.ToInt32(se[j].Replace("m", ""));
                        }
                    }
                    Estimation = t / 60;
                }
                return Estimation;
            }
        }
        public List<Lines> lines { get; set; }
    }
    public class Lines
    {
        public string userName { get; set; }
        public string issueId { get; set; }
        public string issueUrl { get; set; }
        public string issueSummary { get; set; }
        public string duration { get; set; }
        public double durationNumber
        {
            get
            {
                double Duration = 0;
                if (!string.IsNullOrEmpty(duration) && !string.IsNullOrWhiteSpace(duration))
                {
                    string[] sd = duration.Split(' ');
                    double t = 0;
                    for (int j = 0; j < sd.Count(); j++)
                    {
                        if (sd[j].Contains("h"))
                        {
                            t += Convert.ToInt32(sd[j].Replace("h", "")) * 60;
                        }
                        if (sd[j].Contains("m"))
                        {
                            t += Convert.ToInt32(sd[j].Replace("m", ""));
                        }
                    }
                    Duration = t / 60;
                }
                return Duration;
            }
        }
        public string estimation { get; set; }
        public double estimationNumber
        {
            get
            {
                double Estimation = 0;
                if (!string.IsNullOrEmpty(estimation) && !string.IsNullOrWhiteSpace(estimation))
                {
                    string[] se = estimation.Split(' ');
                    double t = 0;
                    for (int j = 0; j < se.Count(); j++)
                    {
                        if (se[j].Contains("h"))
                        {
                            t += Convert.ToInt32(se[j].Replace("h", "")) * 60;
                        }
                        if (se[j].Contains("m"))
                        {
                            t += Convert.ToInt32(se[j].Replace("m", ""));
                        }
                    }
                    Estimation = t / 60;
                }
                return Estimation;
            }
        }
        public string groupName { get; set; }
        public List<TypeDurations> typeDurations { get; set; }
    }
    public class TypeDurations
    {
        public string workType { get; set; }
        public string timeSpent { get; set; }
    }
    public class CreateReport
    {
        public string type { get; set; }
        public CreateReportParameters parameters { get; set; }
        public bool own { get; set; }
        public string name { get; set; }
    }
    public class CreateReportParameters
    {
        public Range range { get; set; }
        public List<Projects> projects { get; set; }
        public string groupById { get; set; }
    }
    public class Range
    {
        public string type { get; set; }
        public long from { get; set; }
        public long to { get; set; }
    }
    public class Projects
    {
        public string name { get; set; }
        public string shortName { get; set; }
    }
    public class VisibleTo
    {
        public string name { get; set; }
        public string url { get; set; }
    }
    public class csvReport
    {
        public string UserName { get; set; }
        public string UserLogin { get; set; }
        public string GroupName {
            get {
                string groupName = UserName;
                if(!string.IsNullOrEmpty(UserLogin)) groupName += " (" + UserLogin + ")";
                return groupName;
            }
        }
        public List<WorkingDay> WorkingDays { get; set; }
    }
    public class WorkingDay
    {
        public DateTime Date { get; set; }
        public double Duration { get; set; }
        public double Estimation { get; set; }
        public int Norm { get; set; }
        public int WeekNumber { get; set; }
    }
}