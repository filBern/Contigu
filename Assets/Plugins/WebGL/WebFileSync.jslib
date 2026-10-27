// Browsers keep Unity's persistentDataPath in memory until it is copied into IndexedDB. Called after every save-file write or delete
// (Assets/Scripts/Presentation/WebFileSync.cs), so the run save, the stars and the collection survive closing or reloading the page.
var WebFileSyncState = { running: false, again: false };

mergeInto(LibraryManager.library, {
  // One sync at a time: a write that lands while a sync is running triggers a single follow-up sync afterwards.
  WebFileSync_Flush: function () {
    try {
      if (typeof FS !== 'undefined' && FS.syncfs) {
        if (WebFileSyncState.running) { WebFileSyncState.again = true; return; }
        WebFileSyncState.running = true;
        FS.syncfs(false, function (err) {
          if (err) { console.warn('WebFileSync: syncfs failed: ' + err); }
          WebFileSyncState.running = false;
          if (WebFileSyncState.again) { WebFileSyncState.again = false; _WebFileSync_Flush(); }
        });
      } else if (typeof _JS_FileSystem_Sync === 'function') {
        _JS_FileSystem_Sync();
      }
    } catch (e) {
      console.warn('WebFileSync: ' + e);
    }
  }
});
