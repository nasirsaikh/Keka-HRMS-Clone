using System.Text.Json;
using System.Text.RegularExpressions;

namespace PeopleHub.Services;

public static class ThemeService
{
    public static readonly Dictionary<string,object> Defaults=new(){
        ["primary"]="#4f46e5",["success"]="#16a34a",["warning"]="#d97706",
        ["danger"]="#dc2626",["info"]="#0891b2",["sidebar_bg"]="#18243a",
        ["body_bg"]="#f5f7fb",["density"]="compact",["sidebar_mode"]="dark",
        ["header_mode"]="light",["mode"]="light",["font_family"]="system",
        ["font_size"]=13,["radius"]=8
    };
    public static readonly Dictionary<string,Dictionary<string,object>> Presets=new(){
        ["Indigo"]=new(),["AdminLTE"]=new(){["primary"]="#0d6efd",["sidebar_bg"]="#343a40"},
        ["Ocean"]=new(){["primary"]="#0891b2",["sidebar_bg"]="#0f2b3d"},
        ["Emerald"]=new(){["primary"]="#059669",["sidebar_bg"]="#064e3b"},
        ["Sunset"]=new(){["primary"]="#ea580c",["sidebar_bg"]="#1c1917"},
        ["Slate"]=new(){["primary"]="#475569",["sidebar_bg"]="#0f172a",["radius"]=3},
        ["Rose"]=new(){["primary"]="#e11d48",["sidebar_bg"]="#fff7f8",["sidebar_mode"]="light"},
    };
    public static bool TryValidate(string json,out string error)
    {
        error="";
        try{
            using var doc=JsonDocument.Parse(json);
            if(doc.RootElement.ValueKind!=JsonValueKind.Object){error="Theme must be an object.";return false;}
            foreach(var item in doc.RootElement.EnumerateObject()){
                if(!Defaults.ContainsKey(item.Name)){error="Unknown theme token.";return false;}
                var name=item.Name;
                if(name is "font_size" or "radius") {
                    if(item.Value.ValueKind!=JsonValueKind.Number || !item.Value.TryGetInt32(out int n) || n<(name=="radius"?0:12)||n>16){error="Invalid size/radius.";return false;}
                }else{
                    if(item.Value.ValueKind!=JsonValueKind.String){error="Invalid theme token type.";return false;}
                    var val=item.Value.GetString()??"";
                    if(name.EndsWith("_bg") || name is "primary" or "danger" or "success" or "warning" or "info"){
                        if(!Regex.IsMatch(val,@"^#[0-9a-fA-F]{6}$")){error="Invalid hex colour.";return false;}
                    } else {
                        var options=name switch{
                            "density"=>new[]{"compact","comfortable"},"sidebar_mode"=>new[]{"dark","light"},
                            "header_mode"=>new[]{"light","dark","primary"},"mode"=>new[]{"light","dark","system"},
                            "font_family"=>new[]{"system","inter","roboto","nunito","manrope"},
                            _=>Array.Empty<string>()
                        };
                        if(!options.Contains(val)){error="Unsupported theme option.";return false;}
                    }
                }
            }
            return true;
        }catch(JsonException){error="Invalid theme JSON.";return false;}
    }
}
