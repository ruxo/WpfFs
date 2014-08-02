namespace RZ.Wpf.Directives

open System.Windows

type Evt() =
    static let setEvent (el: DependencyObject) (event: string) (property: string) =
        let trigger = Interactivity.EventTrigger(EventName=event)
        match (el :?> FrameworkElement).DataContext with
        | null    -> () // failwith "DataContext must have been set"
        | context -> let ct = context.GetType()
                     let propGet = ct.GetProperty(property).GetGetMethod()
                     let command = propGet.Invoke(context, null) :?> Input.ICommand
                     let action = Interactivity.InvokeCommandAction(Command = command)
                     trigger.Actions.Add(action)
                     trigger.Attach(el)

    static member SetToCommand(element: DependencyObject, value: string) =
        match value.Split([|"->"|], System.StringSplitOptions.None) with
        | [| event; property |] -> setEvent (element) (event.Trim()) (property.Trim())
        | _                     -> failwithf "Invalid command format"