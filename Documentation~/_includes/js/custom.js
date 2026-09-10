// Just the Docs 0.12.0 assumes focusout.relatedTarget is an Element.
// Leaving the page or clicking non-focusable content can legitimately yield null.
jtd.onReady(function () {
  function closeSearchWithoutFocusTarget(event) {
    if (event.relatedTarget) return;
    document.documentElement.classList.remove('search-active');
    event.stopImmediatePropagation();
  }
  ['search-input', 'search-results'].forEach(function (id) {
    var element = document.getElementById(id);
    if (element) element.addEventListener('focusout', closeSearchWithoutFocusTarget, true);
  });
});
