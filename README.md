## Introduction

This library is built as an alternative to use XAML in F#.  

### Nuget repository

Library is available at [Nuget](https://www.nuget.org/packages/RZ.Wpf/)

### Code Behind in F#

While `FsXaml`'s Type Provider is useful and simple, it does not give ease to work with XAML compared with C#.  I propose an alternative solution by following example.  Suppose we have a "Hello, World!" XAML as a project `Resource`.

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

### Handle Routed Events (1.0.1)

Version 1.0.1 has introduced a way to handle routed events.  This works between View Model, which is assigned to `DataContext`, and code behind (as the view logic).  This design uses some idea from MVVM of `FsXaml` and `FsXaml.ViewModel` libraries.

Suppose that our sample XAML utilizes Command pattern.

**Sample.xaml**
```xaml
<Window xmlns="http://schemas.microsoft.com/netfx/2009/xaml/presentation"
        xmlns:model="clr-namespace:Sample;assembly=Sample">
  <Window.DataContext>
    <model:MainWindowModel />
  </Window.DataContext>
  <StackPanel>
    <Label>Hello, world!</Label>
    <Button Command="Open" CommandParameter="http://google.com/">Click me!</Button>
  </StackPanel>
</Window>
```

**Codebehind.fs**

Here we create a _view model_ and use it in the XAML.  The model is a bridge between View and Model (or Controller).

```fsharp
namespace Sample
open System.Windows
open System.Windows.Input
open RZ.Wpf.CodeBehind

type MainWindowEvent =
| Help of string

type MainWindowModel() =
  let handler = function
    | Help url -> System.Diagnostics.Process.Start url |> ignore

  let mapper =
    [ ApplicationCommands.Open |> CommandMap.to' (fun p -> Help (p :?> string)) ]
    |> CommandControlCenter handler
    
  interface ICommandHandler with
    member __.ControlCenter = mapper

type MainWindow() as me =
  inherit Window()
  do me.InitializeCodeBehind("Sample.xaml")
  do me.InstallCommandForwarder()   // install command forwarder to `DataContext`'s ICommandHandler.
```

With this sample, when you click the button it will raise command `Open` which will be forward to `ICommandHandler` via `DataContext`.  The `ICommandHandler` will return command handler which, in this case, is created by pre-defined function in the library.  Finally, the handler is called with the raised command accordingly.

### XamlLoader changes (1.1.0)

`XamlLoader` module is re-written and now has simple 4 functions to create/load XAML object.

| Function                             | Description                          |
| ------------------------------------ | ------------------------------------ |
| `createFromFile: string -> obj option` | Load XAML from file and instantiate. |
| `createFromResource: string -> Assembly -> obj option` | Load XAML from the assembly's resource |
| `createFromXaml: string -> obj` | Create directly from XAML string. |
| `createFromXamlObj: obj -> string -> obj` | Create directly from XAML string with root object. |

Note that `createFromXaml` and `createFromXamlObj` functions do not return *option*.  These functions throw an exception if XAML text is malformed or incompatible with its code behind class.  This behavior is by design for most of usage, IMO, is in an application itself and bad XAML should be seriously fixed.

#### Other notes
Examples in this project come from the reference of book "WPF 5 Unleashed"
