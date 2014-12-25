namespace RZ.Wpf
// v20140819

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

        #if REFALL  // define this if you want to use all known types in the expression..
        AppDomain.CurrentDomain.GetAssemblies()
        |> Seq.filter (fun asm -> (not asm.IsDynamic) && IO.File.Exists(asm.Location))
        |> Seq.iter (fun asm -> ignore <| cp.ReferencedAssemblies.Add(asm.Location))
        #endif

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

    let mutable updaterList = []
    let onValueChanged (s:obj) (args: PropertyChangedEventArgs) = this.UpdateValue()

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
            this.UpdateValue()
            subscribePropertyChanged(args.NewValue)
        )

    static let defaultValue propType = if (propType:Type).IsValueType then Activator.CreateInstance(propType) else null
    let eval ctx propType expr =
        match evaluator.Invoke(expr, ctx) with
        | null -> defaultValue propType
        | value when propType.IsAssignableFrom(value.GetType()) -> value
        | :? string as value -> TypeDescriptor.GetConverter(propType).ConvertFromInvariantString(value)
        | value -> TypeDescriptor.GetConverter(propType).ConvertFrom(value)

    static member SetExpression(element: DependencyObject, value: string) =
        let property, expr = match value.Split([|"->"|], StringSplitOptions.None) with
                             | [| p; expr |] -> p.Trim(), expr.Trim()
                             | _ -> failwith "Invalid expression"
        let dc = let fe = element :?> FrameworkElement
                 match dcs.TryGetValue(fe) with
                 | false, _ -> let newDC = DC(fe)
                               dcs.[fe] <- newDC
                               newDC
                 | true, currentDC -> currentDC

        let et = element.GetType()

        match et.GetProperty(property) with
        | null -> failwithf "Unrecognize property %s" property
        | ep -> dc.AddWatch(ep, expr.Replace('`','"'))
        dc.UpdateValue(property)

    member me.AddWatch(property, expr) =
        updaterList <- (property, expr)::updaterList

        subscribePropertyChanged <| me.GetContext()

    member me.Evaluate propType expr = match me.GetContext() with
                                       | null -> defaultValue propType
                                       | ctx -> eval ctx propType expr
    member me.GetContext() =
        let getContext() = element.DataContext
        if element.Dispatcher.CheckAccess() then
            getContext()
        else
            element.Dispatcher.Invoke(getContext)

    member me.UpdateValue(?property) = 
        match me.GetContext() with
        | null -> ()
        | ctx -> 
            match property with
            | None -> updaterList
            | Some name -> updaterList |> List.filter (fun (ep,_) -> ep.Name = name)
            |> List.iter (fun (ep, expr) -> 
                            let value = eval ctx ep.PropertyType expr
                            let action = Action(fun() -> ep.SetValue(element, value))
                            ignore <| element.Dispatcher.BeginInvoke(action))

open System.Windows.Markup
type DCExpression(expr: string) =
    inherit MarkupExtension()

    static let dcs = Dictionary<FrameworkElement, DC>()

    override me.ProvideValue(serviceProvider) =
        let targetService = serviceProvider.GetService(typeof<IProvideValueTarget>) :?> IProvideValueTarget
        let fe = targetService.TargetObject :?> FrameworkElement
        let dp = targetService.TargetProperty :?> DependencyProperty
        let property = let et = fe.GetType()
                       et.GetProperty(dp.Name)

        let dc = match dcs.TryGetValue(fe) with
                 | false, _ -> let newDC = DC(fe)
                               dcs.[fe] <- newDC
                               newDC
                 | true, currentDC -> currentDC

        let converted = expr.Replace('`', '"')
        dc.AddWatch(property, converted)
        dc.Evaluate dp.PropertyType converted
