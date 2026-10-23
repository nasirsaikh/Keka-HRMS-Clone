using PeopleHub.Services;
using Xunit;
namespace PeopleHub.Tests;
public sealed class ThemeAndModuleTests
{
    [Fact] public void Theme_accepts_valid_tokens() => Assert.True(ThemeService.TryValidate("{\"primary\":\"#4f46e5\",\"density\":\"compact\"}",out _));
    [Fact] public void Theme_rejects_css_injection() => Assert.False(ThemeService.TryValidate("{\"primary\":\"red;url(javascript:evil)\"}",out _));
    [Fact] public void Theme_rejects_unknown_keys() => Assert.False(ThemeService.TryValidate("{\"unexpected\":42}",out _));
    [Fact] public void Module_registry_is_unique_and_complete(){Assert.Equal(Modules.All.Count,Modules.Index.Count);Assert.All(Modules.All,m=>Assert.True(Modules.Handlers.ContainsKey(m.Slug)));}
    [Fact] public void All_registered_modules_are_company_scoped(){foreach(var handler in Modules.Handlers.Values)Assert.True(typeof(PeopleHub.Domain.CompanyEntity).IsAssignableFrom(handler.EntityType));}
}
