namespace RZ.Wpf.Markups

open System.Reflection
open System.Windows
open System.Windows.Markup
open System.Windows.Input
open System
open FSharp.Core.Fluent
open RZ.Foundation

type private RoutedEventForwarder<'a when 'a :> RoutedEventArgs>
      (cmdGetter: obj -> ICommand option, commandParameter: string, converter: string option) =

  static let getFrameworkElementDC = tryCast<FrameworkElement> >> Option.bind (fun fe -> fe.DataContext |> Option.ofObj)
  static let getFrameworkContentElementDC = tryCast<FrameworkContentElement> >> Option.bind (fun fe -> fe.DataContext |> Option.ofObj)
  static let getDataContext (sender: obj) =
    sender |> getFrameworkElementDC
           |> Option.orTry (fun() -> sender |> getFrameworkContentElementDC)

  static let getPublicMethod name o = o.GetType().GetMethod(name) |> Option.ofObj
  static let verifyConverterSignature (m:MethodInfo) =
    let p = m.GetParameters()
    p.Length = 2 && p.[0].ParameterType = typeof<string> && p.[1].ParameterType.IsAssignableFrom(typeof<'a>)

  static let getConverter converterName dc =
    getPublicMethod converterName dc 
    |> Option.filter verifyConverterSignature
    |> Option.map (fun m -> (fun (param: string) (e: obj) -> m.Invoke(dc, [| box param; e |])))

  let raiseRoutedCommand p (target: IInputElement) (cmd: RoutedUICommand) =
    if cmd.CanExecute(p, target) then
      cmd.Execute(p, target)
      true
    else
      false

  let raiseNormalCommand p (cmd: ICommand) =
    if cmd.CanExecute(p) then
      cmd.Execute(p)
      true
    else
      false

  let raiseCommand param cmd sender =
    match sender.tryCast<IInputElement>(), cmd.tryCast<RoutedUICommand>() with
    | Some iinput, Some c -> raiseRoutedCommand param iinput c
    | _, _ -> raiseNormalCommand param cmd

  member __.Invoke(sender:obj, e: 'a) =
    let dc = getDataContext sender
    let param =
      Some getConverter
       |> Option.ap converter
       |> Option.ap dc
       |> Option.join
       |> Option.map (fun f -> f commandParameter e)
       |> Option.getOrElse (fun() -> box commandParameter)

    if not <| param.Equals(DependencyProperty.UnsetValue) then
      e.Handled <-
        (Some <| raiseCommand param)
          |> Option.ap (dc.bind(cmdGetter))
          |> Option.call sender
          |> Option.getOrDefault e.Handled


type BindCommandType =
| Standard = 0
| ContextCommand = 1


module private RoutedEventForwarder =
  let routedEventForwarder = typedefof<RoutedEventForwarder<_>>

  let of' = RZ.Wpf.Utils.memoize (fun routedEventType -> routedEventForwarder.MakeGenericType([| routedEventType |]))


[<MarkupExtensionReturnType(typeof<MethodInfo>)>]
type BindCommand(cmd: string) =
  inherit MarkupExtension()

  static let getDelegateTypeFromEvent(sp: IServiceProvider) =
    sp.GetService(typeof<IProvideValueTarget>)
      .cast<IProvideValueTarget>()
    |> Option.ofObj
    |> Option.bind (fun o -> o.TargetProperty.tryCast<EventInfo>())
    |> Option.map (fun o -> o.EventHandlerType)

  static let verifyDelegateSignature(m:MethodInfo) =
      let p = m.GetParameters()
      m.ReturnType = typeof<System.Void>
      && p.Length = 2 
      && p.[0].ParameterType = typeof<obj> 
      && typeof<RoutedEventArgs>.IsAssignableFrom(p.[1].ParameterType)

  static let getArgType(delegateType: Type) =
    delegateType.GetMethod("Invoke")
      |> Option.ofObj
      |> Option.filter(verifyDelegateSignature)
      |> Option.map(fun m -> m.GetParameters().[1].ParameterType)

  static let createWrapper (cmd: obj -> ICommand option, cmdParam: string, converter: string option) delType argType =
    let ft = argType |> RoutedEventForwarder.of'
    let wrapper = Activator.CreateInstance(ft, [| box cmd; box cmdParam; box converter |])
    let m = ft.GetMethod("Invoke", BindingFlags.NonPublic|||BindingFlags.Instance)
    Delegate.CreateDelegate(delType, wrapper, m)

  static let createWrapper1 cmd delType = delType |> getArgType |> Option.map (createWrapper cmd delType)
  static let standardConverter = CommandConverter()

  let mutable command = cmd
  let mutable commandParameter = ""
  let mutable converter = None
  let mutable commandType = BindCommandType.Standard

  let getCommandResolver() =
    match commandType with
    | BindCommandType.Standard ->
      try
        constant (standardConverter.ConvertFromString(command) |> tryCast<ICommand>)
      with
      | :? NotSupportedException -> constant None
    | BindCommandType.ContextCommand ->
      fun dc ->
        dc.GetType().GetProperty(command)
          |> Option.ofObj
          |> Option.bind (fun p -> p.GetMethod |> Option.ofObj)
          |> Option.bind (fun pGet -> pGet.Invoke(dc, null) |> tryCast<ICommand>)
    | _ -> constant None

  new() = BindCommand(null)

  [<ConstructorArgument("command")>]
  member __.Command with get() = command and set v = command <- v
  member __.CommandParameter with get() = commandParameter and set v = commandParameter <- v
  member __.EventArgumentConverter with get() = converter.getOrElse(constant "") and set v = converter <- if String.IsNullOrWhiteSpace(v) then None else Some v
  member __.CommandType with get() = commandType and set v = commandType <- v

  override __.ProvideValue(serviceProvider) =
    getDelegateTypeFromEvent(serviceProvider)
      .bind(createWrapper1 (getCommandResolver(), commandParameter, converter))
      .map(box)
      .get()
