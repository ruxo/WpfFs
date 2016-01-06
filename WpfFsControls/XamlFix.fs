namespace RZ.Wpf.Directives

open System.Windows
open RZ.Foundation

module private XamlFixImpl =
  open System.ComponentModel
  open System.Windows

  let getProperties o = TypeDescriptor.GetProperties(o: obj)
  let getProperty key (props: PropertyDescriptorCollection) = props.[key:string] |> Option.ofObj

  let commandParameterChanged (d: DependencyObject) (e: DependencyPropertyChangedEventArgs) =
    let resetCommandParameter (cmd: PropertyDescriptor) (param: PropertyDescriptor) =
      param.SetValue(d, e.NewValue)
      let temp = cmd.GetValue d
      cmd.SetValue(d, null)
      cmd.SetValue(d, temp)   // reset to re-evaluate CanExecute()

    let p = getProperties d
    let cmd = p |> getProperty "Command"
    let param = p |> getProperty "CommandParameter"
    (Some resetCommandParameter).ap(cmd).ap(param) |> ignore

type XamlFix() =
  static let getProperty (p:DependencyProperty) (d:DependencyObject) = d.GetValue(p)
  static let setProperty (p:DependencyProperty) (d:DependencyObject, value: obj) = d.SetValue(p, value)
  
  static let CommandParameterProperty =
    DependencyProperty.RegisterAttached("CommandParameter",
      typeof<obj>,
      typeof<XamlFix>,
      UIPropertyMetadata(XamlFixImpl.commandParameterChanged))

  static member GetCommandParameter d = d |> getProperty CommandParameterProperty
  static member SetCommandParameter (d, value: obj) = (d, value) |> setProperty CommandParameterProperty
