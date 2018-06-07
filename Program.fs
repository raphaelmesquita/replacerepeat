open System
open System.IO
open Argu
open Result.Operators

type Arguments =
    | [<ExactlyOnce; AltCommandLine("-t")>]
      Template of path:string
    | [<ExactlyOnce; AltCommandLine("-r")>]
      Replacements of path:string
    | [<ExactlyOnce; AltCommandLine("-o")>]
      Output of path:string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Template _ -> "Specify a template file."
            | Replacements _ -> "Specify a replacements file. This file must have the search strings on the first line and the replacements on the following lines. If more than one term is to be replaced, separate both search string and replacements by double pipe (||). \nExample content:\nOld-A||Old-B||Old-C\nNew-A1||New-B1||New-C1\nNew-A2||New-B2||New-C2"
            | Output _ -> "Specify an output file."

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
    with e -> e.Message |> Error

let createOutputFile filePath contentList = 
    try File.WriteAllLines(filePath, contentList |> Array.ofList) |> Ok
    with e -> e.Message |> Error

let getReplacements filePath =
    try File.ReadAllLines(filePath) |> List.ofArray |> Ok
    with e -> e.Message |> Error    

let splitLine (line: string) =
    line.Split("||") |> List.ofArray

let parseReplacements (textLines: string list) =
    match textLines with
    | [] -> 
        Error "The replacements file is empty."
    | firstLine :: replacementLines ->
        let oldStrings = splitLine firstLine
        try
            let getReplacements line =
                line
                |> splitLine
                |> List.zip oldStrings
                |> List.map (fun (o, n) -> { Old = o; New = n })
            replacementLines
            |> List.map getReplacements
            |> Ok
        with 
        | :? ArgumentException ->
            "Invalid replacements file: searchString and replacements don't match in length." |> Error
        | e -> 
            e.Message |> Error

let replaceAndRepeat templatePath outputPath replacementsPath =
    result {
        let! template = getTemplate templatePath
        let! replacementLines = getReplacements replacementsPath
        let! replacementsList = parseReplacements replacementLines
        let contentList = generateReplacements template replacementsList
        do! createOutputFile outputPath contentList
    }

[<EntryPoint>]
let main argv =
    let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<Arguments>(programName = "replacerepeat", errorHandler = errorHandler)

    let results = parser.ParseCommandLine argv
    let templatePath = results.GetResult(Template)
    let outputPath = results.GetResult(Output)
    let replacementsPath = results.GetResult(Replacements)

    match replaceAndRepeat templatePath outputPath replacementsPath with
    | Ok _ -> 
        printf "Output file %s successfully created." outputPath
        0
    | Error msg ->
        printf "%s" msg
        -1 // return an integer exit code
