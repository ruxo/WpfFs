﻿namespace RZ.Wpf.Controls

open System
open System.Linq
open System.ComponentModel
open System.Windows
open System.Windows.Controls
open RZ.Foundation
open RZ.Wpf

type public Inclusion() as me = 
    inherit System.Windows.Controls.Panel()
    let designMode = DesignerProperties.GetIsInDesignMode me

    let loadUIElementFromResource resourceName =
        System.Reflection.Assembly.GetEntryAssembly() |> XamlLoader.createFromResource resourceName

    let loadUiElement filename =
        XamlLoader.createFromFile filename
          |> Option.orTry (fun _ -> loadUIElementFromResource filename)
          |> Option.bind tryCast<UIElement>

    let loadXamlRuntimeMode = loadUiElement >> Option.map List.singleton >> Option.getOrElse (constant [])
                   
    let loadXamlDesignMode filename =
        [Label(Content = filename) :> UIElement]

    let loadXamlTo = if designMode then loadXamlDesignMode else loadXamlRuntimeMode

    static let onXamlChanged (o: DependencyObject) (e: DependencyPropertyChangedEventArgs) =
        match o with
        | :? Inclusion as this -> this.Children.Clear()
                                  let value = e.NewValue :?> string
                                  if not (String.IsNullOrEmpty value) then
                                      this.LoadXamlTo value
                                      |> List.iter (this.Children.Add >> ignore)
        | _ -> failwith "Unexpected"
    
    static let xamlProperty = DependencyProperty.Register("Xaml", typeof<string>, typeof<Inclusion>,
                                    FrameworkPropertyMetadata(PropertyChangedCallback(onXamlChanged))
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