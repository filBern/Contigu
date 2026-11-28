import json,re,sys
import os
OUT=os.path.join(os.path.dirname(os.path.abspath(__file__)),'out')
d=json.load(open(os.path.join(OUT,'dump.json')))
unl={u['key']:u for u in d['unlocks']}
def un(kind,n):
    u=unl.get(kind+str(n)); return u['text'] if u else None
def clean(s): 
    s=s.replace('((color))','').replace('(color)','the chosen color')
    return s
out={}
out['fert']=[dict(n=f['n'],id=f['id'],name=f['name'],cat=('Seed Bag' if f['cat']=='SeedBag' else f['cat']),desc=f['desc'],price=f['price'],rar=f['rarity'],unlock=un('F',f['n'])) for f in d['fertilizers']]
grid=[u for u in d['upgrades'] if u['pool']=='Grid']
out['sprouts']=[dict(n=u['n'],id=u['id'],name=u['name'],desc=u['desc'],rar=u['rarity'],unlock=un('S',u['n'])) for u in grid]
out['other']=[dict(id=u['id'],pool=u['pool'],name=u['name'],desc=u['desc'],rar=u['rarity']) for u in d['upgrades'] if u['pool']!='Grid']
out['packs']=d['packs']
out['enemies']=d['enemies']
out['weather']=[dict(id=w['id'],mech=w['mech'],name=re.sub(r'\s*\(\(color\)\)','',w['name']).replace('(color)','').strip(),desc=clean(w['desc']),weight=w['weight'],harsh=w['harsh'],rar=w['rarity'],color=w['color']) for w in d['weather']]
ch=[]
for c in d['challenges']:
    ch.append(dict(n=c['n'],id=c['Id'],name=c['Name'],desc=c['Description'],rounds=c['RoundCount'],grid=c['GridSize'],colors=c['ActiveColorCount'],world=c['WorldEnemy'],swaps=c['EnemySwaps'],hp=c['EnemyHpMultiplier'],bonus=c['PieceBudgetBonus'],life=c['FertilizerLifespanRounds'],pollen=c['RoundEndPollenBonus'],wint=c['WeatherIntensity'],wprof=c['WeatherProfile'],shuf=c['ShuffleDelta'],late=c['LateHpMultiplier'],act1=c['ActOneHpMultiplier'],price=c['FertilizerPriceDelta'],unlock=un('C',c['n']),table=c['rounds']))
out['challenges']=ch
out['pledges']=d['pledges']; out['events']=d['events']; out['shapes']=d['shapes']
E=d['consts']['EconomyConstants']; S=d['consts']['ScoringConstants']; R=d['consts']['RunConfig']
out['c']=dict(E=E,R={k:v for k,v in R.items()},S={k:S[k] for k in ['GroupBonusPerCell','GoldenCellBonus','LineClearBonusPerCell','GridClearMultiplier','PointsPerMult','EnemyHpGrowthPerRound','EnemyHpGrowthFirstRoundIndex','LateHpFirstRoundIndex','EarlyRoundHpFactors','MaxEnemiesPerRound','WorldEnemyRoundInterval','EnemyTickInterval','HurricaneInterval','HurricaneTilesMoved','LeechHealPerLineClear','FrostFrozenPlots','PollenShowerPollen','RainPollenPerSeed','SunshinePointsPerCell','DewPollenPerLine','GoldenHourPlots','HoarfrostMaxFrozenPlots','LocustPollenPerShuffle','EnemyHpGrowthPerRoundEndless']})
out['unlockCounts']={k:sum(1 for u in d['unlocks'] if u['kind']==k) for k in ['Challenge','Sprout','Fertilizer']}
DATA=os.path.join(OUT,'data.json')
json.dump(out,open(DATA,'w'),ensure_ascii=False,separators=(',',':'))
print(len(open(DATA).read()), sum(1 for f in out['fert'] if f['unlock']), sum(1 for f in out['sprouts'] if f['unlock']), len(out['fert']), len(out['sprouts']), out['unlockCounts'])
print(out['challenges'][1]['table'][:3], out['packs'][0])
