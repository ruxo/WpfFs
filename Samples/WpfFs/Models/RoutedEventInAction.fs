namespace WpfFs.Models

open FSharp.ViewModule
open RZ.Foundation
open RZ.Wpf

type RoutedEventInActionModel() as me =
  inherit ViewModelBase()

  let showPopupCommand = me.Factory.CommandSync RoutedEventInActionModel.ShowPopup

  static member ShowPopup() =
      (XamlLoader.createFromResource "RoutedEventInAction.xaml" <| System.Reflection.Assembly.GetExecutingAssembly())
        .get()
        .cast<System.Windows.Window>()
        .ShowDialog()
      |> ignore

  member __.ShowPopupCommand = showPopupCommand

