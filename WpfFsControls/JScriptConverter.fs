namespace Utils.Avalon

open System
open System.Collections.Generic
open System.ComponentModel
open System.CodeDom.Compiler
open System.Windows
open System.Windows.Data

type private Evaluator = delegate of string * obj[] -> obj

type private EvaluatorFactory() =
    member this.Create(script: string) =
        let cp = CompilerParameters(GenerateInMemory = true)
        (*  // remove this comment if you want to use other classes in the expression..
        AppDomain.CurrentDomain.GetAssemblies()
        |> Seq.filter (fun asm -> (not asm.IsDynamic) && IO.File.Exists(asm.Location))
        |> Seq.iter (fun asm -> ignore <| cp.ReferencedAssemblies.Add(asm.Location))
        *)

        let codeProvider = new Microsoft.JScript.JScriptCodeProvider()
        let results = codeProvider.CompileAssemblyFromSource(cp, script)
        results.CompiledAssembly

// ref: http://www.11011.net/wpf-binding-expressions
type JScriptConverter() =
    let mutable trap = false

    static let ctor() =
        let source = @"
import System;
class Eval{
    public function Evaluate(code: String, values: Object[]) : Object { return eval(code); }
}"
        let eval = EvaluatorFactory().Create(source).GetType("Eval")
        JScriptConverter.evaluator <- Delegate.CreateDelegate(typeof<Evaluator>, Activator.CreateInstance(eval), "Evaluate") :?> Evaluator

    static do ctor()

    [<Microsoft.FSharp.Core.DefaultValue>]
    static val mutable private evaluator : Evaluator
    member this.TrapExceptions with get() = trap and set(v:bool) = trap <- true

    interface IMultiValueConverter with
        member this.Convert(values, targetType, parameter, culture) =
            try
                JScriptConverter.evaluator.Invoke(parameter.ToString(), values)
            with
            | _ -> if trap then null else reraise()
            
        member this.ConvertBack(value, targetTypes, parameter, culture) = raise <| new NotSupportedException()

    interface IValueConverter with
        member this.Convert(value, targetType, parameter, culture) = (this :> IMultiValueConverter).Convert([|value|], targetType, parameter, culture)
        member this.ConvertBack(value, targetType, parameter, culture) = raise <| new NotSupportedException()

type DC(element: FrameworkElement) as this =
    static let evaluator =
        let source = @"
import System;
class Eval{
    public function Evaluate(code: String, ctx: Object) : Object { return eval(code); }
}"
        let eval = EvaluatorFactory().Create(source).GetType("Eval")
        Delegate.CreateDelegate(typeof<Func<string,obj,obj>>, Activator.CreateInstance(eval), "Evaluate") :?> Func<string,obj,obj>

    static let dcs = Dictionary<FrameworkElement, DC>()

    static let getDC(element: DependencyObject) =
        let getContext() = (element :?> FrameworkElement).DataContext
        if element.Dispatcher.CheckAccess() then
            getContext()
        else
            element.Dispatcher.Invoke(getContext)

    let mutable updaterList = []
    let elementType = element.GetType()

    let onValueChanged (s:obj) (args: PropertyChangedEventArgs) = this.updateValue()

    let subscribePropertyChanged(ctx: obj) =
        match ctx with
        | :? INotifyPropertyChanged as notifier -> notifier.PropertyChanged.AddHandler(PropertyChangedEventHandler(onValueChanged))
        | _ -> ()
    let unsubscribePropertyChanged(ctx: obj) =
        match ctx with
        | :? INotifyPropertyChanged as notifier -> notifier.PropertyChanged.RemoveHandler(PropertyChangedEventHandler(onValueChanged))
        | _ -> ()

    do element.DataContextChanged.AddHandler(
        fun _ args ->
            unsubscribePropertyChanged(args.OldValue)
            this.updateValue()
            subscribePropertyChanged(args.NewValue)
        )

    static member SetExpression(element: DependencyObject, value: string) =
        let property, expr = match value.Split([|"->"|], StringSplitOptions.None) with
                             | [| p; expr |] -> p.Trim(), expr.Trim()
                             | _ -> failwith "Invalid expression"
        let fe = element :?> FrameworkElement
        let dc = match dcs.TryGetValue(fe) with
                 | false, _ -> let newDC = DC(fe)
                               dcs.[fe] <- newDC
                               newDC
                 | true, currentDC -> currentDC
        dc.addWatch(property, expr)
        dc.updateValue(property)

    member private this.addWatch(property, expr) =
        match elementType.GetProperty(property) with
        | null -> failwithf "Unrecognize property %s" property
        | ep -> updaterList <- (ep, expr)::updaterList

    member private this.updateValue(?property) = 
        match getDC(element) with
        | null -> ()
        | ctx -> 
            let list = match property with
                       | None -> updaterList
                       | Some name -> updaterList |> List.filter (fun (ep,_) -> ep.Name = name)
            list
            |> List.iter (fun (ep, expr) -> 
                            let value = evaluator.Invoke(expr, ctx)
                            let action = Action(fun() -> ep.SetValue(element, value))
                            ignore <| element.Dispatcher.BeginInvoke(action))
