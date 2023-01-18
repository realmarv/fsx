#!/usr/bin/env fsharpi

open System
open System.IO
open System.Text

#r "System.Configuration"
open System.Configuration

#load "../Fsdk/Misc.fs"
open Fsdk

let ReplaceInFile (file: FileInfo) (oldString: string) (newString: string) =
    let hasUtf8Bom(file: FileInfo) =
        let fileContentInBytes = File.ReadAllBytes file.FullName

        fileContentInBytes.Length > 2
        && fileContentInBytes.[..2] = [| 0xEFuy; 0xBBuy; 0xBFuy |]

    let oldText = File.ReadAllText file.FullName
    let newText = oldText.Replace(oldString, newString)

    if newText <> oldText then
        File.WriteAllText(file.FullName, newText, UTF8Encoding(hasUtf8Bom file))

let rec ReplaceInDir
    (dir: DirectoryInfo)
    (oldString: string)
    (newString: string)
    =
    for file in dir.GetFiles() do
        if file.Extension.ToLower() <> ".dll"
           && file.Extension.ToLower() <> ".exe"
           && file.Extension.ToLower() <> ".png" then
            ReplaceInFile file oldString newString

    for subFolder in dir.GetDirectories() do
        if subFolder.Name <> ".git" then
            ReplaceInDir subFolder oldString newString

let args = Misc.FsxOnlyArguments()

let errTooManyArgs =
    "Can only pass two arguments, with optional flag: replace.fsx -f=a.b oldstring newstring"

let note =
    "NOTE: by default, some kind of files/folders will be excluded, e.g.: .git, *.dll, *.png, ..."

if args.Length > 3 then
    Console.Error.WriteLine errTooManyArgs
    Console.WriteLine note
    Environment.Exit 1
elif args.Length < 2 then
    Console.Error.WriteLine
        "Need to pass two arguments: replace.fsx oldstring newstring"

    Console.WriteLine note
    Environment.Exit 1

let firstArg = args.[0]

let particularFile =
    if firstArg.StartsWith "--file=" || firstArg.StartsWith "-f=" then
        let file = firstArg.Substring(firstArg.IndexOf("=") + 1) |> FileInfo

        if not file.Exists then
            failwithf "File '%s' doesn't exist" file.FullName

        file |> Some
    else
        if args.Length = 3 then
            Console.Error.WriteLine errTooManyArgs
            Console.WriteLine note
            Environment.Exit 1
            failwith "Unreachable"

        None

match particularFile with
| None ->
    let startDir = DirectoryInfo(Directory.GetCurrentDirectory())
    let oldString, newString = args.[0], args.[1]
    ReplaceInDir startDir oldString newString
| Some file ->
    let oldString, newString = args.[1], args.[2]
    ReplaceInFile file oldString newString
