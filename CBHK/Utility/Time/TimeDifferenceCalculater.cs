using System;

namespace CBHK.Utility.Time
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
            if (year > 0 || month > 0 && day < 0)
            {
                result = "更早";
            }
            if (year == 0 && month == 0 && day > 7 && day <= 14)
            {
                result = "上周";
            }
            if (year == 0 && month == 0 && day <= 7)
            {
                result = "一周内";
            }
            if (year == 0 && month == 0 && day == 1)
            {
                result = "一天前";
            }
            if (year == 0 && month == 0 && day == 0 && hour < 24)
            {
                result = "一天内";
            }
            return result;
        }
    }
}