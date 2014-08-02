namespace RZ.Wpf.Controls

open System
open System.Linq
open System.Windows
open System.Windows.Media

type public Inclusion() = 
    inherit System.Windows.Controls.Panel()

    static let OnXamlChanged (o: DependencyObject) (e: DependencyPropertyChangedEventArgs) =
        match o with
        | :? Inclusion as this -> this.Children.Clear()
                                  let value = e.NewValue :?> string
                                  if value <> null then
                                      try
                                          match RZ.Wpf.XamlLoader.LoadWpf(value) :?> UIElement with
                                          | null -> ()
                                          | newChild -> ignore <| this.Children.Add(newChild)
                                      with
                                      | :? System.IO.FileNotFoundException ->
                                          let error = sprintf "Cannot find %s in %s." (value) (System.IO.Directory.GetCurrentDirectory())
                                          ignore <| this.Children.Add( System.Windows.Controls.Label(Content = error))
        | _ -> ()
    
    static let xamlProperty = DependencyProperty.Register("Xaml", typeof<string>, typeof<Inclusion>,
                                    FrameworkPropertyMetadata(PropertyChangedCallback(OnXamlChanged))
                              )
    static member XamlProperty with get() = xamlProperty

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