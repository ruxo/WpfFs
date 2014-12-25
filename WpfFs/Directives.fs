namespace WpfFs.Directives

open System
open System.Windows

type Ng() =
    static let onMouseRightButtonDown (sender: obj) (e: Input.MouseButtonEventArgs) =
        printfn "Source = %A, OriginalSource = %A @ %A" (e.Source.GetType().Name) (e.OriginalSource.GetType().Name) (e.Timestamp)

        match e.Source with
        | :? Controls.Control as source ->
                if source.BorderThickness <> Thickness(5.0) then
                    source.BorderThickness <- Thickness(5.0)
                    source.BorderBrush <- Media.Brushes.Black
                else
                    source.BorderThickness <- Thickness(0.0)
        | _ -> ()

    static member SetBlockMark(el: DependencyObject, value: bool) =
        match el with
        | :? UIElement as ui -> if value then
                                    ui.AddHandler(UIElement.MouseRightButtonDownEvent, Input.MouseButtonEventHandler(onMouseRightButtonDown), true)
                                else
                                    ui.RemoveHandler(UIElement.MouseRightButtonDownEvent, Input.MouseButtonEventHandler(onMouseRightButtonDown))
        | _ -> ()