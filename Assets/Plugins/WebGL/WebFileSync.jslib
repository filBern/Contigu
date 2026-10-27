// Browsers keep Unity's persistentDataPath in memory until it is copied into IndexedDB. Called after every save-file write or delete
// (Assets/Scripts/Presentation/WebFileSync.cs), so the run save, the stars and the collection survive closing or reloading the page.
mergeInto(LibraryManager.library, {
  // One sync at a time: a write that lands while a sync is running triggers a single follow-up sync afterwards.
  // (State lives on Module: a jslib function is copied into the build on its own, so it cannot see a variable declared beside it.)
  WebFileSync_Flush: function () {
    try {
      if (typeof FS !== 'undefined' && FS.syncfs) {
        var state = Module.webFileSyncState || (Module.webFileSyncState = { running: false, again: false });
        if (state.running) { state.again = true; return; }
        var run = function () {
          state.running = true;
          FS.syncfs(false, function (err) {
            if (err) { console.warn('WebFileSync: syncfs failed: ' + err); }
            state.running = false;
            if (state.again) { state.again = false; run(); }
          });
        };
        run();
      } else if (typeof _JS_FileSystem_Sync === 'function') {
        _JS_FileSystem_Sync();
      }
    } catch (e) {
      console.warn('WebFileSync: ' + e);
    }
  }
});
