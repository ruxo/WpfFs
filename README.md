## Introduction

This library is built as an alternative to use XAML in F#.  While `FsXaml`'s Type Provider is useful and simple, it does not give ease to work with XAML compared with C#.  I propose an alternative solution by following example.  Suppose we have a "Hello, World!" XAML as a project `Resource`.

**Sample.xaml**
```xaml
<Window xmlns="http://schemas.microsoft.com/netfx/2009/xaml/presentation">
  <StackPanel>
    <Label>Hello, world!</Label>
    <Button>Click me!</Button>
  </StackPanel>
</Window>
```

To declare type for this window with FsXaml Type Provider.
```fsharp
type MainWindow = FsXaml.XAML<"Sample.xaml">
```

But using this library, the declaration becomes following:
```fsharp
open System.Windows
open RZ.Wpf.CodeBehind

type MainWindow() as me =
  inherit Window()
  do me.InitializeCodeBehind("Sample.xaml")
```

It is longer, but it gives you flexibility to extend the control like in C# (e.g. make a User Control with custom properties).

#### Other notes
Examples in this project come from the reference of book "WPF 5 Unleashed"
