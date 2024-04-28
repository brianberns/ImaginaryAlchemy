namespace ImaginaryAlchemy

open Suave
open Suave.Logging
open Suave.Operators

open Fable.Remoting.Server
open Fable.Remoting.Suave

module Program =

    let combine oracle (conceptA, conceptB) =
        async {
            let! conceptOpt =
                Oracle.combine oracle conceptA conceptB
            return conceptOpt
                |> Option.map (fun concept ->
                    {
                        Concept = concept
                        Generation = 0
                    })
        }

    try

        let alchemyApi =
            let oracle = Oracle.create ()
            { Combine = combine oracle }

            // create the web service
        let service : WebPart =
            let logger = Targets.create LogLevel.Info [||]
            (Remoting.createApi()
                |> Remoting.fromValue alchemyApi
                |> Remoting.buildWebPart)
                >=> Filters.logWithLevelStructured
                    LogLevel.Info
                    logger
                    Filters.logFormatStructured

            // start the web server
        let config =
            { defaultConfig with
                bindings = [ HttpBinding.createSimple HTTP "localhost" 5000 ] }
        startWebServer config service

    with exn -> printfn $"{exn.Message}"
