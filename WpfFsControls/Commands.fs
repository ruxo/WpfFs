namespace RZ.Wpf.Commands

open System
open System.Windows.Input

type RelayCommand(execute: Action<obj>, ?canExecute: Predicate<obj>) =
    let canExecuteChanged = new Event<EventHandler, EventArgs>()
    do
        if isNull execute then
            raise <| new ArgumentNullException("execute")

    interface ICommand with
        member __.CanExecute(param) = match canExecute with
                                        | Some fn -> fn.Invoke(param)
                                        | None -> true

        member __.Execute(param) = execute.Invoke(param)
        [<CLIEvent>]
        member __.CanExecuteChanged = canExecuteChanged.Publish

    member this.UpdateCanExecuteCommand() = canExecuteChanged.Trigger(this, EventArgs.Empty)

    static member BindCommand fn = RelayCommand(new Action<obj>(fn)) :> ICommand
