open System
open System.Windows

open WpfFs.UI

[<STAThread>]
[<EntryPoint>]
let main _ = 
    printfn "Window is started"
    let win = MainWindow()

    let ret = Application().Run(win)

    printfn "Finished."
    ret
