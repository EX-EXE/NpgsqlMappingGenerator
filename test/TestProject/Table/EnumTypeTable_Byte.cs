using Npgsql;
using NpgsqlMappingGenerator;

namespace TestProject;


[DbTableGenerator("public.enumtype_byte")]
public partial class EnumTypeTable_Byte
{
    [DbColumn<DbParamString>("data1")]
    public string Data1 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data2")]
    public string Data2 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data3")]
    public string Data3 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data4")]
    public string Data4 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data5")]
    public string Data5 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data6")]
    public string Data6 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data7")]
    public string Data7 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data8")]
    public string Data8 { get; set; } = string.Empty;
}