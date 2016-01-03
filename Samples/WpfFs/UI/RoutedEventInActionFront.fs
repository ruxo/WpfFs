namespace WpfFs.UI

open System.Windows
open System.Windows.Controls
open RZ.Foundation
open RZ.Wpf.CodeBehind
open System.Windows.Media

type RoutedEventInActionFront() as me =
  inherit UserControl()

  static let brushConverter = BrushConverter()
  static let colorShades =
    [ Brushes.Brown; Brushes.Red; Brushes.Orange; Brushes.Yellow; Brushes.Green ]
    |> Seq.map brushConverter.ConvertToString
    |> Seq.toArray
  static let findColorIndex = (flip Array.findIndex) colorShades

  let getNextColor clr =
    let next = ((findColorIndex ((=)clr)) + 1) % colorShades.Length
    colorShades.[next]

  do me.InitializeCodeBehind("RoutedEventInActionFront.xaml")

  member __.ChangeColor(sender: obj, _: RoutedEventArgs) =
    sender
      .tryCast<Border>()
      .do'(fun border ->
        let next = brushConverter.ConvertToString(border.Background) |> getNextColor
        border.Background <- brushConverter.ConvertFromString(next).cast<Brush>())

  member __.PreventEvents(_:obj, e:RoutedEventArgs) = e.Handled <- true
  member __.ShowPopup(_:obj, e:RoutedEventArgs) =
    WpfFs.Models.RoutedEventInActionModel.ShowPopup()
    e.Handled <- true