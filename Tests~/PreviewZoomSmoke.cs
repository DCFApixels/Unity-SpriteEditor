var type = typeof(DCFApixels.SpriteEditor.TextureCompositorWindow).Assembly.GetType("DCFApixels.SpriteEditor.PreviewViewport", true);
var viewport = System.Activator.CreateInstance(type, true);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var bounds = new UnityEngine.Rect(0, 0, 808, 408);
var dimensions = new UnityEngine.Vector2(200, 100);
int checks = 0;
void Call(string method, params object[] args) => type.GetMethod(method, flags).Invoke(viewport, args);
UnityEngine.Rect Image(UnityEngine.Rect area, bool transforming = false) =>
    (UnityEngine.Rect)type.GetMethod("ImageRect", flags).Invoke(viewport, new object[] { area, dimensions, transforming });
UnityEngine.Vector2 UV(UnityEngine.Rect image, UnityEngine.Vector2 point) =>
    new UnityEngine.Vector2((point.x - image.x) / image.width, (point.y - image.y) / image.height);
void Check(bool value, string message)
{
    if (!value) throw new System.Exception(message);
    checks++;
}
bool Close(UnityEngine.Vector2 a, UnityEngine.Vector2 b) => (a - b).sqrMagnitude < 0.00001f;

var fitted = Image(bounds);
Check(fitted == new UnityEngine.Rect(4, 4, 800, 400), "Initial view fits the full image with padding");
var anchor = new UnityEngine.Vector2(204, 104);
var uv = UV(fitted, anchor);
Call("ZoomAt", bounds, dimensions, fitted, anchor, 8f);
var zoomed = Image(bounds);
Check(UnityEngine.Mathf.Approximately(zoomed.width, 1600), "Click doubles image scale");
Check(Close(UV(zoomed, anchor), uv), "Pixel under click stays under cursor");
Check(Image(bounds, true) == zoomed, "Manual zoom is stable when selecting Transform");
Call("ZoomAt", bounds, dimensions, zoomed, anchor, 4f);
Check(Close(Image(bounds).position, fitted.position), "Zoom-out reverses anchored zoom-in");
var selection = new UnityEngine.Rect(204, 104, 200, 100);
Call("Frame", bounds, dimensions, Image(bounds), selection);
var framed = Image(bounds);
Check(Close(UV(framed, bounds.center), UV(fitted, selection.center)), "Area is centered in the viewport");
Check(UnityEngine.Mathf.Approximately(framed.width / fitted.width, 4f), "Area uses the limiting axis to fit");
Call("Pan", bounds, dimensions, framed, new UnityEngine.Vector2(37, -19));
var panned = Image(bounds);
Check(Close(panned.position, framed.position + new UnityEngine.Vector2(37, -19)), "Panning follows pointer displacement");
var resizedBounds = new UnityEngine.Rect(0, 0, 1200, 700);
var resized = Image(resizedBounds);
Check(Close(resized.size, panned.size), "Window resize preserves manual pixel scale");
Check(Close(UV(resized, resizedBounds.center), UV(panned, bounds.center)), "Window resize preserves viewed image center");
Call("ZoomAt", bounds, dimensions, panned, bounds.center, 1f);
Check(Close(Image(bounds).size, dimensions), "100 percent uses one UI unit per source pixel");
Call("ZoomAt", bounds, dimensions, Image(bounds), bounds.center, 100000f);
Check(UnityEngine.Mathf.Approximately(Image(bounds).width, dimensions.x * 64f), "Maximum scale is bounded");
Call("ZoomAt", bounds, dimensions, Image(bounds), bounds.center, -1f);
Check(Image(bounds).width > 0f, "Minimum scale stays positive");
Call("Reset");
Check(Image(bounds) == fitted, "Fit restores initial framing");
Call("Frame", bounds, dimensions, fitted, new UnityEngine.Rect(20, 20, 0, 1));
Check(Image(bounds) == fitted, "Degenerate area does not change the view");
Check(Image(new UnityEngine.Rect(0, 0, 0, 0)).size == UnityEngine.Vector2.zero, "Zero-size viewport stays finite");
Check(Image(new UnityEngine.Rect(0, 0, float.NaN, float.NaN)) == UnityEngine.Rect.zero,
    "Unresolved layout does not write non-finite image geometry");
return "Preview zoom checks passed: " + checks + ". No assets or GPU resources created.";
