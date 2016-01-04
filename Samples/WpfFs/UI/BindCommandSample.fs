namespace WpfFs.UI

open System.Collections.Generic
open System.Windows.Input
open System.Windows.Controls
open RZ.Foundation
open RZ.Wpf.CodeBehind
open FSharp.ViewModule
open System.Collections.ObjectModel

type BindCommandSampleModel() as me =
  inherit ViewModelBase()

  let logList = me.Factory.Backing(<@ me.LogList @>, ObservableCollection<string>())

  let cmdHandler =
    [ NavigationCommands.BrowseHome |> CommandMap.to' (constant "Browse Home")
      ApplicationCommands.Open |> CommandMap.to' (fun o -> o.ToString()) ]
    |> CommandControlCenter ((+) "Got" >> logList.Value.Add)

  interface ICommandHandler with
    member __.ControlCenter = cmdHandler

  member __.LogList = logList.Value

type BindCommandSample() as me =
  inherit UserControl()

  do me.InitializeCodeBehind("BindCommandSample.xaml")
     me.InstallCommandForwarder()
