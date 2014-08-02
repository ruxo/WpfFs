namespace TestFs.Models

open RZ.Wpf.Commands

type MainWindowModel() as this =
    inherit RZ.Wpf.ViewModelBase()

    let mutable xamlFileName = ""

    member this.XamlViewFilename with get() = xamlFileName and set (value) = this.setValue(&xamlFileName, value, "XamlViewFilename")

    member val RunAbout = RelayCommand.BindCommand(fun _ -> this.XamlViewFilename <- "AboutDialog.xaml") with get
    member val RunGridSharedSizeGroup = RelayCommand.BindCommand(fun _ -> this.XamlViewFilename <- "GridSharedSizeGroup.xaml") with get