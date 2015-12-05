namespace WpfFs.Models

open FSharp.Core.Printf
open FSharp.ViewModule
open System.Windows

type MainWindowEvents =
  | Invalid
  | SelectShow of string

type SelectConverter() =
  inherit FsXaml.EventArgsConverter<RoutedEventArgs,MainWindowEvents>(SelectConverter.TagCapture, Invalid)

  static member private TagCapture routeArgs = SelectShow "hello"

type MainWindowModel() as me =
    inherit EventViewModelBase<MainWindowEvents>()

    let eventCommand = me.Factory.EventValueCommand()
    let xamlFileName = me.Factory.Backing(<@ me.XamlViewFilename @>, System.String.Empty)

    let helpCommand = me.Factory.CommandSync(fun _ -> System.Diagnostics.Process.Start "http://google.com" |> ignore)

    do
      me.EventStream
      |> Observable.subscribe (kprintf System.Diagnostics.Debug.Print "%A")
      |> ignore

    member x.XamlViewFilename with get() = xamlFileName.Value and set v = xamlFileName.Value <- v

    member x.Help: INotifyCommand = helpCommand

    member x.EventCommand = eventCommand
    
