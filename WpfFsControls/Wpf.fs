namespace RZ.Wpf
// v20151206

open System
open System.Collections.Specialized
open System.IO
open System.Xml.Linq
open System.Xaml
open System.Windows
open System.Windows.Markup
open System.Linq
open RZ.Foundation

module Utils = 
    let memoize (f: 'a -> 'b) =
        let dict = System.Collections.Concurrent.ConcurrentDictionary<'a,'b>()

        let memoizedFunc input =
            match dict.TryGetValue input with
            | true, x -> x
            | false, _ ->
                let answer = f input
                dict.TryAdd(input, answer) |> ignore
                answer
        memoizedFunc

(******************* Resources ************************)
module ResourceManager =
    open System.Reflection

    let private getResourceLookup0 (asm: Assembly) =
        let makeXamlString (stream: Stream) =
            use reader = new StreamReader(stream)
            in  reader.ReadToEnd()

        let readResourceStream (stream: Stream) =
            use reader = new System.Resources.ResourceReader(stream)
            reader
            |> Seq.cast :> seq<Collections.DictionaryEntry>
            |> Seq.map (fun entry -> entry.Key :?> string, makeXamlString (entry.Value :?> Stream))
            |> Map.ofSeq

        let getWpfResourceName (asm: Assembly) = sprintf "%s.g.resources" <| asm.GetName().Name

        let resourceName = getWpfResourceName asm
        in  match asm.GetManifestResourceStream resourceName with
            | null -> Map.empty
            | stream -> readResourceStream stream

    let private getResourceLookup = Utils.memoize getResourceLookup0

    let findWpfResource (name: string) = getResourceLookup >> Map.tryFind (name.ToLower())

(******************* Timer ************************)
module DispatcherTimer =
    open System.Windows.Threading

    let create period = DispatcherTimer(Interval=period)
    let start timer = (timer:DispatcherTimer).Start()
    let startTimer period handler =
        let t = create period
        let r = t.Tick |> Observable.subscribe handler
        start t
        { new IDisposable with member me.Dispose() = r.Dispose(); t.Stop() }

// ------------------------- View Model ------------------------- //
type UIEvent = string * obj

type ViewModelBase() =
    let propertyChangedEvent = new DelegateEvent<ComponentModel.PropertyChangedEventHandler>()
    let runEvent = Event<UIEvent>()

    interface ComponentModel.INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChangedEvent.Publish

    [<CLIEvent>]
    member x.UIEvent = runEvent.Publish

    member x.OnUIEvent data = runEvent.Trigger data
    member x.OnPropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| x; new ComponentModel.PropertyChangedEventArgs(propertyName) |])
        
    member self.setValue(field : byref<_>, value, [<ParamArray>] fieldNames: string[]) =
            if field = value then ()
            else field <- value
                 Array.iter self.OnPropertyChanged fieldNames

    member x.SubscribeUIEvent(evt, f) = x.UIEvent |> Observable.filter (fun (n, _) -> n = evt) |> Observable.subscribe (fun (_, data) -> f data) |> ignore

// ------------- WPF Observable Collection -------------------- //
type IObservableTracker<'Args> =
    abstract member Disposed: ObserverTracker<'Args> -> unit

