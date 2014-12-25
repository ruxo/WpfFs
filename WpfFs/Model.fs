namespace TestFs.Models

open System.Windows
open RZ.Wpf
open RZ.Wpf.Commands

type MainWindowModel() as this =
    inherit ViewModelBase()

    let mutable xamlFileName = ""

    member this.XamlViewFilename with get() = xamlFileName and set (value) = this.setValue(&xamlFileName, value, "XamlViewFilename")

    member val RunAbout = RelayCommand.BindCommand(fun _ -> this.XamlViewFilename <- "AboutDialog.xaml")
    member val RunGridSharedSizeGroup = RelayCommand.BindCommand(fun _ -> this.XamlViewFilename <- "GridSharedSizeGroup.xaml")
    member val RunRoutedEventInAction = RelayCommand.BindCommand(fun _ -> this.XamlViewFilename <- "RoutedEventInActionFront.xaml")

type RoutedEventInActionModel() =
    inherit ViewModelBase()

    member val ShowPopup = RelayCommand.BindCommand(fun _ -> let win = XamlLoader.LoadWpfFromFile "RoutedEventInAction.xaml" :?> Window
                                                             ignore <| win.ShowDialog())