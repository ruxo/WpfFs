namespace RZ.Wpf

module ResourceManager =
  open System
  open System.Collections
  open System.Reflection
  open System.IO
  open FSharp.Core.Fluent
  open RZ.Foundation

  let private getCandidateAssemblies() =
    let primary = [ Assembly.GetCallingAssembly(); Assembly.GetEntryAssembly() ]
    let isPrimary = (flip List.contains) primary
    let isSystem (asm:Assembly) = asm.FullName.StartsWith("SYSTEM", StringComparison.OrdinalIgnoreCase)
    let rest =
       AppDomain
        .CurrentDomain.GetAssemblies()
        .filter(fun a -> not (isPrimary a || isSystem a))
    primary |> Seq.append rest

  let private getResourceLookup0 (asm: Assembly) =
      let makeXamlString (stream: Stream) =
          use reader = new StreamReader(stream)
          in  reader.ReadToEnd()

      let readResourceStream (stream: Stream) =
          use reader = new System.Resources.ResourceReader(stream)
          reader
          |> Seq.cast :> seq<DictionaryEntry>
          |> Seq.map (fun entry -> entry.Key :?> string, makeXamlString (entry.Value :?> Stream))
          |> Map.ofSeq

      let getWpfResourceName (asm: Assembly) = sprintf "%s.g.resources" <| asm.GetName().Name

      let resourceName = getWpfResourceName asm
      in  match asm.GetManifestResourceStream resourceName with
          | null -> Map.empty
          | stream -> readResourceStream stream

  let getResourceLookup: Assembly -> Map<string,string> = Utils.memoize getResourceLookup0

  let findWpfResource (name: string) = getResourceLookup >> Map.tryFind (name.ToLower())

