const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const root = path.resolve(process.argv[2]);
const output = process.argv[3];
function walk(directory) {
 return fs.readdirSync(directory,{withFileTypes:true}).flatMap(entry=>{
  const file=path.join(directory,entry.name);
  if(entry.isDirectory())return walk(file);
  const data=fs.readFileSync(file);
  let decodedBytes=data.length, compression='none';
  try {
   if(data[0]===0x1f&&data[1]===0x8b){decodedBytes=zlib.gunzipSync(data).length;compression='gzip';}
   else if(file.endsWith('.br')){decodedBytes=zlib.brotliDecompressSync(data).length;compression='brotli';}
  }catch(error){compression='decode-error: '+error.message;decodedBytes=null;}
  return [{file:path.relative(root,file).replaceAll('\\','/'),bytes:data.length,decodedBytes,compression}];
 });
}
const files=walk(root).sort((a,b)=>b.bytes-a.bytes);
const report={root,measuredAt:new Date().toISOString(),fileBytes:files.reduce((n,f)=>n+f.bytes,0),
 decodedBytes:files.reduce((n,f)=>n+(f.decodedBytes??0),0),files,
 note:'File totals are not browser transfer totals. Symbols/debug artifacts, when present, are listed but may not be requested.'};
if(output)fs.writeFileSync(output,JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
