namespace RZ.Wpf.Commands

open System
open System.Windows.Input

type RelayCommand(execute: Action<obj>, ?canExecute: Predicate<obj>) =
    let canExecuteChanged = new Event<EventHandler, EventArgs>()
    do
        if execute = null then
            raise <| new ArgumentNullException("execute")

    interface ICommand with
        member this.CanExecute(param) = match canExecute with
                                        | Some fn -> fn.Invoke(param)
                                        | None -> true

        member this.Execute(param) = execute.Invoke(param)
        [<CLIEvent>]
        member this.CanExecuteChanged = canExecuteChanged.Publish

    member this.UpdateCanExecuteCommand() = canExecuteChanged.Trigger(this, EventArgs.Empty)

    static member BindCommand fn = RelayCommand(new Action<obj>(fn)) :> ICommand