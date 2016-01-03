namespace RZ.Wpf

open System
open System.Collections.Specialized
open System.IO
open System.Xml.Linq
open System.Xaml
open System.Windows
open System.Windows.Markup
open System.Linq
open FSharp.Core.Fluent
open RZ.Foundation

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

[<RequireQualifiedAccess>]
module XamlLoader =
    let private locateType (typeName) =
        AppDomain
          .CurrentDomain.GetAssemblies()
          .AsParallel()
          .choose(fun asm -> asm.GetType(typeName) |> Option.ofObj)
          .tryHead()

    let private getRootObject<'a> (xaml) =
        let xml = XElement.Parse(xaml)
        let clsName = XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml")
        xml.Attribute(clsName) 
          |> Option.ofObj
          |> Option.map(fun attr -> 
                match locateType(attr.Value) with
                | None          -> failwithf "Type %s not found." attr.Value
                | Some rootType -> Activator.CreateInstance(rootType) :?> 'a
             )

    let private loadWpfInternal rootObject (xamlContent: string)  :obj =
        let stream = new StringReader(xamlContent)
        use reader = new XamlXmlReader(stream, XamlReader.GetWpfSchemaContext())

        let writerSettings = XamlObjectWriterSettings()
        match rootObject with
        | Some root -> writerSettings.RootObjectInstance <- root
        | None -> ()

        use writer = new XamlObjectWriter(reader.SchemaContext, writerSettings)

        while reader.Read() do
            writer.WriteNode(reader)

        writer.Result

    let private readTextFile f =
      if File.Exists f then
        try
          Some (File.ReadAllText f)
        with
        | _ -> None
      else
        None

    let createFromXamlObj: obj -> string -> obj = Some >> loadWpfInternal

    // assume that the class initializes *code behind* by itself.
    let createFromXaml xaml = getRootObject(xaml).getOrElse(fun _ -> loadWpfInternal None xaml)

    let createFromResource: string -> Reflection.Assembly -> obj option = ResourceManager.findWpfResource >>.>> (Option.map createFromXaml)

    let createFromFile = readTextFile >> Option.map createFromXaml

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