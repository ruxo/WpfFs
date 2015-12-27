namespace RZ.NetWrapper

open System
open System.IO

[<AutoOpen>]
module Utils =
  let (+=>) f g = f >> (>>) g // Read: put 1st param in f, 2nd in g, and then 2nd in f
  let dispose d = (d:IDisposable).Dispose()

module Assembly =
    open System.Reflection

    let getManifestResourceStream resourceName asm = (asm:Assembly).GetManifestResourceStream(resourceName)
    let getCallingAssembly() = System.Reflection.Assembly.GetCallingAssembly()

module DirInfo =
    let getFiles pattern dir = (dir:DirectoryInfo).GetFiles pattern

module Random =
    let next rand = (rand:Random).Next

module Seq =
    let pickSome predicate = Seq.where predicate >> (Seq.tryPick Some) 

module Xml =
    open System.Xml.Linq

    // basic wrappers
    let attrLocalName xa = (xa:XAttribute).Name.LocalName 
    let attrs xe = (xe:XElement).Attributes()
    let attrValue xa = (xa:XAttribute).Value
    let elementLocalName xe = (xe:XElement).Name.LocalName
    let getDescendants xe = (xe:XElement).Descendants()
    let getElements xe = (xe:XElement).Elements()
    let content xe = (xe:XElement).Value

    // extended util functions
    let isAttrName = (=) >> (>>) attrLocalName
    let attr name = (isAttrName >> Seq.pickSome >> (>>) attrs) name
    let attrValueFromName name = (attr >> (<<) (Option.map attrValue >> Option.get)) name

    let filterElementByName name = Seq.where (elementLocalName >> (=) name)
    let descendant name =  getDescendants >> filterElementByName name
    let childElement name xe = xe |> getElements |> Seq.pickSome (elementLocalName >> (=) name)
    let getChildContent name = childElement name >> Option.get >> content
    let getChildrenByName name = getElements >> filterElementByName name
