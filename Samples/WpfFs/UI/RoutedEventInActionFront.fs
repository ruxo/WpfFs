namespace WpfFs.UI

open System.Windows
open System.Windows.Controls
open RZ.Wpf.CodeBehind

type RoutedEventInActionFront() as me =
  inherit UserControl()

  do me.InitializeCodeBehind("RoutedEventInActionFront.xaml")

  member __.ShowPopup(_:obj, _:RoutedEventArgs) = WpfFs.Models.RoutedEventInActionModel.ShowPopup()