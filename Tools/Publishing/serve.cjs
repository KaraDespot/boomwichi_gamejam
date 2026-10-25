// Local-only release-build measurement server. Does not modify build files.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(process.argv[2] || 'Builds/PublishingBaseline');
const port = Number(process.argv[3] || 8080);
let instrumentation = `<script>
performance.setResourceTimingBufferSize(2000);
window.__publishingMessages=[];
for(const level of ['log','warn','error']) {
 const original=console[level].bind(console);
 console[level]=(...args)=>{const message=args.join(' ');window.__publishingMessages.push({level,message,ms:performance.now()});original(...args);
  if(message.includes('SceneLoader: loading scene MainMenu')&&!window.__publishingReadyMs)requestAnimationFrame(()=>requestAnimationFrame(()=>{
   if(!window.__publishingReadyMs){window.__publishingReadyMs=performance.now();window.__publishingInitialResources=performance.getEntriesByType('resource').map(r=>({url:r.name,transfer:r.transferSize,encoded:r.encodedBodySize,decoded:r.decodedBodySize,duration:r.duration}));}
  }));};
}
addEventListener('DOMContentLoaded',()=>{
 const output=document.createElement('pre');output.id='publishing-measurement';
 output.style='position:fixed;bottom:0;left:0;max-width:100vw;max-height:26vh;overflow:auto;background:#111e;color:#dff;z-index:99999;font:12px monospace;pointer-events:none';
 document.body.append(output);
 if(new URLSearchParams(location.search).has('publishingTest')) {
  const controls=document.createElement('div');controls.style='position:fixed;top:0;left:0;z-index:99999;background:#eee;padding:8px';
  for(const command of ['start','results','interstitial','rewarded','pause','resume','platformPause','platformResume','mute','unmute','status','storage']) {
   const button=document.createElement('button');button.textContent=command;
   button.onclick=()=>{
    if(command==='storage')window.bridge?.storage.get(['crunchie_munchie_v1'],false).then(value=>console.log('Storage snapshot: '+JSON.stringify(value))).catch(error=>console.error('Storage snapshot failed: '+error));
    else window.unityInstance?.SendMessage('PublishingServices','RunLocalDiagnostic',command);
   };
   controls.append(button);
  }
  document.body.append(controls);
 }
 setInterval(()=>{
  const messages=window.__publishingMessages;
  const resources=performance.getEntriesByType('resource');
  output.textContent=JSON.stringify({readyMs:window.__publishingReadyMs??null,elapsedMs:performance.now(),
   initialResources:window.__publishingInitialResources??null,
   transferBytes:resources.reduce((n,r)=>n+r.transferSize,0),encodedBodyBytes:resources.reduce((n,r)=>n+r.encodedBodySize,0),
   decodedBodyBytes:resources.reduce((n,r)=>n+r.decodedBodySize,0),
   resources:resources.map(r=>({url:r.name,transfer:r.transferSize,encoded:r.encodedBodySize,decoded:r.decodedBodySize,duration:r.duration})),
   messages:messages.slice(-20)},null,2);
 },1000);
});
</script>`;
instrumentation += '<script>' + fs.readFileSync(path.join(__dirname, 'local-qa.js'), 'utf8') + '</script>';
http.createServer((req,res)=>{
 let url;
 try { url=decodeURIComponent(new URL(req.url,'http://localhost').pathname); } catch {res.writeHead(400).end();return;}
 const file=path.resolve(root,'.'+(url==='/'?'/index.html':url));
 if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}
 fs.readFile(file,(error,data)=>{
  if(error){res.writeHead(404).end('Not found');return;}
  const headers={'Cache-Control':'no-store','Timing-Allow-Origin':'*'};
  const plain=file.replace(/\.(br|gz)$/,'');
  if(file.endsWith('.br'))headers['Content-Encoding']='br';
  if(file.endsWith('.gz'))headers['Content-Encoding']='gzip';
  const ext=path.extname(plain);
  headers['Content-Type']=({'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json','.png':'image/png','.data':'application/octet-stream','.unityweb':'application/octet-stream'})[ext]||'application/octet-stream';
  if(ext==='.html')data=Buffer.from(data.toString().replace('</head>',instrumentation+'</head>'));
  headers['Content-Length']=data.length;
  res.writeHead(200,headers);res.end(data);
 });
}).listen(port,'127.0.0.1',()=>console.log('Publishing measurement: http://127.0.0.1:'+port+' root='+root));
