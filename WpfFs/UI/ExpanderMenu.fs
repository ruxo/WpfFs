namespace WpfFs.UI

open System.Windows.Controls
open System.Windows
open System.Windows.Data
open RZ
open RZ.Foundation
open RZ.Wpf.CodeBehind

type MenuItem = string * string
type ExpanderMenuItem = string * MenuItem list

type ExpanderMenu() as me =
  inherit UserControl()

  do me.InitializeCodeBehind("ExpanderMenu.xaml")

  let itemContainer = me.FindName("item_container").cast<ListBox>()

  do Binding(Source = me, Path = PropertyPath("ItemsSource"))
     |> Pair.sndWith(ItemsControl.ItemsSourceProperty)
     |> Pair.call(itemContainer.SetBinding)
     |> ignore

  static let itemsSourceProperty =
      DependencyProperty.Register( "ItemsSource"
                                 , typeof<ExpanderMenuItem seq>
                                 , typeof<ExpanderMenu> )

  static member ItemsSourceProperty = itemsSourceProperty

  member me.ItemsSource with get() = me.GetValue itemsSourceProperty :?> ExpanderMenuItem seq
                        and set (v: ExpanderMenuItem seq) = me.SetValue(itemsSourceProperty, v)
