# DebugLib
C#用オブジェクトダンプライブラリです。
インスタンスの持つプロパティや、コレクションの各要素を再帰的に列挙します。

##Sample
#####Person
```cs
namespace Sample
{
  class Person
  {
      public int Age { get; set; }
      public string Name { get; set; }
  }
}
```

```cs
using DebugLib;

var person = new Person() { Age = 20, Name = "Example" };
person.Dump();
```

```
Sample.Person (Sample.Person)
{
    Age = 20 (System.Int32)
    Name = "Example" (System.String)
}
```

#####System.Uri
```cs
var uri = new Uri("https://www.google.co.jp/webhp?hl=ja");
uri.Dump();
```

```
https://www.google.co.jp/webhp?hl=ja (System.Uri)
{
    AbsolutePath = "/webhp" (System.String)
    AbsoluteUri = "https://www.google.co.jp/webhp?hl=ja" (System.String)<LoopReference>
    LocalPath = "/webhp" (System.String)
    Authority = "www.google.co.jp" (System.String)
    HostNameType = Dns (System.UriHostNameType)
    IsDefaultPort = True (System.Boolean)
    IsFile = False (System.Boolean)
    IsLoopback = False (System.Boolean)
    PathAndQuery = "/webhp?hl=ja" (System.String)
    Segments = System.String[] (System.String[])
    {
        [0] "/" (System.String)
        [1] "webhp" (System.String)
    }
    IsUnc = False (System.Boolean)
    Host = "www.google.co.jp" (System.String)
    Port = 443 (System.Int32)
    Query = "?hl=ja" (System.String)
    Fragment = "" (System.String)
    Scheme = "https" (System.String)
    OriginalString = "https://www.google.co.jp/webhp?hl=ja" (System.String)<LoopReference>
    DnsSafeHost = "www.google.co.jp" (System.String)
    IdnHost = "www.google.co.jp" (System.String)
    IsAbsoluteUri = True (System.Boolean)
    UserEscaped = False (System.Boolean)
    UserInfo = "" (System.String)
}
```

##Settings
インデントサイズや型の表示/非表示、列挙するプロパティのアクセスフラグなどを変更できます。
```cs
Dumper.IndentSize = 2;
Dumper.ShowPropertyType = false;
person.Dump();
```

```
Sample.Person
{
  Age = 20
  Name = "Example"
}
```
