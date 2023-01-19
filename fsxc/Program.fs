open FSX.Compiler

[<EntryPoint>]
let main argv =
    try
        Program.Main argv
    finally
#if LEGACY_FRAMEWORK
        if nugetExeTmpLocation.IsValueCreated then
            nugetExeTmpLocation.Value.Delete()
#else
        ()
#endif