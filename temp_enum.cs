using System;
using System.Reflection;
class P {
  static void Main() {
    var asm = Assembly.LoadFrom(@"C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Data\Managed\UnityEditor.dll");
    var t = asm.GetType("UnityEditor.AndroidApplicationEntry");
    Console.WriteLine(t == null ? "null" : t.FullName);
    if (t != null && t.IsEnum) {
      foreach (var name in Enum.GetNames(t)) {
        var val = Convert.ToInt32(Enum.Parse(t, name));
        Console.WriteLine(name + "=" + val);
      }
    }
  }
}
