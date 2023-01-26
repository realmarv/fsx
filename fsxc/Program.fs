open FSX.Compiler

[<EntryPoint>]
let main argv =
    try
        Program.Main argv
    finally
#if LEGACY_FRAMEWORK
        if Program.nugetExeTmpLocation.IsValueCreated then
            Program.nugetExeTmpLocation.Value.Delete()
#else
        ()
#endif
