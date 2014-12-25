open System
open System.Threading
open System.Threading.Tasks
open System.Windows
open System.Windows.Threading

open WpfFs.UI

[<STAThread>]
[<EntryPoint>]
let main _ = 
    printfn "Window is started"
    let win = MainWindow()

    let ret = Application().Run(win)

    printfn "Finished."
    ret
