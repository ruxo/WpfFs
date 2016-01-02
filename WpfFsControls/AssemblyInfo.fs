namespace System

open System.Reflection

[<assembly: AssemblyTitle("FSharp XAML processor library")>]
[<assembly: AssemblyProduct("RZ.Wpf")>]
[<assembly: AssemblyDescription("Alternative F# XAML processor and utilties")>]
[<assembly: AssemblyVersion("1.0.1")>]
[<assembly: AssemblyFileVersion("1.0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0.1"
