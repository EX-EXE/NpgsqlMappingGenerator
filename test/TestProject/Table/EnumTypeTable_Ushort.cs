using Npgsql;
using NpgsqlMappingGenerator;

namespace TestProject;


[DbTableGenerator("public.enumtype_ushort")]
public partial class EnumTypeTable_Ushort
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

    [DbColumn<DbParamString>("data9")]
    public string Data9 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data10")]
    public string Data10 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data11")]
    public string Data11 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data12")]
    public string Data12 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data13")]
    public string Data13 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data14")]
    public string Data14 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data15")]
    public string Data15 { get; set; } = string.Empty;

    [DbColumn<DbParamString>("data16")]
    public string Data16 { get; set; } = string.Empty;
}