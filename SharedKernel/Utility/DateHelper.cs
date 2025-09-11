using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Utility
{
    public static class DateHelper
    {
        public static T AddBusinessDays<T>(int numberOfDays, DateTime? startDate = null) where T : struct
        {
            DateTime date = startDate ?? DateTime.Today;
            int addedDays = 0;

            while (addedDays < numberOfDays)
            {
                date = date.AddDays(1);

                // Check if the day is a weekend
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    addedDays += 1;
                }
            }

            if (typeof(T) == typeof(DateTime))
            {
                return (T)(object)date;
            }
            else if (typeof(T) == typeof(DateOnly))
            {
                return (T)(object)DateOnly.FromDateTime(date);
            }
            else
            {
                throw new InvalidOperationException("Unsupported type");
            }
        }

        public static T AddBusinessDaysOffset<T>(int numberOfDays, DateTimeOffset? startDate = null) where T : struct
        {
            DateTimeOffset date = startDate ?? DateTimeOffset.UtcNow;
            int addedDays = 0;
            int direction = numberOfDays >= 0 ? 1 : -1;
            int absDays = Math.Abs(numberOfDays);

            while (addedDays < absDays)
            {
                date = date.AddDays(direction);

                // Check if the day is a weekend
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    addedDays += 1;
                }
            }

            if (typeof(T) == typeof(DateTime))
            {
                return (T)(object)date.DateTime;
            }
            else if (typeof(T) == typeof(DateTimeOffset))
            {
                return (T)(object)date;
            }
            else if (typeof(T) == typeof(DateOnly))
            {
                return (T)(object)DateOnly.FromDateTime(date.Date);
            }
            else
            {
                throw new InvalidOperationException("Unsupported type");
            }
        }

        public static DateTimeOffset GetNextBusinessDay(DateTimeOffset date)
        {
            // Check if the input date is a business day
            while (!IsBusinessDay(date))
            {
                // Move to the next day if it's not a business day
                date = date.AddDays(1);
            }

            return date;
        }

        public static DateTimeOffset GetPriorBusinessDay(DateTimeOffset date)
        {
            // Check if the input date is a business day
            while (!IsBusinessDay(date))
            {
                // Move to the next day if it's not a business day
                date = date.AddDays(-1);
            }

            return date;
        }

        // Function to check if a given date is a business day
        public static bool IsBusinessDay(DateTimeOffset date)
        {
            // Check if the day is a weekend (Saturday or Sunday)
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }

            // Check if the day is a public holiday
            //if (PublicHolidays.Contains(date.Date))
            //{
            //    return false;
            //}

            return true; // If it's not a weekend or holiday, it's a business day
        }
    }
}
