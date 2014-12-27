namespace RZ.Wpf.Controls

open System
open System.Linq
open System.ComponentModel
open System.Windows
open System.Windows.Controls
open RZ.Wpf

type public Inclusion() as me = 
    inherit System.Windows.Controls.Panel()
    let designMode = DesignerProperties.GetIsInDesignMode me

    let loadUIElementFromResource filename =
        let asm = System.Reflection.Assembly.GetEntryAssembly()
        in  XamlLoader.loadFromResource0 filename None asm

    let loadUiElement filename =
        try
            match RZ.Wpf.XamlLoader.LoadWpfFromFile filename with
            | :? UIElement as ele -> Some ele
            | _ -> None
        with
        | :? System.IO.FileNotFoundException ->
            Some (loadUIElementFromResource filename :?> UIElement)
            (*
            let error = sprintf "Cannot find %s in %s." filename (System.IO.Directory.GetCurrentDirectory())
            Some (Label(Content = error) :> UIElement)
            *)

    let loadXamlRuntimeMode filename =
        match loadUiElement filename with
        | None -> []
        | Some ui -> [ui]
                   
    let loadXamlDesignMode filename =
        [Label(Content = filename) :> UIElement]

    let loadXamlTo = if designMode then loadXamlDesignMode else loadXamlRuntimeMode

    static let OnXamlChanged (o: DependencyObject) (e: DependencyPropertyChangedEventArgs) =
        match o with
        | :? Inclusion as this -> this.Children.Clear()
                                  let value = e.NewValue :?> string
                                  if not (String.IsNullOrEmpty value) then
                                      this.LoadXamlTo value
                                      |> List.iter (fun ui -> ignore (this.Children.Add ui))
        | _ -> failwith "Unexpected"
    
    static let xamlProperty = DependencyProperty.Register("Xaml", typeof<string>, typeof<Inclusion>,
                                    FrameworkPropertyMetadata(PropertyChangedCallback(OnXamlChanged))
                              )
    static member XamlProperty with get() = xamlProperty

    member private me.LoadXamlTo = loadXamlTo

    member this.Xaml with get() = this.GetValue(Inclusion.XamlProperty) :?> string
                     and  set (value: string) = this.SetValue(Inclusion.XamlProperty, value)

    override this.MeasureOverride(availableSize) =
        this.Children.Cast<UIElement>()
        |> Seq.iter (fun child -> child.Measure(Size(Double.PositiveInfinity, Double.PositiveInfinity)))

        Size(0.0,0.0)

    override this.ArrangeOverride(finalSize) =
        this.Children.Cast<UIElement>()
        |> Seq.iter (fun child -> child.Arrange(Rect(Point(0.0,0.0), finalSize)))

        finalSize