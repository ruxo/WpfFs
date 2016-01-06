namespace RZ.Wpf.Markups

open System.Reflection
open System.Windows
open System.Windows.Markup
open System.Windows.Input
open System.Diagnostics
open System
open FSharp.Core.Fluent
open RZ.Foundation

type private RoutedEventForwarder<'a when 'a :> RoutedEventArgs>
      (command: ICommand, commandParameter: string, converter: string option) =
  let routedCmd = command.tryCast<RoutedUICommand>()

  static let getFrameworkElementDC = tryCast<FrameworkElement> >> Option.bind (fun fe -> fe.DataContext |> Option.ofObj)
  static let getFrameworkContentElementDC = tryCast<FrameworkContentElement> >> Option.bind (fun fe -> fe.DataContext |> Option.ofObj)
  static let getDataContext (sender: obj) =
    sender |> getFrameworkElementDC
           |> Option.orTry (fun() -> sender |> getFrameworkContentElementDC)

  static let getPublicMethod name o = o.GetType().GetMethod(name) |> Option.ofObj
  static let getConverter converterName dc =
    match getPublicMethod converterName dc with
    | None -> None
    | Some m -> 
      let p = m.GetParameters()
      if p.Length = 2 && p.[0].ParameterType = typeof<string> && p.[1].ParameterType.IsAssignableFrom(typeof<'a>)
        then Some (fun (param: string) (e: obj) -> m.Invoke(dc, [| box param; e |]))
        else None

  let raiseRoutedCommand p (target: IInputElement) (cmd: RoutedUICommand) =
    if cmd.CanExecute(p, target) then
      cmd.Execute(p, target)
      true
    else
      false

  let raiseCommand p =
    if command.CanExecute(p) then
      command.Execute(p)
      true
    else
      false

  member __.Invoke(sender:obj, e: 'a) =
    let param =
      Some getConverter
       |> Option.ap converter
       |> Option.ap (getDataContext sender)
       |> Option.join
       |> Option.map (fun f -> f commandParameter e)
       |> Option.getOrElse (fun() -> box commandParameter)

    let handled =
      match sender.tryCast<IInputElement>(), routedCmd with
      | Some iinput, Some c -> raiseRoutedCommand param iinput c
      | _, _ -> raiseCommand param
    e.Handled <- handled


module private RoutedEventForwarder =
  let routedEventForwarder = typedefof<RoutedEventForwarder<_>>

  let of' = RZ.Wpf.Utils.memoize (fun routedEventType -> routedEventForwarder.MakeGenericType([| routedEventType |]))


[<MarkupExtensionReturnType(typeof<MethodInfo>)>]
type BindCommand(cmd: ICommand) =
  inherit MarkupExtension()

  static let getDelegateTypeFromEvent(sp: IServiceProvider) =
    sp.GetService(typeof<IProvideValueTarget>)
      .cast<IProvideValueTarget>()
    |> Option.ofObj
    |> Option.bind (fun o -> o.TargetProperty.tryCast<EventInfo>())
    |> Option.map (fun o -> o.EventHandlerType)

  static let getArgType(delegateType: Type) =
    match delegateType.GetMethod("Invoke") with
    | null -> None
    | m ->
      let p = m.GetParameters()
      if m.ReturnType = typeof<System.Void>
         && p.Length = 2 
         && p.[0].ParameterType = typeof<obj> 
         && typeof<RoutedEventArgs>.IsAssignableFrom(p.[1].ParameterType) then
        Some p.[1].ParameterType
      else
        Debug.WriteLine("Not a valid WPF event type!")
        None

  static let createWrapper (cmd: ICommand, cmdParam: string, converter: string option) delType argType =
    let ft = argType |> RoutedEventForwarder.of'
    let wrapper = Activator.CreateInstance(ft, [| box cmd; box cmdParam; box converter |])
    let m = ft.GetMethod("Invoke", BindingFlags.NonPublic|||BindingFlags.Instance)
    Delegate.CreateDelegate(delType, wrapper, m)

  static let createWrapper1 cmd delType = delType |> getArgType |> Option.map (createWrapper cmd delType)

  let mutable command = cmd
  let mutable commandParameter = ""
  let mutable converter = None

  new() = BindCommand(null)

  [<ConstructorArgument("command")>]
  member __.Command with get() = command and set v = command <- v
  member __.CommandParameter with get() = commandParameter and set v = commandParameter <- v
  member __.EventArgumentConverter with get() = converter.getOrElse(constant "") and set v = converter <- if String.IsNullOrWhiteSpace(v) then None else Some v

  override __.ProvideValue(serviceProvider) =
    getDelegateTypeFromEvent(serviceProvider)
      .bind(createWrapper1 (command, commandParameter, converter))
      .map(box)
      .get()
