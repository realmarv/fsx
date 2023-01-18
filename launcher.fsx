// this script is the equivalent of unixy launcher.sh script but for windows (where we're sure a FSI exists)

open System
open System.IO
open System.Text
open System.Linq
open System.Diagnostics

#r "System.Configuration"
open System.Configuration

#load "Fsdk/Misc.fs"
#load "Fsdk/Process.fs"

open Fsdk
open Fsdk.Process

type FsxScriptDiscoveryInfo =
    | FsxFsxNotFoundYet
    | FsxFsxFoundButNoFsxScriptFoundYet
    | FsxFsxFoundAndFsxScriptNameSupplied of _userScriptName: string

let SplitArgsIntoFsxcArgsAndUserArgs() : seq<string> * string * seq<string> =
    let rec userArgsInternal
        (fsxScriptDiscoverySoFar: FsxScriptDiscoveryInfo)
        (fsxcArgsSoFar: List<string>)
        (userArgsSoFar: List<string>)
        (nextArgs: List<string>)
        : seq<string> * string * seq<string> =
        match nextArgs, fsxScriptDiscoverySoFar with
        | [], FsxFsxFoundAndFsxScriptNameSupplied userScriptName ->
            let finalFscxArgs = fsxcArgsSoFar |> List.rev |> Seq.ofList
            let finalUserArgs = userArgsSoFar |> List.rev |> Seq.ofList
            finalFscxArgs, userScriptName, finalUserArgs
        | [], _ -> failwith "fsx.fsx not found"
        | head :: tail, fsxScriptDiscoverySoFar ->
            match fsxScriptDiscoverySoFar, head with
            | FsxFsxNotFoundYet, arg when
                arg
                    .Split(Path.DirectorySeparatorChar)
                    .Last()
                    .EndsWith "fsx.fsx"
                ->
                if not fsxcArgsSoFar.IsEmpty then
                    failwith
                        "no fsxc args should have been added yet if FsxFsxNotFoundYet"

                if not userArgsSoFar.IsEmpty then
                    failwith
                        "no fsxc args should have been added yet if FsxFsxNotFoundYet"

                userArgsInternal
                    FsxFsxFoundButNoFsxScriptFoundYet
                    List.Empty
                    List.Empty
                    tail
            | FsxFsxNotFoundYet, _likelyFsiExePath ->
                if not fsxcArgsSoFar.IsEmpty then
                    failwith
                        "no fsxc args should have been added yet if FsxFsxNotFoundYet"

                if not userArgsSoFar.IsEmpty then
                    failwith
                        "no fsxc args should have been added yet if FsxFsxNotFoundYet"

                userArgsInternal FsxFsxNotFoundYet List.empty List.Empty tail
            | FsxFsxFoundButNoFsxScriptFoundYet, arg when
                arg
                    .Split(Path.DirectorySeparatorChar)
                    .Last()
                    .EndsWith ".fsx"
                ->
                if not userArgsSoFar.IsEmpty then
                    failwith
                        "no fsxc args should have been added yet if FsxFsxNotFoundYet"

                userArgsInternal
                    (FsxFsxFoundAndFsxScriptNameSupplied arg)
                    fsxcArgsSoFar
                    List.empty
                    tail
            | FsxFsxFoundButNoFsxScriptFoundYet, fsxcArg ->
                if not userArgsSoFar.IsEmpty then
                    failwith
                        "no fsxc args should have been added yet if FsxFsxFoundButNoFsxScriptFoundYet"

                userArgsInternal
                    FsxFsxFoundButNoFsxScriptFoundYet
                    (fsxcArg :: fsxcArgsSoFar)
                    List.empty
                    tail
            | (FsxFsxFoundAndFsxScriptNameSupplied userScriptName), userArg ->
                userArgsInternal
                    (FsxFsxFoundAndFsxScriptNameSupplied userScriptName)
                    fsxcArgsSoFar
                    (userArg :: userArgsSoFar)
                    tail


    Environment.GetCommandLineArgs()
    |> List.ofArray
    |> userArgsInternal FsxFsxNotFoundYet List.empty List.empty

let assemblyExecutableExtension =
#if !LEGACY_FRAMEWORK
    "dll"
#else
    "exe"
#endif

let InjectBinSubfolderInPath(userScript: FileInfo) =
    if not(userScript.FullName.EndsWith ".fsx") then
        failwithf
            "Assertion failed: %s should end with .fsx"
            userScript.FullName

    let binPath =
        match userScript.FullName.LastIndexOf Path.DirectorySeparatorChar with
        | index when index >= 0 ->
            let path = userScript.FullName.Substring(0, index)

            sprintf
                "%s%sbin%s%s.exe"
                path
                (Path.DirectorySeparatorChar.ToString())
                (Path.DirectorySeparatorChar.ToString())
                (Path.GetFileName userScript.FullName)
        | _ ->
            sprintf
                "bin%s%s.exe"
                (Path.DirectorySeparatorChar.ToString())
                (Path.GetFileName userScript.FullName)

    FileInfo binPath


let thisScriptFileName = __SOURCE_FILE__

if thisScriptFileName <> "fsx.fsx" then
    failwith
        "this launcher should have been renamed to fsx.fsx at install time; please report this bug"

let sourceDir = DirectoryInfo __SOURCE_DIRECTORY__

let fsxcAssembly =
    Path.Combine(
        sourceDir.FullName,
        sprintf "fsxc.%s" assemblyExecutableExtension
    )
    |> FileInfo

if not fsxcAssembly.Exists then
    failwith
        "fsxc assembly not found in the same folder as this launcher; please report this bug"

let fsxcArgs, userScript, userArgs = SplitArgsIntoFsxcArgsAndUserArgs()

let userScriptFile = FileInfo userScript

#if !LEGACY_FRAMEWORK
let fsxcCmd =
    {
        Command = "dotnet"
        Arguments =
            sprintf
                "\"%s\" %s %s"
                fsxcAssembly.FullName
                (String.Join(" ", fsxcArgs))
                userScript
    }
#else
let fsxcCmd =
    {
        Command = fsxcAssembly.FullName
        Arguments = sprintf "%s %s" (String.Join(" ", fsxcArgs)) userScript
    }
#endif

let proc = Process.Execute(fsxcCmd, Echo.Off)
proc.UnwrapDefault() |> ignore<string>

let finalLaunch =
    {
        Command = (InjectBinSubfolderInPath userScriptFile).FullName
        Arguments = String.Join(" ", userArgs)
    }

let finalProc = Process.Execute(finalLaunch, Echo.OutputOnly)
// FIXME: maybe using a .fsx file as a launcher in Windows wasn't the best idea after all, because it means
// that, on Windows, fsx will run fsharpi while the compiled user script is running, which means that the
// memory gains of using fsx instead of fsharpi (as explained in the ReadMe.md file) don't exist for this OS
// (while in Unix, i.e. Linux and macOS, they exist because we use a bash script which uses 'exec')
match finalProc.Result with
| Error(exitCode, _errOutput) -> Environment.Exit exitCode
| _ -> Environment.Exit 0
