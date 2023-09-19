using AutoMapper;
using MessagePack;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace API
{
    public static class AutoMap
    {
        public static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<DBNull, DateTime?>().ConvertUsing<DateTimeNullableDBNullConverter>();
                cfg.CreateMap<DBNull, DateTime>().ConvertUsing<DateTimeDBNullConverter>();
                cfg.CreateMap<DBNull, int>().ConvertUsing<IntDBNullConverter>();
                cfg.CreateMap<DBNull, decimal>().ConvertUsing<DecimalDBNullConverter>();
                cfg.CreateMap<JValue, bool>().ConvertUsing<StringToBoolConverter>();
                cfg.CreateMap<JValue, bool?>().ConvertUsing<StringToNullBoolConverter>();
                cfg.CreateMap<JValue, int>().ConvertUsing<StringToIntConverter>();
                cfg.CreateMap<JValue, int?>().ConvertUsing<StringToNullIntConverter>();
                cfg.CreateMap<JValue, double>().ConvertUsing<StringToDoubleConverter>();
                cfg.CreateMap<JValue, double?>().ConvertUsing<StringToNullDoubleConverter>();
                cfg.CreateMap<JValue, float>().ConvertUsing<StringToFloatConverter>();
                cfg.CreateMap<JValue, float?>().ConvertUsing<StringToNullFloatConverter>();
                cfg.CreateMap<JValue, DateTime>().ConvertUsing<StringToDateTimeConverter>();
                cfg.CreateMap<JValue, DateTime?>().ConvertUsing<StringToNullDateTimeConverter>();
                cfg.CreateMap<JValue, Decimal>().ConvertUsing<StringToDecimalConverter>();
                cfg.CreateMap<JValue, Decimal?>().ConvertUsing<StringToNullDecimalConverter>();
            });

            var mapper = config.CreateMapper();
            return mapper;
        }

        public static IMapper Mapper;

        public class DateTimeDBNullConverter : ITypeConverter<DBNull, DateTime>
        {
            public DateTime Convert(DBNull source, DateTime destination, ResolutionContext context) => default;
        }
        public class DateTimeNullableDBNullConverter : ITypeConverter<DBNull, DateTime?>
        {
            public DateTime? Convert(DBNull source, DateTime? destination, ResolutionContext context) => null;
        }

        public class IntDBNullConverter : ITypeConverter<DBNull, int>
        {
            public int Convert(DBNull source, int destination, ResolutionContext context) => default;
        }

        public class DecimalDBNullConverter : ITypeConverter<DBNull, decimal>
        {
            public decimal Convert(DBNull source, decimal destination, ResolutionContext context) => default;
        }
        public class StringToBoolConverter : ITypeConverter<JValue, bool>
        {
            public bool Convert(JValue source, bool destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return default;
                }
                else
                {
                    if (!bool.TryParse(s, out bool result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToNullBoolConverter : ITypeConverter<JValue, bool?>
        {
            public bool? Convert(JValue source, bool? destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return null;
                }
                else
                {
                    if (!bool.TryParse(s, out bool result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToIntConverter : ITypeConverter<JValue, int>
        {
            public int Convert(JValue source, int destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return default;
                }
                else
                {
                    if (!int.TryParse(s, out int result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToNullIntConverter : ITypeConverter<JValue, int?>
        {
            public int? Convert(JValue source, int? destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return null;
                }
                else
                {
                    if (!int.TryParse(s, out int result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToDoubleConverter : ITypeConverter<JValue, double>
        {
            public double Convert(JValue source, double destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return default;
                }
                else
                {
                    if (!double.TryParse(s, out double result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToNullDoubleConverter : ITypeConverter<JValue, double?>
        {
            public double? Convert(JValue source, double? destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return null;
                }
                else
                {
                    if (!double.TryParse(s, out double result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToFloatConverter : ITypeConverter<JValue, float>
        {
            public float Convert(JValue source, float destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return default;
                }
                else
                {

                    if (!float.TryParse(s, out float result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToNullFloatConverter : ITypeConverter<JValue, float?>
        {
            public float? Convert(JValue source, float? destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return null;
                }
                else
                {
                    if (!float.TryParse(s, out float result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToDecimalConverter : ITypeConverter<JValue, decimal>
        {
            public decimal Convert(JValue source, decimal destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return default;
                }
                else
                {

                    if (!decimal.TryParse(s, out decimal result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToNullDecimalConverter : ITypeConverter<JValue, decimal?>
        {
            public decimal? Convert(JValue source, decimal? destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return null;
                }
                else
                {
                    if (!decimal.TryParse(s, out decimal result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToDateTimeConverter : ITypeConverter<JValue, DateTime>
        {
            public DateTime Convert(JValue source, DateTime destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return default;
                }
                else
                {
                    string apiFormats = ApiServicesHelper.ApiConfiguration.Settings["API_DATETIME_FORMAT"];
                    string[] formatesSplit = apiFormats.Split(',');
                    if (!DateTime.TryParseExact(s, formatesSplit, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }

        public class StringToNullDateTimeConverter : ITypeConverter<JValue, DateTime?>
        {
            public DateTime? Convert(JValue source, DateTime? destination, ResolutionContext context)
            {
                string s = source.Value == null ? null : source.Value.ToString();
                if (s.IsNullOrEmpty())
                {
                    return null;
                }
                else
                {
                    string apiFormats = ApiServicesHelper.ApiConfiguration.Settings["API_DATETIME_FORMAT"];
                    string[] formatesSplit = apiFormats.Split(',');
                    if (!DateTime.TryParseExact(s, formatesSplit, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result)) throw new FormatException("Invalid input format");
                    return result;
                }
            }
        }
    }
}