and ObserverTracker<'Args>(observer: IObserver<'Args>, tracker: IObservableTracker<'Args>) =
    let mutable disposed = false

    member this.Callback(sender: obj, args: 'Args) = observer.OnNext(args)

    interface IDisposable with
        member this.Dispose() = if not disposed then
                                    disposed <- true
                                    observer.OnCompleted()
                                    tracker.Disposed(this)

type TrackableEvent<'Delegate, 'Args when 'Delegate: delegate<'Args, unit> and 'Delegate :> System.Delegate>() as this =
    static let dummy(s:obj, a:'Args) = ()

    [<DefaultValue>]
    val mutable del : Delegate
    do this.del <- Delegate.CreateDelegate(typeof<'Delegate>, typeof<TrackableEvent<'Delegate,'Args>>, "dummy")

    member this.Publish = (this :> IEvent<'Delegate,'Args>)

    abstract Trigger: obj * 'Args -> unit
    default this.Trigger(sender: obj, args: 'Args) =
        let argPack = [|sender; args :> obj|]
        this.del.GetInvocationList()
        |> Array.iter (fun d -> ignore <| d.DynamicInvoke(argPack))

    interface IObservableTracker<'Args> with
        member this.Disposed(tracked) =
            let d = Delegate.CreateDelegate(typeof<'Delegate>, tracked, "Callback")
            this.del <- Delegate.Remove(this.del, d)

    interface IEvent<'Delegate,'Args> with
        member this.Subscribe(observer) =
            let tracker = new ObserverTracker<'Args>(observer, this)
            let d = Delegate.CreateDelegate(typeof<'Delegate>, tracker, "Callback")
            this.del <- Delegate.Combine(this.del, d)
            tracker :> IDisposable

        member this.AddHandler(d: 'Delegate) = this.del <- Delegate.Combine(this.del, d)
        member this.RemoveHandler(d: 'Delegate) = this.del <- Delegate.Remove(this.del, d)

type WpfAwareEvent<'Delegate, 'Args when 'Delegate: delegate<'Args, unit> and 'Delegate :> System.Delegate>() =
    inherit TrackableEvent<'Delegate, 'Args>()

    override this.Trigger(sender, args) =
        let argPack = [|sender; args :> obj|]
        this.del.GetInvocationList()
        |> Array.iter (fun d ->
            match d.Target with
            | :? Threading.DispatcherObject as wpfObj when not(wpfObj.CheckAccess()) ->
                ignore <| wpfObj.Dispatcher.Invoke(d, argPack)
            | _ ->
                ignore <| d.DynamicInvoke(argPack))


type WpfObservableCollection<'T>() as this =
    inherit Collections.ObjectModel.Collection<'T>()

    [<Literal>] static let CountString = "Count"
    [<Literal>] static let IndexerName = "Index"

    let collectionChanged = WpfAwareEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>()

    let propertyChangedEvent = new DelegateEvent<ComponentModel.PropertyChangedEventHandler>()

    let onPropertyChanged propertyName = 
        propertyChangedEvent.Trigger([| this; ComponentModel.PropertyChangedEventArgs(propertyName) |])
        
    let setValue(field : byref<_>, value, [<ParamArray>] fieldNames: string[]) =
            if field = value then ()
            else field <- value
                 Array.iter onPropertyChanged fieldNames

    override this.ClearItems() =
        let oldItems = this.Items
        base.ClearItems()
        onPropertyChanged(CountString)
        onPropertyChanged(IndexerName)
        collectionChanged.Trigger(this, NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, oldItems, 0))

    override this.InsertItem(index, item) =
        base.InsertItem(index, item)
        onPropertyChanged(CountString)
        onPropertyChanged(IndexerName)
        collectionChanged.Trigger(this, NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index))

    override this.RemoveItem(index) =
        let removedItem = this.[index]
        base.RemoveItem(index)
        onPropertyChanged(CountString)
        onPropertyChanged(IndexerName)
        collectionChanged.Trigger(this, NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index))

    override this.SetItem(index, item) = 
        let oldItem = this.[index]
        base.SetItem(index, item)
        onPropertyChanged(IndexerName)
        collectionChanged.Trigger(this, NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index))

    interface ComponentModel.INotifyPropertyChanged with
        [<CLIEvent>]
        member me.PropertyChanged = propertyChangedEvent.Publish

    interface INotifyCollectionChanged with
        [<CLIEvent>]
        member this.CollectionChanged = collectionChanged.Publish


// ------------------------- WPF Loader ------------------------- //

/// <summary>
/// Provide ready signal when XAML is completely read.
/// </summary>
type IXamlConnector =
    abstract Ready : unit -> unit

[<RequireQualifiedAccess>]
module XamlLoader =
    let locateType (typeName) =
        AppDomain.CurrentDomain.GetAssemblies().AsParallel()
        |> Seq.map (fun asm -> asm.GetType(typeName))
        |> Seq.filter (fun t -> t <> null)
        |> Seq.head

    let private getRootObject<'a> (xaml) =
        let xml = XElement.Parse(xaml)
        let clsName = XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml")
        let attr = xml.Attribute(clsName)
        if attr = null then
            None
        else
            match locateType(attr.Value) with
            | null     -> failwithf "Type %s not found." attr.Value
            | rootType -> Some (Activator.CreateInstance(rootType) :?> 'a)

    let private loadStreamInternal(reader: XamlXmlReader, rootObject) =
        let writerSettings = XamlObjectWriterSettings()
        let connector = match rootObject with
                        | Some root -> writerSettings.RootObjectInstance <- root
                                       match (root :> obj) with
                                       | :? IXamlConnector as c -> Some c
                                       | _ -> None
                        | None      -> None

        use writer = new XamlObjectWriter(reader.SchemaContext, writerSettings)

        while reader.Read() do
            writer.WriteNode(reader)

        let result = writer.Result
        match connector with
        | None -> ()
        | Some c ->
            // prevent access to the interface's member before the instance is fully initialized.
            let action = Action(fun() -> c.Ready())
            ignore <| Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(action)
        result

    let private LoadWpfInternal xamlContent rootObject =
        let stream = new StringReader(xamlContent)
        use reader = new XamlXmlReader(stream, XamlReader.GetWpfSchemaContext())
        loadStreamInternal(reader, rootObject)

    let loadWpfFromString xamlBody = LoadWpfInternal xamlBody (getRootObject(xamlBody))

    let private readTextFile f =
      if File.Exists f then
        try
          Some (File.ReadAllText f)
        with
        | _ -> None
      else
        None

    let LoadWpfFromFile xamlFilename = readTextFile xamlFilename |> Option.map loadWpfFromString

    let LoadWpf xamlFilename rootObject = 
        let xamlContent = File.ReadAllText(xamlFilename)

        LoadWpfInternal xamlContent rootObject

    let private showAllResources (asm: Reflection.Assembly) = String.Join("\n\t", asm.GetManifestResourceNames())

    let loadXmlFromString (rootObj: 'a option) xaml =
      let text = new StringReader(xaml)
      let finalRoot = rootObj |> Option.orTry (fun() -> getRootObject xaml)
      use reader = new XamlXmlReader(text, XamlReader.GetWpfSchemaContext())
      loadStreamInternal(reader, finalRoot)

    let loadFromResource0 resourceName (rootObj: 'a option) asm =
        ResourceManager.findWpfResource resourceName asm
          |> Option.map (loadXmlFromString rootObj)
        
    let loadFromResource resourceName (rootObj: 'a option) =
        let asm = System.Reflection.Assembly.GetCallingAssembly()
        in  loadFromResource0 resourceName rootObj asm

// ------------------------- DataContext method binder ------------------------- //
type DCMethodExtension(methodName: string) =
    inherit MarkupExtension()

    let assignContext (ctx: obj) evtInfo = 
        match ctx with
        | null -> null
        | ctx -> Delegate.CreateDelegate(evtInfo, ctx, methodName)

    override me.ProvideValue(serviceProvider) =
        let targetService = serviceProvider.GetService(typeof<IProvideValueTarget>) :?> IProvideValueTarget
        let fe = targetService.TargetObject :?> FrameworkElement
        let event = targetService.TargetProperty :?> Reflection.EventInfo
        let evtType = event.EventHandlerType

        let lastAssignment = ref <| assignContext (fe.DataContext) evtType

        ignore <| fe.DataContextChanged.Subscribe(
            fun args -> match !lastAssignment with
                        | null -> ()
                        | handler -> event.RemoveEventHandler(fe, handler)

                        lastAssignment := assignContext (args.NewValue) evtType
                        match !lastAssignment with
                        | null -> ()
                        | handler -> event.AddEventHandler(fe, handler))
        
        !lastAssignment :> obj

// -------------- Utilities -----------------
module RZWpf =
    let DesignerMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode