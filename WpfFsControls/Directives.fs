namespace RZ.Wpf.Directives

open System
open System.Windows
open System.Windows.Data
open System.Windows.Controls

type Evt() =
    static let setEvent (el: DependencyObject) (event: string) (property: string) =
        let fe = el :?> FrameworkElement
        let trigger = Interactivity.EventTrigger(EventName=event)
        match fe.DataContext with
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
        | _                     -> failwith "Invalid command format"

[<AllowNullLiteral>]
type ModelBinderInfo() =
    member val Target = String.Empty with get, set
    member val Value = String.Empty with get, set

    member x.ChangeDC (fe: FrameworkElement) =
        // specially for RadioButton
        if fe.DataContext = null
            then BindingOperations.ClearBinding(fe, RadioButton.IsCheckedProperty)
            else let binding = Binding(x.Target, Source = fe.DataContext, Converter = x)
                 in  fe.SetBinding(RadioButton.IsCheckedProperty, binding) |> ignore

    member x.Rebound (fe: FrameworkElement) =
        match fe.GetBindingExpression RadioButton.IsCheckedProperty with
        | null -> ()
        | expr -> expr.UpdateTarget()

    interface IValueConverter with
        member x.Convert(value, targetType, parameter, culture) =
            value.ToString() = x.Value :> obj
        member x.ConvertBack(value, targetType, parameter, culture) =
            let is_checked = value :?> bool
            in  if is_checked
                then Enum.Parse(targetType, x.Value)
                else Binding.DoNothing

type Model() =
    static let modelBinderProperty = DependencyProperty.RegisterAttached( "ModelBinder"
                                                                        , typeof<ModelBinderInfo>
                                                                        , typeof<Model> )
    static let makeModelBinder (el: DependencyObject) =
        match el.GetValue modelBinderProperty :?> ModelBinderInfo with
        | null -> let m = ModelBinderInfo()
                  in el.SetValue(modelBinderProperty, m)
                  m
        | model -> model

    static member SetBind(el: DependencyObject, value: string) =
        let fe = el :?> FrameworkElement
        let model = makeModelBinder el
        model.Target <- value
        model.ChangeDC fe

    static member SetValue(el: DependencyObject, value: string) =
        let fe = el :?> FrameworkElement
        let model = makeModelBinder el
        model.Value <- value
        model.Rebound fe

