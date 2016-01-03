namespace WpfFs.UI

open System.Windows
open System.Windows.Controls

open WpfFs.Models
open RZ.Wpf.CodeBehind

type DataBindingSampleView() as me =
    inherit UserControl()
    let mutable running_id = 1
    let getId() = System.Threading.Interlocked.Increment(&running_id)

    do me.InitializeCodeBehind("DataBindingSample.xaml")

    member private x.context with get() = me.DataContext :?> DatabindingSampleModel

    member x.Add10Items  (_:obj, _:RoutedEventArgs) =
        for i = 1 to 10 do
            let id = getId()
            in  x.context.Data.Add <| Person(Id = id, Name = "Test " + id.ToString())

    member x.Remove5Items(_:obj, _:RoutedEventArgs) =
        let removeTop() = if x.context.Data.Count > 0 then x.context.Data.RemoveAt 0
        let rec remove = function
                         | 0 -> ()
                         | n -> removeTop()
                                remove (n-1)
        in  remove 5
    
    member x.ClearItems  (_:obj, _:RoutedEventArgs) = x.context.Data.Clear()
