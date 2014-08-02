namespace RZ.Wpf

open System
open System.IO
open System.Xml.Linq
open System.Xaml
open System.Windows.Markup
open System.Linq

open System.ComponentModel

type ViewModelBase() =
    let propertyChangedEvent = new DelegateEvent<PropertyChangedEventHandler>()
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish

    member x.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| x; new PropertyChangedEventArgs(propertyName) |])
        
    member self.setValue(field : byref<_>, value, [<ParamArray>] fieldNames: string[]) =
            if field = value then ()
            else field <- value
                 Array.iter self.OnPropertyChanged fieldNames

type IXamlConnector =
    abstract Ready : unit -> unit

type XamlLoader() =
    static let locateType (typeName) =
        AppDomain.CurrentDomain.GetAssemblies().AsParallel()
        |> Seq.map (fun asm -> asm.GetType(typeName))
        |> Seq.filter (fun t -> t <> null)
        |> Seq.head

    static let getRootObject (xaml) =
        let xml = XElement.Parse(xaml)
        let clsName = XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml")
        let attr = xml.Attribute(clsName)
        if attr = null then
            None
        else
            match locateType(attr.Value) with
            | null     -> failwithf "Type %s not found." attr.Value
            | rootType -> Some <| Activator.CreateInstance(rootType)

    static member LoadWpf xamlFilename =
        let xamlContent = File.ReadAllText(xamlFilename)
        let rootObject = getRootObject(xamlContent)

        let stream = new StringReader(xamlContent)
        use reader = new XamlXmlReader(stream, XamlReader.GetWpfSchemaContext())

        let writerSettings = XamlObjectWriterSettings()
        let connector = match rootObject with
                        | Some root -> writerSettings.RootObjectInstance <- root
                                       match root with
                                       | :? IXamlConnector as c -> Some c
                                       | _ -> None
                        | None      -> None

        use writer = new XamlObjectWriter(reader.SchemaContext, writerSettings)

        while reader.Read() do
            writer.WriteNode(reader)

        let result = writer.Result
        match connector with
        | None -> ()
        | Some c -> c.Ready()
        result

