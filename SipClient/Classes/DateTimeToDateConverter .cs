using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SipClient.Classes
{
    public class DateTimeToDateConverter : IValueConverter
    {
        private static DateTime currentDate
            = new DateTime(
                DateTime.Now.Year,
                DateTime.Now.Month,
                DateTime.Now.Day);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string answer = String.Empty;
            if (value is DateTime)
            {
                var date = (DateTime)value;

                var cmpDate = new DateTime(date.Year, date.Month, date.Day); // Year:Month:Day

                if (cmpDate < currentDate)
                {
                    answer = ((DateTime)value).ToString("dd.MM.yyyy");
                }
                else
                {
                    answer = ((DateTime)value).ToString("HH:mm");
                }
            }
            return answer;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
