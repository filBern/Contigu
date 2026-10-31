// QA hook (see Assets/Scripts/Presentation/QaBridge.cs): only active when the page URL has ?qa=1.
mergeInto(LibraryManager.library, {
  QaBridge_Enabled: function () {
    var on = typeof location !== 'undefined' && /[?&]qa=1/.test(location.search);
    if (on && !window.__qa) {
      window.__qa = {
        stateJson: null,
        stateTick: 0,
        queue: [],
        results: {},
        seq: 0,
        send: function (command) {
          var id = ++this.seq;
          command.id = id;
          this.queue.push(JSON.stringify(command));
          return id;
        }
      };
    }
    return on ? 1 : 0;
  },
  QaBridge_Publish: function (ptr) {
    if (window.__qa) {
      window.__qa.stateJson = UTF8ToString(ptr);
      window.__qa.stateTick++;
    }
  },
  QaBridge_Result: function (ptr) {
    if (window.__qa) {
      var r = JSON.parse(UTF8ToString(ptr));
      window.__qa.results[r.id] = r.result;
    }
  },
  QaBridge_Poll: function () {
    if (!window.__qa || window.__qa.queue.length === 0) { return 0; }
    var s = window.__qa.queue.shift();
    var size = lengthBytesUTF8(s) + 1;
    var buffer = _malloc(size);
    stringToUTF8(s, buffer, size);
    return buffer;
  }
});
