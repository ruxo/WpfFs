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
  | Help

module private MainWindowData =
  let menuDefault =
    [ "Default", ["Sample", "AboutDialog.xaml"]
      "Controls", ["Decorators", "DecoratorSample.xaml"]
      "Layouts", ["Grid: share side group", "GridSharedSizeGroup.xaml"]
      "Input", ["Routed Events", "RoutedEventInActionFront.xaml"]
      "Data Binding", ["Collection Binding", "DataBindingSample.xaml"]
      "Documents", ["Flow Document", "FlowDocumentSample.xaml"]

    ] :> ExpanderMenuItem seq 


type MainWindowModel() as me =
    inherit ViewModelBase()

    static let startGoogle() = System.Diagnostics.Process.Start "http://google.com" |> ignore

    let xamlFileName = me.Factory.Backing(<@ me.XamlViewFilename @>, System.String.Empty)

    let handleEvents = function
      | Invalid -> System.Diagnostics.Debug.Print "WARN: Invalid message detected!!"
      | SelectShow show -> me.XamlViewFilename <- show
      | Help -> startGoogle()

    let cmdCenter =
      [ ApplicationCommands.Help |> CommandMap.to' (constant Help) 
        ApplicationCommands.Open |> CommandMap.to' (SelectShow << cast<string>) ]
      |> CommandControlCenter handleEvents

    member __.XamlViewFilename with get() = xamlFileName.Value and set v = xamlFileName.Value <- v
    member __.MenuItems = MainWindowData.menuDefault

    interface ICommandHandler with
      member __.ControlCenter = cmdCenter

type MainWindow() as me =
  inherit Window()

  do me.InitializeCodeBehind("MainWindow.xaml")
  do me.InstallCommandForwarder()
