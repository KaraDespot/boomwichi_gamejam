mergeInto(LibraryManager.library, {
  PublishingWatchVisibility: function () {
    if (window.__publishingVisibilityListener) return;
    window.__publishingVisibilityListener = function () {
      SendMessage('PublishingServices', 'OnBrowserVisibility', document.hidden ? 'hidden' : 'visible');
    };
    document.addEventListener('visibilitychange', window.__publishingVisibilityListener);
    window.__publishingVisibilityListener();
  }
});
