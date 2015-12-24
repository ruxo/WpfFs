namespace WpfFs.UI

open System.Windows
open System.Windows.Input
open FSharp.Core.Fluent
open FSharp.ViewModule
open RZ.Foundation
open RZ.Wpf.CodeBehind

type MainWindowEvents =
  | Invalid
  | SelectShow of string

module private MainWindowData =
  let menuDefault =
    [ "Default",
        ["Sample", "AboutDialog.xaml"]
      "Layouts",
        ["Grid: share side group", "GridSharedSizeGroup.xaml"]
      "Input",
        ["Routed Events", "RoutedEventInActionFront.xaml"]
      "Data Binding", ["Collection Binding", "DataBindingSample.xaml"]
      "Documents", ["Flow Document", "FlowDocumentSample.xaml"]

    ] :> ExpanderMenuItem seq 

type MainWindowModel() as me =
    inherit EventViewModelBase<MainWindowEvents>()

    let eventCommand = me.Factory.EventValueCommand()
    let xamlFileName = me.Factory.Backing(<@ me.XamlViewFilename @>, System.String.Empty)

    let handleEvents = function
      | Invalid -> System.Diagnostics.Debug.Print "WARN: Invalid message detected!!"
      | SelectShow show -> me.XamlViewFilename <- show

    let navCommand = me.Factory.EventValueCommand(Option.ofObj >> Option.map SelectShow >> Option.getOrElse (constant Invalid))

    do
      me.EventStream
      |> Observable.subscribe handleEvents
      |> ignore

    member __.XamlViewFilename with get() = xamlFileName.Value and set v = xamlFileName.Value <- v
    member __.MenuItems = MainWindowData.menuDefault

    member __.EventCommand = eventCommand
    member __.NavCommand = navCommand

    member __.Process = handleEvents

type MainWindow() as me =
  inherit Window()

  do me.InitializeCodeBehind("MainWindow.xaml")
  let model = me.DataContext.tryCast<MainWindowModel>().get()

  let startGoogle() = System.Diagnostics.Process.Start "http://google.com" |> ignore
  let show = SelectShow >> model.Process

  let commandBindings =
    [ CommandBinding(ApplicationCommands.Help, fun _ _ -> startGoogle())
      CommandBinding(ApplicationCommands.Open, fun _ e -> show (e.Parameter |> cast<string>)) ]

  do commandBindings |> Seq.iter (me.CommandBindings.Add >> ignore)
