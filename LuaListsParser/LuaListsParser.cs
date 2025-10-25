#!/usr/bin/dotnet run
#:package NLua@1.7.6
#:package System.CommandLine@2.0.0-rc.2.25502.107

using System.Text.Json;
using System.CommandLine;
using NLua;

var command = new RootCommand("converts chatsound lua definitions to json")
{
    new Option<string>("--chatsounds-path") {
      Required = true
    }
};

var parseResult = command.Parse(args);
var chatsoundsPath = parseResult.GetValue<string>("--chatsounds-path");

var state = new Lua();

state.DoString(@"
  chatsounds = {}
  c = chatsounds
  
  c.List = {}

  function chatsounds.StartList(name, override)
    c.List[name] = not override and c.List[name] or {}
    L = c.List[name]
  end

  function chatsounds.EndList(name)
    L = nil
  end
");

foreach (var file in Directory.EnumerateFiles($"{chatsoundsPath}/lists_nosend"))
{
  state.DoFile(file);
}
foreach (var file in Directory.EnumerateFiles($"{chatsoundsPath}/lists_send"))
{
  state.DoFile(file);
}

var categories = (LuaTable)state["c.List"];
var categoryDefinitions = ExtractCategoryDefinitions(categories).ToDictionary();


var serialized = JsonSerializer.Serialize(categoryDefinitions);
Console.WriteLine(serialized);

static IEnumerable<(string, Dictionary<string, List<SoundDefinition>>)> ExtractCategoryDefinitions(LuaTable categories)
{
  foreach (KeyValuePair<object, object> cd in categories)
  {
    var category = (string)cd.Key;
    var definitions = (LuaTable)cd.Value;

    yield return (category, ExtractSoundDefinitions(definitions).ToDictionary());
  }
}

static IEnumerable<(string, List<SoundDefinition>)> ExtractSoundDefinitions(LuaTable definitions)
{
  foreach (KeyValuePair<object, object> kv in definitions)
  {
    var name = (string)kv.Key;
    var soundDefinitions = ((LuaTable)kv.Value).Values.Cast<LuaTable>().Select(t => new SoundDefinition((string)t["path"], (double)t["length"]));
    yield return (name, soundDefinitions.ToList());
  }
}

record SoundDefinition(string Path, double Length);