using PeopleHub.Domain;

namespace PeopleHub.Services;

public sealed class ShellModel
{
    public required AppUser User {get;init;}
    public required Company Company {get;init;}
    public required string Role {get;init;}
    public required IReadOnlyList<ModuleSpec> Navigation {get;init;}
    public required string ActiveSlug {get;init;}
}
public sealed class DashboardModel
{
    public required ShellModel Shell {get;init;}
    public int Employees {get;set;}
    public int Present {get;set;}
    public int Leaves {get;set;}
    public int Tickets {get;set;}
    public int Vacancies {get;set;}
    public int Expenses {get;set;}
    public IReadOnlyList<RecordRow> Recent {get;set;}=[];
}
public sealed class ModuleModel
{
    public required ShellModel Shell {get;init;}
    public required ModuleSpec Module {get;init;}
    public required ListResult Results {get;set;}
    public string Search {get;set;}="";
    public bool CanWrite {get;set;}
    public bool CanDecide {get;set;}
    public IReadOnlyDictionary<string,Dictionary<int,string>> LookupNames {get;set;}=new Dictionary<string,Dictionary<int,string>>();
    public string Display(RecordRow row,string name)
    {
        var raw=row.Fields.GetValueOrDefault(name);
        if(raw is null)return "—";
        if(Modules.Foreign.TryGetValue(name,out var foreign)&&LookupNames.TryGetValue(foreign,out var options)&&int.TryParse(raw.ToString(),out var key))
            return options.GetValueOrDefault(key,$"#{key}");
        if(raw is DateOnly d)return d.ToString("dd MMM yyyy");
        if(raw is DateTime dt)return dt.ToLocalTime().ToString("dd MMM yyyy");
        if(raw is bool b)return b?"Yes":"No";
        return raw.ToString()??"—";
    }
}
public sealed class EditorModel
{
    public required ModuleSpec Module {get;init;}
    public required IReadOnlyList<FieldSpec> Fields {get;init;}
    public required IReadOnlyDictionary<string,Dictionary<int,string>> Lookups {get;init;}
    public RecordRow? Record {get;init;}
    public string Error {get;set;}="";
    public bool ReadOnly {get;init;}
    public string Value(string key)=>Record?.Fields.GetValueOrDefault(key) switch{
        null=>"",bool b=>b?"true":"false",DateOnly d=>d.ToString("yyyy-MM-dd"),
        TimeOnly t=>t.ToString("HH:mm"),decimal n=>n.ToString(System.Globalization.CultureInfo.InvariantCulture),
        object obj=>obj.ToString()??""
    };
}
