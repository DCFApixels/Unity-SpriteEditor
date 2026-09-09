// Opt-in after manual compilation. Pure selection data; no assets, windows or Undo changes.
var assembly = typeof(DCFApixels.SpriteEditor.TextureCompositor).Assembly;
var type = assembly.GetType("DCFApixels.SpriteEditor.CanvasSelection", true);
var combine = assembly.GetType("DCFApixels.SpriteEditor.SelectionCombine", true);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var selection = System.Activator.CreateInstance(type, flags, null, new object[] { 8, 8 }, null);
int checks = 0;
object Mode(string name) => System.Enum.Parse(combine, name);
object Call(string name, params object[] args) => type.GetMethod(name, flags).Invoke(selection, args);
byte[] Mask() => (byte[])type.GetProperty("Coverage", flags).GetValue(selection);
int Count() { int n = 0; foreach (byte b in Mask()) if (b != 0) n++; return n; }
void Check(bool value, string message) { if (!value) throw new System.Exception(message); checks++; }
void Rect(float x0, float y0, float x1, float y1, string mode = "Replace", bool wrap = false) =>
    Call("Rectangle", new UnityEngine.Vector2(x0, y0), new UnityEngine.Vector2(x1, y1), Mode(mode), wrap);
float Sample(float x, float y, bool wrap = false) => (float)Call("Sample", new UnityEngine.Vector2(x, y), wrap);
Check(Sample(.5f, .5f) == 1f, "Inactive selection permits painting");
Rect(1, 1, 5, 5);
Check(Count() == 16 && Sample(.2f, .2f) == 1 && Sample(.9f, .9f) == 0, "Rectangle uses canvas pixels");
Check(Sample(-.1f, .2f) == 0 && Sample(float.NaN, .2f) == 0, "Outside and invalid sample coordinates are rejected");
Rect(4, 4, 7, 7, "Add"); Check(Count() == 24, "Add unions coverage");
Rect(0, 0, 4, 8, "Intersect"); Check(Count() == 12, "Intersect preserves overlap");
Rect(0, 0, 8, 8, "Subtract"); Check(Count() == 0 && Sample(.5f, .5f) == 0, "Empty active selection blocks painting");
Call("Clear"); Check(Sample(.5f, .5f) == 1, "Deselect removes painting restriction");
Call("All"); Check(Count() == 64, "Select All");
Call("Invert"); Check(Count() == 0, "Invert full selection");
Rect(-5, -5, 2, 3); Check(Count() == 6, "Ordinary selection clamps to canvas");
Rect(7, 2, 9, 4, "Replace", true);
Check(Count() == 4 && Mask()[2 * 8 + 7] == 255 && Mask()[2 * 8] == 255, "Wrapped rectangle crosses the seam");
Rect(-1, -1, 1, 1, "Replace", true);
Check(Count() == 4 && Mask()[63] == 255 && Mask()[0] == 255, "Negative repeat coordinates wrap");
Rect(-100, -100, 100, 100, "Replace", true); Check(Count() == 64, "Large wrapped rectangle selects the whole canvas");
var points = new System.Collections.Generic.List<UnityEngine.Vector2> {
    new UnityEngine.Vector2(1,1), new UnityEngine.Vector2(5,1), new UnityEngine.Vector2(1,5) };
Call("Polygon", points, Mode("Replace"), false); Check(Count() == 6, "Triangle uses pixel-center coverage");
points = new System.Collections.Generic.List<UnityEngine.Vector2> {
    new UnityEngine.Vector2(0,0), new UnityEngine.Vector2(4,0), new UnityEngine.Vector2(4,2),
    new UnityEngine.Vector2(2,2), new UnityEngine.Vector2(2,4), new UnityEngine.Vector2(0,4) };
Call("Polygon", points, Mode("Replace"), false); Check(Count() == 12, "Concave polygon");
points = new System.Collections.Generic.List<UnityEngine.Vector2> {
    new UnityEngine.Vector2(7,2), new UnityEngine.Vector2(9,2), new UnityEngine.Vector2(9,4), new UnityEngine.Vector2(7,4) };
Call("Polygon", points, Mode("Replace"), true); Check(Count() == 4, "Polygon crosses repeated canvas seam");
var soft = new byte[64]; soft[0] = 128;
Call("Set", soft, Mode("Replace"));
Check(System.Math.Abs(Sample(.01f,.01f) - 128f / 255f) < .001f, "Alpha-derived selection retains soft coverage");
var other = new byte[64]; other[0] = 128;
Call("Set", other, Mode("Intersect")); Check(Mask()[0] == 64, "Soft intersection multiplies coverage");
other = new byte[64]; other[0] = 128;
Call("Set", other, Mode("Subtract")); Check(Mask()[0] == 32, "Soft subtraction");
return "Canvas selection checks passed: " + checks;
