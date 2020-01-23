using System;
using System.Globalization;
using Newtonsoft.Json;
using restapi.Helpers;
using System.Collections.Generic;

namespace restapi.Models
{
    public class TimecardLine
    {
        public TimecardLine() { }

        public TimecardLine(DocumentLine line)
        {
            Week = line.Week;
            Year = line.Year;
            Day = line.Day;
            Hours = line.Hours;
            Project = line.Project;

            Recorded = DateTime.UtcNow;
            Updated = DateTime.UtcNow;

            SetupPeriodValues();

            UniqueIdentifier = Guid.NewGuid();
        }

        public TimecardLine Update(DocumentLine line)
        {
            Week = line.Week;
            Year = line.Year;
            Day = line.Day;
            Hours = line.Hours;
            Project = line.Project;

            Updated = DateTime.UtcNow;

            SetupPeriodValues();

            return this;
        }

        // Added method to update items in line in a timecard
        public TimecardLine Patch(TimecardLine currentLine, DocumentLine line)
        {
            if(line.Week != 0 && line.Week != currentLine.Week)
            {
                currentLine.Week = line.Week;
            }
            if (line.Year != 0 && line.Year != currentLine.Year)
            {
                currentLine.Year = line.Year;
            }
            if (line.Day != 0 && line.Day != currentLine.Day)
            {
                currentLine.Day = line.Day;
            }
            if (line.Hours != 0 && line.Hours != currentLine.Hours)
            {
                currentLine.Hours = line.Hours;
            }
            if (line.Project != null && line.Project != ""  && line.Project != currentLine.Project)
            {
                currentLine.Project = line.Project;
            }

            currentLine.Updated = DateTime.UtcNow;
            return currentLine;
        }

     

        // public TimecardLine Update(JObject line)
        // {
        //     // this is a little too brittle for my taste because of the
        //     // hard-coded strings, but it does work to show that you need
        //     // to step outside of the type system to make this work
        //     //
        //     // and, because this is brittle, it should be wrapped in an
        //     // appropriate try/catch to throw a 4xx error back

        //     Week = (int)(line.SelectToken("week") ?? Week);
        //     Year = (int)(line.SelectToken("year") ?? Year);
        //     var day = line.SelectToken("day");
        //     Hours = (float)(line.SelectToken("hours") ?? Hours);
        //     Project = (string)(line.SelectToken("project") ?? Project);

        //     if (day != null)
        //     {
        //         Day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), (string)day, true);
        //     }

        //     // if the date components change, let's update
        //     SetupPeriodValues();

        //     return this;
        // }

        [JsonIgnore]
        public DateTime workDate { get; set; }

        [JsonIgnore]
        public DateTime periodFrom { get; set; }

        [JsonIgnore]
        public DateTime periodTo { get; set; }

        public int Week { get; set; }

        public int Year { get; set; }

        public DayOfWeek Day { get; set; }

        public float Hours { get; set; }

        public string Project { get; set; }

        public DateTime Recorded { get; set; }

        public DateTime Updated { get; set; }

        public string WorkDate { get => workDate.ToString("yyyy-MM-dd"); }

        [JsonProperty("id")]
        public Guid UniqueIdentifier { get; set; }

        public string PeriodFrom => periodFrom.ToString("yyyy-MM-dd");

        public string PeriodTo => periodTo.ToString("yyyy-MM-dd");

        [JsonProperty("actions")]
        public IList<DocumentLink> Documents { get => GetDocumentLinks(); }

        public string Version { get; set; } = "line-0.1";

        private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            var firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            var firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }

            var result = firstThursday.AddDays(weekNum * 7);

            return result.AddDays(-3);
        }

        private void SetupPeriodValues()
        {
            workDate = FirstDateOfWeekISO8601(Year, Week).AddDays((int)Day - 1);
            periodFrom = FirstDateOfWeekISO8601(Year, Week);
            periodTo = periodFrom.AddDays(7);
        }


        public override string ToString()
        {
            return PublicJsonSerializer.SerializeObjectIndented(this);
        }

        private IList<DocumentLink> GetDocumentLinks()
        {
            var links = new List<DocumentLink>();
            links.Add(new DocumentLink()
            {
                Method = Method.Post,
                Type = ContentTypes.TimesheetLine,
                Relationship = DocumentRelationship.Lines,
                Reference = $"/lines/{UniqueIdentifier}/replace"
            });
            links.Add(new DocumentLink()
            {
                Method = Method.Patch,
                Type = ContentTypes.TimesheetLine,
                Relationship = DocumentRelationship.Lines,
                Reference = $"/lines/{UniqueIdentifier}/update"
            });

            return links;
        }

    }
}