open System
open System.IO
open Argu

type Arguments =
    | [<ExactlyOnce; AltCommandLine("-t")>]
      Template of path:string
    | [<ExactlyOnce; AltCommandLine("-o")>]
      Output of path:string
    | [<ExactlyOnce; AltCommandLine("-r")>]
      Replacements of replacements_file:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Template _ -> "Specify a template file."
            | Output _ -> "Specify an output file."
            | Replacements _ -> "Specify a replacements file. This file must have the search strings on the first line and the replacements on the following lines. If more than one term is to be replaced, separate both search string and replacements by double pipe (||). \nExample content:\nOld-A||Old-B||Old-C\nNew-A1||New-B1||New-C1\nNew-A2||New-B2||New-C2"

type Replacement = 
    { Old: string
      New: string }

let replace (template: string) { Old = old; New =  new' } =
    template.Replace(old, new')

let applyTemplate template replacements = 
    List.fold replace template replacements
    
let generateReplacements template replacementsList = 
    replacementsList |> List.map (applyTemplate template)

let getTemplate filePath =
    try File.ReadAllText(filePath) |> Ok
    with e -> e |> Error

let createOutputFile filePath contentList = 
    try File.WriteAllLines(filePath, contentList |> Array.ofList) |> Ok
    with e -> e |> Error

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    0 // return an integer exit code
