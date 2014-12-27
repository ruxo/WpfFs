namespace RZ.Wpf.Controls

open System
open System.Linq
open System.ComponentModel
open System.Windows
open System.Windows.Controls

type public Inclusion() as me = 
    inherit System.Windows.Controls.Panel()

    let loadUiElement filename =
        match RZ.Wpf.XamlLoader.LoadWpfFromFile filename with
        | :? UIElement as ele -> Some ele
        | _ -> None

    let loadXamlRuntimeMode filename (this: Inclusion) =
        this.Children.Clear()
        match loadUiElement filename with
        | None -> ()
        | Some ui -> ignore <| this.Children.Add ui
                   
    let loadXamlDesignMode filename (this: Inclusion) =
        ignore (this.Children.Add <| Label(Content = filename))

    let loadXamlTo = if DesignerProperties.GetIsInDesignMode(me) then loadXamlDesignMode else loadXamlRuntimeMode

    static let OnXamlChanged (o: DependencyObject) (e: DependencyPropertyChangedEventArgs) =
        match o with
        | :? Inclusion as this -> this.Children.Clear()
                                  let value = e.NewValue :?> string
                                  if not (String.IsNullOrEmpty value) then
                                      try
                                          this.LoadXamlTo value this
                                      with
                                      | :? System.IO.FileNotFoundException ->
                                          let error = sprintf "Cannot find %s in %s." (value) (System.IO.Directory.GetCurrentDirectory())
                                          ignore <| this.Children.Add( System.Windows.Controls.Label(Content = error))
        | _ -> ()
    
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