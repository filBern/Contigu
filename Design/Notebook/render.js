const {chromium}=require('playwright');const fs=require('fs');
(async()=>{const jobs=JSON.parse(fs.readFileSync(process.argv[2]||'jobs.json'));const b=await chromium.launch();
for(const [svg,png,w,h,transparent] of jobs){const p=await b.newPage({viewport:{width:w,height:h}});
await p.goto('file://'+process.cwd()+'/'+svg);await p.screenshot({path:png,omitBackground:transparent});await p.close();}
await b.close()})()
