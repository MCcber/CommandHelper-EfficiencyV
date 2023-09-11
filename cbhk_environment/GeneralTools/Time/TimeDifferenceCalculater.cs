using System;

namespace cbhk_environment.GeneralTools.Time
{
    class TimeDifferenceCalculater
    {
        public static string Calculate(DateTime targetTime)
        {
            string result = "";
            int year = DateTime.Now.Year - targetTime.Year;
            int month = DateTime.Now.Month - targetTime.Month;
            int day = DateTime.Now.Day - targetTime.Day;
            int hour = DateTime.Now.Hour - targetTime.Hour;
            if (year > 1)
                result = "很久前";
            if (year == 1)
                result = "去年";
            if (year == 0 && month > 1)
                result = "一年内";
            if (year == 0 && month == 1)
                result = "上个月";
            if (year == 0 && month == 0 && day > 7)
                result = "一月内";
            if (year == 0 && month == 0 && day > 1 && day < 7)
                result = "上周";
            if (year == 0 && month == 0 && day == 7)
                result = "七天内";
            if (year == 0 && month == 0 && day == 1)
                result = "一天前";
            if (year == 0 && month == 0 && day == 0 && hour < 24)
                result = "一天内";
            return result;
        }
    }
}
