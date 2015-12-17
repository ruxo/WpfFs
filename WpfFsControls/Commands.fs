namespace RZ.Wpf.Commands

open System
open System.Windows
open System.Windows.Input
open RZ.Foundation

type RelayCommand(execute: Action<obj>, ?canExecute: Predicate<obj>) =
    let canExecuteChanged = new Event<EventHandler, EventArgs>()
    do
        if isNull execute then
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

module private CPB =
  let updateCommandState (cmd: ICommand) =
    match cmd with
    | :? RoutedCommand -> CommandManager.InvalidateRequerySuggested()
    | _ -> ()

  let isCommandRequeriedOnChange (d: DependencyObject) (e: DependencyPropertyChangedEventArgs) =
    let command = d |> tryCast<ICommand>
    match box d with
    | :? ICommandSource
    | :? FrameworkElement
    | :? FrameworkContentElement ->
      ()
    | _ -> ()


type CommandParameterBehavior() =
  static let commandChanged (d: DependencyObject) e = ()

  static let IsCommandRequeriedOnChangeProperty =
    DependencyProperty.RegisterAttached("IsCommandRequeriedOnChange",
      typeof<bool>,
      typeof<CommandParameterBehavior>,
      UIPropertyMetadata(false, PropertyChangedCallback(commandChanged)))
