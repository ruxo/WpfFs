namespace WpfFs.UI

open System.Windows.Input

type MainWindow = FsXaml.XAML<"MainWindow.xaml", true>

type MainWindowController() =
  inherit FsXaml.WindowViewController<MainWindow>()

  let startGoogle() = System.Diagnostics.Process.Start "http://google.com" |> ignore

  override __.OnInitialized window =
    window.Root.CommandBindings.Add
      <| CommandBinding(ApplicationCommands.Help, fun _ _ -> startGoogle())
    |> ignore
