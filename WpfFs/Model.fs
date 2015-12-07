namespace WpfFs.Models

open FSharp.Core.Printf
open FSharp.ViewModule
open System.Windows
open RZ.Foundation;
open FSharp.Core.Fluent

type MainWindowEvents =
  | Invalid
  | SelectShow of string

type SelectConverter() =
  inherit FsXaml.EventArgsConverter<RoutedEventArgs,MainWindowEvents>(SelectConverter.TagCapture, Invalid)

  static member private TagCapture routeArgs =
    routeArgs.Source
             .tryCast<FrameworkElement>()
             .map(fun fe -> SelectShow (fe.Tag.cast<string>()))
             |> Option.getOrElse (constant Invalid)

type MainWindowModel() as me =
    inherit EventViewModelBase<MainWindowEvents>()

    let eventCommand = me.Factory.EventValueCommand()
    let xamlFileName = me.Factory.Backing(<@ me.XamlViewFilename @>, System.String.Empty)
    let navCommand = me.Factory.EventValueCommand(fun x -> System.Diagnostics.Debug.Print (x.ToString()); Invalid)

    let helpCommand = me.Factory.CommandSync(fun _ -> System.Diagnostics.Process.Start "http://google.com" |> ignore)

    let handleEvents = function
      | Invalid -> System.Diagnostics.Debug.Print "WARN: Invalid message detected!!"
      | SelectShow show -> me.XamlViewFilename <- show

    do
      me.EventStream
      |> Observable.subscribe handleEvents
      |> ignore

    member x.XamlViewFilename with get() = xamlFileName.Value and set v = xamlFileName.Value <- v

    member x.Help: INotifyCommand = helpCommand

    member x.EventCommand = eventCommand
    member x.NavCommand = navCommand
    
