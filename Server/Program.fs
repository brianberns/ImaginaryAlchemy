namespace ImaginaryAlchemy

open System.Collections.Generic

open Suave
open Suave.Logging
open Suave.Operators

open Fable.Remoting.Server
open Fable.Remoting.Suave

module Program =

    let private genDict =
        [
            "Earth", 0
            "Air", 0
            "Fire", 0
            "Water", 0
            "Steam", 1
        ]
            |> Seq.map KeyValuePair
            |> Dictionary<_, _>

    let private memoize oracle =
        Oracle.combine oracle
            |> uncurry
            |> Prelude.memoize
            |> curry

    let private apply combine first second =
        lock genDict (fun () ->
            option {
                let! genFirst = Dictionary.tryFind first genDict
                let! genSecond = Dictionary.tryFind second genDict
                let! concept = combine first second
                let newGen = (max genFirst genSecond) + 1
                let isNew =
                    match Dictionary.tryFind concept genDict with
                        | Some oldGen when oldGen >= newGen ->
                            false
                        | _ ->
                            genDict[concept] <- newGen
                            true
                return concept, isNew
            })

    try

        let alchemyApi =
            {
                Combine =
                    let combine = memoize (Oracle.create ())
                    fun (first, second) ->
                        async {
                            return apply combine first second
                        }
            }

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
        startWebServer config service

    with exn -> printfn $"{exn.Message}"
