// Keep Cyrillic tokens as well as Latin ones; use prefix search without language-specific stemming.
if (!this.spriteEditorUnicodeSearch) {
  var trimUnicode = function (token) {
    return token.update(function (value) {
      return value.replace(/^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$/gu, '');
    });
  };
  lunr.Pipeline.registerFunction(trimUnicode, 'spriteEditorUnicodeTrim');
  this.pipeline.reset();
  this.searchPipeline.reset();
  this.pipeline.add(trimUnicode);
  this.searchPipeline.add(trimUnicode);
  this.spriteEditorUnicodeSearch = true;
}
