open System
open System.Threading
open System.Threading.Tasks
open System.Windows
open System.Windows.Threading

let startWindow (result: obj) =
    let root = RZ.Wpf.XamlLoader.LoadWpfFromFile "MainWindow.xaml" :?> Window

    let dispatcher = Dispatcher.CurrentDispatcher

    let action = Action(fun () -> 
                            let dispatcherResult = result :?> TaskCompletionSource<Dispatcher>
                            dispatcherResult.SetResult dispatcher
                       )
    ignore <| dispatcher.BeginInvoke(action, null)
    ignore <| Application().Run root

let startWindowInThread() =
    let result = TaskCompletionSource<Dispatcher>()

    let winThread = Thread(fun() -> startWindow result)
    winThread.SetApartmentState ApartmentState.STA
    winThread.Start()

    result.Task

[<EntryPoint>]
let main argv = 
    let winTask = startWindowInThread()
    let dispatcher = winTask.Result

    let shutdownWait = TaskCompletionSource<bool>()
    dispatcher.ShutdownFinished.Add(fun _ -> shutdownWait.SetResult true)

    printfn "Window is started"
    shutdownWait.Task.Wait()

    printfn "Finished."
    0
