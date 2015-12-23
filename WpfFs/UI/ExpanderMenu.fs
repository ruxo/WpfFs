namespace WpfFs.UI

open System.Windows.Controls
open RZ.Wpf.CodeBehind

type ExpanderMenu() as me =
  inherit UserControl()

  do me.InitializeCodeBehind("ExpanderMenu.xaml")
