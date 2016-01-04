namespace RZ.Wpf.Markups

open System.Reflection
open System.Windows
open System.Windows.Markup
open System.Windows.Input
open System.Diagnostics
open System
open RZ.Foundation

type private RoutedEventForwarder<'a when 'a :> RoutedEventArgs>(command: ICommand) =
  member __.Invoke(sender:obj, e: 'a) = ()

[<MarkupExtensionReturnType(typeof<MethodInfo>)>]
type BindCommand(cmd: ICommand) =
  inherit MarkupExtension()

  let getDelegateTypeFromEvent(sp: IServiceProvider) =
    sp.GetService(typeof<IProvideValueTarget>)
      .cast<IProvideValueTarget>()
    |> Option.ofObj
    |> Option.bind (fun o -> o.TargetProperty.tryCast<EventInfo>())
    |> Option.map (fun o -> o.EventHandlerType)

  let createForward(delegateType: Type) = ()

  let mutable command = cmd |> Option.ofObj

  let fireCommand (sender:obj) (e:RoutedEventArgs) =
    Debug.WriteLine("OK fire!")
    e.Handled <- true

  new() = BindCommand(null)

  [<ConstructorArgument("command")>]
  member __.Command with get() = command and set v = command <- v

  override me.ProvideValue(serviceProvider) =
    getDelegateTypeFromEvent(serviceProvider)
      .do'(fun delType ->
         Debug.WriteLine("Target type = {0}", delType))

    RoutedEventHandler(fireCommand) |> box 
