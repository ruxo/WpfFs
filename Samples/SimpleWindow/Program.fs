open System
open RZ.Wpf.CodeBehind
open System.Windows

// Note that Sample.xaml must be compiled as 'Resource' for this to work.
//
type MainWindow() as me =
  inherit Window()

  do me.InitializeCodeBehind("Sample.xaml")

  member __.ShowMessage(_: obj, _: RoutedEventArgs) = MessageBox.Show("Hello you!") |> ignore

[<STAThread>]
Application().Run <| MainWindow() |> ignore