// Browsers keep Unity's persistentDataPath in memory until it is copied into IndexedDB. Called after every save-file write or delete
// (Assets/Scripts/Presentation/WebFileSync.cs), so the run save, the stars and the collection survive closing or reloading the page.
mergeInto(LibraryManager.library, {
  WebFileSync_Flush: function () {
    try {
      if (typeof FS !== 'undefined' && FS.syncfs) {
        FS.syncfs(false, function (err) { if (err) { console.warn('WebFileSync: syncfs failed: ' + err); } });
      } else if (typeof _JS_FileSystem_Sync === 'function') {
        _JS_FileSystem_Sync();
      }
    } catch (e) {
      console.warn('WebFileSync: ' + e);
    }
  }
});
