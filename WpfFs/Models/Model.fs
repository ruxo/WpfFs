namespace WpfFs.Models

open FSharp.ViewModule
open RZ.Foundation

type MainWindowEvents =
  | Invalid
  | SelectShow of string

type MainWindowModel() as me =
    inherit EventViewModelBase<MainWindowEvents>()

    let eventCommand = me.Factory.EventValueCommand()
    let xamlFileName = me.Factory.Backing(<@ me.XamlViewFilename @>, System.String.Empty)

    let helpCommand = me.Factory.CommandSync(fun _ -> System.Diagnostics.Process.Start "http://google.com" |> ignore)

    let handleEvents = function
      | Invalid -> System.Diagnostics.Debug.Print "WARN: Invalid message detected!!"
      | SelectShow show -> me.XamlViewFilename <- show

    let navCommand = me.Factory.EventValueCommand(Option.ofObj >> Option.map SelectShow >> Option.getOrElse (constant Invalid))

    do
      me.EventStream
      |> Observable.subscribe handleEvents
      |> ignore

    member __.XamlViewFilename with get() = xamlFileName.Value and set v = xamlFileName.Value <- v

    member __.Help: INotifyCommand = helpCommand

    member __.EventCommand = eventCommand
    member __.NavCommand = navCommand
    
