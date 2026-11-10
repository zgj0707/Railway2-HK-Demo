mergeInto(LibraryManager.library, {
  VibrateJS: function (ms) {
    if (navigator.vibrate) navigator.vibrate(ms);
  }
});
