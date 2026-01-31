using System;
using System.Collections.Generic;
using System.Linq;

namespace Brudixy.Interfaces.Generators
{
	internal class StorageType
	{
		public string ShortAlias { get; }

		public StorageType(string name, bool comparable, bool arraySupport, string genClassName, string shortAlias)
		{
			ShortAlias = shortAlias;
			EnumName = name;
			Comparable = comparable;
			ArraySupport = arraySupport;
			GenClassName = genClassName;
		}
		
		public string EnumName;
		public bool Comparable;
		public bool CustomComparable;
		public bool ArraySupport;
		public bool RangeSupport = false;
		public bool Struct = true;
		public string GenClassName;
		public string CustomDataItemName;
		public bool GenerateCtr = true;
		public bool AutoIncrementSupport;
		public string ToStr = "return value.ToString();";
		public string ToStrArray;
		public string FromStrArray;
		public string FromStr = "throw new NotImplementedException();";
		public string Default = "new object();";
		public string CustomType;
		public string DeepEquals;
		public string ValueValidator;
		public string[] UserFriendlyAliases = new String[0];
	}
	
	internal static class BuiltinSupportStorageTypes
    {
	    public static List<StorageType> StorageTypes = new ()
	    {
		    new ("Object", false, false, "object", "object") { Struct = false, GenerateCtr = false, RangeSupport = false },
		    new("Boolean", true, false ,"System.Boolean", "bool") { Default = "false", FromStr = "return XmlConvert.ToBoolean(value.ToLower());", ToStr = "return XmlConvert.ToString(value);", RangeSupport = false, UserFriendlyAliases = ["Flag", "Bool"]} ,
		    new("Char", true, false ,"System.Char", "char") { Default = "Char.MinValue", FromStr = "return value.FirstOrDefault();", FromStrArray = "return value.ToCharArray();", ToStr = "return value == char.MinValue ? string.Empty : XmlConvert.ToString(value);", ToStrArray = "return new string(value);"},
		    new("SByte", true, false ,"System.SByte", "sbyte") { Default = "sbyte.MinValue", FromStr = "return XmlConvert.ToSByte(value);", ToStr = "return XmlConvert.ToString(value);", AutoIncrementSupport = true },
		    new("Byte", true, false ,"System.Byte", "byte") { Default = "byte.MinValue", FromStrArray = "return Convert.FromBase64String(value);", FromStr = "return  (byte) Tool.IntParseFast(value, 0, true);", ToStr = "return XmlConvert.ToString(value);", ToStrArray = "return Convert.ToBase64String(value); ", AutoIncrementSupport = true },
		    new("Int16", true, false ,"System.Int16", "short") { Default = "short.MinValue", FromStr = "return (Int16)Tool.IntParseFast(value, 0, true);", ToStr = "return value.ToString(Tool.mNumberinfo ?? Tool.GetNumberFormat());", AutoIncrementSupport = true },
		    new("UInt16", true, false ,"System.UInt16", "ushort") { Default = "ushort.MinValue", FromStr = "return (UInt16)Tool.UIntParseFast(value, 0, true);", ToStr = "return value.ToString(Tool.mNumberinfo ?? Tool.GetNumberFormat());", AutoIncrementSupport = true },
		    new("Int32", true, false ,"System.Int32", "int") { Default = "int.MinValue", FromStr = "return Tool.IntParseFast(value, 0, true);", ToStr = "return value.ToString(Tool.mNumberinfo ?? Tool.GetNumberFormat());", AutoIncrementSupport = true, UserFriendlyAliases = ["Integer"]},
		    new("UInt32", true, false ,"System.UInt32", "uint") { Default = "uint.MinValue", FromStr = "return Tool.UIntParseFast(value, 0, true);", ToStr = "return value.ToString(Tool.mNumberinfo ?? Tool.GetNumberFormat());", AutoIncrementSupport = true },
		    new("Int64", true, false ,"System.Int64", "long") { Default = "long.MinValue", FromStr = "return Tool.LongParseFast(value, 0, true);", ToStr = "return value.ToString(Tool.mNumberinfo ?? Tool.GetNumberFormat());", AutoIncrementSupport = true },
		    new("UInt64", true, false ,"System.UInt64", "ulong") { Default = "ulong.MinValue", FromStr = "return Tool.ULongParseFast(value, 0, true);", ToStr = "return value.ToString(Tool.mNumberinfo ?? Tool.GetNumberFormat());", AutoIncrementSupport = true },
		    new("Single", true, false ,"System.Single", "float") { DeepEquals = "Math.Abs(val1 - val2) < 0.0000000000000001f", Default = "float.MinValue", FromStr = "return (float)Tool.DoubleParseVeryFast(value, 0d, true);", AutoIncrementSupport = true, ToStr = "return Tool.FloatToString(value, string.Empty);"},
		    new("Double", true, false ,"System.Double", "double") { DeepEquals = "Math.Abs(val1 - val2) < 0.0000000000000001d", Default = "double.MinValue", FromStr = "return Tool.DoubleParseVeryFast(value, 0d, true);", AutoIncrementSupport = true, ToStr = "return Tool.DoubleToString(value, string.Empty);"},
		    new("Decimal", true, false ,"System.Decimal", "decimal") { Default = "decimal.MinValue", FromStr = "return Decimal.Parse(value, Tool.mNumberinfo ?? Tool.GetNumberFormat());", AutoIncrementSupport = true, ToStr = "return Tool.DecimalToString(value, string.Empty);", UserFriendlyAliases = ["Money"]},
		    new("DateTime", true, false ,"System.DateTime", "DateTime") { Default = "DateTime.MinValue", FromStr = "return XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Utc);", ToStr = "return XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);", ValueValidator = "DefaultDataItemValueValidator.DateTimeValidator"},
		    new("TimeSpan", true, false ,"System.TimeSpan", "TimeSpan") { Default = "TimeSpan.MinValue", FromStr = "return XmlConvert.ToTimeSpan(value);", ToStr = "return XmlConvert.ToString(value);"},
		    new("String", true, false ,"System.String", "string") { Default = "string.Empty", FromStr = "return value;", ToStr = "return value;", Struct = false, CustomComparable = true, RangeSupport = false, CustomDataItemName = "StringDataItem", ValueValidator = "DefaultDataItemValueValidator.StringValidator" },
		    new("Guid", true, false ,"System.Guid", "guid") { Default = "Guid.Empty", FromStr = "return Guid.Parse(value);", RangeSupport = false},
		    new("DateTimeOffset", true, false ,"System.DateTimeOffset", "DateTimeOffset") { Default = "DateTimeOffset.MinValue", FromStr = "return XmlConvert.ToDateTimeOffset(value);", ToStr = "return XmlConvert.ToString(value);"},
		    new("BigInteger", true, false ,"System.Numerics.BigInteger", "BigInteger") { Default = "BigInteger.MinusOne", FromStr = "return BigInteger.Parse(value);", ToStr = "return value.ToString(\"D\", CultureInfo.InvariantCulture);"},
		    new("Uri", false, false ,"Uri", "uri") { CustomType = "Uri", Default = "new Uri(\"http://temp\")", FromStr = "return new Uri(value);", Struct = false, RangeSupport = false  },
		    new("Type", false, false ,"System.Type", "type") { CustomType = "Type", Default = "typeof(object)", FromStr = "return Type.GetType(value);", ToStr = "return ((Type)value).AssemblyQualifiedName;", Struct = false , RangeSupport = false},
		    new("Xml", false, false ,"XElement", "xml") { DeepEquals = "XElement.DeepEquals(val1, val2)", CustomType = "XElement", Default = "new XElement(\"temp\")", FromStr = "return XElement.Parse(value);", Struct = false, GenerateCtr = false, RangeSupport = false },
		    new("Json", false, false ,"JsonObject", "json") { DeepEquals = "JsonObject.DeepEquals(val1, val2)", CustomType = "JsonObject", Default = " new JsonObject() { {\"temp\", null} };", FromStr = "return JsonObject.Parse(value);", Struct = false, GenerateCtr = false, RangeSupport = false },
		    new("UserType", false, false ,"object", "") { Struct = false },
	    };

	    public static readonly Dictionary<string, string> KnownTypesToGenClassName = StorageTypes.Select(s => (s.EnumName, s.GenClassName, s.ShortAlias)).Where(s => string.IsNullOrEmpty(s.ShortAlias) == false).Select(s => s).ToDictionary(s => s.EnumName, s => s.GenClassName, StringComparer.OrdinalIgnoreCase);
	    public static readonly Dictionary<string, string> UserFriendlyAliasMapTypes = StorageTypes.Where(s => s.UserFriendlyAliases.Length > 0 && string.IsNullOrEmpty(s.ShortAlias) == false).SelectMany(s => s.UserFriendlyAliases, (type, s) => (UserFriendly: s, type.EnumName)).Select(s => s).ToDictionary(s => s.UserFriendly, s => s.EnumName, StringComparer.OrdinalIgnoreCase);
	    public static readonly Dictionary<string, string> AliasMapTypes = StorageTypes.Where(s => string.IsNullOrEmpty(s.ShortAlias) == false).ToDictionary(s => s.ShortAlias, s => s.EnumName, StringComparer.OrdinalIgnoreCase);
    }
}