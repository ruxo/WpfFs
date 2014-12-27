namespace WpfFs.Models

open System.Windows
open RZ.Wpf
open RZ.Wpf.Commands

type MainWindowModel() =
    inherit ViewModelBase()

    let mutable xamlFileName = ""

    member this.XamlViewFilename with get() = xamlFileName and set (value) = this.setValue(&xamlFileName, value, "XamlViewFilename")

type MainWindowScope(model: MainWindowModel)  =
    let changeView view = model.XamlViewFilename <- view
    do  model.SubscribeUIEvent ("CHANGEVIEW", fun viewname -> changeView (viewname :?> string))

type RoutedEventInActionModel() =
    inherit ViewModelBase()

    member val ShowPopup = RelayCommand.BindCommand(fun _ -> let win = XamlLoader.loadFromResource "RoutedEventInAction.xaml" None :?> Window
                                                             in ignore <| win.ShowDialog())