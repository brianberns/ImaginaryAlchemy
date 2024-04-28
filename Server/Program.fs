namespace ImaginaryAlchemy

open Suave
open Suave.Logging
open Suave.Operators

open Fable.Remoting.Server
open Fable.Remoting.Suave

module Program =

    let combine (conceptA, conceptB) =
        async {
            let! conceptOpt =
                Oracle.combine conceptA conceptB
            return conceptOpt
                |> Option.map (fun concept ->
                    concept, 0)
        }

    let alchemyApi : IAlchemyApi =
        {
            Combine = combine
        }

    try

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
                bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" 5000 ] }
        // startWebServer config service
        ()

    with exn -> printfn $"{exn.Message}"
