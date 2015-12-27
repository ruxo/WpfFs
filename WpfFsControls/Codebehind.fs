module RZ.Wpf.CodeBehind

open FSharp.Core.Fluent
open RZ.Foundation

let private reportError (asm: System.Reflection.Assembly) resourceName: unit = 
  let availableResources =
    ResourceManager.getResourceLookup(asm)
    |> Map.toSeq
    |> Seq.map fst

  let sb = System.Text.StringBuilder()
  sb.AppendLine(sprintf "Cannot find resource %s in the calling assembly %A" resourceName asm.FullName)
    .AppendLine("\tAvailable resources:") |> ignore

  availableResources.iter(sprintf "\t\t%s" >> sb.AppendLine >> ignore)

  let txt = sb.ToString()
  System.Diagnostics.Debug.Print txt
  failwith txt

type System.Windows.Media.Visual with
  member rootObj.InitializeCodeBehind (resourceName: string) =
    let asm = rootObj.GetType().Assembly
    asm |> ResourceManager.findWpfResource resourceName
        |> Option.cata 
          (fun() -> reportError asm resourceName)
          (XamlLoader.loadXmlFromString (Some <| box rootObj) >> ignore)
