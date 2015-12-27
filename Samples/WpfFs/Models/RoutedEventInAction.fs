﻿namespace WpfFs.Models

open FSharp.ViewModule
open RZ.Foundation
open RZ.Wpf

type RoutedEventInActionModel() as me =
  inherit ViewModelBase()

  let showPopup() =
      (XamlLoader.loadFromResource "RoutedEventInAction.xaml" None)
        .get()
        .cast<System.Windows.Window>()
        .ShowDialog()
      |> ignore

  let showPopupCommand = me.Factory.CommandSync showPopup

  member __.ShowPopupCommand = showPopupCommand
