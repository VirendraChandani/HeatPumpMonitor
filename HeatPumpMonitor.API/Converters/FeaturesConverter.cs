using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace HeatPumpMonitor.API.Converters
{
    public class FeaturesConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            return text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => x.Trim())
                      .ToList();
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is List<string> features)
            {
                return string.Join(",", features);
            }
            return string.Empty;
        }
    }
}
