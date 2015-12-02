namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("MBrace.ProgFSharp")>]
[<assembly: AssemblyProductAttribute("MBrace.ProgFSharp")>]
[<assembly: AssemblyDescriptionAttribute("MBrace tutorial for ProgFSharp 2015")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
