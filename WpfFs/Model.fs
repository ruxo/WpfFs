namespace WpfFs.Models

open System.Windows
open RZ.Wpf
open RZ.Wpf.Commands
open System.Collections.ObjectModel

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

type Person() =
    member val Id = 0 with get, set
    member val Name = "" with get, set

type PersonCollection() = inherit ObservableCollection<Person>()

type DataBindingMode = SingleThread = 0 | MultiThread = 1

type DatabindingSampleModel() =
    inherit ViewModelBase()

    let mutable data = PersonCollection()
    let mutable bindingMode = DataBindingMode.SingleThread

    member x.Data with get() = data and set(v) = x.setValue(&data, v, "Data")
    member x.DataBindingMode with get() = bindingMode and set v = x.setValue(&bindingMode, v, "DataBindingMode")