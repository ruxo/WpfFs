namespace WpfFs.Models

open System.Collections.ObjectModel
open RZ.Wpf
open RZ.Wpf.Commands
open System.Windows

type RoutedEventInActionModel() =
    inherit ViewModelBase()

    member val ShowPopup = RelayCommand.BindCommand(fun _ -> let win = XamlLoader.loadFromResource "RoutedEventInAction.xaml" None :?> Window
                                                             in ignore <| win.ShowDialog())

type Person() =
    member val Id = 0 with get, set
    member val Name = "" with get, set

type PersonCollection() = inherit ObservableCollection<Person>()

type DataBindingMode = SingleThread = 0 | MultiThread = 1
type DataBindingCollectionMode = ObserverableCollection = 0 | WpfObservableCollection = 1

type DatabindingSampleModel() =
    inherit ViewModelBase()

    let mutable data = PersonCollection()
    let mutable bindingMode = DataBindingMode.SingleThread

    member x.Data with get() = data and set(v) = x.setValue(&data, v, "Data")
    member x.DataBindingMode with get() = bindingMode and set v = x.setValue(&bindingMode, v, "DataBindingMode")
    member val CollectionMode = [DataBindingCollectionMode.ObserverableCollection; DataBindingCollectionMode.WpfObservableCollection] with get

