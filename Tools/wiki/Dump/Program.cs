using System; using System.Collections.Generic; using System.Linq; using System.Reflection; using System.Text.Json; using Contigu.Core;
static class P {
  static Dictionary<string,object> Fields(object o){ var d=new Dictionary<string,object>(); foreach(var f in o.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance)){ var v=f.GetValue(o); d[f.Name]=Conv(v);} return d; }
  static object Conv(object v){ if(v==null) return null; var t=v.GetType(); if(t.IsEnum) return v.ToString(); if(t.IsPrimitive||v is string) return v; if(v is Array a){ var l=new List<object>(); foreach(var x in a) l.Add(Conv(x)); return l;} if(v is System.Collections.IEnumerable) return null; return Fields(v);} 
  static void Main(string[] args){
    var root=new Dictionary<string,object>();
    var mods=new List<object>();
    foreach(ModifierId id in Enum.GetValues(typeof(ModifierId))){ if(ModifierCatalog.IsRetired(id)) continue; ModifierDefinition m; try{ m=ModifierCatalog.Get(id);}catch{continue;} if(m==null) continue; mods.Add(new{ n=(int)id, id=id.ToString(), name=m.Name, cat=m.Category.ToString(), desc=m.Description, price=ModifierPricing.GetPrice(id), rarity=ModifierRarityUtility.Of(id).ToString() }); }
    root["fertilizers"]=mods;
    root["upgrades"]=UpgradeCatalog.All.Select(u=>new{ n=(int)u.Id, id=u.Id.ToString(), pool=u.Pool.ToString(), name=u.Name, desc=u.Description, rarity=u.Rarity.ToString(), sub=u.RequiresSubChoice }).ToList();
    root["packs"]=UpgradeCatalog.WorkshopPacks.Select(u=>new{ id=u.Id.ToString(), name=u.Name, desc=u.Description }).ToList();
    var en=new List<object>(); foreach(EnemyId id in Enum.GetValues(typeof(EnemyId))){ var e=EnemyCatalog.Get(id); if(e==null) continue; en.Add(new{ id=id.ToString(), name=e.Name, role=e.Role.ToString(), hp=e.MaxHp, desc=e.Description, lore=e.Lore }); } root["enemies"]=en;
    var we=new List<object>(); foreach(WeatherId id in Enum.GetValues(typeof(WeatherId))){ string n,d; try{ n=WeatherCatalog.GetName(id,"(color)"); d=WeatherCatalog.GetDescription(id,"(color)"); }catch{continue;} we.Add(new{ id=id.ToString(), mech=WeatherCatalog.Mechanic(id).ToString(), name=n, desc=d, weight=WeatherCatalog.GetWeight(id), harsh=WeatherCatalog.IsHarsh(id), rarity=WeatherCatalog.GetRarityLabel(id), color=WeatherCatalog.UsesColor(id)}); } root["weather"]=we;
    root["challenges"]=ChallengeCatalog.All.Select(c=>{ var d=Fields(c); d["n"]=(int)c.Id; var ros=new List<object>(); for(int r=0;r<c.RoundCount;r++){ ros.Add(new{ round=r+1, quota=c.QuotaFor(r), budget=c.BudgetFor(r), enemies=EncounterCatalog.GetEncounter(c,r).Select(x=>x.ToString()).ToList()}); } d["rounds"]=ros; return d; }).ToList();
    root["shapes"]=PieceShapeCatalog.AllIds.Select(i=>new{ id=i.ToString(), cells=PieceShapeCatalog.Get(i).Cells.Select(c=>new[]{c.x,c.y}).ToList()}).ToList();
    root["pledges"]=PledgeCatalog.All.Select(p=>new{ name=PledgeCatalog.Name(p), desc=PledgeCatalog.Description(p)}).ToList();
    root["events"]=EventCatalog.All.Select(e=>new{ id=e.Id, title=e.Title, story=e.Story, options=e.Options.Select(o=>new{ label=o.Label, req=o.RequiredPollen, outcomes=o.Outcomes.Select(oc=>new{ w=oc.Weight, fx=oc.Effects.Select(x=>x.Describe()).ToList()}).ToList()}).ToList()}).ToList();
    root["unlocks"]=UnlockCatalog.All.Select(u=>new{ kind=u.Kind.ToString(), id=u.Id, key=u.Key, text=u.Condition.Text, scope=u.Condition.Scope.ToString(), thr=u.Condition.Threshold }).ToList();
    var consts=new Dictionary<string,object>(); foreach(var t in new[]{typeof(RunConfig),typeof(ScoringConstants),typeof(EconomyConstants)}){ var d=new Dictionary<string,object>(); foreach(var f in t.GetFields(BindingFlags.Public|BindingFlags.Static)){ var v=f.GetValue(null); if(v is int||v is float||v is double||v is bool||v is string) d[f.Name]=v; else if(v is int[] ia) d[f.Name]=ia; else if(v is float[] fa) d[f.Name]=fa; } consts[t.Name]=d; } root["consts"]=consts;
    Console.WriteLine(JsonSerializer.Serialize(root,new JsonSerializerOptions{WriteIndented=false,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
  }
}
