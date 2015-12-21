namespace WpfFs.Models

open System.Collections.ObjectModel

type Person() =
    member val Id = 0 with get, set
    member val Name = "" with get, set

type PersonCollection() = inherit ObservableCollection<Person>()

type DataBindingMode = SingleThread = 0 | MultiThread = 1
type DataBindingCollectionMode = ObserverableCollection = 0 | WpfObservableCollection = 1

type DatabindingSampleModel() as me =
    inherit FSharp.ViewModule.ViewModelBase()

    let data = me.Factory.Backing(<@ me.Data @>, PersonCollection())
    let bindingMode = me.Factory.Backing(<@ me.DataBindingMode @>, DataBindingMode.SingleThread)

    member __.Data with get() = data.Value and set v = data.Value <- v
    member __.DataBindingMode with get() = bindingMode.Value and set v = bindingMode.Value <- v
    member val CollectionMode = [DataBindingCollectionMode.ObserverableCollection; DataBindingCollectionMode.WpfObservableCollection] with get

