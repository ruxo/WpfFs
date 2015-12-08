namespace WpfFs.Models

open FSharp.Core.Printf
open FSharp.ViewModule
open System.Windows
open RZ.Foundation;
open FSharp.Core.Fluent
open System.Windows.Input
open System

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

    let dummyEvent = new Event<EventHandler,EventArgs>()

    let helpCommand = me.Factory.CommandSync(fun _ -> System.Diagnostics.Process.Start "http://google.com" |> ignore)

    let handleEvents = function
      | Invalid -> System.Diagnostics.Debug.Print "WARN: Invalid message detected!!"
      | SelectShow show -> me.XamlViewFilename <- show

    let navCommand = { new ICommand with
                         [<CLIEvent>]
                         member x.CanExecuteChanged = dummyEvent.Publish
                         member x.CanExecute _ = true
                         member x.Execute o = tryCast<string>(o).map(SelectShow) |> Option.do' handleEvents }

    do
      me.EventStream
      |> Observable.subscribe handleEvents
      |> ignore

    member x.XamlViewFilename with get() = xamlFileName.Value and set v = xamlFileName.Value <- v

    member x.Help: INotifyCommand = helpCommand

    member x.EventCommand = eventCommand
    member x.NavCommand = navCommand
    
