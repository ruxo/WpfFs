namespace RZ.Wpf.Markups

open System.Windows
open System.Windows.Markup
open RZ.Foundation
open System.Windows.Media

module private VisualHelper =
  let getParentContentElement contentElement =
    ContentOperations.GetParent contentElement
      |> Option.ofObj 
      |> Option.orTry (fun _ -> tryCast<FrameworkContentElement> contentElement |> Option.map (fun fce -> fce.Parent))

  let getParentFrameworkElement (frameworkElement: FrameworkElement) = Option.ofObj frameworkElement.Parent

  let getParentObject (child: DependencyObject) =
    match child with
    | :? ContentElement as ce -> getParentContentElement ce
    | :? FrameworkElement as fe -> getParentFrameworkElement fe // (e.g. DockPanel)
    | _ -> Option.ofObj <| VisualTreeHelper.GetParent child

  let rec tryGetParent<'T when 'T :> DependencyObject> child =
    match getParentObject child with
    | None -> None
    | Some parent ->
      match parent with
      | :? 'T as x -> Some x
      | _ -> tryGetParent<'T> parent

/// <summary>
/// 'em' font size unit like in HTML
/// </summary>
/// <remarks>
/// Source from http://stackoverflow.com/questions/653918/wpf-analogy-for-em-unit/
/// </remarks>
[<MarkupExtensionReturnType(typeof<float>)>]
type EmFontSize(size) =
  inherit MarkupExtension()

  let mutable _size = size

  new() = EmFontSize(8.0)

  [<ConstructorArgument("size")>]
  member __.Size with get() = _size and set v = _size <- v

  override x.ProvideValue(serviceProvider) =
    let ipvt = serviceProvider.GetService typeof<IProvideValueTarget> :?> IProvideValueTarget
    // about SharedDp: http://www.thomaslevesque.com/2009/08/23/wpf-markup-extensions-and-templates/
    //
    // explained: When a markup extension is evaluated inside a template, the TargetObject is actually an instance of System.Windows.SharedDp, an internal WPF class.
    // For the markup extension to be able to access its target, it has to be evaluated when the template is applied : we need to defer its evaluation until this time.
    //
    if ipvt.TargetObject.GetType().FullName = "System.Windows.SharedDp" then
      x :> obj
    else
      ipvt.TargetObject :?> DependencyObject
        |> VisualHelper.tryGetParent<Window>
        |> Option.map (fun w -> w.FontSize * _size)
        |> Option.getOrElse (fun _ -> 12.0 * _size)
        :> obj
     