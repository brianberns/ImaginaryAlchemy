namespace ImaginaryAlchemy

open Suave
open Suave.Logging
open Suave.Operators

open Fable.Remoting.Server
open Fable.Remoting.Suave

/// Option computation expression builder.
type OptionBuilder() =
    member _.Bind(opt, f) = Option.bind f opt
    member _.Return(x) = Some x
    member _.ReturnFrom(opt : Option<_>) = opt
    member _.Zero() = None

[<AutoOpen>]
module OptionBuilder =

    /// Option computation expression builder.
    let option = OptionBuilder()

type StateValue =
    {
        First : Concept
        Second : Concept
        Generation : int
    }

type State = Map<Concept, StateValue>

module State =

    let empty : State = Map.empty

module Program =

    let mutable state = State.empty

    let combine oracle (first, second) =
        async {
            let! conceptOpt =
                Oracle.combine oracle first second
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
