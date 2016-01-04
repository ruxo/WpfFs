namespace RZ.Wpf.Markups

open System.Reflection
open System.Windows
open System.Windows.Markup
open System.Windows.Input
open System.Diagnostics
open System
open FSharp.Core.Fluent
open RZ.Foundation

type private RoutedEventForwarder<'a when 'a :> RoutedEventArgs>(command: ICommand) =
  member __.Invoke(sender:obj, e: 'a) =
    Debug.WriteLine("Hell yeah!!!!")
    e.Handled <- true

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

  static let createWrapper (cmd: ICommand) delType argType =
    let ft = argType |> RoutedEventForwarder.of'
    let wrapper = Activator.CreateInstance(ft, [| box cmd |])
    let m = ft.GetMethod("Invoke", BindingFlags.NonPublic|||BindingFlags.Instance)
    Delegate.CreateDelegate(delType, wrapper, m)

  static let createWrapper1 cmd delType = delType |> getArgType |> Option.map (createWrapper cmd delType)

  let mutable command = cmd |> Option.ofObj

  new() = BindCommand(null)

  [<ConstructorArgument("command")>]
  member __.Command with get() = command and set v = command <- v

  override __.ProvideValue(serviceProvider) =
    getDelegateTypeFromEvent(serviceProvider)
      .bind(createWrapper1 cmd)
      .map(box)
      .get()
