namespace WpfFs.UI

open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Input
open WpfFs.Models
open RZ.Wpf

type MainWindow() as me =
    inherit System.Windows.Window()

    do  RZ.Wpf.XamlLoader.loadFromResource "mainwindow.xaml" (Some me) |> ignore
    let context = me.DataContext :?> MainWindowModel
    let scope = MainWindowScope context
    let changeView (name:string) = context.OnUIEvent("CHANGEVIEW", name :> obj)

    member this.HelpCanExecute(sender:obj, e:CanExecuteRoutedEventArgs) = e.CanExecute <- true

    member this.HelpExecuted(sender:obj, e:ExecutedRoutedEventArgs) = ignore <| System.Diagnostics.Process.Start "http://google.com"

    member this.ListBox_SelectionChanged(sender:obj, e:SelectionChangedEventArgs) =
        if e.AddedItems.Count > 0 then
            ignore <| MessageBox.Show( "You just selected " + e.AddedItems.[0].ToString())

    member this.Button_Click(sender:obj, e:RoutedEventArgs) = ignore <| MessageBox.Show("You just clicked " + e.Source.ToString())

    member this.AboutDialog_MouseRightButtonDown(sender: obj, e: MouseButtonEventArgs) =
        this.Title <- "Source = " + e.Source.GetType().Name + ", Original Source = " + 
                        e.OriginalSource.GetType().Name + " @ " + e.Timestamp.ToString()

        let source = e.Source :?> Control

        if source.BorderThickness <> Thickness(5.0) then
            source.BorderThickness <- Thickness(5.0)
            source.BorderBrush <- Brushes.Black
        else
            source.BorderThickness <- Thickness(0.0)

    member private me.RunAbout (s:obj, e:RoutedEventArgs) = changeView "AboutDialog.xaml"
    member private me.RunRoutedEventInAction (s:obj, e:RoutedEventArgs) = changeView "RoutedEventInActionFront.xaml"
    member private me.RunDataBinding (s:obj, e:RoutedEventArgs) = changeView "DataBindingSample.xaml"
    member private me.RunGridSharedSizeGroup (s:obj, e:RoutedEventArgs) = changeView "GridSharedSizeGroup.xaml"
