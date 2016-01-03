namespace WpfFs.UI

open System.Windows
open System.Windows.Input
open System.Windows.Media
open System.Windows.Controls
open RZ.Foundation
open RZ.Wpf
open RZ.Wpf.CodeBehind

type RoutedEventInActionModel() =
  let commandCenter =
    [ ApplicationCommands.Open |> CommandMap.to' id ]
    |> CommandControlCenter (fun _ -> RoutedEventInActionModel.ShowPopup())

  interface ICommandHandler with
    member __.ControlCenter = commandCenter

  static member ShowPopup() =
      (XamlLoader.createFromResource "RoutedEventInAction.xaml" <| System.Reflection.Assembly.GetExecutingAssembly())
        .get()
        .cast<System.Windows.Window>()
        .ShowDialog()
      |> ignore


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

  let changeColor =
    tryCast<Border>
    >> Option.do' (fun border ->
        let next = brushConverter.ConvertToString(border.Background) |> getNextColor
        border.Background <- brushConverter.ConvertFromString(next).cast<Brush>())

  do me.InitializeCodeBehind("RoutedEventInActionFront.xaml")
     me.InstallCommandForwarder()

  member __.ChangeColor(sender: obj, _: RoutedEventArgs) = changeColor sender
  member __.ChangeColor2(sender: obj, _: ExecutedRoutedEventArgs) = changeColor sender

  member __.RaisedAsCommand(sender: obj, e:RoutedEventArgs) =
    ApplicationCommands.Open.Execute(null, sender :?> IInputElement)
    e.Handled <- true
  member __.PreventEvents(_:obj, e:RoutedEventArgs) = e.Handled <- true
  member __.ShowPopup(_:obj, e:RoutedEventArgs) =
    RoutedEventInActionModel.ShowPopup()
    e.Handled <- true