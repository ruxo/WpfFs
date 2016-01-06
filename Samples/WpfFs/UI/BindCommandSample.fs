namespace WpfFs.UI

open System
open System.Collections.ObjectModel
open System.Windows
open System.Windows.Input
open System.Windows.Controls
open FSharp.ViewModule
open RZ.Foundation
open RZ.Wpf.CodeBehind

type BindCommandSampleModel() as me =
  inherit ViewModelBase()

  let logList = me.Factory.Backing(<@ me.LogList @>, ObservableCollection<string>())
  
  let myCommand =
    let evt = Event<EventHandler,EventArgs>()
    { new ICommand with
        member __.CanExecute(_) = true
        member __.Execute(_) = logList.Value.Add "My direct command is called!"
        [<CLIEvent>]
        member __.CanExecuteChanged = evt.Publish }

  let cmdHandler =
    [ NavigationCommands.BrowseHome |> CommandMap.to' (constant "Browse Home")
      ApplicationCommands.Open |> CommandMap.to' (fun o -> o.ToString()) ]
    |> CommandControlCenter ((+) "Got " >> logList.Value.Add)

  interface ICommandHandler with
    member __.ControlCenter = cmdHandler

  member __.LogList = logList.Value

  member __.MyCustomCommand = myCommand

  member __.ToUpper(param: string, _: RoutedEventArgs) = param.ToUpper()
  member __.MouseToPoint(_: string, e: MouseEventArgs) = e.GetPosition(e.Source.cast<IInputElement>())
  member __.PreventEvent(_:string, _: RoutedEventArgs): obj = DependencyProperty.UnsetValue

type BindCommandSample() as me =
  inherit UserControl()

  do me.InitializeCodeBehind("BindCommandSample.xaml")
     me.InstallCommandForwarder()
