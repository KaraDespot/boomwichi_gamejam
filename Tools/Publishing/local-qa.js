// Local driver for the public QaToolPlatformBridge postMessage protocol.
// Simulated callbacks only: this does not request real advertising inventory.
if (new URLSearchParams(location.search).has('localQa')) {
  let activeAd = null;
  const send = message => window.postMessage({ source: 'publishing_local_qa', ...message }, location.origin);
  const storageKey = key => 'publishing_qa_storage_' + key;
  window.addEventListener('message', event => {
    if (event.source !== window || event.origin !== location.origin || event.data?.source !== 'bridge') return;
    const data = event.data;
    if (data.type === 'platform' && data.action === 'initialize') {
      send({ type: 'platform', action: 'initialize', supportedFeatures: ['isInterstitialSupported', 'isRewardedSupported'], config: { platformLanguage: 'en', deviceType: 'desktop' } });
    } else if (data.type === 'player' && data.action === 'get_player') {
      send({ type: data.type, action: data.action, id: data.id, player: { isAuthorized: false } });
    } else if (data.type === 'storage' && data.action === 'get_data_from_storage') {
      send({ type: data.type, action: data.action, id: data.id, storage: { [data.options.key]: localStorage.getItem(storageKey(data.options.key)) } });
    } else if (data.type === 'storage' && data.action === 'set_data_to_storage') {
      localStorage.setItem(storageKey(data.options.key), data.options.value);
    } else if (data.type === 'storage' && data.action === 'delete_data_from_storage') {
      localStorage.removeItem(storageKey(data.options.key));
    } else if (data.type === 'advertisement' && ['reward', 'interstitial'].includes(data.action)) {
      activeAd = data.action;
      console.log('Local QA ad request: ' + activeAd);
      document.getElementById('publishing-qa-ad').textContent = 'SIMULATED ' + activeAd + ' OPEN';
      send({ type: 'advertisement', action: activeAd, payload: { status: 'start' } });
      send({ type: 'advertisement', action: activeAd, payload: { status: 'open' } });
    }
  });
  addEventListener('DOMContentLoaded', () => {
    const controls = document.createElement('div');
    controls.style = 'position:fixed;top:36px;left:0;z-index:100000;background:#e6f3ff;padding:8px';
    const label = document.createElement('span'); label.id = 'publishing-qa-ad'; label.textContent = 'LOCAL QA: simulated ads'; controls.append(label);
    for (const [name, status] of [['QA confirm reward', 'rewarded'], ['QA close ad', 'close'], ['QA fail ad', 'failed']]) {
      const button = document.createElement('button');button.textContent = name;
      button.onclick = () => {
        if (!activeAd) return;
        send({ type: 'advertisement', action: activeAd, payload: { status } });
        console.log('Local QA ad response: ' + activeAd + ' ' + status);
        if (status === 'close' || status === 'failed') { activeAd = null; label.textContent = 'LOCAL QA: no active ad'; }
      };
      controls.append(button);
    }
    const future = document.createElement('button');future.textContent = 'QA future save';
    future.onclick = async () => {
      const key = 'crunchie_munchie_v1';
      const previous = await window.bridge.storage.get([key], false);
      if (sessionStorage.getItem('publishing_qa_backup') === null) sessionStorage.setItem('publishing_qa_backup', JSON.stringify(previous[0] ?? null));
      await window.bridge.storage.set([key], [JSON.stringify({ version: 999, sound: 0.9, music: 0.9 })]);
      console.log('Local QA future-version save installed; reload to test preservation.');
    };
    controls.append(future);
    const restore = document.createElement('button');restore.textContent = 'QA restore save';
    restore.onclick = async () => {
      const backup = sessionStorage.getItem('publishing_qa_backup');
      if (backup === null) return;
      const previous = JSON.parse(backup);
      if (previous === null) await window.bridge.storage.delete(['crunchie_munchie_v1']);
      else await window.bridge.storage.set(['crunchie_munchie_v1'], [previous]);
      sessionStorage.removeItem('publishing_qa_backup');
      console.log('Local QA original save restored; reload to load it.');
    };
    controls.append(restore);
    document.body.append(controls);
  });
}
